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

    public enum MetaTypeDTOFeatureType
    {
        /// <summary>
        /// 资源类型
        /// </summary>
        id,
        /// <summary>
        /// 默认成员
        /// </summary>
        canonical,
        /// <summary>
        /// 实体唯一标识符
        /// </summary>
        uuid,
        /// <summary>
        /// 资源注册类型
        /// </summary>
        register,
        /// <summary>
        /// 不包括的成员
        /// </summary>
        exclude,
        /// <summary>
        /// 包含成员
        /// </summary>
        include,
        /// <summary>
        /// DEC颜色类别
        /// </summary>
        dec_rgb,
        /// <summary>
        /// Composite颜色类别
        /// </summary>
        composite_rgb,
        /// <summary>
        /// HEX颜色类别
        /// </summary>
        hex_rgb,
        /// <summary>
        /// 命令
        /// </summary>
        command,
        /// <summary>
        /// 已过时
        /// </summary>
        deprecated,
        /// <summary>
        /// 调度器键
        /// </summary>
        dispatcher_key,
        /// <summary>
        /// 被指定的数字整除
        /// </summary>
        divisible_by,
        /// <summary>
        /// 实体类别
        /// </summary>
        entity,
        /// <summary>
        /// 游戏规则
        /// </summary>
        game_rule,
        /// <summary>
        /// 匹配正则表达式
        /// </summary>
        match_regex,
        /// <summary>
        /// SNBT
        /// </summary>
        nbt,
        /// <summary>
        /// SNBT路径
        /// </summary>
        nbt_path,
        /// <summary>
        /// 记分板变量名
        /// </summary>
        objective,
        /// <summary>
        /// 包含正则表达式的字符串
        /// </summary>
        regex_pattern,
        /// <summary>
        /// 分数持有者
        /// </summary>
        score_holder,
        /// <summary>
        /// 从指定版本起
        /// </summary>
        since,
        /// <summary>
        /// 标签名
        /// </summary>
        tag,
        /// <summary>
        /// 队伍名
        /// </summary>
        team,
        /// <summary>
        /// 文本组件
        /// </summary>
        text_component,
        /// <summary>
        /// 直到指定版本
        /// </summary>
        until
    }

    public enum MetaTypeDTOFeatureCommandOption
    {
        /// <summary>
        /// 允许斜杠
        /// </summary>
        slash,
        /// <summary>
        /// 允许为空
        /// </summary>
        empty,
        /// <summary>
        /// 最长长度
        /// </summary>
        max_length,
        /// <summary>
        /// 包含宏表达式
        /// </summary>
        macro
    }

    public enum MetaTypeDTOFeatureEntityOption
    {
        /// <summary>
        /// 实体数量
        /// </summary>
        amount,
        /// <summary>
        /// 实体类型
        /// </summary>
        type
    }
}
