using System;
using System.Collections.Generic;
using System.Linq;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 界面那条同步时序的**唯一实现**：结构/值变更之后该做什么（刷新哪一层、物化到不动点、什么时候整表重投影）。
    /// VM 与探针都拿这一份 —— 探针如果自己手抄一遍时序，它绿了也不能说明界面绿（上一轮就是这么翻车的）。
    /// 这里不持有文档：文档、投影、根类型、loading 都由调用方注入，VM 与探针各用自己的那一份。
    /// </summary>
    public sealed class McdocPipeline
    {
        public McdocTreeDocument Document;
        public McdocRowProjector Projector;
        /// <summary>重建文档时用的根类型（换版本会换它）。</summary>
        public Func<McdocType> RootType;
        /// <summary>VM 的 loading 闸门：建树/物化期间不许把赋值当用户编辑。</summary>
        public Func<bool> GetLoading;
        public Action<bool> SetLoading;
        /// <summary>文档被改动，需要把 RootJson 回写到编辑器（VM 接 SyncJsonText；探针可为空）。</summary>
        public Action TextDirty;
        /// <summary>重建失败时的回退（VM 用"不重解析文本的重载"，探针用整篇 Load）。</summary>
        public Action Fallback;
        /// <summary>运行期依赖触发"延后一拍重建"的出口（VM 接 ScheduleRebuild）。</summary>
        public Action<McdocTreeNode> ScheduleRebuild;
        /// <summary>
        /// 正在做重建/刷新。期间投影会去 EnsureChildren，从而触发"刚建出子级"的钩子；
        /// 那个钩子本来只为"用户点开懒建节点"服务，如果它也参与重建，就会自我驱动：
        /// 重建 → 新节点 → 懒建子级 → 钩子 → 再重建（现场：proj-splice/proj-reset 各 118 次、structure 只有 2 次，UI 线程被占死）。
        /// </summary>
        public bool Rebuilding;

        /// <summary>
        /// 重建闸门里"刚建出子级"的节点。投影在【刷新 → 物化 → 投影】这一段里首次建出的子树，
        /// 补物化必须等闸门结束再跑 —— 闸门里写值要么抬出自己被早退的结构通知，要么换掉正在投影的行。
        /// 不补跑的话，新子树里的必填判别键永远不落盘，靠它做动态索引的兄弟 spread 停在 %none
        /// （表现：加了元素，同层调度器成员一个都不出现，行上还挂着「动态键…没有对应成员」）。
        /// </summary>
        private readonly List<McdocTreeNode> pendingChildBuilds = [];
        private const int MaxPendingChildBuilds = 64;

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
            var written = MaterializeChildDefaults(node);
            if (written > 0) ScheduleRebuild?.Invoke(node.RebuildAnchor ?? node);
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
                    written += MaterializeChildDefaults(node);
                    anchor ??= node.RebuildAnchor ?? node;
                }
                if (written > 0)
                {
                    McdocWriteTrace.Trace("children-built-drain", "nodes=" + nodes.Count + " written=" + written);
                    Projector?.Reset(Document.Nodes.ToList());
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
            foreach (var row in Projector.Rows)
            {
                var path = row.Node?.JsonPath;
                if (row.Node is null || string.IsNullOrEmpty(path) || path == "$") continue;
                var current = Document.FindNode(path);
                if (current is null || ReferenceEquals(current, row.Node)) continue;
                McdocWriteTrace.Trace("projection-stale", "path=" + path + " rows=" + Projector.Rows.Count);
                Projector.Reset(Document.Nodes.ToList());
                return;
            }
        }

        private int MaterializeChildDefaults(McdocTreeNode node)
        {
            if (node is null || Document?.IsAttached(node) != true) return 0;
            var wasLoading = GetLoading?.Invoke() ?? false;
            SetLoading?.Invoke(true);
            try
            {
                return McdocCollectionEditor.Default.MaterializeRequiredSelections([node], false, Document.CreateRuntime());
            }
            catch (Exception)
            {
                return 0;
            }
            finally
            {
                SetLoading?.Invoke(wasLoading);
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
                McdocWriteTrace.Trace("structure-root", "rows=" + Projector.Rows.Count + " resets=" + Projector.ResetCount);
                Reload();
                return;
            }
            McdocWriteTrace.Trace("structure", "path=" + (node.JsonPath ?? "?")
                + " runtimeDep=" + node.IsRuntimeDependent + " isMapEntry=" + node.IsMapEntry
                + " rows=" + Projector.Rows.Count + " resets=" + Projector.ResetCount);
            if (node.IsRuntimeDependent)
            {
                // 运行期依赖（动态索引/调度器解出新字段）：重建最近容器，延后一拍
                ScheduleRebuild?.Invoke(node.RebuildAnchor);
                TextDirty?.Invoke();
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
                MaterializeUntilStable();
                if (!Projector.RefreshAround(node))
                {
                    Projector.Reset(Document.Nodes.ToList());
                }
                RefreshDiagnostics(node);
                EnsureProjectionCurrent();
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
                written = MaterializeUntilStable();
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
                if (!Projector.RefreshAround(node)) Projector.Reset(Document.Nodes.ToList());
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

        public int MaterializeUntilStable(bool includeDisplayedDefaults = false, int maxPasses = 3)
        {
            var total = 0;
            for (var pass = 0; pass < maxPasses; pass++)
            {
                // includeDisplayedDefaults 只对第一轮有效：之后是"重建新长出来的字段"，不是载入补默认值
                var written = MaterializeOnce(includeDisplayedDefaults && pass == 0);
                if (written == 0) return total;
                total += written;
            }
            McdocWriteTrace.Log("物化未收敛", null, "已写 " + total + " 个键，到达 " + maxPasses + " 轮上限");
            return total;
        }

        /// <summary>
        /// 物化写值之后只重解"被写了值的那一层"（默认）。设 <c>MCDOC_TARGETED_MATERIALIZE=0</c>
        /// 退回"每轮整篇 Load"的旧行为 —— 定向重建是性能改动，这是它的一键逃生阀。
        /// </summary>
        public static bool TargetedMaterialize { get; set; } = !string.Equals(
            Environment.GetEnvironmentVariable("MCDOC_TARGETED_MATERIALIZE"), "0", StringComparison.Ordinal);

        public int MaterializeOnce(bool includeDisplayedDefaults)
        {
            var wasLoading = GetLoading?.Invoke() ?? false;
            SetLoading?.Invoke(true);
            try
            {
                int materialized;
                var writtenNodes = new List<McdocMaterializeWrite>();
                using (McdocWriteTrace.Stage("mat.walk"))
                {
                    materialized = McdocCollectionEditor.Default.MaterializeRequiredSelections(
                        Document.Nodes, includeDisplayedDefaults, Document.CreateRuntime(), writtenNodes);
                }
                if (materialized == 0) return 0;
                if (!TargetedMaterialize)
                {
                    using (McdocWriteTrace.Stage("mat.load")) Document.Load(RootType?.Invoke(), Document.RootJson);
                    return materialized;
                }
                RebuildWritten(writtenNodes);
                return materialized;
            }
            finally
            {
                SetLoading?.Invoke(wasLoading);
            }
        }

        /// <summary>
        /// 物化写完之后：只重解**被写了值的那一层**，而不是整篇 <see cref="McdocTreeDocument.Load"/>。
        /// 写值只是改文档，重建才会让派发器/动态索引按新键析出新的必填字段；
        /// 不动点由 MaterializeUntilStable 的多轮保证。先浅后深（祖先重建会连带子孙），
        /// 任何一个重解不出来就回落整篇 Load —— 语义与"每轮全量重解"等价，只是范围更小。
        /// </summary>
        private void RebuildWritten(List<McdocMaterializeWrite> writtenNodes)
        {
            if (writtenNodes.Count == 0 || Document is null) return;
            var ordered = writtenNodes
                .Where(write => write?.Node is not null && Document.IsAttached(write.Node))
                .OrderBy(write => (write.Node.JsonPath ?? "").Length)
                .ToList();
            var rebuiltPaths = new List<string>();
            foreach (var write in ordered)
            {
                // 只重解**被写了值的那一层**：上提到父容器看到起来更"安全"，
                // 但实测会把"根列表唯一元素删除"这类删除手势吃掉（父容器的 Json 还是删之前那一代），
                // 兄弟分支的依赖由 RebuildDependents 那条既有出口负责，不在这里兜。
                var path = write.Node.JsonPath ?? "";
                if (rebuiltPaths.Any(ancestor => IsBelow(path, ancestor))) continue;
                if (Document.RebuildNode(write.Node) is null)
                {
                    using (McdocWriteTrace.Stage("mat.load")) Document.Load(RootType?.Invoke(), Document.RootJson);
                    return;
                }
                rebuiltPaths.Add(path);
            }
            // 空壳是这一笔补出来的：新节点要按"建节点时的默认展开口径"撑开（depth < 默认深度 + 有值），
            // 否则定向重建会把整篇 Load 那条路默认会展开的新容器建出来却是折叠的（行上少一整片子行）。
            foreach (var write in ordered.Where(write => write.WasEmpty))
            {
                var node = Document.FindNode(write.Node.JsonPath ?? "");
                if (node is null || node.Json is null || node.IsExpanded) continue;
                if (node.Depth < McdocNodeFactory.DefaultExpandDepth && McdocPresenterRegistry.Default.CanHaveChildren(node))
                {
                    node.IsExpanded = true;
                }
            }
        }

        private static bool IsBelow(string path, string ancestor)
            => !string.IsNullOrEmpty(ancestor) && path.Length > ancestor.Length
               && path.StartsWith(ancestor, StringComparison.Ordinal)
               && (path[ancestor.Length] == '.' || path[ancestor.Length] == '[');

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
                    Projector.Reset(Document.Nodes.ToList());
                }
                if (MaterializeUntilStable() > 0)
                {
                    Projector.Reset(Document.Nodes.ToList());
                }
                EnsureProjectionCurrent();
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
        /// 改完一个值之后重算诊断，并把**行上的快照**刷掉。
        /// 行的徽标读的是 Head（构造/RefreshHead 时算的），只调 Document.ValidateNode 的话
        /// 文档级清单变了、行上那个黄色/红色感叹号却还在（现场就是这么卡住的）。
        /// </summary>
        public void RefreshDiagnostics(McdocTreeNode node)
        {
            if (node is null || Document is null) return;
            using var stage = McdocWriteTrace.Stage("diag.refresh");
            // 自己这一行一定刷；祖先行只在"文字真的变了"时继续往上（必填缺键那类记在父行上）
            var current = node;
            for (var depth = 0; current is not null && depth < 64; depth++)
            {
                var row = Projector?.Find(current);
                var before = row?.DiagnosticSummary;
                Document.ValidateNode(current);
                if (row is null)
                {
                    current = current.Parent;
                    continue;
                }
                row.RefreshHead();
                if (depth >= 1 && string.Equals(before, row.DiagnosticSummary, StringComparison.Ordinal)) break;
                current = current.Parent;
            }
            // 子树里的行也要重读：ValidateNode 现在连子树一起算，子行不刷就还挂着旧提醒
            var stack = new System.Collections.Generic.Stack<McdocTreeNode>();
            stack.Push(node);
            var guard = 0;
            while (stack.Count > 0 && guard++ < 20000)
            {
                var child = stack.Pop();
                foreach (var next in McdocPresenterRegistry.Default.Children(child)) stack.Push(next);
                if (!ReferenceEquals(child, node)) Projector?.Find(child)?.RefreshHead();
            }
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

        private void OnValueChangedCore(McdocTreeNode node)
        {
            RefreshDiagnostics(node);
            var rebuilt = RebuildDependents([node]);
            foreach (var dependent in rebuilt)
            {
                // 重建出来的新成员在懒建下是"只有壳"的：不先把子级建出来，投影画不出行、
                // 物化也遍历不到 —— 现场就是"切成员后 block 有行没值、properties 没有方块上下文"。
                // 这里已经在 Rebuilding 闸门里，补建抬起的钩子不会再排第二次重建（不自我驱动）。
                dependent.EnsureChildren();
                if (!Projector.RefreshAround(dependent)) Projector.Reset(Document.Nodes.ToList());
                RefreshDiagnostics(dependent);
            }
            // 重建会把"必填 + 闭合候选"的判别键写进文档（工厂的判别键物化），写过就得重排一次
            if (rebuilt.Count > 0 && MaterializeUntilStable() > 0)
            {
                Projector.Reset(Document.Nodes.ToList());
            }

            // 模型可能被重建写过：文本必须跟上，否则"视图同代"当场破（这条出口原先漏了）
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
        {
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
                    && (parent.DependsOnRuntimeData
                        || HasSpreadIndex(EffectiveStruct(parent) ?? parent.DeclaredType, node.Key)))
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
                DropEmptyDerivedContainers(freshParent);
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
        private void ReshapeDependentValue(string path, McdocType oldType)
        {
            if (Document is null || string.IsNullOrEmpty(path)) return;
            var fresh = Document.FindNode(path);
            if (fresh?.Json is null) return;
            // 判据要用**声明重新求值**出来的类型：节点上的 Type 在"值与类型不符"时已经降级成 any，
            // 拿它判 ValueFits 恒真，整形永远不会发生（现场：argument 明明该是 FloatWithAlpha）。
            var target = McdocDefaults.ResolveType(fresh.DeclaredType, Document.CreateRuntime(), fresh.ValueNode);
            if (target is null || target.Kind is McdocTypeKind.Any or McdocTypeKind.Unsafe) return;
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
                    && McdocHelpers.ValueFits(fresh.Json, field.Type));
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
        /// 声明类型里是否有「按这个键做动态索引」的 dispatcher/indexed（探针也用它断言反向）。
        /// 声明常被指令包着（Mapped{copy} / Concrete / Indexed 空壳——它们只换上下文、不带自己的索引，
        /// 见 McdocSimplifier.SimplifyIndexed 的"外壳"规则），而索引挂在里层的 Dispatcher 上：
        /// 只看顶层 Kind 会漏掉整条链，表现就是"兄弟键落值了、依赖它的子树不重新求值"。
        /// </summary>
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
            // 判别键的宿主可能被 union 摊平（字段行直接挂在 union 行下）：这一层判不准，保守重建
            if (parent.Type?.Kind == McdocTypeKind.Union) return true;
            if (HasSpreadIndex(EffectiveStruct(parent) ?? parent.DeclaredType, node.Key)) return true;
            foreach (var sibling in McdocPresenterRegistry.Default.Children(parent))
            {
                if (sibling is null || ReferenceEquals(sibling, node)) continue;
                if (IndexesByKey(sibling.DeclaredType, node.Key)) return true;
            }
            return false;
        }

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
                MaterializeUntilStable();
                Projector.Reset(Document.Nodes.ToList());
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
