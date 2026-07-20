using OpenCvSharp;
using PLCSharp.VVMs.Vision;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Algorithm
{
    /// <summary>
    /// ORB匹配Handler
    /// </summary>
    public class ORBMatchHandler : IVisionFlowHandler
    {
        /// <summary>
        /// 类型
        /// </summary>
        public VisionFlowType Type => VisionFlowType.ORB匹配;

        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="func">视觉功能</param>
        /// <param name="item">变量项</param>
        /// <returns>返回布尔值</returns>
        public bool Execute(VisionFunction func, VisionFlow item)
        {
            var src = func.Src;
            if (src == null) throw new Exception("请先获取图片！");

            // 读取 ROI 形状
            string roiShape = item.StringParams.TryGetValue("ROIShape", out var rs)
                && !string.IsNullOrEmpty(rs) ? rs : "矩形";

            // 读取公共模板参数
            if (!item.DoubleParams.TryGetValue("TemplateCenterX", out double centerX) ||
                !item.DoubleParams.TryGetValue("TemplateCenterY", out double centerY) ||
                !item.DoubleParams.TryGetValue("Width", out double width) ||
                !item.DoubleParams.TryGetValue("Height", out double height))
                throw new Exception("模板未配置，请先画选区截取模板！");

            // 根据形状读取特有参数
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

            // 读取 ORB 参数
            int nfeatures = item.IntParams.TryGetValue("NFeatures", out int nf) ? Math.Max(nf, 500) : 500;
            double minGoodRatio = item.DoubleParams.TryGetValue("MinGoodRatio", out double mgr) ? Math.Clamp(mgr, 0.01, 1.0) : 0.3;
            int minMatches = item.IntParams.TryGetValue("MinMatches", out int mm) ? Math.Max(mm, 8) : 8;

            // 模板名称
            string templateName = item.StringParams.TryGetValue("TemplateName", out var tn) && !string.IsNullOrEmpty(tn)
                ? tn : "ORB_Template";

            // 动态创建的 Mat，需要 finally 中释放
            Mat graySrc = null, grayTemplate = null;
            Mat templateDescriptors = null;
            Mat srcDescriptors = null;
            Mat mask = null;
            Mat homography = null;
            Mat template = null;
            Mat templateMask = new Mat(); // 圆形模板遮罩（空 Mat = 无掩码）

            try
            {
                // ---------- 1. 提取或缓存模板 ----------
                template = Mat.Zeros((int)height, (int)width, MatType.CV_8UC3);
                var existing = func.LocalImageDatas.FirstOrDefault(m => m.Name == templateName);
                if (existing?.Mat != null && !existing.Mat.Empty())
                {
                    if (roiShape == "旋转矩形")
                    {
                        // 旋转矩形：模板为外接矩形（完全包含旋转矩形），通过 BBox 参数定位
                        if (!item.DoubleParams.TryGetValue("TemplateBBoxLeft", out double bbx) ||
                            !item.DoubleParams.TryGetValue("TemplateBBoxTop", out double bby) ||
                            !item.DoubleParams.TryGetValue("TemplateBBoxWidth", out double bbw) ||
                            !item.DoubleParams.TryGetValue("TemplateBBoxHeight", out double bbh))
                            throw new Exception("旋转矩形模板参数缺失！");
                        OpenCvSharp.Rect bboxRoi = new((int)bbx, (int)bby, (int)bbw, (int)bbh);
                        using var cloneMat = existing.Mat.Clone();
                        template[bboxRoi] = cloneMat;
                    }
                    else
                    {
                        // 矩形 / 圆形：直接用 center - half 定位
                        double left = centerX - templateW / 2.0;
                        double top = centerY - templateH / 2.0;
                        OpenCvSharp.Rect roi = new((int)left, (int)top, (int)templateW, (int)templateH);
                        using var cloneMat = existing.Mat.Clone();
                        template[roi] = cloneMat;
                    }
                }
                else
                {
                    throw new Exception("模板提取失败");
                }

                // 灰度化
                if (src.Channels() == 3)
                {
                    graySrc = new Mat();
                    Cv2.CvtColor(src, graySrc, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    graySrc = src.Clone();
                }

                if (template.Channels() == 3)
                {
                    grayTemplate = new Mat();
                    Cv2.CvtColor(template, grayTemplate, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    grayTemplate = template.Clone();
                }

                // 圆形模板创建遮罩（仅圆形区域参与特征检测）
                if (roiShape == "圆形")
                {
                    templateMask = Mat.Zeros(grayTemplate.Size(), MatType.CV_8UC1);
                    Cv2.Circle(templateMask,
                        (int)(templateW / 2.0), (int)(templateH / 2.0),
                        (int)(templateW / 2.0), Scalar.White, -1);
                }

                // ---------- 2. ORB 特征检测 ----------
                var orb = ORB.Create(nfeatures);
                // 源图特征
                srcDescriptors = new Mat();
                using Mat noMask = new Mat();
                orb.DetectAndCompute(graySrc, noMask, out KeyPoint[] srcKps, srcDescriptors);
                // 模板特征（圆形模板使用遮罩）
                templateDescriptors = new Mat();
                orb.DetectAndCompute(grayTemplate, templateMask, out KeyPoint[] templateKps, templateDescriptors);

                if (templateKps.Length < 2 || srcKps.Length < 2)
                    throw new Exception($"特征点不足：模板 {templateKps.Length} 个，源图 {srcKps.Length} 个！");

                // ---------- 3. 特征匹配 ----------
                var bf = new BFMatcher(NormTypes.Hamming);

                DMatch[][] matchesKnn = bf.KnnMatch(templateDescriptors, srcDescriptors, 2);

                DMatch[] matches = [.. matchesKnn.Where(mt => mt[0].Distance < 0.7 * mt[1].Distance).Select(mt => mt[0])];

                if (matches.Length <= 3)
                    throw new Exception("未找到任何匹配！");


                // 按距离排序，取前 N 个
                var sortedMatches = matches.OrderBy(m => m.Distance).ToArray();

                // 使用距离阈值：保留距离 < 2 * 最小距离 的匹配
                double minDist = sortedMatches[0].Distance;
                double maxDist = Math.Max(minDist * 2.0, 30.0);
                var goodMatches = sortedMatches.Where(m => m.Distance <= maxDist).ToArray();

                if (goodMatches.Length < minMatches)
                    throw new Exception($"优质匹配点不足 ({goodMatches.Length} < {minMatches})，请调整参数或更换图片！");

                // ---------- 4. 计算变换 ----------
                // 模板上的点 (query)
                Point2f[] srcPts = [.. goodMatches.Select(m => templateKps[m.QueryIdx].Pt)];
                // 源图上的点 (train)
                Point2f[] dstPts = [.. goodMatches.Select(m => srcKps[m.TrainIdx].Pt)];

                // 用 FindHomography 找到透视变换矩阵
                mask = new Mat();
                homography = Cv2.FindHomography(InputArray.Create(srcPts), InputArray.Create(dstPts), HomographyMethods.Ransac, 3.0, mask);

                if (homography == null || homography.Empty())
                    throw new Exception("计算变换矩阵失败！");

                // ---------- 5. 提取偏移量 ----------
                double h00 = homography.At<double>(0, 0);
                double h01 = homography.At<double>(0, 1);
                double h10 = homography.At<double>(1, 0);
                double h11 = homography.At<double>(1, 1);

                // 旋转角度（度）
                double angleRad = Math.Atan2(h10, h00);
                double deltaAngle = angleRad * 180.0 / Math.PI;

                // 将模板中心变换到源图中的位置（通过单应性矩阵）
                double[] srcCenter = { centerX, centerY, 1.0 };
                double tx = homography.At<double>(0, 0) * srcCenter[0] + homography.At<double>(0, 1) * srcCenter[1] + homography.At<double>(0, 2);
                double ty = homography.At<double>(1, 0) * srcCenter[0] + homography.At<double>(1, 1) * srcCenter[1] + homography.At<double>(1, 2);
                double tz = homography.At<double>(2, 0) * srcCenter[0] + homography.At<double>(2, 1) * srcCenter[1] + homography.At<double>(2, 2);
                if (Math.Abs(tz) > 1e-10)
                {
                    tx /= tz;
                    ty /= tz;
                }

                double offsetX = tx - centerX;
                double offsetY = ty - centerY;

                // 内点比例
                int inlierCount = Cv2.CountNonZero(mask);
                double inlierRatio = (double)inlierCount / goodMatches.Length;

                // ---------- 6. 存储结果 ----------
                // 绝对位置：匹配到在源图中的位置和角度
                func.Params.ResultDoubles["MatchPosX"] = Math.Round(tx, 3);
                func.Params.ResultDoubles["MatchPosY"] = Math.Round(ty, 3);
                func.Params.ResultDoubles["MatchAngle"] = Math.Round(deltaAngle, 3);
                // 相对位移：相对于模板原位的偏移
                func.Params.ResultDoubles["ORBMatch_OffsetX"] = Math.Round(offsetX, 3);
                func.Params.ResultDoubles["ORBMatch_OffsetY"] = Math.Round(offsetY, 3);
                func.Params.ResultDoubles["ORBMatch_Angle"] = Math.Round(deltaAngle, 3);
                func.Params.ResultDoubles["ORBMatch_MatchCount"] = goodMatches.Length;
                func.Params.ResultDoubles["ORBMatch_InlierRatio"] = Math.Round(inlierRatio, 3);
                func.Params.ResultDoubles["ORBMatch_Score"] = Math.Round(inlierRatio * 100, 1);

                // ---------- 7. 写入局部变量表 ----------
                string posVarName = item.StringParams.TryGetValue("ORBMatch_PosVar", out var pn)
                    && !string.IsNullOrEmpty(pn) ? pn : "ORB匹配_Pos";
                string offsetVarName = item.StringParams.TryGetValue("ORBMatch_OffsetVar", out var ov)
                    && !string.IsNullOrEmpty(ov) ? ov : "ORB匹配_Offset";

                var posVar = func.Params.Variables.FirstOrDefault(v => v.Name == posVarName);
                if (posVar == null)
                {
                    posVar = new LocalVariableItem(posVarName, "Pos", new Pos());
                    System.Windows.Application.Current.Dispatcher.Invoke(() => func.Params.Variables.Add(posVar));
                }
                posVar.RawValue = new Pos(Math.Round(tx, 3), Math.Round(ty, 3), 0, Math.Round(deltaAngle, 3));

                var offsetVar = func.Params.Variables.FirstOrDefault(v => v.Name == offsetVarName);
                if (offsetVar == null)
                {
                    offsetVar = new LocalVariableItem(offsetVarName, "Pos", new Pos());
                    System.Windows.Application.Current.Dispatcher.Invoke(() => func.Params.Variables.Add(offsetVar));
                }
                offsetVar.RawValue = new Pos(Math.Round(offsetX, 3), Math.Round(offsetY, 3), 0, Math.Round(deltaAngle, 3));

                // ---------- 8. 可视化 ----------
                Mat colorSrc = null;
                if (func.Src.Channels() == 1)
                {
                    colorSrc = new Mat();
                    Cv2.CvtColor(func.Src, colorSrc, ColorConversionCodes.GRAY2BGR);
                    func.Src = colorSrc;
                }

                // 根据形状画匹配结果
                switch (roiShape)
                {
                    case "矩形":
                        // 画旋转矩形（模板在源图中的位置，带旋转）
                        var matchedRect = new RotatedRect(
                            new Point2f((float)tx, (float)ty),
                            new Size2f((float)templateW, (float)templateH),
                            (float)deltaAngle);
                        Cv2.Polylines(func.Src,
                            [[.. matchedRect.Points().Select(p => new Point((int)p.X, (int)p.Y))]],
                            true, Scalar.Magenta, 2);
                        break;

                    case "旋转矩形":
                        // 读取模板原始角度，加上匹配旋转得到最终角度
                        item.DoubleParams.TryGetValue("TemplateAngle", out double templateAngle);
                        double finalAngle = deltaAngle + templateAngle;
                        var rotatedRect = new RotatedRect(
                            new Point2f((float)tx, (float)ty),
                            new Size2f((float)templateW, (float)templateH),
                            (float)finalAngle);
                        Cv2.Polylines(func.Src,
                            [[.. rotatedRect.Points().Select(p => new Point((int)p.X, (int)p.Y))]],
                            true, Scalar.Magenta, 2);
                        break;

                    case "圆形":
                        // 画匹配到的圆形
                        Cv2.Circle(func.Src, (int)Math.Round(tx), (int)Math.Round(ty),
                            (int)(templateW / 2.0), Scalar.Magenta, 2);
                        Cv2.Circle(func.Src, (int)Math.Round(tx), (int)Math.Round(ty),
                            3, Scalar.Magenta, -1);
                        break;
                }

                // 画匹配到的模板中心
                Cv2.Circle(func.Src, (int)Math.Round(tx), (int)Math.Round(ty), 8, Scalar.Magenta, 2);
                Cv2.Circle(func.Src, (int)Math.Round(tx), (int)Math.Round(ty), 3, Scalar.Magenta, -1);

                // 显示匹配信息
                string info = $"pos: ({tx:F1}, {ty:F1}) angle={deltaAngle:F1}  |  offs ({offsetX:F1}, {offsetY:F1})";
                Cv2.PutText(func.Src, info, new Point(10, 100), HersheyFonts.HersheySimplex, 1.8, Scalar.Lime, 2);
                Mat reMat = new();
                Cv2.DrawMatches(template, templateKps, func.Src, srcKps, goodMatches, reMat);

                // reMat 接管 func.Src，旧的 colorSrc 需要释放
                func.Src = reMat;
                colorSrc?.Dispose();

                item.Flow.Done = true;
                return true;
            }
            finally
            {
                // 确保异常时也释放资源
                graySrc?.Dispose();
                grayTemplate?.Dispose();
                templateDescriptors?.Dispose();
                srcDescriptors?.Dispose();
                mask?.Dispose();
                homography?.Dispose();
                template?.Dispose();
                templateMask?.Dispose();
            }
        }
    }
}
