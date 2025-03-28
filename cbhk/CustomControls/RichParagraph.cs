using System.Windows.Documents;

namespace CBHK.CustomControls
{
    public class RichParagraph:Paragraph
    {
        public RichParagraph()
        {
            Inlines.Add(new RichRun());
        }
    }
}