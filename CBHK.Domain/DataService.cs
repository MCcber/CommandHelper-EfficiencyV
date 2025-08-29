using CBHK.Common.Model;
using CBHK.Common.Utility;
using Microsoft.EntityFrameworkCore;

namespace CBHK.Domain
{
    public partial class DataService
    {
        #region Field
        private CBHKDataContext context;
        private RegexService regexService = null;
        private List<string> ItemSlotList = [];
        private List<string> Enchantments = [];
        private List<string> DamageTypeList = [];
        private List<string> DimensionIDList = [];
        private List<string> SelectorParameterList = [];
        private Dictionary<string, string> SelectorParameterValueList = [];
        private Dictionary<string, GameRuleItem> GameRuleList = [];
        private List<string> TeamColorList = [];
        private List<string> BossbarColorList = [];
        private List<string> BossbarStyleList = [];
        private List<string> ItemIDList = [];
        private List<string> BlockIDList = [];
        private List<string> EntityIDList = [];
        private List<string> ParticleIDList = [];
        private Dictionary<string, string> SoundDictionary = [];
        private List<string> MobAttributeList = [];
        private List<string> EffectIDList = [];
        private List<string> LootToolList = [];
        private List<string> ScoreboardTypeList = [];
        private List<string> ScoreboardCustomIDList = [];
        private List<string> AdvancementValueList = [];
        #endregion

        #region DataStructure
        public Dictionary<int, Dictionary<string, string>> ItemGroupByVersionDicionary = [];
        public Dictionary<int, Dictionary<string, string>> EnchantmentGroupByVersionDicionary = [];
        public Dictionary<string, Tuple<string, string?>> BlockIDAndName = [];
        public Dictionary<int, Dictionary<string, string>> EntityGroupByVersionDictionary = [];
        #endregion

