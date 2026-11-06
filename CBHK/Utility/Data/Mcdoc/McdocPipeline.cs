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
        /// 结构变更（增删条目 / 切 union 分支 / 展开补建）之后：刷新这一层 → 物化到不动点 → 只重拼父行。
        /// 运行期依赖（动态索引/调度器解出新字段）交给调用方延后一拍重建。
        /// </summary>
        public void OnStructureChanged(McdocTreeNode node)
        {
            if (node is null) return;
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
                TextDirty?.Invoke();
            }
            finally
            {
                SetLoading?.Invoke(wasLoading);
                Rebuilding = wasRebuilding;
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

        public int MaterializeOnce(bool includeDisplayedDefaults)
        {
            var wasLoading = GetLoading?.Invoke() ?? false;
            SetLoading?.Invoke(true);
            try
            {
                var materialized = McdocCollectionEditor.Default.MaterializeRequiredSelections(Document.Nodes, includeDisplayedDefaults);
                if (materialized == 0) return 0;
                Document.Load(RootType?.Invoke(), Document.RootJson);
                return materialized;
            }
            finally
            {
                SetLoading?.Invoke(wasLoading);
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
                    Projector.Reset(Document.Nodes.ToList());
                }
                if (MaterializeUntilStable() > 0)
                {
                    Projector.Reset(Document.Nodes.ToList());
                }
                TextDirty?.Invoke();
            }
            finally
            {
                SetLoading?.Invoke(wasLoading);
                Rebuilding = wasRebuilding;
            }
        }

        /// <summary>
        /// 改完一个值之后重算诊断，并把**行上的快照**刷掉。
        /// 行的徽标读的是 Head（构造/RefreshHead 时算的），只调 Document.ValidateNode 的话
        /// 文档级清单变了、行上那个黄色/红色感叹号却还在（现场就是这么卡住的）。
        /// </summary>
        public void RefreshDiagnostics(McdocTreeNode node)
        {
            if (node is null || Document is null) return;
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
        }

        private void OnValueChangedCore(McdocTreeNode node)
        {
            RefreshDiagnostics(node);
            var rebuilt = RebuildDependents([node]);
            foreach (var dependent in rebuilt)
            {
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
            var seen = new HashSet<McdocTreeNode>();
            foreach (var node in changed)
            {
                if (node?.Parent is null || string.IsNullOrEmpty(node.Key)) continue;
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
                        PruneAbandonedMembers(sibling, fresh);
                        rebuilt.Add(fresh);
                    }
                }
            }
            if (rebuilt.Count > 0)
            {
                McdocWriteTrace.Trace("dependents", "changed=" + changed.Count + " rebuilt=" + rebuilt.Count);
            }
            return rebuilt;
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
        /// 依赖键换了（切方块）之后，旧结构声明过、新结构不再声明的键要**真的删掉**：
        /// 留着它们只会变成一行"当前版本不存在"的未声明键（用户看到的是"上一块方块的属性还挂着"）。
        /// 与切 union 分支的 DropAbandonedKeys 同一条规则：只剪"旧声明过、新不声明"的键，
        /// 用户在别处手写的内容一律不动。
        /// </summary>
        private static int PruneAbandonedMembers(McdocTreeNode oldNode, McdocTreeNode rebuilt)
        {
            if (oldNode?.Type?.Kind != McdocTypeKind.Struct || rebuilt?.Type?.Kind != McdocTypeKind.Struct) return 0;
            if (rebuilt.Json is not { Kind: McdocJsonKind.Object } container || container.Properties is null) return 0;
            var declared = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in rebuilt.Type.Fields ?? [])
            {
                if (field?.Name is { Length: > 0 } name) declared.Add(name);
            }
            var abandoned = new List<string>();
            foreach (var field in oldNode.Type.Fields ?? [])
            {
                if (field?.Name is not { Length: > 0 } name || declared.Contains(name)) continue;
                if (container.Get(name) is null) continue;
                abandoned.Add(name);
            }
            if (abandoned.Count == 0) return 0;
            foreach (var name in abandoned)
            {
                container.Properties.RemoveAll(pair => string.Equals(pair.Key, name, StringComparison.Ordinal));
                McdocWriteTrace.Trace("pruned", "path=" + (rebuilt.JsonPath ?? "?") + " key=" + name);
            }
            // 子级是按剪之前的 JSON 建的：让它在下一拍按当前文档重新建一次
            rebuilt.ChildrenBuilt = false;
            rebuilt.Children.Clear();
            return abandoned.Count;
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
