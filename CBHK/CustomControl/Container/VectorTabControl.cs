using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    public class VectorTabControl:TabControl
    {
        #region Property
        public Brush ContentSplitLineBrush
        {
            get { return (Brush)GetValue(ContentSplitLineBrushProperty); }
            set { SetValue(ContentSplitLineBrushProperty, value); }
        }

        public static readonly DependencyProperty ContentSplitLineBrushProperty =
            DependencyProperty.Register("ContentSplitLineBrush", typeof(Brush), typeof(VectorTabControl), new PropertyMetadata(default(Brush)));

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(VectorTabControl), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorTabControl()
        {
            SetResourceReference(ThemeBackgroundProperty,Theme.CommonBackground);
            Loaded += VectorTabControl_Loaded;
        }

        public void UpdateBorderColorByBackgroundColor()
        {
            if(ThemeBackground is SolidColorBrush themeBrush)
            {
                ContentSplitLineBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.6f));
            }
        }
        #endregion

        #region Event
        private void VectorTabControl_Loaded(object sender, RoutedEventArgs e)
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