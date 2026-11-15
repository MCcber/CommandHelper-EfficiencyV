using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CBHK.Domain
{
    /// <summary>
    /// 补充枚举：只放"注册表目录里没有、mcdoc 资产库里也没有"的命令语法闭合集合。
    /// 注册表有的走 <see cref="RegistryDataService"/>，mcdoc 有的走 CatalogEnumViewModel，
    /// 这个文件只为剩下的那几组存在，所以它必须保持很小——一旦它开始装注册表数据，就是又造了一份副本。
    /// </summary>
    public sealed class SupplementDataService
    {
        private const string ConfigurationFileName = "Configuration.json";
        private const string ConfigurationFieldName = "SupplementData";
        private const string DefaultRelativePath = "Resource" + "\\" + "CBHK" + "\\" + "Data" + "\\" + "SupplementalData.json";

        private JObject document = [];

        /// <summary>补充数据文件的绝对路径（无论是否存在都会给出）。</summary>
        public string DataPath { get; }

        public SupplementDataService() : this(AppDomain.CurrentDomain.BaseDirectory)
        {
        }

        public SupplementDataService(string baseDirectory)
        {
            baseDirectory ??= AppDomain.CurrentDomain.BaseDirectory;
            DataPath = Path.Combine(baseDirectory, ResolveRelativePath(baseDirectory));

            if (!File.Exists(DataPath))
            {
                return;
            }

            try
            {
                document = JObject.Parse(File.ReadAllText(DataPath));
            }
            catch
            {
                // 文件坏了跟没有一样：调用方拿到空列表，界面退化成没有候选，但不会起不来
                document = [];
            }
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
            }

            return DefaultRelativePath;
        }

        /// <summary>取一个字符串数组；键不存在或形状不对时返回空列表。</summary>
        public IReadOnlyList<string> GetValues(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return [];
            }

            if (document[key] is not JArray array)
            {
                return [];
            }

            return [.. array.Select(item => (item as JValue)?.Value as string).Where(item => !string.IsNullOrWhiteSpace(item))];
        }

        /// <summary>取一个"名字 → 取值域"映射；键不存在时返回空字典。</summary>
        public IReadOnlyDictionary<string, string> GetMap(string key)
        {
            Dictionary<string, string> result = [];
            if (string.IsNullOrWhiteSpace(key) || document[key] is not JObject map)
            {
                return result;
            }

            foreach (JProperty property in map.Properties())
            {
                if (property.Value is JValue value && value.Value is string text && !string.IsNullOrWhiteSpace(text))
                {
                    result[property.Name] = text;
                }
            }

            return result;
        }
    }
}
