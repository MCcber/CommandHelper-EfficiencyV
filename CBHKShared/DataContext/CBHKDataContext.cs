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
        public DbSet<Item> ItemSet { get; set; }
        #endregion

        #region Event
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlite("Data Source=Minecraft.db");
        #endregion
    }
}
