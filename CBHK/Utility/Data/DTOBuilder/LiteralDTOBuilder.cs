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
    public class LiteralDTOBuilder(Resource resource, MCDocumentMetaTypeDTOHelper helper, DocumentDTOBuildStrategyRegistry registry) : IDocumentDTOBuildStrategy
    {
        #region Field
        private readonly Resource resource = resource;
        private readonly MCDocumentMetaTypeDTOHelper helper = helper;
        private readonly DocumentDTOBuildStrategyRegistry registry = registry;
        #endregion

        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, StringBuilder documentItemPath, Dictionary<string, KeyValueAnchors> anchorMap)
        {
            string currentValueString = target.Value?.ToString() ?? "";
            MetaTypeEditorFieldDTO targetStruct = null;
            if (!string.IsNullOrWhiteSpace(currentValueString))
            {
                #region 计算引用路径
                string currentDocumentItemPath = string.Empty;
                if (documentItemPath is not null)
                {
                    string documentItemPathString = documentItemPath.ToString();
                    int lastColonIndex = documentItemPathString.LastIndexOf("::");
                    //精准匹配目标结构
                    if (lastColonIndex > -1 && resource.DocumentPathItemMap.TryGetValue(documentItemPathString[..lastColonIndex], out List<string> targetFileUseList) &&
                       targetFileUseList is not null)
                    {
                        string targetUsePath = targetFileUseList.FirstOrDefault(item => item.EndsWith(currentValueString));
                        //查找当前上下文的文件引用列表，找出目标引用路径
                        if (!string.IsNullOrEmpty(targetUsePath))
                        {
                            _ = resource.DocumentItemMap.TryGetValue(targetUsePath, out targetStruct);
                            currentDocumentItemPath = targetUsePath;
                        }
                        //找出当前文档内部的结构资源
                        else if (resource.DocumentItemMap.TryGetValue(documentItemPathString[..lastColonIndex] + "::" + currentValueString, out targetStruct))
                        {
                            currentDocumentItemPath = documentItemPathString;
                        }
                        //不符合上述两种情况则为字符串变量
                    }
                } 
                #endregion

                #region 未找到目标结构则将当前引用类型转为对应数据类型的枚举节点
                if (targetStruct is null)
                {
                    target.TypeKind = MetaTypeKind.Enum;
                    target.EnumOptionList ??= [];
                    if (target.IsRequired)
                    {
                        target.EnumOptionList = [new EnumMember() { Name = "- unset- ", Value = new MetaValue { LiteralValue = "unset" } }];
                    }
                    target.EnumOptionList.Add(new EnumMember() { Name = currentValueString, Value = new MetaValue { LiteralValue = currentValueString } });
                    target.SelectedEnumOption = target.EnumOptionList[0];
                    return;
                }
                #endregion

                #region 根据目标结构的类型分情况处理
                if (MCDocumentMetaTypeDTOHelper.IsContainerType(targetStruct.TypeKind) || MCDocumentMetaTypeDTOHelper.IsIndirectType(targetStruct.TypeKind))
                {
                    //若当前层的Literal节点指向Union结构，则需要将Literal节点的下一个兄弟节点作为Union的子节点插入位置
                    List<MetaTypeEditorFieldDTO> subUnionInsertItemList = [];
                    List<MetaTypeEditorFieldDTO> templateChildren = targetStruct.Children?.ToList() ?? [];

                    // 准备实例子节点列表：优先使用 target 已有的 Children（用户已编辑的数据），

                    //非Union类型则全量验证
                    if (targetStruct.TypeKind is not MetaTypeKind.Union)
                    {
                        #region 装载成员
                        // 否则创建空列表，让 Verify 内部通过模板克隆生成实例。
                        List<MetaTypeEditorFieldDTO> verifiedChildren = [];
                        if (targetStruct.Children is not null && targetStruct.Children.Count > 0)
                        {
                            for (int j = 0; j < targetStruct.Children.Count; j++)
                            {
                                var instance = helper.InstantiateDTO(targetStruct.Children[j], version);
                                verifiedChildren.Add(instance);
                            }
                        }
                        else
                        {
                            var instance = helper.InstantiateDTO(targetStruct, version);
                            verifiedChildren.Add(instance);
                            templateChildren = [targetStruct];
                        }
                        #endregion

                        #region 构造验证所需的元组
                        target.Children ??= [];
                        for (int i = 0; i < verifiedChildren.Count; i++)
                        {
                            MCDocumentResourceBuilder.BaseDataHandler(verifiedChildren[i]);
                            MCDocumentResourceBuilder.BuildResource(verifiedChildren[i], templateChildren[0], version, documentItemPath, resource, helper);
                            var childRegistry = registry.Get(verifiedChildren[i].TypeKind);
                            childRegistry.Build(verifiedChildren[i], templateChildren[i], version, documentItemPath, anchorMap);
                            target.Children.Add(verifiedChildren[i]);
                        }
                        #endregion
                    }
                    //联合体仅验证第一个成员，减轻系统计算负载
                    else
                    {
                        #region 优先进行版本校验，随后实例化
                        MCDocumentMetaTypeDTOHelper.VerifyVersion([.. targetStruct.Children], version);
                        List<MetaTypeEditorFieldDTO> verifiedTemplateList = [targetStruct.Children.FirstOrDefault(item => MCDocumentMetaTypeDTOHelper.IsIndirectType(item.TypeKind))];
                        verifiedTemplateList.RemoveAll(item => item is null);
                        var firstTemplate = targetStruct.Children.FirstOrDefault(item => item.IsVisible);
                        var instance = helper.InstantiateDTO(firstTemplate, version);
                        #endregion

                        #region 展平指向Union的Literal节点
                        string targetDocumentItemPath = "";
                        if (verifiedTemplateList.Count > 0)
                        {
                            string baseFilePath = "";
                            int lastDoubleColonIndex = currentDocumentItemPath.LastIndexOf("::");
                            if (lastDoubleColonIndex > -1)
                            {
                                baseFilePath = currentDocumentItemPath[..lastDoubleColonIndex];
                            }
                            MetaTypeEditorFieldDTO targetDTOValue = null;
                            List<MetaTypeEditorFieldDTO> verifiedTargetDTOValueList = [];
                            for (int j = 0; j < verifiedTemplateList.Count; j++)
                            {
                                if (verifiedTemplateList[j].TypeKind is MetaTypeKind.Literal && verifiedTemplateList[j].Value is not null &&
                                    resource.DocumentPathItemMap.TryGetValue(baseFilePath, out List<string> useFilePathList) && useFilePathList?.Count > 0)
                                {
                                    string currentLiteralValue = verifiedTemplateList[j].Value.ToString();
                                    targetDocumentItemPath = baseFilePath + "::" + currentLiteralValue;
                                    if (!string.IsNullOrEmpty(targetDocumentItemPath))
                                    {
                                        verifiedTemplateList[0].DocumentItemPath ??= new(targetDocumentItemPath);
                                    }
                                    string targetUseFilePath = useFilePathList.FirstOrDefault(item => item.EndsWith(currentLiteralValue));
                                    //先搜索全局资源
                                    if (!string.IsNullOrEmpty(targetUseFilePath) && resource.DocumentItemMap.TryGetValue(targetUseFilePath, out targetDTOValue) && targetDTOValue.TypeKind is MetaTypeKind.Union && targetDTOValue.Children?.Count > 0)
                                    {
                                        MCDocumentMetaTypeDTOHelper.VerifyVersion([.. targetDTOValue.Children], version);
                                        verifiedTargetDTOValueList = [.. targetDTOValue.Children.Where(item => item.IsVisible)];

                                    }
                                    //后搜索内部资源
                                    else if (!string.IsNullOrEmpty(targetDocumentItemPath) && resource.DocumentItemMap.TryGetValue(targetDocumentItemPath, out targetDTOValue) && targetDTOValue.TypeKind is MetaTypeKind.Union)
                                    {
                                        MCDocumentMetaTypeDTOHelper.VerifyVersion([.. targetDTOValue.Children], version);
                                        verifiedTargetDTOValueList = [.. targetDTOValue.Children.Where(item => item.IsVisible)];
                                    }

                                    if (verifiedTargetDTOValueList?.Count > 0)
                                    {
                                        subUnionInsertItemList.Add(new() { ID = verifiedTemplateList[j].ID, TypeKind = MetaTypeKind.Any, Children = [.. verifiedTargetDTOValueList] });
                                    }
                                }
                            }
                        }
                        #endregion

                        #region 验证并执行装载节点
                        // 构造验证所需的元组
                        MCDocumentResourceBuilder.BaseDataHandler(instance);
                        MCDocumentResourceBuilder.BuildResource(instance, firstTemplate, version, documentItemPath, resource, helper);
                        var instanceRegistry = registry.Get(instance.TypeKind);
                        instanceRegistry.Build(instance, firstTemplate, version, new(currentDocumentItemPath), anchorMap);

                        target.Children ??= [];
                        //添加首个被验证的子节点
                        target.Children.Add(instance);
                        #endregion

                        #region 将所有子Union结构展开后的成员全部插入到指定位置
                        List<MetaTypeEditorFieldDTO> unVerifyChildren = [.. targetStruct.Children];
                        List<EnumMember> UnionTypeNameList = [.. targetStruct.UnionTypeNameList];
                        List<string> unionStringList = [];
                        if (subUnionInsertItemList.Count > 0)
                        {
                            for (int j = 0; j < subUnionInsertItemList.Count; j++)
                            {
                                for (int k = 0; k < subUnionInsertItemList[j].Children.Count; k++)
                                {
                                    subUnionInsertItemList[j].Children[k].DocumentItemPath = new(targetDocumentItemPath);
                                }
                                var dto = unVerifyChildren.FirstOrDefault(item => item.ID == subUnionInsertItemList[j].ID);
                                int index = unVerifyChildren.IndexOf(dto);
                                unVerifyChildren.Remove(dto);
                                if (index > -1)
                                {
                                    unVerifyChildren.InsertRange(index, subUnionInsertItemList[j].Children);
                                    unionStringList.Clear();
                                    unionStringList.AddRange(subUnionInsertItemList[j].Children.Select(item => !string.IsNullOrEmpty(item.FieldName) ? item.FieldName : item.TypeKind.ToString()));
                                    UnionTypeNameList.RemoveAt(index);
                                    UnionTypeNameList.InsertRange(index, unionStringList.Select(item => new EnumMember() { Name = item, Value = new() { LiteralValue = item } }));
                                }
                                else if (subUnionInsertItemList[j] is null)
                                {
                                    unVerifyChildren.AddRange(subUnionInsertItemList[j].Children);
                                    unionStringList.Clear();
                                    unionStringList.AddRange(subUnionInsertItemList[j].Children.Select(item => !string.IsNullOrEmpty(item.FieldName) ? item.FieldName : item.TypeKind.ToString()));
                                    UnionTypeNameList.RemoveAt(index);
                                    UnionTypeNameList.InsertRange(index, unionStringList.Select(item => new EnumMember() { Name = item, Value = new() { LiteralValue = item } }));
                                }
                            }
                            target.Children.AddRange(unVerifyChildren[1..]);
                            bool haveBaseType = !MCDocumentMetaTypeDTOHelper.IsContainerType(instance.TypeKind) &&
                                !MCDocumentMetaTypeDTOHelper.IsIndirectType(instance.TypeKind);
                            //改装为复合节点，默认显示第一个已验证的节点
                            if (haveBaseType)
                            {
                                MetaTypeEditorFieldDTO unionDTO = new()
                                {
                                    ID = "placeHolder",
                                    TypeKind = MetaTypeKind.Union,
                                    FieldName = target.FieldName,
                                    UnionTypeNameList = [.. UnionTypeNameList],
                                    Parent = target,
                                    DocumentItemPath = !string.IsNullOrEmpty(currentDocumentItemPath) ? new(currentDocumentItemPath) : documentItemPath ?? target.DocumentItemPath
                                };
                                unionDTO.SelectedUnionTypeName = unionDTO.UnionTypeNameList[0];
                                unionDTO.SelectedUnionItemUpdated = () => helper.SelectedUnionItemUpdated(unionDTO, version);

                                instance.FieldName = "";
                                target.TypeKind = MetaTypeKind.Composite;
                                target.DocumentItemPath = !string.IsNullOrEmpty(currentDocumentItemPath) ? new(currentDocumentItemPath) : documentItemPath ?? target.DocumentItemPath;
                                target.Items = [unionDTO, instance];
                                target.SelectedUnionChildren?.Clear();
                                target.UnionTypeNameList?.Clear();
                                //target.SelectedUnionChildren ??= [];
                            }
                        }
                        //没有基础类型则放入第一个成员
                        else if (MCDocumentMetaTypeDTOHelper.IsContainerType(instance.TypeKind))
                        {
                            target.SelectedUnionChildren = [.. instance.Children];
                        }
                        else
                        {
                            target.SelectedUnionChildren = [instance];
                        }
                        #endregion
                    }

                    #region 更新当前节点为容器类型，挂接验证后的子节点
                    if (target.TypeKind is MetaTypeKind.Dispatch)
                    {
                        target.OriginKind = target.TypeKind;
                        target.TypeKind = MetaTypeKind.Struct;
                    }
                    else if (target.TypeKind is not MetaTypeKind.Composite)
                    {
                        target.OriginKind = target.TypeKind;
                        target.TypeKind = targetStruct.TypeKind;
                    }
                    #endregion
                }
                #endregion

                #region 处理基本类型（int, bool, string, 枚举等）
                else
                {
                    target.TypeKind = targetStruct.TypeKind;
                    if (targetStruct.EnumOptionList is not null)
                    {
                        target.EnumOptionList ??= [.. targetStruct.EnumOptionList];
                        target.SelectedEnumOption = target.EnumOptionList?[0];
                    }
                    target.Value = null;
                    target.Children = null;
                } 
                #endregion
            }
        }

        public bool CanHandle(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.Literal;
        }
    }
}
