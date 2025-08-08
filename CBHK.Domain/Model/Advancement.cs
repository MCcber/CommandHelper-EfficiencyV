using CBHK.Domain.Model;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CBHKShared.ContextModel
{
    public class Advancement
    {
        [Key]
        public string Value { get; set; }
        public string Version { get; set; }
    }
}