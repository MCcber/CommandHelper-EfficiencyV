using CBHK.Interface.Data;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using MinecraftLanguageModelLibrary.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace CBHK.Utility.Data.DTOBuilder
{
    public class UnionDTOBuilder(Resource resource, MCDocumentMetaTypeDTOHelper helper, DocumentDTOBuildStrategyRegistry registry) : IDocumentDTOBuildStrategy
    {
        #region Field
        private readonly Resource resource = resource;
        private readonly MCDocumentMetaTypeDTOHelper helper = helper;
        private readonly DocumentDTOBuildStrategyRegistry registry = registry;
        #endregion

        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, StringBuilder documentItemPath, Dictionary<string, KeyValueAnchors> anchorMap)
        {
            #region 验证首个元素
            List<MetaTypeEditorFieldDTO> verifiedDTOList = [];
            List<MetaTypeEditorFieldDTO> templateChildren = [];
            MCDocumentMetaTypeDTOHelper.VerifyVersion([.. template.Children], version);
            templateChildren = [.. template.Children.Where(item => item.IsVisible)];
            if (templateChildren?.Count > 0)
            {
                var instance = helper.InstantiateDTO(templateChildren[0], version);
                MCDocumentResourceBuilder.BaseDataHandler(instance);
                MCDocumentResourceBuilder.BuildResource(instance, templateChildren[0], version, documentItemPath, resource, helper);
                var childRegistry = registry.Get(instance.TypeKind);
                childRegistry.Build(instance, templateChildren[0], version, documentItemPath, anchorMap);
                verifiedDTOList.Add(instance);
                if (templateChildren.Count > 1)
                {
                    verifiedDTOList.AddRange(templateChildren[1..]);
                }
            }
            #endregion

            #region 通过验证的成员数大于0时处理不同情况
            if (verifiedDTOList.Count > 0)
            {
                target.Children = [.. verifiedDTOList];
                //确保联合体节点有默认选中项
                if (target.UnionTypeNameList is not null && target.UnionTypeNameList.Count > 0 && target.SelectedUnionTypeName is null && !verifiedDTOList.Any(item => item.TypeKind is (MetaTypeKind.List or MetaTypeKind.ByteArray or MetaTypeKind.IntArray or MetaTypeKind.LongArray)))
                {
                    target.SelectedUnionTypeName = target.UnionTypeNameList[0];
                    target.SelectedUnionItemUpdated = () => helper.SelectedUnionItemUpdated(target, version);
                }
                //拥有多个子级且至少有一个子级不是容器类型，则将当前节点提升为复合类型，并将第一个子级作为联合体的默认选中项
                else if (verifiedDTOList.Count > 1)
                {
                    target.TypeKind = MetaTypeKind.Composite;

                    MetaTypeEditorFieldDTO unionItem = new()
                    {
                        FieldName = target.FieldName,
                        ID = Guid.NewGuid().ToString(),
                        TypeKind = MetaTypeKind.Union,
                        Children = [.. target.Children],
                        Parent = target,
                        UnionTypeNameList = [.. target.UnionTypeNameList]
                    };
                    unionItem.SelectedUnionTypeName = unionItem.UnionTypeNameList[0];
                    unionItem.SelectedUnionItemUpdated = () => helper.SelectedUnionItemUpdated(unionItem, version);
                    target.Items ??= [];
                    target.Items.Add(unionItem);
                    if (MCDocumentMetaTypeDTOHelper.IsContainerType(target.Children[0].TypeKind))
                    {
                        target.SelectedUnionChildren = target.Children[0].Children;
                    }
                    else
                    {
                        target.Items.Add(target.Children[0]);
                    }
                    target.UnionTypeNameList?.Clear();
                    target.SelectedUnionChildren?.Clear();
                    target.SelectedUnionTypeName = null;
                    target.Children.Clear();
                }
            }
            #endregion

            #region 验证后只有一个子节点，则转换类型
            if (verifiedDTOList.Count == 1 && target.Children?.Count == 1)
            {
                target.UnionTypeNameList.Clear();
                target.SelectedUnionChildren = null;
                target.OriginKind = target.TypeKind;
                target.TypeKind = MetaTypeKind.Struct;
            }
            #endregion
        }

        public bool CanHandle(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.Union;
        }
    }
}
