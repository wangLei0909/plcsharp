using PLCSharp.Core.Prism;
using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.Workflows
{
    /// <summary>
    /// Workflows
    /// </summary>
    [DialogMenu(ViewName = "Workflows",
       IconKind = "\ueb71",
       DisplayName = "工艺流程", UserLevel = Authority.Authority.Engineer, Index = 3)]
    public partial class Workflows : UserControl
    {
        /// <summary>
        /// Workflows
        /// </summary>
        public Workflows()
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