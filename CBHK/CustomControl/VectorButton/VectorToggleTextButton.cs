using CBHK.Utility.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorToggleTextButton:ToggleButton
    {
        #region Field
        private Thickness OriginMargin;
        private Brush OriginLeftTopBorderBrush;
        private Brush OriginRightBottomBorderBrush;
        private Brush OriginBottomBorderBrush;
        private Brush OriginCheckMarkerBrush;
        private Brush OriginBorderCornerBrush;
        private Brush OriginForegroundBrush;
        private Brush OriginBackgroundBrush;
        private bool isUserClicking = false;
        private bool lastIsCheckedValue = false;
        #endregion

        #region Property
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

        public Brush LeftTopBorderBrush
        {
            get { return (Brush)GetValue(LeftTopBorderBrushProperty); }
            set { SetValue(LeftTopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftTopBorderBrushProperty =
            DependencyProperty.Register("LeftTopBorderBrush", typeof(Brush), typeof(VectorToggleTextButton), new PropertyMetadata(default(Brush)));

        public Brush RightBottomBorderBrush
        {
            get { return (Brush)GetValue(RightBottomBorderBrushProperty); }
            set { SetValue(RightBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RightBottomBorderBrushProperty =
            DependencyProperty.Register("RightBottomBorderBrush", typeof(Brush), typeof(VectorToggleTextButton), new PropertyMetadata(default(Brush)));

        public Brush BottomBorderBrush
        {
            get { return (Brush)GetValue(BottomBorderBrushProperty); }
            set { SetValue(BottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty BottomBorderBrushProperty =
            DependencyProperty.Register("BottomBorderBrush", typeof(Brush), typeof(VectorToggleTextButton), new PropertyMetadata(default(Brush)));

        public Brush BorderCornerBrush
        {
            get { return (Brush)GetValue(BorderCornerBrushProperty); }
            set { SetValue(BorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(VectorToggleTextButton), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorToggleTextButton()
        {
            Initialized += VectorToggleTextButton_Initialized;
            SizeChanged += VectorToggleTextButton_SizeChanged;
            MouseEnter += VectorToggleTextButton_MouseEnter;
            MouseLeave += VectorToggleTextButton_MouseLeave;
        }

        private void UpdateBorderColorByBackgroundColor(object sender)
        {
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
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.8f);
                BorderCornerBrush = OriginBorderCornerBrush = new SolidColorBrush(color);
            }
            var originLeftTopBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, LeftTopBorderBrushProperty);
            if (originLeftTopBorderBrushSource.BaseValueSource is BaseValueSource.Default || originLeftTopBorderBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.6f);
                LeftTopBorderBrush = OriginLeftTopBorderBrush = new SolidColorBrush(color);
            }
            var originRightBottomBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, RightBottomBorderBrushProperty);
            if (originRightBottomBorderBrushSource.BaseValueSource is BaseValueSource.Default || originRightBottomBorderBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.4f);
                RightBottomBorderBrush = OriginRightBottomBorderBrush = new SolidColorBrush(color);
            }
            var originBottomBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, BottomBorderBrushProperty);
            if (originBottomBorderBrushSource.BaseValueSource is BaseValueSource.Default || originBottomBorderBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.Darken(solidBorderBrush.Color, 0.5f);
                BottomBorderBrush = OriginBottomBorderBrush = new SolidColorBrush(color);
            }
            var originCheckMarkerBrushSource = DependencyPropertyHelper.GetValueSource(this, CheckedMarkerBrushProperty);
            if (originCheckMarkerBrushSource.BaseValueSource is BaseValueSource.Default || originCheckMarkerBrushSource.BaseValueSource is BaseValueSource.Style || CheckedMarkerBrush is null)
            {
                CheckedMarkerBrush = OriginCheckMarkerBrush = Brushes.White;
            }

            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(OriginBottomHeight, GridUnitType.Pixel);
            }

            OriginForegroundBrush ??= Foreground;
            OriginBackgroundBrush ??= Background;

            if (IsChecked is bool value && value)
            {
                OnChecked(null);
            }
            else
            {
                OnUnchecked(null);
            }
        }
        #endregion

        #region Event

        private void VectorToggleTextButton_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double currentWidth = ActualWidth <= 0 || ActualWidth is double.NaN ? Text.Length : ActualWidth;
            SelectedMarkerWidth = ActualWidth / 2;
        }

        protected override void OnClick()
        {
            isUserClicking = true;
            base.OnClick();
            isUserClicking = false;
        }

        protected override void OnChecked(RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                IsShowCheckedMarker = Visibility.Visible;
                if (IsChecked is bool value && value)
                {
                    This_PreviewMouseLeftButtonDown(this, null);
                }
            }
        }

        protected override void OnUnchecked(RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                IsShowCheckedMarker = Visibility.Hidden;
                if (IsChecked is bool value && !value && isUserClicking)
                {
                    VectorToggleTextButton_MouseEnter(this, null);
                }
                else
                {
                    VectorToggleTextButton_MouseLeave(this, null);
                }
            }
        }

        private void VectorToggleTextButton_Initialized(object sender, System.EventArgs e)
        {
            ApplyTemplate();
            BorderBrush = Brushes.Black;
            OriginMargin = Margin = new(0);
            OriginForegroundBrush = Foreground;

            IsShowCheckedMarker = Visibility.Hidden;

            if (Text == "")
            {
                Text = "Button";
            }
            if (OriginBottomHeight == 0)
            {
                OriginBottomHeight = 6;
            }

            UpdateBorderColorByBackgroundColor(sender);
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
            LeftTopBorderBrush = OriginLeftTopBorderBrush;
            RightBottomBorderBrush = OriginRightBottomBorderBrush;
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
            Color leftToplightBorderColor = ColorTool.Lighten((OriginLeftTopBorderBrush as SolidColorBrush).Color, 0.4f);
            LeftTopBorderBrush = new SolidColorBrush(leftToplightBorderColor);
            Color rightBottomlightBorderColor = ColorTool.Lighten((OriginRightBottomBorderBrush as SolidColorBrush).Color, 0.2f);
            RightBottomBorderBrush = new SolidColorBrush(rightBottomlightBorderColor);
            Color lightCornerColor = ColorTool.Lighten((OriginBorderCornerBrush as SolidColorBrush).Color, 0.2f);
            BorderCornerBrush = new SolidColorBrush(lightCornerColor);
        }
        #endregion
    }
}