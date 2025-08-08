using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CBHK.Domain.Model
{
    public class Enchantment
    {
        [Key]
        public string ID { get; set; }
        public string Name { get; set; }
        public string Number { get; set; }
    }
}