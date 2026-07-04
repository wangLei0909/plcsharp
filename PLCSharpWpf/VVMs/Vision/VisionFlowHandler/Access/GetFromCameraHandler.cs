namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Access
{
    /// <summary>
    /// 获取From相机Handler
    /// </summary>
    public class GetFromCameraHandler : IVisionFlowHandler
    {
        /// <summary>
        /// 类型
        /// </summary>
        public VisionFlowType Type => VisionFlowType.拍照;
        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="func">视觉功能</param>
        /// <param name="item">变量项</param>
        /// <returns>返回布尔值</returns>
        public bool Execute(VisionFunction func, VisionFlow item)
        {
            switch (item.Flow.Step)
            {
                case 0:
                    if (item.StringParams.TryGetValue("Camera", out string cameraName))
                    {
                        item.Camera = func.VisionsModel.Cameras.Where(c => c.Name == cameraName).FirstOrDefault();
                        if (item.Camera != null)
                        {
                            item.Camera.Params.ExposureTime = item.DoubleParams["ExposureTime"];
                            item.Camera.Trig();
                            item.Flow.Step++;

                        }
                        else
                        {

                            throw new Exception("未找到流程配置的相机！");
                        }
                    }
                    else
                    {

                        throw new Exception("本流程未正确配置相机！");
                    }
                    break;
                case 1:

                    if (item.Camera.WaitOne(3000))
                    {
                        func.Src = item.Camera.Mat;
                        item.Flow.Done = true;
                        item.Flow.Step++;
                        return true;
                    }
                    break;

            }
            return false;
        }
    }
}
