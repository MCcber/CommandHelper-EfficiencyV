using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace CBHK.CustomControl.Input
{
    public class VectorRichTextBox : RichTextBox
    {
        #region Property
        public Run CurrentMouseHoverRun { get; private set; }

        public string WaterMarkerText
        {
            get { return (string)GetValue(WaterMarkerTextProperty); }
            set { SetValue(WaterMarkerTextProperty, value); }
        }

        public static readonly DependencyProperty WaterMarkerTextProperty =
            DependencyProperty.Register("WaterMarkerText", typeof(string), typeof(VectorRichTextBox), new PropertyMetadata(default(string)));

        public Brush WaterMarkerBrush
        {
            get { return (Brush)GetValue(WaterMarkerBrushProperty); }
            set { SetValue(WaterMarkerBrushProperty, value); }
        }

        public static readonly DependencyProperty WaterMarkerBrushProperty =
            DependencyProperty.Register("WaterMarkerBrush", typeof(Brush), typeof(VectorRichTextBox), new PropertyMetadata(default(Brush)));

        public Brush LocateLineBrush
        {
            get { return (Brush)GetValue(LocateLineBrushProperty); }
            set { SetValue(LocateLineBrushProperty, value); }
        }

        public static readonly DependencyProperty LocateLineBrushProperty =
            DependencyProperty.Register("LocateLineBrush", typeof(Brush), typeof(VectorRichTextBox), new PropertyMetadata(default(Brush)));

        public Thickness WaterMarkerMargin
        {
            get { return (Thickness)GetValue(WaterMarkerMarginProperty); }
            set { SetValue(WaterMarkerMarginProperty, value); }
        }

        public static readonly DependencyProperty WaterMarkerMarginProperty =
            DependencyProperty.Register("WaterMarkerMargin", typeof(Thickness), typeof(VectorRichTextBox), new PropertyMetadata(default(Thickness)));

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(VectorRichTextBox), new PropertyMetadata(default(Brush)));

        public bool HasText => (bool)GetValue(HasTextPropertyKey.DependencyProperty);

        private static readonly DependencyPropertyKey HasTextPropertyKey =
            DependencyProperty.RegisterReadOnly("HasText", typeof(bool), typeof(VectorRichTextBox), new PropertyMetadata(false));
        #endregion

        #region Method
        public VectorRichTextBox()
        {
            SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
            SetResourceReference(ForegroundProperty, Theme.CommonForeground);
            WaterMarkerBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D6D6D6"));
            LocateLineBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#242425"));
            Loaded += VectorRichTextBox_Loaded;
            MouseMove += VectorRichTextBox_MouseMove;
        }

        private void VectorRichTextBox_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Point mousePoint = e.GetPosition(this);
            var element = InputHitTest(mousePoint);
            if (element is Run run)
            {
                CurrentMouseHoverRun = run;
            }
        }

        private void UpdateBorderColorByBackgroundColor()
        {
            if(ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.1f));
                BorderBrush = LocateLineBrush = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.2f));
                WaterMarkerBrush = ColorTool.GetWatermarkBrush(themeBrush);
            }
        }

        public bool IsEmpty()
        {
            var blocks = Document.Blocks;

            if (blocks.Count == 0) return true;

            if (blocks.FirstBlock is Paragraph paragraph)
            {
                if (paragraph.Inlines.Count == 0) return true;
                if (paragraph.Inlines.Count == 1 && paragraph.Inlines.FirstInline is Run run)
                {
                    return string.IsNullOrWhiteSpace(run.Text);
                }
            }
            return false;
        }
        #endregion

        #region Event
        private void VectorRichTextBox_Loaded(object sender, RoutedEventArgs e)
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

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            SetValue(HasTextPropertyKey, !IsEmpty());
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
