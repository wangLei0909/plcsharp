using PLCSharp.Core.Prism;
using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.Robots
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    [DialogMenu(ViewName = "Robots", IconKind = "\ueb61", 
        DisplayName = "机械手  ",
        UserLevel = Authority.Authority.SeniorMaintainer, Index = 8)]
    [NavigationPage(ViewName = "Robots", IconKind = "\ueb61",
        DisplayName = "机械手  ",
        UserLevel = Authority.Authority.SeniorMaintainer, Index = 8)]
    public partial class Robots : UserControl
    {
        /// <summary>
        /// Robots
        /// </summary>
        public Robots()
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
