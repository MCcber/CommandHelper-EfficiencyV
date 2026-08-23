using CBHK.Interface.Data;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;
using System.Text;

namespace CBHK.Utility.Data.DTOBuilder
{
    public class LiteralDTOBuilder(Resource resource, MCDocumentMetaTypeDTOHelper helper, DocumentDTOBuildStrategyRegistry registry) : IDocumentDTOBuildStrategy
    {
        #region Field
        private readonly Resource resource = resource;
        private readonly MCDocumentMetaTypeDTOHelper helper = helper;
        private readonly DocumentDTOBuildStrategyRegistry registry = registry;
        #endregion

        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, DocumentPath documentPath, Dictionary<string, KeyValueAnchors> anchorMap, bool justSetView = false, string typeName = "")
        {
            string currentValueString = target.Value?.ToString() ?? "";

            if (!string.IsNullOrEmpty(currentValueString))
            {
                target.TypeKind = MetaTypeKind.Enum;
                target.EnumOptionList ??= [];
                if (!target.IsRequired)
                {
                    target.EnumOptionList = [new EnumMember() { Name = "- unset- ", Value = new MetaValue { LiteralValue = "unset" } }];
                }
                target.EnumOptionList.Add(new EnumMember() { Name = currentValueString, Value = new MetaValue { LiteralValue = currentValueString } });
                target.SelectedEnumOption = target.EnumOptionList[0];
            }
        }

        public bool CanHandle(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.Literal;
        }
    }
}
