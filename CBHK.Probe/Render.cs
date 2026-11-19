using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CBHK.Utility.Data.Mcdoc;
using MinecraftLanguageModelLibrary.Data;

namespace CBHK.Probe
{
    /// <summary>
    /// 行视图的离屏渲染：用项目自己的模板字典 + 真 schema 把 mcdoc 行画成 PNG，
    /// 让"用户到底看见什么"变成可核对的证据（外观类缺陷不能只靠属性断言）。
    /// </summary>
    internal static class Render
    {
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

        public static int Rows(string outputDirectory)
        {
            if (Prepare(outputDirectory) is not { } prepared) return 2;
            var app = prepared.App;
            var index = prepared.Index;
            var rootType = new McdocType
            {
                Kind = McdocTypeKind.Dispatcher,
                Registry = "minecraft:item",
                Indices = [McdocIndex.Static("acacia_boat")],
            };
            var loading = false;
            var document = new McdocTreeDocument(index) { GameVersion = "1.21.11" };
            var projector = new McdocRowProjector();
            var pipeline = new McdocPipeline
            {
                Document = document,
                Projector = projector,
                RootType = () => rootType,
                GetLoading = () => loading,
                SetLoading = value => loading = value,
            };
            pipeline.ScheduleRebuild = anchor => pipeline.RebuildTargetedOrFallback(anchor);
            pipeline.Fallback = () => pipeline.Reload();
            projector.Document = document;
            projector.RequestReload = () => pipeline.Reload();
            document.NodeValueChanged = node => pipeline.OnValueChanged(node);
            document.NodeStructureChanged = node => pipeline.OnStructureChanged(node);
            document.Load(rootType, McdocJsonParser.Parse("{ }"));
            projector.Reset(document.ProjectionRoots);
            Pump();

            DumpRows(projector, "加键之前");
            var before = Path.Combine(outputDirectory, "item-before.png");
            Capture(app, projector, before);
            Console.WriteLine("[信息] 已渲染 " + before);

            var placeholder = document.Nodes.Select(FindPlaceholder).FirstOrDefault(found => found is not null);
            if (placeholder is not null)
            {
                placeholder.EditingKey = "my_tag";
                Console.WriteLine("[信息] 占位行 ⊕ 可用=" + (placeholder.AddEntryCommand?.CanExecute(null) ?? false));
                placeholder.AddEntryCommand?.Execute(null);
                Pump();
                foreach (var top in document.Nodes) top.EnsureChildren();
                DumpRows(projector, "加键之后");
                var entryNode = document.Nodes.Select(FindEntryNode).FirstOrDefault(found => found is not null);
                if (entryNode is not null)
                {
                    entryNode.EnsureChildren();
                    Console.WriteLine("[信息] 条目子级=" + entryNode.Children.Count + " 条目.DynamicFieldPositions="
                        + string.Join(",", entryNode.DynamicFieldPositions));
                    foreach (var child in entryNode.Children)
                    {
                        Console.WriteLine("   子级 name='" + (child.FieldName ?? "") + "' path=" + child.JsonPath
                            + " type=" + (child.Type?.Kind.ToString() ?? "-")
                            + " declared=" + (child.DeclaredType?.Kind.ToString() ?? "-")
                            + " dynamicKey=" + child.IsDynamicKey + " mapEntry=" + child.IsMapEntry);
                    }
                }
                var after = Path.Combine(outputDirectory, "item-after-add.png");
                Capture(app, projector, after);
                Console.WriteLine("[信息] 已渲染 " + after + " 行数=" + projector.Rows.Count);

                // AnyHead 各分支的行形状：string（文本）/ double（数字）/ boolean（False-True）/ list（⊕ 加项）
                foreach (var (branch, fileName) in new (string Branch, string File)[]
                         {
                             ("string", "item-any-string.png"),
                             ("double", "item-any-double.png"),
                             ("boolean", "item-any-boolean.png"),
                             ("list", "item-any-list.png"),
                         })
                {
                    var option = AnyOption(entryNode, branch);
                    if (option is null) continue;
                    entryNode.SelectedAnyType = option;
                    Pump();
                    foreach (var top in document.Nodes) top.EnsureChildren();
                    var liveRow = document.Nodes.Select(top => FindEntryNode(top)).FirstOrDefault(found => found is not null);
                    var path = Path.Combine(outputDirectory, fileName);
                    Capture(app, projector, path);
                    Console.WriteLine("[信息] 已渲染 " + path
                        + " 分支=" + (liveRow?.AnyTypeKey ?? "<null>")
                        + " 显示=" + (liveRow?.SelectedAnyType?.Name ?? "<null>")
                        + " json=" + (liveRow?.Json?.Kind.ToString() ?? "<null>"));
                    // 回到 struct，让下一轮从同一个起点开始
                    var back = AnyOption(liveRow, "struct");
                    if (back is not null) { liveRow.SelectedAnyType = back; Pump(); }
                }
            }
            // 候选键（#[id]）的「新键」行同样要画一张：键控件在那种形态下是下拉，布局不能跟文本框打架
            RenderDimensionAttributes(app, index, outputDirectory);
            return 0;
        }

