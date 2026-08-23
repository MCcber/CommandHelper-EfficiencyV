using CBHK.Interface.Data;
using CBHK.Model.Constant;
using CBHK.Utility.Data.DTOBuilder;
using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;

namespace CBHK.Utility.Data
{
    public class DocumentDTOBuildStrategyRegistry
    {
        private readonly Dictionary<MetaTypeKind, IDocumentDTOBuildStrategy> _strategies = [];

        public void Register(MetaTypeKind kind, IDocumentDTOBuildStrategy strategy)
            => _strategies[kind] = strategy;

        public IDocumentDTOBuildStrategy Get(MetaTypeKind kind)
            => _strategies.TryGetValue(kind, out var s) ? s : _strategies[MetaTypeKind.Any];

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
            // 注意：Dispatch 不注册策略。调度器在树中作为驻留锚点，
            // 子树由 Helper 的 GetDispatchResource / SelectedEnumItemUpdated 根据上下文动态生成。
            // 因此 registry.Get(Dispatch) 会走 Any 兜底（无操作），与原空壳 DispatchDTOBuilder 行为一致。
            registry.Register(MetaTypeKind.Any, new AnyDTOBuilder(resource, helper, registry));

            return registry;
        }
    }
}
