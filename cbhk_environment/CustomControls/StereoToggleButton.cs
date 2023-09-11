using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace cbhk_environment.CustomControls
{
    public class StereoToggleButton:ToggleButton
    {
        public Brush LeftBorderTexture
        {
            get { return (Brush)GetValue(LeftBorderTextureProperty); }
            set { SetValue(LeftBorderTextureProperty, value); }
        }


        public static readonly DependencyProperty LeftBorderTextureProperty =
            DependencyProperty.Register("LeftBorderTexture", typeof(Brush), typeof(ToggleButton), new PropertyMetadata(null));

        public Brush TopBorderTexture
        {
            get { return (Brush)GetValue(TopBorderTextureProperty); }
            set { SetValue(TopBorderTextureProperty, value); }
        }


        public static readonly DependencyProperty TopBorderTextureProperty =
            DependencyProperty.Register("TopBorderTexture", typeof(Brush), typeof(ToggleButton), new PropertyMetadata(null));

        public Brush RightBorderTexture
        {
            get { return (Brush)GetValue(RightBorderTextureProperty); }
            set { SetValue(RightBorderTextureProperty, value); }
        }


        public static readonly DependencyProperty RightBorderTextureProperty =
            DependencyProperty.Register("RightBorderTexture", typeof(Brush), typeof(ToggleButton), new PropertyMetadata(null));

        public Brush BottomBorderTexture
        {
            get { return (Brush)GetValue(BottomBorderTextureProperty); }
            set { SetValue(BottomBorderTextureProperty, value); }
        }


        public static readonly DependencyProperty BottomBorderTextureProperty =
            DependencyProperty.Register("BottomBorderTexture", typeof(Brush), typeof(ToggleButton), new PropertyMetadata(null));

        public Brush TrueTopBorderTexture
        {
            get { return (Brush)GetValue(TrueTopBorderTextureProperty); }
            set { SetValue(TrueTopBorderTextureProperty, value); }
        }

        public static readonly DependencyProperty TrueTopBorderTextureProperty =
            DependencyProperty.Register("TrueTopBorderTexture", typeof(Brush), typeof(ToggleButton), new PropertyMetadata(null));

        public Brush TrueBottomBorderTexture
        {
            get { return (Brush)GetValue(TrueBottomBorderTextureProperty); }
            set { SetValue(TrueBottomBorderTextureProperty, value); }
        }

        public static readonly DependencyProperty TrueBottomBorderTextureProperty =
            DependencyProperty.Register("TrueBottomBorderTexture", typeof(Brush), typeof(ToggleButton), new PropertyMetadata(null));

        public Brush TrueLeftBorderTexture
        {
            get { return (Brush)GetValue(TrueLeftBorderTextureProperty); }
            set { SetValue(TrueLeftBorderTextureProperty, value); }
        }

        public static readonly DependencyProperty TrueLeftBorderTextureProperty =
            DependencyProperty.Register("TrueLeftBorderTexture", typeof(Brush), typeof(ToggleButton), new PropertyMetadata(null));

        public Brush TrueRightBorderTexture
        {
            get { return (Brush)GetValue(TrueRightBorderTextureProperty); }
            set { SetValue(TrueRightBorderTextureProperty, value); }
        }

        public static readonly DependencyProperty TrueRightBorderTextureProperty =
            DependencyProperty.Register("TrueRightBorderTexture", typeof(Brush), typeof(ToggleButton), new PropertyMetadata(null));

        public Thickness IsCheckedThickness
        {
            get { return (Thickness)GetValue(IsCheckedThicknessProperty); }
            set { SetValue(IsCheckedThicknessProperty, value); }
        }

        public static readonly DependencyProperty IsCheckedThicknessProperty =
            DependencyProperty.Register("IsCheckedThickness", typeof(Thickness), typeof(ToggleButton), new PropertyMetadata(null));
    }
}
