// JSON 感知的文档状态：组合在 TextDocument 之上（AvalonEdit 的 TextDocument 是 sealed，无法派生）。
// 维护"节点 → 文本锚点"的映射，并提供手术式编辑：只替换出问题的那一段，
// 其余位置由 TextDocument 的 TextAnchor 自动平移。
using System;
using System.Collections.Generic;
using System.Linq;
using CBHK.Utility.Data.Mcdoc;
using ICSharpCode.AvalonEdit.Document;

namespace CBHK.Utility.Data.JsonText
{
    /// <summary>一个 JSON 节点在文本中的锚点（值区间 + 整段属性区间）。</summary>
    public sealed class JsonNodeAnchors
    {
        public TextAnchor ValueStart;
        public TextAnchor ValueEnd;
        public TextAnchor PairStart;
        public TextAnchor PairEnd;

        /// <summary>锚点仍指向合法区间时返回 true（被外部删除后为 false，需要 Reparse）。</summary>
        public bool IsValid => ValueStart is { IsDeleted: false } && ValueEnd is { IsDeleted: false };
    }

    public sealed class JsonTextState
    {
        private readonly TextDocument document;
        private readonly Dictionary<McdocJsonValue, JsonNodeAnchors> anchors = [];

        /// <summary>
        /// 锚点能不能当坐标用。整篇替换（<c>Document.Text = …</c> 这类外部批量改写）会把所有 TextAnchor 拍到替换边界上，
        /// 之后再读它们全是错的 —— 那种情况下立刻降级成"不认锚点"，由调用方回落 <see cref="Reparse"/>，
        /// 而不是拿一批假偏移去切片。本类的四条全量登记出口（Reparse / 整篇写回 / 换根拼接）会把它重新立起来。
        /// </summary>
        private bool anchorsTrusted = true;

        public JsonTextState(TextDocument document)
        {
            this.document = document ?? throw new ArgumentNullException(nameof(document));
            document.Changed += OnDocumentChanged;
            Reparse();
        }

        /// <summary>把状态挂到（很可能已经有内容的）文档上。</summary>
        public static JsonTextState Attach(TextDocument document) => new(document);

        /// <summary>
        /// 只有一个改动能把整篇坐标一次性作废：一次性替换掉整篇文本（编辑器 Load、外部 <c>Document.Text = …</c>）。
        /// 逐字符/逐段的编辑交给 TextAnchor 自己平移，这里不插手。
        /// </summary>
        private void OnDocumentChanged(object sender, DocumentChangeEventArgs e)
        {
            if (!anchorsTrusted || e.Offset != 0) return;
            var before = document.TextLength - e.InsertionLength + e.RemovalLength;
            if (before > 0 && e.RemovalLength >= before) anchorsTrusted = false;
        }

        public TextDocument Document => document;

        /// <summary>整篇接管次数（Reload / 新代文本落地）。门禁用：树侧编辑必须一次都不涨。</summary>
        public int WholeWriteCount { get; private set; }

        /// <summary>上一次写回的第一遍没落地、靠第二遍补齐（= 边界被别的写入动过）。</summary>
        public bool LastWriteRepaired { get; private set; }

        /// <summary>物理定向写回次数（只替换真正变过的那一段，整篇文本不动）。</summary>
        public int SpliceCount { get; private set; }

        /// <summary>上一次定向写回动过的字符数（删 + 增，诊断/门禁用）。</summary>
        public int LastSpliceLength { get; private set; }

        /// <summary>整篇写回前后的光标归属（由编辑器注入：状态层不直接依赖 WPF 控件）。</summary>
        public Func<int> CaretOffsetProvider { get; set; }

        /// <summary>写回落地后把光标放回去（按这次写回真正动过的那一段做一次算术映射）。</summary>
        public Action<int> CaretOffsetRestorer { get; set; }

        private bool writing;
        private string pendingText;

        public McdocJsonValue Root { get; private set; } = McdocJsonValue.Null();

        /// <summary>产生当前 <see cref="Root"/> 的那份文本（精确字符串）。</summary>
        public string RootText { get; private set; } = "";

        /// <summary>
        /// 同代断言：AST 与编辑器文本必须是同一代。任何"重解析"都只能读 <see cref="Document"/> 的当前文本，
        /// 不许接受外部传入的旧快照（否则 AST 会退到上一代、文本却还是新的）。
        /// </summary>
        public bool IsInStepWithDocument
            => string.Equals(RootText, document.Text, StringComparison.Ordinal);

        /// <summary>文本↔AST 的"代际"：每次重解析或整篇写回自增（守卫据此跳过无意义的走查）。</summary>
        public int Generation { get; private set; }

