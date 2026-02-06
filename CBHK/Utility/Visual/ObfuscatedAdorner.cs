using CBHK.CustomControl.Input;
using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace CBHK.Utility.Visual
{
    public class ObfuscatedAdorner : Adorner
    {
        #region Method
        private readonly VectorRichTextBox editor;
        private readonly Random _rng = new();
        private const string CharPool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*";
        #endregion

        #region Method
        public ObfuscatedAdorner(UIElement adornedElement) : base(adornedElement)
        {
            editor = adornedElement as VectorRichTextBox;
            // 订阅渲染事件：每一帧都会调用 OnRender，实现“高频变化”
            CompositionTarget.Rendering += (s, e) => InvalidateVisual();
            //IsHitTestVisible = false; // 不阻塞鼠标事件
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (editor == null) return;

            // 1. 获取鼠标相对于 RichTextBox 的位置
            Point mousePos = Mouse.GetPosition(editor);

            TextPointer position = editor.Document.ContentStart;
            while (position != null && position.CompareTo(editor.Document.ContentEnd) < 0)
            {
                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart)
                {
                    if (position.GetAdjacentElement(LogicalDirection.Forward) is Run run &&
                        ObfuscatedProvider.GetIsObfuscated(run))
                    {
                        // --- 物理命中测试核心 (解决多行问题) ---
                        bool isHovering = IsMouseOverRun(run, mousePos);

                        if (isHovering)
                        {
                            run.Foreground = Brushes.White;
                        }
                        else
                        {
                            run.Foreground = editor.Background;
                            DrawObfuscatedText(dc, run);
                        }
                    }
                }
                position = position.GetNextContextPosition(LogicalDirection.Forward);
            }
        }

        private static bool IsMouseOverRun(Run run, Point mousePoint)
        {
            TextPointer start = run.ContentStart;
            TextPointer end = run.ContentEnd;

            // 获取 Run 的起始和结束矩形
            Rect startRect = start.GetCharacterRect(LogicalDirection.Forward);
            Rect endRect = end.GetCharacterRect(LogicalDirection.Backward);

            // 处理跨行逻辑：如果 Top 一致，说明在同一行
            if (Math.Abs(startRect.Top - endRect.Top) < 2)
            {
                Rect combined = new(startRect.TopLeft, endRect.BottomRight);
                // 稍微扩充一下判定区域（增加 2 像素容错）
                combined.Inflate(2, 2);
                return combined.Contains(mousePoint);
            }
            else
            {
                // 如果跨行，简单的矩形包含判断会失效（会变成一个包围两行的大矩形）
                // 此时需要检测鼠标是否在“起点行”或“终点行”或中间行
                // 为求精确，可以遍历 Run 内部的 TextPointer
                TextPointer tp = start;
                while (tp != null && tp.CompareTo(end) < 0)
                {
                    Rect charRect = tp.GetCharacterRect(LogicalDirection.Forward);
                    if (charRect.Contains(mousePoint)) return true;
                    tp = tp.GetNextInsertionPosition(LogicalDirection.Forward);
                }
            }
            return false;
        }

        private void DrawObfuscatedText(DrawingContext dc, Run run)
        {
            // 获取 Run 的视觉矩形（处理跨行逻辑）
            TextPointer start = run.ContentStart;
            Rect startRect = start.GetCharacterRect(LogicalDirection.Forward);

            // 构造随机等宽字符
            char[] chars = new char[run.Text.Length];
            for (int i = 0; i < chars.Length; i++) chars[i] = CharPool[_rng.Next(CharPool.Length)];

            // 使用 FormattedText 直接绘制到 DrawingContext
            FormattedText ft = new(
                new string(chars),
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(run.FontFamily, run.FontStyle, run.FontWeight, run.FontStretch),
                run.FontSize,
                editor.Foreground, // 使用编辑器前景色
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            // 直接在 GPU 渲染路径上绘制
            dc.DrawText(ft, startRect.TopLeft);
        } 
        #endregion
    }
}