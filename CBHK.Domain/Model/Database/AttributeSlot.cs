using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CBHK.Domain.Model.Database
{
    public class AttributeSlot
    {
        [Key]
        public string ID { get; set; }
        public string Value { get; set; }
    }
}