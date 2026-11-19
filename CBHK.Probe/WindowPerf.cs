using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CBHK.Model.Constant;
using CBHK.Utility.Data.Mcdoc;

namespace CBHK.Probe
{
    /// <summary>
    /// [perf-probe] 量「点开维度类型页面」那一段 WPF 成本：资源字典、窗口构造（BAML）、
    /// VM 构造、Show + 首帧布局、首帧渲染。窗口开在屏幕外，量完即关。
    /// 每档跑三次取最小值（这台机器单次测量能差 50%）。
    /// </summary>
    internal static class WindowPerf
    {
        private const int Runs = 3;

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
            "ControlDictionary/ViewModel/Recipe.xaml",
        ];

        public static int Open(string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);

            var watch = Stopwatch.StartNew();
            var app = EnsureApplication();
            watch.Stop();
            Console.WriteLine("[计时] A 应用资源字典（" + Dictionaries.Length + " 份）= " + Ms(watch) + "ms");

            // 行尾那颗按钮的图标是代码里按 key 取的（菜单态 ⋮ / 就地常驻时换动作图标），这里钉一次资源确实在
            foreach (var key in new[] { "MdocMoreGeometry", "TreeNodePlusGeometry", "MdocDuplicateGeometry", "TreeNodeMinusGeometry" })
            {
                Console.WriteLine("[资源] " + key + " = " + (app.TryFindResource(key) is Geometry geometry ? "命中 " + geometry.Bounds : "缺失"));
            }

