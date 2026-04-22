using CBHK.CustomControl;
using CBHK.CustomControl.TextElement;
using System.Windows.Documents;

namespace CBHK.Utility.Common
{
    public static class RichTextBoxTools
    {
        public static bool IsEndOfBlock(this TextPointer position)
        {
            for (; position is not null; position = position.GetNextContextPosition(LogicalDirection.Forward))
            {
                switch (position.GetPointerContext(LogicalDirection.Forward))
                {
                    case TextPointerContext.ElementEnd:
                        var element = position.GetAdjacentElement(LogicalDirection.Forward);
                        if (element is Paragraph || element is RichParagraph) return true;
                        break;

                    default:
                        return false;
                }
            }
            return false;
        }
    }
}
