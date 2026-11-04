// mcdoc 类型求值器 —— 逐函数移植自 SpyglassMC/Spyglass
// packages/mcdoc/src/runtime/checker/index.ts 的 simplify 部分（L803-1362）。
// 与原实现的差异只在"如何拿到符号"：spyglass 走 SymbolUtil 查询，这里走 IMcdocSymbols。
// 方法数与 spyglass 一一对应，不再额外分层（避免类/方法爆炸）。
using System;
using System.Collections.Generic;
using System.Linq;

namespace CBHK.Utility.Data.Mcdoc
{
    public static class McdocSimplifier
    {
        /// <summary>
        /// 索引追踪（默认关闭）：探针把它接到 Console，用来定位"同级字段动态索引"类缺陷
        /// （例如 `dispatch[axolotl][[type]]` 里 type 该从哪一层取）。生产路径不设它就没有开销。
        /// </summary>
        public static Action<string> IndexTrace;
        /// <summary>
        /// 把 mcdoc 类型在当前运行期节点上求值成"它现在该长什么样"。
        /// 对应 spyglass 的 simplify()：深求值 reference/dispatcher/indexed，
        /// 浅求值 struct（字段的值类型留到字段自己有运行期节点时再算）。
        /// </summary>
        public static McdocSimplifyResult Simplify(McdocType typeDef, McdocSimplifyContext context)
        {
            if (typeDef == null) return new McdocSimplifyResult(McdocType.Any());
            var result = typeDef.Kind switch
            {
                McdocTypeKind.Reference => SimplifyReference(typeDef, context),
                McdocTypeKind.Dispatcher => SimplifyDispatcher(typeDef, context),
                McdocTypeKind.Indexed => SimplifyIndexed(typeDef, context),
                McdocTypeKind.Union => SimplifyUnion(typeDef, context),
                McdocTypeKind.Struct => SimplifyStruct(typeDef, context),
                McdocTypeKind.List => SimplifyList(typeDef, context),
                McdocTypeKind.Tuple => SimplifyTuple(typeDef, context),
                McdocTypeKind.Enum => SimplifyEnum(typeDef, context),
                McdocTypeKind.Concrete => SimplifyConcrete(typeDef, context),
                McdocTypeKind.Template => SimplifyTemplate(typeDef, context),
                McdocTypeKind.Mapped => SimplifyMapped(typeDef, context),
                _ => new McdocSimplifyResult(typeDef)
            };
            return Wrap(result, typeDef, context);
        }

        /// <summary>对结果套用 #[...] 的 mapType 处理器。注意判定用的是结果的属性、遍历的是输入类型的属性（与 spyglass 一致）。</summary>
        private static McdocSimplifyResult Wrap(McdocSimplifyResult result, McdocType source, McdocSimplifyContext context)
        {
            var typeDef = result.TypeDef;
            if (typeDef?.Attributes == null || typeDef.Attributes.Count == 0) return result;
            context.Runtime.Attributes.Handle(source?.Attributes, context.Runtime, (handler, config) =>
            {
                if (handler.MapType != null) typeDef = handler.MapType(config, typeDef);
            });
            typeDef = ResolveIdMembers(typeDef, source, context);
            return new McdocSimplifyResult(typeDef, result.DynamicData);
        }

        /// <summary>
        /// 求值期把 #[id] 候选（注册表 / 调度器索引成员）解析到类型上。
        /// presenter 只读 type.IdMembers —— 渲染层不再碰注册表/调度器，也不做"缺数据时回落"的判断。
        /// </summary>
        private static McdocType ResolveIdMembers(McdocType typeDef, McdocType source, McdocSimplifyContext context)
        {
            if (typeDef is null || context.Runtime?.IdSpaces is null) return typeDef;
            var spec = McdocIdSpec.FromAttributes(typeDef.Attributes) ?? McdocIdSpec.FromAttributes(source?.Attributes);
            if (spec is null || string.IsNullOrEmpty(spec.Registry)) return typeDef;

            var resolved = context.Runtime.IdSpaces.Resolve(spec, context.Runtime);
            if (resolved.Members is not { Count: > 0 }) return typeDef;

            var clone = typeDef.Clone();
            clone.IdMembers = resolved.Members;
            clone.IdSource = resolved.Source;
            return clone;
        }

