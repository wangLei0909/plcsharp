using OpenCvSharp;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Processing
{
    /// <summary>
    /// Bgr2灰度Handler
    /// </summary>
    public class Bgr2GrayHandler : IVisionFlowHandler
    {
        /// <summary>
        /// 类型
        /// </summary>
        public VisionFlowType Type => VisionFlowType.BGR2GRAY;
        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="func">视觉功能</param>
        /// <param name="item">变量项</param>
        /// <returns>返回布尔值</returns>
        public bool Execute(VisionFunction func, VisionFlow item)
        {
            if (func.Src.Channels() == 3)
            {
                Cv2.CvtColor(func.Src, func.Src, ColorConversionCodes.BGR2GRAY);
                item.Flow.Done = true;
            }
            return true;
        }
    }
}