        /// <summary>
        /// 26.3 维度类型的真实行树（用户截图那份 JSON）：渲染整棵树，给缩进、按钮、分支下拉留外观证据。
        /// </summary>
        public static int Dimension(string outputDirectory)
        {
            // 夹具可换：MCDOC_PROBE_JSON 指向一个 JSON 文件时用它，方便把「用户那张截图」原样复现出来
            var fixture = Environment.GetEnvironmentVariable("MCDOC_PROBE_JSON") is { Length: > 0 } custom && File.Exists(custom)
                ? File.ReadAllText(custom)
                : DimensionJson;
            return RenderTree("dimension_type", fixture, "dimension-tree.png", outputDirectory);
        }

        /// <summary>26.3 进度编辑器的真实行树：行内元组（pos 三个数字框）与必填红叹号的外观证据。</summary>
        public static int Advancement(string outputDirectory)
            => RenderTree("advancement", AdvancementJson, "advancement-tree.png", outputDirectory);

        private static int RenderTree(string rootTypeName, string fixture, string fileName, string outputDirectory)
        {
            // 夹具可用环境变量换（MCDOC_PROBE_JSON=夹具路径）：取证现场形状时不改代码
            if (Environment.GetEnvironmentVariable("MCDOC_PROBE_JSON") is { Length: > 0 } custom && File.Exists(custom))
            {
                fixture = File.ReadAllText(custom);
            }
            var outputDir = outputDirectory ?? Path.Combine(Path.GetTempPath(), "mcdoc-render");
            if (Prepare(outputDir) is not { } prepared) return 2;
            var loading = false;
            var document = new McdocTreeDocument(prepared.Index)
            {
                GameVersion = "26.3",
                RegistryOptions = new RenderRegistryStub(),
            };
            var projector = new McdocRowProjector();
            var pipeline = new McdocPipeline
            {
                Document = document,
                Projector = projector,
                RootType = () => McdocHelpers.GetRootType("dimension_type"),
                GetLoading = () => loading,
                SetLoading = value => loading = value,
            };
            pipeline.ScheduleRebuild = anchor => pipeline.RebuildTargetedOrFallback(anchor);
            pipeline.Fallback = () => pipeline.Reload();
            projector.Document = document;
            projector.RequestReload = () => pipeline.Reload();
            document.NodeValueChanged = node => pipeline.OnValueChanged(node);
            document.NodeStructureChanged = node => pipeline.OnStructureChanged(node);
            document.Load(McdocHelpers.GetRootType(rootTypeName), McdocJsonParser.Parse(fixture));
            projector.Reset(document.ProjectionRoots);
            Pump();
            foreach (var top in document.Nodes) top.EnsureChildren();
            Pump();
            // 展开到第四层：spawns_by_category 的键占位行（闭合枚举键的候选下拉）也要出现在证据里。
            // union 分支的子级在 SelectedUnionChildren 里，用 presenter 的 Children 取，别只看 Children。
            for (var level = 0; level < 3; level++)
            {
                var expandable = new List<McdocTreeNode>();
                foreach (var top in document.Nodes.ToList()) CollectExpandable(top, expandable, 0);
                if (expandable.Count == 0) break;
                foreach (var node in expandable)
                {
                    node.IsExpanded = true;
                    node.EnsureChildren();
                }
                Pump();
            }
            // 展开状态只改节点，不重排行：投影得重建一次才会把新子行算进来
            projector.Reset(document.ProjectionRoots);
            Pump();
            var path = Path.Combine(outputDir, fileName);
            Capture(prepared.App, projector, path);
            Console.WriteLine("[信息] 已渲染 " + path + " 行数=" + projector.Rows.Count);
            for (var i = 0; i < projector.Rows.Count; i++)
            {
                var row = projector.Rows[i];
                var keyCell = row.Kind is McdocRowKind.Keyed or McdocRowKind.Placeholder
                    ? " keySelect=" + row.Node?.UseKeySelect + " options=" + (row.Node?.KeyOptionList?.Count ?? 0)
                      + " rename=" + row.RenameVisibility + " combo=" + row.KeyComboVisibility
                    : "";
                var branchCell = row.Node is { IsMapEntry: true } || row.Node?.UnionTypeNameList is { Count: > 0 }
                    ? " mapEntry=" + row.Node?.IsMapEntry + " req=" + row.Node?.IsRequired
                      + " canUnset=" + row.Head?.CanUnset
                      + " branches=[" + string.Join(",", (row.Head?.BranchOptions ?? []).Select(b => b.Name)) + "]"
                    : "";
                Console.WriteLine("[行] " + i + " depth=" + row.Depth + " indent=" + row.Indent.Left
                    + " kind=" + row.Kind + " key='" + (row.Node?.FieldName ?? "") + "'" + keyCell + branchCell);
            }
            return 0;
        }

