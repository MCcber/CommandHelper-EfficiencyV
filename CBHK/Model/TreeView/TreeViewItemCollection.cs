using CBHK.Interface.TreeView;
using System;
using System.Collections.ObjectModel;

namespace CBHK.Model.TreeView
{
    public class TreeViewItemCollection<T> : ObservableCollection<T> where T : BaseTreeViewDataItem
    {
        #region Field
        private readonly IContainerItem owner;
        private readonly Action<BaseTreeViewDataItem, IContainerItem> parentSetter;
        #endregion

        #region Method
        public TreeViewItemCollection(IContainerItem owner, Action<BaseTreeViewDataItem, IContainerItem> parentSetter)
        {
            this.owner = owner;
            this.parentSetter = parentSetter;
            if (this.parentSetter is null)
            {
                throw new ArgumentNullException("父级设置器为null，请向开发者反馈！");
            }
        }

        protected override void InsertItem(int index, T item)
        {
            // 如果项已有父级，可先解除原关系（可选）
            if (item.Parent is not null && item.Parent != owner)
            {
                item.Parent.Children.Remove(item);
            }

            parentSetter.Invoke(item, owner);
            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, T item)
        {
            var oldItem = this[index];
            if (oldItem is not null && oldItem.Parent == owner)
            {
                parentSetter.Invoke(oldItem, null);
            }
            if (item.Parent is not null && item.Parent != owner)
            {
                item.Parent.Children.Remove(item);
            }

            if (owner is not null)
            {
                parentSetter.Invoke(item, owner);
            }
            base.SetItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            var item = this[index];
            if (item is not null && item.Parent == owner)
            {
                parentSetter.Invoke(item, null);
            }
            base.RemoveItem(index);
        }

        protected override void ClearItems()
        {
            foreach (var item in this)
            {
                if (item.Parent == owner)
                {
                    parentSetter.Invoke(item, null);
                }
            }
            base.ClearItems();
        } 
        #endregion
    }
}
