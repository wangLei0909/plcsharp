namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Access
{
    /// <summary>
    /// 从局部图像（局部图像）获取图片
    /// </summary>
    public class GetImageFromProcessHandler : IVisionFlowHandler
    {
        /// <summary>
        /// 类型
        /// </summary>
        public VisionFlowType Type => VisionFlowType.从局部图像获取图片;
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
                var theMat = func.Params.Mats.Where(w => w.Name == imageName).FirstOrDefault();
                if (theMat != null)
                {
                    func.Src = theMat.Mat.Clone();
                    item.Flow.Done = true;
                    return true;
                }
                else
                {

                    throw new Exception("未找到局部图像！");
                }
            }
            throw new Exception("未配置局部图像名称！");
        }
    }
}
