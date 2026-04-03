using System.Collections.Generic;

namespace CBHK.Model.MCDocument
{
    public class MCDocumentField
    {
        public List<MCDocumentSummary> SummaryList { get; set; }
        public List<MCDocumentAttribute> AttributeList { get; set; }
    }
}