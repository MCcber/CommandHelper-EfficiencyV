using CBHK.Interface.TreeView;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class ParentItem : IBaseItem, IBaseKeyItem, ITreeViewItem
    {
        public string Key { get; set; }
        public Brush Foreground { get; set; } = Brushes.White;
        public Brush Background { get; set; } = Brushes.Transparent;
    }
}