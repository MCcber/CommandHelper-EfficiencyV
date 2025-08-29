using System.Collections.Generic;

namespace CBHK.Model.Common
{
    public class MCDocumentField
    {
        public List<MCDocumentSummary> SummaryList { get; set; }
        public List<MCDocumentAttribute> AttributeList { get; set; }
    }
}