using CBHK.Interface.TreeView;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class BoolItem : IBaseItem, IBaseKeyItem, IBoolItem, ITreeViewItem
    {
        public string Key { get; set; }
        public Brush Foreground { get; set; } = Brushes.White;
        public Brush Background { get; set; } = Brushes.Transparent;
        public bool Value { get; set; } = true;
    }
}