        /// <summary>
        /// 区间表（诊断视图）。真相是锚点：文本一动 TextAnchor 自己跟着走，这里只是按需折算出来给门禁/诊断看，
        /// 热路径不许查它（每次访问都是 O(节点)）。
        /// </summary>
        public IReadOnlyDictionary<McdocJsonValue, McdocJsonSpan> Spans
        {
            get
            {
                var snapshot = new Dictionary<McdocJsonValue, McdocJsonSpan>(anchors.Count);
                foreach (var pair in anchors)
                {
                    if (pair.Value is not { IsValid: true } anchor) continue;
                    snapshot[pair.Key] = new McdocJsonSpan(anchor.ValueStart.Offset, anchor.ValueEnd.Offset,
                        anchor.PairStart.Offset, anchor.PairEnd.Offset);
                }
                return snapshot;
            }
        }

        public IReadOnlyDictionary<McdocJsonValue, JsonNodeAnchors> Anchors => anchors;

        /// <summary>
        /// 重新解析全文并重建锚点。**只从 <see cref="Document"/> 的当前文本重解析**（外部改过文本、或整体重排之后）。
        /// </summary>
        public void Reparse()
        {
            var parsed = McdocJsonTextParser.Parse(document.Text);
            RootText = document.Text;
            Generation++;
            Root = parsed.Root;
            anchorsTrusted = true;
            anchors.Clear();
            foreach (var pair in parsed.Spans)
            {
                anchors[pair.Key] = CreateNodeAnchors(pair.Value);
            }
        }

