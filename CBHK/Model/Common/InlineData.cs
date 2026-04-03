using System.Windows;
using System.Windows.Media;
using Windows.Devices.Enumeration.Pnp;

namespace CBHK.Model.Common
{
    public class InlineData
    {
        public string Text { get; set; }
        public FontFamily FontFamily { get; set; }
        public FontStyle FontStyle { get; set; }
        public FontWeight FontWeight { get; set; }
        public TextDecorationCollection TextDecorationCollection { get; set; }

        private bool isBold = false;
        public bool IsBold 
        {
            get => isBold;
            set
            {
                isBold = FontWeight == FontWeights.Bold;
            }
        }

        private bool isItalic = false;
        public bool IsItalic
        {
            get => isItalic;
            set
            {
                isItalic = FontStyle == FontStyles.Italic;
            }
        }

        private bool isUnderline = false;
        public bool IsUnderline
        {
            get => isUnderline;
            set
            {
                isUnderline = TextDecorationCollection == TextDecorations.Underline;
            }
        }

        private bool isStrikeThrough = false;
        public bool IsStrikeThrough
        {
            get => isStrikeThrough;
            set
            {
                isStrikeThrough = TextDecorationCollection == TextDecorations.Strikethrough;
            }
        }

        public Brush Foreground { get; set; }

        public Brush DisplayPanelBrush { get; set; } = Brushes.Black;
        public Brush MemberBrush { get; set; } = Brushes.White;
    }
}