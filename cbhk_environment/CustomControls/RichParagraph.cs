using System.Windows.Documents;

namespace cbhk_environment.CustomControls
{
    public class RichParagraph:Paragraph
    {
        public RichParagraph()
        {
            Inlines.Add(new RichRun());
        }
    }
}
