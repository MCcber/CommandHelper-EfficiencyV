using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControls
{
    public class StereoTextButton:Button
    {
        public Brush LeftBorderTexture
        {
            get { return (Brush)GetValue(LeftBorderTextureProperty); }
            set { SetValue(LeftBorderTextureProperty, value); }
        }

        public static readonly DependencyProperty LeftBorderTextureProperty =
            DependencyProperty.Register("LeftBorderTexture", typeof(Brush), typeof(StereoTextButton), new PropertyMetadata(null));

        public Brush RightBorderTexture
        {
            get { return (Brush)GetValue(RightBorderTextureProperty); }
            set { SetValue(RightBorderTextureProperty, value); }
        }

        public static readonly DependencyProperty RightBorderTextureProperty =
            DependencyProperty.Register("RightBorderTexture", typeof(Brush), typeof(StereoTextButton), new PropertyMetadata(null));

        public Brush TopBorderTexture
        {
            get { return (Brush)GetValue(TopBorderTextureProperty); }
            set { SetValue(TopBorderTextureProperty, value); }
        }

        public static readonly DependencyProperty TopBorderTextureProperty =
            DependencyProperty.Register("TopBorderTexture", typeof(Brush), typeof(StereoTextButton), new PropertyMetadata(null));

        public Brush BottomBorderTexture
        {
            get { return (Brush)GetValue(BottomBorderTextureProperty); }
            set { SetValue(BottomBorderTextureProperty, value); }
        }

        public static readonly DependencyProperty BottomBorderTextureProperty =
            DependencyProperty.Register("BottomBorderTexture", typeof(Brush), typeof(StereoTextButton), new PropertyMetadata(null));

        public Brush PressedLeftBorderTexture
        {
            get { return (Brush)GetValue(PressedLeftBorderTextureProperty); }
            set { SetValue(PressedLeftBorderTextureProperty, value); }
        }

        public static readonly DependencyProperty PressedLeftBorderTextureProperty =
            DependencyProperty.Register("PressedLeftBorderTexture", typeof(Brush), typeof(StereoTextButton), new PropertyMetadata(null));

        public Brush PressedRightBorderTexture
        {
            get { return (Brush)GetValue(PressedRightBorderTextureProperty); }
            set { SetValue(PressedRightBorderTextureProperty, value); }
        }

        public static readonly DependencyProperty PressedRightBorderTextureProperty =
            DependencyProperty.Register("PressedRightBorderTexture", typeof(Brush), typeof(StereoTextButton), new PropertyMetadata(null));

        public Brush PressedTopBorderTexture
        {
            get { return (Brush)GetValue(PressedTopBorderTextureProperty); }
            set { SetValue(PressedTopBorderTextureProperty, value); }
        }

        public static readonly DependencyProperty PressedTopBorderTextureProperty =
            DependencyProperty.Register("PressedTopBorderTexture", typeof(Brush), typeof(StereoTextButton), new PropertyMetadata(null));

        public Brush PressedBottomBorderTexture
        {
            get { return (Brush)GetValue(PressedBottomBorderTextureProperty); }
            set { SetValue(PressedBottomBorderTextureProperty, value); }
        }

        public static readonly DependencyProperty PressedBottomBorderTextureProperty =
            DependencyProperty.Register("PressedBottomBorderTexture", typeof(Brush), typeof(StereoTextButton), new PropertyMetadata(null));

        public Thickness PressedBorderThickness
        {
            get { return (Thickness)GetValue(PressedBorderThicknessProperty); }
            set { SetValue(PressedBorderThicknessProperty, value); }
        }

        public static readonly DependencyProperty PressedBorderThicknessProperty =
            DependencyProperty.Register("PressedBorderThickness", typeof(Thickness), typeof(StereoTextButton), new PropertyMetadata(null));
    }
}
