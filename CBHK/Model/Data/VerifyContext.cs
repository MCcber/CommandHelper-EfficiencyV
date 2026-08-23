using CBHK.Model.Constant;
using CBHK.Utility.Data;
using System.Collections.Generic;

namespace CBHK.Model.Data
{
    public class VerifyContext
    {
        public string Version { get; set; }
        public Dictionary<string,List<string>> DocumentItemMap { get; set; }
        public MCDocumentMetaTypeDTOHelper DTOHelper { get; set; }
        public Dictionary<string, KeyValueAnchors> AnchorMap { get; set; }
        public Resource Resource { get; set; }
    }
}