        /// <summary>
        /// 增量重解析：只重解析"改动落在其中"的顶层成员，其余成员的 AST 对象原样复用（身份不变）。
        /// 返回 false = 这次改动不归增量管（跨成员 / 触及顶层分隔符 / 根换了形状），调用方必须走 <see cref="Reparse"/>。
        /// 与 <see cref="Reparse"/> 同一条同代契约：唯一输入是 <see cref="Document"/> 的当前文本。
        /// </summary>
        public bool TryIncrementalReparse(string text)
        {
            if (text is null) return false;
            // 同代契约：只认 Document 的当前文本；外部旧快照会让 AST 退代
            if (!string.Equals(text, document.Text, StringComparison.Ordinal)) return false;
            var oldText = RootText ?? "";
            if (string.Equals(text, oldText, StringComparison.Ordinal)) return true;

            var root = Root;
            var isObject = root is not null && root.Kind == McdocJsonKind.Object;
            var isArray = root is not null && root.Kind == McdocJsonKind.Array;
            if (!isObject && !isArray) return false;

            var members = new List<McdocJsonValue>(isObject ? root.Properties?.Count ?? 0 : root.Items?.Count ?? 0);
            if (isObject)
            {
                foreach (var pair in root.Properties ?? []) members.Add(pair.Value);
            }
            else
            {
                foreach (var item in root.Items ?? []) members.Add(item);
            }
            if (members.Count == 0) return false;
            if (!TryGetSpan(root, out var rootSpan)) return false;
            foreach (var member in members)
            {
                // 没有区间就拼不出新根：宁可回落整篇
                if (member is null || !TryGetSpan(member, out _)) return false;
            }

            // 公共前后缀 → 旧脏区间 [prefix, oldDirtyEnd)；改动落在单个成员内部才有增量可言
            var oldLength = oldText.Length;
            var newLength = text.Length;
            var shared = Math.Min(oldLength, newLength);
            var prefix = 0;
            while (prefix < shared && oldText[prefix] == text[prefix]) prefix++;
            var suffix = 0;
            while (suffix < shared - prefix && oldText[oldLength - 1 - suffix] == text[newLength - 1 - suffix]) suffix++;
            var oldDirtyEnd = oldLength - suffix;
            var delta = newLength - oldLength;

            var first = -1;
            var last = -1;
            for (var index = 0; index < members.Count; index++)
            {
                if (!TryGetSpan(members[index], out var span)) return false;
                // 空脏区间（纯插入）没有宽度可交，只能看插入点是否落在成员内部
                var hit = prefix < oldDirtyEnd
                    ? span.PairStart < oldDirtyEnd && prefix < span.PairEnd
                    : span.PairStart < prefix && prefix < span.PairEnd;
                if (!hit) continue;
                if (first < 0) first = index;
                last = index;
            }
            if (first < 0) return false;

            if (!TryGetSpan(members[first], out var firstSpan) || !TryGetSpan(members[last], out var lastSpan)) return false;
            var runStart = firstSpan.PairStart;
            var runEnd = lastSpan.PairEnd;
            // 脏区间出了成员段（或段本身自相矛盾）→ 边界要重算，增量拼接必然错位
            if (prefix < runStart || oldDirtyEnd > runEnd || runEnd > oldLength) return false;
            if (TouchesTopLevelSeparator(oldText, runStart, prefix, oldDirtyEnd)) return false;

            var newRunEnd = runEnd + delta;
            if (newRunEnd < runStart || newRunEnd > newLength) return false;
            var runLength = last - first + 1;
            // 复用既有解析器：片段补上容器外壳就能整段解析，多出的一个字符由 pairStart-1 抵消
            var wrapped = (isObject ? "{" : "[") + text.Substring(runStart, newRunEnd - runStart) + (isObject ? "}" : "]");
            var fragment = McdocJsonTextParser.Parse(wrapped);
            var fragmentRoot = fragment.Root;
            if (fragmentRoot is null || fragmentRoot.Kind != (isObject ? McdocJsonKind.Object : McdocJsonKind.Array)) return false;
            // 解析器很宽容（非法内容也能吐出一个值）；只有吃掉整段外壳才算语法成立
            if (!fragment.Spans.TryGetValue(fragmentRoot, out var fragmentSpan)
                || fragmentSpan.ValueStart != 0 || fragmentSpan.ValueEnd != wrapped.Length) return false;
            var parsedCount = isObject ? fragmentRoot.Properties?.Count ?? 0 : fragmentRoot.Items?.Count ?? 0;
            // 成员数会变（增删成员）就不是"改一个值"，回落整篇
            if (parsedCount != runLength) return false;

            var newRoot = isObject ? McdocJsonValue.Object() : McdocJsonValue.Array();
            var expectedKeys = new List<string>(members.Count);
            for (var index = 0; index < first; index++) AppendMember(newRoot, isObject, root, index, members[index], expectedKeys);
            for (var offset = 0; offset < parsedCount; offset++)
            {
                if (isObject)
                {
                    var pair = fragmentRoot.Properties[offset];
                    newRoot.Properties.Add(new KeyValuePair<string, McdocJsonValue>(pair.Key, pair.Value));
                    expectedKeys.Add(pair.Key);
                }
                else
                {
                    newRoot.Items.Add(fragmentRoot.Items[offset]);
                }
            }
            for (var index = last + 1; index < members.Count; index++) AppendMember(newRoot, isObject, root, index, members[index], expectedKeys);

            // 质量守卫：拼出来的键序列必须与"旧键 + 受影响成员解析结果"逐一相等（拼错一位就会在树里显形）
            if (isObject)
            {
                if (newRoot.Properties.Count != expectedKeys.Count) return false;
                for (var index = 0; index < expectedKeys.Count; index++)
                {
                    if (!string.Equals(newRoot.Properties[index].Key, expectedKeys[index], StringComparison.Ordinal)) return false;
                }
            }
            else if (newRoot.Items.Count != members.Count)
            {
                return false;
            }

            // 以下开始改状态：上面的判据全部成立才允许落地
            for (var index = first; index <= last; index++) DropSubtree(members[index]);
            if (delta != 0)
            {
                for (var index = last + 1; index < members.Count; index++) ShiftSubtree(members[index], delta, null);
            }
            var shift = runStart - 1;
            if (shift != 0)
            {
                if (isObject)
                {
                    foreach (var pair in fragmentRoot.Properties ?? []) ShiftSubtree(pair.Value, shift, fragment.Spans);
                }
                else
                {
                    foreach (var item in fragmentRoot.Items ?? []) ShiftSubtree(item, shift, fragment.Spans);
                }
            }
            foreach (var pair in fragment.Spans)
            {
                if (ReferenceEquals(pair.Key, fragmentRoot)) continue;
                anchors[pair.Key] = CreateNodeAnchors(pair.Value);
            }

            anchors.Remove(root);
            var newRootSpan = new McdocJsonSpan(rootSpan.ValueStart, rootSpan.ValueEnd + delta, rootSpan.PairStart, rootSpan.PairEnd + delta);
            newRoot.Start = newRootSpan.ValueStart;
            newRoot.End = newRootSpan.ValueEnd;
            anchors[newRoot] = CreateNodeAnchors(newRootSpan);

            RootText = text;
            Generation++;
            Root = newRoot;
            return true;
        }

        private static void AppendMember(McdocJsonValue target, bool isObject, McdocJsonValue oldRoot, int index,
            McdocJsonValue value, List<string> keys)
        {
            if (!isObject)
            {
                target.Items.Add(value);
                return;
            }
            var key = oldRoot.Properties[index].Key;
            target.Properties.Add(new KeyValuePair<string, McdocJsonValue>(key, value));
            keys.Add(key);
        }

        /// <summary>顶层逗号/括号被碰过就必须重算成员边界；这里只在脏区间上判，避免整篇走查。</summary>
        private static bool TouchesTopLevelSeparator(string text, int runStart, int from, int to)
        {
            var depth = 0;
            var inString = false;
            for (var index = runStart; index < to && index < text.Length; index++)
            {
                var current = text[index];
                if (inString)
                {
                    if (current == '\\') { index++; continue; }
                    if (current == '"') inString = false;
                    continue;
                }
                if (current == '"') { inString = true; continue; }
                if (current == '{' || current == '[') { depth++; continue; }
                if (current == '}' || current == ']')
                {
                    depth--;
                    if (depth < 0) return true;
                    continue;
                }
                if (current == ',' && depth <= 0 && index >= from) return true;
            }
            return false;
        }

