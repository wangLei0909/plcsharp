using Newtonsoft.Json;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using PLCSharp.Core.Common;
using PLCSharp.Core.Tools;
using PLCSharp.Core.UserControls;
using PLCSharp.VVMs.Homepage.TableControl;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Brush = System.Windows.Media.Brush;
using FontFamily = System.Windows.Media.FontFamily;


namespace PLCSharp.VVMs.Homepage
{
    /// <summary>
    /// Custom控件
    /// </summary>
    public class CustomControl : BindableBase
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        [Key]
        public Guid ID { get; set; } = Guid.NewGuid();
        /// <summary>
        /// 配方标识
        /// </summary>
        public Guid RecipeID { get; set; }
        private string _Name;
        /// <summary>
        /// 自定义控件属性
        /// </summary>
        public string Name
        {
            get { return _Name; }
            set
            {
                if (_Name != value)
                {
                    Prompt = "已修改，请保存";
                }
                SetProperty(ref _Name, value);
            }
        }

        private ControlType _Type;
        /// <summary>
        /// 自定义控件属性
        /// </summary>
        public ControlType Type
        {
            get { return _Type; }
            set { SetProperty(ref _Type, value); }
        }

        private string _Prompt;
        /// <summary>
        /// 提示
        /// </summary>
        [NotMapped]
        public string Prompt
        {
            get { return _Prompt; }
            set { SetProperty(ref _Prompt, value); }
        }
        private int _Row;
        /// <summary>
        /// 自定义控件属性
        /// </summary>
        public int Row
        {
            get { return _Row; }
            set { SetProperty(ref _Row, value); }
        }

        private int _Column;
        /// <summary>
        /// 自定义控件属性
        /// </summary>
        public int Column
        {
            get { return _Column; }
            set { SetProperty(ref _Column, value); }
        }

        private int _RowSpan;
        /// <summary>
        /// 自定义控件属性
        /// </summary>
        public int RowSpan
        {
            get { return _RowSpan; }
            set { SetProperty(ref _RowSpan, value); }
        }

        private int _ColumnSpan;
        /// <summary>
        /// 自定义控件属性
        /// </summary>
        public int ColumnSpan
        {
            get { return _ColumnSpan; }
            set { SetProperty(ref _ColumnSpan, value); }
        }
        private string _Comment;
        /// <summary>
        /// 备注
        /// </summary>
        public string Comment

        {
            get { return _Comment; }
            set
            {
                if (_Comment != value)
                {
                    Prompt = "已修改，请保存";
                }
                SetProperty(ref _Comment, value);
            }
        }
        #region Params
        /// <summary>
        /// 序列化Params
        /// </summary>
        [Column("Params")]


        public string SerializedParams
        {
            get => JsonConvert.SerializeObject(Params); // 自动序列化
            set => Params = JsonConvert.DeserializeObject<ControlParams>(value); // 自动反序列化

        }
        private ControlParams _Params;
        /// <summary>
        /// 参数集合
        /// </summary>
        [JsonIgnore]
        [NotMapped]
        public ControlParams Params
        {
            get
            {
                _Params ??= new ControlParams();

                return _Params;
            }
            set
            {
                SetProperty(ref _Params, value);

            }
        }
        #endregion
        private ObservableCollection<RangeValue> _RangeValues = [];
        /// <summary>
        /// RangeValues
        /// </summary>
        [NotMapped]
        public ObservableCollection<RangeValue> RangeValues
        {
            get { return _RangeValues; }
            set { if (value != null) SetProperty(ref _RangeValues, value); }
        }

        /// <summary>
        /// 序列化RangeValues
        /// </summary>
        [Column("RangeValues")]


        public string SerializedRangeValues
        {
            get => JsonConvert.SerializeObject(RangeValues); // 自动序列化
            set => RangeValues = JsonConvert.DeserializeObject<ObservableCollection<RangeValue>>(value); // 自动反序列化

        }
        /// <summary>
        /// 设置CellState
        /// </summary>
        /// <param name="index">当前索引</param>
        /// <param name="cellState">cellState</param>
        public void SetCellState(int index, CellState cellState)
        {
            var cell = Params.CellInfos.Where(c => c.Index == index).FirstOrDefault();
            if (cell != null)
            {
                cell.State = cellState;
            }
            else
            {
                throw new Exception($"未找到索引{index}的单元格");
            }

        }
        private WriteableBitmap _ImgSrc;
        /// <summary>
        /// 显示的图像源
        /// </summary>
        [NotMapped]
        public WriteableBitmap ImgSrc
        {
            get { return _ImgSrc; }
            set { SetProperty(ref _ImgSrc, value); }
        }
        private void Dispatch(Action action)
        {
            Application.Current.Dispatcher.Invoke(action);
        }

