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
        public static string Format(McdocJsonValue value, int indentSize = 2)
        {
            var builder = new StringBuilder();
            Write(builder, value, 0, indentSize);
            return builder.ToString();
        }

        private static void Write(StringBuilder builder, McdocJsonValue value, int indent, int indentSize)
        {
            if (value is null)
            {
                builder.Append("null");
                return;
            }

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
                        builder.Append(JsonSerializer.Serialize(properties[i].Key)).Append(": ");
                        Write(builder, properties[i].Value, indent + 1, indentSize);
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
                        Write(builder, items[i], indent + 1, indentSize);
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