        /// <summary>被重解析的旧成员整棵子树要从登记表里摘掉，否则旧对象会把区间表越撑越大。</summary>
        private void DropSubtree(McdocJsonValue node)
        {
            if (node is null) return;
            anchors.Remove(node);
            if (node.Kind == McdocJsonKind.Object)
            {
                foreach (var pair in node.Properties ?? []) DropSubtree(pair.Value);
            }
            else if (node.Kind == McdocJsonKind.Array)
            {
                foreach (var item in node.Items ?? []) DropSubtree(item);
            }
        }

        /// <summary>复用的子树随文本平移：节点自身的 Start/End 与各层区间必须一起挪，否则锚点与树对不上。</summary>
        private static void ShiftSubtree(McdocJsonValue node, int delta, IReadOnlyDictionary<McdocJsonValue, McdocJsonSpan> source)
        {
            if (node is null) return;
            if (node.Start >= 0) node.Start += delta;
            if (node.End >= 0) node.End += delta;
            if (source is not null && source.TryGetValue(node, out var span))
            {
                if (source is Dictionary<McdocJsonValue, McdocJsonSpan> mutable)
                {
                    mutable[node] = new McdocJsonSpan(span.ValueStart + delta, span.ValueEnd + delta,
                        span.PairStart + delta, span.PairEnd + delta);
                }
            }
            if (node.Kind == McdocJsonKind.Object)
            {
                foreach (var pair in node.Properties ?? []) ShiftSubtree(pair.Value, delta, source);
            }
            else if (node.Kind == McdocJsonKind.Array)
            {
                foreach (var item in node.Items ?? []) ShiftSubtree(item, delta, source);
            }
        }

        /// <summary>
        /// 锚点就是唯一坐标：文本怎么动，TextAnchor 自己跟着走，没有第二张表要跟它对账。
        /// 只有四种情形会让锚点失效 —— 节点被重解析换掉、被整篇重建清掉、或锚点本身被删除，
        /// 这些出口都在本类里，登记与注销成对，所以这里只验一次 <see cref="JsonNodeAnchors.IsValid"/>。
        /// </summary>
        private bool TryGetAnchors(McdocJsonValue node, out JsonNodeAnchors anchor)
        {
            anchor = null;
            if (!anchorsTrusted) return false;
            if (node is null || !anchors.TryGetValue(node, out anchor) || anchor is null) return false;
            if (anchor.IsValid) return true;
            anchors.Remove(node);
            anchor = null;
            return false;
        }

        /// <summary>按锚点折算某个节点当前的区间（诊断/增量解析用，热路径请直接用锚点）。</summary>
        private bool TryGetSpan(McdocJsonValue node, out McdocJsonSpan span)
        {
            span = default;
            if (!TryGetAnchors(node, out var anchor)) return false;
            span = new McdocJsonSpan(anchor.ValueStart.Offset, anchor.ValueEnd.Offset,
                anchor.PairStart.Offset, anchor.PairEnd.Offset);
            return true;
        }

        /// <summary>在指定偏移建一个光标锚点。带 SurviveDeletion：被删除时只看 IsDeleted，不抛异常。</summary>
        public TextAnchor CreateCaretAnchor(int offset)
        {
            var anchor = CreateAnchorAt(offset);
            anchor.SurviveDeletion = true;
            return anchor;
        }

        /// <summary>
        /// 按 JSON 路径（<c>$.a.b[0]</c>）在当前 AST 里找节点。
        /// 树视图节点带着自己的路径，回写时靠它在（编辑器自己解析出的）AST 里定位同一个位置。
        /// </summary>
        public McdocJsonValue FindByPath(string path)
        {
            if (string.IsNullOrEmpty(path) || path[0] != '$') return null;
            var current = Root;
            var index = 1;
            while (index < path.Length && current is not null)
            {
                if (path[index] == '.')
                {
                    index++;
                    var start = index;
                    while (index < path.Length && path[index] != '.' && path[index] != '[') index++;
                    current = current.Get(path.Substring(start, index - start));
                }
                else if (path[index] == '[')
                {
                    var close = path.IndexOf(']', index);
                    if (close < 0) return null;
                    if (!int.TryParse(path.Substring(index + 1, close - index - 1), out var item)) return null;
                    current = current.Items is not null && item >= 0 && item < current.Items.Count ? current.Items[item] : null;
                    index = close + 1;
                }
                else
                {
                    index++;
                }
            }
            return current;
        }

