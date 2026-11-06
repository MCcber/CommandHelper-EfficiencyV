using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 可见节点 → 行的投影器：折叠子树一行都不产生；
    /// 展开 / 结构变更只在那一行的位置拼接一次，其余行对象原样保留（容器不回收、滚动不复位）。
    /// </summary>
    public sealed class McdocRowProjector
    {
        private readonly Dictionary<McdocTreeNode, McdocRow> byNode = new();

        /// <summary>列表一页展示多少项（int.MaxValue 表示不分页，sweep 用）。</summary>
        public static int PageSize { get; set; } = 50;

        public McdocRowCollection Rows { get; } = new();

        /// <summary>某节点刚把子级建出来（懒建首次展开）：宿主在这里补物化，再自行安排重建。</summary>
        public Action<McdocTreeNode> AfterChildrenBuilt { get; set; }
        public int SpliceCount { get; private set; }
        public long LastSpliceMs { get; private set; }

        /// <summary>整表重投影次数（诊断用：值编辑不该把它推高 —— 一推高就是"改一个值整树重来"）。</summary>
        public int ResetCount { get; private set; }

        public void Reset(IEnumerable<McdocTreeNode> roots)
        {
            McdocWriteTrace.Trace("proj-reset", "rows=" + Rows.Count + " reset=" + ResetCount + " splice=" + SpliceCount);
            ResetCount++;
            McdocWriteTrace.LogText("整表重投影", "reset#" + ResetCount, withCaller: true);
            byNode.Clear();
            var rows = new List<McdocRow>();
            foreach (var root in roots) Collect(root, 0, false, 0, rows);
            Rows.SetAll(rows);
        }

        public McdocRow Find(McdocTreeNode node)
            => node is not null && byNode.TryGetValue(node, out var row) ? row : null;

        /// <summary>整棵子树标成展开（基准测试与"全展开"用，一次投影）。</summary>
        public static void ExpandAll(IEnumerable<McdocTreeNode> roots)
        {
            void Walk(McdocTreeNode node)
            {
                if (node is null) return;
                node.IsExpanded = true;
                foreach (var child in McdocPresenterRegistry.Default.Children(node)) Walk(child);
            }
            foreach (var root in roots) Walk(root);
        }

        private void Collect(McdocTreeNode node, int depth, bool listChild, int index, List<McdocRow> into)
        {
            var row = new McdocRow(node, depth, this, index, listChild);
            byNode[node] = row;
            into.Add(row);
            // 缺省字段（可选 + 文档里没有）只留一行 ⊕：即使折叠状态是脏的，也不许把子级拼进列表
            if (!node.IsExpanded || (!node.IsRequired && node.Json is null)) return;
            node.EnsureChildren();
            CollectChildren(node, depth, into);
        }

        /// <summary>
        /// 一行展开后的子行：分页截断 + More 行 + 列表尾部 add-tail。
        /// 初次投影和后续展开拼接必须走同一条路，否则"展开后才出现/消失 More 行"。
        /// </summary>
        private void CollectChildren(McdocTreeNode node, int depth, List<McdocRow> into)
        {
            // 展开走的是"只拼这一行子树"的捷径（AppendChildren → 这里），不经过 Collect 的 EnsureChildren；
            // 懒建节点（深度 >= DefaultExpandDepth）不先建子级的话，点箭头只会得到 0 行 —— 看起来就是"展开没反应"。
            // 只在这一拍真的把子级建出来时才通知：每次投影都无条件喊一遍的话，
            // 宿主那边"补物化 + 排重建"就会跟着每一轮 Reset 重新触发 —— 现场就是
            // proj-splice / proj-reset 各 100 次、structure 只有 3 次的自我驱动循环。
            var justBuilt = !node.ChildrenBuilt;
            node.EnsureChildren();
            if (justBuilt) AfterChildrenBuilt?.Invoke(node);
            var children = McdocPresenterRegistry.Default.Children(node);
            var childIsList = IsListLike(node);
            // ItemsShown 是"这一行显示多少条"的用户意图（点"再显示"会 +PageSize，点"全部"设 MaxValue）；
            // PageSize == int.MaxValue 表示完全不分页（sweep 用它保持行数==节点数的不变量）。
            // 注意不要再和 PageSize 取 min：那样"再显示"永远加不出来。
            var shown = !childIsList || PageSize == int.MaxValue
                ? children.Count
                : Math.Min(children.Count, Math.Max(1, node.ItemsShown));
            for (var i = 0; i < shown; i++) Collect(children[i], depth + 1, childIsList, i, into);
            if (childIsList && children.Count > shown)
            {
                into.Add(new McdocRow(node, depth + 1, this, shown, true, McdocRowKind.More));
            }
            // misode ListBody：列表项之后还有一条"追加"行（add_bottom）
            else if (childIsList && children.Count > 0 && node.Head.CanAddBottom)
            {
                into.Add(new McdocRow(node, depth + 1, this, children.Count, true, McdocRowKind.AddTail));
            }
        }

        private static bool IsListLike(McdocTreeNode node) => McdocPresenterRegistry.Default.IsListLike(node);

        private int DescendantEnd(int index)
        {
            var depth = Rows[index].Depth;
            var end = index + 1;
            while (end < Rows.Count && Rows[end].Depth > depth) end++;
            return end;
        }

        private void DropRange(int index, int count)
        {
            for (var i = 0; i < count; i++)
            {
                byNode.Remove(Rows[index + i].Node);
                Rows[index + i].Detach();
            }
            Rows.RemoveRange(index, count);
        }

        /// <summary>
        /// 原位重建一行（含子树）：union 切分支后行上的 Head 变了，
        /// 必须换一个行实例，ContentControl 才会重新选编辑器模板。
        /// </summary>
        public void ReplaceRow(McdocRow row)
        {
            McdocWriteTrace.Trace("proj-replace", "path=" + (row.Node?.JsonPath ?? "?")
                + " kind=" + row.Kind + " rows=" + Rows.Count + " splice=" + SpliceCount);
            var index = Rows.IndexOf(row);
            if (index < 0)
            {
                row.RefreshHead();
                return;
            }
            var end = DescendantEnd(index);
            DropRange(index, end - index);
            var fresh = new List<McdocRow>();
            Collect(row.Node, row.Depth, row.IsListChild, row.Index, fresh);
            Rows.InsertRange(index, fresh);
            // Collect 期间会 EnsureChildren → union 的 PopulateChildren 可能再改一次镜像状态（ScalarEditorKind），
            // 而 McdocRow 的 Head 是"构造时求值 + 缓存"的：这里必须重算一次，否则切分支后行上还是旧 Head
            // （表现：切回 value 支，编辑器不回来）。
            if (fresh.Count > 0) fresh[0].RefreshHead();
        }

        private void AppendChildren(McdocRow row, int at)
        {
            var rows = new List<McdocRow>();
            CollectChildren(row.Node, row.Depth, rows);
            Rows.InsertRange(at, rows);
        }

        /// <summary>展开/折叠：只动这一行的子树。</summary>
        public void SpliceExpansion(McdocRow row)
        {
            var index = Rows.IndexOf(row);
            if (index < 0) return;
            var started = Environment.TickCount64;
            var end = DescendantEnd(index);
            if (end > index + 1) DropRange(index + 1, end - index - 1);
            if (row.Node.IsExpanded) AppendChildren(row, index + 1);
            row.NotifyLayout();
            SpliceCount++;
            LastSpliceMs = Environment.TickCount64 - started;
        }

        /// <summary>结构变更（增删条目 / union 换分支）：行对象保留，子行重拼。</summary>
        /// <summary>
        /// 节点被原位替换后刷新投影：父行还在就只重拼该父行的子行（保留其它行对象）；
        /// 返回 false 表示父行不存在（根级替换），调用方应 Reset 整表。
        /// </summary>
        public bool RefreshAround(McdocTreeNode node)
        {
            var parent = node?.Parent;
            if (parent is null) return false;
            var row = Find(parent);
            if (row is null) return false;
            SpliceSubtree(row);
            return true;
        }

        public void SpliceSubtree(McdocRow row, bool ensureExpanded = false)
        {
            if (row is null) return;
            McdocWriteTrace.Trace("proj-splice", "path=" + (row.Node?.JsonPath ?? "?")
                + " kind=" + row.Kind + " rows=" + Rows.Count + " splice=" + SpliceCount);
            var index = Rows.IndexOf(row);
            if (index < 0) return;
            var started = Environment.TickCount64;
            // 懒建时 Children 还没建，数它是 0：必须问 presenter 有没有子级
            if (ensureExpanded && !row.Node.IsExpanded && McdocPresenterRegistry.Default.CanHaveChildren(row.Node))
            {
                row.Node.IsExpanded = true;
                row.NotifyLayout();
            }
            var end = DescendantEnd(index);
            if (end > index + 1) DropRange(index + 1, end - index - 1);
            if (row.Node.IsExpanded) AppendChildren(row, index + 1);
            row.NotifyLayout();
            SpliceCount++;
            LastSpliceMs = Environment.TickCount64 - started;
        }

        /// <summary>
        /// 顶层区的局部重拼（根层成员增删 / 顶层节点被重建）：根没有行可挂（合成根不在 Rows 里），
        /// 所以只能按"第几个顶层行"定位。传入上一代与这一代的顶层节点列表，只重拼中间变化的那一段。
        /// </summary>
        public void SpliceTopLevels(IReadOnlyList<McdocTreeNode> previous, IReadOnlyList<McdocTreeNode> current)
        {
            previous ??= [];
            current ??= [];
            var start = 0;
            while (start < previous.Count && start < current.Count
                && ReferenceEquals(previous[start], current[start]))
            {
                start++;
            }
            var suffix = 0;
            while (suffix < previous.Count - start && suffix < current.Count - start
                && ReferenceEquals(previous[previous.Count - 1 - suffix], current[current.Count - 1 - suffix]))
            {
                suffix++;
            }
            var removed = previous.Count - start - suffix;
            var inserted = new List<McdocTreeNode>();
            for (var index = start; index < current.Count - suffix; index++) inserted.Add(current[index]);
            if (removed == 0 && inserted.Count == 0) return;

            var started = Environment.TickCount64;
            var rowStart = TopRow(start);
            var rowEnd = removed == 0 ? rowStart : TopRow(start + removed);
            if (rowEnd > rowStart) DropRange(rowStart, rowEnd - rowStart);
            var rows = new List<McdocRow>();
            foreach (var node in inserted) Collect(node, 0, false, 0, rows);
            Rows.InsertRange(rowStart, rows);
            SpliceCount++;
            LastSpliceMs = Environment.TickCount64 - started;
        }

        /// <summary>第 <paramref name="index"/> 个顶层行（Depth == 0）的下标；越界返回行数。</summary>
        private int TopRow(int index)
        {
            var seen = 0;
            for (var position = 0; position < Rows.Count; position++)
            {
                if (Rows[position].Depth != 0) continue;
                if (seen == index) return position;
                seen++;
            }
            return Rows.Count;
        }
    }
}