        private static McdocSimplifyResult SimplifyReference(McdocType typeDef, McdocSimplifyContext context)
        {
            if (string.IsNullOrEmpty(typeDef.Path))
            {
                context.Runtime.Report("Tried to access empty reference");
                context.Diagnostics.Add("空引用");
                return new McdocSimplifyResult(McdocType.Any());
            }
            if (context.TypeMapping != null)
            {
                if (context.TypeMapping.TryGetValue(typeDef.Path, out var mapped) && mapped != null)
                {
                    return new McdocSimplifyResult(mapped, true);
                }
                // 模板形参引用可能带模块前缀（::mod::T）或与 mapping 的键写法不一致：
                // 按最后一段再匹配一次，否则 Mapped/Concrete 里的 T 会解不开（字段退化成 Mapped 包装 → 文本框）
                var lastSegment = LastSegment(typeDef.Path);
                foreach (var pair in context.TypeMapping)
                {
                    if (pair.Value is null) continue;
                    if (string.Equals(pair.Key, lastSegment, StringComparison.Ordinal)
                        || string.Equals(LastSegment(pair.Key), lastSegment, StringComparison.Ordinal))
                    {
                        return new McdocSimplifyResult(pair.Value, true);
                    }
                }
            }
            // 优先用"引用声明所在的模块"解析：跨文件引用按文档模块解析会撞上同名类型
            var referenceModule = string.IsNullOrEmpty(typeDef.Module) ? context.Runtime.ModulePath : typeDef.Module;
            var target = context.Runtime.Symbols?.GetTypeDef(typeDef.Path, context.Runtime.DocumentUri, referenceModule);
            if (target == null)
            {
                // 与 misode 对齐：解不开的引用渲染成 any（原始 JSON 编辑），不能是空联合（整行空白）
                context.Runtime.Report("Tried to access unknown reference " + typeDef.Path);
                context.Diagnostics.Add("未解析的引用：" + typeDef.Path);
                return new McdocSimplifyResult(McdocType.Any());
            }
            // 引用环（A→B→A）会让递归永不收敛：第一次进环时用空联合打断，外层继续求值
            if (!context.ResolvingReferences.Add(typeDef.Path))
            {
                context.Runtime.Report("Cyclic reference: " + typeDef.Path);
                context.Diagnostics.Add("引用环：" + typeDef.Path);
                return new McdocSimplifyResult(McdocType.Any());
            }
            // 进入目标类型自己的命名空间再继续求值，相对名 / use 别名才不会串模块
            var targetContext = string.IsNullOrEmpty(target.Module) || target.Module == context.Runtime.ModulePath
                ? context
                : context.Clone();
            if (!ReferenceEquals(targetContext, context))
            {
                targetContext.Runtime = context.Runtime.WithModule(target.Module);
            }
            McdocSimplifyResult result;
            try
            {
                result = Simplify(target, targetContext);
            }
            finally
            {
                context.ResolvingReferences.Remove(typeDef.Path);
            }
            if (typeDef.Attributes != null && typeDef.Attributes.Count > 0)
            {
                var merged = new List<McdocAttribute>(typeDef.Attributes);
                if (result.TypeDef.Attributes != null) merged.AddRange(result.TypeDef.Attributes);
                var clone = result.TypeDef.Clone();
                clone.Attributes = merged;
                result = new McdocSimplifyResult(clone, result.DynamicData);
            }
            return result;
        }

        private static McdocSimplifyResult SimplifyDispatcher(McdocType typeDef, McdocSimplifyContext context)
        {
            var members = context.Runtime.Symbols?.GetDispatcherMembers(typeDef.Registry, context.Runtime.DocumentUri);
            if (members == null)
            {
                context.Runtime.Report("Tried to access unknown dispatcher " + typeDef.Registry);
                context.Diagnostics.Add("未知调度器：" + typeDef.Registry);
                return new McdocSimplifyResult(McdocType.EmptyUnion());
            }
            var dispatcherContext = context;
            if (typeDef.TypeParams is { Count: > 0 } parameters && context.TypeArgs is { Count: > 0 } arguments)
            {
                // dispatch ...<T> to X：形参挂在 dispatcher 上，实参由外层 Concrete 排队送进来。
                // 把 mapping 挂到上下文里，成员目标里引用 T 的地方才会被替换（缺省 Any）。
                var mapping = context.TypeMapping is null
                    ? new Dictionary<string, McdocType>(StringComparer.Ordinal)
                    : new Dictionary<string, McdocType>(context.TypeMapping, StringComparer.Ordinal);
                for (var i = 0; i < parameters.Count; i++)
                {
                    mapping[parameters[i]] = i < arguments.Count ? arguments[i] : McdocType.Any();
                }
                dispatcherContext = context.Clone();
                dispatcherContext.TypeMapping = mapping;
                dispatcherContext.TypeArgs = [];
            }
            return ResolveIndices(typeDef.Indices, new Dictionary<string, McdocType>(members), dispatcherContext);
        }

        /// <summary>
        /// 归一形态判定：顶层不得是求值指令。
        /// deep 只往 **Union 成员**里走——成员会被"按 JSON 选分支/当分支行渲染"直接消费，必须已归一；
        /// Struct 字段 / List 项 / Tuple 项都保持声明类型，由工厂在创建对应节点时归一（唯一的惰性边界）。
        /// </summary>
        public static bool IsNormalForm(McdocType type, bool deep = false)
        {
            if (type is null) return false;
            if (type.IsDirective) return false;
            if (!deep) return true;
            if (type.Kind != McdocTypeKind.Union) return true;
            return (type.Members ?? []).All(member => IsNormalForm(member, true));
        }

