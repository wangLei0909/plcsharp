namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Access
{
    /// <summary>
    /// 获取From全局图像Handler
    /// </summary>
    public class GetFromGlobalImageHandler : IVisionFlowHandler
    {
        /// <summary>
        /// 类型
        /// </summary>
        public VisionFlowType Type => VisionFlowType.从全局图像获取图片;
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

                    func.Src = imageData.Mat.Clone();
                    return true;
                }
                else
                {
                    throw new Exception("图像池中未找到对应图片！");
                }
            }
            return false;
        }
    }
}
