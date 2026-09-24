using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;
using System.Linq;

namespace CBHK.Utility.Data
{
    /// <summary>
    /// 判断一个节点是否应该使用 Composite 形态。
    /// 只有同一行确实需要多个交互控件时才返回 true，避免把简单节点复杂化。
    /// </summary>
    public static class CompositePolicy
    {
        /// <summary>
        /// 业务规则：Composite 表示同一行需要多个交互控件。
        /// 至少需要一个选择器（Enum / Union / List）和一个值编辑器，
        /// 否则保持普通 Struct / Union / Enum / Value 形态，避免把简单节点复杂化。
        /// </summary>
        public static bool ShouldUseComposite(IEnumerable<MetaTypeEditorFieldDTO>? items)
        {
            if (items is null)
            {
                return false;
            }

            List<MetaTypeEditorFieldDTO> meaningfulItems = [.. items.Where(item =>
                item is not null
                && item.ID != "placeHolder"
                && item.TypeKind is not (MetaTypeKind.Add or MetaTypeKind.Remove))];

            if (meaningfulItems.Count < 2)
            {
                return false;
            }

            bool hasSelector = meaningfulItems.Any(item => IsSelector(item.TypeKind));
            bool hasValueEditor = meaningfulItems.Any(item => IsValueEditor(item.TypeKind));

            return hasSelector && hasValueEditor;
        }

        private static bool IsSelector(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.Enum or MetaTypeKind.Union or MetaTypeKind.List;
        }

        private static bool IsValueEditor(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.Byte
                or MetaTypeKind.Short
                or MetaTypeKind.Int
                or MetaTypeKind.Long
                or MetaTypeKind.Float
                or MetaTypeKind.Double
                or MetaTypeKind.String
                or MetaTypeKind.Boolean
                or MetaTypeKind.Struct
                or MetaTypeKind.Dispatch
                or MetaTypeKind.Union
                or MetaTypeKind.List
                or MetaTypeKind.Composite
                or MetaTypeKind.Literal;
        }
    }
}
