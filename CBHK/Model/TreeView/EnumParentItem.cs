using CBHK.CustomControl.VectorComboBox;
using CBHK.Interface.TreeView;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class EnumParentItem : IBaseItem, IBaseKeyItem, IEnumItem, ITreeViewItem
    {
        public string Key { get; set; }
        public Brush Foreground { get; set; } = Brushes.White;
        public Brush Background { get; set; } = Brushes.Transparent;
        public string SelectedItem { get; set; }
        public ObservableCollection<VectorTextComboBoxItem> ItemList { get; set; }
        public event EventHandler ItemChangedEvent;
    }
}