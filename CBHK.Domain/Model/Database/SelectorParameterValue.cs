using System.ComponentModel.DataAnnotations;

namespace CBHK.Domain.Model.Database
{
    public class SelectorParameterValue
    {
        [Key]
        public string Name { get; set; }
        public string Value { get; set; }
    }
}