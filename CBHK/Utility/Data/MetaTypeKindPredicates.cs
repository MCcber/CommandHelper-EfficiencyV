using MinecraftLanguageModelLibrary.Data;

namespace CBHK.Utility.Data
{
    /// <summary>
    /// MetaTypeKind 的类型谓词工具（自 MCDocumentMetaTypeDTOHelper 抽出）
    /// </summary>
    public static class MetaTypeKindPredicates
    {
        /// <summary>
        /// 判断是否为容器类型
        /// </summary>
        public static bool IsContainerType(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.Struct
                or MetaTypeKind.List
                or MetaTypeKind.Dispatch
                or MetaTypeKind.ByteArray
                or MetaTypeKind.IntArray
                or MetaTypeKind.LongArray
                or MetaTypeKind.Composite
                or MetaTypeKind.Entry;
        }

        /// <summary>
        /// 判断是否为泛引用类型
        /// </summary>
        public static bool IsIndirectType(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.Union
                or MetaTypeKind.Generic
                or MetaTypeKind.Reference
                or MetaTypeKind.Literal;
        }
    }
}
