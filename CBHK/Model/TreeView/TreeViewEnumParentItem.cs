using CBHK.CustomControl.VectorComboBox;
using CBHK.Interface.Data;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class TreeViewEnumParentItem : IBaseItem, IBaseKeyItem, IEnumItem, ITreeViewItem
    {
        public string Key { get; set; }
        public Brush Foreground { get; set; } = Brushes.White;
        public Brush Background { get; set; } = Brushes.Transparent;
        public string SelectedItem { get; set; }
        public ObservableCollection<VectorTextComboBoxItem> ItemList { get; set; }
        public event EventHandler ItemChangedEvent;
    }
}
