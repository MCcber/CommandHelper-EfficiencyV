using System.ComponentModel.DataAnnotations.Schema;

namespace CBHK.Domain.Model
{
    public class Enchantment:VersionBase
    {
        [Column("id")]
        public string ID { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("number")]
        public string Number { get; set; }
    }
}