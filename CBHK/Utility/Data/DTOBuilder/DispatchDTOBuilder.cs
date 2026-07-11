using CBHK.Interface.Data;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;
using System.Text;

namespace CBHK.Utility.Data.DTOBuilder
{
    public class DispatchDTOBuilder(Resource resource, MCDocumentMetaTypeDTOHelper helper, DocumentDTOBuildStrategyRegistry registry) : IDocumentDTOBuildStrategy
    {
        #region Field
        private readonly Resource resource = resource;
        private readonly MCDocumentMetaTypeDTOHelper helper = helper;
        private readonly DocumentDTOBuildStrategyRegistry registry = registry;
        #endregion

        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, StringBuilder docPath, Dictionary<string, KeyValueAnchors> anchorMap)
        {
            if (target.FeatureMap.ContainsKey("Resource") && target.FeatureMap.ContainsKey("Index") && string.IsNullOrEmpty(target.TypeName))
            {
                target.IsTransformFromDispatch = true;
                helper.GetDispatchResource(target, version);
            }
        }

        public bool CanHandle(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.Dispatch;
        }
    }
}
