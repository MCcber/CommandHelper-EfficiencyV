using CBHK.Interface.Data;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using MinecraftLanguageModelLibrary.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace CBHK.Utility.Data.DTOBuilder
{
    public class ListDTOBuilder(Resource resource, MCDocumentMetaTypeDTOHelper helper, DocumentDTOBuildStrategyRegistry registry) : IDocumentDTOBuildStrategy
    {
        #region Field
        private readonly Resource resource = resource;
        private readonly MCDocumentMetaTypeDTOHelper helper = helper;
        private readonly DocumentDTOBuildStrategyRegistry registry = registry;
        #endregion

        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, StringBuilder documentItemPath, Dictionary<string, KeyValueAnchors> anchorMap)
        {
            target.DocumentItemPath = new(documentItemPath.ToString());
            target.AddItemCommand = helper.CreateAddItemCommand(target, version);
            target.RemoveItemCommand = helper.CreateRemoveItemCommand(target);
            target.Items ??= [];
        }

        public bool CanHandle(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.List;
        }
    }
}
