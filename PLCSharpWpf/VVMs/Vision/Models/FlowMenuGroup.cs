namespace PLCSharp.VVMs.Vision.Models;

/// <summary>
/// 添加流程菜单中的一个分组（一个子菜单）
/// </summary>
public class FlowMenuGroup
{
    /// <summary>
    /// Header
    /// </summary>
    public string Header { get; set; }

    /// <summary>
    /// Items
    /// </summary>
    public List<FlowMenuItem> Items { get; set; }
}
