using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.Robots.Epson
{
    /// <summary>
    /// RobotPointsConfig.xaml 的交互逻辑
    /// </summary>
    public partial class RobotPointsConfig : UserControl
    {
        /// <summary>
        /// 机器人Points配置
        /// </summary>
        public RobotPointsConfig()
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
