using CBHK.Common.Model;
using System;
using System.Linq;
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
        private bool isClosing = false;
        private string restoreGeometryData = "M795.5 230.6v727.2H64V230.6h731.5z m-82.2 78H146.2V880h567V308.6zM228.5 66.2H960v731.5H795.5v-82.2h82.2V148.4h-567v82.2h-82.2V66.2z";
        /// <summary>
        /// 设计时基准高度（96 DPI下的高度）
        /// </summary>
        private double baseTitleBarHeight = 30.0;
        private double baseTitleButtonWidth = 40.0;
        private Button MinimizeButton;
        private Button MaximizeButton;
        private Button CloseButton;
        private const int GWL_STYLE = -16;
        private const uint WS_MAXIMIZEBOX = (uint)0x00010000L;
        private const uint WS_MINIMIZEBOX = (uint)0x00020000L;
        private const uint WS_SYSMENU = (uint)0x00080000L;
        private const uint WS_CAPTION = 0x00C00000;
        private const uint WS_THICKFRAME = (uint)0x00040000L;

        // Win32 常量
        private const int SC_MINIMIZE = 0xF020;
        private const int SC_MAXIMIZE = 0xF030;
        private const int SC_RESTORE = 0xF120;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int WM_GETMINMAXINFO = 0x0024;
        private const int WM_SETTINGCHANGE = 0x001A;
        private const int WM_NCHITTEST = 0x0084;//非客户区碰撞测试
        private const int WM_NCMOUSEMOVE = 0x00A0;//非客户区鼠标移动
        private const int WM_NCMOUSELEAVE = 0x02A2;//非客户区鼠标离开
        private const int WM_NCLBUTTONDOWN = 0x00A1; // 非客户区左键按下
        private const int WM_NCLBUTTONUP = 0x00A2;   // 非客户区左键抬起
        private const int WM_NCLBUTTONDBLCLK = 0x00A3; // 非客户区左键双击
        private const int HTCAPTION = 2;
        private const int HTMINBUTTON = 8;
        private const int HTMAXBUTTON = 9;//撞击最大化按钮
        private const int HTCLOSEBUTTON = 20;
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

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MONITORINFO
        {
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            public RECT rcMonitor = new();
            public RECT rcWork = new();
            public int dwFlags = 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        private struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);

        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        [DllImport("user32.dll")]
        static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("dwmapi.dll")]
        static extern int DwmFlush();
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

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

        public WindowVisualType VisualType
        {
            get { return (WindowVisualType)GetValue(VisualTypeProperty); }
            set { SetValue(VisualTypeProperty, value); }
        }

        public static readonly DependencyProperty VisualTypeProperty =
            DependencyProperty.Register("VisualType", typeof(WindowVisualType), typeof(VectorWindow), new PropertyMetadata(default(WindowVisualType), OnVisualType_Changed));

        public WindowThemeType ThemeType
        {
            get { return (WindowThemeType)GetValue(ThemeTypeProperty); }
            set { SetValue(ThemeTypeProperty, value); }
        }

        public static readonly DependencyProperty ThemeTypeProperty =
            DependencyProperty.Register("WindowThemeType", typeof(WindowThemeType), typeof(VectorWindow), new PropertyMetadata(default(WindowThemeType), OnThemeType_Changed));

        public WindowCornerPreference CornerPreference
        {
            get { return (WindowCornerPreference)GetValue(CornerPreferenceProperty); }
            set { SetValue(CornerPreferenceProperty, value); }
        }

        public static readonly DependencyProperty CornerPreferenceProperty =
            DependencyProperty.Register("CornerPreference", typeof(WindowCornerPreference), typeof(VectorWindow), new PropertyMetadata(default(WindowCornerPreference), OnCornderType_Changed));
        #endregion

        #region Method
        public VectorWindow()
        {
            Loaded += VectorWindow_Loaded;
            Activated += VectorWindow_Activated;
            RestorePathData = Geometry.Parse(restoreGeometryData);
            Rectangle rectangle = new();
            rectangle.Width = rectangle.Height = 10;
            rectangle.StrokeThickness = 1;
            rectangle.Stroke = Brushes.White;
            RectangleGeometry rectangleGeometry = new(new Rect(0, 0, 10, 10), 0, 0);
            MaximizePathData = rectangleGeometry;
            MaximizeButtonPathData = MaximizePathData;
            // 强制尝试查找并应用按类型键的样式
            var style = TryFindResource(typeof(VectorWindow)) as Style
                        ?? (Style)Application.Current.Resources[typeof(VectorWindow)];
            if (style != null)
            {
                Style = style;
            }
        }

        private static IntPtr SetWindowLongPtrCompat(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8) return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        private static IntPtr GetWindowLongPtrCompat(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8) return GetWindowLongPtr64(hWnd, nIndex);
            return new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        /// <summary>
        /// 设置主题模式，深色或浅色
        /// </summary>
        /// <param name="isDarkMode"></param>
        public void SetThemeMode(int isDarkMode = 1)
        {
            var handle = new WindowInteropHelper(this).Handle;
            _ = DwmSetWindowAttribute(handle, 20, ref isDarkMode, sizeof(int));
        }

        private void SetAcrylicBackground()
        {
            var handle = new WindowInteropHelper(this).Handle;
            int backup = DWMSBT_TRANSIENTWINDOW;
            _ = DwmSetWindowAttribute(handle, 38, ref backup, sizeof(int));

            MARGINS margins = new() { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            _ = DwmExtendFrameIntoClientArea(handle, ref margins);

            RedrawWindow(handle, IntPtr.Zero, IntPtr.Zero, RDW_INVALIDATE | RDW_UPDATENOW | RDW_FRAME);
            _ = DwmFlush();
        }

        private void SetMicaBackground()
        {
            var handle = new WindowInteropHelper(this).Handle;
            int backup = DWMSBT_MAINWINDOW;
            _ = DwmSetWindowAttribute(handle, 38, ref backup, sizeof(int));

            MARGINS margins = new() { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            _ = DwmExtendFrameIntoClientArea(handle, ref margins);

            RedrawWindow(handle, IntPtr.Zero, IntPtr.Zero, RDW_INVALIDATE | RDW_UPDATENOW | RDW_FRAME);
            _ = DwmFlush();
        }

        private void SetMicaAltBackground()
        {
            var handle = new WindowInteropHelper(this).Handle;
            int backup = DWMSBT_TABBEDWINDOW;
            _ = DwmSetWindowAttribute(handle, 38, ref backup, sizeof(int));

            MARGINS margins = new() { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            _ = DwmExtendFrameIntoClientArea(handle, ref margins);

            RedrawWindow(handle, IntPtr.Zero, IntPtr.Zero, RDW_INVALIDATE | RDW_UPDATENOW | RDW_FRAME);
            _ = DwmFlush();
        }

        public void UpdateWindowVisualStyle(WindowVisualType windowVisualType)
        {
            switch (windowVisualType)
            {
                case WindowVisualType.Acrylic:
                    {
                        SetAcrylicBackground();
                        break;
                    }
                case WindowVisualType.Mica:
                    {
                        SetMicaBackground();
                        break;
                    }
                case WindowVisualType.MicaAlt:
                    {
                        SetMicaAltBackground();
                        break;
                    }
            }
        }

        public void UpdateRoundCornerStyle(WindowCornerPreference preference)
        {
            var handle = new WindowInteropHelper(this).Handle;
            int cornerPreference = (int)preference;
            _ = DwmSetWindowAttribute(handle, 33, ref cornerPreference, sizeof(int));

            MARGINS margins = new() { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            _ = DwmExtendFrameIntoClientArea(handle, ref margins);

            RedrawWindow(handle, IntPtr.Zero, IntPtr.Zero, RDW_INVALIDATE | RDW_UPDATENOW | RDW_FRAME);
            _ = DwmFlush();
        }

        private static bool IsMouseOnButton(IInputElement element)
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
        private void TemporarilyUseCaptionForAction(Action action, int delayMs = 150)
        {
            var handle = new WindowInteropHelper(this).Handle;
            IntPtr originalPtr = GetWindowLongPtrCompat(handle, GWL_STYLE);
            uint original = (uint)(originalPtr.ToInt64() & 0xFFFFFFFF);
            bool hadCaption = (original & WS_CAPTION) != 0;

            if (!hadCaption)
            {
                uint newStyle = (original | WS_CAPTION | WS_SYSMENU) /*& ~WS_MINIMIZEBOX & ~WS_MAXIMIZEBOX*/;
                _ = SetWindowLongPtrCompat(handle, GWL_STYLE, new IntPtr((int)newStyle));
                SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, 0x0037);
                _ = DwmFlush();
            }

            try
            {
                action?.Invoke();
            }
            finally
            {
                if (!isClosing)
                {
                    // 在后台等待一段时间再移除 WS_CAPTION，确保系统有时间做动画
                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        await System.Threading.Tasks.Task.Delay(delayMs).ConfigureAwait(false);
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (!hadCaption)
                            {
                                uint now = (uint)(GetWindowLongPtrCompat(handle, GWL_STYLE).ToInt64() & 0xFFFFFFFF);
                                //& ~WS_CAPTION & ~WS_SYSMENU & ~WS_MINIMIZEBOX & ~WS_MAXIMIZEBOX
                                uint restored = (now | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX) & ~WS_CAPTION;
                                _ = SetWindowLongPtrCompat(handle, GWL_STYLE, new IntPtr((int)restored));
                                SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, 0x0037);
                                _ = DwmFlush();
                            }
                        }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                    });
                }
            }
        }

        private void UpdateThemeModeBySystem(nint lParam,bool skipParammeter = false)
        {
            string changedArea = Marshal.PtrToStringAuto(lParam);
            if (changedArea == "ImmersiveColorSet" || skipParammeter)
            {
                bool isDark = IsSystemDarkMode();
                int darkModeValue = isDark ? 1 : 0;
                var windowList = Application.Current.Windows.OfType<VectorWindow>();
                foreach (var window in windowList)
                {
                    window.SetThemeMode(darkModeValue);
                }
            }
        }

        private static bool IsSystemDarkMode()
        {
            // 查询注册表：0 = 深色，1 = 浅色
            object value = Microsoft.Win32.Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme",
                1 // 默认值设为1（浅色）
            );
            // 如果值是0，表示启用深色模式
            return value != null && (int)value == 0;
        }

        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new();
                GetMonitorInfo(monitor, monitorInfo);

                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;

                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.Left - rcMonitorArea.Left);
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.Top - rcMonitorArea.Top);
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.Right - rcWorkArea.Left);
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.Bottom - rcWorkArea.Top);

                // 【关键修复】：获取当前 DPI 缩放并转换 MinWidth/MinHeight
                var source = PresentationSource.FromVisual(this);
                if (source?.CompositionTarget != null)
                {
                    double scaleX = source.CompositionTarget.TransformToDevice.M11;
                    double scaleY = source.CompositionTarget.TransformToDevice.M22;

                    mmi.ptMinTrackSize.x = (int)(MinWidth * scaleX);
                    mmi.ptMinTrackSize.y = (int)(MinHeight * scaleY);
                }
            }
            Marshal.StructureToPtr(mmi, lParam, true);
        }

        private void UpdateMaximizeButtons(int command)
        {
            // 如果是还原，或者指令是还原
            if (command == SC_RESTORE)
            {
                MaximizeButtonPathData = MaximizePathData;
                RestorePathFillBrush = Brushes.Transparent;
            }
            // 如果指令是最大化，或者当前准备最大化
            else if (command == SC_MAXIMIZE)
            {
                MaximizeButtonPathData = RestorePathData;
                RestorePathFillBrush = Brushes.White;
            }
        }
        #endregion

        #region Event
        private void VectorWindow_Activated(object sender, EventArgs e)
        {
            #region 与主窗体同步主题与视觉效果
            string type = GetType().ToString();
            if (!type.EndsWith("MainView"))
            {
                var mainView = Application.Current.MainWindow as VectorWindow;
                ThemeType = mainView.ThemeType;
                VisualType = mainView.VisualType;
            }
            #endregion
        }

        /// <summary>
        /// 加载事件，处理标题栏尺寸
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VectorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 获取当前DPI缩放因子
            //var dpiInfo = VisualTreeHelper.GetDpi(this);

            // 通过绑定或直接设置CustomTitleBar的高度
            TitleBarHeight = baseTitleBarHeight;
            TitleButtonWidth = baseTitleButtonWidth;
        }

        public void MinimizeButton_Loaded(object sender, RoutedEventArgs e) => MinimizeButton = (sender as Button);

        public void MaximizeButton_Loaded(object sender, RoutedEventArgs e) => MaximizeButton = (sender as Button);

        public void CloseButton_Loaded(object sender, RoutedEventArgs e) => CloseButton = (sender as Button);

        protected override void OnSourceInitialized(EventArgs e)
        {
            #region Init And Field
            base.OnSourceInitialized(e);
            var handle = new WindowInteropHelper(this).Handle;
            #endregion

            #region 设置各类外观属性
            //设置窗体样式
            IntPtr stylePtr = GetWindowLongPtrCompat(handle, GWL_STYLE);
            uint style = (uint)(stylePtr.ToInt64() & 0xFFFFFFFF);
            style |= WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU | WS_THICKFRAME;
            style &= ~WS_CAPTION;
            _ = SetWindowLongPtrCompat(handle, GWL_STYLE, new IntPtr((int)style));
            //设置窗体视觉风格
            UpdateWindowVisualStyle(VisualType);
            //挂载钩子
            HwndSource.FromHwnd(handle).AddHook(new HwndSourceHook(WndProc));
            //强制刷新窗口非客户区
            SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, 0x0037);
            #endregion
        }

        private static void OnThemeType_Changed(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is VectorWindow window)
            {
                // 处理属性变更
                window.OnThemeType_Changed((WindowThemeType)e.OldValue, (WindowThemeType)e.NewValue);
            }
        }

        private static void OnVisualType_Changed(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is VectorWindow window)
            {
                // 处理属性变更
                window.OnVisualType_Changed((WindowVisualType)e.OldValue, (WindowVisualType)e.NewValue);
            }
        }

        private static void OnCornderType_Changed(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is VectorWindow window)
            {
                // 处理属性变更
                window.OnCornerType_Changed((WindowCornerPreference)e.OldValue, (WindowCornerPreference)e.NewValue);
            }
        }

        private void OnThemeType_Changed(WindowThemeType oldValue, WindowThemeType newValue)
        {
            switch (newValue)
            {
                case WindowThemeType.Light:
                    {
                        SetThemeMode(0);
                        break;
                    }
                case WindowThemeType.Dark:
                    {
                        SetThemeMode();
                        break;
                    }
                case WindowThemeType.FllowSystem:
                    {
                        UpdateThemeModeBySystem(new IntPtr(), true);
                        break;
                    }
            }

            if (GetType().ToString().EndsWith("MainView"))
            {
                var windowList = Application.Current.Windows.OfType<VectorWindow>();
                foreach (var window in windowList)
                {
                    window.ThemeType = newValue;
                }
            }
        }

        private void OnVisualType_Changed(WindowVisualType oldValue, WindowVisualType newValue)
        {
            UpdateWindowVisualStyle(newValue);
            if (GetType().ToString().EndsWith("MainView"))
            {
                var windowList = Application.Current.Windows.OfType<VectorWindow>();
                foreach (var window in windowList)
                {
                    window.VisualType = newValue;
                }
            }
        }

        private void OnCornerType_Changed(WindowCornerPreference oldValue, WindowCornerPreference newValue)
        {
            UpdateRoundCornerStyle(newValue);
            if (GetType().ToString().EndsWith("MainView"))
            {
                var windowList = Application.Current.Windows.OfType<VectorWindow>();
                foreach (var window in windowList)
                {
                    window.CornerPreference = newValue;
                }
            }
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

                        var source = PresentationSource.FromVisual(this);
                        if (source == null || source.CompositionTarget == null) return IntPtr.Zero;

                        // 将物理像素转为 WPF 逻辑坐标
                        Point mousePos = source.CompositionTarget.TransformFromDevice.Transform(mouseScreenPos);

                        if (MinimizeButton is not null)
                        {
                            Point btnTopLeft = MinimizeButton.PointToScreen(new Point(0, 0));
                            Point btnBottomRight = MinimizeButton.PointToScreen(new Point(MinimizeButton.ActualWidth, MinimizeButton.ActualHeight));

                            if (mouseScreenPos.X >= btnTopLeft.X && mouseScreenPos.X < btnBottomRight.X &&
                                mouseScreenPos.Y >= btnTopLeft.Y && mouseScreenPos.Y < btnBottomRight.Y)
                            {
                                handled = true;
                                return new IntPtr(HTMINBUTTON);
                            }
                        }

                        if (MaximizeButton is not null)
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

                        if (CloseButton is not null)
                        {
                            Point btnTopLeft = CloseButton.PointToScreen(new Point(0, 0));
                            Point btnBottomRight = CloseButton.PointToScreen(new Point(CloseButton.ActualWidth, CloseButton.ActualHeight));

                            if (mouseScreenPos.X >= btnTopLeft.X && mouseScreenPos.X < btnBottomRight.X &&
                                mouseScreenPos.Y >= btnTopLeft.Y && mouseScreenPos.Y < btnBottomRight.Y)
                            {
                                handled = true;
                                return new IntPtr(HTCLOSEBUTTON);
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
                        if (wParam.ToInt32() == HTMINBUTTON)
                        {
                            if (MinimizeButton != null)
                            {
                                MinimizeButton.Tag ??= "Hover"; // 触发 XAML 变色
                            }
                        }
                        else
                        {
                            if (MinimizeButton != null)
                            {
                                MinimizeButton.Tag = null;
                            }
                        }

                        if (wParam.ToInt32() == HTMAXBUTTON)
                        {
                            if (MaximizeButton != null)
                            {
                                MaximizeButton.Tag ??= "Hover"; // 触发 XAML 变色
                            }
                        }
                        else
                        {
                            if (MaximizeButton != null)
                            {
                                MaximizeButton.Tag = null;
                            }
                        }

                        if (wParam.ToInt32() == HTCLOSEBUTTON)
                        {
                            if (CloseButton != null)
                            {
                                CloseButton.Tag ??= "Hover"; // 触发 XAML 变色
                            }
                        }
                        else
                        {
                            if (CloseButton != null)
                            {
                                CloseButton.Tag = null;
                            }
                        }
                        break;
                    }
                case WM_NCMOUSELEAVE:
                    {
                        if (MaximizeButton != null)
                        {
                            MaximizeButton.Tag = null;
                        }
                        if(MinimizeButton != null)
                        {
                            MinimizeButton.Tag = null;
                        }
                        break;
                    }
                case WM_NCLBUTTONDOWN:
                    {
                        if (wParam.ToInt32() == HTMAXBUTTON || wParam.ToInt32() == HTCLOSEBUTTON || wParam.ToInt32() == HTMINBUTTON)
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

                        if (wParam.ToInt32() == HTMINBUTTON)
                        {
                            TemporarilyUseCaptionForAction(() => SystemCommands.MinimizeWindow(this));

                            handled = true;
                            return IntPtr.Zero;
                        }

                        if (wParam.ToInt32() == HTCLOSEBUTTON)
                        {
                            isClosing = true;
                            string typename = GetType().ToString();
                            TemporarilyUseCaptionForAction(Close);
                            handled = true;
                            return IntPtr.Zero;
                        }

                        break;
                    }
                case WM_NCLBUTTONDBLCLK:
                    {
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
                case WM_SETTINGCHANGE:
                    {
                        if (ThemeType is not WindowThemeType.FllowSystem)
                        {
                            return IntPtr.Zero;
                        }
                        UpdateThemeModeBySystem(lParam);
                        break;
                    }
                case WM_GETMINMAXINFO:
                    {
                        WmGetMinMaxInfo(hwnd, lParam);
                        handled = true; // 告诉系统我们已经处理了窗口大小限制
                        break;
                    }
                case WM_SYSCOMMAND:
                    {
                        int command = wParam.ToInt32() & 0xFFF0;
                        if (command == SC_MINIMIZE || command == SC_RESTORE || command == SC_MAXIMIZE)
                        {
                            TemporarilyUseCaptionForAction(null);

                            UpdateMaximizeButtons(command);

                            Activate();
                            handled = false;
                        }
                        break;
                    }
            }
            return IntPtr.Zero;
        }
        #endregion
    }
}