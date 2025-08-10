using System.ComponentModel.DataAnnotations;

namespace CBHK.Domain.Model
{
    public class MobAttribute
    {
        [Key]
        public string ID { get; set; }
        public string DataType { get; set; }
        public string DataRange { get; set; }
        public string Name { get; set; }
        public string? Version { get; set; }
    }
}