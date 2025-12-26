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
        #endregion

        #region Method
        public VectorToggleIconTextButton()
        {
            ThumbMargin = OriginThumbMargin;
            Click += VectorToggleIconTextButton_Click;
        }

        private void VectorToggleIconTextButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsChecked.Value)
            {
                ThumbMargin = new(30, -5, 0, 1);
            }
            else
            {
                ThumbMargin = OriginThumbMargin;
            }
        }
        #endregion
    }
}
