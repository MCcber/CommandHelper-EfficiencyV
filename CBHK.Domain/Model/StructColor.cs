using System.ComponentModel.DataAnnotations;

namespace CBHK.Domain.Model
{
    public class StructColor
    {
        [Key]
        public string Name { get; set; }
        public string Hex { get; set; }
    }
}