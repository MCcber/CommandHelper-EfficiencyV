// mcdoc 类型模型 —— 用"单类 + Kind 枚举"表达 spyglass 的 McdocType 判别联合。
// 取舍：spyglass 用 18 个 interface 表达各 kind，C# 里改成 1 个类 + 枚举 + 载荷字段，
// 好处是序列化无需多态标注、跨越 语言服务器/客户端 边界只传一种形状。
namespace CBHK.Utility.Data.Mcdoc
{
    public enum McdocTypeKind
    {
        Any, Unsafe, Boolean, String,
        Byte, Short, Int, Long, Float, Double,
        Literal, Enum, Struct, Union, List,
        ByteArray, IntArray, LongArray, Tuple,
        Reference, Dispatcher, Indexed, Concrete, Template, Mapped
    }

    public enum McdocLiteralKind { Boolean, String, Byte, Short, Int, Long, Float, Double }

    /// <summary>数值区间。Exclusive 标记对应 spyglass 的 RangeKind 位标记。</summary>
    public sealed class McdocRange
    {
        public bool MinExclusive;
        public bool MaxExclusive;
        public decimal? Min;
        public decimal? Max;

        public static McdocRange Exact(decimal value) => new() { Min = value, Max = value };

        public bool Contains(decimal value)
            => (Min == null || (MinExclusive ? value > Min.Value : value >= Min.Value))
            && (Max == null || (MaxExclusive ? value < Max.Value : value <= Max.Value));
    }

    /// <summary>#[name] / #[name=value] / #[name{...}]</summary>
    public sealed class McdocAttribute
    {
        public string Name = "";
        public McdocAttrValue Value;
    }

    /// <summary>属性值：字面量、树、或一个类型表达式（三者互斥）。</summary>
    public sealed class McdocAttrValue
    {
        public object Literal;
        public System.Collections.Generic.Dictionary<string, McdocAttrValue> Tree;
        public McdocType Type;

        public static McdocAttrValue Of(object literal) => new() { Literal = literal };
        public string AsString() => Literal == null ? null : System.Convert.ToString(Literal);
    }

    public enum McdocIndexKind { Static, Dynamic }

    /// <summary>Dispatcher/Indexed 的平行索引项。Dynamic 时 Accessor 存名字或 %parent/%key/%none。</summary>
    public sealed class McdocIndex
    {
        public McdocIndexKind Kind;
        public string Value;
        public System.Collections.Generic.List<string> Accessor;

        public static McdocIndex Static(string value) => new() { Kind = McdocIndexKind.Static, Value = value };
        public static McdocIndex Dynamic(System.Collections.Generic.List<string> accessor)
            => new() { Kind = McdocIndexKind.Dynamic, Accessor = accessor };
    }

    /// <summary>struct 字段。Name 非空表示字面量 key；否则 Key 是动态 key 的类型。</summary>
    public sealed class McdocStructField
    {
        public string Name;
        public McdocType Key;
        public McdocType Type = new();
        public bool Optional;
        public bool Deprecated;
        public bool IsSpread;
        public string Desc;
        public System.Collections.Generic.List<McdocAttribute> Attributes;

        public McdocStructField Clone() => (McdocStructField)MemberwiseClone();
    }

    public sealed class McdocEnumValue
    {
        public string Identifier = "";
        public object Value;
        public string Desc;
        public System.Collections.Generic.List<McdocAttribute> Attributes;
    }

    /// <summary>
    /// 一个类承载所有 mcdoc kind。使用哪个载荷字段由 Kind 决定（见各自注释）。
    /// Simplify 之后的不变式：Struct.Fields[i].Key 恒为 LiteralType（字面量字符串）、Union.Members 不再嵌套 Union。
    /// </summary>
    public sealed class McdocType
    {
        public McdocTypeKind Kind;
        public System.Collections.Generic.List<McdocAttribute> Attributes;

