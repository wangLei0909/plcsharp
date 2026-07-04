using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

namespace PLCSharp.Core.UserControls.ROI.DiagramDesigner
{
    /// <summary>
    /// ResizeThumb
    /// </summary>
    public class ResizeThumb : Thumb
    {
        private RotateTransform rotateTransform;
        private double angle;
        private Adorner adorner;
        private Point transformOrigin;
        private ContentControl designerItem;
        private Canvas canvas;

        /// <summary>
        /// ResizeThumb
        /// </summary>
        public ResizeThumb()
        {
            DragStarted += new DragStartedEventHandler(ResizeThumb_DragStarted);
            DragDelta += new DragDeltaEventHandler(ResizeThumb_DragDelta);
            DragCompleted += new DragCompletedEventHandler(ResizeThumb_DragCompleted);
        }

        private void ResizeThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            designerItem = DataContext as ContentControl;

            if (designerItem != null)
            {
                canvas = VisualTreeHelper.GetParent(designerItem) as Canvas;

                if (canvas != null)
                {
                    transformOrigin = designerItem.RenderTransformOrigin;

                    rotateTransform = designerItem.RenderTransform as RotateTransform;
                    angle = rotateTransform != null ? rotateTransform.Angle * Math.PI / 180.0 : 0.0d;

                }
            }
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (designerItem != null)
            {
                double deltaVertical, deltaHorizontal;

                switch (VerticalAlignment)
                {
                    case VerticalAlignment.Bottom:
                        deltaVertical = Math.Min(-e.VerticalChange, designerItem.ActualHeight - designerItem.MinHeight);
                        Canvas.SetTop(designerItem, Canvas.GetTop(designerItem) + transformOrigin.Y * deltaVertical * (1 - Math.Cos(-angle)));
                        Canvas.SetLeft(designerItem, Canvas.GetLeft(designerItem) - deltaVertical * transformOrigin.Y * Math.Sin(-angle));
                        designerItem.Height -= deltaVertical;
                        break;
                    case VerticalAlignment.Top:
                        deltaVertical = Math.Min(e.VerticalChange, designerItem.ActualHeight - designerItem.MinHeight);
                        Canvas.SetTop(designerItem, Canvas.GetTop(designerItem) + deltaVertical * Math.Cos(-angle) + transformOrigin.Y * deltaVertical * (1 - Math.Cos(-angle)));
                        Canvas.SetLeft(designerItem, Canvas.GetLeft(designerItem) + deltaVertical * Math.Sin(-angle) - transformOrigin.Y * deltaVertical * Math.Sin(-angle));
                        designerItem.Height -= deltaVertical;
                        break;
                    case VerticalAlignment.Center:
                        break;
                    case VerticalAlignment.Stretch:
                        break;
                    default:
                        break;
                }

                switch (HorizontalAlignment)
                {
                    case HorizontalAlignment.Left:
                        deltaHorizontal = Math.Min(e.HorizontalChange, designerItem.ActualWidth - designerItem.MinWidth);
                        Canvas.SetTop(designerItem, Canvas.GetTop(designerItem) + deltaHorizontal * Math.Sin(angle) - transformOrigin.X * deltaHorizontal * Math.Sin(angle));
                        Canvas.SetLeft(designerItem, Canvas.GetLeft(designerItem) + deltaHorizontal * Math.Cos(angle) + transformOrigin.X * deltaHorizontal * (1 - Math.Cos(angle)));
                        designerItem.Width -= deltaHorizontal;
                        break;
                    case HorizontalAlignment.Right:
                        deltaHorizontal = Math.Min(-e.HorizontalChange, designerItem.ActualWidth - designerItem.MinWidth);
                        Canvas.SetTop(designerItem, Canvas.GetTop(designerItem) - transformOrigin.X * deltaHorizontal * Math.Sin(angle));
                        Canvas.SetLeft(designerItem, Canvas.GetLeft(designerItem) + deltaHorizontal * transformOrigin.X * (1 - Math.Cos(angle)));
                        designerItem.Width -= deltaHorizontal;
                        break;
                    case HorizontalAlignment.Center:
                        break;
                    case HorizontalAlignment.Stretch:
                        break;
                    default:
                        break;
                }
            }

            double x = Canvas.GetLeft(designerItem);
            double y = Canvas.GetTop(designerItem);

            if (designerItem != null)
            {
                Point dragDelta = new(e.HorizontalChange, e.VerticalChange);

                var rrr = (designerItem.Parent as Canvas).Parent as RotateRectROI;
                if (rrr is not null)
                {
                    rrr.CenterX = (x + dragDelta.X + x + dragDelta.X + designerItem.Width) / 2;
                    rrr.CenterY = (y + dragDelta.Y + y + dragDelta.Y + designerItem.Height) / 2;

                    rrr.RectWidth = designerItem.Width;
                    rrr.RectHeight = designerItem.Height;
                }

            }

            e.Handled = true;
        }

        private void ResizeThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (adorner != null)
            {
                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(canvas);
                if (adornerLayer != null)
                {
                    adornerLayer.Remove(adorner);
                }

                adorner = null;
            }
        }
    }
}
