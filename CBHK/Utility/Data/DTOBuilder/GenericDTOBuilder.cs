using CBHK.Interface.Data;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using CommunityToolkit.Mvvm.Input;
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

        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, DocumentPath documentPath, Dictionary<string, KeyValueAnchors> anchorMap, RenderDepth depth, string typeName = "")
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

            if (targetContext.Item is MetaTypeEditorFieldDTO targetTypeDTO)
            {
                // 业务步骤 1：建立当前泛型层的绑定作用域。
                // 形参来自泛型声明模板，实参来自当前泛型实例；scope 会沿 Generic -> Dispatch -> Generic 链向下传递。
                List<Tuple<string, MetaValue>> formalParamMap = targetTypeDTO.TypeParameterNameList ?? [];
                List<Tuple<string, MetaValue>> actualArgMap = target.TypeParameterNameList ?? [];
                TypeBindingScope bindingScope = MCDocumentMetaTypeDTOHelper.CreateBindingScope(formalParamMap, actualArgMap, target.BindingScope);
                target.BindingScope = bindingScope;

                // 业务步骤 2：处理动态 Key Map。
                // 例如 type EnvironmentAttributeMap<K> = struct { [K]: (...) }。
                // 匹配后生成 Key Enum + Value 编辑器的 Composite 入口。
                if (TryBuildDynamicKeyMap(target, targetTypeDTO, actualArgMap, bindingScope, version, documentPath, anchorMap, depth, typeName))
                {
                    return;
                }

                // 业务步骤 3：普通泛型替换。
                // 非动态 Key Map 的泛型按原逻辑替换形参，再交给对应 Builder 继续构建。
                if (formalParamMap.Count == actualArgMap.Count)
                {
                    var substituteResult = helper.SubstituteGenericIterative(targetTypeDTO, formalParamMap, actualArgMap, version);
                    MCDocumentMetaTypeDTOHelper.VerifyVersion(substituteResult, version);
                    List<MetaTypeEditorFieldDTO> expandedChildrenList = [..substituteResult.Where(item => item.IsVisible)];

                    for (int i = 0; i < expandedChildrenList.Count; i++)
                    {
                        AttachBindingScopeRecursively(expandedChildrenList[i], bindingScope);
                        MCDocumentResourceBuilder.BaseDataHandler(expandedChildrenList[i]);
                        MCDocumentResourceBuilder.BuildResource(expandedChildrenList[i], expandedChildrenList[i], version, documentPath, resource, helper);
                        var childRegistry = registry.Get(expandedChildrenList[i].TypeKind);
                        childRegistry.Build(expandedChildrenList[i], expandedChildrenList[i], version, documentPath, anchorMap, depth, targetTypeName);
                    }

                    List<MetaTypeEditorFieldDTO> verifiedDTOList = expandedChildrenList;
                    target.TypeKind = targetTypeDTO.TypeKind;
                    if (targetTypeDTO.UnionTypeNameList is not null)
                    {
                        target.UnionTypeNameList = [.. targetTypeDTO.UnionTypeNameList];
                    }
                    if (targetTypeDTO.FeatureMap is not null)
                    {
                        target.FeatureMap = new(targetTypeDTO.FeatureMap);
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

        /// <summary>
        /// 动态 Key Map 识别：访问器把方括号计算键标记成 Tuple（元素 0 = 键类型，元素 1 = 值类型）。
        /// </summary>
        private static bool IsDynamicKeyMapTemplate(MetaTypeEditorFieldDTO targetTypeDTO)
        {
            return targetTypeDTO.Children?.Count == 1
                && targetTypeDTO.Children[0].TypeKind is MetaTypeKind.Tuple
                && targetTypeDTO.Children[0].Children?.Count >= 2;
        }
        private bool TryBuildDynamicKeyMap(
            MetaTypeEditorFieldDTO target,
            MetaTypeEditorFieldDTO targetTypeDTO,
            IReadOnlyList<Tuple<string, MetaValue>> actualArgMap,
            TypeBindingScope bindingScope,
            string version,
            DocumentPath documentPath,
            Dictionary<string, KeyValueAnchors> anchorMap,
            RenderDepth depth,
            string typeName)
        {

            if (!IsDynamicKeyMapTemplate(targetTypeDTO))
            {
                return false;
            }

            //构建 Key 枚举。K 可能来自 registry，也可能来自同 Resource 的 dispatch Index 列表。
            MetaTypeEditorFieldDTO keyField = targetTypeDTO.Children![0];
            //Tuple 形态（[K] 计算键）下元素 1 才是值模板
            MetaTypeEditorFieldDTO valueTemplate = keyField.TypeKind is MetaTypeKind.Tuple && keyField.Children?.Count >= 2
                ? keyField.Children[1]
                : keyField;
            MetaTypeEditorFieldDTO keyEnum = BuildDynamicKeyEnum(targetTypeDTO, actualArgMap[0].Item2, version);

            if (keyEnum?.EnumOptionList is null || keyEnum.EnumOptionList.Count == 0)
            {
                return false;
            }

            for (int i = keyEnum.EnumOptionList.Count - 1; i >= 0; i--)
            {
                if (keyEnum.EnumOptionList[i].Name == "- unset -")
                {
                    keyEnum.EnumOptionList.RemoveAt(i);
                }
            }

            if (keyEnum.EnumOptionList.Count == 0)
            {
                return false;
            }

            //键选择器是字段的内部控件，字段名由承载它的 Struct 行显示
            keyEnum.FieldName = "";
            keyEnum.IsVisible = true;
            keyEnum.SelectedEnumOption = keyEnum.EnumOptionList[0];

            ObservableCollection<MetaTypeEditorFieldDTO> keyItems = [keyEnum];
            target.FeatureMap = new(targetTypeDTO.FeatureMap ?? []);
            target.BindingScope = bindingScope;

            // Key 枚举与添加按钮一起放进 Composite 容器，值结构等点添加按钮时再求解。
            // 第一个子级是 Composite 容器：横向排 Key 枚举与添加按钮，值结构挂它的 SelectedUnionChildren
            MetaTypeEditorFieldDTO keySelector = new()
            {
                ID = Guid.NewGuid().ToString(),
                TypeKind = MetaTypeKind.Composite,
                Path = target.Path,
                Parent = target,
                SelectedUnionChildren = []
            };
            MetaTypeEditorFieldDTO addButton = MCDocumentMetaTypeDTOHelper.BuildAddButton(keySelector, target.Path);
            keySelector.Items = [keyEnum, addButton];
            keyEnum.Parent = keySelector;

            target.TypeKind = MetaTypeKind.Struct;
            target.Children = [keySelector];

            //点添加按钮后，按当前选中成员求解调度器并把结果挂成子节点。
            addButton.AddItemCommand = new RelayCommand(() =>
            {
                MetaTypeEditorFieldDTO valueNode = ResolveDynamicMapValue(
                    valueTemplate,
                    keyEnum,
                    keyItems,
                    bindingScope,
                    keyEnum.SelectedEnumItemIndex,
                    version,
                    target.Path ?? documentPath,
                    anchorMap,
                    depth,
                    typeName);

                AttachValueStructure(keySelector, valueNode);
            });

            return true;
        }
        private MetaTypeEditorFieldDTO? BuildDynamicKeyEnum(
            MetaTypeEditorFieldDTO targetTypeDTO,
            MetaValue actualArg,
            string version)
        {
            // GenericDTOTemplateBuilder 对 #[id="environment_attribute"] string
            // 这类实参会展开成 { id: environment_attribute } 的 MetaValue，
            // 这里需要把它重新组装回 FeatureMap 再交给 CompoundGenericMetaValueParser。
            if (actualArg.Kind is MetaValueKind.Type
                && actualArg.TypeValue?.LiteralValue is not null)
            {
                Dictionary<string, MetaValue> featureMap = new()
                {
                    { "id", actualArg }
                };
                MetaTypeEditorFieldDTO? enumDTO = CompoundGenericMetaValueParser.Parse(
                    resource,
                    version,
                    featureMap);
                if (enumDTO?.EnumOptionList?.Any(option => option.Name != "- unset -") == true)
                {
                    return enumDTO;
                }

                // mcdoc 里的资源通常带命名空间，例如 minecraft:environment_attribute，
                // 但 #[id="environment_attribute"] 可能省略命名空间。没有匹配到 dispatch 时补一次 minecraft: 前缀。
                string? idText = actualArg.TypeValue?.LiteralValue?.ToString();
                if (!string.IsNullOrEmpty(idText) && !idText.Contains(':'))
                {
                    MetaValue prefixedId = new()
                    {
                        Kind = MetaValueKind.Type,
                        TypeValue = new MetaType
                        {
                            Kind = MetaTypeKind.Literal,
                            LiteralValue = "minecraft:" + idText
                        }
                    };
                    Dictionary<string, MetaValue> prefixedFeatureMap = new()
                    {
                        { "id", prefixedId }
                    };
                    enumDTO = CompoundGenericMetaValueParser.Parse(resource, version, prefixedFeatureMap);
                    if (enumDTO?.EnumOptionList?.Any(option => option.Name != "- unset -") == true)
                    {
                        return enumDTO;
                    }
                }
            }

            if (actualArg.Kind is MetaValueKind.Type
                && actualArg.TypeValue?.AttributeList is not null)
            {
                MetaTypeEditorFieldDTO? enumDTO = CompoundGenericMetaValueParser.Parse(
                    resource,
                    version,
                    actualArg.TypeValue.AttributeList);
                if (enumDTO?.EnumOptionList?.Count > 0)
                {
                    return enumDTO;
                }
            }

            if (actualArg.Kind is MetaValueKind.Literal && actualArg.LiteralValue is not null)
            {
                ResolvedTypeReference reference = UsePathParser.Parse(
                    resource,
                    targetTypeDTO.Path,
                    actualArg.LiteralValue.ToString()!);
                if (reference?.Item?.FeatureMap is not null)
                {
                    MetaTypeEditorFieldDTO? enumDTO = CompoundGenericMetaValueParser.Parse(
                        resource,
                        version,
                        reference.Item.FeatureMap);
                    if (enumDTO?.EnumOptionList?.Count > 0)
                    {
                        return enumDTO;
                    }
                }
            }

            return null;
        }
        private MetaTypeEditorFieldDTO ResolveDynamicMapValue(
            MetaTypeEditorFieldDTO valueTemplate,
            MetaTypeEditorFieldDTO keyEnum,
            ObservableCollection<MetaTypeEditorFieldDTO> keyItems,
            TypeBindingScope bindingScope,
            int selectedIndex,
            string version,
            DocumentPath documentPath,
            Dictionary<string, KeyValueAnchors> anchorMap,
            RenderDepth depth,
            string typeName)
        {
            //克隆 Value 模板，附加上下文与 Key 集合，让内部 dispatch 能解析 %key。
            MetaTypeEditorFieldDTO valueNode = helper.InstantiateDTO(valueTemplate, version);
            valueNode.FieldName = "";
            valueNode.TypeName = null;
            valueNode.BindingScope = bindingScope;
            valueNode.Path = valueTemplate.Path ?? documentPath;
            AttachKeyItems(valueNode, keyItems);
            if (valueNode.Items is null) { valueNode.Items = keyItems; }

            AttachPath(valueNode, valueNode.Path);
            PrepareDispatchFieldNames(valueNode);
            MCDocumentResourceBuilder.BaseDataHandler(valueNode);
            IDocumentDTOBuildStrategy childBuilder = registry.Get(valueNode.TypeKind);
            childBuilder.Build(valueNode, valueNode, version, valueNode.Path ?? documentPath, anchorMap, depth, typeName);
            return valueNode;
        }
        /// <summary>
        /// 递归补齐子树路径，保证 Validator / UsePathParser 能定位资源。
        /// </summary>
        /// <summary>
        /// 业务过程：把当前泛型作用域递归挂到整棵子树上。
        /// 这样即使子节点不是 Dispatch，也能在后续泛型/别名解析中消费到外层实参。
        /// </summary>
        private static void AttachBindingScopeRecursively(MetaTypeEditorFieldDTO node, TypeBindingScope scope)
        {
            node.BindingScope ??= scope;

            if (node.Children is not null)
            {
                foreach (MetaTypeEditorFieldDTO child in node.Children)
                {
                    AttachBindingScopeRecursively(child, scope);
                }
            }

            if (node.SelectedUnionChildren is not null)
            {
                foreach (MetaTypeEditorFieldDTO child in node.SelectedUnionChildren)
                {
                    AttachBindingScopeRecursively(child, scope);
                }
            }
        }
        private static void AttachPath(MetaTypeEditorFieldDTO node, DocumentPath? path)
        {
            if (path is null)
            {
                return;
            }

            node.Path ??= path;

            if (node.Children is not null)
            {
                foreach (MetaTypeEditorFieldDTO child in node.Children)
                {
                    AttachPath(child, node.Path);
                }
            }

            if (node.SelectedUnionChildren is not null)
            {
                foreach (MetaTypeEditorFieldDTO child in node.SelectedUnionChildren)
                {
                    AttachPath(child, node.Path);
                }
            }
        }
        /// <summary>
        /// Dispatch 节点没有显示字段名时，用 Accessor 作为字段名，避免被 MCDocumentResourceBuilder 当成空节点隐藏。
        /// </summary>
        private static void PrepareDispatchFieldNames(MetaTypeEditorFieldDTO node)
        {
            if (node.TypeKind is MetaTypeKind.Dispatch
                && string.IsNullOrEmpty(node.FieldName)
                && node.FeatureMap.TryGetValue("Accessor", out MetaValue? accessor)
                && accessor is not null)
            {
                node.FieldName = accessor.Kind is MetaValueKind.List && accessor.Items?.Count > 0
                    ? accessor.Items[0].LiteralValue?.ToString() ?? ""
                    : accessor.LiteralValue?.ToString() ?? "";
            }

            if (node.Children is not null)
            {
                foreach (MetaTypeEditorFieldDTO child in node.Children)
                {
                    PrepareDispatchFieldNames(child);
                }
            }

            if (node.SelectedUnionChildren is not null)
            {
                foreach (MetaTypeEditorFieldDTO child in node.SelectedUnionChildren)
                {
                    PrepareDispatchFieldNames(child);
                }
            }
        }
        /// <summary>
        /// 把 Key 枚举集合挂到 Value 子树上，使 dispatch 分支里的 %key 能解析出当前选中项。
        /// </summary>
        private static void AttachKeyItems(MetaTypeEditorFieldDTO node, ObservableCollection<MetaTypeEditorFieldDTO> keyItems)
        {
            if (node.Children is not null)
            {
                foreach (MetaTypeEditorFieldDTO child in node.Children)
                {
                    child.Items ??= keyItems;
                    AttachKeyItems(child, keyItems);
                }
            }

            if (node.SelectedUnionChildren is not null)
            {
                foreach (MetaTypeEditorFieldDTO child in node.SelectedUnionChildren)
                {
                    child.Items ??= keyItems;
                    AttachKeyItems(child, keyItems);
                }
            }
        }
        /// <summary>
        /// 把求解出的值结构挂到 Composite 容器的子节点上：单分支直接挂，value 与 modifier 并存时保留 Union 外壳供切换。
        /// </summary>
        private static void AttachValueStructure(MetaTypeEditorFieldDTO field, MetaTypeEditorFieldDTO valueNode)
        {
            field.SelectedUnionChildren ??= [];
            if (valueNode?.Children is null || valueNode.Children.Count == 0)
            {
                return;
            }

            if (valueNode.Children.Count == 1)
            {
                valueNode.Children[0].Parent = field;
                field.SelectedUnionChildren.Add(valueNode.Children[0]);
                return;
            }

            valueNode.Parent = field;
            field.SelectedUnionChildren.Add(valueNode);
        }

        public bool CanHandle(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.Generic;
        }
    }
}


