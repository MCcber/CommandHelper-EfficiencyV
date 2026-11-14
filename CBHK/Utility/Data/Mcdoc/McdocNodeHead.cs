using System.Collections.Generic;
using MinecraftLanguageModelLibrary.Data;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>行的形状（misode 的 Head 分派结果），视图只认这个枚举，不看类型。</summary>
    public enum McdocRowKind { Plain, Union, Keyed, Placeholder, More, AddTail }

    /// <summary>值编辑器的种类（misode 各 *Head 的编辑控件），模板选择器只认这个枚举。</summary>
    public enum McdocEditorKind { None, Text, Number, Bool, Enum, Raw, InlineTuple }

    /// <summary>
    /// 一行的"头"：标签、文档、行形状、编辑器种类、候选值。misode 里 Head/Key/Errors 三个组件的信息合集，
    /// 由 presenter 产出，视图层与投影层不再接触 McdocTypeKind。
    /// </summary>
    public sealed class McdocNodeHead
    {
        public string Label { get; init; } = "";
        public string Documentation { get; init; } = "";
        public McdocRowKind RowKind { get; init; }
        public McdocEditorKind EditorKind { get; init; }
        /// <summary>闭合下拉（misode 白名单内的 `<select>`：只能选候选）。</summary>
        public bool IsSelect { get; init; }

        /// <summary>
        /// 可输入候选之外的值（misode 白名单外的 `<input list=datalist>`）。
        /// true 时 UI 用"可编辑下拉"，Options/EnumOptionList 仍是候选；与 IsSelect 互斥。
        /// </summary>
        public bool AllowCustomValue { get; init; }
        public IReadOnlyList<EnumMember> Options { get; init; }
        public IReadOnlyList<EnumMember> BranchOptions { get; init; }
        public string Watermark { get; init; } = "";

        /// <summary>列表头/尾的 ⊕ 是否可用（受 LengthRange.Max 约束）。</summary>
        public bool CanAddTop { get; init; }
        public bool CanAddBottom { get; init; }
        public bool CanSetDefault { get; init; }
        public bool IsInlineTuple { get; init; }
        public bool IsRawKey { get; init; }
        public bool CanUnset { get; init; }

        /// <summary>hex 颜色字段：用 VectorColorPicker。</summary>
        public bool IsColorPicker { get; init; }

        /// <summary>其它颜色形态（named / dec_rgb / composite_*）：按字符串常量枚举处理。</summary>
        public bool IsColorEnum { get; init; }

        /// <summary>物品/方块 id 字段：用带图标的下拉框。</summary>
        public bool IsIconSelect { get; init; }

        /// <summary>misode getCategory 的分类（pool / predicate / ...），供行左侧色条使用。</summary>
        public string Category { get; init; } = "";

        /// <summary>任意 JSON 行的推断类型短名（misode AnyBody 的 selectAnyType）。</summary>
        public string TypeHint { get; init; } = "";

        /// <summary>#[color] 的形态（hex_rgb / dec_rgb / composite_*），空表示不是颜色字段。</summary>
        public string ColorKind { get; init; } = "";

        /// <summary>行上的诊断（未解析引用 / 求值失败），对应 misode 的 Errors。</summary>
        public IReadOnlyList<string> Diagnostics { get; init; }

        /// <summary>
        /// 复制一份并替换几个能力位：属性是 init-only，presenter 覆写 Head 时不能就地改。
        /// </summary>
        public McdocNodeHead With(bool? canAddTop = null, bool? canAddBottom = null) => new()
        {
            Label = Label,
            Documentation = Documentation,
            RowKind = RowKind,
            EditorKind = EditorKind,
            IsSelect = IsSelect,
            AllowCustomValue = AllowCustomValue,
            Options = Options,
            BranchOptions = BranchOptions,
            Watermark = Watermark,
            CanAddTop = canAddTop ?? CanAddTop,
            CanAddBottom = canAddBottom ?? CanAddBottom,
            CanSetDefault = CanSetDefault,
            IsInlineTuple = IsInlineTuple,
            IsRawKey = IsRawKey,
            CanUnset = CanUnset,
            IsColorPicker = IsColorPicker,
            IsColorEnum = IsColorEnum,
            IsIconSelect = IsIconSelect,
            Category = Category,
            TypeHint = TypeHint,
            ColorKind = ColorKind,
            Diagnostics = Diagnostics,
        };
    }
}
