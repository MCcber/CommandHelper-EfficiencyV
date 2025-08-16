using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace CBHK.CustomControl
{
    public class IconComboBoxItem
    {
        public ImageSource ComboBoxItemIcon { get; set; } = new BitmapImage();

        public string ComboBoxItemText { get; set; } = "";
        public string ComboBoxItemId { get; set; } = "";
    }
}