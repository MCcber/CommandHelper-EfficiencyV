using System.Windows.Documents;

namespace cbhk.CustomControls
{
    public class RichParagraph:Paragraph
    {
        public RichParagraph()
        {
            Inlines.Add(new RichRun());
        }
    }
}