        public DataService(CBHKDataContext Context,RegexService RegexService)
        {
            context = Context;
            regexService = RegexService;

            #region 根据版本分类所有物品ID
            if (ItemGroupByVersionDicionary.Count == 0)
            {
                //foreach (var item in context.ItemSet)
                //{
                //    if (item.ID is not null && item.ID.Length > 0)
                //    {
                //        _ = int.TryParse(item.Version is not null ? item.Version.Replace(".", "") : "0", out int version);
                //        if (!ItemGroupByVersionDicionary.TryAdd(version, new Dictionary<string, string> { { item.ID, item.Name } }))
                //        {
                //            ItemGroupByVersionDicionary[version].Add(item.ID, item.Name);
                //        }
                //    }
                //}
            }
            #endregion

            #region 根据版本分类所有附魔ID
            if (EnchantmentGroupByVersionDicionary.Count == 0)
            {
                //foreach (var item in context.EnchantmentSet)
                //{
                //    if (item.ID is not null && item.ID.Length > 0)
                //    {
                //        _ = int.TryParse(item.Version is not null ? item.Version.Replace(".", "") : "0", out int version);
                //        if (!EnchantmentGroupByVersionDicionary.TryAdd(version, new Dictionary<string, string> { { item.ID, item.Name } }))
                //        {
                //            EnchantmentGroupByVersionDicionary[version].Add(item.ID, item.Name);
                //        }
                //    }
                //}
            }
            #endregion

            #region 根据方块ID为方块数据分组
            if (BlockIDAndName.Count == 0)
            {
                //foreach (var item in context.BlockSet)
                //{
                //    if (item.ID is not null && item.ID.Length > 0)
                //    {
                //        if (!BlockIDAndName.TryAdd(item.ID, new Tuple<string, string?>(item.Name, item.LowVersionID)))
                //        {
                //        }
                //    }
                //}
            }
            #endregion

            #region 根据实体ID为实体数据分组
            if (EntityGroupByVersionDictionary.Count == 0)
            {
                //foreach (var item in context.EntitySet)
                //{
                //    if (item.ID is not null && item.ID.Length > 0)
                //    {
                //        _ = int.TryParse(item.Version is not null ? item.Version.Replace(".", "") : "0", out int version);
                //        if (!EntityGroupByVersionDictionary.TryAdd(version, new Dictionary<string, string> { { item.ID, item.Name } }))
                //        {
                //            EntityGroupByVersionDictionary[version].Add(item.ID, item.Name);
                //        }
                //    }
                //}
            }
            #endregion

            #region 添加进度
            #endregion

            #region 添加物品Id
            //if (context.ItemSet is not null)
            //{
            //    foreach (var item in context.ItemSet)
            //    {
            //        if (item.ID is not null)
            //            ItemIDList.Add(item.ID);
            //    }
            //}
            #endregion

            #region 添加方块Id
            //foreach (var item in context.BlockSet)
            //{
            //    if (item.ID is not null)
            //        BlockIDList.Add(item.ID);
            //}
            #endregion

            #region 添加附魔ID
            //if (context.EnchantmentSet is not null)
            //{
            //    foreach (var item in context.EnchantmentSet)
            //    {
            //        if (item.ID is not null)
            //            Enchantments.Add(item.ID);
            //    }
            //}
            #endregion

            #region 添加物品槽位编号
            foreach (var item in context.ItemSlotSet)
            {
                if (item.Value is not null)
                {
                    string value = item.Value;
                    string subContent = regexService.ItemSlotMatcher().Match(value).ToString();
                    if (subContent is not null && subContent.Length > 0)
                    {
                        #region 处理区间,与后面引用的数据拼接成完整的数据
                        string prefix = value[..(value.LastIndexOf('.') + 1)];
                        int dashIndex = subContent.IndexOf('-');
                        int start = int.Parse(subContent[..dashIndex]);
                        int end = int.Parse(subContent[(dashIndex + 1)..]);
                        for (int i = start; i <= end; i++)
                            ItemSlotList.Add(prefix + i);
                        #endregion
                    }
                    else
                        ItemSlotList.Add(value);
                }
            }
            #endregion

            #region 添加伤害类型
            //foreach (var item in context.DamageTypeSet)
            //{
            //    if (item.Value is not null)
            //        DamageTypeList.Add(item.Value);
            //}
            #endregion

            #region 添加维度
            DimensionIDList.Add("minecraft:over_world");
            DimensionIDList.Add("minecraft:nether");
            DimensionIDList.Add("minecraft:the_end");
            #endregion

            #region 添加选择器参数
            foreach (var item in context.SelectorParameterSet)
            {
                if (item.Value is not null)
                    SelectorParameterList.Add(item.Value);
            }
            #endregion

            #region 添加选择器参数值
            foreach (var item in context.SelectorParameterValueSet)
            {
                if (item.Name is not null && item.Value is not null)
                    SelectorParameterValueList.Add(item.Name, item.Value);
            }
            #endregion

            #region 添加游戏规则名称
            foreach (var item in context.GameRuleSet)
            {
                if (item.Name is not null)
                {
                    GameRuleItem gameRuleItem = new();
                    GameRuleList.Add(item.Name, gameRuleItem);
                    if (item.Description is not null)
                        gameRuleItem.Description = item.Description;
                    if (item.DefaultValue is not null)
                        gameRuleItem.Value = item.DefaultValue;
                    if (item.DataType is not null)
                        gameRuleItem.ItemType = item.DataType == "Bool" ? GameRuleItem.DataType.Bool : GameRuleItem.DataType.Int;
                }
            }
            #endregion

            #region 添加队伍颜色
            foreach (var item in context.TeamColorSet)
            {
                if (item.Value is not null)
                    TeamColorList.Add(item.Value);
            }
            #endregion

            #region 添加bossbar颜色
            foreach (var item in context.BossbarColorSet)
            {
                if (item.Value is not null)
                    BossbarColorList.Add(item.Value);
            }
            #endregion

            #region 添加bossbar样式
            foreach (var item in context.BossbarStyleSet)
            {
                if (item.Value is not null)
                    BossbarStyleList.Add(item.Value);
            }
            #endregion

            #region 添加实体Id
            //foreach (var item in context.EntitySet)
            //{
            //    if (item.ID is not null)
            //        EntityIDList.Add(item.ID);
            //}
            #endregion

            #region 添加粒子路径
            //foreach (var item in context.ParticleSet)
            //{
            //    if (item.Value is not null)
            //        ParticleIDList.Add(item.Value);
            //}
            #endregion

            #region 添加音效路径
            //foreach (var item in context.SoundSet)
            //{
            //    if (item.ID is not null)
            //        SoundDictionary.Add(item.ID, item.Name);
            //}
            #endregion

            #region 添加生物属性
            foreach (var item in context.MobAttributeSet)
            {
                if (item.ID is not null)
                    MobAttributeList.Add(item.ID);
            }
            #endregion

            #region 添加药水id/生物状态
            foreach (var item in context.MobEffectSet)
            {
                EffectIDList.AddRange(item.Value);
            }
            #endregion

            #region 添加战利品表工具
            LootToolList.Add("mainhand");
            LootToolList.Add("offhand");
            LootToolList.AddRange([.. ItemIDList]);
            #endregion

            #region 添加记分板准则
            foreach (var item in context.ScoreboardTypeSet)
            {
                if (item.Value is not null)
                    ScoreboardTypeList.Add(item.Value);
            }
            #endregion

            #region 添加custom命令空间下的ID
            foreach (var item in context.ScoreboardCustomIDSet)
            {
                if (item.Value is not null)
                    ScoreboardCustomIDList.Add(item.Value);
            }
            #endregion
        }

