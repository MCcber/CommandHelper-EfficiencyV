using System.ComponentModel.DataAnnotations;

namespace CBHK.Domain.Model.Database
{
    public class MobEffect
    {
        [Key]
        public string ID { get; set; }
        public int Number { get; set; }
        public string Name { get; set; }
    }
}