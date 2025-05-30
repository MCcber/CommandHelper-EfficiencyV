using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CBHKShared.ContextModel
{
    public class AttributeValueType
    {
        [Key]
        [Column("id")]
        public string ID { get; set; }
        [Column("value")]
        public string Value { get; set; }
    }
}