            McdocWriteTrace.StatsEnabled = true;
            watch.Restart();
            var resource = new Resource();
            RealSchema.Load(resource.McdocTypes, RealSchema.LocateMcdocRoot());
            resource.LoadRegistries(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\CBHK\bin\Debug\net8.0-windows10.0.22621.0\Resource\Registries"));
            watch.Stop();
            Console.WriteLine("[计时] B schema + 注册表 = " + Ms(watch) + "ms 类型=" + resource.McdocTypes.TypeDefCount);

            Resource.PerfResetGetIds();
            watch.Restart();
            var vm = new CBHK.ViewModel.Generator.DimensionTypeViewModel(resource);
            watch.Stop();
            Console.WriteLine("[计时] C VM 构造（含默认文档）= " + Ms(watch) + "ms 行=" + vm.Rows?.Count);
            Console.WriteLine("[计数] 建这一次页面期间 GetIds 调用=" + Resource.PerfGetIdsCalls
                + " 累计=" + (Resource.PerfGetIdsTicks * 1000.0 / Stopwatch.Frequency).ToString("F1") + "ms");
            foreach (var pair in Resource.PerfGetIdsByRegistry.OrderByDescending(item => item.Value).Take(5))
            {
                Resource.PerfGetIdsMsByRegistry.TryGetValue(pair.Key, out long ms);
                Console.WriteLine("   " + pair.Key + " 调用x" + pair.Value + " " + ms + "ms");
            }

            var vmEmpty = new CBHK.ViewModel.Generator.DimensionTypeViewModel(resource);
            vmEmpty.Rows.Clear();

            McdocRow.DeferEditors = true;
            var vmDeferred = new CBHK.ViewModel.Generator.DimensionTypeViewModel(resource);
            McdocRow.DeferEditors = false;

            // 预热：WPF 第一次建 HWND 要初始化 MIL/DWM/字体缓存，不能算进任何一档
            ShowOnce("预热：空窗口", () => new Window { Width = 400, Height = 300, Content = new Border() }, null, runs: 1);
            ShowOnce("预热：真文档（丢弃，烫掉一次性开销）", () => new CBHK.View.Generator.DimensionTypeView(), vm, runs: 1);

            var results = new List<(string Label, double Built, double Shown, double Pumped)>
            {
                ShowOnce("空窗口（地板价）", () => new Window { Width = 1080, Height = 700, Content = new Border() }, null),
                ShowOnce("真窗口·无 DataContext", () => new CBHK.View.Generator.DimensionTypeView(), null),
                ShowOnce("真文档·前排副本", () => new CBHK.View.Generator.DimensionTypeView(), vm, runs: 5),
                ShowOnce("真窗口·真文档", () => new CBHK.View.Generator.DimensionTypeView(), vm, outputDirectory, "dimension-window.png", runs: 5),
                ShowOnce("真窗口·延迟编辑器", () => new CBHK.View.Generator.DimensionTypeView(), vmDeferred),
                ShowOnce("只树·17 行（真模板）", () => TreeWindow(vm.Rows.ToList()), null),
                ShowOnce("只树·17 行（延迟编辑器）", () => TreeWindow(vmDeferred.Rows.ToList()), null),
                ShowOnce("只树·17 行（简易模板）", () => TreeWindow(vm.Rows.ToList(), SimpleRowTemplate(), true), null),
                ShowOnce("只 chrome", () => new Window
                {
                    Width = 780,
                    Height = 700,
                    Content = new ContentControl
                    {
                        Content = vm,
                        ContentTemplate = app.Resources["McdocGeneratorChrome"] as DataTemplate,
                    },
                }, null),
                ShowOnce("真窗口·清空行（树以外）", () => new CBHK.View.Generator.DimensionTypeView(), vmEmpty),
                ShowOnce("真窗口·空行·去 chrome", () => new CBHK.View.Generator.DimensionTypeView(), vmEmpty,
                    prepare: window => Collapse(window, parts => parts.Chrome)),
                ShowOnce("真窗口·真文档·去树", () => new CBHK.View.Generator.DimensionTypeView(), vm,
                    prepare: window => Collapse(window, parts => parts.Tree)),
                ShowOnce("真窗口·真文档·去 JSON", () => new CBHK.View.Generator.DimensionTypeView(), vm,
                    prepare: window => Collapse(window, parts => parts.Json)),
                ShowOnce("真窗口·真文档·只 chrome", () => new CBHK.View.Generator.DimensionTypeView(), vm,
                    prepare: window => { Collapse(window, parts => parts.Tree); Collapse(window, parts => parts.Json); }),
                ShowOnce("真窗口·真文档·只树（无 chrome 无 JSON）", () => new CBHK.View.Generator.DimensionTypeView(), vm,
                    prepare: window => { Collapse(window, parts => parts.Chrome); Collapse(window, parts => parts.Json); }),
                ShowOnce("真窗口·真文档·软件渲染全文", () => new CBHK.View.Generator.DimensionTypeView(), vm,
                    prepare: window => RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly),
                ShowOnce("真窗口·真文档·树无源·去 JSON", () => new CBHK.View.Generator.DimensionTypeView(), vm,
                    prepare: window => { var parts = Parts(window); if (parts?.Tree is not null) parts.Tree.ItemsSource = null; Collapse(window, parts => parts.Json); }),
                ShowOnce("真文档·带 PNG 渲染", () => new CBHK.View.Generator.DimensionTypeView(), vm, outputDirectory, "dimension-window-extra.png"),
                ShowOnce("真文档·原样再来一次", () => new CBHK.View.Generator.DimensionTypeView(), vm),
                ShowOnce("真文档·原样再来两次", () => new CBHK.View.Generator.DimensionTypeView(), vm),
                ShowOnce("真文档·原样再来三次", () => new CBHK.View.Generator.DimensionTypeView(), vm),
                ShowOnce("真文档·JSON 不换行", () => new CBHK.View.Generator.DimensionTypeView(), vm,
                    prepare: window => { var parts = Parts(window); if (parts?.Json is not null) parts.Json.WordWrap = false; }),
                ShowOnce("真文档·左列定宽 648", () => new CBHK.View.Generator.DimensionTypeView(), vm,
                    prepare: window => { var parts = Parts(window); if (parts?.Root is not null) parts.Root.ColumnDefinitions[0].Width = new GridLength(648); }),
                ShowOnce("真文档·定宽+不换行", () => new CBHK.View.Generator.DimensionTypeView(), vm,
                    prepare: window =>
                    {
                        var parts = Parts(window);
                        if (parts?.Json is not null) parts.Json.WordWrap = false;
                        if (parts?.Root is not null) parts.Root.ColumnDefinitions[0].Width = new GridLength(648);
                    }),
                ShowOnce("真窗口·真文档·树无源", () => new CBHK.View.Generator.DimensionTypeView(), vm,
                    prepare: window => { var parts = Parts(window); if (parts?.Tree is not null) parts.Tree.ItemsSource = null; }),
                ShowOnce("真窗口·真文档·软件渲染", () => new CBHK.View.Generator.DimensionTypeView(), vm,
                    prepare: window => { var parts = Parts(window); if (parts?.Tree is not null) parts.Tree.ItemsSource = null; RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly; }),
                ShowOnce("只树·17 行（绑定真集合）", () => TreeWindow(vm.Rows), null),
                ShowOnce("只树·17 行（窄 620 宽）", () => NarrowTreeWindow(vm.Rows.ToList()), null),
                ShowOnce("170 个 VectorFlatIconButton", () => ButtonsWindow(170, true), null),
                ShowOnce("170 个 Border（同尺寸）", () => ButtonsWindow(170, false), null),
                ShowOnce("170 个 VectorTextComboBox（158 项）", () => CombosWindow(170, 158), null),
                ShowOnce("170 个 VectorTextComboBox（无源）", () => CombosWindow(170, 0), null),
                ShowOnce("只 JSON 文本（AvalonEdit）", () => new Window
                {
                    Width = 480,
                    Height = 700,
                    Content = new CBHK.Utility.Data.JsonText.JsonTextEditor { Text = TemplateText() },
                }, null),
            };

            Console.WriteLine("[汇总] 每档 " + Runs + " 次取最小（含窗口首帧布局）：");
            foreach (var item in results)
            {
                Console.WriteLine("   " + item.Label.PadRight(24, '　')
                    + " 构造=" + item.Built.ToString("F1") + "ms Show=" + (item.Shown - item.Pumped).ToString("F1")
                    + "ms 泵=" + item.Pumped.ToString("F1") + "ms 合计=" + item.Shown.ToString("F1") + "ms");
            }

            RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.Default;
            Console.WriteLine("[行数] 真文档=" + vm.Rows?.Count + " 清空副本=" + vmEmpty.Rows?.Count + "（清空后再绑定会不会被填回来，看这里）");
            Console.WriteLine("[布局] 只量根 Grid 的 Measure/Arrange/UpdateLayout（不含 HWND 与渲染），每档 3 次取最小：");
            foreach (var item in new (string Label, Func<FrameworkElement> Build)[]
            {
                ("无 VM", () => LayoutRoot(new CBHK.View.Generator.DimensionTypeView(), null)),
                ("真 VM 全文", () => LayoutRoot(new CBHK.View.Generator.DimensionTypeView(), vm)),
                ("真 VM 去树", () => LayoutRoot(new CBHK.View.Generator.DimensionTypeView(), vm, parts => parts.Tree.Visibility = Visibility.Collapsed)),
                ("真 VM 去 JSON", () => LayoutRoot(new CBHK.View.Generator.DimensionTypeView(), vm, parts => parts.Json.Visibility = Visibility.Collapsed)),
                ("真 VM 去 chrome", () => LayoutRoot(new CBHK.View.Generator.DimensionTypeView(), vm, parts => parts.Chrome.Visibility = Visibility.Collapsed)),
                ("空行 VM 全文", () => LayoutRoot(new CBHK.View.Generator.DimensionTypeView(), vmEmpty)),
            })
            {
                Console.WriteLine("   " + item.Label.PadRight(16, '　') + " = " + LayoutMs(item.Build) + "ms");
            }

            CensusRowVisuals(vm);
            CensusButtons(vm);
            MeasureRowGetters(vm);

            app = null;
            Console.WriteLine("[绿] --render-window 跑完");
            return 0;
        }

