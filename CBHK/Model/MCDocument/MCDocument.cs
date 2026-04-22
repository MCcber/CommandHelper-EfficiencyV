using System.Collections.Generic;

namespace CBHK.Model.MCDocument
{
    public class MCDocument
    {
        public List<string> UseReferenceList { get; set; }
        public List<MCDocumentStruct> StructList { get; set; }
        public List<MCDocumentDispatch> DispatchList { get; set; }
    }
}
