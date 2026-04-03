using CBHK.Interface.TreeView;
using CBHK.Model.Common;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class UnSignedIntItem : IBaseItem, IBaseKeyItem, INumberItem<uint>, ITreeViewItem
    {
        public string Key { get; set; }
        public Brush Foreground { get; set; } = Brushes.Black;
        public Brush Background { get; set; } = Brushes.Transparent;
        public uint Value { get; set; }

        object INumberItem.Value
        {
            get => Value;
            set => Value = (uint)value;
        }
        public NumberType ValueType { get; }
    }
}