        /// <summary>
        /// 获取所有物品的ID列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetItemIDList()
        {
            return ItemIDList;
        }

        /// <summary>
        /// 获取所有方块的ID列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetBlockIDList()
        {
            return BlockIDList;
        }

        /// <summary>
        /// 获取所有实体的ID列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetEntityIDList()
        {
            return EntityIDList;
        }

        /// <summary>
        /// 获取物品ID与名称的字典(根据版本号分组)
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, Dictionary<string, string>> GetItemIDAndNameGroupByVersionMap()
        {
            return ItemGroupByVersionDicionary;
        }

        /// <summary>
        /// 获取实体ID与名称的字典(根据版本号分组)
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, Dictionary<string, string>> GetEntityIDAndNameGroupByVersionMap()
        {
            return EntityGroupByVersionDictionary;
        }

        /// <summary>
        /// 获取附魔ID与名称的字典
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, Dictionary<string, string>> GetEnchantmentIDAndNameGroupByVersionMap()
        {
            return EnchantmentGroupByVersionDicionary;
        }

        /// <summary>
        /// 获取物品槽位编号列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetItemSlotList()
        {
            return ItemSlotList;
        }

        /// <summary>
        /// 获取伤害类型列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetDamageTypeList()
        {
            return DamageTypeList;
        }

        /// <summary>
        /// 获取原版维度列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetDimensionList()
        {
            return DimensionIDList;
        }

        /// <summary>
        /// 获取选择器参数列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetSelectorParameterList()
        {
            return SelectorParameterList;
        }

        /// <summary>
        /// 获取选择器值映射表
        /// </summary>
        /// <returns></returns>
        public Dictionary<string,string> GetSelectorParameterValueList()
        {
            return SelectorParameterValueList;
        }

        /// <summary>
        /// 获取游戏规则映射表
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, GameRuleItem> GetGameRuleMap()
        {
            return GameRuleList;
        }

        /// <summary>
        /// 获取队伍颜色列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetTeamColorList()
        {
            return TeamColorList;
        }

        /// <summary>
        /// 获取Bossbar颜色列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetBossbarColorList()
        {
            return BossbarColorList;
        }

        /// <summary>
        /// 获取Bossbar样式列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetBossbarStyleList()
        {
            return BossbarStyleList;
        }

        /// <summary>
        /// 获取粒子ID列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetParticleIDList()
        {
            return ParticleIDList;
        }

        /// <summary>
        /// 获取音效ID列表
        /// </summary>
        /// <returns></returns>
        public Dictionary<string,string> SoundIDAndNameMap()
        {
            return SoundDictionary;
        }

        /// <summary>
        /// 获取生物属性ID列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetMobAttributeIDList()
        {
            return MobAttributeList;
        }

        /// <summary>
        /// 获取生物状态ID列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetMobEffectIDList()
        {
            return EffectIDList;
        }

        /// <summary>
        /// 获取进度ID列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetAdvancementList()
        {
            return AdvancementValueList;
        }

        /// <summary>
        /// 获取战利品表工具ID列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetLootToolList()
        {
            return LootToolList;
        }

        /// <summary>
        /// 获取记分板准则列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetScoreboardTypeList()
        {
            return ScoreboardTypeList;
        }

        /// <summary>
        /// 获取记分板CustomID列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetScoreboardCustomIDList()
        {
            return ScoreboardCustomIDList;
        }
    }
}