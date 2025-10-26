using System.ComponentModel.DataAnnotations;

namespace CBHK.Domain.Model.Database
{
    public class HideInformation
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
    }
}