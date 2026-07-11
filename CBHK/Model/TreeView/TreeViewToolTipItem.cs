using CBHK.Interface.Data;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class TreeViewToolTipItem : IBaseItem, IBaseKeyItem, ITreeViewItem
    {
        public string Key { get; set; }
        public Brush Foreground { get; set; } = Brushes.Black;
        public Brush Background { get; set; }
        public Geometry Icon { get; set; }
        public Brush IconBrush { get; set; } = Brushes.Gray;
    }
}
