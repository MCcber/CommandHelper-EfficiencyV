using System.Windows;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorFlatImageTextButton:BaseVectorFlatButton
    {
        #region Property
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(VectorFlatImageTextButton), new PropertyMetadata(default(string)));

        public Thickness TextMargin
        {
            get { return (Thickness)GetValue(TextMarginProperty); }
            set { SetValue(TextMarginProperty, value); }
        }

        public static readonly DependencyProperty TextMarginProperty =
            DependencyProperty.Register(nameof(TextMargin), typeof(Thickness), typeof(VectorFlatImageTextButton), new PropertyMetadata(default(Thickness)));

        public ImageSource Image
        {
            get { return (ImageSource)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register("Image", typeof(ImageSource), typeof(VectorFlatImageTextButton), new PropertyMetadata(default(ImageSource)));

        public Thickness ImageMargin
        {
            get { return (Thickness)GetValue(ImageMarginProperty); }
            set { SetValue(ImageMarginProperty, value); }
        }

        public static readonly DependencyProperty ImageMarginProperty =
            DependencyProperty.Register("ImageMargin", typeof(Thickness), typeof(VectorFlatImageTextButton), new PropertyMetadata(default(Thickness)));
        #endregion
    }
}
