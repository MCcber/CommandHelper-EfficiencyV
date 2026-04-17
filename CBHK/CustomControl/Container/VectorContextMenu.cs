using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    public class VectorContextMenu : ContextMenu
    {
        #region Field
        private Brush OriginBorderCornerBrush;
        private Brush OriginRoundBorderBrush;
        #endregion

        #region Property
        public Brush RoundBorderBrush
        {
            get { return (Brush)GetValue(RoundBorderBrushProperty); }
            set { SetValue(RoundBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RoundBorderBrushProperty =
            DependencyProperty.Register("RoundBorderBrush", typeof(Brush), typeof(VectorContextMenu), new PropertyMetadata(default(Brush)));

        public Brush BorderCornerBrush
        {
            get { return (Brush)GetValue(BorderCornerBrushProperty); }
            set { SetValue(BorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(VectorContextMenu), new PropertyMetadata(default(Brush)));

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(VectorContextMenu), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorContextMenu()
        {
            SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
            Loaded += VectorContextMenu_Loaded;
        }

        public void UpdateBorderColorByBackgroundColor()
        {
            if (ThemeBackground is SolidColorBrush themeBrush)
            {
                BorderCornerBrush = OriginBorderCornerBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.4f));
                RoundBorderBrush = OriginRoundBorderBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.2f));
            }
        }
        #endregion

        #region Event
        private void VectorContextMenu_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateBorderColorByBackgroundColor();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if(e.Property == ThemeBackgroundProperty)
            {
                UpdateBorderColorByBackgroundColor();
            }
        }
        #endregion
    }
}
