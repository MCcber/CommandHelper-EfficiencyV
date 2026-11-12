using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace CBHK.Model.Data
{
    public class InlineData
    {
        public string Text { get; set; }
        public FontFamily FontFamily { get; set; }
        public FontStyle FontStyle { get; set; }
        public FontWeight FontWeight { get; set; }
        public TextDecorationCollection TextDecorationCollection { get; set; }

        public bool IsBold => FontWeight == FontWeights.Bold;
        public bool IsItalic => FontStyle == FontStyles.Italic;
        public bool IsUnderline => TextDecorationCollection?.Any(decoration => decoration.Location == TextDecorationLocation.Underline) ?? false;
        public bool IsStrikeThrough => TextDecorationCollection?.Any(decoration => decoration.Location == TextDecorationLocation.Strikethrough) ?? false;

        /// <summary>
        /// 混淆标记只能从附加属性读出：ObfuscatedAdorner 每帧都在改写 Run.Foreground，无法由颜色或字体反推
        /// </summary>
        public bool IsObfuscated { get; set; }

        public Brush Foreground { get; set; }

        public Brush DisplayPanelBrush { get; set; } = Brushes.Black;
        public Brush MemberBrush { get; set; } = Brushes.White;
    }
}