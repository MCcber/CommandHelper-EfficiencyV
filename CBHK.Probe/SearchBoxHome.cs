using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CBHK.CustomControl.Input;

namespace CBHK.Probe
{
    /// <summary>
    /// 主页搜索框（CBHK\View\MainView.xaml:61-71）的像素门禁。
    /// 判据只落在"用户看得见什么"上：TextBlock 有 Text、Path 有 Data 都不算证据 ——
    /// 空 Foreground 的 TextBlock 照样有 Text，几何原点偏掉的 Path 照样有 Data。
    /// 所以这里用真窗口 + RenderTargetBitmap 数墨迹像素。
    /// </summary>
    internal static class SearchBoxHome
    {
        // 与 Render 同一批字典：SearchBox 依赖 Magnifier(Icon)、CommonBackground(橙色主题)、
        // VectorFlatIconButtonStyle(Button)、VectorScrollViewerStyle(Container)。
        private static readonly string[] Dictionaries =
        [
            "ControlDictionary/Visual.xaml",
            "ControlDictionary/Common.xaml",
            "ControlDictionary/CommandBlockOrangeTheme.xaml",
            "ControlDictionary/Button.xaml",
            "ControlDictionary/CheckBox.xaml",
            "ControlDictionary/Icon.xaml",
            "ControlDictionary/ComboBox.xaml",
            "ControlDictionary/Input.xaml",
            "ControlDictionary/Container.xaml",
        ];

        private static int red;

        private static void That(bool ok, string criterion, string detail = "")
        {
            Console.WriteLine((ok ? "[绿] " : "[红] ") + criterion + (string.IsNullOrEmpty(detail) ? "" : " | " + detail));
            if (!ok) red++;
        }

