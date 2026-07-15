using System.Linq;
using System.Windows.Media;

namespace PLCSharp.VVMs.Vision.VisionFlowHandler
{
    public class DrawCommand
    {
        public enum Type { Line, Circle, Polygon, Text }

        public Type Shape { get; init; }
        public double X1 { get; init; }
        public double Y1 { get; init; }
        public double X2 { get; init; }
        public double Y2 { get; init; }
        public double Radius { get; init; }
        public double Thickness { get; init; } = 1;
        public bool Filled { get; init; }
        public Color Color { get; init; } = Colors.Magenta;
        public Point[] Points { get; init; }
        public string Text { get; init; }
        public double FontSize { get; init; } = 12;
        public bool IsDrawn { get;  set; }
        public bool IsDrawnEdit { get; set; }

        public record struct Point(double X, double Y);

        public static DrawCommand Line(double x1, double y1, double x2, double y2, Color color, double thickness = 1)
            => new() { Shape = Type.Line, X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Color = color, Thickness = thickness };

        public static DrawCommand Circle(double cx, double cy, double r, Color color, double thickness = 1)
            => new() { Shape = Type.Circle, X1 = cx, Y1 = cy, Radius = r, Color = color, Thickness = thickness };

        public static DrawCommand FilledCircle(double cx, double cy, double r, Color color)
            => new() { Shape = Type.Circle, X1 = cx, Y1 = cy, Radius = r, Color = color, Filled = true, Thickness = 0 };

        public static DrawCommand TextBlock(double x, double y, string text, Color color, double fontSize = 12)
            => new() { Shape = Type.Text, X1 = x, Y1 = y, Text = text, Color = color, FontSize = fontSize };

        public static DrawCommand Polygon(System.Windows.Point[] pts, Color color, double thickness = 1)
            => new() { Shape = Type.Polygon, Points = pts.Select(p => new Point(p.X, p.Y)).ToArray(), Color = color, Thickness = thickness };
    }
}
