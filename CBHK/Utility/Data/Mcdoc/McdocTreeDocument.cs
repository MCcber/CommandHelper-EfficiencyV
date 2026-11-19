// 视图层适配器 + JSON 运行节点访问器。
// View 只需要绑 McdocTreeDocument.Nodes；符号索引/属性/求值器都收在这里，不散进各 ViewModel。
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>把 McdocJsonValue 当作运行节点：动态索引（%parent/兄弟键）靠它取子节点。</summary>
    public sealed class McdocJsonAccessor : IMcdocNodeAccessor
    {
        public IEnumerable<McdocChildPair> GetChildren(object node, McdocType simplified)
        {
            if (node is not McdocJsonValue value || value.Kind != McdocJsonKind.Object) yield break;
            foreach (var pair in value.Properties ?? new List<KeyValuePair<string, McdocJsonValue>>())
            {
                var child = new McdocChildPair
                {
                    Key = new McdocRuntimeNode(pair.Key, McdocType.LiteralString(pair.Key))
                };
                child.PossibleValues.Add(new McdocRuntimeNode(pair.Value, pair.Value?.InferType() ?? McdocType.Any()));
                yield return child;
            }
        }
    }

    /// <summary>遗留键 / 幽灵行删除的落点判定：直接通知活父级、需要整篇重载、或找不到落点。</summary>
    public enum LegacyDeleteOutcome
    {
        NotFound,
        Notified,
        NeedsReload,
    }

    /// <summary>
    /// 一次编辑会话的视图状态：符号索引 + 运行期 + 可绑定树 + 日志。
    /// 用法：RegisterTypeDefs(...) → Load(rootType, jsonText) → 绑定 Nodes。
    /// </summary>
    public sealed class McdocTreeDocument
    {
        /// <summary>符号索引：可注入 Resource.Init 建好的共享实例，避免每个 View 重建一遍。</summary>
        public McdocTypeIndex Symbols { get; }
        public McdocJsonAccessor Accessor { get; } = new();

        public McdocTreeDocument(McdocTypeIndex symbols = null)
        {
            Symbols = symbols ?? new McdocTypeIndex();
        }
        public ObservableCollection<McdocTreeNode> Nodes { get; } = [];
        public ObservableCollection<string> Log { get; } = [];

        /// <summary>未解析的类型名（诊断用）。</summary>
        public IReadOnlyCollection<string> MissedReferences => Symbols.MissedReferences;

        public string GameVersion { get; set; } = "1.21";

        /// <summary>注册表候选值来源（#[id] 下拉）。为 null 时那些字段退化成自由输入。</summary>
        public IMcdocRegistryOptions RegistryOptions { get; set; }
        public string DocumentUri { get; set; } = "file:///draft.json";

        /// <summary>
        /// 根类型所在模块（如 ::java::data::advancement）。相对名/use 解析以它为基准；
        /// 留空时按根类型自己登记的模块兜底。
        /// </summary>
        public string ModulePath { get; set; }

        /// <summary>树上有值被改动时回调宿主（宿主负责回写文本并重算）。</summary>
        public Action<McdocTreeNode> NodeValueChanged { get; set; }

        /// <summary>树上结构被改动（增删项）时回调宿主：需要整体重排文本并重建树。</summary>
        public Action<McdocTreeNode> NodeStructureChanged { get; set; }

        /// <summary>当前树的 JSON 根（结构变更后据此整体重新序列化）。</summary>
        public McdocJsonValue RootJson
        {
            get => rootJson;
            private set
            {
                McdocWriteTrace.LogText("RootJson<-", McdocWriteTrace.Keys(value), true);
                rootJson = value;
            }
        }

        private McdocJsonValue rootJson;
        private McdocTreeNode rootNode;

        /// <summary>
        /// 最近一次编辑"只脏了这一棵子树"的目标：文本侧据此只重排那段子树（结构编辑别整篇格式化）。
        /// null = 这次变动可能落在任何位置（整篇 Load / 补建子级 / 物化写到别处），文本必须走整篇路径。
        /// </summary>
        public McdocTreeNode TextDirtyNode { get; set; }

        /// <summary>
        /// 这一次结构编辑是"删掉了数组尾部一条"时，被删掉的那个 AST 实例（一次性）。
        /// 它已经不在模型里，文本侧只能靠它自己的锚点定位要抹掉的那一段。
        /// </summary>
        public McdocJsonValue TextDirtyRemovedItem { get; set; }

        /// <summary>取走脏子树标记（取走即清：一次脏标记只配一次回写）。</summary>
        public McdocTreeNode TakeTextDirtyNode(out McdocJsonValue removedItem)
        {
            var node = TextDirtyNode;
            removedItem = TextDirtyRemovedItem;
            TextDirtyNode = null;
            TextDirtyRemovedItem = null;
            return node;
        }

        /// <summary>最近一次结构校验得到的诊断。</summary>
        public IReadOnlyList<McdocDiagnostic> Diagnostics { get; private set; } = [];
        public int DiagnosticsCount => Diagnostics.Count;

        /// <summary>跑一遍结构校验（Load / 结构变更后调用）。</summary>
        public IReadOnlyList<McdocDiagnostic> Validate()
        {
            Diagnostics = McdocValidator.Validate(this);
            return Diagnostics;
        }

        /// <summary>
        /// 把"当前版本不存在"的键从文档里摘掉（换版本后调用）：左侧行**留着**（黄色感叹号），
        /// 右侧 JSON 里这个键不再存在。所以这里必须**静默**改模型 —— 走 RemoveField 会抬 StructureChanged，
        /// 父级一重建，JSON 里没有的键就不会再出"未声明键"行，那行当场消失。
        /// </summary>
        public int CountUnknownFields()
        {
            var count = 0;
            void Walk(McdocTreeNode node)
            {
                if (node is null) return;
                if (node.IsUnknownField && node.Json is not null)
                {
                    // 遗留键不建子级，它的子树里不会再冒出别的键
                    count++;
                    return;
                }
                foreach (var child in node.Children) Walk(child);
                foreach (var child in node.Items) Walk(child);
                foreach (var child in node.SelectedUnionChildren) Walk(child);
            }
            foreach (var root in Nodes) Walk(root);
            // 未声明键现在收在保留区里（不在任何 Node 之下）：状态栏的计数必须一起算
            if (PreservedGroup is not null)
            {
                foreach (var child in PreservedGroup.Children)
                {
                    if (child?.Json is not null) count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 清空"当前版本不存在"的键（保留区里的 + 树上残留的），返回删掉的个数。
        /// 全层级扫描 → 每个键按 JsonPath 从活 RootJson 里删；**不碰文本消化**（调用方自己整篇重建 + 回写文本）。
        /// </summary>
        public int RemoveAllUnknownFields()
        {
            var removed = 0;
            foreach (var node in FindUnknownFields())
            {
                if (TryRemoveLegacyOrGhost(node, out _) != LegacyDeleteOutcome.NotFound) removed++;
            }
            return removed;
        }

        /// <summary>全层级找出"当前版本不存在且有值"的键：树上残留的 + 保留区里的（去重，顺序稳定）。</summary>
        public List<McdocTreeNode> FindUnknownFields()
        {
            var targets = new List<McdocTreeNode>();
            var seen = new HashSet<McdocTreeNode>();
            void Walk(McdocTreeNode node)
            {
                if (node is null || !seen.Add(node)) return;
                if (node.IsUnknownField)
                {
                    if (node.Json is not null) targets.Add(node);
                    return;   // 未知键的子级不解析：不必往下走
                }
                foreach (var child in node.Children) Walk(child);
                foreach (var item in node.Items) Walk(item);
                foreach (var item in node.SelectedUnionChildren) Walk(item);
            }
            foreach (var node in Nodes) Walk(node);
            if (PreservedGroup is not null)
            {
                foreach (var node in PreservedGroup.Children) Walk(node);
            }
            return targets;
        }

        /// <summary>
        /// 遗留键 / 幽灵行的删除落点：**按 JsonPath 在活的 RootJson 里**解析父容器，而不是拿
        /// <see cref="McdocTreeNode.Parent"/> 缓存的那份 JSON。缓存父级可能是上一代节点、union 分支壳，
        /// 在它上面删键活文档一点没动 —— 投影一对账，行又回来（用户看到"闪一下、两侧都没删掉"）。
        /// 返回值告诉调用方：已经能从活父级通知（Notified）、父级实例已换代要整篇重载（NeedsReload），
        /// 或连父路径都解析不到（NotFound，调用方保留旧兜底）。
        /// </summary>
        public LegacyDeleteOutcome TryRemoveLegacyOrGhost(McdocTreeNode node, out McdocTreeNode liveParent)
        {
            liveParent = null;
            if (node is null || string.IsNullOrEmpty(node.FieldName)) return LegacyDeleteOutcome.NotFound;
            var path = node.JsonPath ?? "";
            var suffix = "." + node.FieldName;
            if (!path.EndsWith(suffix, StringComparison.Ordinal)) return LegacyDeleteOutcome.NotFound;
            var parentPath = path.Substring(0, path.Length - suffix.Length);
            if (string.IsNullOrEmpty(parentPath)) return LegacyDeleteOutcome.NotFound;

            var parentJson = ResolvePath(RootJson, parentPath);
            if (parentJson is not { Kind: McdocJsonKind.Object } parentObject) return LegacyDeleteOutcome.NotFound;
            liveParent = parentPath == "$" ? RootNode : FindNode(parentPath);

            var removed = parentObject.Properties?.RemoveAll(pair => pair.Key == node.FieldName) ?? 0;
            if (McdocWriteTrace.Enabled)
            {
                McdocWriteTrace.Trace("legacy-remove", "path=" + path + " parentPath=" + parentPath
                    + " field=" + node.FieldName + " removed=" + removed
                    + " keys=" + string.Join(",", parentObject.Properties?.Select(pair => pair.Key) ?? [])
                    + " sameJson=" + ReferenceEquals(liveParent?.Json, parentObject)
                    + " liveParent=" + (liveParent?.JsonPath ?? "<null>"));
            }
            if (removed > 0)
            {
                Detach(node);
                return ReferenceEquals(liveParent?.Json, parentObject)
                    ? LegacyDeleteOutcome.Notified
                    : LegacyDeleteOutcome.NeedsReload;
            }

            // 键已经不在文档里（右侧删键 / 切支剪键之后的幽灵行）：从**活父级**的子级集合摘掉再通知。
            // 活父级找不到时不能只摘缓存父级：那正是"摘了旧壳、投影又从根长回来"的来源。
            if (liveParent is null) return LegacyDeleteOutcome.NeedsReload;
            liveParent.Children.Remove(node);
            liveParent.Items.Remove(node);
            liveParent.SelectedUnionChildren.Remove(node);
            node.Parent = null;
            PreservedGroup?.Children.Remove(node);
            nodeIndex = null;
            return ReferenceEquals(liveParent.Json, parentObject)
                ? LegacyDeleteOutcome.Notified
                : LegacyDeleteOutcome.NeedsReload;

            void Detach(McdocTreeNode detached)
            {
                detached.Json = null;
                detached.Children.Clear();
                detached.Items.Clear();
                detached.SelectedUnionChildren.Clear();
                detached.ChildrenBuilt = true;
                detached.SyncScalarFromJson();
                detached.Parent = null;
                // 搬进保留区的键：摘掉时也要从那里注销，否则会留一行指向空路径的幽灵行
                PreservedGroup?.Children.Remove(detached);
                // 索引也是「还能找到这个键」的地方：不失效就会出现「文档里没了、FindNode 还命中」
                nodeIndex = null;
            }
        }

        /// <summary>
        /// 只重建该节点的直接子级（结构变更的快路径）。
        /// 返回 true 表示节点**被换掉了**（声明按兄弟键求值，只能重新求值），调用方要按新节点重拼投影。
        /// </summary>
        public bool RefreshNode(McdocTreeNode node)
        {
            if (node is null)
            {
                nodeIndex = null;
                return false;
            }
            // 子级要被重建：路径索引按这棵子树局部失效（否则 FindNode 会把刚被删掉/换掉的旧节点捞回来，
            // 表现是"JSON 里键没了、树侧那一行却还在"）。不整体置 null —— 那是全树 9.4 万个节点的重建。
            var localIndex = nodeIndex is not null;
            if (localIndex) DropIndexSubtree(node);
            // 重建期间的子级回调不补索引：这一层由下面那一次 IndexSubtree 统一装回去
            suppressIndexPatch = true;
            // RefreshChildren 捕获的是建节点那一刻的 type：dispatcher/indexed 的形状由**兄弟键**决定，
            // 复用它等于永远按旧值重建子级（切方块后旧属性行不走、按钮身份也不对）。这类节点只能重新求值。
            try
            {
                node.RefreshChildren?.Invoke();
            }
            finally
            {
                suppressIndexPatch = false;
            }
            // 这一层重建会把它的未声明键搬进保留区；键被删掉的旧保留条目在这里一起清
            PrunePreserved();
            // 结构变更只脏这一层：整篇 Validate 在 1MB 档要 84~562ms。诊断严格按节点本地写
            // （ValidateNodeCore 只碰 node.Diagnostics），所以只重算这棵子树等价 —— 除非子树大到
            // 会撞 ValidateSubtree 的 20000 节点守卫，那种情况宁可整篇也不许诊断停在半路。
            if (McdocValidator.SubtreeExceeds(node, 20000)) Validate();
            else ValidateNode(node);
            if (localIndex && nodeIndex is not null) IndexSubtree(node);
            return false;
        }

        /// <summary>
        /// 值补丁只改一个节点的 JSON、不重建，所以诊断必须按节点重算
        /// （否则"JSON 里改回合法值，行上的感叹号去不掉"）。同时把该路径在文档级清单里的旧条目换掉，
        /// 状态栏的计数才不会停在上一次。
        /// </summary>
        public void ValidateNode(McdocTreeNode node)
        {
            if (node is null) return;
            // 子树一起算：切分支后子节点（block / properties）的旧诊断必须被换掉，否则行上的感叹号下不去
            var fresh = McdocValidator.ValidateSubtree(node, MissedReferences, RegistryOptions, GameVersion);
            var path = node.JsonPath ?? "$";
            Diagnostics = (Diagnostics ?? [])
                .Where(item => !SameOrBelow(item.Path, path))
                .Concat(fresh)
                .ToList();
        }

        /// <summary>
        /// 只重算**这一个节点**的诊断（不走进子树）：结构变更后要刷的是祖先那几行，
        /// 对祖先走子树的代价是"每级都按整棵子树重算一遍"（1MB 档在根上就是 9.4 万节点）。
        /// 子树自己的诊断由脏节点那次 <see cref="ValidateNode"/> 负责。
        /// </summary>
        public void ValidateSelf(McdocTreeNode node)
        {
            if (node is null) return;
            var fresh = McdocValidator.ValidateNode(node, MissedReferences, RegistryOptions, GameVersion);
            var path = node.JsonPath ?? "$";
            Diagnostics = (Diagnostics ?? [])
                .Where(item => !string.Equals(item.Path, path, StringComparison.Ordinal))
                .Concat(fresh)
                .ToList();
        }

        /// <summary>诊断路径是否落在该子树里（"$.a" 与其 "a[0]" / "a.b" 都算）。</summary>
        internal static bool SameOrBelow(string itemPath, string rootPath)
        {
            if (string.IsNullOrEmpty(itemPath) || string.IsNullOrEmpty(rootPath)) return false;
            if (string.Equals(itemPath, rootPath, StringComparison.Ordinal)) return true;
            if (!itemPath.StartsWith(rootPath, StringComparison.Ordinal)) return false;
            var next = itemPath[rootPath.Length];
            return next is '.' or '[';
        }

        public McdocRuntime CreateRuntime() => new()
        {
            DocumentUri = DocumentUri,
            ModulePath = ModulePath,
            Symbols = Symbols,
            Accessor = Accessor,
            Attributes = McdocAttributeRegistry.CreateDefault(GameVersion),
            RegistryOptions = RegistryOptions,
            GameVersion = GameVersion,
            QuietUnknownPaths = quietUnknownPaths,
            Warn = message => Log.Add(message)
        };

        /// <summary>
        /// 载入那一刻文档里出现过的全部 JsonPath（对象成员与列表下标都算）。
        /// 用来回答"这个版本外键是打开文档时就有的，还是用户刚切出来的"：
        /// 前者静默（不挂牌、不进父行名单），后者照常报黄。
        /// </summary>
        private readonly HashSet<string> quietUnknownPaths = new(StringComparer.Ordinal);

        private void CaptureLoadKeys()
        {
            quietUnknownPaths.Clear();
            if (RootJson is null) return;
            var stack = new Stack<(McdocJsonValue Value, string Path)>();
            stack.Push((RootJson, "$"));
            while (stack.Count > 0)
            {
                var (value, path) = stack.Pop();
                if (value is null) continue;
                if (value.Kind == McdocJsonKind.Object)
                {
                    foreach (var pair in value.Properties ?? [])
                    {
                        var childPath = path + "." + pair.Key;
                        quietUnknownPaths.Add(childPath);
                        if (pair.Value is not null) stack.Push((pair.Value, childPath));
                    }
                }
                else if (value.Kind == McdocJsonKind.Array)
                {
                    var items = value.Items ?? [];
                    for (var index = 0; index < items.Count; index++)
                    {
                        if (items[index] is not null) stack.Push((items[index], path + "[" + index + "]"));
                    }
                }
            }
        }

        /// <summary>
        /// 用"已经解析好的 JSON AST"重建树（编辑器与树共用同一批节点时用，保证回写能按节点定位）。
        /// </summary>
        private Dictionary<string, McdocTreeNode> nodeIndex;

        /// <summary>RefreshNode 重建子级期间的抑制闸：那一层的索引由调用方统一装回去。</summary>
        private bool suppressIndexPatch;

        /// <summary>按 JsonPath 找节点（同步引擎用）。索引随 Load/RebuildNode 失效。</summary>
        public McdocTreeNode FindNode(string jsonPath)
        {
            if (string.IsNullOrEmpty(jsonPath)) return null;
            nodeIndex ??= BuildNodeIndex();
            return nodeIndex.TryGetValue(jsonPath, out var node) ? node : null;
        }

        /// <summary>
        /// 懒建把子级建出来时补索引：EnsureChildren 不走 RefreshNode，漏补的后果是
        /// "界面上刚展开的条目，FindNode 说它不存在"（结构编辑落点、删除落点、投影对账全靠这条索引）。
        /// </summary>
        private void IndexChildrenBuilt(McdocTreeNode node)
        {
            if (nodeIndex is null || suppressIndexPatch) return;
            IndexSubtree(node);
        }

        /// <summary>
        /// 按 JSON 路径（<c>$.a.b[0]</c>）在给定根上取值。树、投影、同步引擎共用这一份解析：
        /// "行指向的对象是不是文档里该路径的对象"这条判据全靠它。
        /// </summary>
        public static McdocJsonValue ResolvePath(McdocJsonValue root, string jsonPath)
        {
            if (root is null || string.IsNullOrEmpty(jsonPath)) return null;
            var current = root;
            var index = 0;
            while (index < jsonPath.Length)
            {
                if (jsonPath[index] == '.')
                {
                    var end = index + 1;
                    while (end < jsonPath.Length && jsonPath[end] != '.' && jsonPath[end] != '[') end++;
                    current = current.Get(jsonPath.Substring(index + 1, end - index - 1));
                    index = end;
                }
                else if (jsonPath[index] == '[')
                {
                    var end = jsonPath.IndexOf(']', index);
                    if (end < 0) return null;
                    if (!int.TryParse(jsonPath.Substring(index + 1, end - index - 1), out var position)) return null;
                    if (current?.Items is null || position < 0 || position >= current.Items.Count) return null;
                    current = current.Items[position];
                    index = end + 1;
                }
                else
                {
                    index++;
                }
                if (current is null) return null;
            }
            return current;
        }

        /// <summary>节点是否仍挂在这棵树上：重建会换掉节点对象，按路径重找可能命中别的节点（例如占位行）。</summary>
        public bool IsAttached(McdocTreeNode node)
        {
            if (node is null) return false;
            var top = node;
            for (var steps = 0; steps < 256 && top.Parent is not null; steps++) top = top.Parent;
            return Nodes.Contains(top) || ReferenceEquals(top, rootNode) || ReferenceEquals(top, PreservedGroup);
        }

        /// <summary>
        /// 索引的局部失效：先按**旧**子树把它覆盖的路径从索引里丢掉，调用方重建完子级后再把新子树装回去。
        /// 整体置 null 会让下一次 FindNode 重建全树索引（1MB 档 9.4 万个节点 ≈ 50~140ms），
        /// 而结构变更真正动过的只有这一层。
        /// </summary>
        private void DropIndexSubtree(McdocTreeNode node)
        {
            if (nodeIndex is null || node is null) return;
            var path = node.JsonPath;
            if (string.IsNullOrEmpty(path))
            {
                nodeIndex = null;
                return;
            }
            List<string> stale = null;
            foreach (var key in nodeIndex.Keys)
            {
                if (SameOrBelow(key, path)) (stale ??= []).Add(key);
            }
            if (stale is null) return;
            foreach (var key in stale) nodeIndex.Remove(key);
        }

        /// <summary>把一棵（刚重建的）子树装回路径索引；键已存在的按先写优先跳过。</summary>
        private void IndexSubtree(McdocTreeNode node)
        {
            if (nodeIndex is null || node is null) return;
            var stack = new Stack<McdocTreeNode>();
            stack.Push(node);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current is null) continue;
                if (!(current.IsDynamicKey && current.Json is null) && !nodeIndex.ContainsKey(current.JsonPath))
                {
                    nodeIndex[current.JsonPath] = current;
                }
                foreach (var child in current.Children) stack.Push(child);
                foreach (var child in current.Items) stack.Push(child);
                foreach (var child in current.SelectedUnionChildren) stack.Push(child);
            }
        }

        private Dictionary<string, McdocTreeNode> BuildNodeIndex()
        {
            var map = new Dictionary<string, McdocTreeNode>(StringComparer.Ordinal);
            void Walk(McdocTreeNode node)
            {
                if (node is null) return;
                // 动态键占位行与父容器共用 JsonPath（McdocPresenters 里 FieldName=""、JsonPath=父级）：
                // 它没有键身份，被路径索引命中并就地重建会造出"匿名条目行"。先写优先 + 跳过占位行。
                if (node.IsDynamicKey && node.Json is null) return;
                if (map.ContainsKey(node.JsonPath)) return;
                map[node.JsonPath] = node;
                foreach (var child in node.Children) Walk(child);
                foreach (var child in node.Items) Walk(child);
                foreach (var child in node.SelectedUnionChildren) Walk(child);
            }
            foreach (var node in Nodes) Walk(node);
            // 保留区里的键同样是文档节点（只是父容器是合成的）：按路径依旧要能找回来 —
            // 同步引擎、删除落点、投影对账全靠这条索引。它们不建子级，一层就够。
            if (PreservedGroup is not null)
            {
                if (!map.ContainsKey(PreservedGroup.JsonPath)) map[PreservedGroup.JsonPath] = PreservedGroup;
                foreach (var child in PreservedGroup.Children)
                {
                    if (child is null || string.IsNullOrEmpty(child.JsonPath)) continue;
                    if (!map.ContainsKey(child.JsonPath)) map[child.JsonPath] = child;
                }
            }
            return map;
        }

        /// <summary>
        /// 只重建一个节点（含其子树），在原父容器里原位替换。
        /// 运行期依赖（enum 改值 → 调度器目标变化 / 结构变化）走这里，不再整树 Load。
        /// 返回新节点；父链或根集合里找不到原节点时返回 null（调用方回退整树）。
        /// </summary>
        public McdocTreeNode RebuildNode(McdocTreeNode node)
        {
            if (node is null) return null;
            // 没有求值现场（ValueNode）的节点重建出来必然没有键身份：%key 落 %none → 成员退化成 any。
            // 让调用方回退整树重建，而不是把匿名空壳摆成条目行。
            if (node.ValueNode is null) return null;
            var factory = new McdocNodeFactory { PreservedGroup = PreservedGroup, OnChildrenBuilt = IndexChildrenBuilt };
            var rebuildRuntime = CreateRuntime();
            if (node.UnionSelectionExplicit && node.Type?.Members is { } unionMembers && node.SelectedUnionMember is not null)
            {
                var index = unionMembers.IndexOf(node.SelectedUnionMember);
                if (index >= 0)
                {
                    rebuildRuntime.UnionSelectionHints = new Dictionary<string, int>(StringComparer.Ordinal)
                    {
                        [node.JsonPath ?? ""] = index
                    };
                }
            }
            // 值必须取**活文档里的那一份**，不许拿节点自己的旧快照：切 union 分支 / 值刚写进父容器时，
            // 节点上的 Json 可能还停在上代（UpdateOne 的锚点也可能是上一代实例），
            // 定向重建于是把上一支又建回来 —— 现场：Potion contents 从 String 支选 Object 支，
            // 下拉当场变了、右侧文本也已经是 {}，一拍重建之后行又弹回 String（必须走「未设置」才切得过去）。
            var liveJson = TryLiveValue(node, out var current) ? current : node.Json;
            var rebuilt = factory.CreateNode(node.FieldName, node.DeclaredType, liveJson, rebuildRuntime,
                node.ValueNode?.Parent, node.ValueNode?.RuntimeKey, node.Depth, true,
                NodeValueChanged, node.JsonPath, null, NodeStructureChanged, null);
            if (rebuilt is null) return null;
            // 父级表现器是**建完子节点之后**才写字段语义的（必选性 / 文档注释 / 键提示），重建不会带回来：
            // 丢了就是"可选字段突然变成必填"（行上的「清空」让位给 ⊕，ui-gate S20-4 直接红）
            rebuilt.IsRequired = node.IsRequired;
            // 重建出来的是新节点：可选字段下拉的（未设置）哨兵与"缺键就显示未设置"的预选要补回来
            McdocSelectOptions.EnsureUnsetOption(rebuilt);
            rebuilt.EnsureAnyUnsetOption();
            // any 行的分支草稿同理：父级重建换了实例，草稿不搬过去 → 切回原分支只能拿到默认值
            rebuilt.AdoptAnyBranchDrafts(node);
            rebuilt.Desc = node.Desc;
            rebuilt.Key = node.Key;
            // 用户显式选过的 union 分支：重建后必须还是那一支，否则一改别的字段就自己弹回去
            rebuilt.UnionSelectionExplicit = node.UnionSelectionExplicit;
            // 缺省字段（⊕ 行）长出了值：展开态要用新节点算出来的默认值（depth < DefaultExpandDepth + 有 JSON），
            // 否则「新加进来的 display 是折叠的」，而整树重建出来却是展开的 —— 两个视图观感不同代
            if (node.Json is not null) rebuilt.IsExpanded = node.IsExpanded;
            rebuilt.ReplaceRoot = node.ReplaceRoot;

            var replaced = false;
            if (node.Parent is { } parent)
            {
                rebuilt.Parent = parent;
                // map 条目的"行身份"同样是父级建完子节点之后才写的（是不是条目 / 键名 / 动态字段下标 / 键候选），
                // 重建只按声明类型重跑一遍会把这些全部丢掉 —— 表现是行上的「重命名」与条目删除按钮当场消失。
                // 命令必须按**新节点**重挂：旧闭包捕获的是已经被换掉的旧节点。
                // 父级类型把这个键声明成**静态字段**时，它就不是 map 条目：这时把旧节点的条目身份搬回来
                // 会让行上同时出现"添加"和"删除条目"（AddEntryVisibility = !IsMapEntry）—— 身份必须按新形态判。
                var declaredByParent = parent.Type?.Fields?.Any(field => field is not null
                    && string.Equals(field.Name, node.FieldName, StringComparison.Ordinal)) == true;
                if (node.IsMapEntry && !declaredByParent)
                {
                    var editor = McdocCollectionEditor.Default;
                    var runtime = CreateRuntime();
                    rebuilt.IsMapEntry = true;
                    rebuilt.DynamicFieldIndex = node.DynamicFieldIndex;
                    rebuilt.UseKeySelect = node.UseKeySelect;
                    rebuilt.KeyOptionList ??= node.KeyOptionList;
                    rebuilt.AddEntryCommand = new McdocCommand(
                        () => editor.CommitEntry(parent, rebuilt, runtime),
                        () => editor.CanCommitEntry(parent, rebuilt));
                    rebuilt.RemoveEntryCommand = new McdocCommand(
                        () => editor.RemoveEntry(parent, rebuilt),
                        () => rebuilt.IsMapEntry);
                    rebuilt.EditingKey = node.EditingKey;
                }
                // 列表/元组子行的「删掉这一项」也是父级（列表表现器）建完子节点之后才挂的：
                // 重建按声明类型重跑一遍就没有这条命令 → 行上那个删除按钮永久塌掉
                // （现场：Entry 里把子 condition 换一支之后，Entry 自己的删除按钮消失）。
                // "这一项是列表条目"不能只查 parent.Items：union 的列表分支把条目上提到
                // SelectedUnionChildren（parent 是 union 行、Items 是空的）。漏判的后果是
                // 重建出来的条目没有 RemoveSelfCommand —— 行上的 ⊖ 当场消失（现场：只有 1 个元素时）。
                // **只认真正的列表条目**：map 条目也在 parent.Children 里，把它算成"列表条目"会给它挂上
                // ▲▼/⊖ 三颗列表命令（找不到 owner → list-item-owner-miss，行上还多出没法用的按钮）。
                var wasListItem = parent.Items.Contains(node)
                    || (parent.UnionListLike && parent.SelectedUnionChildren.Contains(node));
                replaced = ReplaceIn(parent.Children) || ReplaceIn(parent.Items) || ReplaceIn(parent.SelectedUnionChildren);
                // 列表项的三个命令（删/上移/下移）都是父级建完子节点之后才挂的：重建必须整套搬回来，
                // 而且下标要在**执行时**现问 —— 只搬"删除"会让 ▲▼ 当场消失或挪错项
                if (replaced && wasListItem) McdocCollectionEditor.Default.AttachListItemCommands(parent, rebuilt);
                // 摊平根的顶层成员：Nodes 是投影与一致性守卫的来源，只换 rootNode.Children 的话
                // 视图还指着旧实例（子行永远不更新），而 RefreshAround 又因为父行是合成根而返回 false
                if (replaced && ReferenceEquals(parent, rootNode))
                {
                    var topIndex = Nodes.IndexOf(node);
                    if (topIndex >= 0) Nodes[topIndex] = rebuilt;
                }
            }
            else
            {
                var index = Nodes.IndexOf(node);
                if (index >= 0)
                {
                    Nodes[index] = rebuilt;
                    rebuilt.ReplaceRoot ??= value => RootJson = value;
                    rebuilt.IsDocumentRoot = rebuilt.Parent is null;
                    replaced = true;
                }
            }
            if (!replaced) return null;

            nodeIndex = null;
            Validate();
            return rebuilt;

            static bool Contains(McdocTreeNode owner, McdocTreeNode child)
                => owner is not null && child is not null
                    && (owner.Children.Contains(child) || owner.Items.Contains(child)
                        || owner.SelectedUnionChildren.Contains(child));

            bool ReplaceIn(ObservableCollection<McdocTreeNode> collection)
            {
                var index = collection.IndexOf(node);
                if (index < 0) return false;
                collection[index] = rebuilt;
                return true;
            }
        }

        /// <summary>
        /// 这个节点在**活文档**里对应的值：父容器按键身份（对象取同名键 / 数组按下标）现取。
        /// 判不了（没有父级、父级没值、字段名不是下标）时返回 false，调用方退回节点自己的快照。
        /// 键被删掉时返回 true + null：那也是一份"活值"，拿旧快照重建会把删掉的键复活。
        /// </summary>
        private static bool TryLiveValue(McdocTreeNode node, out McdocJsonValue value)
        {
            value = null;
            if (node?.Parent?.Json is not { } parentJson) return false;
            if (parentJson.Kind == McdocJsonKind.Object)
            {
                if (string.IsNullOrEmpty(node.FieldName)) return false;
                value = parentJson.Get(node.FieldName);
                return true;
            }
            if (parentJson.Kind == McdocJsonKind.Array && int.TryParse(node.FieldName, out var index))
            {
                var items = parentJson.Items;
                value = items is not null && index >= 0 && index < items.Count ? items[index] : null;
                return true;
            }
            return false;
        }

        /// <summary>构建期造出的合成根（摊平根时它不进 Nodes，但仍是顶层节点的父容器）。</summary>
        public McdocTreeNode RootNode => rootNode;

        /// <summary>
        /// 保留区合成容器（"当前版本不存在（N）"）：未声明键从原父级搬到这里，主视图因此不会到处冒黄叹号行。
        /// 它**不在 Nodes、也不在 rootNode.Children 里** —— 顶层成员表、文本同步、条目排序都看不到它；
        /// 行只由投影器在整棵树之后追加一条。子级仍保留各自的 JsonPath，删除照旧按活文档解析。
        /// </summary>
        public McdocTreeNode PreservedGroup { get; private set; }

        /// <summary>
        /// 投影根：顶层节点 + 保留区容器（真的有残留键时）。投影、VM、探针都用这一份列表 ——
        /// 谁再自己拼 <c>Nodes.ToList()</c>，保留区那一行就会在某条路径上凭空消失。
        /// </summary>
        public IReadOnlyList<McdocTreeNode> ProjectionRoots
        {
            get
            {
                var roots = new List<McdocTreeNode>(Nodes.Count + 1);
                roots.AddRange(Nodes);
                if (PreservedGroup is { Children.Count: > 0 }) roots.Add(PreservedGroup);
                return roots;
            }
        }

        /// <summary>保留区每个文档一份，只在整树重建时换新（展开状态跟着搬）。</summary>
        private void ResetPreservedGroup(bool expanded)
        {
            PreservedGroup = new McdocTreeNode
            {
                FieldName = "当前版本不存在",
                Key = "当前版本不存在",
                JsonPath = "$.__preserved",
                Depth = 0,
                Type = McdocType.Any(),
                IsRequired = false,
                Json = null,
                ChildrenBuilt = true,
                IsPreservedGroup = true,
                Runtime = CreateRuntime(),
                StructureChanged = NodeStructureChanged,
                ValueChanged = NodeValueChanged,
                // 必须在 Json=null 之后：那个 setter 会把"可选且没值"的节点折叠回去
                IsExpanded = expanded,
            };
        }

        /// <summary>
        /// 摘掉保留区里"文档里已经没有这个键"的陈旧条目：未声明键是子级重建时搬进来的，
        /// 键在文本侧被删掉时没人会通知保留区 —— 不清就会留一行指向空路径的幽灵行。
        /// </summary>
        internal void PrunePreserved()
        {
            if (PreservedGroup is null) return;
            for (var index = PreservedGroup.Children.Count - 1; index >= 0; index--)
            {
                var child = PreservedGroup.Children[index];
                if (child is not null && ResolvePath(RootJson, child.JsonPath) is not null) continue;
                if (child is not null) child.Parent = null;
                PreservedGroup.Children.RemoveAt(index);
            }
        }

        /// <summary>根是 struct 且子级直接摊进 Nodes（misode 的 McdocRoot 摊平分支）。</summary>
        public bool FlattenedRoot => rootNode is not null && !Nodes.Contains(rootNode);

        /// <summary>顶层成员表：键 → 有键身份的顶层节点（动态键占位行没有键身份，不进表）。</summary>
        internal Dictionary<string, McdocTreeNode> TopLevelMembers()
        {
            var map = new Dictionary<string, McdocTreeNode>(StringComparer.Ordinal);
            foreach (var node in Nodes)
            {
                if (node is null || node.IsDynamicKey) continue;
                var key = node.FieldName;
                if (string.IsNullOrEmpty(key)) continue;
                if (!string.Equals(node.JsonPath, "$." + key, StringComparison.Ordinal)) continue;
                map[key] = node;
            }
            return map;
        }

        /// <summary>
        /// 建一个根层成员节点（文本里出现了文档原本没有的顶层键）。静态字段 / 动态条目 / 未声明字段
        /// 三条分支全走表现器那一份规则，与整树重建同源。
        /// </summary>
        public McdocTreeNode CreateRootMember(string key, McdocJsonValue containerJson)
        {
            if (rootNode is null || string.IsNullOrEmpty(key)) return null;
            var structType = rootNode.Type;
            if (structType?.Kind != McdocTypeKind.Struct) return null;
            if (McdocPresenterRegistry.Default.For(structType) is not StructNodePresenter presenter) return null;
            try
            {
                var context = new McdocBuildContext
                {
                    Runtime = CreateRuntime(),
                    Factory = new McdocNodeFactory { PreservedGroup = PreservedGroup, OnChildrenBuilt = IndexChildrenBuilt },
                    Self = rootNode.ValueNode,
                    KeyNode = rootNode.ValueNode?.RuntimeKey,
                    Depth = rootNode.Depth,
                    Narrow = true,
                    JsonPath = "$",
                    Visiting = [],
                    Budget = new McdocNodeBudget(McdocNodeFactory.MaxNodes),
                    OnValueChanged = NodeValueChanged,
                    OnStructureChanged = NodeStructureChanged,
                };
                var node = presenter.CreateMemberNode(rootNode, structType, containerJson ?? RootJson, key, context);
                if (node is null) return null;
                node.Parent = rootNode;
                node.JsonPath = "$." + key;
                return node;
            }
            catch (Exception error)
            {
                Log.Add("[McdocTreeDocument] 根层成员构建失败: " + key + " — " + error.Message);
                return null;
            }
        }

        /// <summary>把一个根层成员挂到根上（按新 JSON 的键序插进条目区），并同步顶层列表。</summary>
        public bool InsertRootMember(McdocTreeNode node, IReadOnlyList<string> keyOrder)
        {
            if (rootNode is null || node is null || rootNode.Children.Contains(node)) return false;
            node.Parent = rootNode;
            rootNode.Children.Insert(EntryInsertIndex(node.FieldName, keyOrder), node);
            SyncTopLevelFromRoot();
            return true;
        }

        /// <summary>把一个根层成员摘掉（文档里没有这个键、schema 也没声明它），并同步顶层列表。</summary>
        public bool RemoveRootMember(McdocTreeNode node)
        {
            if (rootNode is null || node is null || !rootNode.Children.Remove(node)) return false;
            node.Parent = null;
            SyncTopLevelFromRoot();
            return true;
        }

        /// <summary>
        /// 文档里删掉了一个**声明过的**键：行必须留成一行 ⊕（与整树重建一致），子级整片作废。
        /// </summary>
        public void ClearRootMember(McdocTreeNode node)
        {
            ClearMember(node);
            nodeIndex = null;
        }

        /// <summary>
        /// 建一个**非根**容器里的成员节点（容器里出现了原本没有的键）。静态字段 / 动态条目 / 未声明字段
        /// 三条分支仍由表现器那一份规则决定，与根层 CreateRootMember 同源。
        /// </summary>
        public McdocTreeNode CreateMemberNode(McdocTreeNode container, string key, McdocJsonValue containerJson)
        {
            if (container is null || string.IsNullOrEmpty(key)) return null;
            var structType = container.Type;
            if (structType?.Kind != McdocTypeKind.Struct) return null;
            if (McdocPresenterRegistry.Default.For(structType) is not StructNodePresenter presenter) return null;
            try
            {
                var context = new McdocBuildContext
                {
                    Runtime = CreateRuntime(),
                    Factory = new McdocNodeFactory { PreservedGroup = PreservedGroup, OnChildrenBuilt = IndexChildrenBuilt },
                    Self = container.ValueNode,
                    KeyNode = container.ValueNode?.RuntimeKey,
                    Depth = container.Depth,
                    Narrow = true,
                    JsonPath = container.JsonPath ?? "$",
                    Visiting = [],
                    Budget = new McdocNodeBudget(McdocNodeFactory.MaxNodes),
                    OnValueChanged = NodeValueChanged,
                    OnStructureChanged = NodeStructureChanged,
                };
                var node = presenter.CreateMemberNode(container, structType, containerJson ?? container.Json, key, context);
                if (node is null) return null;
                node.Parent = container;
                node.JsonPath = (container.JsonPath ?? "$") + "." + key;
                return node;
            }
            catch (Exception error)
            {
                Log.Add("[McdocTreeDocument] 成员构建失败: " + key + " — " + error.Message);
                return null;
            }
        }

        /// <summary>把一个成员挂进非根容器（按新 JSON 的键序插进子级），并补进路径索引。</summary>
        public bool InsertMemberNode(McdocTreeNode container, McdocTreeNode node, IReadOnlyList<string> keyOrder)
        {
            if (container is null || node is null || container.Children.Contains(node)) return false;
            node.Parent = container;
            container.Children.Insert(MemberInsertIndex(container, node.FieldName, keyOrder), node);
            IndexSubtree(node);
            return true;
        }

        /// <summary>把一个成员从非根容器上摘掉（文档里没有这个键、schema 也没声明它）。</summary>
        public bool RemoveMemberNode(McdocTreeNode container, McdocTreeNode node)
        {
            if (container is null || node is null || !container.Children.Remove(node)) return false;
            node.Parent = null;
            DropIndexSubtree(node);
            return true;
        }

        /// <summary>
        /// 文档里删掉了一个**声明过的**非根键：行必须留成一行 ⊕（与整树重建一致），子级整片作废。
        /// </summary>
        public void ClearMember(McdocTreeNode node)
        {
            if (node is null) return;
            node.Json = null;
            node.Children.Clear();
            node.Items.Clear();
            node.SelectedUnionChildren.Clear();
            node.ChildrenBuilt = true;
            DropIndexSubtree(node);
        }

        /// <summary>新成员在容器子级里的位置：按新 JSON 的键序，找不到后继就排到最后。</summary>
        private static int MemberInsertIndex(McdocTreeNode container, string key, IReadOnlyList<string> keyOrder)
        {
            var rank = RankOf(key, keyOrder);
            for (var index = 0; index < container.Children.Count; index++)
            {
                var child = container.Children[index];
                if (child is null || string.IsNullOrEmpty(child.FieldName)) continue;
                if (RankOf(child.FieldName, keyOrder) > rank) return index;
            }
            return container.Children.Count;
        }

        /// <summary>建一个数组条目节点（尾部追加用）：与 ListNodePresenter.PopulateChildren 同源。</summary>
        public McdocTreeNode CreateItemNode(McdocTreeNode listNode, McdocJsonValue itemJson, int index)
        {
            if (listNode is null || index < 0) return null;
            var listType = listNode.Type;
            if (listType?.Kind != McdocTypeKind.List) return null;
            if (McdocPresenterRegistry.Default.For(listType) is not ListNodePresenter presenter) return null;
            try
            {
                var context = new McdocBuildContext
                {
                    Runtime = CreateRuntime(),
                    Factory = new McdocNodeFactory { PreservedGroup = PreservedGroup, OnChildrenBuilt = IndexChildrenBuilt },
                    Self = listNode.ValueNode,
                    KeyNode = listNode.ValueNode?.RuntimeKey,
                    Depth = listNode.Depth,
                    Narrow = true,
                    JsonPath = listNode.JsonPath ?? "$",
                    Visiting = [],
                    Budget = new McdocNodeBudget(McdocNodeFactory.MaxNodes),
                    OnValueChanged = NodeValueChanged,
                    OnStructureChanged = NodeStructureChanged,
                };
                var node = presenter.CreateItemNode(listNode, listType, itemJson, index, context);
                if (node is null) return null;
                node.Parent = listNode;
                return node;
            }
            catch (Exception error)
            {
                Log.Add("[McdocTreeDocument] 条目构建失败: " + index + " — " + error.Message);
                return null;
            }
        }

        /// <summary>把条目挂进 Items（命令在挂上之后再挂：CanExecute 要问 Items.IndexOf）。</summary>
        public bool InsertItemNode(McdocTreeNode listNode, McdocTreeNode item, int index)
        {
            if (listNode is null || item is null || listNode.Items.Contains(item)) return false;
            if (index < 0 || index > listNode.Items.Count) index = listNode.Items.Count;
            item.Parent = listNode;
            listNode.Items.Insert(index, item);
            McdocCollectionEditor.Default.AttachListItemCommands(listNode, item);
            IndexSubtree(item);
            return true;
        }

        /// <summary>把条目从 Items 上摘掉（尾部删除用）。</summary>
        public bool RemoveItemNode(McdocTreeNode listNode, McdocTreeNode item)
        {
            if (listNode is null || item is null || !listNode.Items.Remove(item)) return false;
            item.Parent = null;
            DropIndexSubtree(item);
            return true;
        }

        /// <summary>
        /// 摊平根的顶层列表就是根的子级，顺序也照抄：行序只在模型里定一次
        /// （StructNodePresenter.OrderChildren：文档序 + 未写字段的声明位），文本同步的
        /// "第几个顶层行"也按它定位 —— 投影再排一次就会与它错位（行重复、键缺行）。
        /// </summary>
        private void SyncTopLevelFromRoot()
        {
            if (rootNode is null || !FlattenedRoot) return;
            Nodes.Clear();
            foreach (var child in rootNode.Children) Nodes.Add(child);
            nodeIndex = null;
        }

        /// <summary>新成员在条目区里的位置：按新 JSON 的键序；找不到后继就排到最后一个条目后面。</summary>
        private int EntryInsertIndex(string key, IReadOnlyList<string> keyOrder)
        {
            var rank = RankOf(key, keyOrder);
            for (var index = 0; index < rootNode.Children.Count; index++)
            {
                var child = rootNode.Children[index];
                if (child is null || !(child.IsMapEntry || child.IsUnknownField)) continue;
                if (RankOf(child.FieldName, keyOrder) > rank) return index;
            }
            for (var index = rootNode.Children.Count - 1; index >= 0; index--)
            {
                var child = rootNode.Children[index];
                if (child is not null && (child.IsMapEntry || child.IsUnknownField)) return index + 1;
            }
            return rootNode.Children.Count;
        }

        private static int RankOf(string key, IReadOnlyList<string> keyOrder)
        {
            if (keyOrder is null) return int.MaxValue;
            for (var index = 0; index < keyOrder.Count; index++)
            {
                if (string.Equals(keyOrder[index], key, StringComparison.Ordinal)) return index;
            }
            return int.MaxValue;
        }

        /// <summary>文本同步引擎已经把补丁打到节点上：这里只换根引用（保持 AST 与编辑器共享）。</summary>
        public void AdoptRoot(McdocJsonValue root)
        {
            RootJson = root;
            // 摊平后合成根不在 Nodes 里，重指（RelinkToRoot）走不到它；不同步就会把顶层增删写进上一份 JSON
            if (rootNode is not null)
            {
                rootNode.Json = root;
                // 求值现场也得跟到这一代：顶层字段按"同层兄弟键"求值（dispatcher 的动态索引）读的就是它，
                // 合成根漏了就会按上一代 AST 取键 —— 右侧把 block 改空、左侧 properties 照样解成旧方块
                if (rootNode.ValueNode?.Node is not null) rootNode.ValueNode.Node.OriginalNode = root;
            }
            nodeIndex = null;
            Generation++;
        }

        /// <summary>
        /// 树的"代际"：每次<b>整树换代</b>（<see cref="Load"/> / <see cref="AdoptRoot"/>）自增。
        /// 一致性守卫据此判断"要不要真的走查一遍"——没换代就是 O(1)，别再每次防抖都扫全树。
        /// </summary>
        public int Generation { get; private set; }

        public void Load(McdocType rootType, McdocJsonValue root)
        {
            using var stageTotal = McdocWriteTrace.Stage("doc.load");
            // 成员顺序不在这里排：行序 = 文档序 + 未写字段的声明位（StructNodePresenter.OrderChildren），
            // 文本按 AST 原序写出，新键按声明序插位（McdocJsonOrder.InsertIndex）—— 一处定序，别处只读。
            RootJson = root;
            nodeIndex = null;
            Generation++;
            // 建树之前先记下"打开时就有哪些键"：静默判据要在第一个未声明节点建出来之前就位
            CaptureLoadKeys();
            // 重建会换掉所有节点，而编辑时又频繁重建 —— 先把展开/选中状态按 JsonPath 记下来，
            // 建完再恢复，否则用户点了 ⊕ 或改了个值，整棵树就全折叠（截图里的现象）。
            var expandedPaths = new HashSet<string>(StringComparer.Ordinal);
            var selectedPath = (string)null;
            void Capture(McdocTreeNode node)
            {
                if (node is null) return;
                if (node.IsExpanded) expandedPaths.Add(node.JsonPath);
                if (node.IsSelected) selectedPath = node.JsonPath;
                foreach (var child in node.Children) Capture(child);
                foreach (var child in node.Items) Capture(child);
                foreach (var child in node.SelectedUnionChildren) Capture(child);
            }
            foreach (var node in Nodes) Capture(node);

            try
            {
                // 模块没显式给定时用根类型登记的模块：相对名解析不该取决于调用方有没有记得设置
                ModulePath ??= rootType?.Module;
                // 保留区先于建树就位：建子级的唯一出口（RefreshChildren）要把未声明键搬进来
                ResetPreservedGroup(PreservedGroup?.IsExpanded == true);
                var factory = new McdocNodeFactory { PreservedGroup = PreservedGroup, OnChildrenBuilt = IndexChildrenBuilt };
                System.Collections.ObjectModel.ObservableCollection<McdocTreeNode> built;
                using (McdocWriteTrace.Stage("doc.build"))
                {
                    built = factory.Build(rootType, root, CreateRuntime(), NodeValueChanged, NodeStructureChanged);
                }
                rootNode = factory.RootNode;
                Nodes.Clear();
                foreach (var node in built)
                {
                    // 根节点没有父容器：编辑它时要直接替换 RootJson（否则改值不落盘）
                    node.ReplaceRoot = value => RootJson = value;
                    node.IsDocumentRoot = node.Parent is null;
                    Nodes.Add(node);
                }

                void Restore(McdocTreeNode node)
                {
                    if (node is null) return;
                    if (expandedPaths.Contains(node.JsonPath)) node.IsExpanded = true;
                    // 不恢复 IsSelected：重建时重新选中会让 TreeView 反复 BringIntoView，
                    // 表现就是"界面一闪一闪 + 不停向右滚"。
                    foreach (var child in node.Children) Restore(child);
                    foreach (var child in node.Items) Restore(child);
                    foreach (var child in node.SelectedUnionChildren) Restore(child);
                }
                foreach (var node in Nodes) Restore(node);
                PrunePreserved();

                // 把"查不到的类型名"暴露出来：界面上一眼能看到，不用再猜
                if (Symbols.MissedReferences.Count > 0)
                {
                    Log.Add("未解析引用: " + string.Join(", ", Symbols.MissedReferences));
                }
            }
            catch (Exception error)
            {
                Log.Add("[McdocTreeDocument] 构建失败: " + error.Message);
                Nodes.Clear();
            }
            using (McdocWriteTrace.Stage("doc.validate")) Validate();
        }

        /// <summary>
        /// 把这次求值的现场写到文件：索引规模、未解析引用、每个节点的"求值后类型/声明类型/子级数"。
        /// 实机现象与离线校验不一致时（多半是语言服务器版本或索引缺文档），靠它一眼看出差在哪，
        /// 不用再靠截图猜模板。
        /// </summary>
        public void WriteDiagnostic(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            try
            {
                var builder = new System.Text.StringBuilder();
                builder.AppendLine("gameVersion = " + GameVersion);
                builder.AppendLine("documentUri = " + DocumentUri);
                builder.AppendLine("modulePath  = " + (ModulePath ?? "<null>"));
                builder.AppendLine("typeDefCount= " + Symbols.TypeDefCount);
                builder.AppendLine("missed      = " + (Symbols.MissedReferences.Count == 0
                    ? "<none>" : string.Join(", ", Symbols.MissedReferences)));
                builder.AppendLine("log         = " + (Log.Count == 0 ? "<none>" : string.Join(" | ", Log)));
                builder.AppendLine();
                void Walk(McdocTreeNode node, int depth)
                {
                    if (node is null) return;
                    builder.Append(new string(' ', depth * 2))
                        .Append('[').Append(string.IsNullOrEmpty(node.FieldName) ? "<empty>" : node.FieldName).Append(']')
                        .Append(" type=").Append(node.Type?.Kind.ToString() ?? "<null>")
                        .Append(" declared=").Append(node.DeclaredType?.Kind.ToString() ?? "<null>")
                        .Append(" dynKey=").Append(node.IsDynamicKey)
                        .Append(" mapEntry=").Append(node.IsMapEntry)
                        .Append(" json=").Append(node.Json?.Kind.ToString() ?? "null")
                        .AppendLine();
                    foreach (var child in node.Children) Walk(child, depth + 1);
                    foreach (var item in node.Items) Walk(item, depth + 1);
                    foreach (var item in node.SelectedUnionChildren) Walk(item, depth + 1);
                }
                foreach (var node in Nodes) Walk(node, 0);
                System.IO.File.WriteAllText(path, builder.ToString());
            }
            catch (Exception error)
            {
                Log.Add("[McdocTreeDocument] 诊断写入失败: " + error.Message);
            }
        }

        /// <summary>
        /// 从文本重建整棵树。解析后统一走 AST 重载，保证两条入口的回调接线完全一致
        /// （曾经这里单独 BuildResult，漏了结构变更回调，导致列表 ⊕/⊖ 改了 JSON 却不通知宿主）。
        /// </summary>
        public void Load(McdocType rootType, string jsonText)
            => Load(rootType, McdocJsonTextParser.Parse(jsonText).Root);
    }
}