        public static int Run(string outputDirectory = null)
        {
            var output = outputDirectory ?? Path.Combine(Path.GetTempPath(), "searchbox-home");
            Directory.CreateDirectory(output);
            if (Prepare() is not { } app) return 2;

            var background = (Brush)app.Resources["CommonBackground"];
            var icon = (Geometry)app.Resources["Magnifier"];
            Console.WriteLine("[信息] Magnifier.Bounds=" + icon.Bounds
                + "（原点不在 (0,0) 时，Viewbox 会按整个偏移后的包围盒去缩放）");

            // 逐字照抄 MainView.xaml:61-71 的属性
            var box = new SearchBox
            {
                Height = 45,
                FontSize = 20,
                SearchIcon = icon,
                SearchIconBrush = (Brush)app.Resources["CommonForeground"],
                SearchIconMargin = new Thickness(0),
                SearhIconHeight = 40,
                SearhIconWidth = 40,
                Style = (Style)app.Resources["SearchBoxStyle"],
                WaterMarkerMargin = new Thickness(2, 0, 0, 0),
                WaterMarkerText = "搜索生成器",
            };

            // 主页的形状：Grid 第一行 Auto 放搜索框，其余是 * 的内容区
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(box, 0);
            grid.Children.Add(box);

            var window = new Window
            {
                Width = 1080,
                Height = 240,
                Background = background,
                Content = grid,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -4000,
                Top = -4000,
            };
            window.Show();
            Pump();
            window.UpdateLayout();

            That(box.Template is not null, "模板套上了（SearchBoxStyle 能解析到）", "");
            That(Math.Abs(box.ActualHeight - 45) < 0.5 && box.ActualWidth > 100, "搜索框按 45 高铺开",
                "尺寸=" + box.ActualWidth.ToString("F0") + "x" + box.ActualHeight.ToString("F0"));

            // 根因判据：颜色类的 DP 到底有没有值（没有值 = 模板画不出颜色）
            That(box.WaterMarkerBrush is not null, "水印画刷有值（WaterMarkerBrush）",
                "WaterMarkerBrush=" + Describe(box.WaterMarkerBrush));
            That(box.LocateLineBrush is not null, "顶部定位线画刷有值（LocateLineBrush）",
                "LocateLineBrush=" + Describe(box.LocateLineBrush));

            var bar = Capture(box, 1);
            var barX4 = Capture(box, 4);
            var path = Path.Combine(output, "searchbox-home.png");
            Save(bar, path);
            Save(barX4, Path.Combine(output, "searchbox-home-x4.png"));
            Console.WriteLine("[信息] 已渲染 " + path + " 尺寸=" + bar.PixelWidth + "x" + bar.PixelHeight);

            var width = bar.PixelWidth;
            var height = bar.PixelHeight;
            var backgroundRgb = ((SolidColorBrush)background).Color;
            // 图标列：模板里 VectorFlatIconButton 宽 = 搜索框高 = 45，行 0 的 5px 定位线不算
            var iconArea = new Int32Rect(Math.Max(0, width - 45), 5, Math.Min(45, width), Math.Max(1, height - 5));
            var markArea = new Int32Rect(0, 5, Math.Max(1, width - 45), Math.Max(1, height - 5));
            // 图标按钮自己的 1px 边框也在图标列里，算"墨迹"会把边框误判成字形（bbox 恰好铺满 45x40）：
            // 所以字形只数深色像素（放大镜刷的是 CommonForeground #1E1E1E），并且四周内缩把边框整圈排除。
            var glyphArea = Inset(iconArea, 4);
            var iconInk = Ink(bar, iconArea, backgroundRgb);
            var glyphInk = Ink(bar, glyphArea, backgroundRgb, darkOnly: true);
            var markInk = Ink(bar, markArea, backgroundRgb);
            Console.WriteLine("[信息] 图标区 " + Rect(iconArea) + " 墨迹=" + iconInk.Count + "px bbox=" + iconInk.Box);
            Console.WriteLine("[信息] 图标字形 " + Rect(glyphArea) + " 深色墨迹=" + glyphInk.Count + "px bbox=" + glyphInk.Box);
            Console.WriteLine("[信息] 水印区 " + Rect(markArea) + " 墨迹=" + markInk.Count + "px bbox=" + markInk.Box);


            // 一整个放大镜画进 40x45：字形包围盒至少得有图标区一半大，缩成角落 3px 的小点不算"渲染"
            // （放大镜是描边图形，31x32 的轮廓约 140px 深色像素；偏移没修时这条是 0px）
            That(glyphInk.Count >= 100 && glyphInk.Height >= 18 && glyphInk.Width >= 18,
                "搜索图标真的画出放大镜（深色墨迹 ≥100px、包围盒 ≥18x18）",
                "墨迹=" + glyphInk.Count + "px bbox=" + glyphInk.Width + "x" + glyphInk.Height + " @" + glyphInk.Box);
            // 图标按钮比内容行高时，放大镜的柄会被控件底边切掉：用 4x 图看字形有没有贴到底边
            var glyphColumn4 = new Int32Rect(Math.Max(0, barX4.PixelWidth - 45 * 4), 5 * 4,
                Math.Min(45 * 4, barX4.PixelWidth), Math.Max(1, barX4.PixelHeight - 5 * 4));
            var glyph4 = Ink(barX4, Inset(glyphColumn4, 4), backgroundRgb, darkOnly: true);
            That(glyph4.Count > 0 && glyph4.MaxY < barX4.PixelHeight - 1 && glyph4.MinY > glyphColumn4.Y,
                "放大镜没被控件边界裁掉（字形不贴底边）",
                "4x 字形 y=" + glyph4.MinY + ".." + glyph4.MaxY + " 控件高=" + barX4.PixelHeight);
            // IconMargin 把 Viewbox 推出可用区时，环的最左那段（环中部左侧 4x 宽 2px 的竖带）会被裁掉，
            // 放大镜就成了 "C" 形：所以环中部左侧这几列必须有墨迹。
            // 判据用字形最左像素的位置：margin=2 时整个字形右移 2px（4x 8 个子像素），环左带就落进裁剪区
            var glyphLeftLimit = glyphColumn4.X + 4 * 4 + 2;
            That(glyph4.Count > 0 && glyph4.MinX <= glyphLeftLimit, "放大镜环左侧没被裁掉（字形贴到图标列左缘）",
                "4x 字形左缘 x=" + glyph4.MinX + " ≤ 上限 " + glyphLeftLimit);
            // 水印"搜索生成器"五个字：几百个墨迹像素，高度接近字号
            That(markInk.Count >= 200 && markInk.Height >= 12,
                "水印真的画出文字（墨迹 ≥200px、包围盒高 ≥12）",
                "墨迹=" + markInk.Count + "px bbox=" + markInk.Width + "x" + markInk.Height + " @" + markInk.Box);

            // 水印不能永远压在输入文字上：有字时让位
            if (box.Template?.FindName("waterMarker", box) is TextBlock watermark)
            {
                box.Text = "acacia";
                window.UpdateLayout();
                Pump();
                That(watermark.Visibility != Visibility.Visible, "有文字后水印让位（HasText 触发器）",
                    "Visibility=" + watermark.Visibility);
                box.Text = "";
                window.UpdateLayout();
                Pump();
                That(watermark.Visibility == Visibility.Visible, "清空后水印重新出现",
                    "Visibility=" + watermark.Visibility);

                // 再取一次像素：清空后水印必须回到画面上（触发器不能把颜色一起收起）
                var restored = Ink(Capture(box, 1), markArea, backgroundRgb);
                That(restored.Count >= 200, "清空后水印墨迹回来了", "墨迹=" + restored.Count + "px");
            }
            else
            {
                That(false, "模板里找得到 waterMarker", "");
            }

            window.Close();
            Pump();
            Console.WriteLine(red == 0 ? "[绿] --case searchbox-home 全绿" : "[红] --case searchbox-home 红 " + red + " 条");
            return red == 0 ? 0 : 1;
        }

