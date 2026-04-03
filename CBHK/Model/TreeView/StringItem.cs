using CBHK.Interface.TreeView;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class StringItem : IBaseItem, IStringItem, IBaseKeyItem, ITreeViewItem
    {
        public string Key { get; set; }
        public Brush BorderBrush { get; set; } = Brushes.Black;
        public Brush Foreground { get; set; } = Brushes.White;
        public Brush Background { get; set; } = Brushes.Transparent;
        public string Value { get; set; } = "";
    }
}