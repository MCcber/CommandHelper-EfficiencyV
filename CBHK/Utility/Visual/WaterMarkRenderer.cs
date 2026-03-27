using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CBHK.Utility.Visual
{
    /// <summary>
    /// 在指定位置之后渲染文本水印
    /// </summary>
    /// <param name="textToRender"></param>
    /// <param name="startLocation"></param>
    /// <param name="typeface"></param>
    /// <param name="fontSize"></param>
    /// <param name="foregroundBrush"></param>
    public class WaterMarkRenderer(string textToRender, TextLocation startLocation, Typeface typeface, double fontSize, SolidColorBrush foregroundBrush) : IBackgroundRenderer
    {
        public KnownLayer Layer => KnownLayer.Selection; // Draw on the selection layer

        private readonly string textToRender = textToRender;
        private readonly TextLocation startLocation = startLocation;
        private readonly Typeface typeface = typeface;
        private readonly double fontSize = fontSize;
        private readonly SolidColorBrush foregroundBrush = foregroundBrush;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (textView is null || textView.VisualLinesValid == false)
                return;

            int startOffset = textView.Document.GetOffset(startLocation.Line, startLocation.Column);
            //int endOffset = startOffset + textToRender.Length;
            VisualLine vl = textView.GetVisualLine(startLocation.Line);

            if (vl is null) return;

            int visualColumn = vl.GetVisualColumn(startOffset);
            double topLeft = vl.GetTextLineVisualXPosition(vl.TextLines[0], visualColumn);

            FormattedText formattedText = new(
                textToRender,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                foregroundBrush, 1);

            drawingContext.DrawText(formattedText, new Point(topLeft, vl.VisualTop - textView.ScrollOffset.Y));
        }
    }
}
