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
    public sealed class McdocCollectionEditor
    {
        public static McdocCollectionEditor Default { get; } = new();

        /// <summary>
        /// 把"必填 + 闭合下拉 + 已经替用户选好默认项、但 JSON 里还没有这个键"的值写进文档。
        /// 这些值界面上本来就是选中的，不落盘会出现"行上显示 minecraft:xxx、依赖它的调度器/spread 却是空的"
        /// （advancement.criteria.&lt;name&gt;.trigger 就是这种情况：trigger 缺键 → ...minecraft:trigger[[trigger]] 解析成 %none → 无子级）。
        /// 只处理必填字段：可选字段绝不能因为显示默认值就被凭空插进用户的文档。
        /// </summary>
        public int MaterializeRequiredSelections(IEnumerable<McdocTreeNode> roots, bool includeDisplayedDefaults = false)
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
                    || (node.Json.Kind == McdocJsonKind.String && string.IsNullOrWhiteSpace(node.Json.StringValue));
                // 有候选且有选中项就物化：普通 enum / literal 的 UseSelect 是 false，
                // 只看 UseSelect 会让必选枚举（frame=Task 这类）永远进不了文档。
                // **静默写入**：不走 ApplyValue（它抬 ValueChanged/StructureChanged），否则重建会把
                // 用户正在操作的行换成脱挂节点（S12 的候选选择写不进 JSON 就是这么坏的）。
                var hasOptions = node.EnumOptionList is { Count: > 0 };
                if (node.IsRequired && emptyText && (node.UseSelect || hasOptions)
                    && node.SelectedEnumOption?.Value?.LiteralValue is not null)
                {
                    var text = Convert.ToString(node.SelectedEnumOption.Value.LiteralValue);
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
                node.Json = value;
                SetMemberOrdered(target, node, value);
                written++;
            }

            foreach (var root in roots ?? []) Walk(root);
            return written;
        }

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
                ? "该键的值类型没解出来（已按 any 生成空结构）"
                : null;
            // 追加位置：紧跟"同一个动态字段的最后一个已有键"之后（该字段还没键时追加到末尾）。
            var insertIndex = EntryInsertIndex(mapNode, row, map);
            map.Properties.Insert(insertIndex, new KeyValuePair<string, McdocJsonValue>(key, inserted));
            mapNode.StructureChanged?.Invoke(mapNode);
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
                // 编辑器的"空状态"（文本框清空 / bool 两个都取消选中）语义 == unset：
                // 可选字段直接把键从文档里删掉；必填写空要有可见诊断，不许静默不动。
                ApplyEmptyValue(node);
                return false;
            }
            ApplyValue(node, value);
            return true;
        }

        /// <summary>编辑器被清空：可选字段删键（misode 的 unset），必填字段挂诊断。</summary>
        public void ApplyEmptyValue(McdocTreeNode node)
        {
            if (node is null) return;
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
            return head.EditorKind switch
            {
                // 枚举写回的是**字面量值**（Block = "block"），不是标识符（Block）；
                // 写标识符会让 JSON 语义错误，且重解析后选不回原项
                McdocEditorKind.Enum => EnumValue(node.SelectedEnumOption),
                // bool：可选节点允许"两个都不选"，这就是"没有值"（回落到删键）
                McdocEditorKind.Bool => !node.IsRequired && !node.IsTrue && !node.IsFalse
                    ? null : McdocJsonValue.Boolean(node.IsTrue),
                McdocEditorKind.Number => ToNumber(node.Value),
                // 必填字符串的空串是合法值（写 ""）；可选空串才算 unset
                McdocEditorKind.Text => string.IsNullOrEmpty(Convert.ToString(node.Value))
                    ? (node.IsRequired ? McdocJsonValue.String("") : null)
                    : McdocJsonValue.String(Convert.ToString(node.Value)),
                // Raw 编辑器自己解析成 Json 了；None / InlineTuple 没有行内值（子项各走各的行）
                _ => null,
            };
        }

        /// <summary>EnumMember.Value 的字面量 → JSON 值（字符串/布尔/数值三种枚举底层）。</summary>
        private static McdocJsonValue EnumValue(EnumMember option)
        {
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
                    McdocWriteTrace.Log("ReplaceRoot 跳过（脱挂节点）", node, "<无父级>");
                    node.Error = "写回已跳过：节点已脱挂（不是文档根）";
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
            if (row?.Json is null || mapNode?.Json is not { Kind: McdocJsonKind.Object } map || map.Properties is null)
            {
                return;
            }
            map.Properties.RemoveAll(pair => ReferenceEquals(pair.Value, row.Json));
            mapNode.StructureChanged?.Invoke(mapNode);
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

        /// <summary>列表的"+"：按元素类型造一个默认值追加到 JSON 数组末尾。</summary>
        public void AppendItem(McdocTreeNode node, McdocRuntime runtime)
        {
            // tuple 是定长结构：长度由类型决定，API 自己就得拒绝（不能只靠 UI 隐藏按钮）。
            // 注意别用 IsListLike 判：列表/标签联合（biomes、blocks 这类）的节点类型是 union，
            // 选中列表那一支时同样要能加项，用 IsListLike 会把它们一并挡掉。
            if (node?.Type is null || node.Type.Kind == McdocTypeKind.Tuple) return;
            if (EnsureContainer(node, McdocJsonKind.Array) is not { Kind: McdocJsonKind.Array } array)
            {
                return;
            }
            var itemType = McdocHelpers.GetItemType(ItemOwnerType(node));
            var itemDefault = McdocDefaults.Resolve(itemType, runtime);
            array.Items ??= [];
            // 定长（tuple / #[length=N]）不允许再追加：UI 上按钮是隐藏的，但 API 自身也要守住这个不变量
            var maxLength = node.Type?.LengthRange?.Max;
            if (maxLength.HasValue && array.Items.Count >= maxLength.Value) return;
            array.Items.Add(itemDefault ?? McdocJsonValue.Null());
            KeepListBranchExpanded(node);
            node.StructureChanged?.Invoke(node);
        }

        /// <summary>列表 ⊕（misode ListHead/ListBody 的 add_top / add_bottom）。</summary>
        public void AddItem(McdocTreeNode node, McdocRuntime runtime, bool atTop = false)
        {
            if (node?.Type is null || node.Type.Kind == McdocTypeKind.Tuple) return;
            if (EnsureContainer(node, McdocJsonKind.Array) is not { Kind: McdocJsonKind.Array } array) return;
            if (node.Type?.LengthRange is { Max: { } max } && (array.Items?.Count ?? 0) >= max) return;
            var itemType = McdocHelpers.GetItemType(ItemOwnerType(node));
            var value = McdocDefaults.Resolve(itemType, runtime);
            array.Items ??= [];
            if (atTop) array.Items.Insert(0, value); else array.Items.Add(value);
            KeepListBranchExpanded(node);
            node.StructureChanged?.Invoke(node);
        }

        /// <summary>删除指定下标的列表项；删空且不允许为空的可选列表时，整个字段一起移除。</summary>
        public void RemoveItemAt(McdocTreeNode node, int index)
        {
            // tuple 是定长结构：删除会把长度改坏（AppendItem/AddItem 已拒绝，这里同样要守）
            if (node?.Type?.Kind == McdocTypeKind.Tuple) return;
            if (node?.Json is not { Kind: McdocJsonKind.Array } array || array.Items is null) return;
            if (index < 0 || index >= array.Items.Count) return;
            array.Items.RemoveAt(index);
            var min = node.Type?.LengthRange?.Min ?? 0m;
            if (array.Items.Count == 0 && min > 0 && node.IsRequired == false && node.Parent is not null)
            {
                RemoveField(node);
                return;
            }
            node.StructureChanged?.Invoke(node);
        }

        /// <summary>列表项上移 / 下移。</summary>
        public void MoveItem(McdocTreeNode node, int from, int to)
        {
            if (node?.Json is not { Kind: McdocJsonKind.Array } array || array.Items is null) return;
            if (from < 0 || from >= array.Items.Count || to < 0 || to >= array.Items.Count || from == to) return;
            var value = array.Items[from];
            array.Items.RemoveAt(from);
            array.Items.Insert(to, value);
            node.StructureChanged?.Invoke(node);
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
            // 解不开按 any 处理（默认 {}），但必须让人看得见
            node.Error = McdocDefaults.IsUnresolved(declared, runtime) ? "该字段的类型没解出来（已按 any 生成空结构）" : null;
            ApplyValue(node, value);
            // 一键之后应该立刻看得到子字段行：写入后展开并重建父级
            node.IsExpanded = true;
            McdocExpandState.Set(node.JsonPath, true);
            node.Parent?.StructureChanged?.Invoke(node.Parent);
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
            if (node.Type?.Range is { } range)
            {
                if (range.Min.HasValue && next < range.Min.Value) next = range.Min.Value;
                if (range.Max.HasValue && next > range.Max.Value) next = range.Max.Value;
            }
            var value = McdocJsonValue.Number(next, next == decimal.Truncate(next));
            ApplyValue(node, value);
            node.StructureChanged?.Invoke(node.Parent ?? node);
        }

        /// <summary>列表的"−"：删掉最后一项。</summary>
        public void RemoveLastItem(McdocTreeNode node)
        {
            if (node?.Json is not { Kind: McdocJsonKind.Array } array || array.Items is null || array.Items.Count == 0)
            {
                return;
            }
            array.Items.RemoveAt(array.Items.Count - 1);
            node.StructureChanged?.Invoke(node);
        }

        /// <summary>右键"复制"：深拷贝本节点的 JSON 值并插到同层（仅列表项；对象属性复制会产生重复键）。</summary>
        public void DuplicateItem(McdocTreeNode node)
        {
            var parent = node?.Parent;
            if (parent?.Json is not { Kind: McdocJsonKind.Array } array || array.Items is null || node.Json is null)
            {
                return;
            }
            var index = array.Items.FindIndex(item => ReferenceEquals(item, node.Json));
            if (index < 0)
            {
                return;
            }
            array.Items.Insert(index + 1, node.Json.Clone());
            node.StructureChanged?.Invoke(node);
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
