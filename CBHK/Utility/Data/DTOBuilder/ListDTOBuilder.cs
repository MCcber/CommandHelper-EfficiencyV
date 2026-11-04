using CBHK.Interface.Data;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;

namespace CBHK.Utility.Data.DTOBuilder
{
    public class ListDTOBuilder(Resource resource, MCDocumentMetaTypeDTOHelper helper, DocumentDTOBuildStrategyRegistry registry) : IDocumentDTOBuildStrategy
    {
        #region Field
        private readonly Resource resource = resource;
        private readonly MCDocumentMetaTypeDTOHelper helper = helper;
        private readonly DocumentDTOBuildStrategyRegistry registry = registry;
        #endregion

        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, DocumentPath documentPath, Dictionary<string, KeyValueAnchors> anchorMap, RenderDepth depth, string typeName = "")
        {
            if (documentPath is not null)
            {
                target.Path = new(documentPath.TargetPath);
            }
            target.Items ??= [];
            //只有"精确长度"（@ 3，上下界相同）且元素是值类型时才按声明补出元素；
            //最小长度（@ 1..，只有下界）或容器/间接元素（[Text]）都不预置：
            //预置出来的只是没展开的壳，最低数量交给 UI 提示，增删按钮照给
            bool hasExactLength = false;
            string exactLengthText = null;
            if (target.FeatureMap is not null
                && target.FeatureMap.TryGetValue("LengthFrom", out MetaValue lengthFromValue)
                && target.FeatureMap.TryGetValue("LengthTo", out MetaValue lengthToValue)
                && lengthFromValue?.LiteralValue?.ToString() == lengthToValue?.LiteralValue?.ToString())
            {
                hasExactLength = true;
                exactLengthText = lengthFromValue.LiteralValue?.ToString();
            }
            bool canPresetItem = hasExactLength && target.ElementType is not null
                && !MetaTypeKindPredicates.IsContainerType(target.ElementType.TypeKind)
                && !MetaTypeKindPredicates.IsIndirectType(target.ElementType.TypeKind);
            if (target.Items.Count == 0 && canPresetItem && int.TryParse(exactLengthText, out int length))
            {
                for (int i = 0; i < length; i++)
                {
                    MetaTypeEditorFieldDTO item = helper.InstantiateDTO(target.ElementType, version);
                    item.FieldName = "";
                    item.Parent = target;
                    target.Items.Add(item);
                }
            }
            else
            {
                target.AddItemCommand = helper.CreateAddListItemCommand(target, version);
                target.RemoveItemCommand = helper.CreateClearListItemCommand(target);
            }
        }

        public bool CanHandle(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.List;
        }
    }
}
