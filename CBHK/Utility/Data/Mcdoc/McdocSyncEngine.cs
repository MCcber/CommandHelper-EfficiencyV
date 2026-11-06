// 文本 → 树 的增量同步：结构 diff + 标量快路径 + 子树重建。
// 约定：调用方已经用 System.Text.Json 校验过语法（McdocJsonParser.TryParse），并且 newRoot 与编辑器共享同一份 AST。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CBHK.Utility.Data.Mcdoc
{
    public sealed class McdocSyncStats
    {
        public int ValuePatches;
        public int StructureAnchors;
        public int RebuiltNodes;
        /// <summary>补丁太多/路径对不上：调用方应回退整树重建。</summary>
        public bool Fallback;
        public long ElapsedMs;
        /// <summary>被就地改值的节点（行需要 RefreshHead）。</summary>
        public List<McdocTreeNode> PatchedNodes = [];
        /// <summary>被重建的节点（行需要 RefreshAround）。</summary>
        public List<McdocTreeNode> RebuiltNodeList = [];
        /// <summary>走了根层成员差分时的"上一代顶层列表"：投影按它分段重拼，不做整表 Reset。</summary>
        public List<McdocTreeNode> PreviousTopLevel;
        /// <summary>根层成员增删的数量（诊断用）。</summary>
        public int MemberOps;
    }

    /// <summary>
    /// 文本编辑 → 树的差量应用。只处理"形状"层面的差异：
    /// 容器键集合/数组长度相同 → 逐叶子比较，叶子值变化走快路径（不换节点）；
    /// 容器形状变化 → 记最深的容器作为重建锚点，只重建那棵子树。
    /// </summary>
    public static class McdocSyncEngine
    {
        private const int MaxAnchors = 20;
        private const double MaxAnchorRatio = 0.3;

        public static bool TryApply(McdocTreeDocument document, McdocJsonValue newRoot, out McdocSyncStats stats)
        {
            stats = new McdocSyncStats();
            if (document is null || newRoot is null) return false;

            // 顶层行没有可挂的父行（摊平后合成根不在 Rows 里）：任何一次同步都要记下"上一代顶层列表"，
            // 否则顶层字段被重建时 RefreshAround 必然失败 → 整表重投影（判据：结构变化也要 0 次）
            stats.PreviousTopLevel = document.Nodes.ToList();

            var started = Stopwatch.GetTimestamp();
            var valuePatches = new List<(string Path, McdocJsonValue Value)>();
            var anchors = new List<string>();
            var addedMembers = new List<string>();
            var removedMembers = new List<string>();
            Diff("$", document.RootJson, newRoot, true);

            // 锚点去重：祖先锚点覆盖后代锚点
            var deepest = anchors
                .Where(path => !anchors.Any(other => other.Length < path.Length && path.StartsWith(other, StringComparison.Ordinal)
                    && (path.Length == other.Length || path[other.Length] == '.' || path[other.Length] == '[')))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            stats.StructureAnchors = deepest.Count;

            var nodeCount = Math.Max(1, CountNodes(document.Nodes));
            if (deepest.Count > MaxAnchors || deepest.Count > nodeCount * MaxAnchorRatio)
            {
                stats.Fallback = true;
                stats.ElapsedMs = ElapsedMs(started);
                return false;
            }

            // 根层成员增删（README §八 第 1 步）：变化落在文档根上时只动受影响的顶层行。
            // 覆盖出来的键进 coverage：重指（RelinkToRoot）要认它们是被处理过的，不能算"作废子树"。
            var coverage = new List<string>(deepest);
            if ((addedMembers.Count > 0 || removedMembers.Count > 0)
                && !TryApplyRootMembers(document, newRoot, addedMembers, removedMembers, coverage, stats))
            {
                stats.Fallback = true;
                stats.ElapsedMs = ElapsedMs(started);
                return false;
            }

            foreach (var patch in valuePatches)
            {
                var node = document.FindNode(patch.Path);
                if (node is null || node.Type?.IsDirective == true)
                {
                    stats.Fallback = true;
                    stats.ElapsedMs = ElapsedMs(started);
                    return false;
                }
                McdocWriteTrace.LogText("TryApply补丁 " + patch.Path + " 容器键", Keys(node.Parent?.Json));
                node.Json = patch.Value;
                node.SyncScalarFromJson();
                // 值补丁不重建：诊断按节点重算，否则改回合法值后红色感叹号仍挂着
                document.ValidateNode(node);
                stats.ValuePatches++;
                stats.PatchedNodes.Add(node);
            }

            // 先整体指向新 AST，再重建锚点子树：反过来的话 RebuildNode 用的是**旧** node.Json，
            // 重建出来的子树里没有新键（现象：JSON 里多了 criteria.test，树上只有占位行）。
            // 只要某个节点的 JSON 真的换了（键集合可能也变了），子级已作废 —— 局部拼接保证不了行与文档一一对应，
            // 交给调用方整树重建（Fallback），别留下引用旧对象的残骸行。
            if (RelinkToRoot(document, newRoot, coverage))
            {
                // 作废的子树不在任何锚点里：局部拼接保证不了行与文档一一对应，交给整树重建
                document.AdoptRoot(newRoot);
                stats.Fallback = true;
                stats.ElapsedMs = ElapsedMs(started);
                return false;
            }
            document.AdoptRoot(newRoot);

            foreach (var anchorPath in deepest)
            {
                var node = document.FindNode(anchorPath);
                if (node is null)
                {
                    stats.Fallback = true;
                    stats.ElapsedMs = ElapsedMs(started);
                    return false;
                }
                // union 节点：重建选中分支的容器子级，避免 PopulateChildren 按 JSON 重新挑分支
                var target = node.Type?.Kind == McdocTypeKind.Union
                    ? node.SelectedUnionChildren.FirstOrDefault() ?? node
                    : node;
                var rebuilt = document.RebuildNode(target);
                if (rebuilt is null)
                {
                    stats.Fallback = true;
                    stats.ElapsedMs = ElapsedMs(started);
                    return false;
                }
                stats.RebuiltNodes++;
                stats.RebuiltNodeList.Add(rebuilt);
            }

            stats.ElapsedMs = ElapsedMs(started);
            return true;

            void Diff(string path, McdocJsonValue oldValue, McdocJsonValue newValue, bool isDocumentRoot = false)
            {
                if (oldValue is null || newValue is null)
                {
                    if (!ReferenceEquals(oldValue, newValue)) anchors.Add(path);
                    return;
                }
                if (oldValue.Kind != newValue.Kind)
                {
                    anchors.Add(path);
                    return;
                }
                switch (oldValue.Kind)
                {
                    case McdocJsonKind.Object:
                    {
                        var oldPairs = oldValue.Properties ?? [];
                        var newPairs = newValue.Properties ?? [];
                        if (!SameKeySequence(oldPairs, newPairs))
                        {
                            // 根对象：键集合的增删交给"根层成员差分"（只建/清/摘那几个顶层行），
                            // 公共键继续往下差分（嵌套值变化 / 嵌套锚点照旧产出）；其余容器仍然整体当锚点。
                            if (!isDocumentRoot || !TryPlanRootMembers(oldPairs, newPairs))
                            {
                                anchors.Add(path);
                                return;
                            }
                        }
                        foreach (var pair in newPairs)
                        {
                            // 根层新增成员：旧值不存在，节点是新造好的；再走一次差分只会把它当锚点重建一遍，
                            // 而重建拿不到建树期的语义（UnknownField / 动态键身份 / 必选性 / 「缺少必填键」诊断），
                            // 表现是"未声明键的行变成 Plain 行、占位子行却还在"
                            if (isDocumentRoot && addedMembers.Contains(pair.Key)) continue;
                            var oldChild = oldValue.Get(pair.Key);
                            Diff(path + "." + pair.Key, oldChild, pair.Value);
                        }
                        break;
                    }
                    case McdocJsonKind.Array:
                    {
                        var oldItems = oldValue.Items ?? [];
                        var newItems = newValue.Items ?? [];
                        if (oldItems.Count != newItems.Count)
                        {
                            anchors.Add(path);
                            return;
                        }
                        for (var i = 0; i < oldItems.Count; i++)
                        {
                            Diff(path + "[" + i + "]", oldItems[i], newItems[i]);
                        }
                        break;
                    }
                    default:
                        if (!ScalarEquals(oldValue, newValue)) valuePatches.Add((path, newValue));
                        break;
                }
            }

            /// <summary>
            /// 根对象的键集合增删：记下加/删的键，公共键的相对顺序必须不变（顺序变化不做局部同步，
            /// 交回整树重建）。返回 false = 这次变化不归根层成员差分管。
            /// </summary>
            bool TryPlanRootMembers(List<KeyValuePair<string, McdocJsonValue>> oldPairs,
                List<KeyValuePair<string, McdocJsonValue>> newPairs)
            {
                var rank = new Dictionary<string, int>(StringComparer.Ordinal);
                for (var index = 0; index < newPairs.Count; index++) rank[newPairs[index].Key] = index;
                var pendingAdded = new List<string>();
                var pendingRemoved = new List<string>();
                var last = -1;
                foreach (var pair in oldPairs)
                {
                    if (!rank.TryGetValue(pair.Key, out var position))
                    {
                        pendingRemoved.Add(pair.Key);
                        continue;
                    }
                    if (position < last) return false;   // 公共键被重排
                    last = position;
                }
                var oldKeys = new HashSet<string>(oldPairs.Select(pair => pair.Key), StringComparer.Ordinal);
                foreach (var pair in newPairs)
                {
                    if (!oldKeys.Contains(pair.Key)) pendingAdded.Add(pair.Key);
                }
                if (pendingAdded.Count + pendingRemoved.Count == 0) return false;   // 只是键序不同
                addedMembers.AddRange(pendingAdded);
                removedMembers.AddRange(pendingRemoved);
                return true;
            }
        }

        /// <summary>对象键序是否完全一致（长度 + 逐项键名）。</summary>
        private static bool SameKeySequence(List<KeyValuePair<string, McdocJsonValue>> left,
            List<KeyValuePair<string, McdocJsonValue>> right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null || left.Count != right.Count) return false;
            for (var index = 0; index < left.Count; index++)
            {
                if (!string.Equals(left[index].Key, right[index].Key, StringComparison.Ordinal)) return false;
            }
            return true;
        }

        /// <summary>
        /// 根层成员增删（README §八 第 1 步）：变化落在文档根上时，只建 / 清 / 摘受影响的顶层行，
        /// 不整树重建、不整表重投影。返回 false = 这个形状不归这里管（调用方整树重建）。
        /// </summary>
        private static bool TryApplyRootMembers(McdocTreeDocument document, McdocJsonValue newRoot,
            List<string> added, List<string> removed, List<string> coverage, McdocSyncStats stats)
        {
            var rootType = document.RootNode?.Type;
            if (!document.FlattenedRoot || rootType?.Kind != McdocTypeKind.Struct) return false;
            if (added.Count + removed.Count > MaxAnchors) return false;

            var members = document.TopLevelMembers();
            var keyOrder = (newRoot.Properties ?? []).Select(pair => pair.Key).ToList();

            foreach (var key in removed)
            {
                coverage.Add("$." + key);
                if (!members.TryGetValue(key, out var node) || node is null) continue;
                if (StructNodePresenter.IsStaticField(rootType, key))
                {
                    // 声明过的键：行留成一行 ⊕（与整树重建一致），子级整片作废
                    document.ClearRootMember(node);
                    continue;
                }
                if (!document.RemoveRootMember(node)) return false;
            }

            foreach (var key in added)
            {
                coverage.Add("$." + key);
                if (members.TryGetValue(key, out var node) && node is not null)
                {
                    // 声明过的静态字段早就有一行 ⊕：补上值，再按新值重建这一棵子树
                    node.Json = newRoot.Get(key);
                    node.ChildrenBuilt = false;
                    var rebuilt = document.RebuildNode(node);
                    if (rebuilt is null) return false;
                    stats.RebuiltNodes++;
                    stats.RebuiltNodeList.Add(rebuilt);
                    continue;
                }
                var created = document.CreateRootMember(key, newRoot);
                if (created is null || !document.InsertRootMember(created, keyOrder)) return false;
            }

            stats.MemberOps += added.Count + removed.Count;
            McdocWriteTrace.Log("根层成员差分", null,
                "加=[" + string.Join("/", added) + "] 删=[" + string.Join("/", removed)
                + "] 顶层=" + document.Nodes.Count);
            return true;
        }

        /// <summary>按 JsonPath 把每个节点的 Json 重新指向新 AST 里的同一个值。</summary>
        private static bool RelinkToRoot(McdocTreeDocument document, McdocJsonValue root, List<string> anchors)
        {
            var uncovered = 0;
            McdocWriteTrace.LogText("RelinkToRoot newRoot键", Keys(root));
            McdocWriteTrace.LogText("RelinkToRoot 旧RootJson键", Keys(document?.RootJson));
            void Walk(McdocTreeNode node)
            {
                if (node is null) return;
                // 只重指"自己有值"的节点：动态键占位行/合成根没有自己的 JSON，
                // 给它们塞父对象会让占位行看着像"文档里有值"，也把值补丁快路径拖成整树重建
                var value = node.Json is null ? null : ResolvePath(root, node.JsonPath);
                if (value is not null)
                {
                    // JSON 换了：键集合可能也变了（新加的条目/字段），旧子级一律作废，
                    // 否则 ChildrenBuilt 会把它冻在上一代 —— 表现是"文档里有键、树上没有行"
                    if (!ReferenceEquals(node.Json, value))
                    {
                        // 只有"形状"变了（对象键集合 / 数组长度 / 种类）才作废子级；
                        // 纯标量改值仍走值补丁快路径，否则每一处编辑都会退化成整树重建
                        if (!SameShape(node.Json, value))
                        {
                            node.ChildrenBuilt = false;
                            var path = node.JsonPath ?? "";
                            var covered = anchors.Any(anchor => path.StartsWith(anchor, StringComparison.Ordinal));
                            if (!covered) uncovered++;
                        }
                    }
                    node.Json = value;
                    // 求值现场（ValueNode）也要指到这一代 AST：只改 node.Json 的话，之后按**同层兄弟键**
                    // 求值时读到的还是旧对象（README §2.7「AST 同源」）。ValueNode 的父子是同一批实例，
                    // 逐个就地改 OriginalNode，整条链就一起跟过来了（RuntimeKey 是键节点，值不变不动它）。
                    if (node.ValueNode?.Node is not null) node.ValueNode.Node.OriginalNode = value;
                }
                foreach (var child in node.Children) Walk(child);
                foreach (var child in node.Items) Walk(child);
                foreach (var child in node.SelectedUnionChildren) Walk(child);
            }
            foreach (var node in document.Nodes) Walk(node);
            return uncovered > 0;
        }

        /// <summary>对象键集合（trace 用）：直接看"哪次重载把根形状换掉了"。</summary>
        private static string Keys(McdocJsonValue value)
        {
            if (value is not { Kind: McdocJsonKind.Object } || value.Properties is null) return "<非对象>";
            return "{" + string.Join(",", value.Properties.Select(pair => pair.Key)) + "}";
        }

        private static McdocJsonValue ResolvePath(McdocJsonValue root, string jsonPath)
            => McdocTreeDocument.ResolvePath(root, jsonPath);

        private static int CountNodes(IEnumerable<McdocTreeNode> roots)
        {
            var count = 0;
            void Walk(McdocTreeNode node)
            {
                if (node is null) return;
                count++;
                foreach (var child in node.Children) Walk(child);
                foreach (var child in node.Items) Walk(child);
                foreach (var child in node.SelectedUnionChildren) Walk(child);
            }
            foreach (var root in roots) Walk(root);
            return count;
        }

        /// <summary>只比"形状"：种类一致、对象键序一致、数组长度一致。值不算。</summary>
        private static bool SameShape(McdocJsonValue left, McdocJsonValue right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            if (left.Kind != right.Kind) return false;
            if (left.Kind == McdocJsonKind.Object)
            {
                var mine = left.Properties ?? [];
                var theirs = right.Properties ?? [];
                if (mine.Count != theirs.Count) return false;
                for (var index = 0; index < mine.Count; index++)
                {
                    if (!string.Equals(mine[index].Key, theirs[index].Key, StringComparison.Ordinal)) return false;
                }
            }
            else if (left.Kind == McdocJsonKind.Array)
            {
                if ((left.Items?.Count ?? 0) != (right.Items?.Count ?? 0)) return false;
            }
            return true;
        }

        private static bool ScalarEquals(McdocJsonValue left, McdocJsonValue right) => left.Kind switch
        {
            McdocJsonKind.String => string.Equals(left.StringValue, right.StringValue, StringComparison.Ordinal),
            McdocJsonKind.Number => left.NumberValue == right.NumberValue,
            McdocJsonKind.Boolean => left.BooleanValue == right.BooleanValue,
            _ => true,
        };

        private static long ElapsedMs(long started)
            => (Stopwatch.GetTimestamp() - started) * 1000 / Math.Max(1, Stopwatch.Frequency);
    }
}
