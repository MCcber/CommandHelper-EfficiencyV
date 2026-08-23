using CBHK.CustomControl.VectorComboBox;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using CommunityToolkit.Mvvm.Input;
using DryIoc.ImTools;
using MinecraftLanguageModelLibrary.Data;
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
        #endregion

        #region Property
        public MetaTypeDTOValidator Validator { get; set; }
        public ICommand CreateAddItemCommand(MetaTypeEditorFieldDTO dto, string version)
            => new RelayCommand(() => ExecuteAddItem(dto, version));

        public ICommand CreateRemoveItemCommand(MetaTypeEditorFieldDTO dto, MetaTypeEditorFieldDTO item = null)
            => new RelayCommand(() => ExecuteRemoveItem(dto, item));

        public ICommand CreateReFreshCommand(MetaTypeEditorFieldDTO dto, string version)
            => new RelayCommand(() => ExecuteReFreshItem(dto, version));
        #endregion

        #region Method
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
        /// 联合体成员更新事件
        /// </summary>
        public void SelectedUnionItemUpdated(MetaTypeEditorFieldDTO unionDTO, string version)
        {
            MetaTypeEditorFieldDTO targetDTO;
            if (unionDTO.Parent.Children?.Count > 0)
            {
                targetDTO = unionDTO.Parent;
            }
            else
            {
                targetDTO = unionDTO;
            }
            if (unionDTO.Children is not null && unionDTO.SelectedUnionItemIndex > -1 && unionDTO.SelectedUnionItemIndex <= unionDTO.Children.Count)
            {
                #region 提取目标分支、发送给验证器、执行剥壳
                int index = unionDTO.SelectedUnionItemIndex;
                if (!targetDTO.IsRequired)
                {
                    index--;
                }
                if (index < 0)
                {
                    index = 0;
                }
                if(index < 0 || index >= targetDTO.Children.Count)
                {
                    return;
                }
                MetaTypeEditorFieldDTO targetChildTemplate = targetDTO.Children[index];
                MetaTypeEditorFieldDTO targetChildInstance = InstantiateDTO(targetChildTemplate, version);
                DTOInstanceContext context = new([targetChildInstance], []);
                Validator.Verify(context, [targetChildTemplate], version, targetChildInstance.Path ?? targetDTO.Path, false);
                // 对当前节点执行展平/提升，去除内部可能残留的 Literal、Generic 或单子 Union
                HierarchicallyUpdateTreeStructuredData(context.dtoInstanceList[0], version); 
                #endregion

                //处理容器类枚举
                if ((IsContainerType(context.dtoInstanceList[0].TypeKind) || IsIndirectType(context.dtoInstanceList[0].TypeKind)) && context.dtoInstanceList[0].Children is not null)
                {
                    #region 更换分支
                    targetDTO.SelectedUnionChildren.Clear();
                    for (int i = 0; i < targetDTO.Items.Count; i++)
                    {
                        if(!IsContainerType(targetDTO.Items[i].TypeKind) || targetDTO.Items[i].TypeKind is not MetaTypeKind.Union)
                        {
                            targetDTO.Items.RemoveAt(i);
                            i--;
                        }
                    }

                    targetDTO.SelectedUnionChildren.AddRange([.. context.dtoInstanceList[0].Children]);
                    #endregion
                }
                //处理值类枚举
                else
                {
                    #region 锁定处理目标并清除它的子级
                    MetaTypeEditorFieldDTO compositeDTO = null;
                    if (targetDTO.TypeKind is MetaTypeKind.Composite)
                    {
                        compositeDTO = targetDTO;
                    }
                    else if (targetDTO.Parent?.TypeKind is MetaTypeKind.Composite)
                    {
                        compositeDTO = targetDTO.Parent;
                    }
                    compositeDTO.Items ??= [];
                    var valueDTO = compositeDTO.Items.FirstOrDefault(item => !IsContainerType(item.TypeKind) && !IsIndirectType(item.TypeKind) && item.TypeKind is not MetaTypeKind.Remove);
                    if (valueDTO is not null)
                    {
                        compositeDTO.Items.Remove(valueDTO);
                    }
                    #endregion

                    #region 若切换为列表则给当前Composite容器赋值并给予容器添加按钮，否则视为值类型分支添加给Items列表
                    if (targetChildInstance.TypeKind is MetaTypeKind.List)
                    {
                        compositeDTO.Items.Add(new()
                        {
                            ID = "placeHolder",
                            TypeKind = MetaTypeKind.Add,
                            Parent = compositeDTO,
                            AddItemCommand = CreateAddItemCommand(compositeDTO, version),
                            RemoveItemCommand = CreateRemoveItemCommand(compositeDTO)
                        });
                        if (compositeDTO.ElementType is null)
                        {
                            var listDTO = compositeDTO.Children.FirstOrDefault(item => item.TypeKind is MetaTypeKind.List);
                            compositeDTO.ElementType = listDTO.ElementType;
                        }
                    }
                    else
                    {
                        compositeDTO.Items.Add(targetChildInstance);
                    }
                    compositeDTO.SelectedUnionChildren?.Clear();
                    #endregion
                }
            }
        }

        /// <summary>
        /// 枚举成员更新事件
        /// </summary>
        public void SelectedEnumItemUpdated(MetaTypeEditorFieldDTO enumDTO, string version)
        {
            #region 更新同层的所有动态调度器

            #region Field
            MetaTypeEditorFieldDTO parentDTO = enumDTO.Parent;
            if (parentDTO is null)
            {
                return;
            }

            int removeIndex = -1;
            List<int> dynamicDispatchIndexList = [];
            #endregion

            #region 搜索动态调度器的同时删除遗留的调度器解释后出现的节点
            for (int i = 0; i < parentDTO.Children.Count; i++)
            {
                //筛选出符合条件的调度器
                if (parentDTO.Children[i] != enumDTO && parentDTO.Children[i].TypeKind is MetaTypeKind.Dispatch && string.IsNullOrEmpty(parentDTO.Children[i].FieldName) && parentDTO.Children[i].FeatureMap.TryGetValue("Index", out MetaValue metaValue) && metaValue.LiteralValue?.ToString() == enumDTO.FieldName)
                {
                    removeIndex = i;
                    dynamicDispatchIndexList.Add(i);
                }
                //删除调度器之后带标记的节点
                if (removeIndex > -1 && i > removeIndex && parentDTO.Children[i].IsInterpretFromDispatch)
                {
                    parentDTO.Children.RemoveAt(i);
                    i--;
                }
            }
            #endregion

            #region 处理调度器
            string selectedEnumValue = enumDTO.SelectedEnumOption.Value?.LiteralValue?.ToString() ?? "";
            string fieldName = enumDTO.FieldName;
            int currentInsertIndex;
            for (int i = 0; i < dynamicDispatchIndexList.Count; i++)
            {
                MetaTypeEditorFieldDTO currentDTO = parentDTO.Children[dynamicDispatchIndexList[i]];
                currentInsertIndex = parentDTO.Children.IndexOf(currentDTO) + 1;
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
                        if (targetDispatchDTO is null)
                        {
                            return;
                        }
                        var targetDispatchDTOInstance = InstantiateDTO(targetDispatchDTO, version, parentDTO);
                        var registry = DocumentDTOBuildStrategyRegistry.Create(resource, this);
                        List<MetaTypeEditorFieldDTO> resultList = [];

                        if (!string.IsNullOrEmpty(targetDispatchDTOInstance.TypeName) && targetDispatchDTO.Children?.Count > 0)
                        {
                            resultList = SubstituteGenericIterative(targetDispatchDTOInstance, version);
                            for (int j = 0; j < resultList.Count; j++)
                            {
                                resultList[j].IsInterpretFromDispatch = true;
                                if (resultList[j].IsRequired)
                                {
                                    resultList[j].TypeKind = MetaTypeKind.Struct;
                                    var childBuilder = registry.Get(resultList[j].TypeKind);
                                    childBuilder.Build(resultList[j], resultList[j], version, resultList[j].Path ?? enumDTO.Path, [], resultList[j].TypeKind is MetaTypeKind.Struct);
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
                        }
                        else
                        {
                            for (int j = 0; j < targetDispatchDTO.Children?.Count; j++)
                            {
                                if (targetDispatchDTO.Children[j].IsRequired)
                                {
                                    var instance = InstantiateDTO(targetDispatchDTO.Children[j], version, parentDTO);
                                    instance.IsInterpretFromDispatch = true;
                                    var childBuilder = registry.Get(instance.TypeKind);
                                    childBuilder.Build(instance, targetDispatchDTO.Children[j], version, targetDispatchDTO.Children[j].Path ?? enumDTO.Path, [], targetDispatchDTO.Children[j].TypeKind is MetaTypeKind.Struct);
                                    resultList.Add(instance);
                                }
                            }
                        }

                        //剥壳
                        for (int j = 0; j < resultList.Count; j++)
                        {
                            HierarchicallyUpdateTreeStructuredData(resultList[j], version);
                            if (resultList[j].OriginKind is MetaTypeKind.Dispatch && resultList[j].Value is IEnumerable<MetaTypeEditorFieldDTO>)
                            {
                                resultList[j].IsVisible = true;
                            }
                            parentDTO.Children.Insert(currentInsertIndex, resultList[j]);
                            currentInsertIndex++;
                        }
                        #endregion
                    }
                }
            }
            #endregion

            #endregion

            #region 更新代码编辑器

            #endregion
        }

        /// <summary>
        /// 处理自定义Key的节点
        /// </summary>
        /// <param name="definitionDTO"></param>
        /// <param name="resource"></param>
        /// <param name="anchorMap"></param>
        /// <param name="version"></param>
        public void DefinitionEnterKeyDown(MetaTypeEditorFieldDTO definitionDTO, Resource resource, Dictionary<string, KeyValueAnchors> anchorMap, string version)
        {
            #region Field
            bool haveDefinitionData = !string.IsNullOrEmpty(definitionDTO.FieldName) && definitionDTO.Path?.TargetPath.Length > 0;
            if (!haveDefinitionData)
            {
                return;
            }
            MetaTypeEditorFieldDTO definitionParent = definitionDTO.Parent;
            if (definitionParent is null)
            {
                return;
            }

            string fieldName = definitionDTO.FieldName;
            string documentItemPath = definitionDTO.Path.TargetPath.ToString();
            #endregion

            #region 搜索内部资源
            int lastDoubleColonIndex = documentItemPath.LastIndexOf("::");
            string baseDocumentItemPath = documentItemPath[..lastDoubleColonIndex];
            string targetDocumentItemPath = baseDocumentItemPath + "::" + fieldName;
            bool isInnerDTO = resource.DocumentItemMap.TryGetValue(targetDocumentItemPath, out MetaTypeEditorFieldDTO targetDTO);
            #endregion

            #region 搜索外部资源
            if (!isInnerDTO && resource.DocumentPathItemMap.TryGetValue(baseDocumentItemPath, out List<string> usePathList) && usePathList?.Count > 0)
            {
                targetDocumentItemPath = usePathList.FirstOrDefault(item => item.EndsWith(fieldName));
                if (!string.IsNullOrEmpty(targetDocumentItemPath) && resource.DocumentItemMap.TryGetValue(targetDocumentItemPath, out targetDTO)) { }
            }
            #endregion

            #region 执行构建
            if (targetDTO is not null)
            {
                #region 构建外层容器并使定义节点归位
                MetaTypeEditorFieldDTO customDefinitionDTO = new()
                {
                    ID = Guid.NewGuid().ToString(),
                    TypeKind = MetaTypeKind.Struct,
                    FieldName = definitionDTO.Value.ToString(),
                    Children = []
                };
                definitionDTO.Value = "";
                definitionParent.Children.Add(customDefinitionDTO);
                #endregion

                #region 使用注册器执行构建
                var registry = DocumentDTOBuildStrategyRegistry.Create(resource, this);
                var childBuilder = registry.Get(targetDTO.TypeKind);
                var instance = InstantiateDTO(targetDTO, version);
                childBuilder.Build(instance, targetDTO, version, new(targetDocumentItemPath), anchorMap, instance.TypeKind is MetaTypeKind.Struct && instance.IsRequired);
                #endregion

                #region 添加并剥壳
                for (int i = 0; i < instance.Children.Count; i++)
                {
                    instance.Children[i].Path = new(targetDocumentItemPath);
                    instance.Children[i].Parent = customDefinitionDTO;
                }
                customDefinitionDTO.Children.AddRange(instance.Children);
                instance.Children.Clear();
                HierarchicallyUpdateTreeStructuredData(customDefinitionDTO, version);
                #endregion
            }
            #endregion
        }

        private void ExecuteAddItem(MetaTypeEditorFieldDTO currentDTO, string version)
        {
            var unionOREnumItemDTO = currentDTO.Items?.FirstOrDefault(item => item.TypeKind is MetaTypeKind.Union or MetaTypeKind.Enum);
            bool isCompositeItem = currentDTO.TypeKind is MetaTypeKind.Composite && currentDTO.Items?.Count > 1 && unionOREnumItemDTO?.SelectedUnionTypeName?.Name == "List";
            bool isListItem = currentDTO.TypeKind is MetaTypeKind.List;
            if (isCompositeItem || isListItem)
            {
                if (currentDTO.ElementType is null)
                {
                    return;
                }
                var entryItem = InstantiateDTO(currentDTO.ElementType, version, currentDTO);

                if (IsIndirectType(entryItem.TypeKind))
                {
                    entryItem.SetRequired(true);
                    currentDTO.ElementType.SetRequired(true);
                    DTOInstanceContext context = new([entryItem], []);
                    Validator.Verify(context, [currentDTO.ElementType], version, unionOREnumItemDTO?.Path ?? currentDTO.Path);
                    // 对当前节点执行展平/提升
                    entryItem = context.dtoInstanceList[0];
                    HierarchicallyUpdateTreeStructuredData(entryItem, version);
                    if (entryItem.TypeKind is not MetaTypeKind.Composite)
                    {
                        entryItem.TypeKind = MetaTypeKind.Entry;
                    }
                    entryItem.FieldName = "Entry";
                    entryItem.IsVisible = true;
                }

                if (isCompositeItem)
                {
                    currentDTO.SelectedUnionChildren ??= [];
                    currentDTO.SelectedUnionChildren.Add(entryItem);
                }
                else if (currentDTO.Parent?.TypeKind is MetaTypeKind.Composite)
                {
                    currentDTO.Parent.SelectedUnionChildren ??= [];
                    currentDTO.Parent.SelectedUnionChildren.Add(entryItem);
                    entryItem.Parent = currentDTO.Parent;
                    entryItem.RemoveItemCommand = CreateRemoveItemCommand(entryItem.Parent, entryItem);
                }
                else
                {
                    currentDTO.Items ??= [];
                    currentDTO.Items.Add(entryItem);
                    entryItem.Parent = currentDTO;
                    entryItem.RemoveItemCommand = CreateRemoveItemCommand(currentDTO);
                }
            }
            else
            {
                GetDispatchResource(currentDTO, version);
            }
        }

        private static void ExecuteRemoveItem(MetaTypeEditorFieldDTO currentDTO, MetaTypeEditorFieldDTO item = null)
        {
            if (currentDTO.TypeKind is MetaTypeKind.List)
            {
                currentDTO?.Items?.Clear();
            }
            else if (currentDTO.TypeKind is MetaTypeKind.Composite)
            {
                currentDTO.SelectedUnionChildren?.Remove(item);
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
                    EnumOptionList = template.EnumOptionList ?? null,
                    TypeKind = template.TypeKind,
                    OriginKind = template.OriginKind,
                    TypeName = template.TypeName,
                    TypeParameterNameList = template.TypeParameterNameList,
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
                TemplateReference = template,
                FeatureMap = new(template.FeatureMap),
                TypeName = template.TypeName,
                EnumOptionList = template.EnumOptionList ?? null,
                TypeParameterNameList = template.TypeParameterNameList,
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
                case MetaTypeKind.Dispatch:
                    {
                        if (template.Children is not null)
                        {
                            dto.Children = new(template.Children.Select(child => InstantiateDTO(child, version, dto, visited)));
                        }
                        // 如果调度器的目标类型是联合体，需要把联合体的选项信息也带过来
                        if (template.UnionTypeNameList?.Count > 0)
                        {
                            dto.UnionTypeNameList = [.. template.UnionTypeNameList];
                            dto.SelectedUnionTypeName = dto.UnionTypeNameList[0];
                            if (dto.Children?.Count > 0)
                            {
                                if (dto.Children[0].Children is not null)
                                {
                                    dto.SelectedUnionChildren = [.. dto.Children[0].Children];
                                }
                                else
                                {
                                    dto.SelectedUnionChildren = [dto.Children[0]];
                                }
                            }
                            else
                            {
                                dto.SelectedUnionChildren = [.. dto.Children];
                            }
                        }
                        if (template.EnumOptionList?.Count > 0)
                        {
                            dto.EnumOptionList = template.EnumOptionList;
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
                        }
                        break;
                    }

                case MetaTypeKind.Union:
                    {
                        dto.SelectedUnionChildren ??= [];
                        if (template.UnionTypeNameList?.Count > 0)
                        {
                            dto.UnionTypeNameList = [.. template.UnionTypeNameList];
                        }
                        if (template.Children is not null)
                        {
                            dto.Children = dto.Children = new(template.Children.Select(child => InstantiateDTO(child, version, dto, visited)));
                            if (dto.Children?.Count > 0)
                            {
                                if (dto.Children[0].Children is not null)
                                {
                                    dto.SelectedUnionChildren = [.. dto.Children[0].Children];
                                }
                                else
                                {
                                    dto.SelectedUnionChildren = [dto.Children[0]];
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
                            dto.AddItemCommand = CreateAddItemCommand(dto, version);
                            dto.RemoveItemCommand = CreateRemoveItemCommand(dto);
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
            string documentItemPath = currentDTO.Path.TargetPath.ToString();
            string indexString = currentIndex.Kind is MetaValueKind.Literal ? currentIndex.LiteralValue.ToString().TrimStart('[').TrimEnd(']') : "";
            string indexValue = "";
            if (currentIndex.Kind is MetaValueKind.List && currentIndex.Items is not null)
            {
                indexString = currentIndex.Items[0].LiteralValue?.ToString() ?? "";
            }
            #endregion

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
                            if (currentDTO.Items is not null && currentDTO.Items.Count > 0)
                            {
                                var targetEnumDTO = currentDTO.Items.FirstOrDefault(item => item.TypeKind is MetaTypeKind.Enum);
                                if (targetEnumDTO.SelectedEnumOption is EnumMember enumMember && enumMember.Value?.LiteralValue is not null)
                                {
                                    indexValue = enumMember.Value.LiteralValue.ToString();
                                }
                                else
                                {
                                    indexValue = "Error! No Dispatch structure corresponds to the key.";
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
                                        indexValue = currentDTO.Parent.Children[i].GetDefaultValue()?.ToString() ?? "";
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
            //搜素调度器
            var dispatchPairList = resource.DocumentItemMap.Where(item => item.Value?.TypeKind is MetaTypeKind.Dispatch || item.Value?.OriginKind is MetaTypeKind.Dispatch);
            //搜索资源键
            List<KeyValuePair<string, MetaTypeEditorFieldDTO>> targetResourcePairList = [.. dispatchPairList.Where(item => item.Value.FeatureMap?["Resource"]?.LiteralValue?.ToString() == currentResourceLocation)];

            for (int i = 0; i < targetResourcePairList.Count; i++)
            {
                if (targetResourcePairList[i].Value.FeatureMap.TryGetValue("Index", out MetaValue currentIndexValue) && currentIndexValue is not null && currentIndexValue.LiteralValue is not null && currentIndexValue.LiteralValue.ToString().Trim() == indexValue)
                {
                    result = targetResourcePairList[i].Value;
                }
                else if (targetResourcePairList[i].Value.FeatureMap["Index"]?.Items?.Count > 0 && targetResourcePairList[i].Value.FeatureMap["Index"].Items[0].LiteralValue is not null && targetResourcePairList[i].Value.FeatureMap["Index"].Items[0].LiteralValue.ToString() == indexValue)
                {
                    List<MetaValue> indexList = targetResourcePairList[i].Value.FeatureMap["Index"].Items;
                    //搜索索引值
                    bool haveTargetIndex = indexList.Any(item => item.LiteralValue is not null && item.LiteralValue.ToString() == indexValue);
                    if (haveTargetIndex)
                    {
                        result = targetResourcePairList[i].Value;
                        break;
                    }
                }
            }

            if (result.Children?.Count > 0)
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
        public void GetDispatchResource(MetaTypeEditorFieldDTO targetDTO, string version)
        {
            //处理有具体资源引用的调度器
            _ = targetDTO.FeatureMap.TryGetValue("Resource", out MetaValue resource);
            _ = targetDTO.FeatureMap.TryGetValue("Index", out MetaValue index);
            if (resource is null || index is null)
            {
                return;
            }
            //if (resource is not null && index is not null)
            //{
            string resourceString = resource.Kind is MetaValueKind.Literal ? resource.LiteralValue.ToString() : "";
            if (targetDTO.Parent is null)
            {
                return;
            }
            targetDTO.Parent.Children ??= [];

            if (index.Kind is MetaValueKind.Literal)//处理单索引的调度器
            {
                var targetInstanceDTO = InvokeAndInterpretDispatchResource([], targetDTO, index, resourceString, version);
                if (targetInstanceDTO is not null)
                {
                    #region 控制第一层子节点的路径
                    if (targetInstanceDTO.Children?.Count > 0)
                    {
                        for (int i = 0; i < targetInstanceDTO.Children.Count; i++)
                        {
                            targetInstanceDTO.Children[i].Path ??= new(targetInstanceDTO.Path.TargetPath);
                        }
                        for (int i = 0; i < targetInstanceDTO.SelectedUnionChildren.Count; i++)
                        {
                            targetInstanceDTO.SelectedUnionChildren[i].Path = new(targetInstanceDTO.Path.TargetPath);
                        }
                    }
                    #endregion

                    #region 封装新节点、刷新视图
                    MetaTypeEditorFieldDTO removeDTO = new()
                    {
                        ID = "placeHolder",
                        TypeKind = MetaTypeKind.Remove
                    };
                    MetaTypeEditorFieldDTO compositeDTO = new()
                    {
                        ID = "placeHolder",
                        TypeKind = MetaTypeKind.Composite,
                        Items = [removeDTO],
                        Parent = targetDTO.Parent,
                        Path = new(targetInstanceDTO.Path.TargetPath)
                    };
                    removeDTO.Parent = compositeDTO;

                    //处理结构体
                    if (targetInstanceDTO.Children?.Count > 0)
                    {
                        compositeDTO.Items.Add(targetInstanceDTO);
                        targetInstanceDTO.Children = targetInstanceDTO.Children;
                    }//处理列表
                    else if (targetInstanceDTO.TypeKind is MetaTypeKind.List)
                    {
                        targetInstanceDTO.Parent = compositeDTO;
                        targetInstanceDTO.AddItemCommand = CreateAddItemCommand(targetInstanceDTO, version);
                        targetInstanceDTO.RemoveItemCommand = CreateRemoveItemCommand(targetInstanceDTO);
                        compositeDTO.Items.Add(targetInstanceDTO);
                        compositeDTO.ElementType = targetInstanceDTO.ElementType;
                    }
                    else//处理值类型
                    {
                        compositeDTO.Items.Add(targetInstanceDTO);
                    }

                    removeDTO.Parent = compositeDTO;
                    removeDTO.RemoveItemCommand = CreateRemoveItemCommand(targetDTO, compositeDTO);
                    targetInstanceDTO.Parent = compositeDTO;
                    //处理联合体
                    if (targetInstanceDTO.Children?.Count > 1 && targetInstanceDTO.UnionTypeNameList?.Count > 0)
                    {
                        targetInstanceDTO.OriginKind = targetInstanceDTO.TypeKind;
                        targetInstanceDTO.TypeKind = MetaTypeKind.Union;
                        compositeDTO.SelectedUnionChildren = targetInstanceDTO.SelectedUnionChildren;
                    }

                    //添加给当前复合节点的父级
                    if (targetDTO.TypeKind is MetaTypeKind.Composite)
                    {
                        targetDTO.Parent.Children.Add(compositeDTO);
                        compositeDTO.Parent = targetDTO.Parent;
                    }
                    else
                    {
                        targetDTO.Children.Add(targetInstanceDTO);
                    }
                    #endregion
                }
            }
            //else if (index.Kind is MetaValueKind.List)//处理多索引的调度器
            //{
            //    for (int j = 0; j < index.Items.Count; j++)
            //    {
            //        var resultDTO = InvokeAndInterpretDispatchResource([], targetDTO, index.Items[j], resourceString, version);
            //        if (resultDTO is not null)
            //        {
            //            targetDTO.Parent.Children.Add(resultDTO);
            //        }
            //    }
            //}
            //}
            //else
            //{
            //    var enumDTO = targetDTO.Items.FirstOrDefault(item => item.TypeKind is MetaTypeKind.Enum);
            //    if(enumDTO is not null)
            //    {
            //        MetaTypeEditorFieldDTO compositeDTO = new()
            //        {
            //            ID = "placeHolder",
            //            TypeKind = MetaTypeKind.Composite,
            //            Parent = targetDTO.Parent
            //        };
            //        MetaTypeEditorFieldDTO removeDTO = new()
            //        {
            //            ID = "placeHolder",
            //            TypeKind = MetaTypeKind.Remove,
            //            Parent = compositeDTO
            //        };
            //        removeDTO.RemoveItemCommand = CreateRemoveItemCommand(targetDTO, compositeDTO);
            //        compositeDTO.Items =
            //        [
            //            removeDTO,
            //            new()
            //            {
            //                ID = Guid.NewGuid().ToString(),
            //                TypeKind = MetaTypeKind.Struct,
            //                FieldName = !string.IsNullOrEmpty(enumDTO.SelectedEnumOption.Name) ? enumDTO.SelectedEnumOption.Name : "",
            //                Parent = compositeDTO
            //            }
            //        ];
            //        compositeDTO.Parent = targetDTO.Parent;
            //        targetDTO.Parent.Children.Add(compositeDTO);
            //    }
            //}
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
            MetaTypeEditorFieldDTO targetDispatchDTO = EvaluateKeyExpression(currentDTO, resourceString, index, resource);
            MetaTypeEditorFieldDTO targetDispatchInstance = null;
            if (targetDispatchDTO is null)
            {
                return null;
            }
            #endregion

            #region 解释可能为泛引用节点的子级
            targetDispatchInstance = InstantiateDTO(targetDispatchDTO, version);
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
                        (string realUsePath, MetaTypeEditorFieldDTO realDTO) = UsePathParser.Parse(resource, targetDispatchInstance.Path, targetDispatchInstance.Children[i].Value.ToString());
                        if (realDTO is not null)
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
            if (targetDispatchInstance.Children?.Count > 0)
            {
                Validator.Verify(new([.. targetDispatchInstance.Children], anchorMap), [.. targetDispatchInstance.Children], version, targetDispatchInstance.Path);
                for (int i = 0; i < targetDispatchInstance.Children.Count; i++)
                {
                    targetDispatchInstance.Children[i].Parent = targetDispatchInstance;
                    targetDispatchInstance.Children[i].Path ??= targetDispatchInstance.Path;
                }
            }

            if (targetDispatchInstance.Children?.Count == 1)
            {
                targetDispatchInstance = targetDispatchInstance.Children[0];
                targetDispatchInstance.Parent = currentDTO;
            }
            else if (targetDispatchInstance.Children?.Count > 0)
            {
                targetDispatchInstance.UnionTypeNameList = targetDispatchDTO.UnionTypeNameList;
                targetDispatchInstance.SelectedUnionTypeName = targetDispatchDTO.UnionTypeNameList[0];
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

            targetDispatchInstance.Path = targetDispatchDTO.Path;
            targetDispatchInstance.TemplateReference = targetDispatchDTO;

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
                        if (node.Children?.Count == 1)
                        {
                            node.OriginKind = node.TypeKind;
                            node.TypeKind = MetaTypeKind.Struct;
                            node.UnionTypeNameList = null;
                        }

                        #region 检测是否需要重置联合体名称列表，是则执行重置
                        if (node.Children?.Count > 1)
                        {
                            bool isNeedRefreshUnionNameList = false;
                            for (int i = 0; i < node.Children.Count; i++)
                            {
                                isNeedRefreshUnionNameList = node.Children[i].OriginKind != node.Children[i].TypeKind;
                                if (isNeedRefreshUnionNameList)
                                {
                                    break;
                                }
                            }
                            //需要重置联合体名称列表
                            if (isNeedRefreshUnionNameList && (node.TypeKind is MetaTypeKind.Union || (node.TypeKind is MetaTypeKind.Composite && node.Items?.Count > 0 && node.Items[0].TypeKind is MetaTypeKind.Union)) && node.Children?.Count > 1)
                            {
                                node.UnionTypeNameList ??= [];
                                node.UnionTypeNameList.Clear();
                                List<string> unionNameList = UnionTypeNameParser.Parse([.. node.Children]);
                                node.UnionTypeNameList.AddRange(unionNameList.Select(item => new EnumMember() { Name = item, Value = new MetaValue() { Kind = MetaValueKind.Literal, LiteralValue = item } }));
                                if (!node.IsRequired)
                                {
                                    node.UnionTypeNameList.Insert(0, new EnumMember() { Name = "- unset -", Value = new MetaValue() { Kind = MetaValueKind.Literal, LiteralValue = "unset" } });
                                }
                            }
                        }
                        #endregion

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
                    }

                    // 判断是否应当展平
                    bool isCompoundItem = (IsContainerType(node.TypeKind) || IsIndirectType(node.TypeKind)) && node.TypeKind is not (MetaTypeKind.ByteArray or MetaTypeKind.IntArray or MetaTypeKind.LongArray or MetaTypeKind.List or MetaTypeKind.Composite);

                    if (!isCompoundItem || node.ID == "placeHolder")
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

                            if (!IsContainerType(only.TypeKind) && only.ID != "placeHolder" && !string.IsNullOrEmpty(node.FieldName))
                            {
                                // 唯一子节点是基本类型 -> 将当前节点提升为该基本类型
                                //if (node.Children?.Count == 1 || node.Children is null)
                                //{
                                //    node.Children.Clear();
                                //    node.Value = only.Value;
                                //}
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
                                // 保留所有上下文属性（FieldName, IsRequired, FeatureMap 等）
                                resultCache[node] = [node];
                            }
                            else if (only.ID != "placeHolder")
                            {
                                // 唯一子节点是容器类型 -> 将当前节点替换为那个容器，接管其子树
                                if (node.Children?.Count == 1 || node.Children is null)
                                {
                                    bool isRequired = node.IsRequired;
                                    // 必须就地复制，不能用 node = new(only)：
                                    // 父级 Children 集合与 resultCache 的键仍持有 node 的原引用，
                                    // 重新赋值局部变量不会更新它们，会导致"提升"静默失效。
                                    node.CopyFrom(only);
                                    if (!IsContainerType(node.TypeKind))
                                    {
                                        node.Children = null;
                                    }
                                    node.SetRequired(isRequired);
                                }
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
            var targetGenericDTO = resource.DocumentItemMap[targetGenericStructUsePath];
            if (targetGenericDTO is null)
            {
                List<string> currentUseList = resource.DocumentPathItemMap[baseUsePath];
                targetGenericStructUsePath = currentUseList.FirstOrDefault(item => item.EndsWith(targetTypeName));
                targetGenericDTO = resource.DocumentItemMap[targetGenericStructUsePath];
            }
            if (targetGenericDTO is not null && targetGenericDTO.Children?.Count > 0)
            {
                MetaValue formalParameter = targetGenericDTO.TypeParameterNameList.FirstOrDefault().Item2;
                var registry = DocumentDTOBuildStrategyRegistry.Create(resource, this);
                for (int i = 0; i < targetGenericDTO.Children.Count; i++)
                {
                    var childInstance = InstantiateDTO(targetGenericDTO.Children[i], version);
                    //找到使用了目标泛型的字段
                    if (childInstance.Value?.ToString() == formalParameter.LiteralValue?.ToString())
                    {
                        childInstance.Children ??= [];
                        for (int j = 0; j < targetDispatchDTOInstance.Children.Count; j++)
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
        /// 使用栈迭代展开泛型，并返回展开后的统一单层子节点列表
        /// </summary>
        /// <param name="targetTemplate">目标模板</param>
        /// <param name="formalParams">形参列表</param>
        /// <param name="actualArgs">实参列表</param>
        /// <returns></returns>
        public List<MetaTypeEditorFieldDTO> SubstituteGenericIterative(
            MetaTypeEditorFieldDTO targetTemplate,
            List<Tuple<string,MetaValue>> formalParams,
            List<Tuple<string, MetaValue>> actualArgs,
            string version)
        {
            var paramMap = new Dictionary<string, MetaValue>();
            //组合形参和实参映射表
            for (int i = 0; i < formalParams.Count && i < actualArgs.Count; i++)
            {
                paramMap[formalParams[i].Item1] = actualArgs[i].Item2;
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

                //参数替换逻辑
                string typeResourcefString = child.TypeName ?? child.Value?.ToString() ?? "";

                if (!string.IsNullOrEmpty(typeResourcefString) && paramMap.TryGetValue(typeResourcefString, out var actualName))
                {
                    KeyValuePair<string, MetaTypeEditorFieldDTO> pair = resource.DocumentItemMap.FirstOrDefault(p => p.Key.EndsWith("::" + actualName));
                    //处理泛引用类型的实参
                    if (pair.Value is MetaTypeEditorFieldDTO actualStruct)
                    {
                        var replacedChild = InstantiateDTO(actualStruct, version);
                        replacedChild.FieldName = child.FieldName;
                        replacedChild.SetRequired(child.IsRequired);
                        replacedChild.Watermark = child.Watermark;

                        // 1. 判断该结构是容器还是基础值
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
                            replacedChild.TemplateReference = actualStruct;
                            replacedChild.Value = actualName;
                        }
                        else
                        {
                            //基础值模式,更改节点类型并赋初值
                            replacedChild.TypeKind = actualStruct.TypeKind;
                            replacedChild.Children = null;
                            replacedChild.Value = child.Value ?? actualStruct.GetDefaultValue();
                        }

                        child = replacedChild;

                        //无论容器还是值，统统阻断迭代器的后续下钻
                        skipDrillDown = true;
                    }
                    else//处理基础类型的实参
                    {
                        child.TypeName = actualName.LiteralValue?.ToString() ?? "";
                        //根据实参字符串自动转换为对应的枚举类型
                        if (Enum.TryParse<MetaTypeKind>(child.TypeName, true, out var parsedKind))
                        {
                            child.TypeKind = parsedKind;
                            var actualDTO = new MetaTypeEditorFieldDTO() { ID = "", TypeKind = parsedKind };
                            child.Value = actualDTO.GetDefaultValue();
                        }
                        else//若实参不是基础类型，则将其视为字符串类型
                        {
                            child.TypeKind = MetaTypeKind.String;
                            child.Value = "";
                        }
                    }
                }
                else if (child.TypeKind is MetaTypeKind.Generic)
                {
                    //嵌套泛型的处理
                    KeyValuePair<string, MetaTypeEditorFieldDTO> pair = resource.DocumentItemMap.FirstOrDefault(p => p.Key.EndsWith("::" + child.TypeName));
                    if (pair.Value is MetaTypeEditorFieldDTO subActualStruct)
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
