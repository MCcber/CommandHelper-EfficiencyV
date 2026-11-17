// JSON 格式化器：按 spyglass 风格输出（2 空格缩进、保留树里的键顺序）。
// 纯字符串处理，不依赖 AvalonEdit / WPF，便于无头测试。
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace CBHK.Utility.Data.Mcdoc
{
    public static class McdocJsonFormatter
    {
        public static string Format(McdocJsonValue value, int indentSize = 2) => Format(value, indentSize, null);

        /// <summary>
        /// 与 <see cref="Format(McdocJsonValue, int)"/> 同一遍输出，顺带记下每个节点在结果文本里的区间
        /// （值区间 + 键区间，与 <see cref="McdocJsonSpan"/> 同语义）。写回时用它把锚点直接落到正确位置，
        /// 免掉"写完整篇再重解析一遍"的第二套坐标。
        /// </summary>
        public static string Format(McdocJsonValue value, int indentSize, IDictionary<McdocJsonValue, McdocJsonSpan> spans)
        {
            var builder = new StringBuilder();
            Write(builder, value, 0, indentSize, spans);
            return builder.ToString();
        }

        /// <summary>
        /// 只出一个子树的文本：<paramref name="level"/> 是它在整篇里的缩进层，产出与整篇 Format 中这段值逐字相同的文本
        /// （值自身的首行不带缩进 —— 整篇里那一行的缩进是父级写的）。<paramref name="spans"/> 里的区间**相对这段文本**，
        /// 调用方负责平移到全文。结构编辑只脏一层子树，没必要为它跑整篇格式化（1MB 档 = 5.4 万个节点）。
        /// </summary>
        public static string FormatAt(McdocJsonValue value, int level, int indentSize,
            IDictionary<McdocJsonValue, McdocJsonSpan> spans)
        {
            var builder = new StringBuilder();
            Write(builder, value, Math.Max(0, level), indentSize, spans);
            return builder.ToString();
        }

        private static void Write(StringBuilder builder, McdocJsonValue value, int indent, int indentSize,
            IDictionary<McdocJsonValue, McdocJsonSpan> spans)
        {
            if (value is null)
            {
                builder.Append("null");
                return;
            }

            var start = builder.Length;
            WriteBody(builder, value, indent, indentSize, spans);
            if (spans is not null)
            {
                spans[value] = new McdocJsonSpan(start, builder.Length, start, builder.Length);
            }
        }

        private static void WriteBody(StringBuilder builder, McdocJsonValue value, int indent, int indentSize,
            IDictionary<McdocJsonValue, McdocJsonSpan> spans)
        {
            switch (value.Kind)
            {
                case McdocJsonKind.Object:
                {
                    var properties = value.Properties ?? [];
                    if (properties.Count == 0)
                    {
                        builder.Append("{}");
                        return;
                    }
                    builder.Append('{').Append('\n');
                    for (var i = 0; i < properties.Count; i++)
                    {
                        Indent(builder, indent + 1, indentSize);
                        var pairStart = builder.Length;
                        builder.Append(JsonSerializer.Serialize(properties[i].Key)).Append(": ");
                        var valueStart = builder.Length;
                        Write(builder, properties[i].Value, indent + 1, indentSize, spans);
                        if (spans is not null && properties[i].Value is not null)
                        {
                            spans[properties[i].Value] = new McdocJsonSpan(valueStart, builder.Length, pairStart, builder.Length);
                        }
                        if (i < properties.Count - 1) builder.Append(',');
                        builder.Append('\n');
                    }
                    Indent(builder, indent, indentSize);
                    builder.Append('}');
                    return;
                }

                case McdocJsonKind.Array:
                {
                    var items = value.Items ?? [];
                    if (items.Count == 0)
                    {
                        builder.Append("[]");
                        return;
                    }
                    builder.Append('[').Append('\n');
                    for (var i = 0; i < items.Count; i++)
                    {
                        Indent(builder, indent + 1, indentSize);
                        Write(builder, items[i], indent + 1, indentSize, spans);
                        if (i < items.Count - 1) builder.Append(',');
                        builder.Append('\n');
                    }
                    Indent(builder, indent, indentSize);
                    builder.Append(']');
                    return;
                }

                case McdocJsonKind.String:
                    builder.Append(JsonSerializer.Serialize(value.StringValue ?? ""));
                    return;

                case McdocJsonKind.Number:
                    builder.Append(value.IsInteger
                        ? ((long)value.NumberValue).ToString(CultureInfo.InvariantCulture)
                        : ((double)value.NumberValue).ToString("R", CultureInfo.InvariantCulture));
                    return;

                case McdocJsonKind.Boolean:
                    builder.Append(value.BooleanValue ? "true" : "false");
                    return;

                default:
                    builder.Append("null");
                    return;
            }
        }

        private static void Indent(StringBuilder builder, int level, int indentSize)
            => builder.Append(' ', level * indentSize);
    }
}
