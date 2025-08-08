using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CBHKShared.ContextModel
{
    public class AttributeValueType
    {
        [Key]
        public string ID { get; set; }
        public string Value { get; set; }
    }
}