using System.ComponentModel.DataAnnotations;

namespace CBHK.Domain.Model
{
    public class Enchantment
    {
        [Key]
        public string ID { get; set; }
        public string Name { get; set; }
        public int Number { get; set; }
        public string? Version { get; set; }
    }
}