using CBHK.Model.Data;
using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;

namespace CBHK.Interface.Data
{
    public interface IDocumentDTOBuildStrategy
    {
        void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template,
                   string version, DocumentPath documentItemPath,
                   Dictionary<string, KeyValueAnchors> anchorMap,
                   bool justSetView = false,
                   string typeName = "");
    }
}
