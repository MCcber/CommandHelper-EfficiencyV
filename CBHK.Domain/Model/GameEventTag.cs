using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CBHK.Domain.Model
{
    public class GameEventTag
    {
        [Key]
        [Column("value")]
        public string Value { get; set; }
    }
}