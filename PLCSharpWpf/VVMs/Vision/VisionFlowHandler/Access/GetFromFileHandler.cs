using OpenCvSharp;
using System.IO;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Access
{
    /// <summary>
    /// 获取From文件Handler
    /// </summary>
    public class GetFromFileHandler : IVisionFlowHandler
    {
        /// <summary>
        /// 类型
        /// </summary>
        public VisionFlowType Type => VisionFlowType.从文件获取图片;
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
                if (File.Exists(filePath))
                {
                    func.Src = Cv2.ImRead(filePath);
                    return true;
                }
                else
                {
                    throw new Exception($"文件路径不存在: {filePath}");
                }
            }
            return false;

        }
    }
}
