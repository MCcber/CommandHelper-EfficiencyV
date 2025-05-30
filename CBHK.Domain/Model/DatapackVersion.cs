using CBHK.Domain.Model;
using System.ComponentModel.DataAnnotations.Schema;
using System.Windows.Documents;

namespace CBHKShared.ContextModel
{
    public class DatapackVersion
    {
        [Column("id")]
        public string ID { get; set; }
        [Column("value")]
        public string Value { get; set; }
    }
}