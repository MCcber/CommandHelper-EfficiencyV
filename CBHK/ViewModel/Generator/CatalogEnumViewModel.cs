using CBHK.Domain;
using CBHK.Model.Constant;
using CBHK.Utility.Data;
using CBHK.Utility.Data.Mcdoc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CBHK.ViewModel.Generator
{
    /// <summary>
    /// 命令语法闭合枚举的 mcdoc 侧实现：能在本地 mcdoc 资产库里查到闭合集合的项走符号索引，
    /// 库里确实没有的项从 SupplementalData.json 取（注册表与 mcdoc 都没有的才写在那里）。入口名 <c>catalog_enum</c> 没有对应的 dispatcher，基类因此只建一份空文档，
    /// 本类不依赖那棵树 —— 只借它的会话（符号表 + 版本下拉 + #[since]/#[until] 口径）。
    /// </summary>
    public sealed class CatalogEnumViewModel(Resource resource, SupplementDataService supplements)
        : McdocGeneratorViewModelBase(resource, EntryName), ICatalogEnumProvider
    {
        #region Constant
        /// <summary>入口名：没有同名 dispatcher，基类落到空文档（取值全由本类解析）。</summary>
        private const string EntryName = "catalog_enum";

        /// <summary>槽位名的闭合集合：java\util\slot.mcdoc 的 enum(string) EquipmentSlot。</summary>
        private const string EquipmentSlotTypePath = "::java::util::slot::EquipmentSlot";

        /// <summary>
        /// 装备槽位组 → 该组接受的槽位名。组的划分是命令语法事实（armor 不收 mainhand），
        /// 槽位名本身以枚举为准，所以这里只做"组 × 枚举"的笛卡尔配对。
        /// </summary>
        private static readonly (string Group, string[] Slots)[] EquipmentSlotGroups =
        [
            ("armor", ["head", "chest", "legs", "feet", "body"]),
            ("weapon", ["mainhand", "offhand"]),
            ("horse", ["armor", "saddle"]),
        ];
        #endregion

        #region Field
        private readonly Resource catalogResource = resource;
        /// <summary>库里没有的那些项的唯一来源：注册表与 mcdoc 都没有的才写在 SupplementalData.json 里。</summary>
        private readonly SupplementDataService supplements = supplements;
        private readonly object gate = new();
        private bool resolved;
        private IReadOnlyList<string> itemSlots;
        #endregion

        #region Property
        /// <summary>
        /// 是否真的从 mcdoc 符号索引里抽到了槽位集合。false 表示符号表还没就绪
        /// （Resource.Init 是异步的，236 个 mcdoc 走命名管道），此时整份清单退回原版兜底。
        /// </summary>
        public bool ExtractedFromMcdoc { get; private set; }

        public IReadOnlyList<string> ItemSlots { get { EnsureResolved(); return itemSlots; } }

        public IReadOnlyList<string> SelectorParameters => supplements.GetValues("selector_parameter");

        public IReadOnlyDictionary<string, string> SelectorParameterValues => supplements.GetMap("selector_parameter_value");

        public IReadOnlyList<string> TeamColors => supplements.GetValues("team_color");

        public IReadOnlyList<string> BossbarColors => supplements.GetValues("bossbar_color");

        public IReadOnlyList<string> BossbarStyles => supplements.GetValues("bossbar_style");

        public IReadOnlyList<string> ScoreboardTypes => supplements.GetValues("scoreboard_type");

        /// <summary>由注册表 custom_stat 提供，补充文件里不重复一份。</summary>
        public IReadOnlyList<string> ScoreboardCustomIds => [];
        #endregion

        #region Method
        /// <summary>
        /// 槽位清单只在第一次访问时定住：符号表是异步就绪的，反复重取会让同一份下拉在两次调用之间变内容。
        /// 要按新版本重取，构造一个新实例。
        /// </summary>
        private void EnsureResolved()
        {
            lock (gate)
            {
                if (resolved) return;
                resolved = true;
                var slots = ResolveEnumValues(catalogResource?.McdocTypes, EquipmentSlotTypePath,
                    SelectedGameVersionOption?.Name);
                ExtractedFromMcdoc = slots.Count > 0;
                itemSlots = slots.Count > 0
                    ? OverlayEquipmentSlots(slots)
                    : supplements.GetValues("item_slot_supplement");
            }
        }

        /// <summary>
        /// 用枚举结果替换兜底清单里的装备槽位段：<c>armor.* / weapon.* / horse.*</c> 由枚举 + #[since]/#[until] 决定
        /// （1.20.5 才有 armor.body、1.21.5 才有 horse.saddle），其余段（hotbar.0 / container.0 …）库里没有，保留兜底。
        /// </summary>
        private IReadOnlyList<string> OverlayEquipmentSlots(IReadOnlyList<string> equipmentSlots)
        {
            var prefixes = EquipmentSlotGroups.Select(group => group.Group + ".").ToArray();
            var result = new List<string>();
            // 装备槽位段整段由枚举产出（顺序沿用 arm/weapon/horse 三组，条目按枚举 + 版本门控）
            foreach (var (group, slots) in EquipmentSlotGroups)
            {
                foreach (var slot in slots)
                {
                    if (equipmentSlots.Contains(slot, StringComparer.Ordinal)) result.Add(group + "." + slot);
                }
            }
            // 兜底里剩下的段照原顺序接在后面；同名"组.槽位名"已被上面覆盖，只保留数值型（horse.0 这类）
            foreach (var entry in supplements.GetValues("item_slot_supplement"))
            {
                var prefix = prefixes.FirstOrDefault(item => entry.StartsWith(item, StringComparison.Ordinal));
                if (prefix is not null && !int.TryParse(entry.AsSpan(prefix.Length), out _)) continue;
                result.Add(entry);
            }
            return result;
        }

        /// <summary>
        /// 类型路径 → 闭合枚举值。这是管线里唯一一条"不建树也能拿到候选"的路：
        /// <see cref="IMcdocSymbols.GetTypeDef"/> 取定义，值在 <see cref="McdocType.EnumValues"/>。
        /// 只对 Kind == Enum 成立 —— #[id] / #[dispatcherkey] 那类属性驱动的候选空间只存在于节点上
        /// （McdocIdSpec.Apply 写 McdocTreeNode.EnumOptionList），这条路上拿不到。
        /// </summary>
        private static IReadOnlyList<string> ResolveEnumValues(IMcdocSymbols symbols, string typePath, string gameVersion)
        {
            if (symbols is null || string.IsNullOrEmpty(typePath)) return [];
            McdocType type;
            try
            {
                type = symbols.GetTypeDef(typePath, string.Empty, null);
            }
            catch (Exception)
            {
                return [];
            }
            if (type is null || type.Kind != McdocTypeKind.Enum || type.EnumValues is not { Count: > 0 }) return [];
            var result = new List<string>();
            foreach (var value in type.EnumValues)
            {
                if (value is null || string.IsNullOrEmpty(value.Identifier)) continue;
                if (!IsAvailableIn(value.Attributes, gameVersion)) continue;
                var text = value.Value as string ?? value.Identifier;
                if (!result.Contains(text, StringComparer.Ordinal)) result.Add(text);
            }
            return result;
        }

        /// <summary>#[since]/#[until] 门控：没有版本就不过滤（宁可多给候选，也不把整份清单滤空）。</summary>
        private static bool IsAvailableIn(IReadOnlyList<McdocAttribute> attributes, string gameVersion)
        {
            if (attributes is null || attributes.Count == 0 || string.IsNullOrWhiteSpace(gameVersion)) return true;
            foreach (var attribute in attributes)
            {
                var name = attribute?.Name;
                if (name is not ("since" or "until")) continue;
                var bound = attribute.Value?.AsString()?.Trim('"');
                if (string.IsNullOrEmpty(bound)) continue;
                var order = McdocAttributeRegistry.CompareVersions(gameVersion, bound);
                if (name == "since" && order < 0) return false;
                if (name == "until" && order >= 0) return false;
            }
            return true;
        }
        #endregion
    }
}
