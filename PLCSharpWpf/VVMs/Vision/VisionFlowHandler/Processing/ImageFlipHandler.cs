using OpenCvSharp;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Processing
{
    /// <summary>
    /// 图像翻转Handler — 对图像进行水平或垂直翻转
    /// </summary>
    public class ImageFlipHandler : IVisionFlowHandler
    {
        public VisionFlowType Type => VisionFlowType.图像翻转;

        public bool Execute(VisionFunction func, VisionFlow item)
        {
            var src = func.Src ?? throw new Exception("请先获取图片！");

            // 读取翻转方向：0=水平(左右)，1=垂直(上下)
            int direction = item.IntParams.TryGetValue("FlipDirection", out int dir) ? dir : 0;

            FlipMode flipMode = direction switch
            {
                0 => FlipMode.Y,   // 水平翻转（左右）
                1 => FlipMode.X,   // 垂直翻转（上下）
                _ => FlipMode.Y,
            };

            Cv2.Flip(src, src, flipMode);

            return true;
        }
    }
}
