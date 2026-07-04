using PLCSharp.Core.Prism;
using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.Connects.ModbusRtu
{
    /// <summary>
    /// ModbusRtu配置
    /// </summary>
    [Dialog]
    public partial class ModbusRtuConfig : UserControl
    {
        /// <summary>
        /// ModbusRtu配置
        /// </summary>
        public ModbusRtuConfig()
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
