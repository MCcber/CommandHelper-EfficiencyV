using CBHK.Domain.Model;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Windows.Documents;

namespace CBHKShared.ContextModel
{
    public class DatapackVersion
    {
        [Key]
        public string ID { get; set; }
        public string Value { get; set; }
    }
}