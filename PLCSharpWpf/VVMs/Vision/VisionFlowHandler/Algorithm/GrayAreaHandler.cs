using OpenCvSharp;
using System.Windows.Media;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Algorithm
{
    /// <summary>
    /// 灰度面积Handler — 计算ROI内指定灰度范围的面积占比，超出上下限输出NG
    /// </summary>
    public class GrayAreaHandler : IVisionFlowHandler
    {
        public VisionFlowType Type => VisionFlowType.灰度面积;

        public bool Execute(VisionFunction func, VisionFlow item)
        {
            var src = func.Src ?? throw new Exception("请先获取图片！");

            // 读取ROI参数
            if (!item.DoubleParams.TryGetValue("ROILeft", out double roiLeft) ||
                !item.DoubleParams.TryGetValue("ROITop", out double roiTop) ||
                !item.DoubleParams.TryGetValue("ROIWidth", out double roiWidth) ||
                !item.DoubleParams.TryGetValue("ROIHeight", out double roiHeight))
                throw new Exception("ROI未配置，请先框选ROI区域！");

            // 读取灰度范围
            int grayMin = item.IntParams.TryGetValue("GrayMin", out int gm) ? Math.Clamp(gm, 0, 255) : 0;
            int grayMax = item.IntParams.TryGetValue("GrayMax", out int gmx) ? Math.Clamp(gmx, 0, 255) : 255;

            // 读取面积百分比上下限
            double areaMinPercent = item.DoubleParams.TryGetValue("AreaMinPercent", out double minP) ? minP : 0;
            double areaMaxPercent = item.DoubleParams.TryGetValue("AreaMaxPercent", out double maxP) ? maxP : 100;

            // 裁剪ROI
            int x = Math.Clamp((int)roiLeft, 0, src.Width - 1);
            int y = Math.Clamp((int)roiTop, 0, src.Height - 1);
            int w = Math.Min((int)roiWidth, src.Width - x);
            int h = Math.Min((int)roiHeight, src.Height - y);

            if (w <= 0 || h <= 0)
                throw new Exception("ROI超出图像范围！");

            using Mat roi = new Mat(src, new OpenCvSharp.Rect(x, y, w, h));

            // 转灰度
            using Mat gray = new Mat();
            if (roi.Channels() == 3)
                Cv2.CvtColor(roi, gray, ColorConversionCodes.BGR2GRAY);
            else
                roi.CopyTo(gray);

            // 创建灰度范围掩码
            using Mat mask = new Mat();
            Cv2.InRange(gray, new Scalar(grayMin), new Scalar(grayMax), mask);

            // 统计目标像素数
            int targetPixelCount = Cv2.CountNonZero(mask);

            // ROI总像素数
            int roiPixelCount = w * h;

            // 计算面积百分比
            double areaPercent = roiPixelCount > 0
                ? (double)targetPixelCount / roiPixelCount * 100.0
                : 0;
            func.ResultDouble = areaPercent;
            // 判断OK/NG
            bool isPass = areaPercent >= areaMinPercent && areaPercent <= areaMaxPercent;
            string result = isPass ? "OK" : "NG";
            func.ResultString = result;
            // 在原始图像上绘制ROI边框
            func.DrawCommands.Add(DrawCommand.Line(x, y, x + w, y, Colors.Lime, 2));
            func.DrawCommands.Add(DrawCommand.Line(x + w, y, x + w, y + h, Colors.Lime, 2));
            func.DrawCommands.Add(DrawCommand.Line(x + w, y + h, x, y + h, Colors.Lime, 2));
            func.DrawCommands.Add(DrawCommand.Line(x, y + h, x, y, Colors.Lime, 2));

            // 绘制目标区域的轮廓
            using Mat maskForContour = mask.Clone();
            Cv2.FindContours(maskForContour, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            if (contours.Length > 0)
            {
                var sorted = contours.OrderByDescending(c => Cv2.ContourArea(c)).ToArray();
                var largest = sorted[0];

                var offsetPts = largest.Select(p => new System.Windows.Point(p.X + x, p.Y + y)).ToArray();
                func.DrawCommands.Add(DrawCommand.Polygon(offsetPts, Colors.Yellow, 2));

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
            string text = $"灰度面积: {areaPercent:F1}%  [{result}]";
            func.DrawCommands.Add(DrawCommand.TextBlock(x, y - 20 < 0 ? y + h + 5 : y - 20, text, Colors.Cyan, 14));

            // 写入结果到ResultDoubles
            func.Params.ResultDoubles["GrayAreaPercent"] = Math.Round(areaPercent, 2);
            func.Params.ResultDoubles["GrayPixelCount"] = targetPixelCount;
            func.Params.ResultDoubles["ROIPixelCount"] = roiPixelCount;

            // 写入判定结果到StringParams（用于UI绑定显示）
            item.StringParams["GrayAreaResult"] = result;

            item.Flow.Done = true;
            _ = func.RenderDrawAsync();
            return true;
        }
    }
}
