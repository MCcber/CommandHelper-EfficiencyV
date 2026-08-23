using CBHK.Model.Data;
using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;

namespace CBHK.Utility.Data
{
    public sealed record DTOInstanceContext(List<MetaTypeEditorFieldDTO> dtoInstanceList,Dictionary<string, KeyValueAnchors> anchorMap);
}
