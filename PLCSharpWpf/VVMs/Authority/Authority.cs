namespace PLCSharp.VVMs.Authority
{
    [Flags]
    /// <summary>
    /// 权限
    /// </summary
    public enum Authority
    {

        /// <summary>
        /// 仅查看
        /// </summary>
        Guset = 1,

        /// <summary>
        /// 操作人员
        /// </summary>
        Operator = 3,

        /// <summary>
        /// 维护人员
        /// </summary>
        Maintainer = 7,

        /// <summary>
        /// 高级维护人员
        /// </summary>
        SeniorMaintainer = 31,
        /// <summary>
        /// 管理人员
        /// </summary>
        Administrator = 63,

        /// <summary>
        /// 开发人员
        /// </summary>
        Engineer = 127,

        /// <summary>
        /// 超级权限
        /// </summary>
        Super = 255
    }
}