        public bool TryGetCurrentSpan(McdocJsonValue node, out int start, out int length)
        {
            start = 0;
            length = 0;
            if (!TryGetAnchors(node, out var anchor)) return false;
            start = anchor.ValueStart.Offset;
            length = Math.Max(0, anchor.ValueEnd.Offset - anchor.ValueStart.Offset);
            return true;
        }

        /// <summary>取得某个节点当前的文本（读自锚点）。</summary>
        public string GetCurrentText(McdocJsonValue node)
            => TryGetCurrentSpan(node, out var start, out var length) ? document.Text.Substring(start, length) : null;

        /// <summary>
        /// 只替换某个值的文本。标量走就地替换（滚动/撤销粒度不被破坏，其它锚点自动平移）；
        /// 容器替换会改变排版，改为改 AST 后整体重排。
        /// </summary>
        public void ReplaceValue(McdocJsonValue node, McdocJsonValue newValue)
        {
            if (node is null || newValue is null || writing) return;
            var parent = FindParent(Root, node);
            if (parent is null) return;   // 根节点整体替换请直接 Reload
            ReplaceInParent(parent, node, newValue);
            // AST 里换成了新对象：锚点/区间登记要跟着搬过去，否则下次按 Root 取到的新节点查不到
            MigrateRegistration(node, newValue);

            var isContainer = newValue.Kind is McdocJsonKind.Object or McdocJsonKind.Array;
            if (isContainer || !TryGetCurrentSpan(newValue, out var start, out var length))
            {
                Reformat();
                return;
            }

            var text = McdocJsonFormatter.Format(newValue);
            if (text.Contains('\n'))
            {
                Reformat();
                return;
            }

            document.Replace(start, length, text);
            // 只给"被替换的节点"重建锚点（TextAnchor.Offset 只读）；其余锚点由 AvalonEdit 自己平移
            anchors[newValue] = CreateNodeAnchors(start, start + text.Length, start, start + text.Length);
        }

        /// <summary>删除整个属性（键 + 冒号 + 值），并吃掉多余的逗号。</summary>
        public void RemoveProperty(McdocJsonValue node)
        {
            if (node is null || writing) return;
            var parent = FindParent(Root, node);
            if (parent is null || parent.Kind != McdocJsonKind.Object || !TryGetAnchors(node, out var anchor)) return;

            var start = anchor.PairStart.Offset;
            var end = anchor.PairEnd.Offset;
            parent.Properties?.RemoveAll(pair => ReferenceEquals(pair.Value, node));

            var index = document.Text.IndexOf(',', end);
            if (index >= 0 && index - end <= 8)
            {
                document.Remove(start, index - start + 1);
            }
            else
            {
                var comma = document.Text.LastIndexOf(',', Math.Max(0, start - 1));
                if (comma >= 0) document.Remove(comma, end - comma);
                else document.Remove(start, end - start);
            }
            Reformat();
        }

        /// <summary>
        /// 整体重排（spyglass 风格：2 空格、按树里的键序）。
        /// 不换 Root 身份，也不整篇替换文本：目标区间表在同一遍格式化里算出来，写回只动真正变过的那一段，
        /// 其余文本与锚点原地不动（TextAnchor 自己平移，不需要写完整篇再逐节点重指）。
        /// </summary>
        public void Reformat(int indentSize = 2)
        {
            var targetSpans = new Dictionary<McdocJsonValue, McdocJsonSpan>();
            var text = McdocJsonFormatter.Format(Root, indentSize, targetSpans);
            ApplySplice(text, targetSpans, adoptRoot: null);
        }

        /// <summary>换一代文本：落地后重解析，Root 换成文本里的那棵树（物理写回仍然只动变过的那一段）。</summary>
        public void Reload(string text) => WriteWholeText(text);

        /// <summary>
        /// 定向写回（模型 → 编辑器）：算出当前文本与目标文本的公共前后缀，只替换中间那一段
        /// （一次值编辑通常只有几个字符）。其余文本原封不动 —— TextAnchor 自动平移、语法波浪线不断、
        /// 光标一次算术落位，不需要"整篇替换 + 全文重解析 + 逐节点重指"。
        /// <paramref name="targetSpans"/> 必须来自 <see cref="McdocJsonFormatter.Format(McdocJsonValue, int, IDictionary{McdocJsonValue, McdocJsonSpan})"/>，
        /// 且键与 <see cref="Root"/> 是同一批节点对象。
        /// </summary>
        public bool SpliceModelText(string text, IDictionary<McdocJsonValue, McdocJsonSpan> targetSpans)
            => ApplySplice(text, targetSpans, adoptRoot: null);

        /// <summary>
        /// 编辑器文本已经不代表模型这一代时（根换支 / 外部改文本后被模型顶回）：写同步 + 把 Root 直接指向模型，
        /// 锚点按目标区间整体重建。比 Reload 少一次全文解析，也不会留下"状态的树"和"模型的树"两套对象。
        /// </summary>
        public bool AdoptModel(McdocJsonValue root, string text, IDictionary<McdocJsonValue, McdocJsonSpan> targetSpans)
            => ApplySplice(text, targetSpans, adoptRoot: root);

