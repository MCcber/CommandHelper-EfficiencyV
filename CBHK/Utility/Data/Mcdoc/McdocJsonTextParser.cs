// 带区间的 JSON 解析器：产出 McdocJsonValue（每个节点记录在原文中的字符区间），
// 并额外给出"整个属性（键 + 冒号 + 值）"的区间，供手术式删除/替换用。
// 纯字符串处理，不依赖 AvalonEdit；锚点由上层（JsonAnchorMap）基于这些区间创建。
using System;
using System.Collections.Generic;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>某个值节点在原文中的字符区间。Pair* 覆盖"键: 值"整段（无键时为 0）。</summary>
    public readonly struct McdocJsonSpan
    {
        public readonly int ValueStart;
        public readonly int ValueEnd;
        public readonly int PairStart;
        public readonly int PairEnd;

        public McdocJsonSpan(int valueStart, int valueEnd, int pairStart, int pairEnd)
        {
            ValueStart = valueStart;
            ValueEnd = valueEnd;
            PairStart = pairStart;
            PairEnd = pairEnd;
        }

        public int ValueLength => ValueEnd - ValueStart;
        public int PairLength => PairEnd - PairStart;
    }

    public sealed class McdocJsonParseResult
    {
        public McdocJsonValue Root = McdocJsonValue.Null();
        public Dictionary<McdocJsonValue, McdocJsonSpan> Spans = [];
    }

    public static class McdocJsonTextParser
    {
        /// <summary>解析文本；容忍 // 与 /* */ 注释、以及尾随逗号（探索性 JSON 常见写法）。</summary>
        public static McdocJsonParseResult Parse(string text)
        {
            var result = new McdocJsonParseResult();
            if (string.IsNullOrWhiteSpace(text))
            {
                return result;
            }
            var cursor = new Cursor(text ?? "");
            result.Root = ReadValue(cursor, result.Spans, -1, -1);
            return result;
        }

        private sealed class Cursor(string text)
        {
            public readonly string Text = text;
            public int Index;

            public bool End => Index >= Text.Length;

            public char Peek => End ? '\0' : Text[Index];

            public void SkipTrivia()
            {
                while (!End)
                {
                    var current = Text[Index];
                    if (char.IsWhiteSpace(current))
                    {
                        Index++;
                        continue;
                    }
                    if (current == '/' && Index + 1 < Text.Length && Text[Index + 1] == '/')
                    {
                        Index += 2;
                        while (!End && Text[Index] != '\n') Index++;
                        continue;
                    }
                    if (current == '/' && Index + 1 < Text.Length && Text[Index + 1] == '*')
                    {
                        Index += 2;
                        while (Index + 1 < Text.Length && !(Text[Index] == '*' && Text[Index + 1] == '/')) Index++;
                        Index = Math.Min(Text.Length, Index + 2);
                        continue;
                    }
                    break;
                }
            }
        }

        private static McdocJsonValue ReadValue(Cursor cursor, Dictionary<McdocJsonValue, McdocJsonSpan> spans, int pairStart, int pairEnd)
        {
            cursor.SkipTrivia();
            var start = cursor.Index;
            if (cursor.End)
            {
                return McdocJsonValue.Null();
            }

            var value = cursor.Peek switch
            {
                '{' => ReadObject(cursor, spans),
                '[' => ReadArray(cursor, spans),
                '"' => McdocJsonValue.String(ReadString(cursor)),
                't' => ReadKeyword(cursor, "true", McdocJsonValue.Boolean(true)),
                'f' => ReadKeyword(cursor, "false", McdocJsonValue.Boolean(false)),
                'n' => ReadKeyword(cursor, "null", McdocJsonValue.Null()),
                _ => ReadNumber(cursor),
            };

            var end = cursor.Index;
            spans[value] = new McdocJsonSpan(start, end, pairStart >= 0 ? pairStart : start, pairEnd >= 0 ? pairEnd : end);
            value.Start = start;
            value.End = end;
            return value;
        }

        private static McdocJsonValue ReadObject(Cursor cursor, Dictionary<McdocJsonValue, McdocJsonSpan> spans)
        {
            var result = McdocJsonValue.Object();
            cursor.Index++;   // {
            while (true)
            {
                cursor.SkipTrivia();
                if (cursor.End || cursor.Peek == '}')
                {
                    if (!cursor.End) cursor.Index++;
                    return result;
                }

                var pairStart = cursor.Index;
                var key = ReadString(cursor);
                cursor.SkipTrivia();
                if (cursor.Peek == ':') cursor.Index++;
                var child = ReadValue(cursor, spans, pairStart, -1);
                var pairEnd = cursor.Index;
                spans[child] = new McdocJsonSpan(child.Start, child.End, pairStart, pairEnd);
                result.Add(key, child);

                cursor.SkipTrivia();
                if (cursor.Peek == ',')
                {
                    cursor.Index++;
                    continue;
                }
                if (cursor.Peek == '}') cursor.Index++;
                return result;
            }
        }

        private static McdocJsonValue ReadArray(Cursor cursor, Dictionary<McdocJsonValue, McdocJsonSpan> spans)
        {
            var result = McdocJsonValue.Array();
            cursor.Index++;   // [
            while (true)
            {
                cursor.SkipTrivia();
                if (cursor.End || cursor.Peek == ']')
                {
                    if (!cursor.End) cursor.Index++;
                    return result;
                }

                var item = ReadValue(cursor, spans, -1, -1);
                spans[item] = new McdocJsonSpan(item.Start, item.End, item.Start, item.End);
                result.Add(item);

                cursor.SkipTrivia();
                if (cursor.Peek == ',')
                {
                    cursor.Index++;
                    continue;
                }
                if (cursor.Peek == ']') cursor.Index++;
                return result;
            }
        }

        private static string ReadString(Cursor cursor)
        {
            if (cursor.Peek != '"') return "";
            cursor.Index++;
            var builder = new System.Text.StringBuilder();
            while (!cursor.End)
            {
                var current = cursor.Text[cursor.Index++];
                if (current == '"') break;
                if (current != '\\')
                {
                    builder.Append(current);
                    continue;
                }
                if (cursor.End) break;
                var escaped = cursor.Text[cursor.Index++];
                builder.Append(escaped switch
                {
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    'b' => '\b',
                    'f' => '\f',
                    'u' => ReadUnicode(cursor),
                    _ => escaped,
                });
            }
            return builder.ToString();
        }

        private static char ReadUnicode(Cursor cursor)
        {
            if (cursor.Index + 4 > cursor.Text.Length) return '?';
            var hex = cursor.Text.Substring(cursor.Index, 4);
            cursor.Index += 4;
            return ushort.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var code)
                ? (char)code
                : '?';
        }

        private static McdocJsonValue ReadKeyword(Cursor cursor, string keyword, McdocJsonValue value)
        {
            cursor.Index += keyword.Length;
            return value;
        }

        private static McdocJsonValue ReadNumber(Cursor cursor)
        {
            var start = cursor.Index;
            while (!cursor.End && "+-.0123456789eE".IndexOf(cursor.Peek, StringComparison.Ordinal) >= 0) cursor.Index++;
            var text = cursor.Text.Substring(start, cursor.Index - start);
            var isInteger = text.IndexOf('.') < 0 && text.IndexOf('e') < 0 && text.IndexOf('E') < 0;
            return decimal.TryParse(text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var number)
                ? McdocJsonValue.Number(number, isInteger)
                : McdocJsonValue.Number(0m, isInteger);
        }
    }
}
