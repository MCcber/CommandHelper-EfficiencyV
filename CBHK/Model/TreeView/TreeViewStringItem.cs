using CBHK.Interface.Data;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class TreeViewStringItem : IBaseItem, IStringItem, IBaseKeyItem, ITreeViewItem
    {
        public string Key { get; set; }
        public Brush BorderBrush { get; set; } = Brushes.Black;
        public Brush Foreground { get; set; } = Brushes.White;
        public Brush Background { get; set; } = Brushes.Transparent;
        public string Value { get; set; } = "";
    }
}
