using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.MotionController.Config
{
    /// <summary>
    /// SmcIO.xaml 的交互逻辑
    /// </summary>
    public partial class EmcIO : UserControl
    {
        /// <summary>
        /// EmcIO
        /// </summary>
        public EmcIO()
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
