using CBHK.Interface.Data;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace CBHK.Utility.Data.DTOBuilder
{
    public class GenericDTOBuilder(Resource resource, MCDocumentMetaTypeDTOHelper helper, DocumentDTOBuildStrategyRegistry registry) : IDocumentDTOBuildStrategy
    {
        #region Field
        private readonly Resource resource = resource;
        private readonly MCDocumentMetaTypeDTOHelper helper = helper;
        private readonly DocumentDTOBuildStrategyRegistry registry = registry;
        #endregion

        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, StringBuilder documentItemPath, Dictionary<string, KeyValueAnchors> anchorMap)
        {
            string targetTypeName = target.TypeName ?? "";
            KeyValuePair<string, MetaTypeEditorFieldDTO> pair = resource.DocumentItemMap.FirstOrDefault(pair => pair.Key.EndsWith("::" + targetTypeName));
            if (pair.Value is MetaTypeEditorFieldDTO targetTypeDTO)
            {
                List<string> formalParamList = [.. targetTypeDTO.TypeParameterNameList];
                List<string> actualArgList = [.. target.TypeParameterNameList];
                if (formalParamList.Count == actualArgList.Count)
                {
                    var substituteResult = helper.SubstituteGenericIterative(targetTypeDTO, formalParamList, actualArgList, version);
                    MCDocumentMetaTypeDTOHelper.VerifyVersion(substituteResult, version);
                    List<MetaTypeEditorFieldDTO> expandedChildrenList = [..substituteResult.Where(item => item.IsVisible)];

                    for (int i = 0; i < expandedChildrenList.Count; i++)
                    {
                        //优先解析资源数据
                        MCDocumentResourceBuilder.BaseDataHandler(expandedChildrenList[i]);
                        MCDocumentResourceBuilder.BuildResource(expandedChildrenList[i], expandedChildrenList[i], version, documentItemPath, resource, helper);
                        var childRegistry = registry.Get(expandedChildrenList[i].TypeKind);
                        childRegistry.Build(expandedChildrenList[i], expandedChildrenList[i], version, documentItemPath, anchorMap);
                    }

                    List<MetaTypeEditorFieldDTO> verifiedDTOList = expandedChildrenList;
                    target.TypeKind = targetTypeDTO.TypeKind;
                    if (targetTypeDTO.UnionTypeNameList is not null)
                    {
                        target.UnionTypeNameList = targetTypeDTO.UnionTypeNameList;
                    }
                    if (targetTypeDTO.FeatureMap is not null)
                    {
                        target.FeatureMap = targetTypeDTO.FeatureMap;
                    }
                    if (targetTypeDTO.Value is not null)
                    {
                        target.Value = targetTypeDTO.Value;
                    }
                    target.IsTrue = targetTypeDTO.IsTrue;
                    target.IsFalse = targetTypeDTO.IsFalse;

                    target.Children = new ObservableCollection<MetaTypeEditorFieldDTO>(verifiedDTOList);
                }
            }
        }

        public bool CanHandle(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.Generic;
        }
    }
}
