using OpenCvSharp;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Processing
{
    /// <summary>
    /// ROI剪切Handler — 将ROI区域从图像中裁剪出来作为新的源图像
    /// </summary>
    public class ROICropHandler : IVisionFlowHandler
    {
        public VisionFlowType Type => VisionFlowType.ROI剪切;

        public bool Execute(VisionFunction func, VisionFlow item)
        {
            var src = func.Src ?? throw new Exception("请先获取图片！");

            // 读取ROI参数
            if (!item.DoubleParams.TryGetValue("ROILeft", out double roiLeft) ||
                !item.DoubleParams.TryGetValue("ROITop", out double roiTop) ||
                !item.DoubleParams.TryGetValue("ROIWidth", out double roiWidth) ||
                !item.DoubleParams.TryGetValue("ROIHeight", out double roiHeight))
                throw new Exception("ROI未配置，请先框选ROI区域！");

            int x = Math.Clamp((int)roiLeft, 0, src.Width - 1);
            int y = Math.Clamp((int)roiTop, 0, src.Height - 1);
            int w = Math.Min((int)roiWidth, src.Width - x);
            int h = Math.Min((int)roiHeight, src.Height - y);

            if (w <= 0 || h <= 0)
                throw new Exception("ROI超出图像范围！");

            using Mat cropped = new Mat(src, new OpenCvSharp.Rect(x, y, w, h));
            Mat newSrc = cropped.Clone();
            func.Src = newSrc;

            return true;
        }
    }
}
