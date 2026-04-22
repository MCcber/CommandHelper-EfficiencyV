using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorTextButton : BaseVectorTextButton
    {
        #region Field
        private Thickness OriginMargin;
        private Brush OriginRoundBorderBrush;
        private Brush OriginBorderCornerBrush;
        #endregion

        #region Property
        public double OriginBottomHeight { get; set; }

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
            if (!IsManualBackground)
            {
                SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
                SetResourceReference(ForegroundProperty, Theme.CommonForeground);
            }

            MouseEnter += VectorTextButton_MouseEnter;
            MouseLeave += VectorTextButton_MouseLeave;
            PreviewMouseLeftButtonDown += VectorTextButton_PreviewMouseLeftButtonDown;
            PreviewMouseLeftButtonUp += VectorTextButton_PreviewMouseLeftButtonUp;
        }

        public override void UpdateBorderColorByBackgroundColor()
        {
            base.UpdateBorderColorByBackgroundColor();
            OriginBackground = Background;
            SolidColorBrush targetBrush = !IsManualBackground ? ThemeBackground as SolidColorBrush : Background as SolidColorBrush;
            if (targetBrush is not null)
            {
                Foreground = new SolidColorBrush(ColorTool.GetOptimalForeground(targetBrush.Color));
                BorderCornerBrush = OriginBorderCornerBrush = new SolidColorBrush(ColorTool.Lighten(targetBrush.Color, 0.4f));
                RoundBorderBrush = OriginRoundBorderBrush = new SolidColorBrush(ColorTool.Lighten(targetBrush.Color, 0.2f));
                BottomBorderBrush = new SolidColorBrush(ColorTool.Darken(targetBrush.Color, 0.5f));
            }
        }
        #endregion

        #region Event
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            OriginMargin = Margin;

            if (Text == "")
            {
                Text = "Button";
            }

            if (OriginBottomHeight == 0)
            {
                OriginBottomHeight = 6;
            }

            if(GetTemplateChild("extraBottomLine") is RowDefinition rowDefinition)
            {
                rowDefinition.Height = new(OriginBottomHeight, GridUnitType.Pixel);
            }

            UpdateBorderColorByBackgroundColor();
        }

        private void VectorTextButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            VectorTextButton_MouseEnter(sender, null);
        }

        private void VectorTextButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ThemeBackground is SolidColorBrush solidColorBrush && !IsManualBackground)
            {
                Background = new SolidColorBrush(ColorTool.Darken(solidColorBrush.Color, 0.4f));
            }
            if (OriginBackground is SolidColorBrush originBackground)
            {
                Background = new SolidColorBrush(ColorTool.Darken(originBackground.Color, 0.4f));
            }
            if (GetTemplateChild("extraBottomLine") is RowDefinition rowDefinition)
            {
                rowDefinition.Height = new(0, GridUnitType.Pixel);
            }
            Margin = new(Margin.Left, Margin.Top + 2, Margin.Right, Margin.Bottom);
        }

        private void VectorTextButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (ThemeBackground is SolidColorBrush solidColorBrush && !IsManualBackground)
            {
                Background = new SolidColorBrush(solidColorBrush.Color);
            }
            else
            {
                Background = OriginBackground;
            }
            if (GetTemplateChild("extraBottomLine") is RowDefinition rowDefinition)
            {
                rowDefinition.Height = new(OriginBottomHeight, GridUnitType.Pixel);
            }
            RoundBorderBrush = OriginRoundBorderBrush;
            BorderCornerBrush = OriginBorderCornerBrush;
            Margin = OriginMargin;
        }

        private void VectorTextButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (ThemeBackground is SolidColorBrush solidColorBrush && !IsManualBackground)
            {
                Background = new SolidColorBrush(ColorTool.Darken(solidColorBrush.Color, 0.2f));
            }
            else
            if (OriginBackground is SolidColorBrush originBackground)
            {
                Background = new SolidColorBrush(ColorTool.Darken(originBackground.Color, 0.2f));
            }
            if (GetTemplateChild("extraBottomLine") is RowDefinition rowDefinition)
            {
                rowDefinition.Height = new(OriginBottomHeight, GridUnitType.Pixel);
            }
            if(OriginRoundBorderBrush is SolidColorBrush originRoundBorderBrush)
            {
                RoundBorderBrush = new SolidColorBrush(ColorTool.Lighten(originRoundBorderBrush.Color, 0.4f));
            }
            if (OriginBorderCornerBrush is SolidColorBrush originBorderCornerBrush)
            {
                BorderCornerBrush = new SolidColorBrush(ColorTool.Lighten(originBorderCornerBrush.Color, 0.6f));
            }
            Margin = OriginMargin;
        }
        #endregion
    }
}