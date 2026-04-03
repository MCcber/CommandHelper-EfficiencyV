using CBHK.Interface.TreeView;
using CBHK.Model.Common;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class ByteItem : IBaseItem, IBaseKeyItem, INumberItem<byte>, ITreeViewItem
    {
        public string Key { get; set; }
        public Brush Foreground { get; set; } = Brushes.Black;
        public Brush Background { get; set; } = Brushes.Transparent;
        public byte Value { get; set; }

        object INumberItem.Value
        {
            get => Value;
            set => Value = (byte)value;
        }
        public NumberType ValueType { get; } = NumberType.Byte;
    }
}
