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
                //可选结构先放懒加载桩：子级缓存起来，等展开时再走验证流程。
                //只对含元组成员的映射结构放桩，普通可选结构仍直接展开，避免子级无谓消失。
                //泛型应用不能放桩：它的子级还没做形参替换，必须先让 GenericDTOBuilder 展开成实参结构
                if (!target.IsRequired && target.Children?.Count > 0 && !MCDocumentMetaTypeDTOHelper.IsPlaceHolderNode(target.Children[0])
                    && target.TypeKind is not MetaTypeKind.Generic
                    && target.Children.Any(child => child.TypeKind is MetaTypeKind.Tuple))
                {
                    target.Value = new ObservableCollection<MetaTypeEditorFieldDTO>(target.Children);
                    //桩缓存里的子级（含整棵子树）也要挂父链：界面会直接渲染/交互这些节点，
                    //缺父链会让"同级字段驱动的调度器重解析"永远找不到宿主（切了没反应、argument 停在模板态）
                    foreach (MetaTypeEditorFieldDTO stashedChild in target.Children)
                    {
                        stashedChild.Parent = target;
                        MCDocumentMetaTypeDTOHelper.AttachParentLinks(stashedChild);
                    }
                    target.Children = [new MetaTypeEditorFieldDTO() { ID = "placeHolder", TypeKind = MetaTypeKind.Any }];
                    return;
                }
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

                MetaTypeEditorFieldDTO instance = helper.InstantiateDTO(childTemplate, version);
                //定义键（[#[x(definition=true)] string]: V）本身是动态 map 的键，由 TupleDTOBuilder
                //的"输入框 + 加号"接管，加号里会把值类型完整解析出来。
                //早先在这里把它转成 Definition 节点会把整张 map 吃掉：Tuple 因为子级不足直接返回，
                //结果是既没有加号、值也停在未解析的字面量上。

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
                    instance.Children.Clear();
                }
                else
                {
                    built.Add(instance);
                }
                if (instance.TypeKind is MetaTypeKind.Composite && instance.Items?.Count > 0)
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
                    //子树其它层级（含懒加载桩缓存 Value）也要带回父子链：同级字段驱动的调度器重解析依赖 Parent
                    MCDocumentMetaTypeDTOHelper.AttachParentLinks(item);
                }
            }
            #endregion

            #region 索引写成同级字段的调度器，在同级就绪后补解析
            //按同级字段当前值就地重解析依赖它的调度器，并把订阅挂到里层枚举上。
            //实现挪到了 helper：联合落点重建子树后会再调一次，从而免疫"订阅挂在被替换掉的旧实例上"
            helper.BindSiblingIndexDispatchers(target, version);
            #endregion
        }

        public bool CanHandle(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.Struct;
        }

        #endregion
    }
}