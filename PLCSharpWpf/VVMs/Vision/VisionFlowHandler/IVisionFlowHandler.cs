namespace PLCSharp.VVMs.Vision.VisionFlowHandler
{
    // 定义处理策略接口
    /// <summary>
    /// IVisionFlowHandler
    /// </summary>
    public interface IVisionFlowHandler
    {
        VisionFlowType Type { get; }
        bool Execute(VisionFunction func, VisionFlow item);
    }
}
