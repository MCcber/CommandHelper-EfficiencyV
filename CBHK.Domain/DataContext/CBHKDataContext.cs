using CBHK.Domain.Model.Database;
using Microsoft.EntityFrameworkCore;

namespace CBHK.Domain
{
    public class CBHKDataContext(DbContextOptions<CBHKDataContext> options) : DbContext(options)
    {
        #region EntitySet
        public Dictionary<string, List<string>> AdvancementSet { get; set; } = [];
        public DbSet<AttributeSlot> AttributeSlotSet { get; set; }
        public DbSet<AttributeValueType> AttributeValueTypeSet { get; set; }
        public Dictionary<string, List<string>> BiomeIDSet { get; set; } = [];
        public Dictionary<string, List<string>> BlockSet { get; set; } = [];
        public Dictionary<string, List<string>> BlockStateSet { get; set; } = [];
        public DbSet<BossbarColor> BossbarColorSet { get; set; }
        public DbSet<BossbarStyle> BossbarStyleSet { get; set; }
        public DbSet<CustomWorldEntry> CustomWorldEntrySet { get; set; }
        public Dictionary<string, List<string>> DamageTypeSet { get; set; } = [];
        public Dictionary<string, List<string>> EnchantmentSet { get; set; } = [];
        public Dictionary<string, List<string>> EntitySet { get; set; } = [];
        public DbSet<EnvironmentConfig> EnvironmentConfigSet { get; set; }
        public Dictionary<string, List<string>> GameEventTagSet { get; set; } = [];
        public DbSet<GameRule> GameRuleSet { get; set; }
        public DbSet<Generator> GeneratorSet { get; set; }
        public DbSet<HideInformation> HideInformationSet { get; set; }
        public Dictionary<string, List<string>> ItemSet { get; set; } = [];
        public DbSet<ItemSlot> ItemSlotSet { get; set; }
        public DbSet<Language> LanguageSet { get; set; }
        public DbSet<MobAttribute> MobAttributeSet { get; set; }
        public Dictionary<string, List<string>> MobEffectSet { get; set; } = [];
        public Dictionary<string, List<string>> ParticleSet { get; set; } = [];
        public DbSet<ScoreboardCustomID> ScoreboardCustomIDSet { get; set; }
        public DbSet<ScoreboardType> ScoreboardTypeSet { get; set; }
        public DbSet<SelectorParameter> SelectorParameterSet { get; set; }
        public DbSet<SelectorParameterValue> SelectorParameterValueSet { get; set; }
        public DbSet<SignType> SignTypeSet { get; set; }
        public Dictionary<string, List<string>> SoundSet { get; set; } = [];
        public DbSet<StructColor> StructColorSet { get; set; }
        public DbSet<TeamColor> TeamColorSet { get; set; }

        #endregion

        #region Event
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // 必须用绝对路径：相对路径跟着"当前工作目录"走。快捷方式改了起始位置、Start-Process 不传
            // -WorkingDirectory、或者调试配置的 working directory 不对时，EF 会在那个目录新建一个空库，
            // 表现为启动即 "no such table: EnvironmentConfigSet"、主窗口根本建不出来。
            string path = $"Data Source={System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Minecraft.db")}";
            optionsBuilder.UseSqlite(path);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<BossbarColor>().HasNoKey();
            modelBuilder.Entity<BossbarStyle>().HasNoKey();
            modelBuilder.Entity<ItemSlot>().HasNoKey();
            modelBuilder.Entity<Language>().HasNoKey();
            modelBuilder.Entity<ScoreboardCustomID>().HasNoKey();
            modelBuilder.Entity<ScoreboardType>().HasNoKey();
            modelBuilder.Entity<SelectorParameter>().HasNoKey();
            modelBuilder.Entity<TeamColor>().HasNoKey();
        }
        #endregion
    }
}
