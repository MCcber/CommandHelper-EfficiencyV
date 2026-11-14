using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MinecraftLanguageModelLibrary.Data;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 集合编辑（misode 的 ⊕ / ⊖ / 改名 / 复制）：只改 JSON 值并让宿主重排文本，
    /// 不接触视图。命令对象由工厂与表现器用委托构造，避免一类一命令的膨胀。
    /// </summary>
    /// <summary>物化写过的一个键：谁被写了、写之前是不是"没有值"（空壳是这一笔补出来的）。</summary>
    public sealed class McdocMaterializeWrite
    {
        public McdocTreeNode Node;
        public bool WasEmpty;
    }

    public sealed class McdocCollectionEditor
    {
        public static McdocCollectionEditor Default { get; } = new();

        /// <summary>
        /// 把"必填 + 闭合下拉 + 已经替用户选好默认项、但 JSON 里还没有这个键"的值写进文档。
        /// 这些值界面上本来就是选中的，不落盘会出现"行上显示 minecraft:xxx、依赖它的调度器/spread 却是空的"
        /// （advancement.criteria.&lt;name&gt;.trigger 就是这种情况：trigger 缺键 → ...minecraft:trigger[[trigger]] 解析成 %none → 无子级）。
        /// 只处理必填字段：可选字段绝不能因为显示默认值就被凭空插进用户的文档。
        /// </summary>
        public int MaterializeRequiredSelections(IEnumerable<McdocTreeNode> roots, bool includeDisplayedDefaults = false,
            McdocRuntime runtime = null, List<McdocMaterializeWrite> writtenNodes = null)
        {
            var written = 0;
            void Walk(McdocTreeNode node)
            {
                if (node is null) return;
                // 删除/重建之后还伸手写回的陈旧节点：不物化、不往下走（否则会把子字段提到上层）
                if (!node.IsAttachedToRoot(out var brokenAt))
                {
                    McdocWriteTrace.Log("Materialize 跳过", node, brokenAt);
                    return;
                }
                // 空串同样算"没有值"：默认值把必填枚举物化成 "" 时，依赖它的调度器 spread 会解析成 %none
                var emptyText = node.Json is null
                    || node.Json.Kind == McdocJsonKind.Null
                    || (node.Json.Kind == McdocJsonKind.String && string.IsNullOrWhiteSpace(node.Json.StringValue));
                // 有候选且有选中项就物化：普通 enum / literal 的 UseSelect 是 false，
                // 只看 UseSelect 会让必选枚举（frame=Task 这类）永远进不了文档。
                // **静默写入**：不走 ApplyValue（它抬 ValueChanged/StructureChanged），否则重建会把
                // 用户正在操作的行换成脱挂节点（S12 的候选选择写不进 JSON 就是这么坏的）。
                // 必填容器没有任何值：先把它本身建出来（空对象 / 空数组）。
            // 不建这一层，里面的必填字段（必选枚举就是典型）连节点都没有，物化永远轮不到它，
            // 校验就一直报「缺少必填键」——行上那颗感叹号也是这么来的。
            // 形态必须按**求值后**判：声明常被 Mapped/Reference/Concrete/Template/Indexed 包着
            // （argument: dispatch[[modifier]]<T> 就是），按声明 kind 判会一路漏写空壳 ——
            // 于是行上没有编辑器、没有 ⊕、展开又不建子级（必选空壳不摊开），整行是死的。
            if (node.IsRequired && emptyText && node.DeclaredType is not null)
            {
                // 先看声明的直接形态（没有 runtime 的入口——探针/命令——也能照旧工作），
                // 声明是指令包装（Mapped/Reference/Concrete/Template/Indexed）时才求值一次看穿它。
                var containerKind = node.DeclaredType.Kind is McdocTypeKind.Struct or McdocTypeKind.List or McdocTypeKind.Tuple
                    ? node.DeclaredType.Kind
                    : McdocDefaults.ResolveType(node.DeclaredType, runtime, node.ValueNode)?.Kind;
                if (containerKind is McdocTypeKind.Struct or McdocTypeKind.List or McdocTypeKind.Tuple)
                {
                    // 空壳必须**本身成立**：结构是空对象（里面的必填键交给不动点循环逐个补），
                    // 数组却不能用空数组 —— 定长容器（tuple / #[length=N] 的 list）的 [] 是非法值，
                    // 行上会永远挂着"列表长度 0 < 下限 N"，内联编辑器也因此一个可编辑项都没有。
                    // 数组空壳一律取默认值代数给出的形状（下限这么多个元素），与 ⊕ / 默认文档同一个出口。
                    WriteDisplayed(node, containerKind == McdocTypeKind.Struct
                        ? McdocJsonValue.Object()
                        : ArrayShell(node, runtime));
                }
            }
            // union 行：行上显示着某个成员，文档里却没有它的值 —— 按"所见即所得"把成员默认结构落盘。
            // 不落盘的话，行上选着 block_state_property、文档里没有 condition，同层就没有 Block/Properties
            // （调度器也因为没有可索引的键而解不出成员）。落盘后本轮 Load 会把它连同必填键一起解出来。
            if (node.Type?.Kind == McdocTypeKind.Union && node.Json is null
                && node.SelectedUnionMember is { } displayedMember && !McdocUnionSwitch.IsUnsetOption(node.SelectedUnionTypeName))
            {
                var unionDefault = McdocHelpers.GetChange(displayedMember, node.Type, null);
                if (unionDefault is not null) WriteDisplayed(node, unionDefault);
            }
            var hasOptions = node.EnumOptionList is { Count: > 0 };
            // 键已经在文档里、值却是空串（block: ""）：那是"有键没值"，必须按显示中的候选补上。
            // 可选字段只在"键已存在"时补，绝不因为"行上显示了默认值"就凭空插键。
            var presentButEmpty = node.Json is not null && emptyText;
            // "显示中的那个候选"：行的 Head 是构造时求值的，新长出来的子树（切成员/重建）还没有行，
            // SelectedEnumOption 就还是 null —— 这时按候选表首项落盘（行上下拉显示的正是它）。
            // 只对必填字段生效；哨兵永远不落盘。
            var shown = node.SelectedEnumOption
                ?? (hasOptions && !McdocUnionSwitch.IsUnsetOption(node.EnumOptionList[0]) ? node.EnumOptionList[0] : null);
            // 注意：不能把"值不在候选内"也当成待物化 —— 判别键的候选字面量常常不带命名空间
            // （dimension_type 的候选写作 weighted_list，文档里合法地写着 minecraft:weighted_list），
            // 按字符串比会把它判成非法并改写成不带前缀的那一支，整个结构当场没了。
            // 运行数据就绪前落下的那版值，由宿主"按原始文档重来"（RefreshWithReadyRuntime）修，
            // 不在物化这条热路径上猜。
            if ((node.IsRequired || presentButEmpty) && emptyText
                && (node.UseSelect || hasOptions) && shown?.Value?.LiteralValue is not null)
                {
                    var text = Convert.ToString(shown.Value.LiteralValue);
                    if (!string.IsNullOrEmpty(text))
                    {
                        WriteDisplayed(node, McdocJsonValue.String(text));
                    }
                }
                // 数字 / bool / union 的标量支：只在"载入渲染完毕"这一路补（includeDisplayedDefaults），
                // 结构变更后不补 —— 否则编辑时会被凭空插进一堆默认键，把"只删这一个键"搞脏。
                else if (includeDisplayedDefaults && node.Json is null && DisplayedScalar(node) is { } displayed)
                {
                    WriteDisplayed(node, displayed);
                }
                // 注意：这里**不能** EnsureChildren —— 物化是在每次 Load 之后跑的，
                // 顺手展开会重建子树、把用户正在操作的行换成脱挂节点（表现为"选了候选 JSON 没变"）。
                foreach (var child in McdocPresenterRegistry.Default.Children(node)) Walk(child);
            }

            // 借容器规则（不变量 25）：只有 union 摊平出来的分支层可以往上找容器；
            // 可选字段缺省不是分支层，不许把子键提到祖先上（曾经把 title 提到根上）。
            void WriteDisplayed(McdocTreeNode node, McdocJsonValue value)
            {
                var container = node.Parent;
                while (container is not null && container.Json is null)
                {
                    if (string.IsNullOrEmpty(container.UnionBranchKind)) { container = null; break; }
                    container = container.Parent;
                }
                if (container?.Json is not { Kind: McdocJsonKind.Object } target || string.IsNullOrEmpty(node.FieldName)) return;
                var wasEmpty = node.Json is null;
                node.Json = value;
                SetMemberOrdered(target, node, value);
                written++;
                writtenNodes?.Add(new McdocMaterializeWrite { Node = node, WasEmpty = wasEmpty });
            }

            foreach (var root in roots ?? []) Walk(root);
            return written;
        }

        /// <summary>
        /// 必填数组容器的空壳：取默认值代数给出的数组（定长时长度已经满足下限），解不出才退空数组。
        /// 手写 [] 会让定长容器从第一拍就非法：行上挂着长度告警，内联元组也没有可编辑的项。
        /// </summary>
        private static McdocJsonValue ArrayShell(McdocTreeNode node, McdocRuntime runtime)
            => McdocDefaults.Resolve(node.DeclaredType, runtime, node.ValueNode) is { Kind: McdocJsonKind.Array } array
                ? array
                : McdocJsonValue.Array();

        /// <summary>
        /// 行上当前"显示"的标量值（数字 / bool / union 的标量支）。没有就不物化：
        /// 数字和字符串没有隐式默认值，凭空写 0 或 "" 会把用户的文档弄脏。
        /// </summary>
        private static McdocJsonValue DisplayedScalar(McdocTreeNode node)
        {
            var isBoolRow = node.Type?.Kind == McdocTypeKind.Boolean || node.ScalarEditorKind == "Bool";
            if (isBoolRow)
            {
                if (node.IsTrue) return McdocJsonValue.Boolean(true);
                if (node.IsFalse) return McdocJsonValue.Boolean(false);
                // bool 的默认值就是 false（mcdoc 语义）：行上显示 False，JSON 里也要有 false
                node.SetBoolSilently(false);
                return McdocJsonValue.Boolean(false);
            }
            return ToNumber(node.Value);
        }

        /// <summary>
        /// 默认值生成的可变输入：#[pack_format] 跟着文档版本走，不再写死 48；
        /// 并且与页面默认值同口径（字段类型先求值）——否则 ⊕ 加出来的条目与默认文档对同一 union 字段取到不同的值。
        /// </summary>
        private static McdocDefaultOptions DefaultOptions(McdocRuntime runtime) => McdocDefaults.Options(runtime);

        /// <summary>动态键行的确认：占位行新增一个键；已有行把键改名（重名或空名直接忽略）。</summary>
        public void CommitEntry(McdocTreeNode mapNode, McdocTreeNode row, McdocRuntime runtime)
        {
            if (EnsureContainer(mapNode, McdocJsonKind.Object) is not { Kind: McdocJsonKind.Object } map)
            {
                return;
            }
            map.Properties ??= [];
            var key = row?.EditingKey?.Trim();
            if (string.IsNullOrEmpty(key))
            {
                // 键留空也能加：自动生成一个唯一键（有注册表候选时取第一个没用的）
                key = NextEntryKey(mapNode, row);
                row.EditingKey = key;
            }
            if (map.Properties.Any(pair => pair.Key == key))
            {
                return;
            }

            if (row.Json is not null)
            {
                for (var i = 0; i < map.Properties.Count; i++)
                {
                    if (!ReferenceEquals(map.Properties[i].Value, row.Json)) continue;
                    map.Properties[i] = new KeyValuePair<string, McdocJsonValue>(key, map.Properties[i].Value);
                    mapNode.StructureChanged?.Invoke(mapNode);
                    return;
                }
                return;
            }

            // 值类型常常依赖键本身（如 minecraft:data_component[[%key]]），
            // 必须把新键作为 %key 上下文再求值，否则拿到的是调度兜底类型。
            var keyNode = new McdocRuntimeNode(key, McdocType.LiteralString(key));
            var self = McdocValueNode.Of(new McdocRuntimeNode(null, McdocType.Any()), null, keyNode);
            // 只有一个出口：必选字段由代数递归带上，不许按类型写"struct 就给空对象"
            var inserted = McdocDefaults.Resolve(row.DeclaredType, runtime, self);
            row.Error = McdocDefaults.IsUnresolved(row.DeclaredType, runtime, self)
                ? "该键的值类型没解出来（已按 any 的首分支生成默认值）"
                : null;
            // 追加位置：紧跟"同一个动态字段的最后一个已有键"之后（该字段还没键时追加到末尾）。
            var insertIndex = EntryInsertIndex(mapNode, row, map);
            map.Properties.Insert(insertIndex, new KeyValuePair<string, McdocJsonValue>(key, inserted));
            // 新条目此刻还没有节点（要等重建）：把"展开"记在它的路径上，重建建出来就是展开的
            McdocExpandState.Set(mapNode.JsonPath + "." + key, true);
            mapNode.StructureChanged?.Invoke(mapNode);
            RevealCreated(mapNode);
        }

        /// <summary>⊕ 是否可用：键非空、没被占用；已有行还要求键确实变了。</summary>
        public bool CanCommitEntry(McdocTreeNode mapNode, McdocTreeNode row)
        {
            if (mapNode is null || row is null) return false;
            var key = row.EditingKey?.Trim();
            if (string.IsNullOrEmpty(key)) return false;
            if (mapNode.Json is null) return true;
            if (mapNode.Json is not { Kind: McdocJsonKind.Object } map || map.Properties is null) return false;
            if (row.Json is null) return map.Properties.All(pair => pair.Key != key);
            return row.FieldName != key && map.Properties.All(pair => pair.Key != key);
        }

        /// <summary>
        /// 任何"添加"手势之后统一揭示被创建的东西：展开它、记住展开态、并让投影重拼父行。
        /// 以前只有 `SetDefault` 自己置 IsExpanded，map 条目 / 列表新项都不展开 ——
        /// 表现就是"有的自动展开、有的要手动再点一下"。
        /// </summary>
        public void RevealCreated(McdocTreeNode node)
        {
            if (node is null) return;
            node.IsExpanded = true;
            McdocExpandState.Set(node.JsonPath, true);
            node.Parent?.StructureChanged?.Invoke(node.Parent ?? node);
        }

        /// <summary>缺省字段首次编辑：把值挂到模型上，并把缺失的祖先对象/数组补进 JSON。</summary>
        public void ApplyValue(McdocTreeNode node, McdocJsonValue value)
        {
            // 祖先里有 union：文档当前形状和"选中的分支"不一致时先转换（标量 ↔ 结构）。
            // 否则结构分支上的子字段没有合法落点，编辑会被静静丢掉。
            for (var ancestor = node?.Parent; ancestor is not null; ancestor = ancestor.Parent)
            {
                if (ancestor.Type?.Kind == McdocTypeKind.Union) ancestor.ConvertJsonToSelectedBranch();
            }
            if (node is null || value is null) return;
            // 顺序很关键：先补出祖先容器，再把值写回父容器，
            // 否则父里已有的 null 会把刚写入的值覆盖回去（"种了值却仍是 null"）
            // 值从"没有"变成"有"：删除字段时冻结的子级要放开，否则 ⊕ 之后看不到子行（结构回来了、行回不来）
            var wasAbsent = node.Json is null;
            EnsurePath(node);
            node.Json = value;
            if (wasAbsent) node.ChildrenBuilt = false;
            WriteIntoParent(node, value);
        }

        /// <summary>
        /// 把行上编辑出来的值写回 JSON——值编辑的唯一入口（顺序仍由 <see cref="ApplyValue"/> 保证）。
        /// 用 presenter 算好的 `Head.EditorKind` 分派，不在编辑层再按 `McdocTypeKind` 判类型。
        /// </summary>
        public bool ApplyNodeValue(McdocTreeNode node)
        {
            var value = ReadEditedValue(node);
            if (value is null)
            {
                // 可选 bool 两个按钮都取消：这是 unset，不是"默认 false"。
                // union 标量 bool 必须回到未设置（删键/清分支），普通 bool 删键；
                // 否则界面已取消选中、JSON 还停在上一代。
                if (TryUnsetOptionalBool(node)) return true;
                // 候选下拉行（#[id] / 枚举的文本变体）：编辑器里显示的是候选，空文本不等于"用户要写空串"。
                // 写 "" 会让这个键带着一个非法值留在文档里 —— 依赖它的 dispatcher 索引
                // （properties: mcdoc:block_states[[block]]）整片解不出来，行上还会挂"值不在候选内"。
                // 只要行上有非哨兵的选中项，落盘的就是它（所见即所得）；真 unset 走下面的哨兵分支。
                if (node.EnumOptionList is { Count: > 0 } && node.SelectedEnumOption is { } shown
                    && !McdocUnionSwitch.IsUnsetOption(shown) && EnumValue(shown) is { } candidate)
                {
                    ApplyValue(node, candidate);
                    return true;
                }
                // union 行镜像着的标量分支：空编辑器 = "这一支的默认标量"（与切分支同一个出口），不是"删键"。
                // 删键会把**分支身份**一起删掉：文档里没有这个键 → 下一次重建 union 只能回到未设置，
                // 用户看到的就是"选了 String 分支立刻弹回（未设置）"（chat_type 的 color 正是这一条）。
                // unset 在下拉里是显式的一项（未设置哨兵），不走"清空编辑器"这条路。
                var branchValue = ScalarBranchValue(node);
                if (branchValue is not null)
                {
                    ApplyValue(node, branchValue);
                    return true;
                }
                // 编辑器的"空状态"（文本框清空 / bool 两个都取消选中）语义 == unset：
                // 可选字段直接把键从文档里删掉；必填写空要有可见诊断，不许静默不动。
                ApplyEmptyValue(node);
                return false;
            }
            ApplyValue(node, value);
            return true;
        }

        /// <summary>
        /// 可选 bool 两个按钮都取消：普通字段删键；union 标量 bool 回到未设置分支。
        /// 必选 bool 不在这里处理（IsTrue/IsFalse 的互斥仍保证恰有一个选中）。
        /// </summary>
        private bool TryUnsetOptionalBool(McdocTreeNode node)
        {
            if (node is null || node.IsRequired || node.IsTrue || node.IsFalse) return false;
            if (node.Type?.Kind == McdocTypeKind.Union && node.ScalarEditorKind == "Bool")
            {
                if (node.Parent?.Json is not { Kind: McdocJsonKind.Object }) return false;
                var unset = node.UnionTypeNameList?.FirstOrDefault(McdocUnionSwitch.IsUnsetOption)
                    ?? new EnumMember
                    {
                        Name = McdocNodePresenter.UnsetLabel,
                        Value = new MetaValue { LiteralValue = -1 }
                    };
                return McdocUnionSwitch.SwitchTo(node, unset);
            }
            if (node.Json is not { Kind: McdocJsonKind.Boolean }) return false;
            if (node.RemoveFieldCommand?.CanExecute(null) == true)
            {
                node.RemoveFieldCommand.Execute(null);
                return true;
            }
            if (node.Parent?.Json is { Kind: McdocJsonKind.Object })
            {
                RemoveField(node);
                return node.Json is null;
            }
            return false;
        }

        /// <summary>
        /// union 行上镜像着的标量分支的默认值（与切分支同一个口径 <see cref="McdocHelpers.GetChange"/>）；
        /// 不是"union + 标量镜像"时返回 null，交回既有的 unset 语义。
        /// </summary>
        private static McdocJsonValue ScalarBranchValue(McdocTreeNode node)
        {
            if (node?.Type?.Kind != McdocTypeKind.Union || node.ScalarEditorKind is null) return null;
            var member = node.SelectedUnionMember;
            return member is null ? null : McdocHelpers.GetChange(member, node.Type, node.Json);
        }

        /// <summary>编辑器被清空：可选字段删键（misode 的 unset），必填字段挂诊断。</summary>
        public void ApplyEmptyValue(McdocTreeNode node)
        {
            if (node is null) return;
            // map 条目：清空 = **只把键从文档里去掉，行留在树上**（README「条目的（未设置）」）。
            // 走 RemoveField 会连整棵子树一起断掉 —— 用户看到的是"清个值，行直接没了"。
            if (node.IsMapEntry && !node.IsRequired && node.Json is not null
                && node.RemoveEntryCommand?.CanExecute(null) == true)
            {
                UnsetEntryValue(node);
                // UnsetEntryValue 按设计不发通知（父级重建会把这一行冲掉）：这里清完镜像后只发一次
                node.RaiseValueChanged();
                return;
            }
            if (!node.IsRequired && node.Json is not null && node.RemoveFieldCommand?.CanExecute(null) == true)
            {
                node.RemoveFieldCommand.Execute(null);
                return;
            }
            if (node.IsRequired) node.Error = "必填字段不能清空（请输入值）";
        }

        private static McdocJsonValue ReadEditedValue(McdocTreeNode node)
        {
            if (node?.Head is not { } head) return null;
            // any 行（AnyHead）：值控件是按 SelectedAnyType 换的，写回也必须按同一个类型出口。
            // 以前这条一律落到 Raw 分支 return null → ApplyEmptyValue → RemoveField，
            // 表现就是"选中 string 后敲第一个字符，键从 JSON 里消失、行也没了"。
            if (node.Type?.Kind is McdocTypeKind.Any or McdocTypeKind.Unsafe) return ReadAnyValue(node);
            // 下拉里选中（未设置）= unset（走 ApplyEmptyValue 删键），与这一行用的是 Enum 还是
            // "#[id] 只有下拉"的 Text 变体无关。union 行不算：它的（未设置）在分支下拉里。
            if (node.Type?.Kind != McdocTypeKind.Union
                && node.EnumOptionList is { Count: > 0 } options
                && McdocUnionSwitch.IsUnsetOption(options[0])
                && McdocUnionSwitch.IsUnsetOption(node.SelectedEnumOption))
            {
                // 选中的是（未设置）哨兵 = 正常 unset。但 **可输入下拉里用户自己敲的值** 走的是 Value 这一面，
                // 哨兵不会因为"敲了字"而换掉 —— 一律当 unset 会让"敲完等于没敲"
                // （现场：advancement.parent 写进 Value、文档里还是空）。
                var typed = node.Value as string;
                if (string.IsNullOrEmpty(typed) || string.Equals(typed, options[0]?.Name, StringComparison.Ordinal))
                {
                    return null;
                }
            }
            return head.EditorKind switch
            {
                // 枚举写回的是**字面量值**（Block = "block"），不是标识符（Block）；
                // 写标识符会让 JSON 语义错误，且重解析后选不回原项
                McdocEditorKind.Enum => EnumValue(node.SelectedEnumOption),
                // bool：可选节点允许"两个都不选"，这就是"没有值"（回落到删键）
                McdocEditorKind.Bool => !node.IsRequired && !node.IsTrue && !node.IsFalse
                    ? null : McdocJsonValue.Boolean(node.IsTrue),
                McdocEditorKind.Number => ToNumber(node.Value),
                // 必填字符串的空串只在"没有候选"时才算合法值（写 ""）；候选下拉行的空文本不是写空串 ——
                // 行上选着候选就意味着用户看到的值是那个候选，交给 ApplyNodeValue 的候选物化 / unset 语义。
                McdocEditorKind.Text => string.IsNullOrEmpty(Convert.ToString(node.Value))
                    ? (node.EnumOptionList is { Count: > 0 } ? null : (node.IsRequired ? McdocJsonValue.String("") : null))
                    : McdocJsonValue.String(Convert.ToString(node.Value)),
                // Raw 编辑器自己解析成 Json 了；None / InlineTuple 没有行内值（子项各走各的行）
                _ => null,
            };
        }

        /// <summary>
        /// any 行的值出口，与 AnyHead 的控件一一对应：string→文本、double→数字、boolean→False/True。
        /// list / struct / 认不出的形状这一行**没有行内值**（值在子行里、或已由 RawJson 写在 node.Json 上）——
        /// 返回**当前值**让写回幂等；返回 null 会被 ApplyEmptyValue 当成 unset，把键删掉。
        /// 哨兵（未设置）仍然返回 null：那才是"这个键不存在"。
        /// </summary>
        private static McdocJsonValue ReadAnyValue(McdocTreeNode node)
        {
            if (McdocUnionSwitch.IsUnsetOption(node.SelectedAnyType)) return null;
            // 只看稳定键（显示名是大写的 Boolean/String…，不能拿它当判据）
            return node.AnyTypeKey switch
            {
                "string" => McdocJsonValue.String(System.Convert.ToString(node.Value) ?? ""),
                "double" => ToNumber(node.Value),
                "boolean" => McdocJsonValue.Boolean(node.IsTrue),
                _ => node.Json,
            };
        }

        /// <summary>EnumMember.Value 的字面量 → JSON 值（字符串/布尔/数值三种枚举底层）。</summary>
        private static McdocJsonValue EnumValue(EnumMember option)
        {
            if (McdocUnionSwitch.IsUnsetOption(option)) return null;   // 哨兵永不落盘
            var literal = option?.Value?.LiteralValue;
            return literal switch
            {
                null => null,
                string text => McdocJsonValue.String(text),
                bool flag => McdocJsonValue.Boolean(flag),
                decimal number => McdocJsonValue.Number(number),
                _ => decimal.TryParse(Convert.ToString(literal), out var parsed)
                    ? McdocJsonValue.Number(parsed)
                    : McdocJsonValue.String(Convert.ToString(literal)),
            };
        }

        private static McdocJsonValue ToNumber(object value) => value switch
        {
            decimal number => McdocJsonValue.Number(number, number == decimal.Truncate(number)),
            double floating => McdocJsonValue.Number((decimal)floating, floating == Math.Truncate(floating)),
            float single => McdocJsonValue.Number(Convert.ToDecimal(single), single == Math.Truncate(single)),
            int or long or short or byte => McdocJsonValue.Number(Convert.ToDecimal(value), true),
            _ => null,
        };

        /// <summary>对象成员写入的唯一原语：同键替换、缺键追加（重复键会让 dispatcher 读到旧值）。</summary>
        public static void SetMember(McdocJsonValue target, string key, McdocJsonValue value)
        {
            if (target is not { Kind: McdocJsonKind.Object } || string.IsNullOrEmpty(key)) return;
            target.Properties ??= [];
            for (var i = 0; i < target.Properties.Count; i++)
            {
                if (target.Properties[i].Key != key) continue;
                target.Properties[i] = new KeyValuePair<string, McdocJsonValue>(key, value);
                return;
            }
            target.Add(key, value);
        }

        /// <summary>
        /// 按"树序"写入成员：键已存在就原位替换；缺键时插到"树序上排在它后面、且已经在文档里"的兄弟之前。
        /// 纯追加会让新键永远跑到末尾，和左侧树的相对顺序对不上。
        /// </summary>
        public static void SetMemberOrdered(McdocJsonValue target, McdocTreeNode node, McdocJsonValue value)
        {
            if (target is not { Kind: McdocJsonKind.Object } || node is null || string.IsNullOrEmpty(node.FieldName)) return;
            target.Properties ??= [];
            for (var i = 0; i < target.Properties.Count; i++)
            {
                if (target.Properties[i].Key != node.FieldName) continue;
                target.Properties[i] = new KeyValuePair<string, McdocJsonValue>(node.FieldName, value);
                return;
            }
            var insertAt = SchemaInsertIndex(target, node);
            var siblings = node.Parent?.Children;
            if (siblings is not null)
            {
                var seenSelf = false;
                foreach (var sibling in siblings)
                {
                    if (ReferenceEquals(sibling, node)) { seenSelf = true; continue; }
                    if (!seenSelf || string.IsNullOrEmpty(sibling?.FieldName)) continue;
                    var index = target.Properties.FindIndex(pair => pair.Key == sibling.FieldName);
                    if (index >= 0) { insertAt = index; break; }
                }
            }
            target.Properties.Insert(insertAt, new KeyValuePair<string, McdocJsonValue>(node.FieldName, value));
        }

        /// <summary>
        /// 树里找不到"排在它后面的已存在兄弟"时的退路：按结构声明序，插到"文档里已有的、声明序在它之后的第一个字段"之前。
        /// 取不到声明序就返回末尾（追加）。
        /// </summary>
        private static int SchemaInsertIndex(McdocJsonValue target, McdocTreeNode node)
        {
            var count = target.Properties?.Count ?? 0;
            var fields = node.Parent?.Type?.Fields;
            if (fields is null || fields.Count == 0 || string.IsNullOrEmpty(node.FieldName)) return count;
            var order = fields.Where(field => field?.Name is { Length: > 0 })
                .Select(field => field.Name).ToList();
            var self = order.IndexOf(node.FieldName);
            if (self < 0) return count;
            for (var i = self + 1; i < order.Count; i++)
            {
                var index = target.Properties.FindIndex(pair => pair.Key == order[i]);
                if (index >= 0) return index;
            }
            return count;
        }

        /// <summary>把节点的值写进父对象/数组（已存在则替换，不产生重复键）。</summary>
        public void WriteIntoParent(McdocTreeNode node, McdocJsonValue value)
        {
            if (node?.Parent is null)
            {
                // 只有"文档根"才允许替换整份文档：其余没有父级的节点都是脱挂的陈旧节点，
                // 让它们 ReplaceRoot 会把子树内容顶到根上（删 display 却出现 { title: x }）
                if (node.IsDocumentRoot && node.ReplaceRoot is not null)
                {
                    McdocWriteTrace.Log("ReplaceRoot", node, "<document-root>");
                    node.ReplaceRoot(value);
                }
                else
                {
                    // 重建期间旧节点被换掉是**预期**：这类跳过不许变成行上的感叹号
                    // （现场日志里 41 条"写回已跳过"把黄标永久钉在 block/condition 上）。只留 trace，
                    // 行上的诊断只许来自"当前状态"的校验。
                    McdocWriteTrace.Log("ReplaceRoot 跳过（脱挂节点）", node, "<无父级>");
                }
                return;
            }
            McdocWriteTrace.Log("WriteIntoParent", node, "key=" + node.FieldName
                + " parent=" + (node.Parent?.JsonPath ?? "?"));

            switch (node.Parent.Json)
            {
                case { Kind: McdocJsonKind.Object } parent when !string.IsNullOrEmpty(node.FieldName):
                    SetMemberOrdered(parent, node, value);
                    return;
                case { Kind: McdocJsonKind.Array } array when int.TryParse(node.FieldName, out var index):
                    array.Items ??= [];
                    while (array.Items.Count <= index) array.Items.Add(McdocJsonValue.Null());
                    array.Items[index] = value;
                    return;
            }
        }

        /// <summary>从根往下依次保证"父容器里有这个键"，缺的就按结构化默认值补出来。</summary>
        public void EnsurePath(McdocTreeNode node)
        {
            if (node?.Parent is null) return;
            var chain = new List<McdocTreeNode>();
            for (var current = node; current is not null; current = current.Parent) chain.Add(current);
            chain.Reverse();

            for (var i = 1; i < chain.Count; i++)
            {
                var parent = chain[i - 1];
                var child = chain[i];
                if (parent.Json is not { } parentJson) return;

                if (parentJson.Kind == McdocJsonKind.Object && !string.IsNullOrEmpty(child.FieldName))
                {
                    if (parentJson.Get(child.FieldName) is null)
                    {
                        // 纯追加会把新键排到末尾（后出现的 parent 跑到 sends_telemetry_event 后面）：
                        // 文档顺序必须跟树的相对位置一致，所以这里也走"按树序插入"的同一个出口
                        SetMemberOrdered(parentJson, child, child.Json ?? McdocJsonValue.Object());
                    }
                    if (child.Json is null)
                    {
                        child.Json = parentJson.Get(child.FieldName);
                    }
                    continue;
                }
                if (parentJson.Kind == McdocJsonKind.Array && int.TryParse(child.FieldName, out var index))
                {
                    parentJson.Items ??= [];
                    while (parentJson.Items.Count <= index) parentJson.Items.Add(child.Json ?? McdocJsonValue.Null());
                    if (parentJson.Items[index] is null) parentJson.Items[index] = child.Json ?? McdocJsonValue.Null();
                    child.Json = parentJson.Items[index];
                }
            }
        }

        /// <summary>删掉字段本身（misode 的 unset / FieldRemove）：可选字段清空成"不存在"。</summary>
        public void RemoveField(McdocTreeNode node)
        {
            if (node?.Parent?.Json is not { Kind: McdocJsonKind.Object } parent || string.IsNullOrEmpty(node.FieldName))
            {
                return;
            }
            if (parent.Properties is null) return;
            McdocWriteTrace.Log("RemoveField 前", node, "parentKeys=" + McdocWriteTrace.Keys(parent)
                + " nodeKeys=" + McdocWriteTrace.Keys(node.Json) + " parentField=" + (node.Parent?.FieldName ?? "<无>"));
            var removed = parent.Properties.RemoveAll(pair => pair.Key == node.FieldName);
            McdocWriteTrace.Log("RemoveField 后", node, "removed=" + removed + " parentKeys=" + McdocWriteTrace.Keys(parent));
            if (removed == 0) return;
            // 删掉之后这棵子树不再属于文档：就地清空，避免物化/回写流程顺着陈旧子节点
            // 把值写回父级（曾经出现"删 display，title 被提到根上"）
            node.Json = null;
            node.Children.Clear();
            node.Items.Clear();
            node.SelectedUnionChildren.Clear();
            node.ChildrenBuilt = true;
            // 值也跟着断：不然 bool 行的 True/False 还挂着上一份镜像（删掉键了，行上却仍有一个选中）
            node.SyncScalarFromJson();
            node.Parent.StructureChanged?.Invoke(node.Parent);
        }

        /// <summary>删掉这个动态键（从父对象里移除对应的属性）。</summary>
        public void RemoveEntry(McdocTreeNode mapNode, McdocTreeNode row)
        {
            if (mapNode is null || row is null) return;
            if (row.Json is null)
            {
                // 值已经被（未设置）清掉：键早就不在文档里了，这里只把这一行摘掉。
                // 走父级重建会把同一父级下**其它行**一起换掉（幽灵行的老坑），所以只摘行 + 让投影重拼父行。
                mapNode.Children.Remove(row);
                mapNode.Items.Remove(row);
                mapNode.SelectedUnionChildren.Remove(row);
                row.Parent = null;
                mapNode.StructureChanged?.Invoke(mapNode);
                return;
            }
            if (mapNode.Json is not { Kind: McdocJsonKind.Object } map || map.Properties is null) return;
            map.Properties.RemoveAll(pair => ReferenceEquals(pair.Value, row.Json));
            mapNode.StructureChanged?.Invoke(mapNode);
        }

        /// <summary>
        /// map 条目的（未设置）：**只把键从文档里去掉，行留在树上**（用户还要能再选分支、或直接删掉这一行）。
        /// 与 RemoveField 的区别：那条是"这个字段不存在了，子树一起断"，会连行一起没了。
        /// 只走值通道（文本同步），不抬结构变更 —— 结构变更会重建父级，按 JSON 重建的父级不会再有这一行。
        /// </summary>
        public void UnsetEntryValue(McdocTreeNode node)
        {
            if (node?.Parent?.Json is not { Kind: McdocJsonKind.Object } parent || string.IsNullOrEmpty(node.FieldName)) return;
            parent.Properties?.RemoveAll(pair => pair.Key == node.FieldName);
            node.Json = null;
            node.Children.Clear();
            node.Items.Clear();
            node.SelectedUnionChildren.Clear();
            node.ChildrenBuilt = true;      // 键不在了：子级不重建（与 RemoveField 同一口径）
            node.SyncScalarFromJson();
            McdocWriteTrace.Log("条目（未设置）只删键", node, "parentKeys=" + McdocWriteTrace.Keys(parent));
            // 这里**不许**自己发值通知：此刻分支镜像（ScalarEditorKind / 子级）还挂着，
            // 宿主收到通知会按"这不是结构变更"把节点的旧值重新 ApplyValue 写回文档 —— 键当场又回来了。
            // 通知由 SwitchTo 在清完镜像之后统一发一次。
        }

        /// <summary>列表动作的"元素类型来源"：union 用选中的成员类型，否则用节点自身的类型。</summary>
        private static McdocType ItemOwnerType(McdocTreeNode node)
            => node?.Type?.Kind == McdocTypeKind.Union && node.SelectedUnionMember is not null
                ? node.SelectedUnionMember
                : node?.Type;

        /// <summary>union 的列表分支：加/删条目后必须保持展开，否则重建出来的节点一折叠，条目行就整片消失。</summary>
        private static void KeepListBranchExpanded(McdocTreeNode node)
        {
            if (node?.UnionListLike != true) return;
            node.IsExpanded = true;
            McdocExpandState.Set(node.JsonPath, true);
        }

        /// <summary>
        /// 结构变更的通知目标：union 的列表分支是**临时节点**（不挂在树上，条目直接挂 union 的
        /// SelectedUnionChildren），在它身上抬 StructureChanged 等于重建一个脱挂的壳 ——
        /// 宿主那边 RefreshAround 找不到父行、Reset 出来的还是旧树，表现就是"删了条目，右侧 JSON 少了、左侧树一动不动"。
        /// 一律向上找到最近的"挂在树上"的祖先再通知（找不到就退回自己）。
        /// </summary>
        private static void NotifyStructure(McdocTreeNode node)
        {
            if (node is null) return;
            // 判据用"父级的三个子级集合里有没有它"：union 的列表/结构分支本身不会被 Add 进父级
            // （子级被上提了），只有后者能代表"能重建的那一层"
            var target = node;
            while (target.Parent is { } parent && !Contains(parent, target))
            {
                target = parent;
            }
            (target.StructureChanged ?? node.StructureChanged)?.Invoke(target);
        }

        private static bool Contains(McdocTreeNode parent, McdocTreeNode child)
            => parent.Children.Contains(child) || parent.Items.Contains(child) || parent.SelectedUnionChildren.Contains(child);

        /// <summary>
        /// 列表项自己的命令（删这一项 / 上移 / 下移）：下标必须**执行时**再问 `Items.IndexOf(item)` ——
        /// 建节点时捕获的下标会在任何一次移动/追加之后过期，表现就是"再点一次 ▲ 却把别的项挪走了"、
        /// "新加进来的项按 ▲ 没反应"。一个出口：列表表现器建项、`RebuildNode` 重建项都走这里。
        /// </summary>
        public void AttachListItemCommands(McdocTreeNode listNode, McdocTreeNode item)
        {
            if (listNode is null || item is null) return;
            // 下标必须在**执行时**现问，而且 owner 必须是"真的装着这一项"的那个节点。
            // union 的列表分支把子项上提到 SelectedUnionChildren（父级是 union 行、Items 是空的，
            // 见 McdocPresenters.cs:745-757）：只看 item.Parent.Items 会得到 -1 ——
            // 表现就是"根列表的 ▲▼ 点不动、⊖ 两个视图都没反应"（战利品表的 list 是真字段，所以不受影响）。
            var tracedMiss = false;
            bool Locate(out McdocTreeNode owner, out int index)
            {
                if (TryLocateItem(item, listNode, out owner, out index)) return true;
                if (!tracedMiss)
                {
                    tracedMiss = true;
                    McdocWriteTrace.Trace("list-item-owner-miss", "field=" + (item.FieldName ?? "<空>")
                        + " path=" + (item.JsonPath ?? "?") + " parent=" + (item.Parent?.JsonPath ?? "<null>"));
                }
                return false;
            }
            item.RemoveSelfCommand = new McdocCommand(() =>
            {
                if (Locate(out var owner, out var index)) RemoveItemAt(owner, index);
            });
            item.MoveUpCommand = new McdocCommand(() =>
            {
                if (Locate(out var owner, out var index) && index > 0) MoveItem(owner, index, index - 1);
            }, () => Locate(out _, out var index) && index > 0);
            item.MoveDownCommand = new McdocCommand(() =>
            {
                if (!Locate(out var owner, out var index)) return;
                if (index >= 0 && index < ItemCount(owner) - 1) MoveItem(owner, index, index + 1);
            }, () =>
            {
                if (!Locate(out var owner, out var index)) return false;
                return index >= 0 && index < ItemCount(owner) - 1;
            });
            item.RaiseListItemCommandStateChanged();
        }

        /// <summary>
        /// 谁真的装着这一项：先从 item.Parent 逐级上溯，再退回捕获的 listNode（本代分支节点）。
        /// 集合只认 Items 与"union 列表分支的 SelectedUnionChildren" —— 结构分支的字段也在
        /// SelectedUnionChildren 里，但那些不是数组项，不能让它们被当成条目移动。
        /// </summary>
        private static bool TryLocateItem(McdocTreeNode item, McdocTreeNode fallbackList, out McdocTreeNode owner, out int index)
        {
            owner = null;
            index = -1;
            if (item is null) return false;
            for (var candidate = item.Parent; candidate is not null; candidate = candidate.Parent)
            {
                if (!TryIndexOfItem(candidate, item, out index)) continue;
                owner = candidate;
                return true;
            }
            if (TryIndexOfItem(fallbackList, item, out index))
            {
                owner = fallbackList;
                return true;
            }
            index = -1;
            return false;
        }

        private static bool TryIndexOfItem(McdocTreeNode owner, McdocTreeNode item, out int index)
        {
            index = -1;
            if (owner is null || item is null) return false;
            if (owner.Items is { } items && (index = items.IndexOf(item)) >= 0) return true;
            if (owner.UnionListLike && owner.SelectedUnionChildren is { } branch
                && (index = branch.IndexOf(item)) >= 0)
            {
                return true;
            }
            index = -1;
            return false;
        }

        /// <summary>这一层"条目"的条数：union 列表分支数 SelectedUnionChildren，其余数 Items。</summary>
        private static int ItemCount(McdocTreeNode owner)
        {
            if (owner is null) return 0;
            if (owner.UnionListLike && owner.SelectedUnionChildren.Count > 0) return owner.SelectedUnionChildren.Count;
            return owner.Items?.Count ?? 0;
        }

        /// <summary>条目增删/移动之后：每一项的可用性都要重算并通知，否则按钮停在上一代的灰/亮。</summary>
        private static void NotifyItemCommandState(McdocTreeNode listNode)
        {
            if (listNode is null) return;
            foreach (var item in listNode.Items) item?.RaiseListItemCommandStateChanged();
            if (!listNode.UnionListLike) return;
            foreach (var item in listNode.SelectedUnionChildren) item?.RaiseListItemCommandStateChanged();
        }

        /// <summary>列表 ⊕（misode ListHead/ListBody 的 add_top / add_bottom）。</summary>
        public void AddItem(McdocTreeNode node, McdocRuntime runtime, bool atTop = false)
        {
            if (node?.Type is null || node.Type.Kind == McdocTypeKind.Tuple) return;
            if (EnsureContainer(node, McdocJsonKind.Array) is not { Kind: McdocJsonKind.Array } array) return;
            if (node.Type?.LengthRange is { Max: { } max } && (array.Items?.Count ?? 0) >= max) return;
            // any 行的 list 分支：元素类型就是 any（GetItemType 的 default 已经是 any，这里只把语义写清）
            var itemType = node.Type.Kind is McdocTypeKind.Any or McdocTypeKind.Unsafe
                ? McdocType.Any()
                : McdocHelpers.GetItemType(ItemOwnerType(node));
            var value = McdocDefaults.Resolve(itemType, runtime);
            array.Items ??= [];
            if (atTop) array.Items.Insert(0, value); else array.Items.Add(value);
            NotifyItemCommandState(node);
            KeepListBranchExpanded(node);
            // 新项此刻还没有节点（要等重建）；把"展开"记在它的路径上，重建建出来就是展开的 ——
            // 与 SetDefault / 条目同一个揭示口径，只是这里必须按路径预约
            McdocExpandState.Set(node.JsonPath + "[" + (atTop ? 0 : (array.Items.Count - 1)) + "]", true);
            // **列表自己**的展开态也必须先预约：StructureChanged 会把它重建一代，
            // 而 RevealCreated 这时手里的 node 已经是旧实例 —— 只作用在旧实例上，
            // 新实例按"默认折叠"建出来，于是"JSON 多了一条、左侧树一动不动"。
            McdocExpandState.Set(node.JsonPath, true);
            NotifyStructure(node);
            RevealCreated(node);
        }

        /// <summary>删除指定下标的列表项；删空且不允许为空的可选列表时，整个字段一起移除。</summary>
        public void RemoveItemAt(McdocTreeNode node, int index)
        {
            // tuple 是定长结构：删除会把长度改坏（AppendItem/AddItem 已拒绝，这里同样要守）
            if (node?.Type?.Kind == McdocTypeKind.Tuple) return;
            if (node?.Json is not { Kind: McdocJsonKind.Array } array || array.Items is null) return;
            if (index < 0 || index >= array.Items.Count)
            {
                // 文档里没有这一项，行却还在：这是"陈旧项"（某次结构通知被吞掉留下的残影）。
                // 此时按钮不能一点反应都没有 —— 把它当"清掉这一行"处理，并且让子级重建一次。
                var ghost = node.Items.FirstOrDefault(item => string.Equals(item.JsonPath, node.JsonPath + "[" + index + "]", StringComparison.Ordinal));
                if (ghost is not null)
                {
                    node.Items.Remove(ghost);
                    ghost.Parent = null;
                    node.ChildrenBuilt = false;
                    NotifyItemCommandState(node);
                    McdocWriteTrace.Log("RemoveItemAt 清掉陈旧项", ghost, "index=" + index + " items=" + node.Items.Count);
                    NotifyStructure(node);
                }
                return;
            }
            array.Items.RemoveAt(index);
            NotifyItemCommandState(node);
            // 数组少了项：子级一律按当前文档重建（少了这一步，通知被吞掉时就会留下"文档里没有、行还在"的残影）
            node.ChildrenBuilt = false;
            var min = node.Type?.LengthRange?.Min ?? 0m;
            if (array.Items.Count == 0 && min > 0 && node.IsRequired == false && node.Parent is not null)
            {
                RemoveField(node);
                return;
            }
            NotifyStructure(node);
        }

        /// <summary>列表项上移 / 下移。</summary>
        public void MoveItem(McdocTreeNode node, int from, int to)
        {
            if (node?.Json is not { Kind: McdocJsonKind.Array } array || array.Items is null) return;
            if (from < 0 || from >= array.Items.Count || to < 0 || to >= array.Items.Count || from == to) return;
            var value = array.Items[from];
            array.Items.RemoveAt(from);
            array.Items.Insert(to, value);
            NotifyItemCommandState(node);
            NotifyStructure(node);
        }

        /// <summary>拖拽/测试用：条目在哪个 List owner 的第几项（entry 自己不能猜 Parent.Items）。</summary>
        public bool TryLocateListItem(McdocTreeNode item, out McdocTreeNode owner, out int index)
            => TryLocateItem(item, item?.Parent, out owner, out index);

        /// <summary>拖拽/测试用：这个 owner 是否是允许拖拽重排的 List（tuple / 定长数组不算）。</summary>
        public bool CanReorderList(McdocTreeNode owner) => IsReorderableList(owner);

        /// <summary>两个条目是否属于同一个可重排 List（tuple / 定长数组 / map 条目都不算）。</summary>
        public bool CanSwapListItems(McdocTreeNode source, McdocTreeNode target)
        {
            if (source is null || target is null || ReferenceEquals(source, target)) return false;
            if (!TryLocateListItem(source, out var sourceOwner, out var sourceIndex)
                || !TryLocateListItem(target, out var targetOwner, out var targetIndex))
            {
                return false;
            }
            if (!ReferenceEquals(sourceOwner, targetOwner)) return false;
            if (!IsReorderableList(sourceOwner)) return false;
            return sourceIndex >= 0 && targetIndex >= 0;
        }

        /// <summary>
        /// 交换同一 List 父节点内两个条目的槽位（不是插入/移动）。JSON 数组是唯一真相：
        /// 交换值后走既有的列表命令状态 + 结构通知，投影与右侧文本由管线同步。
        /// </summary>
        public bool SwapListItems(McdocTreeNode source, McdocTreeNode target)
        {
            if (!CanSwapListItems(source, target)) return false;
            if (!TryLocateListItem(source, out var owner, out var sourceIndex)) return false;
            if (!TryLocateListItem(target, out _, out var targetIndex)) return false;
            if (owner.Json is not { Kind: McdocJsonKind.Array } array || array.Items is null) return false;
            if (sourceIndex < 0 || targetIndex < 0
                || sourceIndex >= array.Items.Count || targetIndex >= array.Items.Count) return false;
            (array.Items[sourceIndex], array.Items[targetIndex]) = (array.Items[targetIndex], array.Items[sourceIndex]);
            NotifyItemCommandState(owner);
            NotifyStructure(owner);
            return true;
        }

        private static bool IsReorderableList(McdocTreeNode owner)
        {
            if (owner?.Json is not { Kind: McdocJsonKind.Array }) return false;
            if (owner.Type?.Kind == McdocTypeKind.List) return true;
            // union 的列表分支：条目被上提到 SelectedUnionChildren，owner 自己是 union 节点。
            return owner.UnionListLike && owner.SelectedUnionMember?.Kind == McdocTypeKind.List;
        }

        /// <summary>
        /// 缺省字段的 ⊕：按声明类型造默认值写进父对象（misode StructHead 的 set default）。
        /// 值与"页面默认文档 / 列表追加 / 动态键提交"同一个出口，必选子字段递归带上。
        /// </summary>
        public void SetDefault(McdocTreeNode node, McdocRuntime runtime)
        {
            if (node is null) return;
            var declared = node.DeclaredType ?? node.Type;
            if (declared is null)
            {
                node.Error = "该字段没有可生成的默认结构（声明类型缺失）";
                return;
            }
            var value = McdocDefaults.Resolve(declared, runtime);
            // 派生容器（mcdoc:block_states[[block]] 这类）在无属性方块上解出零字段：
            // 点 ⊕ 不能把 {} 写进文档，否则切回有属性方块后 Json 已非空、⊕ 直接失效，
            // 用户看到的就是"点添加没反应"。普通空结构仍允许写 {}。
            var resolved = McdocDefaults.ResolveType(declared, runtime, node.ValueNode);
            if (value is { Kind: McdocJsonKind.Object } empty
                && (empty.Properties?.Count ?? 0) == 0
                && resolved is { Kind: McdocTypeKind.Struct } resolvedStruct
                && (resolvedStruct.Fields?.Count ?? 0) == 0
                && IsDerivedContainerDeclaration(declared, runtime))
            {
                return;
            }
            // 解不开按 any 处理（默认 {}），但必须让人看得见
            node.Error = McdocDefaults.IsUnresolved(declared, runtime) ? "该字段的类型没解出来（已按 any 生成空结构）" : null;
            ApplyValue(node, value);
            // 一键之后应该立刻看得到子字段行：与其它添加手势同一出口
            RevealCreated(node);
        }

        /// <summary>声明是不是调度器/索引派生容器（含 Reference/Mapped/Concrete/Template 壳）。</summary>
        private static bool IsDerivedContainerDeclaration(McdocType type, McdocRuntime runtime, int depth = 0)
        {
            if (type is null || depth > 8) return false;
            if (type.Kind is McdocTypeKind.Dispatcher or McdocTypeKind.Indexed) return true;
            if (type.Kind == McdocTypeKind.Reference)
            {
                McdocType target = null;
                try
                {
                    target = string.IsNullOrEmpty(type.Path)
                        ? null
                        : runtime?.Symbols?.GetTypeDef(type.Path, runtime.DocumentUri ?? "",
                            string.IsNullOrEmpty(type.Module) ? runtime.ModulePath : type.Module);
                }
                catch (Exception)
                {
                    target = null;
                }
                return target is not null && !ReferenceEquals(target, type)
                    && IsDerivedContainerDeclaration(target, runtime, depth + 1);
            }
            if (type.Kind == McdocTypeKind.Union)
            {
                foreach (var member in type.Members ?? [])
                {
                    if (IsDerivedContainerDeclaration(member, runtime, depth + 1)) return true;
                }
                return false;
            }
            return type.IsDirective && type.Item is not null
                && IsDerivedContainerDeclaration(type.Item, runtime, depth + 1);
        }

        /// <summary>
        /// 数值行的 +/-：按步长改值并立刻写回文档（缺键时 EnsurePath 会把这个键建出来）。
        /// 不依赖控件内部的步进——那条路不会触发 ValueChanged，JSON 永远不更新。
        /// </summary>
        public void StepNumber(McdocTreeNode node, decimal delta)
        {
            if (node is null) return;
            decimal current = node.Json is { Kind: McdocJsonKind.Number } json ? json.NumberValue : 0m;
            if (node.Json is null && node.Value is not null && decimal.TryParse(Convert.ToString(node.Value), out var parsed))
            {
                current = parsed;
            }
            var next = current + delta;
            var value = McdocJsonValue.Number(next, next == decimal.Truncate(next));
            var wasAbsent = node.Json is null;
            ApplyValue(node, value);
            // 走值的标准出口（node.Value）：行上的数字框重读自己、VM 那里还能把这一代文本写回，
            // 都在这一条通道上。以前这里什么都不发，靠"重建父级"顺带刷新 ——
            // 那会重造整层兄弟行（整表 Reset → 滚动视图跳一下、点一下卡一下），而且右侧文本要等下一次同步才跟上。
            node.Value = next;
            // 只有"这个键原来不在文档里"（形状真的变了）才需要结构变更；值变了不需要。
            if (wasAbsent) node.StructureChanged?.Invoke(node.Parent ?? node);
        }

        /// <summary>列表的"−"：删掉最后一项。</summary>
        public void RemoveLastItem(McdocTreeNode node)
        {
            if (node?.Json is not { Kind: McdocJsonKind.Array } array || array.Items is null || array.Items.Count == 0)
            {
                return;
            }
            array.Items.RemoveAt(array.Items.Count - 1);
            NotifyStructure(node);
        }

        /// <summary>右键"复制"：深拷贝本节点的 JSON 值并插到同层（仅列表项；对象属性复制会产生重复键）。</summary>
        public void DuplicateItem(McdocTreeNode node)
        {
            var parent = node?.Parent;
            if (parent?.Json is not { Kind: McdocJsonKind.Array } array || array.Items is null || node.Json is null)
            {
                return;
            }
            // 定长结构（tuple / #[length=N]）复制会改坏长度：与 AddItem / RemoveItemAt 同一判据
            if (parent.Type?.Kind == McdocTypeKind.Tuple) return;
            if (parent.Type?.LengthRange is { Max: { } max } && array.Items.Count >= max) return;
            var index = array.Items.FindIndex(item => ReferenceEquals(item, node.Json));
            if (index < 0)
            {
                return;
            }
            array.Items.Insert(index + 1, node.Json.Clone());
            // 与 AddItem 同一条收尾：通知目标是**列表 owner**，不是被复制的那一项。
            // 只通知 item 的话宿主只刷新它的子树 —— 右侧 JSON 多了一项、左侧列表没有对应行。
            NotifyItemCommandState(parent);
            KeepListBranchExpanded(parent);
            // 新项此刻还没有节点（要等列表重建）：按路径预约展开态，重建出来才是可见的
            var path = parent.JsonPath ?? "$";
            McdocExpandState.Set(path + "[" + (index + 1) + "]", true);
            McdocExpandState.Set(path, true);
            NotifyStructure(parent);
            RevealCreated(parent);
        }

        /// <summary>节点还没有 JSON 时（如可选字段缺省），先在父对象里把容器建出来。</summary>
        public McdocJsonValue EnsureContainer(McdocTreeNode node, McdocJsonKind kind)
        {
            if (node is null) return null;
            if (node.Json is { } existing) return existing.Kind == kind ? existing : null;
            if (node.Parent?.Json is not { Kind: McdocJsonKind.Object } parent || string.IsNullOrEmpty(node.FieldName))
            {
                return null;
            }
            var created = kind == McdocJsonKind.Array ? McdocJsonValue.Array() : McdocJsonValue.Object();
            parent.Add(node.FieldName, created);
            node.Json = created;
            return created;
        }

        /// <summary>键为空时自动挑一个：优先注册表里第一个没被占用的候选，否则 key / key2 / key3…</summary>
        public string NextEntryKey(McdocTreeNode mapNode, McdocTreeNode row)
        {
            var existing = mapNode?.Json is { Kind: McdocJsonKind.Object } map && map.Properties is not null
                ? map.Properties.Select(pair => pair.Key).ToHashSet(StringComparer.Ordinal)
                : [];
            foreach (var option in row?.KeyOptionList ?? [])
            {
                if (!string.IsNullOrEmpty(option?.Name) && !existing.Contains(option.Name)) return option.Name;
            }
            for (var i = 1; i < 1000; i++)
            {
                var candidate = i == 1 ? "key" : "key" + i;
                if (!existing.Contains(candidate)) return candidate;
            }
            return "key";
        }

        /// <summary>新键在对象里的落点：同一动态字段最后一个已登记键之后；该字段还没有键就追加到末尾。</summary>
        public int EntryInsertIndex(McdocTreeNode mapNode, McdocTreeNode row, McdocJsonValue map)
        {
            var last = -1;
            if (mapNode is null || row is null || row.DynamicFieldIndex < 0) return map.Properties.Count;
            foreach (var child in mapNode.Children ?? [])
            {
                if (!child.IsMapEntry || child.DynamicFieldIndex != row.DynamicFieldIndex || child.Json is null) continue;
                for (var i = map.Properties.Count - 1; i > last; i--)
                {
                    if (!ReferenceEquals(map.Properties[i].Value, child.Json)) continue;
                    last = i;
                    break;
                }
            }
            return last >= 0 ? last + 1 : map.Properties.Count;
        }
    }
}
