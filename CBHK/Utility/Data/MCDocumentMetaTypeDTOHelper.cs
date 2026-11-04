using CBHK.CustomControl.VectorComboBox;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using CBHK.Utility.Data.DTOBuilder;
using CommunityToolkit.Mvvm.Input;
using DryIoc.ImTools;
using MinecraftLanguageModelLibrary.Data;
using CBHK.Interface.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
namespace CBHK.Utility.Data
{
    public class MCDocumentMetaTypeDTOHelper(Resource resource)
    {
        #region Field
        private Resource resource = resource;
        /// <summary>
        /// 正在构建的结构路径集合：同一份结构在构建过程中再次进入时不再展开，
        /// 用于打断 dispatch → generic → struct 这类间接递归（替代旧管线的 justSetView）。
        /// </summary>
        public HashSet<string> BuildingStructures { get; } = [];
        /// <summary>
        /// 正在补解析索引调度器的节点集合：构建期重入同一节点时直接跳过，避免递归。
        /// </summary>
        public HashSet<MetaTypeEditorFieldDTO> ResolvingIndexDispatchers { get; } = [];
        #endregion

        #region Property
        public MetaTypeDTOValidator Validator { get; set; }
        public ICommand CreateAddListItemCommand(MetaTypeEditorFieldDTO dto, string version)
            => new RelayCommand(() => ExecuteAddListItem(dto, version));
        public ICommand CreateAddCompositeOrCompoundItemCommand(MetaTypeEditorFieldDTO dto, string version)
            => new RelayCommand(() => ExecuteAddCompositeOrCompoundItem(dto, version));
        public ICommand CreateClearListItemCommand(MetaTypeEditorFieldDTO dto)
            => new RelayCommand(() => ExecuteClearListItem(dto));
        public ICommand CreateRemoveListItemCommand(MetaTypeEditorFieldDTO dto, MetaTypeEditorFieldDTO item = null)
            => new RelayCommand(() => ExecuteRemoveListItem(dto, item));
        public ICommand CreateRemoveCompositeOrCompoundItemCommand(MetaTypeEditorFieldDTO dto, MetaTypeEditorFieldDTO item = null)
            => new RelayCommand(() => ExecuteRemoveCompositeOrCompoundOrListItem(dto, item));
        public ICommand CreateReFreshCommand(MetaTypeEditorFieldDTO dto, string version)
            => new RelayCommand(() => ExecuteReFreshItem(dto, version));
        #endregion

        #region Method
        /// <summary>
        /// 根据形参列表与实参列表创建逐层泛型绑定作用域。
        /// </summary>
        public static TypeBindingScope CreateBindingScope(
            IList<Tuple<string, MetaValue>>? formalParams,
            IList<MetaValue>? actualArgs,
            TypeBindingScope? parent = null,
            string? documentPath = null)
        {
            TypeBindingScope scope = new()
            {
                Parent = parent,
                //未显式给出文档时沿用外层作用域，保证嵌套替换也能解析相对类型名
                DocumentPath = string.IsNullOrEmpty(documentPath) ? parent?.ResolveDocumentPath() : documentPath
            };
            if (formalParams is null || actualArgs is null)
            {
                return scope;
            }
            int count = Math.Min(formalParams.Count, actualArgs.Count);
            for (int i = 0; i < count; i++)
            {
                string formalName = formalParams[i].Item1;
                if (!string.IsNullOrEmpty(formalName))
                {
                    scope.Bindings[formalName] = ResolveArgument(actualArgs[i], parent);
                }
            }
            return scope;
        }

        /// <summary>
        /// 实参本身是外层形参占位符时，从绑定作用域取出外层注入的实参。
        /// </summary>
        private static MetaValue ResolveArgument(MetaValue argument, TypeBindingScope? scope)
        {
            if (scope is not null
                && argument.Kind is MetaValueKind.Type
                && argument.TypeValue?.Kind is MetaTypeKind.Literal)
            {
                string placeholderName = argument.TypeValue.LiteralValue?.ToString() ?? "";
                if (!string.IsNullOrEmpty(placeholderName)
                    && scope.TryResolve(placeholderName, out MetaValue boundArgument)
                    && boundArgument is not null)
                {
                    return boundArgument;
                }
            }
            return argument;
        }

        /// <summary>
        /// 生成 Composite 容器里的添加按钮。
        /// </summary>
        public static MetaTypeEditorFieldDTO BuildAddButton(MetaTypeEditorFieldDTO owner, DocumentPath path)
        {
            return new MetaTypeEditorFieldDTO
            {
                ID = "placeHolder",
                TypeKind = MetaTypeKind.Add,
                FieldName = "",
                Path = path,
                Parent = owner
            };
        }

        public static VectorTextComboBoxItem GetUnsetComboBoxItem() => new()
        {
            ItemID = "unset",
            Text = "- unset -",
            IsSelected = true
        };

        public static VectorTextComboBoxItem BuildTextComboBoxItem(string id, string text) => new()
        {
            ItemID = id,
            Text = text
        };

        /// <summary>
        /// 根据目标DTO生成一个Entry节点，Entry节点用于在Composite或Compound类型中添加新的子项
        /// </summary>
        /// <param name="targetDTO"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public MetaTypeEditorFieldDTO GetEntryItem(MetaTypeEditorFieldDTO targetDTO, string version)
        {
            MetaTypeEditorFieldDTO result = new();
            result.CopyFrom(targetDTO);
            result.TypeKind = MetaTypeKind.Composite;
            result.FieldName = "Entry";
            result.Items = [];
            if (targetDTO.Children is not null)
            {
                result.SelectedUnionChildren ??= [.. targetDTO.Children];
            }
            MetaTypeEditorFieldDTO removeDTO = new()
            {
                ID = "placeHolder",
                TypeKind = MetaTypeKind.Remove,
                RemoveItemCommand = targetDTO.TypeKind is MetaTypeKind.List ? CreateClearListItemCommand(result) : CreateRemoveCompositeOrCompoundItemCommand(targetDTO.Parent, result)
            };
            result.Items.Insert(0, removeDTO);
            result.Items.Add(targetDTO);
            targetDTO.Parent = result;
            if (targetDTO.AddItemCommand is not null)
            {
                targetDTO.AddItemCommand = CreateAddListItemCommand(result, version);
            }
            return result;
        }

        /// <summary>
        /// 联合体成员更新事件
        /// </summary>
        /// <summary>
        /// 正在执行切换的联合体集合，防止切换过程中重入自身导致递归。
        /// </summary>
        private static readonly HashSet<MetaTypeEditorFieldDTO> updatingUnions = [];

        /// <summary>
        /// 可选结构的懒加载桩就地展开：StructDTOBuilder 会把真子级缓存进 Value，自身只留一个 Any 占位。
        /// 分支内容（切换落点）与默认选中内容（联合构建收尾）都要取回它，否则界面上只剩一个空桩。
        /// </summary>
        public bool TryExpandLazyStub(MetaTypeEditorFieldDTO node, string version)
        {
            if (node is null || node.IsRequired || node.TypeKind is not MetaTypeKind.Struct
                || node.Children is not { Count: 1 } || !IsPlaceHolderNode(node.Children[0])
                || node.Value is not ObservableCollection<MetaTypeEditorFieldDTO> stashedMembers)
            {
                return false;
            }
            node.Value = null;
            node.Children = [.. stashedMembers];
            HierarchicallyUpdateTreeStructuredData(node, version);
            //恢复出来的子级还没走过各自类型的构建（桩是"先缓存、展开时再走验证流程"），这里按类型补建：
            //否则动态键元组不会生成键选择器和增删按钮，泛型 map 展开后也只剩一堆未替换的模板
            DocumentDTOBuildStrategyRegistry registry = DocumentDTOBuildStrategyRegistry.Create(resource, this);
            foreach (MetaTypeEditorFieldDTO child in node.Children.ToList())
            {
                if (child is null || IsPlaceHolderNode(child) || !child.IsVisible)
                {
                    continue;
                }
                MCDocumentResourceBuilder.BuildResource(child, child, version, node.Path, resource, this);
                registry.Get(child.TypeKind).Build(child, child, version, node.Path, [], RenderDepth.Deep);
            }
            return true;
        }

        private readonly HashSet<MetaTypeEditorFieldDTO> bindingIndexStructures = [];

