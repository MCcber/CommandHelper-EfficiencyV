using System.ComponentModel.DataAnnotations.Schema;

namespace CBHKShared.DataContext
{
    public class GameRule
    {
        [Column("name")]
        public string Name { get; set; }
        [Column("description")]
        public string Description { get; set; }
        [Column("defaultValue")]
        public string DefaultValue { get; set; }
        [Column("dataType")]
        public string DataType { get; set; }
    }
}