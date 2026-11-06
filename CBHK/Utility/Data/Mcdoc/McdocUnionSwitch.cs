// union 切分支的唯一业务入口：行、探针、命令都走这里，避免"每个调用点各拼一遍流程"。
// 语义与 misode 的 UnionHead.onSelect 一致：切到哪一支 → 按该支 getChange 改写文档 → 分支子树重建。
using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftLanguageModelLibrary.Data;

namespace CBHK.Utility.Data.Mcdoc
{
    public static class McdocUnionSwitch
    {
        /// <summary>unset 哨兵项（ComboBox 往返后 LiteralValue 可能变字符串，两种形态都认）。</summary>
        public static bool IsUnsetOption(EnumMember option)
        {
            if (option is null) return false;
            if (option.Name == McdocNodePresenter.UnsetLabel) return true;
            return option.Value?.LiteralValue switch
            {
                int number => number < 0,
                decimal decimalNumber => decimalNumber < 0,
                string text => text == "-1",
                _ => false,
            };
        }

        /// <summary>按成员下标切换（探针/命令用；与下拉等价）。</summary>
        public static bool Switch(McdocTreeNode node, int memberIndex)
        {
            if (node?.Type is not { Kind: McdocTypeKind.Union }) return false;
            var option = node.UnionTypeNameList?.FirstOrDefault(item =>
            {
                var raw = item.Value?.LiteralValue;
                var index = raw switch
                {
                    int value => value,
                    decimal number => (int)number,
                    string text when int.TryParse(text, out var parsed) => parsed,
                    _ => -2,
                };
                return index == memberIndex;
            });
            return option is not null && SwitchTo(node, option);
        }

        /// <summary>
        /// 切到指定分支。返回是否真的发生了变化（unset/同分支都算没变化）。
        /// 只做节点级动作（选支、必要时展开、按 getChange 改文档形状），行级通知由调用方负责。
        /// </summary>
        /// <summary>
        /// 分支身份按「名字 + 下标」比，**不按 EnumMember 实例引用**：每次建支都会新建一批 EnumMember，
        /// 绑定的 SelectedItem 与节点上那个必然不是同一个对象。按引用比 → 每次绑定都判成"换了一支" →
        /// SwitchTo 又走一遍 getChange（改写文档）并 ReplaceRow → 新 Head、新 BranchOptions → 再触发绑定：
        /// 表现就是「选中项高频跳、JSON 跟着抖」。
        /// </summary>
        public static bool IsSameOption(EnumMember left, EnumMember right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            if (!string.Equals(left.Name, right.Name, StringComparison.Ordinal)) return false;
            return string.Equals(Convert.ToString(left.Value?.LiteralValue),
                Convert.ToString(right.Value?.LiteralValue), StringComparison.Ordinal);
        }

