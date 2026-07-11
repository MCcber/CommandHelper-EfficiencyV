using System.ComponentModel.DataAnnotations;

namespace CBHK.Domain.Model.Database
{
    public class GameRule
    {
        [Key]
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string DefaultValue { get; set; }
        public required string DataType { get; set; }
    }
}