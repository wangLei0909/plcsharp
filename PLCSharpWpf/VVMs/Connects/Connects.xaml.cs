using PLCSharp.Core.Prism;
using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.Connects
{
    /// <summary>
    /// 连接模型
    /// </summary>
    [DialogMenu(ViewName = "Connects",
       IconKind = "\ue62e",
       DisplayName = "通信配置", UserLevel = Authority.Authority.SeniorMaintainer, Index = 7)]
    public partial class Connects : UserControl
    {
        /// <summary>
        /// 连接模型
        /// </summary>
        public Connects()
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