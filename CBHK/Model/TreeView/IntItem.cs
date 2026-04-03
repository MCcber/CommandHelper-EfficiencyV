using CBHK.Interface.TreeView;
using CBHK.Model.Common;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class IntItem : IBaseItem, IBaseKeyItem, INumberItem<int>, ITreeViewItem
    {
        public string Key { get; set; }
        public Brush Foreground { get; set; } = Brushes.Black;
        public Brush Background { get; set; } = Brushes.Transparent;
        public int Value { get; set; }

        object INumberItem.Value
        {
            get => Value;
            set => Value = (int)value;
        }
        public NumberType ValueType { get; }
    }
}