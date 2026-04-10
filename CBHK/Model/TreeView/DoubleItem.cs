using CBHK.Interface.TreeView;
using CBHK.Model.Common;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class DoubleItem : IBaseItem, IBaseKeyItem, INumberItem<double>, ITreeViewItem
    {
        public string Key { get; set; }
        public Brush Foreground { get; set; } = Brushes.Black;
        public Brush Background { get; set; } = Brushes.Transparent;
        public double Value { get; set; }

        object INumberItem.Value
        {
            get => Value;
            set => Value = (double)value;
        }
        public NumberType ValueType { get; } = NumberType.Double;
    }
}