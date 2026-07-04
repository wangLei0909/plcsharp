using System.Collections.Concurrent;
using System.Drawing;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PLCSharp.Core.Tools
{
    /// <summary>
    /// Wpf工具
    /// </summary>
    public class WpfTool
    {
        private readonly ConcurrentQueue<PointF> PointsQueue = new();
        readonly List<PointF> Points = [];
        /// <summary>
        /// 执行绘制Async
        /// </summary>
        /// <param name="_imgSource">_imgSource</param>
        /// <returns>返回结果</returns>
        public async Task ExecuteDrawAsync(WriteableBitmap _imgSource)
        {
            while (true)
            {
                await Task.Delay(100);
                if (PointsQueue.IsEmpty) continue;

                while (!PointsQueue.IsEmpty)
                {
                    PointsQueue.TryDequeue(out PointF point);
                    Points.Add(point);
                }
                Application.Current?.Dispatcher.Invoke(delegate
                {
                    WriteableBitmap writeableBitmap = _imgSource;
                    int width = (int)writeableBitmap.Width;
                    int height = (int)writeableBitmap.Height;
                    writeableBitmap.Lock();
                    using var bitmap = new Bitmap(width, height, writeableBitmap.BackBufferStride, System.Drawing.Imaging.PixelFormat.Format24bppRgb, writeableBitmap.BackBuffer);
                    using Graphics graphics = Graphics.FromImage(bitmap);
                    graphics.Clear(System.Drawing.Color.Black);
                    //System.Drawing.FontFamily family = new("微软雅黑");

                    //求最小点，看是否为负；为负则整体偏移
                    var min_x = Points.Min(p => p.X);
                    var min_y = Points.Min(p => p.Y);

                    var points = Points.ToArray();
                    if (min_x < 0)
                        for (int i = 0; i < Points.Count; i++)
                        {
                            points[i] = new PointF(Points[i].X - min_x, Points[i].Y);
                        }
                    if (min_y < 0)
                        for (int i = 0; i < Points.Count; i++)
                        {
                            points[i] = new(Points[i].X, Points[i].Y - min_y);
                        }
                    for (int i = 0; i < Points.Count; i++)
                    {
                        points[i] = new(points[i].X + 100, points[i].Y + 100);
                    }
                    var max_x = points.Max(p => p.X);
                    var max_y = points.Max(p => p.Y);
                    var scalex = max_x / width;
                    var scaley = max_y / height;
                    var scale = Math.Max(scaley, scalex);
                    foreach (var point in points)
                    {

                        graphics.DrawRectangle(new System.Drawing.Pen(System.Drawing.Color.White), new Rectangle((int)(point.X / scale), (int)(point.Y / scale), 1, 1));

                    }

                    graphics.Flush();
                    writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
                    writeableBitmap.Unlock();

                });
            }
        }

        /// <summary>
        /// 获取RandColor
        /// </summary>
        /// <param name="start">启动</param>
        /// <param name="end">end</param>
        /// <returns>返回结果</returns>
        public static System.Windows.Media.Color GetRandColor(int start = 0, int end = 256)
        {
            // 创建一个Random对象
            Random random = new Random();

            // 生成一个0到255之间的随机整数作为红色分量
            byte red = Convert.ToByte(random.Next(start, end));

            // 生成一个0到255之间的随机整数作为绿色分量
            byte green = Convert.ToByte(random.Next(start, end));

            // 生成一个0到255之间的随机整数作为蓝色分量
            byte blue = Convert.ToByte(random.Next(start, end));

            // 将RGB值转换为ARGB格式的颜色值
            System.Windows.Media.Color color = System.Windows.Media.Color.FromArgb(255, red, green, blue);

            return color;

        }

        /// <summary>
        /// 获取RandBrush
        /// </summary>
        /// <param name="start">启动</param>
        /// <param name="end">end</param>
        /// <returns>返回结果</returns>
        public static System.Windows.Media.Brush GetRandBrush(int start = 0, int end = 256)
        {
            return new System.Windows.Media.SolidColorBrush(GetRandColor(start, end));
        }

        /// <summary>
        /// 在 WPF 视觉树中按类型查找第一个匹配的后代元素
        /// </summary>
        public static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var found = FindVisualChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }
    }
}
