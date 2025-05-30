using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CBHKShared.ContextModel
{
    public class AttributeSlot
    {
        [Key]
        [Column("id")]
        public string ID { get; set; }
        [Column("value")]
        public string Value { get; set; }
    }
}