        private bool ApplySplice(string text, IDictionary<McdocJsonValue, McdocJsonSpan> targetSpans, McdocJsonValue adoptRoot)
        {
            var target = text ?? "";
            if (writing)
            {
                McdocWriteTrace.Log("定向写回跳过", null, "写回进行中，目标长度 " + target.Length);
                return false;
            }
            var caret = CaretOffsetProvider?.Invoke() ?? -1;
            var (start, removed, inserted) = ReplaceRegion(target);
            SpliceCount++;
            LastSpliceLength = removed + inserted;
            // 换根、或锚点已经不认（整篇被外部拍平过）：目标区间就是全文，整表按它重建
            if (adoptRoot is not null || !anchorsTrusted)
            {
                if (adoptRoot is not null) Root = adoptRoot;
                anchors.Clear();
                foreach (var pair in targetSpans ?? new Dictionary<McdocJsonValue, McdocJsonSpan>())
                {
                    anchors[pair.Key] = CreateNodeAnchors(pair.Value);
                }
                anchorsTrusted = true;
            }
            else
            {
                ReanchorSplicedRegion(targetSpans, start, start + removed, start + inserted);
            }
            RootText = document.Text;
            Generation++;
            RestoreSplicedCaret(caret, start, removed, inserted);
            McdocWriteTrace.Log("定向写回", null, "起=" + start + " 删=" + removed + " 增=" + inserted
                + " 新长=" + target.Length + (adoptRoot is null ? "" : " 换根"));
            return string.Equals(document.Text, target, StringComparison.Ordinal);
        }

        /// <summary>
        /// 物理写回的唯一原语（定向写回 / 整篇接管共用）：只替换公共前后缀之间那一段，
        /// 返回"这次动过的区间"。文本树保持存活，区间外的锚点由 AvalonEdit 自己平移。
        /// </summary>
        private (int Start, int Removed, int Inserted) ReplaceRegion(string target)
        {
            var current = document.Text ?? "";
            var start = CommonPrefixLength(current, target);
            var suffix = CommonSuffixLength(current, target, start);
            var removed = current.Length - start - suffix;
            var inserted = target.Length - start - suffix;
            if (removed > 0 || inserted > 0)
            {
                var wasWriting = writing;
                writing = true;
                try
                {
                    document.Replace(start, removed, target.Substring(start, inserted));
                }
                finally
                {
                    writing = wasWriting;
                }
            }
            return (start, removed, inserted);
        }

        /// <summary>写回落地后把光标放回去：同一次写回只有一段脏区间，一次算术足够（不需要节点级别二次定位）。</summary>
        private void RestoreSplicedCaret(int caret, int start, int removed, int inserted)
        {
            if (caret < 0 || CaretOffsetRestorer is null) return;
            CaretOffsetRestorer(MapSplicedCaret(caret, start, removed, inserted, document.TextLength));
        }

        /// <summary>
        /// 只重建"落在被替换区间里"的锚点：区间外的锚点由 TextDocument 自动平移，一个都不碰。
        /// 用闭区间判重叠 —— 纯插入时，正好停在插入点上的那个值尾锚点不会自己往后走
        /// （AnchorMovementType.BeforeInsertion），必须按目标区间重建。
        /// </summary>
        private void ReanchorSplicedRegion(IDictionary<McdocJsonValue, McdocJsonSpan> targetSpans,
            int oldStart, int oldEnd, int newEnd)
        {
            if (targetSpans is null) return;
            List<McdocJsonValue> stale = null;
            foreach (var pair in anchors)
            {
                var anchor = pair.Value;
                if (anchor is null || !anchor.IsValid) continue;
                if (anchor.ValueEnd.Offset < oldStart || anchor.ValueStart.Offset > oldEnd) continue;
                if (!targetSpans.ContainsKey(pair.Key)) (stale ??= []).Add(pair.Key);
            }
            if (stale is not null)
            {
                foreach (var node in stale) anchors.Remove(node);
            }
            foreach (var pair in targetSpans)
            {
                if (pair.Value.ValueEnd < oldStart || pair.Value.ValueStart > newEnd) continue;
                anchors[pair.Key] = CreateNodeAnchors(pair.Value);
            }
        }

        private static int CommonPrefixLength(string left, string right)
        {
            var limit = Math.Min(left.Length, right.Length);
            var index = 0;
            while (index < limit && left[index] == right[index]) index++;
            return index;
        }

        private static int CommonSuffixLength(string left, string right, int prefix)
        {
            var limit = Math.Min(left.Length, right.Length) - prefix;
            var index = 0;
            while (index < limit && left[left.Length - 1 - index] == right[right.Length - 1 - index]) index++;
            return index;
        }