        private static string LastSegment(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            var index = path.LastIndexOf("::", StringComparison.Ordinal);
            return index < 0 ? path : path.Substring(index + 2);
        }

        private static McdocSimplifyResult SimplifyIndexed(McdocType typeDef, McdocSimplifyContext context)
        {
            var childContext = context.Clone();
            // 不能清 TypeArgs：Concrete{Item=Indexed(Dispatcher), Items=[…]} 这条链上，
            // 实参必须原样传到 dispatcher 目标模板（dispatch ...<T> to X<T>）。
            var childResult = Simplify(typeDef.Item, childContext);
            // 没有平行索引时 Indexed 只是"外壳"：原样返回子类型（否则会退化成空联合）
            if (typeDef.Indices is not { Count: > 0 })
            {
                return new McdocSimplifyResult(childResult.TypeDef, childResult.DynamicData);
            }
            var indexed = ApplyIndices(childResult.TypeDef, typeDef.Indices, context);
            return new McdocSimplifyResult(indexed.TypeDef, childResult.DynamicData || indexed.DynamicData);
        }

        /// <summary>索引对**任意归一形态**都要有定义（见 README 求解代数 §0.4）。</summary>
        private static McdocSimplifyResult ApplyIndices(McdocType typeDef, IReadOnlyList<McdocIndex> indices,
            McdocSimplifyContext context)
        {
            if (typeDef is null) return new McdocSimplifyResult(McdocType.Any());
            switch (typeDef.Kind)
            {
                case McdocTypeKind.Struct:
                {
                    var symbolMap = new Dictionary<string, McdocType>();
                    foreach (var field in typeDef.Fields ?? [])
                    {
                        var key = field.Name ?? (field.Key is { Kind: McdocTypeKind.Literal }
                            ? field.Key.LiteralText()
                            : null);
                        if (key != null) symbolMap[key] = field.Type;
                    }
                    return ResolveIndices(indices, symbolMap, context);
                }
                case McdocTypeKind.Union:
                {
                    // 逐成员索引后取并集（去重、保序）；不可索引的成员跳过并报告，不让整支失败
                    var results = new List<McdocType>();
                    var dynamicData = false;
                    var skipped = 0;
                    foreach (var member in typeDef.Members ?? [])
                    {
                        if (member is null) continue;
                        if (results.Count >= 64)
                        {
                            Diagnose(context, "联合超过 64 个可索引成员，已退 any");
                            return new McdocSimplifyResult(McdocType.Any(), true);
                        }
                        var memberResult = ApplyIndices(member, indices, context);
                        if (memberResult.DynamicData) dynamicData = true;
                        var memberType = memberResult.TypeDef;
                        if (memberType is null || (memberType.Kind == McdocTypeKind.Union && (memberType.Members?.Count ?? 0) == 0))
                        {
                            skipped++;
                            continue;
                        }
                        if (memberType.Kind == McdocTypeKind.Union)
                        {
                            foreach (var branch in memberType.Members ?? []) AddUnique(branch);
                        }
                        else
                        {
                            AddUnique(memberType);
                        }
                    }
                    if (results.Count == 0)
                    {
                        if (skipped > 0) Diagnose(context, "按索引取联合成员时没有可索引的成员（已退 any）");
                        return new McdocSimplifyResult(McdocType.Any(), dynamicData);
                    }
                    return new McdocSimplifyResult(
                        results.Count == 1 ? results[0] : new McdocType { Kind = McdocTypeKind.Union, Members = results },
                        dynamicData);

                    void AddUnique(McdocType candidate)
                    {
                        if (candidate is null) return;
                        if (results.Any(existing => McdocHelpers.QuickEqualTypes(existing, candidate))) return;
                        results.Add(candidate);
                    }
                }
                case McdocTypeKind.Tuple:
                {
                    var items = typeDef.Items ?? [];
                    var staticIndex = indices.Count == 1 && indices[0].Kind == McdocIndexKind.Static
                        ? indices[0].Value
                        : null;
                    if (!string.IsNullOrEmpty(staticIndex) && int.TryParse(staticIndex, out var position))
                    {
                        if (position < 0 || position >= items.Count)
                        {
                            Diagnose(context, "元组下标越界：" + position);
                            return new McdocSimplifyResult(McdocType.Any());
                        }
                        return Simplify(items[position], context);
                    }
                    // 动态索引：对每一项求值后并集
                    var union = new List<McdocType>();
                    var dynamicData = false;
                    foreach (var item in items)
                    {
                        if (item is null) continue;
                        var simplified = Simplify(item, context);
                        if (simplified.DynamicData) dynamicData = true;
                        if (simplified.TypeDef is null) continue;
                        if (simplified.TypeDef.Kind == McdocTypeKind.Union) union.AddRange(simplified.TypeDef.Members ?? []);
                        else union.Add(simplified.TypeDef);
                    }
                    return new McdocSimplifyResult(
                        union.Count switch
                        {
                            0 => McdocType.Any(),
                            1 => union[0],
                            _ => new McdocType { Kind = McdocTypeKind.Union, Members = union },
                        },
                        dynamicData);
                }
                case McdocTypeKind.List:
                case McdocTypeKind.ByteArray:
                case McdocTypeKind.IntArray:
                case McdocTypeKind.LongArray:
                    return Simplify(typeDef.Item ?? McdocType.Any(), context);
                case McdocTypeKind.Any:
                    return new McdocSimplifyResult(McdocType.Any(), true);
                default:
                    Diagnose(context, "该类型不可索引（" + typeDef.Kind + "），已退 any");
                    return new McdocSimplifyResult(McdocType.Any());
            }
        }

