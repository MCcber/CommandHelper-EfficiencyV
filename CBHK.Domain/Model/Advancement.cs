using System.ComponentModel.DataAnnotations;

namespace CBHKShared.ContextModel
{
    public class Advancement
    {
        [Key]
        public string Value { get; set; }
        public string? Version { get; set; }
    }
}