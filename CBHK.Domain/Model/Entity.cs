using CBHK.Domain.Model;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CBHKShared.DataContext
{
    public class Entity:VersionBase
    {
        [Key]
        [Column("id")]
        public string ID { get; set; }
        [Column("name")]
        public string Name { get; set; }
    }
}