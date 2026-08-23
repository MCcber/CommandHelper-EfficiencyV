using CBHK.Interface.Data;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;

namespace CBHK.Utility.Data.DTOBuilder
{
    public class StructDTOBuilder(Resource resource, MCDocumentMetaTypeDTOHelper helper, DocumentDTOBuildStrategyRegistry registry) : IDocumentDTOBuildStrategy
    {
        #region Field
        private readonly Resource resource = resource;
        private readonly MCDocumentMetaTypeDTOHelper helper = helper;
        private readonly DocumentDTOBuildStrategyRegistry registry = registry;
        #endregion

        #region Method
        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, DocumentPath documentPath, Dictionary<string, KeyValueAnchors> anchorMap, bool justSetView = false, string typeName = "")
        {
            if (template.Children is null || template.Children.Count == 0)
            {
                return;
            }

            List<MetaTypeEditorFieldDTO> built = [];
            MCDocumentMetaTypeDTOHelper.VerifyVersion([.. template.Children], version);

            for (int i = 0; i < template.Children.Count; i++)
            {
                #region 检测占位符
                var childTemplate = template.Children[i];
                if (childTemplate.ID == "placeHolder" || !childTemplate.IsVisible)
                {
                    continue;
                } 
                #endregion

                #region 检测是否为定义类节点
                MetaTypeEditorFieldDTO instance = helper.InstantiateDTO(childTemplate, version);
                if (MCDocumentMetaTypeDTOHelper.IsDefinitionItem(instance.FeatureMap))
                {
                    instance.FieldName = instance.Value?.ToString() ?? "";
                    instance.Value = "";
                    instance.Path = new(documentPath.TargetPath);
                    instance.TypeKind = MetaTypeKind.Definition;
                    instance.DefinitionEnterKeyDown = () => helper.DefinitionEnterKeyDown(instance, resource, anchorMap, version);
                    target.SetRequired(true);
                    built.Insert(0, instance);
                    continue;
                }
                #endregion

                #region 给实例执行浅表映射
                ResolvedTypeReference instanceRealData = null;
                if (childTemplate.Value is not null && !string.IsNullOrEmpty(childTemplate.Value.ToString()))
                {
                    #region 搜索真实映射
                    instanceRealData = UsePathParser.Parse(resource, documentPath, childTemplate.Value.ToString());
                    //MCDocumentMetaTypeDTOHelper.ShallowCopy(instanceRealData.Item2, instance);
                    //if (instanceRealData.Item2 is not null)
                    //{
                    //    instance.TemplateReference = instanceRealData.Item2;
                    //}

                    var realInstance = helper.InstantiateDTO(instanceRealData.DTO, version);
                    if(realInstance is not null)
                    {
                        string fieldName = instance.FieldName;
                        string displayName = instance.DisplayName;
                        bool isRequired = instance.IsRequired;
                        instance = realInstance;
                        instance.FieldName = fieldName;
                        instance.DisplayName = displayName;
                        instance.SetRequired(isRequired);
                    }
                    #endregion

                    #region 处理泛引用类子级
                    bool isReferenceType = childTemplate.TypeKind is MetaTypeKind.Literal && string.IsNullOrEmpty(childTemplate.FieldName);
                    if (instanceRealData.DTO is not null)
                    {
                        childTemplate = instanceRealData.DTO;
                    }
                    if (isReferenceType)
                    {
                        childTemplate.FieldName = instance.FieldName = "";
                        instance.SetRequired(true);
                        childTemplate.SetRequired(true);
                    } 
                    #endregion

                    #region 修正可选联合体节点的默认选项
                    if (instance.TypeKind is MetaTypeKind.Union)
                    {
                        instance.SelectedUnionItemUpdated = () => helper.SelectedUnionItemUpdated(instance, version);
                        if (!instance.IsRequired)
                        {
                            instance.UnionTypeNameList.Insert(0, new EnumMember() { Name = "- unset -", Value = new MetaValue() { Kind = MetaValueKind.Literal, LiteralValue = "unset" } });
                        }
                    }
                    #endregion

                    #region 处理已经被转换为结构体并有子级且可选的节点
                    else if (!instance.IsRequired && instance.TypeKind is MetaTypeKind.Struct && instance.Children?.Count > 0/* && !justSetView*/)
                    {
                        instance.Path ??= new(documentPath.TargetPath);
                        if (instance.Path.TargetPath.Length > 0)
                        {
                            string targetItemPath = instance.Path.TargetPath;
                            int lastDoubleColonIndex = targetItemPath.LastIndexOf("::");
                            if (lastDoubleColonIndex > -1)
                            {
                                instance.Value = targetItemPath[(lastDoubleColonIndex + 2)..];
                            }
                        }
                        instance.Children ??= [];
                        instance.Children.Clear();
                        instance.Children.Add(new MetaTypeEditorFieldDTO() { ID = "placeHolder", TypeKind = MetaTypeKind.Any });
                        built.Add(instance);
                        continue;
                    } 
                    #endregion
                }
                #endregion

                #region 处理各类引用与容器
                bool isContainerOrIndirectType = MCDocumentMetaTypeDTOHelper.IsContainerType(instance.TypeKind)
                 || MCDocumentMetaTypeDTOHelper.IsIndirectType(instance.TypeKind);
                MCDocumentResourceBuilder.BaseDataHandler(instance);
                MCDocumentResourceBuilder.BuildResource(instance, childTemplate, version, documentPath, resource, helper);
                if (target.Path?.TargetPath is not null)
                {
                    instance.Path ??= new(target.Path.TargetPath);
                }
                if (isContainerOrIndirectType && !justSetView)
                {
                    var childStrategy = registry.Get(instance.TypeKind);
                    childStrategy.Build(instance, childTemplate, version, instance.Path ?? documentPath, anchorMap, !instance.IsRequired, typeName);

                    //Literal展开后提升子节点，不保留外壳
                    if (instance.TypeKind is MetaTypeKind.Literal && instance.Children?.Count > 0)
                    {
                        built.AddRange(instance.Children);
                        instance.Children.Clear();
                    }
                    else
                    {
                        built.Add(instance);
                    }
                }
                else
                {
                    //找不到引用类型则说明是常量
                    if (instance.TypeKind is MetaTypeKind.Literal && instanceRealData.DTO is null)
                    {
                        var subLiteralNode = registry.Get(MetaTypeKind.Literal);
                        subLiteralNode.Build(instance, childTemplate, version, instance.Path ?? childTemplate.Path ?? documentPath, anchorMap, justSetView, typeName);
                    }
                    built.Add(instance);
                }
                if(instance.TypeKind is MetaTypeKind.Composite && instance.Items?.Count > 0)
                {
                    instance.Items[0].FieldName = instance.FieldName;
                }
                #endregion
            }

            #region 重新填充子级
            if (built.Count > 0)
            {
                target.Children ??= [];
                target.Children.Clear();
                foreach (var item in built)
                {
                    target.Children.Add(item);
                    item.Parent = target;
                }
            }
            #endregion
        }

        public bool CanHandle(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.Struct;
        } 

        #endregion
    }
}
