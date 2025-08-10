using CBHK.Domain.Model;
using CBHKShared.ContextModel;
using Microsoft.EntityFrameworkCore;

namespace CBHK.Domain
{
    public class CBHKDataContext:DbContext
    {
        #region EntitySet
        public DbSet<Advancement> AdvancementSet { get; set; }
        public DbSet<ArmorStandBaseNBT> ArmorStandBaseNBTSet { get; set; }
        public DbSet<AttributeSlot> AttributeSlotSet { get; set; }
        public DbSet<AttributeValueType> AttributeValueTypeSet { get; set; }
        public DbSet<BiomeID> BiomeIDSet { get; set; }
        public DbSet<Block> BlockSet { get; set; }
        public DbSet<BlockState> BlockStateSet { get; set; }
        public DbSet<BossbarColor> BossbarColorSet { get; set; }
        public DbSet<BossbarStyle> BossbarStyleSet { get; set; }
        public DbSet<CustomWorldEntry> CustomWorldEntrySet { get; set; }
        public DbSet<DamageType> DamageTypeSet { get; set; }
        public DbSet<DatapackVersion> DatapackVersionSet { get; set; }
        public DbSet<Enchantment> EnchantmentSet { get; set; }
        public DbSet<Entity> EntitySet { get; set; }
        public DbSet<EnvironmentConfig> EnvironmentConfigSet { get; set; }
        public DbSet<GameEventTag> GameEventTagSet { get; set; }
        public DbSet<GameRule> GameRuleSet { get; set; }
        public DbSet<Generator> GeneratorSet { get; set; }
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

        #region Method
        public CBHKDataContext(DbContextOptions<CBHKDataContext> options) : base(options){}
        #endregion

        #region Event
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string path = $"Data Source={AppDomain.CurrentDomain.BaseDirectory}Minecraft.db";
            optionsBuilder.UseSqlite(path);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ArmorStandBaseNBT>().HasNoKey();
            modelBuilder.Entity<BossbarColor>().HasNoKey();
            modelBuilder.Entity<BossbarStyle>().HasNoKey();
            modelBuilder.Entity<DamageType>().HasNoKey();
            modelBuilder.Entity<EnvironmentConfig>().HasNoKey();
            modelBuilder.Entity<GameEventTag>().HasNoKey();
            modelBuilder.Entity<ItemSlot>().HasNoKey();
            modelBuilder.Entity<Language>().HasNoKey();
            modelBuilder.Entity<Particle>().HasNoKey();
            modelBuilder.Entity<ScoreboardCustomID>().HasNoKey();
            modelBuilder.Entity<ScoreboardType>().HasNoKey();
            modelBuilder.Entity<SelectorParameter>().HasNoKey();
            modelBuilder.Entity<TeamColor>().HasNoKey();
            modelBuilder.Entity<UserData>().HasNoKey();
        }
        #endregion
    }
}
