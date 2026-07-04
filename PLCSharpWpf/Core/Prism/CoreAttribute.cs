namespace PLCSharp.Core.Prism
{
    /// <summary>
    /// 标志此特性的类会被注册为单例
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]

    public class ModelAttribute : Attribute
    {



    }


    /// <summary>
    ///  标志此特性的类会被注册为弹出窗口
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DialogAttribute : Attribute
    {



    }

    /// <summary>
    ///  标志此特性的类会被注册到弹出窗口列表
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DialogMenuAttribute : Attribute
    {

        /// <summary>
        /// 视图名称
        /// </summary>
        public string ViewName { get; set; }

        /// <summary>
        /// IconKind
        /// </summary>
        public string IconKind { get; set; }

        /// <summary>
        /// Display名称
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 用户Level
        /// </summary>
        public VVMs.Authority.Authority UserLevel { get; set; }

        /// <summary>
        /// Display
        /// </summary>
        public bool Display { get; set; }

        /// <summary>
        /// 当前索引
        /// </summary>
        public int Index { get; set; }

    }

    /// <summary>
    ///  标志此特性的类会被注册到导航列表
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class NavigationPageAttribute : Attribute
    {
        /// <summary>
        /// 视图名称
        /// </summary>
        public string ViewName { get; set; }

        /// <summary>
        /// IconKind
        /// </summary>
        public string IconKind { get; set; }

        /// <summary>
        /// Display名称
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 用户Level
        /// </summary>
        public VVMs.Authority.Authority UserLevel { get; set; }

        /// <summary>
        /// Display
        /// </summary>
        public bool Display { get; set; }

        /// <summary>
        /// 当前索引
        /// </summary>
        public int Index { get; set; }

    }
}