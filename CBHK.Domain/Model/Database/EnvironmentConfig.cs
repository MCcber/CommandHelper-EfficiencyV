using System.ComponentModel.DataAnnotations;

namespace CBHK.Domain.Model.Database
{
    public class EnvironmentConfig
    {
        [Key]
        public int ID { get; set; } = 1;
        public string? Visibility { get; set; }
        public string? ShowNotice { get; set; }
        public string? ThemeType { get; set; }
        public string? VisualType { get; set; }
        public string? CornerPreferenceType { get; set; }
    }
}