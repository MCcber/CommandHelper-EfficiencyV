using System.Windows;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorFlatImageButton : BaseVectorFlatButton
    {
        #region Property
        public ImageSource Image
        {
            get { return (ImageSource)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register("Image", typeof(ImageSource), typeof(VectorFlatImageButton), new PropertyMetadata(default(ImageSource)));

        public Thickness ImageMargin
        {
            get { return (Thickness)GetValue(ImageMarginProperty); }
            set { SetValue(ImageMarginProperty, value); }
        }

        public static readonly DependencyProperty ImageMarginProperty =
            DependencyProperty.Register("ImageMargin", typeof(Thickness), typeof(VectorFlatImageButton), new PropertyMetadata(default(Thickness)));
        #endregion
    }
}