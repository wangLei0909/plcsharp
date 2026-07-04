using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.Core.UserControls.ROI.DiagramDesigner
{
    /// <summary>
    /// Resize旋转Chrome
    /// </summary>
    public class ResizeRotateChrome : Control
    {
        static ResizeRotateChrome()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ResizeRotateChrome), new FrameworkPropertyMetadata(typeof(ResizeRotateChrome)));
        }
    }
}
