using PLCSharp.Core.Prism;
using System.Windows;
using System.Windows.Controls;
namespace PLCSharp.VVMs.GlobalVariables
{
    /// <summary>
    /// 全局Variables
    /// </summary>
    [DialogMenu(ViewName = "GlobalVariables",
       IconKind = "\ue604",
       DisplayName = "全局变量", UserLevel = Authority.Authority.Engineer, Index = 6)]
    public partial class GlobalVariables : UserControl
    {
        /// <summary>
        /// 全局Variables
        /// </summary>
        public GlobalVariables()
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