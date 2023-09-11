using cbhk_environment.CustomControls;
using System.Windows.Documents;

namespace cbhk_environment.GeneralTools
{
    public static class RichTextBoxTools
    {
        public static bool IsEndOfBlock(this TextPointer position)
        {
            for (; position != null; position = position.GetNextContextPosition(LogicalDirection.Forward))
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
