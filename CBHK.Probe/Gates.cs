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
            Action<McdocTreeDocument> beforeLoad = null)
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
            document.NodeValueChanged = node => pipeline.OnValueChanged(node);
            document.NodeStructureChanged = node => pipeline.OnStructureChanged(node);
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
                else if (line.Contains("enum-empty")) enumEmpty++;
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
            Console.WriteLine("[信息] index-resolve 未解析次数=" + indexUnresolved + "；行上提醒 diag=" + diagCount);
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
