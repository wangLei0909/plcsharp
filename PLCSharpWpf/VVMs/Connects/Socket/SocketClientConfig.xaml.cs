using PLCSharp.Core.Prism;
using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.Connects.Socket
{
    /// <summary>
    /// Socket客户端配置
    /// </summary>
    [Dialog]
    public partial class SocketClientConfig : UserControl
    {
        /// <summary>
        /// Socket客户端配置
        /// </summary>
        public SocketClientConfig()
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