        private static string Describe(Brush brush) => brush switch
        {
            null => "<null>",
            SolidColorBrush solid => "#" + solid.Color.ToString(),
            _ => brush.GetType().Name,
        };

        private static string Rect(Int32Rect area) => "(" + area.X + "," + area.Y + ") " + area.Width + "x" + area.Height;

        /// <summary>四周各内缩 px 像素，用来甩掉控件自己的 1px 边框。</summary>
        private static Int32Rect Inset(Int32Rect area, int px)
        {
            var width = Math.Max(1, area.Width - px * 2);
            var height = Math.Max(1, area.Height - px * 2);
            return new Int32Rect(area.X + px, area.Y + px, width, height);
        }

        private sealed class InkResult
        {
            public int Count { get; set; }
            public int MinX { get; set; } = int.MaxValue;
            public int MinY { get; set; } = int.MaxValue;
            public int MaxX { get; set; } = int.MinValue;
            public int MaxY { get; set; } = int.MinValue;

            public int Width => MaxX < MinX ? 0 : MaxX - MinX + 1;
            public int Height => MaxY < MinY ? 0 : MaxY - MinY + 1;
            public string Box => Width == 0 ? "<无>" : "(" + MinX + "," + MinY + ")-(" + MaxX + "," + MaxY + ")";
        }

        /// <summary>
        /// 数区域内"跟底色不一样"的像素：底色由模板 Grid 的 CommonBackground 决定。
        /// darkOnly=true 时不比底色，只数深色像素（放大镜刷的 #1E1E1E 那一路），用于把控件边框排除掉。
        /// </summary>
        private static InkResult Ink(BitmapSource bitmap, Int32Rect area, Color background, bool darkOnly = false)
        {
            var stride = bitmap.PixelWidth * 4;
            var buffer = new byte[stride * bitmap.PixelHeight];
            bitmap.CopyPixels(buffer, stride, 0);
            var result = new InkResult();
            for (var y = area.Y; y < area.Y + area.Height; y++)
            {
                for (var x = area.X; x < area.X + area.Width; x++)
                {
                    var at = y * stride + x * 4;
                    // Pbgra32：整条搜索框是不透明底色，alpha 不参与判断
                    if (darkOnly)
                    {
                        if (buffer[at] >= 100 || buffer[at + 1] >= 100 || buffer[at + 2] >= 100)
                        {
                            continue;
                        }
                    }
                    else if (Math.Abs(buffer[at] - background.B) <= 8
                        && Math.Abs(buffer[at + 1] - background.G) <= 8
                        && Math.Abs(buffer[at + 2] - background.R) <= 8)
                    {
                        continue;
                    }
                    result.Count++;
                    if (x < result.MinX) result.MinX = x;
                    if (y < result.MinY) result.MinY = y;
                    if (x > result.MaxX) result.MaxX = x;
                    if (y > result.MaxY) result.MaxY = y;
                }
            }
            return result;
        }

        /// <summary>把元素画成位图；scale 用来放大取像素证据（96dpi = 1:1）。</summary>
        private static RenderTargetBitmap Capture(FrameworkElement element, int scale)
        {
            var width = Math.Max(1, (int)Math.Ceiling(element.ActualWidth * scale));
            var height = Math.Max(1, (int)Math.Ceiling(element.ActualHeight * scale));
            var bitmap = new RenderTargetBitmap(width, height, 96 * scale, 96 * scale, PixelFormats.Pbgra32);
            bitmap.Render(element);
            return bitmap;
        }

        private static void Save(BitmapSource bitmap, string path)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(path);
            encoder.Save(stream);
        }

        private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);

        /// <summary>资源字典指回 CBHK 程序集（与 Render.Prepare 同一套 pack URI 处理，但不载 mcdoc schema）。</summary>
        private static Application Prepare()
        {
            var cbhkAssembly = typeof(SearchBox).Assembly;
            try
            {
                Application.ResourceAssembly = cbhkAssembly;
            }
            catch (InvalidOperationException)
            {
                typeof(Application)
                    .GetField("_resourceAssembly", BindingFlags.NonPublic | BindingFlags.Static)
                    ?.SetValue(null, cbhkAssembly);
            }
            var app = Application.Current ?? new Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            foreach (var path in Dictionaries)
            {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/CBHK;component/" + path, UriKind.Absolute),
                });
            }
            return app;
        }
    }
}
