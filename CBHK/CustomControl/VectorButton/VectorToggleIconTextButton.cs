using CBHK.Utility.Common;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorToggleIconTextButton : ToggleButton
    {
        #region Field
        private int OriginBottomHeight;
        private Thickness OriginThumbMargin;
        private Brush OriginThumbBackground;
        private Brush OriginForegroundBrush;
        private Brush OriginBottomBorderBrush;
        private Brush OriginLeftSideBorderCornerBrush;
        private Brush OriginLeftTopSelectedBorderBrush;
        private Brush OriginLeftTopBorderBrush;
        private Brush OriginLeftSideBottomBorderBrush;
        private Brush OriginRightBottomBorderBrush;
        private Brush OriginRightTopUnSelectedBorderBrush;
        private Brush OriginRightSideBottomBorderBrush;
        private Brush OriginRightSideBorderCornerBrush;
        private Brush OriginBorderCornerBrush;
        #endregion

        #region Property

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(string)));

        #region Thumb
        public Thickness ThumbMargin
        {
            get { return (Thickness)GetValue(ThumbMarginProperty); }
            set { SetValue(ThumbMarginProperty, value); }
        }

        public static readonly DependencyProperty ThumbMarginProperty =
            DependencyProperty.Register("ThumbMargin", typeof(Thickness), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Thickness)));

        public Brush ThumbBackground
        {
            get { return (Brush)GetValue(ThumbBackgroundProperty); }
            set { SetValue(ThumbBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThumbBackgroundProperty =
            DependencyProperty.Register("ThumbBackground", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));

        public double ThumbWidth
        {
            get { return (double)GetValue(ThumbWidthProperty); }
            set { SetValue(ThumbWidthProperty, value); }
        }

        public static readonly DependencyProperty ThumbWidthProperty =
            DependencyProperty.Register("ThumbWidth", typeof(double), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(double)));

        public double ThumbHeight
        {
            get { return (double)GetValue(ThumbHeightProperty); }
            set { SetValue(ThumbHeightProperty, value); }
        }

        public static readonly DependencyProperty ThumbHeightProperty =
            DependencyProperty.Register("ThumbHeight", typeof(double), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(double)));

        public Brush BottomBorderBrush
        {
            get { return (Brush)GetValue(BottomBorderBrushProperty); }
            set { SetValue(BottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty BottomBorderBrushProperty =
            DependencyProperty.Register("BottomBorderBrush", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));

        public Brush LeftTopBorderBrush
        {
            get { return (Brush)GetValue(LeftTopBorderBrushProperty); }
            set { SetValue(LeftTopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftTopBorderBrushProperty =
            DependencyProperty.Register("LeftTopBorderBrush", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));
        public Brush RightBottomBorderBrush
        {
            get { return (Brush)GetValue(RightBottomBorderBrushProperty); }
            set { SetValue(RightBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RightBottomBorderBrushProperty =
            DependencyProperty.Register("RightBottomBorderBrush", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));
        public Brush BorderCornerBrush
        {
            get { return (Brush)GetValue(BorderCornerBrushProperty); }
            set { SetValue(BorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));
        #endregion

        #region Background
        public Brush UnSelectedBackground
        {
            get { return (Brush)GetValue(UnSelectedBackgroundProperty); }
            set { SetValue(UnSelectedBackgroundProperty, value); }
        }

        public static readonly DependencyProperty UnSelectedBackgroundProperty =
            DependencyProperty.Register("UnSelectedBackground", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));

        public Brush SelectedBackground
        {
            get { return (Brush)GetValue(SelectedBackgroundProperty); }
            set { SetValue(SelectedBackgroundProperty, value); }
        }

        public static readonly DependencyProperty SelectedBackgroundProperty =
            DependencyProperty.Register("SelectedBackground", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));

        public double BackgroundWidth
        {
            get { return (double)GetValue(BackgroundWidthProperty); }
            set { SetValue(BackgroundWidthProperty, value); }
        }

        public static readonly DependencyProperty BackgroundWidthProperty =
            DependencyProperty.Register("BackgroundWidth", typeof(double), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(double)));

        public double BackgroundHeight
        {
            get { return (double)GetValue(BackgroundHeightProperty); }
            set { SetValue(BackgroundHeightProperty, value); }
        }

        public static readonly DependencyProperty BackgroundHeightProperty =
            DependencyProperty.Register("BackgroundHeight", typeof(double), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(double)));

        public Brush LeftTopSelectedBorderBrush
        {
            get { return (Brush)GetValue(LeftTopSelectedBorderBrushProperty); }
            set { SetValue(LeftTopSelectedBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftTopSelectedBorderBrushProperty =
            DependencyProperty.Register("LeftTopSelectedBorderBrush", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));

        public Brush RightTopUnSelectedBorderBrush
        {
            get { return (Brush)GetValue(RightTopUnSelectedBorderBrushProperty); }
            set { SetValue(RightTopUnSelectedBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RightTopUnSelectedBorderBrushProperty =
            DependencyProperty.Register("RightTopUnSelectedBorderBrush", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));

        public Brush LeftSideBottomSelectedBorderBrush
        {
            get { return (Brush)GetValue(LeftSideBottomSelectedBorderBrushProperty); }
            set { SetValue(LeftSideBottomSelectedBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftSideBottomSelectedBorderBrushProperty =
            DependencyProperty.Register("LeftSideBottomSelectedBorderBrush", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));

        public Brush RightSideBottomUnSelectedBorderBrush
        {
            get { return (Brush)GetValue(RightSideBottomUnSelectedBorderBrushProperty); }
            set { SetValue(RightSideBottomUnSelectedBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RightSideBottomUnSelectedBorderBrushProperty =
            DependencyProperty.Register("RightSideBottomUnSelectedBorderBrush", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));

        public Brush RightSideUnSelectedBorderCornerBrush
        {
            get { return (Brush)GetValue(RightSideUnSelectedBorderCornerBrushProperty); }
            set { SetValue(RightSideUnSelectedBorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty RightSideUnSelectedBorderCornerBrushProperty =
            DependencyProperty.Register("RightSideUnSelectedBorderCornerBrush", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));

        public Brush LeftSideSelectedBorderCornerBrush
        {
            get { return (Brush)GetValue(LeftSideSelectedBorderCornerBrushProperty); }
            set { SetValue(LeftSideSelectedBorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftSideSelectedBorderCornerBrushProperty =
            DependencyProperty.Register("LeftSideSelectedBorderCornerBrush", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));
        #endregion

        #endregion

        #region Method
        public VectorToggleIconTextButton()
        {
            Loaded += VectorToggleIconTextButton_Loaded;
            PreviewMouseLeftButtonUp += VectorToggleIconTextButton_PreviewMouseLeftButtonUp;
            PreviewMouseLeftButtonDown += VectorToggleIconTextButton_PreviewMouseLeftButtonDown;
            MouseLeave += VectorToggleIconTextButton_MouseLeave;
            MouseEnter += VectorToggleIconTextButton_MouseEnter;
            Click += VectorToggleIconTextButton_Click;
            Checked += VectorToggleIconTextButton_Checked;
            Unchecked += VectorToggleIconTextButton_Unchecked;
        }

        private void UpdateBorderColorByBackgroundColor(object sender)
        {
            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(OriginBottomHeight, GridUnitType.Pixel);
            }

            var foregroundSource = DependencyPropertyHelper.GetValueSource(this, ForegroundProperty);
            if (foregroundSource.BaseValueSource is BaseValueSource.DefaultStyle || foregroundSource.BaseValueSource is BaseValueSource.Style || Foreground is null)
            {
                Foreground = OriginForegroundBrush = Brushes.White;
            }

            var selectedbackgroundSource = DependencyPropertyHelper.GetValueSource(this, SelectedBackgroundProperty);
            if (selectedbackgroundSource.BaseValueSource is BaseValueSource.DefaultStyle || selectedbackgroundSource.BaseValueSource is BaseValueSource.Style || SelectedBackground is null)
            {
                SelectedBackground = new BrushConverter().ConvertFromString("#3C8527") as Brush;
            }
            var unSelectedbackgroundSource = DependencyPropertyHelper.GetValueSource(this, UnSelectedBackgroundProperty);
            if (unSelectedbackgroundSource.BaseValueSource is BaseValueSource.DefaultStyle || unSelectedbackgroundSource.BaseValueSource is BaseValueSource.Style || UnSelectedBackground is null)
            {
                UnSelectedBackground = new BrushConverter().ConvertFromString("#8C8D90") as Brush;
            }
            var thumbBackgroundSource = DependencyPropertyHelper.GetValueSource(this, ThumbBackgroundProperty);
            if (thumbBackgroundSource.BaseValueSource is BaseValueSource.DefaultStyle || thumbBackgroundSource.BaseValueSource is BaseValueSource.Style || ThumbBackground is null)
            {
                ThumbBackground = OriginThumbBackground = new BrushConverter().ConvertFromString("#D0D1D4") as Brush;
            }
            var borderBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderBrushProperty);
            if (borderBrushSource.BaseValueSource is BaseValueSource.DefaultStyle || borderBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                BorderBrush = Brushes.Black;
            }

            var borderCornerBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderCornerBrushProperty);
            if (borderCornerBorderBrushSource.BaseValueSource is BaseValueSource.DefaultStyle || borderCornerBorderBrushSource.BaseValueSource is BaseValueSource.Style || borderCornerBorderBrushSource.BaseValueSource is BaseValueSource.Default || BorderCornerBrush is null)
            {
                SolidColorBrush solidColorBrush = ThumbBackground as SolidColorBrush;
                Color color = ColorTool.Lighten(solidColorBrush.Color, 0.4f);
                BorderCornerBrush = OriginBorderCornerBrush = new SolidColorBrush(color);
            }

            var originleftBorderCornerBrushSource = DependencyPropertyHelper.GetValueSource(this, LeftSideSelectedBorderCornerBrushProperty);
            if (originleftBorderCornerBrushSource.BaseValueSource is BaseValueSource.Default || originleftBorderCornerBrushSource.BaseValueSource is BaseValueSource.Style || LeftSideSelectedBorderCornerBrush is null)
            {
                SolidColorBrush solidColorBrush = SelectedBackground as SolidColorBrush;
                Color color = ColorTool.Lighten(solidColorBrush.Color, 0.15f);
                LeftSideSelectedBorderCornerBrush = OriginLeftSideBorderCornerBrush = new SolidColorBrush(color);
            }
            var originLeftSideBottomBrushSource = DependencyPropertyHelper.GetValueSource(this, LeftSideBottomSelectedBorderBrushProperty);
            if (originLeftSideBottomBrushSource.BaseValueSource is BaseValueSource.Default || originLeftSideBottomBrushSource.BaseValueSource is BaseValueSource.Style || LeftSideBottomSelectedBorderBrush is null)
            {
                Color color = ColorTool.Darken((SelectedBackground as SolidColorBrush).Color, 0.1f);
                LeftSideBottomSelectedBorderBrush = OriginLeftSideBottomBorderBrush = new SolidColorBrush(color);
            }
            var originLeftTopSelectedBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, LeftTopSelectedBorderBrushProperty);
            if (originLeftTopSelectedBorderBrushSource.BaseValueSource is BaseValueSource.Default || originLeftTopSelectedBorderBrushSource.BaseValueSource is BaseValueSource.Style || LeftTopSelectedBorderBrush is null)
            {
                SolidColorBrush solidBorderBrush = SelectedBackground as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.1f);
                LeftTopSelectedBorderBrush = OriginLeftTopSelectedBorderBrush = new SolidColorBrush(color);
            }

            var originRightTopSelectedBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, RightTopUnSelectedBorderBrushProperty);
            if (originRightTopSelectedBorderBrushSource.BaseValueSource is BaseValueSource.Default || originRightTopSelectedBorderBrushSource.BaseValueSource is BaseValueSource.Style || RightTopUnSelectedBorderBrush is null)
            {
                SolidColorBrush solidBorderBrush = UnSelectedBackground as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.1f);
                RightTopUnSelectedBorderBrush = OriginRightTopUnSelectedBorderBrush = new SolidColorBrush(color);
            }
            var originRightBottomSelectedBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, RightSideBottomUnSelectedBorderBrushProperty);
            if (originRightBottomSelectedBorderBrushSource.BaseValueSource is BaseValueSource.Default || originRightBottomSelectedBorderBrushSource.BaseValueSource is BaseValueSource.Style || RightSideBottomUnSelectedBorderBrush is null)
            {
                SolidColorBrush solidBorderBrush = UnSelectedBackground as SolidColorBrush;
                Color color = ColorTool.Darken(solidBorderBrush.Color, 0.1f);
                RightSideBottomUnSelectedBorderBrush = OriginRightSideBottomBorderBrush = new SolidColorBrush(color);
            }
            var originRightBorderCornerBrushSource = DependencyPropertyHelper.GetValueSource(this, RightSideUnSelectedBorderCornerBrushProperty);
            if (originRightBorderCornerBrushSource.BaseValueSource is BaseValueSource.Default || originRightBorderCornerBrushSource.BaseValueSource is BaseValueSource.Style || RightSideUnSelectedBorderCornerBrush is null)
            {
                SolidColorBrush solidBorderBrush = UnSelectedBackground as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.15f);
                RightSideUnSelectedBorderCornerBrush = OriginRightSideBorderCornerBrush = new SolidColorBrush(color);
            }
            var originLeftTopBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, LeftTopBorderBrushProperty);
            if (originLeftTopBorderBrushSource.BaseValueSource is BaseValueSource.Default || originLeftTopBorderBrushSource.BaseValueSource is BaseValueSource.Style || LeftTopBorderBrush is null)
            {
                SolidColorBrush solidBorderBrush = ThumbBackground as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.2f);
                LeftTopBorderBrush = OriginLeftTopBorderBrush = new SolidColorBrush(color);
            }
            var originRightBottomBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, RightBottomBorderBrushProperty);
            if (originRightBottomBorderBrushSource.BaseValueSource is BaseValueSource.Default || originRightBottomBorderBrushSource.BaseValueSource is BaseValueSource.Style || LeftTopBorderBrush is null)
            {
                SolidColorBrush solidBorderBrush = ThumbBackground as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.3f);
                RightBottomBorderBrush = OriginRightBottomBorderBrush = new SolidColorBrush(color);
            }
            var originBottomBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, BottomBorderBrushProperty);
            if (originBottomBorderBrushSource.BaseValueSource is BaseValueSource.Default || originBottomBorderBrushSource.BaseValueSource is BaseValueSource.Style || BottomBorderBrush is null)
            {
                SolidColorBrush solidBorderBrush = ThumbBackground as SolidColorBrush;
                Color color = ColorTool.Darken(solidBorderBrush.Color, 0.6f);
                BottomBorderBrush = OriginBottomBorderBrush = new SolidColorBrush(color);
            }
        }
        #endregion

        #region Event
        private void VectorToggleIconTextButton_Loaded(object sender, EventArgs e)
        {
            Text ??= "";

            if (OriginBottomHeight == 0)
            {
                OriginBottomHeight = 4;
            }

            if(BackgroundWidth == 0)
            {
                BackgroundWidth = 70;
            }
            if (BackgroundHeight == 0)
            {
                BackgroundHeight = 35;
            }
            if(ThumbWidth == 0)
            {
                ThumbWidth = BackgroundWidth / 2 + 2;
            }
            if(ThumbHeight == 0)
            {
                ThumbHeight = BackgroundHeight + 5;
            }
            OriginThumbMargin = ThumbMargin = new(0, -5, 0, 0);

            UpdateBorderColorByBackgroundColor(sender);
        }

        private void VectorToggleIconTextButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            VectorToggleIconTextButton_MouseEnter(sender, null);
        }

        private void VectorToggleIconTextButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Color thumbColor = ColorTool.Darken((OriginThumbBackground as SolidColorBrush).Color, 0.4f);
            ThumbBackground = new SolidColorBrush(thumbColor);
        }

        private void VectorToggleIconTextButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ThumbBackground = OriginThumbBackground;
            LeftTopBorderBrush = OriginLeftTopBorderBrush;
            RightBottomBorderBrush = OriginRightBottomBorderBrush;
        }

        private void VectorToggleIconTextButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Color darkColor = ColorTool.Darken((OriginThumbBackground as SolidColorBrush).Color, 0.2f);
            ThumbBackground = new SolidColorBrush(darkColor);
            Color leftTopColor = ColorTool.Lighten((OriginLeftTopBorderBrush as SolidColorBrush).Color, 0.3f);
            LeftTopBorderBrush = new SolidColorBrush(leftTopColor);
            Color rightBottomColor = ColorTool.Lighten((OriginRightBottomBorderBrush as SolidColorBrush).Color, 0.4f);
            RightBottomBorderBrush = new SolidColorBrush(rightBottomColor);
        }

        private void VectorToggleIconTextButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsChecked is bool value && value)
            {
                VectorToggleIconTextButton_Checked(sender, null);
            }
            else
            {
                VectorToggleIconTextButton_Unchecked(sender,null);
            }
        }

        private void VectorToggleIconTextButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (IsChecked is not null)
            {
                ThumbMargin = OriginThumbMargin;
            }
        }

        private void VectorToggleIconTextButton_Checked(object sender, RoutedEventArgs e)
        {
            if (IsChecked is not null)
            {
                ThumbMargin = new(BackgroundWidth / 2, -5, 0, 0);
            }
        }
        #endregion
    }
}
