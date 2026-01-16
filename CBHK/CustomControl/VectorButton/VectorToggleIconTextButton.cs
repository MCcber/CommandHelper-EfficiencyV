using System.Windows;
using System.Windows.Controls.Primitives;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorToggleIconTextButton : ToggleButton
    {
        #region Field
        private Thickness OriginThumbMargin = new(0,-5,0,1);
        #endregion

        #region Property
        public Thickness ThumbMargin
        {
            get { return (Thickness)GetValue(ThumbMarginProperty); }
            set { SetValue(ThumbMarginProperty, value); }
        }

        public static readonly DependencyProperty ThumbMarginProperty =
            DependencyProperty.Register("ThumbMargin", typeof(Thickness), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Thickness)));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(string)));
        #endregion

        #region Method
        public VectorToggleIconTextButton()
        {
            ThumbMargin = OriginThumbMargin;
            Checked += VectorToggleIconTextButton_Checked;
            Unchecked += VectorToggleIconTextButton_Unchecked;
        }

        private void VectorToggleIconTextButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ThumbMargin = OriginThumbMargin;
        }

        private void VectorToggleIconTextButton_Checked(object sender, RoutedEventArgs e)
        {
            ThumbMargin = new(30, -5, 0, 1);
        }
        #endregion
    }
}
