namespace PLCSharp.VVMs.Vision.VisionFlowHandler.Algorithm
{
    /// <summary>
    /// 清理图像上的 DrawCommands 覆盖层
    /// </summary>
    public class ClearDrawHandler : IVisionFlowHandler
    {
        public VisionFlowType Type => VisionFlowType.清除绘制;

        public bool Execute(VisionFunction func, VisionFlow item)
        {
            _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
           {
               func.DrawCommands.Clear();
               func.EditImageEdit?.Remove("DrawOverlay");
               func.GlobalModel.GetImageControl(func.ControlName)?.Remove("DrawOverlay");

               item.Flow.Done = true;

           });
            return true;
        }
    }
}