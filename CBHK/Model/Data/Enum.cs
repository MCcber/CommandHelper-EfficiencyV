using System.ComponentModel;

namespace CBHK.Model.Data
{
    public enum WindowVisualType
    {
        [Description("普通")]
        Default,
        [Description("亚克力")]
        Acrylic,
        [Description("云母")]
        Mica,
        [Description("云母Alt")]
        MicaAlt
    }

    public enum WindowThemeType
    {
        [Description("命令方块橙")]
        CommandBlockOrange,
        [Description("命令方块绿")]
        CommandBlockBlueGreen,
        [Description("命令方块紫")]
        CommandBlockPurple,
        [Description("自定义")]
        Custom
    }

    public enum WindowCornerPreference
    {
        Default = 0,    // 系统默认
        DoNotRound = 1, // 强制方形
        Round = 2,      // 强制圆角（常用）
        RoundSmall = 3  // 强制小圆角
    }

    public enum LanuchState
    {
        Visible,
        Hidden
    }

    public enum ModifyType
    {
        Remove,
        Get
    }

    public enum ModiferType
    {
        Range,
        Length
    }

    public enum MoveDirection
    {
        Up,
        Down
    }

    public enum ColorModifyMode
    {
        Darken,
        Lighten
    }

    public enum ChangeType
    {
        None,
        NumberAndBool,
        String,
        AddCompoundObject,
        AddListElement,
        AddListElementToEnd,
        RemoveCompound,
        RemoveList,
        RemoveListElement,
    }

    public enum DataType
    {
        None,
        Object,
        Number,
        Bool,
        String,
        Byte,
        Decimal,
        Short,
        Int,
        Float,
        Double,
        Long
    }

    public enum ItemType
    {
        BottomButton,
        Enum,
        Compound,
        CustomCompound,
        OptionalCompound,
        MultiType,
        Array,
        List
    }

    public enum RecipeType
    {
        CraftingTable,
        Furnace,
        BlastFurnace,
        Campfire,
        SmithingTable,
        Smoker,
        Stonecutter
    }

    public enum PackDescriptionType
    {
        StringType,
        BoolType,
        IntType,
        ObjectType,
        ArrayType
    };

    public enum InterpolationType : byte
    {
        Linear,
        Discrete,
        CubicBezier,
        Quadratic
    }

    public enum KeyFrameValueType
    {
        // 数值
        Byte,
        Short,
        Int,
        Float,
        Double,
        Long,
        UnsignedInt,
        UnsignedFloat,
        UnsignedLong,
        // 字符串句柄
        StringId,
        // 布尔
        Boolean
    }

    public enum NumberType
    {
        Byte,
        Short,
        Int,
        Float,
        Double,
        Long,
        UnsignedInt,
        UnsignedFloat,
        UnsignedLong,
        Decimal
    }

    /// <summary>
    /// JSON 节点类型，对应 JSON 的结构或值。
    /// </summary>
    public enum JsonNodeType
    {
        Object,
        Array,
        Property,   // 键值对中的键
        String,
        Number,
        True,
        False,
        Null
    }
}
