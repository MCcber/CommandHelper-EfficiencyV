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
    public class UnionDTOBuilder(Resource resource, MCDocumentMetaTypeDTOHelper helper, DocumentDTOBuildStrategyRegistry registry) : IDocumentDTOBuildStrategy
    {
        #region Field
        private readonly Resource resource = resource;
        private readonly MCDocumentMetaTypeDTOHelper helper = helper;
        private readonly DocumentDTOBuildStrategyRegistry registry = registry;
        #endregion

        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, DocumentPath documentPath, Dictionary<string, KeyValueAnchors> anchorMap, bool justSetView = false, string typeName = "")
        {
            #region Field
            int index = 0;
            bool isHaveArrayOrList = false, isContainerOrReference = true, isUnion = false;
            ResolvedTypeReference realData = new("", default);
            string targetUsePath = string.Empty;
            MetaTypeEditorFieldDTO targetDTO = null;
            if (template.Children is null || template.Children?.Count == 0)
            {
                return;
            }
            #endregion

            #region 去除版本之外的节点
            if(target.Children is null || target.Children.Count == 0)
            {
                return;
            }

            MCDocumentMetaTypeDTOHelper.VerifyVersion([.. target.Children], version);
            for (index = 0; index < target.Children.Count; index++)
            {
                if (!target.Children[index].IsVisible)
                {
                    target.Children.RemoveAt(index);
                    index--;
                }
            }
            #endregion

            #region 仍然是联合体则执行展平
            index = 0;
            do
            {
                isUnion = index > 0;

                //搜索真实的路径与DTO实例
                if (!string.IsNullOrEmpty(target.Children[index].Value?.ToString()))
                {
                    realData = UsePathParser.Parse(resource, target.Children[index].Path ?? documentPath, target.Children[index].Value.ToString());
                    targetUsePath = realData.Path;
                    targetDTO = realData.DTO;
                }

                //若已提前替换则直接赋值
                if (target.Children[index].TypeKind is not MetaTypeKind.Literal && targetDTO is null)
                {
                    targetDTO = target.Children[index];
                    targetUsePath ??= target.Children[index].Path?.ToString();
                }

                if (targetDTO is not null)
                {
                    if (!isHaveArrayOrList && targetDTO.TypeKind is (MetaTypeKind.List or MetaTypeKind.ByteArray or MetaTypeKind.IntArray or MetaTypeKind.LongArray))
                    {
                        isHaveArrayOrList = true;
                    }

                    var childRegistry = registry.Get(targetDTO.TypeKind);
                    if (targetDTO.TemplateReference is null)
                    {
                        targetDTO = helper.InstantiateDTO(targetDTO, version);
                    }

                    MCDocumentResourceBuilder.BaseDataHandler(targetDTO);
                    MCDocumentResourceBuilder.BuildResource(targetDTO, targetDTO, version, targetDTO.Path, resource, helper);
                    childRegistry.Build(targetDTO, targetDTO, version, targetDTO.Path ?? documentPath, anchorMap, true, typeName);

                    if (MCDocumentMetaTypeDTOHelper.IsContainerType(targetDTO.TypeKind))
                    {
                        targetDTO.DisplayName = targetDTO.FieldName;
                        targetDTO.FieldName = "";
                    }
                    target.Children[index] = targetDTO;

                    //处理子联合体
                    if (targetDTO.TypeKind is MetaTypeKind.Union && targetDTO.Children?.Count > 1)
                    {
                        target.Children.RemoveAt(index);
                        int insertIndex = index;
                        for (int i = 0; i < targetDTO.Children.Count; i++)
                        {
                            if (!MCDocumentMetaTypeDTOHelper.IsIndirectType(targetDTO.Children[i].TypeKind))
                            {
                                targetDTO.Children[i].Path = new(targetUsePath);
                            }
                            target.Children.Insert(insertIndex, targetDTO.Children[i]);
                            insertIndex++;
                        }
                        index += targetDTO.Children.Count - 1;
                    }

                    #region 判断是否为容器或引用
                    if (!isContainerOrReference && (MCDocumentMetaTypeDTOHelper.IsContainerType(targetDTO.TypeKind) || MCDocumentMetaTypeDTOHelper.IsIndirectType(targetDTO.TypeKind)) && targetDTO.TypeKind is not (MetaTypeKind.List or MetaTypeKind.ByteArray or MetaTypeKind.IntArray or MetaTypeKind.LongArray))
                    {
                        isContainerOrReference = true;
                    }
                    #endregion
                }
                //执行资源解释
                else
                {
                    if (!isHaveArrayOrList && target.Children[index].TypeKind is (MetaTypeKind.List or MetaTypeKind.ByteArray or MetaTypeKind.IntArray or MetaTypeKind.LongArray))
                    {
                        isHaveArrayOrList = true;
                    }
                    MCDocumentResourceBuilder.BaseDataHandler(target.Children[index]);
                    MCDocumentResourceBuilder.BuildResource(target.Children[index], target.Children[index], version, target.Children[index].Path, resource, helper);
                }
                targetUsePath = string.Empty;
                targetDTO = null;
            }
            while (++index < target.Children.Count && target.Children.Count > 1);
            #endregion

            #region 处理默认选中的数据
            if(!isUnion)
            {
                return;
            }
            target.UnionTypeNameList ??= [];
            target.UnionTypeNameList?.Clear();
            //处理可选节点
            if (!target.IsRequired && (target.UnionTypeNameList?.Count > 0 && target.UnionTypeNameList[0].Name != "- unset -" || target.UnionTypeNameList?.Count == 0))
            {
                target.UnionTypeNameList.Insert(0, new EnumMember() { Name = "- unset -", Value = new() { Kind = MetaValueKind.Literal, LiteralValue = "unset" } });
            }
            //根据成员类型计算所有联合体名称
            if (target.Children?.Count > 1)
            {
                List<string> unionNameTypeList = UnionTypeNameParser.Parse([.. target.Children]);
                target.UnionTypeNameList.AddRange([.. unionNameTypeList.Select(item => new EnumMember() { Name = item, Value = new MetaValue() { Kind = MetaValueKind.Literal, LiteralValue = item } })]);
            }
            //确保联合体节点有默认选中项
            if (!isHaveArrayOrList || !isContainerOrReference)
            {
                target.SelectedUnionTypeName = target.UnionTypeNameList[0];
                target.SelectedUnionItemUpdated = () => helper.SelectedUnionItemUpdated(target, version);
            }
            //拥有多个子级且至少有一个子级不是容器类型，则将当前节点提升为复合类型，并将第一个子级作为联合体的默认选中项
            else if (isHaveArrayOrList)
            {
                target.TypeKind = MetaTypeKind.Composite;

                MetaTypeEditorFieldDTO unionItem = new()
                {
                    FieldName = target.FieldName,
                    Path = target.Path,
                    ID = Guid.NewGuid().ToString(),
                    TypeKind = MetaTypeKind.Union,
                    Children = target.Children,
                    Parent = target,
                    UnionTypeNameList = target.UnionTypeNameList
                };
                unionItem.SelectedUnionTypeName = unionItem.UnionTypeNameList[0];
                unionItem.SelectedUnionItemUpdated = () => helper.SelectedUnionItemUpdated(unionItem, version);
                target.Items ??= [];
                target.Items.Add(unionItem);

                target.SelectedUnionItemUpdated = null;
                target.SelectedUnionChildren ??= [];
                target.SelectedUnionChildren.Clear();
                target.SelectedUnionTypeName = null;
            }

            if (target.IsRequired)
            {
                if (MCDocumentMetaTypeDTOHelper.IsContainerType(target.Children[0].TypeKind))
                {
                    target.SelectedUnionChildren = target.Children[0].Children;
                }
                else if (target.Items is not null)
                {
                    target.Items.Add(target.Children[0]);
                }
                else
                {
                    target.SelectedUnionChildren = new([target.Children[0]]);
                }
            }
            #endregion
        }

        public bool CanHandle(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.Union;
        }
    }
}