        /// <summary>Numeric/String/PrimitiveArray 的取值范围。</summary>
        public McdocRange Range;
        /// <summary>String/List/PrimitiveArray 的长度范围。</summary>
        public McdocRange LengthRange;

        /// <summary>Struct 字段。</summary>
        public System.Collections.Generic.List<McdocStructField> Fields;
        /// <summary>Union 成员。</summary>
        public System.Collections.Generic.List<McdocType> Members;
        /// <summary>List.Item / Indexed|Concrete|Template|Mapped.Child。</summary>
        public McdocType Item;
        /// <summary>Tuple 元素 / Concrete 类型实参。</summary>
        public System.Collections.Generic.List<McdocType> Items;
        /// <summary>Template 形参路径。</summary>
        public System.Collections.Generic.List<string> TypeParams;

        /// <summary>求值期解析出的 #[id] 候选（注册表或调度器索引成员）。presenter 只读这份，不再回查数据源。</summary>
        public System.Collections.Generic.IReadOnlyList<string> IdMembers;

        /// <summary>候选来自哪个空间（Registry / Dispatcher / None），用于诊断与断言。</summary>
        public McdocIdSpaceSource IdSource;
        /// <summary>Mapped 的形参→类型映射。</summary>
        public System.Collections.Generic.Dictionary<string, McdocType> Mapping;
        /// <summary>Reference 路径。</summary>
        public string Path;
        /// <summary>该类型声明所在的模块路径（如 ::java::data::worldgen::feature），供相对名解析用。</summary>
        public string Module;
        /// <summary>Dispatcher 注册表 id。</summary>
        public string Registry;
        /// <summary>Dispatcher / Indexed 的平行索引。</summary>
        public System.Collections.Generic.List<McdocIndex> Indices;

        /// <summary>Literal 的值种类。</summary>
        public McdocLiteralKind LiteralKind;
        /// <summary>Literal 的值（string / bool / decimal）。</summary>
        public object Value;
        /// <summary>Enum 的底层类型；null 表示非法枚举。</summary>
        public McdocTypeKind? EnumKind;
        /// <summary>Enum 成员。</summary>
        public System.Collections.Generic.List<McdocEnumValue> EnumValues;

        /// <summary>
        /// 求值指令：必须在求值期消掉，不许出现在节点上（渲染层只认归一形态）。
        /// Reference/Concrete/Template/Mapped/Indexed/Dispatcher 都是"怎么算"，不是"算出来是什么"。
        /// </summary>
        public bool IsDirective => Kind is McdocTypeKind.Reference or McdocTypeKind.Concrete
            or McdocTypeKind.Template or McdocTypeKind.Mapped or McdocTypeKind.Indexed
            or McdocTypeKind.Dispatcher;

        public static McdocType Create(McdocTypeKind kind) => new() { Kind = kind };
        public static McdocType Any() => new() { Kind = McdocTypeKind.Any };
        public static McdocType LiteralString(string value)
            => new() { Kind = McdocTypeKind.Literal, LiteralKind = McdocLiteralKind.String, Value = value };
        public static McdocType EmptyUnion() => new() { Kind = McdocTypeKind.Union, Members = [] };

        public bool IsNumeric => Kind is McdocTypeKind.Byte or McdocTypeKind.Short or McdocTypeKind.Int
            or McdocTypeKind.Long or McdocTypeKind.Float or McdocTypeKind.Double;
        public bool IsPrimitiveArray => Kind is McdocTypeKind.ByteArray or McdocTypeKind.IntArray or McdocTypeKind.LongArray;
        public bool IsListLike => Kind is McdocTypeKind.List || IsPrimitiveArray;

        public string LiteralText() => Value == null ? "" : System.Convert.ToString(Value);

        /// <summary>浅拷贝（等价 TS 的对象展开 {...typeDef}）。</summary>
        public McdocType Clone() => (McdocType)MemberwiseClone();
    }
}
