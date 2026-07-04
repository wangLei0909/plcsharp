using PLCSharp.Core.Prism;
using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.Connects.SerialPort
{
    /// <summary>
    /// Free串口协议配置
    /// </summary>
    [Dialog]
    public partial class FreeSerialProtocolConfig : UserControl
    {
        /// <summary>
        /// Free串口协议配置
        /// </summary>
        public FreeSerialProtocolConfig()
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