using System.Windows.Media;

namespace CBHK.Interface.TreeView
{
    public interface IBaseItem
    {
        public Brush Foreground { get; set; }
        public Brush Background { get; set; }
    }
}
