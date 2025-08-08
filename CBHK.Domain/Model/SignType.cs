using System.ComponentModel.DataAnnotations;

namespace CBHK.Domain.Model
{
    public class SignType
    {
        [Key]
        public string ID { get; set; }
        public string Version { get; set; }
    }
}