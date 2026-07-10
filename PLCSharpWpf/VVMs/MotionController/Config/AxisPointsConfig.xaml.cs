using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.MotionController.Config
{
    /// <summary>
    /// AxisPointsConfig.xaml 的交互逻辑
    /// </summary>
    public partial class AxisPointsConfig : UserControl
    {
        /// <summary>
        /// 轴Points配置
        /// </summary>
        public AxisPointsConfig()
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
