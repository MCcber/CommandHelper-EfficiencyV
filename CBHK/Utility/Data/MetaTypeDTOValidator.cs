using CBHK.Model.Constant;
using CBHK.Utility.Data.DTOBuilder;
using CBHK.Interface.Data;
using CBHK.Model.Data;
using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;
using System.Linq;

namespace CBHK.Utility.Data
{
    public partial class MetaTypeDTOValidator(Resource resource)
    {
        #region Field
        private Resource resource = resource;
        #endregion

        #region Property
        public DocumentDTOBuildStrategyRegistry Registry { get; set; }
        public MCDocumentMetaTypeDTOHelper DTOHelper { get; set; }

        #endregion

        #region Method
        /// <summary>
        /// 验证单层DTO实例
        /// </summary>
        /// <param name="instanceContext">实例 Item 根列表</param>
        /// <param name="templateList">模板 Item 根列表</param>
        /// <returns></returns>
        public void Verify(
            DTOInstanceContext instanceContext,
            List<MetaTypeEditorFieldDTO> templateList,
            string version,
            DocumentPath documentItemPath = null,bool isLoaded = false)
        {
            #region 遍历前就执行版本过滤，将计算开销降到最低
            if (templateList.Count == 0)
            {
                return;
            }

            List<MetaTypeEditorFieldDTO> templates = [.. templateList];
            if (templates.Count == 0)
            {
                return;
            }
            //版本校验完毕后也应该留下，不应该删除，谁也不知道下次切换版本后会不会需要这个节点
            MCDocumentMetaTypeDTOHelper.VerifyVersion(templates, version);
            if (instanceContext.dtoInstanceList.Count > 0 && !templates.Contains(instanceContext.dtoInstanceList[0]))
            {
                MCDocumentMetaTypeDTOHelper.VerifyVersion(instanceContext.dtoInstanceList, version);
            }
            #endregion

            for (int i = 0; i < templates.Count; i++)
            {
                #region Field
                MetaTypeEditorFieldDTO template = templates[i];
                //提前保存实例字段名
                string fieldName = template.FieldName;
                DocumentPath itemPath = null;
                if (documentItemPath is not null && documentItemPath.TargetPath.Length > 0)
                {
                    itemPath = new(documentItemPath.TargetPath);
                }
                #endregion

                #region 分流：带类型引用的节点先解析文档类型
                if (HasTypeReference(template))
                {
                    //先解析真实文档类型
                    if (template.Value is not null && !string.IsNullOrEmpty(template.Value.ToString()))
                    {
                        ResolvedTypeReference resolved = UsePathParser.Parse(resource, itemPath, template.Value.ToString());
                        if (resolved?.Item is not null)
                        {
                            //保留模板的必选性
                            bool isRequired = template.IsRequired;
                            template = templates[i] = resolved.Item;
                            template.SetRequired(isRequired);

                            if (!string.IsNullOrEmpty(resolved.Path))
                            {
                                itemPath = new(resolved.Path);
                            }
                        }
                    }
                }
                #endregion

                #region 与已有实例对齐：能就地复用则保住对象身份（JSON 锚点挂在它上面）
                MetaTypeEditorFieldDTO existing = FindExistingInstance(instanceContext, template, fieldName);
                MetaTypeEditorFieldDTO instance;
                if (existing is not null)
                {
                    existing.CopyFrom(template);
                    instance = existing;
                }
                else
                {
                    instance = DTOHelper.InstantiateDTO(template, version);
                }
                #endregion

                #region 处理资源、分配策略并执行构造
                instance.FieldName = fieldName;
                MCDocumentResourceBuilder.BaseDataHandler(instance);
                MCDocumentResourceBuilder.BuildResource(instance, template, version, itemPath, resource, DTOHelper);
                //由注册器使用策略模式动态分配不同类型的构造器执行DTO实例构造任务
                //可选项与载入态只出当前层
                RenderDepth depth = (!instance.IsRequired || isLoaded) ? RenderDepth.Shallow : RenderDepth.Deep;
                var strategy = Registry.Get(instance.TypeKind);
                strategy.Build(instance, template, version, itemPath, instanceContext.anchorMap, depth);
                #endregion

                #region 直接在对应位置替换或追加
                if (i < instanceContext.dtoInstanceList.Count)
                {
                    instanceContext.dtoInstanceList[i] = instance;
                }
                else
                {
                    instanceContext.dtoInstanceList.Add(instance);
                }
                #endregion
            }

            //按文本锚点把 JSON 里的值取回节点
            BindValues(instanceContext);
        }

        /// <summary>
        /// 按文本锚点把 JSON 里的值取回节点
        /// </summary>
        private static void BindValues(DTOInstanceContext instanceContext)
        {
            for (int i = 0; i < instanceContext.dtoInstanceList.Count; i++)
            {
                MetaTypeEditorFieldDTO node = instanceContext.dtoInstanceList[i];
                if (node is null || MetaTypeKindPredicates.IsContainerType(node.TypeKind) || MetaTypeKindPredicates.IsIndirectType(node.TypeKind))
                {
                    continue;
                }
                if (!instanceContext.anchorMap.TryGetValue(node.ID, out KeyValueAnchors anchor) || anchor?.Source is null || anchor.IsContainer)
                {
                    continue;
                }

                string text = anchor.Source.GetText(anchor.ValueStart.Offset, anchor.ValueEnd.Offset - anchor.ValueStart.Offset);
                node.Value = text;
                if (node.TypeKind is MetaTypeKind.Boolean)
                {
                    node.IsTrue = text == "true";
                    node.IsFalse = text == "false";
                }
            }
        }

        /// <summary>
        /// 分流判定：模板节点是否需要先解析文档类型。
        /// Value / TypeName / 调度器特征（Resource + Index）任一命中即为 true。
        /// </summary>
        private static bool HasTypeReference(MetaTypeEditorFieldDTO template)
        {
            if (template is null)
            {
                return false;
            }

            if (template.Value is not null && !string.IsNullOrEmpty(template.Value.ToString()))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(template.TypeName))
            {
                return true;
            }

            return template.FeatureMap is not null
                && template.FeatureMap.ContainsKey("Resource")
                && template.FeatureMap.ContainsKey("Index");
        }

        /// <summary>
        /// 在当前层实例列表中查找同名的已有实例。
        /// </summary>
        private static MetaTypeEditorFieldDTO FindExistingInstance(
            DTOInstanceContext instanceContext,
            MetaTypeEditorFieldDTO template,
            string fieldName)
        {
            if (instanceContext.dtoInstanceList.Count == 0)
            {
                return null;
            }

            //搜索普通节点
            MetaTypeEditorFieldDTO result = instanceContext.dtoInstanceList.FirstOrDefault(
                item => item.FieldName == fieldName && !string.IsNullOrWhiteSpace(fieldName));

            //搜索特殊节点
            result ??= instanceContext.dtoInstanceList.FirstOrDefault(
                item => item.TemplateReference == template || item.ID == template.ID);

            return result;
        }
        #endregion
    }
}