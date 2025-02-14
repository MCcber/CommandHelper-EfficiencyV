using cbhk.CustomControls.JsonTreeViewComponents;
using System.Collections.ObjectModel;
using System.Text;

namespace cbhk.Model.Common
{
    public class JsonTreeViewDataStructure
    {
        public ObservableCollection<JsonTreeViewItem> Result { get; set; } = [];
        public StringBuilder ResultString { get; set; } = new();
        //public bool HaveReferenceContent { get; set; } = false;
    }
}
