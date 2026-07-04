using OpenCvSharp;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Processing
{
    /// <summary>
    /// 拆分通道Handler
    /// </summary>
    public class SplitChannelHandler : IVisionFlowHandler
    {
        /// <summary>
        /// 类型
        /// </summary>
        public VisionFlowType Type => VisionFlowType.取通道;
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
                if (item.IntParams.TryGetValue("ChannelIndex", out int _channelIndex))
                {
                    Mat[] channels = func.Src.Split();
                    func.Src = channels[_channelIndex];

                }
            }
            return true;
        }
    }
}