        /// <summary>按平行索引在符号表里查类型；动态索引靠当前节点的兄弟键取值。</summary>
        private static McdocSimplifyResult ResolveIndices(IReadOnlyList<McdocIndex> parallelIndices, Dictionary<string, McdocType> symbolMap, McdocSimplifyContext context)
        {
            var dynamicData = false;
            var values = new List<McdocType>();

            void PushValue(string key, McdocType typeDef)
            {
                if (typeDef == null) return;
                if (!context.Runtime.Attributes.ShouldKeep(typeDef.Attributes, context.Runtime)) return;
                var simplified = Simplify(typeDef, context);
                if (simplified.DynamicData) dynamicData = true;
                if (simplified.TypeDef.Kind == McdocTypeKind.Union) values.AddRange(simplified.TypeDef.Members ?? []);
                else values.Add(simplified.TypeDef);
            }

            McdocType unknownTypeDef = null;
            var unknownResolved = false;
            McdocType GetUnknownTypeDef()
            {
                if (!unknownResolved)
                {
                    unknownResolved = true;
                    symbolMap.TryGetValue("%unknown", out unknownTypeDef);
                }
                return unknownTypeDef;
            }

            foreach (var index in parallelIndices ?? Array.Empty<McdocIndex>())
            {
                var lookup = new List<string>();
                var bailedOut = false;
                IndexTrace?.Invoke("[" + (context.Tag ?? "?") + "] index kind=" + index.Kind + " value=" + (index.Value ?? "-")
                    + " accessor=[" + string.Join(",", index.Accessor ?? []) + "]"
                    + " symbolMap=" + symbolMap.Count + " isMember=" + context.IsMember
                    + " selfJson=" + DescribeRuntimeOriginal(context.Node?.Node?.OriginalNode)
                    + " parentJson=" + DescribeRuntimeOriginal(context.Node?.Parent?.Node?.OriginalNode));
                if (index.Kind == McdocIndexKind.Static)
                {
                    if (index.Value == "%fallback")
                    {
                        values.Clear();
                        foreach (var pair in symbolMap) PushValue(pair.Key, pair.Value);
                        break;
                    }
                    lookup.Add(CutNamespace(index.Value));
                }
                else
                {
                    dynamicData = true;
                    // spyglass 的 %fallback = 整个 dispatcher 的并集（runtime/checker/index.ts:995）。
                    // 我们在解析层拿到的是"动态索引 + %fallback 访问器"，等价于静态写法，这里照搬同一语义。
                    if (index.Accessor?.Contains("%fallback") == true)
                    {
                        values.Clear();
                        foreach (var pair in symbolMap) PushValue(pair.Key, pair.Value);
                        continue;
                    }
                    var possibilities = new List<(McdocValueNode value, McdocRuntimeNode key)>
                    {
                        context.IsMember
                            ? (context.Node, context.Node?.RuntimeKey)
                            : (context.Node?.Parent, context.Node?.RuntimeKey)
                    };
                    foreach (var entry in index.Accessor ?? [])
                    {
                        if (entry == "%parent")
                        {
                            possibilities = possibilities.Select(p => (p.value?.Parent, p.value?.RuntimeKey)).ToList();
                        }
                        else if (entry == "%key")
                        {
                            possibilities = possibilities.Select(p => p.key == null
                                ? ((McdocValueNode)null, (McdocRuntimeNode)null)
                                : (McdocValueNode.Of(p.key, p.value, p.key), (McdocRuntimeNode)null)).ToList();
                            break;
                        }
                        else if (!entry.StartsWith("%", StringComparison.Ordinal))
                        {
                            var next = new List<(McdocValueNode, McdocRuntimeNode)>();
                            foreach (var node in possibilities)
                            {
                                if (node.value == null)
                                {
                                    next.Add((null, null));
                                    continue;
                                }
                                var childContext = context.Clone();
                                childContext.Node = node.value;
                                var inferred = node.value.Node?.InferredType ?? McdocType.Any();
                                var simplified = Simplify(inferred, childContext).TypeDef;
                                var accessor = context.Runtime.Accessor;
                                if (accessor == null) continue;
                                foreach (var pair in accessor.GetChildren(node.value.Node?.OriginalNode, simplified))
                                {
                                    var keyType = pair?.Key?.InferredType;
                                    if (keyType == null || keyType.Kind != McdocTypeKind.Literal
                                        || keyType.LiteralKind != McdocLiteralKind.String
                                        || !Equals(keyType.Value, entry)) continue;
                                    foreach (var value in pair.PossibleValues ?? [])
                                        next.Add((McdocValueNode.Of(value, node.value, pair.Key), null));
                                }
                            }
                            possibilities = next;
                        }
                        else
                        {
                            lookup.Add("%none");
                            bailedOut = true;
                            break;
                        }
                    }
                    if (!bailedOut)
                    {
                        foreach (var possibility in possibilities)
                        {
                            var node = possibility.value?.Node;
                            if (node?.InferredType != null && node.InferredType.Kind == McdocTypeKind.Literal
                                && node.InferredType.LiteralKind == McdocLiteralKind.String)
                            {
                                lookup.Add(CutNamespace(node.InferredType.LiteralText()));
                            }
                            else
                            {
                                lookup.Add("%none");
                            }
                        }
                    }
                }

                if (lookup.Count == 0) lookup.Add("%none");
                IndexTrace?.Invoke("[" + (context.Tag ?? "?") + "] lookup=[" + string.Join(",", lookup) + "] hit=["
                    + string.Join(",", lookup.Select(value => value + ":" + symbolMap.ContainsKey(value))) + "]");

                var missing = false;
                var currentValues = new List<(string value, McdocType data)>();
                foreach (var value in lookup)
                {
                    var data = symbolMap.TryGetValue(value, out var found) ? found : GetUnknownTypeDef();
                    if (data == null)
                    {
                        missing = true;
                        // 只有"确实有键但查不到"才算问题：%none 表示这个位置还没有键（可选字段的常态）。
                        // 这条是 D1 那类"键身份丢了"的唯一现场痕迹，必须可见。
                        if (value != "%none" && value != "%unknown" && value != "%fallback")
                        {
                            Diagnose(context, "动态键 " + value + " 没有对应成员（已退 any）");
                        }
                    }
                    currentValues.Add((value, data));
                }
                if (missing)
                {
                    IndexTrace?.Invoke("[" + (context.Tag ?? "?") + "] miss（没有 %unknown 成员）-> any");
                    return new McdocSimplifyResult(McdocType.Any(), dynamicData);
                }
                foreach (var entry in currentValues)
                {
                    IndexTrace?.Invoke("[" + (context.Tag ?? "?") + "] push " + entry.value + " -> " + (entry.data?.Kind.ToString() ?? "null"));
                    PushValue(entry.value, entry.data);
                }
            }

            IndexTrace?.Invoke("[" + (context.Tag ?? "?") + "] values=" + values.Count
                + " kinds=[" + string.Join(",", values.Select(value => value?.Kind.ToString() ?? "null")) + "]");
            if (values.Count == 0)
            {
                // 空联合是合法归一形态（spyglass 同款，对拍按它算），所以这里**不**改形状；
                // 但"一个成员都没命中"必须可见：键可能在当前版本已被移除。
                Diagnose(context, "索引没有命中任何成员（键在当前版本可能已被移除）");
            }
            if (values.Count == 1) return new McdocSimplifyResult(values[0], dynamicData);
            return new McdocSimplifyResult(new McdocType { Kind = McdocTypeKind.Union, Members = values }, dynamicData);
        }

