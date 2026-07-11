using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Windows;
using System.Windows.Media;

namespace CBHK.Utility.Visual
{
    public class WaveUnderlineBackgroundRenderer(ITextAnchor startAnchor, ITextAnchor endAnchor, SolidColorBrush underlineColor) : IBackgroundRenderer
    {
        #region Field
        public KnownLayer Layer => KnownLayer.Selection;

        /// <summary>
        /// 辅助 ISegment 实现
        /// </summary>
        private readonly struct SimpleSegment(int offset, int length) : ISegment
        {
            public int Offset { get; } = offset;
            public int Length { get; } = length;
            public int EndOffset => Offset + Length;
        }
        #endregion

        #region Method
        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (startAnchor.Offset >= endAnchor.Offset) return;
            textView.EnsureVisualLines();
            var document = textView.Document;

            int startLine = document.GetLocation(startAnchor.Offset).Line;
            int endLine = document.GetLocation(endAnchor.Offset).Line;

            for (int lineNum = startLine; lineNum <= endLine; lineNum++)
            {
                var line = document.GetLineByNumber(lineNum);
                int segStart = Math.Max(line.Offset, startAnchor.Offset);
                int segEnd = Math.Min(line.EndOffset, endAnchor.Offset);
                if (segStart >= segEnd) continue;

                var segment = new SimpleSegment(segStart, segEnd - segStart);
                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
                {
                    if (rect.IsEmpty) continue;
                    DrawAdaptiveWave(drawingContext, rect.Left, rect.Right, rect.Bottom - 1, underlineColor);
                }
            }
        }

        private static void DrawAdaptiveWave(DrawingContext dc, double x1, double x2, double y, SolidColorBrush brush)
        {
            double width = x2 - x1;
            if (width <= 0) return;

            const double baseAmplitude = 2.5;
            const double baseWaveLength = 4.0;
            const double transitionWidth = 50.0;
            const double minimumAmplitudeThreshold = 0.3;

            // 平滑因子 t ∈ [0, 1]，宽度超过 transitionWidth 后开始线性上升
            double t = Math.Clamp((width - transitionWidth) / 600.0, 0.0, 1.0);
            // 振幅：从 baseAmplitude 平滑过渡到 0
            double amplitude = baseAmplitude * (1.0 - t);
            // 波长：从 baseWaveLength 平滑过渡到更长的周期
            double waveLength = baseWaveLength + t * 12.0;

            // 极平缓时直接画直线，性能最优
            if (amplitude < minimumAmplitudeThreshold)
            {
                dc.DrawLine(new Pen(brush, 1), new Point(x1, y), new Point(x2, y));
                return;
            }

            var pen = new Pen(brush, 1);
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                double x = x1;
                while (x < x2)
                {
                    double nx = Math.Min(x + waveLength, x2);
                    double mx = (x + nx) / 2;
                    ctx.BeginFigure(new Point(x, y), false, false);
                    ctx.QuadraticBezierTo(new Point(mx, y - amplitude), new Point(nx, y), true, false);
                    x = nx;
                }
            }
            geometry.Freeze();
            dc.DrawGeometry(null, pen, geometry);
        } 
        #endregion
    }
}