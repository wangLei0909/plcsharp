using PLCSharp.Core.Prism;
using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.Connects.ModbusTcp
{
    /// <summary>
    /// Modbus客户端配置
    /// </summary>
    [Dialog]
    public partial class ModbusClientConfig : UserControl
    {
        /// <summary>
        /// Modbus客户端配置
        /// </summary>
        public ModbusClientConfig()
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