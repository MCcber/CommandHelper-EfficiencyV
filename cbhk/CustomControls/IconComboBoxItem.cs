using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace cbhk.CustomControls
{
    public class IconComboBoxItem
    {
        public ImageSource ComboBoxItemIcon { get; set; } = new BitmapImage();

        public string ComboBoxItemText { get; set; } = "";
        public string ComboBoxItemId { get; set; } = "";
    }
}