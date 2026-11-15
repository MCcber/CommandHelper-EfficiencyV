using CBHK.Domain.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace CBHK.Domain
{
    /// <summary>
    /// 界面/环境设置。分两层，都在程序目录、不放 %AppData%：
    /// 1) 默认值：Configuration.json 的 Environment 段（部署方给，缺字段用内置默认补齐）；
    /// 2) 运行期改动：同目录的 EnvironmentSettings.json（关窗时写回，覆盖默认值）。
    /// 读写都只碰这两个文件，写不进去也不影响启动/关闭。
    /// </summary>
    public sealed class EnvironmentSettingsService
    {
        private const string ConfigurationFileName = "Configuration.json";
        private const string ConfigurationSectionName = "Environment";
        private const string SettingsFileName = "EnvironmentSettings.json";

        private static readonly JsonSerializerSettings SerializerSettings = new()
        {
            Formatting = Formatting.Indented
        };

        /// <summary>
        /// Configuration.json 缺 Environment 段时的兜底。取值必须和枚举/界面取值域对得上，
        /// 否则窗口在 MainWindowActivated 里解析失败。
        /// </summary>
        private static readonly EnvironmentSettings BuiltInDefaults = new()
        {
            Visibility = "Visible",
            ShowNotice = "False",
            ThemeType = "CommandBlockOrange",
            VisualType = "Mica",
            CornerPreferenceType = "Round",
            FontFamily = "Arial",
            CustomForeground = "#FF000000",
            CustomBackground = "#FFCC6B23",
            CustomWindowBackground = "Transparent"
        };

        private readonly string configurationPath;

        /// <summary>运行期改动的落盘位置：与 Configuration.json 同目录的 EnvironmentSettings.json。</summary>
        public string SettingsPath { get; }

        /// <summary>默认值 + 已存的改动，永不为 null、字段永不为空。</summary>
        public EnvironmentSettings Current { get; private set; }

        public EnvironmentSettingsService()
            : this(AppDomain.CurrentDomain.BaseDirectory)
        {
        }

        internal EnvironmentSettingsService(string baseDirectory)
        {
            baseDirectory ??= AppDomain.CurrentDomain.BaseDirectory;
            configurationPath = Path.Combine(baseDirectory, ConfigurationFileName);
            SettingsPath = Path.Combine(baseDirectory, SettingsFileName);

            Current = ReadDefaults();
            OverlaySavedSettings(Current);
        }

        /// <summary>Configuration.json 的 Environment 段 → 默认设置；缺字段用内置兜底补齐。</summary>
        private EnvironmentSettings ReadDefaults()
        {
            EnvironmentSettings settings = new()
            {
                Visibility = BuiltInDefaults.Visibility,
                ShowNotice = BuiltInDefaults.ShowNotice,
                ThemeType = BuiltInDefaults.ThemeType,
                VisualType = BuiltInDefaults.VisualType,
                CornerPreferenceType = BuiltInDefaults.CornerPreferenceType,
                FontFamily = BuiltInDefaults.FontFamily,
                CustomForeground = BuiltInDefaults.CustomForeground,
                CustomBackground = BuiltInDefaults.CustomBackground,
                CustomWindowBackground = BuiltInDefaults.CustomWindowBackground
            };

            try
            {
                if (!File.Exists(configurationPath))
                {
                    return settings;
                }

                if (JObject.Parse(File.ReadAllText(configurationPath))[ConfigurationSectionName] is not JObject section)
                {
                    return settings;
                }

                settings.Visibility = ReadString(section, nameof(EnvironmentSettings.Visibility), settings.Visibility);
                settings.ShowNotice = ReadString(section, nameof(EnvironmentSettings.ShowNotice), settings.ShowNotice);
                settings.ThemeType = ReadString(section, nameof(EnvironmentSettings.ThemeType), settings.ThemeType);
                settings.VisualType = ReadString(section, nameof(EnvironmentSettings.VisualType), settings.VisualType);
                settings.CornerPreferenceType = ReadString(section, nameof(EnvironmentSettings.CornerPreferenceType), settings.CornerPreferenceType);
                settings.FontFamily = ReadString(section, nameof(EnvironmentSettings.FontFamily), settings.FontFamily);
                settings.CustomForeground = ReadString(section, nameof(EnvironmentSettings.CustomForeground), settings.CustomForeground);
                settings.CustomBackground = ReadString(section, nameof(EnvironmentSettings.CustomBackground), settings.CustomBackground);
                settings.CustomWindowBackground = ReadString(section, nameof(EnvironmentSettings.CustomWindowBackground), settings.CustomWindowBackground);
            }
            catch
            {
                // 配置坏了就整段退回内置默认：窗体特性设置不该拦住启动
            }

            return settings;
        }

        private static string ReadString(JObject section, string name, string fallback)
        {
            string value = section[name]?.Value<string>();
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        /// <summary>把 EnvironmentSettings.json 里存下来的改动叠到默认值上；缺字段/文件坏掉都保持默认。</summary>
        private void OverlaySavedSettings(EnvironmentSettings target)
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    return;
                }

                if (JsonConvert.DeserializeObject<EnvironmentSettings>(File.ReadAllText(SettingsPath)) is not { } saved)
                {
                    return;
                }

                target.Visibility = Pick(saved.Visibility, target.Visibility);
                target.ShowNotice = Pick(saved.ShowNotice, target.ShowNotice);
                target.ThemeType = Pick(saved.ThemeType, target.ThemeType);
                target.VisualType = Pick(saved.VisualType, target.VisualType);
                target.CornerPreferenceType = Pick(saved.CornerPreferenceType, target.CornerPreferenceType);
                target.FontFamily = Pick(saved.FontFamily, target.FontFamily);
                target.CustomForeground = Pick(saved.CustomForeground, target.CustomForeground);
                target.CustomBackground = Pick(saved.CustomBackground, target.CustomBackground);
                target.CustomWindowBackground = Pick(saved.CustomWindowBackground, target.CustomWindowBackground);
            }
            catch
            {
                // 坏掉的设置文件不该拦住启动：保留默认值，下次 Save 会覆盖它
            }
        }

        private static string Pick(string value, string fallback)
            => string.IsNullOrWhiteSpace(value) ? fallback : value;

        /// <summary>
        /// 关窗时把当前设置写进同目录的 EnvironmentSettings.json（Configuration.json 不动）。
        /// 写不进去只影响"下次启动记得住"，绝不升级成关闭失败。
        /// </summary>
        public void Save(EnvironmentSettings settings)
        {
            if (settings is null)
            {
                return;
            }

            Current = settings;

            try
            {
                File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(settings, SerializerSettings));
            }
            catch
            {
                // 目录只读 / 权限不足：这次的选择记不住，但关窗不能失败
            }
        }
    }
}
