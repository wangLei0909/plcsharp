using PLCSharp.Core.Prism;
using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.MotionController
{
    /// <summary>
    /// Controllers
    /// </summary>
    [DialogMenu(ViewName = "Controllers",
       IconKind = "\ue687",
       DisplayName = "运动控制", UserLevel = Authority.Authority.SeniorMaintainer, Index = 7)]
    /// <summary>
    /// ControllerConfig.xaml 的交互逻辑
    /// </summary>
    public partial class Controllers : UserControl

    {
        /// <summary>
        /// Controllers
        /// </summary>
        public Controllers()
        {
            InitializeComponent();
        }
        private void DataGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
        }
    }
}
