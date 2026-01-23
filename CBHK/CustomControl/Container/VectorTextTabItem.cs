using CBHK.Utility.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    public class VectorTextTabItem:TabItem
    {
        #region Field
        private bool isLoaded = false;
        #endregion

        #region Property
        public Thickness OriginMargin { get; set; }
        public Brush OriginBackground { get; set; }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(VectorTextTabItem), new PropertyMetadata(default(string)));

        public Brush SideBorderBrush
        {
            get { return (Brush)GetValue(SideBorderBrushProperty); }
            set { SetValue(SideBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty SideBorderBrushProperty =
            DependencyProperty.Register("SideBorderBrush", typeof(Brush), typeof(VectorTextTabItem), new PropertyMetadata(default(Brush)));

        public Brush TopBorderBrush
        {
            get { return (Brush)GetValue(TopBorderBrushProperty); }
            set { SetValue(TopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty TopBorderBrushProperty =
            DependencyProperty.Register("TopBorderBrush", typeof(Brush), typeof(VectorTextTabItem), new PropertyMetadata(default(Brush)));

        public Brush SelectedBackground
        {
            get { return (Brush)GetValue(SelectedBackgroundProperty); }
            set { SetValue(SelectedBackgroundProperty, value); }
        }

        public static readonly DependencyProperty SelectedBackgroundProperty =
            DependencyProperty.Register("SelectedBackground", typeof(Brush), typeof(VectorTextTabItem), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorTextTabItem()
        {
            Loaded += VectorTextTabItem_Loaded;
        }

        public void UpdateBackground()
        {
            var originSideBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, SideBorderBrushProperty);
            if (originSideBorderBrushSource.BaseValueSource is BaseValueSource.Default || originSideBorderBrushSource.BaseValueSource is BaseValueSource.Style || SideBorderBrush is null || isLoaded)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.4f);
                SideBorderBrush = new SolidColorBrush(color);
            }
            var originTopBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, TopBorderBrushProperty);
            if (originTopBorderBrushSource.BaseValueSource is BaseValueSource.Default || originTopBorderBrushSource.BaseValueSource is BaseValueSource.Style || TopBorderBrush is null || isLoaded)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.4f);
                TopBorderBrush = new SolidColorBrush(color);
            }
        }
        #endregion

        #region Event
        private void VectorTextTabItem_Loaded(object sender, RoutedEventArgs e)
        {
            if (Title == "")
            {
                Title = "Title";
            }

            var foregroundSource = DependencyPropertyHelper.GetValueSource(this, ForegroundProperty);
            if (foregroundSource.BaseValueSource is BaseValueSource.DefaultStyle || foregroundSource.BaseValueSource is BaseValueSource.Style)
            {
                Foreground = Brushes.White;
            }
            var backgroundSource = DependencyPropertyHelper.GetValueSource(this, BackgroundProperty);
            if (backgroundSource.BaseValueSource is BaseValueSource.DefaultStyle || backgroundSource.BaseValueSource is BaseValueSource.Style)
            {
                Background = new BrushConverter().ConvertFromString("#48382C") as Brush;
            }

            var selectedBackgroundSource = DependencyPropertyHelper.GetValueSource(this, SelectedBackgroundProperty);
            if (selectedBackgroundSource.BaseValueSource is BaseValueSource.DefaultStyle || selectedBackgroundSource.BaseValueSource is BaseValueSource.Style || SelectedBackground is null)
            {
                SelectedBackground = new BrushConverter().ConvertFromString("#CC6B23") as Brush;
            }

            var borderBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderBrushProperty);
            if (borderBrushSource.BaseValueSource is BaseValueSource.DefaultStyle || borderBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                BorderBrush = Brushes.Black;
            }

            OriginBackground ??= Background;
            if (!isLoaded)
            {
                OriginMargin = Margin;
            }

            if (IsSelected)
            {
                Margin = new(OriginMargin.Left, OriginMargin.Top - 2, OriginMargin.Right, OriginMargin.Bottom);
                Background = SelectedBackground;
            }

            UpdateBackground();

            isLoaded = true;
        }
        #endregion
    }
}
