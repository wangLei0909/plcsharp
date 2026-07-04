using PLCSharp.Core.Common;
using Prism.Commands;
using Prism.Mvvm;
using System.Windows.Media;
namespace PLCSharp.Core.UserControls
{
    /// <summary>
    /// SimpleCell视图模型
    /// </summary>
    public class SimpleCellViewModel : BindableBase
    {

        private DelegateCommand<object> _MenuItemCommand;
        /// <summary>
        /// 菜单项Command
        /// </summary>
        public DelegateCommand<object> MenuItemCommand =>
            _MenuItemCommand ??= new DelegateCommand<object>(ExecuteMenuItemCommand);

        void ExecuteMenuItemCommand(object state)
        {
            CellInfo.State = (CellState)state;

        }

        /// <summary>
        /// CellChanged
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        internal void CellChanged(object sender, CellState e)
        {
            Background = CellInfo.State switch
            {
                CellState.无料 => Brushes.Gray,
                CellState.有料 => Brushes.CornflowerBlue,
                CellState.请求 => Brushes.LightYellow,
                CellState.完成 => Brushes.SpringGreen,
                CellState.允许 => Brushes.White,
                CellState.确认 => Brushes.Gold,
                CellState.OK => Brushes.Lime,
                CellState.NG => Brushes.Red,
                _ => Brushes.White,
            };
        }

        private Brush _Background;
        /// <summary>
        /// 背景画刷
        /// </summary>
        public Brush Background
        {
            get { return _Background; }
            set { SetProperty(ref _Background, value); }
        }
        private CellInfo _CellInfo;
        /// <summary>
        /// 单元格信息
        /// </summary>
        public CellInfo CellInfo
        {
            get { return _CellInfo; }
            set { SetProperty(ref _CellInfo, value); }
        }

  
    }
}