        /// <summary>收集还没展开的节点（按 presenter 认得的孩子走，union 分支也算）。</summary>
        private static void CollectExpandable(McdocTreeNode node, List<McdocTreeNode> into, int depth)
        {
            if (node is null || depth > 4) return;
            node.EnsureChildren();
            var children = McdocPresenterRegistry.Default.Children(node);
            if (!node.IsExpanded && children.Count > 0) into.Add(node);
            foreach (var child in children)
            {
                CollectExpandable(child, into, depth + 1);
            }
        }

        private const string DimensionJson = @"{
  ""attributes"": {
    ""minecraft:visual/sky_color"": { ""modifier"": ""blend_to_gray"", ""argument"": { ""brightness"": 4, ""factor"": -1 } },
    ""minecraft:visual/sky_fog_end_distance"": { ""modifier"": ""override"", ""argument"": 0 },
    ""minecraft:gameplay/natural_mob_spawns"": { ""spawns_by_category"": {}, ""spawn_costs"": { ""cave_spider"": { ""energy_budget"": 0, ""charge"": 0 } } }
  },
  ""has_skylight"": true,
  ""has_ceiling"": false,
  ""has_ender_dragon_fight"": false,
  ""coordinate_scale"": 1,
  ""ambient_light"": 0,
  ""logical_height"": 384,
  ""infiniburn"": ""#minecraft:infiniburn_overworld"",
  ""min_y"": -64,
  ""height"": 384,
  ""monster_spawn_light_level"": 0,
  ""monster_spawn_block_light_limit"": 0
}";

