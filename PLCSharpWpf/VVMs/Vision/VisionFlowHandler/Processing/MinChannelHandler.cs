using OpenCvSharp;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Processing
{
    /// <summary>
    /// 最小通道Handler
    /// </summary>
    public class MinChannelHandler : IVisionFlowHandler
    {
        /// <summary>
        /// 类型
        /// </summary>
        public VisionFlowType Type => VisionFlowType.各通道最小值;
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
                Mat[] channels = func.Src.Split();
                Mat minMat = new Mat();
                Cv2.Min(channels[0], channels[1], minMat);
                Cv2.Min(minMat, channels[2], minMat);
                func.Src = minMat;
            }
            return true;
        }
    }
}
