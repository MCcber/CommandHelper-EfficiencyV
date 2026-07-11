using CBHK.CustomControl.VectorComboBox;
using System;
using System.Collections.ObjectModel;

namespace CBHK.Interface.Data
{
    public interface IEnumItem
    {
        event EventHandler ItemChangedEvent;
        public string SelectedItem { get; set; }
        public ObservableCollection<VectorTextComboBoxItem> ItemList { get; set; }
    }
}