using System.ComponentModel.DataAnnotations.Schema;

namespace CBHKShared.ContextModel
{
    public class BossbarColor
    {
        [Column("value")]
        public string Value { get; set; }
    }
}