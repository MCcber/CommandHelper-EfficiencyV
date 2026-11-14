using CBHK.CustomControl.VectorComboBox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CBHK.Model.Constant
{
    /// <summary>
    /// 配方格式有改动的 Java 版版本集合；下拉成员直接用 VectorTextComboBoxItem。
    /// 版本集合取自本地 mcdoc 资产 vanilla-mcdoc/java/data/recipe.mcdoc 里的
    /// #[since="..."] / #[until="..."] 标注（配方序列化器与字段的变更历史）；
    /// 资产缺失时退回下面写死的同一组版本。
    /// </summary>
    public static class RecipeVersionCatalog
    {
        private const string RecipeSchemaRelativePath = @"Resource\vanilla-mcdoc\java\data\recipe.mcdoc";

        private static readonly string[] FallbackMilestones = ["1.19.4", "1.20", "1.20.5", "1.21.2", "1.21.5"];

        private static readonly string[] VersionMarkers = ["#[since=", "#[until="];

        /// <summary>
        /// 构造版本下拉成员：mcdoc 里记录的变更版本 + 数据目录里的最高可用版本
        /// </summary>
        public static IReadOnlyList<VectorTextComboBoxItem> CreateMembers(Resource resource)
        {
            List<string> versions = GetMilestones();
            string latest = GetAvailableKeys(resource).LastOrDefault();
            if (latest is not null && !versions.Contains(latest))
                versions.Add(latest);

            return [.. versions.Select(version => new VectorTextComboBoxItem
            {
                ItemID = version,
                Text = version
            })];
        }

        /// <summary>
        /// 从本地配方 mcdoc 的 since/until 标注读出版本里程碑；每次调用按资产现算，不做缓存
        /// </summary>
        public static List<string> GetMilestones()
        {
            string schemaPath = AppDomain.CurrentDomain.BaseDirectory + RecipeSchemaRelativePath;
            if (!File.Exists(schemaPath))
                return [.. FallbackMilestones];

            HashSet<string> versions = new(StringComparer.OrdinalIgnoreCase);
            string content = File.ReadAllText(schemaPath);

            foreach (string marker in VersionMarkers)
            {
                int index = content.IndexOf(marker, StringComparison.Ordinal);
                while (index >= 0)
                {
                    index += marker.Length;
                    if (index < content.Length && content[index] == '"')
                    {
                        int end = content.IndexOf('"', index + 1);
                        if (end > index)
                            versions.Add(content[(index + 1)..end]);
                    }
                    index = content.IndexOf(marker, index, StringComparison.Ordinal);
                }
            }

            if (versions.Count == 0)
                return [.. FallbackMilestones];

            return [.. versions.OrderBy(version => version, Comparer<string>.Create(CompareVersion))];
        }

        /// <summary>
        /// 选中版本落到实际注册表键：取不大于该版本的最高可用键；没有更低的就用最低可用键
        /// </summary>
        public static string ResolveDataKey(Resource resource, string version)
        {
            List<string> keys = GetAvailableKeys(resource);
            if (keys.Count == 0)
                return null;
            if (string.IsNullOrWhiteSpace(version))
                return keys[^1];

            string resolved = null;
            foreach (string key in keys)
            {
                if (CompareVersion(key, version) <= 0)
                    resolved = key;
            }
            return resolved ?? keys[0];
        }

        private static List<string> GetAvailableKeys(Resource resource)
        {
            if (resource?.RunningDataObject is null)
                return [];

            return [.. resource.RunningDataObject.Properties()
                .Select(property => property.Name)
                .OrderBy(key => key, Comparer<string>.Create(CompareVersion))];
        }

        /// <summary>
        /// 按段比较版本号，段内按数字比，非数字段按字符串比
        /// </summary>
        private static int CompareVersion(string left, string right)
        {
            string[] leftParts = (left ?? "").Split('.');
            string[] rightParts = (right ?? "").Split('.');
            int length = Math.Max(leftParts.Length, rightParts.Length);

            for (int i = 0; i < length; i++)
            {
                string leftPart = i < leftParts.Length ? leftParts[i] : "0";
                string rightPart = i < rightParts.Length ? rightParts[i] : "0";

                int result = int.TryParse(leftPart, out int leftNumber) && int.TryParse(rightPart, out int rightNumber)
                    ? leftNumber.CompareTo(rightNumber)
                    : string.CompareOrdinal(leftPart, rightPart);
                if (result != 0)
                    return result;
            }
            return 0;
        }
    }
}
