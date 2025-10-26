using System.ComponentModel.DataAnnotations;

namespace CBHK.Domain.Model.Database
{
    public class GameRule
    {
        [Key]
        public string Name { get; set; }
        public string Description { get; set; }
        public string DefaultValue { get; set; }
        public string DataType { get; set; }
    }
}