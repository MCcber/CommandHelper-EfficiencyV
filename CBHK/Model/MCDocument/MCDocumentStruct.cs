using System.Collections.Generic;

namespace CBHK.Model.MCDocument
{
    public class MCDocumentStruct
    {
        public string StructName { get; set; }
        public List<MCDocumentField> FieldList { get; set; }
    }
}