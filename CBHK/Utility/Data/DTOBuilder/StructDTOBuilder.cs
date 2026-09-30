using CBHK.Interface.Data;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using MinecraftLanguageModelLibrary.Data;
using System;
using System.Collections.Generic;
using System.Linq;

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
        /// <summary>
        /// 构建结构体。同一份结构在构建过程中再次进入时不再展开，
        /// 用于打断 dispatch → generic → struct 这类间接递归（替代旧管线的 justSetView）。
        /// </summary>
        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, DocumentPath documentPath, Dictionary<string, KeyValueAnchors> anchorMap, RenderDepth depth, string typeName = "")
        {
            string buildingKey = target.Path?.TargetPath;
            if (string.IsNullOrEmpty(buildingKey))
            {
                buildingKey = target.TypeName ?? target.FieldName ?? target.TemplateReference?.Path?.TargetPath;
            }
            if (!string.IsNullOrEmpty(buildingKey) && !helper.BuildingStructures.Add(buildingKey))
            {
                return;
            }

            try
            {
                BuildCore(target, template, version, documentPath, anchorMap, depth, typeName);
            }
            finally
            {
                if (!string.IsNullOrEmpty(buildingKey))
                {
                    helper.BuildingStructures.Remove(buildingKey);
                }
            }
        }

        private void BuildCore(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, DocumentPath documentPath, Dictionary<string, KeyValueAnchors> anchorMap, RenderDepth depth, string typeName = "")
        {
            //解析阶段可能已把节点物化成新的类型，此时以节点自身的子级为准
            if (target.Children is null || target.Children.Count == 0)
            {
                return;
            }

            List<MetaTypeEditorFieldDTO> built = [];
            MCDocumentMetaTypeDTOHelper.VerifyVersion([.. target.Children], version);

            for (int i = 0; i < target.Children.Count; i++)
            {
                #region 检测占位符
                var childTemplate = target.Children[i];
                if (MCDocumentMetaTypeDTOHelper.IsPlaceHolderNode(childTemplate) || !childTemplate.IsVisible)
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

                #region 名称引用类的必选性修正
                bool isReferenceType = childTemplate.TypeKind is MetaTypeKind.Literal
                    && string.IsNullOrEmpty(childTemplate.FieldName)
                    && !string.IsNullOrEmpty(childTemplate.Value?.ToString());
                if (isReferenceType)
                {
                    childTemplate.FieldName = instance.FieldName = "";
                    instance.SetRequired(true);
                    childTemplate.SetRequired(true);
                }
                #endregion

                #region 解析引用、资源与调度器
                MCDocumentResourceBuilder.BaseDataHandler(instance);
                MCDocumentResourceBuilder.BuildResource(instance, childTemplate, version, documentPath, resource, helper);
                if (target.Path?.TargetPath is not null)
                {
                    instance.Path ??= new(target.Path.TargetPath);
                }
                #endregion

                #region 可选结构在浅层渲染为懒加载桩
                if (depth is RenderDepth.Shallow && !instance.IsRequired && instance.TypeKind is MetaTypeKind.Struct && instance.Children?.Count > 0)
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
                    instance.Children.Clear();
                    instance.Children.Add(new MetaTypeEditorFieldDTO() { ID = "placeHolder", TypeKind = MetaTypeKind.Any });
                    built.Add(instance);
                    continue;
                }
                #endregion

                #region 按解析后的类型分发给对应构建器
                //容器类型只在深层展开；非容器类型与调度器在任何深度都自建，
                //调度器由它自己的构建器按字段必选性决定立刻物化还是留给展开时物化
                bool isContainerOrIndirectType = MCDocumentMetaTypeDTOHelper.IsContainerType(instance.TypeKind)
                 || MCDocumentMetaTypeDTOHelper.IsIndirectType(instance.TypeKind);
                bool needsOwnBuild = !MCDocumentMetaTypeDTOHelper.IsContainerType(instance.TypeKind)
                    || instance.TypeKind is MetaTypeKind.Dispatch;
                if (isContainerOrIndirectType && (depth is RenderDepth.Deep || needsOwnBuild))
                {
                    registry.Get(instance.TypeKind).Build(instance, childTemplate, version, instance.Path ?? documentPath, anchorMap, depth, typeName);
                }
                #endregion

                //无名结构字段是数据文件里的 ...展开引用，不保留外壳，直接把它的子级拼进父级
                if (string.IsNullOrEmpty(childTemplate.FieldName) && instance.TypeKind is MetaTypeKind.Struct && instance.Children?.Count > 0)
                {
                    built.AddRange(instance.Children);
                    instance.Children.Clear();
                    continue;
                }

                //Literal不保留外壳，展开后提升子节点
                if (instance.TypeKind is MetaTypeKind.Literal && instance.Children?.Count > 0)
                {
                    built.AddRange(instance.Children);
                    instance.Children.Clear();                }
                else
                {
                    built.Add(instance);
                }
                if(instance.TypeKind is MetaTypeKind.Composite && instance.Items?.Count > 0)
                {
                    instance.Items[0].FieldName = instance.FieldName;
                }
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

            #region 索引写成同级字段的调度器，在同级就绪后补解析
            //索引变化后就地重解析（内容落点、必选性都沿用原节点）
            void ResolveAccessorIndex(MetaTypeEditorFieldDTO child, string indexValue)
            {
                if (string.IsNullOrEmpty(indexValue) || !helper.ResolvingIndexDispatchers.Add(child))
                {
                    return;
                }

                try
                {
                    child.FeatureMap["Index"] = new MetaValue { Kind = MetaValueKind.Literal, LiteralValue = indexValue };
                    child.TypeKind = MetaTypeKind.Dispatch;
                    child.Children = null;
                    MCDocumentResourceBuilder.BuildResource(child, child, version, documentPath, resource, helper);
                    registry.Get(child.TypeKind).Build(child, child, version, child.Path ?? documentPath, anchorMap, depth, typeName);
                }
                finally
                {
                    helper.ResolvingIndexDispatchers.Remove(child);
                }
            }

            for (int i = 0; i < target.Children.Count; i++)
            {
                MetaTypeEditorFieldDTO child = target.Children[i];
                if (child.FeatureMap is null
                    || !child.FeatureMap.TryGetValue("Resource", out MetaValue indexResource) || indexResource is null
                    || !child.FeatureMap.TryGetValue("Index", out MetaValue indexMeta) || indexMeta is null)
                {
                    continue;
                }

                //索引是同级字段名时，取该字段的当前值；字段无实例或匹配不到结构时按常量处理
                string indexFieldName = MCDocumentMetaTypeDTOHelper.ExtractAccessorPath(indexMeta);
                if (string.IsNullOrEmpty(indexFieldName) || indexFieldName.StartsWith('%'))
                {
                    continue;
                }
                MetaTypeEditorFieldDTO indexField = target.Children.FirstOrDefault(item => item.FieldName == indexFieldName);
                string indexValue = indexField?.SelectedEnumOption?.Value?.LiteralValue?.ToString()
                    ?? indexField?.SelectedEnumOption?.Name
                    ?? (indexField?.TypeKind is MetaTypeKind.Literal ? indexField.Value?.ToString() : null);
                ResolveAccessorIndex(child, indexValue);

                if (indexField is not null)
                {
                    //索引字段变化后，重新解析同级里依赖它的调度器（带重入保护）
                    bool isRefreshingIndex = false;
                    Action previousEnumUpdated = indexField.SelectedEnumItemUpdated;
                    indexField.SelectedEnumItemUpdated = () =>
                    {
                        previousEnumUpdated?.Invoke();
                        if (isRefreshingIndex)
                        {
                            return;
                        }
                        isRefreshingIndex = true;
                        try
                        {
                            string currentIndexValue = indexField.SelectedEnumOption?.Value?.LiteralValue?.ToString()
                                ?? indexField.SelectedEnumOption?.Name
                                ?? (indexField.TypeKind is MetaTypeKind.Literal ? indexField.Value?.ToString() : null);
                            foreach (MetaTypeEditorFieldDTO sibling in target.Children ?? [])
                            {
                                if (sibling.FeatureMap is null
                                    || !sibling.FeatureMap.ContainsKey("Resource")
                                    || !sibling.FeatureMap.TryGetValue("Index", out MetaValue siblingIndex) || siblingIndex is null)
                                {
                                    continue;
                                }
                                if (MCDocumentMetaTypeDTOHelper.ExtractAccessorPath(siblingIndex) == indexFieldName)
                                {
                                    ResolveAccessorIndex(sibling, currentIndexValue);
                                }
                            }
                        }
                        finally
                        {
                            isRefreshingIndex = false;
                        }
                    };
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
