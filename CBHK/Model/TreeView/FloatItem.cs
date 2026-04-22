using CBHK.Interface.TreeView;
using CBHK.Model.Common;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class FloatItem : IBaseItem, IBaseKeyItem, INumberItem<float>, ITreeViewItem
    {
        public string Key { get; set; }
        public Brush Foreground { get; set; } = Brushes.Black;
        public Brush Background { get; set; } = Brushes.Transparent;
        public float Value { get; set; }

        object INumberItem.Value
        {
            get => Value;
            set => Value = (float)value;
        }
        public NumberType ValueType { get; }
    }
}
