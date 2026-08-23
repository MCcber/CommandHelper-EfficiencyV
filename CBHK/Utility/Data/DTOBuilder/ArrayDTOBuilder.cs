using CBHK.Interface.Data;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using MinecraftLanguageModelLibrary.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CBHK.Utility.Data.DTOBuilder
{
    public class ArrayDTOBuilder(Resource resource, MCDocumentMetaTypeDTOHelper helper, DocumentDTOBuildStrategyRegistry registry) : IDocumentDTOBuildStrategy
    {
        #region Field
        private readonly Resource resource = resource;
        private readonly MCDocumentMetaTypeDTOHelper helper = helper;
        private readonly DocumentDTOBuildStrategyRegistry registry = registry;
        #endregion

        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, DocumentPath documentPath, Dictionary<string, KeyValueAnchors> anchorMap, bool justSetView = false, string typeName = "")
        {
            //必选数组仅占位，由后续懒加载处理
            if (target.IsRequired)
            {
                target.ID = "placeHolder";
                return;
            }
            //没元素则直接返回
            if (target.ElementType is null)
            {
                target.Items =
                    [
                        new()
                        {
                            ID = "",
                            TypeKind = MetaTypeKind.Any,
                            FieldName = "Error,No Element Template."
                        }
                    ];
                return;
            }

            target.Path = new(documentPath.TargetPath.ToString());
            target.Items ??= [];
            // 将现有的子节点列表传给递归方法
            var elementTypeCopy = helper.InstantiateDTO(target.ElementType, version);

            var elementRegistry = registry.Get(elementTypeCopy.TypeKind);
            elementRegistry.Build(elementTypeCopy, target.ElementType, version, documentPath, anchorMap, justSetView);
            if ((target.Items.Count > 0 && target.Items[0].ID != "placeHolder") || target.Items.Count == 0)
            {
                target.Items = [elementTypeCopy];
            }
        }

        public bool CanHandle(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.ByteArray or MetaTypeKind.IntArray or MetaTypeKind.LongArray or MetaTypeKind.UUIDArray;
        }
    }
}
