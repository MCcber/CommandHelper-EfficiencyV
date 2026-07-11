using System.Windows.Media;

namespace CBHK.Interface.Data
{
    public interface IBaseItem
    {
        public Brush Foreground { get; set; }
        public Brush Background { get; set; }
    }
}
