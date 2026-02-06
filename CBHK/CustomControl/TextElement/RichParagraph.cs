using System.Windows.Documents;

namespace CBHK.CustomControl.TextElement
{
    public class RichParagraph:Paragraph
    {
        public RichParagraph()
        {
            Inlines.Add(new RichRun());
        }
    }
}