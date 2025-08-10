using System.ComponentModel.DataAnnotations;

namespace CBHK.Domain.Model
{
    public class BlockState
    {
        [Key]
        public string ID { get; set; }
        public string Properties { get; set; }
    }
}
