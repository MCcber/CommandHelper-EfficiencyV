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
        public static bool Switch(McdocTreeNode node, int memberIndex, McdocRuntime runtime = null)
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
            return option is not null && SwitchTo(node, option, runtime);
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

        public static bool SwitchTo(McdocTreeNode node, EnumMember option, McdocRuntime runtime = null)
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
                // map 条目：只把右侧 JSON 的键去掉，**行留在树上**（用户还要能再选分支，或直接删掉这一行）。
                // 普通可选字段：整条路径断掉，行也一起消失（下面那条）。
                var entryUnset = node.IsMapEntry;
                if (entryUnset) McdocCollectionEditor.Default.UnsetEntryValue(node);
                else node.RemoveFieldCommand?.Execute(null);
                node.SetUnionTypeSilently(new EnumMember
                {
                    Name = McdocNodePresenter.UnsetLabel,
                    Value = new MetaValue { LiteralValue = -1 },
                });
                node.ClearUnionBranch();
                node.UnionSelectionExplicit = false;
                node.IsExpanded = false;
                // 条目的（未设置）只走值通道，而且必须在镜像清完之后发：早发一拍，宿主就会把旧值写回去
                if (entryUnset) node.RaiseValueChanged();
                // 真的变了：行要重投影（RefreshHead + ReplaceRow），不然界面上还是上一支的编辑器
                return true;
            }
            McdocWriteTrace.Trace("union-switch", "path=" + (node.JsonPath ?? "?")
                + " from=" + (node.SelectedUnionTypeName?.Name ?? "<null>")
                + " to=" + (option?.Name ?? "<null>")
                + " same=" + IsSameOption(node.SelectedUnionTypeName, option)
                + " explicit=" + node.UnionSelectionExplicit);
            if (IsSameOption(node.SelectedUnionTypeName, option)) return false;
            // 切走的成员：getChange 的"旧类型"（是它才有搬运语义，union 声明没有）
            var previousMember = node.SelectedUnionMember;
            // 切到"文档里还没有值"的**容器**分支：先把这一支的空壳写进文档，再换选中。
            // 不先落值，分支会在"没有 JSON"的状态下建出来 —— 可选成员不摊开子级（StructPresenter 的
            // "缺省成员不摊开"守卫），空分支随即被 ApplyUnionBranch 丢掉，选中落回（未设置）。
            // 现场：物品组件 profile 的 properties 切 Object 支（[string]:[string] 的 map），
            // 行上直接回到（未设置），而文档里连 properties 都没写进去。
            SeedContainerBranchValue(node, option, runtime);

            // 切支会改文档（剪旧键 / 补新支的默认结构 / getChange 改形状）：整段只通知宿主一次。
            // "一次"是硬约束 —— 两侧（文本同步 + 这一支的重建）都挂在这条通知上，重复会让重建排两遍；
            // 一次都不发就是截图现场：右侧 JSON 停在 {}，行停在上一支。
            var before = Snapshot(node.Json);
            var wasSuppressed = node.SuppressValueNotifications;
            node.SuppressValueNotifications = true;
            var reverted = false;
            try
            {
                // setter 里已经做过 ResetSelectedBranch（剪旧键 + 按新成员补默认结构）与分支子树重建，
                // 分支子树因此建在**文档里的新值**上（键 ↔ 行双射）。
                node.SelectedUnionTypeName = option;
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
                    reverted = true;
                }
            }
            finally
            {
                node.SuppressValueNotifications = wasSuppressed;
            }
            // 文档真的变了才通知宿主（形状/键值）。整段切换只发这一次。
            bool Notify()
            {
                if (!wasSuppressed
                    && !string.Equals(before, Snapshot(node.Json), StringComparison.Ordinal))
                {
                    node.RaiseValueChanged();
                }
                return true;
            }
            if (reverted) return Notify();

            node.SuppressValueNotifications = true;
            try
            {
                // 切到结构分支：直接展开，字段才看得见（misode 同款）
                // 懒建时子级还没建，数 Children 会得到 0：问 presenter 有没有子级
                if (node.ScalarEditorKind is null
                    && !node.IsExpanded
                    && McdocPresenterRegistry.Default.CanHaveChildren(node))
                {
                    node.IsExpanded = true;
                }
                // misode 的 UnionHead.onSelect：切分支当场按新分支改写文档（getChange）
                node.ConvertJsonToSelectedBranch(previousMember, replaceWhenUnfit: true);
                // getChange 是"没有运行期上下文"的代数：成员里必填字段是**引用类型**时（TextComponent 的
                // `keybind: Keybind`）它会把这个字段丢掉，写出一个 `{}` —— 值落不进这一支，下一次重建
                // 按值重选就弹回 String（用户看到的是"选了 Keybind 立刻弹回"）。
                // 这里用**带运行期的默认值代数**补一份落得进去的值：宁可写默认坐标，也不留一条写不出合法值的死分支。
                EnsureValueFitsSelectedMember(node, runtime);
                // 有的结构支"建支那一刻文档里还没有值"就立不住（ProfilePropertyMap 只有动态键、没有静态字段，
                // 分支建出来 0 子级，选中会落回（未设置））。值现在已经写进文档了，再选一次就能立住；
                // 同时记成"用户显式选择"，重建时按路径提示（UnionSelectionHints）保回来。
                if (node.Json is not null
                    && (node.SelectedUnionTypeName is null || IsUnsetOption(node.SelectedUnionTypeName))
                    && !IsUnsetOption(option))
                {
                    node.UnionSelectionExplicit = true;
                    node.SelectedUnionTypeName = option;
                }
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
            }
            finally
            {
                node.SuppressValueNotifications = wasSuppressed;
            }
            return Notify();
        }

        /// <summary>求值后的成员（下标与下拉选项一致）。</summary>
        private static McdocType EvaluatedMemberAt(McdocTreeNode node, EnumMember option)
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

        /// <summary>切到缺值的容器分支前，先把这一支的空壳写进文档（详见调用点注释）。</summary>
        private static void SeedContainerBranchValue(McdocTreeNode node, EnumMember option, McdocRuntime runtime)
        {
            if (node?.Json is not null) return;
            if (node.Parent?.Json is not { Kind: McdocJsonKind.Object }) return;
            var member = EvaluatedMemberAt(node, option);
            if (member is null) return;
            if (member.Kind is not (McdocTypeKind.Struct or McdocTypeKind.List or McdocTypeKind.Tuple
                or McdocTypeKind.ByteArray or McdocTypeKind.IntArray or McdocTypeKind.LongArray)) return;
            var seeded = McdocDefaults.Resolve(member, runtime ?? node.Runtime);
            if (seeded is null) return;
            node.Json = seeded;
            McdocCollectionEditor.Default.WriteIntoParent(node, seeded);
            McdocWriteTrace.Trace("union-branch-seed", "path=" + (node.JsonPath ?? "?")
                + " member=" + member.Kind + " value=" + seeded.Kind);
        }

        /// <summary>
        /// 值必须落得进刚切到的这个成员：落不进（缺必填字段 / 形状不对）就用**带运行期的默认值代数**
        /// 重新生成一份并写回父容器。没有 runtime 时什么也不做（探针/命令这类入口不算改文档的时机）。
        /// </summary>
        private static void EnsureValueFitsSelectedMember(McdocTreeNode node, McdocRuntime runtime)
        {
            if (node?.SelectedUnionMember is not { } member) return;
            // null 也按"落不进"处理：`ValueFitsMember(null) == true` 是给"可选字段没值"用的，
            // 但切支的结果是 null 就等于这一支没落地 —— 现场：profile.properties 切到 Object 支
            // （只有动态键的 ProfilePropertyMap）会拿到 null，行上直接回到（未设置）。
            if (node.Json is not null && ValueFitsMember(
                    McdocDefaults.ResolveType(member, runtime ?? node.Runtime, node.ValueNode) ?? member, node.Json, null)) return;
            runtime ??= node.Runtime;
            if (runtime is null) return;
            // 判据用**求值后**的成员：成员声明常常是个 Reference（ProfilePropertyMap / KeybindText），
            // 拿声明去 Matches 永远 false，会把自己挡在门外。
            var resolved = McdocDefaults.ResolveType(member, runtime, node.ValueNode) ?? member;
            var fitted = McdocDefaults.Resolve(member, runtime);
            if (fitted is null || !ValueFitsMember(resolved, fitted, null)) return;
            if (!node.SuppressValueNotifications) node.SuppressValueNotifications = true;
            node.Json = fitted;
            McdocCollectionEditor.Default.WriteIntoParent(node, fitted);
            McdocWriteTrace.Trace("union-branch-fit", "path=" + (node.JsonPath ?? "?")
                + " member=" + member.Kind + " value=" + (fitted.Kind.ToString()));
        }

        /// <summary>
        /// 文档里的值能不能原样落进这个成员：能就不许再算一遍 getChange（那会把用户数据换成默认值）。
        /// <see cref="McdocTypeMatcher.Matches"/> 对 list/tuple 只判"是不是数组"，
        /// 切到 <c>[string]</c> 支会把 <c>[1,2,3]</c> 原样留下（行上一条条"值与类型不符"），所以条目也要判。
        /// </summary>
        internal static bool ValueFitsMember(McdocType member, McdocJsonValue value, McdocSimplifyContext context)
        {
            if (member is null || value is null) return false;
            if (!McdocTypeMatcher.Matches(member, value, context)) return false;
            if (member.Kind is not (McdocTypeKind.List or McdocTypeKind.ByteArray
                or McdocTypeKind.IntArray or McdocTypeKind.LongArray or McdocTypeKind.Tuple)) return true;
            if (value.Kind != McdocJsonKind.Array) return true;
            var items = value.Items ?? [];
            if (member.Kind == McdocTypeKind.Tuple)
            {
                // 定长表：多出来的项按"落不进"处理（tuple 的默认值就是声明长度，转换会截到声明项数）
                var declared = member.Items ?? [];
                if (items.Count > declared.Count) return false;
                for (var i = 0; i < items.Count; i++)
                {
                    if (!McdocTypeMatcher.Matches(declared[i], items[i], context)) return false;
                }
                return true;
            }
            // 声明了长度区间的（#[length] / 定长 list）：条数越界就算落不进
            if (member.LengthRange is { } range
                && ((range.Min.HasValue && items.Count < range.Min.Value)
                    || (range.Max.HasValue && items.Count > range.Max.Value))) return false;
            var itemType = McdocHelpers.GetItemType(member);
            foreach (var item in items)
            {
                if (!McdocTypeMatcher.Matches(itemType, item, context)) return false;
            }
            return true;
        }

        /// <summary>值的文本快照：判断"这次切换有没有真的动过文档"用比"值"不比"对象身份"。</summary>
        private static string Snapshot(McdocJsonValue value)
            => value is null ? "<null>" : McdocJsonFormatter.Format(value);

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
        /// 切成员的语义：**旧成员留下的键不能带过去**，只保留"新成员也声明、且值在新声明下仍然成立"的那些。
        /// 两个要点：① 新成员不声明的键一律删（现场：切走后 `terms` 还挂着）；
        /// ② 两边都声明的键，值不成立就删（现场：旧成员 `x: int` 带到新成员 `x: string` 上）。
        /// 「保留旧键」只属于**换游戏版本**那条路径（README §2.7 换版本不改文档），切成员不属于它。
        /// </summary>
        /// <summary>
        /// 返回"文档真的被改过"（剪掉了旧键 / 补了新支的默认键）——调用方靠它决定要不要通知宿主。
        /// 一次切换的合同：这里有改动就必须发一条值通知，否则右侧文本与这一支的行都停在上一代（截图现场）。
        /// </summary>
        public static bool ResetSelectedBranch(McdocTreeNode node, EnumMember previous, EnumMember current)
        {
            var path = node?.JsonPath ?? "?";
            if (node?.Json is not { Kind: McdocJsonKind.Object } json)
            {
                McdocWriteTrace.Trace("branch-reset-skip", "path=" + path + " reason=json-not-object kind="
                    + (node?.Json?.Kind.ToString() ?? "<null>") + ")");
                return false;
            }
            if (node.Type?.Members is null || json.Properties is null)
            {
                McdocWriteTrace.Trace("branch-reset-skip", "path=" + path + " reason=no-members");
                return false;
            }
            if (IsSameOption(previous, current))
            {
                McdocWriteTrace.Trace("branch-reset-skip", "path=" + path + " reason=same-option");
                return false;
            }
            var member = MemberAt(node, current);
            if (member is null)
            {
                McdocWriteTrace.Trace("branch-reset-skip", "path=" + path + " reason=no-member option=" + (current?.Name ?? "<null>"));
                return false;
            }
            // 剪枝只对「旧新都是结构」生效（README §三）：新成员是标量/列表/any 时它一个静态字段都没有，
            // 按"没声明"判会把整段值吃光 —— 而 {k:X}→X、[X]→X 的搬运恰恰要靠这份旧值。
            // 现场：{ "v": "a" } 切到 string 支先被剪成 {}，getChange 取不到 v，最后落成 ""。
            var previousMember = MemberAt(node, previous);
            if (previousMember?.Kind != McdocTypeKind.Struct || member.Kind != McdocTypeKind.Struct)
            {
                McdocWriteTrace.Trace("branch-reset-skip", "path=" + path + " reason=not-struct-pair old="
                    + (previousMember?.Kind.ToString() ?? "<null>") + " new=" + member.Kind);
                return false;
            }
            var kept = new List<KeyValuePair<string, McdocJsonValue>>();
            var dropped = 0;
            foreach (var pair in json.Properties)
            {
                var field = FieldFor(member, pair.Key);
                if (field is null || !McdocHelpers.ValueFits(pair.Value, field.Type))
                {
                    dropped++;
                    McdocWriteTrace.Trace("branch-drop", "path=" + (node.JsonPath ?? "?") + " key=" + pair.Key);
                    continue;
                }
                kept.Add(pair);
            }
            json.Properties.Clear();
            foreach (var pair in kept) json.Properties.Add(pair);
            // 新成员声明的键在文档里缺着：用默认结构补上（不是"恢复旧值"，是按新支重建）
            var fresh = BranchDefault(member);
            var added = 0;
            if (fresh is { Kind: McdocJsonKind.Object })
            {
                foreach (var pair in fresh.Properties ?? [])
                {
                    if (json.Get(pair.Key) is not null) continue;
                    json.Add(pair.Key, pair.Value);
                    added++;
                }
            }
            McdocCollectionEditor.Default.WriteIntoParent(node, json);
            McdocWriteTrace.Trace("branch-reset", "path=" + (node.JsonPath ?? "?")
                + " from=" + (previous?.Name ?? "<unset>") + " to=" + (current?.Name ?? "<unset>")
                + " kept=" + json.Properties.Count + " dropped=" + dropped + " added=" + added);
            return dropped > 0 || added > 0;
        }

        /// <summary>
        /// 分支的默认结构：按成员**声明的字段**逐个取默认值。
        /// 不用 <see cref="McdocHelpers.GetChange"/>：那条路要 runtime 才能求值泛型实参（`argument: T`），
        /// 拿不到就回 Null，整支只剩 {}（现场：切到 Modifier 支后 `modifier`/`argument` 都没落盘）。
        /// 指令包装（未绑定的泛型实参 / Reference）拿不到默认值时兜底成 `{}`：至少保证键存在、形状对得上；
        /// 标量包装则跳过——宁缺，不写错类型。
        /// </summary>
        private static McdocJsonValue BranchDefault(McdocType member)
        {
            if (member?.Kind != McdocTypeKind.Struct) return McdocJsonValue.Object();
            var result = McdocJsonValue.Object();
            foreach (var field in member.Fields ?? [])
            {
                if (field?.Name is not { Length: > 0 } name || field.Optional || field.IsSpread) continue;
                // 泛型实例化后字段类型常被 Mapped/Concrete 包着：先沿 Item 拆壳再取默认值，
                // 否则 `modifier: "override"` 这种字面量会取成 null（现场：写成了 {}）。
                var declared = field.Type;
                for (var depth = 0; declared?.IsDirective == true && declared.Item is not null && depth < 8; depth++)
                {
                    declared = declared.Item;
                }
                var value = McdocHelpers.GetDefault(declared);
                if (value is null || value.Kind == McdocJsonKind.Null)
                {
                    if (declared?.IsDirective == true || declared?.Kind == McdocTypeKind.Struct)
                    {
                        value = McdocJsonValue.Object();
                    }
                    else
                    {
                        continue;
                    }
                }
                result.Add(name, value);
            }
            return result;
        }

        /// <summary>成员里声明这个键的字段（没有就当"新成员不声明"）。</summary>
        private static McdocStructField FieldFor(McdocType member, string key)
        {
            if (member?.Fields is null || string.IsNullOrEmpty(key)) return null;
            foreach (var field in member.Fields)
            {
                if (field is null) continue;
                if (string.Equals(field.Name, key, StringComparison.Ordinal)) return field;
            }
            return null;
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
            if (members is null || index < 0) return null;
            // 声明里的成员会被版本门控成"新老两种写法"（Profile.properties 就是
            // `[ProfileProperty]` + `[ProfileProperty] @ 0..16` + `ProfilePropertyMap`），
            // 而求值后的 Members 只剩一种 —— 直接按下标取会错位（option=Object 取到第二个 List，
            // 于是"切到结构支"既补不出默认结构、也判不对解不解得出来）。
            // 先按同一套版本规则过滤，下标才对得上。
            var attributes = node?.Runtime?.Attributes;
            if (attributes is not null)
            {
                var kept = members
                    .Where(member => member is not null && attributes.ShouldKeep(member.Attributes, node.Runtime))
                    .ToList();
                if (kept.Count > 0 && kept.Count <= members.Count) members = kept;
            }
            return index < members.Count ? members[index] : null;
        }
    }
}
