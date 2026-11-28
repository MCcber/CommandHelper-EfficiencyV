using CBHK.Utility.Visual;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CBHK.CustomControl.Input
{
    public class SearchBox : TextBox
    {
        #region Property
        public Geometry ClearSearchButtonIcon
        {
            get { return (Geometry)GetValue(ClearSearchButtonIconProperty); }
            set { SetValue(ClearSearchButtonIconProperty, value); }
        }

        public static readonly DependencyProperty ClearSearchButtonIconProperty =
            DependencyProperty.Register(nameof(ClearSearchButtonIcon), typeof(Geometry), typeof(SearchBox), new PropertyMetadata(default(Geometry)));

        public string WaterMarkerText
        {
            get { return (string)GetValue(WaterMarkerTextProperty); }
            set { SetValue(WaterMarkerTextProperty, value); }
        }

        public static readonly DependencyProperty WaterMarkerTextProperty =
            DependencyProperty.Register("WaterMarkerText", typeof(string), typeof(SearchBox), new PropertyMetadata(default(string)));

        public Brush WaterMarkerBrush
        {
            get { return (Brush)GetValue(WaterMarkerBrushProperty); }
            set { SetValue(WaterMarkerBrushProperty, value); }
        }

        public static readonly DependencyProperty WaterMarkerBrushProperty =
            DependencyProperty.Register("WaterMarkerBrush", typeof(Brush), typeof(SearchBox), new PropertyMetadata(default(Brush)));

        public Brush LocateLineBrush
        {
            get { return (Brush)GetValue(LocateLineBrushProperty); }
            set { SetValue(LocateLineBrushProperty, value); }
        }

        public static readonly DependencyProperty LocateLineBrushProperty =
            DependencyProperty.Register("LocateLineBrush", typeof(Brush), typeof(SearchBox), new PropertyMetadata(default(Brush)));

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(SearchBox), new PropertyMetadata(default(Brush)));

        public Thickness WaterMarkerMargin
        {
            get { return (Thickness)GetValue(WaterMarkerMarginProperty); }
            set { SetValue(WaterMarkerMarginProperty, value); }
        }

        public static readonly DependencyProperty WaterMarkerMarginProperty =
            DependencyProperty.Register("WaterMarkerMargin", typeof(Thickness), typeof(SearchBox), new PropertyMetadata(default(Thickness)));

        public bool HasText => (bool)GetValue(HasTextPropertyKey.DependencyProperty);

        private static readonly DependencyPropertyKey HasTextPropertyKey =
            DependencyProperty.RegisterReadOnly("HasText", typeof(bool), typeof(SearchBox), new PropertyMetadata(false));

        public Geometry SearchIcon
        {
            get { return (Geometry)GetValue(SearchIconProperty); }
            set { SetValue(SearchIconProperty, value); }
        }
        public static readonly DependencyProperty SearchIconProperty =
            DependencyProperty.Register(nameof(SearchIcon), typeof(Geometry), typeof(SearchBox), new PropertyMetadata(default(Geometry)));

        public Brush SearchIconBrush
        {
            get { return (Brush)GetValue(SearchIconBrushProperty); }
            set { SetValue(SearchIconBrushProperty, value); }
        }

        public static readonly DependencyProperty SearchIconBrushProperty =
            DependencyProperty.Register(nameof(SearchIconBrush), typeof(Brush), typeof(SearchBox), new PropertyMetadata(default(Brush)));

        public double SearhIconHeight
        {
            get { return (double)GetValue(SearhIconHeightProperty); }
            set { SetValue(SearhIconHeightProperty, value); }
        }

        public static readonly DependencyProperty SearhIconHeightProperty =
            DependencyProperty.Register(nameof(SearhIconHeight), typeof(double), typeof(SearchBox), new PropertyMetadata(default(double)));

        public double SearhIconWidth
        {
            get { return (double)GetValue(SearhIconWidthProperty); }
            set { SetValue(SearhIconWidthProperty, value); }
        }

        public static readonly DependencyProperty SearhIconWidthProperty =
            DependencyProperty.Register(nameof(SearhIconWidth), typeof(double), typeof(SearchBox), new PropertyMetadata(default(double)));

        public Thickness SearchIconMargin
        {
            get { return (Thickness)GetValue(SearchIconMarginProperty); }
            set { SetValue(SearchIconMarginProperty, value); }
        }

        public static readonly DependencyProperty SearchIconMarginProperty =
            DependencyProperty.Register(nameof(SearchIconMargin), typeof(Thickness), typeof(SearchBox), new PropertyMetadata(default(Thickness)));

        public IRelayCommand SearchClick
        {
            get { return (IRelayCommand)GetValue(SearchClickProperty); }
            set { SetValue(SearchClickProperty, value); }
        }

        public static readonly DependencyProperty SearchClickProperty =
            DependencyProperty.Register(nameof(SearchClick), typeof(IRelayCommand), typeof(SearchBox), new PropertyMetadata(default(IRelayCommand)));

        public IRelayCommand ClearSearchClick
        {
            get { return (IRelayCommand)GetValue(ClearSearchClickProperty); }
            set { SetValue(ClearSearchClickProperty, value); }
        }

        public static readonly DependencyProperty ClearSearchClickProperty =
            DependencyProperty.Register(nameof(ClearSearchClick), typeof(IRelayCommand), typeof(SearchBox), new PropertyMetadata(default(IRelayCommand)));
        #endregion

        #region Method
        public SearchBox()
        {
            // 主题底色由 VectorTextBoxStyle 提供：构造函数里设的是本地值，会压过样式，行内就改不成透明
            Loaded += VectorTextBox_Loaded;
        }

        public void UpdateBorderColorByBackgroundColor()
        {
            // 透明主题（行内"不带底色"的用法）：配色交给 XAML，别再刷掉 Background/BorderBrush
            if (ThemeBackground is SolidColorBrush { Color.A: 0 }) return;
            if (ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(themeBrush.Color);
                WaterMarkerBrush = ColorTool.GetWatermarkBrush(themeBrush);
                BorderBrush = LocateLineBrush = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.2f));
            }
        }
        #endregion

        #region Event
        private void VectorTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateBorderColorByBackgroundColor();
            PreviewKeyDown -= SearchBox_PreviewKeyDown;
            PreviewKeyDown += SearchBox_PreviewKeyDown;
        }

        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Enter)
            {
                SearchClick?.Execute(null);
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == ThemeBackgroundProperty)
            {
                UpdateBorderColorByBackgroundColor();
            }
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            SetValue(HasTextPropertyKey, Text is not null && Text.Length > 0);
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            if (!IsFocused && ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.1f));
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(themeBrush.Color);
            }
        }
        #endregion
    }
}
