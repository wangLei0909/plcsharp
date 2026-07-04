namespace PLCSharp.VVMs.Workflows.Script
{
    /// <summary>
    /// 快捷代码片段项
    /// </summary>
    public class SnippetItem
    {
        /// <summary>
        /// 按钮显示名称
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 要插入的代码内容
        /// </summary>
        public string Content { get; set; } = "";
    }
}
