using System.Windows;

namespace CBHK.Common.Model
{
    public class RemoveComponentModel
    {
        public required FrameworkElement Container { get; set; }
        public required FrameworkElement Element { get; set; }
    }
}
