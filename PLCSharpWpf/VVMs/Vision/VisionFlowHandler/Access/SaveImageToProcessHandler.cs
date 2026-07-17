namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Access
{
    /// <summary>
    /// 存到局部图像（局部图像）
    /// </summary>
    public class SaveImageToProcessHandler : IVisionFlowHandler
    {
        /// <summary>
        /// 类型
        /// </summary>
        public VisionFlowType Type => VisionFlowType.存到局部图像;
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
                if (func.Params.ImageDatas.Where(w => w.Name == imageName).Any())
                {
                    func.Params.ImageDatas.Where(w => w.Name == imageName).FirstOrDefault().Mat = func.Src.Clone();
                    item.Flow.Done = true;
                    return true;
                }
            }
            throw new Exception("未配置局部图像名称！");
        }
    }
}
