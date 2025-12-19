using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorScrollBar
{
    public class VectorScrollBar:ScrollBar
    {
        #region Field
        private Brush OriginBackground;
        #endregion

        #region Property
        public Brush TopBorderBrush
        {
            get { return (Brush)GetValue(TopBorderBrushProperty); }
            set { SetValue(TopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty TopBorderBrushProperty =
            DependencyProperty.Register("TopBorderBrush", typeof(Brush), typeof(VectorScrollBar), new PropertyMetadata(default(Brush)));

        public Brush CornerBrush
        {
            get { return (Brush)GetValue(CornerBrushProperty); }
            set { SetValue(CornerBrushProperty, value); }
        }

        public static readonly DependencyProperty CornerBrushProperty =
            DependencyProperty.Register("CornerBrush", typeof(Brush), typeof(VectorScrollBar), new PropertyMetadata(default(Brush)));

        public Brush BottomBrush
        {
            get { return (Brush)GetValue(BottomBrushProperty); }
            set { SetValue(BottomBrushProperty, value); }
        }

        public static readonly DependencyProperty BottomBrushProperty =
            DependencyProperty.Register("BottomBrush", typeof(Brush), typeof(VectorScrollBar), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorScrollBar()
        {
            Loaded += VectorScrollBar_Loaded;
        }

        private void VectorScrollBar_Loaded(object sender, RoutedEventArgs e)
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E6E8EB"));

            OriginBackground = Background;
        }
        #endregion
    }
}
