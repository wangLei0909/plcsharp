using PLCSharp.Core.Prism;
using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.Connects.Socket
{
    /// <summary>
    /// Socket服务端配置
    /// </summary>
    [Dialog]
    public partial class SocketServerConfig : UserControl
    {
        /// <summary>
        /// Socket服务端配置
        /// </summary>
        public SocketServerConfig()
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
