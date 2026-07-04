using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.Core.UserControls
{
    /// <summary>
    /// 树视图Ex
    /// </summary>
    public class TreeViewEx
    {
        static TreeViewEx()
        {
            EventManager.RegisterClassHandler(typeof(TreeView), TreeView.SelectedItemChangedEvent, new RoutedEventHandler(TreeView_SelectedItemChanged));
        }

        private static void TreeView_SelectedItemChanged(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (sender is not TreeView treeView) return;

            if (e is not RoutedPropertyChangedEventArgs<object> ee) return;

            SetSelectedItem(treeView, ee.NewValue);
        }

        private static Dictionary<DependencyObject, TreeViewSelectedItemBehavior> behaviors = new Dictionary<DependencyObject, TreeViewSelectedItemBehavior>();

        /// <summary>
        /// 获取Selected项
        /// </summary>
        /// <param name="obj">obj</param>
        /// <returns>返回 object</returns>
        public static object GetSelectedItem(DependencyObject obj)
        {
            return obj.GetValue(SelectedItemProperty);
        }

        /// <summary>
        /// 设置Selected项
        /// </summary>
        /// <param name="obj">obj</param>
        /// <param name="value">值</param>
        public static void SetSelectedItem(DependencyObject obj, object value)
        {
            obj.SetValue(SelectedItemProperty, value);
        }

        // Using a DependencyProperty as the backing store for SelectedItem.  This enables animation, styling, binding, etc...
        /// <summary>
        /// Selected项Property
        /// </summary>
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.RegisterAttached("SelectedItem", typeof(object),
                typeof(TreeViewEx), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SelectedItemChanged));

        private static void SelectedItemChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is not TreeView)
                return;

            if (!behaviors.ContainsKey(obj))
                behaviors.Add(obj, new TreeViewSelectedItemBehavior(obj as TreeView));

            TreeViewSelectedItemBehavior view = behaviors[obj];
            view.ChangeSelectedItem(e.NewValue);
        }

        private class TreeViewSelectedItemBehavior
        {
            private readonly TreeView view;

            /// <summary>
            /// 树视图Selected项Behavior
            /// </summary>
            public TreeViewSelectedItemBehavior(TreeView view)
            {
                this.view = view;
                view.SelectedItemChanged += (sender, e) => SetSelectedItem(view, e.NewValue);
            }

            /// <summary>
            /// ChangeSelected项
            /// </summary>
            /// <param name="p">p</param>
            internal void ChangeSelectedItem(object p)
            {
                var item = FindItemByDataContext(view, p);
                if (item != null)
                {
                    item.IsSelected = true;
                }
            }

            private TreeViewItem FindItemByDataContext(TreeView treeView, object dataContext)
            {
                for (int i = 0; i < treeView.Items.Count; i++)
                {
                    if (treeView.ItemContainerGenerator.ContainerFromIndex(i) is not TreeViewItem treeItem) continue;

                    var result = FindItemByDataContext(treeItem, dataContext);
                    if (result != null)
                    {
                        return result;
                    }
                }
                return null;
            }

            private TreeViewItem FindItemByDataContext(TreeViewItem item, object dataContext)
            {
                if (item.DataContext == dataContext)
                {
                    return item;
                }

                for (int i = 0; i < item.Items.Count; i++)
                {
                    if (item.ItemContainerGenerator.ContainerFromIndex(i) is not TreeViewItem subItem) continue;

                    var result = FindItemByDataContext(subItem, dataContext);
                    if (result != null)
                    {
                        return result;
                    }
                }
                return null;
            }
        }
    }
}