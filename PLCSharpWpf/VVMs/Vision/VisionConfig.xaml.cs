using PLCSharp.Core.Prism;
using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.Vision
{
    /// <summary>
    /// Vision配置
    /// </summary>
    [Dialog]
    /// <summary>
    /// OpenCVVisionConfig.xaml 的交互逻辑
    /// </summary>
    public partial class VisionConfig : UserControl
    {

        /// <summary>
        /// Vision配置
        /// </summary>
        public VisionConfig()
        {
            InitializeComponent();
        }

        private void DataGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
        }

        private void OnAddFlowItemClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is MenuItem menu)
            {
                var type = (FlowMenuItem)menu.Header;
                (DataContext as VisionConfigViewModel)?.AddFlow.Execute(type.FlowType.ToString());
                e.Handled = true;
            }
        }


    }
}