        private static int MapSplicedCaret(int caret, int start, int removed, int inserted, int length)
        {
            var mapped = caret <= start ? caret
                : caret >= start + removed ? caret + inserted - removed
                : start + Math.Min(caret - start, inserted);
            return Math.Max(0, Math.Min(mapped, length));
        }

        /// <summary>
        /// 整篇接管的唯一出口（Reload / 页面 Load 都走它）。三条约束：
        /// 写回不许嵌套（重入只记"最后一次"，由外层收尾时补写）；写完必须校验文档真的等于目标文本；
        /// 写回与重解析在同一次调用里完成，AST 与文本永远同代。
        /// 物理写回与定向写回共用 ReplaceRegion —— 只替换变过的那一段，不再 document.Replace(0, len, …)。
        /// </summary>
        public bool WriteWholeText(string text)
        {
            var value = text ?? "";
            var landed = true;
            if (writing)
            {
                // 重入：物理写回正在路上，边界已经过期。只记"最后一次"，由外层收尾时补写一次。
                pendingText = value;
                McdocWriteTrace.Log("整篇写回合并", null, "写回进行中，改记最后一次（目标长度 " + value.Length + "）");
                return false;
            }
            for (var pass = 0; pass < 4; pass++)
            {
                writing = true;
                try
                {
                    landed = ApplyWholeText(value);
                }
                finally
                {
                    writing = false;
                }
                if (pendingText is null) return landed;
                value = pendingText;
                pendingText = null;
            }
            McdocWriteTrace.Log("整篇写回未收敛", null, "连续 4 轮仍收到新的写回请求");
            return landed;
        }

        private bool ApplyWholeText(string value)
        {
            WholeWriteCount++;
            var oldLength = document.TextLength;
            LastWriteRepaired = false;
            var caret = CaretOffsetProvider?.Invoke() ?? -1;
            var (start, removed, inserted) = ReplaceRegion(value);
            var landed = string.Equals(document.Text, value, StringComparison.Ordinal);
            // 第二遍是为了"边界被写回事件里的另一次写回动过"：重算边界再写一次，不许残留上一次的内容
            for (var attempt = 0; !landed && attempt < 2; attempt++)
            {
                LastWriteRepaired = attempt > 0;
                (start, removed, inserted) = ReplaceRegion(value);
                landed = string.Equals(document.Text, value, StringComparison.Ordinal);
            }
            McdocWriteTrace.LogWholeWrite("Reload", oldLength, document.Text, LastWriteRepaired);
            Reparse();
            RestoreSplicedCaret(caret, start, removed, inserted);
            return landed;
        }

        /// <summary>一次"值编辑"在文本里的定位：父容器 + 下标 + 该值改前的区间。</summary>
        public readonly struct JsonMemberSlot
        {
            public readonly McdocJsonValue Parent;
            public readonly int Index;
            public readonly int Start;
            public readonly int Length;

            public JsonMemberSlot(McdocJsonValue parent, int index, int start, int length)
            {
                Parent = parent;
                Index = index;
                Start = start;
                Length = length;
            }

            public bool IsValid => Parent is not null && Index >= 0;
        }

        /// <summary>
        /// 在改 AST **之前**抓一次定位（父容器 + 下标 + 当前区间）。
        /// 值编辑的快路径靠它：改完只替换这一段文本，不整篇重排。
        /// </summary>
        public bool TryCaptureMember(McdocJsonValue value, out JsonMemberSlot slot)
        {
            slot = default;
            if (value is null || !TryGetCurrentSpan(value, out var start, out var length)) return false;
            var parent = FindParent(Root, value);
            if (parent is null) return false;
            var index = -1;
            if (parent.Kind == McdocJsonKind.Object && parent.Properties is not null)
            {
                for (var i = 0; i < parent.Properties.Count; i++)
                {
                    if (ReferenceEquals(parent.Properties[i].Value, value)) { index = i; break; }
                }
            }
            else if (parent.Kind == McdocJsonKind.Array && parent.Items is not null)
            {
                for (var i = 0; i < parent.Items.Count; i++)
                {
                    if (ReferenceEquals(parent.Items[i], value)) { index = i; break; }
                }
            }
            if (index < 0) return false;
            slot = new JsonMemberSlot(parent, index, start, length);
            return true;
        }

