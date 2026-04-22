using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows;
using System.Windows.Media;

namespace CBHK.Utility.Visual
{
    /// <summary>
    /// 用于在指定位置的文本下方渲染虚线
    /// </summary>
    /// <param name="startOffset"></param>
    /// <param name="endOffset"></param>
    /// <param name="underlineColor"></param>
    public class DotUnderlineBackgroundRenderer(TextLocation textLocation, int length, SolidColorBrush underlineColor) : IBackgroundRenderer
    {
        public KnownLayer Layer => KnownLayer.Selection;

        private readonly SolidColorBrush underlineColor = underlineColor;
        private readonly TextLocation textLocation = textLocation;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            textView.EnsureVisualLines();
            var visualLines = textView.VisualLines;
            if (visualLines.Count == 0)
                return;

            // 创建一个TextViewPosition对象
            TextViewPosition position = new(textLocation);
            Point coordinates = textView.GetVisualPosition(position, VisualYPosition.LineMiddle);
            coordinates.Y += 6;
            coordinates.X += 5;

            Pen pen = new(underlineColor, 1);
            for (int i = 0; i < length; i++)
            {
                drawingContext.DrawEllipse(underlineColor, pen, coordinates, 0.5, 0.5);
                coordinates.X += 5;
            }
        }
    }
}
