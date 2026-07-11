using CBHK.Model.TreeView;

namespace CBHK.Interface.Data
{
    public interface IContainerItem
    {
        public bool IsExpanded { get; set; }
        public TreeViewItemCollection<BaseTreeViewDataItem> Children { get; set; }
    }
}