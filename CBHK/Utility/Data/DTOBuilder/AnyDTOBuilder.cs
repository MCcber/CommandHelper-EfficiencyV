using CBHK.Interface.Data;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;
using System.Text;

namespace CBHK.Utility.Data.DTOBuilder
{
    public class AnyDTOBuilder(Resource resource, MCDocumentMetaTypeDTOHelper helper, DocumentDTOBuildStrategyRegistry registry) : IDocumentDTOBuildStrategy
    {
        #region Field
        private readonly Resource resource = resource;
        private readonly MCDocumentMetaTypeDTOHelper helper = helper;
        private readonly DocumentDTOBuildStrategyRegistry registry = registry;
        #endregion

        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, StringBuilder docPath, Dictionary<string, KeyValueAnchors> anchorMap)
        {
            
        }

        public bool CanHandle(MetaTypeKind kind)
        {
            return true;
        }
    }
}
