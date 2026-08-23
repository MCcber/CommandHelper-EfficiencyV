using CBHK.Interface.Data;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;

namespace CBHK.Utility.Data.DTOBuilder
{
    public class ReferenceDTOBuilder(Resource resource, MCDocumentMetaTypeDTOHelper helper, DocumentDTOBuildStrategyRegistry registry) : IDocumentDTOBuildStrategy
    {
        #region Field
        private readonly Resource resource = resource;
        private readonly MCDocumentMetaTypeDTOHelper helper = helper;
        private readonly DocumentDTOBuildStrategyRegistry registry = registry;
        #endregion

        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, DocumentPath documentPath, Dictionary<string, KeyValueAnchors> anchorMap, bool justSetView = false, string typeName = "")
        {
            if (target.Value is not null)
            {
                string targetReferenceValue = target.Value.ToString();
                string basePath = target.Path?.ToString() ?? "";
                if (!string.IsNullOrEmpty(basePath))
                {
                    int lastSeparatorIndex = basePath.LastIndexOf("::");
                    if (lastSeparatorIndex > -1)
                    {
                        string targetReferenceKey = basePath[..lastSeparatorIndex] + "::" + targetReferenceValue;
                        if (resource.DocumentItemMap.TryGetValue(targetReferenceKey, out MetaTypeEditorFieldDTO referencedDTO) && referencedDTO is not null)
                        {
                            var instanceDTO = helper.InstantiateDTO(referencedDTO, version);
                            MCDocumentResourceBuilder.BaseDataHandler(instanceDTO);
                            MCDocumentResourceBuilder.BuildResource(instanceDTO, referencedDTO, version, documentPath, resource, helper);
                            var instanceRegistry = registry.Get(referencedDTO.TypeKind);
                            instanceRegistry.Build(instanceDTO, referencedDTO, version, documentPath, anchorMap, justSetView);
                            target.Children ??= [];
                            target.Children.Add(instanceDTO);
                        }
                    }
                }
            }
        }

        public bool CanHandle(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.Reference;
        }
    }
}
