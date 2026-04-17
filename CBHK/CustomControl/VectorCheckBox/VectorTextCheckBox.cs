using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorCheckBox
{
    public class VectorTextCheckBox : CheckBox
    {
        #region Field
        private readonly Brush OriginInnerBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B8BEBA"));
        private readonly Brush OriginSelectedInnerBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
        #endregion

        #region Property
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(VectorTextCheckBox), new PropertyMetadata(default(string)));

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(VectorTextCheckBox), new PropertyMetadata(default(Brush)));

        public Brush InnerBorderBrush
        {
            get { return (Brush)GetValue(InnerBorderBrushProperty); }
            set { SetValue(InnerBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty InnerBorderBrushProperty =
            DependencyProperty.Register("InnerBorderBrush", typeof(Brush), typeof(VectorTextCheckBox), new PropertyMetadata(default(Brush)));

        public Brush InnerLeftTopBackground
        {
            get { return (Brush)GetValue(InnerLeftTopBackgroundProperty); }
            set { SetValue(InnerLeftTopBackgroundProperty, value); }
        }

        public static readonly DependencyProperty InnerLeftTopBackgroundProperty =
            DependencyProperty.Register("InnerLeftTopBackground", typeof(Brush), typeof(VectorTextCheckBox), new PropertyMetadata(default(Brush)));

        public Brush InnerRightTopBackground
        {
            get { return (Brush)GetValue(InnerRightTopBackgroundProperty); }
            set { SetValue(InnerRightTopBackgroundProperty, value); }
        }

        public static readonly DependencyProperty InnerRightTopBackgroundProperty =
            DependencyProperty.Register("InnerRightTopBackground", typeof(Brush), typeof(VectorTextCheckBox), new PropertyMetadata(default(Brush)));

        public Brush InnerLeftBottomBackground
        {
            get { return (Brush)GetValue(InnerLeftBottomBackgroundProperty); }
            set { SetValue(InnerLeftBottomBackgroundProperty, value); }
        }

        public static readonly DependencyProperty InnerLeftBottomBackgroundProperty =
            DependencyProperty.Register("InnerLeftBottomBackground", typeof(Brush), typeof(VectorTextCheckBox), new PropertyMetadata(default(Brush)));

        public Brush InnerRightBottomBackground
        {
            get { return (Brush)GetValue(InnerRightBottomBackgroundProperty); }
            set { SetValue(InnerRightBottomBackgroundProperty, value); }
        }

        public static readonly DependencyProperty InnerRightBottomBackgroundProperty =
            DependencyProperty.Register("InnerRightBottomBackground", typeof(Brush), typeof(VectorTextCheckBox), new PropertyMetadata(default(Brush)));

        public Brush LeftTopBorderBrush
        {
            get { return (Brush)GetValue(LeftTopBorderBrushProperty); }
            set { SetValue(LeftTopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftTopBorderBrushProperty =
            DependencyProperty.Register("LeftTopBorderBrush", typeof(Brush), typeof(VectorTextCheckBox), new PropertyMetadata(default(Brush)));

        public Brush RightBottomBorderBrush
        {
            get { return (Brush)GetValue(RightBottomBorderBrushProperty); }
            set { SetValue(RightBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RightBottomBorderBrushProperty =
            DependencyProperty.Register("RightBottomBorderBrush", typeof(Brush), typeof(VectorTextCheckBox), new PropertyMetadata(default(Brush)));

        public Brush BorderCornerBrush
        {
            get { return (Brush)GetValue(BorderCornerBrushProperty); }
            set { SetValue(BorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(VectorTextCheckBox), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorTextCheckBox()
        {
            SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
            Loaded += VectorTextCheckBox_Loaded;
        }

        private void UpdateBorderColorByBackgroundColor()
        {
            bool isChecked = IsChecked is bool value && value;
            SolidColorBrush innerBrush = isChecked ? OriginSelectedInnerBorderBrush as SolidColorBrush : OriginInnerBorderBrush as SolidColorBrush;

            if (ThemeBackground is SolidColorBrush themeBrush)
            {
                if(isChecked)
                {
                    InnerBorderBrush = new SolidColorBrush(themeBrush.Color);
                    BorderCornerBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.4f));
                    LeftTopBorderBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.3f));
                    RightBottomBorderBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.2f));
                }
                else
                if(innerBrush is not null)
                {
                    InnerBorderBrush = new SolidColorBrush(innerBrush.Color);
                    BorderCornerBrush = new SolidColorBrush(ColorTool.Lighten(innerBrush.Color, 0.2f));
                    LeftTopBorderBrush = new SolidColorBrush(ColorTool.Lighten(innerBrush.Color, 0.4f));
                    RightBottomBorderBrush = new SolidColorBrush(ColorTool.Lighten(innerBrush.Color, 0.2f));
                }
            }

            if (innerBrush is not null)
            {
                InnerLeftTopBackground = new SolidColorBrush(ColorTool.Lighten(innerBrush.Color, 0.02f));
                InnerRightTopBackground = new SolidColorBrush(ColorTool.Lighten(innerBrush.Color, 0.05f));
                InnerLeftBottomBackground = new SolidColorBrush(ColorTool.Lighten(innerBrush.Color, 0.05f));
                InnerRightBottomBackground = new SolidColorBrush(ColorTool.Lighten(innerBrush.Color, 0.08f));
            }
        }
        #endregion

        #region Event
        private void VectorTextCheckBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (Text == "")
            {
                Text = "CheckButton";
            }

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

        protected override void OnChecked(RoutedEventArgs e)
        {
            base.OnChecked(e);
            UpdateBorderColorByBackgroundColor();
        }

        protected override void OnUnchecked(RoutedEventArgs e)
        {
            base.OnUnchecked(e);
            UpdateBorderColorByBackgroundColor();
        }
        #endregion
    }
}