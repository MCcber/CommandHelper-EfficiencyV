using CBHK.Interface.TreeView;
using CBHK.Model.Common;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class UnSignedFloatItem : IBaseItem, IBaseKeyItem, INumberItem<ushort>, ITreeViewItem
    {
        public string Key { get; set; }
        public Brush Foreground { get; set; } = Brushes.Black;
        public Brush Background { get; set; } = Brushes.Transparent;
        public ushort Value { get; set; }

        object INumberItem.Value
        {
            get => Value;
            set => Value = (ushort)value;
        }
        public NumberType ValueType { get; }
    }
}