        /// <summary>
        /// 显示的图像矩阵
        /// </summary>
        /// <param name="_ShowMat">_显示矩阵</param>
        public void ShowMat(Mat _ShowMat)
        {
            if (_ShowMat == null) return;
            Dispatch(() =>
            {
                if (_ImgSrc != null
                && _ShowMat.Width == _ImgSrc.PixelWidth
                && _ShowMat.Height == _ImgSrc.PixelHeight
                && _ShowMat.Channels() == _ImgSrc.Format.BitsPerPixel / 8
                    )
                {
                    WriteableBitmapConverter.ToWriteableBitmap(_ShowMat, _ImgSrc);

                }
                else
                {
                    ImgSrc = WriteableBitmapConverter.ToWriteableBitmap(_ShowMat);
                }
            });

        }
        /// <summary>
        /// 显示圆
        /// </summary>
        /// <param name="centerRow">中心点X</param>
        /// <param name="centerY">中心点Y</param>
        /// <param name="radiusRow">半径X</param>
        /// <param name="radiusColumn">半径Y</param>
        /// <param name="color">颜色</param>
        /// <param name="thickness">线径</param>
        public void ShowEllipse(double centerRow, double centerColumn, double radiusRow, double radiusColumn, Brush color, double thickness = 3)
        {
            Dispatch(() =>
           {
               Path path = new()
               {
                   StrokeThickness = thickness,
                   Stroke = color
               };
               var g = new EllipseGeometry
               {
                   Center = new() { X = centerRow, Y = centerColumn },
                   RadiusX = radiusRow,
                   RadiusY = radiusColumn
               };
               path.Data = g;
               var ui = UIElement as ImageEdit;
               if (ui == null) return;
               ui.Draw(path);
           });

        }
        /// <summary>
        /// 显示线段
        /// </summary>
        /// <param name="fromRow"></param>
        /// <param name="fromColumn"></param>
        /// <param name="toRow"></param>
        /// <param name="toColumn"></param>
        /// <param name="thickness"></param>
        /// <param name="color"></param>
        /// <param name="thickness"></param>
        public void ShowLine(double fromRow, double fromColumn, double toRow, double toColumn, Brush color, double thickness = 3)
        {
            Dispatch(() =>
            {
                Path path = new()
                {
                    StrokeThickness = thickness,
                    Stroke = color//设置Path基础属性
                };
                var g = new LineGeometry
                {
                    StartPoint = new() { X = fromRow, Y = fromColumn },
                    EndPoint = new() { X = toRow, Y = toColumn },

                };
                path.Data = g;
                if (UIElement is not ImageEdit ui) return;
                ui.Draw(path);
            });

        }
        private static XPoint[] GetArrowPoints(XPoint end, XPoint start, float size)
        {
            XPoint[] points = new XPoint[2];

            // 计算箭头的角度
            double angle = Math.Atan2(end.Y - start.Y, end.X - start.X);

            // 计算箭头顶点的位置


            // 计算箭头左侧端点位置
            points[0] = new XPoint
            {
                X = (float)(end.X - size * Math.Cos(angle + Math.PI / 8)),
                Y = (float)(end.Y - size * Math.Sin(angle + Math.PI / 8))
            };

            // 计算箭头右侧端点位置
            points[1] = new XPoint
            {
                X = (float)(end.X - size * Math.Cos(angle - Math.PI / 8)),
                Y = (float)(end.Y - size * Math.Sin(angle - Math.PI / 8))
            };

            return points;
        }
        /// <summary>
        /// 显示Arrow直线
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="fromRow">fromRow</param>
        /// <param name="fromColumn">fromColumn</param>
        /// <param name="toRow">toRow</param>
        /// <param name="toColumn">toColumn</param>
        /// <param name="color">color</param>
        /// <param name="thickness">thickness</param>
        public void ShowArrowLine(string name, double fromRow, double fromColumn, double toRow, double toColumn, Brush color, double thickness = 3)
        {
            Dispatch(() =>
            {
                Path path = new()
                {
                    StrokeThickness = thickness,
                    Stroke = color,
                    Tag = name
                };
                var g = new LineGeometry
                {
                    StartPoint = new() { X = fromColumn, Y = fromRow },
                    EndPoint = new() { X = toColumn, Y = toRow },

                };
                path.Data = g;
                var ui = UIElement as ImageEdit;
                if (ui == null) return;
                ui.Draw(path);

                var headlen = 30;//箭头线的长度

                var startPoint = new XPoint() { X = fromColumn, Y = fromRow };
                var endPoint = new XPoint() { X = toColumn, Y = toRow };

                XPoint[] points = GetArrowPoints(endPoint, startPoint, headlen);

                var line2 = new Line
                {
                    Stroke = color,
                    StrokeThickness = thickness,
                    X1 = points[1].X,
                    Y1 = points[1].Y,
                    X2 = toColumn,
                    Y2 = toRow,
                    Tag = name
                };

                ui.Draw(line2);


                var line3 = new Line
                {
                    Stroke = color,
                    StrokeThickness = thickness,
                    X1 = points[0].X,
                    Y1 = points[0].Y,
                    X2 = toColumn,
                    Y2 = toRow,
                    Tag = name
                };

                ui.Draw(line3);

            });

        }
        /// <summary>
        /// 显示Cross
        /// </summary>
        /// <param name="pointRow">点位Row</param>
        /// <param name="pointColumn">点位Column</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="color">color</param>
        /// <param name="thickness">thickness</param>
        public void ShowCross(int pointRow, int pointColumn, int width, int height, Brush color, double thickness = 3)
        {
            Dispatch(() =>
            {

                var g1 = new LineGeometry
                {
                    StartPoint = new() { X = pointRow - width / 2, Y = pointColumn },
                    EndPoint = new() { X = pointRow + width / 2, Y = pointColumn },

                };
                var g2 = new LineGeometry
                {
                    StartPoint = new() { X = pointRow, Y = pointColumn - height / 2 },
                    EndPoint = new() { X = pointRow, Y = pointColumn + height / 2 },

                };
                var ui = UIElement as ImageEdit;
                if (ui == null) return;
                Path path1 = new()
                {
                    StrokeThickness = thickness,
                    Stroke = color,
                    Data = g1
                };
                ui.Draw(path1);

                Path path2 = new()
                {
                    StrokeThickness = thickness,
                    Stroke = color
                };
                path2.Data = g2;
                ui.Draw(path2);
            });

        }
        /// <summary>
        /// 显示Rectangle
        /// </summary>
        /// <param name="pointRow">点位Row</param>
        /// <param name="pointColumn">点位Column</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="color">color</param>
        /// <param name="thickness">thickness</param>
        public void ShowRectangle(int pointRow, int pointColumn, int width, int height, Brush color, double thickness = 3)
        {
            Dispatch(() =>
            {

                var g1 = new RectangleGeometry
                {
                    Rect = new System.Windows.Rect(pointColumn - width / 2, pointRow - height / 2, width, height),
                    RadiusX = 0,
                    RadiusY = 0
                };


                Path path1 = new()
                {
                    StrokeThickness = thickness,
                    Stroke = color
                };
                path1.Data = g1;
                var ui = UIElement as ImageEdit;
                if (ui == null) return;
                ui.Draw(path1);


            });

        }
        /// <summary>
        /// 显示圆弧
        /// </summary>
        /// <param name="startPoint">启动点位</param>
        /// <param name="middlePoint">middle点位</param>
        /// <param name="endPoint">end点位</param>
        /// <param name="color">color</param>
        /// <param name="thickness">thickness</param>
        public void ShowArc(XPoint startPoint, XPoint middlePoint, XPoint endPoint, Brush color, double thickness = 3)
        {
            Dispatch(() =>
            {
                PathGeometry geometry = new();
                PathFigure figure = new()
                {
                    StartPoint = new() { X = startPoint.X, Y = startPoint.Y }
                };
                figure.Segments.Add(new ArcSegment()
                {

                    Point = new() { X = endPoint.X, Y = endPoint.Y },
                    Size = new() { Width = middlePoint.X, Height = middlePoint.Y },

                });
                geometry.Figures.Add(figure);

                Path path1 = new()
                {
                    StrokeThickness = thickness,
                    Stroke = color
                };
                path1.Data = geometry;
                var ui = UIElement as ImageEdit;
                if (ui == null) return;
                ui.Draw(path1);
            });


        }
        /// <summary>
        /// 显示Text
        /// </summary>
        /// <param name="pointRow">点位Row</param>
        /// <param name="pointColumn">点位Column</param>
        /// <param name="text">text</param>
        /// <param name="fontSize">font尺寸</param>
        /// <param name="color">color</param>
        /// <param name="thickness">thickness</param>
        public void ShowText(int pointRow, int pointColumn, string text, double fontSize, Brush color, double thickness = 3)
        {
            Dispatch(() =>
            {
                TextBlock textBlock = new()
                {
                    Text = text,
                    FontSize = fontSize,
                    FontFamily = new FontFamily("Arial"),
                    Foreground = color
                };

                var ui = UIElement as ImageEdit;
                if (ui == null) return;
                ui.Draw(textBlock, pointRow, pointColumn);
            });

        }

