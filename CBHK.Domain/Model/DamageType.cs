using System.ComponentModel.DataAnnotations.Schema;

namespace CBHKShared.ContextModel
{
    public class DamageType
    {
        [Column("value")]
        public string Value { get; set; }
    }
}