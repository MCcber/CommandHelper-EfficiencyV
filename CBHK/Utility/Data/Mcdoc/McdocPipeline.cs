using System;
using System.Collections.Generic;
using System.Linq;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 界面那条同步时序的**唯一实现**：结构/值变更之后该做什么（刷新哪一层、物化到不动点、什么时候整表重投影）。
    /// VM 与探针都拿这一份 —— 探针如果自己手抄一遍时序，它绿了也不能说明界面绿（上一轮就是这么翻车的）。
    /// 状态在构造时注入、之后只读：VM 与探针各注入自己那一份，换版本换的是整条 pipeline。
    /// 物化 / 诊断不持有它，只拿 <see cref="IMcdocPipelineHost"/> 这份只读视图（反向依赖到此为止）。
    /// </summary>
    /// <summary>
    /// 物化 / 诊断只依赖这份只读契约：它们能读文档、投影、根类型与 loading 闸门，
    /// 但拿不到写权，也改不了 Pipeline 的接线 —— 反向依赖到此为止（不再 new 一个具体类互指）。
    /// </summary>
    internal interface IMcdocPipelineHost
    {
        McdocTreeDocument Document { get; }
        McdocRowProjector Projector { get; }
        Func<McdocType> RootType { get; }
        Func<bool> GetLoading { get; }
        Action<bool> SetLoading { get; }
    }

    public sealed class McdocPipeline : IMcdocPipelineHost
    {
        // ── 状态：构造时注入，之后只读（谁都不许把别人的文档塞进来）──
        public McdocTreeDocument Document { get; init; }
        public McdocRowProjector Projector { get; init; }
        /// <summary>重建文档时用的根类型（换版本会换它）。</summary>
        public Func<McdocType> RootType { get; init; }
        /// <summary>VM 的 loading 闸门：建树/物化期间不许把赋值当用户编辑。</summary>
        public Func<bool> GetLoading { get; init; }
        public Action<bool> SetLoading { get; init; }
        // ── 接线口：往哪里回调。运行期可换（探针靠它注入替身），但它不参与内部状态不变量 ──
        /// <summary>文档被改动，需要把 RootJson 回写到编辑器（VM 接 SyncJsonText；探针可为空）。</summary>
        public Action TextDirty { get; set; }
        /// <summary>重建失败时的回退（VM 用"不重解析文本的重载"，探针用整篇 Load）。</summary>
        public Action Fallback { get; set; }
        /// <summary>运行期依赖触发"延后一拍重建"的出口（VM 接 ScheduleRebuild）。</summary>
        public Action<McdocTreeNode> ScheduleRebuild { get; set; }
        /// <summary>
        /// 正在做重建/刷新。期间投影会去 EnsureChildren，从而触发"刚建出子级"的钩子；
        /// 那个钩子本来只为"用户点开懒建节点"服务，如果它也参与重建，就会自我驱动：
        /// 重建 → 新节点 → 懒建子级 → 钩子 → 再重建（现场：proj-splice/proj-reset 各 118 次、structure 只有 2 次，UI 线程被占死）。
        /// </summary>
        public bool Rebuilding { get; private set; }

        /// <summary>
        /// 把"正在重建"这段窗口显式框起来。生产代码自己在 try/finally 里置位；
        /// 探针要模拟这个窗口时用 scope —— 恢复原值交给 Dispose，没有公开 setter 可写。
        /// </summary>
        public RebuildingScope BeginRebuilding()
        {
            var scope = new RebuildingScope(this, Rebuilding);
            Rebuilding = true;
            return scope;
        }

        public readonly struct RebuildingScope : IDisposable
        {
            private readonly McdocPipeline owner;
            private readonly bool wasRebuilding;

            internal RebuildingScope(McdocPipeline owner, bool wasRebuilding)
            {
                this.owner = owner;
                this.wasRebuilding = wasRebuilding;
            }

            public void Dispose()
            {
                if (owner is not null) owner.Rebuilding = wasRebuilding;
            }
        }

        /// <summary>物化：把缺掉的必填值写进文档、再定向重解被写的那一层，直到不动点。</summary>
        public McdocMaterializer Materializer { get; }

        /// <summary>诊断调度：重算校验并刷新行上的徽标（规则本体在 <see cref="McdocValidator"/>）。</summary>
        public McdocDiagnostics Diagnostics { get; }

        public McdocPipeline()
        {
            Materializer = new McdocMaterializer(this);
            Diagnostics = new McdocDiagnostics(this);
        }

        /// <summary>
        /// 重建闸门里"刚建出子级"的节点。投影在【刷新 → 物化 → 投影】这一段里首次建出的子树，
        /// 补物化必须等闸门结束再跑 —— 闸门里写值要么抬出自己被早退的结构通知，要么换掉正在投影的行。
        /// 不补跑的话，新子树里的必填判别键永远不落盘，靠它做动态索引的兄弟 spread 停在 %none
        /// （表现：加了元素，同层调度器成员一个都不出现，行上还挂着「动态键…没有对应成员」）。
        /// </summary>
        private readonly List<McdocTreeNode> pendingChildBuilds = [];
        private const int MaxPendingChildBuilds = 64;

        /// <summary>
        /// 切支判废删掉的路径。删的是键、行要留在树上，可**必填容器在空值处会被物化补一个空壳**（{} / []）——
        /// 那个壳不是用户写进去的值，留着等于把刚删掉的值又写回一行（行上立刻挂回“值不属于任何一支”）。
        /// 物化之后按这张表把空壳再摘一次；路径上重新有了真值（用户自己填进去）就出表。
        /// 记路径而不是节点：切支会把节点按新类型重建成新实例，路径才是跨代稳定的身份。
        /// </summary>
        private readonly HashSet<string> staleUnsetPaths = new(StringComparer.Ordinal);
        private const int MaxStaleUnsetPaths = 256;

        /// <summary>这一拍里有“判废删除”（键被摘掉）：文本不能只脏一棵子树，否则删掉的键留在文本里。</summary>
        private bool staleValueDropped;

        /// <summary>投影刚给某个节点建出子级：闸门外立刻补物化，闸门内先登记（排空时补跑）。</summary>
        public void NoteChildrenBuilt(McdocTreeNode node)
        {
            if (node is null || Document is null) return;
            if (Rebuilding)
            {
                if (pendingChildBuilds.Count < MaxPendingChildBuilds && !pendingChildBuilds.Contains(node))
                {
                    pendingChildBuilds.Add(node);
                }
                return;
            }
            var written = Materializer.MaterializeChildDefaults(node);
            if (written > 0)
            {
                // 写进去的是 AST：文本还是载入那一刻的那份。不在这里推一次，
                // 就出现"行上显示 Oak、AST 里有 variant、右侧 JSON 没有"——
                // 第二次载入后展开 type_specific 正是这条路（载入时子级还没建，物化没得写；
                // 展开时才补写，而补写过去只排了重建、没同步文本）。
                // 投影不在这里动（钩子就在投影里跑，Reset 会自激），交给排出去的这一拍重建收口。
                // 补建出来的必填值可能落在任意节点上，文本不能按"只脏了某棵子树"处理
                MarkTextDirty(null);
                TextDirty?.Invoke();
                ScheduleRebuild?.Invoke(node.RebuildAnchor ?? node);
            }
        }

        /// <summary>
        /// 闸门结束后的补跑：只写值（静默，不抬事件）→ 整表重投影 → 排**一拍**延后重建。
        /// 延后是硬要求：就地重建会回到"重建 → 新节点 → 懒建 → 钩子 → 再重建"的自激循环。
        /// </summary>
        public void DrainPendingChildBuilds()
        {
            if (Rebuilding || pendingChildBuilds.Count == 0 || Document is null) return;
            var nodes = pendingChildBuilds.ToList();
            pendingChildBuilds.Clear();
            var written = 0;
            McdocTreeNode anchor = null;
            var wasLoading = GetLoading?.Invoke() ?? false;
            var wasRebuilding = Rebuilding;
            Rebuilding = true;
            SetLoading?.Invoke(true);
            try
            {
                foreach (var node in nodes)
                {
                    if (node is null || !Document.IsAttached(node)) continue;
                    written += Materializer.MaterializeChildDefaults(node);
                    anchor ??= node.RebuildAnchor ?? node;
                }
                if (written > 0)
                {
                    McdocWriteTrace.Trace("children-built-drain", "nodes=" + nodes.Count + " written=" + written);
                    Projector?.Reset(Document.ProjectionRoots);
                    MarkTextDirty(null);
                    TextDirty?.Invoke();
                }
            }
            finally
            {
                SetLoading?.Invoke(wasLoading);
                Rebuilding = wasRebuilding;
            }
            if (written > 0 && anchor is not null) ScheduleRebuild?.Invoke(anchor);
        }

        /// <summary>
        /// 投影与文档必须同代：任一行指向的不是"文档里该路径的那个节点实例"，就整表重投影。
        /// 局部拼接漏一次（父行换了实例、锚点落在旧代上）时，行就整批停在上一代 ——
        /// 现象是"右侧 JSON 已经有新元素，左侧树一动不动，点一下复制才全出来"。
        /// 判据用路径 + 实例身份（IsAttached 只看父链，换成旧实例照样算挂着）。
        /// </summary>
        private void EnsureProjectionCurrent()
        {
            if (Document is null || Projector is null) return;
            // 保留区的陈旧条目先清：运行期重建那条支路不经过 RefreshNode，键删了没人通知保留区。
            // 清理改变条目数就整表重投影 —— 容器行自己也得跟着走（清空后这一行必须消失）。
            var preservedBefore = Document.PreservedGroup?.Children.Count ?? 0;
            Document.PrunePreserved();
            if ((Document.PreservedGroup?.Children.Count ?? 0) != preservedBefore)
            {
                McdocWriteTrace.Trace("projection-stale", "preserved-pruned rows=" + Projector.Rows.Count);
                Projector.Reset(Document.ProjectionRoots);
                return;
            }
            foreach (var row in Projector.Rows)
            {
                var path = row.Node?.JsonPath;
                if (row.Node is null || string.IsNullOrEmpty(path) || path == "$") continue;
                // 行上的节点已经不在树上（遗留键被删、保留区条目被摘）——投影过期，跟路径对不上一样处理
                if (!Document.IsAttached(row.Node))
                {
                    McdocWriteTrace.Trace("projection-stale", "detached path=" + path + " rows=" + Projector.Rows.Count);
                    Projector.Reset(Document.ProjectionRoots);
                    return;
                }
                var current = Document.FindNode(path);
                if (current is null || ReferenceEquals(current, row.Node)) continue;
                McdocWriteTrace.Trace("projection-stale", "path=" + path + " rows=" + Projector.Rows.Count);
                Projector.Reset(Document.ProjectionRoots);
                return;
            }
        }

        /// <summary>
        /// 结构变更（增删条目 / 切 union 分支 / 展开补建）之后：刷新这一层 → 物化到不动点 → 只重拼父行。
        /// 运行期依赖（动态索引/调度器解出新字段）交给调用方延后一拍重建。
        /// </summary>
        public void OnStructureChanged(McdocTreeNode node)
        {
            if (node is null) return;
            using var stage = McdocWriteTrace.Stage("pipeline.structure");
            // 重建期间收到的结构变更通知是**我自己造出来的**（PopulateChildren / ApplyBranch / 物化），
            // 不是用户操作：再排一次重建就是自我驱动。这条闸门对整个管线恒定成立，与节点类型无关。
            if (Rebuilding)
            {
                McdocWriteTrace.Trace("structure-reentrant", "path=" + (node.JsonPath ?? "?"));
                return;
            }
            // 摊平根的合成根本身不在 Rows 里、RefreshChildren 也建不出"行"：
            // 它换子级时只能按 JSON 重建顶层（Nodes），否则被删掉的顶层行会一直留在投影里
            // —— 现场就是「删掉 $.tset 之后 JSON 没了、左侧那行还在，再点就是幽灵行」。
            if (Document.FlattenedRoot && string.Equals(node.JsonPath ?? "", "$", StringComparison.Ordinal))
            {
                McdocWriteTrace.Trace("structure-root", "rows=" + Projector.Rows.Count + " resets=" + Projector.ResetCount
                    + McdocWriteTrace.CallerSuffix());
                Reload();
                return;
            }
            McdocWriteTrace.Trace("structure", "path=" + (node.JsonPath ?? "?")
                + " runtimeDep=" + node.IsRuntimeDependent + " isMapEntry=" + node.IsMapEntry
                + " rows=" + Projector.Rows.Count + " resets=" + Projector.ResetCount);
            if (node.IsRuntimeDependent)
            {
                // 运行期依赖（动态索引/调度器解出新字段）：重建最近容器，延后一拍。
                // 但**添加/展开**这类手势不能只排延后：用户点完 ⊕ 必须当场看到新子行，
                // 否则要点一次折叠/展开（现场：block_state_property 的 Properties 添加后子结构空白）。
                // 先用当前模型把这一层拼进投影，延后重建随后再按新解出的类型校正。
                var wasDeferredLoading = GetLoading?.Invoke() ?? false;
                var wasDeferredRebuilding = Rebuilding;
                Rebuilding = true;
                SetLoading?.Invoke(true);
                try
                {
                    node.EnsureChildren();
                    if (!Projector.RefreshAround(node))
                    {
                        Projector.Reset(Document.ProjectionRoots);
                    }
                    Diagnostics.Refresh(node, refreshHead: true);
                    McdocWriteTrace.Trace("structure-runtime-splice", "path=" + (node.JsonPath ?? "?"));
                }
                finally
                {
                    SetLoading?.Invoke(wasDeferredLoading);
                    Rebuilding = wasDeferredRebuilding;
                }
                ScheduleRebuild?.Invoke(node.RebuildAnchor);
                MarkTextDirty(null);
                TextDirty?.Invoke();
                DrainPendingChildBuilds();
                return;
            }
            // 闸门必须覆盖【刷新 → 物化到不动点 → 投影 → 回写】整段：
            // 只包住 RefreshNode 的话，后面的 Reset 会 EnsureChildren → 钩子 → 又排一次重建（自激循环）
            var wasLoading = GetLoading?.Invoke() ?? false;
            var wasRebuilding = Rebuilding;
            Rebuilding = true;
            SetLoading?.Invoke(true);
            try
            {
                Document.RefreshNode(node);
                var writes = new List<McdocTreeNode>();
                var shellsBefore = SnapshotEmptyStalePaths();
                Materializer.MaterializeUntilStable(writtenNodes: writes);
                var purgedShells = PurgeStaleShells(shellsBefore, writes);
                if (purgedShells > 0 || !Projector.RefreshAround(node)) { Projector.Reset(Document.ProjectionRoots); }
                // RefreshNode 刚把这棵子树的诊断整篇重算过，这里只刷行、不再算第二遍
                Diagnostics.Refresh(node, refreshHead: true, nodeValidated: true);
                EnsureProjectionCurrent();
                MarkTextDirtyAfterSync(node, writes);
                TextDirty?.Invoke();
            }
            finally
            {
                SetLoading?.Invoke(wasLoading);
                Rebuilding = wasRebuilding;
            }
            DrainPendingChildBuilds();
        }

        /// <summary>
        /// 切 union 结构分支之后的唯一收尾：<c>SwitchTo</c> 只按 <c>getChange</c> 重建分支子树并写 JSON，
        /// 必选判别键还没有值、同层 spread 也还没重新求值 —— 只同步文本会停在「Type 有显示、文档里是 null、
        /// min/max 不出现」（截图现场）。延后一拍重建这一支：重建会按声明重写判别键，再把物化跑到不动点。
        /// </summary>
        public void OnUnionBranchSwitched(McdocTreeNode node)
        {
            McdocWriteTrace.Trace("branch-switch-enter", "node=" + (node is null ? "<null>" : (node.JsonPath ?? "?"))
                + " json=" + (node?.Json?.Kind.ToString() ?? "<null>")
                + " member=" + (node?.SelectedUnionMember?.Kind.ToString() ?? "<null>"));
            if (node?.Json is null || node.SelectedUnionMember is null) return;
            // 切支只把值换成新支的形状（getChange 经常给 {}）：必填判别键此刻还是空的，
            // 靠它做动态索引的同层 spread 自然解不出来（现场：List 支切 Object 支后只有 function 行、
            // 文档里 json={}，enchantment/formula 一个都不出现）。
            // 这里与"给列表加一项"走**同一条时序**：物化到不动点（写判别键 → 整篇重解）→ 重投影 → 再排一拍定向重建。
            var wasLoading = GetLoading?.Invoke() ?? false;
            var wasRebuilding = Rebuilding;
            Rebuilding = true;
            SetLoading?.Invoke(true);
            var written = 0;
            try
            {
                var shellsBefore = SnapshotEmptyStalePaths();
                written = Materializer.MaterializeUntilStable() + PurgeStaleShells(shellsBefore, null);
            }
            finally
            {
                SetLoading?.Invoke(wasLoading);
                Rebuilding = wasRebuilding;
            }
            if (written > 0 && Document is not null)
            {
                McdocWriteTrace.Trace("union-branch-materialize", "path=" + (node.JsonPath ?? "?") + " written=" + written);
                EnsureProjectionCurrent();
                if (!Projector.RefreshAround(node)) Projector.Reset(Document.ProjectionRoots);
                MarkTextDirtyAfterSync(node, null);
                TextDirty?.Invoke();
            }
            // **无论有没有写过值**都要按当前文档把这一支重解一次：切支会把 union 的 Json 换成新对象
            // （ConvertJsonToSelectedBranch），而这一支的分支节点是按**旧对象**建的（branchNode.Json ??= node.Json），
            // 它的 spread 读到的还是上一支的 JSON —— 表现就是"切到 Object 支只有 function 一行、
            // 兄弟字段一个都不出现"，只有再手动触发一次整支重建才会好。
            if (Document is not null)
            {
                // 两拍：第一拍把分支建出来（这一拍里分支节点仍按**切支前**的 JSON 求值，
                // 因为 union 的 Json 是刚换的新对象、分支节点的绑定要等它落地），
                // 第二拍按"已经绑定新对象"的节点再解一次 —— spread 才会真正吃到判别键。
                var path = node.JsonPath ?? "";
                RebuildTargetedOrFallback(Document.FindNode(path) ?? node);
                RebuildTargetedOrFallback(Document.FindNode(path) ?? node);
            }
        }

        /// <summary>按锚点重建一棵子树；锚点找不到就整篇重载。重建后补物化，写过值就整表重投影。</summary>
        public void RebuildTargetedOrFallback(McdocTreeNode anchor)
        {
            if (anchor is null)
            {
                Fallback?.Invoke();
                return;
            }
            // 锚点已被上一次重建换掉时才按路径重找：同路径可能命中动态键占位行（无键身份）
            var current = Document.IsAttached(anchor) ? anchor : Document.FindNode(anchor.JsonPath) ?? anchor;
            var wasLoading = GetLoading?.Invoke() ?? false;
            var wasRebuilding = Rebuilding;
            Rebuilding = true;
            SetLoading?.Invoke(true);
            try
            {
                var rebuilt = Document.RebuildNode(current);
                if (rebuilt is null)
                {
                    Fallback?.Invoke();
                    return;
                }
                if (!Projector.RefreshAround(rebuilt))
                {
                    Projector.Reset(Document.ProjectionRoots);
                }
                var writes = new List<McdocTreeNode>();
                var shellsBefore = SnapshotEmptyStalePaths();
                if (Materializer.MaterializeUntilStable(writtenNodes: writes) + PurgeStaleShells(shellsBefore, writes) > 0)
                {
                    Projector.Reset(Document.ProjectionRoots);
                }
                EnsureProjectionCurrent();
                MarkTextDirtyAfterSync(rebuilt, writes);
                TextDirty?.Invoke();
            }
            finally
            {
                SetLoading?.Invoke(wasLoading);
                Rebuilding = wasRebuilding;
            }
            DrainPendingChildBuilds();
        }

        /// <summary>
        /// 给文本侧留一次"只脏了这棵子树"的标记：文本可以只重排这段子树、只替换它自己那段字符。
        /// <paramref name="written"/> 是这一笔物化写过的节点 —— 只要有一个落在子树之外，整篇文本都可能变，
        /// 标记必须退回 null（宁可整篇格式化一次，也不许文本与模型对不上）。
        /// </summary>
        private void MarkTextDirty(McdocTreeNode node, List<McdocTreeNode> written = null)
        {
            if (Document is null) return;
            // 条目级删除要带出"被删掉的那个值"（模型里已经没有它，文本侧只能靠它的锚点定位）
            var removedItem = node?.TextRemovedItem;
            if (node is not null) node.TextRemovedItem = null;
            if (node is null)
            {
                Document.TextDirtyNode = null;
                Document.TextDirtyRemovedItem = null;
                return;
            }
            if (written is not null)
            {
                foreach (var other in written)
                {
                    if (other?.Json is null) continue;
                    if (!IsSameOrBelow(other.JsonPath, node.JsonPath))
                    {
                        Document.TextDirtyNode = null;
                        Document.TextDirtyRemovedItem = null;
                        return;
                    }
                }
            }
            Document.TextDirtyNode = node;
            Document.TextDirtyRemovedItem = removedItem;
        }

        /// <summary>
        /// 同步收尾的文本标记：这一拍若摘过“判废删除”的键，动的是**另一棵子树**里的键，
        /// 只脏这一棵会把删掉的键留在文本里 —— 退回整篇对账。
        /// </summary>
        private void MarkTextDirtyAfterSync(McdocTreeNode node, List<McdocTreeNode> written)
        {
            if (staleValueDropped)
            {
                staleValueDropped = false;
                MarkTextDirty(null);
                return;
            }
            MarkTextDirty(node, written);
        }

        /// <summary>路径相等或在其之下（下一个字符必须是 <c>.</c> 或 <c>[</c>，否则 $.ab 会算成 $.a 的子级）。</summary>
        private static bool IsSameOrBelow(string path, string rootPath)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(rootPath)) return false;
            if (string.Equals(path, rootPath, StringComparison.Ordinal)) return true;
            return path.Length > rootPath.Length && path.StartsWith(rootPath, StringComparison.Ordinal)
                && (path[rootPath.Length] == '.' || path[rootPath.Length] == '[');
        }

        /// <summary>
        /// 用户在行内编辑框里连续键入时走的值路径：只刷诊断与文本，不重建依赖。
        /// 依赖重建由调用方按"这个键是否真的参与索引"延后到失焦补跑 —— 同步重建会把承载光标的行换掉。
        /// </summary>
        public void RefreshValueOnly(McdocTreeNode node)
        {
            if (node is null || Document is null) return;
            Diagnostics.Refresh(node);
            MarkTextDirty(node);
            TextDirty?.Invoke();
        }

        /// <summary>
        /// 一个值改完之后要做的：诊断，以及"靠这个键解出来的兄弟子树"的重建。
        /// 不做后一步的话，properties 这类 dispatcher 索引在兄弟键落值前后解出的类型不同，
        /// 行却只刷自己不重建 —— 用户看到的就是"第一次是哑的，折叠再展开才好"。
        /// </summary>
        public void OnValueChanged(McdocTreeNode node)
        {
            if (node is null || Document is null) return;
            using var stage = McdocWriteTrace.Stage("pipeline.value");
            var wasLoading = GetLoading?.Invoke() ?? false;
            var wasRebuilding = Rebuilding;
            Rebuilding = true;
            SetLoading?.Invoke(true);
            try
            {
                OnValueChangedCore(node);
            }
            finally
            {
                SetLoading?.Invoke(wasLoading);
                Rebuilding = wasRebuilding;
            }
            DrainPendingChildBuilds();
        }

        private const string ShapeMismatchPrefix = "值与类型不符";

        private void OnValueChangedCore(McdocTreeNode node)
        {
            // 这次写入换掉了"值的形状"：建树时按旧值下的错配诊断必须跟着重解，否则行上留着一条
            // 描述旧值的黄叹号（现场：枚举行上原本是 {}，选了候选项后还在说"实际 Object"）。
            if (node?.Error?.StartsWith(ShapeMismatchPrefix, StringComparison.Ordinal) == true
                && Document.RebuildNode(node) is { } healed)
            {
                node = healed;
                if (!Projector.RefreshAround(healed)) Projector.Reset(Document.ProjectionRoots);
            }
            Diagnostics.Refresh(node);
            var rebuilt = RebuildDependents([node], out var parentRebuilds, out var droppedDerivedContainers);
            // 父级重建/派生容器被摘掉时，子行集合和行命令都换代了：局部 RefreshAround 在
            // 嵌套 list/union 的已展开子树上不保证覆盖（现场：item_modifier 的 Properties 展开着切
            // 无属性方块，旧行/旧 trash 留着；再切回来添加按钮点不动）。这一拍必须整表对账。
            var forceProjectionReconcile = parentRebuilds > 0 || droppedDerivedContainers > 0;
            foreach (var dependent in rebuilt)
            {
                // 重建出来的新成员在懒建下是"只有壳"的：不先把子级建出来，投影画不出行、
                // 物化也遍历不到 —— 现场就是"切成员后 block 有行没值、properties 没有方块上下文"。
                // 这里已经在 Rebuilding 闸门里，补建抬起的钩子不会再排第二次重建（不自我驱动）。
                dependent.EnsureChildren();
                if (!forceProjectionReconcile && !Projector.RefreshAround(dependent))
                {
                    Projector.Reset(Document.ProjectionRoots);
                }
                Diagnostics.Refresh(dependent, refreshHead: true);
            }
            // 重建会把"必填 + 闭合候选"的判别键写进文档（工厂的判别键物化），写过就得重排一次
            var dirtyWrites = new List<McdocTreeNode>();
            var shellsBefore = SnapshotEmptyStalePaths();
            var materialized = rebuilt.Count > 0 ? Materializer.MaterializeUntilStable(writtenNodes: dirtyWrites) : 0;
            materialized += PurgeStaleShells(shellsBefore, dirtyWrites);
            if (forceProjectionReconcile || materialized > 0)
            {
                Projector.Reset(Document.ProjectionRoots);
            }

            // 模型可能被重建写过：文本必须跟上，否则"视图同代"当场破（这条出口原先漏了）
            MarkTextDirtyAfterSync(node, dirtyWrites);
            TextDirty?.Invoke();
        }

        /// <summary>一次值变化最多重建多少个依赖子树（防意外大扇出把界面拖住）。</summary>
        private const int MaxDependents = 64;

        /// <summary>
        /// 值变化之后"靠这个键解出来的兄弟子树"必须**重新求值**（不是刷新）：dispatcher/indexed 按同级键求值，
        /// 兄弟键没值之前只能解成兜底类型（空下拉、动态键占位）。按**声明**判断依赖关系，不认路径也不认名字。
        /// 这里只做模型改动：不开闸、不碰投影、不回写 —— 闸门与投影由调用方一拍完成。
        /// 文本→树通道（McdocSyncEngine 的值补丁）也走这一份，否则"右侧把 block 改空、左侧 properties 还留着旧子级"。
        /// </summary>
        public List<McdocTreeNode> RebuildDependents(IReadOnlyList<McdocTreeNode> changed)
            => RebuildDependents(changed, out _, out _);

        internal List<McdocTreeNode> RebuildDependents(IReadOnlyList<McdocTreeNode> changed,
            out int parentRebuilds, out int droppedDerivedContainers)
        {
            parentRebuilds = 0;
            droppedDerivedContainers = 0;
            var rebuilt = new List<McdocTreeNode>();
            if (changed is null || Document is null) return rebuilt;
            using var stage = McdocWriteTrace.Stage("dep.rebuild");
            var seen = new HashSet<McdocTreeNode>();
            // 判别键的**父级**也是依赖方：...dispatch[[key]] 的 spread 挂在父结构上（父级自己的 Indices 是空的），
            // 只扫兄弟会整条漏掉 —— 现场是"切 condition 成员后 block 没出现、旧成员的 terms 还挂着、
            // properties 没有方块上下文"。父级重建放到第二段做：它会把 changed 里的节点一起换掉。
            var parents = new List<McdocTreeNode>();
            // 值整形必须放到**所有重建之后**做：父级重建会把刚重建好的兄弟节点再换掉，
            // 就地整形等于把结果写进一个马上作废的节点（现场：argument 整了形，行上还是 6）。
            var reshapes = new List<(string Path, McdocType OldType)>();
            foreach (var node in changed)
            {
                if (node?.Parent is null || string.IsNullOrEmpty(node.Key)) continue;
                var parent = node.Parent;
                // 判据两条：声明里明确有"按这个键索引的 spread"；或父级求值时读过运行期数据
                // （真 schema 的 spread 常被 Reference/Mapped/Concrete 包着，声明上摸不到 Fields）。
                // 判别键的宿主可能被 union 摊平（Object 支的字段直接挂在 union 行下）：
                // 这时 parent.DeclaredType 是 union、Fields 在 SelectedUnionMember 上，HasSpreadIndex 恒 false；
                // 而且根 union 的 Parent 是 null，老条件 parent.Parent is not null 会把整条依赖链漏掉
                // （现场：根切 Object 后切 Function，同层 shape/trail 永远不解）。
                var flattenedBranch = parent.Type?.Kind == McdocTypeKind.Union;
                if ((flattenedBranch || parent.Parent is not null)
                    && parent.Json is { Kind: McdocJsonKind.Object }
                    && seen.Add(parent)
                    && ParentDrivesDependents(node))
                {
                    McdocWriteTrace.Trace("dependents-parent", "path=" + (parent.JsonPath ?? "?") + " key=" + node.Key);
                    parents.Add(parent);
                }
                foreach (var sibling in McdocPresenterRegistry.Default.Children(node.Parent).ToList())
                {
                    if (ReferenceEquals(sibling, node) || !seen.Add(sibling)) continue;
                    if (!IndexesByKey(sibling.DeclaredType, node.Key)) continue;
                    if (rebuilt.Count >= MaxDependents)
                    {
                        McdocWriteTrace.Trace("dependents-limit", "cap=" + MaxDependents);
                        return rebuilt;
                    }
                    // 必须**重新求值**（RebuildNode）：RefreshNode 只是重跑表现器，用的还是建节点那一刻算出来的
                    // 兜底类型 —— 兄弟键落值前解成 any 的子树，光刷新永远刷不回来。
                    // 没有求值现场（占位行）重建出来必然没有键身份：跳过这一个，不整树回退（那会打断编辑）。
                    if (Document.RebuildNode(sibling) is { } fresh)
                    {
                        ClearDerivedMembers(sibling, fresh);
                        reshapes.Add((sibling.JsonPath, sibling.Type));
                        rebuilt.Add(fresh);
                    }
                }
            }
            foreach (var parent in parents)
            {
                if (rebuilt.Count >= MaxDependents)
                {
                    McdocWriteTrace.Trace("dependents-limit", "cap=" + MaxDependents);
                    break;
                }
                if (Document.RebuildNode(parent) is not { } freshParent)
                {
                    McdocWriteTrace.Trace("dependents-parent-skip", "path=" + (parent.JsonPath ?? "?"));
                    continue;
                }
                ClearDerivedMembers(parent, freshParent);
                // 依赖键换掉之后，父级下那些"解不出任何成员"的派生容器要退回"没有值"：
                // 它们的 ⊕ 判据是 Json == null，留着空对象就是一条既没子行、又点不动的死行
                // （现场：Properties 展开着切到无属性方块，行还在但 ⊕ 灰着）。
                // **必须按声明求值**：重建出来的父级这一拍还没建子级，遍历 Children 会一个都碰不到。
                droppedDerivedContainers += DropEmptyDerivedContainers(freshParent);
                parentRebuilds++;
                reshapes.Add((parent.JsonPath, parent.Type));
                rebuilt.Add(freshParent);
            }
            foreach (var (path, oldType) in reshapes) ReshapeDependentValue(path, oldType);
            if (rebuilt.Count > 0)
            {
                McdocWriteTrace.Trace("dependents", "changed=" + changed.Count + " rebuilt=" + rebuilt.Count);
            }
            return rebuilt;
        }

        /// <summary>
        /// 依赖键换了之后，被它决定的那一层**值**也要按新类型整形。
        /// 现场：modifier 从（未设置）切到 alpha_blend，argument 的类型由 float 变成 FloatWithAlpha，
        /// 文档里却还留着 6 —— 行上只能挂"值与类型不符：期望 Struct，实际 Number"，用户看着就是"解析错了"。
        /// misode 的 getChange 在这里把 6 包成 { value: 6 }。
        /// </summary>
        /// <summary>
        /// 包装宿主判定用的类型：先沿 Item 拆掉指令壳（Mapped/Concrete/Reference 这些"怎么算"不是"算出来是什么"），
        /// 解不出来的形态一律当成"判不了"返回 null。
        /// 以前直接拿声明判 ValueFits，而它对未知 kind 返回 true —— 现场：blend_to_gray 的 brightness: float @ 0..1
        /// 声明是 Mapped，数组被当成"吃得下 float"，整个数组被塞进 { "brightness": [...] }。
        /// </summary>
        private static McdocType ShapeConclusiveType(McdocType declared)
        {
            for (var depth = 0; declared?.IsDirective == true && declared.Item is not null && depth < 8; depth++)
            {
                declared = declared.Item;
            }
            if (declared is null) return null;
            return declared.Kind is McdocTypeKind.Any or McdocTypeKind.Unsafe or McdocTypeKind.Union
                or McdocTypeKind.Reference or McdocTypeKind.Concrete or McdocTypeKind.Template
                or McdocTypeKind.Mapped or McdocTypeKind.Indexed or McdocTypeKind.Dispatcher
                ? null
                : declared;
        }

        private void ReshapeDependentValue(string path, McdocType oldType)
        {
            if (Document is null || string.IsNullOrEmpty(path)) return;
            var fresh = Document.FindNode(path);
            if (fresh?.Json is null) return;
            // 判据要用**声明重新求值**出来的类型：节点上的 Type 在"值与类型不符"时已经降级成 any，
            // 拿它判 ValueFits 恒真，整形永远不会发生（现场：argument 明明该是 FloatWithAlpha）。
            var runtime = Document.CreateRuntime();
            var target = McdocDefaults.ResolveType(fresh.DeclaredType, runtime, fresh.ValueNode);
            if (target is null || target.Kind is McdocTypeKind.Any or McdocTypeKind.Unsafe) return;
            if (target.Kind == McdocTypeKind.Union)
            {
                ReshapeUnionValue(fresh, target, oldType, runtime);
                return;
            }
            if (McdocHelpers.ValueFits(fresh.Json, target)) return;
            // 旧类型优先用重建前那个；它常常已经退化成 any（值本来就不合法）——
            // 那时用**值自己的形状**当旧类型，getChange 的 X→{k:X} 才认得出来（6 → { value: 6 }）。
            if (oldType is null || oldType.Kind is McdocTypeKind.Any or McdocTypeKind.Unsafe)
            {
                oldType = fresh.Json.InferType() ?? oldType;
            }
            // X → {k:X}：新类型是结构、而旧值不是对象时，唯一"吃得下这个值"的必填字段就是它的落点。
            // getChange 的 QuickEqualTypes 不跨 kind（float vs int 判不同型），数值档会被丢成默认 0 ——
            // 现场：argument 从 6 变成 { value: 6 }，不是 { value: 0 }。
            if (target.Kind == McdocTypeKind.Struct && fresh.Json.Kind != McdocJsonKind.Object)
            {
                var host = (target.Fields ?? []).FirstOrDefault(field => field is not null && !field.Optional
                    && !field.IsSpread && !string.IsNullOrEmpty(field.Name)
                    && ShapeConclusiveType(field.Type) is { } fieldShape
                    && McdocHelpers.ValueFits(fresh.Json, fieldShape));
                if (host is not null)
                {
                    var wrapped = McdocJsonValue.Object();
                    wrapped.Add(host.Name, fresh.Json);
                    McdocWriteTrace.Trace("dependents-reshape", "path=" + (fresh.JsonPath ?? "?")
                        + " wrap " + fresh.Json.Kind + " -> {" + host.Name + "}");
                    McdocCollectionEditor.Default.EnsurePath(fresh);
                    fresh.Json = wrapped;
                    McdocCollectionEditor.Default.WriteIntoParent(fresh, wrapped);
                    Document.RebuildNode(fresh);
                    return;
                }
            }
            McdocJsonValue converted;
            try
            {
                converted = McdocHelpers.GetChange(target, oldType ?? fresh.DeclaredType, fresh.Json,
                    McdocDefaults.Options(Document.CreateRuntime()));
            }
            catch (Exception)
            {
                return;     // 整形失败宁可保留原值 + 行上诊断，也不许把文档改坏
            }
            // 同 kind 不动手：那一档交给 ClearDerivedMembers 的"值是否仍然成立"
            if (converted is null || converted.Kind == fresh.Json.Kind) return;
            McdocWriteTrace.Trace("dependents-reshape", "path=" + (fresh.JsonPath ?? "?")
                + " from=" + fresh.Json.Kind + " to=" + converted.Kind);
            McdocCollectionEditor.Default.EnsurePath(fresh);
            fresh.Json = converted;
            McdocCollectionEditor.Default.WriteIntoParent(fresh, converted);
            // 值换了形状：这一支要按新值重新求值一次，行上的类型才会跟着变成 FloatWithAlpha
            Document.RebuildNode(fresh);
        }

        /// <summary>
        /// 目标求值成联合时的整形：**一支都装不下就清掉**（用户口径），但只清"切支后失效的旧值"。
        /// 判据不能再用 <see cref="McdocHelpers.ValueFits"/>：它对联合恒真（落到默认那一支），
        /// "值不属于这个联合的任何一支"的现场在它那里就先退了，永远走不到整形。
        /// 现场：modifier 从 blend_to_gray 切到 alpha_blend，argument 留着 {brightness,factor}，
        /// 行上只能把旧值挂成一条「原始 JSON」子行 —— 那是"值本来就不属于任何一支"的兜底，
        /// 不是"数据该留"。
        /// 与「外来值」的分界只有一条：值在**切换前那一版**类型下确实成立（能证明它曾是这一支的数据）。
        /// 判不了（旧类型是联合/any，或值本来就对不上旧类型）就不删，仍然挂「原始 JSON」。
        /// </summary>
        private void ReshapeUnionValue(McdocTreeNode fresh, McdocType target, McdocType oldType, McdocRuntime runtime)
        {
            var context = new McdocSimplifyContext { Runtime = runtime, Node = fresh.ValueNode };
            // 与渲染层同一条判据：有没有一支吃得下（SelectMember 认"唯一命中"，AnyMemberFitsShape 认"形状对得上"）
            if (McdocTypeMatcher.SelectMember(target, fresh.Json, context, preferShapeOverAny: true) is not null
                || McdocTypeMatcher.AnyMemberFitsShape(target, fresh.Json, context)) return;
            var stale = ValueFitsSwitchedOldType(fresh.Json, oldType, context);
            McdocWriteTrace.Trace("dependents-stale", "path=" + (fresh.JsonPath ?? "?")
                + " kind=" + fresh.Json.Kind + " old=" + (oldType?.Kind.ToString() ?? "null")
                + " stale=" + (stale?.ToString() ?? "?"));
            if (stale != true) return;
            // 只剪键、行留在树上（= map 条目（未设置）那一档）：结构变更会把父级按 JSON 重建，这一行就没了
            DropStaleValue(fresh);
            McdocWriteTrace.Trace("dependents-reshape", "path=" + (fresh.JsonPath ?? "?") + " stale-drop");
        }

        /// <summary>
        /// 这个值当初在**旧类型**下成立吗。返回 null 表示判不了（旧类型是联合/any/解不完的指令壳）——
        /// 判不了就不许当成"旧值"删掉：手改 JSON 塞进来的外来值必须继续走「原始 JSON」兜底。
        /// </summary>
        private static bool? ValueFitsSwitchedOldType(McdocJsonValue value, McdocType oldType,
            McdocSimplifyContext context)
        {
            var shape = ShapeConclusiveType(oldType);
            if (shape is null || value is null) return null;
            return McdocTypeMatcher.Matches(shape, value, context);
        }

        /// <summary>
        /// 判废的旧值：摘掉键、行留在树上，并收回“值不属于这个联合的任何一支”那条判词。
        /// 路径记进 <see cref="staleUnsetPaths"/> —— 必填容器随后会被物化补一个空壳，壳要等物化之后再摘一次。
        /// </summary>
        private void DropStaleValue(McdocTreeNode node)
        {
            if (node is null) return;
            if (!string.IsNullOrEmpty(node.JsonPath) && staleUnsetPaths.Count < MaxStaleUnsetPaths)
            {
                staleUnsetPaths.Add(node.JsonPath);
            }
            node.Error = null;
            McdocCollectionEditor.Default.UnsetEntryValue(node);
            staleValueDropped = true;
        }

        /// <summary>
        /// 物化前给“判废删除”的路径拍一次快照：只有这一刻**确实没值**的路径才可能被物化补出空壳。
        /// 拍在物化之前是硬要求 —— 拍在之后分不清“物化补的壳”和“用户刚选进来的空容器”。
        /// </summary>
        private List<string> SnapshotEmptyStalePaths()
        {
            if (staleUnsetPaths.Count == 0 || Document is null) return null;
            List<string> empty = null;
            foreach (var path in staleUnsetPaths)
            {
                if (Document.FindNode(path)?.Json is null) (empty ??= []).Add(path);
            }
            return empty;
        }

        /// <summary>
        /// 物化之后给“判废删除”的路径收口：必填容器在空值处会被补出空壳（{} / []），
        /// 那个壳既不是用户写的值、也不属于任何一支，留着等于把刚删掉的值又写回一行。
        /// 判据是双条件：物化前没值（快照）+ 物化后是空壳。路径上一旦有真值就出表。
        /// </summary>
        private int PurgeStaleShells(List<string> emptyBeforeMaterialize, List<McdocTreeNode> written)
        {
            if (emptyBeforeMaterialize is null || emptyBeforeMaterialize.Count == 0 || Document is null) return 0;
            var purged = 0;
            foreach (var path in emptyBeforeMaterialize)
            {
                var node = Document.FindNode(path);
                if (node is null || !Document.IsAttached(node))
                {
                    staleUnsetPaths.Remove(path);
                    continue;
                }
                if (node.Json is null) continue;                                  // 这一趟没补壳：还是（未设置）
                if (!IsMaterializedShell(node.Json))
                {
                    staleUnsetPaths.Remove(path);                                 // 有真值了：这条不再管它
                    continue;
                }
                node.Error = null;
                McdocCollectionEditor.Default.UnsetEntryValue(node);
                written?.Add(node);
                staleValueDropped = true;
                purged++;
                McdocWriteTrace.Trace("dependents-reshape", "path=" + path + " stale-shell-drop");
            }
            return purged;
        }

        /// <summary>空壳：物化给“必填容器没有值”补的那个占位（空对象 / 空数组）。</summary>
        private static bool IsMaterializedShell(McdocJsonValue json)
            => json.Kind switch
            {
                McdocJsonKind.Object => json.Properties is not { Count: > 0 },
                McdocJsonKind.Array => json.Items is not { Count: > 0 },
                _ => false,
            };

        /// <summary>
        /// 这个键的值变了，是否**真的**需要"重新求值依赖方"。
        /// 以前只看 <see cref="McdocTreeNode.IsRuntimeDependent"/>（"这一段里有人读过运行期数据"），
        /// 于是容器里任何一个数字/布尔改动都会rebuild 一层：整表 Reset → 行容器重建 → 滚动视图跳一下，
        /// 而 typo（weight 这种不参与索引的键）根本没有任何依赖方。
        /// 判据与 RebuildDependents 同源：同层 spread 按这个键索引，或某个兄弟声明的类型按这个键索引。
        /// </summary>
        public bool ValueDrivesDependents(McdocTreeNode node)
        {
            if (node?.Parent is null || string.IsNullOrEmpty(node.Key)) return false;
            var parent = node.Parent;
            if (ParentDrivesDependents(node)) return true;
            foreach (var sibling in McdocPresenterRegistry.Default.Children(parent))
            {
                if (sibling is null || ReferenceEquals(sibling, node)) continue;
                if (IndexesByKey(sibling.DeclaredType, node.Key)) return true;
            }
            return false;
        }

        /// <summary>
        /// 父级自己是否"按这个键求值"：只认生效结构上声明了按 key 索引的 spread，或父级类型
        /// 自身的 Indices 访问器。以前用 parent.DependsOnRuntimeData 当兜底，把"结构里任何角落
        /// 读过运行期数据"都当成依赖方 —— 改一个不参与索引的数字也会重建父级，行被换掉、输入框丢焦点。
        /// </summary>
        private bool ParentDrivesDependents(McdocTreeNode node)
        {
            var parent = node?.Parent;
            if (parent is null || string.IsNullOrEmpty(node.Key)) return false;
            return ParentDrivesDependentsCore(node, parent);
        }

        private bool ParentDrivesDependentsCore(McdocTreeNode node, McdocTreeNode parent)
        {
            // 求值后的 type 已经把 spread 摊进字段（IsSpread 没了），按声明再找一遍；
            // 声明里的 LootCondition/Predicate 这类还是 Reference，要沿符号表解到定义体。
            // 真 schema 的根常常是 dispatcher（predicate = dispatch ... to Predicate）：父节点
            // declaredType 是 Dispatcher，声明里找不到 spread；但"候选表 + 父级读过运行期数据"
            // 就是判别键的现场（condition / block 这类），必须重建父级让同层 spread 重新求解。
            // 普通数字/纯文本没有候选表，不会走这条兜底；候选行里允许自由输入的文本框在 VM 侧
            // 已由 IsTextEditorFocused 延后重建，不会每敲一字换行。
            if (parent.DependsOnRuntimeData && node.EnumOptionList is { Count: > 0 }) return true;
            if (DeclaredSpreadIndexesKey(parent.DeclaredType, node.Key)) return true;
            var effective = EffectiveStruct(parent) ?? parent.DeclaredType;
            if (HasSpreadIndex(effective, node.Key)) return true;
            return IndexesByKey(effective, node.Key) || IndexesByKey(parent.DeclaredType, node.Key);
        }

        /// <summary>
        /// 声明里有没有"按 key 做动态索引的 spread"：结构体字段、union 成员、指令壳和 Reference 都要看穿。
        /// 求值后的 <see cref="McdocTreeNode.Type"/> 会把 spread 摊平（字段不再带 IsSpread），
        /// 只查求值形态会漏掉 condition / block_states 这类"父级 spread 消费判别键"的现场。
        /// </summary>
        private bool DeclaredSpreadIndexesKey(McdocType type, string key, int depth = 0)
        {
            if (type is null || depth > 12) return false;
            if (IndexesByKey(type, key)) return true;
            if (type.Kind == McdocTypeKind.Struct)
            {
                foreach (var field in type.Fields ?? [])
                {
                    if (field is not { IsSpread: true }) continue;
                    if (IndexesByKey(field.Type, key)) return true;
                }
                return false;
            }
            if (type.Kind == McdocTypeKind.Union)
            {
                foreach (var member in type.Members ?? [])
                {
                    if (DeclaredSpreadIndexesKey(member, key, depth + 1)) return true;
                }
                return false;
            }
            if (type.Kind == McdocTypeKind.Reference)
            {
                McdocType target = null;
                try
                {
                    target = string.IsNullOrEmpty(type.Path)
                        ? null
                        : Document?.Symbols?.GetTypeDef(type.Path, Document.DocumentUri ?? "",
                            string.IsNullOrEmpty(type.Module) ? Document.ModulePath : type.Module);
                }
                catch (Exception)
                {
                    // 符号表对相对名/未绑定形参不保证返回 null：判定只是启发式，查不到就交给后续兜底
                    target = null;
                }
                return target is not null && !ReferenceEquals(target, type)
                    && DeclaredSpreadIndexesKey(target, key, depth + 1);
            }
            return type.IsDirective && type.Item is not null
                && DeclaredSpreadIndexesKey(type.Item, key, depth + 1);
        }

        /// <summary>
        /// 声明类型里是否有「按这个键做动态索引」的 dispatcher/indexed（探针也用它断言反向）。
        /// 声明常被指令包着（Mapped{copy} / Concrete / Indexed 空壳——它们只换上下文、不带自己的索引，
        /// 见 McdocSimplifier.SimplifyIndexed 的"外壳"规则），而索引挂在里层的 Dispatcher 上：
        /// 只看顶层 Kind 会漏掉整条链，表现就是"兄弟键落值了、依赖它的子树不重新求值"。
        /// </summary>
        public static bool IndexesByKey(McdocType type, string key)
        {
            if (type is null || string.IsNullOrEmpty(key)) return false;
            var current = type;
            // 环保护：声明链最长也就几层包装，8 层足够且不许无限下钻
            for (var depth = 0; current is not null && depth < 8; depth++)
            {
                if (HasDynamicAccessor(current, key)) return true;
                if (!current.IsDirective) return false;
                // 这一层自己就带动态索引：判定到此为止（再往下钻会把"另一层索引"混算成命中）
                if (HasDynamicIndex(current)) return false;
                current = current.Item;
            }
            return false;
        }

        private static bool HasDynamicIndex(McdocType type)
        {
            foreach (var index in type.Indices ?? [])
            {
                if (index?.Kind == McdocIndexKind.Dynamic && index.Accessor is { Count: > 0 }) return true;
            }
            return false;
        }

        /// <summary>
        /// 依赖键换了（切方块 / 切枚举成员）之后，被它决定的那一层要**按新结构重建**：
        /// ① 新结构不声明、旧结构声明过的键删掉（旧成员的残影）；
        /// ② 两边都声明的键，值在新声明下不成立也删（现场：方块 A 的 `facing` 值落到方块 B 上）。
        /// 两边都声明且值仍成立的键保留 —— 那是用户的数据，不是"旧成员的残影"。
        /// 形态没变（两侧字段集相同）则一个字都不动。
        /// 「保留旧键」只属于**换游戏版本**那条路径（README §2.7），它不经过这里。
        /// </summary>
        private static int ClearDerivedMembers(McdocTreeNode oldNode, McdocTreeNode rebuilt)
        {
            // 依赖层常常求值成 **Union**（`Int | struct`、"版本变体 union"）：只看 Type.Kind 会把整批跳过
            // （现场日志：dependents=8 次而 dependents-drop=0 次）。取"这一支实际生效的结构"再判。
            var path = rebuilt?.JsonPath ?? "?";
            var oldStruct = EffectiveStruct(oldNode);
            var newStruct = EffectiveStruct(rebuilt);
            if (oldStruct is null || newStruct is null)
            {
                McdocWriteTrace.Trace("dependents-clear-skip", "path=" + path + " reason=no-struct"
                    + " old=" + (oldNode?.Type?.Kind.ToString() ?? "null") + "/" + (oldStruct is null ? "-" : "ok")
                    + " new=" + (rebuilt?.Type?.Kind.ToString() ?? "null") + "/" + (newStruct is null ? "-" : "ok"));
                return 0;
            }
            if (rebuilt.Json is not { Kind: McdocJsonKind.Object } container || container.Properties is not { Count: > 0 })
            {
                var reason = rebuilt.Json is { Kind: McdocJsonKind.Object } ? "empty-object" : "no-object";
                McdocWriteTrace.Trace("dependents-clear-skip", "path=" + path + " reason=" + reason
                    + " kind=" + (rebuilt.Json?.Kind.ToString() ?? "null")
                    + " keys=" + (rebuilt.Json?.Properties?.Count ?? -1));
                return 0;
            }
            var declared = new Dictionary<string, McdocType>(StringComparer.Ordinal);
            foreach (var field in newStruct.Fields ?? [])
            {
                if (field?.Name is { Length: > 0 } name) declared[name] = field.Type;
            }
            var dropped = new List<string>();
            foreach (var pair in container.Properties)
            {
                // 新结构完全不声明这个键：旧成员的残影
                if (!declared.TryGetValue(pair.Key, out var fieldType))
                {
                    // 但**动态键（map）里的键是数据**，不是残影：map 容器一个静态字段都没有，按"没声明"
                    // 判会把整张表清空（现场：attributes 里任意一个值变化 → 父级重建 → 所有条目消失）。
                    // 判据必须走 #[id] 的 prefix 规则 —— spread / 派发兜底字段也长成无名字段，
                    // 拿"有没有无名字段"放宽会把切成员该剪的残影一起留下。
                    if (StructNodePresenter.AcceptsDynamicKey(newStruct, pair.Key)) continue;
                    dropped.Add(pair.Key);
                    continue;
                }
                // 两边都声明：值在新声明下还成立吗
                if (!McdocHelpers.ValueFits(pair.Value, fieldType)) dropped.Add(pair.Key);
            }
            if (dropped.Count == 0)
            {
                McdocWriteTrace.Trace("dependents-clear-skip", "path=" + path + " reason=all-valid"
                    + " fields=" + string.Join(",", declared.Keys));
                return 0;
            }
            foreach (var name in dropped)
            {
                container.Properties.RemoveAll(pair => string.Equals(pair.Key, name, StringComparison.Ordinal));
                // 键从文档里剪掉了，**子节点也必须当场摘掉**：只靠 ChildrenBuilt=false 靠不住 ——
                // 只要有一拍在重建前把子级建出来，那个"文档里已经没有的 key"就还挂着一份旧 Json
                // （现场：Properties 展开着切到无属性方块，行还在、⊕ 又因为"已有值"而禁用 = 点不动）。
                DropChildNode(rebuilt, name);
                McdocWriteTrace.Trace("dependents-drop", "path=" + (rebuilt.JsonPath ?? "?") + " key=" + name);
            }
            // 子级是按删之前的 JSON 建的：让它在下一拍按当前文档重新建一次
            rebuilt.ChildrenBuilt = false;
            rebuilt.Children.Clear();
            return dropped.Count;
        }

        /// <summary>把这个字段名的子节点从三个子级集合里摘掉（文档里已经没有它了）。</summary>
        private static void DropChildNode(McdocTreeNode owner, string fieldName)
        {
            if (owner is null || string.IsNullOrEmpty(fieldName)) return;
            for (var i = owner.Children.Count - 1; i >= 0; i--)
            {
                if (!string.Equals(owner.Children[i]?.FieldName, fieldName, StringComparison.Ordinal)) continue;
                owner.Children[i].Parent = null;
                owner.Children.RemoveAt(i);
            }
            for (var i = owner.Items.Count - 1; i >= 0; i--)
            {
                if (!string.Equals(owner.Items[i]?.FieldName, fieldName, StringComparison.Ordinal)) continue;
                owner.Items[i].Parent = null;
                owner.Items.RemoveAt(i);
            }
            for (var i = owner.SelectedUnionChildren.Count - 1; i >= 0; i--)
            {
                if (!string.Equals(owner.SelectedUnionChildren[i]?.FieldName, fieldName, StringComparison.Ordinal)) continue;
                owner.SelectedUnionChildren[i].Parent = null;
                owner.SelectedUnionChildren.RemoveAt(i);
            }
        }

        /// <summary>
        /// 依赖键换掉之后，"解不出任何成员"的派生容器要退回"没有值"：行上的 ⊕ 判据是 Json == null，
        /// 留着空对象就成了一条既没有子行、又点不动的死行（现场：Properties 展开着切到无属性方块）。
        /// 判据全部走**声明**：字段可选、声明是调度器/索引、按当前上下文求值后是零字段结构、值确实是空对象。
        /// 不看 Children —— 重建出来的这一拍子级还没建，按子级遍历会整片漏掉。
        /// </summary>
        private int DropEmptyDerivedContainers(McdocTreeNode parent)
        {
            if (Document is null || parent?.Json is not { Kind: McdocJsonKind.Object } container) return 0;
            var host = EffectiveStruct(parent);
            if (host?.Fields is null) return 0;
            var runtime = Document.CreateRuntime();
            var dropped = 0;
            foreach (var field in host.Fields)
            {
                if (field is null || field.IsSpread || field.Optional != true) continue;
                if (string.IsNullOrEmpty(field.Name)) continue;
                if (field.Type?.Kind is not (McdocTypeKind.Dispatcher or McdocTypeKind.Indexed)) continue;
                if (container.Get(field.Name) is not { Kind: McdocJsonKind.Object } value) continue;
                if (value.Properties is { Count: > 0 }) continue;
                var context = McdocValueNode.Of(
                    new McdocRuntimeNode(value, value.InferType() ?? McdocType.Any()),
                    parent.ValueNode,
                    new McdocRuntimeNode(field.Name, McdocType.LiteralString(field.Name)));
                var resolved = McdocDefaults.ResolveType(field.Type, runtime, context);
                if (resolved?.Kind != McdocTypeKind.Struct || resolved.Fields is { Count: > 0 }) continue;
                container.Properties.RemoveAll(pair => string.Equals(pair.Key, field.Name, StringComparison.Ordinal));
                // 键没了，行上那个旧节点也必须当场摘掉：它还攥着 {}，⊕ 判据（Json == null）就永远不成立。
                DropChildNode(parent, field.Name);
                parent.ChildrenBuilt = false;
                dropped++;
                McdocWriteTrace.Trace("dependents-drop-empty-container",
                    "path=" + (parent.JsonPath ?? "?") + "." + field.Name + " keys=0");
            }
            return dropped;
        }

        /// <summary>节点"实际生效"的结构：Union 取选中成员（标量支返回 null，那种支没有子成员）。</summary>
        private static McdocType EffectiveStruct(McdocTreeNode node)
        {
            var type = node?.Type;
            if (type?.Kind == McdocTypeKind.Struct) return type;
            if (type?.Kind != McdocTypeKind.Union) return null;
            // union 的"选中成员"是建子级那一拍才算出来的（懒建时还是 null）：先把它建出来再取。
            // 这一步在重建闸门内，结构通知会被早退掉，不会自激。
            if (node.SelectedUnionMember is null) node.EnsureChildren();
            return node.SelectedUnionMember is { Kind: McdocTypeKind.Struct } member ? member : null;
        }

        /// <summary>两侧结构声明的字段集是否一致（按名字与顺序；顺序变了也当变了）。</summary>
        private static bool SameFieldSet(McdocType left, McdocType right)
        {
            var a = (left?.Fields ?? []).Select(field => field?.Name).Where(name => !string.IsNullOrEmpty(name)).ToList();
            var b = (right?.Fields ?? []).Select(field => field?.Name).Where(name => !string.IsNullOrEmpty(name)).ToList();
            if (a.Count != b.Count) return false;
            for (var i = 0; i < a.Count; i++)
            {
                if (!string.Equals(a[i], b[i], StringComparison.Ordinal)) return false;
            }
            return true;
        }

        /// <summary>声明里有 spread 字段，且那个 spread 的类型按这个键做动态索引（父级结构自己的 Indices 是空的）。</summary>
        public static bool HasSpreadIndex(McdocType type, string key)
        {
            if (type is null || string.IsNullOrEmpty(key)) return false;
            foreach (var field in type.Fields ?? [])
            {
                if (field is not { IsSpread: true }) continue;
                if (IndexesByKey(field.Type, key)) return true;
            }
            return false;
        }

        private static bool HasDynamicAccessor(McdocType type, string key)
        {
            foreach (var index in type.Indices ?? [])
            {
                if (index?.Kind != McdocIndexKind.Dynamic || index.Accessor is null) continue;
                foreach (var accessor in index.Accessor)
                {
                    if (string.Equals(accessor, key, StringComparison.Ordinal)) return true;
                }
            }
            return false;
        }

        /// <summary>整篇按当前 JSON 重建（不再重解析文本）。</summary>
        public void Reload()
        {
            using var stage = McdocWriteTrace.Stage("pipeline.rootReload");
            var rootType = RootType?.Invoke();
            if (rootType is null) return;
            var wasLoading = GetLoading?.Invoke() ?? false;
            var wasRebuilding = Rebuilding;
            Rebuilding = true;
            SetLoading?.Invoke(true);
            try
            {
                Document.Load(rootType, Document.RootJson);
                Materializer.MaterializeUntilStable();
                Projector.Reset(Document.ProjectionRoots);
                MarkTextDirty(null);
                TextDirty?.Invoke();
            }
            finally
            {
                SetLoading?.Invoke(wasLoading);
                Rebuilding = wasRebuilding;
            }
        }
    }
}
