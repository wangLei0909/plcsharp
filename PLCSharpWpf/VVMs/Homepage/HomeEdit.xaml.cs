using PLCSharp.Core.Prism;
using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.Homepage
{
    /// <summary>
    /// 回零编辑
    /// </summary>
    [DialogMenu(ViewName = "HomeEdit",
       IconKind = "\ue622",
       DisplayName = "主页面  ", UserLevel = Authority.Authority.Engineer, Index = 0)]
    public partial class HomeEdit : UserControl
    {
        /// <summary>
        /// 回零编辑
        /// </summary>
        public HomeEdit()
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