        /// <summary>
        /// 设置CellState
        /// </summary>
        /// <param name="row">row</param>
        /// <param name="column">column</param>
        /// <param name="cellState">cellState</param>
        public void SetCellState(int row, int column, CellState cellState)
        {

            var cell = Params.CellInfos.Where(c => c.Row == row && c.Column == column).FirstOrDefault();
            if (cell != null)
            {
                cell.State = cellState;
            }
            else
            {
                throw new Exception($"未找到行{row}列{column}的单元格");
            }

        }
        /// <summary>
        /// 设置测量值 — 供外部模块（视觉/运动）按名称推送测量结果
        /// </summary>
        /// <param name="name">RangeValue 名称</param>
        /// <param name="value">测量值</param>
        public void SetMeasurementValue(string name, double value)
        {
            var rv = RangeValues.FirstOrDefault(r => r.Name == name);
            if (rv != null)
            {
                Dispatch(() =>
                {
                    rv.ShowValue = value;
                    rv.GetResult();
                });
            }
            else
            {
                throw new Exception($"未找到名称为 '{name}' 的 RangeValue");
            }
        }
        /// <summary>
        /// UIElement
        /// </summary>
        [NotMapped]
        public UIElement UIElement { get; set; }
        /// <summary>
        /// 控件Params
        /// </summary>
        public class ControlParams : BindableBase
        {
            private int _Rows;
            /// <summary>
            /// 自定义控件属性
            /// </summary>
            public int Rows
            {
                get { return _Rows; }
                set { SetProperty(ref _Rows, value); }
            }
            private int _Columns;
            /// <summary>
            /// 自定义控件属性
            /// </summary>
            public int Columns
            {
                get { return _Columns; }
                set { SetProperty(ref _Columns, value); }
            }

            private int _Layout;
            /// <summary>
            ///  <para>左 -> 右  上 -> 下 0  </para>
            ///  <para>左 -> 右  下 -> 上 1  </para>
            ///  <para>右 -> 左  上 -> 下 2  </para>
            ///  <para>右 -> 左  下 -> 上 3  </para>
            ///  <para>上 -> 下  左 -> 右 4  </para>
            ///  <para>上 -> 下  右 -> 左 5  </para>
            ///  <para>下 -> 上  左 -> 右 6  </para>
            ///  <para>下 -> 上  右 -> 左 7  </para>
            /// </summary>
            public int Layout
            {
                get { return _Layout; }
                set { SetProperty(ref _Layout, value); }
            }

            private ObservableCollection<CellInfo> _CellInfos = [];
            /// <summary>
            /// CellInfos
            /// </summary>
            public ObservableCollection<CellInfo> CellInfos
            {
                get { return _CellInfos; }
                set
                {
                    if (value != null)
                        SetProperty(ref _CellInfos, value);
                }
            }

        }
    }
    public enum ControlType
    {
        Image,
        State,
        RangeValueTalbe,

    }
}
