using CBHK.CustomControl.VectorComboBox;
using System;
using System.Collections.ObjectModel;

namespace CBHK.Interface.TreeView
{
    public interface IEnumItem
    {
        event EventHandler ItemChangedEvent;
        public VectorTextComboBoxItem SelectedItem { get; set; }
        public ObservableCollection<VectorTextComboBoxItem> ItemList { get; set; }
    }
}