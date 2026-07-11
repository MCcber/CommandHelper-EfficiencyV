using CBHK.Interface.Data;
using CBHK.Model.Constant;
using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;

namespace CBHK.Utility.Data.DTOBuilder
{
    public class DocumentDTOBuildStrategyRegistry
    {
        private readonly Dictionary<MetaTypeKind, IDocumentDTOBuildStrategy> _strategies = [];

        public void Register(MetaTypeKind kind, IDocumentDTOBuildStrategy strategy)
            => _strategies[kind] = strategy;

        public IDocumentDTOBuildStrategy Get(MetaTypeKind kind)
            => _strategies.TryGetValue(kind, out var s) ? s : _strategies[MetaTypeKind.Any];

        private DocumentDTOBuildStrategyRegistry() { }

        /// <summary>
        /// 分配构造策略
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="helper"></param>
        /// <returns></returns>
        public static DocumentDTOBuildStrategyRegistry Create(Resource resource, MCDocumentMetaTypeDTOHelper helper)
        {
            DocumentDTOBuildStrategyRegistry registry = new();

            registry.Register(MetaTypeKind.Struct, new StructDTOBuilder(resource, helper, registry));
            registry.Register(MetaTypeKind.Union, new UnionDTOBuilder(resource, helper, registry));
            registry.Register(MetaTypeKind.Literal, new LiteralDTOBuilder(resource, helper, registry));
            registry.Register(MetaTypeKind.ByteArray, new ArrayDTOBuilder(resource, helper, registry));
            registry.Register(MetaTypeKind.IntArray, new ArrayDTOBuilder(resource, helper, registry));
            registry.Register(MetaTypeKind.LongArray, new ArrayDTOBuilder(resource, helper, registry));
            registry.Register(MetaTypeKind.UUIDArray, new ArrayDTOBuilder(resource, helper, registry));
            registry.Register(MetaTypeKind.List, new ListDTOBuilder(resource, helper, registry));
            registry.Register(MetaTypeKind.Generic, new GenericDTOBuilder(resource, helper, registry));
            registry.Register(MetaTypeKind.Reference, new ReferenceDTOBuilder(resource, helper, registry));
            registry.Register(MetaTypeKind.Dispatch, new DispatchDTOBuilder(resource, helper, registry));
            registry.Register(MetaTypeKind.Any, new AnyDTOBuilder(resource, helper, registry));

            return registry;
        }
    }
}