        private static string Ms(Stopwatch watch) => watch.Elapsed.TotalMilliseconds.ToString("F1");

        private sealed class ViewParts
        {
            public Grid Root;
            public ContentControl Chrome;
            public TextBlock Status;
            public McdocTreeRowView Tree;
            public CBHK.Utility.Data.JsonText.JsonTextEditor Json;
        }

        /// <summary>把窗口内容拆成 chrome / 状态栏 / 树 / JSON 编辑器四件，供消融用。</summary>
        private static ViewParts Parts(DependencyObject root)
        {
            if (root is not Window window || window.Content is not Grid grid) return null;
            var dock = grid.Children.OfType<DockPanel>().FirstOrDefault();
            var parts = new ViewParts
            {
                Root = grid,
                Chrome = dock?.Children.OfType<ContentControl>().FirstOrDefault(),
                Status = dock?.Children.OfType<TextBlock>().FirstOrDefault(),
                Tree = dock?.Children.OfType<McdocTreeRowView>().FirstOrDefault(),
                Json = grid.Children.OfType<CBHK.Utility.Data.JsonText.JsonTextEditor>().FirstOrDefault(),
            };
            return parts;
        }

        private static void Collapse(Window window, Func<ViewParts, FrameworkElement> pick)
        {
            var parts = Parts(window);
            if (parts is null) return;
            var element = pick(parts);
            if (element is not null) element.Visibility = Visibility.Collapsed;
        }

        /// <summary>只跑内容根的布局（Measure→Arrange→UpdateLayout），不含 HWND 与首帧渲染。</summary>
        private static double LayoutMs(Func<FrameworkElement> build, int runs = Runs)
        {
            var best = double.MaxValue;
            for (var run = 0; run < runs; run++)
            {
                var element = build();
                if (element is null) return -1;
                var watch = Stopwatch.StartNew();
                element.Measure(new Size(1080, 700));
                element.Arrange(new Rect(0, 0, 1080, 700));
                element.UpdateLayout();
                watch.Stop();
                best = Math.Min(best, watch.Elapsed.TotalMilliseconds);
            }
            return Math.Round(best, 1);
        }

        private static FrameworkElement LayoutRoot(CBHK.View.Generator.DimensionTypeView view, object dataContext, Action<ViewParts> tweak = null)
        {
            view.DataContext = dataContext;
            tweak?.Invoke(Parts(view));
            return view.Content as FrameworkElement;
        }

