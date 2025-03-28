using CBHK.CustomControls.JsonTreeViewComponents;
using System.Collections.ObjectModel;
using System.Text;

namespace CBHK.Model.Common
{
    public class JsonTreeViewDataStructure
    {
        public ObservableCollection<JsonTreeViewItem> Result { get; set; } = [];
        public StringBuilder ResultString { get; set; } = new();
    }
}
