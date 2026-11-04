using System;
using System.Collections.Generic;
using System.Text;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 文本里"能包含住某个偏移的最小 JSON 结构"：段路径 + 区间。
    /// 只做词法扫描：不建值模型、不猜字符（修复一律是"整段换成左侧模型的版本"）。
    /// </summary>
    public sealed class McdocJsonRegion
    {
        /// <summary>段路径：string = 对象键；int = 数组下标（键里带 . 或 [ 也不会解析歪）。</summary>
        public List<object> Segments = [];

        /// <summary>开括号位置。</summary>
        public int Start;

        /// <summary>配对闭括号之后；文本走到头还没闭合时等于文本长度。</summary>
        public int End;

        /// <summary>括号是否真的闭上了（false = 脏区域一直脏到文末）。</summary>
        public bool Closed;

        public string Path => McdocJsonRegions.DescribePath(Segments);
    }

    /// <summary>
    /// 右侧 JSON 的"脏区域定位"工具：把 System.Text.Json 的行列换成字符偏移，
    /// 沿括号栈找到包住它的最内层结构，再按段路径在左侧模型里取出同一份结构做整段替换。
    /// 与 <see cref="McdocJsonParser.TryParse"/> 同口径（容忍 // 、/* */ 与尾随逗号）。
    /// </summary>
    public static class McdocJsonRegions
    {
        /// <summary>行列（1-based，按字符）→ 字符偏移；越界钳到文本末尾。</summary>
        public static int OffsetOf(string text, int line, int column)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            var target = Math.Max(1, line);
            var offset = 0;
            for (var current = 1; current < target; current++)
            {
                var next = text.IndexOf('\n', offset);
                if (next < 0) return text.Length;
                offset = next + 1;
            }
            return Math.Clamp(offset + Math.Max(0, column - 1), 0, text.Length);
        }

        /// <summary>包住 <paramref name="offset"/> 的最内层结构（最深的那个容器）。</summary>
        public static bool TryFindEnclosing(string text, int offset, out McdocJsonRegion region)
        {
            region = null;
            if (string.IsNullOrEmpty(text)) return false;
            var scanner = new Scanner(text);
            var regions = scanner.Scan();
            var position = Math.Clamp(offset, 0, text.Length);
            foreach (var candidate in regions)
            {
                var inside = candidate.Closed
                    ? position >= candidate.Start && position < candidate.End
                    : position >= candidate.Start;
                if (!inside) continue;
                if (region is null || candidate.Start > region.Start) region = candidate;
            }
            return region is not null;
        }

        /// <summary>按段路径在 JSON 模型里取值；缺任何一段就返回 null（调用方回退整篇口径）。</summary>
        public static McdocJsonValue Resolve(McdocJsonValue root, IReadOnlyList<object> segments)
        {
            var current = root;
            if (segments is null) return current;
            foreach (var segment in segments)
            {
                if (current is null) return null;
                current = segment switch
                {
                    string key => current.Get(key),
                    int index => current.Items is not null && index >= 0 && index < current.Items.Count
                        ? current.Items[index] : null,
                    _ => null,
                };
            }
            return current;
        }

        /// <summary>
        /// 把 <c>[Start, End)</c> 换成 <paramref name="replacement"/>，再整体排版；
        /// 结果是严格解析验收过的合法文本（不合法返回 null，调用方不许写半成品）。
        /// </summary>
        public static string Splice(string text, McdocJsonRegion region, McdocJsonValue replacement)
        {
            if (string.IsNullOrEmpty(text) || region is null || replacement is null) return null;
            var start = Math.Clamp(region.Start, 0, text.Length);
            var end = Math.Clamp(region.End, start, text.Length);
            var builder = new StringBuilder(text.Length + 64);
            builder.Append(text, 0, start);
            builder.Append(McdocJsonFormatter.Format(replacement));
            builder.Append(text, end, text.Length - end);
            if (!McdocJsonParser.TryParse(builder.ToString(), out var merged, out _)) return null;
            // 最后排版：内容按拼接结果，版式统一成 Format 的行为
            return McdocJsonFormatter.Format(merged);
        }

        /// <summary>两条段路径的最长公共前缀长度（求"既要含脏区域、又要含本次操作"的最小容器）。</summary>
        public static int CommonPrefixLength(IReadOnlyList<object> left, IReadOnlyList<object> right)
        {
            if (left is null || right is null) return 0;
            var limit = Math.Min(left.Count, right.Count);
            var length = 0;
            while (length < limit && Equals(left[length], right[length])) length++;
            return length;
        }

        /// <summary>段路径 → 日志/状态提示用的 <c>$.a.b[0]</c>。</summary>
        public static string DescribePath(IReadOnlyList<object> segments)
        {
            var builder = new StringBuilder("$");
            foreach (var segment in segments ?? [])
            {
                if (segment is int index) builder.Append('[').Append(index).Append(']');
                else builder.Append('.').Append(segment);
            }
            return builder.ToString();
        }

        /// <summary>
        /// 容忍注释 / 尾随逗号 / 未闭合字符串的结构扫描：只记容器区间与段路径，不校验内容。
        /// 遇到不认识的东西就往前挪一格继续扫（这里的职责是"定位"，不是"判定合法"）。
        /// </summary>
        private sealed class Scanner
        {
            private const int MaxDepth = 512;

            private readonly string text;
            private readonly List<object> path = [];
            private readonly List<McdocJsonRegion> regions = [];
            private int index;

            public Scanner(string text) => this.text = text ?? "";

            public List<McdocJsonRegion> Scan()
            {
                var depth = 0;
                while (true)
                {
                    SkipTrivia();
                    if (index >= text.Length) return regions;
                    if (text[index] is '{' or '[')
                    {
                        if (depth++ > MaxDepth) return regions;
                        ScanContainer();
                        depth--;
                        continue;
                    }
                    // 根是标量/坏字符：只前进一格，继续找后面的容器
                    index++;
                }
            }

            private void ScanContainer()
            {
                var region = new McdocJsonRegion { Start = index, Segments = new List<object>(path) };
                regions.Add(region);
                var isArray = text[index] == '[';
                var close = isArray ? ']' : '}';
                index++;
                var slot = 0;
                while (true)
                {
                    SkipTrivia();
                    if (index >= text.Length)
                    {
                        region.End = text.Length;
                        region.Closed = false;
                        return;
                    }
                    if (text[index] == close)
                    {
                        index++;
                        region.End = index;
                        region.Closed = true;
                        return;
                    }
                    if (isArray)
                    {
                        path.Add(slot++);
                        ScanValue();
                        if (path.Count > 0) path.RemoveAt(path.Count - 1);
                    }
                    else
                    {
                        SkipTrivia();
                        if (index < text.Length && text[index] == '"')
                        {
                            var key = ReadString();
                            SkipTrivia();
                            if (index < text.Length && text[index] == ':') index++;
                            path.Add(key);
                            ScanValue();
                            if (path.Count > 0) path.RemoveAt(path.Count - 1);
                        }
                        else
                        {
                            index++;   // 坏键：跳过一格继续找
                        }
                    }
                    SkipTrivia();
                    if (index < text.Length && text[index] == ',') index++;
                }
            }

            private void ScanValue()
            {
                SkipTrivia();
                if (index >= text.Length) return;
                switch (text[index])
                {
                    case '{':
                    case '[':
                        ScanContainer();
                        return;
                    case '"':
                        ReadString();
                        return;
                    default:
                        while (index < text.Length && !char.IsWhiteSpace(text[index])
                               && text[index] != ',' && text[index] != '}' && text[index] != ']')
                        {
                            index++;
                        }
                        return;
                }
            }

            /// <summary>读字符串（容忍未闭合：读到文末就停）。</summary>
            private string ReadString()
            {
                if (index >= text.Length || text[index] != '"') return "";
                index++;
                var builder = new StringBuilder();
                while (index < text.Length)
                {
                    var c = text[index++];
                    if (c == '"') return builder.ToString();
                    if (c == '\\' && index < text.Length)
                    {
                        builder.Append(text[index++]);
                        continue;
                    }
                    builder.Append(c);
                }
                return builder.ToString();
            }

            private void SkipTrivia()
            {
                while (index < text.Length)
                {
                    var c = text[index];
                    if (char.IsWhiteSpace(c)) { index++; continue; }
                    if (c == '/' && index + 1 < text.Length)
                    {
                        if (text[index + 1] == '/')
                        {
                            var next = text.IndexOf('\n', index + 2);
                            index = next < 0 ? text.Length : next + 1;
                            continue;
                        }
                        if (text[index + 1] == '*')
                        {
                            var next = text.IndexOf("*/", index + 2, StringComparison.Ordinal);
                            index = next < 0 ? text.Length : next + 2;
                            continue;
                        }
                    }
                    return;
                }
            }
        }
    }
}