namespace PLCSharp.VVMs.Vision
{
    //一些视觉相关的数据类型
    public record struct Pos(double X, double Y,double Z, double Angle);
    public record struct Circle(Pos Center, double Radius);
    public record struct Line(Pos From,Pos To);
    public record struct Rect(Pos Center, double Width,double Height);
    public record struct Barcode(Pos Box, string Info);


}