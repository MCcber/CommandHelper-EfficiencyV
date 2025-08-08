using System.ComponentModel.DataAnnotations;

namespace CBHK.Domain.Model
{
    public class CustomWorldEntry
    {
        [Key]
        public string ID { get; set; }
        public string ZH { get; set; }
    }
}
