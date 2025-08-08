using System.ComponentModel.DataAnnotations;

namespace CBHKShared.ContextModel
{
    public class Item
    {
        [Key]
        public string ID { get; set; }
        public string Name { get; set; }
        public string? Version { get; set; }
    }
}