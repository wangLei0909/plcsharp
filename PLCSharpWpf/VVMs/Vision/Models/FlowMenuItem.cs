using PLCSharp.VVMs.Vision;

namespace PLCSharp.VVMs.Vision.Models;

/// <summary>
/// 添加流程菜单中的一个叶子菜单项
/// </summary>
public class FlowMenuItem
{
    /// <summary>
    /// Header
    /// </summary>
    public string Header { get; set; }

    /// <summary>
    /// Flow类型
    /// </summary>
    public VisionFlowType FlowType { get; set; }
}
