using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorToggleImageTextButton : VectorToggleTextButton
    {
        #region Field
        #endregion

        #region Property
        public ImageSource Image
        {
            get { return (ImageSource)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }
        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register(nameof(Image), typeof(ImageSource), typeof(VectorToggleImageTextButton), new PropertyMetadata(default(ImageSource)));
        #endregion

        #region Method
        public VectorToggleImageTextButton()
        {
            ClickMode = ClickMode.Press;
        }
        #endregion

        #region Event
        #endregion
    }
}
