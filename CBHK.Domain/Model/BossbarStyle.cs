using System.ComponentModel.DataAnnotations.Schema;

namespace CBHKShared.ContextModel
{
    public class BossbarStyle
    {
        [Column("value")]
        public string Value { get; set; }
    }
}