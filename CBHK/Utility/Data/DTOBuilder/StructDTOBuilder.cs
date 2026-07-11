using CBHK.Interface.Data;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;
using System.Text;

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
        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, StringBuilder documentItemPath, Dictionary<string, KeyValueAnchors> anchorMap)
        {
            if (template.Children is null || template.Children.Count == 0)
            {
                return;
            }

            List<MetaTypeEditorFieldDTO> built = [];

            for (int i = 0; i < template.Children.Count; i++)
            {
                var childTemplate = template.Children[i];
                if (childTemplate.ID == "placeHolder")
                {
                    continue;
                }

                var instance = helper.InstantiateDTO(childTemplate, version);

                #region 检测是否为定义类节点
                if (MCDocumentMetaTypeDTOHelper.IsDefinitionItem(instance.FeatureMap))
                {
                    instance.FieldName = instance.Value?.ToString() ?? "";
                    instance.Value = "";
                    instance.DocumentItemPath = documentItemPath;
                    instance.TypeKind = MetaTypeKind.Definition;
                    instance.DefinitionEnterKeyDown = () => helper.DefinitionEnterKeyDown(instance, resource, anchorMap, version);
                    target.IsRequired = true;
                    built.Insert(0, instance);
                    continue;
                }
                #endregion

                #region 处理各类引用与容器
                if (MCDocumentMetaTypeDTOHelper.IsContainerType(childTemplate.TypeKind)
            || MCDocumentMetaTypeDTOHelper.IsIndirectType(childTemplate.TypeKind))
                {
                    MCDocumentResourceBuilder.BaseDataHandler(instance);
                    MCDocumentResourceBuilder.BuildResource(instance, childTemplate, version, documentItemPath, resource, helper);
                    var childStrategy = registry.Get(instance.TypeKind);
                    childStrategy.Build(instance, childTemplate, version, documentItemPath, anchorMap);

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
                    built.Add(instance);
                } 
                #endregion
            }

            target.Children ??= [];
            target.Children.Clear();
            foreach (var item in built)
            {
                target.Children.Add(item);
                item.Parent = target;
            }
        }

        public bool CanHandle(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.Struct;
        } 

        #endregion
    }
}
