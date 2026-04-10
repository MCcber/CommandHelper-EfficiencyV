using CBHK.Model.Constant;
using CBHK.Utility.Common;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorTextButton : Button
    {
        #region Property
        public double OriginBottomHeight { get; set; }
        private Thickness OriginMargin { get; set; }
        private Brush OriginTopBorderBrush { get; set; }
        private Brush OriginBorderCornerBrush { get; set; }
        private Brush OriginBackgroundBrush { get; set; }

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(VectorTextButton), new PropertyMetadata(default(Brush)));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(VectorTextButton), new PropertyMetadata(default(string)));

        public Brush RoundBorderBrush
        {
            get { return (Brush)GetValue(RoundBorderBrushProperty); }
            set { SetValue(RoundBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RoundBorderBrushProperty =
            DependencyProperty.Register("RoundBorderBrush", typeof(Brush), typeof(VectorTextButton), new PropertyMetadata(default(Brush)));

        public Brush TopBorderBrush
        {
            get { return (Brush)GetValue(TopBorderBrushProperty); }
            set { SetValue(TopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty TopBorderBrushProperty =
            DependencyProperty.Register("TopBorderBrush", typeof(Brush), typeof(VectorTextButton), new PropertyMetadata(default(Brush)));

        public Brush BorderCornerBrush
        {
            get { return (Brush)GetValue(BorderCornerBrushProperty); }
            set { SetValue(BorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(VectorTextButton), new PropertyMetadata(default(Brush)));

        public Brush BottomBorderBrush
        {
            get { return (Brush)GetValue(BottomBorderBrushProperty); }
            set { SetValue(BottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty BottomBorderBrushProperty =
            DependencyProperty.Register("BottomBorderBrush", typeof(Brush), typeof(VectorTextButton), new PropertyMetadata(default(Brush)));

        public TextDecorationCollection TextDecoration
        {
            get { return (TextDecorationCollection)GetValue(TextDecorationProperty); }
            set { SetValue(TextDecorationProperty, value); }
        }

        public static readonly DependencyProperty TextDecorationProperty =
            DependencyProperty.Register("TextDecoration", typeof(TextDecorationCollection), typeof(VectorTextButton), new PropertyMetadata(default(TextDecorationCollection)));
        #endregion

        #region Method
        public VectorTextButton()
        {
            RoundBorderBrush = Brushes.Black;
            Initialized += VectorTextButton_Initalized;
            MouseEnter += VectorTextButton_MouseEnter;
            MouseLeave += VectorTextButton_MouseLeave;
            PreviewMouseLeftButtonDown += VectorTextButton_PreviewMouseLeftButtonDown;
            PreviewMouseLeftButtonUp += VectorTextButton_PreviewMouseLeftButtonUp;
        }

        private void UpdateBorderColorByBackgroundColor()
        {
            BorderBrush = Brushes.Black;

            Color backgroundColor = ColorTool.Lighten((Background as SolidColorBrush).Color, 0.4f);
            BorderCornerBrush = OriginBorderCornerBrush = new SolidColorBrush(backgroundColor);

            Color topBorderColor = ColorTool.Lighten((Background as SolidColorBrush).Color, 0.2f);
            TopBorderBrush = OriginTopBorderBrush = new SolidColorBrush(topBorderColor);

            Color bottomBordercolor = ColorTool.Darken((Background as SolidColorBrush).Color, 0.5f);
            BottomBorderBrush = new SolidColorBrush(bottomBordercolor);
        }
        #endregion

        #region Event

        private void VectorTextButton_Initalized(object sender, EventArgs e)
        {
            ApplyTemplate();
            OriginMargin = Margin;

            if (Text == "")
            {
                Text = "Button";
            }

            if (OriginBottomHeight == 0)
            {
                OriginBottomHeight = 6;
            }

            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(OriginBottomHeight, GridUnitType.Pixel);
            }

            SetResourceReference(ThemeBackgroundProperty, Theme.TextButtonBackground);
            SetResourceReference(ForegroundProperty, Theme.TextBlockForeground);
            UpdateBorderColorByBackgroundColor();

            if (Background is SolidColorBrush backgroundBrush)
            {
                OriginBackgroundBrush = new SolidColorBrush(backgroundBrush.Color);
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == ThemeBackgroundProperty)
            {
                if (ThemeBackground is SolidColorBrush brush)
                {
                    Background = new SolidColorBrush(brush.Color);
                    OriginBackgroundBrush = new SolidColorBrush(brush.Color);
                    UpdateBorderColorByBackgroundColor();
                }
            }
        }

        private void VectorTextButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            VectorTextButton_MouseEnter(sender, null);
        }

        private void VectorTextButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);
            Color color = ColorTool.Darken((OriginBackgroundBrush as SolidColorBrush).Color, 0.4f);
            Background = new SolidColorBrush(color);
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(0, GridUnitType.Pixel);
            }
            Margin = new(Margin.Left, Margin.Top + 2, Margin.Right, Margin.Bottom);
        }

        private void VectorTextButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);
            Background = OriginBackgroundBrush;
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(OriginBottomHeight, GridUnitType.Pixel);
            }
            TopBorderBrush = OriginTopBorderBrush;
            BorderCornerBrush = OriginBorderCornerBrush;
            Margin = OriginMargin;
        }

        private void VectorTextButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            object extraBottomLine = Template.FindName("extraBottomLine", sender as FrameworkElement);

            Color darkColor = ColorTool.Darken((OriginBackgroundBrush as SolidColorBrush).Color, 0.2f);
            Background = new SolidColorBrush(darkColor);
            if (extraBottomLine is RowDefinition row)
            {
                row.Height = new(OriginBottomHeight, GridUnitType.Pixel);
            }
            Margin = OriginMargin;
            Color lightBorderColor = ColorTool.Lighten((OriginTopBorderBrush as SolidColorBrush).Color, 0.4f);
            TopBorderBrush = new SolidColorBrush(lightBorderColor);
            Color lightCornerColor = ColorTool.Lighten((OriginBorderCornerBrush as SolidColorBrush).Color, 0.6f);
            BorderCornerBrush = new SolidColorBrush(lightCornerColor);
        }
        #endregion
    }
}