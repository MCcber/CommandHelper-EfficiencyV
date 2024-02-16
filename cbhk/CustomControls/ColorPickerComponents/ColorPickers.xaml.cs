using cbhk.CustomControls.ColorPickerComponents.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace cbhk.CustomControls.ColorPickerComponents
{
    /// <summary>
    /// ColorPickers.xaml 的交互逻辑
    /// </summary>
    public partial class ColorPickers : UserControl
    {
        private bool isPresetColorMode = false;

        public bool IsPresetColorMode
        {
            get => isPresetColorMode;
            set
            {
                isPresetColorMode = value;
                if(IsPresetColorMode)
                {
                    SelectPoint.Visibility = CustomColorGrid.Visibility = Visibility.Collapsed;
                    PresetColorGrid.Visibility = Visibility.Visible;
                }
                else
                {
                    SelectPoint.Visibility = CustomColorGrid.Visibility = Visibility.Visible;
                    PresetColorGrid.Visibility = Visibility.Collapsed;
                }
            }
        }

        public static Dictionary<Color, string> PresetColorList = new() { { Colors.Black, @"\\u00a70" }, { Colors.DarkBlue, @"\\u00a71" }, { Colors.DarkGreen, @"\\u00a72" },{ Colors.Cyan,@"\\u00a73" }, { Colors.DarkRed, @"\\u00a74" }, { Colors.Purple, @"\\u00a75" }, { Colors.Gold, @"\\u00a76" }, { Colors.LightGray, @"\\u00a77" }, { Colors.DarkGray, @"\\u00a78" }, { Colors.Blue, @"\\u00a79" }, { Colors.LightGreen, @"\\u00a7a" }, { Colors.LightBlue, @"\\u00a7b" }, { Colors.Red, @"\\u00a7c" }, { Colors.Pink, @"\\u00a7d" }, { Colors.Yellow, @"\\u00a7e" }, { Colors.White, @"\\u00a7f" } };

        public ColorPickers()
        {
            InitializeComponent();

            Loaded += ColorPickers_Loaded;
        }

        private void ColorPickers_Loaded(object sender, RoutedEventArgs e)
        {
            int rowIndex = 0;
            int columnIndex = -1;
            foreach (var item in PresetColorList)
            {
                Border border = new()
                {
                    Background = new SolidColorBrush(item.Key),
                    CornerRadius = new(4),
                    Width = 20,
                    Height = 20,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                border.MouseLeftButtonUp += Border_MouseLeftButtonUp;
                PresetColorGrid.Children.Add(border);
                columnIndex++;
                if (columnIndex > 4)
                {
                    columnIndex = 0;
                    rowIndex++;
                }
                Grid.SetColumn(border, columnIndex);
                Grid.SetRow(border, rowIndex);
            }
        }

        public string ColorKey
        {
            get { return (string)GetValue(ColorKeyProperty); }
            set { SetValue(ColorKeyProperty, value); }
        }

        public static readonly DependencyProperty ColorKeyProperty =
            DependencyProperty.Register("ColorKey", typeof(string), typeof(ColorPickers), new PropertyMetadata(default(string)));

        private void Border_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Border border = sender as Border;
            Color color = (border.Background as SolidColorBrush).Color;
            ColorKey = PresetColorList[color];
            SelectColor = border.Background as SolidColorBrush;
            pop.IsOpen = false;
        }

        double H = 0;
        double S = 1;
        double B = 1;

        private void ThumbPro_ValueChanged(double xpercent, double ypercent)
        {
            H = 360 * ypercent;
            HsbaColor Hcolor = new HsbaColor(H, 1, 1, 1);
            viewSelectColor.Fill = Hcolor.SolidColorBrush;

            Hcolor = new HsbaColor(H, S, B, 1);
            SelectColor = Hcolor.SolidColorBrush;

            ColorChange(Hcolor.RgbaColor);
        }

        private void ThumbPro_ValueChanged_1(double xpercent, double ypercent)
        {
            S = xpercent;
            B = 1 - ypercent;
            HsbaColor Hcolor = new HsbaColor(H, S, B, 1);

            SelectColor = Hcolor.SolidColorBrush;

            ColorChange(Hcolor.RgbaColor);
        }

        SolidColorBrush _SelectColor = Brushes.Transparent;

        //public SolidColorBrush SelectColor
        //{
        //    get
        //    {
        //        return _SelectColor;
        //        return (SolidColorBrush)GetValue(SelectColorProperty);
        //    }
        //    set
        //    {
        //        _SelectColor = value;
        //        SetValue(SelectColorProperty, value);
        //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectColor"));
        //        Background = SelectColor;
        //    }
        //    get { return (SolidColorBrush)GetValue(SelectColorProperty); }
        //    set { SetValue(SelectColorProperty, value); }
        //}

        public static readonly DependencyProperty SelectColorProperty =
            DependencyProperty.Register("SelectColor", typeof(SolidColorBrush), typeof(ColorPickers), new PropertyMetadata(default(SolidColorBrush)));


        public SolidColorBrush SelectColor
        {
            get
            {
                _SelectColor = (SolidColorBrush)GetValue(SelectColorProperty);
                return _SelectColor;
            }
            set
            {
                _SelectColor = value;
                SetValue(SelectColorProperty, value);
                Background = SelectColor;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectColor)));
            }
        }


        int R = 255;
        int G = 255;
        int _B = 255;
        int A = 255;

        public event PropertyChangedEventHandler PropertyChanged;

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            string text = textBox.Text;

            //错误的数据，则使用上次的正确数据
            if (!int.TryParse(TextR.Text, out int Rvalue) || (Rvalue > 255 || Rvalue < 0))
            {
                TextR.Text = R.ToString();
                return;
            }

            if (!int.TryParse(TextG.Text, out int Gvalue) || (Gvalue > 255 || Gvalue < 0))
            {
                TextG.Text = G.ToString();
                return;
            }

            if (!int.TryParse(TextB.Text, out int Bvalue) || (Bvalue > 255 || Bvalue < 0))
            {
                TextB.Text = _B.ToString();
                return;
            }
            if (!int.TryParse(TextA.Text, out int Avalue) || (Avalue > 255 || Avalue < 0))
            {
                TextA.Text = A.ToString();
                return;
            }
            R = Rvalue; G = Gvalue; _B = Bvalue; A = Avalue;


            RgbaColor Hcolor = new RgbaColor(R, G, _B,A);
            SelectColor = Hcolor.SolidColorBrush;

            TextHex.Text = Hcolor.HexString;

        }

        private void HexTextLostFocus(object sender, RoutedEventArgs e)
        {

            RgbaColor Hcolor = new RgbaColor(TextHex.Text);

            SelectColor = Hcolor.SolidColorBrush;
            TextR.Text = Hcolor.R.ToString();
            TextG.Text = Hcolor.G.ToString();
            TextB.Text = Hcolor.B.ToString();
            TextA.Text = Hcolor.A.ToString();
        }

        private void ColorChange(Media.RgbaColor Hcolor)
        {
            TextR.Text = Hcolor.R.ToString();
            TextG.Text = Hcolor.G.ToString();
            TextB.Text = Hcolor.B.ToString();
            TextA.Text = Hcolor.A.ToString();

            TextHex.Text = Hcolor.HexString;
        }

        private void btn_Click(object sender, RoutedEventArgs e)
        {
            pop.IsOpen = true;
            //SelectColor = btn.Background as SolidColorBrush;
            RgbaColor Hcolor = new RgbaColor(SelectColor);
            ColorChange(Hcolor);

            var xpercent = Hcolor.HsbaColor.S;
            var ypercent = 1 - Hcolor.HsbaColor.B;

            var Ypercent = Hcolor.HsbaColor.H / 360;

            thumbH.SetTopLeftByPercent(1, Ypercent);
            thumbSB.SetTopLeftByPercent(xpercent, ypercent);


        }
    }

    /// <summary>
    /// 封装Canvas 到Thumb来简化 Thumb的使用，关注熟悉X,Y 表示 thumb在坐标中距离左，上的距离
    /// 默认canvas 里用一个小圆点来表示当前位置
    /// </summary>
    public class ThumbPro : Thumb
    {
        //距离Canvas的Top,模板中需要Canvas.Top 绑定此Top
        public double Top
        {
            get { return (double)GetValue(TopProperty); }
            set { SetValue(TopProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Top.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TopProperty =
            DependencyProperty.Register("Top", typeof(double), typeof(ThumbPro), new PropertyMetadata(0.0));


        //距离Canvas的Top,模板中需要Canvas.Left 绑定此Left
        public double Left
        {
            get { return (double)GetValue(LeftProperty); }
            set { SetValue(LeftProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Left.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LeftProperty =
            DependencyProperty.Register("Left", typeof(double), typeof(ThumbPro), new PropertyMetadata(0.0));

        double FirstTop;
        double FirstLeft;

        //小圆点的半径
        public double Xoffset { get; set; }
        public double Yoffset { get; set; }

        public bool VerticalOnly { get; set; } = false;

        public double Xpercent { get { return (Left + Xoffset) / ActualWidth; } }
        public double Ypercent { get { return (Top + Yoffset) / ActualHeight; } }

        public void SetTopLeftByPercent(double xpercent, double ypercent)
        {
            Top = ypercent * ActualHeight - Yoffset;
            if (!VerticalOnly)
                Left = xpercent * ActualWidth - Xoffset;
        }

        public event Action<double, double> ValueChanged;

        public ThumbPro()
        {
            Loaded += (object sender, RoutedEventArgs e) => {
                if (!VerticalOnly)
                    Left = -Xoffset;
                Top = -Yoffset;


            };
            DragStarted += (object sender, DragStartedEventArgs e) =>
            {
                //当随便点击某点，把小远点移到当前位置，注意是小远点的中心位置移到当前位置
                if (!VerticalOnly)
                {
                    Left = e.HorizontalOffset - Xoffset;
                    FirstLeft = Left;
                }
                Top = e.VerticalOffset - Yoffset;
                FirstTop = Top;

                ValueChanged?.Invoke(Xpercent, Ypercent);
            };

            DragDelta += (object sender, DragDeltaEventArgs e) =>
            {
                //按住拖拽时，小圆点随着鼠标移动
                if (!VerticalOnly)
                {
                    double x = FirstLeft + e.HorizontalChange;

                    if (x < -Xoffset) Left = -Xoffset;
                    else if (x > ActualWidth - Xoffset) Left = ActualWidth - Xoffset;
                    else Left = x;
                }




                double y = FirstTop + e.VerticalChange;

                if (y < -Yoffset) Top = -Yoffset;
                else if (y > ActualHeight - Yoffset) Top = ActualHeight - Yoffset;
                else Top = y;
                ValueChanged?.Invoke(Xpercent, Ypercent);
            };
        }
    }
}