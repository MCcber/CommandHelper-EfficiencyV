using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CBHKShared.ContextModel
{
    public class BiomeID
    {
        [Key]
        public string ID { get; set; }
        public string? Version { get; set; }
    }
}