        /// <summary>建窗口 → 挂 DataContext → Show → 首帧布局 → 关闭；跑多次取最小值。</summary>
        private static (string Label, double Built, double Shown, double Pumped) ShowOnce(
            string label, Func<Window> factory, object dataContext, string outputDirectory = null, string fileName = null, int runs = Runs,
            Action<Window> prepare = null)
        {
            var builtMin = double.MaxValue;
            var shownMin = double.MaxValue;
            var pumpMin = double.MaxValue;
            for (var run = 0; run < runs; run++)
            {
                var watch = Stopwatch.StartNew();
                Window window;
                try
                {
                    window = factory();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[异常] " + label + " 构造失败 " + ex.GetType().Name + "：" + ex.Message);
                    return (label, -1, -1, -1);
                }
                watch.Stop();
                builtMin = Math.Min(builtMin, watch.Elapsed.TotalMilliseconds);

                window.DataContext = dataContext;
                prepare?.Invoke(window);
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Left = -4000;
                window.Top = -4000;
                window.ShowInTaskbar = false;

                var stamps = new List<(string Kind, double At)>();
                if (run == 0 && label.Contains("真文档"))
                {
                    var stopwatch = watch;
                    window.LayoutUpdated += (_, _) => stamps.Add(("布局遍", stopwatch.Elapsed.TotalMilliseconds));
                    window.AddHandler(FrameworkElement.LoadedEvent,
                        new RoutedEventHandler((_, _) => stamps.Add(("Loaded", stopwatch.Elapsed.TotalMilliseconds))), true);
                    window.AddHandler(FrameworkElement.SizeChangedEvent,
                        new RoutedEventHandler((_, _) => stamps.Add(("尺寸变", stopwatch.Elapsed.TotalMilliseconds))), true);
                }
                McdocWriteTrace.ResetStages();
                watch.Restart();
                window.Show();
                if (run == 0 && label.Contains("真文档"))
                {
                    var passWatch = Stopwatch.StartNew();
                    window.UpdateLayout();
                    var idlePass = passWatch.Elapsed.TotalMilliseconds;
                    passWatch.Restart();
                    Parts(window)?.Tree?.InvalidateMeasure();
                    window.UpdateLayout();
                    var forcedPass = passWatch.Elapsed.TotalMilliseconds;
                    Console.WriteLine("[Show 内] 再跑一遍布局=" + idlePass.ToString("F1") + "ms 手动失效树后一遍=" + forcedPass.ToString("F1") + "ms");
                }
                if (stamps.Count > 0)
                {
                    foreach (var group in stamps.GroupBy(stamp => stamp.Kind))
                    {
                        Console.WriteLine("[Show 内] " + group.Key + " x" + group.Count()
                            + " 首=" + group.First().At.ToString("F1") + "ms 末=" + group.Last().At.ToString("F1") + "ms");
                    }
                    var timeline = string.Join(" ", stamps.OrderBy(stamp => stamp.At).Take(24)
                        .Select(stamp => stamp.Kind + "@" + stamp.At.ToString("F0")));
                    Console.WriteLine("[Show 内] 前 24 个事件：" + timeline);
                }
                watch.Stop();
                var showMs = watch.Elapsed.TotalMilliseconds;
                watch.Restart();
                Pump();
                watch.Stop();
                pumpMin = Math.Min(pumpMin, watch.Elapsed.TotalMilliseconds);
                shownMin = Math.Min(shownMin, showMs + watch.Elapsed.TotalMilliseconds);

                if (run == 0 && label.Contains("真窗口·真文档"))
                {
                    Console.WriteLine("[阶段] " + label + "：proj.reset=" + McdocWriteTrace.StageMs("proj.reset").ToString("F1")
                        + "ms vm.loadTree=" + McdocWriteTrace.StageMs("vm.loadTree").ToString("F1")
                        + "ms doc.load=" + McdocWriteTrace.StageMs("doc.load").ToString("F1") + "ms");
                    Console.WriteLine("[阶段报告] " + McdocWriteTrace.StageReport());
                    var visualTotal = Descendants(window).Count();
                    var rowCount = (dataContext as CBHK.ViewModel.Generator.McdocGeneratorViewModelBase)?.Rows?.Count ?? 0;
                    Console.WriteLine("[视觉] 整窗 visual 合计=" + visualTotal + " 行=" + rowCount
                        + " 每行=" + ((double)visualTotal / Math.Max(1, rowCount)).ToString("F1"));
                    if (outputDirectory is not null && fileName is not null && window.ActualWidth > 0)
                    {
                        var renderWatch = Stopwatch.StartNew();
                        var bitmap = new RenderTargetBitmap(
                            Math.Max(1, (int)Math.Ceiling(window.ActualWidth)),
                            Math.Max(1, (int)Math.Ceiling(window.ActualHeight)), 96, 96, PixelFormats.Pbgra32);
                        bitmap.Render(window);
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmap));
                        var path = Path.Combine(outputDirectory, fileName);
                        using (var stream = File.Create(path))
                        {
                            encoder.Save(stream);
                        }
                        Console.WriteLine("[信息] 已渲染 " + path + " 尺寸=" + window.ActualWidth + "x" + window.ActualHeight
                            + " 软件重绘=" + renderWatch.Elapsed.TotalMilliseconds.ToString("F1") + "ms");
                    }
                }

