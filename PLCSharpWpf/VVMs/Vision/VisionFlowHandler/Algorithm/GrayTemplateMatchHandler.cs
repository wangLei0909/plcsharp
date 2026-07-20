using OpenCvSharp;
using System.Windows.Media;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Algorithm
{
    /// <summary>
    /// 灰度模板匹配 Handler —— 灰度模板旋转 + CCorrNormed 匹配。
    /// 预生成多角度灰度模板（BBox裁剪），在源图上做归一化互相关匹配。
    /// </summary>
    public class GrayTemplateMatchHandler : IVisionFlowHandler
    {
        public VisionFlowType Type => VisionFlowType.灰度模板匹配;

        public bool Execute(VisionFunction func, VisionFlow item)
        {
            var src = func.Src;
            if (src == null || src.Empty()) throw new Exception("请先获取图片！");

            // ===== 1. 读取参数 =====
            string roiShape = item.StringParams.TryGetValue("ROIShape", out var rs)
                && !string.IsNullOrEmpty(rs) ? rs : "矩形";

            if (!item.DoubleParams.TryGetValue("TemplateCenterX", out double centerX) ||
                !item.DoubleParams.TryGetValue("TemplateCenterY", out double centerY))
                throw new Exception("模板未配置，请先画选区截取模板！");

            double templateW, templateH;
            switch (roiShape)
            {
                case "矩形":
                    if (!item.DoubleParams.TryGetValue("TemplateLeft", out double tLeft) ||
                        !item.DoubleParams.TryGetValue("TemplateTop", out double tTop) ||
                        !item.DoubleParams.TryGetValue("TemplateWidth", out templateW) ||
                        !item.DoubleParams.TryGetValue("TemplateHeight", out templateH))
                        throw new Exception("矩形模板参数缺失！");
                    centerX = tLeft + templateW / 2.0;
                    centerY = tTop + templateH / 2.0;
                    break;
                case "旋转矩形":
                    if (!item.DoubleParams.TryGetValue("TemplateWidth", out templateW) ||
                        !item.DoubleParams.TryGetValue("TemplateHeight", out templateH))
                        throw new Exception("旋转矩形模板参数缺失！");
                    break;
                case "圆形":
                    if (!item.DoubleParams.TryGetValue("TemplateRadius", out double radius))
                        throw new Exception("圆形模板参数缺失！");
                    templateW = radius * 2; templateH = radius * 2;
                    break;
                default:
                    throw new Exception($"不支持的 ROI 形状：{roiShape}");
            }

            double minAngle   = item.DoubleParams.TryGetValue("MinAngle",  out double minA) ? minA : 0;
            double maxAngle   = item.DoubleParams.TryGetValue("MaxAngle",  out double maxA) ? maxA : 360;
            double angleStep  = item.DoubleParams.TryGetValue("AngleStep", out double aStep) ? Math.Max(aStep, 0.1) : 5;
            // CCorrNormed: 分数越高越好，阈值是最低分
            double scoreMin   = item.DoubleParams.TryGetValue("MatchScoreMax", out double msm) ? msm : 0.3;
            string templateName = item.StringParams.TryGetValue("TemplateName", out var tn)
                && !string.IsNullOrEmpty(tn) ? tn : "GrayMatch_Template";
            string posVarName = item.StringParams.TryGetValue("GrayTemplateMatch_PosVar", out var pn)
                && !string.IsNullOrEmpty(pn) ? pn : "灰度模板匹配_Pos";

            // ===== 资源 =====
            Mat graySrc      = null;
            Mat grayTemplate = null;
            Mat templateRoi  = null;
            Mat bestResult   = null;
            int  totalTemplates = 0;

            try
            {
                // ===== 2. 加载模板 =====
                var tmpl = func.LocalImageDatas.FirstOrDefault(m => m.Name == templateName);
                if (tmpl?.Mat == null || tmpl.Mat.Empty())
                    throw new Exception("模板提取失败，请先画选区截取模板！");
                templateRoi = tmpl.Mat.Clone();

                // ===== 3. 灰度化 =====
                graySrc      = ToGray(src);
                grayTemplate = ToGray(templateRoi);
                if (grayTemplate.Width < 16 || grayTemplate.Height < 16)
                    throw new Exception($"模板太小（{grayTemplate.Width}x{grayTemplate.Height}），请画大一些的选区！");

                // ===== 4. 搜索区域 =====
                double range = Math.Max(templateW, templateH) * 2.0;
                int rgnX = Math.Max(0, (int)(centerX - range / 2));
                int rgnY = Math.Max(0, (int)(centerY - range / 2));
                int rgnW = Math.Min((int)range, graySrc.Width  - rgnX);
                int rgnH = Math.Min((int)range, graySrc.Height - rgnY);
                using Mat searchRegion = graySrc[new OpenCvSharp.Rect(rgnX, rgnY, rgnW, rgnH)].Clone();
                int offX = rgnX, offY = rgnY;

                // ===== 5-6. 角度搜索（粗搜 + 可选精搜） =====
                double bestScore, bestAngle;
                int bestX, bestY, etW, etH;
                MatchAngle(grayTemplate, searchRegion, offX, offY,
                    minAngle, maxAngle, angleStep, scoreMin,
                    out bestScore, out bestAngle, out bestX, out bestY, out etW, out etH, out bestResult, ref totalTemplates);

                if (bestScore < scoreMin)
                    throw new Exception($"未找到可靠匹配（最佳分数 {bestScore:F3} < 阈值 {scoreMin}）！");

                // GetRotationMatrix2D 正角度 = OpenCV 顺时针 → 取反统一为视觉方向
                bestAngle = -bestAngle;

                // ===== 7. ECC 精调（亚像素位置 + 亚角度） =====
                double matchX = bestX + etW / 2.0;
                double matchY = bestY + etH / 2.0;

                // ECC 需要原始灰度模板（不旋转），searchRegion 已在灰度的灰度源图上
                double tplCX = grayTemplate.Width / 2.0, tplCY = grayTemplate.Height / 2.0;
                double eccCos = Math.Cos(bestAngle * Math.PI / 180.0);
                double eccSin = Math.Sin(bestAngle * Math.PI / 180.0);
                // 匹配中心在 searchRegion 中的坐标
                double matchXroi = matchX - offX;
                double matchYroi = matchY - offY;

                using Mat eccWarp = new Mat(2, 3, MatType.CV_32FC1);
                eccWarp.Set<float>(0, 0, (float)eccCos);
                eccWarp.Set<float>(0, 1, (float)-eccSin);
                eccWarp.Set<float>(0, 2, (float)(matchXroi - eccCos * tplCX + eccSin * tplCY));
                eccWarp.Set<float>(1, 0, (float)eccSin);
                eccWarp.Set<float>(1, 1, (float)eccCos);
                eccWarp.Set<float>(1, 2, (float)(matchYroi - eccSin * tplCX - eccCos * tplCY));

                try
                {
                    var io = InputOutputArray.Create(eccWarp);
                    Cv2.FindTransformECC(grayTemplate, searchRegion, io, MotionTypes.Euclidean,
                        new TermCriteria(CriteriaTypes.Eps | CriteriaTypes.Count, 200, 1e-3));

                    eccCos = eccWarp.At<float>(0, 0); eccSin = eccWarp.At<float>(1, 0);
                    double eccTx = eccWarp.At<float>(0, 2), eccTy = eccWarp.At<float>(1, 2);
                    matchX = eccCos * tplCX - eccSin * tplCY + eccTx + offX;
                    matchY = eccSin * tplCX + eccCos * tplCY + eccTy + offY;
                    bestAngle = Math.Atan2(eccSin, eccCos) * 180.0 / Math.PI;
                    etW = grayTemplate.Width; etH = grayTemplate.Height;
                }
                catch
                {
                    // ECC 失败，保留粗搜结果
                }

                // ===== 9. 边界检查 =====
                double hw = etW / 2.0, hh = etH / 2.0;
                double ow = Math.Min(matchX + hw, graySrc.Width)  - Math.Max(matchX - hw, 0);
                double oh = Math.Min(matchY + hh, graySrc.Height) - Math.Max(matchY - hh, 0);
                if (ow < hw || oh < hh)
                    throw new Exception($"匹配结果超出图像边界！");

                // ===== 9. 偏移量 =====
                double offsetX = matchX - centerX;
                double offsetY = matchY - centerY;

                // ===== 10. 存储结果 =====
                func.Params.ResultDoubles["GrayMatch_MatchX"] = Math.Round(matchX, 2);
                func.Params.ResultDoubles["GrayMatch_MatchY"] = Math.Round(matchY, 2);
                func.Params.ResultDoubles["GrayMatch_Angle"] = Math.Round(bestAngle, 1);
                func.Params.ResultDoubles["GrayMatch_Score"] = Math.Round(bestScore, 4);
                func.Params.ResultDoubles["GrayMatch_Count"] = totalTemplates;
                func.Params.ResultDoubles["GrayMatch_OffsetX"] = Math.Round(offsetX, 2);
                func.Params.ResultDoubles["GrayMatch_OffsetY"] = Math.Round(offsetY, 2);

                // ===== 11. 局部变量（位置 + 偏移） =====
                var pv = func.Params.Variables.FirstOrDefault(v => v.Name == posVarName);
                if (pv == null)
                {
                    pv = new LocalVariableItem(posVarName, "Pos", new Pos());
                    System.Windows.Application.Current.Dispatcher.Invoke(() => func.Params.Variables.Add(pv));
                }
                pv.RawValue = new Pos(Math.Round(matchX, 2), Math.Round(matchY, 2), 0, Math.Round(bestAngle, 2));

                string offVarName = posVarName + "_Offset";
                var ov = func.Params.Variables.FirstOrDefault(v => v.Name == offVarName);
                if (ov == null)
                {
                    ov = new LocalVariableItem(offVarName, "Pos", new Pos());
                    System.Windows.Application.Current.Dispatcher.Invoke(() => func.Params.Variables.Add(ov));
                }
                ov.RawValue = new Pos(Math.Round(offsetX, 2), Math.Round(offsetY, 2), 0, 0);

                // ===== 12. 可视化 =====
                var magenta = Colors.Magenta;
                var lime    = Colors.Lime;

                double rad = bestAngle * Math.PI / 180.0;
                double cosA = Math.Cos(rad), sinA = Math.Sin(rad);
                double hw0 = templateW / 2.0, hh0 = templateH / 2.0;

                var corners = new (double x, double y)[]
                {
                    (-hw0, -hh0), (hw0, -hh0), (hw0, hh0), (-hw0, hh0)
                };
                var polyPts = corners.Select(c => new System.Windows.Point(
                    matchX + c.x * cosA - c.y * sinA,
                    matchY + c.x * sinA + c.y * cosA)).ToArray();

                func.DrawCommands.Add(DrawCommand.Polygon(polyPts, magenta, 3));
                func.DrawCommands.Add(DrawCommand.Line(matchX - 15, matchY, matchX + 15, matchY, magenta, 2));
                func.DrawCommands.Add(DrawCommand.Line(matchX, matchY - 15, matchX, matchY + 15, magenta, 2));
                func.DrawCommands.Add(DrawCommand.TextBlock(10, 60,
                    $"x:{matchX:F1} y:{matchY:F1} a:{bestAngle:F1}° s:{bestScore:F4}", lime, 18));
                func.DrawCommands.Add(DrawCommand.TextBlock(10, 84,
                    $"offset: ({offsetX:+0.0;-0.0}, {offsetY:+0.0;-0.0})", lime, 14));
                func.DrawCommands.Add(DrawCommand.TextBlock(10, 104,
                    $"[灰度模板匹配] {totalTemplates} templates", lime, 14));

                _ = func.RenderDrawAsync();
                item.Flow.Done = true;
                return true;
            }
            finally
            {
                graySrc?.Dispose();
                grayTemplate?.Dispose();
                templateRoi?.Dispose();
                bestResult?.Dispose();
            }
        }

        private static void MatchAngle(Mat grayTemplate, Mat searchRegion, int offX, int offY,
            double minAngle, double maxAngle, double step, double scoreMin,
            out double bestScore, out double bestAngle, out int bestX, out int bestY,
            out int etW, out int etH, out Mat bestResult, ref int totalCount)
        {
            bestScore = double.MinValue; bestAngle = 0; bestX = 0; bestY = 0;
            etW = 0; etH = 0; bestResult = null;

            int numAngles = Math.Max(1, (int)((maxAngle - minAngle) / step) + 1);
            Point2f tmc = new(grayTemplate.Width / 2f, grayTemplate.Height / 2f);

            for (int i = 0; i < numAngles; i++)
            {
                double ang = minAngle + i * step;
                using var rm = Cv2.GetRotationMatrix2D(tmc, ang, 1.0);
                double ca = Math.Abs(rm.At<double>(0, 0));
                double sa = Math.Abs(rm.At<double>(0, 1));
                int nw = (int)(grayTemplate.Height * sa + grayTemplate.Width * ca);
                int nh = (int)(grayTemplate.Height * ca + grayTemplate.Width * sa);
                rm.Set(0, 2, rm.At<double>(0, 2) + nw / 2.0 - tmc.X);
                rm.Set(1, 2, rm.At<double>(1, 2) + nh / 2.0 - tmc.Y);

                using Mat rot = new Mat();
                Cv2.WarpAffine(grayTemplate, rot, rm, new Size(nw, nh),
                    InterpolationFlags.Linear, BorderTypes.Constant, Scalar.Black);

                using Mat maskB = new Mat();
                Cv2.Threshold(rot, maskB, 1, 255, ThresholdTypes.Binary);
                OpenCvSharp.Rect bbox = Cv2.BoundingRect(maskB);
                if (bbox.Width < 16 || bbox.Height < 16) continue;

                using Mat tpl = rot[bbox].Clone();
                totalCount++;

                int rw = searchRegion.Width  - tpl.Width  + 1;
                int rh = searchRegion.Height - tpl.Height + 1;
                using Mat res = new Mat(rh, rw, MatType.CV_32FC1);
                Cv2.MatchTemplate(searchRegion, tpl, res, TemplateMatchModes.CCorrNormed);
                Cv2.MinMaxLoc(res, out _, out double mv, out _, out Point ml);
                if (mv > bestScore)
                {
                    bestScore = mv;
                    bestAngle = ang;
                    bestX  = ml.X + offX;
                    bestY  = ml.Y + offY;
                    etW = tpl.Width; etH = tpl.Height;
                    bestResult?.Dispose();
                    bestResult = res.Clone();
                }
            }

            if (bestResult == null)
                throw new Exception("所有角度的模板生成均失败！请检查模板图像。");
        }

        private static Mat ToGray(Mat src)
        {
            if (src.Channels() == 1) return src.Clone();
            var g = new Mat();
            Cv2.CvtColor(src, g, ColorConversionCodes.BGR2GRAY);
            return g;
        }
    }
}
