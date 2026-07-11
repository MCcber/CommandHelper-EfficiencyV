using CBHK.Model.Data;
using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;
using System.Text;

namespace CBHK.Interface.Data
{
    public interface IDocumentDTOBuildStrategy
    {
        bool CanHandle(MetaTypeKind kind);
        void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template,
                   string version, StringBuilder docPath,
                   Dictionary<string, KeyValueAnchors> anchorMap);
    }
}
