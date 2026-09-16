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
                or MetaTypeKind.Composite;
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

        public static bool IsListOrArrayOrValueType(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.List
                or MetaTypeKind.UUIDArray
                or MetaTypeKind.LongArray
                or MetaTypeKind.IntArray
                or MetaTypeKind.ByteArray
                or MetaTypeKind.CompositeARGB
                or MetaTypeKind.CompositeRGB
                or MetaTypeKind.DecRGB
                or MetaTypeKind.DecRGBA
                or MetaTypeKind.HexARGB
                or MetaTypeKind.HexRGB
                or MetaTypeKind.Byte
                or MetaTypeKind.Int
                or MetaTypeKind.Boolean
                or MetaTypeKind.Short
                or MetaTypeKind.Float
                or MetaTypeKind.Double
                or MetaTypeKind.Long
                or MetaTypeKind.String;
        }
    }
}
