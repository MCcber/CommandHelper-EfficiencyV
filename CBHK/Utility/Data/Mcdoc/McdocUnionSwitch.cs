// union 切分支的唯一业务入口：行、探针、命令都走这里，避免"每个调用点各拼一遍流程"。
// 语义与 misode 的 UnionHead.onSelect 一致：切到哪一支 → 按该支 getChange 改写文档 → 分支子树重建。
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
                node.IsExpanded = false;
                // 真的变了：行要重投影（RefreshHead + ReplaceRow），不然界面上还是上一支的编辑器
                return true;
            }
            if (Equals(node.SelectedUnionTypeName, option)) return false;

            node.SelectedUnionTypeName = option;
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
            // 切到列表分支：值必须是数组，否则列表动作没有落点（标量就地包成一条，不丢用户的值）
            if (node.UnionListLike && node.Json is not { Kind: McdocJsonKind.Array })
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