        /// <summary>追踪用：把运行节点的原始 JSON 描述成"种类:内容"，null 就是 null。</summary>
        private static string DescribeRuntimeOriginal(object original)
        {
            if (original is not McdocJsonValue value) return original is null ? "<null>" : original.GetType().Name;
            return value.Kind switch
            {
                McdocJsonKind.Object => "object(" + (value.Properties?.Count ?? 0) + " keys)",
                McdocJsonKind.Array => "array(" + (value.Items?.Count ?? 0) + ")",
                McdocJsonKind.String => "string(" + value.StringValue + ")",
                _ => value.Kind.ToString(),
            };
        }

        /// <summary>求值诊断：进日志的同时挂到行上（不变量 10：解不开退 any 必须可见，不能只写日志）。</summary>
        private static void Diagnose(McdocSimplifyContext context, string message)
        {
            context.Runtime.Report(message);
            context.Diagnostics.Add(message);
        }

        private static McdocSimplifyResult SimplifyUnion(McdocType typeDef, McdocSimplifyContext context)
        {
            var dynamicData = false;
            var validMembers = (typeDef.Members ?? []).Where(m => m != null
                && context.Runtime.Attributes.ShouldKeep(m.Attributes, context.Runtime)).ToList();

            if (context.Runtime.RequireCanonical && validMembers.Any(m => HasAttribute(m, "canonical")))
            {
                validMembers = validMembers.Where(m => HasAttribute(m, "canonical")).ToList();
            }
            if (validMembers.Count == 1) return Simplify(validMembers[0], context);

            var members = new List<McdocType>();
            foreach (var member in validMembers)
            {
                var simplified = Simplify(member, context);
                if (simplified.DynamicData) dynamicData = true;
                var simplifiedType = simplified.TypeDef;
                if (simplifiedType.Kind != McdocTypeKind.Union)
                {
                    members.Add(simplifiedType);
                    continue;
                }
                if ((simplifiedType.Members?.Count ?? 0) > 0)
                {
                    members.AddRange(simplifiedType.Members);
                    continue;
                }
                // 成员求值成空联合（引用解不开 / 模板缺参）时不能丢弃，也不能保留声明形态——
                // 声明形态常常是 Reference/Template 这类"指令"，留着会让选支/渲染拿到非归一形态。
                // 用 any 占位并报告：分支数不缩水，界面明确显示"这一支没解出来"。
                context.Runtime.Report("Union member could not be resolved: " + member.Kind);
                context.Diagnostics.Add("联合成员未解出：" + member.Kind);
                members.Add(McdocType.Any());
            }
            // 注意：这里不做"按节点收窄"。spyglass 的 simplify 只做属性过滤 + 展平，
            // 收窄发生在 checker 里（渲染层再读 node.typeDef）。本项目在 McdocTreeBuilder 里补那一步，
            // 见 McdocTypeMatcher.Narrow —— 这样才能与 oracle（纯 simplify）逐节点对拍。
            if (members.Count == 1) return new McdocSimplifyResult(members[0], dynamicData);
            return new McdocSimplifyResult(new McdocType { Kind = McdocTypeKind.Union, Members = members }, dynamicData);
        }