        public static bool SwitchTo(McdocTreeNode node, EnumMember option)
        {
            if (node is null) return false;
            if (IsUnsetOption(option))
            {
                // unset 语义 = "这个键不存在"：删键 + 清分支子级 + 清行内镜像（编辑器/候选/形态后缀）+ 折叠。
                // 只删键是不够的：ScalarEditorKind / EnumOptionList 是分支镜像，留着就会在行上留一个
                // 写着旧值、点不动的死编辑器（"选了未设置，行里还有 minecraft:allay"）。
                if (node.Parent?.Json is not { Kind: McdocJsonKind.Object })
                {
                    // 不是对象字段（列表项里的 union）：没有"删键"这条语义，别把行改成未设置骗用户
                    return false;
                }
                node.RemoveFieldCommand?.Execute(null);
                node.SetUnionTypeSilently(new EnumMember
                {
                    Name = McdocNodePresenter.UnsetLabel,
                    Value = new MetaValue { LiteralValue = -1 },
                });
                node.ClearUnionBranch();
                node.UnionSelectionExplicit = false;
                node.IsExpanded = false;
                // 真的变了：行要重投影（RefreshHead + ReplaceRow），不然界面上还是上一支的编辑器
                return true;
            }
            McdocWriteTrace.Trace("union-switch", "path=" + (node.JsonPath ?? "?")
                + " from=" + (node.SelectedUnionTypeName?.Name ?? "<null>")
                + " to=" + (option?.Name ?? "<null>")
                + " same=" + IsSameOption(node.SelectedUnionTypeName, option)
                + " explicit=" + node.UnionSelectionExplicit);
            if (IsSameOption(node.SelectedUnionTypeName, option)) return false;
            var previousOption = node.SelectedUnionTypeName;

            node.SelectedUnionTypeName = option;
            // 旧成员声明过、新成员不声明的键不再属于这个分支：留着就会在行上长出"上一支的字段"
            // （现场：any_of 切到 block_state_property 后 terms 还留在列表里）。这两个成员都必须是结构 ——
            // any/标量分支本来就什么都能吃，剪了反而是丢数据。
            DropAbandonedKeys(node, previousOption, option);
            // 这是用户选的：此后重建不许用"按值猜"把它顶回去（并列时按值猜会退回首个成员）
            node.UnionSelectionExplicit = true;
            // 成员解不出来（声明不是 any，求值却退成 any）→ 不保留这次选择：与 misode 一致跳回 unset，
            // 并挂可见诊断。否则行上会留下一个"看着有编辑器、其实写不出合法值"的死分支。
            if (IsUnresolvedMember(node, node.SelectedUnionMember) || IsUnresolvedBranch(node))
            {
                node.SetUnionTypeSilently(new EnumMember
                {
                    Name = McdocNodePresenter.UnsetLabel,
                    Value = new MetaValue { LiteralValue = -1 },
                });
                node.ClearUnionBranch();
                node.Error = "该分支的类型解不出来（当前上下文缺所需键），已回到未设置";
                return true;
            }
            // 切到结构分支：直接展开，字段才看得见（misode 同款）
            // 懒建时子级还没建，数 Children 会得到 0：问 presenter 有没有子级
            if (node.ScalarEditorKind is null
                && !node.IsExpanded
                && McdocPresenterRegistry.Default.CanHaveChildren(node))
            {
                node.IsExpanded = true;
            }
            // misode 的 UnionHead.onSelect：切分支当场按新分支改写文档（getChange）
            node.ConvertJsonToSelectedBranch();
            // 值刚落盘：分支节点与行上镜像按新值重新派生（否则镜像停在建支那一刻的（未设置））
            node.RefreshUnionBranchMirror();
            // 切到列表分支：值必须是数组，否则列表动作没有落点（标量就地包成一条，不丢用户的值）。
            // 判据用**声明**（SelectedUnionMember.IsListLike），不许用行上的 UnionListLike 镜像：
            // 那是上一支的状态，从列表支切到标量支时它还留着，会把刚落盘的 "" 又包成 ["c"]。
            var listBranch = node.SelectedUnionMember is { } selectedMember ? selectedMember.IsListLike : node.UnionListLike;
            if (listBranch && node.Json is not { Kind: McdocJsonKind.Array })
            {
                var array = McdocJsonValue.Array();
                array.Items ??= [];
                if (node.Json is { Kind: McdocJsonKind.String or McdocJsonKind.Number or McdocJsonKind.Boolean })
                {
                    array.Items.Add(node.Json);
                }
                McdocCollectionEditor.Default.WriteIntoParent(node, array);
                node.Json = array;
            }
            // 非标量分支（结构 / 列表 / 元组 / any）切过去就直接展开：字段行、条目行要当场看得见。
            // 这一步必须在分支落地之后判 ScalarEditorKind —— 切之前它还挂着上一支的标量镜像，
            // 早判会把"列表分支"当标量，行就永远折叠（S29-6 就是这么红的）。
            if (node.ScalarEditorKind is null
                && !node.IsExpanded
                && McdocPresenterRegistry.Default.CanHaveChildren(node))
            {
                node.IsExpanded = true;
                McdocExpandState.Set(node.JsonPath, true);
            }
            return true;
        }

        /// <summary>
        /// 刚切到的分支是否"解不出来"：成员本身声明成 any/unsafe 是合法兜底，不算；
        /// 其它成员求值退成 any 说明它缺前置键（例如 NBT 分支要靠同级 type 才能解），这次选择不该保留。
        /// </summary>
        private static bool IsUnresolvedBranch(McdocTreeNode node)
        {
            // 用**声明**的成员判：声明成 any/unsafe 是合法兜底；声明成别的却求值成 any，
            // 说明它缺前置键（例如 NBT 分支要靠同级 type 才能解出来）。
            var declared = DeclaredMember(node, node.SelectedUnionTypeName);
            if (declared is null || declared.IsDirective is false && declared.Kind is McdocTypeKind.Any or McdocTypeKind.Unsafe) return false;
            if (declared.Kind is McdocTypeKind.Any or McdocTypeKind.Unsafe) return false;
            // 标量分支一律接受：它写出来就是合法标量（#[nbt]/#[id] 之类只是候选来源，不该被判死）
            if (IsScalarBranch(declared)) return false;
            // 标量分支会镜像到行上（没有独立子级），这类分支不做此判定
            var branch = node.SelectedUnionChildren.FirstOrDefault();
            if (branch is null) return false;
            return branch.Type?.Kind is McdocTypeKind.Any or McdocTypeKind.Unsafe;
        }

