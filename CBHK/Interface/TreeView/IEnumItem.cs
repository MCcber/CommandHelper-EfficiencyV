using System.Collections.ObjectModel;

namespace CBHK.Interface.TreeView
{
    public interface IEnumItem
    {
        public ObservableCollection<string> ItemList { get; set; }
    }
}