        private static McdocSimplifyResult SimplifyStruct(McdocType typeDef, McdocSimplifyContext context)
        {
            var fields = new List<McdocStructField>();
            var dynamicData = false;

            void AddField(object keyOrName, McdocStructField field)
            {
                context.Runtime.Attributes.Handle(field.Attributes, context.Runtime, (handler, config) =>
                {
                    if (handler.MapField != null) field = handler.MapField(config, field);
                });

                if (keyOrName is string name)
                {
                    fields.Add(WithKey(field, name));
                }
                else if (keyOrName is McdocType key && key.Kind == McdocTypeKind.Union)
                {
                    foreach (var member in key.Members ?? [])
                    {
                        var optional = field.Clone();
                        optional.Optional = true;
                        AddField(member, optional);
                    }
                }
                else if (keyOrName is McdocType keyType)
                {
                    fields.Add(WithKey(field, keyType));
                }
            }

            foreach (var field in typeDef.Fields ?? [])
            {
                if (field == null) continue;
                if (!context.Runtime.Attributes.ShouldKeep(field.Attributes, context.Runtime)) continue;

                if (!field.IsSpread)
                {
                    // 动态键字段在 MetaType 侧是"字段类型 = Tuple[键类型, 值类型]"（visitor 的约定，
                    // 见 MCDocumentMetaVisitor.VisitStructField 的"方括号计算键"注释）。
                    // 泛型形参出现在键上时（EnvironmentAttributeMap<K> 的 [K]），键只能在 Tuple[0] 里，
                    // field.Key 是空的——不在这里展开就会把键求成 Any，候选与泛型替换一起丢。
                    var dynamicTuple = field.Name == null
                        && field.Type is { Kind: McdocTypeKind.Tuple, Items: { Count: >= 2 } } tupleSource
                        ? tupleSource
                        : null;

                    object structKey;
                    if (field.Name != null)
                    {
                        structKey = field.Name;
                    }
                    else if (field.Key is { Kind: not McdocTypeKind.Reference } && dynamicTuple != null)
                    {
                        // 已经归一过的动态键字段（Key 已就位、类型仍是 Tuple[键,值]）：
                        // 原样保留。再走一遍会把"键是 union"的字段按成员展开一次（3 支 → 9 支），破坏幂等。
                        dynamicData = true;
                        fields.Add(field);
                        continue;
                    }
                    else
                    {
                        var keyContext = context.Clone();
                        keyContext.IsMember = true;
                        keyContext.TypeArgs = [];
                        var keySource = dynamicTuple != null ? dynamicTuple.Items[0] : (field.Key ?? McdocType.Any());
                        var keyResult = Simplify(keySource, keyContext);
                        if (keyResult.DynamicData) dynamicData = true;
                        structKey = keyResult.TypeDef;

                        if (dynamicTuple != null)
                        {
                            // 值类型（可能引用形参）继续交给 Mapped 延迟替换，但 Tuple 外壳必须保留：
                            // StructNodePresenter 靠 Tuple 的形状区分"键类型 / 值类型"
                            var valuePart = dynamicTuple.Items[1];
                            if (context.TypeMapping != null)
                            {
                                valuePart = new McdocType
                                {
                                    Kind = McdocTypeKind.Mapped,
                                    Item = valuePart,
                                    Mapping = new Dictionary<string, McdocType>(context.TypeMapping)
                                };
                                dynamicData = true;
                            }
                            var dynamicField = field.Clone();
                            dynamicField.Key = keyResult.TypeDef;
                            dynamicField.Type = new McdocType
                            {
                                Kind = McdocTypeKind.Tuple,
                                Items = [keyResult.TypeDef, valuePart]
                            };
                            AddField(structKey, dynamicField);
                            continue;
                        }
                    }

                    var mappedField = field;
                    if (context.TypeMapping != null)
                    {
                        mappedField = field.Clone();
                        mappedField.Type = new McdocType
                        {
                            Kind = McdocTypeKind.Mapped,
                            Item = field.Type,
                            Mapping = new Dictionary<string, McdocType>(context.TypeMapping)
                        };
                        dynamicData = true;
                    }
                    AddField(structKey, mappedField);
                }
                else
                {
                    var spreadContext = context.Clone();
                    spreadContext.IsMember = true;
                    spreadContext.TypeArgs = [];
                    var spread = Simplify(field.Type, spreadContext);
                    if (spread.DynamicData) dynamicData = true;
                    if (spread.TypeDef.Kind == McdocTypeKind.Any)
                    {
                        fields.Add(new McdocStructField { Key = McdocType.Any(), Type = McdocType.Any() });
                    }
                    else if (spread.TypeDef.Kind == McdocTypeKind.Struct)
                    {
                        fields.AddRange(spread.TypeDef.Fields ?? []);
                    }
                    else if (spread.TypeDef.Kind == McdocTypeKind.Union)
                    {
                        // spread 的目标是 union（例如 ...U，U = (P | Q)）：合并所有 struct 成员的字段。
                        // 非 struct 成员报告并跳过；一个都没合并时补"未知字段"占位，保持可见而不是静默空掉。
                        var mergedFields = 0;
                        foreach (var member in spread.TypeDef.Members ?? [])
                        {
                            if (member?.Kind != McdocTypeKind.Struct) continue;
                            fields.AddRange(member.Fields ?? []);
                            mergedFields++;
                        }
                        if (mergedFields == 0)
                        {
                            IndexTrace?.Invoke("[" + (context.Tag ?? "?") + "] spread-union merged=0 selfJson="
                                + DescribeRuntimeOriginal(context.Node?.Node?.OriginalNode));
                            Diagnose(context, "spread 的目标联合里没有结构成员");
                            fields.Add(new McdocStructField { Key = McdocType.Any(), Type = McdocType.Any() });
                        }
                    }
                    else
                    {
                        IndexTrace?.Invoke("  spread-not-struct kind=" + spread.TypeDef.Kind
                            + " selfJson=" + DescribeRuntimeOriginal(context.Node?.Node?.OriginalNode));
                        Diagnose(context, "spread 的目标不是结构（" + spread.TypeDef.Kind + "）");
                    }
                }
            }

            return new McdocSimplifyResult(new McdocType { Kind = McdocTypeKind.Struct, Fields = fields }, dynamicData);
        }

