using PLCSharp.Core.Prism;
using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.Connects.ModbusTcp
{
    /// <summary>
    /// Modbus服务端配置
    /// </summary>
    [Dialog]
    public partial class ModbusServerConfig : UserControl
    {
        /// <summary>
        /// Modbus服务端配置
        /// </summary>
        public ModbusServerConfig()
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
