using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.MotionController.Config
{
    /// <summary>
    /// InterpolationConfig.xaml 的交互逻辑
    /// </summary>
    public partial class InterpolationConfig : UserControl
    {
        /// <summary>
        /// 插补配置
        /// </summary>
        public InterpolationConfig()
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
