using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PLCSharp.Core.UserControls.ROI.DiagramDesigner
{
    /// <summary>
    /// Resize旋转Adorner
    /// </summary>
    public class ResizeRotateAdorner : Adorner
    {
        private VisualCollection visuals;
        private ResizeRotateChrome chrome;

        /// <summary>
        /// VisualChildren数量
        /// </summary>
        protected override int VisualChildrenCount
        {
            get
            {
                return visuals.Count;
            }
        }

        /// <summary>
        /// Resize旋转Adorner
        /// </summary>
        public ResizeRotateAdorner(ContentControl designerItem)
            : base(designerItem)
        {
            SnapsToDevicePixels = true;
            chrome = new ResizeRotateChrome();
            chrome.DataContext = designerItem;
            visuals = new VisualCollection(this);
            visuals.Add(chrome);
        }

        /// <summary>
        /// ArrangeOverride
        /// </summary>
        /// <param name="arrangeBounds">arrangeBounds</param>
        /// <returns>返回结果</returns>
        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            chrome.Arrange(new Rect(arrangeBounds));
            return arrangeBounds;
        }

        /// <summary>
        /// 获取VisualChild
        /// </summary>
        /// <param name="index">当前索引</param>
        /// <returns>返回结果</returns>
        protected override Visual GetVisualChild(int index)
        {
            return visuals[index];
        }
    }
}
