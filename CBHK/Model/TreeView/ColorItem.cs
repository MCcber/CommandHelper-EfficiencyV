using CBHK.Interface.TreeView;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class ColorItem : IBaseItem, IBaseKeyItem, IColorItem, ITreeViewItem
    {
        public Color Value { get; set; }
        public string Key { get; set; }
        public Brush Foreground { get; set; } = Brushes.Black;
        public Brush Background { get; set; } = Brushes.Transparent;
    }
}