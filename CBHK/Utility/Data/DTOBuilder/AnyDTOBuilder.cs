using CBHK.Interface.Data;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;
using System.Text;

namespace CBHK.Utility.Data.DTOBuilder
{
    public class AnyDTOBuilder(Resource resource, MCDocumentMetaTypeDTOHelper helper, DocumentDTOBuildStrategyRegistry registry) : IDocumentDTOBuildStrategy
    {
        #region Field
        private readonly Resource resource = resource;
        private readonly MCDocumentMetaTypeDTOHelper helper = helper;
        private readonly DocumentDTOBuildStrategyRegistry registry = registry;
        #endregion

        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, DocumentPath documentPath, Dictionary<string, KeyValueAnchors> anchorMap, bool justSetView = false, string typeName = "")
        {
            #region 为可选枚举补齐未设置成员
            if (target.TypeKind is MetaTypeKind.Enum)
            {
                target.SelectedEnumItemUpdated = () => helper.SelectedEnumItemUpdated(target, version);
                if (!target.IsRequired && target.EnumOptionList?.Count > 0 && target.EnumOptionList[0].Name != "- unset -")
                {
                    target.EnumOptionList.Insert(0, new EnumMember() { Name = "- unset -", Value = new MetaValue() { Kind = MetaValueKind.Literal, LiteralValue = "unset" } });
                    target.SelectedEnumOption = target.EnumOptionList[0];
                    target.SelectedEnumItemIndex = 0;
                }
            }
            #endregion
        }

        public bool CanHandle(MetaTypeKind kind)
        {
            return true;
        }
    }
}