        /// <summary>
        /// 按同级字段的当前值，就地重解析依赖它的调度器，并把后续变化挂到订阅上。
        ///
        /// 为什么要在构建/落点之后显式跑一遍、而不是只依赖订阅：
        /// 联合落点会用新实例替换索引字段，订阅留在被替换掉的旧实例上，
        /// 用户改的却是新实例，结果就是"选了值但依赖它的调度器不更新"。
        /// 每次子树重建 / 落点之后重新扫一遍，就不依赖事件挂在哪个实例上。
        /// </summary>
        public void BindSiblingIndexDispatchers(MetaTypeEditorFieldDTO structDTO, string version)
        {
            if (structDTO?.Children is null || structDTO.Children.Count == 0
                || !bindingIndexStructures.Add(structDTO))
            {
                return;
            }

            try
            {
                DocumentDTOBuildStrategyRegistry registry = DocumentDTOBuildStrategyRegistry.Create(resource, this);

                //索引可能已被解析成字面量，沿模板链回溯到最初的模板（如 [[modifier]]）取访问器名
                string IndexAccessorName(MetaTypeEditorFieldDTO node)
                {
                    MetaTypeEditorFieldDTO templateNode = node;
                    for (int hopCount = 0; hopCount < 16 && templateNode.TemplateReference is not null && !ReferenceEquals(templateNode.TemplateReference, templateNode); hopCount++)
                    {
                        templateNode = templateNode.TemplateReference;
                    }

                    return templateNode.FeatureMap is not null && templateNode.FeatureMap.TryGetValue("Index", out MetaValue index)
                        ? ExtractAccessorPath(index)
                        : null;
                }

                //索引变化后就地重解析（内容落点、必选性都沿用原节点）
                void ResolveAccessorIndex(MetaTypeEditorFieldDTO child, string indexValue)
                {
                    if (string.IsNullOrEmpty(indexValue) || !ResolvingIndexDispatchers.Add(child))
                    {
                        return;
                    }
                    try
                    {
                        MetaTypeKind previousKind = child.TypeKind;
                        DocumentPath targetPath = child.Path ?? structDTO.Path;
                        child.FeatureMap["Index"] = new MetaValue { Kind = MetaValueKind.Literal, LiteralValue = indexValue };
                        child.TypeKind = MetaTypeKind.Dispatch;
                        child.Children = null;
                        MCDocumentResourceBuilder.BuildResource(child, child, version, targetPath, resource, this);
                        registry.Get(child.TypeKind).Build(child, child, version, targetPath, [], RenderDepth.Deep);
                        //类型变了：在宿主显示它的集合里挪一下，视图会重建该项并按新类型选模板，否则会少行或没名字
                        if (previousKind != child.TypeKind && child.Parent?.SelectedUnionChildren?.Contains(child) == true)
                        {
                            child.Parent.SelectedUnionChildren.Remove(child);
                            child.Parent.SelectedUnionChildren.Add(child);
                        }
                    }
                    finally
                    {
                        ResolvingIndexDispatchers.Remove(child);
                    }
                }

                for (int i = 0; i < structDTO.Children.Count; i++)
                {
                    MetaTypeEditorFieldDTO child = structDTO.Children[i];
                    if (child.FeatureMap is null
                        || !child.FeatureMap.TryGetValue("Resource", out MetaValue indexResource) || indexResource is null
                        || !child.FeatureMap.TryGetValue("Index", out MetaValue indexMeta) || indexMeta is null)
                    {
                        continue;
                    }

                    //索引是同级字段名时，取该字段的当前值；字段无实例或匹配不到结构时按常量处理
                    string indexFieldName = IndexAccessorName(child);
                    if (string.IsNullOrEmpty(indexFieldName) || indexFieldName.StartsWith('%'))
                    {
                        continue;
                    }

                    MetaTypeEditorFieldDTO indexField = structDTO.Children.FirstOrDefault(item => item.FieldName == indexFieldName);
                    //索引字段可能被联合/复合壳包着（如 trigger 在 1.20.3 之后只剩一个 #[id="trigger_type"] 分支），
                    //外壳自己没有候选：取值和订阅都要落到里层真正承载枚举的那个节点上
                    MetaTypeEditorFieldDTO indexValueOwner = ResolveIndexValueOwner(indexField);
                    ResolveAccessorIndex(child, ReadSelectedIndexValue(indexValueOwner));

                    if (indexValueOwner is null)
                    {
                        continue;
                    }

                    //顺手修掉索引字段过期的父链（落点/提升后它可能还指着外层条目），
                    //枚举变化时的重解析要从这里往上找到本结构
                    if (indexField is not null && !ReferenceEquals(indexField.Parent, structDTO))
                    {
                        indexField.Parent = structDTO;
                    }
                    //索引字段变化后，重新解析同级里依赖它的调度器（带重入保护）
                    bool isRefreshingIndex = false;
                    Action previousEnumUpdated = indexValueOwner.SelectedEnumItemUpdated;
                    indexValueOwner.SelectedEnumItemUpdated = () =>
                    {
                        previousEnumUpdated?.Invoke();
                        if (isRefreshingIndex)
                        {
                            return;
                        }
                        isRefreshingIndex = true;
                        try
                        {
                            string currentIndexValue = ReadSelectedIndexValue(indexValueOwner);
                            foreach (MetaTypeEditorFieldDTO sibling in structDTO.Children ?? [])
                            {
                                if (sibling.FeatureMap is null
                                    || !sibling.FeatureMap.ContainsKey("Resource")
                                    || !sibling.FeatureMap.TryGetValue("Index", out MetaValue siblingIndex) || siblingIndex is null)
                                {
                                    continue;
                                }
                                if (IndexAccessorName(sibling) == indexFieldName)
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
            finally
            {
                bindingIndexStructures.Remove(structDTO);
            }
        }

        /// <summary>
        /// 下钻到真正承载候选项的节点。索引字段经常被联合/单子复合包着
        /// （例如 trigger 在 1.20.3 之后只剩一个 #[id="trigger_type"] 分支），
        /// 外壳本身没有 EnumOptionList，取值与订阅都要落到里层枚举上。
        /// </summary>
        public static MetaTypeEditorFieldDTO ResolveIndexValueOwner(MetaTypeEditorFieldDTO field)
        {
            for (int depth = 0; field is not null && depth < 8; depth++)
            {
                if (field.SelectedEnumOption is not null
                    || field.EnumOptionList is { Count: > 0 }
                    || field.TypeKind is MetaTypeKind.Literal)
                {
                    return field;
                }

                MetaTypeEditorFieldDTO inner = field.SelectedUnionChildren?.FirstOrDefault()
                    ?? (field.Children is { Count: 1 } ? field.Children[0] : null);
                if (inner is null)
                {
                    return null;
                }
                field = inner;
            }
            return null;
        }

        /// <summary>读取索引字段当前选中值；停在 "- unset -" 位时视为没有值。</summary>
        public static string ReadSelectedIndexValue(MetaTypeEditorFieldDTO field)
        {
            if (field is null)
            {
                return null;
            }

            if (field.SelectedEnumOption is not null)
            {
                if (field.SelectedEnumOption.Name == "- unset -")
                {
                    return null;
                }
                return field.SelectedEnumOption.Value?.LiteralValue?.ToString() ?? field.SelectedEnumOption.Name;
            }

            return field.TypeKind is MetaTypeKind.Literal ? field.Value?.ToString() : null;
        }

        /// <summary>
        /// 把节点自己的注解留在最上层，并剥离从远端（定义/分支/被提升子级）复制过来的版本门控。
        /// mcdoc 的版本校验是逐节点独立的：since/until 只描述声明它的那个节点，
        /// 跟着 CopyFrom 一起搬下来会把整支的版本条件压到字段上（例如 item 会继承分支的 since）。
        /// </summary>
        public static void ApplyOwnFeatures(MetaTypeEditorFieldDTO target, Dictionary<string, MetaValue> ownFeatures)
        {
            if (target is null)
            {
                return;
            }

            target.FeatureMap ??= [];
            if (!ownFeatures.ContainsKey("since"))
            {
                target.FeatureMap.Remove("since");
            }
            if (!ownFeatures.ContainsKey("until"))
            {
                target.FeatureMap.Remove("until");
            }
            foreach (KeyValuePair<string, MetaValue> pair in ownFeatures)
            {
                target.FeatureMap[pair.Key] = pair.Value;
            }
        }

        /// <summary>
        /// 回填父子链：泛型替换产物、值结构等由拷贝/实例化得到的子树常常没有 Parent，
        /// 而"同级字段变更后重新解析调度器"（SelectedEnumItemUpdated）与落点都依赖 Parent 链。
        /// </summary>
        public static void AttachParentLinks(MetaTypeEditorFieldDTO node)
        {
            if (node is null)
            {
                return;
            }
            foreach (MetaTypeEditorFieldDTO child in node.Children ?? [])
            {
                child.Parent = node;
                AttachParentLinks(child);
            }
            foreach (MetaTypeEditorFieldDTO child in node.SelectedUnionChildren ?? [])
            {
                child.Parent = node;
                AttachParentLinks(child);
            }
            foreach (MetaTypeEditorFieldDTO child in node.Items ?? [])
            {
                child.Parent = node;
                AttachParentLinks(child);
            }
            //懒加载桩的缓存集合（Value）里也挂着待展开的子树，同样要带回父子链
            if (node.Value is ObservableCollection<MetaTypeEditorFieldDTO> stashedChildren)
            {
                foreach (MetaTypeEditorFieldDTO child in stashedChildren)
                {
                    child.Parent = node;
                    AttachParentLinks(child);
                }
            }
        }

        private readonly HashSet<string> deferredKeyResolvedVersions = [];

        /// <summary>
        /// 构建收尾的具名键补解析：DocumentItemMap 是"边建文档边填"的，跨文件枚举引用（use 别名，如
        /// structure.mcdoc 的 use super::biome::MobCategory）在构建期可能还没注册，动态 map 的键会退化成
        /// 类型名 Literal。整批文档建完后在这里补一次，命中后按正常路径把键选择器与增删按钮补出来（每版本一次）。
        /// </summary>
        public void ResolveDeferredNamedKeys(string version)
        {
            if (string.IsNullOrEmpty(version) || !deferredKeyResolvedVersions.Add(version))
            {
                return;
            }
            DocumentDTOBuildStrategyRegistry registry = DocumentDTOBuildStrategyRegistry.Create(resource, this);
            HashSet<MetaTypeEditorFieldDTO> visited = [];
            foreach (MetaTypeEditorFieldDTO root in resource.DocumentItemMap.Values)
            {
                Walk(root);
            }
            void Walk(MetaTypeEditorFieldDTO node)
            {
                if (node is null || !visited.Add(node))
                {
                    return;
                }
                if (node.TypeKind is MetaTypeKind.Tuple && node.Children?.Count > 1
                    && TryResolveNamedKeyEnum(node.Children[0], node.Path))
                {
                    //键解析成功：按正常路径把键选择器与增删按钮补出来（构建期枚举还没注册时这一步被跳过了）
                    registry.Get(MetaTypeKind.Tuple).Build(node, node.TemplateReference ?? node, version, node.Path, [], RenderDepth.Deep);
                }
                foreach (MetaTypeEditorFieldDTO child in (node.Children ?? []).ToList())
                {
                    Walk(child);
                }
                foreach (MetaTypeEditorFieldDTO child in (node.Items ?? []).ToList())
                {
                    Walk(child);
                }
                foreach (MetaTypeEditorFieldDTO child in (node.SelectedUnionChildren ?? []).ToList())
                {
                    Walk(child);
                }
                if (node.Value is ObservableCollection<MetaTypeEditorFieldDTO> stashedMembers)
                {
                    foreach (MetaTypeEditorFieldDTO stashed in stashedMembers.ToList())
                    {
                        Walk(stashed);
                    }
                }
                Walk(node.ElementType);
            }
        }

        /// <summary>
        /// 具名枚举键解析：键元素是类型名引用（[Direction]、[MobCategory] 等）时，借目标枚举的选项当键。
        /// 路径解析（含 use 别名）优先，解不到再按"路径以 ::&lt;类型名&gt; 结尾且带枚举选项"搜一遍，唯一才采用。
        /// </summary>
        public bool TryResolveNamedKeyEnum(MetaTypeEditorFieldDTO keyElement, DocumentPath path)
        {
            if (keyElement is null || keyElement.TypeKind is MetaTypeKind.Enum || keyElement.EnumOptionList is { Count: > 0 })
            {
                return false;
            }
            string keyTypeName = keyElement.Value?.ToString();
            if (string.IsNullOrEmpty(keyTypeName))
            {
                return false;
            }
            MetaTypeEditorFieldDTO referencedEnum = UsePathParser.Parse(resource, path, keyTypeName)?.Item;
            if (referencedEnum?.EnumOptionList is not { Count: > 0 })
            {
                List<MetaTypeEditorFieldDTO> namedEnumMatches = [.. resource.DocumentItemMap
                    .Where(pair => pair.Key.EndsWith("::" + keyTypeName, StringComparison.Ordinal)
                        && pair.Value?.EnumOptionList is { Count: > 0 })
                    .Select(pair => pair.Value)];
                if (namedEnumMatches.Count == 1)
                {
                    referencedEnum = namedEnumMatches[0];
                }
            }
            if (referencedEnum?.EnumOptionList is not { Count: > 0 } referencedOptions)
            {
                return false;
            }
            keyElement.EnumOptionList = [.. referencedOptions];
            keyElement.TypeKind = MetaTypeKind.Enum;
            return true;
        }

        public void SelectedUnionItemUpdated(MetaTypeEditorFieldDTO unionDTO, string version)
        {
            if (!updatingUnions.Add(unionDTO))
            {
                return;
            }
            try
            {
                SelectedUnionItemUpdatedCore(unionDTO, version);
            }
            finally
            {
                updatingUnions.Remove(unionDTO);
            }
        }

        //可选联合的“未设置”位：名称表首项是 - unset -，且当前选择停在它上面。
        //不能只看 IsRequired：该标记可能晚于联合构建才被写上，会造成"已拼入内容却显示未设置"
        public static bool IsUnsetUnionSelection(MetaTypeEditorFieldDTO node)
        {
            return node is not null
                && node.UnionTypeNameList?.FirstOrDefault()?.Name == "- unset -"
                && (node.SelectedUnionItemIndex <= 0 || node.SelectedUnionTypeName?.Name == "- unset -");
        }

        private void SelectedUnionItemUpdatedCore(MetaTypeEditorFieldDTO unionDTO, string version)
        {
            #region Field
            //分支列表取联合体自身的子级；内容落到行内区持有它的宿主容器上（没有宿主时落回自身）
            MetaTypeEditorFieldDTO branchDTO = unionDTO;
            //只有行内选择器（与宿主同名的那个联合）才把落点提升到宿主 Composite 上，
            //结构体字段上的普通联合（如 sound）要落在自己身上，否则内容会写到看不见的宿主行里
            MetaTypeEditorFieldDTO targetDTO = unionDTO.Parent is not null && unionDTO.Parent.Items?.Contains(unionDTO) == true
                && unionDTO.FieldName == unionDTO.Parent.FieldName
                ? unionDTO.Parent
                : unionDTO;
            //行内区里选择器后面的内容每次由落点重建，避免反复切换分支时累积（容器分支与值分支都要先清）
            if (targetDTO.TypeKind is MetaTypeKind.Composite && targetDTO.Items is not null)
            {
                //选择器是与宿主同名的那个联合；行内的值编辑器也可能是 Union，不能用"最后一个 Union"定位
                int hostSelectorIndex = targetDTO.Items.ToList().FindLastIndex(item => item.TypeKind is MetaTypeKind.Union && item.FieldName == targetDTO.FieldName);
                if (hostSelectorIndex > -1)
                {
                    while (targetDTO.Items.Count > hostSelectorIndex + 1)
                    {
                        targetDTO.Items.RemoveAt(targetDTO.Items.Count - 1);
                    }
                }
            }
            if (IsUnsetUnionSelection(unionDTO))
            {
                //模板同样要挪进桩里，否则未设置态还会渲染出分支模板结构
                if (unionDTO.Value is not ObservableCollection<MetaTypeEditorFieldDTO> && unionDTO.Children is { Count: > 0 })
                {
                    unionDTO.Value = new ObservableCollection<MetaTypeEditorFieldDTO>(unionDTO.Children);
                }
                if (targetDTO.TypeKind is MetaTypeKind.Composite && targetDTO.Items is not null)
                {
                    int unsetSelectorIndex = targetDTO.Items.ToList().FindLastIndex(item => item.TypeKind is MetaTypeKind.Union && item.FieldName == targetDTO.FieldName);
                    while (targetDTO.Items.Count > unsetSelectorIndex + 1)
                    {
                        targetDTO.Items.RemoveAt(targetDTO.Items.Count - 1);
                    }
                }
                unionDTO.SelectedUnionChildren = [];
                //换新集合而不是就地清空：子行可能是列表的元素集合，清空会把列表里的元素一起删掉
                targetDTO.SelectedUnionChildren = [];
                unionDTO.Children = [];
                //宿主的成员外壳换成一个空集合，让容器不显示展开箭头。
                //不能 Clear：行内选择器与宿主可能共享同一个集合对象，Clear 会把选择器的分支表也清掉
                if (targetDTO.TypeKind is MetaTypeKind.Composite)
                {
                    targetDTO.Children = [];
                }
                return;
            }
            #endregion
            //界面下拉框回传的可能是同名的另一个实例，按下标找不到时按名字重新定位
            //分支模板挪进不参与渲染的桩（Value），Children 只留给界面渲染的实际子行；
            //否则模板结构会一直显示（比如切到 sound_event 时还挂着 WeightedSoundEvent 的字段）
            if (unionDTO.Value is null && unionDTO.Children is { Count: > 0 })
            {
                unionDTO.Value = new ObservableCollection<MetaTypeEditorFieldDTO>(unionDTO.Children);
            }
            ObservableCollection<MetaTypeEditorFieldDTO> branchTemplates = unionDTO.Value as ObservableCollection<MetaTypeEditorFieldDTO> ?? unionDTO.Children;
            int selectedIndex = unionDTO.SelectedUnionItemIndex;
            if (selectedIndex < 0 || selectedIndex > (branchTemplates?.Count ?? 0))
            {
                selectedIndex = unionDTO.UnionTypeNameList is null
                    ? -1
                    : unionDTO.UnionTypeNameList.ToList().FindIndex(item => item.Name == unionDTO.SelectedUnionTypeName?.Name);
                unionDTO.SelectedUnionItemIndex = selectedIndex;
            }
            if (branchTemplates is not null && selectedIndex > -1 && selectedIndex <= branchTemplates.Count)
            {
                #region 提取目标分支、发送给验证器、执行剥壳
                //分支表比名称表少一个“未设置”项：可选联合如此，必选节点上残留的未设置项同样如此，
                //按名称表首项判断才不会把选中项落到邻支上
                int index = selectedIndex;
                if (branchDTO.UnionTypeNameList?.FirstOrDefault()?.Name == "- unset -")
                {
                    index--;
                }
                if (index < 0)
                {
                    index = 0;
                }
                if (index < 0 || index >= branchTemplates.Count)
                {
                    return;
                }
                MetaTypeEditorFieldDTO targetChildTemplate = branchTemplates[index];
                MetaTypeEditorFieldDTO targetChildInstance = InstantiateDTO(targetChildTemplate, version);
                DTOInstanceContext context = new([targetChildInstance], []);
                Validator.Verify(context, [targetChildTemplate], version, targetChildInstance.Path ?? branchDTO.Path, false);
                // 对当前节点执行展平/提升，去除内部可能残留的 Literal、Generic 或单子 Union
                HierarchicallyUpdateTreeStructuredData(context.dtoInstanceList[0], version);
                var resultDTO = context.dtoInstanceList[0];
                #endregion
                #region 分支内容是懒加载桩时就地展开
                TryExpandLazyStub(resultDTO, version);
                #endregion
                #region 分支内容里只套着一层联合时，把它的成员并到当前联合上
                //分支类型写成具名联合（如 value: StringRGB）时，解析/物化出来的是一个复合，里面挂的才是联合。
                //这一层留着会把名称表拆成两份、界面上多出一层；并上来后名称表合成一份，
                //切换分支仍旧先走联合自己的分支表，取不到分支模板时再由外层兜底。
                MetaTypeEditorFieldDTO hostedUnion = resultDTO.TypeKind is MetaTypeKind.Composite && resultDTO.Children is null or { Count: 0 }
                    ? resultDTO.Items?.FirstOrDefault(item => item.TypeKind is MetaTypeKind.Union && item.FieldName != resultDTO.FieldName)
                    : null;
                //分支表可能已经挪进不参与渲染的桩（Value）：并表时两处都要看
                List<MetaTypeEditorFieldDTO> nestedBranchList = hostedUnion?.Children is { Count: > 0 }
                    ? [.. hostedUnion.Children]
                    : hostedUnion?.Value is ObservableCollection<MetaTypeEditorFieldDTO> nestedStash ? [.. nestedStash] : null;
                if (hostedUnion is { UnionTypeNameList: { Count: > 1 } } && nestedBranchList is { Count: > 0 }
                    && unionDTO.UnionTypeNameList is { Count: > 0 } && unionDTO.SelectedUnionItemIndex >= 0)
                {
                    bool parentHasUnset = unionDTO.UnionTypeNameList[0].Name == "- unset -";
                    bool nestedHasUnset = hostedUnion.UnionTypeNameList[0].Name == "- unset -";
                    int parentSelected = unionDTO.SelectedUnionItemIndex - (parentHasUnset ? 1 : 0);
                    int parentBranchCount = unionDTO.UnionTypeNameList.Count - (parentHasUnset ? 1 : 0);
                    List<EnumMember> mergedNameList = [];
                    List<MetaTypeEditorFieldDTO> mergedBranchList = [];
                    //名表与分支表的下标对齐：未设置位没有对应分支，逐位重建
                    for (int i = 0; i < parentBranchCount; i++)
                    {
                        if (i == parentSelected)
                        {
                            for (int j = nestedHasUnset ? 1 : 0; j < hostedUnion.UnionTypeNameList.Count; j++)
                            {
                                mergedNameList.Add(hostedUnion.UnionTypeNameList[j]);
                            }
                            mergedBranchList.AddRange(nestedBranchList);
                        }
                        else
                        {
                            mergedNameList.Add(unionDTO.UnionTypeNameList[i + (parentHasUnset ? 1 : 0)]);
                            if (unionDTO.Children is not null && i < unionDTO.Children.Count)
                            {
                                mergedBranchList.Add(unionDTO.Children[i]);
                            }
                        }
                    }
                    if (mergedBranchList.Count > 0)
                    {
                        unionDTO.UnionTypeNameList = [.. (parentHasUnset ? [unionDTO.UnionTypeNameList[0]] : Enumerable.Empty<EnumMember>()), .. mergedNameList];
                        unionDTO.Children = [.. mergedBranchList];
                        unionDTO.Value = new ObservableCollection<MetaTypeEditorFieldDTO>(mergedBranchList);
                        //选中项跟到并入的位置上；里层停在未设置位时用父级原来的选中项
                        int nestedSelected = hostedUnion.SelectedUnionItemIndex - (nestedHasUnset ? 1 : 0);
                        int mergedSelected = (parentHasUnset ? 1 : 0) + parentSelected + (nestedSelected >= 0 ? nestedSelected : 0);
                        mergedSelected = Math.Clamp(mergedSelected, 0, unionDTO.UnionTypeNameList.Count - 1);
                        unionDTO.SelectedUnionItemIndex = mergedSelected;
                        unionDTO.SelectedUnionTypeName = unionDTO.UnionTypeNameList[mergedSelected];
                        //里层宿主隐掉：它的成员已经并到父级，再显示就是多余的一层
                        resultDTO.Items?.Remove(hostedUnion);
                        //并表后按新表再走一次落点：选中成员（值类型/容器）由常规落点摆进行内区或子行，
                        //否则首次解析会只剩选择器、行内交互控件丢失。
                        //用 Core 而不是带重入锁的公开入口，避免被"同一节点正在处理"挡掉
                        SelectedUnionItemUpdatedCore(unionDTO, version);
                        return;
                    }
                }
                #endregion
                bool isColorValueMember = resultDTO.ElementType is null
                    && resultDTO.TypeKind is MetaTypeKind.NamedColor or MetaTypeKind.String or MetaTypeKind.Int
                        or MetaTypeKind.Float or MetaTypeKind.Boolean or MetaTypeKind.Enum;
                //列表/数组按值分支落点：定长元素直接进子级，变长在行内区挂添加/清除按钮
                bool isListMember = resultDTO.TypeKind is MetaTypeKind.List or MetaTypeKind.ByteArray
                    or MetaTypeKind.IntArray or MetaTypeKind.LongArray or MetaTypeKind.UUIDArray;
                //结构类容器即使目标没解析出子级也落子级，不能变成行内值编辑器；
                //其余（值类型与联合/引用/字面量等间接类型）仍按值分支落进行内区
                bool isContainerBranch = !isListMember
                    && (IsContainerType(resultDTO.TypeKind) || (IsIndirectType(resultDTO.TypeKind) && resultDTO.Children is not null));
                //处理容器类枚举
                if (!isColorValueMember && isContainerBranch)
                {
                    #region 更换分支
                    if (targetDTO.Items is not null)
                    {
                        //只清掉行内区的值编辑器，保留键选择器与添加/删除按钮
                        for (int i = 0; i < targetDTO.Items.Count; i++)
                        {
                            MetaTypeEditorFieldDTO item = targetDTO.Items[i];
                            bool isValueEditor = !IsContainerType(item.TypeKind)
                                && !IsIndirectType(item.TypeKind)
                                && item.TypeKind is not (MetaTypeKind.Enum or MetaTypeKind.Add or MetaTypeKind.Remove);
                            if (isValueEditor)
                            {
                                targetDTO.Items.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                    //内容落进已有的入口结构节点（由添加按钮建立），没有入口时直接挂内容
                    MetaTypeEditorFieldDTO entry = null;
                    if (targetDTO.SelectedUnionChildren is not null)
                    {
                        foreach (MetaTypeEditorFieldDTO child in targetDTO.SelectedUnionChildren)
                        {
                            if (child.IsInterpretFromDispatch)
                            {
                                entry = child;
                                break;
                            }
                        }
                    }
                    //目标结构没解析出子级时挂节点自身：容器/引用分支绝不能落进行内区变成值编辑器
                    IEnumerable<MetaTypeEditorFieldDTO> branchContent = resultDTO.Children is { Count: > 0 }
                        ? resultDTO.Children
                        : [resultDTO];
                    if (entry is not null)
                    {
                        entry.Children ??= [];
                        entry.Children.Clear();
                        foreach (MetaTypeEditorFieldDTO child in branchContent)
                        {
                            child.Parent = entry;
                            entry.Children.Add(child);
                        }
                    }
                    else
                    {
                        //换新集合而不是就地清空：子行集合可能是列表的元素集合（列表分支套进行内区后两者共用），
                        //清空会把列表里的元素一起删掉
                        ObservableCollection<MetaTypeEditorFieldDTO> containerRows = [];
                        foreach (MetaTypeEditorFieldDTO child in branchContent)
                        {
                            child.Parent = targetDTO;
                            containerRows.Add(child);

                        }
                        targetDTO.SelectedUnionChildren = containerRows;
                        //宿主的成员外壳与实际渲染的子行保持一致，容器的展开箭头才不会错位
                        if (targetDTO.TypeKind is MetaTypeKind.Composite)
                        {
                            targetDTO.Children = [.. targetDTO.SelectedUnionChildren];
                            //子行是从 SelectedUnionChildren 搬过来的，父链要跟着换宿主
                            AttachParentLinks(targetDTO);
                        }
                    }
                    #endregion
                }
                //处理值类枚举
                else
                {
                    #region 锁定处理目标并清除它的子级
                    //只有行内选择器（与宿主同名的联合，宿主即 targetDTO）才把编辑器放进宿主行内区；
                    //结构体字段上的联合（如 loop）必须自己当宿主，否则会清掉父级整层内容
                    MetaTypeEditorFieldDTO compositeDTO = targetDTO.TypeKind is MetaTypeKind.Composite ? targetDTO : null;
                    bool isUnionSelfHost = compositeDTO is null && targetDTO.TypeKind is MetaTypeKind.Union;
                    if (isUnionSelfHost)
                    {
                        //本节点行内区里已经有自己的选择器时按行内宿主处理
                        compositeDTO = targetDTO;
                    }
                    bool hasInlineSelector = compositeDTO?.Items?.Any(item => item.TypeKind is MetaTypeKind.Union && item.FieldName == compositeDTO.FieldName) == true;
                    //宿主不是 Composite（联合直接挂在结构体/元组等带行内区的容器上）时退回目标自身并补出行内区：
                    //落点必须有地方放选择内容，否则后面所有行内区操作都会对着 null 集合进行
                    compositeDTO ??= targetDTO;
                    compositeDTO.Items ??= [];
                    if (compositeDTO?.Items is not null)
                    {
                        //同宿主的选择器定位：用"与宿主同名的 Union"，避免把行内值编辑器当成选择器
                        int selectorIndex = compositeDTO.Items.ToList().FindLastIndex(item => item.TypeKind is MetaTypeKind.Union && item.FieldName == compositeDTO.FieldName);
                        if (selectorIndex > -1)
                        {
                            while (compositeDTO.Items.Count > selectorIndex + 1)
                            {
                                compositeDTO.Items.RemoveAt(compositeDTO.Items.Count - 1);

                            }
                        }
                    }
                    #endregion
                    #region 若切换为列表则给当前Composite容器赋值并给予容器添加按钮，否则视为值类型分支添加给Items列表
                    if (targetChildInstance.TypeKind is MetaTypeKind.List)
                    {
                        //列表分支要把加号/清除按钮挂到宿主行内区：宿主可能还没有行内区（组合壳刚物化出来），先建好再挂，
                        //否则后面的 Items.Add / Items.Count 会直接空引用
                        compositeDTO.Items ??= [];
                        //列表元素落在分支表里的列表节点上：重新实例化不会带上元素，用暂存实例的话切走再切回来元素就没了
                        MetaTypeEditorFieldDTO listTarget = targetChildTemplate.TypeKind is MetaTypeKind.List ? targetChildTemplate : targetChildInstance;
                        //只有"精确长度"（上下界相同）且元素是值类型时才不给增删按钮；
                        //最小长度（@ 1..）与容器元素都要保留加号，最低数量交给 UI 提示
                        bool isFixedLength = listTarget.FeatureMap?.TryGetValue("LengthFrom", out MetaValue lengthFromMeta) == true
                            && listTarget.FeatureMap.TryGetValue("LengthTo", out MetaValue lengthToMeta)
                            && lengthFromMeta?.LiteralValue?.ToString() == lengthToMeta?.LiteralValue?.ToString()
                            && listTarget.ElementType is not null
                            && !MetaTypeKindPredicates.IsContainerType(listTarget.ElementType.TypeKind)
                            && !MetaTypeKindPredicates.IsIndirectType(listTarget.ElementType.TypeKind);
                        if (!isFixedLength)
                        {
                            MetaTypeEditorFieldDTO addDTO = new()
                            {
                                ID = "placeHolder",
                                TypeKind = MetaTypeKind.Add,
                                Parent = compositeDTO,
                                //增删作用在列表节点本身：列表内容已套进行内区，按钮不能再指向宿主
                                AddItemCommand = CreateAddListItemCommand(listTarget, version),

                            }
;
                            MetaTypeEditorFieldDTO removeDTO = new()
                            {
                                ID = "placeHolder",
                                TypeKind = MetaTypeKind.Remove,
                                Parent = compositeDTO,
                                RemoveItemCommand = CreateClearListItemCommand(listTarget)

                            }
;
                            compositeDTO.Items.Add(addDTO);
                            compositeDTO.Items.Add(removeDTO);
                        }
                        //宿主行内区里的联合可能才是真正的列表定义：元素类型从它身上取
                        if (compositeDTO.ElementType is null && compositeDTO.Items.Count > 0)
                        {
                            var subUnionDTO = compositeDTO.Items.FirstOrDefault(item => item.TypeKind is MetaTypeKind.Union);
                            MetaTypeEditorFieldDTO listDTO = null;
                            if (subUnionDTO?.Children?.Count > 0)
                            {
                                listDTO = subUnionDTO.Children.FirstOrDefault(item => item.TypeKind is MetaTypeKind.List);
                            }
                            if (listDTO is not null)
                            {
                                compositeDTO.ElementType = listDTO.ElementType;
                            }
                        }
                        //元素集合本身留给列表节点，切到别的分支不会把它清空
                        listTarget.Items ??= [];
                        targetDTO.SelectedUnionChildren = listTarget.Items;
                    }
                    else
                    {
                        targetChildInstance.FieldName = "";
                        //行内宿主（含带自身选择器的自宿主联合）把值编辑器放进行内区，其余自宿主联合才落子级行
                        if (!isUnionSelfHost || hasInlineSelector)
                        {
                            //落点宿主不是 Composite（联合直接挂在别的带行内区的容器上）时退回自身，编辑器不能丢
                            MetaTypeEditorFieldDTO hostDTO = compositeDTO ?? (targetDTO.Items is not null ? targetDTO : null);
                            if (hostDTO is not null)
                            {
                                //宿主还没有行内区时先建好：值编辑器要挂进去，不能对着 null 集合 Add
                                hostDTO.Items ??= [];
                                hostDTO.Items.Add(targetChildInstance);
                            }
                        }
                    }
                    unionDTO.SelectedUnionChildren = [];
                    //列表分支的子行是列表元素集合本身，留给后面的列表处理替换，这里不动它
                    if (targetChildInstance.TypeKind is not MetaTypeKind.List && compositeDTO is not null)
                    {
                        compositeDTO.SelectedUnionChildren = [];
                    }
                    //自宿主联合没有行内选择器时：清空之后再挂值编辑器，避免刚挂上就被清掉
                    if (isUnionSelfHost && !hasInlineSelector)
                    {
                        targetChildInstance.Parent = compositeDTO;
                        compositeDTO.SelectedUnionChildren ??= [];
                        compositeDTO.SelectedUnionChildren.Add(targetChildInstance);
                    }
                    //宿主的成员外壳与实际渲染的子行保持一致：值分支没有子行，旧的成员模板必须清掉
                    if (compositeDTO?.TypeKind is MetaTypeKind.Composite)
                    {
                        compositeDTO.Children = [.. (compositeDTO.SelectedUnionChildren ?? [])];
                        AttachParentLinks(compositeDTO);
                    }
                    #endregion
                }
            }
            if (unionDTO.Value is ObservableCollection<MetaTypeEditorFieldDTO> && !ReferenceEquals(unionDTO, targetDTO))
            {
                unionDTO.Children = [.. (unionDTO.SelectedUnionChildren ?? [])];
                AttachParentLinks(unionDTO);
            }
            //落点收尾：行里如果有"可选结构懒加载桩"（真子级缓存在 Value、自身只剩 Any 占位），
            //现在就地展开——否则初始渲染与切换后都只能看到一个空桩（如 CustomDataMap 的动态键丢失）
            if (targetDTO.SelectedUnionChildren is { Count: > 0 } landedRows)
            {
                foreach (MetaTypeEditorFieldDTO landedRow in landedRows.ToList())
                {
                    TryExpandLazyStub(landedRow, version);
                }
            }

            //落点会用新实例替换索引字段：构建期挂的订阅落在分支模板上，用户改的却是落点产出的新实例。
            //这里沿落点宿主往下找一层——真正持有"索引字段 + 依赖它的调度器"的结构通常是宿主的直接子级
            //（条目 Composite 里包着的那个 Struct），不能只靠 Parent 上溯，那条链在落点后并不可靠。
            MetaTypeEditorFieldDTO bindingCursor = targetDTO;
            for (int level = 0; bindingCursor is not null && level < 3; level++, bindingCursor = bindingCursor.Parent)
            {
                if (bindingCursor.TypeKind is MetaTypeKind.Struct)
                {
                    BindSiblingIndexDispatchers(bindingCursor, version);
                }
                foreach (MetaTypeEditorFieldDTO subStruct in (bindingCursor.Children ?? []).ToList())
                {
                    if (subStruct.TypeKind is MetaTypeKind.Struct)
                    {
                        BindSiblingIndexDispatchers(subStruct, version);
                    }
                }
            }
        }

        /// <summary>
        /// 枚举成员更新事件
        /// </summary>
        /// <summary>
        /// 按文档树找到某个节点的宿主：用于"节点没有父子链"时（懒加载桩/模板拷贝/共享键集合产出的节点）
        /// 恢复同级关系，让同级字段驱动的调度器重解析仍能工作。
        /// </summary>
        public MetaTypeEditorFieldDTO FindHostNode(MetaTypeEditorFieldDTO target)
        {
            if (target is null || resource?.DocumentItemMap is null)
            {
                return null;
            }
            HashSet<MetaTypeEditorFieldDTO> visited = [];
            foreach (MetaTypeEditorFieldDTO root in resource.DocumentItemMap.Values)
            {
                MetaTypeEditorFieldDTO hit = Walk(root, 0);
                if (hit is not null)
                {
                    return hit;
                }
            }
            return null;
            MetaTypeEditorFieldDTO Walk(MetaTypeEditorFieldDTO node, int depth)
            {
                if (node is null || depth > 24 || !visited.Add(node))
                {
                    return null;
                }
                if ((node.Children?.Contains(target) ?? false) || (node.Items?.Contains(target) ?? false)
                    || (node.SelectedUnionChildren?.Contains(target) ?? false))
                {
                    return node;
                }
                foreach (MetaTypeEditorFieldDTO child in node.Children ?? [])
                {
                    MetaTypeEditorFieldDTO hit = Walk(child, depth + 1);
                    if (hit is not null)
                    {
                        return hit;
                    }
                }
                foreach (MetaTypeEditorFieldDTO child in node.Items ?? [])
                {
                    MetaTypeEditorFieldDTO hit = Walk(child, depth + 1);
                    if (hit is not null)
                    {
                        return hit;
                    }
                }
                foreach (MetaTypeEditorFieldDTO child in node.SelectedUnionChildren ?? [])
                {
                    MetaTypeEditorFieldDTO hit = Walk(child, depth + 1);
                    if (hit is not null)
                    {
                        return hit;
                    }
                }
                if (node.Value is ObservableCollection<MetaTypeEditorFieldDTO> stashedChildren)
                {
                    foreach (MetaTypeEditorFieldDTO child in stashedChildren)
                    {
                        MetaTypeEditorFieldDTO hit = Walk(child, depth + 1);
                        if (hit is not null)
                        {
                            return hit;
                        }
                    }
                }
                return null;
            }
        }

        public void SelectedEnumItemUpdated(MetaTypeEditorFieldDTO enumDTO, string version)
        {
            #region 更新同层的所有动态调度器
            #region Field
            MetaTypeEditorFieldDTO parentDTO = enumDTO.Parent;
            if (parentDTO is null)
            {
                //有些路径（懒加载桩/模板拷贝/共享键集合）产出的节点没有父子链，
                //此时就地按文档树找到它的宿主并补链，否则同级字段驱动的调度器永远不会重新解析
                parentDTO = FindHostNode(enumDTO);
                if (parentDTO is not null)
                {
                    enumDTO.Parent = parentDTO;
                }
            }
            //索引名取"本枚举所在字段"的名字：枚举常常只是联合的一个分支（trigger 的枚举挂在 trigger 联合下），
            //自身没有字段名，只按 enumDTO.FieldName 比对会一个调度器都匹配不上
            string indexFieldName = enumDTO.FieldName;
            MetaTypeEditorFieldDTO nameCursor = enumDTO.Parent;
            for (int level = 0; string.IsNullOrEmpty(indexFieldName) && nameCursor is not null && level < 6; level++)
            {
                indexFieldName = nameCursor.FieldName;
                nameCursor = nameCursor.Parent;
            }
            if (parentDTO is null || string.IsNullOrEmpty(indexFieldName))
            {
                return;
            }
            if (parentDTO.Parent?.TypeKind is MetaTypeKind.Composite)
            {
                parentDTO = parentDTO.Parent;
            }
            //落点后宿主的同级内容未必在 Children 上：Composite 把内容放在 Items/SelectedUnionChildren 里，
            //而 Children 常被留成空集合（不是 null）。所以三份集合都要扫，取真正挂着调度器的那一份，
            //否则表现就是"改了枚举但依赖它的调度器不更新"
            //（进度条件里的 trigger 被联合壳包着，落点后 trigger 与 ...minecraft:trigger[[trigger]] 都在宿主的选中表里）
            ObservableCollection<MetaTypeEditorFieldDTO> siblingCollection = null;
            foreach (ObservableCollection<MetaTypeEditorFieldDTO> candidate in new[]
                     {
                         parentDTO.Children,
                         parentDTO.SelectedUnionChildren,
                         parentDTO.Items
                     })
            {
                if (candidate is null)
                {
                    continue;
                }
                if (candidate.Any(item => item is not null && item != enumDTO
                        && item.TypeKind is MetaTypeKind.Dispatch
                        && item.FeatureMap is not null
                        && item.FeatureMap.TryGetValue("Index", out MetaValue candidateIndex)
                        && candidateIndex?.LiteralValue?.ToString() == indexFieldName))
                {
                    siblingCollection = candidate;
                    break;
                }
            }
            if (siblingCollection is null)
            {
                return;
            }
            int removeIndex = -1;
            List<int> dynamicDispatchIndexList = [];
            #endregion
            #region 搜索动态调度器的同时删除遗留的调度器解释后出现的节点
            for (int i = 0; i < siblingCollection.Count; i++)
            {
                //筛选出符合条件的调度器：具名字段（如 argument: xxx[[modifier]]）同样要收进来，
                //否则改了同级枚举也不会重新解析；是否为"本枚举驱动的调度器"由下面的 Index 比对决定
                if (siblingCollection[i] != enumDTO && siblingCollection[i].TypeKind is MetaTypeKind.Dispatch
                    && siblingCollection[i].FeatureMap.TryGetValue("Index", out MetaValue metaValue) && metaValue.LiteralValue?.ToString() == indexFieldName)
                {
                    removeIndex = i;
                    dynamicDispatchIndexList.Add(i);
                }
                //删除调度器之后带标记的节点
                if (removeIndex > -1 && i > removeIndex && siblingCollection[i].IsInterpretFromDispatch)
                {
                    siblingCollection.RemoveAt(i);
                    i--;
                }
            }
            #endregion
            #region 处理调度器
            string selectedEnumValue = enumDTO.SelectedEnumOption.Value?.LiteralValue?.ToString() ?? "";
            string fieldName = indexFieldName;
            int currentInsertIndex;
            for (int i = 0; i < dynamicDispatchIndexList.Count; i++)
            {
                MetaTypeEditorFieldDTO currentDTO = siblingCollection[dynamicDispatchIndexList[i]];
                currentInsertIndex = siblingCollection.IndexOf(currentDTO) + 1;
                //Index只有一个与当前枚举节点字段名相同的值，则识别为当前层的动态调度器
                if (currentDTO.FeatureMap.TryGetValue("Index", out MetaValue indexValue) &&
                    indexValue.Kind is MetaValueKind.Literal && indexValue.LiteralValue is not null &&
                    indexValue.LiteralValue.ToString() == fieldName)
                {
                    //筛选出当前调度器节点使用枚举值后所对应的池中的调度器资源
                    if (currentDTO.FeatureMap.TryGetValue("Resource", out MetaValue resourceValue) && resourceValue.LiteralValue is not null)
                    {
                        #region 过滤出Resource符合要求的调度器集合
                        string resourceString = resourceValue.LiteralValue.ToString();
                        var dispatchDTOEnumerable = resource.DocumentItemMap.Where(item => item.Value.TypeKind is MetaTypeKind.Dispatch || item.Value.OriginKind is MetaTypeKind.Dispatch);
                        List<MetaTypeEditorFieldDTO> targetDispatchList = [..dispatchDTOEnumerable.Where(item=> item.Value.FeatureMap is not null && item.Value.FeatureMap.TryGetValue("Resource",out MetaValue currentResourceValue) &&
                        currentResourceValue?.LiteralValue is not null && currentResourceValue.LiteralValue.ToString() == resourceString).Select(item=>item.Value)];
                        MetaTypeEditorFieldDTO targetDispatchDTO = null;
                        #endregion
                        #region 从过滤出来的调度器中根据当前选中的枚举值精准搜索指定调度器
                        for (int j = 0; j < targetDispatchList.Count; j++)
                        {
                            if (targetDispatchList[j].FeatureMap.TryGetValue("Index", out MetaValue currentIndexValue))
                            {
                                if (currentIndexValue.Kind is MetaValueKind.Literal && currentIndexValue.LiteralValue?.ToString() == selectedEnumValue)
                                {
                                    targetDispatchDTO = targetDispatchList[j];
                                    break;
                                }
                                else if (currentIndexValue.Kind is MetaValueKind.List)
                                {
                                    bool haveTargetValue = currentIndexValue.Items.Any(item => item.LiteralValue?.ToString() == selectedEnumValue);
                                    if (haveTargetValue)
                                    {
                                        targetDispatchDTO = targetDispatchList[j];
                                        break;
                                    }
                                }
                            }
                        }
                        #endregion
                        #region 将筛选出的目标调度器传入调度器的构造器并添加回当前父级
                        //选中值没有对应调度器时按 mcdoc 规则落到兜底调度器：
                        //%none 管未设置位，%unknown 管未知索引（如 float 修饰器的 add/subtract/multiply 都归到 to float）
                        if (targetDispatchDTO is null)
                        {
                            string fallbackDispatchIndex = selectedEnumValue is "%none" or "unset" ? "%none" : "%unknown";
                            targetDispatchDTO = FindDispatchTemplate(resource, resourceString, fallbackDispatchIndex);
                        }
                        if (targetDispatchDTO is null)
                        {
                            return;
                        }
                        var targetDispatchDTOInstance = InstantiateDTO(targetDispatchDTO, version, parentDTO);
                        var registry = DocumentDTOBuildStrategyRegistry.Create(resource, this);
                        List<MetaTypeEditorFieldDTO> resultList = [];
                        //公共执行一次泛型提取
                        if (!string.IsNullOrEmpty(targetDispatchDTOInstance.TypeName) && targetDispatchDTO.Children?.Count > 0)
                        {
                            resultList = SubstituteGenericIterative(targetDispatchDTOInstance, version);
                        }
                        for (int j = 0; j < resultList.Count; j++)
                        {
                            resultList[j].IsInterpretFromDispatch = true;
                            if (resultList[j].IsRequired)
                            {
                                resultList[j].TypeKind = MetaTypeKind.Struct;
                                var childBuilder = registry.Get(resultList[j].TypeKind);
                                //结构体先浅层交出
                                childBuilder.Build(resultList[j], resultList[j], version, resultList[j].Path ?? enumDTO.Path, [], resultList[j].TypeKind is MetaTypeKind.Struct ? RenderDepth.Shallow : RenderDepth.Deep);
                            }
                            else
                            {
                                resultList[j].Value = new ObservableCollection<MetaTypeEditorFieldDTO>(resultList[j].Children);
                                resultList[j].OriginKind = MetaTypeKind.Dispatch;
                                resultList[j].Children =
                                [
                                    new MetaTypeEditorFieldDTO()
                                        {

                            ID = "placeHolder",
                                            TypeKind = MetaTypeKind.Any
                                        }
                                ];
                            }
                        }
                        //若当前没有泛型参数列表则执行普通解析
                        if (resultList?.Count == 0)
                        {
                            for (int j = 0; j < targetDispatchDTO.Children?.Count; j++)
                            {
                                if (targetDispatchDTO.Children[j].IsRequired)
                                {
                                    var instance = InstantiateDTO(targetDispatchDTO.Children[j], version, parentDTO);
                                    instance.IsInterpretFromDispatch = true;
                                    var childBuilder = registry.Get(instance.TypeKind);
                                    childBuilder.Build(instance, targetDispatchDTO.Children[j], version, targetDispatchDTO.Children[j].Path ?? enumDTO.Path, [], targetDispatchDTO.Children[j].TypeKind is MetaTypeKind.Struct ? RenderDepth.Shallow : RenderDepth.Deep);
                                    resultList.Add(instance);

                                }
                            }
                        }
                        //模板本身是泛型应用或具名定义（如 to Conditions<Location>）时没有可直接实例化的子级，
                        //手工实例化只会得到空列表，表现为"选了 trigger 却什么都没出来"。
                        //这类目标交给统一的解析路径就地物化：与同级索引重解析（ResolveAccessorIndex）同一套逻辑
                        if (resultList.Count == 0)
                        {
                            currentDTO.FeatureMap["Index"] = new MetaValue { Kind = MetaValueKind.Literal, LiteralValue = selectedEnumValue };
                            MCDocumentResourceBuilder.BuildResource(currentDTO, currentDTO, version, currentDTO.Path ?? enumDTO.Path, resource, this);
                            //就地物化会换掉节点类型：宿主集合要重建这一项，视图才会按新类型重新选模板
                            int inPlaceIndex = siblingCollection.IndexOf(currentDTO);
                            if (inPlaceIndex > -1)
                            {
                                siblingCollection.RemoveAt(inPlaceIndex);
                                siblingCollection.Insert(inPlaceIndex, currentDTO);
                            }
                            continue;
                        }
                        //剥壳
                        for (int j = 0; j < resultList.Count; j++)
                        {
                            HierarchicallyUpdateTreeStructuredData(resultList[j], version);
                            if (resultList[j].OriginKind is MetaTypeKind.Dispatch && resultList[j].Value is IEnumerable<MetaTypeEditorFieldDTO>)
                            {
                                resultList[j].IsVisible = true;
                            }
                            //插回原来那份集合：落点后的宿主可能没有 Children，插到别处等于没插
                            siblingCollection.Insert(currentInsertIndex, resultList[j]);
                            currentInsertIndex++;
                        }
                        #endregion
                    }
                }
            }
            #endregion
            #endregion

            //枚举的 Parent 未必是持有"索引字段 + 依赖它的调度器"的那个结构（trigger 被联合壳包着，
            //落点后父链也不可靠），绑定时已把索引字段的父链修正到宿主结构，这里沿它上溯。
            MetaTypeEditorFieldDTO indexHostCursor = parentDTO;
            for (int level = 0; indexHostCursor is not null && level < 4; level++, indexHostCursor = indexHostCursor.Parent)
            {
                if (indexHostCursor.TypeKind is MetaTypeKind.Struct)
                {
                    BindSiblingIndexDispatchers(indexHostCursor, version);
                    break;
                }
            }

            #region 更新代码编辑器
            #endregion
        }

        /// <summary>
        /// 给目标列表添加元素
        /// </summary>
        /// <param name="currentDTO"></param>
        /// <param name="version"></param>
        private void ExecuteAddListItem(MetaTypeEditorFieldDTO currentDTO, string version)
        {
            #region 判断当前节点为简单列表还是复合列表
            MetaTypeEditorFieldDTO listItemDTO = currentDTO.Items?.FirstOrDefault(item => item.TypeKind is MetaTypeKind.List);
            MetaTypeEditorFieldDTO unionItemDTO = currentDTO.Items?.FirstOrDefault(item => item.TypeKind is MetaTypeKind.Union);
            bool isListItem = listItemDTO is not null || (unionItemDTO is not null && unionItemDTO?.SelectedUnionTypeName?.Name == "List");
            isListItem |= currentDTO.TypeKind is MetaTypeKind.List;
            if (!isListItem || currentDTO.ElementType is null)
            {
                return;
            }
            currentDTO.ElementType.Parent = null;
            var elementItem = InstantiateDTO(currentDTO.ElementType, version);
            #endregion
            #region 处理泛引用类列表元素
            if (IsIndirectType(elementItem.TypeKind))
            {
                elementItem.SetRequired(true);
                currentDTO.ElementType.SetRequired(true);
                DTOInstanceContext context = new([elementItem], []);
                Validator.Verify(context, [currentDTO.ElementType], version, listItemDTO?.Path ?? currentDTO.Path);
                // 对当前节点执行展平/提升
                elementItem = context.dtoInstanceList[0];
                HierarchicallyUpdateTreeStructuredData(elementItem, version);
                if (elementItem.TypeKind is MetaTypeKind.Composite)
                {
                    MetaTypeEditorFieldDTO subContainerItem = elementItem.Items.FirstOrDefault(item => IsContainerType(item.TypeKind) || IsIndirectType(item.TypeKind));
                    if (subContainerItem is not null)
                    {
                        subContainerItem.FieldName = "Entry";
                    }
                }
                elementItem.IsVisible = true;
            }
            #endregion
            #region 生成元素的容器
            elementItem.FieldName = "Entry";
            MetaTypeEditorFieldDTO entryDTO = new() { ID = Guid.NewGuid().ToString() };
            if (elementItem.TypeKind is not MetaTypeKind.Composite)
            {
                entryDTO = GetEntryItem(elementItem, version);
            }
            else
            {
                entryDTO = elementItem;
            }
            if (entryDTO.Items?.Count > 0 && entryDTO.Items[0].TypeKind is not MetaTypeKind.Remove)
            {
                MetaTypeEditorFieldDTO removeDTO = new()
                {
                    ID = "placeHoloder",
                    TypeKind = MetaTypeKind.Remove,
                    RemoveItemCommand = CreateRemoveListItemCommand(currentDTO, entryDTO)
                };
                entryDTO.Items.Insert(0, removeDTO);
            }
            #endregion
            #region 根据元素特征与当前列表特征进行合适的添加操作
            if (entryDTO.TypeKind is MetaTypeKind.Composite)
            {
                if (currentDTO.TypeKind is MetaTypeKind.List)
                {
                    currentDTO.Items ??= [];
                    currentDTO.Items.Add(entryDTO);
                }
                else
                {
                    currentDTO.SelectedUnionChildren ??= [];
                    currentDTO.SelectedUnionChildren.Add(entryDTO);
                }
                entryDTO.Parent = currentDTO;
                var subRemoveDTO = entryDTO.Items.FirstOrDefault(item => item.TypeKind is MetaTypeKind.Remove);
                var subListDTO = entryDTO.Items.FirstOrDefault(item => item.TypeKind is MetaTypeKind.List);
                if (subRemoveDTO is not null)
                {
                    subRemoveDTO.RemoveItemCommand = CreateRemoveCompositeOrCompoundItemCommand(entryDTO.Parent, entryDTO);
                }
                if (subListDTO is not null)
                {
                    subListDTO.AddItemCommand = CreateAddListItemCommand(entryDTO, version);
                    subListDTO.RemoveItemCommand = CreateClearListItemCommand(entryDTO);
                }
            }
            else if (currentDTO.Parent is not null)
            {
                currentDTO.Parent.SelectedUnionChildren ??= [];
                currentDTO.Parent.SelectedUnionChildren.Add(entryDTO);
                entryDTO.Parent = currentDTO.Parent;
                entryDTO.RemoveItemCommand = CreateRemoveListItemCommand(entryDTO.Parent, entryDTO);
            }
            #endregion
        }

        /// <summary>
        /// 为复合节点执行添加
        /// </summary>
        /// <param name="currentDTO"></param>
        /// <param name="version"></param>
        private void ExecuteAddCompositeOrCompoundItem(MetaTypeEditorFieldDTO currentDTO, string version)
        {
            if (currentDTO.TypeKind is not MetaTypeKind.Composite)
            {
                return;
            }
            #region 处理调度器
            var resultDTO = GetDispatchResource(currentDTO, version);
            if (resultDTO is not null)
            {
                if (currentDTO.TypeKind is MetaTypeKind.Composite)
                {
                    var subUnionEnumDTO = currentDTO.Items.FirstOrDefault(item => item.TypeKind is MetaTypeKind.Union or MetaTypeKind.Enum);
                    if (subUnionEnumDTO is not null)
                    {
                        currentDTO.Parent?.Children.Add(resultDTO);
                    }
                    else
                    {
                        currentDTO.SelectedUnionChildren ??= [];
                        currentDTO.SelectedUnionChildren.Add(resultDTO);
                    }
                }
                else
                {
                    currentDTO.Children.Add(resultDTO);

                }

            }
            #endregion
            #region 处理空调度器结果
            else if ((currentDTO.Children is null || currentDTO.Children.Count == 0) && currentDTO.Parent is not null)
            {
                currentDTO.Parent.Children ??= [];
                if (currentDTO.TypeKind is MetaTypeKind.Composite)
                {
                    var enumDTO = currentDTO.Items?.FirstOrDefault(item => item.TypeKind is MetaTypeKind.Enum);
                    if (enumDTO?.SelectedEnumOption is not null)
                    {
                        //需要添加可删空节点
                        MetaTypeEditorFieldDTO subCompositeDTO = new()
                        {
                            ID = Guid.NewGuid().ToString(),
                            TypeKind = MetaTypeKind.Composite,
                            FieldName = enumDTO?.SelectedEnumOption.Value.LiteralValue?.ToString() ?? enumDTO?.SelectedEnumOption.Name,
                            Items = []
                        };
                        MetaTypeEditorFieldDTO subEmptySturctDTO = new()
                        {
                            ID = Guid.NewGuid().ToString(),
                            TypeKind = MetaTypeKind.Struct,
                            FieldName = enumDTO?.SelectedEnumOption.Value.LiteralValue?.ToString() ?? enumDTO?.SelectedEnumOption.Name,
                            Items = []
                        };
                        MetaTypeEditorFieldDTO removeDTO = new()
                        {
                            ID = Guid.NewGuid().ToString(),
                            TypeKind = MetaTypeKind.Remove,
                            FieldName = enumDTO?.SelectedEnumOption.Value.LiteralValue?.ToString() ?? enumDTO?.SelectedEnumOption.Name,
                            RemoveItemCommand = CreateRemoveCompositeOrCompoundItemCommand(currentDTO.Parent, subCompositeDTO)
                        };
                        subCompositeDTO.Items.Add(removeDTO);
                        subCompositeDTO.Items.Add(subEmptySturctDTO);
                        currentDTO.Parent.Children.Add(subCompositeDTO);
                    }
                }
            }
            #endregion
        }

        private static void ExecuteClearListItem(MetaTypeEditorFieldDTO listDTO)
        {
            if (listDTO.TypeKind is MetaTypeKind.Composite)
            {
                listDTO.SelectedUnionChildren?.Clear();
            }
            else
            {
                listDTO.Items?.Clear();
            }
        }

        private static void ExecuteRemoveListItem(MetaTypeEditorFieldDTO currentDTO, MetaTypeEditorFieldDTO item = null)
        {
            if (item is null)
            {
                return;
            }
            if (currentDTO.TypeKind is MetaTypeKind.List)
            {
                currentDTO?.Items?.Clear();
            }
            else if (currentDTO.TypeKind is MetaTypeKind.Composite)
            {
                currentDTO.SelectedUnionChildren.Remove(item);
            }
            else if (item is not null)
            {
                currentDTO.SelectedUnionChildren.Remove(item);
            }
        }

        /// <summary>
        /// 删除复合节点/Compound节点中的指定子节点
        /// </summary>
        /// <param name="currentDTO"></param>
        /// <param name="item"></param>
        private static void ExecuteRemoveCompositeOrCompoundOrListItem(MetaTypeEditorFieldDTO currentDTO, MetaTypeEditorFieldDTO item = null)
        {
            if (currentDTO is null)
            {
                return;
            }
            if (currentDTO.TypeKind is MetaTypeKind.Composite)
            {
                currentDTO.SelectedUnionChildren?.Remove(item);
            }
            else if (currentDTO.TypeKind is MetaTypeKind.List)
            {
                currentDTO.Items?.Remove(item);
            }
            else
            {
                currentDTO.Children?.Remove(item);
            }
        }

        private static void ExecuteReFreshItem(MetaTypeEditorFieldDTO currentDTO, string version)
        {
            string newGuidString = Guid.NewGuid().ToString();
            int index = Random.Shared.Next(0, 3);
            string[] newGuidArray = newGuidString.Split('-');
            currentDTO.Value = newGuidArray[index];
        }

        /// <summary>
        /// 实例化DTO模板
        /// </summary>
        /// <param name="template">模板</param>
        /// <param name="parent">父级</param>
        /// <param name="visited">祖先链追踪（仅在当前递归路径上检测循环引用）</param>
        /// <returns></returns>
        public MetaTypeEditorFieldDTO InstantiateDTO(
            MetaTypeEditorFieldDTO template,
            string version,
            MetaTypeEditorFieldDTO parent = null,
            HashSet<MetaTypeEditorFieldDTO> visited = null)
        {
            if (template is null)
            {
                return null;
            }
            //初始化当前DTO实例的ID
            string id = Guid.NewGuid().ToString();
            //初始化祖先链集合（仅在顶层调用时创建）
            visited ??= [];
            if (visited.Count > 400)
            {
                //过深时按懒加载桩返回，静默打断间接递归
                return new MetaTypeEditorFieldDTO
                {
                    ID = "placeHolder",
                    Path = template.Path,
                    TemplateReference = template,
                    Parent = parent,
                    FieldName = template.FieldName,
                    TypeKind = MetaTypeKind.Any
                };
            }
            #region 循环引用检测：模板已出现在当前递归路径中 → 返回桩节点打断环
            if (!visited.Add(template))
            {
                //已出现在祖先链中（循环引用），返回桩节点打断环。
                MetaTypeEditorFieldDTO result = new()
                {
                    ID = id,
                    Path = template.Path,
                    TemplateReference = template,
                    Parent = parent,
                    FieldName = template.FieldName,
                    EnumOptionList = template.EnumOptionList,
                    TypeKind = template.TypeKind,
                    OriginKind = template.OriginKind,
                    TypeName = template.TypeName,
                    TypeParameterNameList = template.TypeParameterNameList,
                    //实参列表按实例复制，替换时的写回不能污染模板
                    ActualTypeArguments = template.ActualTypeArguments is null ? null : [.. template.ActualTypeArguments],
                    BindingScope = template.BindingScope,
                    Watermark = template.Watermark,
                    Value = template.GetDefaultValue(),
                    FeatureMap = new(template.FeatureMap)
                };
                result.SetRequired(template.IsRequired);
                //循环引用：不展开子级，由TemplateReference懒加载
                if (template.EnumOptionList is not null)
                {
                    result.EnumOptionList = [.. template.EnumOptionList];
                }
                return result;
            }
            #endregion
            #region 实例化并设置默认值
            object defaultValue = template.GetDefaultValue();
            var dto = new MetaTypeEditorFieldDTO
            {
                ID = id,
                Path = template.Path,
                Parent = parent,
                //模板本身还是懒加载桩时，穿透到真正带子级的一层
                TemplateReference = template.Children is not null && template.Children.Count == 1 && IsPlaceHolderNode(template.Children[0]) && template.TemplateReference is not null
                    ? template.TemplateReference
                    : template,
                FeatureMap = new(template.FeatureMap),
                TypeName = template.TypeName,
                EnumOptionList = template.EnumOptionList,
                TypeParameterNameList = template.TypeParameterNameList,
                //实参列表按实例复制，替换时的写回不能污染模板
                ActualTypeArguments = template.ActualTypeArguments is null ? null : [.. template.ActualTypeArguments],
                BindingScope = template.BindingScope,
                FieldName = template.FieldName,
                TypeKind = template.TypeKind,
                OriginKind = template.OriginKind,
                Watermark = template.Watermark,
                Value = defaultValue,
                Min = template.Min,
                Max = template.Max,
            };
            dto.SetRequired(template.IsRequired);
            if (template.EnumOptionList is not null)
            {
                dto.EnumOptionList = [.. template.EnumOptionList];
            }
            #endregion
            #region 处理容器子节点
            switch (template.TypeKind)
            {
                case MetaTypeKind.Struct:
                case MetaTypeKind.Composite:
                case MetaTypeKind.Dispatch:
                case MetaTypeKind.Tuple:

                    {
                        if (template.Children is not null)
                        {
                            dto.Children = new(template.Children.Select(child => InstantiateDTO(child, version, dto, visited)));

                        }
                        //Composite 是行内区宿主，选择器/值行也随实例带一份
                        if (template.TypeKind is MetaTypeKind.Composite && template.Items is not null)

                        {
                            dto.Items = new(template.Items.Select(item => InstantiateDTO(item, version, dto, visited)));

                        }
                        //Composite 没有对应构建器，行内区的切换事件在这里接上
                        if (template.TypeKind is MetaTypeKind.Composite)
                        {
                            dto.SelectedUnionItemUpdated = () => SelectedUnionItemUpdated(dto, version);

                        }
                        // 如果调度器的目标类型是联合体，需要把联合体的选项信息也带过来
                        if (template.UnionTypeNameList?.Count > 0)

                        {
                            dto.UnionTypeNameList = [.. template.UnionTypeNameList];
                            dto.SelectedUnionTypeName = dto.UnionTypeNameList[0];
                            //停在“未设置”的可选联合不能拼入任何成员：拷贝路径同样是初始化的一部分
                            if (dto.Children?.Count > 0 && !IsUnsetUnionSelection(dto))

                            {
                                //落点按成员形态判定：颜色值成员把编辑器交给宿主 Composite 的行内区（与切换同形），其余成员挂自身或子级
                                MetaTypeEditorFieldDTO firstMember = dto.Children[0];
                                bool isColorValueMember = firstMember.ElementType is null
                                    && firstMember.TypeKind is MetaTypeKind.NamedColor or MetaTypeKind.String or MetaTypeKind.Int
                                        or MetaTypeKind.Float or MetaTypeKind.Boolean or MetaTypeKind.Enum;
                                MetaTypeEditorFieldDTO hostComposite = dto.Parent?.TypeKind is MetaTypeKind.Composite ? dto.Parent : null;
                                if (isColorValueMember && hostComposite is not null)

                                {
                                    firstMember.FieldName = "";
                                    hostComposite.Items ??= [];
                                    hostComposite.Items.Add(firstMember);
                                    dto.SelectedUnionChildren = [];
                                }
                                else if (isColorValueMember || firstMember.Children is null)

                                {
                                    dto.SelectedUnionChildren = [firstMember];
                                }
                                else

                                {
                                    dto.SelectedUnionChildren = [.. firstMember.Children];
                                }
                            }
                            else
                            {
                                dto.SelectedUnionChildren = [.. dto.Children];
                            }
                        }
                        if (template.EnumOptionList?.Count > 0)
                        {
                            dto.EnumOptionList = [.. template.EnumOptionList];
                            dto.SelectedEnumOption = dto.EnumOptionList[0];
                        }
                        if (template.ElementType is not null)
                        {
                            dto.ElementType = template.ElementType;
                        }
                        if (template.TypeKind is MetaTypeKind.Dispatch && template.UnionTypeNameList?.Count > 0)
                        {
                            dto.TypeKind = MetaTypeKind.Union;
                            dto.OriginKind = MetaTypeKind.Dispatch;
                            //按联合使用的调度器节点同样要能响应成员切换：初始选中项与切换事件缺一不可
                            dto.SelectedUnionItemUpdated = null;
                            if (dto.SelectedUnionTypeName is null)
                            {
                                dto.SelectedUnionTypeName = dto.UnionTypeNameList[0];
                            }
                            dto.SelectedUnionItemUpdated = () => SelectedUnionItemUpdated(dto, version);
                        }
                        break;
                    }
                case MetaTypeKind.Union:
                    {
                        dto.SelectedUnionChildren ??= [];
                        if (template.UnionTypeNameList?.Count > 0)
                        {
                            dto.UnionTypeNameList = [.. template.UnionTypeNameList];
                            //选择项随实例带上：否则界面第一次切换会被 setter 当成初始化而不触发落地
                            dto.SelectedUnionItemUpdated = null;
                            dto.SelectedUnionTypeName = dto.UnionTypeNameList.FirstOrDefault(item => item.Name == template.SelectedUnionTypeName?.Name)
                                ?? dto.UnionTypeNameList[0];
                        }
                        //拷贝出来的联合没有构建器再跑，切换事件在这里接上（行内选择器就靠它落地）；
                        //名称列表可能是后面重建的，所以事件与初始选中项无条件保证
                        if (dto.SelectedUnionTypeName is null && dto.UnionTypeNameList?.Count > 0)
                        {
                            dto.SelectedUnionItemUpdated = null;
                            dto.SelectedUnionTypeName = dto.UnionTypeNameList[0];
                        }
                        dto.SelectedUnionItemUpdated = () => SelectedUnionItemUpdated(dto, version);
                        if (template.Children is not null)
                        {
                            dto.Children = dto.Children = new(template.Children.Select(child => InstantiateDTO(child, version, dto, visited)));
                            //拷贝路径不经过 UnionDTOBuilder：版本过滤与成员裁剪要在这里补做，
                            //否则（如新加的 List 元素）会把版本外的成员一起带出来
                            VerifyVersion([.. dto.Children], version);
                            int prunedCount = 0;
                            for (int k = dto.Children.Count - 1; k >= 0; k--)
                            {
                                if (!dto.Children[k].IsVisible && !IsPlaceHolderNode(dto.Children[k]))
                                {
                                    dto.Children.RemoveAt(k);
                                    prunedCount++;
                                }
                            }
                            //只有真的裁掉了成员才重建名称表：模板自带的名称比类型兜底名准确，不能无条件覆盖
                            if (dto.UnionTypeNameList is not null && prunedCount > 0)
                            {
                                dto.UnionTypeNameList.Clear();
                                if (!dto.IsRequired)
                                {
                                    dto.UnionTypeNameList.Add(new EnumMember() { Name = "- unset -", Value = new MetaValue() { Kind = MetaValueKind.Literal, LiteralValue = "unset" } });
                                }
                                foreach (string name in UnionTypeNameParser.Parse([.. dto.Children.Where(child => child.IsVisible)]))
                                {
                                    dto.UnionTypeNameList.Add(new EnumMember() { Name = name, Value = new MetaValue() { Kind = MetaValueKind.Literal, LiteralValue = name } });

                                }
                                //重建后重新选默认项：先摘掉事件，避免在半成品节点上触发落点
                                dto.SelectedUnionItemUpdated = null;
                                dto.SelectedUnionTypeName = dto.UnionTypeNameList.FirstOrDefault(item => item.Name == template.SelectedUnionTypeName?.Name)
                                    ?? dto.UnionTypeNameList.FirstOrDefault();
                                dto.SelectedUnionItemUpdated = () => SelectedUnionItemUpdated(dto, version);

                            }
                            //停在“未设置”的可选联合不能拼入任何成员：拷贝路径同样是初始化的一部分
                            if (dto.Children?.Count > 0 && !IsUnsetUnionSelection(dto))

                            {
                                //落点按成员形态判定：颜色值成员把编辑器交给宿主 Composite 的行内区（与切换同形），其余成员挂自身或子级
                                MetaTypeEditorFieldDTO firstMember = dto.Children[0];
                                bool isColorValueMember = firstMember.ElementType is null
                                    && firstMember.TypeKind is MetaTypeKind.NamedColor or MetaTypeKind.String or MetaTypeKind.Int
                                        or MetaTypeKind.Float or MetaTypeKind.Boolean or MetaTypeKind.Enum;
                                MetaTypeEditorFieldDTO hostComposite = dto.Parent?.TypeKind is MetaTypeKind.Composite ? dto.Parent : null;
                                if (isColorValueMember && hostComposite is not null)
                                {
                                    firstMember.FieldName = "";
                                    hostComposite.Items ??= [];
                                    hostComposite.Items.Add(firstMember);
                                    dto.SelectedUnionChildren = [];

                                }
                                else if (isColorValueMember || firstMember.Children is null)

                                {
                                    dto.SelectedUnionChildren = [firstMember];

                                }
                                else

                                {
                                    dto.SelectedUnionChildren = [.. firstMember.Children];
                                }
                            }
                        }
                        break;
                    }
                case MetaTypeKind.ByteArray:
                case MetaTypeKind.IntArray:
                case MetaTypeKind.LongArray:
                case MetaTypeKind.List:
                    {
                        //数组元素的实际模板由 ElementType 控制，ElementType模板在每个列表节点实例的模板节点引用中
                        dto.Items = [];
                        if (template.TypeKind is MetaTypeKind.List)
                        {
                            dto.AddItemCommand = CreateAddListItemCommand(dto, version);
                            dto.RemoveItemCommand = CreateRemoveListItemCommand(dto);
                            dto.ReFreshCommand = CreateReFreshCommand(dto, version);
                            if (template.ElementType is not null)
                            {
                                MetaTypeEditorFieldDTO elementType = InstantiateDTO(template.ElementType, version, template, visited);
                                dto.ElementType = elementType;
                            }
                        }
                        break;
                    }
            }
            #endregion
            visited.Remove(template);
            return dto;
        }

        /// <summary>
        /// 计算Key表达式的值
        /// </summary>
        /// <param name="currentDTO"></param>
        /// <param name="currentIndex"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        private static MetaTypeEditorFieldDTO EvaluateKeyExpression(MetaTypeEditorFieldDTO currentDTO, string currentResourceLocation, MetaValue currentIndex, Resource resource)
        {
            #region Field
            MetaTypeEditorFieldDTO result = null;
            // 防御：fallback dispatch 可能没有 Path，仍然需要继续解析 key 表达式。
            string documentItemPath = currentDTO.Path?.TargetPath?.ToString() ?? "";
            string indexString = currentIndex.Kind is MetaValueKind.Literal ? currentIndex.LiteralValue.ToString().TrimStart('[').TrimEnd(']') : "";
            string indexValue = "";
            if (currentIndex.Kind is MetaValueKind.List && currentIndex.Items is not null)
            {
                indexString = currentIndex.Items[0].LiteralValue?.ToString() ?? "";
            }
            #endregion
            // %unknown / %none 是 mcdoc 调度器里的特殊索引，
            // 它们本身也对应 DocumentItemMap 中真实的 dispatch 模板，命中后会正常实例化其目标类型。
            // 它们不是动态表达式，直接按 Resource + Index 精确匹配 dispatch 模板。
            if (indexString is "%unknown" or "%none")
            {
                return FindDispatchTemplate(resource, currentResourceLocation, indexString);
            }
            #region 抓取层级
            //抓取父级长引用
            if (indexString.StartsWith("%parent"))
            {
                MetaTypeEditorFieldDTO iterationDTO = currentDTO;
                string[] indexStringArray = indexString.Split('.');
                for (int i = 0; i < indexStringArray.Length; i++)
                {
                    switch (indexStringArray[i])
                    {
                        case "%parent":
                            {
                                if (iterationDTO.Parent is not null)
                                {
                                    iterationDTO = iterationDTO.Parent;
                                }
                                break;
                            }
                        case "%key":
                            {
                                object defaultValue = iterationDTO.GetDefaultValue();
                                if (defaultValue is not null)
                                {
                                    indexValue = defaultValue.ToString();
                                }
                                break;
                            }
                        default:
                            {
                                //搜索每一层的字段名称
                                if (iterationDTO.Parent?.Children is not null)
                                {
                                    for (int j = 0; j < iterationDTO.Parent.Children.Count; j++)
                                    {
                                        if (iterationDTO.Parent.Children[j].FieldName == indexStringArray[i])
                                        {
                                            object defaultValue = iterationDTO.Parent.Children[j].GetDefaultValue();
                                            if (defaultValue is not null)
                                            {
                                                indexValue = defaultValue.ToString();
                                                break;

                                            }

                                        }

                                    }
                                }
                                break;
                            }
                    }
                }
            }
            else
            {
                switch (indexString)
                {
                    //处理目标的值，调用GetDefaultValue方法自动获取
                    case "%key":
                        {
                            //动态 Key 表条目的键已经写在条目字段名上（"minecraft:" + 键）：重解释历史条目时必须用它，
                            //不能再用键选择器当前选中项，否则会把别的键的类型参数搬进来
                            for (MetaTypeEditorFieldDTO scope = currentDTO; scope is not null && string.IsNullOrEmpty(indexValue); scope = scope.Parent)
                            {
                                if (scope.IsInterpretFromDispatch && scope.FieldName?.StartsWith("minecraft:") == true)
                                {
                                    indexValue = scope.FieldName["minecraft:".Length..];
                                }
                            }
                            if (string.IsNullOrEmpty(indexValue) && currentDTO.Items is not null && currentDTO.Items.Count > 0)
                            {
                                MetaTypeEditorFieldDTO enumDTO = currentDTO.Items.FirstOrDefault(item => item.TypeKind is MetaTypeKind.Enum);
                                if (enumDTO?.SelectedEnumOption is EnumMember enumMember && enumMember.Value?.LiteralValue is not null)
                                {
                                    //选中空成员时按 %none 处理，其余按选中值精确匹配
                                    string selectedValue = enumMember.Value.LiteralValue.ToString();
                                    indexValue = selectedValue == "unset" ? "%none" : selectedValue;

                                }
                            }
                            break;
                        }
                    //处理的当前层直接取值
                    default:
                        {
                            if (currentDTO.Parent?.Children.Count > 0)
                            {
                                for (int i = 0; i < currentDTO.Parent.Children.Count; i++)
                                {
                                    if (currentDTO.Parent.Children[i].FieldName == indexString)
                                    {
                                        //同级字段当调度键（如 argument: xxx[[modifier]]）时必须用"当前选中的值"，
                                        //不能用模板默认值：否则改 modifier 也不会重新解析（AlphaBlend 解不到 alpha_blend）
                                        MetaTypeEditorFieldDTO keySourceField = currentDTO.Parent.Children[i];
                                        EnumMember keySourceMember = keySourceField.TypeKind is MetaTypeKind.Enum
                                            ? keySourceField.SelectedEnumOption
                                            : keySourceField.Items?.FirstOrDefault(item => item.TypeKind is MetaTypeKind.Enum)?.SelectedEnumOption;
                                        if (keySourceMember?.Value?.LiteralValue is not null)
                                        {
                                            string selectedValue = keySourceMember.Value.LiteralValue.ToString();
                                            indexValue = selectedValue == "unset" ? "%none" : selectedValue;

                                        }
                                        else if (keySourceField.SelectedUnionTypeName?.Value?.LiteralValue is not null)

                                        {
                                            indexValue = keySourceField.SelectedUnionTypeName.Value.LiteralValue.ToString();

                                        }

                                        else

                                        {
                                            indexValue = keySourceField.GetDefaultValue()?.ToString() ?? "";

                                        }
                                        break;

                                    }

                                }

                            }
                            break;

                        }
                }
            }
            switch (indexString)
            {
                //目标有值但找不到对应的调度器
                case "%unknown":
                    {
                        break;
                    }
                //目标没有值，调度器索引为空
                case "%none":
                    {
                        break;
                    }
            }
            #endregion
            #region 尝试将解释后Key表达式的值丢向文档资源池搜索指定调度器
            //复用调度器查找：多索引调度器（dispatch resource["a","b",…]）必须按整张索引表匹配，
            //原先这里只比对索引表第一项，键排在表中间或末尾时永远找不到自己的调度器，
            //会掉到 %unknown 兜底调度器上（值类型丢失、泛型实参退化成 any）
            result = FindDispatchTemplate(resource, currentResourceLocation, indexValue);
            // 防御：表达式解析失败时 result 会为 null，后续由 fallback 逻辑处理 %unknown/%none。
            if (result is not null && result.Children?.Count > 0)
            {
                for (int i = 0; i < result.Children.Count; i++)
                {
                }
            }
            #endregion
            return result;
        }

        /// <summary>
        /// 获取调度器并解释相关资源
        /// </summary>
        /// <param name="targetDTO"></param>
        /// <param name="version"></param>
        public MetaTypeEditorFieldDTO GetDispatchResource(MetaTypeEditorFieldDTO targetDTO, string version)
        {
            //处理有具体资源引用的调度器
            _ = targetDTO.FeatureMap.TryGetValue("Resource", out MetaValue resource);
            _ = targetDTO.FeatureMap.TryGetValue("Index", out MetaValue index);
            if (resource is null)
            {
                return null;
            }
            // %none 表示没有调度器 Index，此时也要走 fallback 模板。
            if (index is null)
            {
                index = new MetaValue { Kind = MetaValueKind.Literal, LiteralValue = "%none" };
            }
            string resourceString = resource.Kind is MetaValueKind.Literal ? resource.LiteralValue.ToString() : "";
            //父级只用于承载结果；索引允许字面量与列表（访问器索引 [[field]] 以列表承载），其余形态仍拒绝
            if (targetDTO.Parent is not null)
            {
                targetDTO.Parent.Children ??= [];
            }
            if (index.Kind is not (MetaValueKind.Literal or MetaValueKind.List))
            {
                return null;
            }
            if (index.Kind is MetaValueKind.Literal)//处理单索引的调度器
            {
                var targetInstanceDTO = InvokeAndInterpretDispatchResource([], targetDTO, index, resourceString, version);
                if (targetInstanceDTO is not null)
                {
                    #region 控制第一层子节点的路径
                    if (targetInstanceDTO.Children?.Count > 0 && targetInstanceDTO.Path is not null)
                    {
                        for (int i = 0; i < targetInstanceDTO.Children.Count; i++)
                        {
                            targetInstanceDTO.Children[i].Path ??= new(targetInstanceDTO.Path.TargetPath);
                        }
                        if (targetInstanceDTO.SelectedUnionChildren is not null)
                        {
                            for (int i = 0; i < targetInstanceDTO.SelectedUnionChildren.Count; i++)
                            {
                                targetInstanceDTO.SelectedUnionChildren[i].Path = new(targetInstanceDTO.Path.TargetPath);
                            }
                        }
                    }

                    #endregion
                    #region 返回解析结果，不再套壳
                    if (targetInstanceDTO.TypeKind is MetaTypeKind.List)
                    {
                        targetInstanceDTO.AddItemCommand = CreateAddListItemCommand(targetInstanceDTO, version);
                        targetInstanceDTO.RemoveItemCommand = CreateRemoveListItemCommand(targetInstanceDTO);

                    }
                    if (targetInstanceDTO.Children?.Count > 1 && targetInstanceDTO.UnionTypeNameList?.Count > 0)
                    {
                        targetInstanceDTO.OriginKind = targetInstanceDTO.TypeKind;
                        targetInstanceDTO.TypeKind = MetaTypeKind.Union;
                    }
                    return targetInstanceDTO;
                    #endregion
                }
            }
            #region 处理多索引的调度器
            else if (index.Kind is MetaValueKind.List)
            {
                for (int j = 0; j < index.Items.Count; j++)
                {
                    var resultDTO = InvokeAndInterpretDispatchResource([], targetDTO, index.Items[j], resourceString, version);
                    if (resultDTO is not null)
                    {
                        targetDTO.Parent.Children.Add(resultDTO);
                    }
                }
            }
            else
            {
                var enumDTO = targetDTO.Items.FirstOrDefault(item => item.TypeKind is MetaTypeKind.Enum);
                if (enumDTO is not null)
                {
                    MetaTypeEditorFieldDTO compositeDTO = new()
                    {
                        ID = "placeHolder",
                        TypeKind = MetaTypeKind.Composite,
                        Parent = targetDTO.Parent
                    };
                    MetaTypeEditorFieldDTO removeDTO = new()
                    {
                        ID = "placeHolder",
                        TypeKind = MetaTypeKind.Remove,
                        Parent = compositeDTO
                    };
                    removeDTO.RemoveItemCommand = CreateRemoveListItemCommand(targetDTO, compositeDTO);
                    compositeDTO.Items =
                    [
                        removeDTO,
                        new ()
                        {
                            ID = Guid.NewGuid().ToString(),
                            TypeKind = MetaTypeKind.Struct,
                            FieldName = !string.IsNullOrEmpty(enumDTO.SelectedEnumOption.Name) ? enumDTO.SelectedEnumOption.Name : "",
                            Parent = compositeDTO
                        }
                    ];
                    compositeDTO.Parent = targetDTO.Parent;
                    //父级可能尚未挂载，此时挂到自身子级
                    (targetDTO.Parent?.Children ?? (targetDTO.Children ??= [])).Add(compositeDTO);
                }
            }
            #endregion
            return null;
        }

        /// <summary>
        /// 动态 Map 展开后，索引已变成具体 key（例如 visual/moon_phase），
        /// 返回值是 DocumentItemMap 里的 dispatch 模板 Item，调用方会继续实例化它的真实调度目标。
        /// 此时按 Resource + Index 直接命中 dispatch 模板，而不是再走 %key 表达式。
        /// 根据 Resource 与具体索引值查找调度器模板。
        /// 用于动态 Map 展开后，索引已经从 %key 变成具体字符串的场景。
        /// </summary>
        private static MetaTypeEditorFieldDTO FindDispatchTemplate(Resource resource, string resourceString, string indexValue)
        {
            var dispatchPairList = resource.DocumentItemMap.Where(item =>
                item.Value?.TypeKind is MetaTypeKind.Dispatch
                || item.Value?.OriginKind is MetaTypeKind.Dispatch);
            foreach (var item in dispatchPairList)
            {
                MetaTypeEditorFieldDTO dispatchDTO = item.Value;
                if (dispatchDTO?.FeatureMap is null)
                {
                    continue;
                }
                if (!dispatchDTO.FeatureMap.TryGetValue("Resource", out MetaValue resourceValue)
                    || resourceValue?.LiteralValue?.ToString() != resourceString)
                {
                    continue;
                }
                if (!dispatchDTO.FeatureMap.TryGetValue("Index", out MetaValue indexValueMeta)
                    || indexValueMeta is null)
                {
                    continue;
                }
                if (indexValueMeta.Kind is MetaValueKind.Literal
                    && indexValueMeta.LiteralValue?.ToString() == indexValue)
                {
                    return dispatchDTO;
                }
                if (indexValueMeta.Kind is MetaValueKind.List
                    && indexValueMeta.Items is not null
                    && indexValueMeta.Items.Any(item => item.LiteralValue?.ToString() == indexValue))
                {
                    return dispatchDTO;
                }
            }
            return null;
        }

        /// <summary>
        /// 判断是否为懒加载占位桩：ID 标记为 placeHolder，或实例化后 ID 被替换成 GUID 的空 Any 桩。
        /// </summary>
        public static bool IsPlaceHolderNode(MetaTypeEditorFieldDTO node)
        {
            return node is not null
                && (node.ID == "placeHolder"
                || (node.TypeKind is MetaTypeKind.Any && node.Children is null && string.IsNullOrEmpty(node.FieldName)));
        }

        /// <summary>
        /// 取出调度索引里的访问器路径文本，兼容字面量、字面量列表与嵌套列表（[[field]] 形式）。
        /// </summary>
        /// <summary>
        /// 按最终成员重建各联合的名称列表：成员（尤其跨层合并来的）可能在构建过程中才定型。
        /// </summary>
        public static void RefreshUnionNameLists(MetaTypeEditorFieldDTO root)
        {
            if (root is null)
            {
                return;
            }
            //Composite 宿主的 Children 是给容器判定展开箭头用的行集合，不是分支表，不能拿它重建名称；
            //分支模板可能已经挪进桩（Value），名称必须按模板成员重建
            ObservableCollection<MetaTypeEditorFieldDTO>? members = root.Value as ObservableCollection<MetaTypeEditorFieldDTO> ?? root.Children;
            if (root.UnionTypeNameList?.Count > 1 && members?.Count > 0 && root.TypeKind is not MetaTypeKind.Composite)
            {
                //名称条数与成员一致说明现有名称（多来自模板）是准确的，不能再用类型兜底名覆盖，
                //否则像 error_message 那样会把两个成员都命名成 String；只有条数不符（如版本裁掉了成员）才重建
                int expectedCount = members.Count(child => child.IsVisible) + (root.UnionTypeNameList[0].Name == "- unset -" ? 1 : 0);
                if (root.UnionTypeNameList.Count == expectedCount)
                {

                    return;

                }
                //只按可见成员重建：版本过滤掉的成员不能进名称表，否则名称与 Children 下标会错位
                List<string> unionNames = UnionTypeNameParser.Parse([.. members.Where(child => child.IsVisible)]);
                root.UnionTypeNameList.Clear();
                //可选节点保留未设置项，否则重建后切不回 unset
                if (!root.IsRequired)

                {
                    root.UnionTypeNameList.Add(new EnumMember()
                    {
                        Name = "- unset -",
                        Value = new MetaValue() { Kind = MetaValueKind.Literal, LiteralValue = "unset" }

                    }
);
                }
                foreach (string unionName in unionNames)

                {
                    root.UnionTypeNameList.Add(new EnumMember()

                    {
                        Name = unionName,
                        Value = new MetaValue() { Kind = MetaValueKind.Literal, LiteralValue = unionName }
                    });
                }
            }
            foreach (MetaTypeEditorFieldDTO child in members ?? [])
            {
                RefreshUnionNameLists(child);
            }
            foreach (MetaTypeEditorFieldDTO child in root.SelectedUnionChildren ?? [])
            {
                RefreshUnionNameLists(child);
            }
            foreach (MetaTypeEditorFieldDTO child in root.Items ?? [])
            {
                RefreshUnionNameLists(child);
            }
        }

        /// <summary>
        /// 页面构建完成后处理入口级联合的首次落点：选中颜色值叶子且宿主是 Composite 的联合，走一次与切换一致的落点逻辑。
        /// </summary>
        public void ApplyEntryUnionLanding(MetaTypeEditorFieldDTO root, string version)
        {
            if (root is null)
            {
                return;
            }
            if (root.Parent?.TypeKind is MetaTypeKind.Composite
                && root.SelectedUnionChildren?.Count == 1
                && root.SelectedUnionChildren[0].ElementType is null
                && root.SelectedUnionChildren[0].TypeKind is MetaTypeKind.NamedColor or MetaTypeKind.String or MetaTypeKind.Int
                    or MetaTypeKind.Float or MetaTypeKind.Boolean or MetaTypeKind.Enum
                && root.SelectedUnionItemUpdated is not null)
            {
                root.SelectedUnionItemUpdated();
            }
            foreach (MetaTypeEditorFieldDTO child in root.Children ?? [])
            {
                ApplyEntryUnionLanding(child, version);
            }
            foreach (MetaTypeEditorFieldDTO child in root.SelectedUnionChildren ?? [])
            {
                ApplyEntryUnionLanding(child, version);
            }
            foreach (MetaTypeEditorFieldDTO child in root.Items ?? [])
            {
                ApplyEntryUnionLanding(child, version);
            }
        }

        public static string ExtractAccessorPath(MetaValue index)
        {
            if (index is null)
            {
                return "";
            }
            if (index.Kind is MetaValueKind.Literal)
            {
                return index.LiteralValue?.ToString() ?? "";
            }
            if (index.Kind is MetaValueKind.List && index.Items is not null)
            {
                return string.Join(".", index.Items.Select(ExtractAccessorPath).Where(item => !string.IsNullOrEmpty(item)));
            }
            return index.TypeValue?.LiteralValue?.ToString() ?? "";
        }

        /// <summary>
        /// 按访问器路径定位字段节点，支持 %parent 逐级上溯（[[modifier]] / [[%parent.%parent.modifier]]）。
        /// </summary>
        private static MetaTypeEditorFieldDTO ResolveAccessorTarget(MetaTypeEditorFieldDTO currentDTO, string accessorPath)
        {
            MetaTypeEditorFieldDTO scope = currentDTO;
            string fieldPath = accessorPath;
            while (fieldPath.StartsWith("%parent.", StringComparison.Ordinal))
            {
                scope = scope?.Parent;
                fieldPath = fieldPath["%parent.".Length..];
            }
            return scope?.Parent?.Children?.FirstOrDefault(child => child.FieldName == fieldPath)
                ?? scope?.Children?.FirstOrDefault(child => child.FieldName == fieldPath);
        }

        /// <summary>
        /// 调用并解释Dispatch资源
        /// </summary>
        /// <param name="anchorMap">上下文文本锚点映射表</param>
        /// <param name="parent">DTO实例的父级</param>
        /// <param name="resourceString">目标调度器</param>
        /// <param name="version">目标版本</param>
        /// <param name="index">调度器索引</param>
        /// <returns>返回解析出来的DTO实例列表</returns>
        public MetaTypeEditorFieldDTO InvokeAndInterpretDispatchResource(Dictionary<string, KeyValueAnchors> anchorMap, MetaTypeEditorFieldDTO currentDTO, MetaValue index, string resourceString, string version)
        {
            #region 计算Key表达式的值
            MetaTypeEditorFieldDTO targetDispatchDTO = null;
            // 动态 Map 展开后，索引已经被替换成具体 key（例如 visual/moon_phase）。
            // 索引可能写成访问器（资源对象的字段），先取出字段名/路径文本
            string accessorPath = ExtractAccessorPath(index);
            // 此时直接按 Resource + Index 查找 dispatch 模板，不再走 %key 表达式求值。
            if (!string.IsNullOrEmpty(accessorPath) && !accessorPath.StartsWith('%'))
            {
                targetDispatchDTO = FindDispatchTemplate(resource, resourceString, accessorPath);
            }
            //索引写成字段访问器（dispatch resource[[field]] / [[%parent.%parent.field]]）时，取该字段当前选中值再查表
            if (targetDispatchDTO is null && !string.IsNullOrEmpty(accessorPath) && !accessorPath.StartsWith('%'))
            {
                MetaTypeEditorFieldDTO indexField = ResolveAccessorTarget(currentDTO, accessorPath);
                string indexFieldValue = indexField?.SelectedEnumOption?.Value?.LiteralValue?.ToString()
                    ?? indexField?.SelectedEnumOption?.Name
                    ?? indexField?.Value?.ToString();
                //同级实例尚未构建时，用该字段所在定义的默认成员作索引
                if (string.IsNullOrEmpty(indexFieldValue))
                {
                    MetaTypeEditorFieldDTO indexFieldTemplate = currentDTO?.Parent?.TemplateReference?.Children?.FirstOrDefault(child => child.FieldName == accessorPath);
                    if (indexFieldTemplate is null
                        && currentDTO?.Path?.TargetPath is string definitionPath
                        && resource.DocumentItemMap.TryGetValue(definitionPath, out MetaTypeEditorFieldDTO definitionItem))
                    {
                        indexFieldTemplate = definitionItem.Children?.FirstOrDefault(child => child.FieldName == accessorPath);

                    }
                    if (indexFieldTemplate is not null)
                    {
                        string indexTypeName = indexFieldTemplate.Value?.ToString() ?? indexFieldTemplate.TypeName ?? "";
                        ResolvedTypeReference indexTypeReference = UsePathParser.Parse(resource, indexFieldTemplate.Path ?? currentDTO.Path, indexTypeName);
                        indexFieldValue = indexTypeReference?.Item?.EnumOptionList?.FirstOrDefault()?.Value?.LiteralValue?.ToString()
                            ?? indexTypeReference?.Item?.EnumOptionList?.FirstOrDefault()?.Name
                            //字段本身是常量（如 modifier: "override"）时直接用它作索引
                            ?? (indexFieldTemplate.TypeKind is MetaTypeKind.Literal ? indexTypeName : null);

                    }
                }
                if (!string.IsNullOrEmpty(indexFieldValue))
                {
                    targetDispatchDTO = FindDispatchTemplate(resource, resourceString, indexFieldValue);
                }
            }
            targetDispatchDTO ??= EvaluateKeyExpression(currentDTO, resourceString, index, resource);
            // %none 处理没有调度器 Index，%unknown 处理未知调度器 Index。
            // 这里取出的是 mcdoc 中声明的真实 dispatch 模板，后续会 InstantiateDTO 成对应调度目标，
            // 而不是简单构造一个空 Item。
            if (targetDispatchDTO is null)
            {
                //已经物化过的调度器节点走到这里说明是重新解释（如按 %key 解析出的列表结构再次经过构建），
                //索引解析不出来时保留它现有的解释结果，用兜底调度器覆盖会把已有结构降级成 any。
                //但"只有空桩"（Any 且无子级/行/行内项/值）说明当时没解析出目标，必须允许这次重新解析：
                //否则同级枚举一改（如 modifier 从 override 换到 alpha_blend）就永远不动
                bool hasMaterializedContent = currentDTO?.TypeKind is not MetaTypeKind.Dispatch && currentDTO?.OriginKind is MetaTypeKind.Dispatch
                    && (currentDTO.TypeKind is not MetaTypeKind.Any
                        || currentDTO.Children is { Count: > 0 }
                        || currentDTO.Items is { Count: > 0 }
                        || currentDTO.SelectedUnionChildren is { Count: > 0 }
                        || currentDTO.Value is not null);
                if (hasMaterializedContent)
                {
                    return null;
                }
                //选中空成员时用 %none 调度器，索引不存在时用 %unknown 调度器
                string indexText = index.Kind is MetaValueKind.Literal ? index.LiteralValue?.ToString() : null;
                string fallbackIndex = indexText is "%none" or "unset" ? "%none" : "%unknown";
                targetDispatchDTO = FindDispatchTemplate(resource, resourceString, fallbackIndex);
            }
            MetaTypeEditorFieldDTO targetDispatchInstance = null;
            if (targetDispatchDTO is null)
            {
                return null;
            }
            //目标就是当前节点自身的模板（自引用调度）时直接跳过，避免解析链递归
            if (ReferenceEquals(targetDispatchDTO, currentDTO?.TemplateReference))
            {
                return null;
            }
            #endregion
            #region 解释可能为泛引用节点的子级
            targetDispatchInstance = InstantiateDTO(targetDispatchDTO, version);
            //解析端在内建类型目标上留了 DispatchTarget 标记（记录目标的 MetaTypeKind），
            //但此前没有任何地方消费它。`to float`（%unknown 分支）这类目标既没有类型名、
            //也没有子级，不认这个标记就会被当成"调度器没解析出来"，节点停在 Dispatch 上空转
            //（界面表现为一行没有编辑器的空节点）。具名定义与泛型目标各走各的路径，不受影响。
            if (targetDispatchInstance.TypeKind is MetaTypeKind.Dispatch
                && targetDispatchInstance.FeatureMap is not null
                && targetDispatchInstance.FeatureMap.TryGetValue("DispatchTarget", out MetaValue dispatchTarget)
                && dispatchTarget?.LiteralValue is not null
                && Enum.TryParse(dispatchTarget.LiteralValue.ToString(), out MetaTypeKind dispatchTargetKind))
            {
                targetDispatchInstance.TypeKind = dispatchTargetKind;
                //目标是裸的值类型（to float / to boolean / to string 等）时模板上没带值，
                //补一个类型默认值，否则界面上是一个空编辑框
                if (targetDispatchInstance.Value is null
                    && dispatchTargetKind is MetaTypeKind.Byte or MetaTypeKind.Short or MetaTypeKind.Int
                        or MetaTypeKind.Long or MetaTypeKind.Float or MetaTypeKind.Double
                        or MetaTypeKind.Boolean or MetaTypeKind.String)
                {
                    targetDispatchInstance.Value = targetDispatchInstance.GetDefaultValue();

                }
            }
            //调度目标是泛型参数本身（如 `dispatch ...[override, %none]<T> to T`）时，
            //它的实参只存在于使用点的绑定作用域里（外层 FloatAttributeModifier<T>）。
            //这种壳既没有子级、也不能当文档类型解析，不换成实参类型的话求解结果为空，
            //节点会被静默降级成 Any（表现为一行没有编辑器的空节点）
            if ((targetDispatchInstance.Children is null || targetDispatchInstance.Children.Count == 0)
                && !string.IsNullOrEmpty(targetDispatchInstance.TypeName)
                && currentDTO?.BindingScope is not null
                && currentDTO.BindingScope.TryResolve(targetDispatchInstance.TypeName, out MetaValue boundArgument)
                && boundArgument is not null)

            {
                MetaType boundType = boundArgument.Kind is MetaValueKind.Type ? boundArgument.TypeValue : null;
                if (boundType is not null)

                {
                    MetaTypeEditorFieldDTO boundInstance = BuildDtoFromMetaType(boundType, targetDispatchDTO.FieldName ?? "", version);
                    if (boundInstance is not null)
                    {
                        targetDispatchInstance = boundInstance;

                    }
                }
            }
            // 目标是具名定义时（dispatch ... to SomeStruct / Holder<struct Inner>），
            // 把定义结构物化进调度实例：解析定义、按位置绑定形参并替换，访问器与后续校验都基于它。
            if ((targetDispatchInstance.Children is null || targetDispatchInstance.Children.Count == 0)
                && !string.IsNullOrEmpty(targetDispatchInstance.TypeName))
            {
                MCDocumentResourceBuilder.BuildResource(
                    targetDispatchInstance,
                    targetDispatchInstance,
                    version,
                    targetDispatchInstance.Path ?? targetDispatchDTO.Path,
                    resource,
                    this);
            }
            //设定字段名
            if (targetDispatchInstance.FeatureMap.TryGetValue("Index", out MetaValue indexValue) && indexValue is not null && indexValue.Kind is MetaValueKind.Literal)
            {
                targetDispatchInstance.FieldName = "minecraft:" + indexValue.LiteralValue?.ToString() ?? targetDispatchDTO.FieldName;
            }
            //处理结构体
            if (targetDispatchInstance.Children is not null)
            {
                for (int i = 0; i < targetDispatchInstance.Children.Count; i++)
                {
                    if (targetDispatchInstance.Children[i].Value is not null && targetDispatchInstance.Children[i].TypeKind is MetaTypeKind.Literal)
                    {
                        ResolvedTypeReference realReference = UsePathParser.Parse(resource, targetDispatchInstance.Path, targetDispatchInstance.Children[i].Value.ToString());
                        if (realReference?.Item is MetaTypeEditorFieldDTO realDTO)
                        {
                            var instanceRealDTO = InstantiateDTO(realDTO, version);
                            targetDispatchInstance.Children[i] = instanceRealDTO;
                        }
                    }
                }
            }//处理列表
            else if (targetDispatchInstance.OriginKind is not (MetaTypeKind.None or MetaTypeKind.Dispatch))
            {
                targetDispatchInstance.TypeKind = targetDispatchInstance.OriginKind;
                targetDispatchInstance.FeatureMap.Remove("Resource");
                targetDispatchInstance.FeatureMap.Remove("Index");
            }
            #endregion
            #region 执行验证、执行部分浅表复制、返回
            //验证与子级路径都要用"目标所在文档"：目标定义解析没写路径时退到调度语句所在文档。
            //用空路径去验证，目标内部的定义（player: CompositeEntity）会解析不到，
            //退化成字面量化的单选项枚举
            DocumentPath dispatchDocumentPath = targetDispatchInstance.Path ?? targetDispatchDTO.Path ?? currentDTO?.Path;
            if (targetDispatchInstance.Children?.Count > 0)
            {
                Validator.Verify(new([.. targetDispatchInstance.Children], anchorMap), [.. targetDispatchInstance.Children], version, dispatchDocumentPath);
                for (int i = 0; i < targetDispatchInstance.Children.Count; i++)
                {
                    targetDispatchInstance.Children[i].Parent = targetDispatchInstance;
                    targetDispatchInstance.Children[i].Path ??= dispatchDocumentPath;
                }
            }
            //实例 <- 模板的回填要先做，Accessor 之后 targetDispatchInstance 已经换人
            //模板自身没有路径时沿用调度节点的路径（调度语句所在文档），供目标内部的类型名解析
            targetDispatchInstance.Path = targetDispatchDTO.Path ?? currentDTO?.Path;
            targetDispatchInstance.TemplateReference = targetDispatchDTO;
            //处理 [value] / [modifier] 这类字段访问器。
            //具名结构目标的子级此刻尚未建立，先物化一次，访问器才能取到对应字段。
            if (currentDTO.FeatureMap.TryGetValue("Accessor", out MetaValue accessorMeta) && accessorMeta is not null
                && targetDispatchInstance.Children is null or { Count: 0 }
                && !string.IsNullOrEmpty(targetDispatchInstance.TypeName ?? targetDispatchInstance.Value?.ToString()))
            {
                MCDocumentResourceBuilder.BuildResource(targetDispatchInstance, targetDispatchInstance, version,
                    targetDispatchInstance.Path ?? currentDTO?.Path, resource, this);
            }
            // dispatch 目标解析完成后，再按 Accessor 从目标结构中取出对应字段。
            if (currentDTO.FeatureMap.TryGetValue("Accessor", out MetaValue accessorValue)
                && accessorValue is not null
                && targetDispatchInstance.Children?.Count > 0)
            {
                IEnumerable<string> accessorNameEnumerable = accessorValue.Kind is MetaValueKind.List && accessorValue.Items is not null
                    ? accessorValue.Items.Select(item => item.LiteralValue?.ToString())
                    : [accessorValue.LiteralValue?.ToString()];
                foreach (string accessorName in accessorNameEnumerable)
                {
                    if (string.IsNullOrEmpty(accessorName))
                    {
                        continue;
                    }
                    MetaTypeEditorFieldDTO accessorChild = targetDispatchInstance.Children
                        .FirstOrDefault(child => child.FieldName == accessorName);
                    if (accessorChild is null)
                    {
                        //字段名可能已被定义名顶替：按目标类型名取定义，再用同名字段下标对齐已构建的子级
                        MetaTypeEditorFieldDTO accessorDefinition = targetDispatchInstance.TemplateReference;
                        if (accessorDefinition?.Children is null)
                        {
                            string accessorTypeName = targetDispatchInstance.TypeName ?? targetDispatchInstance.Value?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(accessorTypeName))
                            {
                                accessorDefinition = UsePathParser.Parse(resource, targetDispatchInstance.Path ?? currentDTO?.Path, accessorTypeName).Item;

                            }

                        }
                        List<MetaTypeEditorFieldDTO> accessorTemplateChildren = accessorDefinition?.Children is null
                            ? []
                            : [.. accessorDefinition.Children];
                        int accessorIndex = accessorTemplateChildren.FindIndex(item => item.FieldName == accessorName);
                        if (accessorIndex > -1 && targetDispatchInstance.Children.Count > accessorIndex)
                        {
                            accessorChild = targetDispatchInstance.Children[accessorIndex];
                        }
                    }
                    if (accessorChild is not null)
                    {
                        targetDispatchInstance = accessorChild;
                        break;
                    }
                }
            }
            //懒加载桩不能提升，否则会连同外壳与模板引用一起丢掉
            if (targetDispatchInstance.Children?.Count == 1 && !IsPlaceHolderNode(targetDispatchInstance.Children[0]))
            {
                targetDispatchInstance = targetDispatchInstance.Children[0];
                targetDispatchInstance.Parent = currentDTO;
            }
            else if (targetDispatchInstance.Children?.Count > 0)
            {
                if (targetDispatchDTO.UnionTypeNameList is not null) { targetDispatchInstance.UnionTypeNameList = [.. targetDispatchDTO.UnionTypeNameList]; }
                targetDispatchInstance.SelectedUnionTypeName = targetDispatchDTO.UnionTypeNameList?.FirstOrDefault();
                targetDispatchInstance.SelectedUnionItemUpdated = () => SelectedUnionItemUpdated(targetDispatchInstance, version);
                if (targetDispatchInstance.Children[0].TypeKind is MetaTypeKind.Struct && targetDispatchInstance.Children[0].Children is not null)
                {
                    targetDispatchInstance.SelectedUnionChildren = [.. targetDispatchInstance.Children[0].Children];
                    targetDispatchInstance.SetRequired(true);
                }
                else
                {
                    targetDispatchInstance.SelectedUnionChildren = [targetDispatchInstance.Children[0]];
                }
            }
            //访问器取出的字段若是懒加载桩，就地物化，让属性结构直接可见
            if (targetDispatchInstance.Children?.Count == 1
                && IsPlaceHolderNode(targetDispatchInstance.Children[0])
                && targetDispatchInstance.TemplateReference?.Children is not null)
            {
                MetaTypeEditorFieldDTO stubTemplate = targetDispatchInstance.TemplateReference;
                List<MetaTypeEditorFieldDTO> instanceList = [.. stubTemplate.Children.Select(item => InstantiateDTO(item, version))];
                Validator.Verify(new(instanceList, []), [.. stubTemplate.Children], version, targetDispatchInstance.Path, true);
                targetDispatchInstance.Children = new(instanceList);
            }
            return targetDispatchInstance;
            #endregion
        }

        /// <summary>
        /// 使用栈迭代处理树形结构，展平规范节点或联合体/泛型节点，并在必要时提升节点类型。
        /// </summary>
        /// <returns>返回应当作为父节点子节点的 MetaTypeEditorFieldDTO 列表。</returns>
        public List<MetaTypeEditorFieldDTO> HierarchicallyUpdateTreeStructuredData(
            MetaTypeEditorFieldDTO root, string version)
        {
            Stack<(MetaTypeEditorFieldDTO node, bool stage)> stack = new();
            Dictionary<MetaTypeEditorFieldDTO, List<MetaTypeEditorFieldDTO>> resultCache = [];
            stack.Push((root, false));
            while (stack.Count > 0)
            {
                var (node, stage) = stack.Pop();
                //进入容器分拣
                if (!stage)
                {
                    stack.Push((node, true));
                    //联合（含被提升成枚举的原联合）的名称表不允许为空：版本过滤/裁剪后只剩单支时最容易漏，
                    //空表会让下拉框变成空白。按可见成员的名字/类型名兜底补齐
                    if ((node.TypeKind is MetaTypeKind.Union || node.OriginKind is MetaTypeKind.Union) && node.Children is not null)
                    {
                        List<MetaTypeEditorFieldDTO> unionMembers = [.. node.Children.Where(child => child.IsVisible && !IsPlaceHolderNode(child))];
                        if (unionMembers.Count > 0)
                        {
                            if (node.UnionTypeNameList is null or { Count: 0 })
                            {
                                node.UnionTypeNameList = [.. unionMembers.Select(child => !string.IsNullOrEmpty(child.FieldName)
                                    ? child.FieldName
                                    : child.TypeName ?? child.TypeKind.ToString())
                                    .Select(name => new EnumMember() { Name = name, Value = new MetaValue { Kind = MetaValueKind.Literal, LiteralValue = name } })];
                                node.SelectedUnionTypeName ??= node.UnionTypeNameList[0];

                            }

                            else

                            {
                                //名称表非空也可能撞车（模板直接带来的 Object×4）：按其它有效特征就地消解，界面上才分得清
                                List<string> disambiguatedNames = [.. node.UnionTypeNameList.Select(item => item.Name)];
                                UnionTypeNameParser.Disambiguate(disambiguatedNames, unionMembers);
                                for (int i = 0; i < node.UnionTypeNameList.Count && i < disambiguatedNames.Count; i++)
                                {
                                    node.UnionTypeNameList[i].Name = disambiguatedNames[i];

                                }
                            }
                        }
                    }
                    //可选联合停在“未设置”时不能带出任何成员内容：构建、拷贝、切换都由这里统一收尾
                    if (IsUnsetUnionSelection(node) && node.TypeKind is MetaTypeKind.Union or MetaTypeKind.Composite)

                    {
                        //换新集合而不是就地清空：子行可能是列表的元素集合，清空会把列表里的元素一起删掉
                        node.SelectedUnionChildren = [];
                        if (node.TypeKind is MetaTypeKind.Composite)

                        {
                            //换新集合而不是 Clear：行内选择器与宿主可能共享同一个集合对象
                            node.Children = [];
                            if (node.Items is not null)
                            {
                                int selectorTail = node.Items.ToList().FindLastIndex(item => item.TypeKind is MetaTypeKind.Union && item.FieldName == node.FieldName);
                                if (selectorTail > -1)
                                {
                                    while (node.Items.Count > selectorTail + 1)
                                    {
                                        node.Items.RemoveAt(node.Items.Count - 1);

                                    }
                                }
                            }
                        }
                    }
                    if (node.Children is not null)
                    {
                        VerifyVersion([.. node.Children], version);
                        for (int i = node.Children.Count - 1; i >= 0; i--)
                        {
                            var child = node.Children[i];
                            if (child.IsVisible || (child.Children?.Count > 0 && (IsContainerType(child.TypeKind) || IsIndirectType(child.TypeKind)) && string.IsNullOrEmpty(child.FieldName)))
                            {
                                stack.Push((child, false));
                            }
                            if (string.IsNullOrEmpty(child.FieldName) && child.TypeKind is MetaTypeKind.Dispatch)
                            {
                                child.IsVisible = false;
                            }
                            //只剔除挂载在子级的引用类节点
                            if (!child.IsVisible && string.IsNullOrEmpty(child.FieldName) && child.TypeKind is not MetaTypeKind.Dispatch && child.Value is not null)
                            {
                                node.Children.RemoveAt(i);
                            }
                        }
                    }
                }
                else
                {
                    // 收集所有可见子节点处理后的展平结果
                    List<MetaTypeEditorFieldDTO> allFlattenedChildren = [];
                    if (node.Children is not null)
                    {
                        VerifyVersion([.. node.Children], version);
                        for (int i = 0; i < node.Children.Count; i++)
                        {
                            if (!node.Children[i].IsVisible && node.Children[i].TypeKind is not MetaTypeKind.Dispatch)
                            {
                                node.Children.RemoveAt(i);
                                i--;
                            }
                        }
                        //单成员外壳收敛成结构体；单占位子级是懒加载桩（如 %key 解析出的列表元素类型），
                        //改写类型会把列表/枚举桩降级成结构体。
                        //联合模板的节点不能收敛：版本过滤后只剩一支时它仍是联合（要保留名称表与下拉框），
                        //收敛会把名称表清空、界面变成一个没有选择的空行
                        if (node.Children?.Count == 1 && !IsPlaceHolderNode(node.Children[0])
                            && node.TemplateReference?.TypeKind is not MetaTypeKind.Union
                            && node.UnionTypeNameList is null or { Count: 0 })
                        {
                            node.OriginKind = node.TypeKind;
                            node.TypeKind = MetaTypeKind.Struct;
                            node.UnionTypeNameList = null;
                        }
                        #region 添加符合要求的子节点并尝试从字典中添加它们
                        foreach (var child in node.Children)
                        {
                            if (!child.IsVisible)
                            {
                                //Dispatch无子级：保留空节点以存放 FeatureMap，不参与剥壳
                                if (child.TypeKind is MetaTypeKind.Dispatch && string.IsNullOrEmpty(child.FieldName) && (child.Children is null || child.Children.Count == 0))
                                {
                                    allFlattenedChildren.Add(child);
                                    continue;
                                }
                                //空FieldName的容器是展开残余，提取其子级提升到当前层
                                if ((IsContainerType(child.TypeKind) || IsIndirectType(child.TypeKind)) && string.IsNullOrEmpty(child.FieldName))
                                {
                                    if (resultCache.TryGetValue(child, out var promoted) && promoted.Count > 0)
                                    {
                                        allFlattenedChildren.AddRange(promoted);
                                    }
                                    else if (child.Children is not null)
                                    {
                                        allFlattenedChildren.AddRange(child.Children);
                                    }
                                }
                                continue;
                            }
                            if (resultCache.TryGetValue(child, out var childResult))
                            {
                                // 子节点缓存的展平结果可能为空（它曾在自身 S1 被"无可见子节点→隐藏"，
                                // 但随后父级的 VerifyVersion 又把它 IsVisible 重置回 true）。
                                // 此时应保留子节点本身，而不是 AddRange 一个空列表导致节点静默丢失。
                                if (childResult.Count > 0)
                                {
                                    allFlattenedChildren.AddRange(childResult);
                                }
                                else
                                {
                                    allFlattenedChildren.Add(child);
                                }
                            }
                            else
                            {
                                allFlattenedChildren.Add(child);
                            }
                        }

                        #endregion
                    }
                    // 判断是否应当展平
                    bool isCompoundItem = (IsContainerType(node.TypeKind) || IsIndirectType(node.TypeKind)) && node.TypeKind is not (MetaTypeKind.ByteArray or MetaTypeKind.IntArray or MetaTypeKind.LongArray or MetaTypeKind.List or MetaTypeKind.Composite);
                    if (!isCompoundItem || IsPlaceHolderNode(node))

                    {
                        resultCache[node] = [node];
                        continue;
                    }
                    bool shouldFlatten = false;
                    if (isCompoundItem)

                    {
                        // 只有当经过版本筛选后剩余不超过 1 个可见子节点时，才消除该联合体
                        if (allFlattenedChildren.Count <= 1 || string.IsNullOrEmpty(node.FieldName))
                        {
                            shouldFlatten = true;

                        }
                    }
                    if (!shouldFlatten)
                    {
                        // 保留当前节点（包括多子 Union、普通容器等），仅更新子节点列表
                        node.Children = new(allFlattenedChildren);
                        resultCache[node] = [node];
                    }

                    else

                    {
                        // 展平：要么消除节点，要么提升为唯一子节点
                        if (allFlattenedChildren.Count == 0 && node.TypeKind is not (MetaTypeKind.Composite or MetaTypeKind.Literal))
                        {
                            // 无可见子节点 -> 隐藏当前节点
                            node.IsVisible = false;
                            resultCache[node] = [];

                        }
                        else if (allFlattenedChildren.Count == 1)

                        {
                            var only = allFlattenedChildren[0];
                            bool isDefinitionItem = IsDefinitionItem(only.FeatureMap);
                            if (isDefinitionItem)
                            {
                                // 定义类节点是用户手写的 Key，必须保留原样，不能展平
                                resultCache[node] = [node];
                                continue;

                            }
                            if (!IsContainerType(only.TypeKind) && !IsPlaceHolderNode(only) && !string.IsNullOrEmpty(node.FieldName))

                            {
                                if (!IsIndirectType(only.TypeKind) && string.IsNullOrEmpty(only.FieldName))
                                {
                                    node.TypeKind = only.TypeKind;

                                }
                                if (only.EnumOptionList?.Count > 0 && string.IsNullOrEmpty(only.FieldName))
                                {
                                    node.EnumOptionList = [.. only.EnumOptionList];
                                    node.SelectedEnumItemUpdated = () => SelectedEnumItemUpdated(node, version);

                                }
                                if (only.UnionTypeNameList?.Count > 0)

                                {
                                    node.UnionTypeNameList = [.. only.UnionTypeNameList];
                                    node.SelectedUnionItemUpdated = () => SelectedUnionItemUpdated(node, version);

                                }
                                node.IsFalse = only.IsFalse;
                                node.IsTrue = only.IsTrue;
                                //被提升成员自己带着注解（如联合分支上的值域）时补到可见节点上：
                                //节点已有的注解优先，只补缺的
                                foreach (KeyValuePair<string, MetaValue> pair in only.FeatureMap ?? [])
                                {
                                    node.FeatureMap ??= [];
                                    if (!node.FeatureMap.ContainsKey(pair.Key))
                                    {
                                        node.FeatureMap[pair.Key] = pair.Value;

                                    }
                                }
                                // 保留所有上下文属性（FieldName, IsRequired, FeatureMap 等）
                                resultCache[node] = [node];
                            }
                            //带 Items 的 Composite 是展示容器，不当空壳提升掉
                            else if (!IsPlaceHolderNode(only)
                                && !(only.TypeKind is MetaTypeKind.Composite && only.Items?.Count > 0))
                            {
                                // 唯一子节点是容器类型 -> 将当前节点替换为那个容器，接管其子树
                                if (node.Children?.Count == 1 || node.Children is null)
                                {
                                    // 必须就地复制，不能用 node = new(only)：
                                    // 父级 Children 集合与 resultCache 的键仍持有 node 的原引用，
                                    // 重新赋值局部变量不会更新它们，会导致"提升"静默失效。
                                    var previousUnionHandler = node.SelectedUnionItemUpdated;
                                    var previousEnumHandler = node.SelectedEnumItemUpdated;
                                    //注解按"就近优先"（mcdoc 语义）：外层节点自己的注解压过被提升子级的，
                                    //CopyFrom 会整份替换 FeatureMap，先把外层的留一份再合并回去
                                    Dictionary<string, MetaValue> promotedOwnFeatures = node.FeatureMap is null ? [] : new(node.FeatureMap);
                                    node.CopyFrom(only);
                                    //版本门控逐节点独立：被提升子级的 since/until 不搬到外层节点上
                                    ApplyOwnFeatures(node, promotedOwnFeatures);
                                    //节点类内部不能使用本脚本，故在外侧额外判断一次是否需要订阅添加与删除功能
                                    if (node.TypeKind is MetaTypeKind.List)
                                    {
                                        node.AddItemCommand = CreateAddListItemCommand(node, version);
                                        node.RemoveItemCommand = CreateClearListItemCommand(node);
                                    }
                                    else if (node.TypeKind is MetaTypeKind.Composite && node.Items is not null && node.Items.Any(item => item.TypeKind is MetaTypeKind.List or MetaTypeKind.Add))
                                    {
                                        node.AddItemCommand = CreateAddCompositeOrCompoundItemCommand(node, version);
                                        node.RemoveItemCommand = CreateRemoveCompositeOrCompoundItemCommand(node.Parent, node);
                                    }
                                    node.SelectedUnionItemUpdated = previousUnionHandler;
                                    node.SelectedEnumItemUpdated = previousEnumHandler;
                                    if (node.TypeKind is MetaTypeKind.Union && node.SelectedUnionItemUpdated is null)
                                    {
                                        node.SelectedUnionItemUpdated = () => SelectedUnionItemUpdated(node, version);
                                    }
                                    if (node.TypeKind is MetaTypeKind.Enum && node.SelectedEnumItemUpdated is null)
                                    {
                                        node.SelectedEnumItemUpdated = () => SelectedEnumItemUpdated(node, version);
                                    }
                                    if (only.UnionTypeNameList is not null)
                                    {
                                        node.UnionTypeNameList = [.. only.UnionTypeNameList];
                                    }
                                    if (only.SelectedUnionChildren is not null)
                                    {
                                        node.SelectedUnionChildren = new ObservableCollection<MetaTypeEditorFieldDTO>(only.SelectedUnionChildren);
                                    }
                                    if (!IsContainerType(node.TypeKind))
                                    {
                                        node.Children = null;
                                    }
                                }
                                //CopyFrom 只拷集合引用，被接管子树的父链还指着原实例（被提升掉的那个联合成员），
                                //宿主已经换成 node 了：不补的话同级字段驱动的调度器重解析会去原实例上找宿主，切换静默失效
                                AttachParentLinks(node);
                                // 保留所有上下文属性（FieldName, IsRequired, FeatureMap 等）
                                resultCache[node] = [node];
                            }
                            else
                            {
                                // 唯一子节点是占位符（可选结构体/懒加载桩）：保留当前节点原样，
                                // 否则该节点不会登记进 resultCache，父级可能把它当成不可见节点丢弃。
                                resultCache[node] = [node];
                            }
                        }
                        // allFlattenedChildren.Count > 1
                        else if (node.TypeKind is not (MetaTypeKind.Composite or MetaTypeKind.Literal))
                        {
                            // 只有canonical节点才会进入此分支（Union/Generic 多子时 shouldFlatten 为 false）
                            // 完全消除别名节点，将其子节点列表直接交给父级
                            node.IsVisible = false; // 本身不可见，父级不会保留它
                            node.Children = new(allFlattenedChildren); // 更新 Children 供外部调用取用
                            resultCache[node] = allFlattenedChildren;
                        }
                        else
                        {
                            // Composite / Literal 且无子节点或多子节点：保留原样
                            node.Children = new(allFlattenedChildren);
                            resultCache[node] = [node];
                        }
                    }
                }
            }
            //return RefreshSubtreeLayerByLayer(root, resultCache);
            return resultCache.GetValueOrDefault(root, []);
        }

        /// <summary>
        /// 判断是否为容器类型（已移至 MetaTypeKindPredicates）
        /// </summary>
        public static bool IsContainerType(MetaTypeKind kind)
            => MetaTypeKindPredicates.IsContainerType(kind);

        /// <summary>
        /// 判断是否为泛引用类型（已移至 MetaTypeKindPredicates）
        /// </summary>
        public static bool IsIndirectType(MetaTypeKind kind)
            => MetaTypeKindPredicates.IsIndirectType(kind);

        public static bool IsListOrArrayOrValueType(MetaTypeKind kind)
            => MetaTypeKindPredicates.IsListOrArrayOrValueType(kind);

        /// <summary>
        /// 将调度器结构填入目标泛型
        /// </summary>
        /// <param name="targetDispatchDTOInstance"></param>
        /// <returns></returns>
        private List<MetaTypeEditorFieldDTO> SubstituteGenericIterative(MetaTypeEditorFieldDTO targetDispatchDTOInstance, string version)
        {
            if (targetDispatchDTOInstance.Path is null || string.IsNullOrEmpty(targetDispatchDTOInstance.TypeName))
            {
                return [];
            }
            List<MetaTypeEditorFieldDTO> result = [];
            string targetTypeName = targetDispatchDTOInstance.TypeName;
            string documentItemPathString = targetDispatchDTOInstance.Path.TargetPath.ToString();
            int lastDoubleColonIndex = documentItemPathString.LastIndexOf("::");
            string baseUsePath = "";
            if (lastDoubleColonIndex > -1)
            {
                baseUsePath = documentItemPathString[..lastDoubleColonIndex];
            }
            string targetGenericStructUsePath = baseUsePath + "::" + targetTypeName;
            //池子是"边建边填"的：按下标直取会在跨文件别名还没注册时抛 KeyNotFoundException，
            //整条枚举切换链会跟着断掉，这里一律走 TryGetValue
            _ = resource.DocumentItemMap.TryGetValue(targetGenericStructUsePath, out MetaTypeEditorFieldDTO targetGenericDTO);
            if (targetGenericDTO is null
                && resource.DocumentPathItemMap.TryGetValue(baseUsePath, out List<string> currentUseList)
                && currentUseList is not null)
            {
                string usePath = currentUseList.FirstOrDefault(item => item.EndsWith(targetTypeName));
                if (!string.IsNullOrEmpty(usePath))
                {
                    _ = resource.DocumentItemMap.TryGetValue(usePath, out targetGenericDTO);
                }
            }
            if (targetGenericDTO?.TypeParameterNameList is { Count: > 0 } && targetGenericDTO.Children?.Count > 0)
            {
                MetaValue formalParameter = targetGenericDTO.TypeParameterNameList[0].Item2;
                var registry = DocumentDTOBuildStrategyRegistry.Create(resource, this);
                for (int i = 0; i < targetGenericDTO.Children.Count; i++)
                {
                    var childInstance = InstantiateDTO(targetGenericDTO.Children[i], version);
                    //找到使用了目标泛型的字段
                    if (childInstance.Value?.ToString() == formalParameter.LiteralValue?.ToString())
                    {
                        childInstance.Children ??= [];
                        for (int j = 0; j < (targetDispatchDTOInstance.Children?.Count ?? 0); j++)
                        {
                            childInstance.Children.Add(targetDispatchDTOInstance.Children[j]);
                            targetDispatchDTOInstance.Children[j].Path = targetDispatchDTOInstance.Path;
                            childInstance.Path = targetDispatchDTOInstance.Path;

                        }
                        childInstance.TypeKind = MetaTypeKind.Struct;
                        childInstance.Value = null;
                    }
                    result.Add(childInstance);
                }
            }
            return result;
        }
        /// <summary>
        /// 把匿名/内联 MetaType 实参递归转换为 Item 模板。
        /// 主要用于 dispatch + generic 组合时，结构体/枚举实参没有命名路径可解析的场景。
        /// </summary>
        private MetaTypeEditorFieldDTO BuildDtoFromMetaType(MetaType type, string fieldName, string version)
        {
            MetaTypeEditorFieldDTO dto = new()
            {
                FieldName = fieldName ?? "",
                TypeKind = type.Kind,
                //类型自带的注解（如 IntProvider<int @ 0..15> 里的实参）与字段注解同形，
                //一并搬进 FeatureMap，否则泛型实参/匿名类型上的限制会整块丢掉
                FeatureMap = type.AttributeList is { Count: > 0 } ? new(type.AttributeList) : []
            };
            switch (type.Kind)
            {
                case MetaTypeKind.Struct:
                    dto.Children = [];
                    if (type.FieldList is not null)
                    {
                        foreach (MetaField field in type.FieldList)
                        {
                            MetaTypeEditorFieldDTO child = BuildDtoFromMetaType(field.Type, field.Name, version);
                            child.SetRequired(field.IsRequired);
                            //字段自身注解（#[since] / #[divisible_by] 等）叠在类型注解之上
                            foreach (KeyValuePair<string, MetaValue> pair in field.AttributeList ?? [])
                            {
                                child.FeatureMap[pair.Key] = pair.Value;

                            }
                            dto.Children.Add(child);

                        }
                    }
                    break;
                case MetaTypeKind.Union:
                    dto.Children = [];
                    if (type.UnionOptionList is not null)
                    {
                        foreach (MetaType option in type.UnionOptionList)
                        {
                            dto.Children.Add(BuildDtoFromMetaType(option, "", version));
                        }
                    }
                    break;
                //父带括号的联合/元组在元类型上是 Composite，同样要把它承载的成员建出来
                case MetaTypeKind.Composite:
                    dto.Children = [];
                    foreach (MetaType option in type.UnionOptionList ?? type.TupleElementTypeList ?? [])
                    {
                        dto.Children.Add(BuildDtoFromMetaType(option, "", version));
                    }
                    break;
                case MetaTypeKind.Enum:
                    dto.EnumOptionList = [];
                    if (type.EnumMemberList is not null)
                    {
                        foreach (EnumMember member in type.EnumMemberList)
                        {
                            dto.EnumOptionList.Add(new EnumMember
                            {
                                Name = member.Name,
                                Value = member.Value
                            });
                        }
                    }
                    if (dto.EnumOptionList.Count > 0)
                    {
                        dto.SelectedEnumOption = dto.EnumOptionList[0];
                    }
                    break;
                case MetaTypeKind.List:
                    if (type.ElementType is not null)
                    {
                        dto.ElementType = BuildDtoFromMetaType(type.ElementType, fieldName, version);
                    }
                    break;
                case MetaTypeKind.Literal:
                    dto.Value = type.LiteralValue?.ToString() ?? "";
                    break;
                default:
                    dto.Value = dto.GetDefaultValue();
                    break;
            }
            return dto;
        }
        /// 使用栈迭代展开泛型，并返回展开后的统一单层子节点列表
        /// </summary>
        /// <param name="targetTemplate">目标模板</param>
        /// <param name="formalParams">形参列表</param>
        /// <param name="actualArgs">实参列表</param>
        /// <returns></returns>
        public List<MetaTypeEditorFieldDTO> SubstituteGenericIterative(
            MetaTypeEditorFieldDTO targetTemplate,
            List<Tuple<string, MetaValue>> formalParams,
            List<MetaValue> actualArgs,
            string version,
            TypeBindingScope? scope = null)
        {
            var paramMap = new Dictionary<string, MetaValue>();
            //按位置组合形参和实参映射表，实参是占位符时经作用域取外层实参
            for (int i = 0; i < formalParams.Count && i < actualArgs.Count; i++)
            {
                paramMap[formalParams[i].Item1] = ResolveArgument(actualArgs[i], scope ?? targetTemplate.BindingScope);
            }
            //统一的泛型列表，收集本层展开后的所有单层节点
            var resultList = new List<MetaTypeEditorFieldDTO>();
            //(目标集合, 待处理的当前节点)
            Stack<(IList<MetaTypeEditorFieldDTO> TargetList, MetaTypeEditorFieldDTO Node)> stack = new();
            //将模板的子节点深度克隆并逆序压入栈（逆序是为了出栈时保持原有顺序）
            if (targetTemplate.Children is not null)
            {
                for (int i = targetTemplate.Children.Count - 1; i >= 0; i--)
                {
                    stack.Push((resultList, InstantiateDTO(targetTemplate.Children[i], version)));
                }
            }
            while (stack.Count > 0)
            {
                var (targetList, child) = stack.Pop();
                //阻止下钻
                bool skipDrillDown = false;
                //参数替换逻辑：形参占位符可能以 TypeName 或 Value 承载名称，空串也要回落到 Value
                string typeResourceString = !string.IsNullOrEmpty(child.TypeName) ? child.TypeName : child.Value?.ToString() ?? "";
                MetaValue? actualMetaValue = null;
                _ = paramMap.TryGetValue(typeResourceString, out actualMetaValue);
                // 当前层没有命中形参时，沿 TypeBindingScope 向上查找，
                // 让嵌套泛型/别名节点也能消费外层泛型绑定。
                if (actualMetaValue is null && child.BindingScope is not null)
                {
                    _ = child.BindingScope.TryResolve(typeResourceString, out actualMetaValue);
                }
                //嵌套泛型应用的实参也经当前绑定解析，避免外层实参在下一层丢失
                if (child.ActualTypeArguments is not null)
                {
                    for (int i = 0; i < child.ActualTypeArguments.Count; i++)
                    {
                        MetaValue argument = child.ActualTypeArguments[i];
                        string placeholderName = argument.Kind is MetaValueKind.Type && argument.TypeValue?.Kind is MetaTypeKind.Literal
                            ? argument.TypeValue.LiteralValue?.ToString()
                            : null;
                        if (!string.IsNullOrEmpty(placeholderName) && paramMap.TryGetValue(placeholderName, out MetaValue boundArgument))
                        {
                            child.ActualTypeArguments[i] = boundArgument;
                        }
                    }
                }
                ResolvedTypeReference typeReference = null;
                // 普通非 Generic 节点如果命中了形参名，说明它是“泛型参数占位符”，
                // 需要用实参整体替换；Generic 节点则是“泛型类型应用”，不能在这里被实参直接替换。
                if (!string.IsNullOrEmpty(typeResourceString) && actualMetaValue is not null && child.TypeKind is not MetaTypeKind.Generic)
                {
                    MetaTypeEditorFieldDTO actualStruct = null;
                    if (actualMetaValue.Kind is MetaValueKind.Literal)
                    {
                        string currentActualTypeName = actualMetaValue.LiteralValue?.ToString() ?? "";
                        typeReference = UsePathParser.Parse(resource, targetTemplate.Path, currentActualTypeName);
                        actualStruct = typeReference?.Item;
                    }

                    else
                        if (actualMetaValue.Kind is MetaValueKind.Type && actualMetaValue.TypeValue is not null)

                        {
                            MetaType actualType = actualMetaValue.TypeValue;
                            if (actualType.Kind is MetaTypeKind.Generic)
                            {
                                // 实参本身是嵌套泛型：先构造一个 Generic Item 壳，
                                // 保留 TypeName 和 TypeParameterNameList，交给后续 GenericDTOBuilder 继续展开。
                                actualStruct = new MetaTypeEditorFieldDTO
                                {
                                    TypeKind = MetaTypeKind.Generic,
                                    TypeName = actualType.BaseType?.Name
                                                ?? actualType.BaseType?.MetaTypeName
                                                ?? actualType.BaseType?.Identifier
                                                ?? actualType.BaseType?.ReferencePath
                                                ?? actualType.BaseType?.LiteralValue?.ToString()
                                                ?? "",
                                    TypeParameterNameList = []

                                }
    ;
                                if (actualType.TypeArgumentList is not null)
                                {
                                    foreach (MetaType argType in actualType.TypeArgumentList)
                                    {
                                        if (argType.AttributeList is not null)
                                        {
                                            foreach (KeyValuePair<string, MetaValue> pair in argType.AttributeList)
                                            {
                                                actualStruct.TypeParameterNameList.Add(new Tuple<string, MetaValue>(pair.Key, pair.Value));

                                            }
                                        }
                                        else if (argType.LiteralValue is not null)

                                        {
                                            actualStruct.TypeParameterNameList.Add(new Tuple<string, MetaValue>(
                                                argType.LiteralValue.ToString(),
                                                new MetaValue { Kind = MetaValueKind.Literal, LiteralValue = argType.LiteralValue.ToString() }));
                                        }
                                        else

                                        {
                                            actualStruct.TypeParameterNameList.Add(new Tuple<string, MetaValue>(
                                                argType.Name ?? argType.Identifier ?? argType.ReferencePath ?? argType.LiteralValue?.ToString() ?? "",
                                                new MetaValue { Kind = MetaValueKind.Type, TypeValue = argType }));
                                        }
                                    }
                                }
                            }
                            else if (actualType.AttributeList?.Count > 0)

                            {
                                actualStruct = CompoundGenericMetaValueParser.Parse(resource, version, actualType.AttributeList);
                                //带属性但没有可查资源的实参（如 int @ 0..15）按类型本体构建
                                actualStruct ??= BuildDtoFromMetaType(actualType, child.FieldName ?? "", version);
                                if (actualStruct is not null && actualStruct.TypeKind is MetaTypeKind.Enum)

                                {
                                    actualStruct.SelectedEnumItemUpdated += () => SelectedEnumItemUpdated(actualStruct, version);
                                    actualStruct.SelectedEnumOption = actualStruct.EnumOptionList[0];
                                }
                            }
                            else
                            {
                                string typeName = actualType.Name
                                                ?? actualType.MetaTypeName
                                                ?? actualType.Identifier
                                                ?? actualType.ReferencePath
                                                ?? actualType.LiteralValue?.ToString()
                                                ?? "";
                                if (!string.IsNullOrEmpty(typeName))

                                {
                                    typeReference = UsePathParser.Parse(resource, targetTemplate.Path, typeName);
                                    actualStruct = typeReference?.Item;

                                }
                                // 匿名 struct / enum 实参没有可解析的命名路径，
                                // 直接从 MetaType 递归构建 Item 模板，供后续泛型替换使用。
                                actualStruct ??= BuildDtoFromMetaType(actualType, child.FieldName ?? "", version);

                            }

                        }
                        else if (actualMetaValue.Kind is MetaValueKind.Object && actualMetaValue.TypeValue?.AttributeList is not null)
                        {
                            actualStruct = CompoundGenericMetaValueParser.Parse(resource, version, actualMetaValue.TypeValue.AttributeList);
                            if (actualStruct is not null && actualStruct.TypeKind is MetaTypeKind.Enum)
                            {
                                actualStruct.SelectedEnumItemUpdated += () => SelectedEnumItemUpdated(actualStruct, version);
                                actualStruct.SelectedEnumOption = actualStruct.EnumOptionList[0];

                            }
                        }
                    //处理泛引用类型的实参
                    if (actualStruct is not null)

                    {
                        MetaTypeEditorFieldDTO replacedChild = InstantiateDTO(actualStruct, version);
                        replacedChild.FieldName = child.FieldName;
                        replacedChild.SetRequired(child.IsRequired);
                        replacedChild.Watermark = child.Watermark;
                        //形参成员自身的注解也要留在最终节点上（实参模板的注解已随 InstantiateDTO 带过来，
                        //这里只补成员侧的，避免泛型替换把 #[since] 这类上下文注解吃掉）
                        replacedChild.FeatureMap ??= [];
                        foreach (KeyValuePair<string, MetaValue> pair in child.FeatureMap ?? [])

                        {
                            replacedChild.FeatureMap[pair.Key] = pair.Value;

                        }
                        //继承替换链所在文档的路径，供替换结果内部的类型名解析
                        string? scopeDocumentPath = (scope ?? targetTemplate.BindingScope)?.ResolveDocumentPath();
                        if (replacedChild.Path is null && !string.IsNullOrEmpty(scopeDocumentPath))
                        {
                            replacedChild.Path = new DocumentPath(scopeDocumentPath);
                        }
                        //判断该结构是容器还是基础值
                        bool isContainer = actualStruct.TypeKind is MetaTypeKind.Struct
                                        or MetaTypeKind.List
                                        or MetaTypeKind.Dispatch
                                        or MetaTypeKind.ByteArray
                                        or MetaTypeKind.IntArray
                                        or MetaTypeKind.LongArray;
                        if (isContainer)
                        {
                            //容器模式,保留TypeKind
                            replacedChild.TypeKind = actualStruct.TypeKind;
                            //在Children中塞入唯一占位符，触发视图的折叠/展开按钮（+号）
                            replacedChild.Children = [new MetaTypeEditorFieldDTO() { ID = "placeHolder", TypeKind = MetaTypeKind.Any }];
                            //将完整的结构体模板缓存在TemplateReference中，供UI点击展开时读取和解析
                            //实参本身可能还是桩，穿透到真正带子级的那一层
                            MetaTypeEditorFieldDTO templateSource = actualStruct;
                            while (templateSource.TemplateReference is not null
                                && !ReferenceEquals(templateSource.TemplateReference, templateSource)
                                && (templateSource.Children is null
                                    || templateSource.Children.Count == 0
                                    || IsPlaceHolderNode(templateSource.Children[0])))
                            {
                                templateSource = templateSource.TemplateReference;
                            }
                            replacedChild.TemplateReference = templateSource;
                            replacedChild.Value = actualMetaValue;
                        }

                        else

                        {
                            //基础值模式：占位符自身的值就是形参名，必须丢弃并改用实参的默认值
                            replacedChild.TypeKind = actualStruct.TypeKind;
                            replacedChild.Children = null;
                            replacedChild.Value = actualStruct.GetDefaultValue();

                        }
                        child = replacedChild;
                        //无论容器还是值，统统阻断迭代器的后续下钻
                        skipDrillDown = true;

                    }
                    else

                    {
                        string resourceName = "minecraft:";
                        if (actualMetaValue.LiteralValue is not null)

                        {
                            resourceName += actualMetaValue.LiteralValue.ToString();

                        }
                        else if (actualMetaValue.TypeValue?.LiteralValue is not null)

                        {
                            resourceName += actualMetaValue.TypeValue?.LiteralValue.ToString();

                        }
                        //child.TypeName = resourceName;
                        //根据实参字符串自动转换为对应的枚举类型
                        if (Enum.TryParse<MetaTypeKind>(resourceName, true, out var parsedKind))

                        {
                            child.TypeKind = parsedKind;
                            var actualDTO = new MetaTypeEditorFieldDTO() { ID = "", TypeKind = parsedKind };
                            child.Value = actualDTO.GetDefaultValue();

                        }
                        //检查符号表中是否存在对应数据
                        else if (resource.GetRegistriesData(version) is JToken versionToken && versionToken.SelectToken(resourceName) is JArray jArray)

                        {
                            child.TypeKind = MetaTypeKind.Enum;
                            child.EnumOptionList ??= [];
                            child.EnumOptionList.AddRange(jArray.Values<string>().Select(item => new EnumMember() { Name = item, Value = new MetaValue() { Kind = MetaValueKind.Literal, LiteralValue = item } }));

                        }
                        //符号表没数据就试试搜索调度器
                        else
                        {
                            //应该创建一个只承载FeatureMap的DTO实例丢给资源构造器
                            var dispatchDTOList = resource.DocumentItemMap.Where(item => item.Value.TypeKind is MetaTypeKind.Dispatch);
                            List<KeyValuePair<string, MetaTypeEditorFieldDTO>> targetResourceDispatchDTOPair = [.. dispatchDTOList.Where(item => item.Value.FeatureMap.TryGetValue("Resource", out MetaValue resourceValue) && resourceValue.LiteralValue?.ToString() == resourceName)];
                            for (int i = 0; i < targetResourceDispatchDTOPair.Count; i++)
                            {
                                if (targetResourceDispatchDTOPair[i].Value.FeatureMap.TryGetValue("Index", out MetaValue metaValue) && metaValue.Items?.Count > 0)
                                {
                                    child.TypeKind = MetaTypeKind.Enum;
                                    child.EnumOptionList ??= [];
                                    child.EnumOptionList.AddRange(metaValue.Items.Select(item => new EnumMember() { Name = item.LiteralValue.ToString(), Value = new MetaValue() { Kind = MetaValueKind.Literal, LiteralValue = item } }));

                                }
                            }
                        }
                        //最后是基础类型，则将其视为字符串类型
                        {
                            child.TypeKind = MetaTypeKind.String;
                            child.Value = "";
                        }
                    }
                }
                else if (child.TypeKind is MetaTypeKind.Generic)
                {
                    //嵌套泛型的处理：先按 TypeName 找到泛型基类定义，复制其子模板，
                    //后续栈会继续对这些子模板中的泛型参数占位符做替换。
                    typeReference = UsePathParser.Parse(resource, targetTemplate.Path, child.TypeName);
                    if (typeReference?.Item is MetaTypeEditorFieldDTO subActualStruct)
                    {
                        var subCloned = InstantiateDTO(subActualStruct, version);
                        child.Children = subCloned.Children;
                    }
                }
                //别名结构展开到单层
                bool shouldFlatten = (child.TypeKind is MetaTypeKind.Literal or MetaTypeKind.Generic)
                                     && string.IsNullOrEmpty(child.FieldName)
                                     && child.Children is not null
                                     && child.Children.Count > 0;
                if (shouldFlatten)
                {
                    //如果是别名结构，放弃该外壳节点，直接将它展开的子节点全部推入栈，
                    //让这些子节点直接挂载到现有的targetList（也就是上级的列表）中
                    for (int i = child.Children.Count - 1; i >= 0; i--)
                    {
                        stack.Push((targetList, child.Children[i]));
                    }
                }
                else
                {
                    //直接加入当前目标层级
                    targetList.Add(child);
                    //若包含深层子节点（如普通Struct结构体），则递归下钻（使用新的子集合接管以保持层级包裹）
                    if (!skipDrillDown && child.Children is not null && child.Children.Count > 0)
                    {
                        ObservableCollection<MetaTypeEditorFieldDTO> newChildList = [];
                        for (int i = child.Children.Count - 1; i >= 0; i--)
                        {
                            //下钻时的TargetList变为这个节点的子集合
                            stack.Push((newChildList, child.Children[i]));
                        }
                        child.Children = newChildList;
                    }
                }
            }
            return resultList;
        }
        /// <summary>
        /// 判定是否为需要用户手写的定义类节点（已移至 MetaTypeFeatureHelper）
        /// </summary>
        public static bool IsDefinitionItem(Dictionary<string, MetaValue> featureMap)
            => MetaTypeFeatureHelper.IsDefinitionItem(featureMap);
        /// <summary>
        /// 校验版本：根据 since/until 约束和用户目标版本决定节点是否可见（已移至 VersionFilter）。
        /// </summary>
        public static void VerifyVersion(List<MetaTypeEditorFieldDTO> targetDTOList, string version)
            => VersionFilter.VerifyVersion(targetDTOList, version);
        #endregion
    }
}
