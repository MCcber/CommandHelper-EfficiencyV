using cbhk.CustomControls.JsonTreeViewComponents;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace cbhk.Model.Common
{
    public class JsonTreeViewDataStructure
    {
        public ObservableCollection<JsonTreeViewItem> Result { get; set; } = [];
        public StringBuilder ResultString { get; set; } = new();

        public Dictionary<string, List<JsonTreeViewItem>> DependencyList { get; set; } = [];
    }
}
