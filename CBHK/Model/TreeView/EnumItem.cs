using CBHK.CustomControl.VectorComboBox;
using CBHK.Interface.TreeView;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace CBHK.Model.TreeView
{
    public class EnumItem : IBaseItem, IBaseKeyItem, IEnumItem, ITreeViewItem
    {
        public Brush Foreground { get; set; } = Brushes.Black;
        public Brush Background { get; set; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3C8527"));
        public string Key { get; set; }
        public string SelectedItem { get; set; } = string.Empty;
        public ObservableCollection<VectorTextComboBoxItem> ItemList { get; set; }

        public event EventHandler ItemChangedEvent;
    }
}