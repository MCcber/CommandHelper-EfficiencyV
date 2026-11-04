using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// doc 面板的 markdown 子集渲染。misode 是 marked() + DOMPurify 出 HTML 塞进 .node-doc，
    /// WPF 这边出 TextBlock.Inlines（不再是一段死文本）。
    /// 支持：标题 #、围栏代码块 ```、无序/有序列表、段落、**粗体**、*斜体*/_斜体_、`行内代码`、[文本](链接)。
    /// 认不出的语法（含未闭合的标记）一律原样输出；任何异常都退化成纯文本——行面板不许因为 doc 写法奇怪而崩。
    /// </summary>
    public static class McdocMarkdown
    {
        /// <summary>把 markdown 挂到任意 TextBlock 上（XAML 里 `data:McdocMarkdown.Source="{Binding Documentation}"`）。</summary>
        public static readonly DependencyProperty SourceProperty = DependencyProperty.RegisterAttached(
            "Source", typeof(string), typeof(McdocMarkdown), new PropertyMetadata(null, OnSourceChanged));

        public static void SetSource(DependencyObject element, string value) => element?.SetValue(SourceProperty, value);

        public static string GetSource(DependencyObject element) => element?.GetValue(SourceProperty) as string;

        private static readonly FontFamily MonoFont = new("Consolas, Courier New, Monospace");

        private static void OnSourceChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
        {
            if (element is not TextBlock block) return;
            Apply(block, args.NewValue as string);
        }

        /// <summary>把 markdown 渲染进 TextBlock.Inlines；失败就退化成一段纯文本。</summary>
        public static void Apply(TextBlock block, string markdown)
        {
            if (block is null) return;
            IReadOnlyList<Inline> inlines;
            try
            {
                inlines = BuildInlines(markdown);
            }
            catch
            {
                inlines = new[] { new Run(markdown ?? "") };
            }
            try
            {
                block.Inlines.Clear();
                foreach (var inline in inlines)
                {
                    if (inline is not null) block.Inlines.Add(inline);
                }
            }
            catch
            {
                try
                {
                    block.Inlines.Clear();
                    block.Inlines.Add(new Run(markdown ?? ""));
                }
                catch
                {
                    // 连兜底都失败就保持空面板，绝不把异常抛给 WPF
                }
            }
        }

        /// <summary>纯函数版本（便于无 UI 断言）：markdown → 一组未被挂载的 Inline。</summary>
        public static IReadOnlyList<Inline> BuildInlines(string markdown)
        {
            var result = new List<Inline>();
            var text = (markdown ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
            if (text.Length == 0) return result;
            var lines = text.Split('\n');
            var blocks = 0;

            void BeginBlock()
            {
                if (blocks > 0)
                {
                    result.Add(new LineBreak());
                    result.Add(new LineBreak());
                }
                blocks++;
            }

            var index = 0;
            while (index < lines.Length)
            {
                var trimmed = Normalize(lines[index]);
                if (trimmed.Length == 0)
                {
                    index++;
                    continue;
                }

                // 围栏代码块
                if (trimmed.StartsWith("\u0060\u0060\u0060", StringComparison.Ordinal))
                {
                    index++;
                    var code = new StringBuilder();
                    while (index < lines.Length && !Normalize(lines[index]).StartsWith("\u0060\u0060\u0060", StringComparison.Ordinal))
                    {
                        if (code.Length > 0) code.Append('\n');
                        code.Append(Normalize(lines[index]));
                        index++;
                    }
                    if (index < lines.Length) index++;
                    BeginBlock();
                    result.Add(new Run(code.ToString()) { FontFamily = MonoFont });
                    continue;
                }

                // 标题
                var hashes = 0;
                while (hashes < trimmed.Length && hashes < 6 && trimmed[hashes] == '#') hashes++;
                if (hashes > 0 && hashes < trimmed.Length && trimmed[hashes] == ' ')
                {
                    BeginBlock();
                    result.Add(new Run(trimmed.Substring(hashes + 1).Trim())
                    {
                        FontWeight = FontWeights.Bold,
                        FontSize = hashes <= 2 ? 14 : 13,
                    });
                    index++;
                    continue;
                }

                // 列表项
                if (IsListItem(trimmed, out var itemText))
                {
                    BeginBlock();
                    result.Add(new Run("\u2022 "));
                    AppendInline(result, itemText);
                    index++;
                    continue;
                }

                // 段落：连续的普通行，软换行按空格拼（marked 同款）
                var paragraph = new StringBuilder();
                while (index < lines.Length)
                {
                    var current = Normalize(lines[index]);
                    if (current.Length == 0 || current.StartsWith("\u0060\u0060\u0060", StringComparison.Ordinal)
                        || IsHeading(current) || IsListItem(current, out _))
                    {
                        break;
                    }
                    if (paragraph.Length > 0) paragraph.Append(' ');
                    paragraph.Append(current);
                    index++;
                }
                if (paragraph.Length == 0)
                {
                    index++;   // 兜底：不让循环卡住
                    continue;
                }
                BeginBlock();
                AppendInline(result, paragraph.ToString());
            }
            return result;
        }

        /// <summary>mcdoc 的 Desc 带着 `///` 注释前缀（misode 那边由 spyglass 剥掉），这里统一剥掉再按 markdown 处理。</summary>
        private static string Normalize(string line)
        {
            var trimmed = (line ?? "").TrimStart();
            // 多行注释在 Desc 里是用逗号拼起来的（"a\n,b\n"）：跳过行首那个分隔逗号再认标记
            if (trimmed.StartsWith(",///", StringComparison.Ordinal) || trimmed.StartsWith(",//!", StringComparison.Ordinal))
            {
                trimmed = trimmed.Substring(1).TrimStart();
            }
            if (trimmed.StartsWith("///", StringComparison.Ordinal) || trimmed.StartsWith("//!", StringComparison.Ordinal))
            {
                trimmed = trimmed.Substring(3);
            }
            return trimmed.Trim();
        }

        private static bool IsHeading(string line)
        {
            var hashes = 0;
            while (hashes < line.Length && hashes < 6 && line[hashes] == '#') hashes++;
            return hashes > 0 && hashes < line.Length && line[hashes] == ' ';
        }

        private static bool IsListItem(string line, out string body)
        {
            body = "";
            if (line.Length < 2) return false;
            var mark = line[0];
            if ((mark == '-' || mark == '*' || mark == '+') && line[1] == ' ')
            {
                body = line.Substring(2).Trim();
                return true;
            }
            var digits = 0;
            while (digits < line.Length && char.IsDigit(line[digits])) digits++;
            if (digits > 0 && digits + 1 < line.Length && line[digits] == '.' && line[digits + 1] == ' ')
            {
                body = line.Substring(digits + 2).Trim();
                return true;
            }
            return false;
        }

        private static void AppendInline(ICollection<Inline> target, string text)
        {
            foreach (var inline in ParseInline(text))
            {
                target.Add(inline);
            }
        }

        /// <summary>行内语法：**粗体** / *斜体* / _斜体_ / `代码` / [文本](链接)；未闭合就原样输出。</summary>
        private static List<Inline> ParseInline(string text)
        {
            var result = new List<Inline>();
            if (string.IsNullOrEmpty(text)) return result;
            var buffer = new StringBuilder();

            void Flush()
            {
                if (buffer.Length == 0) return;
                result.Add(new Run(buffer.ToString()));
                buffer.Clear();
            }

            var index = 0;
            while (index < text.Length)
            {
                var current = text[index];

                // 行内代码
                if (current == '\u0060')
                {
                    var close = text.IndexOf('\u0060', index + 1);
                    if (close > index + 1)
                    {
                        Flush();
                        result.Add(new Run(text.Substring(index + 1, close - index - 1)) { FontFamily = MonoFont });
                        index = close + 1;
                        continue;
                    }
                    buffer.Append(current);
                    index++;
                    continue;
                }

                // **粗体**
                if (index + 1 < text.Length && (current == '*' || current == '_') && text[index + 1] == current)
                {
                    var marker = new string(current, 2);
                    var close = text.IndexOf(marker, index + 2, StringComparison.Ordinal);
                    if (close > index + 2)
                    {
                        Flush();
                        var bold = new Bold();
                        AppendInline(bold.Inlines, text.Substring(index + 2, close - index - 2));
                        result.Add(bold);
                        index = close + 2;
                        continue;
                    }
                    buffer.Append(current);
                    index++;
                    continue;
                }

                // *斜体* / _斜体_
                if (current == '*' || current == '_')
                {
                    var close = text.IndexOf(current, index + 1);
                    if (close > index + 1)
                    {
                        Flush();
                        var italic = new Italic();
                        AppendInline(italic.Inlines, text.Substring(index + 1, close - index - 1));
                        result.Add(italic);
                        index = close + 1;
                        continue;
                    }
                    buffer.Append(current);
                    index++;
                    continue;
                }

                // [文本](链接)
                if (current == '[')
                {
                    var closeBracket = text.IndexOf(']', index + 1);
                    if (closeBracket > index + 1 && closeBracket + 1 < text.Length && text[closeBracket + 1] == '(')
                    {
                        var closeParen = text.IndexOf(')', closeBracket + 2);
                        if (closeParen > closeBracket + 2)
                        {
                            var label = text.Substring(index + 1, closeBracket - index - 1);
                            var url = text.Substring(closeBracket + 2, closeParen - closeBracket - 2);
                            Flush();
                            var link = new Hyperlink(new Run(label)) { ToolTip = url };
                            try
                            {
                                if (Uri.TryCreate(url, UriKind.Absolute, out var uri)) link.NavigateUri = uri;
                            }
                            catch
                            {
                                // 链接地址非法就当普通文本链接用，不设 NavigateUri
                            }
                            result.Add(link);
                            index = closeParen + 1;
                            continue;
                        }
                    }
                    buffer.Append(current);
                    index++;
                    continue;
                }

                buffer.Append(current);
                index++;
            }
            Flush();
            return result;
        }
    }
}
