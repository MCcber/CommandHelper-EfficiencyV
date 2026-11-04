// 视图节点 + 构建器 + JSON 解析器：把"文本 → 求值 → 可绑定树"这条路径压在一个文件里。
// 设计目标：让 CBHK 既有的 13 个 TreeView DataTemplate 零改动复用，
// 因此属性名严格对齐 TreeViewCommon.xaml 的绑定（Children/Items/SelectedUnionChildren/
// FieldName/IsVisible/IsRequired/Value/IsTrue/IsFalse/EnumOptionList/SelectedEnumOption/
// UnionTypeNameList/SelectedUnionTypeName）。
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MinecraftLanguageModelLibrary.Data;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// JSON 文本 → 值模型（与 McdocJsonTextParser 同源：容忍 // 、/* */ 注释与尾随逗号）。
    /// 无头测试与视图两条入口共用，避免"两条解析路径行为不同"。
    /// </summary>
    public static class McdocJsonParser
    {
        /// <summary>语法错误位置（行列都是 1-based，列按字符算，已把 UTF-8 字节偏移换算成字符下标）。</summary>
        public sealed class SyntaxError
        {
            public string Message = "";
            /// <summary>1-based 行号。</summary>
            public int Line = 1;
            /// <summary>1-based 列号（字符）。</summary>
            public int Column = 1;
            /// <summary>建议下划长度（字符数，至少 1）。</summary>
            public int Length = 1;
        }

        /// <summary>
        /// 严格校验并转换：失败时给出 <see cref="SyntaxError"/>（用 System.Text.Json 的异常位置），成功时返回转换后的值。
        /// 留出这个方法是因为"语法错误不能污染树"必须在调用处就能判断，不能像 <see cref="Parse"/> 那样静默返回空对象。
        /// </summary>
        public static bool TryParse(string text, out McdocJsonValue value, out SyntaxError error)
        {
            value = null;
            error = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                value = McdocJsonValue.Object();
                return true;
            }
            try
            {
                using var document = JsonDocument.Parse(text, new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                });
                value = Convert(document.RootElement);
                return true;
            }
            catch (JsonException exception)
            {
                error = Describe(exception, text);
                return false;
            }
        }

        private static SyntaxError Describe(JsonException exception, string text)
        {
            var lines = (text ?? "").Split('\n');
            var lineIndex = (int)Math.Clamp(exception.LineNumber ?? 0, 0, Math.Max(0, lines.Length - 1));
            var lineText = lines.Length > 0 ? lines[lineIndex].TrimEnd('\r') : "";
            var charIndex = ByteToCharIndex(lineText, (int)Math.Max(0, exception.BytePositionInLine ?? 0));
            var length = Math.Max(1, lineText.Length - charIndex);
            if (charIndex >= lineText.Length && lineText.Length > 0)
            {
                // 错误发生在行尾（例如缺少闭合符）：下划最后一个字符，保证用户看得见
                charIndex = lineText.Length - 1;
                length = 1;
            }
            return new SyntaxError
            {
                Message = exception.Message,
                Line = lineIndex + 1,
                Column = charIndex + 1,
                Length = length,
            };
        }

        /// <summary>UTF-8 字节偏移 → 字符下标（emoji 这类代理对按 Rune 累加，避免算歪）。</summary>
        public static int ByteToCharIndex(string lineText, int byteOffset)
        {
            if (string.IsNullOrEmpty(lineText) || byteOffset <= 0) return 0;
            var bytes = 0;
            var chars = 0;
            foreach (var rune in lineText.EnumerateRunes())
            {
                if (bytes >= byteOffset) break;
                bytes += rune.Utf8SequenceLength;
                chars += rune.Utf16SequenceLength;
            }
            return Math.Min(chars, lineText.Length);
        }

        public static McdocJsonValue Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return McdocJsonValue.Object();
            }
            try
            {
                using var document = JsonDocument.Parse(text, new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                });
                return Convert(document.RootElement);
            }
            catch (JsonException)
            {
                return McdocJsonValue.Object();
            }
        }

        private static McdocJsonValue Convert(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                {
                    var result = McdocJsonValue.Object();
                    foreach (var property in element.EnumerateObject())
                    {
                        result.Add(property.Name, Convert(property.Value));
                    }
                    return result;
                }
                case JsonValueKind.Array:
                {
                    var result = McdocJsonValue.Array();
                    foreach (var item in element.EnumerateArray())
                    {
                        result.Add(Convert(item));
                    }
                    return result;
                }
                case JsonValueKind.String:
                    return McdocJsonValue.String(element.GetString());
                case JsonValueKind.Number:
                    return element.TryGetInt64(out var integer)
                        ? McdocJsonValue.Number(integer, true)
                        : McdocJsonValue.Number(element.GetDecimal(), false);
                case JsonValueKind.True:
                    return McdocJsonValue.Boolean(true);
                case JsonValueKind.False:
                    return McdocJsonValue.Boolean(false);
                default:
                    return McdocJsonValue.Null();
            }
        }
    }
}
