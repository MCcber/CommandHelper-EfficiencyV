using System.Windows;
using System.Windows.Controls;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>行模板选择器：只按"行的形状"分派，控件种类交给值编辑器选择器，两者正交。</summary>
    public sealed class McdocRowTemplateSelector : DataTemplateSelector
    {
        public DataTemplate PlainRow { get; set; }
        public DataTemplate UnionRow { get; set; }
        public DataTemplate KeyedRow { get; set; }
        public DataTemplate PlaceholderRow { get; set; }
        public DataTemplate MoreRow { get; set; }
        public DataTemplate AddTailRow { get; set; }

        /// <summary>保留区容器行（"当前版本不存在（N）"）：没有编辑器、没有动作按钮，只有展开箭头。</summary>
        public DataTemplate PreservedRow { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
            => item is McdocRow row
                ? row.Kind switch
                {
                    McdocRowKind.Union => UnionRow,
                    McdocRowKind.Keyed => KeyedRow,
                    McdocRowKind.Placeholder => PlaceholderRow,
                    McdocRowKind.More => MoreRow,
                    McdocRowKind.AddTail => AddTailRow,
                    // 探针/预览没给保留区模板时回落普通行：行形状绝不能让模板选择器抛异常
                    McdocRowKind.Preserved => PreservedRow ?? PlainRow,
                    _ => PlainRow,
                }
                : null;
    }

    /// <summary>值编辑器选择器：只认 Head.EditorKind，不看类型（misode 各 *Head 的编辑控件分派）。</summary>
    public sealed class McdocValueEditorTemplateSelector : DataTemplateSelector
    {
        public DataTemplate EmptyEditor { get; set; }
        public DataTemplate TextEditor { get; set; }
        public DataTemplate NumberEditor { get; set; }
        public DataTemplate BoolEditor { get; set; }
        public DataTemplate EnumEditor { get; set; }
        public DataTemplate RawEditor { get; set; }
        public DataTemplate InlineTupleEditor { get; set; }
        public DataTemplate Preview { get; set; }
        public DataTemplate ColorEditor { get; set; }
        public DataTemplate ColorPickerEditor { get; set; }
        public DataTemplate IconSelectEditor { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
            => item is McdocRow
                ? Preview
                : item is McdocTreeNode node
                ? node.Head.IsColorPicker
                    ? ColorPickerEditor
                : node.Head.IsIconSelect
                    ? IconSelectEditor
                : node.Head.ColorKind.Length > 0
                    ? ColorEditor
                : node.Head.EditorKind switch
                {
                    McdocEditorKind.Text => TextEditor,
                    McdocEditorKind.Number => NumberEditor,
                    McdocEditorKind.Bool => BoolEditor,
                    McdocEditorKind.Enum => EnumEditor,
                    McdocEditorKind.Raw => RawEditor,
                    McdocEditorKind.InlineTuple => InlineTupleEditor,
                    _ => EmptyEditor,
                }
                : null;
    }
}
