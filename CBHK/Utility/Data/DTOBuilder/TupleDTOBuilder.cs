using CBHK.Interface.Data;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using CommunityToolkit.Mvvm.Input;
using MinecraftLanguageModelLibrary.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
            MetaTypeEditorFieldDTO keyElement = target.Children[0];
            MetaTypeEditorFieldDTO keyEnum = keyElement.TypeKind is MetaTypeKind.Enum
                ? keyElement
                : CompoundGenericMetaValueParser.Parse(resource, version, keyElement.FeatureMap);
            if (keyEnum?.EnumOptionList is null)
            {
                return;
            }

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
            ObservableCollection<MetaTypeEditorFieldDTO> keyItems = [keyEnum];

            MetaTypeEditorFieldDTO keySelector = new()
            {
                ID = Guid.NewGuid().ToString(),
                TypeKind = MetaTypeKind.Composite,
                Path = target.Path ?? documentPath,
                Parent = target,
                SelectedUnionChildren = []
            };
            MetaTypeEditorFieldDTO addButton = MCDocumentMetaTypeDTOHelper.BuildAddButton(keySelector, target.Path ?? documentPath);
            keySelector.Items = [keyEnum, addButton];
            keyEnum.Parent = keySelector;
            #endregion

            #region 值结构按选中键求值
            MetaTypeEditorFieldDTO valueTemplate = target.Children[1];
            addButton.AddItemCommand = new RelayCommand(() =>
            {
                MetaTypeEditorFieldDTO valueNode = ResolveValueStructure(valueTemplate, keyItems, target.BindingScope, version, target.Path ?? documentPath, anchorMap, depth, typeName);
                AttachValueStructure(keySelector, valueNode, "minecraft:" + keyEnum.SelectedEnumOption?.Name);
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
            valueNode.Items ??= keyItems;
            AttachPath(valueNode, valueNode.Path);
            PrepareDispatchFieldNames(valueNode);
            MCDocumentResourceBuilder.BaseDataHandler(valueNode);
            registry.Get(valueNode.TypeKind).Build(valueNode, valueNode, version, valueNode.Path ?? documentPath, anchorMap, depth, typeName);
            return valueNode;
        }

        /// <summary>
        /// 把求值结果组装成入口节点：入口自身是 Composite（行内区放 Value/Modifier 选择器），
        /// 选中索引成员作为入口字段名，选中分支的结构挂到入口的子级。
        /// </summary>
        private void AttachValueStructure(MetaTypeEditorFieldDTO field, MetaTypeEditorFieldDTO valueNode, string entryName)
        {
            if (valueNode?.Children is null || valueNode.Children.Count == 0)
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
            //Composite 的行内区首位是删除按钮
            MetaTypeEditorFieldDTO removeButton = new()
            {
                ID = "placeHolder",
                TypeKind = MetaTypeKind.Remove,
                Parent = entry,
                RemoveItemCommand = helper.CreateRemoveListItemCommand(field, entry)
            };
            entry.Items = [removeButton, valueNode];

            entry.SelectedUnionChildren ??= [];
            //取有内容的那一份，避免选中空集合导致入口节点没有子级
            IEnumerable<MetaTypeEditorFieldDTO> entryContent = valueNode.SelectedUnionChildren is { Count: > 0 }
                ? valueNode.SelectedUnionChildren
                : valueNode.Children;
            foreach (MetaTypeEditorFieldDTO child in entryContent)
            {
                child.Parent = entry;
                entry.SelectedUnionChildren.Add(child);
            }

            field.SelectedUnionChildren ??= [];
            field.SelectedUnionChildren.Clear();
            field.SelectedUnionChildren.Add(entry);
        }

        /// <summary>
        /// 把键集合挂到值子树上，使分支里的 %key 能解析出当前选中项。
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
