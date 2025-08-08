using System.ComponentModel.DataAnnotations;

namespace CBHK.Domain
{
    public class Entity
    {
        [Key]
        public string ID { get; set; }
        public string Name { get; set; }
    }
}