

using Prism.Mvvm;

namespace PLCSharp.Core.Tools
{
    /// <summary>
    /// X点位
    /// </summary>
    public class XPoint : BindableBase, IEquatable<XPoint>
    {
        private double _X;
        /// <summary>
        /// X坐标
        /// </summary>
        public double X
        {
            get { return _X; }
            set { SetProperty(ref _X, Math.Round(value, 6)); }
        }

        private double _Y;
        /// <summary>
        /// Y
        /// </summary>
        public double Y
        {
            get { return _Y; }
            set { SetProperty(ref _Y, Math.Round(value, 6)); }
        }

        private double _Z;
        /// <summary>
        /// Z
        /// </summary>
        public double Z
        {
            get { return _Z; }
            set { SetProperty(ref _Z, Math.Round(value, 6)); }
        }

        private double _Angle;
        /// <summary>
        /// U
        /// </summary>
        public double Angle
        {
            get { return _Angle; }
            set { SetProperty(ref _Angle, Math.Round(value, 6)); }
        }



        /// <summary>
        /// 获取Offs点位
        /// </summary>
        /// <param name="orign">orign</param>
        /// <param name="angle">角度</param>
        /// <param name="before">before</param>
        /// <returns>返回结果</returns>
        public static XPoint GetOffsPoint(XPoint orign, double angle, XPoint before)
        {
            XPoint to = new();
            var t = Math.Atan2((orign.Y - before.Y), (orign.X - before.X)) * 180 / Math.PI;
            to.Angle = t + angle;
            var angleH = angle * Math.PI / 180;
            var x = before.X - orign.X;
            var y = before.Y - orign.Y;

            to.X = x * Math.Cos(angleH) + y * Math.Sin(angleH) + orign.X;
            to.Y = -x * Math.Sin(angleH) + y * Math.Cos(angleH) + orign.Y;
            to.Z = before.Z;
            return to;
        }

        public bool Equals(XPoint other)
        {
            if (other is null) return false;
            // 避免浮点数精度问题，通常使用一个极小值 Epsilon 进行比较
            return Math.Abs(X - other.X) < 1e-9
                && Math.Abs(Y - other.Y) < 1e-9
                && Math.Abs(Z - other.Z) < 1e-9
                && Math.Abs(Angle - other.Angle) < 1e-9;
        }
        public override bool Equals(object? obj) => Equals(obj as XPoint);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z, Angle);

        public static bool operator ==(XPoint? left, XPoint? right)
        {
            // 处理 null 的情况
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(XPoint? left, XPoint? right) => !(left == right);

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns>返回字符串</returns>
        public override string ToString()
        {
            return $"X:{X:F6},Y:{Y:F6},Z:{Z:F6},Angle:{Angle:F6}";
        }

    }
}
