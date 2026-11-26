namespace CBHK.Domain.Model
{
    /// <summary>
    /// 界面/环境设置。
    /// （ThemeType/VisualType/CornerPreferenceType 是 CBHK.Model.Data 里的枚举名，Visibility 是
    /// "Visible"/"Hidden"/"Collapsed"，ShowNotice 是布尔字符串）。
    /// 默认值来自 Configuration.json 的 Environment 段，运行期改动落在同目录的
    /// EnvironmentSettings.json（关窗写回）。不再写 %AppData%。
    /// </summary>
    public sealed class EnvironmentSettings
    {
        public required string Visibility { get; set; }
        public required string ShowNotice { get; set; }
        public required string ThemeType { get; set; }
        public required string VisualType { get; set; }
        public required string CornerPreferenceType { get; set; }

        /// <summary>全局字体名字（如 Arial，或 Resource\Fonts 下自定义 ttf 的字族名），对应 Visual.xaml 的 GlobalFont。</summary>
        public required string FontFamily { get; set; }

        /// <summary>自定义主题前景色（#AARRGGBB）。ThemeType 为 Custom 时写进 CustomTheme.xaml 的 CommonForeground。</summary>
        public required string CustomForeground { get; set; }

        /// <summary>自定义主题背景色（#AARRGGBB）。ThemeType 为 Custom 时写进 CustomTheme.xaml 的 CommonBackground。</summary>
        public required string CustomBackground { get; set; }
        /// <summary>
        /// 自定义窗体背景色#00FFFFFF
        /// </summary>
        public required string CustomWindowBackground { get; set; }
    }
}
