using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PLCSharp.Core.UserControls;

/// <summary>
/// ImageEdit 的扩展方法，提供便捷的图形绘制辅助
/// </summary>
internal static class ImageEditExtensions
{
    /// <summary>
    /// 在 ImageEdit 上画矩形边框 + 中心十字
    /// </summary>
    public static void DrawRectVisual(this ImageEdit imageEdit, double left, double top, double width, double height)
    {
        var rectBorder = new Rectangle
        {
            Width = width,
            Height = height,
            Stroke = Brushes.Magenta,
            StrokeThickness = 2,
            Tag = "ORB匹配",
        };
        Canvas.SetLeft(rectBorder, left);
        Canvas.SetTop(rectBorder, top);
        imageEdit.Draw(rectBorder);

        double cx = left + width / 2.0, cy = top + height / 2.0;
        imageEdit.DrawCross(cx, cy);
    }

    /// <summary>
    /// 在 ImageEdit 上画圆形边框 + 中心十字
    /// </summary>
    public static void DrawCircleVisual(this ImageEdit imageEdit, double cx, double cy, double radius)
    {
        var ellipse = new Ellipse
        {
            Width = radius * 2,
            Height = radius * 2,
            Stroke = Brushes.Magenta,
            StrokeThickness = 2,
            Tag = "ORB匹配",
        };
        Canvas.SetLeft(ellipse, cx - radius);
        Canvas.SetTop(ellipse, cy - radius);
        imageEdit.Draw(ellipse);

        imageEdit.DrawCross(cx, cy);
    }

    /// <summary>
    /// 在 ImageEdit 上画中心十字
    /// </summary>
    public static void DrawCross(this ImageEdit imageEdit, double cx, double cy)
    {
        const int len = 10;
        var crossH = new Line
        {
            X1 = cx - len,
            Y1 = cy,
            X2 = cx + len,
            Y2 = cy,
            Stroke = Brushes.Magenta,
            StrokeThickness = 1,
            Tag = "ORB匹配",
        };
        imageEdit.Draw(crossH);
        var crossV = new Line
        {
            X1 = cx,
            Y1 = cy - len,
            X2 = cx,
            Y2 = cy + len,
            Stroke = Brushes.Magenta,
            StrokeThickness = 1,
            Tag = "ORB匹配",
        };
        imageEdit.Draw(crossV);
    }
}
