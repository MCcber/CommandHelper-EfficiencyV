namespace CBHK.Domain
{
    /// <summary>
    /// 命令语法里的闭合枚举。这些既不是 Minecraft 注册表，也不在 Resource\Registries 里，
    /// 由 mcdoc 资产库解析指定结构得到；库里确实没有的项由实现给出原版兜底值。
    /// </summary>
    public interface ICatalogEnumProvider
    {
        /// <summary>物品槽位编号（armor.head / hotbar.0 …），来源 java\util\slot.mcdoc</summary>
        IReadOnlyList<string> ItemSlots { get; }

        /// <summary>选择器参数名（x / distance / nbt …），来源 java\util\mod.mcdoc 的选择器结构</summary>
        IReadOnlyList<string> SelectorParameters { get; }

        /// <summary>选择器参数名 → 取值域表达式</summary>
        IReadOnlyDictionary<string, string> SelectorParameterValues { get; }

        IReadOnlyList<string> TeamColors { get; }

        IReadOnlyList<string> BossbarColors { get; }

        IReadOnlyList<string> BossbarStyles { get; }

        IReadOnlyList<string> ScoreboardTypes { get; }

        IReadOnlyList<string> ScoreboardCustomIds { get; }
    }
}
