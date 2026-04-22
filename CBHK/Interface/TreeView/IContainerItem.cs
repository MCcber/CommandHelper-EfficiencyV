using CBHK.Model.TreeView;

namespace CBHK.Interface.TreeView
{
    public interface IContainerItem
    {
        public bool IsExpanded { get; set; }
        public TreeViewItemCollection<BaseTreeViewDataItem> Children { get; set; }
    }
}