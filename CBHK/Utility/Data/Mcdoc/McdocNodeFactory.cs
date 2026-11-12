using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MinecraftLanguageModelLibrary.Data;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>一次构建共享的节点预算（跨子树累计，不是按路径深度）。</summary>
    internal sealed class McdocNodeBudget
    {
        private int remaining;
        public McdocNodeBudget(int limit) => remaining = limit;
        public bool TryConsume() => remaining-- > 0;
    }

    /// <summary>
    /// 节点工厂：只管"求值 + 构建策略（深度 / 预算 / 环）+ 交给表现器生成子级"。
    /// 原来 BuildChildren 的五个 switch 分支已移入表现器族（McdocPresenters.cs）。
    /// </summary>
    public sealed class McdocNodeFactory
    {
        /// <summary>递归上限：mcdoc 允许自引用结构，视图侧必须设硬上限。</summary>
        public const int MaxDepth = 32;

        /// <summary>缺省结构是"一个字段带出整棵子树"的指数展开，必须有总闸。</summary>
        public const int MaxNodes = 3000;

        /// <summary>懒建子级：默认展开深度以内的节点仍然急切构建，深处的节点等展开再建。</summary>
        public static bool LazyChildren { get; set; } = true;

        /// <summary>默认展开深度（misode 是全展开；这里是性能取向的 WPF 偏差，文档里记录）。</summary>
        public static int DefaultExpandDepth { get; set; } = 3;

        private readonly McdocPresenterRegistry registry;

        public McdocNodeFactory(McdocPresenterRegistry registry = null)
            => this.registry = registry ?? McdocPresenterRegistry.Default;

        public McdocPresenterRegistry Registry => registry;

        /// <summary>最近一次 Build 造出的合成根节点：摊平后它不进 Nodes，但仍是顶层节点的父容器。</summary>
        public McdocTreeNode RootNode { get; private set; }

        /// <summary>只构建根节点（不摊平）。narrow=false 供"纯 simplify"对拍使用。</summary>
        public McdocTreeNode BuildRoot(McdocType rootType, McdocJsonValue json, McdocRuntime runtime,
            bool narrow = true, Action<McdocTreeNode> onValueChanged = null, Action<McdocTreeNode> onStructureChanged = null)
            => CreateNode("root", rootType, json, runtime, null, null, 0, narrow, onValueChanged, "$", null, onStructureChanged);

        public ObservableCollection<McdocTreeNode> Build(McdocType rootType, string jsonText, McdocRuntime runtime,
            Action<McdocTreeNode> onValueChanged = null, Action<McdocTreeNode> onStructureChanged = null)
            => Build(rootType, McdocJsonParser.Parse(jsonText), runtime, onValueChanged, onStructureChanged);

        public ObservableCollection<McdocTreeNode> Build(McdocType rootType, McdocJsonValue json, McdocRuntime runtime,
            Action<McdocTreeNode> onValueChanged = null, Action<McdocTreeNode> onStructureChanged = null)
        {
            var result = new ObservableCollection<McdocTreeNode>();
            var rootNode = CreateNode("root", rootType, json, runtime, null, null, 0, true, onValueChanged, "$", null, onStructureChanged);
            RootNode = rootNode;

            // 与 misode 的 McdocRoot 一致：根是 struct 且节点是 object 时，直接取子级，不再多包一层
            if (registry.For(rootNode.Type).FlattensRoot && rootNode.Children.Count > 0)
            {
                foreach (var child in rootNode.Children) result.Add(child);
                return result;
            }
            result.Add(rootNode);
            return result;
        }

        /// <summary>
        /// 求值前把"必填 + 有闭合候选 + 文档里没值"的字段写进 JSON（misode 的必填默认值语义）。
        /// 候选走 IdMembers 那条出口（IdSpec → IdSpaces）：用演示节点问 presenter 的方法在 union 标量支上
        /// 拿不到 IdMembers，会静默什么都不写（type_specific 展开后没有 variant 就是这么留下的）。
        /// </summary>
        private static int MaterializeDiscriminators(McdocType structType, McdocJsonValue json, McdocRuntime runtime)
        {
            var fields = structType?.Fields;
            if (fields is null || fields.Count == 0) return 0;
            var written = 0;
            foreach (var field in fields)
            {
                if (field is null || field.Optional || field.IsSpread) continue;
                var name = field.Name;
                if (string.IsNullOrEmpty(name)) continue;
                // JSON 里写的是 null 也算"没有值"：不补的话同层 spread 拿到 %none，
                // 整支只出 type 一行（现场：weighted_list 条目里 data.type = null）。
                var existing = json.Get(name);
                // 空串同样算"没有值"：McdocDefaults 对必填字符串给的就是 ""，把它当成"有值"
                // 会让判别键永远不落盘（[[type]] 读成 %none → 同层 spread 一个成员都解不出来）。
                var absent = existing is null
                    || existing.Kind == McdocJsonKind.Null
                    || (existing.Kind == McdocJsonKind.String && string.IsNullOrWhiteSpace(existing.StringValue));
                if (!absent) continue;
                var literal = FirstCandidateLiteral(field.Type, runtime);
                if (string.IsNullOrEmpty(literal)) continue;
                if (existing is null)
                {
                    json.Add(name, McdocJsonValue.String(literal));
                }
                else
                {
                    for (var i = 0; i < json.Properties.Count; i++)
                    {
                        if (json.Properties[i].Key != name) continue;
                        json.Properties[i] = new KeyValuePair<string, McdocJsonValue>(name, McdocJsonValue.String(literal));
                        break;
                    }
                }
                written++;
            }
            return written;
        }

        /// <summary>按 #[id] 的注册表候选取首项（自身 + union 标量支都试）。</summary>
        /// <summary>
        /// 沿**声明**找"判别键挂哪个结构"，全程不求值：Mapped 按 mapping 换形参、Concrete/Template 拆壳、
        /// Reference 查符号表、Union 按 JSON 形状取结构成员。
        /// 不能先求值再找：泛型实参里带 [[type]] 时，求值会先把它读成 %none，整支烤成 any，
        /// 那时连"哪个字段是判别键"都看不到了（现场：data.type = null）。
        /// </summary>
        private static McdocType DeclaredDiscriminatorHost(McdocType declaredType, McdocJsonValue json, McdocRuntime runtime)
            => WalkDeclaredHost(declaredType, json, runtime, null, null, 0);

        private static Dictionary<string, McdocType> MergeBindings(Dictionary<string, McdocType> bindings)
            => bindings is null
                ? new Dictionary<string, McdocType>(StringComparer.Ordinal)
                : new Dictionary<string, McdocType>(bindings, StringComparer.Ordinal);

        private static McdocType WalkDeclaredHost(McdocType declaredType, McdocJsonValue json, McdocRuntime runtime,
            Dictionary<string, McdocType> bindings, List<McdocType> pendingArgs, int depth)
        {
            if (declaredType is null || depth > 12) return null;
            switch (declaredType.Kind)
            {
                case McdocTypeKind.Struct:
                    return declaredType;
                case McdocTypeKind.Mapped:
                {
                    var merged = MergeBindings(bindings);
                    foreach (var pair in declaredType.Mapping ?? []) merged[pair.Key] = pair.Value;
                    return WalkDeclaredHost(declaredType.Item, json, runtime, merged, null, depth + 1);
                }
                case McdocTypeKind.Concrete:
                    // 实参登记一次，交给紧接着的 Template 绑形参（与求值期的 TypeArgs 同一条规则）
                    return WalkDeclaredHost(declaredType.Item, json, runtime, bindings, declaredType.Items, depth + 1);
                case McdocTypeKind.Indexed:
                    // Indexed 不清实参（README §2.3：Concrete{Item=Indexed(Dispatcher)} 靠它传参）
                    return WalkDeclaredHost(declaredType.Item, json, runtime, bindings, pendingArgs, depth + 1);
                case McdocTypeKind.Template:
                {
                    var merged = MergeBindings(bindings);
                    var parameters = declaredType.TypeParams ?? [];
                    for (var i = 0; i < parameters.Count && i < (pendingArgs?.Count ?? 0); i++)
                    {
                        merged[parameters[i]] = pendingArgs[i];
                    }
                    return WalkDeclaredHost(declaredType.Item, json, runtime, merged, null, depth + 1);
                }
                case McdocTypeKind.Reference:
                {
                    if (bindings is not null && declaredType.Path is { Length: > 0 } name
                        && bindings.TryGetValue(name, out var bound))
                    {
                        return WalkDeclaredHost(bound, json, runtime, bindings, pendingArgs, depth + 1);
                    }
                    McdocType target = null;
                    try
                    {
                        target = string.IsNullOrEmpty(declaredType.Path)
                            ? null
                            : runtime?.Symbols?.GetTypeDef(declaredType.Path, runtime.DocumentUri ?? "",
                                string.IsNullOrEmpty(declaredType.Module) ? runtime.ModulePath : declaredType.Module);
                    }
                    catch (Exception)
                    {
                        // 符号表对相对名/未绑定形参不保证返回 null，可能直接抛：预写只是启发式，查不到就算了
                        target = null;
                    }
                    return target is null
                        ? null
                        : WalkDeclaredHost(target, json, runtime, bindings, pendingArgs, depth + 1);
                }
                case McdocTypeKind.Union:
                {
                    if (json is not { Kind: McdocJsonKind.Object }) return null;
                    foreach (var member in declaredType.Members ?? [])
                    {
                        var host = WalkDeclaredHost(member, json, runtime, bindings, pendingArgs, depth + 1);
                        if (host is { Kind: McdocTypeKind.Struct }) return host;
                    }
                    return null;
                }
                default:
                    return null;
            }
        }
        /// <summary>按**声明**把"必填 + 有闭合候选"的判别键先写进 JSON。</summary>
        private static int WriteDeclarationDiscriminators(McdocType declaredType, McdocJsonValue json, McdocRuntime runtime)
        {
            if (declaredType is null || json is null) return 0;
            var host = DeclaredDiscriminatorHost(declaredType, json, runtime);
            if (host?.Kind != McdocTypeKind.Struct) return 0;
            return MaterializeDiscriminators(host, json, runtime);
        }

        private static string FirstCandidateLiteral(McdocType type, McdocRuntime runtime)
        {
            if (type is null || runtime?.IdSpaces is null) return null;
            foreach (var candidate in WithUnionMembers(type, member => runtime?.Attributes is null
                || runtime.Attributes.ShouldKeep(member.Attributes, runtime)))
            {
                var spec = McdocIdSpec.FromAttributes(candidate.Attributes);
                if (spec is null || string.IsNullOrEmpty(spec.Registry)) continue;
                // 不看 IsSelect：那只是"闭合下拉 vs 可输入"的分派，决定的是编辑器形态，
                // 不是"有没有候选"。调度器空间（int_provider_type 这类）永远不在闭合白名单里，
                // 但它的候选正是判别键要用的那个（物化阶段用的是同一条 IdSpaces 出口）。
                var resolved = runtime.IdSpaces.Resolve(spec, runtime);
                if (resolved?.Members is { Count: > 0 }) return resolved.Members[0];
            }
            return null;

            static IEnumerable<McdocType> WithUnionMembers(McdocType type, Func<McdocType, bool> applies)
            {
                // 模板字段替换后 #[id] 常常被 Mapped/Concrete/Indexed/Template 包着：
                // 只看顶层 Attributes 会整条漏掉（候选取不到，判别键就不写）。
                var current = type;
                for (var depth = 0; current is not null && depth < 8; depth++)
                {
                    yield return current;
                    if (!current.IsDirective) break;
                    current = current.Item;
                }
                if (type.Kind != McdocTypeKind.Union) yield break;
                foreach (var member in type.Members ?? [])
                {
                    if (member is null) continue;
                    // union 的成员常常是**同一字段的版本变体**（infiniburn 就有 4 支，各自的 #[id] 语义不同：
                    // implicit 允许方块 id、required 只认 tag）。不问版本就把第一支的候选写进文档，
                    // 会得到一个"当前版本不合法"的默认值（现场 infiniburn=minecraft:acacia_button + 值不在候选内）。
                    if (!applies(member)) continue;
                    yield return member;
                }
            }
        }


        internal McdocTreeNode CreateNode(string name, McdocType declaredType, McdocJsonValue json,
            McdocRuntime runtime, McdocValueNode parent, McdocRuntimeNode keyNode, int depth,
            bool narrow = true, Action<McdocTreeNode> onValueChanged = null, string jsonPath = "$",
            List<McdocType> visiting = null, Action<McdocTreeNode> onStructureChanged = null,
            McdocNodeBudget budget = null, bool deferChildren = false, bool unknownField = false)
        {
            var runtimeNode = new McdocRuntimeNode(json, json?.InferType() ?? McdocType.Any());
            var self = McdocValueNode.Of(runtimeNode, parent, keyNode);

            // 判别键必须在**第一次求值之前**落盘：结构常被 Concrete/Reference/Template/Union 包着，
            // 等求值完再写就晚了 —— 求值期 [[type]] 已经读成 %none，字段的闭合候选一起丢了
            // （现场：distribution[0].data = { "type": null } 时同层 spread 永远不出现）。
            if (json is { Kind: McdocJsonKind.Object } earlyJson)
            {
                // 判别键必须在这份 JSON 上、且赶在第一次求值前写进去
                WriteDeclarationDiscriminators(declaredType, earlyJson, runtime);
            }
            var simplifyContext = new McdocSimplifyContext
            {
                Runtime = runtime,
                Node = self,
                Tag = (McdocSimplifier.IndexTrace is null ? null : name + "@" + jsonPath),
            };
            McdocType type;
            var dependsOnRuntimeData = false;
            string evaluationError = null;
            try
            {
                var simplified = McdocSimplifier.Simplify(declaredType, simplifyContext);
                type = simplified.TypeDef;
                dependsOnRuntimeData = simplified.DynamicData;
                if (simplifyContext.Diagnostics.Count > 0)
                {
                    // 一条链上可能连挂十几条：行上只留前 5 条，剩下的给个计数（ToolTip 撑爆比丢信息更糟）
                    var distinctDiagnostics = simplifyContext.Diagnostics.Distinct().ToList();
                    evaluationError = string.Join("；", distinctDiagnostics.Take(5))
                        + (distinctDiagnostics.Count > 5 ? "；…等 " + distinctDiagnostics.Count + " 条" : "");
                }
                // union 保留（UI 上要能切换分支），默认分支由 UnionNodePresenter 挑
            }
            catch (Exception error)
            {
                // 单个字段求值失败不应拖垮整棵树，但要在行上露出来（misode 的 ErrorIndicator）
                type = McdocType.Any();
                evaluationError = error.Message;
            }
            // 派发器/动态索引是在**求值期**读同层取值的：判别键（必填 + 闭合候选）必须在求值前就在 JSON 里，
            // 否则 spread 解析成 %none，父结构"看着渲染完了、其实成员一个都没解出来"（type_specific 没有 variant）。
            // union 也要预写：data 这种"必选 type + 同层 spread"的节点，求值结果是 union，
            // 判别键没值（缺失或 null）时 spread 直接解成 %none，而那时已经无法再靠物化补（没有候选）。
            // 取**按形状选中的结构成员**来预写，写完用同一份 JSON 重求值一次。
            if (json is { Kind: McdocJsonKind.Object } objectJson
                && type?.Kind == McdocTypeKind.Struct
                && MaterializeDiscriminators(type, objectJson, runtime) > 0)
            {
                try
                {
                    var again = new McdocSimplifyContext { Runtime = runtime, Node = self };
                    type = McdocSimplifier.Simplify(declaredType, again).TypeDef;
                }
                catch (Exception)
                {
                    // 重求值失败就保留第一轮结果：宁可行上挂诊断，不静默把树改坏
                }
            }

            if (type?.IsDirective == true)
            {
                // 映射替换可能把"待求值的声明"原样交回来（刻意保留的泛型实参）：在**本节点**再求值一次，
                // 而不是在替换点提前求值（那会打乱泛型实参的作用域）。
                try
                {
                    type = McdocSimplifier.Simplify(type, simplifyContext).TypeDef;
                }
                catch (Exception)
                {
                    // 保持原样，下面按指令降级处理
                }
            }
            if (type?.IsDirective == true)
            {
                // NF 强制：求值后不允许指令逃到节点上（渲染层只认归一形态）。
                // 换成 any 并保留诊断——行会变成"原始 JSON 编辑 + 错误徽标"，而不是把 Reference/Template 当标量文本框。
                evaluationError ??= "未解开的类型包装：" + type.Kind;
                type = McdocType.Any();
            }
            if (evaluationError is null && type?.Kind == McdocTypeKind.Union && (type.Members?.Count ?? 0) == 0)
            {
                evaluationError = "文档里该字段声明为 ()：没有结构（存在性谓词）";
            }
            // 声明/求值是容器、文档里却是标量（或反过来）：这是**非法文档**，树上必须给"诚实的形状" ——
            // 一行原始 JSON + 错误诊断，绝不按声明类型摊开子级（否则会长出"父是字符串、子却在展开"的幽灵行）。
            // 容器之间也会错配（声明 Struct、文档却是数组 —— 现场：loot_table 根是一份"函数数组"，
            // 引擎照样把 type/pools/functions/random_sequence 四个字段摊在一份数组上，行全是幽灵行）。
            var containerMismatch = json is not null && type is not null && IsContainerKind(type.Kind)
                && !ContainerMatchesJson(type.Kind, json.Kind);
            if (json is not null && type is not null && IsContainerKind(type.Kind)
                && (IsScalarJson(json.Kind) || containerMismatch))
            {
                evaluationError ??= "值与类型不符：期望 " + type.Kind + "，实际 " + json.Kind + "（在右侧 JSON 修正）";
                type = McdocType.Any();
            }
            else if (json is not null && type is not null && IsScalarKind(type.Kind)
                && json.Kind is McdocJsonKind.Object or McdocJsonKind.Array)
            {
                evaluationError ??= "值与类型不符：期望 " + type.Kind + "，实际 " + json.Kind + "（在右侧 JSON 修正）";
                type = McdocType.Any();
            }

            // misode：定长 list 当 tuple 处理（每个下标独立成节点）
            if (McdocHelpers.IsFixedList(type) && McdocHelpers.AsFixedTuple(type) is { } fixedTuple)
            {
                type = fixedTuple;
            }

            var node = new McdocTreeNode
            {
                FieldName = name,
                // 未声明键要在 Prepare / 建子级之前就打上标记：表现器与工厂都按它改道
                IsUnknownField = unknownField,
                DeclaredType = declaredType,
                Type = type,
                ValueNode = self,
                Depth = depth,
                Json = json,
                ValueChanged = onValueChanged,
                JsonPath = jsonPath,
                DependsOnRuntimeData = dependsOnRuntimeData,
                Key = name ?? "",
                StructureChanged = onStructureChanged,
                Error = evaluationError,
            };
            // 只在"**静默**解不出来"时自报：已经有诊断（形状不符 / 未解开的包装）的降级是**有意**的，
            // 再记一条 resolved=False 会让 --check-trace 把"非法文档的诚实降级"当成求解失败。
            if (evaluationError is null && declaredType?.Kind is McdocTypeKind.Dispatcher or McdocTypeKind.Indexed)
        {
            // 靠兄弟键/调度器解出来的类型：解不出来时求值退成 any —— 行上就是空下拉或动态键占位
            McdocWriteTrace.Trace("index-resolve", "path=" + (jsonPath ?? "?")
                + " registry=" + (declaredType.Registry ?? "-")
                + " resolved=" + (type?.Kind is not (McdocTypeKind.Any or McdocTypeKind.Unsafe))
                + " kind=" + (type?.Kind.ToString() ?? "null"));
        }
        var editor = McdocCollectionEditor.Default;
            // 只有"列表语义"的节点才有列表追加：map/struct 的 ⊕ 必须走"起键"（CommitEntry），
            // 否则按下一个永远 CanExecute=true 的按钮，什么都写不出来（表现是"按了没反应/结果不对"）
            bool CanAppendItems() => node.Type?.IsListLike == true
                || (node.UnionListLike && node.SelectedUnionMember?.IsListLike == true);
            // 追加与"在顶部新增"必须是同一个出口：AppendItem 那条旧路既不揭开新项、也不记展开态，
            // 于是"点了 ⊕，右侧 JSON 多了一条、左侧树什么都不动"（新项被折叠的列表挡住）。
            node.AddItemCommand = new McdocCommand(() => editor.AddItem(node, runtime), CanAppendItems);
            node.AddItemTopCommand = new McdocCommand(() => editor.AddItem(node, runtime, atTop: true), CanAppendItems);
            node.SetDefaultCommand = new McdocCommand(() => editor.SetDefault(node, runtime),
                () => node.Json is null && node.Parent is not null && !node.IsRequired);
            node.ShowMoreCommand = new McdocCommand(() => node.ItemsShown += Math.Max(1, McdocRowProjector.PageSize));
            node.ShowAllCommand = new McdocCommand(() => node.ItemsShown = int.MaxValue);
            node.RemoveItemCommand = new McdocCommand(() => editor.RemoveLastItem(node),
                () => node.Json is { Kind: McdocJsonKind.Array } list && list.Items is { Count: > 0 });
            node.DuplicateCommand = new McdocCommand(() => editor.DuplicateItem(node),
                () => node.Json is not null && node.Parent?.Json is { Kind: McdocJsonKind.Array });
            node.RemoveFieldCommand = new McdocCommand(() => editor.RemoveField(node),
                // 必选字段删了就是非法文档：删除原语自己就得拒绝（行上的按钮与 union 的 unset 都已按可选性收口）
                () => !node.IsRequired
                    && node.Parent?.Json is { Kind: McdocJsonKind.Object } parent
                    && parent.Properties is not null
                    && parent.Properties.Any(pair => pair.Key == node.FieldName));

            budget ??= new McdocNodeBudget(MaxNodes);
            visiting ??= [];
            var context = new McdocBuildContext
            {
                Runtime = runtime,
                Factory = this,
                Registry = registry,
                Self = self,
                KeyNode = keyNode,
                Depth = depth,
                Narrow = narrow,
                JsonPath = jsonPath,
                Visiting = visiting,
                Budget = budget,
                SimplifyContext = simplifyContext,
                OnValueChanged = onValueChanged,
                OnStructureChanged = onStructureChanged,
            };
            // 行自身状态必须在任何早退之前备好：深度/预算/环只省"子级"，不能省"这行用什么控件/显示什么值"
            registry.For(node).Prepare(node, type, json, context);
            // 取证：声明是枚举/带候选，却一个候选都没建出来 —— 首次建树"哑下拉"的现场特征
            if (node.DeclaredType?.Kind == McdocTypeKind.Enum
                && (node.EnumOptionList is null || node.EnumOptionList.Count == 0))
            {
                McdocWriteTrace.Trace("enum-empty", "path=" + (node.JsonPath ?? "?")
                    + " declared=" + node.DeclaredType.Kind + " values=" + (node.DeclaredType.EnumValues?.Count ?? 0));
            }
            // 取证：把行上现存的提醒原文记下来（哪条消息让感叹号下不去）
            if (!string.IsNullOrEmpty(node.Error) || node.Diagnostics is { Count: > 0 })
            {
                McdocWriteTrace.Trace("diag", "path=" + (node.JsonPath ?? "?")
                    + " err=" + (node.Error ?? "")
                    + " msgs=" + (node.Diagnostics is { Count: > 0 } list ? string.Join(" | ", list) : ""));
            }

            if (depth >= MaxDepth) return node;
            // 预算只约束"缺省展开"（json == null）：真实 JSON 节点不吃预算，
            // 否则导入大文件会变成"文件里有、树里没有"的静默丢数据
            if (json is null && !budget.TryConsume()) return node;

            // 类型级环检测：按**声明类型**判环（simplify 每次都造新对象，按求值后类型判必然失效）
            if (declaredType is not null && visiting.Any(item => ReferenceEquals(item, declaredType))) return node;
            if (declaredType is not null) visiting.Add(declaredType);
            try
            {

                // 局部队列重建：结构变更只重建这一层子树（避免整树 Load）
                node.RefreshChildren = () =>
                {
                    node.Children.Clear();
                    node.Items.Clear();
                    node.SelectedUnionChildren.Clear();
                    registry.For(node).PopulateChildren(node, type, node.Json, context);
                    foreach (var child in node.Children) child.Parent = node;
                    foreach (var item in node.Items) item.Parent = node;
                    foreach (var item in node.SelectedUnionChildren) item.Parent = node;
                    node.ChildrenBuilt = true;
                };

                // 懒建：越深的子树越晚构建；union 例外（分支名在 Head 里要用）。
                // deferChildren：父级已经知道"这是缺省成员"（文档里没有这个键）——
                // 投影对这类节点一列子行都不画（缺省字段只留一行 ⊕），此时建出来的子级全是"看不见的节点"。
                // 但 union 不能 defer：分支候选是行的状态（下拉要能切支），不是"值的子级"。
                var eager = (!deferChildren || type.Kind == McdocTypeKind.Union)
                    && (!LazyChildren || depth < DefaultExpandDepth || type.Kind == McdocTypeKind.Union);
                if (eager)
                {
                    node.RefreshChildren();
                }
                else
                {
                    node.ChildrenBuilt = false;
                }

                // misode：默认展开（只有 IsDefaultCollapsedType 例外）+ 深度上限（WPF 性能偏差）
                var hasChildren = eager
                    ? node.Children.Count + node.Items.Count + node.SelectedUnionChildren.Count > 0
                    : registry.For(node).CanHaveChildren(node);
                // 先看页面级折叠记忆（misode useToggles），没有记录才走"默认展开深度"
                var remembered = McdocExpandState.Lookup(jsonPath);
                if (remembered.HasValue)
                {
                    // 缺省字段只留一行 ⊕（与 misode 一致）：json 为 null 时"记住的展开态"不许把子级撑开，
                    // 否则关窗体重开后会看到"树上凭空多出子行、父行还是个 ⊕"。
                    // 但**不许**再按 hasChildren 拦：加项那一刻是"先把展开态记在路径上、节点随后才重建"，
                    // 这一拍的 hasChildren 可能是 false（数组刚写进去、子级还没建），
                    // 拦下来就变成"点了 ⊕，右侧 JSON 多一条、左侧树一动不动"。
                    node.IsExpanded = remembered.Value && json is not null;
                }
                else if (json is not null && hasChildren && depth < DefaultExpandDepth
                    && !McdocHelpers.IsDefaultCollapsedType(declaredType ?? type))
                {
                    node.IsExpanded = true;
                }
            }
            finally
            {
                if (declaredType is not null) visiting.RemoveAt(visiting.Count - 1);
            }

            return node;
        }

        /// <summary>能装子级的类型（声明成这些却拿到标量 = 非法文档）。</summary>
        private static bool IsContainerKind(McdocTypeKind kind)
            => kind is McdocTypeKind.Struct or McdocTypeKind.List or McdocTypeKind.Tuple
                or McdocTypeKind.ByteArray or McdocTypeKind.IntArray or McdocTypeKind.LongArray;

        /// <summary>叶子类型（声明成这些却拿到对象/数组 = 非法文档）。</summary>
        private static bool IsScalarKind(McdocTypeKind kind)
            => kind is McdocTypeKind.Boolean or McdocTypeKind.String or McdocTypeKind.Literal or McdocTypeKind.Enum
                or McdocTypeKind.Byte or McdocTypeKind.Short or McdocTypeKind.Int or McdocTypeKind.Long
                or McdocTypeKind.Float or McdocTypeKind.Double;

        private static bool IsScalarJson(McdocJsonKind kind)
            => kind is McdocJsonKind.String or McdocJsonKind.Number or McdocJsonKind.Boolean or McdocJsonKind.Null;

        /// <summary>容器的期望形状：结构要对象，列表/元组/原始数组要数组。错配就是非法文档。</summary>
        private static bool ContainerMatchesJson(McdocTypeKind typeKind, McdocJsonKind jsonKind)
            => typeKind == McdocTypeKind.Struct
                ? jsonKind == McdocJsonKind.Object
                : jsonKind == McdocJsonKind.Array;
    }

    /// <summary>老入口的过渡壳：对拍工具（e2e）与探针无需改动即可继续使用。</summary>
    public static class McdocTreeBuilder
    {
        public const int MaxDepth = McdocNodeFactory.MaxDepth;
        public const int MaxNodes = McdocNodeFactory.MaxNodes;

        public static McdocTreeNode BuildRoot(McdocType rootType, McdocJsonValue json, McdocRuntime runtime,
            bool narrow = true, Action<McdocTreeNode> onValueChanged = null, Action<McdocTreeNode> onStructureChanged = null)
            => new McdocNodeFactory().BuildRoot(rootType, json, runtime, narrow, onValueChanged, onStructureChanged);

        public static ObservableCollection<McdocTreeNode> Build(McdocType rootType, string jsonText, McdocRuntime runtime,
            Action<McdocTreeNode> onValueChanged = null)
            => new McdocNodeFactory().Build(rootType, jsonText, runtime, onValueChanged);

        public static ObservableCollection<McdocTreeNode> Build(McdocType rootType, McdocJsonValue json, McdocRuntime runtime,
            Action<McdocTreeNode> onValueChanged = null, Action<McdocTreeNode> onStructureChanged = null)
            => new McdocNodeFactory().Build(rootType, json, runtime, onValueChanged, onStructureChanged);
    }
}