        /// <summary>
        /// 只把这一段的文本换掉（值编辑的快路径）。容器/多行 → false，调用方退回整篇重排。
        /// AST 的换位是幂等的：调用方通常已经写过一次（ApplyNodeValue），这里只在还没换时补一次。
        /// </summary>
        public bool TryRewriteMember(in JsonMemberSlot slot, McdocJsonValue newValue)
        {
            if (writing || newValue is null || !slot.IsValid) return false;
            if (newValue.Kind is McdocJsonKind.Object or McdocJsonKind.Array) return false;
            var text = McdocJsonFormatter.Format(newValue);
            if (text is null || text.Contains('\n')) return false;

            // 换下来的旧节点必须一起从登记表里摘掉：两张表的键是强引用，
            // 留在里面的旧 AST 会被一直吊着 —— 每改一次值就多一项（实测连改 200 次 +200 项）
            McdocJsonValue previous = null;
            if (slot.Parent.Kind == McdocJsonKind.Object && slot.Parent.Properties is not null
                && slot.Index < slot.Parent.Properties.Count)
            {
                var pair = slot.Parent.Properties[slot.Index];
                previous = pair.Value;
                if (!ReferenceEquals(pair.Value, newValue))
                {
                    slot.Parent.Properties[slot.Index] = new KeyValuePair<string, McdocJsonValue>(pair.Key, newValue);
                }
            }
            else if (slot.Parent.Kind == McdocJsonKind.Array && slot.Parent.Items is not null
                && slot.Index < slot.Parent.Items.Count)
            {
                previous = slot.Parent.Items[slot.Index];
                slot.Parent.Items[slot.Index] = newValue;
            }
            else
            {
                return false;
            }
            if (previous is not null && !ReferenceEquals(previous, newValue)) anchors.Remove(previous);

            var start = Math.Max(0, Math.Min(slot.Start, document.TextLength));
            var length = Math.Max(0, Math.Min(slot.Length, document.TextLength - start));
            document.Replace(start, length, text);
            anchors[newValue] = CreateNodeAnchors(start, start + text.Length, start, start + text.Length);
            // 文本与 AST 仍一一对应：同代断言要跟着更新（不重解析，Root 不变）
            Generation++;
            RootText = document.Text;
            return true;
        }

        /// <summary>把某个节点登记的锚点/区间搬到替换后的新节点上。</summary>
        private void MigrateRegistration(McdocJsonValue from, McdocJsonValue to)
        {
            if (ReferenceEquals(from, to)) return;
            if (anchors.Remove(from, out var anchor)) anchors[to] = anchor;
        }

        private JsonNodeAnchors CreateNodeAnchors(McdocJsonSpan span)
            => CreateNodeAnchors(span.ValueStart, span.ValueEnd, span.PairStart, span.PairEnd);

        private JsonNodeAnchors CreateNodeAnchors(int valueStart, int valueEnd, int pairStart, int pairEnd)
            => new()
            {
                ValueStart = CreateAnchorAt(valueStart),
                ValueEnd = CreateAnchorAt(valueEnd),
                PairStart = CreateAnchorAt(pairStart),
                PairEnd = CreateAnchorAt(pairEnd),
            };

        /// <summary>
        /// 四种锚点都跟文本一起长（Default）：这正是旧区间表那条 "offset >= 替换末 → 平移 delta" 的规则，
        /// 也是"在节点末尾插一个兄弟"时父级区间该跟着变宽的原因。光标锚点另走 <see cref="CreateCaretAnchor"/>（停在插入点之前）。
        /// </summary>
        private TextAnchor CreateAnchorAt(int offset)
        {
            var anchor = document.CreateAnchor(Math.Max(0, Math.Min(offset, document.TextLength)));
            anchor.MovementType = AnchorMovementType.Default;
            return anchor;
        }

        private static void ReplaceInParent(McdocJsonValue parent, McdocJsonValue oldValue, McdocJsonValue newValue)
        {
            if (parent is null) return;
            if (parent.Kind == McdocJsonKind.Object && parent.Properties is not null)
            {
                for (var i = 0; i < parent.Properties.Count; i++)
                {
                    if (!ReferenceEquals(parent.Properties[i].Value, oldValue)) continue;
                    parent.Properties[i] = new KeyValuePair<string, McdocJsonValue>(parent.Properties[i].Key, newValue);
                    return;
                }
            }
            if (parent.Kind == McdocJsonKind.Array && parent.Items is not null)
            {
                for (var i = 0; i < parent.Items.Count; i++)
                {
                    if (!ReferenceEquals(parent.Items[i], oldValue)) continue;
                    parent.Items[i] = newValue;
                    return;
                }
            }
        }

        private static McdocJsonValue FindParent(McdocJsonValue root, McdocJsonValue target)
        {
            if (root is null || ReferenceEquals(root, target)) return null;
            if (root.Kind == McdocJsonKind.Object)
            {
                foreach (var pair in root.Properties ?? [])
                {
                    if (ReferenceEquals(pair.Value, target)) return root;
                    var found = FindParent(pair.Value, target);
                    if (found is not null) return found;
                }
            }
            else if (root.Kind == McdocJsonKind.Array)
            {
                foreach (var item in root.Items ?? [])
                {
                    if (ReferenceEquals(item, target)) return root;
                    var found = FindParent(item, target);
                    if (found is not null) return found;
                }
            }
            return null;
        }
    }
}
