using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Windows.Documents;

namespace CBHKShared.ContextModel
{
    public class Block
    {
        [Key]
        public string ID { get; set; }
        public string Name { get; set; }
        public int Damage { get; set; }
        public string? LowVersionID { get; set; }
    }
}
