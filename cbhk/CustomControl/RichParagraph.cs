using System.Windows.Documents;

namespace CBHK.CustomControl
{
    public class RichParagraph:Paragraph
    {
        public RichParagraph()
        {
            Inlines.Add(new RichRun());
        }
    }
}