using CBHK.Model.Constant;
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
        private bool isUserClicking = false;
        private Brush OriginLeftTopBorderBrush;
        private Brush OriginRightBottomBorderBrush;
        private Brush OriginBorderCornerBrush;
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

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(VectorToggleTextButton), new PropertyMetadata(default(Brush)));

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
            BorderBrush = Brushes.Black;
            Loaded += VectorToggleTextButton_Loaded;
            Initialized += VectorToggleTextButton_Initialized;
            SizeChanged += VectorToggleTextButton_SizeChanged;
            MouseEnter += VectorToggleTextButton_MouseEnter;
            MouseLeave += VectorToggleTextButton_MouseLeave;
        }

        private void UpdateBorderColorByBackgroundColor()
        {
            Background = new SolidColorBrush((ThemeBackground as SolidColorBrush).Color);

            BorderCornerBrush = new SolidColorBrush(ColorTool.Lighten((Background as SolidColorBrush).Color, 0.8f));
            LeftTopBorderBrush = new SolidColorBrush(ColorTool.Lighten((Background as SolidColorBrush).Color, 0.6f));
            RightBottomBorderBrush = new SolidColorBrush(ColorTool.Lighten((Background as SolidColorBrush).Color, 0.4f));
            BottomBorderBrush = new SolidColorBrush(ColorTool.Darken((Background as SolidColorBrush).Color, 0.5f));

            OriginBorderCornerBrush = BorderCornerBrush;
            OriginLeftTopBorderBrush = LeftTopBorderBrush;
            OriginRightBottomBorderBrush = RightBottomBorderBrush;

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
        private void VectorToggleTextButton_Loaded(object sender, RoutedEventArgs e)
        {
            SetResourceReference(ThemeBackgroundProperty, Theme.TextButtonBackground);
            SetResourceReference(ForegroundProperty, Theme.TextBlockForeground);
            SetResourceReference(CheckedMarkerBrushProperty, Theme.TextBlockForeground);

            UpdateBorderColorByBackgroundColor();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == ThemeBackgroundProperty && ThemeBackground is not null)
            {
                UpdateBorderColorByBackgroundColor();
            }
        }

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

            IsShowCheckedMarker = Visibility.Hidden;

            if (Text == "")
            {
                Text = "Button";
            }
            if (OriginBottomHeight == 0)
            {
                OriginBottomHeight = 6;
            }

            if (IsChecked is bool value && value)
            {
                This_PreviewMouseLeftButtonDown(this, null);
                IsShowCheckedMarker = Visibility.Visible;
            }
        }

        private void This_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(0, GridUnitType.Pixel);
            }
            Background = new SolidColorBrush(ColorTool.Darken((ThemeBackground as SolidColorBrush).Color, 0.4f));
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
            Background = ThemeBackground;
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

            Background = new SolidColorBrush(ColorTool.Darken((ThemeBackground as SolidColorBrush).Color, 0.2f));
            Margin = OriginMargin;
            LeftTopBorderBrush = new SolidColorBrush(ColorTool.Lighten((ThemeBackground as SolidColorBrush).Color, 0.4f));
            RightBottomBorderBrush = new SolidColorBrush(ColorTool.Lighten((ThemeBackground as SolidColorBrush).Color, 0.2f));
            BorderCornerBrush = new SolidColorBrush(ColorTool.Lighten((ThemeBackground as SolidColorBrush).Color, 0.2f));
        }
        #endregion
    }
}