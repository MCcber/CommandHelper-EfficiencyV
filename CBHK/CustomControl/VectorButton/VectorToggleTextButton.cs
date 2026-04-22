using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorToggleTextButton:BaseVectorToggleButton
    {
        #region Field
        private bool isNeedUpdateState = true;
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
            SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
            SetResourceReference(ForegroundProperty, Theme.CommonForeground);
            SetResourceReference(CheckedMarkerBrushProperty, Theme.CommonForeground);

            Loaded += VectorToggleTextButton_Loaded;
            Initialized += VectorToggleTextButton_Initialized;
            SizeChanged += VectorToggleTextButton_SizeChanged;
            MouseEnter += VectorToggleTextButton_MouseEnter;
            MouseLeave += VectorToggleTextButton_MouseLeave;
        }

        private void SetBottomHeight(double height)
        {
            if (GetTemplateChild("extraBottomLine") is RowDefinition rowDefinition)
            {
                rowDefinition.Height = new(height, GridUnitType.Pixel);
            }
        }

        public override void UpdateBorderColorByBackgroundColor()
        {
            base.UpdateBorderColorByBackgroundColor();

            if (ThemeBackground is SolidColorBrush solidColorBrush)
            {
                Background = new SolidColorBrush(solidColorBrush.Color);

                BorderCornerBrush = new SolidColorBrush(ColorTool.Lighten(solidColorBrush.Color, 0.8f));
                LeftTopBorderBrush = new SolidColorBrush(ColorTool.Lighten(solidColorBrush.Color, 0.6f));
                RightBottomBorderBrush = new SolidColorBrush(ColorTool.Lighten(solidColorBrush.Color, 0.4f));
                BottomBorderBrush = new SolidColorBrush(ColorTool.Darken(solidColorBrush.Color, 0.5f));

                OriginBorderCornerBrush = BorderCornerBrush;
                OriginLeftTopBorderBrush = LeftTopBorderBrush;
                OriginRightBottomBorderBrush = RightBottomBorderBrush;
            }

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
            UpdateBorderColorByBackgroundColor();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (IsChecked is bool value && value)
            {
                This_PreviewMouseLeftButtonDown(this, null);
                IsShowCheckedMarker = Visibility.Visible;
            }
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
            isNeedUpdateState = true;
            base.OnClick();
            isNeedUpdateState = false;
            isUserClicking = false;
        }

        protected override void OnChecked(RoutedEventArgs e)
        {
            IsShowCheckedMarker = Visibility.Visible;
            This_PreviewMouseLeftButtonDown(this, null);
        }

        protected override void OnUnchecked(RoutedEventArgs e)
        {
            IsShowCheckedMarker = Visibility.Hidden;
            if (isUserClicking)
            {
                VectorToggleTextButton_MouseEnter(this, null);
            }
            else
            {
                VectorToggleTextButton_MouseLeave(this, null);
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
        }

        private void This_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(!isNeedUpdateState)
            {
                return;
            }
            SetBottomHeight(0);
            if (ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.4f));
            }
            Margin = new(Margin.Left, MarginTopOffset, Margin.Right, Margin.Bottom);
        }

        private void VectorToggleTextButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if(IsChecked.Value)
            {
                return;
            }

            if(GetTemplateChild("extraBottomLine") is RowDefinition rowDefinition)
            {
                rowDefinition.Height = new(OriginBottomHeight, GridUnitType.Pixel);
            }
            Background = ThemeBackground;
            LeftTopBorderBrush = OriginLeftTopBorderBrush;
            RightBottomBorderBrush = OriginRightBottomBorderBrush;
            BorderCornerBrush = OriginBorderCornerBrush;
            Margin = OriginMargin;
        }

        private void VectorToggleTextButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if(IsChecked.Value)
            {
                return;
            }

            SetBottomHeight(OriginBottomHeight);

            Background = new SolidColorBrush(ColorTool.Darken((ThemeBackground as SolidColorBrush).Color, 0.2f));
            Margin = OriginMargin;
            LeftTopBorderBrush = new SolidColorBrush(ColorTool.Lighten((ThemeBackground as SolidColorBrush).Color, 0.4f));
            RightBottomBorderBrush = new SolidColorBrush(ColorTool.Lighten((ThemeBackground as SolidColorBrush).Color, 0.2f));
            BorderCornerBrush = new SolidColorBrush(ColorTool.Lighten((ThemeBackground as SolidColorBrush).Color, 0.2f));
        }
        #endregion
    }
}