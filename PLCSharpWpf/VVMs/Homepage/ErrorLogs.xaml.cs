using PLCSharp.Core.Prism;
using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.Homepage
{
    /// <summary>
    /// 错误Logs
    /// </summary>
    [NavigationPage(ViewName = "ErrorLogs",
       IconKind = "\ue678",
       DisplayName = "日志", UserLevel = Authority.Authority.Guset, Index = 3)]
    [Dialog]
    /// <summary>
    /// ErrorLog.xaml 的交互逻辑
    /// </summary>
    public partial class ErrorLogs : UserControl
    {
        /// <summary>
        /// 错误Logs
        /// </summary>
        public ErrorLogs()
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
