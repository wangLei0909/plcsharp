namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Access
{
    /// <summary>
    /// 保存图像To全局Handler
    /// </summary>
    public class SaveImageToGlobalHandler : IVisionFlowHandler
    {
        /// <summary>
        /// 类型
        /// </summary>
        public VisionFlowType Type => VisionFlowType.存到全局图像;
        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="func">视觉功能</param>
        /// <param name="item">变量项</param>
        /// <returns>返回布尔值</returns>
        public bool Execute(VisionFunction func, VisionFlow item)
        {
            if (item.StringParams.TryGetValue("Image", out string imageName))
            {
                var imageData = func.ImageDatas.Where(i => i.Name == imageName).FirstOrDefault();
                if (imageData != null)
                {
                    imageData.Mat = func.Src.Clone();
                    item.Flow.Done = true;
                    return true;
                }
                else
                {
                    throw new Exception("图像池中未找到对应图片！");
                }
            }
            else
            {
                throw new Exception("未配置图像名称，请重新配置！");
            }
        }
    }
}
