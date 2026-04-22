using CBHK.Interface.TreeView;
using System.Collections.ObjectModel;

namespace CBHK.Model.TreeView
{
    /// <summary>
    /// 复合节点，用于自由组合其他节点，自动提供不同实现下对应的DataTemplate资源
    /// </summary>
    public class CompositeItem : ITreeViewItem
    {
        public ObservableCollection<ITreeViewItem> ItemList { get; set; } = [];
    }
}