                window.Close();
                Pump();
            }
            return (label, builtMin, shownMin, pumpMin);
        }

        private static Window TreeWindow(IEnumerable<McdocRow> rows, DataTemplate template = null, bool useContainerStyle = true)
        {
            var app = Application.Current;
            var view = new McdocTreeRowView { ItemsSource = rows };
            if (template is null)
            {
                view.ItemTemplateSelector = app.Resources["McdocRowTemplateSelector"] as DataTemplateSelector;
            }
            else
            {
                view.ItemTemplate = template;
            }
            if (useContainerStyle)
            {
                view.ItemContainerStyle = app.Resources["McdocRowItemContainerStyle"] as Style;
            }
            return new Window { Width = 780, Height = 700, Content = view };
        }

        /// <summary>与真实页面同宽（1080 里树占 1.5/2.5 ≈ 620）的树窗口。</summary>
        private static Window NarrowTreeWindow(IEnumerable<McdocRow> rows)
        {
            var window = TreeWindow(rows);
            window.Width = 620;
            return window;
        }

        /// <summary>同数量的 VectorFlatIconButton 全展开，量"一个按钮实例+模板"多少钱。</summary>
        private static Window ButtonsWindow(int count, bool iconButtons)
        {
            var panel = new StackPanel();
            var style = Application.Current.Resources["McdocRowExpanderStyle"] as Style;
            for (var index = 0; index < count; index++)
            {
                panel.Children.Add(iconButtons
                    ? new CBHK.CustomControl.VectorButton.VectorFlatIconButton { Style = style }
                    : new Border { Width = 34, Height = 34, BorderThickness = new Thickness(1), BorderBrush = Brushes.Black });
            }
            return new Window { Width = 780, Height = 700, Content = new ScrollViewer { Content = panel } };
        }

        private static string TemplateText()
        {
            var path = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\CBHK\bin\Debug\net8.0-windows10.0.22621.0\Resource\Configs\DimensionType\Data\Template\empty.json"));
            return File.Exists(path) ? File.ReadAllText(path) : "{}";
        }

        /// <summary>一行只有一个 TextBlock 的简易模板：用来把"行模板成本"和"列表/皮肤成本"分开。</summary>
        private static DataTemplate SimpleRowTemplate()
        {
            var factory = new FrameworkElementFactory(typeof(TextBlock));
            factory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(McdocRow.DisplayLabel)));
            return new DataTemplate { VisualTree = factory };
        }

        /// <summary>把行摊开后数一遍 visual，按类型归类。</summary>
        private static Window CombosWindow(int count, int items)
        {
            var panel = new StackPanel();
            var source = items > 0 ? Enumerable.Range(0, items).Select(index => "minecraft:item_" + index).ToList() : null;
            for (var index = 0; index < count; index++)
            {
                var combo = new CBHK.CustomControl.VectorComboBox.VectorTextComboBox { Width = 200, Height = 28 };
                if (source is not null) combo.ItemsSource = source;
                panel.Children.Add(combo);
            }
            return new Window { Width = 520, Height = 640, Content = new ScrollViewer { Content = panel } };
        }

        private static void CensusRowVisuals(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm)
        {
            var view = new McdocTreeRowView
            {
                Width = 780,
                ItemsSource = vm.Rows.ToList(),
                ItemTemplateSelector = Application.Current.Resources["McdocRowTemplateSelector"] as DataTemplateSelector,
                ItemContainerStyle = Application.Current.Resources["McdocRowItemContainerStyle"] as Style,
            };
            var host = new Border { Width = 780, Child = view };
            host.Measure(new Size(780, double.PositiveInfinity));
            host.Arrange(new Rect(0, 0, 780, host.DesiredSize.Height));
            host.UpdateLayout();

            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            var visible = new Dictionary<string, int>(StringComparer.Ordinal);
            var total = 0;
            foreach (var item in Descendants(view).Where(item => item is ListBoxItem))
            {
                foreach (var element in Descendants(item).Where(element => element is FrameworkElement))
                {
                    var name = element.GetType().Name;
                    counts.TryGetValue(name, out int seen);
                    counts[name] = seen + 1;
                    if (element is UIElement ui && ui.Visibility == Visibility.Visible)
                    {
                        visible.TryGetValue(name, out int seenVisible);
                        visible[name] = seenVisible + 1;
                    }
                    total++;
                }
            }
            Console.WriteLine("[视觉] 行=" + view.Items.Count + " visual 合计=" + total + " 平均=" + (total / Math.Max(1, view.Items.Count)) + " 元素/行；最多的几类：");
            foreach (var pair in counts.OrderByDescending(item => item.Value).Take(24))
            {
                visible.TryGetValue(pair.Key, out int seenVisible);
                Console.WriteLine("   " + pair.Key + " 每行" + ((double)pair.Value / Math.Max(1, view.Items.Count)).ToString("F1")
                    + "（合计 " + pair.Value + "，其中可见 " + seenVisible + " / 折叠 " + (pair.Value - seenVisible) + "）");
            }
        }

        /// <summary>数一下每行那十个图标按钮里，真正摊开模板的有几个（折叠的不摊模板）。</summary>
        private static void CensusButtons(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm)
        {
            var view = new McdocTreeRowView
            {
                Width = 780,
                ItemsSource = vm.Rows.ToList(),
                ItemTemplateSelector = Application.Current.Resources["McdocRowTemplateSelector"] as DataTemplateSelector,
                ItemContainerStyle = Application.Current.Resources["McdocRowItemContainerStyle"] as Style,
            };
            var host = new Border { Width = 780, Child = view };
            host.Measure(new Size(780, double.PositiveInfinity));
            host.Arrange(new Rect(0, 0, 780, host.DesiredSize.Height));
            host.UpdateLayout();

            var templated = 0;
            var bare = 0;
            foreach (var item in Descendants(view).Where(item => item is ListBoxItem))
            {
                foreach (var button in Descendants(item).OfType<CBHK.CustomControl.VectorButton.VectorFlatIconButton>())
                {
                    if (VisualTreeHelper.GetChildrenCount(button) > 0) templated++;
                    else bare++;
                }
            }
            Console.WriteLine("[视觉] 图标按钮：已摊开模板 " + templated + " / 只有元素没有模板 " + bare);
        }

        private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
        {
            var stack = new Stack<DependencyObject>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                yield return current;
                var count = VisualTreeHelper.GetChildrenCount(current);
                for (var i = 0; i < count; i++)
                {
                    stack.Push(VisualTreeHelper.GetChild(current, i));
                }
            }
        }

        private static readonly string[] RowBindingNames =
        [
            "AncestorLineWidth", "ChildGuideVisibility", "ChildGuideMargin", "IsExpanded", "NumberText",
            "DecreaseCommand", "IncreaseCommand", "RegenerateSeedCommand", "RandomButtonVisibility",
            "RawJson", "AnyTypeKey", "PreviewText", "UnknownDeleteCommand", "UnknownDeleteVisibility",
            "RemoveEntryCommand", "RemoveEntryVisibility", "ColorSwatchBrush", "ColorBadgeVisibility",
            "ToggleExpandedCommand", "ExpanderVisibility", "ToggleDocCommand", "HasDocumentation",
            "KeyPlateVisibility", "DisplayLabel", "IsDocOpen", "Documentation", "MoveUpCommand",
            "MoveVisibility", "MoveDownCommand", "DuplicateCommand", "DuplicateVisibility",
            "RemoveSelfCommand", "RemoveSelfVisibility", "Indent", "AddTailCommand", "AddTailVisibility",
            "KeyPlateColumnSpan", "ErrorBorderBrush", "EditorContent", "DiagnosticBadgeVisibility",
            "ToggleDiagnosticsCommand", "ErrorBrush", "DiagnosticSummary", "IsDiagnosticsOpen",
            "Diagnostics", "ActionGroupMargin", "AddTopCommand", "ListAddTopVisibility",
            "SetDefaultCommand", "SetDefaultVisibility", "ClearCommand", "ClearVisibility",
            "UnknownBadgeVisibility", "ListButtonsVisibility", "KeyLabelVisibility", "KeyTextVisibility",
            "KeyComboVisibility", "AddEntryVisibility", "RenameKeyCommand", "RenameVisibility",
            "MoreLabel", "ShowMoreCommand", "ShowAllCommandRow", "Tooltip", "Label",
        ];

        /// <summary>把行模板绑到的属性逐个取一遍，找出自身就很贵的 getter。</summary>
        private static void MeasureRowGetters(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm)
        {
            var rows = vm.Rows;
            var totals = new Dictionary<string, (double Ms, int Hit)>(StringComparer.Ordinal);
            foreach (var name in RowBindingNames)
            {
                var watch = Stopwatch.StartNew();
                var hit = 0;
                foreach (var row in rows)
                {
                    var property = row?.GetType().GetProperty(name);
                    if (property is null) continue;
                    try { _ = property.GetValue(row); hit++; } catch { }
                }
                watch.Stop();
                totals[name] = (watch.Elapsed.TotalMilliseconds, hit);
            }
            var order = totals.Keys.ToList();
            order.Sort((left, right) => totals[right].Ms.CompareTo(totals[left].Ms));
            Console.WriteLine("[取值] 行=" + rows.Count + " 最贵的 getter：");
            foreach (var name in order.Take(8))
            {
                Console.WriteLine("   " + name + " = " + totals[name].Ms.ToString("F2") + "ms / " + totals[name].Hit + " 次");
            }
        }

        /// <summary>
        /// 虚拟化实测：把 1000 / 5000 行的**真行集合**喂给同一个 McdocTreeRowView，
        /// 数「容器实例数」而不是行数——虚拟化生没生效，看的是这个差。
        /// 再滚到底数一次，确认 Recycling 只留一屏、且容器被复用（首个实到下标变了）。
        /// </summary>
        public static int Virtualization(string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);
            EnsureApplication();
            var resource = new Resource();
            RealSchema.Load(resource.McdocTypes, RealSchema.LocateMcdocRoot());
            resource.LoadRegistries(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\CBHK\bin\Debug\net8.0-windows10.0.22621.0\Resource\Registries"));

            var viewport = (Height: 640.0, RowHeight: 34.0);

            // 预热：第一个带行的窗口有一次性开销（之前量过 ~270ms），不预热会把两档的差算进数据
            foreach (var count in new[] { 200 })
            {
                var warm = BuildRows(resource, count);
                if (warm is null) return 1;
                MeasureRealized("预热 · " + count + " 行（丢弃）", warm, viewport, outputDirectory, false);
            }

            foreach (var count in new[] { 1000, 5000 })
            {
                var rows = BuildRows(resource, count);
                if (rows is null) return 1;
                MeasureRealized(count + " 行", rows, viewport, outputDirectory, true);
            }
            return 0;
        }

        /// <summary>造一份「infiniburn 里有 count 个方块 id」的维度类型文档，展开后取出全部行。</summary>
        private static CBHK.ViewModel.Generator.DimensionTypeViewModel BuildRows(Resource resource, int count)
        {
            // attributes 是自由键的 map（[string]: any），键名不受注册表约束，
            // 所以能造出任意行数；infiniburn 那种 #[id="block"] 列表每项都要在注册表里存在，造不出 1000 行。
            var builder = new System.Text.StringBuilder();
            builder.Append("{\n  \"attributes\": {\n");
            for (var index = 0; index < count; index++)
            {
                if (index > 0) builder.Append(",\n");
                builder.Append("    \"probe:key_").Append(index).Append("\": {}");
            }
            builder.Append("\n  }\n}");

            var vm = new CBHK.ViewModel.Generator.DimensionTypeViewModel(resource);
            vm.LoadMcdocTree(builder.ToString());
            var before = vm.Rows.Count;

            var document = DocumentOf(vm);
            var list = document?.FindNode("$.attributes");
            if (list is null)
            {
                Console.WriteLine("[虚拟化] 找不到 $.infiniburn（LoadMcdocTree 之后行数=" + before + "）");
                return null;
            }
            var expand = Stopwatch.StartNew();
            ExpandAll(new[] { list });
            var projector = ProjectorOf(vm);
            projector?.Reset(document.ProjectionRoots);
            expand.Stop();
            Console.WriteLine("[虚拟化] 造 " + count + " 项：展开前=" + before + " 行 展开后=" + vm.Rows.Count
                + " 行 建树+投影=" + Ms(expand) + "ms");
            if (Environment.GetEnvironmentVariable("MCDOC_PROBE_DEBUG") == "1")
            {
                Console.WriteLine("   状态=" + (vm.MdocStatus ?? "<null>"));
                Console.WriteLine("   根子级=" + (document?.Nodes?.Count ?? -1) + " 诊断=" + (document?.Diagnostics?.Count ?? -1));
                Console.WriteLine("   前 12 行=" + string.Join(" | ", vm.Rows.Take(12).Select(row => row.DisplayLabel + ":" + (row.Node?.Json?.Kind.ToString() ?? "-"))));
                Console.WriteLine("   attributes 子级=" + (list.Children?.Count ?? -1) + " 展开=" + list.IsExpanded);
            }
            return vm;
        }

        private static void MeasureRealized(string label, CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm,
            (double Height, double RowHeight) viewport, string outputDirectory, bool scroll)
        {
            var app = Application.Current;
            var view = new McdocTreeRowView
            {
                ItemsSource = vm.Rows,
                ItemTemplateSelector = app.Resources["McdocRowTemplateSelector"] as DataTemplateSelector,
                ItemContainerStyle = app.Resources["McdocRowItemContainerStyle"] as Style,
            };
            var window = new Window
            {
                Width = 780,
                Height = viewport.Height + 60,
                Left = -4000,
                Top = -4000,
                ShowInTaskbar = false,
                Content = view,
            };
            var watch = Stopwatch.StartNew();
            window.Show();
            Pump();
            watch.Stop();

            // 只数这个 ListBox 自己的容器：Descendants 会把行内部的 ComboBox 选项也当 ListBoxItem 捞出来
            var containers = Enumerable.Range(0, vm.Rows.Count)
                .Select(index => view.ItemContainerGenerator.ContainerFromIndex(index) as ListBoxItem)
                .Where(item => item is not null)
                .ToList();
            var visuals = Descendants(view).Count(item => item is System.Windows.Media.Visual);
            var scroller = Descendants(view).OfType<ScrollViewer>().FirstOrDefault();
            var extent = scroller?.ExtentHeight ?? 0;
            var ports = scroller?.ViewportHeight ?? 0;
            Console.WriteLine("[" + label + "] 总行数=" + vm.Rows.Count
                + " 实到容器=" + containers.Count
                + " visual=" + visuals
                + " 视口=" + ports.ToString("F0") + " 可容纳≈" + (ports / viewport.RowHeight).ToString("F1") + " 行"
                + " 内容高=" + extent.ToString("F0") + " Show=" + Ms(watch) + "ms"
                + " 滚动条=" + (scroller is null ? "<无>" : scroller.GetType().Name)
                + " CanContentScroll=" + (scroller?.CanContentScroll.ToString() ?? "-")
                + " 面板=" + (scroller?.Content as System.Windows.Controls.Panel)?.GetType().Name
                + " IsVirtualizing=" + System.Windows.Controls.VirtualizingPanel.GetIsVirtualizing(view)
                + " 模式=" + System.Windows.Controls.VirtualizingPanel.GetVirtualizationMode(view)
                + " 滚动单位=" + System.Windows.Controls.VirtualizingPanel.GetScrollUnit(view));
            if (containers.Count > 0)
            {
                var first = view.ItemContainerGenerator.IndexFromContainer(containers[0]);
                var last = view.ItemContainerGenerator.IndexFromContainer(containers[containers.Count - 1]);
                Console.WriteLine("   首个实到行=" + first + " 末个实到行=" + last);
            }

            if (scroll && scroller is not null)
            {
                scroller.ScrollToEnd();
                Pump();
                var after = Enumerable.Range(0, vm.Rows.Count)
                    .Select(index => view.ItemContainerGenerator.ContainerFromIndex(index) as ListBoxItem)
                    .Where(item => item is not null)
                    .ToList();
                var firstAfter = after.Count > 0 ? view.ItemContainerGenerator.IndexFromContainer(after[0]) : -1;
                var lastAfter = after.Count > 0 ? view.ItemContainerGenerator.IndexFromContainer(after[after.Count - 1]) : -1;
                Console.WriteLine("   滚到底后：实到容器=" + after.Count + " 视觉=" + Descendants(view).Count(item => item is System.Windows.Media.Visual)
                    + " 首个实到行=" + firstAfter + " 末个实到行=" + lastAfter + " 垂直偏移=" + scroller.VerticalOffset.ToString("F0"));
            }

            window.Close();
            Pump();
        }

        private static McdocTreeDocument DocumentOf(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm)
            => typeof(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase)
                .GetField("document", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(vm) as McdocTreeDocument;

        private static McdocRowProjector ProjectorOf(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm)
            => typeof(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase)
                .GetField("projector", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(vm) as McdocRowProjector;

        private static void ExpandAll(IEnumerable<McdocTreeNode> nodes)
        {
            foreach (var node in nodes ?? [])
            {
                if (node is null) continue;
                node.EnsureChildren();
                node.IsExpanded = true;
                ExpandAll(McdocPresenterRegistry.Default.Children(node));
            }
        }

        private static Application EnsureApplication()
        {
            var cbhkAssembly = typeof(McdocRow).Assembly;
            try { Application.ResourceAssembly = cbhkAssembly; }
            catch (InvalidOperationException)
            {
                typeof(Application)
                    .GetField("_resourceAssembly", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    ?.SetValue(null, cbhkAssembly);
            }
            var app = Application.Current ?? new Application();
            // 量窗口时关掉"最后一个窗口关掉就退出应用"：否则第二次 new Window 会撞在 Shutdown 中的 Application 上
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

        private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }
}