        /// <summary>
        /// 某个**求值后**的成员是不是"解不出来"：声明不是 any/unsafe，求值却是 any/unsafe。
        /// 声明成 any 的成员是合法兜底，不算。
        /// </summary>
        public static bool IsUnresolvedMember(McdocTreeNode node, McdocType member)
        {
            if (member?.Kind is not (McdocTypeKind.Any or McdocTypeKind.Unsafe)) return false;
            var evaluated = node?.Type?.Members;
            if (evaluated is null) return false;
            var index = -1;
            for (var i = 0; i < evaluated.Count; i++)
            {
                if (ReferenceEquals(evaluated[i], member)) { index = i; break; }
            }
            var declared = node?.DeclaredType?.Members;
            if (index < 0 || declared is null || index >= declared.Count) return false;
            if (IsScalarBranch(declared[index])) return false;
            return declared[index].Kind is not (McdocTypeKind.Any or McdocTypeKind.Unsafe);
        }

        /// <summary>
        /// 切支：把"旧成员声明、新成员不声明"的键从文档里去掉。
        /// 只处理「旧新都是结构」这一对：结构有确定的字段集合，能判定哪些键属于被放弃的那一支；
        /// 标量/列表/any 分支不做（它们本来就不限定键集）。
        /// </summary>
        public static void DropAbandonedKeys(McdocTreeNode node, EnumMember previous, EnumMember current)
        {
            if (node?.Json is not { Kind: McdocJsonKind.Object } json) return;
            var members = node.Type?.Members;
            if (members is null || json.Properties is null) return;
            var newMember = MemberAt(node, current);
            var oldMember = MemberAt(node, previous);
            if (newMember?.Kind != McdocTypeKind.Struct || oldMember?.Kind != McdocTypeKind.Struct) return;
            var keep = FieldNames(newMember);
            var drop = FieldNames(oldMember).Where(name => !keep.Contains(name)).ToList();
            if (drop.Count == 0) return;
            var kept = json.Properties.Where(pair => !drop.Contains(pair.Key)).ToList();
            if (kept.Count == json.Properties.Count) return;
            var pruned = McdocJsonValue.Object();
            pruned.Properties = kept;
            node.Json = pruned;
            McdocCollectionEditor.Default.WriteIntoParent(node, pruned);
        }

        /// <summary>选项下标 → 求值后的成员（下标存在 EnumMember.Value 里）。</summary>
        private static McdocType MemberAt(McdocTreeNode node, EnumMember option)
        {
            var index = option?.Value?.LiteralValue switch
            {
                int value => value,
                decimal number => (int)number,
                string text when int.TryParse(text, out var parsed) => parsed,
                _ => -1,
            };
            var members = node?.Type?.Members;
            return members is not null && index >= 0 && index < members.Count ? members[index] : null;
        }

        private static HashSet<string> FieldNames(McdocType type)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            if (type?.Fields is null) return names;
            foreach (var field in type.Fields)
            {
                if (!string.IsNullOrEmpty(field.Name)) names.Add(field.Name);
            }
            return names;
        }

        /// <summary>声明成标量的成员：写出来就是合法标量，不参与"解不出来就回退"的判定。</summary>
        private static bool IsScalarBranch(McdocType declared)
            => declared?.Kind is McdocTypeKind.String or McdocTypeKind.Enum or McdocTypeKind.Boolean
                or McdocTypeKind.Literal
                or McdocTypeKind.Byte or McdocTypeKind.Short or McdocTypeKind.Int or McdocTypeKind.Long
                or McdocTypeKind.Float or McdocTypeKind.Double;

        /// <summary>选中项对应的**声明**成员（union 的 Type.Members 已经被求值过，拿不到"声明成什么"）。</summary>
        private static McdocType DeclaredMember(McdocTreeNode node, EnumMember option)
        {
            var index = option?.Value?.LiteralValue switch
            {
                int value => value,
                decimal number => (int)number,
                string text when int.TryParse(text, out var parsed) => parsed,
                _ => -1,
            };
            var members = node?.DeclaredType?.Members;
            return members is not null && index >= 0 && index < members.Count ? members[index] : null;
        }
    }
}
