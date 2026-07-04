using OpenCvSharp;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Processing
{
    /// <summary>
    /// Gray2BGRHandler
    /// </summary>
    public class Gray2BgrHandler : IVisionFlowHandler
    {
        /// <summary>
        /// 类型
        /// </summary>
        public VisionFlowType Type => VisionFlowType.GRAY2BGR;
        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="func">视觉功能</param>
        /// <param name="item">变量项</param>
        /// <returns>返回布尔值</returns>
        public bool Execute(VisionFunction func, VisionFlow item)
        {
            if (func.Src.Channels() == 1)
            {
                Cv2.CvtColor(func.Src, func.Src, ColorConversionCodes.GRAY2BGR);

            }
            return true;
        }
    }
}
