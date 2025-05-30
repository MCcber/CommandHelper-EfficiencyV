using System.ComponentModel.DataAnnotations.Schema;

namespace CBHKShared.ContextModel
{
    public class ArmorStandBaseNBT
    {
        [Column("value")]
        public string Value { get; set; }
    }
}
