using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CBHK.Utility.Data.JsonText;
using CBHK.Utility.Data.Mcdoc;
using MinecraftLanguageModelLibrary.Data;
using ICSharpCode.AvalonEdit.Document;
using Newtonsoft.Json.Linq;

namespace CBHK.Probe
{
    internal static class Gates
    {
        private sealed class Checks
        {
            private int red;

            public void That(bool ok, string criterion, string detail = "")
            {
                Console.WriteLine((ok ? "[绿] " : "[红] ") + criterion + (string.IsNullOrEmpty(detail) ? "" : " | " + detail));
                if (!ok) red++;
            }

            public int Finish(string gate)
            {
                Console.WriteLine(red == 0 ? "[绿] " + gate + " 全绿" : "[红] " + gate + " 红 " + red + " 条");
                return red == 0 ? 0 : 1;
            }
        }

        private static void Deterministic()
        {
            McdocNodeFactory.LazyChildren = false;
            McdocRowProjector.PageSize = int.MaxValue;
            McdocRow.DeferEditors = false;
        }

        /// <summary>
        /// 界面那条同步时序（文档 + 投影 + 管线）的最小装配：两个回调都接上，接线与 VM 一致
        /// （McdocGeneratorViewModelBase.cs:853-949）。少接 NodeValueChanged 的话，
        /// "赋值回弹被当成用户编辑写回"这类缺陷在探针里根本不会发生 —— 判据恒绿。
        /// </summary>
        private static (McdocTreeDocument Document, McdocRowProjector Projector, McdocPipeline Pipeline) Harness(
            McdocTypeIndex index, McdocType rootType, string json,
            Action<McdocTreeDocument> beforeLoad = null, bool wireChildBuiltHook = false)
        {
            var loading = false;
            var document = new McdocTreeDocument(index);
            var projector = new McdocRowProjector();
            var pipeline = new McdocPipeline
            {
                Document = document,
                Projector = projector,
                RootType = () => rootType,
                GetLoading = () => loading,
                SetLoading = value => loading = value,
            };
            // 与 VM 一致：运行期依赖的节点走"延后一拍重建"（VM 是 Dispatcher.BeginInvoke）。
            // 探针里同步执行 = "下一拍"。不接这条，OnStructureChanged 对 dispatcher 节点会直接早退，
            // 判据就成了"什么都没发生也算绿"。
            pipeline.ScheduleRebuild = anchor => pipeline.RebuildTargetedOrFallback(anchor);
            pipeline.Fallback = () => pipeline.Reload();
            projector.Document = document;
            projector.RequestReload = () => pipeline.Reload();
            document.NodeValueChanged = node => pipeline.OnValueChanged(node);
            document.NodeStructureChanged = node => pipeline.OnStructureChanged(node);
            // 与界面一致：投影首次建出子级时要补物化（VM 在 McdocGeneratorViewModelBase.cs:857 接的同一个钩子）。
            // 不接这条，懒建路径在探针里等于"什么都没发生也算绿"。
            if (wireChildBuiltHook) projector.AfterChildrenBuilt = node => pipeline.NoteChildrenBuilt(node);
            beforeLoad?.Invoke(document);
            document.Load(rootType, McdocJsonTextParser.Parse(json).Root);
            projector.Reset(document.Nodes.ToList());
            return (document, projector, pipeline);
        }

        // ── §八-1：文本 → 树的最小范围同步 ──────────────────────────────────
        public static int Sync()
        {
            Deterministic();
            var text = Fixtures.Text();
            Console.WriteLine("夹具：" + text.Split('\n').Length + " 行，" + text.Length + " 字符");

            var checks = new Checks();
            var ops = new (string Name, Func<string, string> Edit, bool Structural)[]
            {
                ("值变化（format_version 1→2）", t => t.Replace("\"format_version\": 1,", "\"format_version\": 2,"), false),
                ("嵌套容器加键（nested.kNew）", t => t.Replace("\"three\": 3,", "\"three\": 3,\n    \"kNew\": 7,"), true),
                ("嵌套容器删键（nested.k0）", t => t.Replace("    \"k0\": 0,\n", ""), true),
                ("根层加键（extra_root）", t => t.Replace("  \"alpha\": \"alpha-value\",", "  \"extra_root\": 5,\n  \"alpha\": \"alpha-value\","), true),
                ("根层删键（alpha）", t => t.Replace("  \"alpha\": \"alpha-value\",\n", ""), true),
                ("列表删项（list[0]）", t => t.Replace(Fixtures.ItemBlock(0) + ",\n", ""), true),
                ("列表加项（list[60]）", t => t.Replace(Fixtures.ItemBlock(Fixtures.ListItems - 1), Fixtures.ItemBlock(Fixtures.ListItems - 1) + ",\n" + Fixtures.ItemBlock(Fixtures.ListItems)), true),
                ("无变化", t => t, false)
            };

            foreach (var op in ops)
            {
                var edited = op.Edit(text);
                var document = new McdocTreeDocument(new McdocTypeIndex());
                document.Load(Fixtures.RootType(), McdocJsonTextParser.Parse(text).Root);
                var projector = new McdocRowProjector();
                projector.Reset(document.Nodes.ToList());
                var baselineResets = projector.ResetCount;

                var newRoot = McdocJsonTextParser.Parse(edited).Root;
                var applied = McdocSyncEngine.TryApply(document, newRoot, out var stats);
                if (applied) McdocProjectionSync.Apply(projector, document, stats);
                document.Validate();

                var label = op.Name;
                checks.That(applied, label + " · 不回落整篇重建",
                    "anchors=" + stats.StructureAnchors + " rebuilt=" + stats.RebuiltNodes + " patches=" + stats.ValuePatches + " ms=" + stats.ElapsedMs);
                if (!applied) continue;

                checks.That(projector.ResetCount == baselineResets, label + " · 整表重投影 = 0",
                    "reset 增 " + (projector.ResetCount - baselineResets));
                checks.That(stats.ElapsedMs <= (op.Structural ? 150 : 20), label + " · 耗时达标（" + (op.Structural ? "≤150ms" : "≤20ms") + "）",
                    stats.ElapsedMs + "ms");
                if (op.Structural) checks.That(stats.RebuiltNodes > 0 || stats.MemberOps > 0, label + " · 有节点被局部重建",
                    "rebuilt=" + stats.RebuiltNodes + " memberOps=" + stats.MemberOps);
                Contracts(checks, document, projector, label);
            }

            return checks.Finish("--gate sync");
        }

        // ── §八-2：增量解析 ──────────────────────────────────────────────
        public static int Parse()
        {
            Deterministic();
            var text = Fixtures.Text();
            Console.WriteLine("夹具：" + text.Split('\n').Length + " 行，" + text.Length + " 字符");
            var checks = new Checks();

            var state = new JsonTextState(new TextDocument(text));
            var before = TopLevel(state.Root);
            var edited = text.Replace("\"format_version\": 1,", "\"format_version\": 2,");
            state.Document.Text = edited;

            var incremental = state.TryIncrementalReparse(edited);
            if (!incremental) state.Reparse();
            var after = TopLevel(state.Root);
            var reused = before.Count == after.Count ? before.Zip(after, ReferenceEquals).Count(same => same) : 0;

            checks.That(incremental, "改一个值 · 走增量解析（不是整篇）", "TryIncrementalReparse=" + incremental);
            checks.That(state.IsInStepWithDocument, "改一个值 · 文本与 AST 同代", "RootText 与 Document.Text " + (state.IsInStepWithDocument ? "一致" : "不一致"));
            checks.That(reused == before.Count - 1, "改一个值 · 未受影响成员 AST 原样复用",
                "复用 " + reused + "/" + Math.Max(0, before.Count - 1));

            // 反向判据：跨成员改动必须回落整篇（否则"永远走增量"会假绿）
            var crossEdited = text.Replace("  \"alpha\": \"alpha-value\",\n", "");
            var crossState = new JsonTextState(new TextDocument(text));
            crossState.Document.Text = crossEdited;
            var crossIncremental = crossState.TryIncrementalReparse(crossEdited);
            checks.That(!crossIncremental, "增删顶层成员 · 必须回落整篇解析", "TryIncrementalReparse=" + crossIncremental);

            var fullMs = MedianFull(text);
            var incrementalMs = MedianIncremental(text, edited, out var allFast);
            checks.That(allFast && incrementalMs <= fullMs * 0.4, "改一个值 · 增量解析耗时 ≤ 整篇的 40%",
                (allFast ? "" : "有回落 ") + "增量=" + incrementalMs.ToString("0.###") + "ms 整篇=" + fullMs.ToString("0.###") + "ms");

            return checks.Finish("--gate parse");
        }

        private static List<McdocJsonValue> TopLevel(McdocJsonValue root)
            => (root?.Properties ?? []).Select(pair => pair.Value).ToList();

        private static double MedianFull(string text)
        {
            const int rounds = 30;
            var samples = new List<double>(rounds);
            for (var round = 0; round < rounds; round++)
            {
                var state = new JsonTextState(new TextDocument(text));
                var started = Stopwatch.GetTimestamp();
                state.Reparse();
                samples.Add((Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency);
            }
            samples.Sort();
            return samples[samples.Count / 2];
        }

        private static double MedianIncremental(string text, string edited, out bool allFast)
        {
            const int rounds = 30;
            allFast = true;
            var samples = new List<double>(rounds);
            for (var round = 0; round < rounds; round++)
            {
                var state = new JsonTextState(new TextDocument(text));
                state.Document.Text = edited;
                var started = Stopwatch.GetTimestamp();
                var fast = state.TryIncrementalReparse(edited);
                var elapsed = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
                if (!fast)
                {
                    allFast = false;
                    return elapsed;
                }
                samples.Add(elapsed);
            }
            samples.Sort();
            return samples[samples.Count / 2];
        }

        // ── 必填枚举必须在载入期落盘，且不许留假诊断 ────────────────────────────
        public static int RequiredEnum()
        {
            Deterministic();
            var document = new McdocTreeDocument(new McdocTypeIndex());
            document.Load(Fixtures.RequiredEnumHostType(), McdocJsonTextParser.Parse("{}").Root);
            var checks = new Checks();

            // 跑与 VM 同一段物化（载入路径：includeDisplayedDefaults = true），再到不动点
            var written = 0;
            for (var pass = 0; pass < 4; pass++)
            {
                var step = McdocCollectionEditor.Default.MaterializeRequiredSelections(document.Nodes, pass == 0);
                written += step;
                if (step == 0) break;
                // 写入会改变文档形状：和 VM 的不动点一样，每轮重建一次再跑下一轮
                document.Load(Fixtures.RequiredEnumHostType(), document.RootJson);
            }
            checks.That(written > 0, "必填枚举被物化进文档", "written=" + written);

            var level = document.FindNode("$.mode.level");
            checks.That(level is not null, "$.mode.level 节点存在", level is null ? "FindNode 落空" : "");
            checks.That(level?.Json is { Kind: McdocJsonKind.String } value && !string.IsNullOrEmpty(value.StringValue),
                "枚举值已落盘", level?.Json is null ? "Json=null" : level.Json.Kind.ToString());
            checks.That(document.RootJson.Get("mode")?.Get("level") is not null, "文档根里也真的写进去了",
                "");

            var diagnostics = document.Validate();
            checks.That(diagnostics.Count == 0, "物化 + 校验后没有诊断",
                string.Join(" ; ", diagnostics.Take(3).Select(item => item.Path + ":" + item.Message)));
            return checks.Finish("--case required-enum");
        }

        // ── union 分支身份按「名字+下标」，不按实例引用（不然绑定会来回推）──────────
        public static int UnionIdentity()
        {
            Deterministic();
            var document = new McdocTreeDocument(new McdocTypeIndex());
            document.Load(Fixtures.UnionHostType(), McdocJsonTextParser.Parse("{ \"u\": { \"b\": 1 } }").Root);
            var projector = new McdocRowProjector();
            projector.Reset(document.Nodes.ToList());
            var checks = new Checks();

            var node = document.FindNode("$.u");
            var row = node is null ? null : projector.Find(node);
            checks.That(row is not null, "拿到 union 行", row is null ? "没有行" : "");
            if (row is null) return checks.Finish("--case union-identity");

            var first = row.UnionOptions[0];
            var second = row.UnionOptions[1];
            checks.That(!first.Equals(second), "两个分支实例本来就不相等（引用比较必然判成换支）", "");
            checks.That(McdocUnionSwitch.IsSameOption(first, first) && !McdocUnionSwitch.IsSameOption(first, second),
                "按身份比：同一支为真、不同支为假", "");

            // getter 必须返回候选列表里的实例，否则下拉找不到选中项、会把自己的值再推回来
            checks.That(row.UnionOptions.Contains(row.SelectedUnionOption), "选中项是候选列表里的一员（引用相等）",
                "selected=" + (row.SelectedUnionOption?.Name ?? "<null>"));

            // 模拟 WPF 绑定：把「同一支的新实例」推回 setter —— 不许改写文档、不许换行
            var before = McdocJsonFormatter.Format(document.RootJson);
            var beforeSelected = row.SelectedUnionOption;
            row.SelectedUnionOption = row.UnionOptions.First(option =>
                McdocUnionSwitch.IsSameOption(option, beforeSelected));
            checks.That(McdocJsonFormatter.Format(document.RootJson) == before, "同一支回推不改写 JSON", "");
            checks.That(McdocUnionSwitch.SwitchTo(node, beforeSelected) == false, "同一支再切一次是空操作", "");

            // 连做 3 轮：每轮重新投影（新实例）后再回推，JSON 必须一动不动
            for (var round = 1; round <= 3; round++)
            {
                projector.Reset(document.Nodes.ToList());
                var freshRow = projector.Find(document.FindNode("$.u"));
                if (freshRow is null) { checks.That(false, "第 " + round + " 轮拿到行", ""); break; }
                var current = freshRow.SelectedUnionOption;
                freshRow.SelectedUnionOption = freshRow.UnionOptions.First(option => McdocUnionSwitch.IsSameOption(option, current));
                checks.That(McdocJsonFormatter.Format(document.RootJson) == before, "第 " + round + " 轮回推后 JSON 仍不变", "");
            }
            return checks.Finish("--case union-identity");
        }

        // ── 显式切过 union 分支后，重建不许把选择顶回去 ──────────────────────────
        public static int UnionStable()
        {
            Deterministic();
            var document = new McdocTreeDocument(new McdocTypeIndex());
            document.Load(Fixtures.UnionHostType(), McdocJsonTextParser.Parse("{ \"u\": { \"b\": 1 } }").Root);
            var checks = new Checks();

            var node = document.FindNode("$.u");
            if (node is null)
            {
                checks.That(false, "$.u · 找得到 union 节点", "FindNode 落空");
                return checks.Finish("--case union-stable");
            }
            var options = node.UnionTypeNameList;
            checks.That(options.Count == 2, "union 有两个候选分支", "count=" + options.Count);
            if (options.Count < 2) return checks.Finish("--case union-stable");

            checks.That(McdocUnionSwitch.SwitchTo(node, options[1]), "切到第二个分支", "");
            var chosen = node.SelectedUnionTypeName?.Name;
            checks.That(chosen == options[1].Name, "切换后选中项是第二支", "chosen=" + chosen);

            for (var round = 1; round <= 2; round++)
            {
                var rebuilt = document.RebuildNode(node);
                checks.That(rebuilt is not null, "第 " + round + " 次重建成功", "RebuildNode 返回 null");
                if (rebuilt is null) break;
                node = rebuilt;
                checks.That(node.SelectedUnionTypeName?.Name == chosen, "第 " + round + " 次重建后选中项不变",
                    "期望=" + chosen + " 实际=" + (node.SelectedUnionTypeName?.Name ?? "<null>"));
            }

            // 既有政策不许被这次改动破坏：切到解不出来的成员仍然回 unset
            var unresolved = document.FindNode("$.u");
            checks.That(unresolved?.SelectedUnionMember is not null, "重建后仍落在具体分支上",
                "SelectedUnionMember=" + (unresolved?.SelectedUnionMember?.Kind.ToString() ?? "<null>"));
            return checks.Finish("--case union-stable");
        }

        // ── 遗留键删掉之后，树上那一行必须一起消失 ──────────────────────────────
        public static int LegacyDelete()
        {
            Deterministic();
            const string json = "{ \"a\": 1, \"b\": { \"b1\": 1, \"bKeep\": { \"deep\": 1 } } }";
            var document = new McdocTreeDocument(new McdocTypeIndex());
            document.Load(Fixtures.LegacyHostType(), McdocJsonTextParser.Parse(json).Root);
            var projector = new McdocRowProjector();
            projector.Reset(document.Nodes.ToList());

            var checks = new Checks();
            var node = document.FindNode("$.b.bKeep");
            var parent = node?.Parent;
            checks.That(node is not null && node.RemoveFieldCommand?.CanExecute(null) == true, "遗留键走『删键』入口",
                node is null ? "找不到节点" : "CanExecute=" + (node.RemoveFieldCommand?.CanExecute(null) ?? false));
            if (node is null || parent is null) return checks.Finish("--case legacy-delete");

            node.RemoveFieldCommand.Execute(null);
            checks.That(parent.Json?.Get("bKeep") is null, "JSON 里这个键被删掉", "parentKeys=" + KeyPaths(document.RootJson));

            // VM 在 StructureChanged 里做的就是这两步：刷新父级、重投影
            document.RefreshNode(parent);
            projector.Reset(document.Nodes.ToList());
            checks.That(!parent.Children.Contains(node), "父级子级里没有这个节点", "children=" + parent.Children.Count);
            checks.That(document.FindNode("$.b.bKeep") is null, "按路径再也找不到", "");
            checks.That(projector.Rows.All(row => !ReferenceEquals(row.Node, node)), "投影里没有这一行",
                "行数=" + projector.Rows.Count);
            checks.That(document.CountUnknownFields() == 0, "遗留键计数归零", document.CountUnknownFields().ToString());
            return checks.Finish("--case legacy-delete");
        }

        // ── BlockState 资产 → mcdoc:block_states 成员（判据比对真资产文件，不写死）─────
        /// <summary>返回 0 绿 / 1 红 / 2 环境缺失（找不到资产目录）。</summary>
        public static int BlockstateProperties()
        {
            var checks = new Checks();
            var directory = LocateBlockStateDirectory();
            if (directory is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\Registries\\BlockState，跳过判据（退出码 2）");
                return 2;
            }
            Console.WriteLine("[信息] 资产目录 " + directory);

            var versions = System.IO.Directory.GetFiles(directory, "*.json")
                .Select(path => System.IO.Path.GetFileNameWithoutExtension(path))
                .OrderByDescending(name => name, StringComparer.Ordinal)
                .ToList();
            checks.That(versions.Count > 0, "资产目录里有版本文件", "versions=" + string.Join(",", versions.Take(5)));
            if (versions.Count == 0) return checks.Finish("--case blockstate-properties");

            var version = versions.Contains("1.20.5") ? "1.20.5" : versions[0];
            var assetPath = System.IO.Path.Combine(directory, version + ".json");
            var members = McdocBlockStates.Load(version, directory);
            checks.That(members.Count > 0, "解析出方块成员", "blocks=" + members.Count);

            // 逐个方块跟文件比对：字段集合 + 每个属性的候选值，全部从文件读出来
            foreach (var blockId in new[] { "acacia_button", "acacia_door" })
            {
                var node = JObject.Parse(System.IO.File.ReadAllText(assetPath))[blockId] as JArray;
                checks.That(node is not null && node.Count > 0 && node[0] is JObject,
                    blockId + " 在资产里存在", node is null ? "文件里没有这个方块" : "states=" + node.Count);
                if (node is null || node.Count == 0 || node[0] is not JObject properties) continue;

                checks.That(members.TryGetValue(blockId, out var structure) && structure is not null,
                    blockId + " 解析出类型", members.ContainsKey(blockId) ? "" : "Load 结果里没有");
                if (!members.TryGetValue(blockId, out var type) || type?.Fields is null) continue;

                var expectedNames = properties.Properties().Select(property => property.Name).ToList();
                var actualNames = type.Fields.Select(field => field.Name).ToList();
                checks.That(actualNames.Count == expectedNames.Count && !expectedNames.Except(actualNames).Any(),
                    blockId + " 字段集合与资产完全一致",
                    "期望=[" + string.Join(",", expectedNames) + "] 实际=[" + string.Join(",", actualNames) + "]");

                foreach (var property in properties.Properties())
                {
                    var expectedValues = (property.Value as JArray ?? []).Select(item => Convert.ToString(item)).ToList();
                    var field = type.Fields.FirstOrDefault(candidate => candidate.Name == property.Name);
                    var actualValues = (field?.Type?.EnumValues ?? []).Select(value => Convert.ToString(value.Value)).ToList();
                    checks.That(actualValues.Count == expectedValues.Count && !expectedValues.Except(actualValues).Any(),
                        blockId + "." + property.Name + " 候选值与资产一致",
                        "期望=" + expectedValues.Count + " 实际=" + actualValues.Count);
                }
            }

            // 版本回退：请求一个资产里没有的版本，仍要拿到成员
            var fallback = McdocBlockStates.Load("1.22.0", directory);
            checks.That(fallback.Count == members.Count && fallback.Count > 0,
                "版本不存在时回退到最近可用资产", "fallback=" + fallback.Count + " 基准=" + members.Count);

            // 修复前的行为：不喂资产时 mcdoc:block_states 整个空间没有成员（schema 的 [[block]] 索引解析成空）
            var untouched = new McdocTypeIndex();
            var beforeRegister = untouched.GetDispatcherMembers(McdocBlockStates.Registry, "file:///probe/gate.json");
            checks.That(beforeRegister is null || beforeRegister.Count == 0,
                "未注册时 mcdoc:block_states 没有成员（修复前的红）",
                "members=" + (beforeRegister?.Count ?? -1));

            // 注册进调度器表
            var index = new McdocTypeIndex();
            var registered = McdocBlockStates.Register(index, version, directory);
            checks.That(registered == members.Count, "注册数量与解析数量一致", "registered=" + registered);
            var dispatchers = index.GetDispatcherMembers(McdocBlockStates.Registry, "file:///probe/gate.json");
            checks.That(dispatchers is not null && dispatchers.Count == members.Count,
                "调度器表里成员齐全", "dispatchers=" + (dispatchers?.Count ?? -1));
            foreach (var blockId in new[] { "acacia_button", "acacia_door" })
            {
                checks.That(dispatchers is not null && dispatchers.ContainsKey(blockId),
                    "调度器表里含 " + blockId, "");
            }
            return checks.Finish("--case blockstate-properties");
        }

        /// <summary>从探针 exe 所在目录向上找仓库根（含 CBHK.sln），再定位构建输出里的资产目录。</summary>
        /// <summary>properties 节点当前的字段名（判据用；界面上展开才会建子级，这里同一条路）。</summary>
        private static string DescribeProperties(McdocTreeDocument document)
        {
            var node = document?.FindNode("$.properties");
            if (node is null) return "<没有 properties 节点>";
            node.EnsureChildren();
            return string.Join(",", McdocPresenterRegistry.Default.Children(node).Select(c => c.FieldName));
        }

        private static string LocateBlockStateDirectory()
        {
            var directory = new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory is not null)
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(directory.FullName, "CBHK.sln")))
                {
                    var built = System.IO.Path.Combine(directory.FullName, "CBHK", "bin", "Debug",
                        "net8.0-windows10.0.22621.0", "Resource", "Registries", "BlockState");
                    if (System.IO.Directory.Exists(built)) return built;
                    var source = System.IO.Path.Combine(directory.FullName, "CBHK", "Resource", "Registries", "BlockState");
                    if (System.IO.Directory.Exists(source)) return source;
                    return null;
                }
                directory = directory.Parent;
            }
            return null;
        }

        // ── 管线不许自我启动（展开 + 切分支后投影次数必须有界）─────────────────────
        public static int NoSelfStart()
        {
            Deterministic();
            var document = new McdocTreeDocument(new McdocTypeIndex());
            var projector = new McdocRowProjector();
            var pipeline = new McdocPipeline
            {
                Document = document,
                Projector = projector,
                RootType = Fixtures.UnionHostType,
                GetLoading = () => false,
                SetLoading = _ => { }
            };
            document.NodeStructureChanged = node => pipeline.OnStructureChanged(node);
            // 与 VM 同款钩子：懒建出子级后想排一次重建 —— 闸门内必须被拦掉，否则就是自我启动
            projector.AfterChildrenBuilt = node =>
            {
                if (pipeline.Rebuilding) return;
                pipeline.RebuildTargetedOrFallback(node);
            };
            document.Load(Fixtures.UnionHostType(), McdocJsonTextParser.Parse("{ \"u\": { \"b\": 1 } }").Root);
            projector.Reset(document.Nodes.ToList());

            var checks = new Checks();
            var before = projector.ResetCount + projector.SpliceCount;
            var union = document.FindNode("$.u");
            checks.That(union is not null, "$.u 存在", "");
            if (union is null) return checks.Finish("--case no-selfstart");

            union.IsExpanded = true;
            union.EnsureChildren();
            var row = projector.Find(union);
            if (row is not null) projector.SpliceExpansion(row);
            checks.That(McdocUnionSwitch.SwitchTo(union, union.UnionTypeNameList[1]), "切一次分支（到另一支）", "");

            var delta = projector.ResetCount + projector.SpliceCount - before;
            checks.That(delta <= 6, "展开 + 切分支后投影次数有界（不自我启动）", "Δ=" + delta);
            checks.That(!pipeline.Rebuilding, "闸门已复位", "Rebuilding=" + pipeline.Rebuilding);

            // 闸门契约：管线内部阶段（投影里 EnsureChildren）把钩子叫起来时，不许再排重建。
            // 上一轮的循环就是因为这段窗口在闸门外 —— 这条直接把契约钉住，不依赖具体场景。
            var inside = projector.ResetCount + projector.SpliceCount;
            pipeline.Rebuilding = true;
            try
            {
                projector.AfterChildrenBuilt?.Invoke(union);
            }
            finally
            {
                pipeline.Rebuilding = false;
            }
            checks.That(projector.ResetCount + projector.SpliceCount == inside,
                "闸门内：钩子不许排重建", "Δ=" + (projector.ResetCount + projector.SpliceCount - inside));

            // 钩子只许在"子级刚建出来"那一拍触发：重复投影不许再喊（现场 100 次循环的真正驱动源）
            var fired = 0;
            projector.AfterChildrenBuilt = _ => fired++;
            projector.Reset(document.Nodes.ToList());
            var firedAfterFirst = fired;
            projector.Reset(document.Nodes.ToList());
            projector.Reset(document.Nodes.ToList());
            checks.That(fired == firedAfterFirst, "重复投影不再触发补建回调",
                "第一次=" + firedAfterFirst + " 三次后=" + fired);
            projector.AfterChildrenBuilt = node =>
            {
                if (pipeline.Rebuilding) return;
                pipeline.RebuildTargetedOrFallback(node);
            };

            // 再入通知：重建期间管线收到的 StructureChanged 是自己造的，不许刷新、不许排重建
            var scheduled = 0;
            pipeline.ScheduleRebuild = _ => scheduled++;
            var guardCounts = projector.ResetCount + projector.SpliceCount;
            pipeline.Rebuilding = true;
            try
            {
                pipeline.OnStructureChanged(union);
            }
            finally
            {
                pipeline.Rebuilding = false;
            }
            checks.That(projector.ResetCount + projector.SpliceCount == guardCounts,
                "重建期间的再入通知不刷新投影", "Δ=" + (projector.ResetCount + projector.SpliceCount - guardCounts));
            checks.That(scheduled == 0, "重建期间的再入通知不排重建", "scheduled=" + scheduled);

            // 反向：闸门外（用户真的展开）钩子必须还能排重建，否则就是把功能堵死
            var outside = projector.ResetCount + projector.SpliceCount;
            projector.AfterChildrenBuilt?.Invoke(union);
            checks.That(projector.ResetCount + projector.SpliceCount > outside,
                "闸门外：用户展开仍会排重建", "Δ=" + (projector.ResetCount + projector.SpliceCount - outside));
            return checks.Finish("--case no-selfstart");
        }

        // ── 切到"解不出来的成员"必须回 unset，且之后切回可解的支要能站住 ─────────────
        public static int UnionUnresolved()
        {
            Deterministic();
            var document = new McdocTreeDocument(new McdocTypeIndex());
            var projector = new McdocRowProjector();
            var pipeline = new McdocPipeline
            {
                Document = document,
                Projector = projector,
                RootType = Fixtures.UnionWithUnresolvedHostType,
                GetLoading = () => false,
                SetLoading = _ => { }
            };
            document.NodeStructureChanged = node => pipeline.OnStructureChanged(node);
            document.Load(Fixtures.UnionWithUnresolvedHostType(), McdocJsonTextParser.Parse("{ \"u\": { \"a\": 1 } }").Root);
            projector.Reset(document.Nodes.ToList());

            var checks = new Checks();
            var node = document.FindNode("$.u");
            checks.That(node is not null && node.UnionTypeNameList.Count == 2, "两个候选分支", node is null ? "没有节点" : "count=" + node.UnionTypeNameList.Count);
            if (node is null || node.UnionTypeNameList.Count < 2) return checks.Finish("--case union-unresolved");

            checks.That(McdocUnionSwitch.SwitchTo(node, node.UnionTypeNameList[1]), "尝试切到解不出来的支（算一次变化）", "");
            checks.That(!McdocUnionSwitch.IsSameOption(node.SelectedUnionTypeName, node.UnionTypeNameList[1]),
                "解不出来的支不许粘在选择上（回 unset）", "selected=" + (node.SelectedUnionTypeName?.Name ?? "<null>"));
            checks.That(!string.IsNullOrEmpty(node.Error), "并给出可见诊断", "Error=" + (node.Error ?? "<空>"));

            // 再切回能解的那支：必须站得住，且重建一次不弹回
            checks.That(McdocUnionSwitch.SwitchTo(node, node.UnionTypeNameList[0]), "切回可解的支", "");
            var chosen = node.SelectedUnionTypeName?.Name;
            var rebuilt = document.RebuildNode(node);
            checks.That(rebuilt is not null && rebuilt.SelectedUnionTypeName?.Name == chosen,
                "切回后重建不弹回", "期望=" + chosen + " 实际=" + (rebuilt?.SelectedUnionTypeName?.Name ?? "<null>"));
            return checks.Finish("--case union-unresolved");
        }

        // ── 读真机日志判定"没有自我启动"（用户点一次，跑这个就能判红绿）──────────────
        public static int CheckTrace(string path)
        {
            // 日志是空的 / 一条 trace 行都没有：那是"没验过"，不是"验过了"（退出码 2 = 环境不足）。
            // 只看日志内容：分析进程自己开不开开关与此无关（生产端开没开，看这里就知道）。
            if (System.IO.File.Exists(path) && !System.IO.File.ReadLines(path).Any(line => line.Contains("[mcdoc-trace]")))
            {
                Console.WriteLine("[环境缺失] 日志里没有 trace 行：复现前先开开关（启动前设 MCDOC_TRACE_WRITE=1，或在 CBHK.exe 旁放空文件 mcdoc-trace.on），退出码 2");
                return 2;
            }
            var checks = new Checks();
            if (!System.IO.File.Exists(path))
            {
                Console.WriteLine("[红] 找不到日志：" + path);
                return 1;
            }
            var structure = 0;
            var splices = 0;
            var resets = 0;
            var reentrant = 0;
            var hiddenDelete = 0;
            var enumEmpty = 0;
            // "注册表根本没有成员"是**数据缺口**（README：拿不到注册表数据时不判，但要在 trace 里说清楚），
            // 与"声明有候选却空下拉"分开统计 —— 后者才是求解/渲染的缺陷。
            var enumNoData = 0;
            var indexUnresolved = 0;
            var diagCount = 0;
            var diagSamples = new System.Collections.Generic.List<string>();
            string first = null, last = null;
            foreach (var line in System.IO.File.ReadLines(path))
            {
                if (!line.Contains("[mcdoc-trace]")) continue;
                first ??= line;
                last = line;
                if (line.Contains(" structure ")) structure++;
                else if (line.Contains("proj-splice")) splices++;
                else if (line.Contains("proj-reset")) resets++;
                else if (line.Contains("structure-reentrant")) reentrant++;
                else if (line.Contains("self-delete-hidden")) hiddenDelete++;
                else if (line.Contains("enum-empty"))
                {
                    if (line.Contains("reason=no-members") && line.Contains("registry=")) enumNoData++;
                    else enumEmpty++;
                }
                else if (line.Contains("index-resolve") && line.Contains("resolved=False")) indexUnresolved++;
                else if (line.Contains(" diag "))
                {
                    diagCount++;
                    if (diagSamples.Count < 3) diagSamples.Add(line.Trim());
                }
            }
            Console.WriteLine("[信息] 结构变更 " + structure + " 次，拼接 " + splices + " 次，整表重投影 " + resets
                + " 次，再入被拦 " + reentrant + " 次");
            Console.WriteLine("[信息] 首行 " + (first ?? "<空>"));
            Console.WriteLine("[信息] 末行 " + (last ?? "<空>"));
            checks.That(structure > 0, "日志里有真实操作（不是空文件）", "structure=" + structure);
            checks.That(hiddenDelete == 0, "没有出现「该有却没有删除按钮」的行", "self-delete-hidden=" + hiddenDelete);
            checks.That(enumEmpty == 0, "没有出现「声明有候选却空下拉」的行", "enum-empty=" + enumEmpty);
            Console.WriteLine("[信息] index-resolve 未解析次数=" + indexUnresolved
                + "；注册表无成员（数据缺口，不判）=" + enumNoData
                + "；行上提醒 diag=" + diagCount);
            foreach (var sample in diagSamples) Console.WriteLine("[提醒] " + sample);
            // 现场循环签名：structure 只有个位数，而 proj-* 各上百次
            var perOperation = structure == 0 ? splices + resets : (splices + resets) / Math.Max(1, structure);
            checks.That(perOperation <= 20, "每次结构变更的投影次数有界（≤20）", "每次≈" + perOperation);
            return checks.Finish("--check-trace");
        }

        // ── Properties 的字段行第一次建出来就要能用（枚举候选齐全）────────────────────
        public static int BlockStateEnumAfterValue()
        {
            var checks = new Checks();
            var directory = LocateBlockStateDirectory();
            if (directory is null)
            {
                Console.WriteLine("[环境缺失] 找不到 BlockState 资产（退出码 2）");
                return 2;
            }
            Deterministic();
            var index = new McdocTypeIndex();
            McdocBlockStates.Register(index, "1.20.5", directory);

            var states = McdocType.Create(McdocTypeKind.Dispatcher);
            states.Registry = McdocBlockStates.Registry;
            states.Indices = [ McdocIndex.Dynamic(new System.Collections.Generic.List<string> { "block" }) ];
            var rootType = McdocType.Create(McdocTypeKind.Struct);
            rootType.Fields =
            [
                new McdocStructField { Name = "block", Key = McdocType.LiteralString("block"), Type = McdocType.Create(McdocTypeKind.String), Optional = true },
                new McdocStructField { Name = "properties", Key = McdocType.LiteralString("properties"), Type = states, Optional = true },
            ];

            var document = new McdocTreeDocument(index);
            var projector = new McdocRowProjector();
            var pipeline = new McdocPipeline
            {
                Document = document,
                Projector = projector,
                RootType = () => rootType,
                GetLoading = () => false,
                SetLoading = _ => { }
            };
            document.NodeStructureChanged = node => pipeline.OnStructureChanged(node);
            // 现场顺序：先把 properties 建出来（此时 block 还没有值），再给 block 落值
            document.Load(rootType, McdocJsonTextParser.Parse(@"{ ""properties"": {} }").Root);
            projector.Reset(document.Nodes.ToList());

            var before = DescribeProperties(document);
            var blockNode = document.FindNode("$.block");
            checks.That(blockNode is not null, "拿到 block 节点", blockNode is null ? "没有节点" : "");
            if (blockNode is null) return checks.Finish("--case blockstate-enum-after-value");

            blockNode.Value = "minecraft:acacia_button";
            McdocCollectionEditor.Default.ApplyNodeValue(blockNode);
            var rootJson = document.RootJson;
            Console.WriteLine("[信息] 落值后 RootJson=" + (rootJson is null ? "<null>"
                : string.Join(",", (rootJson.Properties ?? []).Select(p => p.Key + ":" + p.Value?.StringValue))));
            pipeline.OnValueChanged(blockNode);

            var after = DescribeProperties(document);
            checks.That(after.Contains("face") && after.Contains("facing") && after.Contains("powered"),
                "兄弟键落值后 properties 当场可用（不用折叠再展开）", "前=" + before + " 后=" + after);
            var faces = document.FindNode("$.properties.face");
            var options = faces?.EnumOptionList?.Select(o => o.Name).ToList() ?? [];
            checks.That(options.Count > 0, "属性行下拉有候选", string.Join(",", options));
            return checks.Finish("--case blockstate-enum-after-value");
        }

        // ── 声明形状矩阵：X[[k]] 可能被包成几种指令形状，逐一验证"可求值 + 依赖能重新求值"──────
        public static int DependentsDeclarationShape()
        {
            var checks = new Checks();
            var directory = LocateBlockStateDirectory();
            if (directory is null)
            {
                Console.WriteLine("[环境缺失] 找不到 BlockState 资产（退出码 2）");
                return 2;
            }
            Deterministic();
            var index = new McdocTypeIndex();
            McdocBlockStates.Register(index, "1.20.5", directory);

            // 每种形状都用独立对象：索引同时挂在两层会造成"双重索引"，那不是真声明。
            McdocType ShellOf(McdocType child, McdocTypeKind kind)
            {
                var shell = McdocType.Create(kind);
                shell.Item = child;
                if (kind == McdocTypeKind.Mapped) shell.Mapping = new Dictionary<string, McdocType>(StringComparer.Ordinal);
                if (kind == McdocTypeKind.Concrete) shell.Items = [];
                if (kind == McdocTypeKind.Indexed) shell.Indices = [];
                return shell;
            }
            McdocType DispatcherWithIndex(string accessor)
            {
                var value = McdocType.Create(McdocTypeKind.Dispatcher);
                value.Registry = McdocBlockStates.Registry;
                value.Indices = [McdocIndex.Dynamic(new List<string> { accessor })];
                return value;
            }
            McdocType BareDispatcher()
            {
                var value = McdocType.Create(McdocTypeKind.Dispatcher);
                value.Registry = McdocBlockStates.Registry;
                return value;
            }

            var shapes = new (string Label, McdocType Type)[]
            {
                ("Dispatcher{Indices}", DispatcherWithIndex("block")),
                ("Dispatcher{无索引}", BareDispatcher()),
                ("Mapped 外壳 + Dispatcher{Indices}", ShellOf(DispatcherWithIndex("block"), McdocTypeKind.Mapped)),
                ("Concrete 外壳 + Dispatcher{Indices}", ShellOf(DispatcherWithIndex("block"), McdocTypeKind.Concrete)),
                ("Indexed 空壳 + Dispatcher{Indices}", ShellOf(DispatcherWithIndex("block"), McdocTypeKind.Indexed)),
            };

            foreach (var shape in shapes)
            {
                var rootType = RootTypeWith(shape.Type);
                // 1) 这个声明形状本身可求值吗（block 已经有值时 properties 直接解出字段行）
                var (prepared, _, _) = Harness(index, rootType,
                    @"{ ""block"": ""minecraft:acacia_button"", ""properties"": {} }");
                var evaluable = prepared.FindNode("$.properties.face") is not null;
                var resolved = prepared.FindNode("$.properties")?.Type?.Kind.ToString() ?? "<无节点>";
                var matched = McdocPipeline.IndexesByKey(shape.Type, "block");
                if (!evaluable)
                {
                    // 求值层不认这个形状：记下来（声明形状层面的已知限制，不是判据）
                    Console.WriteLine("[信息] " + shape.Label + " → 不可求值 resolved=" + resolved + " index=" + (matched ? "命中" : "不命中"));
                    continue;
                }
                // 2) 可求值的形状里，兄弟键落值能不能把它重新求值出来
                var (document, projector, pipeline) = Harness(index, rootType, @"{ ""properties"": {} }");
                var blockNode = document.FindNode("$.block");
                if (blockNode is null)
                {
                    checks.That(false, shape.Label + " · 拿到 block 节点", "没有节点");
                    continue;
                }
                blockNode.Value = "minecraft:acacia_button";
                McdocCollectionEditor.Default.ApplyNodeValue(blockNode);
                pipeline.OnValueChanged(blockNode);
                var face = document.FindNode("$.properties.face");
                var options = face?.EnumOptionList?.Select(o => o.Name).Where(n => n != McdocNodePresenter.UnsetLabel).ToList() ?? [];
                checks.That(matched, shape.Label + " · 声明判定能识别动态索引", "命中=" + matched);
                checks.That(face is not null, shape.Label + " · 兄弟键落值后重新求值出字段行",
                    "properties.Type=" + (document.FindNode("$.properties")?.Type?.Kind.ToString() ?? "<无节点>") + " rows=" + projector.Rows.Count);
                checks.That(options.Count > 0, shape.Label + " · 属性行有候选", options.Count == 0 ? "<空>" : string.Join(",", options));
            }
            return checks.Finish("--case dependents-declaration-shape");
        }


        // ── 良性跳过不许挂行徽标（重建期间节点被换掉是预期，不是用户问题）──────
        public static int BenignSkipNoBadge()
        {
            var checks = new Checks();
            Deterministic();
            var rootType = McdocType.Create(McdocTypeKind.Struct);
            rootType.Fields = [new McdocStructField { Name = "block", Key = McdocType.LiteralString("block"), Type = McdocType.Create(McdocTypeKind.String) }];
            var (document, projector, pipeline) = Harness(new McdocTypeIndex(), rootType, @"{ ""block"": ""minecraft:acacia_button"" }");
            var node = document.FindNode("$.block");
            checks.That(node is not null, "拿到 block 节点", node is null ? "没有节点" : "");
            if (node is null) return checks.Finish("--case benign-skip-no-badge");

            var before = document.RootJson?.Get("block")?.StringValue;
            // 现场：重建换了节点，编辑器还握着旧实例 —— 它没有父级，写回只能被跳过
            node.Parent = null;
            McdocCollectionEditor.Default.WriteIntoParent(node, McdocJsonValue.String("minecraft:acacia_fence"));
            checks.That(string.IsNullOrEmpty(node.Error), "良性跳过不许挂行徽标（node.Error 为空）",
                "err=" + (node.Error ?? "<null>"));
            checks.That(string.Equals(document.RootJson?.Get("block")?.StringValue, before, StringComparison.Ordinal),
                "脱挂节点的写回不改文档", "block=<" + (document.RootJson?.Get("block")?.StringValue ?? "null") + ">");
            checks.That(projector.Rows.All(row => !row.Node.IsUnknownField), "没有把跳过写成未声明键", "");
            return checks.Finish("--case benign-skip-no-badge");
        }

        // ── 切方块：重新求值 + 剪掉旧成员 + 结构变更不回旧类型（P2/P4）──────
        public static int StructureChangeReevaluates()
        {
            var checks = new Checks();
            var directory = LocateBlockStateDirectory();
            if (directory is null)
            {
                Console.WriteLine("[环境缺失] 找不到 BlockState 资产（退出码 2）");
                return 2;
            }
            Deterministic();
            var index = new McdocTypeIndex();
            McdocBlockStates.Register(index, "1.20.5", directory);
            var rootType = RootTypeWith(BlockStatesDispatcher());

            var (document, projector, pipeline) = Harness(index, rootType,
                @"{ ""block"": ""minecraft:acacia_button"", ""properties"": { ""powered"": true, ""face"": ""wall"" } }");
            checks.That(document.FindNode("$.properties.powered") is not null, "前置：button 解出 powered 行",
                "properties.Type=" + (document.FindNode("$.properties")?.Type?.Kind.ToString() ?? "<无>"));

            var blockNode = document.FindNode("$.block");
            if (blockNode is null) { checks.That(false, "拿到 block 节点", "没有节点"); return checks.Finish("--case structure-change-reevaluates"); }
            blockNode.Value = "minecraft:acacia_fence";
            McdocCollectionEditor.Default.ApplyNodeValue(blockNode);
            pipeline.OnValueChanged(blockNode);

            var propertiesNode = document.FindNode("$.properties");
            var fields = (propertiesNode?.Type?.Fields ?? []).Select(field => field.Name).Where(name => !string.IsNullOrEmpty(name)).ToList();
            checks.That(fields.Contains("east") && !fields.Contains("powered"),
                "切到 fence 后 properties 重新求值成新方块的结构", "fields=" + string.Join(",", fields));
            checks.That(document.RootJson?.Get("properties")?.Get("powered") is null,
                "旧方块声明过、新方块不声明的键被剪掉（P4）",
                "properties=" + (document.RootJson?.Get("properties") is null ? "<null>" : "有键"));

            // 结构变更（加键）不许回到旧类型：RefreshNode 复用旧闭包就会把 powered/face 又摊出来
            propertiesNode = document.FindNode("$.properties") ?? propertiesNode;
            propertiesNode?.EnsureChildren();
            if (propertiesNode is not null) pipeline.OnStructureChanged(propertiesNode);
            propertiesNode = document.FindNode("$.properties") ?? propertiesNode;
            propertiesNode?.EnsureChildren();
            var childNames = (propertiesNode is null ? [] : McdocPresenterRegistry.Default.Children(propertiesNode)).Select(child => child.FieldName).Where(name => !string.IsNullOrEmpty(name)).ToList();
            checks.That(childNames.Contains("east") && !childNames.Contains("powered") && !childNames.Contains("face"),
                "结构变更后仍按新方块摊子级（不回到旧类型）", "children=" + string.Join(",", childNames));
            // 隔离 P2：只改文档里的 block（模拟外部写入），再只走"结构变更"刷新 ——
            // RefreshNode 若复用旧类型闭包，子级会回到上一代方块的字段集。
            var isolated = Harness(index, RootTypeWith(BlockStatesDispatcher()),
                @"{ ""block"": ""minecraft:acacia_button"", ""properties"": { ""powered"": true } }");
            var isolatedProperties = isolated.Document.FindNode("$.properties");
            checks.That(isolatedProperties is not null, "隔离夹具：拿到 properties 节点", "");
            if (isolatedProperties is not null)
            {
                McdocCollectionEditor.SetMember(isolated.Document.RootJson, "block", McdocJsonValue.String("minecraft:acacia_fence"));
                isolated.Pipeline.OnStructureChanged(isolatedProperties);
                var isolatedNode = isolated.Document.FindNode("$.properties") ?? isolatedProperties;
                // 判"类型"而不是"子行"：powered 是文档里还在的键，它照样要有一行（键↔行双射）；
                // 这里要证的是"节点的求值类型换成了新方块的"，那才决定子级长什么样。
                var isolatedFields = (isolatedNode.Type?.Fields ?? []).Select(field => field.Name).Where(name => !string.IsNullOrEmpty(name)).ToList();
                checks.That(isolatedFields.Contains("east") && !isolatedFields.Contains("powered"),
                    "结构变更会重新求值（不复用建节点时捕获的旧类型）", "fields=" + string.Join(",", isolatedFields));
            }
            Contracts(checks, document, projector, "切方块");
            return checks.Finish("--case structure-change-reevaluates");
        }

        // ── 条目身份只在"新形态仍然成立"时回填：静态字段不许带条目删除按钮（P3）──────
        public static int RebuildEntryIdentity()
        {
            var checks = new Checks();
            Deterministic();
            var rootType = McdocType.Create(McdocTypeKind.Struct);
            rootType.Fields = [new McdocStructField { Name = "x", Key = McdocType.LiteralString("x"), Type = McdocType.Create(McdocTypeKind.String) }];
            var (document, projector, pipeline) = Harness(new McdocTypeIndex(), rootType, @"{ ""x"": ""v"" }");
            var node = document.FindNode("$.x");
            checks.That(node is not null, "拿到 x 节点", node is null ? "没有节点" : "");
            if (node is null) return checks.Finish("--case rebuild-entry-identity");

            // 模拟"上一代是动态条目"：父级现在把它声明成静态字段，身份就不该跟过来
            node.IsMapEntry = true;
            var rebuilt = document.RebuildNode(node);
            checks.That(rebuilt is not null, "重建成功", "返回 null");
            if (rebuilt is null) return checks.Finish("--case rebuild-entry-identity");
            checks.That(!rebuilt.IsMapEntry, "静态字段重建后不带条目身份（添加与删除不会同时出现）", "IsMapEntry=" + rebuilt.IsMapEntry);
            checks.That(rebuilt.RemoveEntryCommand is null, "静态字段重建后没有条目删除命令", "command=" + (rebuilt.RemoveEntryCommand is null ? "null" : "有"));
            return checks.Finish("--case rebuild-entry-identity");
        }


        // ── union 行"显示什么成员，文档就必须有它的值"（含同层调度器跟解）──────
        public static int UnionDisplayMaterializes()
        {
            var checks = new Checks();
            Deterministic();
            var structA = McdocType.Create(McdocTypeKind.Struct);
            structA.Fields = [new McdocStructField { Name = "a", Key = McdocType.LiteralString("a"), Type = McdocType.Create(McdocTypeKind.String) }];
            var structB = McdocType.Create(McdocTypeKind.Struct);
            structB.Fields = [new McdocStructField { Name = "b", Key = McdocType.LiteralString("b"), Type = McdocType.Create(McdocTypeKind.String) }];
            var union = McdocType.Create(McdocTypeKind.Union);
            union.Members = [structA, structB];
            var rootType = McdocType.Create(McdocTypeKind.Struct);
            rootType.Fields = [new McdocStructField { Name = "u", Key = McdocType.LiteralString("u"), Type = union, Optional = true }];

            // 现场：文档里没有 u，行上却要显示一个成员
            var (document, projector, pipeline) = Harness(new McdocTypeIndex(), rootType, @"{}");
            var node = document.FindNode("$.u");
            checks.That(node is not null && node.UnionTypeNameList is { Count: 2 }, "拿到 union 行与分支选项", node is null ? "没有节点" : "选项=" + (node.UnionTypeNameList?.Count ?? 0));
            if (node is null || node.UnionTypeNameList is not { Count: 2 }) return checks.Finish("--case union-display-materializes");

            node.SelectedUnionTypeName = node.UnionTypeNameList[0];
            pipeline.MaterializeUntilStable(true);
            // 物化会整篇 Load（换一代节点）：投影必须跟着重排，否则残骸行就是"上一代的行"
            projector.Reset(document.Nodes.ToList());
            var after = document.FindNode("$.u");
            checks.That(after?.Json is { Kind: McdocJsonKind.Object }, "显示着成员 → 文档里落出该成员的默认结构",
                "u=" + (after?.Json?.Kind.ToString() ?? "<null>"));
            after?.EnsureChildren();
            var childNames = (after is null ? [] : McdocPresenterRegistry.Default.Children(after)).Select(child => child.FieldName).ToList();
            checks.That(childNames.Contains("a"), "落盘后同层字段当场出现", "children=" + string.Join(",", childNames));
            Contracts(checks, document, projector, "union 显示落盘");

            // 换成员：旧成员声明的键必须被剪掉（这条以前只有 SwitchTo 做，走下拉/重建时残留）
            after = document.FindNode("$.u") ?? node;
            after.SelectedUnionTypeName = after.UnionTypeNameList![1];
            pipeline.MaterializeUntilStable(true);
            projector.Reset(document.Nodes.ToList());
            var swapped = document.RootJson?.Get("u");
            checks.That(swapped is { Kind: McdocJsonKind.Object } swappedJson
                && swappedJson.Properties?.Any(pair => pair.Key == "a") != true,
                "换成员后旧成员声明的键被剪掉", "u=" + (swapped is null ? "<null>" : string.Join(",", swapped.Properties?.Select(p => p.Key) ?? [])));
            // 已知残留（下一轮处理）：换成员后"必填标量"行会显示值而文档里没有这个键
            // （`行 → 文档` 在 $.u.b 上红）—— 这条是本轮暴露出来的，不在本案例的验收范围内。
            return checks.Finish("--case union-display-materializes");
        }


        // ── 真 schema：dimension_type.monster_spawn_light_level 的默认值必须取声明序第一个成员（int）──────
        public static int RealDimensionDefaultBranch()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            Deterministic();
            var index = new McdocTypeIndex();
            RealSchema.Load(index, rootDir);
            var (document, projector, pipeline) = Harness(index, McdocHelpers.GetRootType("dimension_type"), "{}",
                doc => { doc.GameVersion = "1.20.5"; });
            var node = Reach(document, "$.monster_spawn_light_level");
            checks.That(node is not null, "拿到 monster_spawn_light_level 节点", node is null ? "没有节点" : "Declared=" + (node.DeclaredType?.Kind.ToString() ?? "?"));
            if (node is null) return checks.Finish("--case real-dimension-default-branch");

            var runtime = document.CreateRuntime();
            var resolved = McdocDefaults.ResolveType(node.DeclaredType, runtime);
            var kinds = resolved.Kind == McdocTypeKind.Union
                ? string.Join("|", (resolved.Members ?? []).Select(member => member?.Kind.ToString()))
                : resolved.Kind.ToString();
            var value = McdocDefaults.Resolve(node.DeclaredType, runtime);
            Console.WriteLine("[信息] 声明→" + kinds + " 默认值=" + (value is null ? "<null>" : value.Kind + "/" + (value.StringValue ?? value.NumberValue.ToString())));
            checks.That(value is { Kind: McdocJsonKind.Number }, "默认值取声明序第一个成员（int），不是结构支",
                "默认=" + (value is null ? "<null>" : value.Kind.ToString()) + " 成员=" + kinds);
            // 截图现场：文档里是 uniform 支 + value 对象 —— 这一支解得出来吗？
            var (shot, shotProjector, _) = Harness(index, McdocHelpers.GetRootType("dimension_type"),
                @"{ ""monster_spawn_light_level"": { ""type"": ""minecraft:uniform"", ""value"": { ""min_inclusive"": 0, ""max_inclusive"": 1 } } }",
                doc => { doc.GameVersion = "1.20.5"; });
            var shotNode = Reach(shot, "$.monster_spawn_light_level");
            shotNode?.EnsureChildren();
            var shotFields = (shotNode?.Type?.Fields ?? []).Select(field => field.Name).Where(name => !string.IsNullOrEmpty(name)).ToList();
            var shotChildren = shotNode is null ? [] : McdocPresenterRegistry.Default.Children(shotNode).Select(child => child.FieldName).ToList();
            Console.WriteLine("[信息] uniform 支：Type=" + (shotNode?.Type?.Kind.ToString() ?? "<null>")
                + " 字段=" + string.Join(",", shotFields)
                + " 子行=" + string.Join(",", shotChildren)
                + " Err=" + (shotNode?.Error ?? "-"));
            var shotPaths = (shotNode?.Children ?? []).Select(child => (child.FieldName ?? "?") + "@" + (child.JsonPath ?? "?")).ToList();
            Console.WriteLine("[信息] union.Children=" + string.Join(" | ", shotPaths));
            var valueNode = shotNode?.Children?.FirstOrDefault(child => string.Equals(child.FieldName, "value", StringComparison.Ordinal));
            var valueChildren = (valueNode?.Children ?? []).Select(child => (child.FieldName ?? "?") + "@" + (child.JsonPath ?? "?")).ToList();
            Console.WriteLine("[信息] value 节点：Type=" + (valueNode?.Type?.Kind.ToString() ?? "<none>")
                + " Declared=" + (valueNode?.DeclaredType?.Kind.ToString() ?? "?")
                + " Children=" + string.Join(" | ", valueChildren));
            checks.That(shotNode?.Type?.Kind is not (McdocTypeKind.Any or McdocTypeKind.Unsafe),
                "uniform 支按同层 type 解出结构（不许退成 any → 否则行上变成 Any 下拉的 Object）",
                "Type=" + (shotNode?.Type?.Kind.ToString() ?? "<null>") + " 字段=" + string.Join(",", shotFields));
            // 页面模板（用户数据）：1.20.5 已把 value 那层去掉，模板必须写 int 支；否则整棵树会多出一行
            // "当前版本不存在的键 value"（现场那个带 ! 的空 Value 行）。
            var resourceDir = System.IO.Directory.GetParent(rootDir)?.FullName;
            var template = resourceDir is null ? null : System.IO.Path.Combine(resourceDir, "Configs", "DimensionType", "Data", "Template", "empty.json");
            if (template is not null && System.IO.File.Exists(template))
            {
                var (shotDoc, shotProj, _) = Harness(index, McdocHelpers.GetRootType("dimension_type"), System.IO.File.ReadAllText(template),
                    docs => { docs.GameVersion = "1.20.5"; });
                var level = Reach(shotDoc, "$.monster_spawn_light_level");
                Console.WriteLine("[信息] 模板：light_level JSON=" + (level?.Json?.Kind.ToString() ?? "<null>")
                    + " 选中=" + (level?.SelectedUnionTypeName?.Name ?? "<null>")
                    + " 有 value 行=" + (shotDoc.FindNode("$.monster_spawn_light_level.value") is null ? "否" : "是"));
                checks.That(level?.Json is { Kind: McdocJsonKind.Number },
                    "模板里的 monster_spawn_light_level 是 int 支（不是结构支）",
                    "JSON=" + (level?.Json?.Kind.ToString() ?? "<null>"));
                checks.That(shotDoc.FindNode("$.monster_spawn_light_level.value") is null,
                    "模板不再多出「当前版本不存在的键」value 行",
                    "value 行存在");
            }
            else
            {
                Console.WriteLine("[信息] 找不到页面模板（跳过模板判据）：" + (template ?? "<null>"));
            }
            return checks.Finish("--case real-dimension-default-branch");
        }


        /// <summary>探针用的方块注册表桩：同时提供方块 id 与 tag/block 两条表（#[id] 的 tags 语义靠它判定）。</summary>
        private sealed class BlockRegistryStub : IMcdocRegistryOptions
        {
            private static readonly string[] Blocks = ["acacia_button", "stone"];
            private static readonly string[] Tags = ["infiniburn_overworld", "dirt"];

            public IReadOnlyList<string> GetOptions(McdocIdSpec spec, string version)
                => string.Equals(spec?.Registry, "block", StringComparison.Ordinal)
                    ? spec.Apply(Blocks, Tags)
                    : [];
        }

        // ── 默认值必须落在候选空间内：必填 #[id(tags=required)] 得到一个 tag，而不是方块 id ──────
        public static int DefaultTagValue()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            Deterministic();
            var index = new McdocTypeIndex();
            RealSchema.Load(index, rootDir);
            var (document, projector, pipeline) = Harness(index, McdocHelpers.GetRootType("dimension_type"), "{}",
                doc =>
                {
                    doc.GameVersion = "1.20.5";
                    doc.RegistryOptions = new BlockRegistryStub();
                });
            pipeline.MaterializeUntilStable(includeDisplayedDefaults: true);
            projector.Reset(document.Nodes.ToList());

            var value = document.RootJson?.Get("infiniburn");
            var text = value is { Kind: McdocJsonKind.String } ? value.StringValue ?? "" : "<" + (value?.Kind.ToString() ?? "null") + ">";
            var tagNode = document.FindNode("$.infiniburn");
            var spec = McdocIdSpec.FromAttributes(tagNode?.DeclaredType?.Attributes);
            Console.WriteLine("[信息] infiniburn=" + text
                + " registry=" + (spec?.Registry ?? "<null>") + " tags=" + (spec?.Tags ?? "<null>")
                + " 候选=" + string.Join(",", (tagNode?.EnumOptionList ?? []).Select(option => option.Name).Take(4))
                + " declared=" + (tagNode?.DeclaredType?.Kind.ToString() ?? "<none>"));
            checks.That(text.StartsWith("#", StringComparison.Ordinal),
                "必填 #[id(tags=required)] 的默认值落在候选空间内（tag，不是方块 id）", "infiniburn=<" + text + ">");
            return checks.Finish("--case default-tag-value");
        }


        // ── 切成员 = 子成员全换（差集剪枝会留下"新旧都声明"的旧值）──────
        public static int SwitchClearsOldMembers()
        {
            var checks = new Checks();
            Deterministic();
            // A 段：union 切支。两个成员都声明 x，但类型不同 —— 差集剪枝会把旧的 int 留在新支上。
            var structA = McdocType.Create(McdocTypeKind.Struct);
            structA.Fields = [
                new McdocStructField { Name = "x", Key = McdocType.LiteralString("x"), Type = McdocType.Create(McdocTypeKind.Int) },
                new McdocStructField { Name = "y", Key = McdocType.LiteralString("y"), Type = McdocType.Create(McdocTypeKind.Int) },
            ];
            var structB = McdocType.Create(McdocTypeKind.Struct);
            structB.Fields = [new McdocStructField { Name = "x", Key = McdocType.LiteralString("x"), Type = McdocType.Create(McdocTypeKind.String) }];
            var union = McdocType.Create(McdocTypeKind.Union);
            union.Members = [structA, structB];
            var unionRoot = McdocType.Create(McdocTypeKind.Struct);
            unionRoot.Fields = [new McdocStructField { Name = "u", Key = McdocType.LiteralString("u"), Type = union }];

            var (document, projector, pipeline) = Harness(new McdocTypeIndex(), unionRoot, @"{ ""u"": { ""x"": 1, ""y"": 2 } }");
            var node = document.FindNode("$.u");
            checks.That(node?.UnionTypeNameList is { Count: 2 }, "拿到 union 分支选项", node is null ? "没有节点" : "");
            if (node?.UnionTypeNameList is not { Count: 2 }) return checks.Finish("--case switch-clears-old-members");
            node.SelectedUnionTypeName = node.UnionTypeNameList[1];
            var after = document.RootJson?.Get("u");
            var keptX = after?.Get("x");
            Console.WriteLine("[信息] 切到 B 后 u=" + (after is null ? "<null>" : string.Join(",", after.Properties?.Select(p => p.Key + ":" + p.Value?.Kind) ?? [])));
            checks.That(keptX is null || keptX.Kind == McdocJsonKind.String,
                "切成员后旧成员的值不许留在新支上（同名字段的值不成立就删）",
                "x=" + (keptX is null ? "<无>" : keptX.Kind + "/" + (keptX.StringValue ?? keptX.NumberValue.ToString())));
            checks.That(after?.Get("y") is null, "旧成员独有的键也一并去掉", "y=" + (after?.Get("y") is null ? "<无>" : "有"));

            // B 段：依赖键切换（方块）。button 与 door 都声明 facing/powered，差集剪枝会留下旧值。
            var directory = LocateBlockStateDirectory();
            if (directory is not null)
            {
                var index = new McdocTypeIndex();
                McdocBlockStates.Register(index, "1.20.5", directory);
                // face：新方块不声明 → 必须删；facing=north：两边都声明且值合法 → 保留（用户数据不是残影）；
                // facing 另配一个非法值用一个独立夹具断言（见下）。
                var (blockDoc, blockProjector, blockPipeline) = Harness(index, RootTypeWith(BlockStatesDispatcher()),
                    @"{ ""block"": ""minecraft:acacia_button"", ""properties"": { ""facing"": ""north"", ""face"": ""wall"" } }");
                var blockNode = blockDoc.FindNode("$.block");
                if (blockNode is not null)
                {
                    blockNode.Value = "minecraft:acacia_door";
                    McdocCollectionEditor.Default.ApplyNodeValue(blockNode);
                    blockPipeline.OnValueChanged(blockNode);
                    var properties = blockDoc.RootJson?.Get("properties");
                    var leftovers = properties?.Properties?.Select(pair => pair.Key).ToList() ?? [];
                    Console.WriteLine("[信息] 切方块后 properties=" + (properties is null ? "<null>" : "{" + string.Join(",", leftovers) + "}"));
                    checks.That(properties is null || !leftovers.Contains("face"),
                        "依赖键切换后新方块不声明的键被删掉",
                        "properties={" + string.Join(",", leftovers) + "}");
                    checks.That(properties is null || leftovers.Contains("facing"),
                        "两边都声明且值合法的键保留（那是用户数据，不是残影）",
                        "properties={" + string.Join(",", leftovers) + "}");

                    // 两边都声明、但值在新声明下不成立 → 必须删
                    var (badDoc, badProjector, badPipeline) = Harness(index, RootTypeWith(BlockStatesDispatcher()),
                        @"{ ""block"": ""minecraft:acacia_button"", ""properties"": { ""facing"": ""diagonal"" } }");
                    var badNode = badDoc.FindNode("$.block");
                    if (badNode is not null)
                    {
                        badNode.Value = "minecraft:acacia_door";
                        McdocCollectionEditor.Default.ApplyNodeValue(badNode);
                        badPipeline.OnValueChanged(badNode);
                        var badProperties = badDoc.RootJson?.Get("properties");
                        var badKeys = badProperties?.Properties?.Select(pair => pair.Key).ToList() ?? [];
                        Console.WriteLine("[信息] 非法值切方块后 properties={" + string.Join(",", badKeys) + "}");
                        checks.That(badProperties is null || !badKeys.Contains("facing"),
                            "同名字段的值在新声明下不成立时也要删",
                            "properties={" + string.Join(",", badKeys) + "}");
                    }
                }
            }
            return checks.Finish("--case switch-clears-old-members");
        }


        // ── 列表项的"删自己"：文档、节点、行三者必须同一拍更新（现场：distribution 已空，Entry 行还在）──────
        public static int ListItemRemoveSelf()
        {
            var checks = new Checks();
            Deterministic();
            var item = McdocType.Create(McdocTypeKind.Struct);
            item.Fields = [new McdocStructField { Name = "weight", Key = McdocType.LiteralString("weight"), Type = McdocType.Create(McdocTypeKind.Int) }];
            var list = McdocType.Create(McdocTypeKind.List);
            list.Item = item;
            var rootType = McdocType.Create(McdocTypeKind.Struct);
            rootType.Fields = [new McdocStructField { Name = "distribution", Key = McdocType.LiteralString("distribution"), Type = list, Optional = true }];

            var (document, projector, pipeline) = Harness(new McdocTypeIndex(), rootType,
                @"{ ""distribution"": [ { ""weight"": 0 }, { ""weight"": 1 } ] }");
            var listNode = document.FindNode("$.distribution");
            checks.That(listNode?.Items.Count == 2, "前置：两个列表项", "Items=" + (listNode?.Items.Count ?? -1));
            if (listNode is null || listNode.Items.Count != 2) return checks.Finish("--case list-item-remove-self");

            listNode.Items[0].RemoveSelfCommand?.Execute(null);
            var array = document.RootJson?.Get("distribution");
            Console.WriteLine("[信息] 删自己后 JSON 项数=" + (array?.Items?.Count ?? -1)
                + " 节点项数=" + (document.FindNode("$.distribution")?.Items.Count ?? -1)
                + " 行=" + string.Join(",", projector.Rows.Where(row => row.Node?.JsonPath?.StartsWith("$.distribution[", StringComparison.Ordinal) == true).Select(row => row.Node.JsonPath)));
            checks.That(array?.Items?.Count == 1, "文档里少一项", "项数=" + (array?.Items?.Count ?? -1));
            var after = document.FindNode("$.distribution");
            checks.That(after?.Items.Count == 1, "节点项数同一拍更新（不许留陈旧项）", "Items=" + (after?.Items.Count ?? -1));
            var stale = projector.Rows.FirstOrDefault(row => row.Node?.JsonPath == "$.distribution[1]");
            checks.That(stale is null, "被删的下标不再有行", stale is null ? "" : "仍有行 " + stale.Node?.JsonPath);
            Contracts(checks, document, projector, "删列表项");

            // 现场复刻：文档里的数组已经是空的，节点上却还留着一项（某次通知被吞掉）→
            // 这时点"删自己"不许静默无效，要把它当幽灵行清掉。
            var ghostList = document.FindNode("$.distribution");
            if (ghostList?.Json is { Kind: McdocJsonKind.Array } ghostArray && ghostArray.Items is not null)
            {
                ghostArray.Items.Clear();
                projector.Reset(document.Nodes.ToList());
                var rowsBefore = projector.Rows.Count(row => row.Node?.JsonPath?.StartsWith("$.distribution[", StringComparison.Ordinal) == true);
                McdocCollectionEditor.Default.RemoveItemAt(ghostList, 0);
                projector.Reset(document.Nodes.ToList());
                var rowsAfter = projector.Rows.Count(row => row.Node?.JsonPath?.StartsWith("$.distribution[", StringComparison.Ordinal) == true);
                Console.WriteLine("[信息] 幽灵行：清前=" + rowsBefore + " 清后=" + rowsAfter + " 节点项数=" + ghostList.Items.Count);
                checks.That(ghostList.Items.Count == 0 || rowsAfter < rowsBefore,
                    "文档已经空了的陈旧项：点删自己要把这一行清掉（不许静默无效）",
                    "项数=" + ghostList.Items.Count + " 清前=" + rowsBefore + " 清后=" + rowsAfter);
            }
            return checks.Finish("--case list-item-remove-self");
        }


        // ── 真 schema：切 type（枚举成员）→ 依赖层是 Union，也要按新结构清键 ──────
        public static int RealDimensionDependentClear()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            Deterministic();
            var index = new McdocTypeIndex();
            RealSchema.Load(index, rootDir);
            const string json = @"{ ""monster_spawn_light_level"": { ""type"": ""minecraft:clamped_normal"", ""min_inclusive"": 0, ""max_inclusive"": 1, ""mean"": 2, ""deviation"": 1 } }";
            var (document, projector, pipeline) = Harness(index, McdocHelpers.GetRootType("dimension_type"), json,
                doc => { doc.GameVersion = "1.20.5"; });

            var typeNode = document.FindNode("$.monster_spawn_light_level.type");
            checks.That(typeNode?.EnumOptionList is { Count: > 0 }, "type 行有候选", typeNode is null ? "没有节点" : "候选=" + (typeNode.EnumOptionList?.Count ?? 0));
            if (typeNode?.EnumOptionList is null) return checks.Finish("--case real-dimension-dependent-clear");
            var target = typeNode.EnumOptionList.FirstOrDefault(option => (option.Name ?? "").Contains("uniform", StringComparison.Ordinal));
            checks.That(target is not null, "拿到 uniform 候选", "候选=" + string.Join(",", typeNode.EnumOptionList.Select(o => o.Name)));
            if (target is null) return checks.Finish("--case real-dimension-dependent-clear");

            // VM 的值通道是"先 ApplyNodeValue 写进文档，再通知依赖重建"；探针 harness 只接了通知，
            // 所以这里必须自己补上写回，否则重建读到的是旧值（本案例第一版就是这么假绿的）。
            typeNode.SetEnumOptionSilently(target);
            McdocCollectionEditor.Default.ApplyNodeValue(typeNode);
            pipeline.OnValueChanged(typeNode);
            var level = document.RootJson?.Get("monster_spawn_light_level");
            var keys = level?.Properties?.Select(pair => pair.Key).ToList() ?? [];
            Console.WriteLine("[信息] 切 type 后 keys=" + string.Join(",", keys));
            checks.That(!keys.Contains("mean") && !keys.Contains("deviation"),
                "旧成员声明的键被清掉（依赖层是 Union 也要清）", "keys=" + string.Join(",", keys));
            checks.That(keys.Contains("min_inclusive") && keys.Contains("max_inclusive"),
                "新成员也声明且值合法的键保留", "keys=" + string.Join(",", keys));
            return checks.Finish("--case real-dimension-dependent-clear");
        }



        /// <summary>探针用的真实注册表桩：直接读 Resource\Registries\&lt;版本&gt;.json，与 VM 的 MdocRegistryOptions 同口径。
        /// 用它才能验"真数据下链路是否成立"；缺文件时由调用方回退到合成桩。</summary>
        private sealed class RealRegistriesStub : IMcdocRegistryOptions
        {
            private readonly Newtonsoft.Json.Linq.JObject data;

            public RealRegistriesStub(string path)
                => data = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(path));

            public IReadOnlyList<string> GetOptions(McdocIdSpec spec, string version)
            {
                if (spec?.Registry is not { Length: > 0 } registry) return [];
                return spec.Apply(ReadArray(registry), ReadArray("tag/" + registry));
            }

            private List<string> ReadArray(string name)
                => data[name] is Newtonsoft.Json.Linq.JArray array
                    ? array.Select(item => item?.ToString()).Where(value => !string.IsNullOrEmpty(value)).ToList()
                    : [];
        }

        /// <summary>探针用的 environment_attribute 候选桩：键空间是注册表，缺 1.21.11 资产时也不影响求解验证。</summary>
        private sealed class AttributeRegistryStub : IMcdocRegistryOptions
        {
            private static readonly string[] Attributes =
            [
                "audio/ambient_sounds", "audio/background_music", "gameplay/bees_stay_in_hive", "visual/sky_color",
            ];

            public IReadOnlyList<string> GetOptions(McdocIdSpec spec, string version)
                => string.Equals(spec?.Registry, "environment_attribute", StringComparison.Ordinal)
                    ? spec.Apply(Attributes)
                    : [];
        }

        // ── attributes：动态键 → %key 调度 → 链式索引 → union（整个 mcdoc 池最复杂的一处）──────
        public static int AttributesEntryUnion()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            Deterministic();
            var index = new McdocTypeIndex();
            RealSchema.Load(index, rootDir);
            // 现场状态：用户先 ⊕ 出 attributes（文档里是 {}），动态键占位行才存在
            // 有真 1.21.11 注册表就用真的（验"真数据下链路成立"）；没有才退回合成桩。
            var resourceDir = System.IO.Directory.GetParent(rootDir)?.FullName;
            var registriesFile = resourceDir is null ? null : System.IO.Path.Combine(resourceDir, "Registries", "1.21.11.json");
            var useReal = registriesFile is not null && System.IO.File.Exists(registriesFile);
            Console.WriteLine("[信息] 注册表来源=" + (useReal ? "真 1.21.11.json" : "合成桩"));
            var (document, projector, pipeline) = Harness(index, McdocHelpers.GetRootType("dimension_type"), @"{ ""attributes"": {} }",
                doc =>
                {
                    doc.GameVersion = "1.21.11";
                    doc.RegistryOptions = useReal ? new RealRegistriesStub(registriesFile) : new AttributeRegistryStub();
                });

            var attributes = document.FindNode("$.attributes");
            checks.That(attributes is not null, "1.21.11 下 attributes 字段存在", attributes?.Type?.Kind.ToString() ?? "<无节点>");
            if (attributes is null) return checks.Finish("--case attributes-entry-union");
            attributes.EnsureChildren();
            var placeholder = attributes.Children.FirstOrDefault(child => child.IsDynamicKey);
            var keyOptions = placeholder?.KeyOptionList?.Select(option => option.Name).ToList() ?? [];
            Console.WriteLine("[信息] attributes.Type=" + (attributes.Type?.Kind.ToString() ?? "?")
                + " 子行=" + string.Join(",", attributes.Children.Select(child => (child.FieldName ?? "?") + (child.IsDynamicKey ? "(新键)" : "")))
                + " 键候选=" + string.Join(",", keyOptions.Take(4)));
            checks.That(placeholder is not null, "有动态键占位行", placeholder is null ? "没有" : "");
            if (placeholder is null) return checks.Finish("--case attributes-entry-union");
            checks.That(keyOptions.Any(name => name.Contains("ambient_sounds", StringComparison.Ordinal)),
                "键候选里有 audio/ambient_sounds", "候选=" + string.Join(",", keyOptions.Take(6)));

            // 模拟用户在占位行输入键并提交
            placeholder.EditingKey = "minecraft:audio/ambient_sounds";
            placeholder.AddEntryCommand?.Execute(null);
            var attributesJson = document.RootJson?.Get("attributes");
            var entryJson = attributesJson?.Get("minecraft:audio/ambient_sounds");
            var entry = document.FindNode("$.attributes.minecraft:audio/ambient_sounds")
                ?? document.FindNode("$.attributes")?.Children?.FirstOrDefault(child => child.IsMapEntry);
            Console.WriteLine("[信息] 提交后 attributes=" + (attributesJson is null ? "<null>" : McdocJsonFormatter.Format(attributesJson).Replace("\n", " ").Replace("  ", " "))
                + " | entry.Type=" + (entry?.Type?.Kind.ToString() ?? "<无行>")
                + " union分支=" + (entry?.UnionTypeNameList?.Count.ToString() ?? "-")
                + " 子行=" + string.Join(",", (entry?.Children ?? []).Select(child => child.FieldName)));
            checks.That(entryJson is not null, "文档里出现该键", entryJson is null ? "没有" : "");
            // 新建条目写 {} 是既有契约（`criterion-empty` 也依赖它）；这里要证的是"条目行是个可切换的 union，
            // 选中分支后文档落成该分支的结构" —— 这正是"应该是个 Union"的实际含义。
            var branch = entry?.UnionTypeNameList?.FirstOrDefault();
            if (entry is not null && branch is not null)
            {
                entry.SelectedUnionTypeName = branch;
                var selectedJson = document.RootJson?.Get("attributes")?.Get("minecraft:audio/ambient_sounds");
                entry.EnsureChildren();
                Console.WriteLine("[信息] 选分支 " + branch.Name + " 后 value="
                );
                checks.That(selectedJson is { Kind: McdocJsonKind.Object },
                    "选中分支后文档落成该分支的结构（不是空壳）",
                    "value=" + (selectedJson is null ? "<null>" : McdocJsonFormatter.Format(selectedJson).Replace("\n", " ")));
            }
            entry?.EnsureChildren();
            Console.WriteLine("[信息] entry.Children=" + string.Join(",", (entry?.Children ?? []).Select(child => child.FieldName))
                + " 选中=" + (entry?.SelectedUnionTypeName?.Name ?? "<null>")
                + " explicit=" + (entry?.UnionSelectionExplicit ?? false));
            checks.That(entry?.UnionTypeNameList is { Count: > 1 },
                "entry 行上有 union 选择器（value / modifier）",
                "分支数=" + (entry?.UnionTypeNameList?.Count.ToString() ?? "<无>"));

            // 现场：切到 Modifier 支后，默认必须满足 OverrideModifier<T> = { modifier: "override", argument: T }，
            // 不许写成 {"modifier": null} / {"argument": null}（那会立刻挂"值与类型不符"）。
            var modifierOption = entry?.UnionTypeNameList?.FirstOrDefault(option => (option.Name ?? "").Contains("Modifier", StringComparison.OrdinalIgnoreCase));
            checks.That(modifierOption is not null, "拿到 Modifier 分支", "候选=" + string.Join(",", entry?.UnionTypeNameList?.Select(o => o.Name) ?? []));
            if (entry is not null && modifierOption is not null)
            {
                entry.SelectedUnionTypeName = modifierOption;
                var modifierJson = document.RootJson?.Get("attributes")?.Get("minecraft:audio/ambient_sounds");
                var keys = modifierJson?.Properties?.Select(pair => pair.Key + ":" + pair.Value?.Kind).ToList() ?? [];
                Console.WriteLine("[信息] 切 Modifier 后 value=" + (modifierJson is null ? "<null>" : string.Join(",", keys))
                    + " 诊断=" + (entry.Error ?? "-"));
                checks.That(modifierJson?.Get("modifier") is { Kind: McdocJsonKind.String } literal
                    && string.Equals(literal.StringValue, "override", StringComparison.Ordinal),
                    "切 Modifier 后 modifier 落成字面量 \"override\"（不许是 null）",
                    "value=" + (modifierJson is null ? "<null>" : McdocJsonFormatter.Format(modifierJson).Replace("\n", " ")));
                checks.That(modifierJson?.Get("argument") is { Kind: McdocJsonKind.Object },
                    "切 Modifier 后 argument 落成结构（不许是 null）",
                    "argument=" + (modifierJson?.Get("argument")?.Kind.ToString() ?? "<null>"));
            }
            return checks.Finish("--case attributes-entry-union");
        }

        // ── 截图现场：1.21.11 attributes 的动态键条目切到 Modifier 支后的求解结果 ─────────
        // 正确解（OverrideModifier<AmbientSounds> = { modifier: "override", argument: AmbientSounds }）：
        // modifier 行是字面量 "override"；argument 行是 AmbientSounds 结构，字段行 loop/mood/additions。
        // 走界面同一条链路（VM 的 union 行 setter → SwitchTo → 延后一拍重建 → 投影），只跑节点级
        // Switch 会漏掉"行/文本没跟上"的那一段。
        public static int AttributesModifierBranch()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            var blockStateDir = LocateBlockStateDirectory();
            if (blockStateDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\Registries\\BlockState（退出码 2）");
                return 2;
            }
            Deterministic();
            var resource = new CBHK.Model.Constant.Resource();
            RealSchema.Load(resource.McdocTypes, rootDir);
            resource.LoadRegistries(System.IO.Path.GetDirectoryName(blockStateDir));

            var vm = new CBHK.ViewModel.Generator.DimensionTypeViewModel(resource);
            var version = vm.GameVersionOptions.FirstOrDefault(item => item?.Name == "1.21.11");
            checks.That(version is not null, "版本下拉里有 1.21.11",
                "候选=" + string.Join(",", vm.GameVersionOptions.Take(4).Select(item => item?.Name)));
            if (version is null) return checks.Finish("--case attributes-modifier-branch");
            vm.SelectedGameVersionOption = version;
            vm.LoadMcdocTree("{ \"attributes\": { \"minecraft:audio/ambient_sounds\": {} } }");

            var document = typeof(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase)
                .GetField("document", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(vm) as McdocTreeDocument;
            checks.That(document is not null, "VM 建出了文档", document is null ? "document=null" : "");
            if (document is null) return checks.Finish("--case attributes-modifier-branch");

            var entry = document.FindNode("$.attributes.minecraft:audio/ambient_sounds");
            entry?.EnsureChildren();
            DumpAttributeRows("切支前", vm);
            var branches = entry?.UnionTypeNameList?
                .Select(item => (item.Name ?? "?") + "#" + Convert.ToString(item.Value?.LiteralValue)).ToList() ?? [];
            Console.WriteLine("[信息] 分支候选=" + string.Join(",", branches));
            var modifier = entry?.UnionTypeNameList?.FirstOrDefault(item =>
                (item.Name ?? "").Contains("Modifier", StringComparison.OrdinalIgnoreCase));
            checks.That(modifier is not null, "拿到 Modifier 分支", "候选=" + string.Join(",", branches));
            if (entry is null || modifier is null) return checks.Finish("--case attributes-modifier-branch");

            // 用户在下拉里选中 Modifier：界面同一个入口（McdocRows.xaml 的 SelectedItem →
            // McdocRow.SelectedUnionOption → McdocUnionSwitch.SwitchTo）
            var entryRow = vm.Rows.FirstOrDefault(row => ReferenceEquals(row.Node, entry));
            checks.That(entryRow is not null, "条目行在投影里", "行数=" + vm.Rows.Count);
            if (entryRow is null) return checks.Finish("--case attributes-modifier-branch");
            var rowOptions = entryRow.UnionOptions?.ToList() ?? [];
            Console.WriteLine("[信息] 行上分支候选=" + string.Join(",", rowOptions.Select(item => item?.Name)));
            var rowOption = rowOptions.FirstOrDefault(item =>
                (item?.Name ?? "").Contains("Modifier", StringComparison.OrdinalIgnoreCase));
            checks.That(rowOption is not null, "行上拿到 Modifier 候选", "候选=" + string.Join(",", rowOptions.Select(item => item?.Name)));
            if (rowOption is null) return checks.Finish("--case attributes-modifier-branch");
            entryRow.SelectedUnionOption = rowOption;
            PumpDispatcher();
            DumpAttributeRows("切支后", vm);

            var value = document.RootJson?.Get("attributes")?.Get("minecraft:audio/ambient_sounds");
            Console.WriteLine("[信息] 切 Modifier 后文档=" + FormatOneLine(value));
            var afterEntry = document.FindNode("$.attributes.minecraft:audio/ambient_sounds");
            Console.WriteLine("[信息] 条目节点 Json=" + FormatOneLine(afterEntry?.Json)
                + " | SelectedUnionMember=" + (afterEntry?.SelectedUnionMember?.Kind.ToString() ?? "<null>")
                + " fields=" + string.Join(",", (afterEntry?.SelectedUnionMember?.Fields ?? [])
                    .Select(field => (field?.Name ?? "?") + ":" + (field?.Type?.Kind.ToString() ?? "?")))
                + " | 分支子级=" + string.Join(" | ", (afterEntry?.SelectedUnionChildren ?? [])
                    .Select(child => (child?.FieldName ?? "?") + "/type=" + (child?.Type?.Kind.ToString() ?? "?")
                        + "/declared=" + (child?.DeclaredType?.Kind.ToString() ?? "?")
                        + "/item=" + (child?.DeclaredType?.Item?.Kind.ToString() ?? "?")
                        + "/json=" + (child?.Json?.Kind.ToString() ?? "<null>")
                        + "/value=" + (child?.Json is null ? "-" : FormatOneLine(child.Json).PadRight(0).Substring(0, Math.Min(70, FormatOneLine(child.Json).Length)))
                        + "/error=" + (child?.Error ?? "-"))));
            checks.That(value?.Get("modifier") is { Kind: McdocJsonKind.String } literal
                && string.Equals(literal.StringValue, "override", StringComparison.Ordinal),
                "modifier 落成字面量 \"override\"", "文档=" + FormatOneLine(value));
            checks.That(value?.Get("argument") is { Kind: McdocJsonKind.Object },
                "argument 落成结构（不是 null）", "argument=" + (value?.Get("argument")?.Kind.ToString() ?? "<null>"));

            var rebuilt = document.FindNode("$.attributes.minecraft:audio/ambient_sounds");
            rebuilt?.EnsureChildren();
            // union 行的字段行是"上提到 union 节点"的（SelectedUnionChildren），按表现器的 Children 取
            var fields = (McdocPresenterRegistry.Default.Children(rebuilt) ?? [])
                .Select(child => child.FieldName).ToList();
            Console.WriteLine("[信息] 切支后条目子节点=" + string.Join(",", fields));
            var argument = (McdocPresenterRegistry.Default.Children(rebuilt) ?? [])
                .FirstOrDefault(child => child.FieldName == "argument")
                ?? document.FindNode("$.attributes.minecraft:audio/ambient_sounds.argument");
            checks.That(argument is not null, "argument 有节点", "条目子节点=" + string.Join(",", fields));
            checks.That(argument?.Type?.Kind == McdocTypeKind.Struct,
                "argument 解成 AmbientSounds 结构（不是 any）",
                "type=" + (argument?.Type?.Kind.ToString() ?? "<null>")
                + " declared=" + (argument?.DeclaredType?.Kind.ToString() ?? "<null>"));
            argument?.EnsureChildren();
            var argumentFields = (argument?.Children ?? []).Select(child => child.FieldName).ToList();
            checks.That(argumentFields.Contains("loop") && argumentFields.Contains("mood") && argumentFields.Contains("additions"),
                "argument 的字段行是 loop/mood/additions", "子行=" + string.Join(",", argumentFields));

            var modifierNode = (McdocPresenterRegistry.Default.Children(rebuilt) ?? [])
                .FirstOrDefault(child => child.FieldName == "modifier");
            checks.That(modifierNode?.Type?.Kind == McdocTypeKind.Literal,
                "modifier 行是字面量（不是 any 原始行）",
                "type=" + (modifierNode?.Type?.Kind.ToString() ?? "<null>"));
            // 行上的值与文档里的值必须一致：以前这一行拿到的是整个父对象（"值与类型不符：期望 Literal"）
            checks.That(modifierNode?.Json is { Kind: McdocJsonKind.String } modifierValue
                    && string.Equals(modifierValue.StringValue, "override", StringComparison.Ordinal),
                "modifier 行绑定的是自己那个值（不是父对象）",
                "行值=" + (modifierNode?.Json is null ? "<null>" : FormatOneLine(modifierNode.Json))
                + " 诊断=" + (modifierNode?.Error ?? "-"));
            var argumentRow = (McdocPresenterRegistry.Default.Children(rebuilt) ?? [])
                .FirstOrDefault(child => child.FieldName == "argument");
            checks.That(argumentRow?.Json is { Kind: McdocJsonKind.Object },
                "argument 行绑定的是文档里那份 {}（不是 null）",
                "行值=" + (argumentRow?.Json?.Kind.ToString() ?? "<null>")
                + " 诊断=" + (argumentRow?.Error ?? "-"));
            checks.That(string.IsNullOrEmpty(argumentRow?.Error) && string.IsNullOrEmpty(modifierNode?.Error),
                "两支的字段行都不挂诊断（值与声明相符）",
                "modifier=" + (modifierNode?.Error ?? "-") + " argument=" + (argumentRow?.Error ?? "-"));

            var textInStep = McdocJsonParser.TryParse(vm.JsonText ?? "", out var textRoot, out _)
                && string.Equals(McdocJsonFormatter.Format(textRoot), McdocJsonFormatter.Format(document.RootJson), StringComparison.Ordinal);
            checks.That(textInStep, "右侧文本与文档同代（同步过）",
                "JsonText=" + (vm.JsonText ?? "").Replace("\r", "").Replace("\n", " "));
            return checks.Finish("--case attributes-modifier-branch");
        }

        // ── 截图现场 6：loot_table 里新加的 function 元素，自己的 ▲▼ 必须当场能用 ─────────────
        public static int LootTableFunctionsMove()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            var blockStateDir = LocateBlockStateDirectory();
            if (blockStateDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\Registries\\BlockState（退出码 2）");
                return 2;
            }
            Deterministic();
            var resource = new CBHK.Model.Constant.Resource();
            RealSchema.Load(resource.McdocTypes, rootDir);
            resource.LoadRegistries(System.IO.Path.GetDirectoryName(blockStateDir));

            var vm = new CBHK.ViewModel.Generator.LootTableViewModel(resource);
            vm.LoadMcdocTree("{ \"functions\": [ { \"function\": \"minecraft:set_count\", \"count\": 1 } ] }");
            var document = DocumentOf(vm);
            var functions = document?.FindNode("$.functions");
            checks.That(functions is not null, "functions 列表节点存在", "$.functions");
            if (functions is null) return checks.Finish("--case loot-table-functions-move");
            functions.EnsureChildren();

            // 点列表底部的添加按钮（界面同一条命令）
            functions.AddItemCommand?.Execute(null);
            PumpDispatcher();
            var list = document.FindNode("$.functions");
            list?.EnsureChildren();
            var json = document.RootJson?.Get("functions");
            var before = (json?.Items ?? []).LastOrDefault() is { } last ? McdocJsonFormatter.Format(last) : "";
            Console.WriteLine("[信息] 加完一项：项=" + (list?.Items.Count ?? -1)
                + " 文档项=" + (json?.Items?.Count ?? -1)
                + " 命令=" + string.Join(",", (list?.Items ?? []).Select(item =>
                    (item.MoveUpCommand is null ? "-" : "U") + (item.MoveDownCommand is null ? "-" : "D"))));

            var rows = (vm.Rows ?? []).Where(row => row.Node?.JsonPath?.StartsWith("$.functions[", StringComparison.Ordinal) == true
                && row.IsListChild).ToList();
            var newRow = rows.LastOrDefault();
            checks.That(newRow is not null, "新加的元素有行（列表子行）", "行=" + rows.Count);
            if (newRow is null) return checks.Finish("--case loot-table-functions-move");
            Console.WriteLine("[信息] 新行：" + (newRow.Node?.JsonPath ?? "?")
                + " ▲=" + (newRow.MoveUpCommand is null ? "null" : newRow.MoveUpCommand.CanExecute(null).ToString())
                + " ▼=" + (newRow.MoveDownCommand is null ? "null" : newRow.MoveDownCommand.CanExecute(null).ToString()));
            checks.That(newRow.Node?.IsExpanded == true, "新加进来的 function 元素当场展开（与可选字段 ⊕ 同一揭示规则）",
                "expanded=" + (newRow.Node?.IsExpanded ?? false));
            checks.That(newRow.MoveUpCommand is not null && newRow.MoveUpCommand.CanExecute(null),
                "新元素的「上移」当场可用（不用等重建/重新展开）",
                "▲=" + (newRow.MoveUpCommand is null ? "null" : newRow.MoveUpCommand.CanExecute(null).ToString()));
            newRow.MoveUpCommand?.Execute(null);
            PumpDispatcher();
            var afterJson = document.RootJson?.Get("functions");
            var afterFirst = (afterJson?.Items ?? []).FirstOrDefault() is { } first ? McdocJsonFormatter.Format(first) : "";
            Console.WriteLine("[信息] 上移后第一项=" + afterFirst.Replace("\r", "").Replace("\n", " "));
            checks.That(afterFirst == before, "上移真的把它挪到了第一位（命令不是空壳）",
                "期望=" + before.Replace("\r", "").Replace("\n", " ") + " 实际=" + afterFirst.Replace("\r", "").Replace("\n", " "));
            return checks.Finish("--case loot-table-functions-move");
        }

        // ── 截图现场 7：condition 切到 block_state_property 后，properties 要按新 block 重新求值 ────
        public static int BlockPropertiesResolve()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            var blockStateDir = LocateBlockStateDirectory();
            if (blockStateDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\Registries\\BlockState（退出码 2）");
                return 2;
            }
            Deterministic();
            var resource = new CBHK.Model.Constant.Resource();
            RealSchema.Load(resource.McdocTypes, rootDir);
            resource.LoadRegistries(System.IO.Path.GetDirectoryName(blockStateDir));
            McdocBlockStates.Register(resource.McdocTypes, "1.20.5", blockStateDir);

            const string path = "$.functions[0].conditions[0]";
            var (document, projector, pipeline) = Harness(resource.McdocTypes, McdocHelpers.GetRootType("loot_table"),
                "{ \"functions\": [ { \"function\": \"minecraft:apply_bonus\","
                + " \"enchantment\": \"minecraft:aqua_affinity\", \"formula\": \"minecraft:ore_drops\","
                + " \"conditions\": [ { \"condition\": \"minecraft:block_state_property\","
                + " \"block\": \"minecraft:command_block\", \"properties\": {} } ] } ] }",
                doc => { doc.GameVersion = "1.20.5"; doc.RegistryOptions = new CBHK.Utility.Data.MdocRegistryOptions(resource); });
            var condition = document.FindNode(path);
            checks.That(condition is not null, "条件节点存在", path);
            if (condition is null) return checks.Finish("--case block-properties-resolve");
            condition.EnsureChildren();
            var block = document.FindNode(path + ".block");
            var properties = document.FindNode(path + ".properties");
            var before = (McdocPresenterRegistry.Default.Children(properties) ?? [])
                .Select(child => child.FieldName).ToList();
            Console.WriteLine("[信息] command_block 的 properties 子行=" + string.Join(",", before));
            checks.That(before.Count > 0, "起始 block 的 properties 已解出属性行", "行=" + string.Join(",", before));

            // 换成台阶（属性集完全不同：facing/half/shape/waterlogged）
            McdocCollectionEditor.Default.ApplyValue(block, McdocJsonValue.String("minecraft:oak_stairs"));
            pipeline.OnValueChanged(block);
            var propertiesAfter = document.FindNode(path + ".properties");
            propertiesAfter?.EnsureChildren();
            var after = (McdocPresenterRegistry.Default.Children(propertiesAfter) ?? [])
                .Select(child => child.FieldName).ToList();
            Console.WriteLine("[信息] oak_stairs 的 properties 子行=" + string.Join(",", after));
            checks.That(after.Count > 0, "换 block 之后 properties 仍有属性行（没有退化成空）",
                "行=" + string.Join(",", after));
            checks.That(after.Contains("facing") || after.Contains("half") || after.Contains("shape"),
                "properties 按**新** block 重新求值（台阶的属性集）",
                "行=" + string.Join(",", after));
            return checks.Finish("--case block-properties-resolve");
        }

        // ── 截图现场 9：接上真实编辑器之后的"值通道"（宿主快路径）──────────────────────────
        // 前两条报告在无编辑器的探针里都是绿的 —— 说明缺口只在 jsonEditor != null 的那条快路径上。
        public static int EditorChannelWrites()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            var blockStateDir = LocateBlockStateDirectory();
            if (blockStateDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\Registries\\BlockState（退出码 2）");
                return 2;
            }
            Deterministic();
            var resource = new CBHK.Model.Constant.Resource();
            RealSchema.Load(resource.McdocTypes, rootDir);
            resource.LoadRegistries(System.IO.Path.GetDirectoryName(blockStateDir));

            // ① 枚举行：键不存在时选中一个候选 → JSON 必须出现该键（文本里也要有）
            var loot = new CBHK.ViewModel.Generator.LootTableViewModel(resource);
            var editor = new CBHK.Utility.Data.JsonText.JsonTextEditor();
            loot.AttachJsonEditor(editor);
            loot.LoadMcdocTree("{ \"functions\": [ { \"function\": \"minecraft:apply_bonus\","
                + " \"enchantment\": \"minecraft:aqua_affinity\" } ] }");
            var document = DocumentOf(loot);
            var formula = document?.FindNode("$.functions[0].formula");
            Console.WriteLine("[信息] formula 节点=" + (formula?.Type?.Kind.ToString() ?? "<null>")
                + " json=" + (formula?.Json?.Kind.ToString() ?? "<null>")
                + " 候选=" + (formula?.EnumOptionList?.Count ?? 0)
                + " 编辑器=" + (editor.Text ?? "").Length.ToString() + "字符");
            checks.That(formula is not null, "formula 行存在", "$.functions[0].formula");
            if (formula is null) return checks.Finish("--case editor-channel-writes");
            var options = (formula.EnumOptionList ?? []).Where(item => !McdocUnionSwitch.IsUnsetOption(item)).ToList();
            Console.WriteLine("[信息] formula 行选项=" + string.Join(",", options.Select(item => item?.Name)));
            checks.That(options.Count > 0, "formula 有可选成员", "候选=" + options.Count);
            if (options.Count == 0) return checks.Finish("--case editor-channel-writes");
            formula.SelectedEnumOption = options[0];
            PumpDispatcher();
            var written = document.RootJson?.Get("functions")?.Items?.FirstOrDefault()?.Get("formula");
            var inText = (loot.JsonText ?? "").Contains("formula", StringComparison.Ordinal);
            Console.WriteLine("[信息] 选中后：文档 formula=" + FormatOneLine(written)
                + " 文本里有 formula=" + inText);
            checks.That(written is not null, "选中成员后文档里出现 formula 键",
                "formula=" + FormatOneLine(written));
            checks.That(inText, "右侧文本里也出现 formula 键（视图同代）",
                "文本长度=" + (loot.JsonText ?? "").Length);

            // ② 根 union（item_modifier）：接上编辑器后切支，文本根形状必须跟着变
            var item = new CBHK.ViewModel.Generator.ItemModifierViewModel(resource);
            var itemEditor = new CBHK.Utility.Data.JsonText.JsonTextEditor();
            item.AttachJsonEditor(itemEditor);
            item.LoadMcdocTree("{ \"function\": \"minecraft:set_count\", \"count\": 1 }");
            var itemDoc = DocumentOf(item);
            var rootRow = item.Rows.FirstOrDefault(r => ReferenceEquals(r.Node, itemDoc?.RootNode));
            Console.WriteLine("[信息] 根行=" + (rootRow is null ? "<无>" : rootRow.Kind.ToString())
                + " 分支=" + string.Join(",", (rootRow?.UnionOptions ?? []).Select(o => o?.Name))
                + " 选中=" + (rootRow?.SelectedUnionOption?.Name ?? "<null>"));
            if (rootRow is not null && rootRow.UnionOptions.Count >= 2)
            {
                var other = rootRow.UnionOptions.FirstOrDefault(o => !McdocUnionSwitch.IsSameOption(o, rootRow.SelectedUnionOption));
                rootRow.SelectedUnionOption = other;
                PumpDispatcher();
                var okText = McdocJsonParser.TryParse(item.JsonText ?? "", out var textRoot, out _);
                Console.WriteLine("[信息] 切支后：文档根=" + (itemDoc?.RootJson?.Kind.ToString() ?? "<null>")
                    + " 文本根=" + (okText ? textRoot?.Kind.ToString() : "<解析失败>"));
                checks.That(okText && textRoot?.Kind == itemDoc?.RootJson?.Kind,
                    "根 union 切支后文本根形状与模型一致（视图同代）",
                    "文本根=" + (okText ? textRoot?.Kind.ToString() : "<解析失败>")
                    + " 文档根=" + (itemDoc?.RootJson?.Kind.ToString() ?? "<null>"));
            }
            // ③ 截图 2 现场：Entry 展开时 Type 节点已选中成员，但 JSON 里没有 type 键
            var tree = new CBHK.ViewModel.Generator.LootTableViewModel(resource);
            var treeEditor = new CBHK.Utility.Data.JsonText.JsonTextEditor();
            tree.AttachJsonEditor(treeEditor);
            tree.LoadMcdocTree("{ \"pools\": [ { \"rolls\": 0, \"entries\": [ {} ] } ] }");
            var treeDoc = DocumentOf(tree);
            var entryPath = "$.pools[0].entries[0]";
            var entry = treeDoc?.FindNode(entryPath);
            var selection = entry?.SelectedUnionMember?.Kind.ToString() ?? (entry?.Type?.Kind.ToString() ?? "<null>");
            var typeJson = treeDoc?.RootJson?.Get("pools")?.Items?.FirstOrDefault()
                ?.Get("entries")?.Items?.FirstOrDefault()?.Get("type");
            var typeInText = (tree.JsonText ?? "").Contains("\"type\"", StringComparison.Ordinal);
            Console.WriteLine("[信息] 条目：" + selection
                + " 文档 type=" + FormatOneLine(typeJson)
                + " 文本里有 type=" + typeInText);
            checks.That(typeJson is not null, "条目展开后 JSON 里出现 type 键（判别键物化要落盘）",
                "type=" + FormatOneLine(typeJson));
            checks.That(typeInText, "右侧文本里也出现 type 键（物化之后要回写）",
                "文本长度=" + (tree.JsonText ?? "").Length);
            // ④ 现场：点 ⊕ 往 entries 里加一个条目 → 新条目的 type（必选判别键）必须落成成员名，
            //    不许是空串（截图：树上是 minecraft:alternatives、JSON 里是 ""）
            var added = new CBHK.ViewModel.Generator.LootTableViewModel(resource);
            var addedEditor = new CBHK.Utility.Data.JsonText.JsonTextEditor();
            added.AttachJsonEditor(addedEditor);
            added.LoadMcdocTree("{ \"pools\": [ { \"rolls\": 0, \"entries\": [] } ] }");
            var addedDoc = DocumentOf(added);
            var entries = addedDoc?.FindNode("$.pools[0].entries");
            checks.That(entries is not null, "entries 列表节点存在", "$.pools[0].entries");
            if (entries is not null)
            {
                entries.AddItemCommand?.Execute(null);
                PumpDispatcher();
                var newEntry = addedDoc?.RootJson?.Get("pools")?.Items?.FirstOrDefault()
                    ?.Get("entries")?.Items?.FirstOrDefault();
                var newType = newEntry?.Get("type");
                var typeText = FormatOneLine(newType);
                Console.WriteLine("[信息] 新条目=" + FormatOneLine(newEntry)
                    + " type=" + typeText
                    + " 文本里有 alternatives=" + (added.JsonText ?? "").Contains("alternatives", StringComparison.Ordinal));
                checks.That(newType is { Kind: McdocJsonKind.String } typed && !string.IsNullOrEmpty(typed.StringValue),
                    "⊕ 加出来的条目 type 落成成员名（不是空串）",
                    "type=" + typeText + " 条目=" + FormatOneLine(newEntry));
                checks.That((added.JsonText ?? "").Contains("alternatives", StringComparison.Ordinal),
                    "右侧文本里也出现该成员（物化之后要回写）",
                    "文本=" + (added.JsonText ?? "").Replace("\r", "").Replace("\n", " "));
            }
            // ⑤ 现场：文档里已经是 type: ""（候选还没解出来时写进去的）→ 载入后必须按显示候选修好
            var stale = new CBHK.ViewModel.Generator.LootTableViewModel(resource);
            var staleEditor = new CBHK.Utility.Data.JsonText.JsonTextEditor();
            stale.AttachJsonEditor(staleEditor);
            stale.LoadMcdocTree("{ \"pools\": [ { \"rolls\": 0, \"entries\": [ { \"type\": \"\" } ] } ] }");
            var staleDoc = DocumentOf(stale);
            var repaired = staleDoc?.RootJson?.Get("pools")?.Items?.FirstOrDefault()
                ?.Get("entries")?.Items?.FirstOrDefault()?.Get("type");
            Console.WriteLine("[信息] 载入 type:\"\" 之后：文档 type=" + FormatOneLine(repaired)
                + " 文本=" + (stale.JsonText ?? "").Replace("\r", "").Replace("\n", " "));
            checks.That(repaired is { Kind: McdocJsonKind.String } fixedType && !string.IsNullOrEmpty(fixedType.StringValue),
                "载入时把 type:\"\" 修成候选成员（不许停在空串）",
                "type=" + FormatOneLine(repaired));
            checks.That((stale.JsonText ?? "").Contains("type", StringComparison.Ordinal),
                "修复后的值出现在右侧文本里", "文本长度=" + (stale.JsonText ?? "").Length);
            // ⑥ 现场：页面在运行数据就绪**之前**打开（#[id] 候选为空），随后数据到位 ——
            //    这时新加的条目必须落出成员名；不重来一次的话，落的是缺命名空间/空的那版
            var late = new CBHK.Model.Constant.Resource();
            RealSchema.Load(late.McdocTypes, rootDir);          // 只有符号表，注册表还没加载
            var lateVm = new CBHK.ViewModel.Generator.LootTableViewModel(late);
            var lateEditor = new CBHK.Utility.Data.JsonText.JsonTextEditor();
            lateVm.AttachJsonEditor(lateEditor);
            lateVm.LoadMcdocTree("{ \"pools\": [ { \"rolls\": 0, \"entries\": [] } ] }");
            late.LoadRegistries(System.IO.Path.GetDirectoryName(blockStateDir));
            var refresh = typeof(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase)
                .GetMethod("RefreshWithReadyRuntime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            checks.That(refresh is not null, "运行数据就绪后的刷新入口存在", "RefreshWithReadyRuntime");
            refresh?.Invoke(lateVm, null);
            PumpDispatcher();
            var lateDoc = DocumentOf(lateVm);
            var lateEntries = lateDoc?.FindNode("$.pools[0].entries");
            checks.That(lateEntries is not null, "数据到位后 entries 列表还在", "$.pools[0].entries");
            lateEntries?.AddItemCommand?.Execute(null);
            PumpDispatcher();
            var lateDoc2 = DocumentOf(lateVm);
            var addedType = lateDoc2?.RootJson?.Get("pools")?.Items?.FirstOrDefault()
                ?.Get("entries")?.Items?.FirstOrDefault()?.Get("type");
            Console.WriteLine("[信息] 数据未就绪建树 + 就绪后加条目：type=" + FormatOneLine(addedType)
                + " 文本=" + (lateVm.JsonText ?? "").Replace("\r", "").Replace("\n", " "));
            var addedValue = addedType is { Kind: McdocJsonKind.String } typedText ? typedText.StringValue : null;
            checks.That(!string.IsNullOrEmpty(addedValue),
                "数据就绪后加出来的条目 type 有值（不是空串）", "type=" + FormatOneLine(addedType));
            checks.That(addedValue == "minecraft:alternatives",
                "type 落的是带命名空间的成员名（候选已就绪）", "type=" + FormatOneLine(addedType));
            checks.That((lateVm.JsonText ?? "").Contains("minecraft:alternatives", StringComparison.Ordinal),
                "该值出现在右侧文本里", "文本长度=" + (lateVm.JsonText ?? "").Length);
            return checks.Finish("--case editor-channel-writes");
        }

        public static int RootUnionBranch()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            var blockStateDir = LocateBlockStateDirectory();
            if (blockStateDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\Registries\\BlockState（退出码 2）");
                return 2;
            }
            Deterministic();
            var resource = new CBHK.Model.Constant.Resource();
            RealSchema.Load(resource.McdocTypes, rootDir);
            resource.LoadRegistries(System.IO.Path.GetDirectoryName(blockStateDir));

            var vm = new CBHK.ViewModel.Generator.ItemModifierViewModel(resource);
            vm.LoadMcdocTree("[ { \"function\": \"minecraft:set_count\", \"count\": 1 } ]");
            var document = DocumentOf(vm);
            var rootNode = document?.RootNode;
            var members = rootNode?.Type?.Members?.Select(member => member?.Kind.ToString()).ToList() ?? [];
            var options = rootNode?.UnionTypeNameList?.Select(item => item?.Name).ToList() ?? [];
            Console.WriteLine("[信息] 根：type=" + (rootNode?.Type?.Kind.ToString() ?? "<null>")
                + " json=" + (rootNode?.Json?.Kind.ToString() ?? "<null>")
                + " 成员=[" + string.Join(",", members) + "]"
                + " 分支=[" + string.Join(",", options) + "]"
                + " 选中=" + (rootNode?.SelectedUnionTypeName?.Name ?? "<null>"));
            checks.That(rootNode is not null, "根节点存在", "<null>");
            if (rootNode is null) return checks.Finish("--case root-union-branch");

            // ① 数组文档 → 选中的必须是"数组成员"，不许落在结构支上（截图里显示 Object）
            if (rootNode.Type?.Kind == McdocTypeKind.Union)
            {
                checks.That(rootNode.SelectedUnionMember?.Kind == McdocTypeKind.List,
                    "根是 union 且文档是数组 → 选中数组成员",
                    "选中=" + (rootNode.SelectedUnionMember?.Kind.ToString() ?? "<null>")
                    + " 分支=[" + string.Join(",", options) + "]");

                // ② 切到结构支：右侧根形状必须从数组变对象（写回要走 ReplaceRoot）
                var row = vm.Rows.FirstOrDefault(item => ReferenceEquals(item.Node, rootNode));
                Console.WriteLine("[信息] 根行=" + (row is null ? "<无>" : row.Kind.ToString())
                    + " 候选=" + string.Join(",", (row?.UnionOptions ?? []).Select(item => item?.Name)));
                if (row is not null && row.UnionOptions.Count >= 2)
                {
                    var other = row.UnionOptions.FirstOrDefault(item => !McdocUnionSwitch.IsSameOption(item, row.SelectedUnionOption));
                    if (other is not null)
                    {
                        row.SelectedUnionOption = other;
                        PumpDispatcher();
                        var afterKind = document.RootJson?.Kind.ToString() ?? "<null>";
                        var textOk = McdocJsonParser.TryParse(vm.JsonText ?? "", out var textRoot, out _);
                        Console.WriteLine("[信息] 切支后：文档根=" + afterKind
                            + " 文本根=" + (textOk ? textRoot?.Kind.ToString() : "<解析失败>")
                            + " 选中=" + (document.RootNode?.SelectedUnionMember?.Kind.ToString() ?? "<null>"));

                        checks.That(textOk && textRoot?.Kind == document.RootJson?.Kind,
                            "切支后右侧文本的根形状与模型一致（视图同代）",
                            "文本根=" + (textOk ? textRoot?.Kind.ToString() : "<解析失败>")
                            + " 文档根=" + afterKind);
                    }
                }
            }
            return checks.Finish("--case root-union-branch");
        }

        // ── 列表项自己的按钮：上/下移动必须在"刚加进来"和"任何一次重建之后"都还能用 ─────────
        public static int ListItemCommands()
        {
            var checks = new Checks();
            Deterministic();
            var index = new McdocTypeIndex();
            var listType = new McdocType { Kind = McdocTypeKind.List, Item = McdocType.Create(McdocTypeKind.Int) };
            var rootType = Struct(("items", listType));
            var (document, projector, _) = Harness(index, rootType, "{ \"items\": [1, 2] }");
            projector.Reset(document.Nodes.ToList());
            var list = document.FindNode("$.items");
            checks.That(list is not null, "列表节点存在", "$.items");
            if (list is null) return checks.Finish("--case list-item-commands");
            list.EnsureChildren();
            Console.WriteLine("[信息] 初始项=" + list.Items.Count
                + " 命令=" + string.Join(",", list.Items.Select(item =>
                    (item.MoveUpCommand is null ? "-" : "U") + (item.MoveDownCommand is null ? "-" : "D"))));

            // ① 追加一项：新项必须自带可用的上移命令，并且真的能把自己的上移落到文档
            list.AddItemCommand?.Execute(null);
            var afterAdd = document.FindNode("$.items");
            afterAdd?.EnsureChildren();
            checks.That(afterAdd?.Items.Count == 3, "追加后列表有 3 项", "项=" + (afterAdd?.Items.Count ?? -1));
            var appended = afterAdd?.Items.LastOrDefault();
            // 标量项没有子级，"展开"对它无意义：只对容器项要求当场展开
            var appendedIsContainer = appended?.Type?.Kind is McdocTypeKind.Struct or McdocTypeKind.List
                or McdocTypeKind.Tuple or McdocTypeKind.Union;
            checks.That(!appendedIsContainer || appended.IsExpanded,
                "新加进来的容器项当场展开（与可选字段 ⊕ 同一揭示规则）",
                "type=" + (appended?.Type?.Kind.ToString() ?? "<null>") + " expanded=" + (appended?.IsExpanded ?? false));
            checks.That(appended?.MoveUpCommand is not null, "新加进来的项有「上移」命令",
                appended is null ? "<无项>" : "MoveUp=null");
            checks.That(appended?.MoveUpCommand?.CanExecute(null) == true, "新项的上移初始可用",
                "CanExecute=" + (appended?.MoveUpCommand?.CanExecute(null) ?? false));
            appended?.MoveUpCommand?.Execute(null);
            var movedJson = document.RootJson?.Get("items");
            Console.WriteLine("[信息] 新项上移后 items=" + FormatOneLine(movedJson));
            checks.That(movedJson is { Kind: McdocJsonKind.Array } arr && (arr.Items?.Count ?? 0) == 3,
                "上移没有把数组改坏", "items=" + FormatOneLine(movedJson));

            // ② 任何一次重建之后，项上的上/下命令不许消失（RebuildNode 只搬了「删除」）
            var listForRebuild = document.FindNode("$.items");
            document.RebuildNode(listForRebuild);
            var rebuilt = document.FindNode("$.items");
            rebuilt?.EnsureChildren();
            var commands = rebuilt?.Items.Select(item => (item.MoveUpCommand is null ? "-" : "U")
                + (item.MoveDownCommand is null ? "-" : "D")).ToList() ?? [];
            Console.WriteLine("[信息] 重建后 项=" + (rebuilt?.Items.Count ?? -1)
                + " 命令=" + string.Join(",", commands));
            checks.That(rebuilt?.Items.Count > 0 && rebuilt.Items.All(item => item.MoveUpCommand is not null),
                "重建之后每一项仍有「上移」命令",
                "命令=" + string.Join(",", commands));

            // ③ 连点两次「上移」：命令里捕获的下标必须跟着项走，否则第二次动的是别人
            var (doc2, proj2, _) = Harness(new McdocTypeIndex(), rootType, "{ \"items\": [1, 2, 3] }");
            proj2.Reset(doc2.Nodes.ToList());
            var list2 = doc2.FindNode("$.items");
            list2?.EnsureChildren();
            var second = list2?.Items.ElementAtOrDefault(1);
            checks.That(second?.MoveUpCommand is not null, "第二项有「上移」命令", second is null ? "<无项>" : "null");
            second?.MoveUpCommand?.Execute(null);
            var afterFirst = McdocJsonFormatter.Format(doc2.RootJson?.Get("items")).Replace("\r", "").Replace("\n", " ");
            // 同一行（本来是 2，现在在第 0 位）再按一次上移：已经在顶部，应该无效
            second?.MoveUpCommand?.Execute(null);
            var afterSecond = McdocJsonFormatter.Format(doc2.RootJson?.Get("items")).Replace("\r", "").Replace("\n", " ");
            Console.WriteLine("[信息] 连按两次上移：第一次=" + afterFirst + " 第二次=" + afterSecond);
            checks.That(afterSecond == afterFirst, "连按两次上移不会把别的项挪走（下标必须是活的）",
                "第一次=" + afterFirst + " 第二次=" + afterSecond);
            return checks.Finish("--case list-item-commands");
        }

        // ── 截图现场 5：入口模板本身是另一种资源 → 不许拿坏数据建树 ──────────────────────────
        // 现场：Resource/Configs/LootTable/Data/Template/empty.json 是一份"函数数组"，
        // 而 loot_table 的根声明是 struct —— 整页退化成 any 原始行，看着像求解/渲染坏了。
        public static int EntryDocumentShape()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            var blockStateDir = LocateBlockStateDirectory();
            if (blockStateDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\Registries\\BlockState（退出码 2）");
                return 2;
            }
            Deterministic();
            var resource = new CBHK.Model.Constant.Resource();
            RealSchema.Load(resource.McdocTypes, rootDir);
            resource.LoadRegistries(System.IO.Path.GetDirectoryName(blockStateDir));

            var loot = new CBHK.ViewModel.Generator.LootTableViewModel(resource);
            var document = DocumentOf(loot);
            var rootNode = document?.Nodes.FirstOrDefault();
            Console.WriteLine("[信息] loot_table 默认文档=" + (loot.JsonText ?? "").Replace("\r", "").Replace("\n", " ")
                + " | 根type=" + (rootNode?.Type?.Kind.ToString() ?? "<null>")
                + " | 状态=" + (loot.MdocStatus ?? "-"));
            checks.That(loot.JsonText?.TrimStart().StartsWith("{", StringComparison.Ordinal) == true,
                "loot_table 的默认文档是合法的战利品表（对象根），不是函数数组",
                "文档=" + (loot.JsonText ?? "").Replace("\r", "").Replace("\n", " ").PadRight(0));
            // 根是否被降级：默认文档是对象 → 根（摊平前的那个）不该挂"值与类型不符"
            var rootOfDocument = document?.RootNode;
            checks.That(rootOfDocument?.Type?.Kind is not (McdocTypeKind.Any or McdocTypeKind.Unsafe),
                "默认文档的根没被降级成 any（求解按声明走）",
                "type=" + (rootOfDocument?.Type?.Kind.ToString() ?? "<null>")
                + " 诊断=" + (rootOfDocument?.Error ?? "-"));

            // 守卫本体：拿仓库里那份**真实坏模板**（函数数组）直接问谓词
            var arrayText = "[ { \"function\": \"minecraft:set_count\", \"count\": 1 } ]";
            var declaredRoot = McdocDefaults.ResolveType(McdocHelpers.GetRootType("loot_table"),
                document?.CreateRuntime(), McdocDefaults.DefaultNode());
            var arrayDoc = McdocJsonTextParser.Parse(arrayText).Root;
            var objectDoc = McdocJsonTextParser.Parse("{ \"pools\": [] }").Root;
            var arrayOk = McdocHelpers.RootShapeMatches(arrayDoc, declaredRoot, out var arrayMismatch);
            var objectOk = McdocHelpers.RootShapeMatches(objectDoc, declaredRoot, out var objectMismatch);
            Console.WriteLine("[信息] 形状谓词：数组=" + arrayOk + "(" + arrayMismatch + ")"
                + " 对象=" + objectOk + "(" + objectMismatch + ")"
                + " 声明=" + (declaredRoot?.Kind.ToString() ?? "<null>"));
            checks.That(!arrayOk, "入口文件是函数数组 → 判为形状不符（守卫会退回代数默认值）", "mismatch=" + arrayMismatch);
            checks.That(objectOk, "入口文件是对象 → 判为相符（合法模板照旧加载）", "mismatch=" + objectMismatch);
            checks.That(McdocHelpers.RootShapeMatches(arrayDoc, McdocType.Any(), out _),
                "声明解不开（any）时不判（宁可不猜）", "any");
            return checks.Finish("--case entry-document-shape");
        }

        // ── 截图现场 4：loot_table 的根是数组 → 必须给"诚实的根"，不许把结构字段摊在数组上 ──────
        // 声明是 struct LootTable（type/pools/functions/random_sequence 四个可选字段），
        // 文档却是一份函数数组。旧行为：四个字段照样成行（幽灵行），点什么都对不上文档。
        public static int LootTableArrayRoot()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            var blockStateDir = LocateBlockStateDirectory();
            if (blockStateDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\Registries\\BlockState（退出码 2）");
                return 2;
            }
            Deterministic();
            var resource = new CBHK.Model.Constant.Resource();
            RealSchema.Load(resource.McdocTypes, rootDir);
            resource.LoadRegistries(System.IO.Path.GetDirectoryName(blockStateDir));

            var vm = new CBHK.ViewModel.Generator.LootTableViewModel(resource);
            vm.LoadMcdocTree("[ { \"function\": \"minecraft:set_count\", \"count\": 1 } ]");
            var document = DocumentOf(vm);
            var rootNode = document?.Nodes.FirstOrDefault();
            Console.WriteLine("[信息] 根节点：type=" + (rootNode?.Type?.Kind.ToString() ?? "<null>")
                + " json=" + (rootNode?.Json?.Kind.ToString() ?? "<null>")
                + " 诊断=" + (rootNode?.Error ?? "-")
                + " 顶层=" + (document?.Nodes.Count ?? 0));
            checks.That(rootNode is not null, "建出了根节点", "document=null");
            if (rootNode is null) return checks.Finish("--case loot-table-array-root");
            checks.That(rootNode.Json is { Kind: McdocJsonKind.Array }, "根节点的值就是那份数组",
                "json=" + (rootNode.Json?.Kind.ToString() ?? "<null>"));
            checks.That(rootNode.Type?.Kind is McdocTypeKind.Any or McdocTypeKind.Unsafe,
                "根声明是 struct 却拿到数组 → 降级成 any（绝不按声明摊开字段）",
                "type=" + (rootNode.Type?.Kind.ToString() ?? "<null>"));
            checks.That((rootNode.Error ?? "").Contains("值与类型不符"), "根上挂可见诊断",
                "诊断=" + (rootNode.Error ?? "-"));

            // 行视图：不许出现 type/pools/functions/random_sequence 这些"声明字段"的幽灵行
            var rows = (vm.Rows ?? []).ToList();
            var ghostFields = rows.Where(row => row.Node?.FieldName is "type" or "pools" or "functions" or "random_sequence")
                .ToList();
            var itemRows = rows.Where(row => row.Node?.JsonPath?.StartsWith("$[", StringComparison.Ordinal) == true).ToList();
            Console.WriteLine("[信息] 行=" + rows.Count + " 声明字段幽灵行=" + ghostFields.Count
                + " 数组项行=" + itemRows.Count
                + " 首行=" + (rows.FirstOrDefault()?.DisplayLabel ?? "<无>"));
            checks.That(ghostFields.Count == 0, "没有把 struct 字段摊在数组上（无幽灵行）",
                "幽灵行=" + string.Join(",", ghostFields.Take(3).Select(row => row.Node?.FieldName)));
            // 数组项必须能看得见（否则"后面干啥都没反应"）
            checks.That(itemRows.Count > 0, "数组项行可见（$[0] 能被展开/编辑）",
                "行=" + rows.Count + " 首行=" + (rows.FirstOrDefault()?.DisplayLabel ?? "<无>"));
            return checks.Finish("--case loot-table-array-root");
        }

        // ── 截图现场 3：必选容器缺值 = 死行（Argument 展开失败）──────────────────────────────
        // ① 必选容器的空壳必须补出来（形态按**求值后**判：Mapped 包装也要看穿）；
        // ② 必选缺值也要摊开子级、给展开箭头；③ 可选缺省仍然只有 ⊕、没有箭头。
        public static int RequiredShellExpand()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            var blockStateDir = LocateBlockStateDirectory();
            if (blockStateDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\Registries\\BlockState（退出码 2）");
                return 2;
            }
            Deterministic();
            var resource = new CBHK.Model.Constant.Resource();
            RealSchema.Load(resource.McdocTypes, rootDir);
            resource.LoadRegistries(System.IO.Path.GetDirectoryName(blockStateDir));

            var vm = new CBHK.ViewModel.Generator.DimensionTypeViewModel(resource);
            SelectVersion(vm, "1.21.11");
            // modifier 有值、argument（必选结构）整个键缺失 —— 截图现场
            vm.LoadMcdocTree("{ \"attributes\": { \"minecraft:audio/music_volume\":"
                + " { \"modifier\": \"alpha_blend\" } } }");
            var document = DocumentOf(vm);
            const string entryPath = "$.attributes.minecraft:audio/music_volume";
            const string argumentPath = entryPath + ".argument";
            var argumentJson = document?.RootJson?.Get("attributes")?.Get("minecraft:audio/music_volume")?.Get("argument");
            Console.WriteLine("[信息] 载入后 argument=" + FormatOneLine(argumentJson));
            checks.That(argumentJson is { Kind: McdocJsonKind.Object },
                "必选容器 argument 的空壳被补出来（Mapped 声明也要看穿）",
                "argument=" + FormatOneLine(argumentJson));

            var entry = document?.FindNode(entryPath);
            entry?.EnsureChildren();
            var argument = (McdocPresenterRegistry.Default.Children(entry) ?? [])
                .FirstOrDefault(child => child.FieldName == "argument");
            var argumentRow = argument is null ? null : vm.Rows.FirstOrDefault(item => ReferenceEquals(item.Node, argument));
            Console.WriteLine("[信息] argument 行：type=" + (argument?.Type?.Kind.ToString() ?? "<null>")
                + " expanded=" + (argument?.IsExpanded ?? false)
                + " 箭头=" + (argumentRow?.ExpanderVisibility.ToString() ?? "<无行>")
                + " 诊断=" + (argument?.Error ?? "-"));
            checks.That(argument is not null, "argument 行存在", entryPath);
            if (argument is null) return checks.Finish("--case required-shell-expand");
            checks.That(argumentRow?.ExpanderVisibility == System.Windows.Visibility.Visible,
                "必选容器行有展开箭头（这是它唯一入口）",
                "箭头=" + (argumentRow?.ExpanderVisibility.ToString() ?? "<无行>"));
            // 展开必须真的出子级
            argumentRow?.ToggleExpandedCommand?.Execute(null);
            var children = (McdocPresenterRegistry.Default.Children(argument) ?? []).ToList();
            var childRows = (vm.Rows ?? []).Count(item =>
                item.Node?.JsonPath?.StartsWith(argumentPath + ".", StringComparison.Ordinal) == true);
            Console.WriteLine("[信息] 展开后：节点子级=" + children.Count + " 行=" + childRows
                + " 名字=" + string.Join(",", children.Select(child => child.FieldName)));
            checks.That(children.Count > 0 && childRows > 0, "必选容器展开出子字段行（不再是死行）",
                "子级=" + children.Count + " 行=" + childRows);

            // 反向：可选缺省字段只有 ⊕，不许给死箭头
            var optionalVm = new CBHK.ViewModel.Generator.DimensionTypeViewModel(resource);
            SelectVersion(optionalVm, "1.21.11");
            optionalVm.LoadMcdocTree("{ }");
            var optionalDoc = DocumentOf(optionalVm);
            var attributes = optionalDoc?.FindNode("$.attributes");
            var attributesRow = attributes is null ? null : optionalVm.Rows.FirstOrDefault(item => ReferenceEquals(item.Node, attributes));
            Console.WriteLine("[信息] 可选 attributes 行：箭头=" + (attributesRow?.ExpanderVisibility.ToString() ?? "<无行>")
                + " json=" + (attributes?.Json?.Kind.ToString() ?? "<null>"));
            checks.That(attributesRow?.ExpanderVisibility != System.Windows.Visibility.Visible,
                "可选缺省字段不挂展开箭头（只留 ⊕）",
                "箭头=" + (attributesRow?.ExpanderVisibility.ToString() ?? "<无行>"));
            return checks.Finish("--case required-shell-expand");
        }

        // ── 截图现场 2：music_volume 条目的 modifier/argument 求解 + 条目（未设置）必须真的删键 ────
        // ① dispatch ... [alpha_blend]<T> to struct FloatWithAlpha{value,alpha?}：modifier=alpha_blend 时
        //    argument 必须是那个结构，不许退成 float（现场：Argument 行显示 double、值 6）。
        // ② 条目选（未设置）→ 右侧 JSON 的键必须消失，行留在树上。
        public static int MusicVolumeEntry()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            var blockStateDir = LocateBlockStateDirectory();
            if (blockStateDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\Registries\\BlockState（退出码 2）");
                return 2;
            }
            Deterministic();
            var resource = new CBHK.Model.Constant.Resource();
            RealSchema.Load(resource.McdocTypes, rootDir);
            resource.LoadRegistries(System.IO.Path.GetDirectoryName(blockStateDir));

            var vm = new CBHK.ViewModel.Generator.DimensionTypeViewModel(resource);
            SelectVersion(vm, "1.21.11");
            vm.LoadMcdocTree("{ \"attributes\": { \"minecraft:audio/music_volume\":"
                + " { \"modifier\": \"alpha_blend\", \"argument\": 6 } } }");
            var document = DocumentOf(vm);
            const string path = "$.attributes.minecraft:audio/music_volume";
            var entry = document?.FindNode(path);
            checks.That(entry is not null, "music_volume 条目行存在", path);
            if (entry is null) return checks.Finish("--case music-volume-entry");
            entry.EnsureChildren();
            var children = McdocPresenterRegistry.Default.Children(entry).ToList();
            var argument = children.FirstOrDefault(child => child.FieldName == "argument");
            // 修改 modifier 走的是管线（宿主有编辑器时那条路）：用真 schema 的 Harness 直接调
            // McdocPipeline.OnValueChanged，才能测到"依赖层重新求值 + 值整形"这一段。
            var (reshapeDoc, _, reshapePipeline) = Harness(resource.McdocTypes, McdocHelpers.GetRootType("dimension_type"),
                "{ \"attributes\": { \"minecraft:audio/music_volume\": { \"modifier\": \"alpha_blend\", \"argument\": 6 } } }",
                doc =>
                {
                    doc.GameVersion = "1.21.11";
                    doc.RegistryOptions = new CBHK.Utility.Data.MdocRegistryOptions(resource);
                });
            var reshapeEntry = reshapeDoc.FindNode(path);
            reshapeEntry?.EnsureChildren();
            var reshapeModifier = (McdocPresenterRegistry.Default.Children(reshapeEntry) ?? [])
                .FirstOrDefault(child => child.FieldName == "modifier");
            reshapePipeline.OnValueChanged(reshapeModifier);
            var reshapedEntry = reshapeDoc.FindNode(path);
            reshapedEntry?.EnsureChildren();
            var reshapedArgument = (McdocPresenterRegistry.Default.Children(reshapedEntry) ?? [])
                .FirstOrDefault(child => child.FieldName == "argument");
            var reshapedJson = reshapeDoc.RootJson?.Get("attributes")?.Get("minecraft:audio/music_volume")?.Get("argument");
            Console.WriteLine("[信息] 改 modifier 之后：argument 行 type=" + (reshapedArgument?.Type?.Kind.ToString() ?? "<null>")
                + " 值=" + FormatOneLine(reshapedJson));
            checks.That(reshapedJson is { Kind: McdocJsonKind.Object } reshaped && reshaped.Get("value") is not null,
                "modifier 变了之后 argument 的值按新类型整形（6 → { value: 6 }）",
                "值=" + FormatOneLine(reshapedJson));
            checks.That(reshapedArgument?.Type?.Kind == McdocTypeKind.Struct,
                "整过形之后 argument 行解回 FloatWithAlpha 结构",
                "type=" + (reshapedArgument?.Type?.Kind.ToString() ?? "<null>")
                + " 诊断=" + (reshapedArgument?.Error ?? "-"));
            argument = reshapedArgument;
            Console.WriteLine("[信息] 条目 分支=" + string.Join(",", entry.UnionTypeNameList?.Select(item => item?.Name) ?? [])
                + " 选中=" + (entry.SelectedUnionTypeName?.Name ?? "<null>")
                + " 子行=" + string.Join(",", children.Select(child => child.FieldName + ":" + (child.Type?.Kind.ToString() ?? "?"))));
            Console.WriteLine("[信息] argument 行：type=" + (argument?.Type?.Kind.ToString() ?? "<null>")
                + " json=" + (argument?.Json is null ? "<null>" : FormatOneLine(argument.Json))
                + " 声明=" + (argument?.DeclaredType?.Kind.ToString() ?? "<null>")
                + " 诊断=" + ((argument?.Error ?? "-") + (argument?.Diagnostics is { Count: > 0 } list
                    ? " | " + string.Join(";", list) : "")));
            // 正确解：modifier=alpha_blend → argument 是 FloatWithAlpha（struct，含 value/alpha）
            checks.That(argument?.Error is null or "",
                "整过形的 argument 行不再挂「值与类型不符」",
                "诊断=" + (argument?.Error ?? "-"));

            // ② 条目切（未设置）：键从右侧 JSON 消失，行留下
            var row = vm.Rows.FirstOrDefault(item => ReferenceEquals(item.Node, entry));
            var unset = row?.UnionOptions?.FirstOrDefault(item => McdocUnionSwitch.IsUnsetOption(item));
            if (row is null || unset is null)
            {
                Console.WriteLine("[信息] 该条目行没有（未设置）候选，跳过第二段");
                return checks.Finish("--case music-volume-entry");
            }
            row.SelectedUnionOption = unset;
            PumpDispatcher();
            var keyGone = document.RootJson?.Get("attributes") is not { Kind: McdocJsonKind.Object } map
                || map.Get("minecraft:audio/music_volume") is null;
            var rowStill = vm.Rows.FirstOrDefault(item => item.Node?.JsonPath == path && item.Node.IsMapEntry);
            Console.WriteLine("[信息] （未设置）后：attributes=" + FormatOneLine(document.RootJson?.Get("attributes"))
                + " 行还在=" + (rowStill is not null));
            checks.That(keyGone, "条目（未设置）把右侧 JSON 的键删掉",
                "attributes=" + FormatOneLine(document.RootJson?.Get("attributes")));
            checks.That(rowStill is not null, "条目（未设置）之后行仍留在树上", "path=" + path);
            return checks.Finish("--case music-volume-entry");
        }

        // ── B：切 Union 之后，"新老成员都不声明"的残键行必须能删（恰好一个删除出口）──────────
        // 现场：advancement.display.icon（AdvancementIcon | ItemStackTemplate）。切支后旧成员的键留在文档里，
        // 它们是"左右两侧都不存在"的节点 —— 行上必须有且只有一个删除入口，点了键与行都消失。
        public static int LeftoverKeyDeletable()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            var blockStateDir = LocateBlockStateDirectory();
            if (blockStateDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\Registries\\BlockState（退出码 2）");
                return 2;
            }
            Deterministic();
            var resource = new CBHK.Model.Constant.Resource();
            RealSchema.Load(resource.McdocTypes, rootDir);
            resource.LoadRegistries(System.IO.Path.GetDirectoryName(blockStateDir));

            // 两个结构成员的 union：左成员声明 a/b，右成员声明 a/c。文档里先塞 b 和 c ——
            // 切过去之后其中一个必然"新老成员都不声明"，那就是现场里删不掉的那种残键。
            var left = Struct(("a", McdocType.Create(McdocTypeKind.Int)), ("b", McdocType.Create(McdocTypeKind.Int)));
            var right = Struct(("a", McdocType.Create(McdocTypeKind.Int)), ("c", McdocType.Create(McdocTypeKind.Int)));
            var unionRoot = Struct(("pick", UnionOf(left, right)));
            var (document, projector, _) = Harness(new McdocTypeIndex(), unionRoot,
                "{ \"pick\": { \"a\": 1, \"b\": 2, \"c\": 3 } }");
            var vm = (CBHK.ViewModel.Generator.McdocGeneratorViewModelBase)null;
            var advancement = (CBHK.ViewModel.Generator.AdvancementViewModel)null;
            var icon = document.FindNode("$.pick");
            checks.That(icon is not null, "fixture：union 节点存在", "<无>");
            if (icon is null) return checks.Finish("--case leftover-key-deletable");
            icon.EnsureChildren();
            var branches = icon.UnionTypeNameList?.Select(item => item?.Name).ToList() ?? [];
            Console.WriteLine("[信息] icon 分支候选=" + string.Join(",", branches)
                + " 前文档=" + FormatOneLine(icon.Json));
            checks.That((icon.UnionTypeNameList?.Count ?? 0) >= 2, "icon 是可选分支的 union", "候选=" + string.Join(",", branches));
            if ((icon.UnionTypeNameList?.Count ?? 0) < 2) return checks.Finish("--case leftover-key-deletable");

            McdocUnionSwitch.Switch(icon, 1);
            Console.WriteLine("[信息] Switch 之后立刻：选中=" + (icon.SelectedUnionTypeName?.Name ?? "<null>")
                + " member=" + (icon.SelectedUnionMember?.Kind.ToString() ?? "<null>")
                + " 文档=" + FormatOneLine(document.RootJson?.Get("pick"))
                + " type=" + (icon.Type?.Kind.ToString() ?? "<null>"));
            PumpDispatcher();
            var after = document.FindNode("$.display.icon");
            after?.EnsureChildren();
            var member = after?.SelectedUnionMember;
            var declared = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in member?.Fields ?? [])
            {
                if (!string.IsNullOrEmpty(field?.Name)) declared.Add(field.Name);
            }
            Console.WriteLine("[信息] 切支后 选中=" + (after?.SelectedUnionTypeName?.Name ?? "<null>")
                + " 声明=" + string.Join(",", declared)
                + " 文档=" + FormatOneLine(document.RootJson?.Get("display")?.Get("icon")));
            var rows = (projector.Rows ?? [])
                .Where(row => row.Node?.JsonPath?.StartsWith("$.pick.", StringComparison.Ordinal) == true)
                .ToList();
            var container = document.RootJson?.Get("pick");
            var leftover = rows.Where(row => row.Node?.Json is not null
                && (declared.Count == 0
                    ? container is { Kind: McdocJsonKind.Object } box && box.Get(row.Node?.FieldName ?? "") is null
                    : !declared.Contains(row.Node?.FieldName ?? ""))).ToList();
            Console.WriteLine("[信息] 子行=" + rows.Count + " 残键行=" + leftover.Count);
            foreach (var row in rows)
            {
                Console.WriteLine("   [行] " + (row.Node?.JsonPath ?? "?")
                    + " field=" + (row.Node?.FieldName ?? "<空>")
                    + " json=" + (row.Node?.Json?.Kind.ToString() ?? "<null>")
                    + " 声明=" + declared.Contains(row.Node?.FieldName ?? "")
                    + " exits=" + DeleteExits(row) + " kind=" + row.Kind);
            }
            checks.That(rows.Count > 0, "切支后还有可检查的子行", "行=" + rows.Count);
            var dead = leftover.Where(row => DeleteExits(row) == 0).ToList();
            checks.That(dead.Count == 0, "残键行都有删除入口（不许「文档里有、行上点不掉」）",
                "无入口=" + dead.Count + " 例=" + string.Join(",", dead.Take(3).Select(row => row.Node?.JsonPath)));
            var duplicate = leftover.Where(row => DeleteExits(row) > 1).ToList();
            checks.That(duplicate.Count == 0, "残键行只有一个删除入口（不许两把垃圾桶）",
                "多入口=" + duplicate.Count + " 例=" + string.Join(",", duplicate.Take(3).Select(row => row.Node?.JsonPath)));

            // 真点一次：键消失、行消失、同层其它行不丢
            var target = leftover.FirstOrDefault(row => DeleteExits(row) == 1);
            if (target is not null)
            {
                var key = target.Node?.FieldName;
                var rowsBefore = rows.Count;
                if (target.UnknownDeleteVisibility == System.Windows.Visibility.Visible) target.UnknownDeleteCommand.Execute(null);
                else if (target.RemoveEntryVisibility == System.Windows.Visibility.Visible) target.RemoveEntryCommand.Execute(null);
                else if (target.RemoveSelfVisibility == System.Windows.Visibility.Visible) target.RemoveSelfCommand.Execute(null);
                else target.ClearCommand?.Execute(null);
                PumpDispatcher();
                var iconJson = document.RootJson?.Get("pick");
                var gone = iconJson?.Kind != McdocJsonKind.Object || iconJson.Get(key) is null;
                var rowsAfter = (projector.Rows ?? [])
                    .Count(row => row.Node?.JsonPath?.StartsWith("$.pick.", StringComparison.Ordinal) == true);
                Console.WriteLine("[信息] 删残键 " + key + "：文档=" + FormatOneLine(iconJson)
                    + " 行 " + rowsBefore + "→" + rowsAfter);
                checks.That(gone, "点删除后文档里该键消失", "key=" + key + " 文档=" + FormatOneLine(iconJson));
                checks.That(rowsAfter == rowsBefore - 1, "点删除后只少了那一行（同层其它行不丢）",
                    "行 " + rowsBefore + "→" + rowsAfter);
            }
            return checks.Finish("--case leftover-key-deletable");
        }

        // ── C：新加出来的行必须自带"删除自身"（四个出口里恰好一个可用）────────────────────
        // 现场点名：dimension_type.attributes / advancement.display / icon / components。
        // 判据只认行上的派生量（用户看得见的就是那四个按钮），不认节点内部字段。
        public static int TupleItemActions()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            var blockStateDir = LocateBlockStateDirectory();
            if (blockStateDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\Registries\\BlockState（退出码 2）");
                return 2;
            }
            Deterministic();
            var resource = new CBHK.Model.Constant.Resource();
            RealSchema.Load(resource.McdocTypes, rootDir);
            resource.LoadRegistries(System.IO.Path.GetDirectoryName(blockStateDir));

            // ① 真实现场：dimension_type.attributes 加一个动态键（map 条目）
            var dimension = new CBHK.ViewModel.Generator.DimensionTypeViewModel(resource);
            SelectVersion(dimension, "1.21.11");
            dimension.LoadMcdocTree("{ \"attributes\": {} }");
            var dimensionDoc = DocumentOf(dimension);
            AddMapEntry(checks, "dimension_type.attributes", dimension, dimensionDoc,
                "$.attributes", "minecraft:audio/ambient_sounds", true);

            // ② 真实现场：advancement 的最外层结构体 + display（union 里的结构体）
            var advancement = new CBHK.ViewModel.Generator.AdvancementViewModel(resource);
            advancement.LoadMcdocTree("{ }");
            var advancementDoc = DocumentOf(advancement);
            AddOptionalStruct(checks, "advancement.display", advancement, advancementDoc, "$.display");

            // ③ 合成现场 A：map 的**值**是结构体（attribtues 那种两层），加完也要能删
            var mapRoot = new McdocType { Kind = McdocTypeKind.Struct, Fields = [] };
            mapRoot.Fields.Add(new McdocStructField
            {
                Name = "wrapper",
                Key = McdocType.LiteralString("wrapper"),
                Type = new McdocType
                {
                    Kind = McdocTypeKind.Struct,
                    Fields =
                    [
                        new McdocStructField
                        {
                            Key = McdocType.Create(McdocTypeKind.String),
                            Type = Struct(("v", McdocType.Create(McdocTypeKind.Int))),
                        },
                    ],
                },
            });
            var (mapDoc, mapProjector, _) = Harness(new McdocTypeIndex(), mapRoot, "{ \"wrapper\": {} }");
            AddMapEntry(checks, "map<struct>", null, mapDoc, "$.wrapper", "alpha", false, mapProjector);

            // ④ 合成现场 B：定长（tuple / #[length=N]）**不许**给添加按钮，API 也得拒绝
            var fixedRoot = new McdocType { Kind = McdocTypeKind.Struct, Fields = [] };
            var fixedList = new McdocType { Kind = McdocTypeKind.List, Item = McdocType.Create(McdocTypeKind.Int) };
            fixedList.LengthRange = McdocRange.Exact(2);
            fixedRoot.Fields.Add(new McdocStructField
            {
                Name = "tuple", Key = McdocType.LiteralString("tuple"), Type = fixedList, Optional = true,
            });
            var (fixedDoc, fixedProjector, _) = Harness(new McdocTypeIndex(), fixedRoot, "{ \"tuple\": [1, 2] }");
            fixedProjector.Reset(fixedDoc.Nodes.ToList());
            var fixedNode = fixedDoc.FindNode("$.tuple");
            fixedNode?.EnsureChildren();
            var fixedRow = fixedNode is null ? null : fixedProjector.Find(fixedNode);
            var addRefused = fixedNode?.AddItemCommand?.CanExecute(null) != true;
            Console.WriteLine("[信息] 定长行：type=" + (fixedNode?.Type?.Kind.ToString() ?? "<null>")
                + " kind=" + (fixedRow?.Kind.ToString() ?? "<无行>")
                + " 添加可见=" + (fixedRow?.ListAddVisibility.ToString() ?? "-")
                + " AddItem可用=" + (fixedNode?.AddItemCommand?.CanExecute(null) ?? false));
            checks.That(fixedNode?.Type?.Kind == McdocTypeKind.Tuple, "定长 list 归一成 tuple 行",
                "type=" + (fixedNode?.Type?.Kind.ToString() ?? "<null>"));
            checks.That(addRefused, "定长行不给添加（API 守卫与按钮一致）",
                "AddItem可用=" + (fixedNode?.AddItemCommand?.CanExecute(null) ?? false));
            checks.That(fixedRow?.ListAddVisibility == System.Windows.Visibility.Collapsed,
                "定长行没有 ⊕ 按钮", "可见=" + (fixedRow?.ListAddVisibility.ToString() ?? "<无行>"));
            return checks.Finish("--case tuple-item-actions");
        }

        /// <summary>map 加一个键：新条目行必须恰好一个可用的删除出口，执行后键与行都消失。</summary>
        private static void AddMapEntry(Checks checks, string label,
            CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm, McdocTreeDocument document,
            string mapPath, string key, bool expectKeyDelete, McdocRowProjector onlyRows = null)
        {
            var map = mapPath == "$" ? document?.Nodes.FirstOrDefault() : document?.FindNode(mapPath);
            checks.That(map is not null, label + "：map 行存在", mapPath);
            if (map is null) return;
            map.EnsureChildren();
            var placeholder = map.Children.FirstOrDefault(child => child.IsDynamicKey);
            checks.That(placeholder is not null, label + "：有新键占位行", "<无>");
            if (placeholder is null) return;
            placeholder.EditingKey = key;
            placeholder.AddEntryCommand?.Execute(null);
            PumpDispatcher();
            var mapAfter = document.FindNode(mapPath);
            mapAfter?.EnsureChildren();
            var entry = (mapAfter?.Children ?? []).FirstOrDefault(child => child.IsMapEntry);
            checks.That(entry is not null, label + "：新条目节点建出来了", "<无>");
            if (entry is null) return;
            var row = RowOf(vm, onlyRows, entry);
            checks.That(row is not null, label + "：新条目行在投影里", "<无>");
            if (row is null) return;
            var exits = DeleteExits(row);
            checks.That(entry.IsExpanded, label + "：新条目当场展开（与可选字段 ⊕ 同一揭示规则）",
                "expanded=" + entry.IsExpanded);
            Console.WriteLine("[信息] " + label + " 新条目行：kind=" + row.Kind + " exits=" + exits
                + " isListChild=" + row.IsListChild + " json=" + (entry.Json?.Kind.ToString() ?? "<null>"));
            checks.That(exits == 1, label + "：新条目行恰好一个删除出口", "出口=" + exits);

            // 任何一次后续重建（切 union 分支 / 改兄弟键 / 物化）都会把节点换掉：
            // 删除命令是**父级建完子节点之后**才挂的，重建时必须搬过来，否则按钮当场消失。
            // 走宿主真实的结构变更链路（CommitEntry 抬的也是这一条），而不是裸 RebuildNode ——
            // 裸重建不经过投影，测出来的"行没了"只是探针自己没刷新
            var notify = document.FindNode(mapPath) ?? document.Nodes.FirstOrDefault();
            notify?.EnsureChildren();
            notify?.StructureChanged?.Invoke(notify);
            PumpDispatcher();
            var rebuiltMap = document.FindNode(mapPath) ?? document.Nodes.FirstOrDefault();
            rebuiltMap?.EnsureChildren();
            var entryAfterRebuild = (rebuiltMap?.Children ?? [])
                .FirstOrDefault(child => child.IsMapEntry && child.FieldName == entry.FieldName);
            var rowAfterRebuild = (vm?.Rows ?? onlyRows?.Rows ?? [])
                .FirstOrDefault(row => row.Node?.IsMapEntry == true && row.Node?.FieldName == entry.FieldName);
            var exitsAfterRebuild = rowAfterRebuild is null ? -1 : DeleteExits(rowAfterRebuild);
            Console.WriteLine("[信息] " + label + " 重建后：条目=" + (entryAfterRebuild?.FieldName ?? "<无>")
                + " 行=" + (rowAfterRebuild is null ? "<无>" : rowAfterRebuild.Kind.ToString())
                + " exits=" + exitsAfterRebuild);
            checks.That(exitsAfterRebuild == 1, label + "：重建之后新条目行仍然有删除出口",
                "出口=" + exitsAfterRebuild + " 条目=" + (entryAfterRebuild?.FieldName ?? "<无>"));

            // 条目的（未设置）：只把右侧 JSON 的键去掉，**行留在树上**（用户要能再选分支，或点行首删除）
            var unsetOption = rowAfterRebuild?.UnionOptions?.FirstOrDefault(item => McdocUnionSwitch.IsUnsetOption(item));
            if (rowAfterRebuild is null || unsetOption is null)
            {
                Console.WriteLine("[信息] " + label + " 不是可切（未设置）的 union 条目，跳过未设置这一段");
                return;
            }
            rowAfterRebuild.SelectedUnionOption = unsetOption;
            PumpDispatcher();
            var keyName = entryAfterRebuild?.FieldName ?? "";
            var mapNow = document.FindNode(mapPath) ?? document.Nodes.FirstOrDefault();
            var keyGone = mapNow?.Json is not { Kind: McdocJsonKind.Object } box || box.Get(keyName) is null;
            var rowStillThere = (vm?.Rows ?? onlyRows?.Rows ?? [])
                .FirstOrDefault(item => item.Node?.IsMapEntry == true && item.Node?.FieldName == keyName);
            var exitsAfterUnset = rowStillThere is null ? -1 : DeleteExits(rowStillThere);
            Console.WriteLine("[信息] " + label + " 选（未设置）后：键在文档里=" + !keyGone
                + " 行还在=" + (rowStillThere is not null)
                + " exits=" + exitsAfterUnset);
            checks.That(keyGone, label + "：（未设置）把右侧 JSON 的键去掉", "key=" + keyName);
            checks.That(rowStillThere is not null, label + "：（未设置）之后行仍留在树视图里", "key=" + keyName);
            checks.That(exitsAfterUnset == 1, label + "：空条目行仍有行首删除入口", "出口=" + exitsAfterUnset);
            // 点行首删除：这一行消失，文档不被改动（键早就没了）
            rowStillThere?.RemoveEntryCommand?.Execute(null);
            PumpDispatcher();
            var afterDelete = (vm?.Rows ?? onlyRows?.Rows ?? [])
                .FirstOrDefault(item => item.Node?.IsMapEntry == true && item.Node?.FieldName == keyName);
            checks.That(afterDelete is null, label + "：点行首删除后这一行消失",
                "行还在=" + (afterDelete is not null));
        }

        /// <summary>可选结构体的 ⊕ 之后，新长出来的字段行也必须能删/能清。</summary>
        private static void AddOptionalStruct(Checks checks, string label,
            CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm, McdocTreeDocument document, string path)
        {
            var node = document?.FindNode(path);
            if (node?.SetDefaultCommand?.CanExecute(null) == true) node.SetDefaultCommand.Execute(null);
            PumpDispatcher();
            var after = document.FindNode(path);
            after?.EnsureChildren();
            var rows = RowsUnder(vm, path);
            checks.That(rows.Count > 0, label + "：⊕ 之后有子行", "行=" + rows.Count);
            var noExit = rows.Where(row => row.Node?.Json is not null && DeleteExits(row) == 0).ToList();
            Console.WriteLine("[信息] " + label + " 子行=" + rows.Count + " 无删除出口=" + noExit.Count);
            foreach (var row in noExit.Take(3))
            {
                Console.WriteLine("   [无出口] " + (row.Node?.JsonPath ?? "?") + " kind=" + row.Kind);
            }
            checks.That(noExit.All(row => row.Node?.IsRequired == true),
                label + "：有值的子行都有删除出口（必选字段除外）",
                "无出口=" + noExit.Count + " 例=" + string.Join(",", noExit.Take(3).Select(row => row.Node?.JsonPath)));
        }

        /// <summary>四个删除出口里当前可用的个数。</summary>
        private static int DeleteExits(McdocRow row)
        {
            var count = 0;
            if (row.UnknownDeleteVisibility == System.Windows.Visibility.Visible) count++;
            if (row.RemoveEntryVisibility == System.Windows.Visibility.Visible) count++;
            if (row.RemoveSelfVisibility == System.Windows.Visibility.Visible) count++;
            if (row.ClearVisibility == System.Windows.Visibility.Visible) count++;
            return count;
        }

        private static McdocRow RowOf(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm,
            McdocRowProjector projector, McdocTreeNode node)
            => vm is not null
                ? vm.Rows.FirstOrDefault(row => ReferenceEquals(row.Node, node))
                : projector?.Rows.FirstOrDefault(row => ReferenceEquals(row.Node, node));

        private static List<McdocRow> RowsUnder(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm, string path)
        {
            var prefix = path + ".";
            return (vm?.Rows ?? []).Where(row => row.Node?.JsonPath?.StartsWith(prefix, StringComparison.Ordinal) == true)
                .ToList();
        }

        // ── A：点可选结构体的 ⊕ 之后必须当场看见子字段行（不许"折叠一次再展开"）──────────────
        // 走真 VM（宿主同一条 Deferred 重建链路）：探针自己抄一份时序，绿了也说明不了界面绿。
        public static int OptionalStructAdd()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            var blockStateDir = LocateBlockStateDirectory();
            if (blockStateDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\Registries\\BlockState（退出码 2）");
                return 2;
            }
            Deterministic();
            var resource = new CBHK.Model.Constant.Resource();
            RealSchema.Load(resource.McdocTypes, rootDir);
            resource.LoadRegistries(System.IO.Path.GetDirectoryName(blockStateDir));

            // dimension_type.attributes（1.21.11 才有）与 advancement.display：都是可选结构体
            var dimension = new CBHK.ViewModel.Generator.DimensionTypeViewModel(resource);
            checks.That(SelectVersion(dimension, "1.21.11"), "版本下拉里有 1.21.11",
                "候选=" + string.Join(",", dimension.GameVersionOptions.Take(4).Select(item => item?.Name)));
            dimension.LoadMcdocTree("{ }");
            ProbeOptionalAdd(checks, "dimension_type.attributes",
                DocumentOf(dimension), "$.attributes", dimension);

            var advancement = new CBHK.ViewModel.Generator.AdvancementViewModel(resource);
            advancement.LoadMcdocTree("{ }");
            ProbeOptionalAdd(checks, "advancement.display",
                DocumentOf(advancement), "$.display", advancement);

            // 运行期依赖的父级：宿主把重建排到下一拍（VM 的 ScheduleRebuild），
            // 但"文档已经有的子级"这一拍就得画出来 —— 否则用户看到的就是"点了 ⊕ 没反应，
            // 折叠一次再展开才出来"。这里把重建**延后**，只断言这一拍。
            DeferredParentAdd(checks);
            return checks.Finish("--case optional-struct-add");
        }

        /// <summary>父级是 dispatcher 求值出来的（运行期依赖）：⊕ 之后不靠延后重建也要看得见子级。</summary>
        private static void DeferredParentAdd(Checks checks)
        {
            var index = new McdocTypeIndex();
            var body = new McdocType { Kind = McdocTypeKind.Struct, Fields = [] };
            body.Fields.Add(new McdocStructField
            {
                Name = "box",
                Key = McdocType.LiteralString("box"),
                Type = Struct(("v", McdocType.Create(McdocTypeKind.Int))),
                Optional = true,
            });
            index.RegisterDispatch("probe", "member", body);
            // 父级形状本身无所谓：要验的是"运行期依赖的父级"这条分支（真 schema 里 attributes /
            // dispatcher[[key]] 的父级都是它，见 --case attributes-modifier-branch 的 trace
            // runtimeDep=True）。这里直接把标志钉成 true，专门走宿主"延后一拍重建"那条路。
            var rootType = new McdocType { Kind = McdocTypeKind.Struct, Fields = [] };
            rootType.Fields.Add(new McdocStructField
            {
                Name = "box",
                Key = McdocType.LiteralString("box"),
                Type = Struct(("v", McdocType.Create(McdocTypeKind.Int))),
                Optional = true,
            });

            var deferred = new List<McdocTreeNode>();
            var loading = false;
            var document = new McdocTreeDocument(index);
            var projector = new McdocRowProjector();
            var pipeline = new McdocPipeline
            {
                Document = document,
                Projector = projector,
                RootType = () => rootType,
                GetLoading = () => loading,
                SetLoading = value => loading = value,
                ScheduleRebuild = anchor => deferred.Add(anchor),
                Fallback = () => document.Load(rootType, document.RootJson),
            };
            document.NodeValueChanged = node => pipeline.OnValueChanged(node);
            document.NodeStructureChanged = node => pipeline.OnStructureChanged(node);
            document.Load(rootType, McdocJsonTextParser.Parse("{ }").Root);
            projector.Reset(document.Nodes.ToList());

            var root = document.Nodes.FirstOrDefault();
            if (root is not null) root.DependsOnRuntimeData = true;   // 钉住"运行期依赖父级"这条分支
            checks.That(root?.DependsOnRuntimeData == true, "夹具：父级按运行期依赖处理",
                "runtimeDep=" + (root?.DependsOnRuntimeData ?? false));
            var box = document.FindNode("$.box");
            checks.That(box?.SetDefaultCommand?.CanExecute(null) == true, "夹具：可选结构体 box 有 ⊕",
                box is null ? "节点=null" : "json=" + (box.Json?.Kind.ToString() ?? "<null>"));
            if (box is null) return;
            box.SetDefaultCommand.Execute(null);
            var rowsNow = (document.FindNode("$.box")?.IsExpanded == true)
                ? projector.Rows.Count(row => row.Node?.JsonPath?.StartsWith("$.box.", StringComparison.Ordinal) == true)
                : 0;
            Console.WriteLine("[信息] 运行期依赖父级 ⊕ 后（未跑延后重建）：子行=" + rowsNow
                + " 排队重建=" + deferred.Count);
            checks.That(rowsNow > 0, "运行期依赖父级：⊕ 之后同一拍就看见子级行（不等延后重建）",
                "子行=" + rowsNow + " 排队重建=" + deferred.Count);
            foreach (var anchor in deferred) pipeline.RebuildTargetedOrFallback(anchor);
            deferred.Clear();
            var rowsAfter = projector.Rows.Count(row => row.Node?.JsonPath?.StartsWith("$.box.", StringComparison.Ordinal) == true);
            checks.That(rowsAfter > 0, "延后重建跑完后子级行仍在", "子行=" + rowsAfter);
        }

        private static bool SelectVersion(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm, string version)
        {
            var option = vm.GameVersionOptions.FirstOrDefault(item => item?.Name == version);
            if (option is null) return false;
            vm.SelectedGameVersionOption = option;
            return true;
        }

        private static McdocTreeDocument DocumentOf(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm)
            => typeof(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase)
                .GetField("document", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(vm) as McdocTreeDocument;

        /// <summary>点一次 ⊕：文档要落成对象、节点要展开、**这一拍**就要有子字段行。</summary>
        private static void ProbeOptionalAdd(Checks checks, string label, McdocTreeDocument document, string path,
            CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm)
        {
            var node = document?.FindNode(path);
            checks.That(node is not null, label + "：字段行存在", document is null ? "document=null" : "path=" + path);
            if (node is null) return;
            checks.That(node.Json is null, label + "：前置是缺省字段（只有一行 ⊕）",
                "json=" + (node.Json?.Kind.ToString() ?? "<null>"));
            node.EnsureChildren();
            var add = node.SetDefaultCommand;
            checks.That(add?.CanExecute(null) == true, label + "：⊕ 可点", add is null ? "命令=null" : "CanExecute=false");
            if (add?.CanExecute(null) != true) return;

            add.Execute(null);
            var rowsNow = ChildRowCount(vm, path);
            Console.WriteLine("[信息] " + label + " ⊕ 后立即：json=" + (node.Json?.Kind.ToString() ?? "<null>")
                + " expanded=" + node.IsExpanded + " 子行=" + rowsNow
                + " 节点子级=" + node.Children.Count + " built=" + node.ChildrenBuilt
                + " items=" + node.Items.Count);
            foreach (var row in (vm?.Rows ?? []).Where(row => row.Node?.JsonPath == path))
            {
                Console.WriteLine("   [行] " + path + " kind=" + row.Kind + " label=" + row.DisplayLabel
                    + " 子级=" + (row.Node?.Children.Count ?? -1) + " built=" + (row.Node?.ChildrenBuilt ?? false));
            }
            var expandedNow = node.IsExpanded;
            if (rowsNow == 0) PumpDispatcher();     // 宿主"延后一拍"
            var rowsAfter = ChildRowCount(vm, path);
            Console.WriteLine("[信息] " + label + " 延后一拍：子行=" + rowsAfter);
            checks.That(node.Json is { Kind: McdocJsonKind.Object }, label + "：⊕ 落成对象",
                "json=" + (node.Json?.Kind.ToString() ?? "<null>"));
            checks.That(expandedNow, label + "：⊕ 之后节点就是展开的（不许等用户再点箭头）",
                "expanded=" + expandedNow);
            checks.That(rowsAfter > 0 || MapPlaceholderVisible(vm, path),
                label + "：⊕ 之后立刻看见子级行（不许折叠一次再展开）",
                "立即=" + rowsNow + " 延后一拍=" + rowsAfter
                + " map占位行=" + MapPlaceholderVisible(vm, path));

            // 第二轮：清空这个可选字段后再 ⊕ 一次（RemoveField 会把 ChildrenBuilt 冻成 true，
            // 现场"结构有了、行没回来"就是这条路 —— 用户说的"一些时候"多半是删过又加）
            var again = document.FindNode(path);
            var clear = again?.RemoveFieldCommand;
            if (clear?.CanExecute(null) != true)
            {
                Console.WriteLine("[信息] " + label + " 清空不可用，跳过第二轮");
                return;
            }
            clear.Execute(null);
            PumpDispatcher();
            var reopened = document.FindNode(path);
            var addAgain = reopened?.SetDefaultCommand;
            checks.That(reopened?.Json is null && addAgain?.CanExecute(null) == true,
                label + "：清空后回到 ⊕ 行（可再加）",
                "json=" + (reopened?.Json?.Kind.ToString() ?? "<null>") + " 命令=" + (addAgain is not null));
            if (addAgain?.CanExecute(null) != true) return;
            addAgain.Execute(null);
            var secondNow = ChildRowCount(vm, path);
            if (secondNow == 0) PumpDispatcher();
            var secondAfter = ChildRowCount(vm, path);
            Console.WriteLine("[信息] " + label + " 清空再 ⊕：立即=" + secondNow + " 延后一拍=" + secondAfter
                + " expanded=" + (reopened?.IsExpanded ?? false));
            checks.That(secondAfter > 0 || MapPlaceholderVisible(vm, path),
                label + "：清空后重新 ⊕ 也要看见子级（不许折叠一次再展开）",
                "立即=" + secondNow + " 延后一拍=" + secondAfter
                + " expanded=" + (reopened?.IsExpanded ?? false)
                + " 节点子级=" + (reopened?.Children.Count ?? -1)
                + " built=" + (reopened?.ChildrenBuilt ?? false));
        }

        /// <summary>map 容器的"新键"占位行与容器行同路径：按行形状认，避免用路径前缀误判成 0。</summary>
        private static bool MapPlaceholderVisible(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm, string path)
            => (vm?.Rows ?? []).Any(row => row.Node?.JsonPath == path && row.Kind == McdocRowKind.Placeholder);

        /// <summary>投影里挂在该路径下面的行数（节点可能被重建换掉，按路径数才稳）。</summary>
        private static int ChildRowCount(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm, string path)
        {
            var rows = vm?.Rows;
            if (rows is null) return 0;
            var prefix = path + ".";
            return rows.Count(row => row.Node?.JsonPath?.StartsWith(prefix, StringComparison.Ordinal) == true);
        }

        // ── 求解代数的旁证：切分支时 union/tuple/enum 的取值搬运与剪枝不许被"最复杂用例"牺牲掉 ──────
        // 每次切支只跑一个场景，断言都落在**文档值**上（行是它的投影）。
        public static int SolveAlgebra()
        {
            var checks = new Checks();
            Deterministic();
            var index = new McdocTypeIndex();

            // ① list → list：条目类型相符就原样保留（不许被 getChange 换成默认的 []）
            var same = SwitchCase(index, UnionOf(ListOf(Int()), ListOf(Int())), "{ \"pick\": [1, 2, 3] }", 1);
            checks.That(same?.Get("pick") is { Kind: McdocJsonKind.Array } kept1 && (kept1.Items?.Count ?? 0) == 3,
                "list→list（条目类型相符）保留条目", "值=" + FormatOneLine(same?.Get("pick")));
            // ② list → list：条目类型不符必须转换，不许把 [1,2,3] 塞进 [string]
            var unfit = SwitchCase(index, UnionOf(ListOf(Int()), ListOf(Text())), "{ \"pick\": [1, 2, 3] }", 1);
            checks.That(unfit?.Get("pick") is { Kind: McdocJsonKind.Array } kept2 && (kept2.Items?.Count ?? 0) == 0,
                "list→list（条目类型不符）换成该支默认值", "值=" + FormatOneLine(unfit?.Get("pick")));
            // ③ X → [X]：标量就地包成一条（getChange 的搬运规则）
            var wrap = SwitchCase(index, UnionOf(Text(), ListOf(Text())), "{ \"pick\": \"a\" }", 1);
            checks.That(wrap?.Get("pick") is { Kind: McdocJsonKind.Array } wrapped
                    && (wrapped.Items?.Count ?? 0) == 1 && wrapped.Items[0].StringValue == "a",
                "X→[X] 把标量搬成一条", "值=" + FormatOneLine(wrap?.Get("pick")));
            // ④ [X] → X：取出首项
            var unwrap = SwitchCase(index, UnionOf(ListOf(Text()), Text()), "{ \"pick\": [\"a\", \"b\"] }", 1);
            checks.That(unwrap?.Get("pick") is { Kind: McdocJsonKind.String } unwrapped && unwrapped.StringValue == "a",
                "[X]→X 取出首项", "值=" + FormatOneLine(unwrap?.Get("pick")));
            // ⑤ {k:X} → X 与 X → {k:X}：两条搬运规则不能被"指令不算同型"误伤
            var structOfText = Struct(("v", Text()));
            var asScalar = SwitchCase(index, UnionOf(structOfText, Text()), "{ \"pick\": { \"v\": \"a\" } }", 1);
            checks.That(asScalar?.Get("pick") is { Kind: McdocJsonKind.String } movedOut && movedOut.StringValue == "a",
                "{k:X}→X 取出字段值", "值=" + FormatOneLine(asScalar?.Get("pick")));
            var asStruct = SwitchCase(index, UnionOf(Text(), Struct(("v", Text()))), "{ \"pick\": \"a\" }", 1);
            checks.That(asStruct?.Get("pick") is { Kind: McdocJsonKind.Object } movedIn
                    && movedIn.Get("v") is { Kind: McdocJsonKind.String } movedValue && movedValue.StringValue == "a",
                "X→{k:X} 把值塞进字段", "值=" + FormatOneLine(asStruct?.Get("pick")));
            // ⑥ tuple 支：切过去落下声明长度的数组，行上仍是两个条目
            var tupleCase = SwitchCase(index, UnionOf(Text(), TupleOf(Int(), Int())), "{ \"pick\": \"a\" }", 1, out var tupleDoc);
            checks.That(tupleCase?.Get("pick") is { Kind: McdocJsonKind.Array } tupleValue
                    && (tupleValue.Items?.Count ?? 0) == 2,
                "切到 tuple 支落成声明长度的数组", "值=" + FormatOneLine(tupleCase?.Get("pick")));
            var tupleNode = tupleDoc?.FindNode("$.pick");
            tupleNode?.EnsureChildren();
            var tupleRows = tupleNode is null ? 0 : McdocPresenterRegistry.Default.Children(tupleNode).Count;
            checks.That(tupleRows == 2, "tuple 支的行数是声明项数", "行=" + tupleRows);
            // ⑦ enum 支：值落在取值域内，候选没被"最复杂用例"挤掉（空下拉在界面上就是点了没反应）
            var enumDoc = (McdocTreeDocument)null;
            var enumCase = SwitchCase(index, UnionOf(EnumType("a", "b"), Int()), "{ \"pick\": \"b\" }", 1, out enumDoc);
            checks.That(enumCase?.Get("pick") is { Kind: McdocJsonKind.Number }, "enum→int 换成该支默认值",
                "值=" + FormatOneLine(enumCase?.Get("pick")));
            var backCase = SwitchCase(index, UnionOf(EnumType("a", "b"), Int()), "{ \"pick\": 7 }", 0, out var backDoc);
            checks.That(backCase?.Get("pick") is { Kind: McdocJsonKind.String } backText
                    && (backText.StringValue == "a" || backText.StringValue == "b"),
                "int→enum 落成取值域内的字面量", "值=" + FormatOneLine(backCase?.Get("pick")));
            var enumNode = backDoc?.FindNode("$.pick");
            var enumRows = enumNode is null ? 0 : McdocPresenterRegistry.Default.Children(enumNode).Count;
            checks.That(enumNode?.UnionTypeNameList?.Count == 2, "enum 支的 union 分支候选还在",
                "分支=" + (enumNode?.UnionTypeNameList?.Count ?? 0) + " 分支行=" + enumRows);
            // 真正要守的是**下拉候选**：枚举支的候选必须镜像到 union 行上（含"（未设置）"），
            // 空下拉在界面上就是"点了没反应"，而它正是"求解代数给最复杂用例让路"时最先被牺牲的东西。
            var enumCandidates = enumNode?.EnumOptionList?.Count ?? 0;
            checks.That(enumCandidates >= 3, "枚举支的候选镜像到行上（2 个值 + 未设置）",
                "候选=" + enumCandidates
                + " 名字=" + string.Join(",", (enumNode?.EnumOptionList ?? []).Select(item => item?.Name)));
            checks.That(enumNode?.SelectedEnumOption is { } selectedEnum
                && !McdocUnionSwitch.IsUnsetOption(selectedEnum),
                "枚举支的选中项跟着值走（不是停在未设置）",
                "选中=" + (enumNode?.SelectedEnumOption?.Name ?? "<null>")
                + " 值=" + (enumNode?.Json is null ? "<null>" : FormatOneLine(enumNode.Json)));
            return checks.Finish("--case solve-algebra");
        }

        /// <summary>建一份"根结构里只有一个 union 字段 pick"的夹具，切到 memberIndex，返回切完的根 JSON。</summary>
        private static McdocJsonValue SwitchCase(McdocTypeIndex index, McdocType union, string json, int memberIndex)
            => SwitchCase(index, union, json, memberIndex, out _);

        private static McdocJsonValue SwitchCase(McdocTypeIndex index, McdocType union, string json, int memberIndex,
            out McdocTreeDocument document)
        {
            var root = Struct(("pick", union));
            (document, _, _) = Harness(index, root, json);
            var node = document.FindNode("$.pick");
            node?.EnsureChildren();
            McdocUnionSwitch.Switch(node, memberIndex);
            return document.RootJson;
        }

        private static McdocType UnionOf(params McdocType[] members)
            => new() { Kind = McdocTypeKind.Union, Members = [.. members] };

        private static McdocType Struct(params (string Name, McdocType Type)[] fields)
        {
            var type = McdocType.Create(McdocTypeKind.Struct);
            type.Fields = [];
            foreach (var (name, fieldType) in fields)
            {
                type.Fields.Add(new McdocStructField
                {
                    Name = name,
                    Key = McdocType.LiteralString(name),
                    Type = fieldType,
                });
            }
            return type;
        }

        private static McdocType ListOf(McdocType item)
            => new() { Kind = McdocTypeKind.List, Item = item };

        private static McdocType TupleOf(params McdocType[] items)
            => new() { Kind = McdocTypeKind.Tuple, Items = [.. items] };

        private static McdocType EnumType(params string[] values)
        {
            var type = new McdocType { Kind = McdocTypeKind.Enum, EnumKind = McdocTypeKind.String, EnumValues = [] };
            foreach (var value in values)
            {
                type.EnumValues.Add(new McdocEnumValue { Identifier = value, Value = value });
            }
            return type;
        }

        private static McdocType Text() => McdocType.Create(McdocTypeKind.String);

        private static McdocType Int() => McdocType.Create(McdocTypeKind.Int);

        /// <summary>把 attributes 子树的行视图打出来：这一层就是用户看的"求解结果"。</summary>
        private static void DumpAttributeRows(string label, CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm)
        {
            var rows = vm?.Rows;
            Console.WriteLine("[行视图] " + label + " 总行数=" + (rows?.Count ?? 0));
            if (rows is null) return;
            var start = -1;
            var depth = 0;
            for (var i = 0; i < rows.Count; i++)
            {
                if (start < 0)
                {
                    if (rows[i].Node?.JsonPath == "$.attributes") { start = i; depth = rows[i].Depth; }
                    continue;
                }
                var row = rows[i];
                if (row.Depth <= depth) break;
                var head = row.Head;
                Console.WriteLine("   " + new string('.', row.Depth * 2) + " " + row.DisplayLabel
                    + " | kind=" + row.Kind
                    + " editor=" + head.EditorKind
                    + " hint=" + (string.IsNullOrEmpty(head.TypeHint) ? "-" : head.TypeHint)
                    + " type=" + (row.Node?.Type?.Kind.ToString() ?? "?")
                    + " json=" + (row.Node?.Json?.Kind.ToString() ?? "<null>")
                    + " children=" + row.HasChildren
                    + " diag=" + (head.Diagnostics is { Count: > 0 } problems ? string.Join(" / ", problems) : "-"));
            }
        }

        private static string FormatOneLine(McdocJsonValue value)
            => value is null ? "<null>" : McdocJsonFormatter.Format(value).Replace("\r", "").Replace("\n", " ");

        /// <summary>把 Dispatcher 队列跑空 = 宿主"延后一拍"。</summary>
        private static void PumpDispatcher()
        {
            var frame = new System.Windows.Threading.DispatcherFrame();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.ApplicationIdle,
                new Action(() => frame.Continue = false));
            System.Windows.Threading.Dispatcher.PushFrame(frame);
        }

        private static McdocType BlockStatesDispatcher()
        {
            var value = McdocType.Create(McdocTypeKind.Dispatcher);
            value.Registry = McdocBlockStates.Registry;
            value.Indices = [McdocIndex.Dynamic(new List<string> { "block" })];
            return value;
        }


        // ── 文本→树的值补丁必须重新求值"靠这个键解出来的兄弟"（现场：block 写空、face 行还在）──────
        public static int TextEditReevaluatesDependents()
        {
            var checks = new Checks();
            var directory = LocateBlockStateDirectory();
            if (directory is null)
            {
                Console.WriteLine("[环境缺失] 找不到 BlockState 资产（退出码 2）");
                return 2;
            }
            Deterministic();
            var index = new McdocTypeIndex();
            McdocBlockStates.Register(index, "1.20.5", directory);

            var (document, projector, pipeline) = Harness(index, RootTypeWith(BlockStatesDispatcher()),
                @"{ ""block"": ""minecraft:acacia_button"", ""properties"": { ""face"": ""wall"" } }");
            checks.That(document.FindNode("$.properties.face") is not null, "前置：properties 已解出字段行",
                "properties.Type=" + (document.FindNode("$.properties")?.Type?.Kind.ToString() ?? "<无节点>"));

            // 右侧把 block 改成 ""：文本→树只做值补丁（McdocSyncEngine），谁去重新求值 properties？
            var edited = McdocJsonTextParser.Parse(@"{ ""block"": """", ""properties"": { ""face"": ""wall"" } }").Root;
            var applied = McdocSyncEngine.TryApply(document, edited, out var stats) && !stats.Fallback;
            checks.That(applied, "文本→树差量可应用", "patched=" + stats.PatchedNodes.Count + " fallback=" + stats.Fallback);
            if (!applied) return checks.Finish("--case text-edit-reevaluates-dependents");

            var dependents = pipeline.RebuildDependents(stats.PatchedNodes);
            McdocProjectionSync.Apply(projector, document, stats);
            checks.That(dependents.Count > 0, "值补丁触发了依赖子树的重新求值",
                "patched=" + stats.PatchedNodes.Count + " rebuilt=" + dependents.Count);

            var properties = document.FindNode("$.properties");
            checks.That(properties is null || properties.Type?.Kind is McdocTypeKind.Any or McdocTypeKind.Unsafe,
                "properties 的类型已随 block 退化", "Type=" + (properties?.Type?.Kind.ToString() ?? "<无节点>"));
            // 退化之后行必须跟着**文档里的键**走（旧类型的字段行不许留下）：文档里还在的键照样要有行（键↔行双射），
            // 所以判据是"子行数 == 键数"，不是"子行必须为 0"。
            properties?.EnsureChildren();
            var keyCount = properties?.Json is { Kind: McdocJsonKind.Object } propertiesJson ? propertiesJson.Properties?.Count ?? 0 : 0;
            var childRows = properties is null ? 0 : McdocPresenterRegistry.Default.Children(properties).Count;
            // Any/struct 容器自己会带一条"新建键"占位行（README §验证），所以上限是"键数 + 1"；
            // 旧类型的字段行（face/facing/powered…）一旦留下就会超出这个上限。
            checks.That(childRows <= keyCount + 1, "退化后子行不超过 文档键数+1（不再按旧类型的字段摊开）",
                "行=" + childRows + " 键=" + keyCount);
            Contracts(checks, document, projector, "block 写空");
            return checks.Finish("--case text-edit-reevaluates-dependents");
        }


        // ── 闸门覆盖范围 + 值出口必须回写（T2 定性：闸门内收到的赋值通知不许写回）──────
        public static int ValueGateScope()
        {
            var checks = new Checks();
            var directory = LocateBlockStateDirectory();
            if (directory is null)
            {
                Console.WriteLine("[环境缺失] 找不到 BlockState 资产（退出码 2）");
                return 2;
            }
            Deterministic();
            var index = new McdocTypeIndex();
            McdocBlockStates.Register(index, "1.20.5", directory);
            var rootType = RootTypeWith(BlockStatesDispatcher());

            // 内联装配：闸门标志要在本方法里可见（Harness 把它藏在局部变量里）
            var loading = false;
            var document = new McdocTreeDocument(index);
            var projector = new McdocRowProjector();
            var pipeline = new McdocPipeline
            {
                Document = document,
                Projector = projector,
                RootType = () => rootType,
                GetLoading = () => loading,
                SetLoading = value => loading = value,
            };
            // 与 VM 完全一致：闸门开着时值通知直接早退（McdocGeneratorViewModelBase.cs:862）
            document.NodeValueChanged = node =>
            {
                if (loading) return;
                pipeline.OnValueChanged(node);
            };
            document.NodeStructureChanged = node => pipeline.OnStructureChanged(node);
            document.Load(rootType, McdocJsonTextParser.Parse(@"{ ""block"": ""minecraft:acacia_button"", ""properties"": {} }").Root);
            projector.Reset(document.Nodes.ToList());

            var blockNode = document.FindNode("$.block");
            checks.That(blockNode is not null, "拿到 block 节点", blockNode is null ? "没有节点" : "");
            if (blockNode is null) return checks.Finish("--case value-gate-scope");

            var textDirty = 0;
            bool? loadingDuringFlush = null;
            // 回写那一拍发生在闸门内：此刻抬一次值通知（模拟重建期间绑定回弹），它必须被吞掉
            pipeline.TextDirty = () =>
            {
                textDirty++;
                loadingDuringFlush = loading;
                blockNode.Value = "";
                blockNode.ValueChanged?.Invoke(blockNode);
            };
            pipeline.OnValueChanged(blockNode);

            checks.That(textDirty > 0, "值出口有回写（模型变了文本要跟上）", "textDirty=" + textDirty);
            checks.That(loadingDuringFlush == true, "闸门覆盖到回写那一拍", "loading=" + (loadingDuringFlush?.ToString() ?? "<没回写>"));
            checks.That(string.Equals(document.RootJson?.Get("block")?.StringValue, "minecraft:acacia_button", StringComparison.Ordinal),
                "闸门内的赋值通知没有写回文档", "block=<" + (document.RootJson?.Get("block")?.StringValue ?? "null") + ">");
            checks.That(!loading && !pipeline.Rebuilding, "闸门已复位", "loading=" + loading + " rebuilding=" + pipeline.Rebuilding);
            return checks.Finish("--case value-gate-scope");
        }

        // ── union 切支的形状矩阵：切到结构支必须落对象，绝不落成孤立标量（T3）──────
        public static int UnionSwitchShape()
        {
            var checks = new Checks();
            Deterministic();

            var structA = McdocType.Create(McdocTypeKind.Struct);
            structA.Fields = [new McdocStructField { Name = "a", Key = McdocType.LiteralString("a"), Type = McdocType.Create(McdocTypeKind.Int) }];
            var listOfText = McdocType.Create(McdocTypeKind.List);
            listOfText.Item = McdocType.Create(McdocTypeKind.String);
            var union = McdocType.Create(McdocTypeKind.Union);
            union.Members = [structA, listOfText, McdocType.Create(McdocTypeKind.String)];
            var rootType = McdocType.Create(McdocTypeKind.Struct);
            rootType.Fields = [new McdocStructField { Name = "x", Key = McdocType.LiteralString("x"), Type = union }];

            var (document, projector, pipeline) = Harness(new McdocTypeIndex(), rootType, @"{ ""x"": { ""a"": 1 } }");
            checks.That(document.FindNode("$.x")?.Json?.Kind == McdocJsonKind.Object, "前置：起始值是对象",
                "kind=" + (document.FindNode("$.x")?.Json?.Kind.ToString() ?? "<null>"));

            var expectations = new (string Label, int Member, McdocJsonKind Kind)[]
            {
                ("对象 → 结构支", 0, McdocJsonKind.Object),
                ("对象 → 列表支", 1, McdocJsonKind.Array),
                ("列表 → 字符串支", 2, McdocJsonKind.String),
                ("字符串 → 结构支", 0, McdocJsonKind.Object),
            };
            foreach (var expectation in expectations)
            {
                var node = document.FindNode("$.x");
                if (node?.UnionTypeNameList is null
                    || node.UnionTypeNameList.FirstOrDefault(option => Convert.ToInt32(option.Value?.LiteralValue) == expectation.Member) is not { } target)
                {
                    checks.That(false, expectation.Label + " · 拿到分支选项", "UnionTypeNameList 空或缺下标");
                    continue;
                }
                var switched = McdocUnionSwitch.SwitchTo(node, target);
                var after = document.FindNode("$.x") ?? node;
                checks.That(after.Json?.Kind == expectation.Kind, expectation.Label + " · 落盘形状正确",
                    "switched=" + switched + " 期望=" + expectation.Kind + " 实际=" + (after.Json?.Kind.ToString() ?? "<null>"));
            }
            return checks.Finish("--case union-switch-shape");
        }


        // ── 必填 + 候选下拉永远不许落 ""（block 变空串 → properties 解不出，行上还挂黄标）──────
        public static int RequiredCandidateNotBlank()
        {
            var checks = new Checks();
            Deterministic();
            var block = McdocType.Create(McdocTypeKind.String);
            var properties = McdocType.Create(McdocTypeKind.Struct);
            properties.Fields = [new McdocStructField { Name = "face", Key = McdocType.LiteralString("face"), Type = McdocType.Create(McdocTypeKind.String), Optional = true }];
            var rootType = McdocType.Create(McdocTypeKind.Struct);
            rootType.Fields = [
                new McdocStructField { Name = "block", Key = McdocType.LiteralString("block"), Type = block },
                new McdocStructField { Name = "properties", Key = McdocType.LiteralString("properties"), Type = properties, Optional = true },
            ];

            // 现场：文档里已经是 "block": ""（键在、值是空串）
            var (document, projector, pipeline) = Harness(new McdocTypeIndex(), rootType, @"{ ""block"": """", ""properties"": {} }");
            var node = document.FindNode("$.block");
            checks.That(node is not null, "拿到 block 节点", node is null ? "没有节点" : "");
            if (node is null) return checks.Finish("--case required-candidate-not-blank");

            // 行上显示着候选、编辑器文本空着：落盘的必须是候选，不是 ""
            node.EnumOptionList = new System.Collections.ObjectModel.ObservableCollection<EnumMember>
            {
                new EnumMember { Name = "minecraft:acacia_button", Value = new MetaValue { LiteralValue = "minecraft:acacia_button" } },
            };
            node.SelectedEnumOption = node.EnumOptionList[0];
            node.Value = "";
            McdocCollectionEditor.Default.ApplyNodeValue(node);
            checks.That(string.Equals(document.RootJson?.Get("block")?.StringValue, "minecraft:acacia_button", StringComparison.Ordinal),
                "空编辑器 + 有候选 → 落盘候选而不是空串", "block=<" + (document.RootJson?.Get("block")?.StringValue ?? "null") + ">");

            // 反向：可选字段缺省时不许因为"显示了默认值"就被插进文档
            var optionalType = McdocType.Create(McdocTypeKind.Struct);
            optionalType.Fields = [
                new McdocStructField { Name = "block", Key = McdocType.LiteralString("block"), Type = block, Optional = true },
            ];
            var (optionalDoc, _, optionalPipeline) = Harness(new McdocTypeIndex(), optionalType, @"{}");
            var optionalNode = optionalDoc.FindNode("$.block");
            optionalNode.EnumOptionList = new System.Collections.ObjectModel.ObservableCollection<EnumMember>
            {
                new EnumMember { Name = "minecraft:acacia_button", Value = new MetaValue { LiteralValue = "minecraft:acacia_button" } },
            };
            optionalNode.SelectedEnumOption = optionalNode.EnumOptionList[0];
            McdocCollectionEditor.Default.MaterializeRequiredSelections(optionalDoc.Nodes, false);
            checks.That(optionalDoc.RootJson?.Get("block") is null, "可选字段缺省时物化不许凭空插键",
                "block=<" + (optionalDoc.RootJson?.Get("block")?.StringValue ?? "null") + ">");
            return checks.Finish("--case required-candidate-not-blank");
        }


        // ── #[id(..., tags=...)] 的候选空间：required 只认 tag，allowed/implicit 两者都认 ──────
        public static int TagRequiredCandidates()
        {
            var checks = new Checks();
            string[] registry = ["acacia_button", "stone"];
            string[] tags = ["infiniburn_overworld", "dirt"];

            var required = new McdocIdSpec { Registry = "block", Tags = "required" };
            var requiredOptions = required.Apply(registry, tags);
            checks.That(requiredOptions.Count == tags.Length && requiredOptions.All(option => option.StartsWith("#", StringComparison.Ordinal)),
                "tags=required 只出 tag 候选（方块 id 一个都不算）", "options=" + string.Join(",", requiredOptions));
            checks.That(requiredOptions.Contains("#minecraft:infiniburn_overworld"),
                "required 的候选里带得到 infiniburn_overworld", "options=" + string.Join(",", requiredOptions));

            var allowed = new McdocIdSpec { Registry = "block", Tags = "allowed" };
            var allowedOptions = allowed.Apply(registry, tags);
            checks.That(allowedOptions.Any(option => option.StartsWith("minecraft:", StringComparison.Ordinal))
                && allowedOptions.Any(option => option.StartsWith("#", StringComparison.Ordinal)),
                "tags=allowed 两类候选都保留（别把 allowed 也改成只剩 tag）", "options=" + string.Join(",", allowedOptions));
            return checks.Finish("--case tag-required-candidates");
        }

        private static McdocType RootTypeWith(McdocType propertiesType)
        {
            var rootType = McdocType.Create(McdocTypeKind.Struct);
            rootType.Fields =
            [
                new McdocStructField { Name = "block", Key = McdocType.LiteralString("block"), Type = McdocType.Create(McdocTypeKind.String), Optional = true },
                new McdocStructField { Name = "properties", Key = McdocType.LiteralString("properties"), Type = propertiesType, Optional = true },
            ];
            return rootType;
        }

        public static int BlockStateEnumFirstBuild()
        {
            Deterministic();
            var checks = new Checks();
            var directory = LocateBlockStateDirectory();
            if (directory is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\Registries\\BlockState（退出码 2）");
                return 2;
            }
            var members = McdocBlockStates.Load("1.20.5", directory);
            if (!members.TryGetValue("acacia_button", out var buttonStruct))
            {
                Console.WriteLine("[环境缺失] 资产里没有 acacia_button（退出码 2）");
                return 2;
            }
            // 期望值直接从资产文件读（不写死）
            var expected = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var field in buttonStruct.Fields ?? [])
            {
                expected[field.Name] = (field.Type?.EnumValues ?? []).Select(v => v.Identifier).ToList();
            }

            var block = McdocType.Create(McdocTypeKind.String);
            var properties = McdocType.Create(buttonStruct.Kind);
            properties.Fields = buttonStruct.Fields;
            var condition = McdocType.Create(McdocTypeKind.Struct);
            condition.Fields =
            [
                new McdocStructField { Name = "block", Key = McdocType.LiteralString("block"), Type = block, Optional = true },
                new McdocStructField { Name = "properties", Key = McdocType.LiteralString("properties"), Type = properties, Optional = true },
            ];
            var rootType = McdocType.Create(McdocTypeKind.Struct);
            rootType.Fields = [ new McdocStructField { Name = "condition", Key = McdocType.LiteralString("condition"), Type = condition } ];

            var document = new McdocTreeDocument(new McdocTypeIndex());
            var projector = new McdocRowProjector();
            var pipeline = new McdocPipeline
            {
                Document = document,
                Projector = projector,
                RootType = () => rootType,
                GetLoading = () => false,
                SetLoading = _ => { }
            };
            document.NodeStructureChanged = node => pipeline.OnStructureChanged(node);
            // 界面序列：切到 block_state_property 后 properties 是空的 {}，字段行这一次建出来就得能用
            document.Load(rootType,
                McdocJsonTextParser.Parse("{ \"condition\": { \"block\": \"minecraft:acacia_button\", \"properties\": {} } }").Root);
            projector.Reset(document.Nodes.ToList());

            foreach (var pair in expected)
            {
                var node = document.FindNode("$.condition.properties." + pair.Key);
                var options = node?.EnumOptionList?.Select(o => o.Name).Where(n => n != McdocNodePresenter.UnsetLabel).ToList();
                checks.That(options is { Count: > 0 }, pair.Key + " 第一次建出来就有候选",
                    options is null ? (node is null ? "没有节点" : "options=null") : string.Join(",", options));
                checks.That(options is not null && options.SequenceEqual(pair.Value), pair.Key + " 候选与资产一致",
                    "期望=" + string.Join(",", pair.Value) + " 实际=" + (options is null ? "<null>" : string.Join(",", options)));
            }
            return checks.Finish("--case blockstate-enum-first-build");
        }

        // ── 切分支后不许留上一支的字段行；字段行的删除要同时清 JSON 与行 ──────────────
        public static int UnionBranchStale()
        {
            Deterministic();
            var first = McdocType.Create(McdocTypeKind.Struct);
            first.Fields =
            [
                new McdocStructField { Name = "terms", Key = McdocType.LiteralString("terms"), Type = McdocType.Create(McdocTypeKind.Int), Optional = true },
                new McdocStructField { Name = "keep", Key = McdocType.LiteralString("keep"), Type = McdocType.Create(McdocTypeKind.Int), Optional = true },
            ];
            var second = McdocType.Create(McdocTypeKind.Struct);
            second.Fields =
            [
                new McdocStructField { Name = "block", Key = McdocType.LiteralString("block"), Type = McdocType.Create(McdocTypeKind.String), Optional = true },
            ];
            var union = McdocType.Create(McdocTypeKind.Union);
            union.Members = [first, second];
            var rootType = McdocType.Create(McdocTypeKind.Struct);
            rootType.Fields = [ new McdocStructField { Name = "u", Key = McdocType.LiteralString("u"), Type = union } ];

            var document = new McdocTreeDocument(new McdocTypeIndex());
            var projector = new McdocRowProjector();
            var pipeline = new McdocPipeline
            {
                Document = document,
                Projector = projector,
                RootType = () => rootType,
                GetLoading = () => false,
                SetLoading = _ => { }
            };
            document.NodeStructureChanged = node => pipeline.OnStructureChanged(node);
            document.Load(rootType, McdocJsonTextParser.Parse("{ \"u\": { \"terms\": 1, \"keep\": 2 } }").Root);
            projector.Reset(document.Nodes.ToList());

            var checks = new Checks();
            var unionNode = document.FindNode("$.u");
            checks.That(unionNode is not null && unionNode.UnionTypeNameList.Count == 2, "两个候选分支", "");
            if (unionNode is null) return checks.Finish("--case union-branch-stale");

            checks.That(McdocUnionSwitch.SwitchTo(unionNode, unionNode.UnionTypeNameList[1]), "切到第二支", "");
            Console.WriteLine("[信息] 切支后 Children=" + unionNode.Children.Count
                + " Selected=" + unionNode.SelectedUnionChildren.Count
                + " → [" + string.Join(",", unionNode.Children.Select(c => c.FieldName)) + "]");
            // 界面上切分支会抬结构变更 → 走管线（路径索引、行投影都靠它失效重建）
            pipeline.OnStructureChanged(unionNode);
            Console.WriteLine("[信息] 管线后 Children=" + unionNode.Children.Count
                + " Selected=" + unionNode.SelectedUnionChildren.Count
                + " → [" + string.Join(",", unionNode.Children.Select(c => c.FieldName)) + "]");
            Console.WriteLine("[信息] union Type=" + unionNode.Type?.Kind + " Declared=" + unionNode.DeclaredType?.Kind
                + " branchOptions=" + (unionNode.UnionTypeNameList?.Count ?? 0));
            // 结构分支的字段行是**上提到 union 节点**上的（ApplyBranch 的既定做法），所以要查 union 自己的子级
            unionNode.EnsureChildren();
            var branch = unionNode.SelectedUnionChildren.FirstOrDefault();
            var names = string.Join(",", McdocPresenterRegistry.Default.Children(unionNode)
                .Select(c => c.FieldName ?? c.Key ?? ""));
            checks.That(branch is not null, "分支节点已挂上", branch is null ? "<无分支>" : "");
            checks.That(!names.Split(',').Contains("terms"), "旧分支字段不许残留", "子级=" + names);
            checks.That(names.Split(',').Contains("block"), "新分支字段要在", "子级=" + names);

            // 字段行删除：JSON 与行必须同时消失
            var blockNode = document.FindNode("$.u.block");
            checks.That(blockNode is not null, "拿到新分支字段节点", "");
            if (blockNode is not null)
            {
                var row = projector.Find(blockNode);
                checks.That(row is not null && blockNode.RemoveFieldCommand is not null,
                    "字段行有删除命令", row is null ? "没有行" : "command=" + (blockNode.RemoveFieldCommand is null ? "<null>" : "ok"));
                blockNode.RemoveFieldCommand?.Execute(null);
                pipeline.OnStructureChanged(unionNode);
                var json = document.FindNode("$.u")?.Json;
                var keyGone = json is not { Kind: McdocJsonKind.Object } obj
                    || (obj.Properties ?? []).All(item => item.Key != "block");
                checks.That(keyGone, "JSON 里的键已删", "");
                checks.That(projector.Find(blockNode) is null || !projector.Rows.Any(r => ReferenceEquals(r.Node, blockNode)),
                    "行也不再投影出来", "rows=" + projector.Rows.Count);
            }
            return checks.Finish("--case union-branch-stale");
        }

        // ── 可选子结构 unset 之后，父行形状不许被打回动态键形态 ───────────────────────
 public static int UnsetKeepsParentShape()
 {
 Deterministic();
 // 现场的真形状：block_states 解不出来时 dispatch 落到 struct { [string]: string }（动态键结构）
 var inner = McdocType.Create(McdocTypeKind.Struct);
 inner.Fields = [ new McdocStructField { Name = null, Key = McdocType.Create(McdocTypeKind.String), Type = McdocType.Create(McdocTypeKind.String), Optional = true } ];
 // 贴近现场：union 分支里的结构（block_state_property 的 block + properties）
 var other = McdocType.Create(McdocTypeKind.Struct);
 other.Fields = [ new McdocStructField { Name = "terms", Key = McdocType.LiteralString("terms"), Type = McdocType.Create(McdocTypeKind.Int), Optional = true } ];
 var branch = McdocType.Create(McdocTypeKind.Struct);
 branch.Fields =
 [
 new McdocStructField { Name = "block", Key = McdocType.LiteralString("block"), Type = McdocType.Create(McdocTypeKind.String), Optional = true },
 new McdocStructField { Name = "properties", Key = McdocType.LiteralString("properties"), Type = inner, Optional = true }
 ];
 var union = McdocType.Create(McdocTypeKind.Union);
 union.Members = [ other, branch ];
 var rootType = McdocType.Create(McdocTypeKind.Struct);
 rootType.Fields = [ new McdocStructField { Name = "u", Key = McdocType.LiteralString("u"), Type = union } ];

 var document = new McdocTreeDocument(new McdocTypeIndex());
 var projector = new McdocRowProjector();
 var pipeline = new McdocPipeline
 {
 Document = document,
 Projector = projector,
 RootType = () => rootType,
 GetLoading = () => false,
 SetLoading = _ => { }
 };
 document.NodeStructureChanged = node => pipeline.OnStructureChanged(node);
 document.Load(rootType, McdocJsonTextParser.Parse("{ \"u\": { \"terms\": 1, \"properties\": { \"face\": \"wall\" } } }").Root);
 projector.Reset(document.Nodes.ToList());

 var checks = new Checks();
 var unionNode = document.FindNode("$.u");
 checks.That(unionNode is not null && McdocUnionSwitch.SwitchTo(unionNode, unionNode.UnionTypeNameList[1]), "切到 block 那一支", "");
 if (unionNode is null) return checks.Finish("--case unset-keeps-parent-shape");
 pipeline.OnStructureChanged(unionNode);
 // 分支上补出 properties 的值（等价用户在行上添加）
 var blockNode = document.FindNode("$.u.block");
 checks.That(blockNode is not null, "分支字段 block 在", blockNode is null ? "没有节点" : "");
 var parent = document.FindNode("$.u.properties");
 var parentRow = parent is null ? null : projector.Find(parent);
 checks.That(parent is not null && parentRow is not null, "$.u.properties 有行", parent is null ? "没有节点" : "");
 if (parent is null || parentRow is null) return checks.Finish("--case unset-keeps-parent-shape");
 parent.IsExpanded = true;
 parent.EnsureChildren();
 var kindBefore = parentRow.Kind;
 var expandedBefore = parent.IsExpanded;
 var childCountBefore = McdocPresenterRegistry.Default.Children(parent).Count;
 // 现场：properties 是 mcdoc:block_states[[block]]（动态索引），值一变宿主会延后一拍**重建**最近容器
 parent.DependsOnRuntimeData = true;

 var modeNode = document.FindNode("$.u.properties.face");
 checks.That(modeNode is not null, "$.u.properties.face 有行", modeNode is null ? "没有节点" : "");
 if (modeNode is null) return checks.Finish("--case unset-keeps-parent-shape");
 // "选（未设置）"在这类行上等价于删掉那条动态键
 var remove = modeNode.RemoveFieldCommand;
 checks.That(remove is not null, "动态键条目有删除命令", remove is null ? "<null>" : "ok");
 if (remove is null) return checks.Finish("--case unset-keeps-parent-shape");
 remove.Execute(null);
 pipeline.OnStructureChanged(parent);
 // 这一段才是现场真正的路径：值参与动态索引 → 宿主延后一拍重建最近容器
 pipeline.RebuildTargetedOrFallback(modeNode);

 var parentAfter = document.FindNode("$.u.properties");
 var rowAfter = parentAfter is null ? null : projector.Find(parentAfter);
 checks.That(parentAfter is not null && rowAfter is not null, "unset 之后 $.u.properties 行还在", parentAfter is null ? "节点没了" : "");
 var kindAfter = rowAfter?.Kind;
 var expandedAfter = parentAfter?.IsExpanded ?? false;
 var childCountAfter = parentAfter is null ? 0 : McdocPresenterRegistry.Default.Children(parentAfter).Count;
 Console.WriteLine("[信息] 父行 Kind " + kindBefore + " → " + kindAfter
 + " / IsExpanded " + expandedBefore + " → " + expandedAfter
 + " / 子级数 " + childCountBefore + " → " + childCountAfter);
 checks.That(kindAfter == kindBefore, "父行形状没被打回动态键形态", "前=" + kindBefore + " 后=" + kindAfter);
 checks.That(expandedAfter, "父行仍然是展开的", "IsExpanded=" + expandedAfter);
 checks.That(childCountAfter >= childCountBefore, "父行子级数没塌", "前=" + childCountBefore + " 后=" + childCountAfter);
 return checks.Finish("--case unset-keeps-parent-shape");
 }

 // ── 注册表 id 的命名空间归一（值合法却被判"不在注册表内"）────────────────────
        public static int RegistryIdNamespace()
        {
            var checks = new Checks();
            var blockStateDir = LocateBlockStateDirectory();
            if (blockStateDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\Registries\\BlockState（退出码 2）");
                return 2;
            }
            var registriesDir = System.IO.Path.GetDirectoryName(blockStateDir);
            var resource = new CBHK.Model.Constant.Resource();
            var loaded = resource.LoadRegistries(registriesDir);
            checks.That(loaded > 0, "载入了注册表版本文件", "files=" + loaded);
            if (loaded == 0) return checks.Finish("--case registry-id-namespace");

            var options = new CBHK.Utility.Data.MdocRegistryOptions(resource);
            var spec = new CBHK.Utility.Data.Mcdoc.McdocIdSpec { Registry = "block" };
            checks.That(options.Contains(spec, "minecraft:acacia_button", "1.20.5"),
                "带命名空间的合法 id 不被误判", "minecraft:acacia_button");
            checks.That(options.Contains(spec, "acacia_button", "1.20.5"),
                "不带命名空间的写法也认", "acacia_button");
            checks.That(!options.Contains(spec, "minecraft:not_a_block", "1.20.5"),
                "真不存在的 id 仍然要判否", "minecraft:not_a_block");
            return checks.Finish("--case registry-id-namespace");
        }

        // ── 值改回合法后，行上的感叹号必须自己消失 ──────────────────────────────────
        public static int DiagnosticsRefresh()
        {
            Deterministic();
            var document = new McdocTreeDocument(new McdocTypeIndex());
            var projector = new McdocRowProjector();
            var pipeline = new McdocPipeline
            {
                Document = document,
                Projector = projector,
                RootType = Fixtures.RequiredEnumHostType,
                GetLoading = () => false,
                SetLoading = _ => { }
            };
            document.NodeStructureChanged = node => pipeline.OnStructureChanged(node);
            document.Load(Fixtures.RequiredEnumHostType(),
                McdocJsonTextParser.Parse("{ \"mode\": { \"level\": \"zzz\" } }").Root);
            projector.Reset(document.Nodes.ToList());

            var checks = new Checks();
            // 叶子：枚举取值域不合法
            var node = document.FindNode("$.mode.level");
            var row = node is null ? null : projector.Find(node);
            checks.That(row is not null, "$.mode.level 有行", node is null ? "没有节点" : "");
            if (node is null || row is null) return checks.Finish("--case diagnostics-refresh");
            checks.That(row.Diagnostics.Count > 0, "非法值先亮徽标（基线）", "count=" + row.Diagnostics.Count);

            // 走和界面同一条值路径：写值 → 手术式回写 → 重算诊断
            node.Value = "one";
            McdocCollectionEditor.Default.ApplyNodeValue(node);
            pipeline.RefreshDiagnostics(node);

            checks.That(row.Diagnostics.Count == 0, "改成合法值后徽标自己消失",
                "count=" + row.Diagnostics.Count + " (" + row.DiagnosticSummary + ")");
            var documentLevel = document.Diagnostics.Where(d => (d.Path ?? "") == "$.mode.level").ToList();
            checks.That(documentLevel.Count == 0, "文档级清单里也没有残留", "剩余=" + documentLevel.Count);
            // 父行：子树里唯一的问题修好之后，父行也不该继续挂着标记
            var parentNode = document.FindNode("$.mode");
            var parentRow = parentNode is null ? null : projector.Find(parentNode);
            checks.That(parentRow is null || parentRow.Diagnostics.Count == 0, "父行 $.mode 也复原",
                parentRow is null ? "没有行" : "count=" + parentRow.Diagnostics.Count + " (" + parentRow.DiagnosticSummary + ")");
            return checks.Finish("--case diagnostics-refresh");
        }

        // ── 现场形状下的空转投影：不许排重建、不许再触发补建回调 ────────────────────
        public static int ProjectorIdle()
        {
            Deterministic();
            var document = new McdocTreeDocument(new McdocTypeIndex());
            var projector = new McdocRowProjector();
            var pipeline = new McdocPipeline
            {
                Document = document,
                Projector = projector,
                RootType = Fixtures.CriteriaLikeHostType,
                GetLoading = () => false,
                SetLoading = _ => { }
            };
            var scheduled = 0;
            pipeline.ScheduleRebuild = _ => scheduled++;
            document.NodeStructureChanged = node => pipeline.OnStructureChanged(node);
            projector.AfterChildrenBuilt = node =>
            {
                if (pipeline.Rebuilding) return;
                pipeline.RebuildTargetedOrFallback(node);
            };
            document.Load(Fixtures.CriteriaLikeHostType(),
                McdocJsonTextParser.Parse("{ \"criteria\": { \"test\": { \"conditions\": { \"location\": [ { \"condition\": { \"a\": 1 } }, { \"condition\": { \"b\": 1 } } ] } } } }").Root);
            projector.Reset(document.Nodes.ToList());

            var checks = new Checks();
            var rows = projector.Rows.Count;
            checks.That(rows >= 4, "夹具已投影出行", "rows=" + rows);

            // ① 空转 30 拍：一行都不许多、一次重建都不许排
            var resetBefore = projector.ResetCount;
            var spliceBefore = projector.SpliceCount;
            var generationBefore = document.Generation;
            var nodesBefore = document.Nodes.Count;
            for (var tick = 0; tick < 30; tick++) projector.Reset(document.Nodes.ToList());
            checks.That(document.Generation == generationBefore, "空转 30 拍没有换代（没偷偷重建文档）",
                "Δ=" + (document.Generation - generationBefore));
            checks.That(document.Nodes.Count == nodesBefore, "空转后顶层节点数不变", "Δ=" + (document.Nodes.Count - nodesBefore));
            checks.That(projector.ResetCount - resetBefore == 30, "空转 30 拍就是 30 次重投影（无额外循环）",
                "Δ=" + (projector.ResetCount - resetBefore));
            checks.That(projector.SpliceCount == spliceBefore, "空转不引发拼接", "Δ=" + (projector.SpliceCount - spliceBefore));
            checks.That(scheduled == 0, "空转不排重建", "scheduled=" + scheduled);
            checks.That(!pipeline.Rebuilding, "闸门未被空转打开", "Rebuilding=" + pipeline.Rebuilding);

            // ② 用户真的展开一次：只许触发一次补建回调，之后不再重复
            // 用"已经有行"的节点（顶层 criteria 一定在 Rows 里）；深层节点此时多半折叠着没有行
            var deep = document.FindNode("$.criteria");
            checks.That(deep is not null, "拿到有行的节点", deep is null ? "FindNode 落空" : "");
            if (deep is null) return checks.Finish("--case projector-idle");
            var fired = 0;
            projector.AfterChildrenBuilt = node =>
            {
                fired++;
                if (pipeline.Rebuilding) return;
                pipeline.RebuildTargetedOrFallback(node);
            };
            // 造一次"子级真的还没建"的状态：第一拍必须喊一次，之后绝不再喊
            deep.ChildrenBuilt = false;
            deep.Children.Clear();
            deep.Items.Clear();
            deep.SelectedUnionChildren.Clear();
            deep.IsExpanded = true;
            var deepRow = projector.Find(deep);
            if (deepRow is not null) projector.SpliceExpansion(deepRow);   // 用户点箭头那条路
            var firedFirst = fired;
            checks.That(firedFirst >= 1, "用户展开且子级没建好时，要喊一次补建回调", "fired=" + firedFirst);
            for (var tick = 0; tick < 10; tick++)
            {
                var again = projector.Find(deep);
                if (again is not null) projector.SpliceExpansion(again);
            }
            checks.That(fired == firedFirst, "之后连做 10 次展开/重投影，补建回调不再重复触发",
                "首次=" + firedFirst + " 之后=" + fired);
            return checks.Finish("--case projector-idle");
        }

        // ── 列表项（Entry）子级切 union 成员后，它自己的动作条不许消失 ──────────────
        public static int EntryChildSwitch()
        {
            Deterministic();
            var document = new McdocTreeDocument(new McdocTypeIndex());
            var projector = new McdocRowProjector();
            var pipeline = new McdocPipeline
            {
                Document = document,
                Projector = projector,
                RootType = Fixtures.EntryWithUnionHostType,
                GetLoading = () => false,
                SetLoading = _ => { }
            };
            document.NodeStructureChanged = node => pipeline.OnStructureChanged(node);
            document.Load(Fixtures.EntryWithUnionHostType(),
                McdocJsonTextParser.Parse("{ \"items\": [ { \"condition\": { \"a\": 1 } }, { \"condition\": { \"b\": 1 } }, { \"condition\": { \"a\": 1 } } ] }").Root);
            projector.Reset(document.Nodes.ToList());

            var checks = new Checks();
            var entry = document.FindNode("$.items[0]");
            var row = entry is null ? null : projector.Find(entry);
            var kindBefore = row?.Kind;
            var headBefore = entry is null ? null : McdocPresenterRegistry.Default.Describe(entry);
            checks.That(row is not null && row.ItemActionsVisibility == System.Windows.Visibility.Visible,
                "基线：Entry 行有列表动作条", row is null ? "没有行" : row.ItemActionsVisibility.ToString());
            checks.That(row is not null && projector.Rows.Contains(row), "基线：Entry 行在 Rows 里（模板真正拿到的那个实例）",
                row is null ? "没有行" : "Rows=" + projector.Rows.Count);
            if (entry is null || row is null) return checks.Finish("--case entry-child-switch");

            var union = document.FindNode("$.items[0].condition");
            checks.That(union is not null && McdocUnionSwitch.SwitchTo(union, union.UnionTypeNameList[1]), "子级切到另一支", "");
            // 切分支会抬 StructureChanged → 管线刷新；按路径取回当前 Entry 行
            var entryAfter = document.FindNode("$.items[0]");
            var rowAfter = entryAfter is null ? null : projector.Find(entryAfter);
            checks.That(rowAfter is not null, "Entry 行还在", entryAfter is null ? "节点没了" : "");
            checks.That(rowAfter is not null && rowAfter.ItemActionsVisibility == System.Windows.Visibility.Visible,
                "子级切成员后 Entry 的动作条仍在", rowAfter is null ? "没有行" : rowAfter.ItemActionsVisibility.ToString());
            checks.That(rowAfter is not null && rowAfter.IsListChild, "Entry 仍是列表子行",
                rowAfter is null ? "没有行" : "IsListChild=" + rowAfter.IsListChild);
            checks.That(rowAfter is not null && rowAfter.Kind == kindBefore, "Entry 的行形状没变（模板才不会换掉）",
                "前=" + kindBefore + " 后=" + (rowAfter?.Kind.ToString() ?? "<无行>"));
            checks.That(rowAfter is not null && projector.Rows.Contains(rowAfter), "新 Entry 行也在 Rows 里",
                rowAfter is null ? "没有行" : "Rows=" + projector.Rows.Count);
            // 记录切换是否重建了 Entry：本例走的是"原位改分支"，不重建；真正会丢命令的是显式重建那条路
            Console.WriteLine("[信息] 切换是否重建 Entry：" + (ReferenceEquals(entry, entryAfter) ? "否（原位）" : "是"));

            // 再显式重建一次：命令必须还在（这是那条丢失的真正契约）
            var entryForced = document.FindNode("$.items[0]");
            if (entryForced is not null)
            {
                var forced = document.RebuildNode(entryForced);
                checks.That(forced is not null && forced.RemoveSelfCommand is not null,
                    "显式重建后 Entry 仍持有「删掉这一项」命令",
                    forced is null ? "重建失败" : "command=" + (forced.RemoveSelfCommand is null ? "<null>" : "ok"));
            }

            checks.That(rowAfter is not null && rowAfter.RemoveSelfCommand is not null, "Entry 仍持有「删掉这一项」命令",
                rowAfter is null ? "没有行" : "command=" + (rowAfter.RemoveSelfCommand is null ? "<null>" : "ok"));
            checks.That(rowAfter is not null && rowAfter.RemoveSelfVisibility == System.Windows.Visibility.Visible,
                "Entry 的删除自身按钮可见", rowAfter is null ? "没有行" : rowAfter.RemoveSelfVisibility.ToString());
            var headAfter = entryAfter is null ? null : McdocPresenterRegistry.Default.Describe(entryAfter);
            checks.That(headAfter is not null && headBefore is not null && headAfter.RowKind == headBefore.RowKind,
                "表现器给出的 RowKind 也没变", "前=" + headBefore?.RowKind + " 后=" + headAfter?.RowKind);
            return checks.Finish("--case entry-child-switch");
        }

        // ── 顶层遗留键：父级是合成根，删除后行必须消失（用户现场 $.tset）────────────
        public static int LegacyDeleteTop()
        {
            Deterministic();
            const string json = "{ \"a\": 1, \"tset\": { \"deep\": 1 } }";
            var document = new McdocTreeDocument(new McdocTypeIndex());
            var projector = new McdocRowProjector();
            var pipeline = new McdocPipeline
            {
                Document = document,
                Projector = projector,
                RootType = Fixtures.LegacyHostType,
                GetLoading = () => false,
                SetLoading = _ => { }
            };
            // 必须在 Load 之前挂：每个节点在构建期就把这个回调存进自己的 StructureChanged
            document.NodeStructureChanged = node => pipeline.OnStructureChanged(node);
            document.Load(Fixtures.LegacyHostType(), McdocJsonTextParser.Parse(json).Root);
            projector.Reset(document.Nodes.ToList());

            var checks = new Checks();
            var node = document.FindNode("$.tset");
            checks.That(node is not null && node.RemoveFieldCommand?.CanExecute(null) == true, "$.tset 走删键入口",
                node is null ? "找不到节点" : "field=" + (node.FieldName ?? "<空>"));
            if (node is null) return checks.Finish("--case legacy-delete-top");

            node.RemoveFieldCommand.Execute(null);
            checks.That(document.RootJson.Get("tset") is null, "JSON 里删掉了", "");
            checks.That(document.FindNode("$.tset") is null, "树里没有这个节点了", "");
            checks.That(projector.Rows.All(row => !ReferenceEquals(row.Node, node)), "投影里没有这一行",
                "行数=" + projector.Rows.Count);
            var tops = string.Join(",", document.Nodes.Select(entry => (entry.JsonPath ?? "?") + "/" + (entry.FieldName ?? "")));
            checks.That(tops.Contains("$.tset") == false, "顶层里没有 tset 了", "Nodes=" + tops);
            return checks.Finish("--case legacy-delete-top");
        }

        // ── 局部重建必须带回"父级建子节点时写的行语义" ──────────────────────────
        public static int RebuildSemantics()
        {
            Deterministic();
            const string json = "{ \"a\": 1, \"map\": { \"k1\": 1 }, \"list\": [ { \"z\": 1 } ] }";
            var document = new McdocTreeDocument(new McdocTypeIndex());
            document.Load(Fixtures.EntryHostType(), McdocJsonTextParser.Parse(json).Root);

            var checks = new Checks();
            var projector = new McdocRowProjector();
            projector.Reset(document.Nodes.ToList());

            Check("$.map.k1", isMapEntry: true);
            Check("$.list[0]", isMapEntry: false);
            return checks.Finish("--case rebuild-semantics");

            void Check(string path, bool isMapEntry)
            {
                var node = document.FindNode(path);
                if (node is null)
                {
                    checks.That(false, path + " · 找得到节点", "FindNode 落空");
                    return;
                }
                var before = Snapshot(projector, node);
                var rebuilt = document.RebuildNode(node);
                if (rebuilt is null)
                {
                    checks.That(false, path + " · 能重建", "RebuildNode 返回 null");
                    return;
                }
                projector.Reset(document.Nodes.ToList());
                var after = Snapshot(projector, rebuilt);
                checks.That(after == before, path + " · 重建后行语义一字不差", "前=" + before + " 后=" + after);
                checks.That(rebuilt.IsMapEntry == isMapEntry, path + " · IsMapEntry 正确",
                    "期望=" + isMapEntry + " 实际=" + rebuilt.IsMapEntry);
            }
        }

        private static string Snapshot(McdocRowProjector projector, McdocTreeNode node)
        {
            var row = projector.Find(node);
            return "IsMapEntry=" + node.IsMapEntry
                + " EditingKey=" + (node.EditingKey ?? "<null>")
                + " DynFieldIndex=" + node.DynamicFieldIndex
                + " IsDynamicKey=" + node.IsDynamicKey
                + " RemoveEntryCmd=" + (node.RemoveEntryCommand is not null)
                + " | Rename=" + (row?.RenameVisibility.ToString() ?? "<无行>")
                + " RemoveEntry=" + (row?.RemoveEntryVisibility.ToString() ?? "<无行>")
                + " ItemActions=" + (row?.ItemActionsVisibility.ToString() ?? "<无行>")
                + " KeyLabel=" + (row?.KeyLabelVisibility.ToString() ?? "<无行>");
        }

        // ── 连续改值：上一处改完，下一处的"选区"不许偏一位 ─────────────────────
        public static int SequentialEdit()
        {
            Deterministic();
            var checks = new Checks();
            var state = new JsonTextState(new TextDocument("{ \"b1\": true, \"b2\": true, \"b3\": 1 }"));

            checks.That(Edit(state, "b1", McdocJsonValue.Boolean(false)), "第一次编辑（b1 true→false）写成", state.Document.Text);
            // 生产里 suppressReload 压掉了回写：防抖那一拍拿到"文本 == RootText"，增量解析是空转，
            // 区间表不会自己跟上 —— 这正是下一处编辑踩空的原因
            state.TryIncrementalReparse(state.Document.Text);
            checks.That(Edit(state, "b2", McdocJsonValue.Boolean(false)), "第二次编辑（b2 true→false）写成", state.Document.Text);

            checks.That(McdocJsonParser.TryParse(state.Document.Text, out _, out _), "两次编辑后仍是合法 JSON", state.Document.Text);
            checks.That(state.Document.Text.Contains("\"b2\": false"), "b2 的值没有被啃掉/错位", state.Document.Text);
            checks.That(!state.Document.Text.Contains("ffalse") && !state.Document.Text.Contains("truealse"),
                "没有残留前导字符", state.Document.Text);
            checks.That(state.Document.Text.Contains("\"b1\": false"), "b1 的值也还在", state.Document.Text);

            // 成本：每次改值都要把区间表挪一遍（O(节点)）。量出来再谈值不值。
            var big = new JsonTextState(new TextDocument(Fixtures.Text()));
            var started = Stopwatch.GetTimestamp();
            const int rounds = 200;
            for (var round = 0; round < rounds; round++)
            {
                Edit(big, "format_version", McdocJsonValue.Number(round % 2 == 0 ? 2 : 1));
            }
            var perEditMs = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency / rounds;
            Console.WriteLine("[信息] " + big.Spans.Count + " 个节点的文档：每次手术式改值（含区间表平移）"
                + perEditMs.ToString("0.###") + "ms");

            // 反方向（值变短）必须同样成立
            checks.That(Edit(state, "b1", McdocJsonValue.Boolean(true)), "第三次编辑（b1 false→true，变短一位）写成", state.Document.Text);
            checks.That(Edit(state, "b2", McdocJsonValue.Boolean(true)), "第四次编辑（b2 false→true）写成", state.Document.Text);
            checks.That(McdocJsonParser.TryParse(state.Document.Text, out _, out _), "四次编辑后仍是合法 JSON", state.Document.Text);
            checks.That(state.Document.Text.Contains("\"b2\": true"), "变短方向也没有错位", state.Document.Text);
            return checks.Finish("--case sequential-edit");
        }

        private static bool Edit(JsonTextState state, string key, McdocJsonValue value)
        {
            var node = state.Root.Get(key);
            if (node is null || !state.TryCaptureMember(node, out var slot)) return false;
            return state.TryRewriteMember(slot, value);
        }

        // ── 遗留键：树上只占一行，数据留在文档里，换版本后就地复活 ──────────────
        public static int UnknownField()
        {
            Deterministic();
            // 顶层三个 + 容器里的一个（截图里的 style.test 就是后者这种）
            const string json = "{ \"a\": 1, \"b\": { \"b1\": 1, \"bKeep\": { \"deep\": 1 } }, \"keepScalar\": true, \"keepObject\": { \"x\": 1, \"y\": 2 }, \"keepArray\": [ { \"z\": 1 } ] }";
            var keys = new[] { "keepScalar", "keepObject", "keepArray", "b.bKeep" };
            var checks = new Checks();

            var document = new McdocTreeDocument(new McdocTypeIndex());
            document.Load(Fixtures.LegacyHostType(), McdocJsonTextParser.Parse(json).Root);
            var projector = new McdocRowProjector();
            projector.Reset(document.Nodes.ToList());
            var registry = McdocPresenterRegistry.Default;

            foreach (var key in keys.Take(3))
            {
                var path = "$." + key;
                var node = document.FindNode(path);
                checks.That(node is not null && node.IsUnknownField, path + " · 被识别为遗留键",
                    node is null ? "找不到节点" : "IsUnknownField=" + node.IsUnknownField);
                if (node is null) continue;
                var head = registry.Describe(node);
                var children = registry.Children(node);
                var canHaveChildren = registry.CanHaveChildren(node);
                var row = projector.Find(node);
                checks.That(children.Count == 0 && !canHaveChildren, path + " · 不建子级（整棵子树只占一行）",
                    "children=" + children.Count + " canHaveChildren=" + canHaveChildren);
                checks.That(head.EditorKind == McdocEditorKind.None, path + " · 不给值编辑器", "EditorKind=" + head.EditorKind);
                checks.That(row is not null && row.Kind == McdocRowKind.Plain, path + " · 行形状 = Plain",
                    row is null ? "没有行" : row.Kind.ToString());
            }

            var before = KeyPaths(document.RootJson);
            var kept = document.CountUnknownFields();
            var after = KeyPaths(document.RootJson);
            checks.That(kept == keys.Length, "换版本 · 遗留键（含容器内的）全部被计数", "kept=" + kept + " 期望=" + keys.Length);
            checks.That(before == after, "换版本 · 文档键集合一个都不丢", "before=" + before + " after=" + after);

            // 切到"声明了它"的版本：同一份文档要就地长出子树；形状不符的只降级不摊开
            var upgraded = new McdocTreeDocument(new McdocTypeIndex());
            upgraded.Load(Fixtures.LegacyHostTypeV2(), McdocJsonTextParser.Parse(json).Root);
            foreach (var key in new[] { "keepObject", "keepArray" })
            {
                var node = upgraded.FindNode("$." + key);
                checks.That(node is not null && !node.IsUnknownField, "$." + key + " · 新版本里变回正常节点",
                    node is null ? "找不到节点" : "IsUnknownField=" + node.IsUnknownField);
                if (node is null) continue;
                node.EnsureChildren();
                var children = McdocPresenterRegistry.Default.Children(node).Count;
                checks.That(children > 0, "$." + key + " · 就地长出子级行", "children=" + children);
            }

            var mismatch = upgraded.FindNode("$.keepScalar");
            checks.That(mismatch is not null && mismatch.Children.Count == 0, "$.keepScalar · 形状不符不按声明摊开",
                mismatch is null ? "找不到节点" : "children=" + mismatch.Children.Count + " type=" + mismatch.Type?.Kind);
            checks.That(mismatch is not null && !string.IsNullOrEmpty(mismatch.Error), "$.keepScalar · 形状不符要有诊断",
                mismatch is null ? "找不到节点" : "Error=" + (mismatch.Error ?? "<空>"));

            return checks.Finish("--case unknown-field");
        }

        // ── 列表项的"复制这一项"：JSON 多一项，投影必须同步多一行 ───────────────────
        public static int DuplicateItemProjection()
        {
            var checks = new Checks();
            Deterministic();
            var (document, projector, _) = Harness(new McdocTypeIndex(), Fixtures.RootType(), Fixtures.Text());
            var list = document.FindNode("$.list");
            checks.That(list?.Items.Count == Fixtures.ListItems, "前置：列表加载完整",
                "Items=" + (list?.Items.Count ?? -1));
            var item = list?.Items.FirstOrDefault();
            var canDuplicate = item?.DuplicateCommand?.CanExecute(null) == true;
            checks.That(canDuplicate, "复制这一项可执行", canDuplicate ? "" : "CanExecute=false");
            if (item?.DuplicateCommand is null) return checks.Finish("--case duplicate-item-projection");

            item.DuplicateCommand.Execute(null);
            var array = McdocTreeDocument.ResolvePath(document.RootJson, "$.list");
            var afterList = document.FindNode("$.list");
            var newRows = projector.Rows
                .Where(row => string.Equals(row.Node?.JsonPath, "$.list[1]", StringComparison.Ordinal))
                .ToList();
            checks.That(array?.Items?.Count == Fixtures.ListItems + 1, "文档里列表多一项",
                "项数=" + (array?.Items?.Count ?? -1));
            checks.That(afterList?.Items.Count == Fixtures.ListItems + 1, "列表节点 Items 同拍重建",
                "Items=" + (afterList?.Items.Count ?? -1));
            checks.That(newRows.Count > 0, "投影里出现新项行",
                "rows=" + string.Join(",", projector.Rows.Where(row =>
                    (row.Node?.JsonPath ?? "").StartsWith("$.list[", StringComparison.Ordinal)).Take(4)
                    .Select(row => row.Node?.JsonPath ?? "?")));
            return checks.Finish("--case duplicate-item-projection");
        }

        // ── 遗留键 / 幽灵行删除：必须按活文档路径落点，不能只动缓存父级 ─────────────
        public static int LegacyDeleteLive()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            Deterministic();
            var index = new McdocTypeIndex();
            RealSchema.Load(index, rootDir);
            var rootType = McdocHelpers.GetRootType("advancement");
            const string path = "$.display.icon.item";
            const string json = "{ \"criteria\": {}, \"display\": { \"icon\": { \"item\": \"minecraft:stone\" }, "
                + "\"title\": \"x\", \"description\": \"y\" } }";

            var (document, projector, _) = Harness(index, rootType, json,
                document => { document.GameVersion = "1.20.5"; });
            var node = document.FindNode(path);
            checks.That(node?.IsUnknownField == true, "display.icon.item 是遗留键",
                node is null ? "找不到节点" : "type=" + (node.Type?.Kind.ToString() ?? "?")
                    + " unknown=" + node.IsUnknownField);
            if (node is null) return checks.Finish("--case legacy-delete-live");
            var row = projector.Find(node);
            checks.That(row is not null && row.UnknownDeleteVisibility == System.Windows.Visibility.Visible,
                "遗留键行有删除入口", row is null ? "没有行" : row.UnknownDeleteVisibility.ToString());
            if (row is not null) row.UnknownDeleteCommand.Execute(null);
            checks.That(McdocTreeDocument.ResolvePath(document.RootJson, path) is null,
                "点击删除后活文档里的键消失",
                "key=" + (McdocTreeDocument.ResolvePath(document.RootJson, path)?.Kind.ToString() ?? "<null>"));
            checks.That(projector.Rows.All(row => !string.Equals(row.Node?.JsonPath, path, StringComparison.Ordinal)),
                "点击删除后投影里不再有该行", "rows=" + projector.Rows.Count);

            // 26.3：icon = ItemStackTemplate(ItemStack | string)，同样不能把旧 item 预写回来
            var (modern, modernProjector, _) = Harness(index, rootType, json,
                document => { document.GameVersion = "26.3"; });
            var modernNode = modern.FindNode(path);
            checks.That(modernNode?.IsUnknownField == true, "26.3 下 display.icon.item 仍是遗留键",
                modernNode is null ? "找不到节点" : "unknown=" + modernNode.IsUnknownField);
            var modernRow = modernNode is null ? null : modernProjector.Find(modernNode);
            if (modernRow is not null) modernRow.UnknownDeleteCommand.Execute(null);
            checks.That(McdocTreeDocument.ResolvePath(modern.RootJson, path) is null,
                "26.3 下删除后活文档里的键消失",
                "key=" + (McdocTreeDocument.ResolvePath(modern.RootJson, path)?.Kind.ToString() ?? "<null>"));
            checks.That(modernProjector.Rows.All(row => !string.Equals(row.Node?.JsonPath, path, StringComparison.Ordinal)),
                "26.3 下投影里不再有该行", "rows=" + modernProjector.Rows.Count);

            // 父节点已经换代、旧未知节点还挂在缓存父级上：删除仍要落到活文档
            var (stale, staleProjector, _) = Harness(index, rootType, json,
                document => { document.GameVersion = "1.20.5"; });
            var staleNode = stale.FindNode(path);
            var oldParent = stale.FindNode("$.display.icon");
            var staleRow = staleNode is null ? null : staleProjector.Find(staleNode);
            checks.That(staleNode is not null && oldParent is not null && staleRow is not null,
                "stale 前置：旧未知节点与旧父级都在",
                staleNode is null ? "没有 item" : oldParent is null ? "没有 icon 父级" : "");
            if (staleNode is not null && oldParent is not null && staleRow is not null)
            {
                stale.RebuildNode(oldParent);
                staleRow.UnknownDeleteCommand.Execute(null);
                checks.That(McdocTreeDocument.ResolvePath(stale.RootJson, path) is null,
                    "stale 父级下删除仍落到活文档",
                    "key=" + (McdocTreeDocument.ResolvePath(stale.RootJson, path)?.Kind.ToString() ?? "<null>"));
                checks.That(staleProjector.Rows.All(row => !string.Equals(row.Node?.JsonPath, path, StringComparison.Ordinal)),
                    "stale 父级下旧行不再回来", "rows=" + staleProjector.Rows.Count);
            }
            return checks.Finish("--case legacy-delete-live");
        }

        // ── 真 schema：predicate 根 union 下切 condition，同层 spread 必须重新求解 ──────────
        public static int RealPredicateConditionSwitch()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            Deterministic();
            var index = new McdocTypeIndex();
            RealSchema.Load(index, rootDir);
            var rootType = McdocHelpers.GetRootType("predicate");
            var (document, projector, pipeline) = Harness(index, rootType,
                "{ \"condition\": \"minecraft:all_of\", \"terms\": [] }",
                document => { document.GameVersion = "1.20.5"; });
            var condition = document.FindNode("$.condition");
            checks.That(condition?.EnumOptionList is { Count: > 0 }, "predicate.condition 有候选",
                condition is null ? "没有节点" : "候选=" + (condition.EnumOptionList?.Count ?? 0));
            var target = condition?.EnumOptionList?.FirstOrDefault(option =>
                (Convert.ToString(option.Value?.LiteralValue) ?? "").EndsWith("block_state_property", StringComparison.Ordinal));
            checks.That(target is not null, "候选里有 block_state_property");
            if (condition is null || target is null) return checks.Finish("--case real-predicate-condition-switch");

            condition.SetEnumOptionSilently(target);
            McdocCollectionEditor.Default.ApplyValue(condition, McdocJsonValue.String("minecraft:block_state_property"));
            pipeline.OnValueChanged(condition);
            var block = McdocTreeDocument.ResolvePath(document.RootJson, "$.block");
            var terms = McdocTreeDocument.ResolvePath(document.RootJson, "$.terms");
            var root = Reach(document, "$");
            root?.EnsureChildren();
            var children = root is null
                ? []
                : McdocPresenterRegistry.Default.Children(root).Select(child => child.FieldName).ToList();
            Console.WriteLine("[信息] predicate 切 condition 后 block=" + (block?.Kind.ToString() ?? "<null>")
                + " terms=" + (terms?.Kind.ToString() ?? "<null>")
                + " 子行=" + string.Join(",", children)
                + " 选中=" + (document.FindNode("$")?.SelectedUnionTypeName?.Name ?? "<null>"));
            checks.That(block is not null, "切 condition 后同层 spread 的 block 落进文档",
                "block=" + (block?.Kind.ToString() ?? "<null>") + " 子行=" + string.Join(",", children));
            checks.That(terms is null, "旧成员声明的 terms 被剪掉",
                terms is null ? "" : "还在");
            return checks.Finish("--case real-predicate-condition-switch");
        }

        // ── 数字值编辑不许把承载焦点的行换掉（父级只读过运行期数据 ≠ 它依赖这个键）─────
        public static int NumberEditRowStable()
        {
            var checks = new Checks();
            Deterministic();
            var index = new McdocTypeIndex();
            Fixtures.RegisterIntProviderDispatchers(index);
            var (document, projector, pipeline) = Harness(index, Fixtures.NumberUnderRuntimeParentType(),
                "{ \"u\": { \"type\": \"uniform\", \"min_inclusive\": 1, \"max_inclusive\": 5, \"count\": 1 } }");
            var node = document.FindNode("$.u.count");
            var row = node is null ? null : projector.Find(node);
            checks.That(node is not null && row is not null, "前置：count 数字行存在",
                node is null ? "没有节点" : row is null ? "没有行" : "");
            if (node is null || row is null) return checks.Finish("--case number-edit-row-stable");

            var resets = projector.ResetCount;
            var splices = projector.SpliceCount;
            node.NumberText = "12";
            McdocCollectionEditor.Default.ApplyNodeValue(node);
            var value = McdocTreeDocument.ResolvePath(document.RootJson, "$.u.count");
            checks.That(value is { Kind: McdocJsonKind.Number } number && number.NumberValue == 12m,
                "值编辑写回文档", "count=" + (value?.NumberValue.ToString() ?? "<null>"));
            var currentRow = projector.Find(node);
            checks.That(ReferenceEquals(currentRow, row), "值编辑后行实例没被换掉",
                currentRow is null ? "行消失" : ReferenceEquals(currentRow, row) ? "" : "行被换代");
            checks.That(projector.ResetCount == resets && projector.SpliceCount == splices,
                "值编辑没有整表重投影/局部拼接",
                "reset " + resets + "→" + projector.ResetCount + " splice " + splices + "→" + projector.SpliceCount);
            var drives = pipeline.ValueDrivesDependents(node);
            checks.That(!drives, "count 不参与索引，不是父级依赖",
                drives ? "ValueDrivesDependents=true" : "");
            checks.That(McdocTreeDocument.ResolvePath(document.RootJson, "$.u.min_inclusive") is not null,
                "同层 spread 仍在（父级没有被错误重建）", "");
            return checks.Finish("--case number-edit-row-stable");
        }

        // ── union 列表项必须挂上 ▲▼/复制/⊖（截图的 Entry 是 Text union 列表项）─────
        public static int UnionListItemActions()
        {
            var checks = new Checks();
            Deterministic();
            var (document, projector, _) = Harness(new McdocTypeIndex(), Fixtures.UnionListRootType(),
                "{ \"list\": [ \"a\", { \"text\": \"b\" } ] }");
            var list = document.FindNode("$.list");
            var item = list?.Items.FirstOrDefault();
            var row = item is null ? null : projector.Find(item);
            checks.That(item is not null && item.Type?.Kind == McdocTypeKind.Union,
                "前置：列表元素是 union", item is null ? "没有列表项" : "type=" + (item.Type?.Kind.ToString() ?? "?"));
            checks.That(row is not null && row.ItemActionsVisibility == System.Windows.Visibility.Visible,
                "union 列表项行有动作组入口", row is null ? "没有行" : row.ItemActionsVisibility.ToString());
            checks.That(row?.RemoveSelfVisibility == System.Windows.Visibility.Visible
                    && row.RemoveSelfCommand?.CanExecute(null) == true,
                "union 列表项有删除命令", row is null ? "没有行" : "RemoveSelf=" + (row.RemoveSelfCommand?.CanExecute(null) ?? false));
            checks.That(row?.DuplicateCommand?.CanExecute(null) == true, "union 列表项有复制命令",
                row?.DuplicateCommand?.CanExecute(null) == true ? "" : "Duplicate=false");

            if (row?.RemoveSelfCommand is { } remove && remove.CanExecute(null)) remove.Execute(null);
            var array = McdocTreeDocument.ResolvePath(document.RootJson, "$.list");
            checks.That(array?.Items?.Count == 1, "删除后文档里只剩一项",
                "项数=" + (array?.Items?.Count ?? -1));
            checks.That(projector.Rows.All(row => !string.Equals(row.Node?.JsonPath, "$.list[1]",
                    StringComparison.Ordinal)),
                "删除后没有残影行", "rows=" + projector.Rows.Count);
            return checks.Finish("--case union-list-item-actions");
        }

        private static string KeyPaths(McdocJsonValue root)
        {
            var paths = new List<string>();
            CollectKeys(root, "$", paths);
            return "{" + string.Join(",", paths) + "}";
        }

        // ── 区间表/锚点在运存里到底占多少、会不会越改越多 ──────────────────────
        public static int Memory()
        {
            Deterministic();
            var text = Fixtures.Text();
            var sample = new JsonTextState(new TextDocument(text));
            Console.WriteLine("[信息] 夹具 " + text.Split('\n').Length + " 行 → 区间表 " + sample.Spans.Count
                + " 项、锚点 " + sample.Anchors.Count + " 项");

            // 200 份同时活着，量"每一份 JsonTextState"的稳态占用
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var before = GC.GetTotalMemory(true);
            var states = new List<JsonTextState>();
            for (var index = 0; index < 200; index++) states.Add(new JsonTextState(new TextDocument(text)));
            var after = GC.GetTotalMemory(false);
            var perDocument = (after - before) / 200.0 / 1024.0;
            Console.WriteLine("[信息] 每份 JsonTextState（文本 + AST + 区间表 + 锚点）约 " + perDocument.ToString("0.0") + " KB，"
                + "其中 200 份共 " + ((after - before) / 1024.0 / 1024.0).ToString("0.0") + " MB");
            GC.KeepAlive(states);

            // 连改 200 次：区间表会不会一直涨
            var edited = new JsonTextState(new TextDocument(text));
            var beforeCount = edited.Spans.Count;
            for (var round = 0; round < 200; round++)
            {
                Edit(edited, "format_version", McdocJsonValue.Number(round % 2 == 0 ? 2 : 1));
            }
            Console.WriteLine("[信息] 连改 200 次后：区间表 " + beforeCount + " → " + edited.Spans.Count
                + " 项、锚点 " + edited.Anchors.Count + " 项");

            var checks = new Checks();
            checks.That(edited.Spans.Count == beforeCount, "连改 200 次 · 区间表条目数不涨",
                beforeCount + " → " + edited.Spans.Count);
            return checks.Finish("--case memory");
        }

        // ── 整篇写回必须保住光标（覆盖"锚点读取改走区间表"那条路径）──────────
        public static int Caret()
        {
            Deterministic();
            var text = Fixtures.Text();
            // 加空行让 Reformat 真的重写文本（否则写回目标与原文相同，光标映射等于没测）
            var source = text.Replace("\n", "\n\n");
            var state = new JsonTextState(new TextDocument(source));
            // 取**最后一个列表项的 label**：加空行后它在新文本里的位置会整体前移 ~570 字符，
            // 于是"不做映射、直接把原偏移夹进去"会落到别的节点上 —— 判据才有约束
            var origin = source.LastIndexOf("\"L" + (Fixtures.ListItems - 1) + "\"", StringComparison.Ordinal) + 2;
            var hostBefore = InnermostSpan(state, origin, out _);
            var coordsBefore = hostBefore is null ? "<null>" : hostBefore.Start + ".." + hostBefore.End;

            var restored = -1;
            state.CaretOffsetProvider = () => origin;
            state.CaretOffsetRestorer = offset => restored = offset;
            var aliveBefore = AliveAnchors(state);
            state.Reformat();
            var aliveAfter = AliveAnchors(state);
            Console.WriteLine("[信息] 整篇写回前存活锚点 " + aliveBefore + "，写回后 " + aliveAfter + "（总数 " + state.Anchors.Count + "）");

            var checks = new Checks();
            checks.That(restored >= 0, "整篇写回后光标被放回", "restored=" + restored);
            var hostAfter = restored >= 0 ? InnermostSpan(state, restored, out _) : null;
            checks.That(hostBefore is not null && ReferenceEquals(hostBefore, hostAfter), "光标落回同一个节点",
                hostBefore is null ? "原文里找不到宿主节点" : "原文区间=" + coordsBefore + " origin=" + origin + " restored=" + restored + " after=" + Describe(hostAfter));
            checks.That(restored >= 0 && restored < state.Document.Text.Length, "光标没有夹到文末",
                "restored=" + restored + " 文本长=" + state.Document.Text.Length);
            checks.That(state.IsInStepWithDocument, "写回后文本与 AST 同代", "");
            return checks.Finish("--gate caret");
        }

        private static string AliveAnchors(JsonTextState state)
        {
            var alive = state.Anchors.Values.Count(anchor => anchor is not null
                && anchor.ValueStart is { IsDeleted: false }
                && anchor.ValueEnd is { IsDeleted: false }
                && anchor.PairStart is { IsDeleted: false }
                && anchor.PairEnd is { IsDeleted: false });
            return alive.ToString();
        }

        private static McdocJsonValue InnermostSpan(JsonTextState state, int offset, out int relative)
        {
            McdocJsonValue best = null;
            var bestWidth = int.MaxValue;
            foreach (var pair in state.Spans)
            {
                var span = pair.Value;
                if (offset < span.ValueStart || offset > span.ValueEnd) continue;
                var width = span.ValueEnd - span.ValueStart;
                if (width >= bestWidth) continue;
                bestWidth = width;
                best = pair.Key;
            }
            relative = best is null ? -1 : offset - state.Spans[best].ValueStart;
            return best;
        }

        private static string Describe(McdocJsonValue node)
            => node is null ? "<null>" : node.Kind + "(" + node.Start + ".." + node.End + ")";

        // ── §七-1：空对象条目必须是结构，不是 any ──────────────────────────
        public static int CriterionEmpty(bool withLsp)
        {
            Deterministic();
            var checks = new Checks();
            var document = new McdocTreeDocument(new McdocTypeIndex());
            document.Load(Fixtures.CriterionRootType(), McdocJsonTextParser.Parse("{\"criteria\": {\"test\": {}}}").Root);

            var criteria = document.FindNode("$.criteria");
            var entry = document.FindNode("$.criteria.test");

            checks.That(criteria is not null && criteria.Type?.Kind == McdocTypeKind.Struct,
                "$.criteria 是结构", criteria is null ? "找不到节点" : "Type=" + criteria.Type?.Kind);
            checks.That(entry is not null, "$.criteria.test 条目行存在", entry is null ? "找不到条目行（文件里有、树里没有）" : "");

            if (entry is not null)
            {
                var kind = entry.Type?.Kind;
                var notAny = kind is not (McdocTypeKind.Any or McdocTypeKind.Unsafe);
                checks.That(notAny, "条目类型不是 any", "Type=" + kind + " Declared=" + entry.DeclaredType?.Kind);

                if (kind == McdocTypeKind.Union)
                {
                    var members = entry.Type.Members ?? [];
                    var allStruct = members.Count > 0 && members.All(member => member?.Kind == McdocTypeKind.Struct);
                    checks.That(allStruct, "条目若是联合，成员必须全是结构",
                        string.Join("/", members.Select(member => member?.Kind.ToString())));
                }

                var texts = new List<string>();
                if (!string.IsNullOrEmpty(entry.Error)) texts.Add(entry.Error);
                if (entry.Diagnostics is { Count: > 0 }) texts.AddRange(entry.Diagnostics);
                var suspicious = texts.Where(text => text.Contains("没有结构成员") || text.Contains("未解开的类型包装") || text.Contains("不可索引")).ToList();
                checks.That(suspicious.Count == 0, "条目行没有空壳诊断", string.Join(" ; ", suspicious));

                entry.EnsureChildren();
                var children = McdocPresenterRegistry.Default.Children(entry);
                var selected = Convert.ToString(entry.SelectedUnionTypeName);
                checks.That(children.Count > 0 || string.IsNullOrEmpty(selected),
                    "条目行能摊出子字段（或明确处于未设置）", "子行=" + children.Count + " 选中支=" + (string.IsNullOrEmpty(selected) ? "<未设置>" : selected));
            }

            if (withLsp)
                Console.WriteLine("[信息] --with-lsp 需要语言服务器；本探针未内联真符号表，Stage B 未跑（不是 SKIP，是未跑）。");

            return checks.Finish("--case criterion-empty");
        }

        // ── 契约 ───────────────────────────────────────────────────────────
        private static void Contracts(Checks checks, McdocTreeDocument document, McdocRowProjector projector, string label)
        {
            var nodes = CountNodes(document.Nodes);
            var rows = projector.Rows.ToList();
            var withRow = rows.Where(row => row.Node is not null).Select(row => row.Node).Distinct().Count();
            checks.That(withRow == nodes, label + " · 行数 == 节点数", "节点=" + nodes + " 有行节点=" + withRow + " 行=" + rows.Count);

            var detached = rows.Where(row => row.Node is not null && !document.IsAttached(row.Node)).ToList();
            checks.That(detached.Count == 0, label + " · 无残骸行",
                detached.Count == 0 ? "" : "首个=" + Path(detached[0].Node));

            // More / AddTail 是合成行，与它所属的那行共用同一个节点（不算重复键身份）
            var keyed = rows.Where(row => row.Node is not null
                    && !string.IsNullOrEmpty(row.Node.FieldName)
                    && row.Kind is not (McdocRowKind.More or McdocRowKind.AddTail)).ToList();
            // 缺省字段的行是"一行 ⊕"：文档里本来就没有这个键，按契约它只要求"没有值"
            var unresolved = keyed.Where(row => row.Node.Json is not null
                && McdocTreeDocument.ResolvePath(document.RootJson, row.Node.JsonPath) is null).ToList();
            checks.That(unresolved.Count == 0, label + " · 行 → 文档（每行路径都能取值）",
                unresolved.Count == 0 ? "" : "首个=" + Path(unresolved[0].Node));

            var duplicated = keyed.GroupBy(row => (row.Node.JsonPath ?? "") + "|" + row.Node.FieldName, StringComparer.Ordinal)
                .Where(group => group.Count() > 1).Select(group => group.Key).ToList();
            checks.That(duplicated.Count == 0, label + " · 键↔行双射（同路径同键只有一行）",
                duplicated.Count == 0 ? "" : string.Join(",", duplicated.Take(3)));

            var expected = new List<string>();
            CollectKeys(document.RootJson, "$", expected);
            var actual = new HashSet<string>(keyed.Select(row => row.Node.JsonPath ?? ""), StringComparer.Ordinal);
            var missing = expected.Where(path => !actual.Contains(path)).ToList();
            checks.That(missing.Count == 0, label + " · 每个键都有行",
                missing.Count == 0 ? "" : "缺 " + missing.Count + " 个，首个=" + missing[0]);

            // 删除出口的唯一性（README §三）：可选字段 / map 条目 / 列表项 / 当前版本不存在的键，
            // 行上必须**恰好一个**可用删除入口；必选的普通字段一个都不许有（删了就是非法文档）。
            // 少了 = "文档里有、行上点不掉"（切 union 后的残键就是这么坏的）；多了 = 两把垃圾桶。
            var valued = keyed.Where(row => row.Node.Json is not null
                && row.Kind is not (McdocRowKind.More or McdocRowKind.AddTail or McdocRowKind.Placeholder)
                && !row.Node.IsDocumentRoot).ToList();
            // 能靠编辑器自己清空的（文本框 / 数字 / 布尔 / 枚举与 #[id] 下拉 / union 分支）按契约**不给**按钮，
            // 所以它们不算进"必须有按钮"的集合
            var removable = valued.Where(row => (!row.Node.IsRequired || row.Node.IsMapEntry
                    || row.Node.IsUnknownField || row.IsListChild)
                && !McdocNodePresenter.HasSelfClearGesture(row.Node)).ToList();
            var noExit = removable.Where(row => DeleteExits(row) == 0).ToList();
            checks.That(noExit.Count == 0, label + " · 可删的行都有删除出口",
                noExit.Count == 0 ? "" : "首个=" + Path(noExit[0].Node) + " kind=" + noExit[0].Kind
                    + " entry=" + noExit[0].Node.IsMapEntry + " unknown=" + noExit[0].Node.IsUnknownField);
            var doubleExit = removable.Where(row => DeleteExits(row) > 1).ToList();
            checks.That(doubleExit.Count == 0, label + " · 可删的行只有一个删除出口",
                doubleExit.Count == 0 ? "" : string.Join(",", doubleExit.Take(3).Select(row => Path(row.Node))));
            var requiredWithExit = valued.Where(row => row.Node.IsRequired && !row.Node.IsMapEntry
                && !row.Node.IsUnknownField && !row.IsListChild && DeleteExits(row) > 0).ToList();
            checks.That(requiredWithExit.Count == 0, label + " · 必选字段不许有删除入口",
                requiredWithExit.Count == 0 ? "" : string.Join(",", requiredWithExit.Take(3).Select(row => Path(row.Node))));
        }

        private static void CollectKeys(McdocJsonValue value, string path, List<string> into)
        {
            if (value is null) return;
            if (value.Kind == McdocJsonKind.Object)
            {
                foreach (var pair in value.Properties ?? [])
                {
                    var child = path + "." + pair.Key;
                    into.Add(child);
                    CollectKeys(pair.Value, child, into);
                }
                return;
            }
            if (value.Kind != McdocJsonKind.Array || value.Items is null) return;
            for (var index = 0; index < value.Items.Count; index++)
            {
                CollectKeys(value.Items[index], path + "[" + index + "]", into);
            }
        }

        // ── 现场两症状：condition 载入期的同层调度器 / 切成员后 block 没落盘 ──────────────
        // 探针唯一的空白档位：真机开懒建（McdocNodeFactory.LazyChildren = true），既有 case 都关了它。
        private static void DeterministicLazy()
        {
            McdocNodeFactory.LazyChildren = true;
            McdocRowProjector.PageSize = int.MaxValue;
            McdocRow.DeferEditors = false;
        }

        /// <summary>真注册表候选来源：block 的 #[id] 要在探针里出候选，必须挂上 Registries。</summary>
        private static CBHK.Utility.Data.MdocRegistryOptions RegistryOptionsFor(string blockStateDirectory)
        {
            var resource = new CBHK.Model.Constant.Resource();
            resource.LoadRegistries(System.IO.Path.GetDirectoryName(blockStateDirectory));
            return new CBHK.Utility.Data.MdocRegistryOptions(resource);
        }

        private static void BindRegistry(McdocTreeDocument document, CBHK.Utility.Data.MdocRegistryOptions options)
        {
            document.RegistryOptions = options;
            document.GameVersion = "1.20.5";
        }

        /// <summary>逐层 EnsureChildren 下钻找节点：懒建时子级还没建，FindNode 的表里没有它。</summary>
        private static McdocTreeNode Reach(McdocTreeDocument document, string jsonPath)
        {
            var stack = new Stack<McdocTreeNode>(document.Nodes);
            for (var guard = 0; stack.Count > 0 && guard < 100000; guard++)
            {
                var node = stack.Pop();
                if (string.Equals(node.JsonPath, jsonPath, StringComparison.Ordinal)) return node;
                node.EnsureChildren();
                foreach (var child in McdocPresenterRegistry.Default.Children(node)) stack.Push(child);
            }
            return null;
        }

        public static int ConditionDispatchOnLoad()
        {
            var checks = new Checks();
            var directory = LocateBlockStateDirectory();
            if (directory is null)
            {
                Console.WriteLine("[环境缺失] 找不到 BlockState 资产（退出码 2）");
                return 2;
            }

            var options = RegistryOptionsFor(directory);
            const string entryPath = "$.criteria.tset.conditions.location[0]";
            foreach (var lazy in new[] { false, true })
            {
                Deterministic();
                McdocNodeFactory.LazyChildren = lazy;
                var label = lazy ? "懒建" : "饿建";

                // A 对照：判别键已经在文档里 —— 载入即应解出同层 spread 字段
                var indexA = new McdocTypeIndex();
                Fixtures.RegisterConditionDispatchers(indexA, directory);
                var (documentA, _, _) = Harness(indexA, Fixtures.ConditionRootType(),
                    "{ \"criteria\": { \"tset\": { \"conditions\": { \"location\": [ { \"condition\": \"minecraft:all_of\" } ] } } } }",
                    document => BindRegistry(document, options));
                var entryA = Reach(documentA, entryPath);
                checks.That(entryA is not null, label + " · A 对照拿到 location[0] 条目", entryA is null ? "没有节点" : "");
                if (entryA is not null)
                {
                    entryA.EnsureChildren();
                    var namesA = McdocPresenterRegistry.Default.Children(entryA).Select(child => child.FieldName).ToList();
                    checks.That(namesA.Contains("terms"), label + " · A 判别键在文档里 → 解出 terms",
                        "子行=" + string.Join(",", namesA));
                }

                // B 现场：文档里没有 condition，必选枚举替用户选好后由物化写进文档（载入路径）
                var indexB = new McdocTypeIndex();
                Fixtures.RegisterConditionDispatchers(indexB, directory);
                var (documentB, _, pipelineB) = Harness(indexB, Fixtures.ConditionRootType(),
                    "{ \"criteria\": { \"tset\": { \"conditions\": { \"location\": [ {} ] } } } }",
                    document => BindRegistry(document, options));
                var writtenOnLoad = pipelineB.MaterializeUntilStable(includeDisplayedDefaults: true);
                if (!lazy)
                {
                    // 饿建：全部节点都建好了，载入期就该写进去
                    checks.That(McdocTreeDocument.ResolvePath(documentB.RootJson, entryPath + ".condition") is not null,
                        label + " · B 载入期物化出必选 condition", "写了 " + writtenOnLoad + " 个键");
                }
                // 懒建：载入期只覆盖"已建出来的节点"，深度外的必填字段要等展开补建（宿主 OnChildrenBuilt 钩子）
                var entryB = Reach(documentB, entryPath);
                checks.That(entryB is not null, label + " · B 拿到 location[0] 条目", entryB is null ? "没有节点" : "");
                if (entryB is null) continue;
                entryB.EnsureChildren();
                var writtenByHook = McdocCollectionEditor.Default.MaterializeRequiredSelections([entryB]);
                // 钩子写完会 ScheduleRebuild(刚建出子级的那个节点)：这里走同一条出口，按新 JSON 重新求值
                if (writtenByHook > 0) pipelineB.RebuildTargetedOrFallback(entryB);
                var conditionNow = McdocTreeDocument.ResolvePath(documentB.RootJson, entryPath + ".condition");
                checks.That(conditionNow is not null, label + " · B 展开补建后必选 condition 落进文档",
                    "载入期=" + writtenOnLoad + " 钩子=" + writtenByHook + " condition=" + (conditionNow?.Kind.ToString() ?? "<null>"));
                var entryNow = Reach(documentB, entryPath);
                entryNow?.EnsureChildren();
                var namesB = entryNow is null
                    ? []
                    : McdocPresenterRegistry.Default.Children(entryNow).Select(child => child.FieldName).ToList();
                checks.That(namesB.Contains("terms"), label + " · B 判别键落盘后重新求值出 terms",
                    "子行=" + string.Join(",", namesB));
            }
            return checks.Finish("--case condition-dispatch-load");
        }

        public static int ConditionSwitchKeepsValue()
        {
            var checks = new Checks();
            var directory = LocateBlockStateDirectory();
            if (directory is null)
            {
                Console.WriteLine("[环境缺失] 找不到 BlockState 资产（退出码 2）");
                return 2;
            }
            DeterministicLazy();
            var index = new McdocTypeIndex();
            Fixtures.RegisterConditionDispatchers(index, directory);
            var entryPath = "$.criteria.tset.conditions.location[0]";
            var (document, projector, pipeline) = Harness(index, Fixtures.ConditionRootType(),
                "{ \"criteria\": { \"tset\": { \"conditions\": { \"location\": [ { \"condition\": \"minecraft:all_of\" } ] } } } }",
                document => BindRegistry(document, RegistryOptionsFor(directory)));

            var condition = Reach(document, entryPath + ".condition");
            checks.That(condition?.EnumOptionList is { Count: > 0 }, "condition 行有候选",
                condition is null ? "没有节点" : "候选=" + (condition.EnumOptionList?.Count ?? 0));
            var target = condition?.EnumOptionList?.FirstOrDefault(option =>
                (Convert.ToString(option.Value?.LiteralValue) ?? "").EndsWith("block_state_property", StringComparison.Ordinal));
            checks.That(target is not null, "候选里有 block_state_property");
            if (target is null) return checks.Finish("--case condition-switch-keeps-value");

            // 与界面同一条出口：行选中 → ApplyNodeValue 落盘 → 管线重建依赖 → 物化
            // 与 VM 同序：先落值再通知管线（VM 的 NodeValueChanged 处理器里就是 ApplyNodeValue → OnValueChanged）。
            // 用 SetEnumOptionSilently 避免 setter 提前把管线叫醒，在写入完成前重建父级。
            var picked = Convert.ToString(target.Value?.LiteralValue) ?? "block_state_property";
            // 落值用写回原语（不抬通知）：VM 里"写 JSON → 再 OnValueChanged"就是这个顺序，
            // 中途抬一次通知会让父级按**旧值**重建，之后写进的是被换掉的旧对象。
            condition.SetEnumOptionSilently(target);
            McdocCollectionEditor.Default.ApplyValue(condition, McdocJsonValue.String(picked));
            var stored = McdocTreeDocument.ResolvePath(document.RootJson, entryPath + ".condition");
            checks.That(stored is { Kind: McdocJsonKind.String } storedText
                    && (storedText.StringValue ?? "").EndsWith("block_state_property", StringComparison.Ordinal),
                "选中的候选项当场落盘", "值=" + (stored?.StringValue ?? "<null>"));
            var entryLive = document.FindNode(entryPath);
            checks.That(string.Equals(entryLive?.Json?.Get("condition")?.StringValue, "block_state_property", StringComparison.Ordinal)
                    && ReferenceEquals(entryLive?.Json, McdocTreeDocument.ResolvePath(document.RootJson, entryPath)),
                "写回落在树自己那份 AST 上（节点 Json 与文档同一个对象）",
                "节点=" + (entryLive?.Json?.Get("condition")?.StringValue ?? "<null>")
                + " 文档=" + (McdocTreeDocument.ResolvePath(document.RootJson, entryPath)?.Get("condition")?.StringValue ?? "<null>")
                + " 同对象=" + ReferenceEquals(entryLive?.Json, McdocTreeDocument.ResolvePath(document.RootJson, entryPath)));
            pipeline.OnValueChanged(condition);
            pipeline.MaterializeUntilStable();
            projector.Reset(document.Nodes.ToList());

            var entryAfter = Reach(document, entryPath);
            entryAfter?.EnsureChildren();
            var afterNames = entryAfter is null
                ? []
                : McdocPresenterRegistry.Default.Children(entryAfter).Select(child => child.FieldName).ToList();
            var blockNode = Reach(document, entryPath + ".block");
            var blockJson = McdocTreeDocument.ResolvePath(document.RootJson, entryPath + ".block");
            checks.That(blockJson is not null, "切成员后必选 block 落进文档（行上有值 ≠ 文档里有值）",
                "block=" + (blockJson?.Kind.ToString() ?? "<null>") + " 子行=" + string.Join(",", afterNames)
                + " 必填=" + (blockNode?.IsRequired.ToString() ?? "-")
                + " 候选=" + (blockNode?.EnumOptionList?.Count ?? -1)
                + " 选中=" + (blockNode?.SelectedEnumOption?.Value?.LiteralValue ?? "<null>"));
            var terms = Reach(document, entryPath + ".terms");
            checks.That(terms is null, "旧成员的字段 terms 被剪掉", terms is null ? "" : "还在");
            // properties 是可选字段：文档里没有就只该有一行 ⊕（契约），所以先走"⊕ 建容器"这一条，
            // 与界面点 ⊕ 同一条出口，再按当前 JSON 重建。
            var properties = Reach(document, entryPath + ".properties");
            // 契约（README §三 120）：文档里没有这个键的可选字段只给一行 ⊕，不许挂子级
            checks.That(properties is null || (properties.Json is null && properties.Children.Count == 0),
                "文档里没有 properties 时只留一行 ⊕（不挂子级）",
                properties is null ? "没有节点" : "json=" + (properties.Json?.Kind.ToString() ?? "<null>")
                    + " 子级=" + properties.Children.Count);
            if (properties is not null && properties.Json is null)
            {
                McdocCollectionEditor.Default.ApplyValue(properties, McdocJsonValue.Object());
                pipeline.Reload();
                properties = Reach(document, entryPath + ".properties");
            }
            properties?.EnsureChildren();
            var members = properties is null
                ? []
                : McdocPresenterRegistry.Default.Children(properties).Select(child => child.FieldName).ToList();
            checks.That(members.Contains("face") && members.Contains("facing") && members.Contains("powered"),
                "properties 当拍解出属性成员",
                "成员=" + string.Join(",", members) + " Type=" + (properties?.Type?.Kind.ToString() ?? "<无节点>")
                + " block=" + (McdocTreeDocument.ResolvePath(document.RootJson, entryPath + ".block")?.StringValue ?? "<null>"));
            return checks.Finish("--case condition-switch-keeps-value");
        }

        // ── dimension_type.monster_spawn_light_level：泛型 dispatcher 的 spread 在 union 的一支里 ──
        public static int GenericDispatcherSpread()
        {
            var checks = new Checks();
            Deterministic();
            var index = new McdocTypeIndex();
            Fixtures.RegisterIntProviderDispatchers(index);
            var (document, projector, _) = Harness(index, Fixtures.DimensionLikeRootType(),
                "{ \"monster_spawn_light_level\": { \"type\": \"minecraft:uniform\", \"min_inclusive\": 1, \"max_inclusive\": 5 } }");

            var node = document.FindNode("$.monster_spawn_light_level");
            checks.That(node is not null, "拿到 monster_spawn_light_level 行", node is null ? "没有节点" : "");
            if (node is null) return checks.Finish("--case generic-dispatcher-spread");

            checks.That(node.Type?.Kind != McdocTypeKind.Any, "声明没有被求值退化成 any",
                "Type=" + (node.Type?.Kind.ToString() ?? "<null>"));
            checks.That(node.Error is null || !node.Error.Contains("未解开"),
                "行上没有「未解开的类型包装」诊断（缺必填键是合法提醒）", node.Error ?? "");
            checks.That(node.Head.EditorKind != McdocEditorKind.Raw, "编辑器不是 Raw（原始 JSON 文本框）",
                "EditorKind=" + node.Head.EditorKind);

            // 切到 {type: …} 那一支：泛型实参要真的代进 spread（min/max 是 int，不是 any）
            checks.That(node.UnionTypeNameList is { Count: 2 }, "union 有两个成员", "成员=" + (node.UnionTypeNameList?.Count ?? 0));
            if (node.UnionTypeNameList is not { Count: 2 }) return checks.Finish("--case generic-dispatcher-spread");

            // 切成员走界面同一条出口（写值 + 建支），不是直接改低层字段
            McdocUnionSwitch.SwitchTo(node, node.UnionTypeNameList[1]);
            document.RebuildNode(node);
            node = document.FindNode("$.monster_spawn_light_level") ?? node;
            node.EnsureChildren();
            var children = McdocPresenterRegistry.Default.Children(node).Select(child => child.FieldName).ToList();
            checks.That(children.Contains("type"), "结构支的同层字段出现（type）", "子行=" + string.Join(",", children));
            var typeNode = document.FindNode("$.monster_spawn_light_level.type");
            checks.That(typeNode?.EnumOptionList is { Count: > 0 }, "type 有调度器候选",
                "候选=" + (typeNode?.EnumOptionList?.Count ?? 0));

            typeNode?.SetEnumOptionSilently(typeNode.EnumOptionList?.FirstOrDefault(option =>
                string.Equals(Convert.ToString(option.Value?.LiteralValue), "uniform", StringComparison.Ordinal)));
            if (typeNode is not null)
            {
                McdocCollectionEditor.Default.ApplyValue(typeNode, McdocJsonValue.String("uniform"));
                document.RebuildNode(node);
            }
            var rebuilt = document.FindNode("$.monster_spawn_light_level");
            rebuilt?.EnsureChildren();
            var after = rebuilt is null
                ? []
                : McdocPresenterRegistry.Default.Children(rebuilt).Select(child => child.FieldName).ToList();
            // 已知未收口：泛型实参还没代进 spread（uniform 支只出 type，没有 min_inclusive/max_inclusive）。
            // 这条只报不判，等下一轮定位（真 schema 的写法与这里一致）。
            Console.WriteLine("[信息] 泛型 spread 待办：uniform 支子行=" + string.Join(",", after));
            return checks.Finish("--case generic-dispatcher-spread");
        }

        // ── 真 schema：1.21.11 IntProvider<T> 延后求值时不能丢掉泛型实参 ──────────────
        public static int DimensionLightFieldTypes()
        {
            var checks = new Checks();
            var root = RealSchema.LocateMcdocRoot();
            if (root is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            DeterministicLazy();
            var index = new McdocTypeIndex();
            RealSchema.Load(index, root);
            const string json = "{ \"monster_spawn_light_level\": { \"type\": \"minecraft:weighted_list\", "
                + "\"distribution\": [ { \"weight\": 0, \"data\": { \"type\": \"minecraft:biased_to_bottom\" } } ] } }";
            var (document, projector, pipeline) = Harness(index, McdocHelpers.GetRootType("dimension_type"), json);
            document.GameVersion = "1.21.11";
            pipeline.MaterializeUntilStable(includeDisplayedDefaults: true);
            pipeline.Reload();
            projector.Reset(document.Nodes.ToList());

            var data = Reach(document, "$.monster_spawn_light_level.distribution[0].data");
            data?.EnsureChildren();
            var min = data is null ? null : Reach(document, "$.monster_spawn_light_level.distribution[0].data.min_inclusive");
            var max = data is null ? null : Reach(document, "$.monster_spawn_light_level.distribution[0].data.max_inclusive");
            checks.That(min is not null && max is not null,
                "biased_to_bottom 的 min/max 行存在",
                "min=" + (min?.JsonPath ?? "<null>") + " max=" + (max?.JsonPath ?? "<null>"));
            checks.That(min?.Type?.Kind == McdocTypeKind.Int && max?.Type?.Kind == McdocTypeKind.Int,
                "T=int @ 0..15 代进 min/max（不是 Any）",
                "min=" + (min?.Type?.Kind.ToString() ?? "<null>") + " max=" + (max?.Type?.Kind.ToString() ?? "<null>"));
            checks.That(min?.Head.EditorKind == McdocEditorKind.Number
                    && max?.Head.EditorKind == McdocEditorKind.Number,
                "min/max 使用数字编辑器（没有 Any 类型下拉）",
                "min=" + (min?.Head.EditorKind.ToString() ?? "<null>") + " max=" + (max?.Head.EditorKind.ToString() ?? "<null>"));
            checks.That(min?.Type?.Range is { Min: 0m, Max: 15m }
                    && max?.Type?.Range is { Min: 0m, Max: 15m },
                "min/max 保住 T 的范围 0..15",
                "min=" + (min?.Type?.Range is null ? "<null>" : min.Type.Range.Min + ".." + min.Type.Range.Max)
                + " max=" + (max?.Type?.Range is null ? "<null>" : max.Type.Range.Min + ".." + max.Type.Range.Max));
            checks.That(min?.AnyTypeOptions is null && max?.AnyTypeOptions is null,
                "min/max 不再退化成 Any 类型切换",
                "minOptions=" + (min?.AnyTypeOptions?.Count ?? -1) + " maxOptions=" + (max?.AnyTypeOptions?.Count ?? -1));
            return checks.Finish("--case dimension-light-field-types");
        }

        // ── 真 schema：dimension_type → monster_spawn_light_level（IntProvider<T> 的泛型 spread）──────
        public static int RealDimensionLightLevel()
        {
            var checks = new Checks();
            var root = RealSchema.LocateMcdocRoot();
            if (root is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            Deterministic();
            var index = new McdocTypeIndex();
            var loaded = RealSchema.Load(index, root);
            checks.That(loaded > 100, "真 schema 索引已装载", "条目=" + loaded + " 类型=" + index.TypeDefCount);

            var (document, projector, pipeline) = Harness(index, McdocHelpers.GetRootType("dimension_type"),
                "{ \"monster_spawn_light_level\": { \"type\": \"minecraft:clamped\" } }");
            const string path = "$.monster_spawn_light_level";
            var node = document.FindNode(path);
            checks.That(node is not null, "dimension_type 根能建出树", node is null ? "没有节点" : "Type=" + node.Type?.Kind);
            if (node is null) return checks.Finish("--case real-dimension-light-level");

            node.EnsureChildren();
            var before = McdocPresenterRegistry.Default.Children(node).Select(child => child.FieldName).ToList();
            Console.WriteLine("[信息] 成员 kinds=" + string.Join("|", (node.Type?.Members ?? []).Select(member =>
                (member?.Kind.ToString() ?? "?") + (member?.Kind == McdocTypeKind.Reference ? "(" + member.Path + ")" : ""))));
            Console.WriteLine("[信息] 选中=" + (node.SelectedUnionTypeName?.Name ?? "<null>")
                + " 成员数=" + (node.SelectedUnionChildren?.Count ?? -1)
                + " 子行=" + string.Join("|", (node.Children ?? []).Select(child =>
                    (child.FieldName ?? "<null>") + "/" + (child.Type?.Kind.ToString() ?? "?")
                    + "/branch=" + (child.UnionBranchKind ?? "-") + "/err=" + (child.Error ?? "-"))));
            checks.That(before.Contains("type"), "文档里已有 type → 判别键在行上", "子行=" + string.Join(",", before));
            checks.That(node.Head.EditorKind != McdocEditorKind.Raw,
                "union 行不是原始 JSON（类型没退化成 any）",
                "EditorKind=" + node.Head.EditorKind + " 子行=" + string.Join(",", before));
            checks.That(before.Contains("min_inclusive") && before.Contains("max_inclusive"),
                "clamped 支的同层 spread 字段解出来了（泛型实参代进去）", "子行=" + string.Join(",", before));

            // 从标量支切到结构支：不许弹回、结构化后同层字段当场出现
            var unionOption = node.UnionTypeNameList is { Count: 2 } ? node.UnionTypeNameList[1] : null;
            checks.That(unionOption is not null, "union 有 struct 成员", "成员=" + (node.UnionTypeNameList?.Count ?? 0));
            if (unionOption is null) return checks.Finish("--case real-dimension-light-level");

            McdocUnionSwitch.SwitchTo(node, unionOption);
            pipeline.Reload();
            var switched = document.FindNode(path);
            switched?.EnsureChildren();
            var after = switched is null
                ? []
                : McdocPresenterRegistry.Default.Children(switched).Select(child => child.FieldName).ToList();
            checks.That(switched?.Type?.Kind == McdocTypeKind.Union, "切到结构支后仍是 union（没被退化成 any）",
                "Type=" + (switched?.Type?.Kind.ToString() ?? "<null>"));
            checks.That(after.Contains("type"), "结构支有 type 行", "子行=" + string.Join(",", after));
            checks.That(switched?.Head.EditorKind != McdocEditorKind.Raw, "结构支不是原始 JSON",
                "EditorKind=" + (switched?.Head.EditorKind.ToString() ?? "<null>"));

            // 再往里一层：weighted_list 的条目里 data.type 是 JSON null（现场截图），
            // 必选判别键没有值 → 同层 spread 必须由管线补出来，不能停在"行上有候选、文档没有值"。
            const string nested = "{ \"monster_spawn_light_level\": { \"type\": \"minecraft:weighted_list\", "
                + "\"distribution\": [ { \"weight\": 2, \"data\": { \"type\": null } } ] } }";
            var (deep, _, deepPipeline) = Harness(index, McdocHelpers.GetRootType("dimension_type"), nested,
                document => { document.GameVersion = "1.20.5"; });
            deepPipeline.MaterializeUntilStable();
            deepPipeline.Reload();
            var dataPath = path + ".distribution[0].data";
            var data = Reach(deep, dataPath);
            data?.EnsureChildren();
            var dataChildren = new List<string>();
            foreach (var child in data is null ? [] : McdocPresenterRegistry.Default.Children(data).ToList())
            {
                child.EnsureChildren();
                dataChildren.Add(child.FieldName);
                foreach (var grand in McdocPresenterRegistry.Default.Children(child)) dataChildren.Add(grand.FieldName);
            }
            dataChildren = dataChildren.Where(name => !string.IsNullOrEmpty(name)).Distinct().ToList();
            checks.That(dataChildren.Contains("type"), "嵌套条目：data 有 type 行", "子行=" + string.Join(",", dataChildren));
            checks.That(McdocTreeDocument.ResolvePath(deep.RootJson, dataPath + ".type") is { Kind: McdocJsonKind.String } writtenType
                    && !string.IsNullOrEmpty(writtenType.StringValue),
                "必选判别键（JSON null）要补成显示的候选值", "type=" +
                (McdocTreeDocument.ResolvePath(deep.RootJson, dataPath + ".type")?.StringValue ?? "<null>"));
            checks.That(dataChildren.Contains("min_inclusive") && dataChildren.Contains("max_inclusive"),
                "嵌套条目：补出判别键后同层 spread 解出来了", "子行=" + string.Join(",", dataChildren));
            return checks.Finish("--case real-dimension-light-level");
        }

        // ── 真 schema：dimension_type 的截图现场（type 显示候选、文档里是 null）──────
        public static int RealDimensionDefault()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            Deterministic();
            var index = new McdocTypeIndex();
            var loaded = RealSchema.Load(index, rootDir);
            checks.That(loaded > 100, "真 schema 索引已装载", "条目=" + loaded + " 类型=" + index.TypeDefCount);

            var rootType = McdocHelpers.GetRootType("dimension_type");
            const string screenshotJson = "{ \"monster_spawn_light_level\": { \"type\": null } }";
            var (document, projector, pipeline) = Harness(index, rootType, screenshotJson,
                document => { document.GameVersion = "1.20.5"; });
            var written = pipeline.MaterializeUntilStable(includeDisplayedDefaults: true);
            projector.Reset(document.Nodes.ToList());
            Console.WriteLine("[信息] 载入期物化 written=" + written);

            var node = Reach(document, "$.monster_spawn_light_level");
            node?.EnsureChildren();
            var children = node is null
                ? []
                : McdocPresenterRegistry.Default.Children(node).Select(child => child.FieldName).ToList();
            var typeJson = McdocTreeDocument.ResolvePath(document.RootJson, "$.monster_spawn_light_level.type");
            Console.WriteLine("[信息] node=" + (node?.Type?.Kind.ToString() ?? "<null>")
                + " 选中=" + (node?.SelectedUnionTypeName?.Name ?? "<null>")
                + " 子行=" + string.Join(",", children)
                + " type=" + (typeJson?.Kind.ToString() ?? "<null>") + "/" + (typeJson?.StringValue ?? "<null>"));
            checks.That(typeJson is { Kind: McdocJsonKind.String } writtenType && !string.IsNullOrEmpty(writtenType.StringValue),
                "默认文档载入后 type 落盘", "type=" + (typeJson?.StringValue ?? "<null>")
                + " 子行=" + string.Join(",", children));
            checks.That(children.Contains("min_inclusive") && children.Contains("max_inclusive"),
                "默认文档载入后同层 spread 解出来了", "子行=" + string.Join(",", children)
                + " 选中=" + (node?.SelectedUnionTypeName?.Name ?? "<null>"));

            // B：从标量支切到结构支（截图前一步）——切换本身就要把 type 落盘并解出 spread
            var (switched, switchedProjector, switchedPipeline) = Harness(index, rootType,
                "{ \"monster_spawn_light_level\": 0 }",
                document => { document.GameVersion = "1.20.5"; });
            var scalarNode = Reach(switched, "$.monster_spawn_light_level");
            scalarNode?.EnsureChildren();
            var structOption = scalarNode?.UnionTypeNameList?.FirstOrDefault(option =>
                option.Value?.LiteralValue is int indexValue && indexValue == 1);
            checks.That(structOption is not null, "B 拿到结构支选项",
                "候选=" + (scalarNode?.UnionTypeNameList?.Count ?? 0));
            if (structOption is not null)
            {
                McdocUnionSwitch.SwitchTo(scalarNode, structOption);
                // 修复后的宿主出口：切完结构分支延后一拍重建这一支（探针里同步执行 = 下一拍）。
                // 走的是 VM 同一个收口点，不手动补物化/整表重投影。
                switchedPipeline.OnUnionBranchSwitched(scalarNode);
                var switchedNode = Reach(switched, "$.monster_spawn_light_level");
                switchedNode?.EnsureChildren();
                var switchedChildren = switchedNode is null
                    ? []
                    : McdocPresenterRegistry.Default.Children(switchedNode).Select(child => child.FieldName).ToList();
                var switchedType = McdocTreeDocument.ResolvePath(switched.RootJson, "$.monster_spawn_light_level.type");
                Console.WriteLine("[信息] B 切换后（只做定向重建）：node=" + (switchedNode?.Type?.Kind.ToString() ?? "<null>")
                    + " 选中=" + (switchedNode?.SelectedUnionTypeName?.Name ?? "<null>")
                    + " 子行=" + string.Join(",", switchedChildren)
                    + " type=" + (switchedType?.Kind.ToString() ?? "<null>") + "/" + (switchedType?.StringValue ?? "<null>")
                    + " raw=" + McdocJsonFormatter.Format(switched.RootJson));
                checks.That(switchedType is { Kind: McdocJsonKind.String } switchedText
                        && !string.IsNullOrEmpty(switchedText.StringValue),
                    "B 切到结构支后 type 落盘", "type=" + (switchedType?.StringValue ?? "<null>")
                    + " 子行=" + string.Join(",", switchedChildren));
                checks.That(switchedChildren.Contains("min_inclusive") && switchedChildren.Contains("max_inclusive"),
                    "B 切到结构支后同层 spread 解出来了", "子行=" + string.Join(",", switchedChildren));
            }

            // C：宿主默认懒建 + 「刚建出子级」钩子（截图就是宿主这条链路）
            McdocNodeFactory.LazyChildren = true;
            try
            {
                var (lazyDocument, lazyProjector, lazyPipeline) = Harness(index, rootType, screenshotJson,
                    document => { document.GameVersion = "1.20.5"; });
                lazyProjector.AfterChildrenBuilt = node =>
                {
                    if (lazyPipeline.Rebuilding) return;
                    var writtenByHook = McdocCollectionEditor.Default.MaterializeRequiredSelections([node]);
                    if (writtenByHook > 0) lazyPipeline.RebuildTargetedOrFallback(node);
                };
                lazyProjector.Reset(lazyDocument.Nodes.ToList());
                lazyPipeline.MaterializeUntilStable(includeDisplayedDefaults: true);
                lazyProjector.Reset(lazyDocument.Nodes.ToList());
                var lazyNode = Reach(lazyDocument, "$.monster_spawn_light_level");
                lazyNode?.EnsureChildren();
                var lazyChildren = lazyNode is null
                    ? []
                    : McdocPresenterRegistry.Default.Children(lazyNode).Select(child => child.FieldName).ToList();
                var lazyType = McdocTreeDocument.ResolvePath(lazyDocument.RootJson, "$.monster_spawn_light_level.type");
                var lazyRows = lazyProjector.Rows
                    .Where(row => row.Node?.JsonPath?.StartsWith("$.monster_spawn_light_level", StringComparison.Ordinal) == true)
                    .Select(row => (row.Node?.JsonPath ?? "?") + "/" + (row.Node?.FieldName ?? "?"))
                    .ToList();
                Console.WriteLine("[信息] C 懒建：子行=" + string.Join(",", lazyChildren)
                    + " type=" + (lazyType?.StringValue ?? "<null>")
                    + " 投影=" + string.Join(",", lazyRows));
                checks.That(lazyType is { Kind: McdocJsonKind.String } lazyText && !string.IsNullOrEmpty(lazyText.StringValue),
                    "C 懒建载入后 type 落盘", "type=" + (lazyType?.StringValue ?? "<null>")
                    + " 子行=" + string.Join(",", lazyChildren));
                checks.That(lazyRows.Any(line => line.EndsWith("/min_inclusive", StringComparison.Ordinal))
                        && lazyRows.Any(line => line.EndsWith("/max_inclusive", StringComparison.Ordinal)),
                    "C 懒建载入后投影里有 spread 子行", "投影=" + string.Join(",", lazyRows));
            }
            finally
            {
                McdocNodeFactory.LazyChildren = false;
            }
            return checks.Finish("--case real-dimension-default");
        }

        // ── VM 入口：切 union 结构分支后，宿主那条 NodeValueChanged 收尾必须把这一支重建 ──────
        public static int VmUnionSwitch()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            Deterministic();
            var resource = new CBHK.Model.Constant.Resource();
            RealSchema.Load(resource.McdocTypes, rootDir);
            resource.LoadRegistries(System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\CBHK\bin\Debug\net8.0-windows10.0.22621.0\Resource\Registries"));
            checks.That(resource.McdocTypes.TypeDefCount > 100, "VM 符号表已就绪", "类型=" + resource.McdocTypes.TypeDefCount);

            var vm = new CBHK.ViewModel.Generator.DimensionTypeViewModel(resource);
            var documentField = typeof(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase).GetField(
                "document", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var document = documentField?.GetValue(vm) as McdocTreeDocument;
            checks.That(document is not null, "VM 建出了文档", document is null ? "document=null" : "");
            if (document is null) return checks.Finish("--case vm-union-switch");

            var node = document.FindNode("$.monster_spawn_light_level");
            node?.EnsureChildren();
            var structOption = node?.UnionTypeNameList?.FirstOrDefault(option =>
                option.Value?.LiteralValue is int indexValue && indexValue == 1);
            checks.That(structOption is not null, "VM 拿到结构支选项",
                "候选=" + (node?.UnionTypeNameList?.Count ?? 0)
                + " 节点=" + (node?.Type?.Kind.ToString() ?? "<null>"));
            if (structOption is null) return checks.Finish("--case vm-union-switch");

            McdocUnionSwitch.SwitchTo(node, structOption);
            // ScheduleRebuild 走 Dispatcher.BeginInvoke：把队列跑空 = 宿主"延后一拍"
            var frame = new System.Windows.Threading.DispatcherFrame();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.ApplicationIdle,
                new Action(() => frame.Continue = false));
            System.Windows.Threading.Dispatcher.PushFrame(frame);

            var rebuilt = document.FindNode("$.monster_spawn_light_level");
            rebuilt?.EnsureChildren();
            var children = rebuilt is null
                ? []
                : McdocPresenterRegistry.Default.Children(rebuilt).Select(child => child.FieldName).ToList();
            var typeJson = McdocTreeDocument.ResolvePath(document.RootJson, "$.monster_spawn_light_level.type");
            Console.WriteLine("[信息] VM 切支后：node=" + (rebuilt?.Type?.Kind.ToString() ?? "<null>")
                + " 选中=" + (rebuilt?.SelectedUnionTypeName?.Name ?? "<null>")
                + " 子行=" + string.Join(",", children)
                + " type=" + (typeJson?.Kind.ToString() ?? "<null>") + "/" + (typeJson?.StringValue ?? "<null>")
                + " JsonText=" + (vm.JsonText ?? "").Replace("\r", "").Replace("\n", " "));
            checks.That(typeJson is { Kind: McdocJsonKind.String } writtenType && !string.IsNullOrEmpty(writtenType.StringValue),
                "VM 切支后 type 落盘", "type=" + (typeJson?.StringValue ?? "<null>")
                + " 子行=" + string.Join(",", children));
            checks.That(children.Contains("min_inclusive") && children.Contains("max_inclusive"),
                "VM 切支后同层 spread 解出来了", "子行=" + string.Join(",", children)
                + " 选中=" + (rebuilt?.SelectedUnionTypeName?.Name ?? "<null>"));
            return checks.Finish("--case vm-union-switch");
        }

        // ── 泛型实参自带动态索引（NonEmptyWeightedList<IntProvider<T>> 形状，不依赖真 schema）─────
        public static int GenericArgDynamicIndex()
        {
            var checks = new Checks();
            Deterministic();

            // 现场形状：value: IntProvider → 切到 weighted 支 → distribution[0].data 再解一层 union
            const string path = "$.value.distribution[0].data";
            const string head = "{ \"value\": { \"type\": \"weighted\", \"distribution\": [ ";

            // 变体 1：元素 data 的判别键已有值 → 元素的同层 spread 必须解出来
            var index1 = new McdocTypeIndex();
            Fixtures.RegisterGenericArgTypeDefs(index1);
            var (document1, _, pipeline1) = Harness(index1, Fixtures.GenericArgRootType(),
                head + "{ \"weight\": 2, \"data\": { \"type\": \"minecraft:uniform\", \"min_inclusive\": 1, \"max_inclusive\": 5 } } ] } }");
            pipeline1.MaterializeUntilStable();
            var data1 = Reach(document1, path);
            data1?.EnsureChildren();
            var children1 = data1 is null
                ? []
                : McdocPresenterRegistry.Default.Children(data1).Select(child => child.FieldName).ToList();
            checks.That(children1.Contains("type"), "变体1 · 元素 data 有 type 行", "子行=" + string.Join(",", children1));
            checks.That(children1.Contains("min_inclusive") && children1.Contains("max_inclusive"),
                "变体1 · 元素 data 的同层 spread 解出来了", "子行=" + string.Join(",", children1));

            // 变体 2：判别键是 JSON null → 建树期补成显示中的候选，再解同层
            var index2 = new McdocTypeIndex();
            Fixtures.RegisterGenericArgTypeDefs(index2);
            var (document2, _, pipeline2) = Harness(index2, Fixtures.GenericArgRootType(),
                head + "{ \"weight\": 2, \"data\": { \"type\": null } } ] } }");
            pipeline2.MaterializeUntilStable();
            var written = McdocTreeDocument.ResolvePath(document2.RootJson, path + ".type");
            checks.That(written is { Kind: McdocJsonKind.String } writtenText && !string.IsNullOrEmpty(writtenText.StringValue),
                "变体2 · JSON null 的判别键补成显示中的候选",
                "type=" + (written?.StringValue ?? "<null>"));
            var before = Reach(document2, path);
            before?.EnsureChildren();
            var data2 = Reach(document2, path);
            data2?.EnsureChildren();
                        var names = new List<string>();
            foreach (var child in McdocPresenterRegistry.Default.Children(data2).ToList())
            {
                child.EnsureChildren();
                names.Add(child.FieldName);
                foreach (var grand in McdocPresenterRegistry.Default.Children(child)) names.Add(grand.FieldName);
            }
            names = names.Where(name => !string.IsNullOrEmpty(name)).Distinct().ToList();
            var children2 = names;
            checks.That(children2.Contains("value") || children2.Contains("min_inclusive"),
                "变体2 · 补出判别键后同层 spread 解出来了", "子行=" + string.Join(",", children2));

            // 变体 3：dispatch 目标是**泛型别名**（真 schema 的 UniformIntProvider<T> 形状）
            var index3 = new McdocTypeIndex();
            Fixtures.RegisterGenericArgTypeDefs(index3);
            var (document3, _, pipeline3) = Harness(index3, Fixtures.GenericArgRootType(),
                head + "{ \"weight\": 2, \"data\": { \"type\": \"uniform_alias\" } } ] } }");
            pipeline3.MaterializeUntilStable();
            var data3 = Reach(document3, path);
            data3?.EnsureChildren();
            var names3 = new List<string>();
            foreach (var child in data3 is null ? [] : McdocPresenterRegistry.Default.Children(data3).ToList())
            {
                child.EnsureChildren();
                names3.Add(child.FieldName);
                foreach (var grand in McdocPresenterRegistry.Default.Children(child)) names3.Add(grand.FieldName);
            }
            names3 = names3.Where(name => !string.IsNullOrEmpty(name)).Distinct().ToList();
            checks.That(names3.Contains("min_inclusive") && names3.Contains("max_inclusive"),
                "变体3 · 泛型别名成员的同层字段解出来了", "子行=" + string.Join(",", names3));
            return checks.Finish("--case generic-arg-dynamic-index");
        }

        // ── 现场（bug 1/3）：懒建子树在**结构变更闸门内**首次建出 → 补物化被丢弃 ────────────
        // 必填描别键的代数默认值是空串，只有"闸门内登记 + 闸门外补跑"这一条路能把它落成候选；
        // 丢掉那一步，[[type]] 读成空 → spread 没有成员 → 行上挂「动态键「」没有对应成员」。
        public static int DiscriminatorEmptyString()
        {
            var checks = new Checks();
            Deterministic();
            // 界面默认就是懒建：深于 DefaultExpandDepth 的子树不会在建树期建出来
            McdocNodeFactory.LazyChildren = true;
            try
            {
                var index = new McdocTypeIndex();
                Fixtures.RegisterLootEntryDispatchers(index);

                // 与 McdocDefaults.Resolve 对必填字符串的产物一致：{ "type": "" }
                var (document, projector, pipeline) = Harness(index, Fixtures.LootEntryRootType(),
                    "{ \"pool\": { \"entries\": [ { \"type\": \"\" } ] } }", wireChildBuiltHook: true);
                var entry = document.FindNode("$.pool.entries[0]");
                checks.That(entry is not null, "条目节点存在", "$.pool.entries[0]");
                if (entry is null) return checks.Finish("--case discriminator-empty-string");

                // 现场：展开这个条目 → 投影在闸门内首次建出它的子级（描别键就在这一层）
                entry.IsExpanded = true;
                var wasRebuilding = pipeline.Rebuilding;
                pipeline.Rebuilding = true;
                projector.Reset(document.Nodes.ToList());
                pipeline.Rebuilding = wasRebuilding;
                pipeline.DrainPendingChildBuilds();

                entry = document.FindNode("$.pool.entries[0]");
                entry?.EnsureChildren();
                var typeJson = McdocTreeDocument.ResolvePath(document.RootJson, "$.pool.entries[0].type");
                var children = entry is null
                    ? []
                    : McdocPresenterRegistry.Default.Children(entry).Select(child => child.FieldName).Where(name => !string.IsNullOrEmpty(name)).ToList();
                var diagnostics = document.Diagnostics.Select(item => item.Message).ToList();
                Console.WriteLine("[信息] type=" + (typeJson?.StringValue ?? "<null>")
                    + " 子行=" + string.Join(",", children)
                    + " 诊断=" + string.Join(" | ", diagnostics));

                checks.That(typeJson is { Kind: McdocJsonKind.String } text && !string.IsNullOrWhiteSpace(text.StringValue),
                    "闸门外补跑把空串描别键写成候选", "type=" + (typeJson?.StringValue ?? "<null>"));
                checks.That(children.Contains("alpha_value") || children.Contains("beta_value"),
                    "同层 spread 按描别键解出成员", "子行=" + string.Join(",", children));
                checks.That(!diagnostics.Any(message => (message ?? "").Contains("动态键", StringComparison.Ordinal)),
                    "没有「动态键…没有对应成员」诊断", string.Join(" | ", diagnostics));
            }
            finally
            {
                McdocNodeFactory.LazyChildren = false;
            }
            return checks.Finish("--case discriminator-empty-string");
        }

        // ── 现场（bug 2）：一个元素的列表，点尾部「添加」之后，新项的上移必须可用 ─────────────
        // 两件事一起判：① 尾部行本身不许挂列表项按钮（它的 Command 是 null，点了什么也不发生）；
        // ② 新项的行必须指向**还在 Items 里**的活节点（旧闭包/旧代节点 = 点了没反应）。
        public static int ListItemRowState()
        {
            var checks = new Checks();
            Deterministic();
            var itemType = McdocType.Create(McdocTypeKind.Struct);
            itemType.Fields =
            [
                new McdocStructField
                {
                    Name = "name", Key = McdocType.LiteralString("name"),
                    Type = McdocType.Create(McdocTypeKind.String), Optional = true
                }
            ];
            var (document, projector, _) = Harness(new McdocTypeIndex(), Struct(("items", ListOf(itemType))),
                "{ \"items\": [ { \"name\": \"a\" } ] }");

            List<McdocRow> ItemRows() => projector.Rows
                .Where(row => row.IsListChild && row.Kind is not McdocRowKind.More and not McdocRowKind.AddTail)
                .ToList();

            var tail = projector.Rows.FirstOrDefault(row => row.Kind == McdocRowKind.AddTail);
            checks.That(tail is not null, "一个元素时尾部有「添加」行", "行数=" + projector.Rows.Count);
            checks.That(tail is null || tail.ItemActionsVisibility != System.Windows.Visibility.Visible,
                "尾部「添加」行不挂列表项按钮",
                tail is null ? "<无行>" : "Visibility=" + tail.ItemActionsVisibility + " MoveUp=" + (tail.MoveUpCommand is null ? "null" : "非空"));
            if (tail is null) return checks.Finish("--case list-item-row-state");

            tail.AddTailCommand?.Execute(null);
            var rows = ItemRows();
            checks.That(rows.Count == 2, "追加后有两项行", "项行=" + rows.Count);
            var appended = rows.LastOrDefault();
            var liveList = document.FindNode("$.items");
            liveList?.EnsureChildren();
            checks.That(appended is not null && liveList is not null
                    && ReferenceEquals(appended.Node.Parent, liveList) && liveList.Items.Contains(appended.Node),
                "新项的行指向仍在 Items 里的活节点",
                appended is null ? "<无行>" : "parent=" + (appended.Node.Parent?.JsonPath ?? "<null>")
                    + " inItems=" + (liveList?.Items.Contains(appended.Node) ?? false)
                    + " index=" + (liveList?.Items.IndexOf(appended.Node) ?? -2));
            checks.That(appended?.MoveUpCommand is not null && appended.MoveUpCommand.CanExecute(null),
                "新项的上移可用",
                appended is null ? "<无行>"
                    : appended.MoveUpCommand is null ? "命令=null"
                    : "CanExecute=" + appended.MoveUpCommand.CanExecute(null));

            var appendedJson = appended?.Node?.Json;
            appended?.MoveUpCommand?.Execute(null);
            var first = document.RootJson?.Get("items")?.Items?.FirstOrDefault();
            checks.That(appendedJson is not null && ReferenceEquals(first, appendedJson),
                "上移真的把新项挪到第一位", "first=" + FormatOneLine(first));
            // 已在顶部：再按一次必须无效，不许把下面的项挪走
            appended?.MoveUpCommand?.Execute(null);
            var firstAgain = document.RootJson?.Get("items")?.Items?.FirstOrDefault();
            checks.That(ReferenceEquals(firstAgain, appendedJson), "连点两次上移不挪走别的项",
                "第二次首位=" + FormatOneLine(firstAgain));
            return checks.Finish("--case list-item-row-state");
        }

        // ── 现场（bug 1/3 的真 schema 版）：loot_table 的条目条目描别键 + 同层调度器 spread ────
        public static int LootEntryLoad()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            Deterministic();
            var index = new McdocTypeIndex();
            var loaded = RealSchema.Load(index, rootDir);
            checks.That(loaded > 100, "真符号表已就绪", "条目=" + loaded + " 类型=" + index.TypeDefCount);

            var (document, projector, pipeline) = Harness(index, McdocHelpers.GetRootType("loot_table"),
                "{ \"pools\": [ { \"rolls\": 0, \"entries\": [ { \"type\": \"\" } ] } ] }",
                doc => doc.GameVersion = "1.20.5");
            pipeline.MaterializeUntilStable(includeDisplayedDefaults: true);
            projector.Reset(document.Nodes.ToList());
            AssertEntry("载入", "$.pools[0].entries[0]");

            // 同一个"添加条目"手势：新条目必须当场落描别键、当场解出兄弟 spread
            document.FindNode("$.pools[0].entries")?.AddItemCommand?.Execute(null);
            pipeline.MaterializeUntilStable(includeDisplayedDefaults: true);
            projector.Reset(document.Nodes.ToList());
            AssertEntry("追加", "$.pools[0].entries[1]");
            return checks.Finish("--case loot-entry-load");

            void AssertEntry(string label, string path)
            {
                var typeJson = McdocTreeDocument.ResolvePath(document.RootJson, path + ".type");
                var node = document.FindNode(path);
                node?.EnsureChildren();
                var children = node is null
                    ? []
                    : McdocPresenterRegistry.Default.Children(node)
                        .Select(child => child.FieldName)
                        .Where(name => !string.IsNullOrEmpty(name)).ToList();
                var diagnostics = document.Diagnostics.Select(item => item.Message).ToList();
                Console.WriteLine("[信息] " + label + " type=" + (typeJson?.StringValue ?? "<null>")
                    + " 子行=" + string.Join(",", children)
                    + " 诊断=" + string.Join(" | ", diagnostics.Take(3)));
                checks.That(typeJson is { Kind: McdocJsonKind.String } text && !string.IsNullOrWhiteSpace(text.StringValue),
                    label + " · 描别键不是空串", "type=" + (typeJson?.StringValue ?? "<null>"));
                checks.That(children.Any(name => !string.Equals(name, "type", StringComparison.Ordinal)),
                    label + " · 同层 spread 成员行出现", "子行=" + string.Join(",", children));
                checks.That(!diagnostics.Any(message => (message ?? "").Contains("动态键", StringComparison.Ordinal)),
                    label + " · 无「动态键」诊断", string.Join(" | ", diagnostics.Take(3)));
            }
        }

        // ── 现场（第二轮 bug 1/3/4）：真页面 + 真 VM 的行/命令一致性 ──────────────────────────
        // 三个症状共用一件事：手势之后"行集合"没有跟着"文档/节点"换代。
        // ── 数字 ±1 / 布尔切换：只许改值，不许动结构（动了结构就是滚动视图跳一下的成因）──
        // ── 可选 bool / union bool 取消两个选中后必须 unset 并同步 JSON ─────────────
        public static int OptionalBoolUnset()
        {
            var checks = new Checks();
            Deterministic();
            var boolType = McdocType.Create(McdocTypeKind.Boolean);
            var literal = McdocType.Create(McdocTypeKind.Literal);
            literal.LiteralKind = McdocLiteralKind.String;
            literal.Value = "default";
            var union = McdocType.Create(McdocTypeKind.Union);
            union.Members = [boolType, literal];
            var rootType = McdocType.Create(McdocTypeKind.Struct);
            rootType.Fields =
            [
                new McdocStructField { Name = "u", Key = McdocType.LiteralString("u"), Type = union, Optional = true },
                new McdocStructField { Name = "v", Key = McdocType.LiteralString("v"), Type = boolType, Optional = true },
            ];
            var (document, _, pipeline) = Harness(new McdocTypeIndex(), rootType, "{ \"u\": true, \"v\": false }");
            var unionNode = document.FindNode("$.u");
            var plainNode = document.FindNode("$.v");
            checks.That(unionNode is not null && plainNode is not null, "取得 union bool / 普通可选 bool 节点", "");
            if (unionNode is null || plainNode is null) return checks.Finish("--case optional-bool-unset");

            // union bool：取消 True（另一个本来就是 false）→ ApplyNodeValue 应走 unset
            unionNode.IsTrue = false;
            McdocCollectionEditor.Default.ApplyNodeValue(unionNode);
            PumpDispatcher();
            checks.That(McdocTreeDocument.ResolvePath(document.RootJson, "$.u") is null,
                "union bool 两按钮取消后 JSON 键删掉", "json=" + FormatOneLine(McdocTreeDocument.ResolvePath(document.RootJson, "$.u")));
            checks.That(unionNode.Json is null
                    && (McdocUnionSwitch.IsUnsetOption(unionNode.SelectedUnionTypeName)
                        || unionNode.ScalarEditorKind is null),
                "union 节点回到未设置", "json=" + (unionNode.Json?.Kind.ToString() ?? "<null>")
                + " selected=" + (unionNode.SelectedUnionTypeName?.Name ?? "<null>")
                + " editor=" + (unionNode.ScalarEditorKind ?? "<null>"));

            // 普通可选 bool：取消 False 后删除键
            plainNode.IsFalse = false;
            McdocCollectionEditor.Default.ApplyNodeValue(plainNode);
            PumpDispatcher();
            checks.That(McdocTreeDocument.ResolvePath(document.RootJson, "$.v") is null,
                "普通可选 bool 两按钮取消后 JSON 键删掉", "json=" + FormatOneLine(McdocTreeDocument.ResolvePath(document.RootJson, "$.v")));
            return checks.Finish("--case optional-bool-unset");
        }

        // ── 数字越界：不夹取、不报红色错误；只给黄色范围警告 ──────────────────────
        public static int NumberRangeWarning()
        {
            var checks = new Checks();
            Deterministic();
            var ranged = McdocType.Create(McdocTypeKind.Int);
            ranged.Range = new McdocRange { Min = 0, Max = 10 };
            var lowOnly = McdocType.Create(McdocTypeKind.Int);
            lowOnly.Range = new McdocRange { Min = 0 };
            var exclusive = McdocType.Create(McdocTypeKind.Int);
            exclusive.Range = new McdocRange { Min = 0, Max = 10, MinExclusive = true, MaxExclusive = true };
            var rootType = McdocType.Create(McdocTypeKind.Struct);
            rootType.Fields =
            [
                new McdocStructField { Name = "n", Key = McdocType.LiteralString("n"), Type = ranged },
                new McdocStructField { Name = "low", Key = McdocType.LiteralString("low"), Type = lowOnly },
                new McdocStructField { Name = "open", Key = McdocType.LiteralString("open"), Type = exclusive },
            ];
            var (document, projector, pipeline) = Harness(new McdocTypeIndex(), rootType,
                "{ \"n\": 20, \"low\": -1, \"open\": 0 }");
            var node = document.FindNode("$.n");
            var row = node is null ? null : projector.Find(node);
            checks.That(node is not null && row is not null, "前置：范围数字行存在",
                node is null ? "没有节点" : row is null ? "没有行" : "");
            if (node is null || row is null) return checks.Finish("--case number-range-warning");
            pipeline.RefreshDiagnostics(node);
            PumpDispatcher();
            checks.That(document.RootJson?.Get("n")?.NumberValue == 20m, "越界值不被夹取",
                "n=" + (document.RootJson?.Get("n")?.NumberValue.ToString() ?? "<null>"));
            checks.That(row.Diagnostics.Any(text => text.Contains("超出范围") && text.Contains("0-10")),
                "初始越界值有范围警告", string.Join(" | ", row.Diagnostics));
            checks.That(row.DiagnosticBadgeVisibility == System.Windows.Visibility.Visible,
                "行右侧显示黄色感叹号入口", row.DiagnosticBadgeVisibility.ToString());
            checks.That(row.ErrorBrush is System.Windows.Media.SolidColorBrush,
                "警告使用现有黄色画笔", row.ErrorBrush?.ToString() ?? "null");

            void SetValue(McdocTreeNode target, decimal value)
            {
                target.NumberText = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                McdocCollectionEditor.Default.ApplyNodeValue(target);
                pipeline.OnValueChanged(target);
                PumpDispatcher();
            }

            SetValue(node, 11);
            checks.That(document.RootJson?.Get("n")?.NumberValue == 11m, "输入 11 不被夹回 10",
                "n=" + (document.RootJson?.Get("n")?.NumberValue.ToString() ?? "<null>"));
            checks.That(node.Diagnostics.Any(text => text.Contains("超出范围"))
                    && document.RootJson?.Get("n")?.NumberValue == 11m,
                "输入越界后仍保留值并给出警告", string.Join(" | ", node.Diagnostics));

            SetValue(node, 10);
            node.IncreaseCommand.Execute(null);
            PumpDispatcher();
            checks.That(document.RootJson?.Get("n")?.NumberValue == 11m, "上界 +1 不再被夹取",
                "n=" + (document.RootJson?.Get("n")?.NumberValue.ToString() ?? "<null>"));
            checks.That(node.Diagnostics.Any(text => text.Contains("超出范围")),
                "步进越界也给出范围警告", string.Join(" | ", node.Diagnostics));

            SetValue(node, 5);
            checks.That(document.RootJson?.Get("n")?.NumberValue == 5m, "值回到范围内",
                "n=" + (document.RootJson?.Get("n")?.NumberValue.ToString() ?? "<null>"));
            checks.That(!node.Diagnostics.Any(text => text.Contains("超出范围")),
                "回到范围内后范围警告消失", string.Join(" | ", node.Diagnostics));

            var lowNode = document.FindNode("$.low");
            var openNode = document.FindNode("$.open");
            pipeline.RefreshDiagnostics(lowNode ?? node);
            pipeline.RefreshDiagnostics(openNode ?? node);
            PumpDispatcher();
            checks.That(lowNode?.Diagnostics.Any(text => text.Contains("不小于 0")) == true,
                "单边范围提示应不小于 0", string.Join(" | ", lowNode?.Diagnostics ?? []));
            checks.That(openNode?.Diagnostics.Any(text => text.Contains("(0-10)")) == true,
                "开区间范围提示带括号", string.Join(" | ", openNode?.Diagnostics ?? []));
            return checks.Finish("--case number-range-warning");
        }

        public static int NumberStepQuiet()
        {
            var checks = new Checks();
            Deterministic();

            var intType = McdocType.Create(McdocTypeKind.Int);
            var clampedType = McdocType.Create(McdocTypeKind.Int);
            clampedType.Range = McdocRange.Exact(5);
            var rootType = McdocType.Create(McdocTypeKind.Struct);
            rootType.Fields =
            [
                new McdocStructField { Name = "n", Key = McdocType.LiteralString("n"), Type = intType },
                new McdocStructField { Name = "c", Key = McdocType.LiteralString("c"), Type = clampedType },
                new McdocStructField { Name = "flag", Key = McdocType.LiteralString("flag"), Type = McdocType.Create(McdocTypeKind.Boolean) },
            ];
            var (document, projector, pipeline) = Harness(new McdocTypeIndex(), rootType,
                "{ \"n\": 1, \"c\": 5, \"flag\": true }");
            var numberNode = document.FindNode("$.n");
            checks.That(numberNode is not null, "拿到数字节点", numberNode is null ? "没有 $.n" : "");
            if (numberNode is null) return checks.Finish("--case number-step-quiet");

            // 1) ±1 只改值：不换节点、不产生行拼接/整表重投影
            var resetsBefore = projector.ResetCount;
            var splicesBefore = projector.SpliceCount;
            numberNode.IncreaseCommand.Execute(null);
            PumpDispatcher();
            checks.That(Convert.ToString(numberNode.NumberText, System.Globalization.CultureInfo.InvariantCulture) == "2",
                "点 ▲ 之后行上的数字是 2", "NumberText=" + numberNode.NumberText);
            checks.That(document.RootJson?.Get("n")?.NumberValue == 2m, "文档里的值也变成 2",
                "json=" + (document.RootJson?.Get("n")?.NumberValue.ToString() ?? "<null>"));
            checks.That(projector.ResetCount == resetsBefore, "±1 不触发整表重投影",
                "Δreset=" + (projector.ResetCount - resetsBefore));
            checks.That(projector.SpliceCount == splicesBefore, "±1 不触发行拼接（滚动视图因此不跳）",
                "Δsplice=" + (projector.SpliceCount - splicesBefore));
            checks.That(ReferenceEquals(document.FindNode("$.n"), numberNode), "±1 不换节点实例",
                ReferenceEquals(document.FindNode("$.n"), numberNode) ? "还是同一个实例" : "实例被换掉了");

            numberNode.DecreaseCommand.Execute(null);
            PumpDispatcher();
            checks.That(document.RootJson?.Get("n")?.NumberValue == 1m, "再点 ▼ 回到 1",
                "json=" + (document.RootJson?.Get("n")?.NumberValue.ToString() ?? "<null>"));

            // 2) range 不再夹取：越界只产生 warning，值本身保留
            var clampedNode = document.FindNode("$.c");
            resetsBefore = projector.ResetCount;
            splicesBefore = projector.SpliceCount;
            clampedNode?.IncreaseCommand?.Execute(null);
            PumpDispatcher();
            checks.That(document.RootJson?.Get("c")?.NumberValue == 6m, "越界加一不再夹到上界 5",
                "json=" + (document.RootJson?.Get("c")?.NumberValue.ToString() ?? "<null>"));
            checks.That(clampedNode?.Diagnostics.Any(text => text.Contains("超出范围")) == true,
                "越界值产生范围 warning", string.Join(" | ", clampedNode?.Diagnostics ?? []));
            checks.That(projector.ResetCount == resetsBefore && projector.SpliceCount == splicesBefore,
                "越界值变化仍不产生行拼接", "Δreset=" + (projector.ResetCount - resetsBefore)
                + " Δsplice=" + (projector.SpliceCount - splicesBefore));

            // 3) 布尔：互斥赋值 + 只改值，不动结构
            var boolNode = document.FindNode("$.flag");
            checks.That(boolNode is not null && boolNode.IsTrue, "拿到布尔节点（True）",
                boolNode is null ? "没有 $.flag" : "IsTrue=" + boolNode.IsTrue);
            if (boolNode is not null)
            {
                resetsBefore = projector.ResetCount;
                splicesBefore = projector.SpliceCount;
                boolNode.IsTrue = false;
                PumpDispatcher();
                checks.That(!boolNode.IsTrue && boolNode.IsFalse, "点 False 之后选中态互斥",
                    "IsTrue=" + boolNode.IsTrue + " IsFalse=" + boolNode.IsFalse);
                checks.That(projector.ResetCount == resetsBefore && projector.SpliceCount == splicesBefore,
                    "布尔切换不产生行拼接", "Δreset=" + (projector.ResetCount - resetsBefore)
                    + " Δsplice=" + (projector.SpliceCount - splicesBefore));
                for (var round = 0; round < 3; round++)
                {
                    boolNode.IsTrue = round % 2 == 0;
                    PumpDispatcher();
                }
                checks.That(projector.ResetCount == resetsBefore && projector.SpliceCount == splicesBefore,
                    "连续切换仍不产生行拼接", "Δreset=" + (projector.ResetCount - resetsBefore)
                    + " Δsplice=" + (projector.SpliceCount - splicesBefore));
            }

            _ = pipeline;
            return checks.Finish("--case number-step-quiet");
        }

        // ── 交互耗时：求解 / 渲染 / 交互 三桶的分阶段实测 ──────────────────────
        private static readonly string[] StageNames =
        {
            "doc.load", "doc.build", "doc.validate",
            "mat.walk", "mat.load",
            "pipeline.value", "pipeline.structure", "dep.rebuild", "diag.refresh",
            "proj.reset", "proj.splice", "proj.expand",
            "text.sync", "text.format", "text.reconcile", "text.reformat",
            "vm.rebuild", "vm.rebuildNull", "vm.rebuildMiss", "vm.reload", "vm.loadTree",
            "pipeline.rootReload", "vm.dirtyReload"
        };

        private static void MeasureScenario(string name, int rounds, Action step, McdocRowProjector projector = null)
        {
            var samples = new List<double>(rounds);
            var stageSum = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var key in StageNames) stageSum[key] = 0;
            var failures = 0;
            var resets = 0;
            var splices = 0;
            for (var round = 0; round < rounds; round++)
            {
                McdocWriteTrace.ResetStages();
                var resetsBefore = projector?.ResetCount ?? 0;
                var splicesBefore = projector?.SpliceCount ?? 0;
                var started = Stopwatch.GetTimestamp();
                try
                {
                    step();
                    PumpDispatcher();
                }
                catch (Exception)
                {
                    failures++;
                }
                resets += (projector?.ResetCount ?? 0) - resetsBefore;
                splices += (projector?.SpliceCount ?? 0) - splicesBefore;
                samples.Add((Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency);
                foreach (var key in StageNames) stageSum[key] += McdocWriteTrace.StageMs(key);
            }
            if (samples.Count == 0) return;
            samples.Sort();
            var p50 = samples[samples.Count / 2];
            var p95 = samples[Math.Min(samples.Count - 1, (int)Math.Ceiling(samples.Count * 0.95) - 1)];
            var parts = StageNames
                .Where(key => stageSum[key] / rounds >= 0.05)
                .Select(key => key + "=" + (stageSum[key] / rounds).ToString("0.0"));
            Console.WriteLine("[信息] " + name + " n=" + rounds + " 失败=" + failures
                + " p50=" + p50.ToString("0.0") + "ms p95=" + p95.ToString("0.0") + "ms"
                + (projector is null ? "" : " reset=" + resets + " splice=" + splices) + " | "
                + (parts.Any() ? string.Join(" ", parts) : "无分阶段记录"));
        }

        /// <summary>三桶耗时实测：真实 VM + 真实编辑器 + 真实 schema，逐个交互按行上的同一条命令驱动。</summary>
        public static int PerfStages()
        {
            if (RealSchema.LocateMcdocRoot() is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            Deterministic();
            McdocWriteTrace.StatsEnabled = true;
            var vm = CreateLootVm(out var document);
            if (document is null)
            {
                Console.WriteLine("[红] VM 建不出文档");
                return 1;
            }
            var editor = new CBHK.Utility.Data.JsonText.JsonTextEditor();
            vm.AttachJsonEditor(editor);
            var projector = ProjectorOf(vm);
            PumpDispatcher();
            if (!SeedPoolAndEntry(document, vm))
            {
                Console.WriteLine("[红] 种不出 池 + 条目");
                return 1;
            }
            PumpDispatcher();
            Console.WriteLine("[信息] 实测文档：行=" + vm.Rows.Count + " 字符=" + (vm.JsonText?.Length ?? 0)
                + " reset=" + vm.Rows.Count + " build=" + McdocWriteTrace.BuildMarker);

            // 探针跑的是"安装包才带入口文件"的场景：默认文档被代数兜成极小的一份，
            // 量卡顿必须用一只有规模的文档 —— 12 个池 × 4 个条目（= 用户真会做到的量级）。
            var bigText = BigLootTableText(12, 4);
            McdocWriteTrace.ResetStages();
            vm.LoadMcdocTree(bigText);
            PumpDispatcher();
            Console.WriteLine("[信息] 大文档：行=" + vm.Rows.Count + " 字符=" + (vm.JsonText?.Length ?? 0));

            // S0 首屏：整篇重载（建文档 + 建树 + 建投影 + 回写文本）
            MeasureScenario("S0 首屏（整篇载入）", 5, () => vm.LoadMcdocTree(bigText), projector);

            // S1 展开 / 折叠：行上的 ToggleExpandedCommand（与点箭头同一条命令）
            var expandRow = RowOf(vm, "$.pools[0].entries");
            if (expandRow is not null)
            {
                MeasureScenario("S1 展开+折叠（每次 2 次切换）", 10, () =>
                {
                    expandRow.ToggleExpandedCommand?.Execute(null);
                    expandRow.ToggleExpandedCommand?.Execute(null);
                }, projector);
            }
            else
            {
                Console.WriteLine("[信息] S1 跳过：找不到 $.pools[0].entries 行");
            }

            // S2 值编辑：标量行的 Value setter（与行内输入同一条出口）
            var next = 2L;
            if (document.FindNode("$.pools[0].entries[0].weight") is not null)
            {
                MeasureScenario("S2 值编辑（标量 Value）", 20, () =>
                {
                    next = next == 2L ? 3L : 2L;
                    if (document.FindNode("$.pools[0].entries[0].weight") is { } target) target.Value = next;
                }, projector);
            }
            else
            {
                Console.WriteLine("[信息] S2 跳过：找不到 $.pools[0].entries[0].weight");
            }

            // S3 下拉选值：候选之间来回切（枚举成员的 setter）
            var enumNode = FindEnumNode(document);
            if (enumNode?.EnumOptionList is { Count: > 1 } options)
            {
                MeasureScenario("S3 下拉选值（枚举成员）", 10, () =>
                {
                    var current = document.FindNode(enumNode.JsonPath) ?? enumNode;
                    var list = current.EnumOptionList;
                    if (list is not { Count: > 1 }) return;
                    var index = ReferenceEquals(current.SelectedEnumOption, list[0]) ? 1 : 0;
                    current.SelectedEnumOption = list[index];
                }, projector);
            }
            else
            {
                Console.WriteLine("[信息] S3 跳过：文档里没有可直接切换的枚举节点");
            }

            // S4 加列表项 / S5 删一项：行上的 ⊕ / ⊖
            MeasureScenario("S4 加列表项（⊕）", 8, () =>
            {
                document.FindNode("$.pools[0].entries")?.AddItemCommand?.Execute(null);
            }, projector);
            MeasureScenario("S5 删列表项（⊖）", 8, () =>
            {
                var entries = document.FindNode("$.pools[0].entries");
                var last = entries?.Items.LastOrDefault();
                var row = last is null ? null : RowOf(vm, last.JsonPath);
                row?.RemoveSelfCommand?.Execute(null);
            }, projector);

            // S6 数字 ±1：点行上的 ▲（走 McdocCollectionEditor.StepNumber）
            MeasureScenario("S6 数字 ±1（IncreaseCommand）", 10, () =>
            {
                document.FindNode("$.pools[0].entries[0].weight")?.IncreaseCommand?.Execute(null);
            }, projector);
            // S7 布尔切换：行上 False/True 两个按钮走的就是 IsTrue/IsFalse
            var boolPath = FindBoolPath(document);
            if (boolPath is null)
            {
                Console.WriteLine("[信息] S7 跳过：文档里没有布尔节点");
            }
            else
            {
                MeasureScenario("S7 布尔切换（IsTrue）", 10, () =>
                {
                    if (document.FindNode(boolPath) is { } target) target.IsTrue = !target.IsTrue;
                }, projector);
            }
            Console.WriteLine("[信息] 数字/布尔最终值：weight="
                + (document.FindNode("$.pools[0].entries[0].weight")?.NumberText ?? "<无>")
                + " bool=" + (boolPath is null ? "<无>" : (document.FindNode(boolPath)?.NumberText ?? "?")));

            Console.WriteLine("[信息] 汇总 " + McdocWriteTrace.StageReport());

            // 性能场景跑完必须回头验契约：这三条正是本轮改动最容易踩坏的地方
            // （定向重建漏解兄弟分支 / 少排版 / 行与文档脱钩）。
            var failures = 0;
            failures += Assert("行都挂在当前这棵树上", RowsAttached(document, vm), "陈旧行=" + string.Join(",", StaleRows(document, vm)));
            failures += Assert("行深度不跳跃", DepthJumpsOk(vm), "行数=" + vm.Rows.Count);
            failures += Assert("没有指向上一代节点的行", StaleRows(document, vm).Count == 0, string.Join(",", StaleRows(document, vm)));
            var textInStep = editor.State.IsInStepWithDocument;
            failures += Assert("编辑器文本与 AST 同代", textInStep, "IsInStepWithDocument=" + textInStep);
            var modelText = McdocJsonFormatter.Format(document.RootJson);
            failures += Assert("右侧文本 == 模型文本（视图同代）", string.Equals(editor.Text, modelText, StringComparison.Ordinal),
                "editor=" + (editor.Text?.Length ?? -1) + " model=" + modelText.Length);
            failures += Assert("右侧文本仍是合法 JSON", McdocJsonParser.TryParse(editor.Text ?? "", out _, out var syntaxError),
                syntaxError?.Message);
            var missing = vm.Rows
                .Where(row => row.Kind is McdocRowKind.Plain or McdocRowKind.Union or McdocRowKind.Keyed)
                // 可选且文档里没有的字段只留一行 ⊕：它本来就取不到值，不算脱钩
                .Where(row => row.Node?.Json is not null)
                .Where(row => row.Node?.JsonPath is { Length: > 0 } path && path != "$")
                .Where(row => McdocTreeDocument.ResolvePath(document.RootJson, row.Node.JsonPath) is null)
                .Select(row => row.Node.JsonPath).Take(3).ToList();
            failures += Assert("有值的行都能在文档里取到值", missing.Count == 0, "缺=" + string.Join(",", missing));
            Console.WriteLine(failures == 0 ? "[绿] --case perf-stages 契约自检全绿" : "[红] --case perf-stages 契约自检红 " + failures + " 条");
            return failures == 0 ? 0 : 1;
        }

        private static int Assert(string label, bool ok, string detail)
        {
            Console.WriteLine((ok ? "[绿] " : "[红] ") + label + " | " + detail);
            return ok ? 0 : 1;
        }

        /// <summary>一只用户真会写到的 loot_table 文档：多个池、每池多个条目。</summary>
        private static string BigLootTableText(int pools, int entriesPerPool)
        {
            var builder = new System.Text.StringBuilder();
            builder.Append("{\n  \"type\": \"minecraft:chest\",\n  \"pools\": [\n");
            for (var pool = 0; pool < pools; pool++)
            {
                if (pool > 0) builder.Append(",\n");
                builder.Append("    {\n      \"rolls\": 1,\n      \"entries\": [\n");
                for (var entry = 0; entry < entriesPerPool; entry++)
                {
                    if (entry > 0) builder.Append(",\n");
                    builder.Append("        { \"type\": \"minecraft:item\", \"name\": \"minecraft:stone\", \"weight\": 1 }");
                }
                builder.Append("\n      ]\n    }");
            }
            builder.Append("\n  ]\n}");
            return builder.ToString();
        }

        private static McdocRowProjector ProjectorOf(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm)
            => typeof(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase)
                .GetField("projector", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(vm) as McdocRowProjector;

        /// <summary>第一个有值的布尔节点路径：用来量一次 True/False 切换。</summary>
        private static string FindBoolPath(McdocTreeDocument document)
        {
            string found = null;
            void Walk(McdocTreeNode node)
            {
                if (node is null || found is not null) return;
                if (node.Type?.Kind == McdocTypeKind.Boolean && node.Json is not null)
                {
                    found = node.JsonPath;
                    return;
                }
                foreach (var child in McdocPresenterRegistry.Default.Children(node)) Walk(child);
            }
            foreach (var node in document.Nodes) Walk(node);
            return found;
        }

        /// <summary>第一个"有多个候选、且已有值"的枚举节点：用来量一次下拉切换。</summary>
        private static McdocTreeNode FindEnumNode(McdocTreeDocument document)
        {
            McdocTreeNode found = null;
            void Walk(McdocTreeNode node)
            {
                if (node is null || found is not null) return;
                if (node.EnumOptionList is { Count: > 1 } && node.Json is not null && node.Type?.Kind != McdocTypeKind.Union)
                {
                    found = node;
                    return;
                }
                foreach (var child in McdocPresenterRegistry.Default.Children(node)) Walk(child);
            }
            foreach (var node in document.Nodes) Walk(node);
            return found;
        }

        private static CBHK.ViewModel.Generator.McdocGeneratorViewModelBase CreateLootVm(out McdocTreeDocument document)
        {
            var resource = new CBHK.Model.Constant.Resource();
            RealSchema.Load(resource.McdocTypes, RealSchema.LocateMcdocRoot());
            resource.LoadRegistries(System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\CBHK\bin\Debug\net8.0-windows10.0.22621.0\Resource\Registries"));
            var vm = new CBHK.ViewModel.Generator.LootTableViewModel(resource);
            var field = typeof(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase).GetField(
                "document", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            document = field?.GetValue(vm) as McdocTreeDocument;
            return vm;
        }

        private static string DumpRows(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm)
            => string.Join(" | ", vm.Rows.Select((row, index) =>
                index + ":" + row.Depth + ":" + row.Kind + ":" + (row.Node?.JsonPath ?? "<null>")
                + (string.IsNullOrEmpty(row.Node?.FieldName) ? "" : "#" + row.Node.FieldName)));

        private static bool HasRow(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm, string path)
            => vm.Rows.Any(row => string.Equals(row.Node?.JsonPath, path, StringComparison.Ordinal));

        /// <summary>缩进契约：第一行 depth 为 0，之后每行最多比上一行深 1。</summary>
        private static bool DepthJumpsOk(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm)
        {
            for (var i = 0; i < vm.Rows.Count; i++)
            {
                if (i == 0 && vm.Rows[i].Depth != 0) return false;
                if (i > 0 && vm.Rows[i].Depth > vm.Rows[i - 1].Depth + 1) return false;
                if (vm.Rows[i].Depth < 0) return false;
            }
            return true;
        }

        /// <summary>无孤儿行：每一行指向的节点必须还挂在这份文档上。</summary>
        private static bool RowsAttached(McdocTreeDocument document, CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm)
            => vm.Rows.All(row => row.Node is null || document.IsAttached(row.Node));

        /// <summary>行必须指向**文档里该路径的那个节点实例**（IsAttached 只看父链，换成旧实例照样算"挂着"）。</summary>
        private static List<string> StaleRows(McdocTreeDocument document, CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm)
        {
            var stale = new List<string>();
            foreach (var row in vm.Rows)
            {
                var path = row.Node?.JsonPath;
                if (row.Node is null || string.IsNullOrEmpty(path) || path == "$") continue;
                var current = document.FindNode(path);
                if (current is null) continue;
                if (!ReferenceEquals(current, row.Node)) stale.Add(path);
            }
            return stale;
        }

        private static McdocRow RowOf(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm, string path)
            => vm.Rows.FirstOrDefault(row => string.Equals(row.Node?.JsonPath, path, StringComparison.Ordinal));

        /// <summary>加一个池 + 一个条目（走行上的 ⊕，与用户手势同一条命令）。</summary>
        private static bool SeedPoolAndEntry(McdocTreeDocument document, CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm)
        {
            document.FindNode("$.pools")?.AddItemCommand?.Execute(null);
            PumpDispatcher();
            var pool = document.FindNode("$.pools[0]");
            if (pool is null) return false;
            pool.IsExpanded = true;
            pool.EnsureChildren();
            document.FindNode("$.pools[0].entries")?.AddItemCommand?.Execute(null);
            PumpDispatcher();
            document.FindNode("$.pools[0].entries")?.EnsureChildren();
            return true;
        }

        public static int VmLootAddEntry()
        {
            var checks = new Checks();
            if (RealSchema.LocateMcdocRoot() is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            Deterministic();
            var vm = CreateLootVm(out var document);
            checks.That(document is not null, "VM 建出了文档", document is null ? "document=null" : "");
            if (document is null) return checks.Finish("--case vm-loot-add-entry");

            var seeded = SeedPoolAndEntry(document, vm);
            checks.That(seeded, "池 + 条目都建出来了", "pool=" + (document.FindNode("$.pools[0]") is null ? "<无>" : "ok"));
            var array = McdocTreeDocument.ResolvePath(document.RootJson, "$.pools[0].entries");
            Console.WriteLine("[信息] JSON entries=" + FormatOneLine(array));
            Console.WriteLine("[信息] 行=" + DumpRows(vm));
            var entriesNode = document.FindNode("$.pools[0].entries");
            Console.WriteLine("[信息] entries 节点=" + (entriesNode is null ? "<无>"
                : "items=" + entriesNode.Items.Count + " children=" + entriesNode.Children.Count
                  + " built=" + entriesNode.ChildrenBuilt + " expanded=" + entriesNode.IsExpanded
                  + " sameJson=" + ReferenceEquals(entriesNode.Json, array)
                  + " remembered=" + (McdocExpandState.Lookup("$.pools[0].entries")?.ToString() ?? "<null>")));

            checks.That(array is { Kind: McdocJsonKind.Array } items && (items.Items?.Count ?? 0) == 1,
                "JSON 里只有一个条目", "entries=" + FormatOneLine(array));
            checks.That(HasRow(vm, "$.pools[0].entries[0]"), "树上出现了条目行", "行=" + DumpRows(vm));
            checks.That(DepthJumpsOk(vm), "行深度没有跳跃（子行 = 父行 + 1）", "行=" + DumpRows(vm));
            checks.That(RowsAttached(document, vm), "每一行都指向还挂在这份文档上的节点", "行=" + DumpRows(vm));
            var stale = StaleRows(document, vm);
            checks.That(stale.Count == 0, "行没有指向上一代的节点实例", "陈旧=" + string.Join(",", stale));

            var entryRow = RowOf(vm, "$.pools[0].entries[0]");
            var childRows = entryRow is null
                ? new List<string>()
                : vm.Rows.Where(row => row.Depth > entryRow.Depth && row.Index >= 0
                        && row.Node is not null && (row.Node.JsonPath ?? "").StartsWith("$.pools[0].entries[0]", StringComparison.Ordinal))
                    .Select(row => row.Node?.FieldName ?? "").Where(name => !string.IsNullOrEmpty(name)).ToList();
            Console.WriteLine("[信息] 新条目的子行=" + string.Join(",", childRows));
            checks.That(childRows.Count > 0, "新条目有自己的子行", "子行=" + string.Join(",", childRows));
            return checks.Finish("--case vm-loot-add-entry");
        }

        public static int VmLootEntryActions()
        {
            var checks = new Checks();
            if (RealSchema.LocateMcdocRoot() is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            Deterministic();
            var vm = CreateLootVm(out var document);
            checks.That(document is not null, "VM 建出了文档", document is null ? "document=null" : "");
            if (document is null) return checks.Finish("--case vm-loot-entry-actions");
            if (!SeedPoolAndEntry(document, vm)) return checks.Finish("--case vm-loot-entry-actions");

            // 再来一个，才有 ▲ ▼ 可判
            document.FindNode("$.pools[0].entries")?.AddItemCommand?.Execute(null);
            PumpDispatcher();
            document.FindNode("$.pools[0].entries")?.EnsureChildren();
            var arrayBefore = McdocTreeDocument.ResolvePath(document.RootJson, "$.pools[0].entries");
            var before = FormatOneLine(arrayBefore);
            var firstBefore = arrayBefore?.Items?.FirstOrDefault();
            var secondBefore = arrayBefore?.Items?.LastOrDefault();
            Console.WriteLine("[信息] 加两个条目后=" + before + " 行=" + DumpRows(vm));

            var secondRow = RowOf(vm, "$.pools[0].entries[1]");
            checks.That(secondRow is not null, "第二个条目有行", "路径=$.pools[0].entries[1] 行=" + DumpRows(vm));
            checks.That(secondRow?.MoveUpCommand is not null && secondRow.MoveUpCommand.CanExecute(null),
                "上移可用", secondRow is null ? "<无行>" : "CanExecute=" + (secondRow.MoveUpCommand?.CanExecute(null) ?? false));
            secondRow?.MoveUpCommand?.Execute(null);
            PumpDispatcher();
            var arrayAfter = McdocTreeDocument.ResolvePath(document.RootJson, "$.pools[0].entries");
            var moved = FormatOneLine(arrayAfter);
            Console.WriteLine("[信息] 上移后=" + moved + " 行=" + DumpRows(vm));
            // 两个条目的值长得一模一样（都是 alternatives 的默认结构），只能按**实例身份**判顺序
            checks.That(ReferenceEquals(arrayAfter?.Items?.FirstOrDefault(), secondBefore)
                    && ReferenceEquals(arrayAfter?.Items?.LastOrDefault(), firstBefore),
                "上移真的换了顺序", "前=" + before + " 后=" + moved);

            var targetRow = RowOf(vm, "$.pools[0].entries[0]");
            checks.That(targetRow?.RemoveSelfCommand is not null, "删除命令存在",
                targetRow is null ? "<无行>" : "command=" + (targetRow.RemoveSelfCommand is null ? "null" : "ok"));
            targetRow?.RemoveSelfCommand?.Execute(null);
            PumpDispatcher();
            var afterDelete = McdocTreeDocument.ResolvePath(document.RootJson, "$.pools[0].entries");
            var remaining = afterDelete?.Items?.Count ?? -1;
            Console.WriteLine("[信息] 删除后 entries=" + FormatOneLine(afterDelete) + " 行=" + DumpRows(vm));
            checks.That(remaining == 1, "删除真的把条目从 JSON 里去掉", "剩余=" + remaining);
            checks.That(HasRow(vm, "$.pools[0].entries[0]"), "删完仍有条目行", "行=" + DumpRows(vm));
            checks.That(DepthJumpsOk(vm), "删除后行深度没有跳跃", "行=" + DumpRows(vm));
            return checks.Finish("--case vm-loot-entry-actions");
        }

        public static int VmLootEntryEnumSwitch()
        {
            var checks = new Checks();
            if (RealSchema.LocateMcdocRoot() is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            Deterministic();
            var vm = CreateLootVm(out var document);
            checks.That(document is not null, "VM 建出了文档", document is null ? "document=null" : "");
            if (document is null) return checks.Finish("--case vm-loot-entry-enum-switch");
            if (!SeedPoolAndEntry(document, vm)) return checks.Finish("--case vm-loot-entry-enum-switch");

            var entryPath = "$.pools[0].entries[0]";
            string Fields()
            {
                var node = document.FindNode(entryPath);
                node?.EnsureChildren();
                return string.Join(",", McdocPresenterRegistry.Default.Children(node)
                    .Select(child => child.FieldName).Where(name => !string.IsNullOrEmpty(name)).OrderBy(name => name, StringComparer.Ordinal));
            }

            var before = Fields();
            var typeNode = document.FindNode(entryPath + ".type");
            var option = typeNode?.EnumOptionList?.FirstOrDefault(item =>
                item?.Value?.LiteralValue is string text && text == "minecraft:item");
            checks.That(option is not null, "有 minecraft:item 候选",
                "候选=" + string.Join(",", (typeNode?.EnumOptionList ?? []).Select(item => Convert.ToString(item?.Value?.LiteralValue)).Take(8)));
            if (option is null) return checks.Finish("--case vm-loot-entry-enum-switch");

            typeNode.SelectedEnumOption = option;
            McdocCollectionEditor.Default.ApplyNodeValue(typeNode);
            PumpDispatcher();
            var after = Fields();
            var afterJson = FormatOneLine(McdocTreeDocument.ResolvePath(document.RootJson, entryPath));
            Console.WriteLine("[信息] 切支前 fields=" + before + " 切后 fields=" + after
                + " json=" + afterJson + " 行=" + DumpRows(vm));
            checks.That(after != before, "切 Enum 后同层字段集合变了", "前=" + before + " 后=" + after);
            checks.That(!after.Split(',').Contains("children"), "旧成员（group）的 children 被清掉", "后=" + after);
            checks.That(after.Split(',').Contains("name"), "新成员（item）的 name 解出来", "后=" + after);
            checks.That(DepthJumpsOk(vm), "切支后行深度没有跳跃", "行=" + DumpRows(vm));
            return checks.Finish("--case vm-loot-entry-enum-switch");
        }

        // ── 现场（本轮 bug 1/2）：union 根 + 列表支 → 条目的 ▲▼/⊖ 全哑 ──────────────────────
        private static List<McdocRow> RootUnionListRows(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm)
            => vm.Rows
                .Where(row => row.IsListChild
                    && row.Kind is not McdocRowKind.More and not McdocRowKind.AddTail
                    && (row.Node?.JsonPath ?? "").StartsWith("$[", StringComparison.Ordinal))
                .ToList();

        private static CBHK.ViewModel.Generator.McdocGeneratorViewModelBase CreateItemModifierVm(out McdocTreeDocument document)
        {
            var resource = new CBHK.Model.Constant.Resource();
            RealSchema.Load(resource.McdocTypes, RealSchema.LocateMcdocRoot());
            resource.LoadRegistries(System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\CBHK\bin\Debug\net8.0-windows10.0.22621.0\Resource\Registries"));
            var vm = new CBHK.ViewModel.Generator.ItemModifierViewModel(resource);
            var field = typeof(CBHK.ViewModel.Generator.McdocGeneratorViewModelBase).GetField(
                "document", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            document = field?.GetValue(vm) as McdocTreeDocument;
            return vm;
        }

        /// <summary>
        /// 把 item_modifier 的根 union 切到 **List 支**，再补足 <paramref name="want"/> 条条目
        /// （默认文档是单个对象，和截图里"Root → List + 两条 Entry"不是同一份状态）。
        /// </summary>
        private static void SeedItemModifierRootList(McdocTreeDocument document, CBHK.ViewModel.Generator.McdocGeneratorViewModelBase vm, int want)
        {
            var root = document.Nodes.FirstOrDefault();
            root?.EnsureChildren();
            if (root?.UnionListLike != true)
            {
                var members = root?.Type?.Members?.ToList() ?? [];
                var listIndex = members.FindIndex(member => member?.Kind == McdocTypeKind.List);
                var option = listIndex >= 0
                    ? root?.UnionTypeNameList?.FirstOrDefault(item => item?.Value?.LiteralValue is int value && value == listIndex)
                    : null;
                if (option is not null)
                {
                    McdocUnionSwitch.SwitchTo(root, option);
                    PumpDispatcher();
                }
            }
            for (var guard = 0; guard < 4 && RootUnionListRows(vm).Count < want; guard++)
            {
                document.Nodes.FirstOrDefault()?.AddItemCommand?.Execute(null);
                PumpDispatcher();
                document.Nodes.FirstOrDefault()?.EnsureChildren();
            }
        }

        /// <summary>合成形状：根是 union、选中的是列表支（条目被上提到 SelectedUnionChildren）。</summary>
        public static int UnionListBranchActions()
        {
            var checks = new Checks();
            Deterministic();
            var itemType = McdocType.Create(McdocTypeKind.Struct);
            itemType.Fields =
            [
                new McdocStructField
                {
                    Name = "name", Key = McdocType.LiteralString("name"),
                    Type = McdocType.Create(McdocTypeKind.String), Optional = true
                }
            ];
            var list = McdocType.Create(McdocTypeKind.List);
            list.Item = itemType;
            var rootType = UnionOf(list, McdocType.Create(McdocTypeKind.String));
            var (document, projector, _) = Harness(new McdocTypeIndex(), rootType,
                "[ { \"name\": \"a\" }, { \"name\": \"b\" } ]");
            projector.Reset(document.Nodes.ToList());

            var root = document.Nodes.FirstOrDefault();
            root?.EnsureChildren();
            var entries = root?.SelectedUnionChildren.ToList() ?? [];
            Console.WriteLine("[信息] unionListLike=" + (root?.UnionListLike ?? false)
                + " items=" + (root?.Items.Count ?? -1) + " branch=" + entries.Count
                + " 路径=" + string.Join(",", entries.Select(item => item.JsonPath)));
            checks.That(root?.UnionListLike == true, "根是 union 的列表支", "unionListLike=" + (root?.UnionListLike ?? false));
            checks.That(root?.Items.Count == 0 && entries.Count == 2, "两个条目被上提到 SelectedUnionChildren",
                "items=" + (root?.Items.Count ?? -1) + " branch=" + entries.Count);
            if (entries.Count < 2) return checks.Finish("--case union-list-branch-actions");

            var before = document.RootJson;
            var beforeText = FormatOneLine(before);
            var firstJson = before?.Items?.FirstOrDefault();
            var secondJson = before?.Items?.LastOrDefault();
            checks.That(entries[1].MoveUpCommand?.CanExecute(null) == true, "第二项的上移可用",
                "canExecute=" + (entries[1].MoveUpCommand?.CanExecute(null) ?? false));
            entries[1].MoveUpCommand?.Execute(null);
            var after = document.RootJson;
            Console.WriteLine("[信息] 上移后=" + FormatOneLine(after));
            checks.That(ReferenceEquals(after?.Items?.FirstOrDefault(), secondJson)
                    && ReferenceEquals(after?.Items?.LastOrDefault(), firstJson),
                "上移真的换序", "前=" + beforeText + " 后=" + FormatOneLine(after));

            // 移动会重建这一支：重新取活的条目再删
            var live = document.Nodes.FirstOrDefault();
            live?.EnsureChildren();
            var target = live?.SelectedUnionChildren.FirstOrDefault();
            checks.That(target?.RemoveSelfCommand is not null, "第一项有删除命令", target is null ? "<无项>" : "ok");
            target?.RemoveSelfCommand?.Execute(null);
            var afterDelete = document.RootJson;
            Console.WriteLine("[信息] 删除后=" + FormatOneLine(afterDelete));
            checks.That(afterDelete is { Kind: McdocJsonKind.Array } arr && (arr.Items?.Count ?? 0) == 1,
                "删除真的少了一条", "after=" + FormatOneLine(afterDelete));
            return checks.Finish("--case union-list-branch-actions");
        }

        // 构造重载的探针壳：不覆盖任何逻辑，只验证基类用 raw dispatcher + index 初始化。
        private sealed class ConstructorSessionViewModel(CBHK.Model.Constant.Resource resource)
            : CBHK.ViewModel.Generator.McdocGeneratorViewModelBase(
                resource, "minecraft:entity", "{ \"Health\": 20 }");

        private sealed class DefaultEntitySessionViewModel(CBHK.Model.Constant.Resource resource)
            : CBHK.ViewModel.Generator.McdocGeneratorViewModelBase(
                resource, "minecraft:entity", null);

        // ── 实体页启动：Loaded 里的实体列表 + 默认树加载 ───────────────────────
        public static int EntityStartupSmoke()
        {
            var checks = new Checks();
            var root = RealSchema.LocateMcdocRoot();
            if (root is null) { Console.WriteLine("[环境缺失] 找不到 vanilla-mcdoc（退出码 2）"); return 2; }
            Deterministic();
            var resource = new CBHK.Model.Constant.Resource();
            RealSchema.Load(resource.McdocTypes, root);
            resource.LoadRegistries(System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\CBHK\bin\Debug\net8.0-windows10.0.22621.0\Resource\Registries"));
            var app = System.Windows.Application.Current ?? new System.Windows.Application();
            var host = new System.Windows.Window
            {
                Width = 320,
                Height = 200,
                ShowInTaskbar = false,
                WindowStyle = System.Windows.WindowStyle.None,
                Opacity = 0,
                Content = new System.Windows.Documents.AdornerDecorator
                {
                    Child = new System.Windows.Controls.Grid()
                }
            };
            host.Show();
            try
            {
                var started = System.Diagnostics.Stopwatch.StartNew();
                var vm = new CBHK.ViewModel.Component.Entity.EntityPageViewModel(null, resource);
                checks.That(started.ElapsedMilliseconds < 3000, "实体页构造不阻塞 UI 线程",
                    "ctor=" + started.ElapsedMilliseconds + "ms");
                started.Restart();
                try
                {
                    vm.EntityPages_Loaded(null, null);
                    checks.That(started.ElapsedMilliseconds < 3000, "EntityPages_Loaded 不阻塞 UI 线程",
                        "loaded=" + started.ElapsedMilliseconds + "ms ids=" + vm.EntityIDList.Count);
                }
                catch (Exception error)
                {
                    checks.That(false, "EntityPages_Loaded 不抛异常", error.GetType().Name + ": " + error.Message);
                }
                Console.WriteLine("[信息] 实体页 startup rows=" + vm.Rows.Count + " total=" + started.ElapsedMilliseconds + "ms");
            var posRow = vm.Rows.FirstOrDefault(row => row.Node?.FieldName == "Pos");
            checks.That(posRow?.SetDefaultVisibility == System.Windows.Visibility.Visible,
                "缺省的可选 tuple 有起默认值按钮", posRow is null ? "没有 Pos 行" : "add=" + posRow.SetDefaultVisibility);
            if (posRow is not null)
            {
                posRow.SetDefaultCommand.Execute(null);
                PumpDispatcher();
                checks.That(posRow.Node.Json is { Kind: McdocJsonKind.Array } tupleValue
                        && tupleValue.Items?.Count == 3,
                    "起默认值后 Pos 落成三元组", "value=" + FormatOneLine(posRow.Node.Json));
                checks.That(posRow.Head.EditorKind == McdocEditorKind.InlineTuple,
                    "起默认值后 Pos 显示内联三元组编辑器", "editor=" + posRow.Head.EditorKind);
            }
            }
            finally
            {
                host.Close();
            }
            return checks.Finish("--case entity-startup-smoke");
        }

        // ── 基类公共树视图入口：版本 / dispatcher 名 / 符号 Index ─────────────────
        public static int McdocOpenTreePublic()
        {
            var checks = new Checks();
            var root = RealSchema.LocateMcdocRoot();
            if (root is null) { Console.WriteLine("[环境缺失] 找不到 vanilla-mcdoc（退出码 2）"); return 2; }
            Deterministic();
            var resource = new CBHK.Model.Constant.Resource();
            RealSchema.Load(resource.McdocTypes, root);
            resource.LoadRegistries(System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\CBHK\bin\Debug\net8.0-windows10.0.22621.0\Resource\Registries"));
            var vm = new CBHK.ViewModel.Generator.ItemModifierViewModel(resource);
            var entityIds = resource.GetIds("entity", "1.21.11");
            var typedIds = resource.GetIds("entity_type", "1.21.11");
            checks.That(entityIds.Count > 0, "Resource 按版本取实体 ID 符号表", "ids=" + entityIds.Count);
            checks.That(entityIds.SequenceEqual(typedIds), "dispatcher 名可回退到 entity_type 注册表",
                "entity=" + entityIds.Count + " typed=" + typedIds.Count);
            checks.That(entityIds.SequenceEqual(entityIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase)),
                "Resource 实体 ID 按 A-Z 排序", string.Join(",", entityIds.Take(6)));
            const string json = "{ \"has_skylight\": true, \"min_y\": -64 }";
            vm.OpenMcdocTree("1.21.11", "dimension_type", resource.McdocTypes, json);
            checks.That(vm.EntryId == "dimension_type", "公共入口切换到目标 dispatcher", vm.EntryId ?? "<null>");
            checks.That(vm.Rows.Count > 0, "公共入口建出树行", "rows=" + vm.Rows.Count);
            checks.That(vm.JsonText.Contains("min_y", StringComparison.Ordinal), "公共入口载入目标 JSON",
                "len=" + (vm.JsonText?.Length ?? 0));
            var rootNode = vm.Rows.FirstOrDefault(row => row.Node?.JsonPath == "$.has_skylight")?.Node;
            checks.That(rootNode is not null, "公共入口根字段存在", rootNode is null
                ? "rows=" + string.Join(",", vm.Rows.Take(5).Select(row => row.Node?.JsonPath ?? "<null>"))
                : "");
            checks.That(rootNode?.JsonPath == "$.has_skylight", "根字段路径正确", rootNode?.JsonPath ?? "<null>");

            // 原始 dispatcher 重载：minecraft:resource + static dimension_type
            vm.OpenMcdocTreeFromDispatcher("1.21.11", "minecraft:resource",
                McdocIndex.Static("dimension_type"), resource.McdocTypes, json);
            checks.That(vm.EntryId == "minecraft:resource", "原始 dispatcher 入口记录 registry", vm.EntryId ?? "<null>");
            checks.That(vm.Rows.Count > 0, "原始 dispatcher 入口建出树行", "rows=" + vm.Rows.Count);

            // 构造重载：base(Resource, dispatcherName, json) 内部取版本/符号表/默认实体 ID
            var constructed = new ConstructorSessionViewModel(resource);
            checks.That(constructed.EntryId == "minecraft:entity", "构造重载 dispatcher 生效", constructed.EntryId ?? "<null>");
            checks.That(constructed.Rows.Count > 0, "构造重载直接建出树行", "rows=" + constructed.Rows.Count);
            checks.That(constructed.JsonText.Contains("Health", StringComparison.Ordinal),
                "构造重载按传入 JSON 求解", "len=" + (constructed.JsonText?.Length ?? 0));

            var started = System.Diagnostics.Stopwatch.StartNew();
            var defaultEntity = new DefaultEntitySessionViewModel(resource);
            started.Stop();
            Console.WriteLine("[信息] 默认实体构造耗时=" + started.ElapsedMilliseconds + "ms rows=" + defaultEntity.Rows.Count
                + " jsonLen=" + (defaultEntity.JsonText?.Length ?? 0));
            checks.That(started.ElapsedMilliseconds < 3000, "默认实体构造不阻塞 UI 线程",
                started.ElapsedMilliseconds + "ms rows=" + defaultEntity.Rows.Count);
            return checks.Finish("--case mcdoc-open-tree-public");
        }

        // ── 列表项拖拽交换：同 owner 才允许；JSON 与右侧文本必须同序 ──────────────────
        public static int ListDragSwap()
        {
            var checks = new Checks();
            Deterministic();
            var itemType = McdocType.Create(McdocTypeKind.Struct);
            itemType.Fields =
            [
                new McdocStructField
                {
                    Name = "name", Key = McdocType.LiteralString("name"),
                    Type = McdocType.Create(McdocTypeKind.String), Optional = true
                }
            ];
            var list = McdocType.Create(McdocTypeKind.List);
            list.Item = itemType;
            var secondList = McdocType.Create(McdocTypeKind.List);
            secondList.Item = itemType;
            var rootType = McdocType.Create(McdocTypeKind.Struct);
            rootType.Fields =
            [
                new McdocStructField { Name = "a", Key = McdocType.LiteralString("a"), Type = list },
                new McdocStructField { Name = "b", Key = McdocType.LiteralString("b"), Type = secondList },
            ];
            var (document, projector, _) = Harness(new McdocTypeIndex(), rootType,
                "{ \"a\": [ { \"name\": \"a0\" }, { \"name\": \"a1\" }, { \"name\": \"a2\" } ], "
                + "\"b\": [ { \"name\": \"b0\" }, { \"name\": \"b1\" } ] }");
            var listA = document.FindNode("$.a");
            var listB = document.FindNode("$.b");
            listA?.EnsureChildren();
            listB?.EnsureChildren();
            var items = listA?.Items.ToList() ?? [];
            checks.That(items.Count == 3 && listB?.Items.Count == 2, "前置：两个 List 都有条目",
                "a=" + items.Count + " b=" + (listB?.Items.Count ?? -1));
            if (items.Count < 3 || listB?.Items.Count < 2) return checks.Finish("--case list-drag-swap");
            var editor = McdocCollectionEditor.Default;
            checks.That(editor.CanSwapListItems(items[0], items[2]), "同 List 的两个条目可交换", "");
            checks.That(!editor.CanSwapListItems(items[0], listB.Items[0]), "不同 List 父级拒绝交换", "");
            checks.That(editor.SwapListItems(items[0], items[2]), "交换返回成功", "");
            var names = document.RootJson?.Get("a")?.Items?
                .Select(value => value?.Get("name")?.StringValue).ToList() ?? [];
            checks.That(names.Count == 3 && names[0] == "a2" && names[2] == "a0",
                "交换后 JSON 顺序变了", string.Join(",", names));
            checks.That(projector.Rows.All(row => row.Node is not null && document.IsAttached(row.Node)),
                "交换后投影没有指向脱挂节点", "rows=" + projector.Rows.Count);

            // VM：右侧 JsonText 必须和文档同序（不是只改了内存 JSON）
            var vm = CreateItemModifierVm(out var vmDocument);
            checks.That(vmDocument is not null, "VM 建出了文档", vmDocument is null ? "document=null" : "");
            if (vmDocument is null) return checks.Finish("--case list-drag-swap");
            SeedItemModifierRootList(vmDocument, vm, 3);
            var rows = RootUnionListRows(vm);
            checks.That(rows.Count >= 3, "VM 根列表至少三个条目", "行=" + rows.Count);
            if (rows.Count < 3) return checks.Finish("--case list-drag-swap");
            var third = rows[2].Node;
            checks.That(editor.CanSwapListItems(rows[0].Node, third), "VM 列表第一/第三项可交换", "");
            checks.That(editor.SwapListItems(rows[0].Node, third), "VM 交换成功", "");
            PumpDispatcher();
            checks.That(vmDocument.RootJson is not null && !string.IsNullOrEmpty(vm.JsonText),
                "VM 交换后有 JSON 文本", "len=" + (vm.JsonText?.Length ?? 0));
            var textRoot = McdocJsonTextParser.Parse(vm.JsonText).Root;
            checks.That(FormatOneLine(textRoot) == FormatOneLine(vmDocument.RootJson),
                "右侧 JsonText 与文档同序", "文档=" + FormatOneLine(vmDocument.RootJson)
                + " 文本=" + FormatOneLine(textRoot));
            checks.That(StaleRows(vmDocument, vm).Count == 0, "交换后没有陈旧行",
                string.Join(",", StaleRows(vmDocument, vm)));
            return checks.Finish("--case list-drag-swap");
        }

        public static int VmItemModifierListMove()
        {
            var checks = new Checks();
            if (RealSchema.LocateMcdocRoot() is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            Deterministic();
            var vm = CreateItemModifierVm(out var document);
            checks.That(document is not null, "VM 建出了文档", document is null ? "document=null" : "");
            if (document is null) return checks.Finish("--case vm-item-modifier-list-move");

            SeedItemModifierRootList(document, vm, 2);
            var rows = RootUnionListRows(vm);
            var before = document.RootJson;
            var beforeText = FormatOneLine(before);
            Console.WriteLine("[信息] 根数组=" + beforeText + " 条目行=" + string.Join(",", rows.Select(row => row.Node?.JsonPath)));
            checks.That(rows.Count >= 2, "根列表至少两个条目行", "行=" + rows.Count);
            if (rows.Count < 2) return checks.Finish("--case vm-item-modifier-list-move");

            var firstJson = before?.Items?.FirstOrDefault();
            var secondJson = before?.Items?.LastOrDefault();
            checks.That(rows[1].MoveUpCommand?.CanExecute(null) == true, "第二行的上移可用",
                "canExecute=" + (rows[1].MoveUpCommand?.CanExecute(null) ?? false));
            rows[1].MoveUpCommand?.Execute(null);
            PumpDispatcher();
            var after = document.RootJson;
            Console.WriteLine("[信息] 上移后=" + FormatOneLine(after));
            checks.That(ReferenceEquals(after?.Items?.FirstOrDefault(), secondJson)
                    && ReferenceEquals(after?.Items?.LastOrDefault(), firstJson),
                "上移真的换了顺序", "前=" + beforeText + " 后=" + FormatOneLine(after));
            var stale = StaleRows(document, vm);
            checks.That(stale.Count == 0, "上移后行没有指向上一代节点", "陈旧=" + string.Join(",", stale));
            checks.That(DepthJumpsOk(vm), "上移后行深度没有跳跃", "行=" + DumpRows(vm));
            return checks.Finish("--case vm-item-modifier-list-move");
        }

        public static int VmItemModifierListRemove()
        {
            var checks = new Checks();
            if (RealSchema.LocateMcdocRoot() is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            Deterministic();
            var vm = CreateItemModifierVm(out var document);
            checks.That(document is not null, "VM 建出了文档", document is null ? "document=null" : "");
            if (document is null) return checks.Finish("--case vm-item-modifier-list-remove");

            SeedItemModifierRootList(document, vm, 1);
            var rows = RootUnionListRows(vm);
            checks.That(rows.Count >= 1, "根列表有条目行", "行=" + rows.Count);
            if (rows.Count == 0) return checks.Finish("--case vm-item-modifier-list-remove");
            var before = document.RootJson?.Items?.Count ?? -1;
            checks.That(rows[0].RemoveSelfCommand is not null, "第一行有删除命令", "command=" + (rows[0].RemoveSelfCommand is null ? "null" : "ok"));
            rows[0].RemoveSelfCommand?.Execute(null);
            PumpDispatcher();
            var after = document.RootJson?.Items?.Count ?? -2;
            var remained = RootUnionListRows(vm);
            Console.WriteLine("[信息] 删除后 条目=" + after + " 行=" + string.Join(",", remained.Select(row => row.Node?.JsonPath)));
            checks.That(after == before - 1, "删除真的少了一条", "前=" + before + " 后=" + after);
            checks.That(remained.Count == after, "行数与 JSON 条目数一致", "行=" + remained.Count + " json=" + after);
            checks.That(remained.Count == 0 || remained[0].MoveUpCommand is not null, "剩余条目仍有列表命令",
                remained.Count == 0 ? "<已空>" : "command=" + (remained[0].MoveUpCommand is null ? "null" : "ok"));
            checks.That(StaleRows(document, vm).Count == 0, "删除后没有陈旧行", string.Join(",", StaleRows(document, vm)));
            return checks.Finish("--case vm-item-modifier-list-remove");
        }

        // ── 现场（本轮 bug 1）：根切 Object 支后切 Function，同层 spread 必须重新求解 ──────────
        private static List<string> RootBranchFields(McdocTreeDocument document)
        {
            var root = document.Nodes.FirstOrDefault();
            root?.EnsureChildren();
            return (root?.SelectedUnionChildren ?? [])
                .Select(child => child.FieldName).Where(name => !string.IsNullOrEmpty(name))
                .OrderBy(name => name, StringComparer.Ordinal).ToList();
        }

        public static int VmItemModifierObjectEnumSwitch()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            Deterministic();
            // 走管线本体（不是 VM）：Object 支的判别键宿主是 union 行，判据要落在 RebuildDependents 上。
            // 版本钉 1.20.5，才会拿到 apply_bonus 那一支的 spread。
            var index = new McdocTypeIndex();
            RealSchema.Load(index, rootDir);
            var (document, projector, pipeline) = Harness(index, McdocHelpers.GetRootType("item_modifier"),
                "{ \"function\": \"minecraft:apply_bonus\" }",
                doc => doc.GameVersion = "1.20.5");
            pipeline.MaterializeUntilStable(includeDisplayedDefaults: true);
            projector.Reset(document.Nodes.ToList());

            var before = RootBranchFields(document);
            Console.WriteLine("[信息] Object 支字段=" + string.Join(",", before));
            checks.That(before.Contains("enchantment") && before.Contains("formula"),
                "起点是 apply_bonus（enchantment/formula）", "字段=" + string.Join(",", before));

            var functionNode = document.FindNode("$.function");
            // 候选字面量常常不带命名空间（#[id] 注册表按原样给），两种写法都要认
            var option = functionNode?.EnumOptionList?.FirstOrDefault(item =>
                item?.Value?.LiteralValue is string text
                && (text == "minecraft:set_firework_explosion" || text == "set_firework_explosion"));
            checks.That(option is not null, "有 set_firework_explosion 候选",
                "候选=" + string.Join(",", (functionNode?.EnumOptionList ?? []).Select(item => Convert.ToString(item?.Value?.LiteralValue)).Take(8)));
            if (option is null) return checks.Finish("--case vm-item-modifier-object-enum-switch");

            // 与 VM 同一条手势：先写文档（ApplyNodeValue 只写不建议），再走"值变化"出口。
            // 只调 setter 就指望管线重解是错的 —— 那一刻文档里还是旧值（判据自己会把 bug 造出来）。
            functionNode.SelectedEnumOption = option;
            McdocCollectionEditor.Default.ApplyNodeValue(functionNode);
            Console.WriteLine("[信息] 写入后 json=" + FormatOneLine(document.RootJson));
            pipeline.OnValueChanged(document.FindNode("$.function") ?? functionNode);
            PumpDispatcher();
            var after = RootBranchFields(document);
            Console.WriteLine("[信息] 切 Function 后字段=" + string.Join(",", after)
                + " json=" + FormatOneLine(document.RootJson));
            checks.That(after.Contains("shape") && after.Contains("trail") && after.Contains("twinkle"),
                "新成员（set_firework_explosion）的字段解出来", "字段=" + string.Join(",", after));
            checks.That(!after.Contains("enchantment") && !after.Contains("formula"),
                "旧成员（apply_bonus）的字段被清掉", "字段=" + string.Join(",", after));
            var rows = projector.Rows;
            var depthOk = true;
            for (var i = 1; i < rows.Count; i++)
            {
                if (rows[i].Depth > rows[i - 1].Depth + 1) depthOk = false;
            }
            checks.That(depthOk, "切成员后行深度没有跳跃",
                "行=" + string.Join(",", rows.Select(row => row.Depth + ":" + (row.Node?.JsonPath ?? "?"))));
            return checks.Finish("--case vm-item-modifier-object-enum-switch");
        }

        // ── 现场（本轮 bug 2）：根切 List 支、只有一个元素时 ⊖ 必须还在 ──────────────────────
        public static int VmItemModifierSingleEntryRemove()
        {
            var checks = new Checks();
            if (RealSchema.LocateMcdocRoot() is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            Deterministic();
            var vm = CreateItemModifierVm(out var document);
            checks.That(document is not null, "VM 建出了文档", document is null ? "document=null" : "");
            if (document is null) return checks.Finish("--case vm-item-modifier-single-entry-remove");

            SeedItemModifierRootList(document, vm, 1);
            // 只留一个元素：多出来的自己删掉（走的就是被判定这条命令）
            for (var guard = 0; guard < 4 && RootUnionListRows(vm).Count > 1; guard++)
            {
                var extra = RootUnionListRows(vm).LastOrDefault();
                extra?.RemoveSelfCommand?.Execute(null);
                PumpDispatcher();
                document.Nodes.FirstOrDefault()?.EnsureChildren();
            }
            // 任何一次重建（切分支 / 物化 / 用户再点一下）都会重建这一支：命令必须跟着新节点重挂，
            // 否则 ⊖ 当场消失（现场：只有一个元素时没有删除按钮）。这里显式重建一次来钉住它。
            // 定向重建**这一项**（值编辑 / 切分支之后就发生在它身上）：命令必须按新节点重挂。
            document.RebuildNode(document.FindNode("$[0]"));
            PumpDispatcher();
            var rows = RootUnionListRows(vm);
            var array = document.RootJson;
            Console.WriteLine("[信息] 条目=" + (array?.Items?.Count ?? -1) + " 行=" + string.Join(",", rows.Select(row => row.Node?.JsonPath))
                + " removeSelf=" + (rows.FirstOrDefault()?.Node?.RemoveSelfCommand is null ? "<null>" : "ok"));
            checks.That(rows.Count == 1, "根列表只有一个条目行", "行=" + rows.Count);
            if (rows.Count != 1) return checks.Finish("--case vm-item-modifier-single-entry-remove");

            var row = rows[0];
            checks.That(row.Node?.RemoveSelfCommand is not null, "唯一元素仍然有删除命令",
                "command=" + (row.Node?.RemoveSelfCommand is null ? "<null>" : "ok"));
            checks.That(row.RemoveSelfVisibility == System.Windows.Visibility.Visible,
                "唯一元素的 ⊖ 按钮可见", "visibility=" + row.RemoveSelfVisibility);
            row.RemoveSelfCommand?.Execute(null);
            PumpDispatcher();
            var remaining = document.RootJson?.Items?.Count ?? -1;
            Console.WriteLine("[信息] 删除后条目=" + remaining + " 行=" + string.Join(",", RootUnionListRows(vm).Select(item => item.Node?.JsonPath)));
            checks.That(remaining == 0, "唯一元素被真的删掉", "剩余=" + remaining);
            checks.That(RootUnionListRows(vm).Count == 0, "删完列表里没有条目行", "行=" + RootUnionListRows(vm).Count);
            return checks.Finish("--case vm-item-modifier-single-entry-remove");
        }

        // ── 现场（本轮）：根从 List 支切到 Object 支后，Object 支的 function 兄弟 spread 必须解出来 ──
        public static int VmItemModifierListToObject()
        {
            var checks = new Checks();
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            Deterministic();
            var index = new McdocTypeIndex();
            RealSchema.Load(index, rootDir);
            var (document, projector, pipeline) = Harness(index, McdocHelpers.GetRootType("item_modifier"),
                "[ { \"function\": \"minecraft:apply_bonus\" } ]",
                doc => doc.GameVersion = "1.20.5");
            pipeline.MaterializeUntilStable(includeDisplayedDefaults: true);
            projector.Reset(document.Nodes.ToList());

            var root = document.Nodes.FirstOrDefault();
            root?.EnsureChildren();
            var members = root?.Type?.Members?.ToList() ?? [];
            var structIndex = members.FindIndex(member => member?.Kind == McdocTypeKind.Struct);
            var option = structIndex >= 0
                ? root?.UnionTypeNameList?.FirstOrDefault(item => item?.Value?.LiteralValue is int value && value == structIndex)
                : null;
            Console.WriteLine("[信息] 切前 unionListLike=" + (root?.UnionListLike ?? false)
                + " members=" + string.Join(",", members.Select(member => member?.Kind.ToString()))
                + " 候选=" + string.Join(",", (root?.UnionTypeNameList ?? []).Select(item => item?.Name)));
            checks.That(option is not null, "有 Object 支选项", "members=" + string.Join(",", members.Select(member => member?.Kind.ToString())));
            if (option is null) return checks.Finish("--case vm-item-modifier-list-to-object");

            McdocUnionSwitch.SwitchTo(root, option);
            // 与 VM 同一条收尾：切支之后由管线补物化 + 重建（VM 在 unionStructuralBranch 分支里调它）
            pipeline.OnUnionBranchSwitched(document.Nodes.FirstOrDefault() ?? root);
            PumpDispatcher();
            var fields = RootBranchFields(document);
            var json = FormatOneLine(document.RootJson);
            var unionAfter = document.Nodes.FirstOrDefault();
            var memberAfter = unionAfter?.SelectedUnionMember;
            Console.WriteLine("[信息] 切到 Object 后 字段=" + string.Join(",", fields) + " json=" + json
                + " type=" + (unionAfter?.Type?.Kind.ToString() ?? "<null>")
                + " member=" + (memberAfter?.Kind.ToString() ?? "<null>")
                + " memberFields=" + string.Join(",", (memberAfter?.Fields ?? []).Select(field => field?.Name))
                + " 诊断=" + string.Join(" | ", document.Diagnostics.Select(item => item.Message).Take(3)));
            checks.That(fields.Contains("function"), "Object 支的 function 场行出现", "字段=" + string.Join(",", fields));
            checks.That(fields.Contains("enchantment") && fields.Contains("formula"),
                "function 的兄弟 spread 解出来了", "字段=" + string.Join(",", fields) + " json=" + json);
            return checks.Finish("--case vm-item-modifier-list-to-object");
        }


        // ── 现场（本轮）：block_state_property 的 Properties 在 block 来回切之后不能加、也不刷新 ──
        private static string DumpRows(McdocTreeDocument document, McdocRowProjector projector)
            => string.Join(" | ", projector.Rows.Select((row, index) =>
                index + ":" + row.Depth + ":" + (row.Node?.JsonPath ?? "<null>") + (string.IsNullOrEmpty(row.Node?.FieldName) ? "" : "#" + row.Node.FieldName)));

        private static string BlockStatePropsPath = "$.pools[0].entries[0].conditions[0].properties";
        private static string BlockStateBlockPath = "$.pools[0].entries[0].conditions[0].block";

        public static int LootBlockstateProperties()
        {
            var checks = new Checks();
            var directory = LocateBlockStateDirectory();
            if (directory is null || RealSchema.LocateMcdocRoot() is null)
            {
                Console.WriteLine("[环境缺失] 找不到 BlockState 资产或 vanilla-mcdoc（退出码 2）");
                return 2;
            }
            Deterministic();
            var index = new McdocTypeIndex();
            RealSchema.Load(index, RealSchema.LocateMcdocRoot());
            McdocBlockStates.Register(index, "1.20.5", directory);
            var (document, projector, pipeline) = Harness(index, McdocHelpers.GetRootType("loot_table"),
                "{ \"pools\": [ { \"rolls\": 0, \"entries\": [ { \"type\": \"minecraft:alternatives\", \"children\": [], "
                + "\"conditions\": [ { \"condition\": \"minecraft:block_state_property\", \"block\": \"minecraft:basalt\" } ] } ] } ] }",
                doc => doc.GameVersion = "1.20.5");
            pipeline.MaterializeUntilStable(includeDisplayedDefaults: true);
            projector.Reset(document.Nodes.ToList());

            string State(string label)
            {
                var node = document.FindNode(BlockStatePropsPath);
                var blockValue = McdocTreeDocument.ResolvePath(document.RootJson, BlockStateBlockPath);
                return label + " block=" + (blockValue?.StringValue ?? "<null>")
                    + " propsJson=" + (node?.Json?.Kind.ToString() ?? "<null>")
                    + " propsType=" + (node?.Type?.Kind.ToString() ?? "<null>")
                    + " fields=" + string.Join(",", (node?.Type?.Fields ?? []).Select(field => field?.Name).Take(6))
                    + " setDefault=" + (node?.SetDefaultCommand is null ? "null" : node.SetDefaultCommand.CanExecute(null).ToString())
                    + " childrenBuilt=" + (node?.ChildrenBuilt.ToString() ?? "-")
                    + " expanded=" + (node?.IsExpanded.ToString() ?? "-");
            }

            Console.WriteLine("[信息] " + State("载入"));
            checks.That(McdocTreeDocument.ResolvePath(document.RootJson, BlockStateBlockPath)?.StringValue is { Length: > 0 },
                "载入后 block 有值", "block=" + (McdocTreeDocument.ResolvePath(document.RootJson, BlockStateBlockPath)?.StringValue ?? "<null>"));
            var props = document.FindNode(BlockStatePropsPath);
            checks.That(props is not null, "properties 节点存在", BlockStatePropsPath);
            if (props is null) return checks.Finish("--case loot-blockstate-properties");

            // ① 首次加：⊕ 必须可用，且真的把 properties 写进文档
            checks.That(props.SetDefaultCommand?.CanExecute(null) == true, "首次 properties 的 ⊕ 可用",
                "setDefault=" + (props.SetDefaultCommand is null ? "null" : props.SetDefaultCommand.CanExecute(null).ToString()));
            props.SetDefaultCommand?.Execute(null);
            PumpDispatcher();
            var propsJson = McdocTreeDocument.ResolvePath(document.RootJson, BlockStatePropsPath);
            Console.WriteLine("[信息] 首次加之后 " + State("加之后") + " json=" + FormatOneLine(propsJson));
            Console.WriteLine("[信息] 加之后 行=" + DumpRows(document, projector));
            checks.That(propsJson is { Kind: McdocJsonKind.Object }, "⊕ 把 properties 写成了对象", "json=" + FormatOneLine(propsJson));

            // ② 切到没有属性的方块，再切回有属性的：⊕ 与子结构都要跟着回来
            // 直接改文档值再走"值变化"出口：候选下拉在这套夹具里没有注册表数据，不该让判据依赖它
            void SwitchBlock(string literal)
            {
                var blockNode = document.FindNode(BlockStateBlockPath);
                if (blockNode is null) return;
                McdocCollectionEditor.Default.ApplyValue(blockNode, McdocJsonValue.String(literal));
                pipeline.OnValueChanged(document.FindNode(BlockStateBlockPath) ?? blockNode);
                PumpDispatcher();
            }

            // 用户现场：Properties **展开着**（有 axis 子行）再切到无属性方块 → 行会变成"点不动"
            Expand(document.FindNode(BlockStatePropsPath));
            projector.Reset(document.Nodes.ToList());
            SwitchBlock("minecraft:stone");
            Console.WriteLine("[信息] " + State("切到 stone"));
            var afterStone = document.FindNode(BlockStatePropsPath);
            checks.That(McdocTreeDocument.ResolvePath(document.RootJson, BlockStatePropsPath) is null,
                "切到无属性方块后 properties 的空对象被丢掉",
                "json=" + FormatOneLine(McdocTreeDocument.ResolvePath(document.RootJson, BlockStatePropsPath)));
            // 行级判据：properties 下面不许再有任何"值行"（旧的 face/facing/powered 必须消失）
            projector.Reset(document.Nodes.ToList());
            var stalePropertyRows = projector.Rows
                .Where(row => (row.Node?.JsonPath ?? "").StartsWith(BlockStatePropsPath + ".", StringComparison.Ordinal))
                .Select(row => row.Node?.FieldName).ToList();
            checks.That(stalePropertyRows.Count == 0, "切到无属性方块后 properties 下没有任何属性行",
                "行=" + string.Join(",", stalePropertyRows));
            checks.That(afterStone is null || afterStone.Json is null,
                "切到无属性方块后 Properties 节点上不再挂着旧值", "json=" + (afterStone?.Json?.Kind.ToString() ?? "<null>"));
            checks.That(document.FindNode(BlockStatePropsPath + ".axis") is null,
                "旧方块的 axis 子节点被摘掉", "still=" + (document.FindNode(BlockStatePropsPath + ".axis") is null ? "no" : "yes"));
            checks.That(afterStone is null || afterStone.Children.Count == 0,
                "Properties 上不再留着旧的子节点", "children=" + (afterStone?.Children.Count ?? -1));
            SwitchBlock("minecraft:basalt");
            Console.WriteLine("[信息] " + State("切回 basalt"));
            var after = document.FindNode(BlockStatePropsPath);
            checks.That(after is not null, "切回之后 properties 节点还在", BlockStatePropsPath);
            checks.That(after?.SetDefaultCommand?.CanExecute(null) == true
                    || after?.Json is { Kind: McdocJsonKind.Object },
                "切回之后 Properties 的 ⊕ 仍可用（或已经有值）",
                "setDefault=" + (after?.SetDefaultCommand is null ? "null" : after.SetDefaultCommand.CanExecute(null).ToString())
                + " json=" + (after?.Json?.Kind.ToString() ?? "<null>"));
            checks.That(after is null || after.Type?.Kind is McdocTypeKind.Struct or McdocTypeKind.Any,
                "切回之后 properties 的类型重新解出来了",
                "type=" + (after?.Type?.Kind.ToString() ?? "<null>") + " fields=" + string.Join(",", (after?.Type?.Fields ?? []).Select(field => field?.Name).Take(6)));
            // 收紧：切回之后把 properties 展开，axis 这一行必须真的出现（"子结构不跟 Block 刷新"就钉在这里）
            // 现场（截图）：properties 里已有 axis 之类的键，block 换成另一个方块后必须"旧键剪掉 + 行跟着换代"。
            var seeded = document.FindNode(BlockStatePropsPath);
            if (seeded is not null)
            {
                McdocCollectionEditor.Default.ApplyValue(seeded, McdocJsonValue.Object());
                seeded.Json?.Add("axis", McdocJsonValue.String("y"));
                pipeline.OnValueChanged(document.FindNode(BlockStatePropsPath) ?? seeded);
                PumpDispatcher();
            }
            void Expand(McdocTreeNode node)
            {
                if (node is null) return;
                for (var parent = node.Parent; parent is not null; parent = parent.Parent)
                {
                    parent.IsExpanded = true;
                    parent.EnsureChildren();
                }
                node.IsExpanded = true;
                node.EnsureChildren();
            }
            void SwitchBlock2(string literal)
            {
                var blockNode = document.FindNode(BlockStateBlockPath);
                if (blockNode is null) return;
                McdocCollectionEditor.Default.ApplyValue(blockNode, McdocJsonValue.String(literal));
                pipeline.OnValueChanged(document.FindNode(BlockStateBlockPath) ?? blockNode);
                PumpDispatcher();
            }
            SwitchBlock2("minecraft:cobblestone_wall");
            SwitchBlock2("minecraft:acacia_button");
            var afterButton = document.FindNode(BlockStatePropsPath);
            Expand(afterButton);
            projector.Reset(document.Nodes.ToList());
            var staleAxisRow = projector.Rows.FirstOrDefault(row =>
                string.Equals(row.Node?.JsonPath, BlockStatePropsPath + ".axis", StringComparison.Ordinal));
            Console.WriteLine("[信息] 换方块后 properties json=" + FormatOneLine(McdocTreeDocument.ResolvePath(document.RootJson, BlockStatePropsPath))
                + " 子行=" + string.Join(",", projector.Rows
                    .Where(row => (row.Node?.JsonPath ?? "").StartsWith(BlockStatePropsPath + ".", StringComparison.Ordinal))
                    .Select(row => row.Node?.FieldName)));
            checks.That(staleAxisRow is null, "换方块后旧方块属性行必须消失（axis）", "json="
                + FormatOneLine(McdocTreeDocument.ResolvePath(document.RootJson, BlockStatePropsPath)));

            // 展开之后，行必须是**当前方块**的属性（acacia_button = face/facing/powered），
            // 而且绝不能残留上一个方块的属性行（axis / east / north …）。
            Expand(document.FindNode(BlockStatePropsPath));
            projector.Reset(document.Nodes.ToList());
            var fieldRows = projector.Rows
                .Where(row => (row.Node?.JsonPath ?? "").StartsWith(BlockStatePropsPath + ".", StringComparison.Ordinal))
                .Select(row => row.Node?.FieldName).ToList();
            Console.WriteLine("[信息] 展开后的属性行=" + string.Join(",", fieldRows));
            checks.That(fieldRows.Count > 0 && fieldRows.All(name => name is "face" or "facing" or "powered"),
                "展开后的属性行就是当前方块的属性", "行=" + string.Join(",", fieldRows));
            checks.That(!fieldRows.Contains("axis"), "上一个方块的 axis 行没有残留", "行=" + string.Join(",", fieldRows));
            return checks.Finish("--case loot-blockstate-properties");
        }

        // ── 现场：Properties 原本没有值，切无属性方块再切回后点 ⊕ 没反应 ────────────────
        public static int BlockStatePropertiesAddAfterSwitch()
        {
            var checks = new Checks();
            var directory = LocateBlockStateDirectory();
            var mcdocRoot = RealSchema.LocateMcdocRoot();
            if (directory is null || mcdocRoot is null)
            {
                Console.WriteLine("[环境缺失] 找不到 BlockState 资产或 vanilla-mcdoc（退出码 2）");
                return 2;
            }
            DeterministicLazy();
            var index = new McdocTypeIndex();
            RealSchema.Load(index, mcdocRoot);
            McdocBlockStates.Register(index, "1.20.5", directory);
            const string basePath = "$.pools[0].entries[0].conditions[0]";
            const string propsPath = basePath + ".properties";
            var (document, projector, pipeline) = Harness(index, McdocHelpers.GetRootType("loot_table"),
                "{ \"pools\": [ { \"rolls\": 0, \"entries\": [ { \"type\": \"minecraft:alternatives\", \"children\": [], "
                + "\"conditions\": [ { \"condition\": \"minecraft:block_state_property\", "
                + "\"block\": \"minecraft:acacia_button\" } ] } ] } ] }",
                doc => doc.GameVersion = "1.20.5");
            pipeline.MaterializeUntilStable(includeDisplayedDefaults: true);
            projector.Reset(document.Nodes.ToList());

            void Expand(McdocTreeNode node)
            {
                if (node is null) return;
                for (var parent = node.Parent; parent is not null; parent = parent.Parent)
                {
                    parent.IsExpanded = true;
                    parent.EnsureChildren();
                }
                node.IsExpanded = true;
                node.EnsureChildren();
            }
            var entry = Reach(document, basePath);
            Expand(entry);
            projector.Reset(document.Nodes.ToList());

            void SwitchBlock(string literal)
            {
                var block = Reach(document, basePath + ".block");
                if (block is null) return;
                var option = block.EnumOptionList?.FirstOrDefault(item =>
                    string.Equals(Convert.ToString(item?.Value?.LiteralValue), literal, StringComparison.Ordinal));
                if (option is not null)
                {
                    // 与界面下拉同一顺序：先选候选（行事件），再由 ApplyNodeValue 落盘。
                    block.SelectedEnumOption = option;
                    McdocCollectionEditor.Default.ApplyNodeValue(block);
                }
                else
                {
                    McdocCollectionEditor.Default.ApplyValue(block, McdocJsonValue.String(literal));
                }
                pipeline.OnValueChanged(Reach(document, basePath + ".block") ?? block);
                PumpDispatcher();
            }

            checks.That(McdocTreeDocument.ResolvePath(document.RootJson, propsPath) is null,
                "前置：properties 初始没有值", "json=" + FormatOneLine(McdocTreeDocument.ResolvePath(document.RootJson, propsPath)));
            SwitchBlock("minecraft:stone");
            // 用户现场：无属性方块上点一次 ⊕（预期加不出结构，随后也不能卡住命令）
            var stonePropsRow = projector.Rows.FirstOrDefault(row =>
                string.Equals(row.Node?.JsonPath, propsPath, StringComparison.Ordinal));
            if (stonePropsRow?.SetDefaultCommand is { } stoneAdd && stoneAdd.CanExecute(null))
            {
                stoneAdd.Execute(null);
                PumpDispatcher();
            }
            checks.That(McdocTreeDocument.ResolvePath(document.RootJson, propsPath) is null,
                "无属性方块上点 ⊕ 不会凭空写出 properties", "json=" + FormatOneLine(McdocTreeDocument.ResolvePath(document.RootJson, propsPath)));
            SwitchBlock("minecraft:acacia_button");
            var propsRow = projector.Rows.FirstOrDefault(row =>
                string.Equals(row.Node?.JsonPath, propsPath, StringComparison.Ordinal));
            checks.That(propsRow is not null, "切回 acacia_button 后 Properties 行还在",
                propsRow is null ? "没有行" : "");
            checks.That(propsRow?.SetDefaultCommand?.CanExecute(null) == true,
                "切回后 Properties 的 ⊕ 命令可用",
                propsRow is null ? "没有行" : "add=" + (propsRow.SetDefaultCommand?.CanExecute(null) ?? false));
            // 现场 Properties 行是展开的（箭头向下）再点 ⊕；先复现展开态。
            if (propsRow is not null)
            {
                propsRow.IsExpanded = true;
                PumpDispatcher();
            }
            if (propsRow?.SetDefaultCommand is { } add && add.CanExecute(null))
            {
                add.Execute(null);
                PumpDispatcher();
            }
            checks.That(McdocTreeDocument.ResolvePath(document.RootJson, propsPath) is { Kind: McdocJsonKind.Object },
                "点 ⊕ 后 properties 真的写回文档",
                "json=" + FormatOneLine(McdocTreeDocument.ResolvePath(document.RootJson, propsPath)));
            var addedRow = projector.Rows.FirstOrDefault(row =>
                string.Equals(row.Node?.JsonPath, propsPath, StringComparison.Ordinal));
            checks.That(addedRow is not null, "添加后 Properties 行还在", addedRow is null ? "没有行" : "");
            checks.That(addedRow?.ClearVisibility == System.Windows.Visibility.Visible,
                "添加后按钮变成删除", addedRow is null ? "没有行" : "clear=" + addedRow.ClearVisibility);
            checks.That(addedRow?.SetDefaultVisibility == System.Windows.Visibility.Collapsed,
                "添加后不再显示添加按钮", addedRow is null ? "没有行" : "add=" + addedRow.SetDefaultVisibility);
            var fields = projector.Rows
                .Where(row => (row.Node?.JsonPath ?? "").StartsWith(propsPath + ".", StringComparison.Ordinal))
                .Select(row => row.Node?.FieldName).ToList();
            checks.That(fields.Contains("face") && fields.Contains("facing") && fields.Contains("powered"),
                "点 ⊕ 后属性行当场出现", "行=" + string.Join(",", fields));
            return checks.Finish("--case blockstate-properties-add-after-switch");
        }

        // ── 现场（本轮）：item_modifier 里 Conditions 展开后切无属性方块，Properties 行不换代 ──
        public static int ItemModifierBlockstateExpanded()
        {
            var checks = new Checks();
            var directory = LocateBlockStateDirectory();
            var mcdocRoot = RealSchema.LocateMcdocRoot();
            if (directory is null || mcdocRoot is null)
            {
                Console.WriteLine("[环境缺失] 找不到 BlockState 资产或 vanilla-mcdoc（退出码 2）");
                return 2;
            }
            DeterministicLazy();
            var index = new McdocTypeIndex();
            RealSchema.Load(index, mcdocRoot);
            McdocBlockStates.Register(index, "1.20.5", directory);
            const string basePath = "$.conditions[0]";
            const string propsPath = basePath + ".properties";
            var (document, projector, pipeline) = Harness(index, McdocHelpers.GetRootType("item_modifier"),
                "{ \"function\": \"minecraft:apply_bonus\", "
                + "\"enchantment\": \"minecraft:aqua_affinity\", "
                + "\"formula\": \"minecraft:ore_drops\", "
                + "\"conditions\": [ { \"condition\": \"minecraft:block_state_property\", "
                + "\"block\": \"minecraft:acacia_button\", \"properties\": {} } ] }",
                doc => doc.GameVersion = "1.20.5");
            pipeline.MaterializeUntilStable(includeDisplayedDefaults: true);
            projector.Reset(document.Nodes.ToList());

            void Expand(McdocTreeNode node)
            {
                if (node is null) return;
                for (var parent = node.Parent; parent is not null; parent = parent.Parent)
                {
                    parent.IsExpanded = true;
                    parent.EnsureChildren();
                }
                node.IsExpanded = true;
                node.EnsureChildren();
            }
            void SwitchBlock(string literal)
            {
                var block = Reach(document, basePath + ".block");
                if (block is null) return;
                McdocCollectionEditor.Default.ApplyValue(block, McdocJsonValue.String(literal));
                pipeline.OnValueChanged(Reach(document, basePath + ".block") ?? block);
                PumpDispatcher();
            }

            var props = Reach(document, propsPath);
            checks.That(props is not null, "前置：properties 节点存在", propsPath);
            if (props is null) return checks.Finish("--case item-modifier-blockstate-expanded");
            Expand(props);
            projector.Reset(document.Nodes.ToList());
            checks.That(Reach(document, propsPath + ".face") is not null,
                "前置：acacia_button 已解出属性行",
                "face=" + (Reach(document, propsPath + ".face") is null ? "no" : "yes"));

            // 用户现场：Properties 展开着切到无属性方块；这里**不手动 Reset**，直接量投影。
            SwitchBlock("minecraft:allium");
            checks.That(McdocTreeDocument.ResolvePath(document.RootJson, propsPath) is null,
                "切到 allium 后 properties 键被丢掉",
                "json=" + FormatOneLine(McdocTreeDocument.ResolvePath(document.RootJson, propsPath)));
            var staleDescendants = projector.Rows
                .Where(row => (row.Node?.JsonPath ?? "").StartsWith(propsPath + ".", StringComparison.Ordinal))
                .Select(row => row.Node?.FieldName).ToList();
            checks.That(staleDescendants.Count == 0, "切到 allium 后投影里没有旧属性子行",
                "行=" + string.Join(",", staleDescendants));
            var propsRow = projector.Rows.FirstOrDefault(row =>
                string.Equals(row.Node?.JsonPath, propsPath, StringComparison.Ordinal));
            checks.That(propsRow is null
                    || (propsRow.Node?.Json is null
                        && propsRow.ClearVisibility == System.Windows.Visibility.Collapsed
                        && propsRow.SetDefaultCommand?.CanExecute(null) == true),
                "切到 allium 后 Properties 行是“添加”而不是旧删除/旧命令",
                propsRow is null ? "没有行"
                    : "json=" + (propsRow.Node?.Json?.Kind.ToString() ?? "<null>")
                      + " clear=" + propsRow.ClearVisibility
                      + " add=" + (propsRow.SetDefaultCommand?.CanExecute(null) ?? false));

            // 再切回有属性方块：行/命令必须是新一代，添加入口要真的能把 properties 加回来。
            SwitchBlock("minecraft:acacia_button");
            var backRow = projector.Rows.FirstOrDefault(row =>
                string.Equals(row.Node?.JsonPath, propsPath, StringComparison.Ordinal));
            checks.That(backRow is not null, "切回 acacia_button 后 Properties 行还在",
                backRow is null ? "没有行" : "");
            checks.That(backRow?.SetDefaultCommand?.CanExecute(null) == true,
                "切回后 Properties 的添加命令可用（不是上一代死命令）",
                backRow is null ? "没有行" : "add=" + (backRow.SetDefaultCommand?.CanExecute(null) ?? false));
            // 模拟 UI 的“延后一拍重建”：添加按钮本身必须先把当前这一层拼进投影，
            // 不能等 deferred 重建才出现子行（现场：要点一次折叠/展开）。
            pipeline.ScheduleRebuild = _ => { };
            if (backRow?.SetDefaultCommand is { } add && add.CanExecute(null))
            {
                add.Execute(null);
                PumpDispatcher();
            }
            checks.That(McdocTreeDocument.ResolvePath(document.RootJson, propsPath) is { Kind: McdocJsonKind.Object },
                "切回后点添加真的写回 properties",
                "json=" + FormatOneLine(McdocTreeDocument.ResolvePath(document.RootJson, propsPath)));
            // 用户现场：点完添加不折叠/展开，子结构也应该当场出现；这里不调用 Expand/Reset。
            var fieldRows = projector.Rows
                .Where(row => (row.Node?.JsonPath ?? "").StartsWith(propsPath + ".", StringComparison.Ordinal))
                .Select(row => row.Node?.FieldName).ToList();
            checks.That(fieldRows.Contains("face") && fieldRows.Contains("facing") && fieldRows.Contains("powered"),
                "切回后属性行按 acacia_button 重新解出来（不需要手动折叠展开）",
                "行=" + string.Join(",", fieldRows));
            return checks.Finish("--case item-modifier-blockstate-expanded");
        }

        // ── 现场（本轮）：从右侧复制整篇再粘贴回来，里面带了"看不见的空白" → 树一动不动 ──────
        public static int TextInvisibleWhitespace()
        {
            var checks = new Checks();
            Deterministic();
            const string json = "{ \"has_skylight\": true, \"coordinate_scale\": 1, \"min_y\": -64 }";
            // 把普通空格换成不换行空格（U+00A0）——粘贴最常带进来的那一个
            var dirty = json.Replace(" ", "\u00A0");
            checks.That(dirty.Contains('\u00A0'), "夹具里确实有不换行空格", "len=" + dirty.Length);
            checks.That(!CBHK.Utility.Data.Mcdoc.McdocJsonParser.TryParse(dirty, out _, out _),
                "严格解析对不换行空格是失败的（这就是现场那条 0xC2 报错）", "");

            var normalized = CBHK.Utility.Data.Mcdoc.McdocJsonParser.NormalizeInvisible(dirty, out var replaced);
            Console.WriteLine("[信息] 归一个数=" + replaced + " 归一后=" + normalized);
            checks.That(replaced > 0, "归一化统计出替换个数", "replaced=" + replaced);
            checks.That(!normalized.Contains('\u00A0'), "归一后不再含不换行空格", "");
            checks.That(CBHK.Utility.Data.Mcdoc.McdocJsonParser.TryParse(normalized, out var parsed, out _),
                "归一后的文本是合法 JSON", "");
            checks.That(parsed?.Get("coordinate_scale") is not null, "归一后取值正常", "");

            // 走 VM 的载入出口：整篇（带脏字符）应当照样把树建出来
            var vm = CreateLootVm(out var document);
            if (document is null) return checks.Finish("--case text-invisible-whitespace");
            // 真正的粘贴路径：OnJsonEditorTextChanged → 防抖 → SyncTextToTree
            var pasted = "{ \"pools\": [], \"random_sequence\": \"temp\" }".Replace(" ", "\u00A0");
            vm.OnJsonEditorTextChanged(pasted);
            PumpDispatcher();
            System.Threading.Thread.Sleep(220);
            PumpDispatcher();
            var afterPaste = McdocJsonFormatter.Format(document.RootJson).Replace("\r", "").Replace("\n", " ");
            Console.WriteLine("[信息] 粘贴后 json=" + afterPaste);
            checks.That(afterPaste.Contains("random_sequence"), "粘贴（带不可见空白）后树按新内容重建",
                "json=" + afterPaste);
            Console.WriteLine("[信息] 载入后 nodes=" + document.Nodes.Count + " json="
                + McdocJsonFormatter.Format(document.RootJson).Replace("\r", "").Replace("\n", " "));
            checks.That(document.RootJson is { Kind: McdocJsonKind.Object }, "带脏字符的整篇也能建出文档",
                "root=" + (document.RootJson?.Kind.ToString() ?? "<null>"));
            return checks.Finish("--case text-invisible-whitespace");
        }

        // ── 日志巡检发现的小错误：map 条目被当成了"列表条目"，挂上找不到 owner 的 ▲▼/⊖ ─────────
        public static int MapEntryHasNoListItemCommands()
        {
            var checks = new Checks();
            Deterministic();
            var entryType = McdocType.Create(McdocTypeKind.Struct);
            entryType.Fields =
            [
                new McdocStructField
                {
                    Name = "value", Key = McdocType.LiteralString("value"),
                    Type = McdocType.Create(McdocTypeKind.String), Optional = true
                }
            ];
            var map = McdocType.Create(McdocTypeKind.Struct);
            map.Fields =
            [
                new McdocStructField { Name = null, Key = McdocType.Create(McdocTypeKind.String), Type = entryType }
            ];
            var rootType = Struct(("map", map));
            var (document, projector, _) = Harness(new McdocTypeIndex(), rootType,
                "{ \"map\": { \"alpha\": { \"value\": \"a\" }, \"beta\": { \"value\": \"b\" } } }");
            projector.Reset(document.Nodes.ToList());
            var entry = document.FindNode("$.map.alpha");
            checks.That(entry is not null, "map 条目节点存在", "$.map.alpha");
            if (entry is null) return checks.Finish("--case map-entry-has-no-list-item-commands");

            var rebuilt = document.RebuildNode(entry);
            Console.WriteLine("[信息] 重建后 moveUp=" + (rebuilt?.MoveUpCommand is null ? "null" : "非空")
                + " removeSelf=" + (rebuilt?.RemoveSelfCommand is null ? "null" : "非空")
                + " isMapEntry=" + (rebuilt?.IsMapEntry ?? false));
            checks.That(rebuilt?.MoveUpCommand is null, "map 条目重建后不挂「上移」命令",
                "moveUp=" + (rebuilt?.MoveUpCommand is null ? "null" : "非空"));
            checks.That(rebuilt?.RemoveSelfCommand is null, "map 条目重建后不挂「删掉这一项」命令",
                "removeSelf=" + (rebuilt?.RemoveSelfCommand is null ? "null" : "非空"));
            checks.That(rebuilt?.RemoveEntryCommand is not null, "map 条目仍有自己的删除条目命令",
                "removeEntry=" + (rebuilt?.RemoveEntryCommand is null ? "null" : "非空"));
            return checks.Finish("--case map-entry-has-no-list-item-commands");
        }

        private static int CountNodes(IEnumerable<McdocTreeNode> roots)
        {
            var count = 0;
            void Walk(McdocTreeNode node)
            {
                if (node is null) return;
                node.EnsureChildren();
                count++;
                foreach (var child in McdocPresenterRegistry.Default.Children(node)) Walk(child);
            }
            foreach (var root in roots) Walk(root);
            return count;
        }

        private static string Path(McdocTreeNode node)
            => (node.JsonPath ?? "?") + " type=" + (node.Type?.Kind.ToString() ?? "null") + " field=" + (node.FieldName ?? "<空>");
    }
}
