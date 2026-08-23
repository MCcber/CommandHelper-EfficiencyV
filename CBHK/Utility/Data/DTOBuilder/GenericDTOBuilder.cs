using CBHK.Interface.Data;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using MinecraftLanguageModelLibrary.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CBHK.Utility.Data.DTOBuilder
{
    public class GenericDTOBuilder(Resource resource, MCDocumentMetaTypeDTOHelper helper, DocumentDTOBuildStrategyRegistry registry) : IDocumentDTOBuildStrategy
    {
        #region Field
        private readonly Resource resource = resource;
        private readonly MCDocumentMetaTypeDTOHelper helper = helper;
        private readonly DocumentDTOBuildStrategyRegistry registry = registry;
        #endregion

        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, DocumentPath documentPath, Dictionary<string, KeyValueAnchors> anchorMap, bool justSetView = false, string typeName = "")
        {
            //为可选则直接返回
            if (target.IsRequired)
            {
                if (target.Parent is not null)
                {
                    target.Parent.Value = target.FieldName ?? target.Value;
                }
            }

            string targetTypeName = target.TypeName ?? typeName;
            //需要精确搜索目标
            if(documentPath is null)
            {
                return;
            }
            var targetContext = UsePathParser.Parse(resource,documentPath, targetTypeName);
            string documentItemPathString = documentPath.TargetPath.ToString();

            if (targetContext.DTO is MetaTypeEditorFieldDTO targetTypeDTO)
            {
                List<Tuple<string, MetaValue>> formalParamMap = targetTypeDTO.TypeParameterNameList;
                List<Tuple<string, MetaValue>> actualArgMap = [];
                if(target.TypeParameterNameList is not null)
                {
                    actualArgMap = target.TypeParameterNameList;
                }
                if (formalParamMap.Count == actualArgMap.Count)
                {
                    var substituteResult = helper.SubstituteGenericIterative(targetTypeDTO, formalParamMap, actualArgMap, version);
                    MCDocumentMetaTypeDTOHelper.VerifyVersion(substituteResult, version);
                    List<MetaTypeEditorFieldDTO> expandedChildrenList = [..substituteResult.Where(item => item.IsVisible)];

                    for (int i = 0; i < expandedChildrenList.Count; i++)
                    {
                        MCDocumentResourceBuilder.BaseDataHandler(expandedChildrenList[i]);
                        MCDocumentResourceBuilder.BuildResource(expandedChildrenList[i], expandedChildrenList[i], version, documentPath, resource, helper);
                        var childRegistry = registry.Get(expandedChildrenList[i].TypeKind);
                        childRegistry.Build(expandedChildrenList[i], expandedChildrenList[i], version, documentPath, anchorMap, justSetView, targetTypeName);
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