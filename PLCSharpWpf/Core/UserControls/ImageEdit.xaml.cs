#nullable enable
using PLCSharp.Core.UserControls.ROI;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PLCSharp.Core.UserControls
{
    /// <summary>
    /// ImageEdit.xaml 的交互逻辑
    /// </summary>
    public partial class ImageEdit : UserControl
    {
        /// <summary>
        /// 图像编辑
        /// </summary>
        public ImageEdit()
        {
            InitializeComponent();
        }

        #region 缩放

        private double zoom = 1d;

        private const double ZoomStep = 0.001;
        private const double ZoomMin = 0.05;   // 5%
        private const double ZoomMax = 50.0;   // 5000%

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 鼠标位置
            Point p = e.GetPosition(mainBox1);

            // 缩放系数  e.Delta 上滚120 & 下滚-120
            double bs = 1 + e.Delta * ZoomStep;
            double newZoom = zoom * bs;

            // 限制缩放范围
            if (newZoom < ZoomMin || newZoom > ZoomMax)
                return;

            // 相对鼠标的移动量
            double offX = p.X - p.X * bs;
            double offY = p.Y - p.Y * bs;

            // 变换矩阵
            var newMatrix = new Matrix(bs, 0, 0, bs, offX, offY);
            matrix.Matrix = newMatrix * matrix.Matrix;
            zoom = newZoom;
            rate.Text = zoom.ToString("P2");
        }

        #endregion

        #region 图片

        /// <summary>
        /// 图像Source
        /// </summary>
        public WriteableBitmap ImageSource
        {
            get { return (WriteableBitmap)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ImageSource.  This enables animation, styling, binding, etc...
        /// <summary>
        /// 图像SourceProperty
        /// </summary>
        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register("ImageSource",
                typeof(WriteableBitmap),
                typeof(ImageEdit),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ImageSourceChangedCallback));

        private static void ImageSourceChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var u = (ImageEdit)d;

            // 总是重置缩放
            u.matrix.Matrix = Matrix.Identity;
            u.zoom = 1d;
            u.rate.Text = u.zoom.ToString("P2");

            if (e.NewValue is WriteableBitmap bmp)
            {
                u.img1.Source = bmp;
                u.columns.Text = bmp.Width.ToString("F0");
                u.rows.Text = bmp.Height.ToString("F0");
            }
            else
            {
                u.img1.Source = null;
                u.columns.Text = "0";
                u.rows.Text = "0";
            }
        }

        #endregion

        #region 拖动和画

        private DateTime _lastClickTime = DateTime.MinValue;
        private const int DoubleClickThreshold = 500; // 500毫秒内视为双击
        private bool DoubleClick;

        private bool isMouseLeftButtonDown;
        private Point previousMousePoint;
        private Point position;

        // --- ROI 绘制会话状态 ---
        private bool drawing;
        private ROIType _roiMode;                           // 当前绘制模式
        private RectROI? _tempRoi;                          // 矩形/旋转矩形橡皮筋预览
        private RectROI? _pendingRoi;                        // 左键松开后创建、等待右键确认的矩形ROI
        private RotateRectROI? _pendingRotateRoi;            // 左键松开后创建、等待右键确认的旋转矩形ROI
        private System.Windows.Shapes.Line? _tempLine;       // 直线橡皮筋预览
        private Polygon? _tempArrow;                          // 橡皮筋箭头
        private LineROI? _pendingLine;                        // 左键松开后创建、等待右键确认的直线
        private string? _lineAdjustMode;                      // null/"start"/"end"/"move"
        private Point _adjustOffset;
        private TaskCompletionSource<RectROI>? _roiTcs;
        private TaskCompletionSource<RotateRectROI>? _rotateRoiTcs;
        private TaskCompletionSource<LineROI>? _lineTcs;
        // --- 圆形状态 ---
        private System.Windows.Shapes.Ellipse? _tempCircle;  // 橡皮筋圆形预览
        private CircleROI? _pendingCircle;                   // 左键松开后创建、等待右键确认的圆形
        private string? _circleAdjustMode;                   // null/"move"/"radius"
        private TaskCompletionSource<CircleROI>? _circleTcs;
        private string? _drawTag;

        private void Img_MouseDown1(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            isMouseLeftButtonDown = true;
            previousMousePoint = e.GetPosition(mainBox1);

            // 捕获鼠标，确保拖拽过程中 MouseUp 始终回到 Canvas
            if (drawing || _pendingLine != null || _pendingCircle != null || _pendingRoi != null || _pendingRotateRoi != null)
                Mouse.Capture(mainBox1);

            // 直线调整模式：检测是否点击到端点或线段
            if (_roiMode == ROIType.Line && _pendingLine != null && !drawing)
            {
                double dStart = DistanceTo(previousMousePoint, _pendingLine.StartX, _pendingLine.StartY);
                double dEnd = DistanceTo(previousMousePoint, _pendingLine.EndX, _pendingLine.EndY);
                double dLine = PointToLineDistance(previousMousePoint,
                    _pendingLine.StartX, _pendingLine.StartY,
                    _pendingLine.EndX, _pendingLine.EndY);

                if (dStart < 12)
                {
                    _lineAdjustMode = "start";
                    _adjustOffset = new Point(_pendingLine.StartX - previousMousePoint.X,
                                               _pendingLine.StartY - previousMousePoint.Y);
                }
                else if (dEnd < 12)
                {
                    _lineAdjustMode = "end";
                    _adjustOffset = new Point(_pendingLine.EndX - previousMousePoint.X,
                                               _pendingLine.EndY - previousMousePoint.Y);
                }
                else if (dLine < 12)
                {
                    _lineAdjustMode = "move";
                    _adjustOffset = new Point(0, 0);
                }
                else
                {
                    _lineAdjustMode = null;
                }

                if (_lineAdjustMode != null)
                    return; // 进入调整模式，不再触发双击复位
            }

            // 圆形调整模式：检测是否点击到圆心或边缘
            if (_roiMode == ROIType.Circle && _pendingCircle != null && !drawing)
            {
                double dCenter = DistanceTo(previousMousePoint, _pendingCircle.CenterX, _pendingCircle.CenterY);
                double dEdge = Math.Abs(DistanceTo(previousMousePoint, _pendingCircle.CenterX, _pendingCircle.CenterY) - _pendingCircle.Radius);

                if (dCenter < 12)
                {
                    _circleAdjustMode = "move";
                    _adjustOffset = new Point(0, 0);
                }
                else if (dEdge < 12)
                {
                    _circleAdjustMode = "radius";
                    _adjustOffset = new Point(0, 0);
                }
                else
                {
                    _circleAdjustMode = null;
                }

                if (_circleAdjustMode != null)
                    return;
            }

            TimeSpan duration = DateTime.Now - _lastClickTime;
            if (duration.TotalMilliseconds <= DoubleClickThreshold)
            {
                matrix.Matrix = Matrix.Identity;
                zoom = 1d;
                rate.Text = zoom.ToString("P2");
                _lastClickTime = DateTime.MinValue; // 重置时间，避免连续触发
                DoubleClick = true;
            }
            else
            {
                _lastClickTime = DateTime.Now;
            }
        }

        private void Img_MouseUp1(object sender, MouseButtonEventArgs e)
        {
            DoubleClick = false;

            // 只处理左键松开
            if (e.ChangedButton != MouseButton.Left)
            {
                isMouseLeftButtonDown = false;
                return;
            }

            if (drawing)
            {
                // 直线模式：松开创建可拖动的预览线
                if (_roiMode == ROIType.Line && _tempLine != null)
                {
                    if (mainBox1.Children.Contains(_tempLine))
                        mainBox1.Children.Remove(_tempLine);
                    RemoveTempArrow();

                    double len = Math.Sqrt(
                        (_tempLine.X2 - _tempLine.X1) * (_tempLine.X2 - _tempLine.X1) +
                        (_tempLine.Y2 - _tempLine.Y1) * (_tempLine.Y2 - _tempLine.Y1));
                    if (len >= 1)
                    {
                        _pendingLine = new LineROI
                        {
                            StartX = _tempLine.X1,
                            StartY = _tempLine.Y1,
                            EndX = _tempLine.X2,
                            EndY = _tempLine.Y2,
                            Tag = _drawTag,
                        };
                        DrawPendingLineVisual();
                        drawing = false;
                    }
                    // len < 1 表示误点（无拖拽），保持 drawing = true 让用户重新画
                    isMouseLeftButtonDown = false;
                    return;
                }

                // 圆形模式：松开创建可拖动的预览圆
                if (_roiMode == ROIType.Circle && _tempCircle != null)
                {
                    if (mainBox1.Children.Contains(_tempCircle))
                        mainBox1.Children.Remove(_tempCircle);

                    double cx = previousMousePoint.X, cy = previousMousePoint.Y;
                    double r = DistanceTo(previousMousePoint, cx, cy);
                    // 注：previousMousePoint = 圆心(cx,cy)，刚松开时 position 是松开点，但 length 用圆心到 previous 算 = 0
                    // 实际半径应取圆心到松开鼠标位置 position 的距离
                    double radius = DistanceTo(position, cx, cy);
                    if (radius >= 1)
                    {
                        _pendingCircle = new CircleROI
                        {
                            CenterX = cx,
                            CenterY = cy,
                            Radius = radius,
                            Tag = _drawTag,
                        };
                        DrawPendingCircleVisual();
                        drawing = false;
                    }
                    isMouseLeftButtonDown = false;
                    return;
                }

                // 矩形/旋转矩形模式
                if (_tempRoi != null)
                {
                    if (mainBox1.Children.Contains(_tempRoi))
                        mainBox1.Children.Remove(_tempRoi);

                    var top = (int)Canvas.GetTop(_tempRoi);
                    var left = (int)Canvas.GetLeft(_tempRoi);
                    double width = _tempRoi.Width;
                    double height = _tempRoi.Height;

                    if (_roiMode == ROIType.RotateRect)
                    {
                        _pendingRotateRoi = new RotateRectROI
                        {
                            Width = width,
                            Height = height,
                            RectWidth = width,
                            RectHeight = height,
                            CenterX = left + width / 2,
                            CenterY = top + height / 2,
                            RectAngle = 0,
                            Tag = _drawTag,
                        };
                        mainBox1.Children.Add(_pendingRotateRoi);
                        Canvas.SetTop(_pendingRotateRoi, top);
                        Canvas.SetLeft(_pendingRotateRoi, left);
                    }
                    else
                    {
                        _pendingRoi = new RectROI
                        {
                            Top = top,
                            Left = left,
                            Width = width,
                            Height = height,
                            Tag = _drawTag,
                        };
                        mainBox1.Children.Add(_pendingRoi);
                        Canvas.SetTop(_pendingRoi, top);
                        Canvas.SetLeft(_pendingRoi, left);
                    }

                    drawing = false;
                }
            }

            isMouseLeftButtonDown = false;
            Mouse.Capture(null);
        }

        /// <summary>
        /// 绘制
        /// </summary>
        /// <param name="path">路径</param>
        public void Draw(FrameworkElement path)
        {
            mainBox1.Children.Add(path);
        }

        /// <summary>
        /// 绘制
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="row">row</param>
        /// <param name="column">column</param>
        public void Draw(FrameworkElement path, int row, int column)
        {
            Canvas.SetTop(path, row);
            Canvas.SetLeft(path, column);
            mainBox1.Children.Add(path);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="tag">tag</param>
        public void Remove(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;

            var toRemove = mainBox1.Children
                .OfType<FrameworkElement>()
                .Where(p => p.Tag is string pTag && pTag == tag)
                .ToList();

            foreach (var p in toRemove)
            {
                mainBox1.Children.Remove(p);
            }
        }

        /// <summary>
        /// 启动交互式 ROI 绘制。用户左键拖拽定义矩形，右键确认完成。
        /// </summary>
        public async Task<RectROI> DrawROIAsync(string tag)
        {
            Remove(tag);

            _roiMode = ROIType.Rect;
            _drawTag = tag;
            _tempRoi = new RectROI { Tag = tag };
            _pendingRoi = null;
            _roiTcs = new TaskCompletionSource<RectROI>();
            drawing = true;

            var roi = await _roiTcs.Task;

            return roi;
        }

        /// <summary>
        /// 启动交互式旋转矩形 ROI 绘制。左键拖拽定义矩形，右键确认完成。
        /// 确认后返回的 <see cref="RotateRectROI"/> 包含最终的 CenterX/CenterY/RectWidth/RectHeight/RectAngle。
        /// </summary>
        public async Task<RotateRectROI> DrawRotateRectROIAsync(string tag)
        {
            Remove(tag);

            _roiMode = ROIType.RotateRect;
            _drawTag = tag;
            _tempRoi = new RectROI { Tag = tag };
            _pendingRotateRoi = null;
            _rotateRoiTcs = new TaskCompletionSource<RotateRectROI>();
            drawing = true;

            var roi = await _rotateRoiTcs.Task;

            return roi;
        }

        /// <summary>
        /// 启动交互式直线绘制。用户左键拖拽定义直线，松开即完成。
        /// </summary>
        public async Task<LineROI> DrawLineAsync(string tag)
        {
            Remove(tag);

            _roiMode = ROIType.Line;
            _drawTag = tag;
            _tempLine = new System.Windows.Shapes.Line
            {
                Stroke = Brushes.Lime,
                StrokeThickness = 2,
                Tag = tag,
            };
            _lineTcs = new TaskCompletionSource<LineROI>();
            drawing = true;

            var roi = await _lineTcs.Task;

            return roi;
        }

        /// <summary>
        /// 启动交互式圆形绘制。用户左键拖拽定义圆形，松开进入调整，右键确认完成。
        /// </summary>
        public async Task<CircleROI> DrawCircleAsync(string tag)
        {
            Remove(tag);

            _roiMode = ROIType.Circle;
            _drawTag = tag;
            _tempCircle = new System.Windows.Shapes.Ellipse
            {
                Stroke = Brushes.Lime,
                StrokeThickness = 2,
                Tag = tag,
            };
            _circleTcs = new TaskCompletionSource<CircleROI>();
            drawing = true;

            var roi = await _circleTcs.Task;

            return roi;
        }

        private void Img_MouseLeave1(object sender, MouseEventArgs e)
        {
            isMouseLeftButtonDown = false;
        }

        private void Img_MouseMove1(object sender, MouseEventArgs e)
        {
            position = e.GetPosition(mainBox1);
            mousex.Text = position.X.ToString("F0");
            mousey.Text = position.Y.ToString("F0");

            // 读取像素值
            if (ImageSource != null)
            {
                int px = (int)Math.Clamp(position.X, 0, ImageSource.PixelWidth - 1);
                int py = (int)Math.Clamp(position.Y, 0, ImageSource.PixelHeight - 1);
                unsafe
                {
                    var buf = (byte*)ImageSource.BackBuffer.ToPointer();
                    int stride = ImageSource.BackBufferStride;
                    if (ImageSource.Format == System.Windows.Media.PixelFormats.Gray8)
                    {
                        var val = buf[py * stride + px];
                        pixelval.Text = val.ToString();
                    }
                    else if (ImageSource.Format == System.Windows.Media.PixelFormats.Bgr24)
                    {
                        int off = py * stride + px * 3;
                        pixelval.Text = $"R:{buf[off+2]} G:{buf[off+1]} B:{buf[off]}";
                    }
                    else if (ImageSource.Format == System.Windows.Media.PixelFormats.Bgra32)
                    {
                        int off = py * stride + px * 4;
                        pixelval.Text = $"R:{buf[off+2]} G:{buf[off+1]} B:{buf[off]}";
                    }
                    else
                    {
                        pixelval.Text = "";
                    }
                }
            }

            // 直线调整模式
            if (_lineAdjustMode != null && _pendingLine != null && isMouseLeftButtonDown)
            {
                double dx = position.X - previousMousePoint.X;
                double dy = position.Y - previousMousePoint.Y;

                switch (_lineAdjustMode)
                {
                    case "start":
                        _pendingLine.StartX = position.X + _adjustOffset.X;
                        _pendingLine.StartY = position.Y + _adjustOffset.Y;
                        break;
                    case "end":
                        _pendingLine.EndX = position.X + _adjustOffset.X;
                        _pendingLine.EndY = position.Y + _adjustOffset.Y;
                        break;
                    case "move":
                        _pendingLine.StartX += dx;
                        _pendingLine.StartY += dy;
                        _pendingLine.EndX += dx;
                        _pendingLine.EndY += dy;
                        break;
                }
                previousMousePoint = position;
                UpdatePendingLineVisual();
                return;
            }

            // 圆形调整模式
            if (_circleAdjustMode != null && _pendingCircle != null && isMouseLeftButtonDown)
            {
                switch (_circleAdjustMode)
                {
                    case "move":
                        _pendingCircle.CenterX += position.X - previousMousePoint.X;
                        _pendingCircle.CenterY += position.Y - previousMousePoint.Y;
                        break;
                    case "radius":
                        double r = DistanceTo(position, _pendingCircle.CenterX, _pendingCircle.CenterY);
                        if (r >= 1)
                            _pendingCircle.Radius = r;
                        break;
                }
                previousMousePoint = position;
                UpdatePendingCircleVisual();
                return;
            }

            if (!isMouseLeftButtonDown) return;

            if (drawing)
            {
                // 直线模式
                if (_roiMode == ROIType.Line && _tempLine != null)
                {
                    mainBox1.Children.Remove(_tempLine);
                    _tempLine.X1 = previousMousePoint.X;
                    _tempLine.Y1 = previousMousePoint.Y;
                    _tempLine.X2 = position.X;
                    _tempLine.Y2 = position.Y;
                    mainBox1.Children.Add(_tempLine);

                    // 橡皮筋箭头
                    RemoveTempArrow();
                    double tdx = position.X - previousMousePoint.X;
                    double tdy = position.Y - previousMousePoint.Y;
                    double tlen = Math.Sqrt(tdx * tdx + tdy * tdy);
                    if (tlen > 3)
                    {
                        double tux = tdx / tlen, tuy = tdy / tlen;
                        const double hs = 12;
                        _tempArrow = new Polygon
                        {
                            Fill = Brushes.Lime,
                            Points = new PointCollection
                            {
                                new Point(position.X, position.Y),
                                new Point(position.X - hs * (tux * 0.7 - tuy * 0.7),
                                           position.Y - hs * (tuy * 0.7 + tux * 0.7)),
                                new Point(position.X - hs * (tux * 0.7 + tuy * 0.7),
                                           position.Y - hs * (tuy * 0.7 - tux * 0.7)),
                            },
                            Tag = _drawTag,
                        };
                        mainBox1.Children.Add(_tempArrow);
                    }
                    return;
                }

                // 圆形模式
                if (_roiMode == ROIType.Circle && _tempCircle != null)
                {
                    mainBox1.Children.Remove(_tempCircle);
                    double cx = previousMousePoint.X, cy = previousMousePoint.Y;
                    double r = DistanceTo(position, cx, cy);
                    if (r < 1) return;

                    _tempCircle.Width = r * 2;
                    _tempCircle.Height = r * 2;
                    mainBox1.Children.Add(_tempCircle);
                    Canvas.SetLeft(_tempCircle, cx - r);
                    Canvas.SetTop(_tempCircle, cy - r);
                    return;
                }

                // 矩形/旋转矩形模式
                if (_tempRoi != null)
                {
                    mainBox1.Children.Remove(_tempRoi);
                    var startX = Math.Min(position.X, previousMousePoint.X);
                    var startY = Math.Min(position.Y, previousMousePoint.Y);
                    var endX = Math.Max(position.X, previousMousePoint.X);
                    var endY = Math.Max(position.Y, previousMousePoint.Y);
                    double width = endX - startX;
                    double height = endY - startY;

                    if (width < 1 || height < 1) return;

                    _tempRoi.Width = (int)width;
                    _tempRoi.Height = (int)height;

                    mainBox1.Children.Add(_tempRoi);
                    Canvas.SetTop(_tempRoi, startY);
                    Canvas.SetLeft(_tempRoi, startX);
                    return;
                }
            }

            if (!DoubleClick)
            {
                double offX = position.X - previousMousePoint.X;
                double offY = position.Y - previousMousePoint.Y;
                var newMatrix = new Matrix(1, 0, 0, 1, offX, offY);
                matrix.Matrix = newMatrix * matrix.Matrix;
            }
        }

        private void mainBox1_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 释放鼠标捕获
            Mouse.Capture(null);

            if (_pendingRoi != null)
            {
                // 计算最终坐标（包含ROI内部子元素的偏移）
                _pendingRoi.Width = _pendingRoi.ROI.Width;
                _pendingRoi.Height = _pendingRoi.ROI.Height;
                _pendingRoi.Left = Canvas.GetLeft(_pendingRoi) + Canvas.GetLeft(_pendingRoi.ROI);
                _pendingRoi.Top = Canvas.GetTop(_pendingRoi) + Canvas.GetTop(_pendingRoi.ROI);

                var result = _pendingRoi;
                _pendingRoi = null;

                if (_drawTag != null) Remove(_drawTag);
                _roiTcs?.TrySetResult(result);
            }
            else if (_pendingRotateRoi != null)
            {
                // 取内部 ContentControl 的实际 Canvas 位置（MoveThumb/ResizeThumb 实时更新它）
                var cc = _pendingRotateRoi.ROI;
                _pendingRotateRoi.CenterX = Canvas.GetLeft(_pendingRotateRoi) + Canvas.GetLeft(cc) + cc.ActualWidth / 2;
                _pendingRotateRoi.CenterY = Canvas.GetTop(_pendingRotateRoi) + Canvas.GetTop(cc) + cc.ActualHeight / 2;

                var result = _pendingRotateRoi;
                _pendingRotateRoi = null;

                if (_drawTag != null) Remove(_drawTag);
                _rotateRoiTcs?.TrySetResult(result);
            }
            else if (_pendingLine != null)
            {
                _lineAdjustMode = null;
                var result = _pendingLine;
                _pendingLine = null;

                if (_drawTag != null) RemoveLineVisual(_drawTag);
                _lineTcs?.TrySetResult(result);
            }
            else if (_pendingCircle != null)
            {
                _circleAdjustMode = null;
                var result = _pendingCircle;
                _pendingCircle = null;

                if (_drawTag != null) RemoveLineVisual(_drawTag);
                _circleTcs?.TrySetResult(result);
            }
        }

        #endregion

        #region 直线辅助方法

        private static double DistanceTo(Point p, double tx, double ty)
        {
            double dx = p.X - tx, dy = p.Y - ty;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static double PointToLineDistance(Point p, double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1, dy = y2 - y1;
            double len2 = dx * dx + dy * dy;
            if (len2 < 1e-10) return DistanceTo(p, x1, y1);
            double t = Math.Clamp(((p.X - x1) * dx + (p.Y - y1) * dy) / len2, 0, 1);
            double projX = x1 + t * dx, projY = y1 + t * dy;
            return DistanceTo(p, projX, projY);
        }

        private void DrawPendingLineVisual()
        {
            if (_pendingLine == null) return;
            string tag = _drawTag ?? "";

            double dx = _pendingLine.EndX - _pendingLine.StartX;
            double dy = _pendingLine.EndY - _pendingLine.StartY;
            double len = Math.Sqrt(dx * dx + dy * dy);

            // 线的本体
            var line = new System.Windows.Shapes.Line
            {
                X1 = _pendingLine.StartX,
                Y1 = _pendingLine.StartY,
                X2 = _pendingLine.EndX,
                Y2 = _pendingLine.EndY,
                Stroke = Brushes.Lime,
                StrokeThickness = 2,
                Tag = tag,
            };
            mainBox1.Children.Add(line);

            // 箭头（三角形）
            const double headSize = 14;
            var arrow = new Polygon
            {
                Fill = Brushes.Lime,
                Stroke = Brushes.Lime,
                StrokeThickness = 1,
                Tag = tag,
            };
            if (len > 1)
            {
                double ux = dx / len, uy = dy / len;
                double ex = _pendingLine.EndX, ey = _pendingLine.EndY;
                arrow.Points = new PointCollection
                {
                    new Point(ex, ey),
                    new Point(ex - headSize * (ux * 0.7 - uy * 0.7),
                               ey - headSize * (uy * 0.7 + ux * 0.7)),
                    new Point(ex - headSize * (ux * 0.7 + uy * 0.7),
                               ey - headSize * (uy * 0.7 - ux * 0.7)),
                };
            }
            else
            {
                arrow.Points = new PointCollection { new Point(0, 0), new Point(0, 0), new Point(0, 0) };
            }
            mainBox1.Children.Add(arrow);

            // 端点圆
            var epStart = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = Brushes.White,
                Stroke = Brushes.Lime,
                StrokeThickness = 2,
                Tag = tag,
            };
            mainBox1.Children.Add(epStart);
            Canvas.SetLeft(epStart, _pendingLine.StartX - 5);
            Canvas.SetTop(epStart, _pendingLine.StartY - 5);

            var epEnd = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = Brushes.White,
                Stroke = Brushes.Lime,
                StrokeThickness = 2,
                Tag = tag,
            };
            mainBox1.Children.Add(epEnd);
            Canvas.SetLeft(epEnd, _pendingLine.EndX - 5);
            Canvas.SetTop(epEnd, _pendingLine.EndY - 5);
        }

        private void UpdatePendingLineVisual()
        {
            if (_pendingLine == null) return;
            string tag = _drawTag ?? "";
            double ex = _pendingLine.EndX, ey = _pendingLine.EndY;
            double sx = _pendingLine.StartX, sy = _pendingLine.StartY;

            // 更新 Line
            var line = mainBox1.Children.OfType<System.Windows.Shapes.Line>()
                .FirstOrDefault(l => l.Tag is string s && s == tag && !double.IsNaN(l.X1));
            if (line != null)
            {
                line.X1 = sx; line.Y1 = sy;
                line.X2 = ex; line.Y2 = ey;
            }

            // 更新箭头
            double dx = ex - sx, dy = ey - sy;
            double len = Math.Sqrt(dx * dx + dy * dy);
            var arrow = mainBox1.Children.OfType<Polygon>()
                .FirstOrDefault(p => p.Tag is string s && s == tag);
            if (arrow != null && len > 1)
            {
                double ux = dx / len, uy = dy / len;
                const double headSize = 14;
                arrow.Points = new PointCollection
                {
                    new Point(ex, ey),
                    new Point(ex - headSize * (ux * 0.7 - uy * 0.7),
                               ey - headSize * (uy * 0.7 + ux * 0.7)),
                    new Point(ex - headSize * (ux * 0.7 + uy * 0.7),
                               ey - headSize * (uy * 0.7 - ux * 0.7)),
                };
            }

            // 更新端点圆
            var eps = mainBox1.Children.OfType<Ellipse>()
                .Where(e => e.Tag is string s && s == tag).ToList();
            if (eps.Count >= 2)
            {
                Canvas.SetLeft(eps[0], sx - 5);
                Canvas.SetTop(eps[0], sy - 5);
                Canvas.SetLeft(eps[1], ex - 5);
                Canvas.SetTop(eps[1], ey - 5);
            }
        }

        private void RemoveLineVisual(string tag)
        {
            var toRemove = mainBox1.Children
                .OfType<FrameworkElement>()
                .Where(p => p.Tag is string pTag && pTag == tag)
                .ToList();
            foreach (var p in toRemove)
                mainBox1.Children.Remove(p);
        }

        private void RemoveTempArrow()
        {
            if (_tempArrow != null && mainBox1.Children.Contains(_tempArrow))
                mainBox1.Children.Remove(_tempArrow);
            _tempArrow = null;
        }

        #endregion

        #region 圆形辅助方法

        private void DrawPendingCircleVisual()
        {
            if (_pendingCircle == null) return;
            string tag = _drawTag ?? "";
            double cx = _pendingCircle.CenterX, cy = _pendingCircle.CenterY;
            double r = _pendingCircle.Radius;

            // 圆形轮廓
            var ellipse = new System.Windows.Shapes.Ellipse
            {
                Width = r * 2,
                Height = r * 2,
                Stroke = Brushes.Lime,
                StrokeThickness = 2,
                Tag = tag,
            };
            mainBox1.Children.Add(ellipse);
            Canvas.SetLeft(ellipse, cx - r);
            Canvas.SetTop(ellipse, cy - r);

            // 圆心十字
            const double crossSize = 6;
            var crossH = new System.Windows.Shapes.Line
            {
                X1 = cx - crossSize, Y1 = cy,
                X2 = cx + crossSize, Y2 = cy,
                Stroke = Brushes.Lime,
                StrokeThickness = 2,
                Tag = tag,
            };
            mainBox1.Children.Add(crossH);
            var crossV = new System.Windows.Shapes.Line
            {
                X1 = cx, Y1 = cy - crossSize,
                X2 = cx, Y2 = cy + crossSize,
                Stroke = Brushes.Lime,
                StrokeThickness = 2,
                Tag = tag,
            };
            mainBox1.Children.Add(crossV);

            // 半径指示线（从圆心到圆周）
            var radiusLine = new System.Windows.Shapes.Line
            {
                X1 = cx, Y1 = cy,
                X2 = cx + r, Y2 = cy,
                Stroke = Brushes.Lime,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 3, 3 },
                Tag = tag,
            };
            mainBox1.Children.Add(radiusLine);

            // 端点圆（圆心可拖拽点 + 圆周可拖拽点）
            var epCenter = new Ellipse
            {
                Width = 10, Height = 10,
                Fill = Brushes.White,
                Stroke = Brushes.Lime,
                StrokeThickness = 2,
                Tag = tag,
            };
            mainBox1.Children.Add(epCenter);
            Canvas.SetLeft(epCenter, cx - 5);
            Canvas.SetTop(epCenter, cy - 5);

            var epEdge = new Ellipse
            {
                Width = 10, Height = 10,
                Fill = Brushes.White,
                Stroke = Brushes.Lime,
                StrokeThickness = 2,
                Tag = tag,
            };
            mainBox1.Children.Add(epEdge);
            Canvas.SetLeft(epEdge, cx + r - 5);
            Canvas.SetTop(epEdge, cy - 5);
        }

        private void UpdatePendingCircleVisual()
        {
            if (_pendingCircle == null) return;
            string tag = _drawTag ?? "";
            double cx = _pendingCircle.CenterX, cy = _pendingCircle.CenterY;
            double r = _pendingCircle.Radius;

            // 更新椭圆（取 Width 最大的那个，即圆轮廓本身）
            var ellipse = mainBox1.Children.OfType<System.Windows.Shapes.Ellipse>()
                .Where(e => e.Tag is string s && s == tag)
                .OrderByDescending(e => e.Width)
                .FirstOrDefault();
            if (ellipse != null)
            {
                ellipse.Width = r * 2;
                ellipse.Height = r * 2;
                Canvas.SetLeft(ellipse, cx - r);
                Canvas.SetTop(ellipse, cy - r);
            }

            // 更新十字线和半径线
            var lines = mainBox1.Children.OfType<System.Windows.Shapes.Line>()
                .Where(l => l.Tag is string s && s == tag).ToList();
            if (lines.Count >= 3)
            {
                // 水平线
                lines[0].X1 = cx - 6; lines[0].Y1 = cy;
                lines[0].X2 = cx + 6; lines[0].Y2 = cy;
                // 垂直线
                lines[1].X1 = cx; lines[1].Y1 = cy - 6;
                lines[1].X2 = cx; lines[1].Y2 = cy + 6;
                // 半径指示线
                lines[2].X1 = cx; lines[2].Y1 = cy;
                lines[2].X2 = cx + r; lines[2].Y2 = cy;
            }

            // 更新端点圆（取 Width == 10 的小圆，即端点标记）
            var eps = mainBox1.Children.OfType<Ellipse>()
                .Where(e => e.Tag is string s && s == tag && e.Width == 10)
                .ToList();
            if (eps.Count >= 2)
            {
                Canvas.SetLeft(eps[0], cx - 5);
                Canvas.SetTop(eps[0], cy - 5);
                Canvas.SetLeft(eps[1], cx + r - 5);
                Canvas.SetTop(eps[1], cy - 5);
            }
        }

        #endregion
    }

    /// <summary>
    /// ROI 形状类型
    /// </summary>
    public enum ROIType
    {
        Rect = 0,
        RotateRect = 1,
        Line = 2,
        Circle = 3,
    }

    /// <summary>
    /// 直线 ROI 结果，包含起点和终点坐标。
    /// </summary>
    public class LineROI
    {
        /// <summary>
        /// 启动X
        /// </summary>
        public double StartX { get; set; }
        /// <summary>
        /// 启动Y
        /// </summary>
        public double StartY { get; set; }
        /// <summary>
        /// EndX
        /// </summary>
        public double EndX { get; set; }
        /// <summary>
        /// EndY
        /// </summary>
        public double EndY { get; set; }
        /// <summary>
        /// Tag
        /// </summary>
        public string? Tag { get; set; }
    }

    /// <summary>
    /// 圆形 ROI 结果，包含圆心和半径。
    /// </summary>
    public class CircleROI
    {
        /// <summary>
        /// 圆心X
        /// </summary>
        public double CenterX { get; set; }
        /// <summary>
        /// 圆心Y
        /// </summary>
        public double CenterY { get; set; }
        /// <summary>
        /// 半径
        /// </summary>
        public double Radius { get; set; }
        /// <summary>
        /// Tag
        /// </summary>
        public string? Tag { get; set; }
    }
}
