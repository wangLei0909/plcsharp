using Prism.Mvvm;

namespace PLCSharp.VVMs.MainWindow
{
    /// <summary>
    /// 环境变量条目（名称-值对）
    /// </summary>
    public class EnvironmentVariableItem : BindableBase
    {
        private string _Name;
        /// <summary>
        /// 变量名
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }

        private string _Value;
        /// <summary>
        /// 变量值（路径等）
        /// </summary>
        public string Value
        {
            get { return _Value; }
            set { SetProperty(ref _Value, value); }
        }
    }
}
