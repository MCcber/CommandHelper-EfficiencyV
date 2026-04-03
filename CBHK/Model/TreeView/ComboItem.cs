using CBHK.CustomControl.VectorComboBox;
using CBHK.Interface.TreeView;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class ComboItem : IBaseItem, IBaseKeyItem, IEnumItem, ITreeViewItem
    {
        public Brush Foreground { get; set; }
        public Brush Background { get; set; }
        public string Key { get; set; }
        public VectorTextComboBoxItem SelectedItem { get; set; }
        public ObservableCollection<VectorTextComboBoxItem> ItemList { get; set; }

        public event EventHandler ItemChangedEvent;
    }
}