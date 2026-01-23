using CBHK.Utility.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorToggleTextButton:ToggleButton
    {
        #region Property
        private Thickness OriginMargin;
        private Brush OriginTopBorderBrush;
        private Brush OriginBorderCornerBrush;
        private Brush OriginForegroundBrush;
        private Brush OriginBackgroundBrush;
        public virtual double MarginTopOffset { get; set; } = 5;
        public virtual double OriginBottomHeight { get; set; } = 6;

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(VectorToggleTextButton), new PropertyMetadata(default(string)));

        public Visibility IsShowCheckedMarker
        {
            get { return (Visibility)GetValue(IsShowCheckedMarkerProperty); }
            set { SetValue(IsShowCheckedMarkerProperty, value); }
        }

        public static readonly DependencyProperty IsShowCheckedMarkerProperty =
            DependencyProperty.Register("IsShowCheckedMarker", typeof(Visibility), typeof(VectorToggleTextButton), new PropertyMetadata(default(Visibility)));

        public double SelectedMarkerWidth
        {
            get { return (double)GetValue(SelectedMarkerWidthProperty); }
            set { SetValue(SelectedMarkerWidthProperty, value); }
        }

        public static readonly DependencyProperty SelectedMarkerWidthProperty =
            DependencyProperty.Register("SelectedMarkerWidth", typeof(double), typeof(VectorToggleTextButton), new PropertyMetadata(default(double)));

        public Brush CheckedMarkerBrush
        {
            get { return (Brush)GetValue(CheckedMarkerBrushProperty); }
            set { SetValue(CheckedMarkerBrushProperty, value); }
        }

        public static readonly DependencyProperty CheckedMarkerBrushProperty =
            DependencyProperty.Register("CheckedMarkerBrush", typeof(Brush), typeof(VectorToggleTextButton), new PropertyMetadata(default(Brush)));

        public Brush RoundBorderBrush
        {
            get { return (Brush)GetValue(RoundBorderBrushProperty); }
            set { SetValue(RoundBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RoundBorderBrushProperty =
            DependencyProperty.Register("RoundBorderBrush", typeof(Brush), typeof(VectorToggleTextButton), new PropertyMetadata(default(Brush)));

        public Brush TopBorderBrush
        {
            get { return (Brush)GetValue(TopBorderBrushProperty); }
            set { SetValue(TopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty TopBorderBrushProperty =
            DependencyProperty.Register("TopBorderBrush", typeof(Brush), typeof(VectorToggleTextButton), new PropertyMetadata(default(Brush)));

        public Brush BorderCornerBrush
        {
            get { return (Brush)GetValue(BorderCornerBrushProperty); }
            set { SetValue(BorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(VectorToggleTextButton), new PropertyMetadata(default(Brush)));

        public Brush OriginBottomBrush
        {
            get { return (Brush)GetValue(OriginBottomBrushProperty); }
            set { SetValue(OriginBottomBrushProperty, value); }
        }

        public static readonly DependencyProperty OriginBottomBrushProperty =
            DependencyProperty.Register("OriginBottomBrush", typeof(Brush), typeof(VectorToggleTextButton), new PropertyMetadata(default(Brush)));

        #endregion

        #region Method
        public VectorToggleTextButton()
        {
            Loaded += VectorToggleTextButton_Loaded;
            Click += VectorToggleTextButton_Click;
            MouseEnter += VectorToggleTextButton_MouseEnter;
            MouseLeave += VectorToggleTextButton_MouseLeave;
        }

        #endregion

        #region Event
        private void VectorToggleTextButton_Click(object sender, RoutedEventArgs e)
        {
            if(IsChecked.Value)
            {
                IsShowCheckedMarker = Visibility.Visible;
                This_PreviewMouseLeftButtonDown(sender, null);
            }
            else
            {
                IsShowCheckedMarker = Visibility.Hidden;
                VectorToggleTextButton_MouseEnter(sender, null);
            }
        }

        private void VectorToggleTextButton_Loaded(object sender, RoutedEventArgs e)
        {
            RoundBorderBrush = Brushes.Black;
            CheckedMarkerBrush = Brushes.White;
            SelectedMarkerWidth = Width / 6.15;
            OriginMargin = Margin;
            if (Text == "")
            {
                Text = "Button";
            }
            if (OriginBottomHeight == 0)
            {
                OriginBottomHeight = 6;
            }

            var foregroundSource = DependencyPropertyHelper.GetValueSource(this, ForegroundProperty);
            if (foregroundSource.BaseValueSource is BaseValueSource.DefaultStyle || foregroundSource.BaseValueSource is BaseValueSource.Style)
            {
                Foreground = Brushes.White;
            }
            var backgroundSource = DependencyPropertyHelper.GetValueSource(this, BackgroundProperty);
            if (backgroundSource.BaseValueSource is BaseValueSource.DefaultStyle || backgroundSource.BaseValueSource is BaseValueSource.Style)
            {
                Background = new BrushConverter().ConvertFromString("#3c8527") as Brush;
            }
            var borderBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderBrushProperty);
            if (borderBrushSource.BaseValueSource is BaseValueSource.DefaultStyle || borderBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                BorderBrush = Brushes.Black;
            }
            var originborderCornerBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderCornerBrushProperty);
            if (originborderCornerBrushSource.BaseValueSource is BaseValueSource.Default || originborderCornerBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.4f);
                BorderCornerBrush = OriginBorderCornerBrush = new SolidColorBrush(color);
            }
            var originTopBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, TopBorderBrushProperty);
            if (originTopBorderBrushSource.BaseValueSource is BaseValueSource.Default || originTopBorderBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.2f);
                TopBorderBrush = OriginTopBorderBrush = new SolidColorBrush(color);
            }
            var originBottomBrushSource = DependencyPropertyHelper.GetValueSource(this, OriginBottomBrushProperty);
            if (originBottomBrushSource.BaseValueSource is BaseValueSource.Default || originBottomBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                Color color = ColorTool.Darken((Background as SolidColorBrush).Color, 0.5f);
                OriginBottomBrush ??= new SolidColorBrush(color);
            }

            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(OriginBottomHeight, GridUnitType.Pixel);
            }

            OriginForegroundBrush = Foreground;
            OriginBackgroundBrush = Background;

            if(IsChecked is not null)
            {
                if(IsChecked.Value)
                {
                    IsShowCheckedMarker = Visibility.Visible;
                }
                else
                {
                    IsShowCheckedMarker = Visibility.Hidden;
                }
            }
        }

        private void This_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(0, GridUnitType.Pixel);
            }
            Color color = ColorTool.Darken((OriginBackgroundBrush as SolidColorBrush).Color, 0.4f);
            Background = new SolidColorBrush(color);
            Margin = new(Margin.Left, Margin.Top + MarginTopOffset, Margin.Right, Margin.Bottom);
        }

        private void VectorToggleTextButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if(IsChecked.Value)
            {
                return;
            }
            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(OriginBottomHeight, GridUnitType.Pixel);
            }
            Background = OriginBackgroundBrush;
            TopBorderBrush = OriginTopBorderBrush;
            BorderCornerBrush = OriginBorderCornerBrush;
            Margin = OriginMargin;
        }

        private void VectorToggleTextButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if(IsChecked.Value)
            {
                return;
            }
            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);

            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(OriginBottomHeight, GridUnitType.Pixel);
            }
            Color darkColor = ColorTool.Darken((OriginBackgroundBrush as SolidColorBrush).Color, 0.2f);
            Background = new SolidColorBrush(darkColor);
            Margin = OriginMargin;
            Color lightBorderColor = ColorTool.Lighten((OriginTopBorderBrush as SolidColorBrush).Color, 0.4f);
            TopBorderBrush = new SolidColorBrush(lightBorderColor);
            Color lightCornerColor = ColorTool.Lighten((OriginBorderCornerBrush as SolidColorBrush).Color, 0.6f);
            BorderCornerBrush = new SolidColorBrush(lightCornerColor);
        }
        #endregion
    }
}
