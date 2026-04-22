using CBHK.Interface.TreeView;
using CBHK.Model.Common;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class ShortItem : IBaseItem, IBaseKeyItem, INumberItem<short>, ITreeViewItem
    {
        public string Key { get; set; }
        public Brush Foreground { get; set; } = Brushes.Black;
        public Brush Background { get; set; } = Brushes.Transparent;
        public short Value { get; set; }

        object INumberItem.Value
        {
            get => Value;
            set => Value = (short)value;
        }
        public NumberType ValueType { get; }
    }
}
