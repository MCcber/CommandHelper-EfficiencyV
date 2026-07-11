using CBHK.Interface.Data;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class TreeViewParentItem : IBaseItem, IBaseKeyItem, ITreeViewItem
    {
        public string Key { get; set; }
        public Brush Foreground { get; set; } = Brushes.White;
        public Brush Background { get; set; } = Brushes.Transparent;
        public ObservableCollection<ITreeViewItem> ItemList { get; set; } = [];
    }
}
