using CBHK.Interface.Data;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;
using System.Text;

namespace CBHK.Utility.Data.DTOBuilder
{
    public class ReferenceDTOBuilder(Resource resource, MCDocumentMetaTypeDTOHelper helper, DocumentDTOBuildStrategyRegistry registry) : IDocumentDTOBuildStrategy
    {
        #region Field
        private readonly Resource resource = resource;
        private readonly MCDocumentMetaTypeDTOHelper helper = helper;
        private readonly DocumentDTOBuildStrategyRegistry registry = registry;
        #endregion

        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, StringBuilder documentItemPath, Dictionary<string, KeyValueAnchors> anchorMap)
        {
            if (target.Value is not null)
            {
                string targetReferenceValue = target.Value.ToString();
                string basePath = target.DocumentItemPath?.ToString() ?? "";
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
                            MCDocumentResourceBuilder.BuildResource(instanceDTO, referencedDTO, version, documentItemPath, resource, helper);
                            var instanceRegistry = registry.Get(referencedDTO.TypeKind);
                            instanceRegistry.Build(instanceDTO, referencedDTO, version, documentItemPath, anchorMap);
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
