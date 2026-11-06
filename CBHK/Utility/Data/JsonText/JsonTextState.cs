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
        private readonly Dictionary<McdocJsonValue, McdocJsonSpan> spans = [];

        public JsonTextState(TextDocument document)
        {
            this.document = document ?? throw new ArgumentNullException(nameof(document));
            Reparse();
        }

        /// <summary>把状态挂到（很可能已经有内容的）文档上。</summary>
        public static JsonTextState Attach(TextDocument document) => new(document);

        public TextDocument Document => document;

        /// <summary>物理整篇写回次数（门禁/诊断用：同一拍出现两次就是"两个根"的现场）。</summary>
        public int WholeWriteCount { get; private set; }

        /// <summary>上一次写回的第一遍没落地、靠第二遍补齐（= 边界被别的写入动过）。</summary>
        public bool LastWriteRepaired { get; private set; }

        /// <summary>整篇写回前后的光标归属（由编辑器注入：状态层不直接依赖 WPF 控件）。</summary>
        public Func<int> CaretOffsetProvider { get; set; }

        /// <summary>写回落地后把光标放回去（按"原来那个 JSON 节点 + 相对偏移"映射，不是简单夹紧）。</summary>
        public Action<int> CaretOffsetRestorer { get; set; }

        private bool writing;
        private string pendingText;
        private bool pendingKeepRoot;

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

        public IReadOnlyDictionary<McdocJsonValue, McdocJsonSpan> Spans => spans;

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
            spans.Clear();
            anchors.Clear();
            foreach (var pair in parsed.Spans)
            {
                spans[pair.Key] = pair.Value;
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
            if (!spans.TryGetValue(root, out var rootSpan)) return false;
            foreach (var member in members)
            {
                // 没有区间就拼不出新根：宁可回落整篇
                if (member is null || !spans.ContainsKey(member)) return false;
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
                var span = spans[members[index]];
                // 空脏区间（纯插入）没有宽度可交，只能看插入点是否落在成员内部
                var hit = prefix < oldDirtyEnd
                    ? span.PairStart < oldDirtyEnd && prefix < span.PairEnd
                    : span.PairStart < prefix && prefix < span.PairEnd;
                if (!hit) continue;
                if (first < 0) first = index;
                last = index;
            }
            if (first < 0) return false;

            var runStart = spans[members[first]].PairStart;
            var runEnd = spans[members[last]].PairEnd;
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
                for (var index = last + 1; index < members.Count; index++) ShiftSubtree(members[index], delta, spans);
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
                spans[pair.Key] = pair.Value;
                anchors[pair.Key] = CreateNodeAnchors(pair.Value);
            }

            spans.Remove(root);
            anchors.Remove(root);
            var newRootSpan = new McdocJsonSpan(rootSpan.ValueStart, rootSpan.ValueEnd + delta, rootSpan.PairStart, rootSpan.PairEnd + delta);
            spans[newRoot] = newRootSpan;
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
            spans.Remove(node);
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
        private static void ShiftSubtree(McdocJsonValue node, int delta, Dictionary<McdocJsonValue, McdocJsonSpan> source)
        {
            if (node is null) return;
            if (node.Start >= 0) node.Start += delta;
            if (node.End >= 0) node.End += delta;
            if (source.TryGetValue(node, out var span))
            {
                source[node] = new McdocJsonSpan(span.ValueStart + delta, span.ValueEnd + delta,
                    span.PairStart + delta, span.PairEnd + delta);
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
        /// 锚点只是区间表的缓存：文本被整篇替换后旧 TextAnchor 会被打散，所以读的时候按 spans 校验一次，
        /// 不在了就现建——增量提交因此不必替整棵树重建锚点（那是这条路径上唯一的 O(节点) 硬开销）。
        /// </summary>
        private bool TryUseAnchors(McdocJsonValue node, out JsonNodeAnchors anchor)
        {
            anchor = null;
            if (node is null || !spans.TryGetValue(node, out var span)) return false;
            if (anchors.TryGetValue(node, out anchor) && IsAligned(anchor, span)) return true;
            anchor = CreateNodeAnchors(span);
            anchors[node] = anchor;
            return true;
        }

        /// <summary>旧锚点（整篇替换后多半已被打散）正好落在目标区间上才算可复用，否则读到的偏移是假的。</summary>
        private static bool IsAligned(JsonNodeAnchors anchor, McdocJsonSpan span)
            => anchor is not null
               && anchor.ValueStart is { IsDeleted: false }
               && anchor.ValueEnd is { IsDeleted: false }
               && anchor.PairStart is { IsDeleted: false }
               && anchor.PairEnd is { IsDeleted: false }
               && anchor.ValueStart.Offset == span.ValueStart
               && anchor.ValueEnd.Offset == span.ValueEnd
               && anchor.PairStart.Offset == span.PairStart
               && anchor.PairEnd.Offset == span.PairEnd;

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
            if (!TryUseAnchors(node, out var anchor)) return false;
            start = anchor.ValueStart.Offset;
            length = Math.Max(0, anchor.ValueEnd.Offset - anchor.ValueStart.Offset);
            return true;
        }

        /// <summary>
        /// 就地改文本之后，把区间表整体挪一下。区间表是锚点的"真相"（<see cref="TryUseAnchors"/> 拿它对账）：
        /// 只改文本不改它，下一次读锚点就会按**旧位置**现建，编辑选区整体偏 delta 位 ——
        /// 表现是"上一处改完，下一处改的时候啃掉/残留一个字符"（ffalse / falsee）。
        /// </summary>
        private void ShiftSpans(int replacedStart, int replacedEnd, int delta)
        {
            if (delta == 0) return;
            foreach (var key in spans.Keys.ToList())
            {
                var span = spans[key];
                spans[key] = new McdocJsonSpan(
                    Shift(span.ValueStart), Shift(span.ValueEnd), Shift(span.PairStart), Shift(span.PairEnd));
            }
            return;
            int Shift(int offset) => offset >= replacedEnd ? offset + delta : offset;
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
            // 只给"被替换的节点"重建锚点（TextAnchor.Offset 只读）；其它锚点由 AvalonEdit 自动平移，
            // 但**区间表不会自己挪**，必须跟着走，否则下一次读锚点会按旧位置现建
            ShiftSpans(start, start + length, text.Length - length);
            spans[newValue] = new McdocJsonSpan(start, start + text.Length, start, start + text.Length);
            anchors[newValue] = CreateNodeAnchors(spans[newValue]);
        }

        /// <summary>删除整个属性（键 + 冒号 + 值），并吃掉多余的逗号。</summary>
        public void RemoveProperty(McdocJsonValue node)
        {
            if (node is null || writing) return;
            var parent = FindParent(Root, node);
            if (parent is null || parent.Kind != McdocJsonKind.Object || !TryUseAnchors(node, out var anchor)) return;

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
        /// 重排后**只重建锚点，不换 Root 身份**：树节点持有的 JsonValue 必须与编辑器一直是同一批对象，
        /// 否则树上改的是一棵树、Reformat 输出的是另一棵树的旧内容（真 bug：页面里改值 JSON 不动，
        /// 再敲一次字就把这次编辑丢掉）。新解析只用来提供偏移量，按结构逐节点映射回现有 Root。
        /// </summary>
        public void Reformat(int indentSize = 2)
            => WriteWholeText(McdocJsonFormatter.Format(Root, indentSize), keepRootIdentity: true);

        /// <summary>换一代文本：落地的同时重解析，Root 换成文本里的那棵树。</summary>
        public void Reload(string text) => WriteWholeText(text, keepRootIdentity: false);

        /// <summary>
        /// 整篇写回的唯一出口（Reformat / Reload / 编辑器 Load 都走它）。三条约束：
        /// 写回不许嵌套（重入只记"最后一次"，由外层收尾时补写）；写完必须校验文档真的等于目标文本；
        /// 写回与锚点重建在同一次调用里完成，AST 与文本永远同代。
        /// </summary>
        public bool WriteWholeText(string text, bool keepRootIdentity)
        {
            var value = text ?? "";
            var keepRoot = keepRootIdentity;
            var landed = true;
            if (writing)
            {
                // 重入：物理写回正在路上，边界已经过期。只记"最后一次"，由外层收尾时补写一次。
                if (pendingText is not null && !pendingKeepRoot && keepRootIdentity)
                {
                    // 排队中的 Reload 会换掉 AST：之后用旧 Root 算出的重排快照必须丢弃，否则会把新文档顶回旧内容
                    McdocWriteTrace.Log("整篇写回合并", null, "Reformat 快照早于排队中的 Reload，丢弃");
                    return false;
                }
                pendingText = value;
                pendingKeepRoot = keepRootIdentity;
                McdocWriteTrace.Log("整篇写回合并", null, "写回进行中，改记最后一次（目标长度 " + value.Length + "）");
                return false;
            }
            for (var pass = 0; pass < 4; pass++)
            {
                writing = true;
                try
                {
                    landed = ApplyWholeText(value, keepRoot);
                }
                finally
                {
                    writing = false;
                }
                if (pendingText is null) return landed;
                value = pendingText;
                keepRoot = pendingKeepRoot;
                pendingText = null;
            }
            McdocWriteTrace.Log("整篇写回未收敛", null, "连续 4 轮仍收到新的写回请求");
            return landed;
        }

        private bool ApplyWholeText(string value, bool keepRootIdentity)
        {
            WholeWriteCount++;
            var oldLength = document.TextLength;
            LastWriteRepaired = false;
            // 光标先定位到"原来那个节点"，写回（排版全变）之后再放回去：丢了光标，用户接着敲的字会落到根外面
            var caret = CaretOffsetProvider?.Invoke() ?? -1;
            McdocJsonValue caretHost = null;
            var caretDelta = 0;
            if (caret >= 0) TryResolveCaretHost(caret, out caretHost, out caretDelta);
            var landed = string.Equals(document.Text, value, StringComparison.Ordinal);
            // 第二遍是为了"边界被写回事件里的另一次写回动过"：重算边界再写一次，不许残留上一次的内容
            for (var attempt = 0; !landed && attempt < 2; attempt++)
            {
                LastWriteRepaired = attempt > 0;
                var length = document.TextLength;
                if (length > 0) document.Replace(0, length, value);
                else if (value.Length > 0) document.Insert(0, value);
                landed = string.Equals(document.Text, value, StringComparison.Ordinal);
            }
            McdocWriteTrace.LogWholeWrite(keepRootIdentity ? "Reformat" : "Reload",
                oldLength, document.Text, keepRootIdentity, LastWriteRepaired);
            if (keepRootIdentity)
            {
                var parsed = McdocJsonTextParser.Parse(document.Text);
                var parsedSpans = new Dictionary<McdocJsonValue, McdocJsonSpan>();
                foreach (var pair in parsed.Spans) parsedSpans[pair.Key] = pair.Value;
                spans.Clear();
                anchors.Clear();
                MirrorRegistration(Root, parsed.Root, parsedSpans);
                // 重排保 AST 身份：文本已经换成"这一代"的排版，RootText 跟着更新，同代断言继续成立
                RootText = document.Text;
                Generation++;
            }
            else
            {
                Reparse();
            }
            if (caret >= 0 && CaretOffsetRestorer is not null)
            {
                CaretOffsetRestorer(MapCaretOffset(caretHost, caretDelta, caret));
            }
            return landed;
        }

        /// <summary>光标所在的"最内层节点"：用它的区间当锚，写回后同一节点（保身份时）还有新区间可用。</summary>
        private void TryResolveCaretHost(int offset, out McdocJsonValue host, out int delta)
        {
            host = null;
            delta = 0;
            var bestStart = int.MinValue;
            // 走区间表而不是锚点表：锚点可能被整篇替换打散，区间表永远反映当前文本
            foreach (var pair in spans)
            {
                var span = pair.Value;
                if (offset < span.ValueStart || offset > span.ValueEnd || span.ValueStart < bestStart) continue;
                bestStart = span.ValueStart;
                host = pair.Key;
                delta = offset - span.ValueStart;
            }
        }

        /// <summary>旧偏移 → 新偏移：节点还在就按"节点新区间 + 相对偏移"落点，否则退化成夹紧。</summary>
        private int MapCaretOffset(McdocJsonValue host, int delta, int fallback)
        {
            var length = document.TextLength;
            if (host is null || !TryUseAnchors(host, out var anchor) || anchor is null)
            {
                var clamped = Math.Max(0, Math.Min(fallback, length));
                McdocWriteTrace.Log("光标映射（退化夹紧）", null, "caret=" + fallback + " host=" + (host?.Kind.ToString() ?? "<无>")
                    + " 命中=" + (host is not null) + " mapped=" + clamped + " 文本长度=" + length);
                return clamped;
            }
            var start = anchor.ValueStart.Offset;
            var end = anchor.ValueEnd.Offset;
            var mapped = Math.Max(0, Math.Min(start + delta, Math.Max(start, end)));
            McdocWriteTrace.Log("光标映射", null, "caret=" + fallback + " host=" + host.Kind + " delta=" + delta
                + " 新区间=" + start + "-" + end + " mapped=" + mapped);
            return mapped;
        }

        /// <summary>把"刚解析出的文本"上的区间搬到现有 AST 的同一个节点上（结构一致时逐节点对齐）。</summary>
        private void MirrorRegistration(McdocJsonValue current, McdocJsonValue parsed,
            Dictionary<McdocJsonValue, McdocJsonSpan> parsedSpans)
        {
            if (current is null || parsed is null) return;
            if (parsedSpans.TryGetValue(parsed, out var span))
            {
                spans[current] = span;
                anchors[current] = CreateNodeAnchors(span);
            }
            if (current.Kind == McdocJsonKind.Object && parsed.Kind == McdocJsonKind.Object)
            {
                var mine = current.Properties ?? [];
                var theirs = parsed.Properties ?? [];
                for (var i = 0; i < mine.Count && i < theirs.Count; i++)
                {
                    MirrorRegistration(mine[i].Value, theirs[i].Value, parsedSpans);
                }
            }
            else if (current.Kind == McdocJsonKind.Array && parsed.Kind == McdocJsonKind.Array)
            {
                var mine = current.Items ?? [];
                var theirs = parsed.Items ?? [];
                for (var i = 0; i < mine.Count && i < theirs.Count; i++)
                {
                    MirrorRegistration(mine[i], theirs[i], parsedSpans);
                }
            }
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
            if (previous is not null && !ReferenceEquals(previous, newValue))
            {
                spans.Remove(previous);
                anchors.Remove(previous);
            }

            var start = Math.Max(0, Math.Min(slot.Start, document.TextLength));
            var length = Math.Max(0, Math.Min(slot.Length, document.TextLength - start));
            document.Replace(start, length, text);
            ShiftSpans(start, start + length, text.Length - length);
            spans[newValue] = new McdocJsonSpan(start, start + text.Length, start, start + text.Length);
            anchors[newValue] = CreateNodeAnchors(spans[newValue]);
            // 文本与 AST 仍一一对应：同代断言要跟着更新（不重解析，Root 不变）
            Generation++;
            RootText = document.Text;
            return true;
        }

        /// <summary>把某个节点登记的锚点/区间搬到替换后的新节点上。</summary>
        private void MigrateRegistration(McdocJsonValue from, McdocJsonValue to)
        {
            if (ReferenceEquals(from, to)) return;
            if (spans.Remove(from, out var span)) spans[to] = span;
            if (anchors.Remove(from, out var anchor)) anchors[to] = anchor;
        }

        private JsonNodeAnchors CreateNodeAnchors(McdocJsonSpan span)
            => new()
            {
                ValueStart = CreateAnchorAt(span.ValueStart),
                ValueEnd = CreateAnchorAt(span.ValueEnd),
                PairStart = CreateAnchorAt(span.PairStart),
                PairEnd = CreateAnchorAt(span.PairEnd),
            };

        private TextAnchor CreateAnchorAt(int offset)
        {
            var anchor = document.CreateAnchor(Math.Max(0, Math.Min(offset, document.TextLength)));
            anchor.MovementType = AnchorMovementType.BeforeInsertion;
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
