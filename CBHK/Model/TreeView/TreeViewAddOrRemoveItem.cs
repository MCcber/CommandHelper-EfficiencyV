using CBHK.Interface.Data;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    internal class TreeViewAddOrRemoveItem : IBaseItem, IBaseKeyItem, ITreeViewItem
    {
        public Brush Foreground { get; set; } = Brushes.Black;
        public Brush Background { get; set; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3C8527"));
        public Brush IconBrush { get; set; } = Brushes.White;
        public RelayCommand Command { get; set; }
        public string Key { get; set; }
    }
}
