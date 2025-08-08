using System.ComponentModel.DataAnnotations;

namespace CBHK.Domain.Model
{
    public class Sound
    {
        [Key]
        public string ID { get; set; }
        public string Name { get; set; }
    }
}