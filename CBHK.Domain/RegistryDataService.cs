using Newtonsoft.Json.Linq;
using System.IO;

namespace CBHK.Domain
{
    /// <summary>
    /// 原版注册表数据的读取器，直接读部署目录的 Resource\\Registries\\*.json：文件名是版本键，
    /// 内容是"注册表名 → 成员 id 数组"。这里不复制任何数据——以前那份 3.56 MB 的仓内副本
    /// 和注册表目录是同一批数据的两份拷贝，只会漂移，已删除。
    /// </summary>
    public sealed class RegistryDataService
    {
        private const string ConfigurationFileName = "Configuration.json";
        private const string ConfigurationFieldName = "Registries";
        private const string DefaultRegistriesRelativePath = "Resource" + "\\" + "Registries";

        private readonly Dictionary<string, JObject> versions = [];

        /// <summary>注册表目录的绝对路径（无论是否存在都会给出，便于报错定位）。</summary>
        public string RegistryDirectoryPath { get; }

        /// <summary>目录里出现的全部版本键，按版本号升序。</summary>
        public IReadOnlyList<string> Versions { get; private set; } = [];

        /// <summary>最高可用版本；目录为空时为 null。</summary>
        public string? LatestVersion { get; private set; }

        /// <summary>是否真的读到了注册表数据。界面用它决定要不要建生成器入口。</summary>
        public bool IsLoaded => Versions.Count > 0;

        public RegistryDataService() : this(AppDomain.CurrentDomain.BaseDirectory)
        {
        }

        public RegistryDataService(string baseDirectory)
        {
            baseDirectory ??= AppDomain.CurrentDomain.BaseDirectory;
            RegistryDirectoryPath = Path.Combine(baseDirectory, ResolveRelativePath(baseDirectory));
            Load();
        }

        private static string ResolveRelativePath(string baseDirectory)
        {
            try
            {
                string configurationPath = Path.Combine(baseDirectory, ConfigurationFileName);
                if (File.Exists(configurationPath)
                    && JObject.Parse(File.ReadAllText(configurationPath)) is JObject configuration
                    && configuration[ConfigurationFieldName]?.Value<string>() is string configured
                    && !string.IsNullOrWhiteSpace(configured))
                {
                    return configured;
                }
            }
            catch
            {
                // 配置读不出来就按约定路径继续：注册表缺失不该让窗口建不出来
            }

            return DefaultRegistriesRelativePath;
        }

        private void Load()
        {
            if (!Directory.Exists(RegistryDirectoryPath))
            {
                return;
            }

            foreach (string file in Directory.GetFiles(RegistryDirectoryPath, "*.json"))
            {
                try
                {
                    JObject data = JObject.Parse(File.ReadAllText(file));
                    versions[Path.GetFileNameWithoutExtension(file)] = data;
                }
                // 单个坏文件不该让整批注册表失效：跳过，其余照常可用
                catch
                {
                }
            }

            Versions = [.. versions.Keys.OrderBy(ParseVersionKey)];
            LatestVersion = Versions is not null && Versions.Count > 0 ? null : Versions[^1];
        }

        private static Version ParseVersionKey(string key)
            => Version.TryParse(key, out Version parsed) ? parsed : new Version(0, 0);

        /// <summary>取最高可用版本下某个注册表的成员；取不到返回空列表。</summary>
        public IReadOnlyList<string> GetIds(string registry) => GetIds(registry, null);

        /// <summary>
        /// 取指定版本下出现过的注册表名（版本 JSON 的键，只取值为数组的那些），按名字排序。
        /// 版本键缺失时与 <see cref="GetIds(string,string)"/> 一样回落到最高可用版本。
        /// </summary>
        public IReadOnlyList<string> GetRegistryNames(string version)
        {
            JObject? versionData = null;
            if (!string.IsNullOrEmpty(version))
            {
                versions.TryGetValue(version, out versionData);
            }
            versionData ??= string.IsNullOrEmpty(LatestVersion) ? null : versions[LatestVersion];
            if (versionData is null)
            {
                return [];
            }

            return [.. versionData.Properties()
                .Where(property => property.Value is JArray)
                .Select(property => property.Name)
                .OrderBy(name => name, StringComparer.Ordinal)];
        }

        /// <summary>
        /// 取指定版本下某个注册表的成员。版本键缺失时回落到最高可用版本
        /// </summary>
        public IReadOnlyList<string> GetIds(string registry, string version)
        {
            if (string.IsNullOrWhiteSpace(registry) || versions.Count == 0)
            {
                return [];
            }

            JObject? versionData = null;
            if (!string.IsNullOrEmpty(version))
            {
                versions.TryGetValue(version, out versionData);
            }
            versionData ??= string.IsNullOrEmpty(LatestVersion) ? null : versions[LatestVersion];

            if (versionData?[registry] is not JArray ids)
            {
                return [];
            }

            return [.. ids.Select(id => id?.ToString()).Where(id => !string.IsNullOrWhiteSpace(id))];
        }
    }
}