        private static McdocSimplifyResult SimplifyList(McdocType typeDef, McdocSimplifyContext context)
        {
            if (context.TypeMapping == null) return new McdocSimplifyResult(typeDef);
            var clone = typeDef.Clone();
            clone.Item = new McdocType
            {
                Kind = McdocTypeKind.Mapped,
                Item = typeDef.Item,
                Mapping = new Dictionary<string, McdocType>(context.TypeMapping)
            };
            return new McdocSimplifyResult(clone, true);
        }

        private static McdocSimplifyResult SimplifyTuple(McdocType typeDef, McdocSimplifyContext context)
        {
            if (context.TypeMapping == null) return new McdocSimplifyResult(typeDef);
            var clone = typeDef.Clone();
            clone.Items = (typeDef.Items ?? []).Select(item => (McdocType)new McdocType
            {
                Kind = McdocTypeKind.Mapped,
                Item = item,
                Mapping = new Dictionary<string, McdocType>(context.TypeMapping)
            }).ToList();
            return new McdocSimplifyResult(clone, true);
        }

        private static McdocSimplifyResult SimplifyEnum(McdocType typeDef, McdocSimplifyContext context)
        {
            // 同一字面量出现多次（如 #[until]/#[since] 成对的 OldDoc 变体）时取后者、沿用首次出现的位置，
            // 与 spyglass simplify 的枚举去重同款；不去重的话下拉里会有两个同值项
            var kept = new List<McdocEnumValue>();
            var positions = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var value in typeDef.EnumValues ?? [])
            {
                if (value is null || !context.Runtime.Attributes.ShouldKeep(value.Attributes, context.Runtime)) continue;
                var key = Convert.ToString(value.Value) ?? "";
                if (positions.TryGetValue(key, out var existing))
                {
                    kept[existing] = value;
                    continue;
                }
                positions[key] = kept.Count;
                kept.Add(value);
            }
            var clone = typeDef.Clone();
            clone.EnumValues = kept;
            return new McdocSimplifyResult(clone);
        }

