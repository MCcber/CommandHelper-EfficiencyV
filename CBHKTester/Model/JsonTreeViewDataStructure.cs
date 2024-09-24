using System.Collections.ObjectModel;
using System.Text;

namespace CBHKTester.Model
{
    public class JsonTreeViewDataStructure
    {
        public ObservableCollection<JsonTreeViewItem> Result { get; set; } = [];
        public StringBuilder ResultString { get; set; } = new();
    }
}
