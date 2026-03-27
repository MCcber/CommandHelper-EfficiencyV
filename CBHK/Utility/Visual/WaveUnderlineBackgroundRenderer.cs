using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows;
using System.Windows.Media;

namespace CBHK.Utility.Visual
{
    /// <summary>
    /// 用于在指定位置的文本下方渲染波浪线
    /// </summary>
    /// <param name="textView">目标文本视图</param>
    /// <param name="startOffset"></param>
    /// <param name="endOffset"></param>
    /// <param name="underlineColor">波浪线颜色</param>
    public class WaveUnderlineBackgroundRenderer(int startOffset, int endOffset, SolidColorBrush underlineColor) : IBackgroundRenderer
    {
        public KnownLayer Layer => KnownLayer.Selection;

        private readonly int startOffset = startOffset;
        private readonly int endOffset = endOffset;
        private readonly SolidColorBrush underlineColor = underlineColor;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            textView.EnsureVisualLines();
            var visualLines = textView.VisualLines;
            if (visualLines.Count == 0)
                return;

            var startPoint = textView.GetVisualPosition(new TextViewPosition(textView.Document.GetLocation(startOffset)), VisualYPosition.LineBottom) - textView.ScrollOffset;
            var endPoint = textView.GetVisualPosition(new TextViewPosition(textView.Document.GetLocation(endOffset)), VisualYPosition.LineBottom) - textView.ScrollOffset;

            Pen pen = new(underlineColor, 1);
            double offset = 2.5;
            double waveLength = offset * 2;
            StreamGeometry geometry = new();
            using (StreamGeometryContext ctx = geometry.Open())
            {
                double x = startPoint.X;

                while (x < endPoint.X)
                {
                    double nextX = x + waveLength; // 下一个波段的结束x坐标
                    if (nextX > endPoint.X)
                    {
                        nextX = endPoint.X; //如果下一个波段会超过endPoint.X，调整它，使得不会绘制过界
                    }

                    double midX = (x + nextX) / 2; // 当前波段的中点x坐标

                    // 绘制从当前开始点到下一个波段的顶点
                    ctx.BeginFigure(new Point(x, startPoint.Y), false, false);
                    ctx.QuadraticBezierTo(
                        new Point(midX, startPoint.Y - offset), // 波谷在中点上方offset个单位
                        new Point(nextX, startPoint.Y), true, false);

                    x = nextX; // 更新x到下一个波段的开始点
                }
            }

            geometry.Freeze();
            drawingContext.DrawGeometry(null, pen, geometry);
        }
    }
}
