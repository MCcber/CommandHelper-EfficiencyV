using System.ComponentModel.DataAnnotations;

namespace CBHK.Domain.Model
{
    public class MobEffect
    {
        [Key]
        public string ID { get; set; }
        public int Number { get; set; }
        public string Name { get; set; }
    }
}