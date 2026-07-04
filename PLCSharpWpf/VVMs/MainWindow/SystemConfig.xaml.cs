using PLCSharp.Core.Prism;
using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.MainWindow
{
    /// <summary>
    /// System配置
    /// </summary>
    [Dialog]
    /// <summary>
    /// SystemConfig.xaml 的交互逻辑
    /// </summary>
    public partial class SystemConfig : UserControl
    {
        /// <summary>
        /// System配置
        /// </summary>
        public SystemConfig()
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
