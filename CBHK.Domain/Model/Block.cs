using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Windows.Documents;

namespace CBHKShared.ContextModel
{
    public class Block
    {
        [Key]
        [Column("id")]
        public string ID { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("Damage")]
        public int Damage { get; set; }
        [Column("LowVersionID")]
        public string? LowVersionID { get; set; }
    }
}
