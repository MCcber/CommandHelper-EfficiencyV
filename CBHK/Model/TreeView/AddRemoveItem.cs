using CBHK.Interface.TreeView;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class AddRemoveItem : IBaseItem, IBaseKeyItem, ITreeViewItem
    {
        public Brush Foreground { get; set; } = Brushes.Black;
        public Brush Background { get; set; }
        public Brush IconBrush { get; set; } = Brushes.White;
        public string Key { get; set; }
        public RelayCommand Command { get; set; }
    }
}
