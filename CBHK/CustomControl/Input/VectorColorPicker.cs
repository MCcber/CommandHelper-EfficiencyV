using CBHK.CustomControl.Container;
using CBHK.CustomControl.VectorButton;
using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CBHK.CustomControl.Input
{
    public class VectorColorPicker : Control
    {
        #region Field
        private double currentHue = 0;
        private double currentSat = 1;
        private double currentVal = 1;
        private Canvas HueCanvas;
        private Border ColorPreview;
        private Thumb PaletteThumb;
        private Rectangle ColorBase;
        private Thumb HueThumb;
        public Grid PaletteArea;
        private VectorFlatIconButton Eyedropper;

        private bool isInternalUpdating = false;
        private bool isPaletteDragging = false;
        private bool isHueDragging = false;
        private bool hasPendingColorCommit = false;
        private readonly SolidColorBrush selectedColorBrush = new(Colors.Red);
        #endregion

        #region Property
        public double OriginBottomHeight { get; set; }
        public Action<Color> UpdateSelectedColorCallBack { get; set; }
        /// <summary>
        /// 用户在面板上完成一次改色后触发（点一下、拖动调色板、拖动滑块都算），一次交互触发一次。
        /// 不是每次鼠标移动都触发——消费方拿它做"提交"（例如往颜色列表里加一条），
        /// 按移动触发会把拖一趟变成几十条记录。颜色值从 <see cref="SelectedColor"/> 取。
        /// </summary>
        public event Action SelectedColorChanged;

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(VectorColorPicker), new PropertyMetadata(default(string)));

        // 定义 SelectedColor 依赖属性，方便外部绑定
        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        public static readonly DependencyProperty SelectedColorProperty =
    DependencyProperty.Register("SelectedColor", typeof(Color), typeof(VectorColorPicker),
        new PropertyMetadata(Colors.Red, OnSelectedColorChanged));

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

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(VectorColorPicker), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        private void UpdateVisuals()
        {
            SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
            // 确保在控件布局完成后执行，否则 ActualWidth 会是 0
            Loaded += VectorColorPicker_Loaded;
        }

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
            currentSat = x / w;

            // 计算 LeftValue (Y轴: 1 -> 0) *注意 Y轴向下是增加，但亮度向下是减小
            currentVal = 1 - (y / h);

            UpdateFinalColor();
            hasPendingColorCommit = true;
        }

        /// <summary>
        /// 色相条：按下即生效，按住拖动持续生效。
        /// </summary>
        private void UpdateHue(double mouseX)
        {
            double width = HueCanvas.ActualWidth;

            if (width <= 0)
            {
                return;
            }

            mouseX = Math.Clamp(mouseX, 0, width);

            // 移动滑块
            Canvas.SetLeft(HueThumb, mouseX - (HueThumb.ActualWidth / 2));

            // 计算 Hue (0-360)
            currentHue = (mouseX / width) * 360;

            // 更新调色板底色
            var hueColor = new HsvColor(currentHue, 1, 1).ToRgb();
            ColorBase.Fill = new SolidColorBrush(hueColor);

            UpdateFinalColor();
            hasPendingColorCommit = true;
        }

        /// <summary>
        /// 一次交互结束（松手 / 滑块拖完）后把改色通知发出去；这一趟没改过色就不发。
        /// </summary>
        private void CommitColorChange()
        {
            if (!hasPendingColorCommit)
            {
                return;
            }

            hasPendingColorCommit = false;
            SelectedColorChanged?.Invoke();
        }

        private void UpdateFinalColor()
        {
            isInternalUpdating = true;

            var color = new HsvColor(currentHue, currentSat, currentVal).ToRgb();
            SelectedColor = color;
            UpdatePreviewColor(color);
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
            currentHue = hsv.H;
            currentSat = hsv.S;
            currentVal = hsv.V;

            // 2. 如果控件还没加载好，ActualWidth 是 0，无法计算位置
            if (!IsLoaded) return;

            // 3. 计算并移动色相滑块 (Hue)
            double hueX = (currentHue / 360) * HueCanvas.ActualWidth;
            Canvas.SetLeft(HueThumb, hueX - (HueThumb.ActualWidth / 2));

            // 4. 更新调色板的背景底色
            var hueColor = new HsvColor(currentHue, 1, 1).ToRgb();
            ColorBase.Fill = new SolidColorBrush(hueColor);

            // 5. 计算并移动调色板中心点 (Saturation & LeftValue)
            double paletteX = currentSat * PaletteArea.ActualWidth;
            double paletteY = (1 - currentVal) * PaletteArea.ActualHeight;
            Canvas.SetLeft(PaletteThumb, paletteX - (PaletteThumb.ActualWidth / 2));
            Canvas.SetTop(PaletteThumb, paletteY - (PaletteThumb.ActualHeight / 2));

            // 6. 更新 Hex 文本和预览色块
            UpdatePreviewColor(color);
            HexCode = color.ToString();
        }

        /// <summary>
        /// 预览色块用的是模板里的 Border，直接改它的 Background 即可：
        /// 这个笔刷只给本控件自己看，没有对外暴露的理由，所以不再占一个依赖属性。
        /// </summary>
        private void UpdatePreviewColor(Color color)
        {
            selectedColorBrush.Color = color;
            if (ColorPreview is not null)
            {
                ColorPreview.Background = selectedColorBrush;
            }
        }

        private void UpdateSenderColor(Color newColor)
        {
            UpdateSelectedColorCallBack?.Invoke(newColor);
        }

        public void UpdateBorderColorByBackgroundColor()
        {
            if (ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(themeBrush.Color);
            }
        }
        #endregion

        #region Event
        private void VectorColorPicker_Loaded(object sender, RoutedEventArgs e)
        {
            LostFocus += VectorColorPicker_LostFocus;
            // 1. 从当前颜色反推 HSV
            var hsv = HsvColor.FromRgb(SelectedColor);
            currentHue = hsv.H;
            currentSat = hsv.S;
            currentVal = hsv.V;

            // 2. 更新色相条滑块位置
            double hueX = (currentHue / 360) * HueCanvas.ActualWidth;
            Canvas.SetLeft(HueThumb, hueX - (HueThumb.ActualWidth / 2));

            // 3. 更新调色板底色
            var hueColor = new HsvColor(currentHue, 1, 1).ToRgb();
            ColorBase.Fill = new SolidColorBrush(hueColor);

            // 4. 更新调色板滑块位置
            double paletteX = currentSat * PaletteArea.ActualWidth;
            double paletteY = (1 - currentVal) * PaletteArea.ActualHeight;
            Canvas.SetLeft(PaletteThumb, paletteX - (PaletteThumb.ActualWidth / 2));
            Canvas.SetTop(PaletteThumb, paletteY - (PaletteThumb.ActualHeight / 2));

            // 5. 更新显示的文本和预览
            UpdateFinalColor();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == ThemeBackgroundProperty)
            {
                UpdateBorderColorByBackgroundColor();
            }
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

            VectorTextButton PickerToggle = GetTemplateChild("PickerToggle") as VectorTextButton;
            HueCanvas = GetTemplateChild("HueCanvas") as Canvas;
            HueThumb = GetTemplateChild("HueThumb") as Thumb;
            PaletteThumb = GetTemplateChild("PaletteThumb") as Thumb;
            ColorBase = GetTemplateChild("ColorBase") as Rectangle;
            PaletteArea = GetTemplateChild("PaletteArea") as Grid;
            Eyedropper = GetTemplateChild("Eyedropper") as VectorFlatIconButton;
            ColorPreview = GetTemplateChild("ColorPreview") as Border;

            PickerToggle.Click += ShowColorPopup_Click;
            if(OriginBottomHeight == 0)
            {
                OriginBottomHeight = 6;
            }
            PickerToggle.Loaded += (sender,e) =>
            {
                PickerToggle.OriginBottomHeight = OriginBottomHeight;
                object extraBottomLine = PickerToggle.Template.FindName("extraBottomLine", PickerToggle);
                if (extraBottomLine is RowDefinition row)
                {
                    row.Height = new(OriginBottomHeight, GridUnitType.Pixel);
                }
            };;

            // 按下必须用 Preview：Thumb 的类处理器会吃掉冒泡的 MouseLeftButtonDown 并自己开拖，
            // 于是点在滑块上时我们的处理根本跑不到（表现为"点Thumb不动、点条背景才跟手"）。
            // Preview 是隧道阶段，先于 Thumb 的类处理器，标记 Handled 后滑块就不会自己抢捕获了。
            HueCanvas.PreviewMouseLeftButtonDown += Hue_MouseDown;
            HueCanvas.MouseMove += Hue_MouseMove;
            HueCanvas.MouseUp += HueCanvas_MouseUp;

            PaletteThumb.DragDelta += PaletteThumb_DragDelta;
            PaletteThumb.DragCompleted += PaletteThumb_DragCompleted;

            PaletteArea.MouseLeftButtonDown += Palette_MouseDown;
            PaletteArea.MouseLeftButtonUp += PaletteArea_MouseLeftButtonUp;
            PaletteArea.MouseMove += Palette_MouseMove;
            PaletteArea.MouseUp += PaletteArea_MouseUp;
            // 只在"调色板自己"丢了捕获时解除拖动。LostMouseCapture 会冒泡：
            // 子级（比如滑块）丢捕获也冒上来，不判来源就会在刚按下的一瞬间把拖动标记清掉，
            // 表现就是拖动期间不更新、松手才跳一次。
            PaletteArea.LostMouseCapture += (sender, e) =>
            {
                if (ReferenceEquals(e.OriginalSource, PaletteArea))
                {
                    isPaletteDragging = false;
                }
            };

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

        private void PaletteArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CommitColorChange();
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

        private void Hue_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HueCanvas.Focus();
            HueCanvas.CaptureMouse();
            isHueDragging = true;
            UpdateHue(e.GetPosition(HueCanvas).X);
            e.Handled = true;
        }

        private void Hue_MouseMove(object sender, MouseEventArgs e)
        {
            // 与调色板同口径：判"这次按下是不是从这里开始的"，不依赖 e.LeftButton
            if (!isHueDragging)
            {
                return;
            }
            UpdateHue(e.GetPosition(HueCanvas).X);
            e.Handled = true;
        }

        private void HueCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isHueDragging = false;
            HueCanvas.ReleaseMouseCapture();
            CommitColorChange();
            e.Handled = true;
        }

        public void Palette_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 如果点击源是 Thumb (或其可视子元素)，则不处理，交由 Thumb 自己的逻辑处理
            if (e.OriginalSource is DependencyObject obj &&
                (PaletteThumb == obj || PaletteThumb.IsAncestorOf(obj)))
            {
                return;
            }
            PaletteArea.Focus();
            PaletteArea.CaptureMouse();
            isPaletteDragging = true;
            UpdateSaturationValue(e.GetPosition(PaletteArea));
            e.Handled = true;
        }

        private void Palette_MouseMove(object sender, MouseEventArgs e)
        {
            // 按住不放就持续改色：判据用"这次按下是不是从这里开始的"，不用 e.LeftButton
            // （捕获态下它可能读到 Released，拖动会在第一帧断掉，表现成"只能点一下"）
            if (!isPaletteDragging)
            {
                return;
            }
            UpdateSaturationValue(e.GetPosition(PaletteArea));
            e.Handled = true;
        }

        private void PaletteArea_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isPaletteDragging = false;
            PaletteArea.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void PaletteThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            CommitColorChange();
        }

        private void PaletteThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            // 处理 Thumb 自身的拖拽
            double x = Canvas.GetLeft(PaletteThumb) + e.HorizontalChange;
            double y = Canvas.GetTop(PaletteThumb) + e.VerticalChange;
            UpdateSaturationValue(new Point(x + PaletteThumb.ActualWidth / 2, y + PaletteThumb.ActualHeight / 2));
        }
        #endregion
    }
}