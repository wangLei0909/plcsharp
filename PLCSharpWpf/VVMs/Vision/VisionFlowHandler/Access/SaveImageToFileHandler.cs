using OpenCvSharp;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Access
{
    /// <summary>
    /// 保存图像To文件Handler
    /// </summary>
    public class SaveImageToFileHandler : IVisionFlowHandler
    {
        /// <summary>
        /// 类型
        /// </summary>
        public VisionFlowType Type => VisionFlowType.存到文件;
        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="func">视觉功能</param>
        /// <param name="item">变量项</param>
        /// <returns>返回布尔值</returns>
        public bool Execute(VisionFunction func, VisionFlow item)
        {
            if (item.StringParams.TryGetValue("Path", out string filePath))
            {
                try
                {
                    Cv2.ImWrite(filePath, func.Src);
                    item.Flow.Done = true;
                    return true;
                }
                catch (Exception ex)
                {
                    throw new Exception($"保存图像失败: {ex.Message}");
                }
            }
            else
            {
                throw new Exception("未配置文件路径，请重新配置！");
            }
        }
    }
}
