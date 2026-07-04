using Prism.Mvvm;
using System.Windows.Media;

namespace PLCSharp.Core.Common
{
    /// <summary>
    /// 标题栏状态
    /// </summary>
    public class TitleState : BindableBase
    {
        private string _Info = "";
        /// <summary>
        /// Info
        /// </summary>
        public string Info
        {
            get { return _Info; }
            set { SetProperty(ref _Info, value); }
        }

        private Brush _Background = Brushes.White;
        /// <summary>
        /// Background
        /// </summary>
        public Brush Background
        {
            get { return _Background; }
            set { SetProperty(ref _Background, value); }
        }
    }
}
