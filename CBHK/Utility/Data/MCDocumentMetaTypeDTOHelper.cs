using CBHK.CustomControl.Container;
using CBHK.CustomControl.VectorComboBox;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using CBHK.Utility.Data.DTOBuilder;
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

        public ICommand CreateRemoveItemCommand(MetaTypeEditorFieldDTO dto)
            => new RelayCommand(() => ExecuteRemoveItem(dto));

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
        public void SelectedUnionItemUpdated(MetaTypeEditorFieldDTO unionDTO,string version)
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
            if (targetDTO.Children is not null && unionDTO.SelectedUnionItemIndex > -1 && unionDTO.SelectedUnionItemIndex < targetDTO.Children.Count)
            {
                MetaTypeEditorFieldDTO targetChildTemplate = targetDTO.Children[unionDTO.SelectedUnionItemIndex];
                MetaTypeEditorFieldDTO targetChildInstance = InstantiateDTO(targetChildTemplate,version);
                Validator.Verify(([targetChildInstance], []), [targetChildTemplate], version, targetChildInstance.DocumentItemPath ?? targetDTO.DocumentItemPath);
                // 对当前节点执行展平/提升，去除内部可能残留的 Literal、Generic 或单子 Union
                HierarchicallyUpdateTreeStructuredData(targetChildInstance, version);
                //处理容器类枚举
                if ((IsContainerType(targetChildInstance.TypeKind) || IsIndirectType(targetChildInstance.TypeKind)) && targetChildInstance.Children is not null)
                {
                    targetDTO.SelectedUnionChildren ??= [];
                    targetDTO.SelectedUnionChildren.Clear();
                    while (targetDTO.Items.Count > 1)
                    {
                        targetDTO.Items.RemoveAt(1);
                    }
                    targetDTO.SelectedUnionChildren.AddRange(targetChildInstance.Children);
                }
                //处理值类枚举
                else if(targetDTO.TypeKind is MetaTypeKind.Composite || targetDTO.Parent?.TypeKind is MetaTypeKind.Composite)
                {
                    targetDTO.Items ??= [];
                    while (targetDTO.Items.Count > 1)
                    {
                        targetDTO.Items.RemoveAt(1);
                    }

                    #region 若切换为列表则给当前Composite容器赋值并给予容器添加按钮，否则视为值类型分支添加给Items列表
                    if (targetChildInstance.TypeKind is MetaTypeKind.List)
                    {
                        if (targetDTO.TemplateReference is not null)
                        {
                            targetDTO.TemplateReference.ElementType = targetChildInstance.ElementType;
                        }

                        MetaTypeEditorFieldDTO compositeDTO = null;
                        if(targetDTO.TypeKind is MetaTypeKind.Composite)
                        {
                            compositeDTO = targetDTO;
                        }
                        else if(targetDTO.Parent?.TypeKind is MetaTypeKind.Composite)
                        {
                            compositeDTO = targetDTO.Parent;
                        }
                        compositeDTO.Items.Add(new()
                        {
                            ID = "placeHolder",
                            TypeKind = MetaTypeKind.Add,
                            Parent = targetDTO,
                            AddItemCommand = CreateAddItemCommand(targetDTO, version),
                            RemoveItemCommand = CreateRemoveItemCommand(targetDTO)
                        });
                    }
                    else
                    {
                        targetDTO.Items.Add(targetChildInstance);
                    } 
                    #endregion
                    targetDTO.SelectedUnionChildren?.Clear();
                }
            }
        }

        /// <summary>
        /// 枚举成员更新事件
        /// </summary>
        public static void SelectedEnumItemUpdated(MetaTypeEditorFieldDTO enumDTO)
        {

        }

        /// <summary>
        /// 处理自定义Key的节点
        /// </summary>
        /// <param name="definitionDTO"></param>
        /// <param name="resource"></param>
        /// <param name="anchorMap"></param>
        /// <param name="version"></param>
        public void DefinitionEnterKeyDown(MetaTypeEditorFieldDTO definitionDTO,Resource resource,Dictionary<string,KeyValueAnchors> anchorMap,string version)
        {
            if(!string.IsNullOrEmpty(definitionDTO.FieldName) && definitionDTO.DocumentItemPath?.Length > 0 && definitionDTO.Parent is MetaTypeEditorFieldDTO definitionParent)
            {
                #region Field
                string fieldName = definitionDTO.FieldName;
                string documentItemPath = definitionDTO.DocumentItemPath.ToString(); 
                #endregion

                #region 搜索内部资源
                int lastDoubleColonIndex = documentItemPath.LastIndexOf("::");
                string baseDocumentItemPath = documentItemPath[..lastDoubleColonIndex];
                string targetDocumentItemPath = baseDocumentItemPath + "::" + fieldName;
                bool isInnerDTO = resource.DocumentItemMap.TryGetValue(targetDocumentItemPath, out MetaTypeEditorFieldDTO targetDTO);
                #endregion

                #region 搜索外部资源
                if (!isInnerDTO && resource.DocumentPathItemMap.TryGetValue(baseDocumentItemPath,out List<string> usePathList) && usePathList?.Count > 0)
                {
                    targetDocumentItemPath = usePathList.FirstOrDefault(item=>item.EndsWith(fieldName));
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
                    childBuilder.Build(instance, targetDTO, version, new(targetDocumentItemPath), anchorMap);
                    #endregion

                    #region 添加并剥壳
                    customDefinitionDTO.Children.Add(instance);
                    HierarchicallyUpdateTreeStructuredData(customDefinitionDTO, version); 
                    #endregion
                }
                #endregion
            }
        }

        private void ExecuteAddItem(MetaTypeEditorFieldDTO currentDTO,string version)
        {
            bool isCompositeItem = currentDTO.TypeKind is MetaTypeKind.Composite && currentDTO.Items?.Count > 1 && currentDTO.Items.FirstOrDefault(item=>item.TypeKind is MetaTypeKind.Union or MetaTypeKind.Enum)?.SelectedUnionTypeName?.Name == "List";
            bool isListItem = currentDTO.TypeKind is MetaTypeKind.List;
            if (isCompositeItem || isListItem)
            {
                if (currentDTO?.TemplateReference is null || currentDTO?.TemplateReference.ElementType is null)
                {
                    return;
                }
                var newItem = InstantiateDTO(currentDTO.TemplateReference.ElementType, version, currentDTO);
                newItem.FieldName = "Entry";

                if (IsIndirectType(newItem.TypeKind))
                {
                    Validator.Verify(([newItem], []), [currentDTO.TemplateReference.ElementType], version,currentDTO.DocumentItemPath ?? currentDTO.Parent?.DocumentItemPath);
                    // 对当前节点执行展平/提升，去除内部可能残留的 Literal、Generic 或单子 Union
                    HierarchicallyUpdateTreeStructuredData(newItem, version);
                    if (newItem.TypeKind is not MetaTypeKind.Composite)
                    {
                        newItem.TypeKind = MetaTypeKind.Entry;
                    }
                }

                if (isCompositeItem)
                {
                    currentDTO.SelectedUnionChildren ??= [];
                    currentDTO.SelectedUnionChildren.Add(newItem);
                }
                else if(currentDTO.Parent?.TypeKind is MetaTypeKind.Composite)
                {
                    currentDTO.Parent.SelectedUnionChildren ??= [];
                    currentDTO.Parent.SelectedUnionChildren.Add(newItem);
                    newItem.RemoveItemCommand = CreateRemoveItemCommand(currentDTO.Parent);
                }
                else
                {
                    currentDTO.Items ??= [];
                    currentDTO.Items.Add(newItem);
                    newItem.RemoveItemCommand = CreateRemoveItemCommand(currentDTO);
                }
            }
            else
            {
                GetDispatchResource(currentDTO, version);
            }

            if (currentDTO.GetCommandParameter is not null)
            {
                object commandParameter = currentDTO.GetCommandParameter.Invoke();
                if (commandParameter is VectorTreeViewItem vectorTreeViewItem)
                {
                    vectorTreeViewItem.IsExpanded = true;
                }
            }
        }

        private static void ExecuteRemoveItem(MetaTypeEditorFieldDTO currentDTO)
        {
            if (currentDTO.TypeKind is MetaTypeKind.List)
            {
                currentDTO?.Items?.Clear();
            }
            else if(currentDTO.TypeKind is MetaTypeKind.Remove)
            {
                currentDTO.Parent?.Parent.Children.Remove(currentDTO.Parent);
            }
            object commandParameter = currentDTO.GetCommandParameter?.Invoke();
            if (commandParameter is VectorTreeViewItem vectorTreeViewItem)
            {
                vectorTreeViewItem.IsExpanded = vectorTreeViewItem.HasItems;
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
        /// <param name="visited">访问标记</param>
        /// <returns></returns>
        public MetaTypeEditorFieldDTO InstantiateDTO(
            MetaTypeEditorFieldDTO template,
            string version,
            MetaTypeEditorFieldDTO parent = null,
            HashSet<MetaTypeEditorFieldDTO> visited = null)
        {
            ArgumentNullException.ThrowIfNull(template);

            //初始化当前DTO实例的ID
            string id = Guid.NewGuid().ToString();

            // 初始化 visited 集合（仅在顶层调用时创建）
            visited ??= [];

            // 如果模板对象已经处理过，直接返回一个浅拷贝（避免无限递归）
            if (!visited.Add(template))
            {
                // 已访问过，返回一个不会引发递归的“叶子”节点（或你定义的特殊标记）
                MetaTypeEditorFieldDTO result = new()
                {
                    ID = id,
                    DocumentItemPath = template.DocumentItemPath,
                    TemplateReference = template,
                    Parent = parent,
                    FieldName = template.FieldName,
                    EnumOptionList = template.EnumOptionList ?? null,
                    TypeKind = template.TypeKind,
                    OriginKind = template.OriginKind,
                    TypeName = template.TypeName,
                    TypeParameterNameList = template.TypeParameterNameList,
                    IsRequired = template.IsRequired,
                    Watermark = template.Watermark,
                    Value = GetDefaultValue(template),
                    FeatureMap = new(template.FeatureMap)
                };
                if(template.Children is not null)
                {
                    result.Children = new(template.Children.Select(item => InstantiateDTO(item, version, result, visited)));
                }
                if(template.EnumOptionList is not null)
                {
                    result.EnumOptionList = [.. template.EnumOptionList];
                }
                //版本属性不泄露到子节点
                //if (template.TypeKind is not (MetaTypeKind.Struct or MetaTypeKind.Enum
                //    or MetaTypeKind.Dispatch or MetaTypeKind.Union))
                //{
                //    result.FeatureMap.Remove("since");
                //    result.FeatureMap.Remove("until");
                //}
                return result;
            }

            object defaultValue = GetDefaultValue(template);
            var dto = new MetaTypeEditorFieldDTO
            {
                ID = id,
                DocumentItemPath = template.DocumentItemPath,
                Parent = parent,
                TemplateReference = template,
                FeatureMap = new(template.FeatureMap),
                TypeName = template.TypeName,
                EnumOptionList = template.EnumOptionList ?? null,
                TypeParameterNameList = template.TypeParameterNameList,
                FieldName = template.FieldName,
                TypeKind = template.TypeKind,
                OriginKind = template.OriginKind,
                IsRequired = template.IsRequired,
                Watermark = template.Watermark,
                Value = defaultValue,
                Min = template.Min,
                Max = template.Max,
            };
            if(template.EnumOptionList is not null)
            {
                dto.EnumOptionList = [.. template.EnumOptionList];
            }

            //版本属性只属于定义它们的顶层结构，不应泄露到子节点
            //if (template.TypeKind is not (MetaTypeKind.Struct or MetaTypeKind.Enum
            //    or MetaTypeKind.Dispatch or MetaTypeKind.Union))
            //{
            //    dto.FeatureMap.Remove("since");
            //    dto.FeatureMap.Remove("until");
            //}

            // 处理容器子节点
            switch (template.TypeKind)
            {
                case MetaTypeKind.Struct:
                case MetaTypeKind.Dispatch:
                    {
                        if (template.Children is not null)
                        {
                            dto.Children = new ObservableCollection<MetaTypeEditorFieldDTO>(template.Children.Select(item => InstantiateDTO(item, version, dto, visited)));
                        }
                        // 如果调度器的目标类型是联合体，需要把联合体的选项信息也带过来
                        if (template.UnionTypeNameList is not null)
                        {
                            dto.UnionTypeNameList = [.. template.UnionTypeNameList];
                            dto.SelectedUnionTypeName = dto.UnionTypeNameList[0];
                            if (dto.Children?.Count > 0)
                            {
                                dto.SelectedUnionChildren = [..dto.Children[0].Children];
                            }
                            else
                            {
                                dto.SelectedUnionChildren = [.. dto.Children];
                            }
                        }
                        if(template.EnumOptionList is not null)
                        {
                            dto.EnumOptionList = template.EnumOptionList;
                            dto.SelectedEnumOption = dto.EnumOptionList[0];
                        }
                        if(template.ElementType is not null)
                        {
                            dto.ElementType = template.ElementType;
                        }

                        if(template.TypeKind is MetaTypeKind.Dispatch && template.UnionTypeNameList is not null)
                        {
                            dto.TypeKind = MetaTypeKind.Union;
                            dto.OriginKind = MetaTypeKind.Dispatch;
                        }
                        break;
                    }

                case MetaTypeKind.Union:
                    {
                        if (template.UnionTypeNameList is not null)
                        {
                            dto.UnionTypeNameList = [.. template.UnionTypeNameList];
                        }
                        if (template.Children is not null)
                        {
                            dto.Children = new ObservableCollection<MetaTypeEditorFieldDTO>(template.Children.Select(item => InstantiateDTO(item, version, dto, visited)));
                            if (dto.Children[0].Children?.Count > 0)
                            {
                                dto.SelectedUnionChildren = [.. dto.Children[0].Children];
                            }
                            else
                            {
                                dto.SelectedUnionChildren = [.. dto.Children];
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
                        if(template.TypeKind is MetaTypeKind.List)
                        {
                            dto.AddItemCommand = new RelayCommand(() => ExecuteAddItem(dto, version));
                            dto.RemoveItemCommand = new RelayCommand(() => ExecuteRemoveItem(dto));
                            dto.ReFreshCommand = new RelayCommand(() => ExecuteReFreshItem(dto, version));
                            if(template.ElementType is not null)
                            {
                                MetaTypeEditorFieldDTO elementType = InstantiateDTO(template.ElementType, version, template, visited);
                                dto.ElementType = elementType;
                            }
                        }
                        break;
                    }
            }

            return dto;
        }

        /// <summary>
        /// 计算Key表达式的值
        /// </summary>
        /// <param name="currentDTO"></param>
        /// <param name="currentIndex"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        private static MetaTypeEditorFieldDTO EvaluateKeyExpression(MetaTypeEditorFieldDTO currentDTO, MetaValue currentIndex,Resource resource)
        {
            #region Field
            MetaTypeEditorFieldDTO result = null;
            string documentItemPath = currentDTO.DocumentItemPath.ToString();
            string indexString = currentIndex.Kind is MetaValueKind.Literal ? currentIndex.LiteralValue.ToString().TrimStart('[').TrimEnd(']') : "";
            string indexValue = "";
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
                        default:
                            {
                                break;
                            }
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
                                        indexValue = GetDefaultValue(currentDTO.Parent.Children[i])?.ToString() ?? "";
                                        break;
                                    }
                                }
                            }
                            break;
                        }

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
            }
            #endregion

            #region 尝试将解释后Key表达式的值丢向文档资源池搜索指定调度器
            //搜素调度器
            var dispatchPairList = resource.DocumentItemMap.Where(item => item.Value?.TypeKind is MetaTypeKind.Dispatch || item.Value?.OriginKind is MetaTypeKind.Dispatch);
            //搜索资源键
            List<KeyValuePair<string, MetaTypeEditorFieldDTO>> targetResourcePairList = [.. dispatchPairList.Where(item => item.Value.FeatureMap?["Resource"]?.LiteralValue?.ToString() == indexValue)];
            for (int i = 0; i < targetResourcePairList.Count; i++)
            {
                if(targetResourcePairList[i].Value.FeatureMap["Index"]?.Items?.Count > 0 && targetResourcePairList[i].Value.FeatureMap["Index"].Items[0].LiteralValue is not null && targetResourcePairList[i].Value.FeatureMap["Index"].Items[0].LiteralValue.ToString() == indexValue)
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
            //目标有值但找不到对应的调度器
            if (result is null)
            {

            }
            #endregion

            return result;
        }

        public void GetDispatchResource(MetaTypeEditorFieldDTO targetDTO, string version)
        {
            if (targetDTO.FeatureMap.TryGetValue("Resource", out MetaValue resource) && resource is not null && targetDTO.FeatureMap.TryGetValue("Index", out MetaValue index) && index is not null)
            {
                string resourceString = resource.Kind is MetaValueKind.Literal ? resource.LiteralValue.ToString() : "";
                if(targetDTO.Parent is null)
                {
                    return;
                }
                targetDTO.Parent.Children ??= [];

                if (index.Kind is MetaValueKind.Literal)
                {
                    var targetInstanceDTO = InvokeAndInterpretDispatchResource([], targetDTO, index, resourceString, version);
                    if (targetInstanceDTO is not null)
                    {
                        MetaTypeEditorFieldDTO removeDTO = new()
                        {
                            ID = "placeHolder",
                            TypeKind = MetaTypeKind.Remove
                        };
                        MetaTypeEditorFieldDTO compositeDTO = new()
                        {
                            ID = "placeHolder",
                            TypeKind = MetaTypeKind.Composite,
                            Items = [removeDTO, targetInstanceDTO]
                        };
                        removeDTO.Parent = compositeDTO;
                        removeDTO.RemoveItemCommand = CreateRemoveItemCommand(removeDTO);
                        targetInstanceDTO.Parent = compositeDTO;
                        //处理结构体/数组/联合体
                        if (targetInstanceDTO.Children?.Count > 1)
                        {
                            //bool haveValueNode = targetInstanceDTO.Children.Any(item => item.TypeKind is MetaTypeKind.List or MetaTypeKind.ByteArray or MetaTypeKind.IntArray or MetaTypeKind.LongArray);
                            //if (haveValueNode)
                            //{
                            //    targetInstanceDTO.SelectedUnionItemUpdated = () => SelectedUnionItemUpdated(targetInstanceDTO, version);
                            //    targetInstanceDTO.SelectedUnionTypeName = targetInstanceDTO.UnionTypeNameList[0];
                            //}
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
                    }
                }
                else if (index.Kind is MetaValueKind.Array)
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
            }
            else
            {
                var enumDTO = targetDTO.Items.FirstOrDefault(item => item.TypeKind is MetaTypeKind.Enum);
                if(enumDTO is not null)
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
                    removeDTO.RemoveItemCommand = CreateRemoveItemCommand(removeDTO);
                    compositeDTO.Items =
                    [
                        removeDTO,
                        new()
                        {
                            ID = Guid.NewGuid().ToString(),
                            TypeKind = MetaTypeKind.Struct,
                            FieldName = !string.IsNullOrEmpty(enumDTO.SelectedEnumOption.Name) ? enumDTO.SelectedEnumOption.Name : "",
                            Parent = compositeDTO
                        }
                    ];
                    compositeDTO.Parent = targetDTO.Parent;
                    targetDTO.Parent.Children.Add(compositeDTO);
                }
            }
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
        public MetaTypeEditorFieldDTO InvokeAndInterpretDispatchResource(Dictionary<string, KeyValueAnchors> anchorMap,MetaTypeEditorFieldDTO currentDTO, MetaValue index, string resourceString, string version)
        {
            //计算Key表达式的值
            MetaTypeEditorFieldDTO targetDispatchDTO = EvaluateKeyExpression(currentDTO, index, resource);

            #region 实例化目标调度器结构、执行验证，最后添加到调用节点的子级中
            if (targetDispatchDTO is not null)
            {
                List<MetaTypeEditorFieldDTO> instanceList = [];
                List<MetaTypeEditorFieldDTO> targetDispatchChildren = [];
                if (targetDispatchDTO.Children is not null)
                {
                    targetDispatchChildren = [.. targetDispatchDTO.Children];
                    for (int i = 0; i < targetDispatchDTO.Children.Count; i++)
                    {
                        var instance = InstantiateDTO(targetDispatchDTO.Children[i], version);
                        instanceList.Add(instance);
                    }
                }
                else
                {
                    targetDispatchChildren = [targetDispatchDTO];
                    var instance = InstantiateDTO(targetDispatchDTO, version);
                    instanceList = [instance];
                    instance.TypeKind = instance.OriginKind;
                    instance.FeatureMap.Remove("Resource");
                    instance.FeatureMap.Remove("Index");
                }

                //递归逐层解释目标Dispatch实例数据
                Validator.Verify((instanceList, anchorMap), targetDispatchChildren, version, targetDispatchDTO.DocumentItemPath);
                targetDispatchDTO.Children = new(instanceList);
                for (int i = 0; i < instanceList.Count; i++)
                {
                    instanceList[i].Parent = targetDispatchDTO;
                    instanceList[i].DocumentItemPath = targetDispatchDTO.DocumentItemPath;
                }

                if (instanceList?.Count == 1)
                {
                    targetDispatchDTO = instanceList[0];
                    instanceList[0].Parent = currentDTO;
                }
                else if (instanceList?.Count > 0)
                {
                    targetDispatchDTO.UnionTypeNameList = [.. targetDispatchDTO.UnionTypeNameList];
                    targetDispatchDTO.SelectedUnionTypeName = targetDispatchDTO.UnionTypeNameList[0];
                    targetDispatchDTO.SelectedUnionItemUpdated = () => SelectedUnionItemUpdated(targetDispatchDTO, version);
                    if (targetDispatchDTO.Children[0].TypeKind is MetaTypeKind.Struct && targetDispatchDTO.Children[0].Children is not null)
                    {
                        targetDispatchDTO.SelectedUnionChildren = [.. targetDispatchDTO.Children[0].Children];
                    }
                    else
                    {
                        targetDispatchDTO.SelectedUnionChildren = [targetDispatchDTO.Children[0]];
                    }
                }

                targetDispatchDTO.DocumentItemPath = targetDispatchDTO.DocumentItemPath;
                targetDispatchDTO.TemplateReference = targetDispatchDTO;
                targetDispatchDTO.FieldName = targetDispatchDTO.FieldName;
            }

            return targetDispatchDTO;
            #endregion
        }

        /// <summary>
        /// 使用栈迭代处理树形结构，展平规范节点或联合体/泛型节点，并在必要时提升节点类型。
        /// </summary>
        /// <returns>返回应当作为父节点子节点的 MetaTypeEditorFieldDTO 列表。</returns>
        public static List<MetaTypeEditorFieldDTO> HierarchicallyUpdateTreeStructuredData(
            MetaTypeEditorFieldDTO root, string version)
        {
            Stack<(MetaTypeEditorFieldDTO node, int stage)> stack = new();
            Dictionary<MetaTypeEditorFieldDTO, List<MetaTypeEditorFieldDTO>> resultCache = [];

            stack.Push((root, 0));
            while (stack.Count > 0)
            {
                var (node, stage) = stack.Pop();
                //进入容器分拣
                if (stage == 0)
                {
                    stack.Push((node, 1));
                    if (node.Children is not null)
                    {
                        VerifyVersion([.. node.Children], version);
                        for (int i = node.Children.Count - 1; i >= 0; i--)
                        {
                            var child = node.Children[i];
                            if (child.IsVisible || (child.Children?.Count > 0 && (IsContainerType(child.TypeKind) || IsIndirectType(child.TypeKind)) && string.IsNullOrEmpty(child.FieldName)))
                            {
                                stack.Push((child, 0));
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
                        foreach (var child in node.Children)
                        {
                            if (!child.IsVisible)
                            {
                                // 空 FieldName 的容器是展开残余，提取其子级提升到当前层
                                if ((IsContainerType(child.TypeKind) || IsIndirectType(child.TypeKind)) && string.IsNullOrEmpty(child.FieldName))
                                {
                                    if (resultCache.TryGetValue(child, out var promoted) && promoted.Count > 0)
                                    {
                                        allFlattenedChildren.AddRange(promoted);
                                    }
                                    else if(child.Children is not null)
                                    {
                                        allFlattenedChildren.AddRange(child.Children);
                                    }
                                }
                                continue;
                            }
                            if (resultCache.TryGetValue(child, out var childResult))
                            {
                                allFlattenedChildren.AddRange(childResult);
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
                        if (allFlattenedChildren.Count == 0 && node.TypeKind is not MetaTypeKind.Composite)
                        {
                            // 无可见子节点 -> 隐藏当前节点
                            node.IsVisible = false;
                            resultCache[node] = [];
                        }
                        else if (allFlattenedChildren.Count == 1)
                        {
                            var only = allFlattenedChildren[0];
                            bool isDefinitionItem = IsDefinitionItem(only.FeatureMap);
                            if(isDefinitionItem)
                            {
                                continue;
                            }

                            if (!IsContainerType(only.TypeKind) && only.ID != "placeHolder")
                            {
                                // 唯一子节点是基本类型 -> 将当前节点提升为该基本类型
                                if (node.Children?.Count == 1 || node.Children is null)
                                {
                                    node.TypeKind = only.TypeKind;
                                    node.Children = null;
                                }
                                node.Value = only.Value;
                                node.EnumOptionList = only.EnumOptionList;
                                node.IsFalse = only.IsFalse;
                                node.IsTrue = only.IsTrue;
                                // 保留所有上下文属性（FieldName, IsRequired, FeatureMap 等）
                                resultCache[node] = [node];
                            }
                            else if(only.ID != "placeHolder")
                            {
                                // 唯一子节点是容器类型 -> 将当前节点替换为那个容器，接管其子树
                                if (node.Children?.Count == 1 || node.Children is null)
                                {
                                    node.TypeKind = only.TypeKind;
                                    node.Children = only.Children;
                                }
                                node.Value = only.Value;
                                node.EnumOptionList = only.EnumOptionList;
                                // 保留所有上下文属性（FieldName, IsRequired, FeatureMap 等）
                                resultCache[node] = [node];
                            }
                        }
                        // allFlattenedChildren.Count > 1
                        else if(node.TypeKind is not MetaTypeKind.Composite)
                        {
                            // 只有canonical节点才会进入此分支（Union/Generic 多子时 shouldFlatten 为 false）
                            // 完全消除别名节点，将其子节点列表直接交给父级
                            node.IsVisible = false; // 本身不可见，父级不会保留它
                            node.Children = new(allFlattenedChildren); // 更新 Children 供外部调用取用
                            resultCache[node] = allFlattenedChildren;
                        }
                    }
                }
            }

            return resultCache.GetValueOrDefault(root, []);
        }

        /// <summary>
        /// 判断是否为容器类型
        /// </summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        public static bool IsContainerType(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.Struct
                or MetaTypeKind.List
                or MetaTypeKind.Dispatch
                or MetaTypeKind.ByteArray
                or MetaTypeKind.IntArray
                or MetaTypeKind.LongArray
                or MetaTypeKind.Composite;
        }

        /// <summary>
        /// 判断是否为间接类型
        /// </summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        public static bool IsIndirectType(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.Union
                or MetaTypeKind.Generic
                or MetaTypeKind.Reference
                or MetaTypeKind.Literal;
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
            List<string> formalParams,
            List<string> actualArgs,
            string version)
        {
            var paramMap = new Dictionary<string, string>();
            for (int i = 0; i < formalParams.Count && i < actualArgs.Count; i++)
            {
                paramMap[formalParams[i]] = actualArgs[i];
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

                    if (pair.Value is MetaTypeEditorFieldDTO actualStruct)
                    {
                        var replacedChild = InstantiateDTO(actualStruct, version);
                        replacedChild.FieldName = child.FieldName;
                        replacedChild.IsRequired = child.IsRequired;
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
                            replacedChild.Value = child.Value ?? GetPrimitiveDefaultValue(actualStruct.TypeKind);
                        }

                        child = replacedChild;

                        //无论容器还是值，统统阻断迭代器的后续下钻
                        skipDrillDown = true;
                    }
                    else
                    {
                        child.TypeName = actualName;
                        if (Enum.TryParse<MetaTypeKind>(actualName, true, out var parsedKind))
                        {
                            child.TypeKind = parsedKind;
                            child.Value = GetPrimitiveDefaultValue(parsedKind);
                        }
                        else
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
        /// 为基础类型赋初值，防止 UI 绑定报错
        /// </summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        private static object GetPrimitiveDefaultValue(MetaTypeKind kind) => kind switch
        {
            MetaTypeKind.Boolean => false,
            MetaTypeKind.Byte or MetaTypeKind.Short or MetaTypeKind.Int => 0,
            MetaTypeKind.Long => 0L,
            MetaTypeKind.Float => 0.0f,
            MetaTypeKind.Double => 0.0,
            _ => ""
        };

        public static bool IsDefinitionItem(Dictionary<string, MetaValue> featureMap)
        {
            bool result = false;
            if (featureMap is not null && featureMap.Count > 0)
            {
                var targetTuplePairList = featureMap.Where(item => item.Value.Kind is MetaValueKind.Tuple);
                List<List<MetaNamedValue>> targetMemberList = [.. targetTuplePairList.Select(item => item.Value.Members)];
                for (int i = 0; i < targetMemberList.Count; i++)
                {
                    //有definition的同时不能有registry，那么就是需要用户手写的定义类节点
                    result = !targetMemberList[i].Any(item => item.Name == "registry" && item.Value?.TypeValue?.LiteralValue is not null) && targetMemberList[i].Any(item => item.Name == "definition" && item.Value?.TypeValue?.LiteralValue is not null && item.Value.TypeValue.LiteralValue.ToString() == "True");
                    if(result)
                    {
                        break;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 校验版本：根据 since/until 约束和用户目标版本决定节点是否可见。
        /// since 为包含下限（≥），until 为排除上限（&lt;）。
        /// 用户版本可以是单版本 "1.20.5" 或范围 "1.20-1.21"。
        /// </summary>
        public static void VerifyVersion(List<MetaTypeEditorFieldDTO> targetDTOList, string version)
        {
            for (int i = 0; i < targetDTOList.Count; i++)
            {
                MetaTypeEditorFieldDTO dto = targetDTOList[i];
                dto.IsVisible = false;

                //从FeatureMap提取since/until版本约束
                Version sinceVersion = TryParseFeatureVersion(dto.FeatureMap, "since");
                Version untilVersion = TryParseFeatureVersion(dto.FeatureMap, "until");

                //无任何版本约束则始终可见
                if (sinceVersion is null && untilVersion is null)
                {
                    dto.IsVisible = true;
                    continue;
                }

                //用户版本是范围（如"1.20-1.21"）：区间重叠即视为可见
                if (version.Contains('-'))
                {
                    string[] parts = version.Split('-');
                    if (parts.Length == 2
                        && Version.TryParse(parts[0], out Version rangeLeft)
                        && Version.TryParse(parts[1], out Version rangeRight))
                    {
                        dto.IsVisible = IsVersionRangeOverlapping(rangeLeft, rangeRight, sinceVersion, untilVersion);
                    }
                }
                else if (Version.TryParse(version, out Version fullVersion))
                {
                    dto.IsVisible = IsVersionInRange(fullVersion, sinceVersion, untilVersion);
                }
            }
        }

        /// <summary>
        /// 从FeatureMap提取"since"或"until"版本值
        /// </summary>
        private static Version TryParseFeatureVersion(Dictionary<string, MetaValue> featureMap, string key)
        {
            if (featureMap is not null
                && featureMap.TryGetValue(key, out MetaValue metaValue)
                && metaValue?.TypeValue?.LiteralValue is not null
                && Version.TryParse(metaValue.TypeValue.LiteralValue.ToString(), out Version parsed))
            {
                return parsed;
            }
            return null;
        }

        /// <summary>
        ///判断单版本是否落在[since, until)区间内。
        ///since为包含（≥），until为排除（&lt;）。
        /// </summary>
        private static bool IsVersionInRange(Version target, Version since, Version until)
        {
            if (since is { } sinceVer && CompareVersions(target, sinceVer) < 0)
                return false;

            if (until is { } untilVer && CompareVersions(target, untilVer) >= 0)
                return false;

            return true;
        }

        /// <summary>
        ///判断用户版本范围[rangeLeft,rangeRight]是否与字段约束[since,until)有交集。
        /// </summary>
        private static bool IsVersionRangeOverlapping(Version rangeLeft, Version rangeRight, Version since, Version until)
        {
            // 用户区间完全在 until 之后则无交集
            if (until is { } untilVer && CompareVersions(rangeLeft, untilVer) >= 0)
                return false;

            // 用户区间完全在 since 之前则无交集
            if (since is { } sinceVer && CompareVersions(rangeRight, sinceVer) < 0)
                return false;

            return true;
        }

        /// <summary>
        /// 逐段比较两个Version，BuildResource/Revision=-1视为"未指定"（等同0参与比较）。
        /// </summary>
        private static int CompareVersions(Version a, Version b)
        {
            int cmp = a.Major.CompareTo(b.Major);
            if (cmp != 0) return cmp;

            cmp = a.Minor.CompareTo(b.Minor);
            if (cmp != 0) return cmp;

            int aBuild = a.Build == -1 ? 0 : a.Build;
            int bBuild = b.Build == -1 ? 0 : b.Build;
            cmp = aBuild.CompareTo(bBuild);
            if (cmp != 0) return cmp;

            int aRev = a.Revision == -1 ? 0 : a.Revision;
            int bRev = b.Revision == -1 ? 0 : b.Revision;
            return aRev.CompareTo(bRev);
        }

        /// <summary>
        /// 辅助：根据类型返回默认值
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        private static object GetDefaultValue(MetaTypeEditorFieldDTO dto) => dto.TypeKind switch
        {
            MetaTypeKind.Boolean => false,
            MetaTypeKind.Byte => (byte)0,
            MetaTypeKind.Short => (short)0,
            MetaTypeKind.Int => 0,
            MetaTypeKind.Long => 0L,
            MetaTypeKind.Float => 0.0f,
            MetaTypeKind.Double => 0.0,
            MetaTypeKind.String => string.Empty,
            MetaTypeKind.Enum => dto.SelectedEnumOption?.Value?.TypeValue.LiteralValue?.ToString(),
            MetaTypeKind.Union => dto.SelectedUnionTypeName.Value?.TypeValue?.LiteralValue?.ToString(),      // Union 值不在 Value 字段体现
            MetaTypeKind.Struct => null,
            MetaTypeKind.Dispatch => null,
            MetaTypeKind.Reference => dto.Value?.ToString() ?? "",
            MetaTypeKind.IntArray or MetaTypeKind.ByteArray or MetaTypeKind.LongArray => null,
            MetaTypeKind.List => null,
            MetaTypeKind.Literal => dto.Value?.ToString(),
            _ => string.Empty
        };
        #endregion
    }
}
