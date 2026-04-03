using CBHK.Interface.TreeView;
using CBHK.Model.Common;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class UnSignedLongItem : IBaseItem, IBaseKeyItem, INumberItem<ulong>, ITreeViewItem
    {
        public string Key { get; set; }
        public Brush Foreground { get; set; } = Brushes.Black;
        public Brush Background { get; set; } = Brushes.Transparent;
        public ulong Value { get; set; }

        object INumberItem.Value
        {
            get => Value;
            set => Value = (ulong)value;
        }
        public NumberType ValueType { get; }
    }
}
