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

        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, DocumentPath documentPath, Dictionary<string, KeyValueAnchors> anchorMap, RenderDepth depth, string typeName = "")
        {
            #region Field
            int index = 0;
            bool isListOrArrayOrValueType = false, isContainerOrReference = true, isUnion = false;
            ResolvedTypeReference realData = new("", default);
            string targetUsePath = string.Empty;
            MetaTypeEditorFieldDTO targetTemplateDTO = null;
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
                    //解析不到真实文档项时按成员自身处理
                    if (realData?.Item is not null)
                    {
                        targetUsePath = realData.Path;
                        targetTemplateDTO = realData.Item;
                        targetDTO = helper.InstantiateDTO(targetTemplateDTO, version);
                        //新建实例继承上下文：父级、动态 Map 的键载荷、绑定作用域
                        targetDTO.Parent ??= target;
                        targetDTO.Items ??= target.Children[index].Items;
                        targetDTO.BindingScope ??= target.BindingScope;
                    }
                }

                //若已提前替换则直接赋值
                if (target.Children[index].TypeKind is not MetaTypeKind.Literal && targetDTO is null)
                {
                    targetDTO = target.Children[index];
                    targetUsePath ??= target.Children[index].Path?.ToString();
                }

                if (targetDTO is not null)
                {
                    if (!isListOrArrayOrValueType && MCDocumentMetaTypeDTOHelper.IsListOrArrayOrValueType(targetDTO.TypeKind))
                    {
                        isListOrArrayOrValueType = true;
                    }

                    MCDocumentResourceBuilder.BaseDataHandler(targetDTO);
                    MCDocumentResourceBuilder.BuildResource(targetDTO, targetDTO, version, targetDTO.Path, resource, helper);
                    registry.Get(targetDTO.TypeKind).Build(targetDTO, targetDTO, version, targetDTO.Path ?? documentPath, anchorMap, depth, typeName);

                    if (MCDocumentMetaTypeDTOHelper.IsContainerType(targetDTO.TypeKind))
                    {
                        //容器分支保留字段名，避免被当成展开残余而拍平掉自己的子结构
                        targetDTO.DisplayName = targetDTO.FieldName;
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
                    if (!isListOrArrayOrValueType && MCDocumentMetaTypeDTOHelper.IsListOrArrayOrValueType(target.Children[index].TypeKind))
                    {
                        isListOrArrayOrValueType = true;
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
            //根据成员类型计算所有联合体名称，并给无名分支补上分支名，避免被当成展开残余而拍平
            if (target.Children?.Count > 1)
            {
                List<string> unionNameTypeList = UnionTypeNameParser.Parse([.. target.Children]);
                target.UnionTypeNameList.AddRange([.. unionNameTypeList.Select(item => new EnumMember() { Name = item, Value = new MetaValue() { Kind = MetaValueKind.Literal, LiteralValue = item } })]);
                for (int i = 0; i < target.Children.Count && i < unionNameTypeList.Count; i++)
                {
                    if (string.IsNullOrEmpty(target.Children[i].FieldName))
                    {
                        target.Children[i].FieldName = unionNameTypeList[i];
                    }
                }
            }
            //确保联合体节点有默认选中项
            if (!isListOrArrayOrValueType || !isContainerOrReference)
            {
                // 防御：某些动态分支可能被版本过滤或 accessor 裁剪后暂时没有 union 名称，
                // 此时不能直接访问 [0]，否则会抛 ArgumentOutOfRangeException。
                if (target.UnionTypeNameList?.Count > 0)
                {
                    target.SelectedUnionTypeName = target.UnionTypeNameList[0];
                }
                target.SelectedUnionItemUpdated = () => helper.SelectedUnionItemUpdated(target, version);
            }
            //拥有多个子级且至少有一个子级不是容器类型，则将当前节点提升为复合类型，并将第一个子级作为联合体的默认选中项
            else if (isListOrArrayOrValueType)
            {
                #region 分支类型完全相同时直接收敛
                //例如颜色字段 color?: ( #[color="hex_rgb"] string | #[color="named"] TextColor )，
                //两个分支都会命中颜色字典、变成同一个类型；此时字段不需要联合外壳，
                //直接采用该类型自己的编辑形态，字段名与必选性保持不变。
                bool isSameLeafKind = target.Children[0].Children is null
                    && target.Children.All(child => child.TypeKind == target.Children[0].TypeKind)
                    && target.Children[0].TypeKind is MetaTypeKind.NamedColor or MetaTypeKind.String or MetaTypeKind.Int
                        or MetaTypeKind.Float or MetaTypeKind.Boolean or MetaTypeKind.Enum;
                if (isSameLeafKind)
                {
                    string keepFieldName = target.FieldName;
                    bool keepRequired = target.IsRequired;
                    target.CopyFrom(target.Children[0]);
                    target.FieldName = keepFieldName;
                    target.SetRequired(keepRequired);
                    return;
                }
                #endregion

                target.TypeKind = MetaTypeKind.Composite;

                MetaTypeEditorFieldDTO unionItem = new()
                {
                    //行内选择器由自身模板显示字段名
                    FieldName = target.FieldName,
                    Path = target.Path,
                    ID = Guid.NewGuid().ToString(),
                    TypeKind = MetaTypeKind.Union,
                    Children = target.Children,
                    Parent = target,
                    UnionTypeNameList = target.UnionTypeNameList
                };
                unionItem.SelectedUnionTypeName = unionItem.UnionTypeNameList[0];
                unionItem.SelectedUnionChildren = target.Children[0].Children is not null
                    ? [.. target.Children[0].Children]
                    : [target.Children[0]];
                unionItem.SetRequired(target.IsRequired);
                unionItem.SelectedUnionItemUpdated = () => helper.SelectedUnionItemUpdated(unionItem, version);
                target.Items ??= [];
                target.Items.Add(unionItem);

                target.SelectedUnionItemUpdated = null;
                target.SelectedUnionChildren ??= [];
                target.SelectedUnionChildren.Clear();
                target.SelectedUnionTypeName = null;
            }

            //处理必选节点
            if (target.IsRequired)
            {
                MetaTypeEditorFieldDTO firstBranch = target.Children[0];
                bool isLazyStub = firstBranch.Children?.Count == 1 && MCDocumentMetaTypeDTOHelper.IsPlaceHolderNode(firstBranch.Children[0]);
                if (isLazyStub)
                {
                    //懒加载桩不能当内容挂，挂分支自身，展开时再物化
                    target.SelectedUnionChildren = [firstBranch];
                }
                else if (MCDocumentMetaTypeDTOHelper.IsContainerType(firstBranch.TypeKind))
                {
                    target.SelectedUnionChildren = firstBranch.Children is null ? new ObservableCollection<MetaTypeEditorFieldDTO>() : new ObservableCollection<MetaTypeEditorFieldDTO>(firstBranch.Children);
                }
                else if (target.Items is not null)
                {
                    var firstSubDTO = helper.InstantiateDTO(firstBranch, version);
                    firstSubDTO.FieldName = "";
                    target.Items.Add(firstSubDTO);
                }
                else
                {
                    target.SelectedUnionChildren = new([firstBranch]);
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