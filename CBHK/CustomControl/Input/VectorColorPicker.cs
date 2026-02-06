using CBHK.CustomControl.Container;
using CBHK.CustomControl.VectorButton;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CBHK.CustomControl.Input
{
    public struct HsvColor(double h, double s, double v)
    {
        public double H = h; // 0-360
        public double S = s; // 0-1
        public double V = v; // 0-1

        public readonly Color ToRgb()
        {
            if (double.IsNaN(H) || 
                double.IsInfinity(H) ||
                double.IsNaN(S) || 
                double.IsInfinity(S) ||
                double.IsNaN(V) || 
                double.IsInfinity(V))
            {
                return Colors.Black; // 或者你想要的默认安全色
            }

            int hi = Convert.ToInt32(Math.Floor(H / 60)) % 6;
            double f = H / 60 - Math.Floor(H / 60);
            double value = V * 255;
            byte v = Convert.ToByte(value);
            byte p = Convert.ToByte(value * (1 - S));
            byte q = Convert.ToByte(value * (1 - f * S));
            byte t = Convert.ToByte(value * (1 - (1 - f) * S));

            return hi switch
            {
                0 => Color.FromRgb(v, t, p),
                1 => Color.FromRgb(q, v, p),
                2 => Color.FromRgb(p, v, t),
                3 => Color.FromRgb(p, q, v),
                4 => Color.FromRgb(t, p, v),
                _ => Color.FromRgb(v, p, q),
            };
        }

        public static HsvColor FromRgb(Color color)
        {
            double r = color.R / 255d;
            double g = color.G / 255d;
            double b = color.B / 255d;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double h = 0;
            if (delta != 0)
            {
                if (max == r) h = 60 * (((g - b) / delta) % 6);
                else if (max == g) h = 60 * (((b - r) / delta) + 2);
                else h = 60 * (((r - g) / delta) + 4);
            }
            if (h < 0) h += 360;

            double s = max == 0 ? 0 : delta / max;
            return new HsvColor(h, s, max);
        }
    }

    public class VectorColorPicker : Control
    {
        #region Field
        private double _currentHue = 0;
        private double _currentSat = 1;
        private double _currentVal = 1;
        private Canvas HueCanvas;
        private Thumb PaletteThumb;
        private Rectangle ColorBase;
        private Thumb HueThumb;
        private Grid PaletteArea;
        private VectorFlatIconButton Eyedropper;

        private bool isInternalUpdating = false;
        #endregion

        #region Property
        public Action<Color> CallBack { get; set; }

        // 定义 SelectedColor 依赖属性，方便外部绑定
        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        public static readonly DependencyProperty SelectedColorProperty =
    DependencyProperty.Register("SelectedColor", typeof(Color), typeof(VectorColorPicker),
        new PropertyMetadata(Colors.Red, OnSelectedColorChanged));

        /// <summary>
        /// 内部用于绑定的属性
        /// </summary>
        public SolidColorBrush SelectedColorBrush
        {
            get { return (SolidColorBrush)GetValue(SelectedColorBrushProperty); }
            private set { SetValue(SelectedColorBrushProperty, value); }
        }
        public static readonly DependencyProperty SelectedColorBrushProperty =
            DependencyProperty.Register("SelectedColorBrush", typeof(SolidColorBrush), typeof(VectorColorPicker), new PropertyMetadata(new SolidColorBrush(Colors.Red)));

        public string HexCode
        {
            get { return (string)GetValue(HexCodeProperty); }
            private set { SetValue(HexCodeProperty, value); }
        }

        public static readonly DependencyProperty HexCodeProperty =
            DependencyProperty.Register("HexCode", typeof(string), typeof(VectorColorPicker), new PropertyMetadata("#FFFF0000"));

        public bool IsShowColorPopup
        {
            get { return (bool)GetValue(IsShowColorPopupProperty); }
            set { SetValue(IsShowColorPopupProperty, value); }
        }

        public static readonly DependencyProperty IsShowColorPopupProperty =
            DependencyProperty.Register("IsShowColorPopup", typeof(bool), typeof(VectorColorPicker), new PropertyMetadata(default(bool)));
        #endregion

        #region Method

        private void UpdateVisuals()
        {
            // 确保在控件布局完成后执行，否则 ActualWidth 会是 0
            Loaded += VectorColorPicker_Loaded;
        }

        #region 色相条 (Hue) 逻辑
        private void UpdateHue(double mouseX)
        {
            double width = HueCanvas.ActualWidth;

            if(width <= 0)
            {
                return;
            }

            mouseX = Math.Clamp(mouseX, 0, width);

            // 移动滑块
            Canvas.SetLeft(HueThumb, mouseX - (HueThumb.Width / 2));

            // 计算 Hue (0-360)
            _currentHue = (mouseX / width) * 360;

            // 更新调色板底色
            var hueColor = new HsvColor(_currentHue, 1, 1).ToRgb();
            ColorBase.Fill = new SolidColorBrush(hueColor);

            UpdateFinalColor();
        }
        #endregion

        #region 调色板 (Sat/Val) 逻辑
        private void UpdateSaturationValue(Point mousePos)
        {
            double w = PaletteArea.ActualWidth;
            double h = PaletteArea.ActualHeight;

            if(w <= 0 || h <= 0)
            {
                return;
            }

            double x = Math.Clamp(mousePos.X, 0, w);
            double y = Math.Clamp(mousePos.Y, 0, h);

            // 移动滑块中心到鼠标位置
            Canvas.SetLeft(PaletteThumb, x - (PaletteThumb.ActualWidth / 2));
            Canvas.SetTop(PaletteThumb, y - (PaletteThumb.ActualHeight / 2));

            // 计算 Saturation (X轴: 0 -> 1)
            _currentSat = x / w;

            // 计算 Value (Y轴: 1 -> 0) *注意 Y轴向下是增加，但亮度向下是减小
            _currentVal = 1 - (y / h);

            UpdateFinalColor();
        }

        private void UpdateFinalColor()
        {
            isInternalUpdating = true;

            var color = new HsvColor(_currentHue, _currentSat, _currentVal).ToRgb();
            SelectedColor = color; // 这会触发 OnSelectedColorChanged
            SelectedColorBrush = new SolidColorBrush(color);
            HexCode = color.ToString();

            isInternalUpdating = false;
        }
        #endregion

        /// <summary>
        /// 同步Thumb的位置
        /// </summary>
        /// <param name="color"></param>
        private void SyncPositionsFromColor(Color color)
        {
            // 1. 转换色彩模型
            var hsv = HsvColor.FromRgb(color);
            _currentHue = hsv.H;
            _currentSat = hsv.S;
            _currentVal = hsv.V;

            // 2. 如果控件还没加载好，ActualWidth 是 0，无法计算位置
            if (!IsLoaded) return;

            // 3. 计算并移动色相滑块 (Hue)
            double hueX = (_currentHue / 360) * HueCanvas.ActualWidth;
            Canvas.SetLeft(HueThumb, hueX - (HueThumb.Width / 2));

            // 4. 更新调色板的背景底色
            var hueColor = new HsvColor(_currentHue, 1, 1).ToRgb();
            ColorBase.Fill = new SolidColorBrush(hueColor);

            // 5. 计算并移动调色板中心点 (Saturation & Value)
            double paletteX = _currentSat * PaletteArea.ActualWidth;
            double paletteY = (1 - _currentVal) * PaletteArea.ActualHeight;
            Canvas.SetLeft(PaletteThumb, paletteX - (PaletteThumb.Width / 2));
            Canvas.SetTop(PaletteThumb, paletteY - (PaletteThumb.Height / 2));

            // 6. 更新 Hex 文本和预览色块
            SelectedColorBrush = new SolidColorBrush(color);
            HexCode = color.ToString();
        }

        private void UpdateSenderColor(Color newColor)
        {
            CallBack?.Invoke(newColor);
        }
        #endregion

        #region Event
        private void VectorColorPicker_Loaded(object sender, RoutedEventArgs e)
        {
            LostFocus += VectorColorPicker_LostFocus;
            // 1. 从当前颜色反推 HSV
            var hsv = HsvColor.FromRgb(SelectedColor);
            _currentHue = hsv.H;
            _currentSat = hsv.S;
            _currentVal = hsv.V;

            // 2. 更新色相条滑块位置
            double hueX = (_currentHue / 360) * HueCanvas.ActualWidth;
            Canvas.SetLeft(HueThumb, hueX - (HueThumb.Width / 2));

            // 3. 更新调色板底色
            var hueColor = new HsvColor(_currentHue, 1, 1).ToRgb();
            ColorBase.Fill = new SolidColorBrush(hueColor);

            // 4. 更新调色板滑块位置
            double paletteX = _currentSat * PaletteArea.ActualWidth;
            double paletteY = (1 - _currentVal) * PaletteArea.ActualHeight;
            Canvas.SetLeft(PaletteThumb, paletteX - (PaletteThumb.ActualWidth / 2));
            Canvas.SetTop(PaletteThumb, paletteY - (PaletteThumb.ActualHeight / 2));

            // 5. 更新显示的文本和预览
            UpdateFinalColor();
        }

        private void VectorColorPicker_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!IsKeyboardFocusWithin)
            {
                ReleaseMouseCapture();
                IsShowColorPopup = false;
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Button PickerToggle = GetTemplateChild("PickerToggle") as Button;
            HueCanvas = GetTemplateChild("HueCanvas") as Canvas;
            HueThumb = GetTemplateChild("HueThumb") as Thumb;
            PaletteThumb = GetTemplateChild("PaletteThumb") as Thumb;
            ColorBase = GetTemplateChild("ColorBase") as Rectangle;
            PaletteArea = GetTemplateChild("PaletteArea") as Grid;
            Eyedropper = GetTemplateChild("Eyedropper") as VectorFlatIconButton;

            PickerToggle.Click += ShowColorPopup_Click;

            HueCanvas.MouseLeftButtonDown += Hue_MouseDown;
            HueCanvas.MouseMove += Hue_MouseMove;
            HueCanvas.MouseUp += HueCanvas_MouseUp;

            //HueThumb.DragDelta += HueThumb_DragDelta;
            PaletteThumb.DragDelta += PaletteThumb_DragDelta;

            PaletteArea.MouseLeftButtonDown += Palette_MouseDown;
            PaletteArea.MouseMove += Palette_MouseMove;
            PaletteArea.MouseUp += PaletteArea_MouseUp;

            Eyedropper.Click += Eyedropper_Click;

            UpdateVisuals();

            // 获取引用后立即同步一次 UI 位置
            if (IsLoaded)
            {
                SyncPositionsFromColor(SelectedColor);
            }
            else
            {
                Loaded += (s, e) => SyncPositionsFromColor(SelectedColor);
            }
        }

        public void ShowColorPopup_Click(object sender, RoutedEventArgs e)
        {
            IsShowColorPopup = !IsShowColorPopup;
        }

        private void Eyedropper_Click(object sender, RoutedEventArgs e)
        {
            var eyeDropper = new EyedropperWindow()
            {
                Style = Application.Current.FindResource("EyedropperWindowStyle") as Style
            };

            // 订阅选色事件
            eyeDropper.ColorPicked += (color) =>
            {
                // 更新当前控件的颜色
                // 因为 SyncPositionsFromColor 处理了 UI 更新，这里直接赋值即可
                // 注意：EyedropperWindow 是在 UI 线程关闭的，所以不需要 Dispatcher.Invoke
                SelectedColor = color;

                // 确保 UI 同步（如果你的 SelectedColor 绑定逻辑还没写全，手动调一下也可以）
                SyncPositionsFromColor(color);
            };

            eyeDropper.Closed += (s, args) =>
            {
                // 确保吸管关闭后，焦点回到 PickerToggle 或控件本身
                // 这样可以防止父级 Popup 认为用户点击了外部
                Dispatcher.InvokeAsync(() => {
                    Focus();
                });
            };

            eyeDropper.Show();
        }

        public void Hue_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HueCanvas.Focus();
            HueCanvas.CaptureMouse();
            UpdateHue(e.GetPosition(HueCanvas).X);
            e.Handled = true;
        }

        public void Hue_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateHue(e.GetPosition(HueCanvas).X);
            }
            e.Handled = true;
        }

        private void HueCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            HueCanvas.ReleaseMouseCapture();
            e.Handled = true;
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VectorColorPicker picker && e.NewValue is Color newColor)
            {
                picker.UpdateSenderColor(newColor);
                // 关键逻辑：如果是 UI 交互触发的颜色变化，不需要反向同步位置
                // 我们只在“外部代码”修改颜色时才进行同步
                if (picker.isInternalUpdating)
                {
                    return;
                }
                picker.SyncPositionsFromColor(newColor);
            }
        }

        public void Palette_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PaletteArea.Focus();
            PaletteArea.CaptureMouse();
            UpdateSaturationValue(e.GetPosition(PaletteArea));
            e.Handled = true;
        }

        private void Palette_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateSaturationValue(e.GetPosition(PaletteArea));
            }
            e.Handled = true;
        }

        private void PaletteArea_MouseUp(object sender, MouseButtonEventArgs e)
        {
            PaletteArea.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void PaletteThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            // 处理 Thumb 自身的拖拽
            double x = Canvas.GetLeft(PaletteThumb) + e.HorizontalChange;
            double y = Canvas.GetTop(PaletteThumb) + e.VerticalChange;
            UpdateSaturationValue(new Point(x + PaletteThumb.Width / 2, y + PaletteThumb.Height / 2));
        }
        #endregion
    }
}
