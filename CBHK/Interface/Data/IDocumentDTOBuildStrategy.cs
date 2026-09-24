using CBHK.Model.Data;
using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;

namespace CBHK.Interface.Data
{
    /// <summary>
    /// 渲染深度：Shallow 只出当前层，Deep 递归展开子级
    /// </summary>
    public enum RenderDepth
    {
        Shallow,
        Deep
    }

    public interface IDocumentDTOBuildStrategy
    {
        void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template,
                   string version, DocumentPath documentItemPath,
                   Dictionary<string, KeyValueAnchors> anchorMap,
                   RenderDepth depth,
                   string typeName = "");
    }
}