        /// <summary>用户 m12984 截图那份 JSON：icon.id 空串、display.count 有值、pos 空数组。</summary>
        private const string AdvancementJson = @"{
  ""parent"": ""minecraft:adventure/adventuring_time"",
  ""display"": {
    ""icon"": {
      ""id"": """",
      ""count"": 10,
      ""components"": {
        ""minecraft:lodestone_tracker"": {
          ""target"": { ""pos"": [], ""dimension"": ""minecraft:overworld"" }
        }
      }
    },
    ""title"": """",
    ""description"": """"
  },
  ""criteria"": {}
}";

        /// <summary>候选键形态的占位行（dimension_type.attributes：键来自 environment_attribute 注册表）。</summary>
        private static void RenderDimensionAttributes(Application app, McdocTypeIndex index, string outputDirectory)
        {
            var loading = false;
            var document = new McdocTreeDocument(index)
            {
                GameVersion = "1.21.11",
                RegistryOptions = new RenderRegistryStub(),
            };
            var projector = new McdocRowProjector();
            var pipeline = new McdocPipeline
            {
                Document = document,
                Projector = projector,
                RootType = () => McdocHelpers.GetRootType("dimension_type"),
                GetLoading = () => loading,
                SetLoading = value => loading = value,
            };
            pipeline.ScheduleRebuild = anchor => pipeline.RebuildTargetedOrFallback(anchor);
            pipeline.Fallback = () => pipeline.Reload();
            projector.Document = document;
            projector.RequestReload = () => pipeline.Reload();
            document.NodeValueChanged = node => pipeline.OnValueChanged(node);
            document.NodeStructureChanged = node => pipeline.OnStructureChanged(node);
            document.Load(McdocHelpers.GetRootType("dimension_type"), McdocJsonParser.Parse("{ \"attributes\": {} }"));
            projector.Reset(document.ProjectionRoots);
            Pump();
            var attributes = document.FindNode("$.attributes");
            attributes?.EnsureChildren();
            var placeholder = attributes?.Children?.FirstOrDefault(child => child.IsDynamicKey);
            Console.WriteLine("[信息] attributes 占位行 useKeySelect=" + (placeholder?.UseKeySelect ?? false)
                + " 键候选=" + (placeholder?.KeyOptionList?.Count ?? -1));
            var path = Path.Combine(outputDirectory, "attributes-key-combo.png");
            Capture(app, projector, path);
            Console.WriteLine("[信息] 已渲染 " + path);
        }

        /// <summary>渲染用的极小注册表桩：只要有 environment_attribute 的候选，键下拉才会出现。</summary>
        private sealed class RenderRegistryStub : IMcdocRegistryOptions
        {
            private static readonly string[] Attributes =
            [
                "audio/ambient_sounds", "audio/background_music", "visual/sky_color", "gameplay/bees_stay_in_hive",
            ];

            public System.Collections.Generic.IReadOnlyList<string> GetOptions(McdocIdSpec spec, string version)
                => string.Equals(spec?.Registry, "environment_attribute", StringComparison.Ordinal)
                    ? spec.Apply(Attributes)
                    : [];
        }

        private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);

        private static void DumpRows(McdocRowProjector projector, string label)
        {
            Console.WriteLine("[信息] " + label + " 行数=" + projector.Rows.Count);
            for (var i = 0; i < projector.Rows.Count; i++)
            {
                var row = projector.Rows[i];
                Console.WriteLine("   #" + i + " " + row.Kind
                    + " name='" + (row.Node?.FieldName ?? "") + "'"
                    + " path=" + (row.Node?.JsonPath ?? "-")
                    + " type=" + (row.Node?.Type?.Kind.ToString() ?? "-")
                    + " declared=" + (row.Node?.DeclaredType?.Kind.ToString() ?? "-")
                    + " editor=" + row.Head?.EditorKind
                    + " json=" + (row.Node?.Json?.Kind.ToString() ?? "<null>")
                    + " expanded=" + (row.Node?.IsExpanded ?? false));
            }
        }

        /// <summary>按稳定键取 AnyHead 分支项（显示名是大写，判据用 Value.LiteralValue）。</summary>
        private static EnumMember AnyOption(McdocTreeNode node, string key)
            => node?.AnyTypeOptions?.FirstOrDefault(option => string.Equals(
                McdocHelpers.AnyBranchKey(Convert.ToString(option.Value?.LiteralValue)), key, StringComparison.Ordinal));

        private static McdocTreeNode FindEntryNode(McdocTreeNode node)
        {
            if (node is null) return null;
            if (node.IsMapEntry && string.Equals(node.FieldName, "my_tag", StringComparison.Ordinal)) return node;
            foreach (var child in node.Children ?? [])
            {
                var found = FindEntryNode(child);
                if (found is not null) return found;
            }
            return null;
        }

        private static McdocTreeNode FindPlaceholder(McdocTreeNode node)
        {
            if (node is null) return null;
            if (node.IsDynamicKey && node.Json is null) return node;
            foreach (var child in node.Children ?? [])
            {
                var found = FindPlaceholder(child);
                if (found is not null) return found;
            }
            return null;
        }

        /// <summary>渲染前的一次性准备：资源字典指回 CBHK 程序集、载入实 schema。找不到资产返回 null。</summary>
        private static (Application App, McdocTypeIndex Index)? Prepare(string outputDirectory)
        {
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return null;
            }
            Directory.CreateDirectory(outputDirectory);

            // 相对 pack URI（字典里 Sub-Dictionary 的 Source="/ControlDictionary/..."）按 ResourceAssembly 解析：
            // 探针是入口程序集，不指回 CBHK 就会去 CBHK.Probe.dll 里找（"找不到资源 controldictionary/..."）。
            // 必须在 Application 实例化之前设，之后它就已经被钉成入口程序集了。
            var cbhkAssembly = typeof(CBHK.Utility.Data.Mcdoc.McdocRow).Assembly;
            try { Application.ResourceAssembly = cbhkAssembly; }
            catch (InvalidOperationException)
            {
                // 已经钉成入口程序集了：只能换私有字段（探针是独立 exe，正常宿主不会走到这里）
                typeof(Application)
                    .GetField("_resourceAssembly", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    ?.SetValue(null, cbhkAssembly);
            }
            var app = Application.Current ?? new Application();
            foreach (var path in Dictionaries)
            {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/CBHK;component/" + path, UriKind.Absolute),
                });
            }

            McdocNodeFactory.LazyChildren = false;
            McdocRowProjector.PageSize = int.MaxValue;
            McdocRow.DeferEditors = false;

            var index = new McdocTypeIndex();
            RealSchema.Load(index, rootDir);
            return (app, index);
        }

        private static void Capture(Application app, McdocRowProjector projector, string path)
        {
            const int width = 816;
            var view = new McdocTreeRowView
            {
                Width = width,
                Background = Brushes.Transparent,
                ItemsSource = projector.Rows,
                ItemTemplateSelector = app.Resources["McdocRowTemplateSelector"] as DataTemplateSelector,
                ItemContainerStyle = app.Resources["McdocRowItemContainerStyle"] as Style,
            };
            var host = new Border { Background = Brushes.White, Width = width, Child = view };
            host.Measure(new Size(width, double.PositiveInfinity));
            host.Arrange(new Rect(0, 0, width, host.DesiredSize.Height));
            host.UpdateLayout();
            var height = Math.Max(1, (int)Math.Ceiling(host.ActualHeight));
            Console.WriteLine("[信息] 渲染尺寸 " + width + "x" + height + " 行数=" + projector.Rows.Count);
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(host);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(path);
            encoder.Save(stream);
        }
    }
}
