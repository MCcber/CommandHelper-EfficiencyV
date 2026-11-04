using System.Windows;
using System.Windows.Controls;
using CBHK.Utility.Data.Mcdoc;

namespace CBHK.Utility.Data
{
    /// <summary>
    /// 新 mcdoc 求值管线的模板选择器：按求值后的 McdocTypeKind 分派。
    /// 与 MCDocumentTreeViewTemplateSelector（旧 DTO 管线）并列存在，互不影响；
    /// 复用的是 TreeViewCommon.xaml 里同一批 DataTemplate —— 前提是 McdocTreeNode 提供了同名属性。
    /// </summary>
    public class McdocTreeViewTemplateSelector : DataTemplateSelector
    {
        public DataTemplate StructTemplate { get; set; }
        public DataTemplate UnionTemplate { get; set; }
        public DataTemplate ListTemplate { get; set; }
        public DataTemplate ArrayTemplate { get; set; }
        public DataTemplate EnumTemplate { get; set; }
        public DataTemplate BoolTemplate { get; set; }
        public DataTemplate NumberTemplate { get; set; }
        public DataTemplate StringTemplate { get; set; }
        /// <summary>动态键行（键可编辑 + 增/删）。</summary>
        public DataTemplate DynamicKeyTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is McdocTreeNode node)
            {
                // 动态键行（map 的条目 / 新增占位）优先用键可编辑的模板
                if (node.IsDynamicKey && DynamicKeyTemplate is not null)
                {
                    return DynamicKeyTemplate;
                }
                // 带 #[id] 的字符串节点（如 advancement 的 parent）在 UI 上当下拉用
                if (node.UseSelect && EnumTemplate is not null)
                {
                    return EnumTemplate;
                }
                return node.Type?.Kind switch
                {
                    McdocTypeKind.Struct => StructTemplate,
                    McdocTypeKind.Union => UnionTemplate,
                    McdocTypeKind.List => ListTemplate,
                    McdocTypeKind.ByteArray or McdocTypeKind.IntArray or McdocTypeKind.LongArray => ArrayTemplate,
                    McdocTypeKind.Tuple => ArrayTemplate,
                    McdocTypeKind.Enum => EnumTemplate,
                    McdocTypeKind.Boolean => BoolTemplate,
                    McdocTypeKind.Byte or McdocTypeKind.Short or McdocTypeKind.Int
                        or McdocTypeKind.Long or McdocTypeKind.Float or McdocTypeKind.Double => NumberTemplate,
                    _ => StringTemplate
                };
            }
            return base.SelectTemplate(item, container);
        }
    }
}
