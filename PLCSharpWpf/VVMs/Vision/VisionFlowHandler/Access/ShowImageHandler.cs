namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Access
{
    /// <summary>
    /// 显示图像Handler
    /// </summary>
    public class ShowImageHandler : IVisionFlowHandler
    {
        /// <summary>
        /// 类型
        /// </summary>
        public VisionFlowType Type => VisionFlowType.显示图像到主页;
        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="func">视觉功能</param>
        /// <param name="item">变量项</param>
        /// <returns>返回布尔值</returns>
        public bool Execute(VisionFunction func, VisionFlow item)
        {
            if (item.StringParams.TryGetValue("CustomControlName", out string customControlName))
            {
                var customControl = func.GlobalModel.GetCustomControl(customControlName);
                if (customControl != null)
                {
                    customControl.ShowMat(func.Src);
                    item.Flow.Done = true;
                    return true;
                }



            }
            else
            {

                throw new Exception("未配置，请重新配置！");
            }
            return false;
        }
    }
}
