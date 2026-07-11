using CBHK.Interface.Data;
using CBHK.Model.Data;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class TreeViewDecimalItem : IBaseItem, IBaseKeyItem, INumberItem<decimal>, ITreeViewItem
    {
        public string Key { get; set; }
        public Brush Foreground { get; set; } = Brushes.Black;
        public Brush Background { get; set; } = Brushes.Transparent;
        public decimal Value { get; set; }

        object INumberItem.Value
        {
            get => Value;
            set => Value = (decimal)value;
        }
        public NumberType ValueType { get; }
    }
}
