using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.Core.UserControls.ROI
{
    /// <summary>
    /// 可拖动/缩放的矩形 ROI 控件
    /// </summary>
    public partial class RectROI : UserControl
    {
        /// <summary>
        /// 矩形ROI
        /// </summary>
        public RectROI()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Top
        /// </summary>
        public double Top
        {
            get { return (double)GetValue(TopProperty); }
            set { SetValue(TopProperty, value); }
        }

        /// <summary>
        /// TopProperty
        /// </summary>
        public static readonly DependencyProperty TopProperty =
            DependencyProperty.Register(nameof(Top), typeof(double), typeof(RectROI));

        /// <summary>
        /// Left
        /// </summary>
        public double Left
        {
            get { return (double)GetValue(LeftProperty); }
            set { SetValue(LeftProperty, value); }
        }

        /// <summary>
        /// LeftProperty
        /// </summary>
        public static readonly DependencyProperty LeftProperty =
            DependencyProperty.Register(nameof(Left), typeof(double), typeof(RectROI));
    }
}
