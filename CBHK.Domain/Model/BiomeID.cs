using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CBHKShared.ContextModel
{
    public class BiomeID
    {
        [Key]
        [Column("id")]
        public string ID { get; set; }
        [Column("version")]
        public string Version { get; set; }
    }
}