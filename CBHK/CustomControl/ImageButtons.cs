using System.Windows;
using System.Windows.Media;

namespace CBHK.CustomControl
{
    public class ImageButtons:System.Windows.Controls.Button
    {
        public ImageSource ImageData
        {
            get { return (ImageSource)GetValue(ImageDataProperty); }
            set { SetValue(ImageDataProperty, value); }
        }

        public static readonly DependencyProperty ImageDataProperty =
            DependencyProperty.Register("ImageData", typeof(ImageSource), typeof(ImageButtons), new PropertyMetadata(default(ImageSource)));

        public ImageSource PressedImage
        {
            get { return (ImageSource)GetValue(PressedImageProperty); }
            set { SetValue(PressedImageProperty, value); }
        }

        public static readonly DependencyProperty PressedImageProperty =
            DependencyProperty.Register("PressedImage", typeof(ImageSource), typeof(ImageButtons), new PropertyMetadata(default(ImageSource)));
    }
}