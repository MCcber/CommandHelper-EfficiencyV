// JSON 语法错误的波浪线：复用项目里现成的 WaveUnderlineBackgroundRenderer（IBackgroundRenderer + ITextAnchor）。
using System;
using System.Windows.Media;
using CBHK.Utility.Data.JsonText;
using CBHK.Utility.Visual;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 在 JSON 编辑器里给语法错误位置画波浪线。只做"挂/摘渲染器"这一件事，
    /// 行列→偏移的换算交给调用方（那里才有 TextDocument）。
    /// </summary>
    public sealed class McdocJsonSyntaxErrorOverlay
    {
        private static readonly Color FallbackColor = Color.FromRgb(0xE8, 0x11, 0x23);

        private readonly JsonTextEditor editor;
        private readonly SolidColorBrush brush;
        private WaveUnderlineBackgroundRenderer renderer;

        public McdocJsonSyntaxErrorOverlay(JsonTextEditor editor, Color? color = null)
        {
            this.editor = editor;
            brush = new SolidColorBrush(color ?? FallbackColor);
            brush.Freeze();
        }

        public bool IsVisible => renderer is not null;

        /// <summary>下划 [offset, offset+length)；长度至少 1，越界自动夹紧到文档内。</summary>
        public void Show(int offset, int length)
        {
            Clear();
            var document = editor?.Document;
            if (document is null || document.TextLength <= 0) return;

            var start = Math.Clamp(offset, 0, Math.Max(0, document.TextLength - 1));
            var end = Math.Clamp(start + Math.Max(1, length), start + 1, document.TextLength);
            if (end <= start) return;

            ITextAnchor startAnchor = document.CreateAnchor(start);
            ITextAnchor endAnchor = document.CreateAnchor(end);
            renderer = new WaveUnderlineBackgroundRenderer(startAnchor, endAnchor, brush);
            var textView = editor.TextArea?.TextView;
            if (textView is null)
            {
                renderer = null;
                return;
            }
            textView.BackgroundRenderers.Add(renderer);
            textView.InvalidateLayer(KnownLayer.Selection);
        }

        /// <summary>按 1-based 行列下划；行/列非法时退回"整行最后一段"。</summary>
        public void Show(McdocJsonParser.SyntaxError error)
        {
            var document = editor?.Document;
            if (document is null || error is null) return;
            var lineNumber = Math.Clamp(error.Line, 1, Math.Max(1, document.LineCount));
            var line = document.GetLineByNumber(lineNumber);
            var column = Math.Clamp(error.Column, 1, Math.Max(1, line.Length + 1));
            var offset = line.Offset + Math.Min(column - 1, line.Length);
            var length = Math.Max(1, Math.Min(error.Length, Math.Max(1, line.Length - Math.Min(column - 1, line.Length))));
            Show(offset, length);
        }

        public void Clear()
        {
            if (renderer is null) return;
            var textView = editor?.TextArea?.TextView;
            textView?.BackgroundRenderers.Remove(renderer);
            textView?.InvalidateLayer(KnownLayer.Selection);
            renderer = null;
        }
    }
}
