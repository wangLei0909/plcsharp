using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.VVMs.Homepage.TableControl
{

    /// <summary>
    /// RangeTable
    /// </summary>
    public partial class RangeTable : UserControl
    {
        /// <summary>
        /// RangeTable
        /// </summary>
        public RangeTable()
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