        private static McdocSimplifyResult SimplifyConcrete(McdocType typeDef, McdocSimplifyContext context)
        {
            var dynamicData = false;
            var simplifiedArgs = new List<McdocType>();
            foreach (var argument in typeDef.Items ?? [])
            {
                var result = Simplify(argument, context);
                if (result.DynamicData) dynamicData = true;
                simplifiedArgs.Add(result.TypeDef);
            }
            var childContext = context.Clone();
            childContext.TypeArgs = simplifiedArgs;
            var childResult = Simplify(typeDef.Item, childContext);
            return new McdocSimplifyResult(
                KeepDeclaredOnFailure(childResult.TypeDef, typeDef, context, "concrete"),
                dynamicData || childResult.DynamicData);
        }

        private static McdocSimplifyResult SimplifyTemplate(McdocType typeDef, McdocSimplifyContext context)
        {
            var typeParams = typeDef.TypeParams ?? [];
            if ((context.TypeArgs?.Count ?? 0) != typeParams.Count)
            {
                Diagnose(context, "模板形参数量不符：需要 " + typeParams.Count + " 个实参，实际 " + (context.TypeArgs?.Count ?? 0) + " 个");
            }
            // 外层 mapping 为底：模板体里再引用外层形参时不丢键（内层形参覆盖同名）
            var mapping = context.TypeMapping is null
                ? new Dictionary<string, McdocType>(StringComparer.Ordinal)
                : new Dictionary<string, McdocType>(context.TypeMapping, StringComparer.Ordinal);
            for (var i = 0; i < typeParams.Count; i++)
            {
                if (context.TypeArgs != null && i < context.TypeArgs.Count)
                {
                    mapping[typeParams[i]] = context.TypeArgs[i];
                    continue;
                }
                // 已经在 mapping 环境里（Mapped/外层模板）时，嵌套模板的实参要从既有 mapping 继承，
                // 否则 FloatProvider<T> 这种"模板里再引用模板"会把 T 直接取成空联合（字段退化成 Mapped 包装）
                mapping[typeParams[i]] = context.TypeMapping != null
                    && context.TypeMapping.TryGetValue(typeParams[i], out var inherited)
                    && inherited is not null
                        ? inherited
                        : McdocType.Any();
            }
            var childContext = context.Clone();
            childContext.TypeArgs = [];
            childContext.TypeMapping = mapping;
            var templateResult = Simplify(typeDef.Item, childContext);
            return new McdocSimplifyResult(
                KeepDeclaredOnFailure(templateResult.TypeDef, typeDef, context, "template"),
                templateResult.DynamicData);
        }

        /// <summary>子求值成空联合（解不开）时保留声明类型并报告，避免整块静默变成空壳。</summary>
        private static McdocType KeepDeclaredOnFailure(McdocType resolved, McdocType declared,
            McdocSimplifyContext context, string what)
        {
            var resolvedEmpty = resolved is null
                || (resolved.Kind == McdocTypeKind.Union && (resolved.Members?.Count ?? 0) == 0);
            if (!resolvedEmpty) return resolved;
            if (declared is null) return resolved;
            if (declared.Kind == McdocTypeKind.Union && (declared.Members?.Count ?? 0) == 0) return resolved;
            context.Runtime.Report(what + " resolved to an empty union; keeping declared type");
            return declared;
        }

        private static McdocSimplifyResult SimplifyMapped(McdocType typeDef, McdocSimplifyContext context)
        {
            var dynamicData = false;
            // 外层 mapping 为底：内层映射覆盖同名键，其余继承
            var mapping = context.TypeMapping is null
                ? new Dictionary<string, McdocType>(StringComparer.Ordinal)
                : new Dictionary<string, McdocType>(context.TypeMapping, StringComparer.Ordinal);
            foreach (var pair in typeDef.Mapping ?? [])
            {
                var result = Simplify(pair.Value, context);
                if (result.DynamicData) dynamicData = true;
                mapping[pair.Key] = result.TypeDef;
            }
            var childContext = context.Clone();
            childContext.TypeMapping = mapping;
            var childResult = Simplify(typeDef.Item, childContext);
            return new McdocSimplifyResult(
                KeepDeclaredOnFailure(childResult.TypeDef, typeDef, context, "mapped"),
                dynamicData || childResult.DynamicData);
        }

        private static McdocStructField WithKey(McdocStructField field, string name)
        {
            var clone = field.Clone();
            clone.Name = name;
            clone.Key = McdocType.LiteralString(name);
            return clone;
        }

        private static McdocStructField WithKey(McdocStructField field, McdocType key)
        {
            var clone = field.Clone();
            clone.Name = null;
            clone.Key = key;
            return clone;
        }

        private static string CutNamespace(string value)
        {
            // 空串不是 "未知"：spyglass 原样拿去查表（runtime/checker/index.js:749-762），
            // 查不到再退到 %unknown；折叠成 %none 会把 dispatch ...[%none] to any 命中，
            // 与 spyglass 的 dispatch ...[%unknown] to struct {} 分道扬镳（particle.type = "" 就是这条）。
            if (value is null) return "%none";
            return value.StartsWith("minecraft:", StringComparison.Ordinal) ? value.Substring(10) : value;
        }

        private static bool HasAttribute(McdocType type, string name)
            => type?.Attributes != null && type.Attributes.Any(a => a != null && a.Name == name);
    }
}
