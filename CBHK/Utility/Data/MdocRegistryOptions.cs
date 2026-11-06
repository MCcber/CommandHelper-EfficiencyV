// IMcdocRegistryOptions 的客户端实现：数据来自 Resource 载入的 Registries/*.json。
// 这样 #[id="advancement"] 之类的字段在 UI 上就是下拉，选项是官方全部成员。
// 复合形式（registry/path/include/exclude/prefix/tags）由 McdocIdSpec.Apply 统一处理。
using CBHK.Model.Constant;
using CBHK.Utility.Data.Mcdoc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CBHK.Utility.Data
{
    public sealed class MdocRegistryOptions : IMcdocRegistryOptions
    {
        private readonly Resource resource;
        private readonly Dictionary<string, List<string>> cache = [];

        public MdocRegistryOptions(Resource resource)
        {
            this.resource = resource;
        }

        public IReadOnlyList<string> GetOptions(McdocIdSpec spec, string version)
        {
            if (resource is null || spec is null || string.IsNullOrEmpty(spec.Registry))
            {
                return [];
            }

            var rawOptions = ReadArray(spec.Registry, version);
            var tagOptions = spec.Tags is "allowed" or "implicit" || spec.Prefix is "!"
                ? ReadArray("tag/" + spec.Registry, version)
                : null;
            return spec.Apply(rawOptions, tagOptions);
        }

        /// <summary>
        /// 跨资源存在性：按规格把候选展开成集合缓存（实例级 —— id 空间不能跨实例/跨版本串）。
        /// 拿不到数据时返回 true（判不了就不判，避免离线全红）。
        /// </summary>
        public bool Contains(McdocIdSpec spec, string value, string version)
        {
            if (spec is null || string.IsNullOrEmpty(value)) return true;
            var options = GetOptions(spec, version);
            if (options is null || options.Count == 0) return true;
            var key = version + "|" + spec.Registry + "|" + spec.Prefix + "|"
                + string.Join(",", spec.Include ?? []) + "|" + (spec.Tags ?? "") + "|" + string.Join(",", spec.Exclude ?? []);
            if (!sets.TryGetValue(key, out var set))
            {
                // 注册表里存的是裸 id（acacia_button），界面里的值带命名空间（minecraft:acacia_button）：
                // 两边都按 #[id] 的 lengthen 语义归一，否则合法值会被判成"不在注册表内"
                set = new HashSet<string>(options.Select(McdocIdSpec.Lengthen), StringComparer.Ordinal);
                sets[key] = set;
            }
            return set.Contains(McdocIdSpec.Lengthen(value));
        }

        private readonly Dictionary<string, HashSet<string>> sets = [];

        private List<string> ReadArray(string name, string version)
        {
            var key = version + "|" + name;
            if (cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            List<string> result = [];
            try
            {
                if (resource.GetRegistriesData(version)?.SelectToken(name.Trim('"')) is JArray array)
                {
                    result = array
                        .Select(item => item?.Type == JTokenType.String ? item.Value<string>() : item?.ToString())
                        .Where(item => !string.IsNullOrEmpty(item))
                        .Distinct()
                        .ToList();
                }
            }
            catch (Exception)
            {
                result = [];
            }
            cache[key] = result;
            return result;
        }
    }
}
