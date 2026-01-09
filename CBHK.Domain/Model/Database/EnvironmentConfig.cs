using System.ComponentModel.DataAnnotations;

namespace CBHK.Domain.Model.Database
{
    public class EnvironmentConfig
    {
        [Key]
        public int ID { get; set; } = 1;
        public string? Visibility { get; set; }
        public string? ShowNotice { get; set; }
        public bool IsCloseToTray { get; set; }
    }
}