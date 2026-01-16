using CommunityToolkit.Mvvm.Input;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CBHK.CustomControl.Container
{
    public partial class VectorWindow : Window
    {
        #region Field And ExternMethod
        private string restoreGeometryData = "M795.5 230.6v727.2H64V230.6h731.5z m-82.2 78H146.2V880h567V308.6zM228.5 66.2H960v731.5H795.5v-82.2h82.2V148.4h-567v82.2h-82.2V66.2z";
        /// <summary>
        /// 设计时基准高度（96 DPI下的高度）
        /// </summary>
        private double baseTitleBarHeight = 25.0;
        private double baseTitleButtonWidth = 30.0;
        private Button MaximizeButton;
        private const int GWL_STYLE = -16;
        private const uint WS_MAXIMIZEBOX = (uint)0x00010000L;
        private const uint WS_MINIMIZEBOX = (uint)0x00020000L;
        private const uint WS_SYSMENU = (uint)0x00080000L;
        private const uint WS_CAPTION = 0x00C00000;
        private const uint WS_THICKFRAME = (uint)0x00040000L;
        // 注意：其确切值可能因 Windows SDK 版本不同而变化，常见的有效值是 38 或 33
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 38;
        private const int DWMWCP_ROUND = 2;  // 圆角

        // Win32 常量
        private const int WM_NCHITTEST = 0x0084;//非客户区碰撞测试
        private const int WM_NCMOUSEMOVE = 0x00A0;//非客户区鼠标移动
        private const int WM_NCMOUSELEAVE = 0x02A2;//非客户区鼠标离开
        private const int WM_NCLBUTTONDOWN = 0x00A1; // 非客户区左键按下
        private const int WM_NCLBUTTONUP = 0x00A2;   // 非客户区左键抬起
        private const int WM_NCLBUTTONDBLCLK = 0x00A3; // 非客户区左键双击
        private const int HTCAPTION = 2;
        private const int WM_NCPAINT = 0x0085;
        private const int WM_NCACTIVATE = 0x0086;
        private const int WM_NCCALCSIZE = 0x0083;
        private const int WM_ERASEBKGND = 0x0014;
        private const int HTMAXBUTTON = 9;//撞击最大化按钮
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38; // 设置背景类型
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20; // 开启深色模式支持
        const uint RDW_INVALIDATE = 0x0001;
        const uint RDW_UPDATENOW = 0x0100;
        const uint RDW_FRAME = 0x0400;
        // 背景枚举值
        /// <summary>
        /// Mica (云母)
        /// </summary>
        private const int DWMSBT_MAINWINDOW = 2;
        /// <summary>
        /// Acrylic (亚克力)
        /// </summary>
        private const int DWMSBT_TRANSIENTWINDOW = 3;
        /// <summary>
        /// Mica Alt (层次感更强，推荐)
        /// </summary>
        private const int DWMSBT_TABBEDWINDOW = 4;

        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MAXIMIZE = 0xF030;
        private const int SC_RESTORE = 0xF120;

        private struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [DllImport("user32.dll")]
        static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("dwmapi.dll")]
        static extern int DwmFlush();
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
        [DllImport("user32.dll")]
        public static extern uint GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);
        [DllImport("user32.dll")]
        private static extern int SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
        #endregion

        #region Property
        public double TitleBarHeight
        {
            get { return (double)GetValue(TitleBarHeightProperty); }
            set { SetValue(TitleBarHeightProperty, value); }
        }

        public static readonly DependencyProperty TitleBarHeightProperty =
            DependencyProperty.Register("TitleBarHeight", typeof(double), typeof(VectorWindow), new PropertyMetadata(default(double)));

        public double TitleButtonWidth
        {
            get { return (double)GetValue(TitleButtonWidthProperty); }
            set { SetValue(TitleButtonWidthProperty, value); }
        }

        public Thickness IconMargin
        {
            get { return (Thickness)GetValue(IconMarginProperty); }
            set { SetValue(IconMarginProperty, value); }
        }

        public static readonly DependencyProperty IconMarginProperty =
            DependencyProperty.Register("IconMargin", typeof(Thickness), typeof(VectorWindow), new PropertyMetadata(default(Thickness)));

        public static readonly DependencyProperty TitleButtonWidthProperty =
            DependencyProperty.Register("TitleButtonWidth", typeof(double), typeof(VectorWindow), new PropertyMetadata(default(double)));
        public IRelayCommand MinimizeWindowCommand
        {
            get { return (IRelayCommand)GetValue(MinimizeWindowCommandProperty); }
            set { SetValue(MinimizeWindowCommandProperty, value); }
        }

        public static readonly DependencyProperty MinimizeWindowCommandProperty =
            DependencyProperty.Register("MinimizeWindowCommand", typeof(IRelayCommand), typeof(VectorWindow), new PropertyMetadata(default(IRelayCommand)));

        public IRelayCommand MaximizeWindowCommand
        {
            get { return (IRelayCommand)GetValue(MaximizeWindowCommandProperty); }
            set { SetValue(MaximizeWindowCommandProperty, value); }
        }

        public static readonly DependencyProperty MaximizeWindowCommandProperty =
            DependencyProperty.Register("MaximizeWindowCommand", typeof(IRelayCommand), typeof(VectorWindow), new PropertyMetadata(default(IRelayCommand)));

        public IRelayCommand RestoreWindowCommand
        {
            get { return (IRelayCommand)GetValue(RestoreWindowCommandProperty); }
            set { SetValue(RestoreWindowCommandProperty, value); }
        }

        public static readonly DependencyProperty RestoreWindowCommandProperty =
            DependencyProperty.Register("RestoreWindowCommand", typeof(IRelayCommand), typeof(VectorWindow), new PropertyMetadata(default(IRelayCommand)));

        public IRelayCommand CloseWindowCommand
        {
            get { return (IRelayCommand)GetValue(CloseWindowCommandProperty); }
            set { SetValue(CloseWindowCommandProperty, value); }
        }

        public static readonly DependencyProperty CloseWindowCommandProperty =
            DependencyProperty.Register("CloseWindowCommand", typeof(IRelayCommand), typeof(VectorWindow), new PropertyMetadata(default(IRelayCommand)));

        public Geometry MaximizePathData
        {
            get { return (Geometry)GetValue(MaximizePathDataProperty); }
            set { SetValue(MaximizePathDataProperty, value); }
        }

        public static readonly DependencyProperty MaximizePathDataProperty =
            DependencyProperty.Register("MaximizePathData", typeof(Geometry), typeof(VectorWindow), new PropertyMetadata(default(Geometry)));

        public Geometry RestorePathData
        {
            get { return (Geometry)GetValue(RestorePathDataProperty); }
            set { SetValue(RestorePathDataProperty, value); }
        }

        public static readonly DependencyProperty RestorePathDataProperty =
            DependencyProperty.Register("RestorePathData", typeof(Geometry), typeof(VectorWindow), new PropertyMetadata(default(Geometry)));

        public Brush RestorePathFillBrush
        {
            get { return (Brush)GetValue(RestorePathFillBrushProperty); }
            set { SetValue(RestorePathFillBrushProperty, value); }
        }

        public static readonly DependencyProperty RestorePathFillBrushProperty =
            DependencyProperty.Register("RestorePathFillBrush", typeof(Brush), typeof(VectorWindow), new PropertyMetadata(default(Brush)));

        public Geometry MaximizeButtonPathData
        {
            get { return (Geometry)GetValue(MaximizeButtonPathDataProperty); }
            set { SetValue(MaximizeButtonPathDataProperty, value); }
        }

        public static readonly DependencyProperty MaximizeButtonPathDataProperty =
            DependencyProperty.Register("MaximizeButtonPathData", typeof(Geometry), typeof(VectorWindow), new PropertyMetadata(default(Geometry)));
        #endregion

        #region Method
        public VectorWindow()
        {
            Loaded += VectorWindow_Loaded;
            RestorePathData = Geometry.Parse(restoreGeometryData);
            Rectangle rectangle = new();
            rectangle.Width = rectangle.Height = 10;
            rectangle.StrokeThickness = 1;
            rectangle.Stroke = Brushes.White;
            RectangleGeometry rectangleGeometry = new(new Rect(0, 0, 10, 10), 0, 0);
            MaximizePathData = rectangleGeometry;
            MaximizeButtonPathData = MaximizePathData;
            //设置三个按钮的命令
            MinimizeWindowCommand = new RelayCommand(() => 
            {
                WindowState = WindowState.Minimized;
            });
            CloseWindowCommand = new RelayCommand(Close);
            // 强制尝试查找并应用按类型键的样式
            var style = TryFindResource(typeof(VectorWindow)) as Style
                        ?? (Style)Application.Current.Resources[typeof(VectorWindow)];
            if (style != null)
            {
                Style = style;
            }
        }

        public void SetAcrylicBackground()
        {
            var handle = new WindowInteropHelper(this).Handle;
            int backup = DWMSBT_TRANSIENTWINDOW;
            DwmSetWindowAttribute(handle, 38, ref backup, sizeof(int));

            MARGINS margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            DwmExtendFrameIntoClientArea(handle, ref margins);

            RedrawWindow(handle, IntPtr.Zero, IntPtr.Zero, RDW_INVALIDATE | RDW_UPDATENOW | RDW_FRAME);
            DwmFlush();
        }

        public void SetMicaBackground()
        {
            var handle = new WindowInteropHelper(this).Handle;
            int backup = DWMSBT_MAINWINDOW;
            DwmSetWindowAttribute(handle, 38, ref backup, sizeof(int));

            MARGINS margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            DwmExtendFrameIntoClientArea(handle, ref margins);

            RedrawWindow(handle, IntPtr.Zero, IntPtr.Zero, RDW_INVALIDATE | RDW_UPDATENOW | RDW_FRAME);
            DwmFlush();
        }

        public void SetMicaAltBackground()
        {
            var handle = new WindowInteropHelper(this).Handle;
            int backup = DWMSBT_TABBEDWINDOW;
            DwmSetWindowAttribute(handle, 38, ref backup, sizeof(int));

            MARGINS margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            DwmExtendFrameIntoClientArea(handle, ref margins);

            RedrawWindow(handle, IntPtr.Zero, IntPtr.Zero, RDW_INVALIDATE | RDW_UPDATENOW | RDW_FRAME);
            DwmFlush();
        }

        private bool IsMouseOnButton(IInputElement element)
        {
            if (element is DependencyObject obj)
            {
                // 向上寻找父级，看是否落在按钮内
                while (obj != null)
                {
                    // 根据你的 XAML，这会匹配到 VectorSolidIconButton
                    if (obj is Button || obj is System.Windows.Controls.Primitives.ButtonBase)
                    {
                        return true;
                    }
                    obj = VisualTreeHelper.GetParent(obj);
                }
            }
            return false;
        }

        /// <summary>
        /// 临时为窗口添加 WS_CAPTION 标志，执行指定动作后（短延迟）恢复原样。
        /// 用途：在执行最大化/还原过渡时让 DWM 使用系统标题栏动画采样。
        /// </summary>
        private void TemporarilyUseCaptionForAction(Action action, int delayMs = 100)
        {
            var handle = new WindowInteropHelper(this).Handle;
            uint original = GetWindowLong(handle, GWL_STYLE);
            bool hadCaption = (original & WS_CAPTION) != 0;

            if (!hadCaption)
            {
                SetWindowLong(handle, GWL_STYLE, original | WS_CAPTION);
                SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, 0x0037);
            }

            try
            {
                action?.Invoke();
            }
            finally
            {
                // 在后台等待一段时间再移除 WS_CAPTION，确保系统有时间做动画
                System.Threading.Tasks.Task.Run(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(delayMs).ConfigureAwait(false);
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (!hadCaption)
                        {
                            uint now = GetWindowLong(handle, GWL_STYLE);
                            SetWindowLong(handle, GWL_STYLE, now & ~WS_CAPTION);
                            SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, 0x0037);
                        }
                    }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                });
            }
        }
        #endregion

        #region Event
        private void VectorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 获取当前DPI缩放因子
            var dpiInfo = VisualTreeHelper.GetDpi(this);
            double dpiScaleY = dpiInfo.DpiScaleY; // Y轴DPI缩放

            // 通过绑定或直接设置CustomTitleBar的高度
            TitleBarHeight = baseTitleBarHeight * dpiInfo.DpiScaleY;
            TitleButtonWidth = baseTitleButtonWidth * dpiInfo.DpiScaleX;
        }

        public void MaximizeButton_Loaded(object sender, RoutedEventArgs e) => MaximizeButton = (sender as Button);

        protected override void OnSourceInitialized(EventArgs e)
        {
            #region Init And Field
            base.OnSourceInitialized(e);
            var handle = new WindowInteropHelper(this).Handle;
            #endregion

            #region 设置各类外观属性
            // 1. 样式设置（保留需要的系统按钮与可调整边框）
            var style = GetWindowLong(handle, GWL_STYLE);
            style |= WS_MAXIMIZEBOX | WS_SYSMENU | WS_THICKFRAME;
            style &= ~WS_CAPTION;
            SetWindowLong(handle, GWL_STYLE, style);

            // 2. 开启深色模式（云母效果必备，否则会变白或变灰）
            int darkMode = 1;
            DwmSetWindowAttribute(handle, 20, ref darkMode, sizeof(int));

            SetMicaAltBackground();

            //圆角
            int cornerRadius = 2;
            DwmSetWindowAttribute(handle, 33, ref cornerRadius, sizeof(int));

            // 5. 挂载钩子
            HwndSource.FromHwnd(handle).AddHook(new HwndSourceHook(WndProc));

            // 6. 强制刷新窗口非客户区
            SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, 0x0037);
            #endregion
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_NCHITTEST:
                    {
                        // 1. 获取鼠标屏幕坐标 (lParam 包含 X 和 Y)
                        // 低 16 位是 X，高 16 位是 Y
                        int x = (short)(lParam.ToInt32() & 0xFFFF);
                        int y = (short)((lParam.ToInt32() >> 16) & 0xFFFF);
                        Point mouseScreenPos = new(x, y);

                        if (MaximizeButton != null)
                        {
                            Point btnTopLeft = MaximizeButton.PointToScreen(new Point(0, 0));
                            Point btnBottomRight = MaximizeButton.PointToScreen(new Point(MaximizeButton.ActualWidth, MaximizeButton.ActualHeight));

                            if (mouseScreenPos.X >= btnTopLeft.X && mouseScreenPos.X < btnBottomRight.X &&
                                mouseScreenPos.Y >= btnTopLeft.Y && mouseScreenPos.Y < btnBottomRight.Y)
                            {
                                handled = true;
                                return new IntPtr(HTMAXBUTTON);
                            }
                        }

                        // 将屏幕像素坐标转换为 WPF 设备无关坐标
                        var src = PresentationSource.FromVisual(this);
                        Point windowPoint;
                        if (src?.CompositionTarget != null)
                        {
                            windowPoint = src.CompositionTarget.TransformFromDevice.Transform(mouseScreenPos);
                        }
                        else
                        {
                            windowPoint = PointFromScreen(mouseScreenPos); // 退回，注意 DPI 情况
                        }

                        // 标题栏范围内才处理（TitleBarHeight 必须与上面坐标单位一致）
                        if (windowPoint.Y >= 0 && windowPoint.Y <= TitleBarHeight)
                        {
                            // 在按钮上时让 WPF 自己处理（HTCLIENT）
                            IInputElement element = InputHitTest(windowPoint);
                            if (IsMouseOnButton(element))
                            {
                                handled = true;
                                return new IntPtr(1); // HTCLIENT
                            }

                            // 不是按钮，交给系统当作标题栏（允许拖拽、Snap、系统动画）
                            handled = true;
                            return new IntPtr(2); // HTCAPTION
                        }
                        break;
                    }
                case WM_NCMOUSEMOVE:
                    {
                        if (wParam.ToInt32() == HTMAXBUTTON)
                        {
                            if (MaximizeButton != null)
                                MaximizeButton.Tag ??= "Hover"; // 触发 XAML 变色
                        }
                        else
                        {
                            if (MaximizeButton != null)
                                MaximizeButton.Tag = null;
                        }
                        break;
                    }
                case WM_NCMOUSELEAVE:
                    {
                        // --- 阶段3：鼠标彻底离开非客户区 ---
                        if (MaximizeButton != null)
                            MaximizeButton.Tag = null; // 确保熄灭
                        break;
                    }
                case WM_NCLBUTTONDOWN:
                    {
                        if (wParam.ToInt32() == HTMAXBUTTON)
                        {
                            handled = true;
                            return IntPtr.Zero;
                        }
                        break;
                    }
                case WM_NCLBUTTONUP:
                    {
                        if (wParam.ToInt32() == HTMAXBUTTON)
                        {
                            if (WindowState == WindowState.Maximized)
                            {
                                TemporarilyUseCaptionForAction(() => SystemCommands.RestoreWindow(this));
                                MaximizeButtonPathData = MaximizePathData;
                                RestorePathFillBrush = Brushes.Transparent;
                            }
                            else
                            {
                                TemporarilyUseCaptionForAction(() => SystemCommands.MaximizeWindow(this));
                                MaximizeButtonPathData = RestorePathData;
                                RestorePathFillBrush = Brushes.White;
                            }
                            handled = true;
                            return IntPtr.Zero;
                        }
                        break;
                    }
                case WM_NCLBUTTONDBLCLK:
                    {
                        // 双击标题栏：使用临时 WS_CAPTION 来允许系统动画
                        if (wParam.ToInt32() == HTCAPTION)
                        {
                            if (WindowState == WindowState.Maximized)
                            {
                                TemporarilyUseCaptionForAction(() => SystemCommands.RestoreWindow(this));
                                MaximizeButtonPathData = MaximizePathData;
                                RestorePathFillBrush = Brushes.Transparent;
                            }
                            else
                            {
                                TemporarilyUseCaptionForAction(() => SystemCommands.MaximizeWindow(this));
                                MaximizeButtonPathData = RestorePathData;
                                RestorePathFillBrush = Brushes.White;
                            }
                            handled = true;
                            return IntPtr.Zero;
                        }
                        break;
                    }
            }
            return IntPtr.Zero;
        }
        #endregion
    }
}