using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.Core.UserControls.ROI
{
    /// <summary>
    /// RotateRectROI.xaml 的交互逻辑
    /// </summary>
    public partial class RotateRectROI : UserControl
    {
        /// <summary>
        /// 旋转矩形ROI
        /// </summary>
        public RotateRectROI()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 圆心 Y
        /// </summary>
        public double CenterY
        {
            get { return (double)GetValue(CenterYProperty); }
            set { SetValue(CenterYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Top.  This enables animation, styling, binding, etc...
        /// <summary>
        /// 中心YProperty
        /// </summary>
        public static readonly DependencyProperty CenterYProperty =
            DependencyProperty.Register("CenterY", typeof(double), typeof(RotateRectROI), new PropertyMetadata(50d));

        /// <summary>
        /// 圆心 X
        /// </summary>
        public double CenterX
        {
            get { return (double)GetValue(CenterXProperty); }
            set { SetValue(CenterXProperty, value); }
        }

        /// <summary>
        /// 中心XProperty
        /// </summary>
        public static readonly DependencyProperty CenterXProperty =
            DependencyProperty.Register("CenterX", typeof(double), typeof(RotateRectROI), new PropertyMetadata(50d));

        /// <summary>
        /// 矩形宽度
        /// </summary>
        public double RectWidth
        {
            get { return (double)GetValue(RectWidthProperty); }
            set { SetValue(RectWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RRWidth.  This enables animation, styling, binding, etc...
        /// <summary>
        /// 矩形宽度Property
        /// </summary>
        public static readonly DependencyProperty RectWidthProperty =
            DependencyProperty.Register("RectWidth", typeof(double), typeof(RotateRectROI), new PropertyMetadata(100d));

        /// <summary>
        /// 矩形高度
        /// </summary>
        public double RectHeight
        {
            get { return (double)GetValue(RectHeightProperty); }
            set { SetValue(RectHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RRHeig.  This enables animation, styling, binding, etc...
        /// <summary>
        /// 矩形高度Property
        /// </summary>
        public static readonly DependencyProperty RectHeightProperty =
            DependencyProperty.Register("RectHeight", typeof(double), typeof(RotateRectROI), new PropertyMetadata(100d));

        /// <summary>
        /// 矩形角度
        /// </summary>
        public double RectAngle
        {
            get { return (double)GetValue(RectAngleProperty); }
            set { SetValue(RectAngleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RectAngle.  This enables animation, styling, binding, etc...
        /// <summary>
        /// 矩形角度Property
        /// </summary>
        public static readonly DependencyProperty RectAngleProperty =
            DependencyProperty.Register("RectAngle", typeof(double), typeof(RotateRectROI), new PropertyMetadata(0d));

    }
}