using Microsoft.Xaml.Behaviors;
using System.ComponentModel;
using System.Windows.Controls;

namespace PLCSharp.Core.Prism
{
    /// <summary>
    /// ClassName：  ListBoxScrollToBottomBehavior
    /// Description：列表框滚动条行为
    /// Author：     luc
    /// CreatTime：  2022/12/12 18:11:48  
    /// </summary>
    public class DataGridScrollToBottomBehavior : Behavior<DataGrid>
    {
        /// <summary>
        /// OnAttached
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            ((ICollectionView)AssociatedObject.Items).CollectionChanged += ListBoxScrollToBottomBehavior_CollectionChanged;
        }

        /// <summary>
        /// OnDetaching
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            ((ICollectionView)AssociatedObject.Items).CollectionChanged -= ListBoxScrollToBottomBehavior_CollectionChanged;
        }

        private void ListBoxScrollToBottomBehavior_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!AssociatedObject.HasItems)
                return;

            // 如果 DataGrid 正处在 AddNew 或 EditItem 事务中，Skip 以避免
            // System.InvalidOperationException: "DeferRefresh" is not allowed during an AddNew or EditItem transaction.
            if (AssociatedObject.Items is System.ComponentModel.IEditableCollectionView editable &&
                (editable.IsAddingNew || editable.IsEditingItem))
            {
                return;
            }

            AssociatedObject.ScrollIntoView(AssociatedObject.Items[^1]);
        }
    }

}
