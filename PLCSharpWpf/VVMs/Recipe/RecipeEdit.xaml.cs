using PLCSharp.Core.Prism;
using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.Recipe
{
    /// <summary>
    /// Recipe编辑
    /// </summary>
    [NavigationPage(ViewName = "RecipeEdit",
       IconKind = "\ue681",
       DisplayName = "机种配方", UserLevel = Authority.Authority.SeniorMaintainer, Index = 4)]
    public partial class RecipeEdit : UserControl
    {
        /// <summary>
        /// Recipe编辑
        /// </summary>
        public RecipeEdit()
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