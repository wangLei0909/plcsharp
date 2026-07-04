using OpenCvSharp;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Processing
{
    /// <summary>
    /// 最大通道Handler
    /// </summary>
    public class MaxChannelHandler : IVisionFlowHandler
    {
        /// <summary>
        /// 类型
        /// </summary>
        public VisionFlowType Type => VisionFlowType.各通道最大值;
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
                Mat[] channels = Cv2.Split(func.Src);
                Mat maxMat = new Mat();
                Cv2.Max(channels[0], channels[1], maxMat);
                Cv2.Max(maxMat, channels[2], maxMat);
                func.Src = maxMat;
            }
            return true;
        }
    }
}
