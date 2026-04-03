using CBHK.Interface.TreeView;
using CBHK.Model.Common;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class LongItem :IBaseItem,IBaseKeyItem, INumberItem<long>, ITreeViewItem
    {
        public string Key { get; set; }
        public Brush Foreground { get; set; } = Brushes.Black;
        public Brush Background { get; set; } = Brushes.Transparent;
        public long Value { get; set; }

        object INumberItem.Value
        {
            get => Value;
            set => Value = (long)value;
        }
        public NumberType ValueType { get; }
    }
}
