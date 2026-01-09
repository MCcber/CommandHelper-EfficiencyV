using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorFlatImageButton:Button
    {
        public ImageSource Image
        {
            get { return (ImageSource)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register("Image", typeof(ImageSource), typeof(VectorFlatImageButton), new PropertyMetadata(default(ImageSource)));
    }
}
