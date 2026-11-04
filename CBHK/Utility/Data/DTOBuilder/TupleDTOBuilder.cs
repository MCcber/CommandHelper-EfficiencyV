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
    /// <summary>
    /// 计算键元组（[K]: V）的构建策略：生成 Key 选择器与添加按钮，值结构在按下添加时按选中 Key 求值。
    /// </summary>
    public class TupleDTOBuilder(Resource resource, MCDocumentMetaTypeDTOHelper helper, DocumentDTOBuildStrategyRegistry registry) : IDocumentDTOBuildStrategy
    {
        #region Field
        private readonly Resource resource = resource;
        private readonly MCDocumentMetaTypeDTOHelper helper = helper;
        private readonly DocumentDTOBuildStrategyRegistry registry = registry;
        #endregion

        #region Method
        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, DocumentPath documentPath, Dictionary<string, KeyValueAnchors> anchorMap, RenderDepth depth, string typeName = "")
        {
            if (target.Children is null || target.Children.Count < 2)
            {
                return;
            }

            #region 解析键枚举
            //裸 string 键没有枚举：键改由用户自由输入，标记出来走下面"文本输入 + 加号"的分支
            bool isTextKey = false;
            MetaTypeEditorFieldDTO keyElement = target.Children[0];
            MetaTypeEditorFieldDTO keyEnum = keyElement.TypeKind is MetaTypeKind.Enum
                ? keyElement
                : CompoundGenericMetaValueParser.Parse(resource, version, keyElement.FeatureMap);
            if (keyEnum?.EnumOptionList is null && helper.TryResolveNamedKeyEnum(keyElement, target.Path ?? documentPath))
            {
                //具名枚举键（[Direction]、[MobCategory] 等）：借目标枚举的选项当键。
                //跨文件 use 别名在构建期可能还没注册，构建收尾的 ResolveDeferredNamedKeys 会再补一次
                keyEnum = keyElement;
            }
            if (keyEnum?.EnumOptionList is null)
            {
                //非枚举键（裸 string / 数字等值类型，如 lang/atlas 与 dispatcher_key 形态的动态 map）：
                //键由用户自由输入，给"值编辑器 + 加号"，不强行枚举（容器/间接类型还是认不出来，放弃）
                if (MCDocumentMetaTypeDTOHelper.IsContainerType(keyElement.TypeKind)
                    || MCDocumentMetaTypeDTOHelper.IsIndirectType(keyElement.TypeKind))
                {
                    return;
                }
                isTextKey = true;
            }

            ObservableCollection<MetaTypeEditorFieldDTO> keyItems;
            if (isTextKey)
            {
                keyElement.FieldName = "";
                keyElement.IsVisible = true;
                keyItems = [];
            }
            else
            {
                //键选择器不接受空成员
                for (int i = keyEnum.EnumOptionList.Count - 1; i >= 0; i--)
                {
                    if (keyEnum.EnumOptionList[i].Name == "- unset -")
                    {
                        keyEnum.EnumOptionList.RemoveAt(i);
                    }
                }
                if (keyEnum.EnumOptionList.Count == 0)
                {
                    return;
                }

                keyEnum.FieldName = "";
                keyEnum.IsVisible = true;
                keyEnum.SelectedEnumOption = keyEnum.EnumOptionList[0];
                keyItems = [keyEnum];
            }

            MetaTypeEditorFieldDTO keySelector = new()
            {
                ID = Guid.NewGuid().ToString(),
                TypeKind = MetaTypeKind.Composite,
                Path = target.Path ?? documentPath,
                Parent = target,
                SelectedUnionChildren = []
            };
            MetaTypeEditorFieldDTO addButton = MCDocumentMetaTypeDTOHelper.BuildAddButton(keySelector, target.Path ?? documentPath);
            //id/注册表键的枚举是"另建"的节点（Named 键才与 keyElement 同一实例）：
            //行内区必须挂枚举那个，挂回原始 String 的话界面上就是一行自由输入框
            MetaTypeEditorFieldDTO keyView = isTextKey ? keyElement : keyEnum ?? keyElement;
            keySelector.Items = [keyView, addButton];
            keyView.Parent = keySelector;
            #endregion

            #region 值结构按选中键求值
            MetaTypeEditorFieldDTO valueTemplate = target.Children[1];
            addButton.AddItemCommand = new RelayCommand(() =>
            {
                string selectedKeyName;
                if (isTextKey)
                {
                    //自由输入的键：空键不产出内容（"该填键"的提示交给 UI）
                    selectedKeyName = keyElement.Value?.ToString() ?? "";
                    if (string.IsNullOrEmpty(selectedKeyName))
                    {
                        return;
                    }
                }
                else
                {
                    //键停在未设置位时不产出内容：这里只保证不炸（"该设置键"的提示交给 UI）
                    if (string.IsNullOrEmpty(keyEnum.SelectedEnumOption?.Name) || keyEnum.SelectedEnumOption.Name == "- unset -")
                    {
                        return;
                    }
                    selectedKeyName = keyEnum.SelectedEnumOption.Name;
                }
                MetaTypeEditorFieldDTO valueNode = ResolveValueStructure(valueTemplate, keyItems, target.BindingScope, version, target.Path ?? documentPath, anchorMap, depth, typeName);
                //定义键（[#[criterion(definition=true)] string] 这类）是用户自定的名字，直接当条目名；
                //其余自由输入的键（如纹理 id）才补命名空间。前缀要留在命名空间外面：!minecraft:xxx
                string entryName = selectedKeyName;
                if (!MCDocumentMetaTypeDTOHelper.IsDefinitionItem(keyElement.FeatureMap))
                {
                    int keyPrefixLength = 0;
                    while (keyPrefixLength < selectedKeyName.Length && !char.IsLetterOrDigit(selectedKeyName[keyPrefixLength]))
                    {
                        keyPrefixLength++;
                    }
                    entryName = selectedKeyName[..keyPrefixLength] + "minecraft:" + selectedKeyName[keyPrefixLength..];
                }
                AttachValueStructure(keySelector, valueNode, entryName, version);
            });
            #endregion

            //外壳只承载键选择器，本身不需要字段名
            target.FieldName = "";
            target.TypeKind = MetaTypeKind.Struct;
            target.Children = [keySelector];
        }

        /// <summary>
        /// 按当前选中的键求值元组的值模板，返回可直接挂到容器上的值结构。
        /// </summary>
        private MetaTypeEditorFieldDTO ResolveValueStructure(
            MetaTypeEditorFieldDTO valueTemplate,
            ObservableCollection<MetaTypeEditorFieldDTO> keyItems,
            TypeBindingScope bindingScope,
            string version,
            DocumentPath documentPath,
            Dictionary<string, KeyValueAnchors> anchorMap,
            RenderDepth depth,
            string typeName)
        {
            MetaTypeEditorFieldDTO valueNode = helper.InstantiateDTO(valueTemplate, version);
            valueNode.FieldName = "";
            valueNode.TypeName = null;
            valueNode.BindingScope ??= bindingScope;
            valueNode.Path = valueTemplate.Path ?? documentPath;
            AttachKeyItems(valueNode, keyItems);
            AttachPath(valueNode, valueNode.Path);
            PrepareDispatchFieldNames(valueNode);
            MCDocumentResourceBuilder.BaseDataHandler(valueNode);
            //验证路径会先走一次 BuildResource 再交给构建策略，加号路径同样要走：
            //否则裸调度器（如 map 的值写成 dispatch resource[[%key]]）没有地方解析出目标结构。
            //Literal / Reference 承载的是类型名（定义键的值常写成 AdvancementCriterion 这种名字），
            //漏掉它们的话会被 LiteralDTOBuilder 当字面量处理，变成一个只有类型名字符串的单选项枚举。
            //联合/结构值仍由各自的构建器负责，避免把未选中的分支提前物化成多余子行。
            if (valueNode.TypeKind is MetaTypeKind.Dispatch or MetaTypeKind.Literal or MetaTypeKind.Reference)
            {
                MCDocumentResourceBuilder.BuildResource(valueNode, valueNode, version, valueNode.Path ?? documentPath, resource, helper);
            }
            registry.Get(valueNode.TypeKind).Build(valueNode, valueNode, version, valueNode.Path ?? documentPath, anchorMap, depth, typeName);
            //拷贝形态的嵌套联合没跑过构建器：这里逐节点补跑，名称列表重建、行内选择器与未设置态才与正常验证路径一致
            Stack<MetaTypeEditorFieldDTO> pending = new([valueNode]);
            while (pending.Count > 0)
            {
                MetaTypeEditorFieldDTO node = pending.Pop();
                if (node.TypeKind is MetaTypeKind.Union)
                {
                    registry.Get(MetaTypeKind.Union).Build(node, node, version, node.Path ?? documentPath, anchorMap, depth, typeName);
                }
                //已经解析出结果、不再是调度器的节点要把键枚举摘掉：留着会被行内区渲染成一个多余的下拉框
                if (node.TypeKind is not MetaTypeKind.Dispatch && node.Items is not null)
                {
                    if (ReferenceEquals(node.Items, keyItems))
                    {
                        //整个集合就是键集合（调度器物化后沿用）：摘引用，不能往里删，否则其它还在等解析的调度器也没键可用了
                        node.Items = null;
                    }
                    else
                    {
                        for (int i = node.Items.Count - 1; i >= 0; i--)
                        {
                            if (keyItems.Contains(node.Items[i]))
                            {
                                node.Items.RemoveAt(i);
                            }
                        }
                    }
                }
                foreach (MetaTypeEditorFieldDTO child in node.Children ?? []) pending.Push(child);
                foreach (MetaTypeEditorFieldDTO child in node.SelectedUnionChildren ?? []) pending.Push(child);
            }
            MCDocumentMetaTypeDTOHelper.AttachParentLinks(valueNode);
            return valueNode;
        }

        /// <summary>
        /// 把求值结果组装成入口节点：入口自身是 Composite（行内区放 Value/Modifier 选择器），
        /// 值结构自带行内宿主时把它拍平进入口，选中内容再交给统一切换落点摆放。
        /// </summary>
        private void AttachValueStructure(MetaTypeEditorFieldDTO field, MetaTypeEditorFieldDTO valueNode, string entryName, string version)
        {
            //没有子级的值（空结构体 struct {}、叶子组件如 float/字符串数组）同样要挂出条目：
            //直接放弃会让这种键在界面上加不出来
            if (valueNode is null)
            {
                return;
            }

            MetaTypeEditorFieldDTO entry = new()
            {
                ID = Guid.NewGuid().ToString(),
                TypeKind = MetaTypeKind.Composite,
                FieldName = entryName,
                Path = valueNode.Path,
                Parent = field,
                IsInterpretFromDispatch = true
            };
            valueNode.Parent = entry;
            //行内选择器由自身模板显示字段名（选中的 Index 成员）
            valueNode.FieldName = entryName;
            MetaTypeEditorFieldDTO inlineSelector = valueNode.Items?.FirstOrDefault(item => item.TypeKind is MetaTypeKind.Union);
            //Composite 的行内区首位是删除按钮
            MetaTypeEditorFieldDTO removeButton = new()
            {
                ID = "placeHolder",
                TypeKind = MetaTypeKind.Remove,
                Parent = entry,
                RemoveItemCommand = helper.CreateRemoveListItemCommand(field, entry)
            };

            if (inlineSelector is not null)
            {
                //入口自己就是行内区：把值结构的行内项搬上来，否则行内的选择器/编辑器要多套一层，
                //而这层宿主只能渲染行内项、它自己的子行没有地方显示
                entry.Items = [removeButton, .. valueNode.Items];
                foreach (MetaTypeEditorFieldDTO item in entry.Items)
                {
                    item.Parent = entry;
                }
                valueNode.Items.Clear();
                entry.SelectedUnionChildren = valueNode.SelectedUnionChildren ?? [];

                //分支表里的 %key 直接钉成条目自己的键：条目建立后再重解释（切换分支）不能用键选择器
                //当前选中项，否则会把别的键的类型参数搬进来（如 argument 拿到别的键的结构）
                string entryKey = entryName.StartsWith("minecraft:") ? entryName["minecraft:".Length..] : entryName;
                Stack<MetaTypeEditorFieldDTO> pendingKeyNode = new([.. entry.Items]);
                while (pendingKeyNode.Count > 0)
                {
                    MetaTypeEditorFieldDTO keyNode = pendingKeyNode.Pop();
                    if (keyNode.FeatureMap?.TryGetValue("Index", out MetaValue keyIndex) == true
                        && keyIndex?.LiteralValue?.ToString() == "%key")
                    {
                        keyNode.FeatureMap["Index"] = new MetaValue { Kind = MetaValueKind.Literal, LiteralValue = entryKey };
                    }
                    foreach (MetaTypeEditorFieldDTO child in keyNode.Children ?? []) pendingKeyNode.Push(child);
                    foreach (MetaTypeEditorFieldDTO child in keyNode.SelectedUnionChildren ?? []) pendingKeyNode.Push(child);
                    foreach (MetaTypeEditorFieldDTO child in keyNode.Items ?? []) pendingKeyNode.Push(child);
                }

                //选中内容交给统一切换落点：值/列表内容进入口行内区，容器子行挂入口子级。
                //值结构是拷贝出来的，行内选择器可能没接过切换事件，这里补上：
                //落点会按"行内选择器提升到宿主"的规则把内容摆到入口上，之后用户切换分支也走这条路
                inlineSelector.FieldName = entryName;
                inlineSelector.SelectedUnionItemUpdated ??= () => helper.SelectedUnionItemUpdated(inlineSelector, version);
                inlineSelector.SelectedUnionItemUpdated?.Invoke();
            }
            else
            {
                //空容器值（struct {}）不进行内区、也不再套一层空节点，条目本身就是它的形态
                bool isContainerValue = valueNode.TypeKind is MetaTypeKind.Struct or MetaTypeKind.Composite
                    or MetaTypeKind.Union or MetaTypeKind.List or MetaTypeKind.Dispatch or MetaTypeKind.Any;
                entry.Items = isContainerValue && (valueNode.Children is null || valueNode.Children.Count == 0)
                    ? [removeButton]
                    : [removeButton, valueNode];
                //没有行内宿主（结构值）时值结构的字段就是入口的子级。
                //联合的子级是分支表（含未选中的分支），只能拿已选内容当子行，否则另一个分支（如 modifier）会跟着显示
                entry.SelectedUnionChildren ??= [];
                IEnumerable<MetaTypeEditorFieldDTO> entryContent = valueNode.TypeKind is MetaTypeKind.Union
                    ? valueNode.SelectedUnionChildren
                    : valueNode.SelectedUnionChildren is { Count: > 0 }
                        ? valueNode.SelectedUnionChildren
                        : valueNode.Children;
                foreach (MetaTypeEditorFieldDTO child in entryContent ?? [])
                {
                    child.Parent = entry;
                    entry.SelectedUnionChildren.Add(child);
                            child.Parent = entry;
                }
                //值本身就是行内选择器（联合）时也要触发一次落点：并表与行归属都在那里完成，
                //不然要等用户手动切一次分支才会生效
                if (valueNode.TypeKind is MetaTypeKind.Union)
                {
                    valueNode.SelectedUnionItemUpdated ??= () => helper.SelectedUnionItemUpdated(valueNode, version);
                    valueNode.SelectedUnionItemUpdated?.Invoke();
                    entry.SelectedUnionChildren = valueNode.SelectedUnionChildren ?? [];
                    foreach (MetaTypeEditorFieldDTO child in entry.SelectedUnionChildren)
                    {
                        child.Parent = entry;
                    }
                }
            }

            //追加新的入口节点，已有的添加结果要保留
            field.SelectedUnionChildren ??= [];
            field.SelectedUnionChildren.Add(entry);
                            entry.Parent = field;
        }

        /// <summary>
        /// 把键集合挂到值子树上，使分支里的 %key 能解析出当前选中项。
        /// 键枚举只服务于 %key 求值：只有调度器节点需要它，挂到可渲染节点上会跟着行内区一起显示出来。
        /// </summary>
        private static void AttachKeyItems(MetaTypeEditorFieldDTO node, ObservableCollection<MetaTypeEditorFieldDTO> keyItems)
        {
            if (node.TypeKind is MetaTypeKind.Dispatch)
            {
                node.Items ??= keyItems;
            }

            if (node.Children is not null)
            {
                foreach (MetaTypeEditorFieldDTO child in node.Children)
                {
                    AttachKeyItems(child, keyItems);
                }
            }

            if (node.SelectedUnionChildren is not null)
            {
                foreach (MetaTypeEditorFieldDTO child in node.SelectedUnionChildren)
                {
                    AttachKeyItems(child, keyItems);
                }
            }
        }

        /// <summary>
        /// 递归补齐子树路径，保证验证器与引用解析能定位资源。
        /// </summary>
        private static void AttachPath(MetaTypeEditorFieldDTO node, DocumentPath path)
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

            if (node.Items is not null)
            {
                foreach (MetaTypeEditorFieldDTO child in node.Items)
                {
                    AttachPath(child, node.Path);
                }
            }
        }

        /// <summary>
        /// 调度器节点没有显示字段名时用访问器名作字段名，避免被当成空节点隐藏。
        /// </summary>
        private static void PrepareDispatchFieldNames(MetaTypeEditorFieldDTO node)
        {
            if (node.TypeKind is MetaTypeKind.Dispatch
                && string.IsNullOrEmpty(node.FieldName)
                && node.FeatureMap.TryGetValue("Accessor", out MetaValue accessor)
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

            if (node.Items is not null)
            {
                foreach (MetaTypeEditorFieldDTO child in node.Items)
                {
                    PrepareDispatchFieldNames(child);
                }
            }
        }

        public bool CanHandle(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.Tuple;
        }
        #endregion
    }
}

