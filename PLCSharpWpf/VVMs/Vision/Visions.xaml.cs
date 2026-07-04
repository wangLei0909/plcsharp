using PLCSharp.Core.Prism;
using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.Vision
{
    /// <summary>
    /// Visions
    /// </summary>
    [DialogMenu(ViewName = "Visions",
       IconKind = "\ue8d1",
       DisplayName = "视觉功能", UserLevel = Authority.Authority.SeniorMaintainer, Index = 6)]
    /// <summary>
    /// Visions.xaml 的交互逻辑
    /// </summary>
    public partial class Visions : UserControl
    {
        /// <summary>
        /// Visions
        /// </summary>
        public Visions()
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
