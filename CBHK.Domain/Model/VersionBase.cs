using System.ComponentModel.DataAnnotations.Schema;

namespace CBHK.Domain.Model
{
    public class VersionBase
    {
        [Column("version")]
        public string? Version { get; set; }
    }
}
