using PLCSharp.Core.Prism;
using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.Authority
{
    /// <summary>
    /// 用户管理
    /// </summary>
    [Dialog]
    public partial class UserManage : UserControl
    {
        /// <summary>
        /// 用户管理
        /// </summary>
        public UserManage()
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