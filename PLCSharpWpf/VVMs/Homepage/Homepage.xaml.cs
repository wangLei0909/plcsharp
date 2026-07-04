using DryIoc;
using PLCSharp.Core.Common;
using PLCSharp.Core.Prism;
using PLCSharp.Core.UserControls;
using PLCSharp.Models;
using PLCSharp.VVMs.Homepage.TableControl;
using Prism.Events;
using Prism.Ioc;
using System.Windows.Controls;
using System.Windows.Data;

namespace PLCSharp.VVMs.Homepage
{
    /// <summary>
    /// Homepage
    /// </summary>
    [NavigationPage(ViewName = "Homepage",
       IconKind = "\ue622",
       DisplayName = "主界面", UserLevel = Authority.Authority.Guset, Index = 0)]
    public partial class Homepage : UserControl
    {
        /// <summary>
        /// Homepage
        /// </summary>
        public Homepage(IEventAggregator ea, IContainerExtension container)
        {
            InitializeComponent();
            _container = container;
            //注册界面重载
            ea.GetEvent<MessageEvent>().Subscribe(
                UIReLoad,
                ThreadOption.UIThread,
                false,
                (filter) => filter.Target.Contains("UIReLoad"));
            ea.GetEvent<MessageEvent>().Publish(new()
            {
                Target = "RecipeChanged"
            });
        }
        IContainerExtension _container;
        /// <summary>
        /// 模型
        /// </summary>
        public GlobalModel Model { get; set; }
        private void UIReLoad(Message message)
        {
            Model = _container.Resolve<GlobalModel>();
            MainGrid.Children.Clear();
            MainGrid.RowDefinitions.Clear();
            MainGrid.ColumnDefinitions.Clear();
            for (int i = 0; i < Model.CurrentCanvasConfig.Rows; i++)
            {
                MainGrid.RowDefinitions.Add(new RowDefinition()); //添加行 
            }
            for (int i = 0; i < Model.CurrentCanvasConfig.Columns; i++)
            {
                MainGrid.ColumnDefinitions.Add(new ColumnDefinition()); //添加列
            }
            foreach (var item in Model.CustomControls)
            {
                switch (item.Type)
                {
                    case ControlType.Image:
                        {
                            var group = new GroupBox() { Header = item.Name };
                            var c = new ImageEdit();
                            group.Content = c;
                            MainGrid.Children.Add(group);
                            group.SetValue(Grid.RowProperty, item.Row);
                            if (item.RowSpan > 0)
                                group.SetValue(Grid.RowSpanProperty, item.RowSpan);
                            group.SetValue(Grid.ColumnProperty, item.Column);
                            if (item.ColumnSpan > 0)
                                group.SetValue(Grid.ColumnSpanProperty, item.ColumnSpan);
                            item.UIElement = c;
                            Binding binding = new("ImgSrc");
                            c.DataContext = item;
                            c.SetBinding(ImageEdit.ImageSourceProperty, binding);

                        }
                        break;
                    case ControlType.State:
                        {
                            var group = new GroupBox() { Header = item.Name };
                            var c = new Grid();
                            group.Content = c;
                            MainGrid.Children.Add(group);

                            item.Params.Rows = item.Params.Rows < 1 ? 1 : item.Params.Rows;
                            item.Params.Columns = item.Params.Columns < 1 ? 1 : item.Params.Columns;

                            for (int i = 0; i < item.Params.Rows; i++)
                            {
                                c.RowDefinitions.Add(new RowDefinition()); //添加行 
                            }
                            for (int i = 0; i < item.Params.Columns; i++)
                            {
                                c.ColumnDefinitions.Add(new ColumnDefinition()); //添加列
                            }

                            for (int i = 0; i < item.Params.Rows; i++)
                            {
                                for (int j = 0; j < item.Params.Columns; j++)
                                {
                                    var cell = new SimpleCell
                                    {
                                    };

                                    var index = i * item.Params.Columns + j;
                                    var cellInfo = item.Params.CellInfos.Where(c => c.Index == index).FirstOrDefault();
                                    if (cellInfo == null)
                                    {
                                        cellInfo = new CellInfo { State = CellState.无料, Index = index, Info = index.ToString() };
                                        item.Params.CellInfos.Add(cellInfo);
                                    }
                                    var viewModel = new SimpleCellViewModel();
                                    cell.DataContext = viewModel;

                                    viewModel.CellInfo = cellInfo;

                                    viewModel.CellInfo._CellChanged += viewModel.CellChanged;

                                    viewModel.CellInfo.State = viewModel.CellInfo.State;
                                    viewModel.CellInfo.Row = i;
                                    viewModel.CellInfo.Column = j;

                                    c.Children.Add(cell);
                                    switch (item.Params.Layout)
                                    {
                                        case 0:

                                            cell.SetValue(Grid.RowProperty, i);
                                            cell.SetValue(Grid.ColumnProperty, j);
                                            break;
                                        case 1:

                                            cell.SetValue(Grid.RowProperty, item.Params.Rows - i - 1);
                                            cell.SetValue(Grid.ColumnProperty, j);
                                            break;
                                        case 2:
                                            cell.SetValue(Grid.RowProperty, i);
                                            cell.SetValue(Grid.ColumnProperty, item.Params.Columns - j - 1);
                                            break;
                                        case 3:
                                            cell.SetValue(Grid.RowProperty, item.Params.Rows - i - 1);
                                            cell.SetValue(Grid.ColumnProperty, item.Params.Columns - j - 1);
                                            break;
                                        case 4:
                                            {
                                                var row = index % item.Params.Rows;
                                                var col = index / item.Params.Rows;
                                                cell.SetValue(Grid.RowProperty, row);
                                                cell.SetValue(Grid.ColumnProperty, col);
                                            }
                                            break;

                                        case 5:
                                            {
                                                var row = index % item.Params.Rows;
                                                var col = index / item.Params.Rows;
                                                cell.SetValue(Grid.RowProperty, row);

                                                cell.SetValue(Grid.ColumnProperty, item.Params.Columns - col - 1);
                                            }
                                            break;
                                        case 6:
                                            {
                                                {
                                                    var row = index % item.Params.Rows;
                                                    var col = index / item.Params.Rows;
                                                    cell.SetValue(Grid.RowProperty, item.Params.Rows - row - 1);

                                                    cell.SetValue(Grid.ColumnProperty, col);
                                                }
                                            }
                                            break;
                                        case 7:
                                            {
                                                var row = index % item.Params.Rows;
                                                var col = index / item.Params.Rows;
                                                cell.SetValue(Grid.RowProperty, item.Params.Rows - row - 1);

                                                cell.SetValue(Grid.ColumnProperty, item.Params.Columns - col - 1);
                                            }
                                            break;
                                        default:     // 左 -> 右  上 -> 下 

                                            cell.SetValue(Grid.RowProperty, i);
                                            cell.SetValue(Grid.ColumnProperty, j);
                                            break;
                                    }

                                }
                            }
                            group.SetValue(Grid.RowProperty, item.Row);
                            if (item.RowSpan > 0)
                                group.SetValue(Grid.RowSpanProperty, item.RowSpan);
                            group.SetValue(Grid.ColumnProperty, item.Column);
                            if (item.ColumnSpan > 0)
                                group.SetValue(Grid.ColumnSpanProperty, item.ColumnSpan);
                            item.UIElement = group;

                        }
                        break;
                    case ControlType.RangeValueTalbe:
                        {
                            var group = new GroupBox() { Header = item.Name };
                            var rangeTable = new RangeTable();
                            group.Content = rangeTable;
                            MainGrid.Children.Add(group);
                            group.SetValue(Grid.RowProperty, item.Row);
                            if (item.RowSpan > 0)
                                group.SetValue(Grid.RowSpanProperty, item.RowSpan);
                            group.SetValue(Grid.ColumnProperty, item.Column);
                            if (item.ColumnSpan > 0)
                                group.SetValue(Grid.ColumnSpanProperty, item.ColumnSpan);

                            var vm = new RangeTableViewModel();
                            vm.RangeValues = item.RangeValues;
                            rangeTable.DataContext = vm;
                            item.UIElement = rangeTable;
                        }
                        break;
                    default:
                        break;
                }

            }
        }



    }
}