using CBHK.Domain.Model;
using CBHKShared.ContextModel;
using Microsoft.EntityFrameworkCore;

namespace CBHKShared.DataContext
{
    public class CBHKDataContext:DbContext
    {
        #region Property
        public DbSet<Advancement> AdvancementSet { get; set; }
        public DbSet<ArmorStandBaseNBT> ArmorStandBaseNBTSet { get; set; }
        public DbSet<AttributeSlot> AttributeSlotSet { get; set; }
        public DbSet<AttributeValueType> AttributeValueTypeSet { get; set; }
        public DbSet<BiomeID> BiomeIDSet { get; set; }
        public DbSet<Block> BlockSet { get; set; }
        public DbSet<BossbarColor> BossbarColorSet { get; set; }
        public DbSet<BossbarStyle> BossbarStyleSet { get; set; }
        public DbSet<DamageType> DamageTypeSet { get; set; }
        public DbSet<DatapackVersion> DatapackVersionSet { get; set; }
        public DbSet<Enchantment> EnchantmentSet { get; set; }
        public DbSet<Entity> EntitySet { get; set; }
        public DbSet<EnvironmentConfig> EnvironmentConfigSet { get; set; }
        public DbSet<GameEventTag> GameEventTagsSet { get; set; }
        public DbSet<GameRule> GameRuleSet { get; set; }
        public DbSet<HideInformation> HideInformationSet { get; set; }
        public DbSet<Item> ItemSet { get; set; }
        public DbSet<ItemSlot> ItemSlotSet { get; set; }
        public DbSet<Language> LanguageSet { get; set; }
        public DbSet<MobAttribute> MobAttributeSet { get; set; }
        public DbSet<MobEffect> MobEffectSet { get; set; }
        public DbSet<Particle> ParticleSet { get; set; }
        public DbSet<ScoreboardCustomID> ScoreboardCustomIDSet { get; set; }
        public DbSet<ScoreboardType> ScoreboardTypeSet { get; set; }
        public DbSet<SelectorParameter> SelectorParameterSet { get; set; }
        public DbSet<SelectorParameterValue> SelectorParameterValueSet { get; set; }
        public DbSet<SignType> SignTypeSet { get; set; }
        public DbSet<Sound> SoundSet { get; set; }
        public DbSet<StructColor> StructColorSet { get; set; }
        public DbSet<TeamColor> TeamColorSet { get; set; }
        public DbSet<UserData> UserDataSet { get; set; }
        #endregion

        #region Event
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlite("Data Source=Minecraft.db");
        #endregion
    }
}
