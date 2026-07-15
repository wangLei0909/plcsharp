using OpenCvSharp;
using System.Linq;
using System.Windows.Media;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Algorithm
{
    /// <summary>
    /// 颜色面积Handler — 计算ROI内指定HSV范围的颜色面积占比，超出上下限输出NG
    /// </summary>
    public class ColorAreaHandler : IVisionFlowHandler
    {
        public VisionFlowType Type => VisionFlowType.颜色面积;

        public bool Execute(VisionFunction func, VisionFlow item)
        {
            var src = func.Src ?? throw new Exception("请先获取图片！");

            // 读取ROI参数
            if (!item.DoubleParams.TryGetValue("ROILeft", out double roiLeft) ||
                !item.DoubleParams.TryGetValue("ROITop", out double roiTop) ||
                !item.DoubleParams.TryGetValue("ROIWidth", out double roiWidth) ||
                !item.DoubleParams.TryGetValue("ROIHeight", out double roiHeight))
                throw new Exception("ROI未配置，请先框选ROI区域！");

            // 读取HSV范围
            int hMin = item.IntParams.TryGetValue("HMin", out int hm) ? Math.Clamp(hm, 0, 180) : 0;
            int hMax = item.IntParams.TryGetValue("HMax", out int hmx) ? Math.Clamp(hmx, 0, 180) : 180;
            int sMin = item.IntParams.TryGetValue("SMin", out int sm) ? Math.Clamp(sm, 0, 255) : 0;
            int sMax = item.IntParams.TryGetValue("SMax", out int smx) ? Math.Clamp(smx, 0, 255) : 255;
            int vMin = item.IntParams.TryGetValue("VMin", out int vm) ? Math.Clamp(vm, 0, 255) : 0;
            int vMax = item.IntParams.TryGetValue("VMax", out int vmx) ? Math.Clamp(vmx, 0, 255) : 255;

            // 读取面积百分比上下限
            double areaMinPercent = item.DoubleParams.TryGetValue("AreaMinPercent", out double minP) ? minP : 0;
            double areaMaxPercent = item.DoubleParams.TryGetValue("AreaMaxPercent", out double maxP) ? maxP : 100;

            // 输出变量名
            string resultVar = item.StringParams.TryGetValue("ResultVarName", out var rv) && !string.IsNullOrEmpty(rv)
                ? rv : "颜色面积_Result";

            // 裁剪ROI
            int x = Math.Clamp((int)roiLeft, 0, src.Width - 1);
            int y = Math.Clamp((int)roiTop, 0, src.Height - 1);
            int w = Math.Min((int)roiWidth, src.Width - x);
            int h = Math.Min((int)roiHeight, src.Height - y);

            if (w <= 0 || h <= 0)
                throw new Exception("ROI超出图像范围！");

            using Mat roi = new Mat(src, new OpenCvSharp.Rect(x, y, w, h));

            // 转换到HSV
            using Mat hsv = new Mat();
            Cv2.CvtColor(roi, hsv, ColorConversionCodes.BGR2HSV);

            // 创建HSV范围掩码
            using Mat mask = new Mat();
            var lower = new Scalar(hMin, sMin, vMin);
            var upper = new Scalar(hMax, sMax, vMax);
            Cv2.InRange(hsv, lower, upper, mask);

            // 统计颜色像素数
            int colorPixelCount = Cv2.CountNonZero(mask);

            // ROI总像素数
            int roiPixelCount = w * h;

            // 计算面积百分比
            double areaPercent = roiPixelCount > 0
                ? (double)colorPixelCount / roiPixelCount * 100.0
                : 0;

            // 判断OK/NG
            bool isPass = areaPercent >= areaMinPercent && areaPercent <= areaMaxPercent;
            string result = isPass ? "OK" : "NG";

            // 在原始图像上绘制ROI边框
            func.DrawCommands.Add(DrawCommand.Line(x, y, x + w, y, Colors.Lime, 2));
            func.DrawCommands.Add(DrawCommand.Line(x + w, y, x + w, y + h, Colors.Lime, 2));
            func.DrawCommands.Add(DrawCommand.Line(x + w, y + h, x, y + h, Colors.Lime, 2));
            func.DrawCommands.Add(DrawCommand.Line(x, y + h, x, y, Colors.Lime, 2));

            // 绘制颜色区域的轮廓（在掩码上找轮廓并绘制到原图位置）
            using Mat maskForContour = mask.Clone();
            Cv2.FindContours(maskForContour, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            if (contours.Length > 0)
            {
                // 按面积排序，取最大的轮廓
                var sorted = contours.OrderByDescending(c => Cv2.ContourArea(c)).ToArray();
                var largest = sorted[0];

                // 将轮廓点偏移回原图坐标系
                var offsetPts = largest.Select(p => new System.Windows.Point(p.X + x, p.Y + y)).ToArray();
                func.DrawCommands.Add(DrawCommand.Polygon(offsetPts, Colors.Yellow, 2));

                // 如果有多个较大轮廓，也画出来（面积大于最大轮廓20%的）
                double maxArea = Cv2.ContourArea(largest);
                foreach (var contour in sorted.Skip(1))
                {
                    if (Cv2.ContourArea(contour) >= maxArea * 0.2)
                    {
                        var pts = contour.Select(p => new System.Windows.Point(p.X + x, p.Y + y)).ToArray();
                        func.DrawCommands.Add(DrawCommand.Polygon(pts, Colors.Yellow, 1));
                    }
                }
            }

            // 在图像上显示结果文本
            string text = $"颜色面积: {areaPercent:F1}%  [{result}]";
            func.DrawCommands.Add(DrawCommand.TextBlock(x, y - 20 < 0 ? y + h + 5 : y - 20, text, Colors.Cyan, 14));

            // 写入结果到ResultDoubles
            func.Params.ResultDoubles["ColorAreaPercent"] = Math.Round(areaPercent, 2);
            func.Params.ResultDoubles["ColorPixelCount"] = colorPixelCount;
            func.Params.ResultDoubles["ROIPixelCount"] = roiPixelCount;

            // 写入判定结果到StringParams（用于UI显示）
            item.StringParams["ColorAreaResult"] = result;

            // 写入结果变量
            var variable = func.Params.Variables.FirstOrDefault(v => v.Name == resultVar);
            if (variable == null)
            {
                variable = new LocalVariableItem(resultVar, "String", result);
                System.Windows.Application.Current.Dispatcher.Invoke(() => func.Params.Variables.Add(variable));
            }
            variable.RawValue = $"{result}|面积比:{areaPercent:F1}%|颜色像素:{colorPixelCount}|ROI像素:{roiPixelCount}";

            item.Flow.Done = true;
            _ = func.RenderDrawAsync();
            return true;
        }
    }
}
