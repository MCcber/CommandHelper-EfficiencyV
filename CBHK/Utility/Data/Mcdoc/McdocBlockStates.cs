using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// BlockState 资产 → 「mcdoc:block_states」调度器成员。
    /// schema 侧照旧用 mcdoc:block_states[[block]] 索引（block_state.mcdoc 的 dispatch 目标），
    /// 这里只负责把「每个方块有哪些属性、属性有哪些候选值」按当前游戏版本喂进调度器表。
    /// 不按路径 / 类型名做任何特判：属性成员完全由资产决定，schema 那边的索引规则原样生效。
    /// </summary>
    public static class McdocBlockStates
    {
        /// <summary>schema 里 <c>mcdoc:block_states</c> 的调度器空间名。</summary>
        public const string Registry = "mcdoc:block_states";

        private const string BlockPrefix = "minecraft:";
        private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, McdocType>> Cache =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>运行期资产目录：与 Resource 里的拼法一致（相对 exe，不写绝对路径）。</summary>
        public static string DefaultDirectory =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource", "Registries", "BlockState");

        /// <summary>
        /// 读某个版本的 BlockState 资产，产出「方块 id（去掉 minecraft: 前缀）→ 属性结构」。
        /// 版本文件不存在时按「不高于当前版本的最近一份，再不行取最高可用」回退；同一版本只解析一次。
        /// </summary>
        public static IReadOnlyDictionary<string, McdocType> Load(string gameVersion, string blockStateDir = null)
        {
            var directory = string.IsNullOrWhiteSpace(blockStateDir) ? DefaultDirectory : blockStateDir;
            var versionFile = ResolveVersionFile(directory, gameVersion);
            if (versionFile is null) return new Dictionary<string, McdocType>(StringComparer.Ordinal);
            return Cache.GetOrAdd(versionFile, file => Parse(file));
        }

        /// <summary>把每个方块的属性结构注册进调度器表（同 id 覆盖，天然幂等）。返回注册数量。</summary>
        public static int Register(McdocTypeIndex index, string gameVersion, string blockStateDir = null)
        {
            if (index is null) return 0;
            var members = Load(gameVersion, blockStateDir);
            var registered = 0;
            foreach (var pair in members)
            {
                index.RegisterDispatch(Registry, pair.Key, pair.Value);
                registered++;
            }
            return registered;
        }

        /// <summary>版本选择：精确 → 不高于当前版本的最近一份 → 最高可用（与注册表数据的回退口径一致）。</summary>
        private static string ResolveVersionFile(string directory, string gameVersion)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return null;
            var files = Directory.GetFiles(directory, "*.json");
            if (files.Length == 0) return null;
            var available = files
                .Select(file => (File: file, Key: Path.GetFileNameWithoutExtension(file)))
                .Where(entry => ParseVersionKey(entry.Key) is not null)
                .ToList();
            if (available.Count == 0) return null;

            if (!string.IsNullOrWhiteSpace(gameVersion))
            {
                var exact = available.FirstOrDefault(entry =>
                    string.Equals(entry.Key, gameVersion, StringComparison.OrdinalIgnoreCase));
                if (exact.File is not null) return exact.File;
            }

            var requested = ParseVersionKey(gameVersion);
            if (requested is not null)
            {
                var notAbove = available
                    .Where(entry => ParseVersionKey(entry.Key) <= requested)
                    .OrderByDescending(entry => ParseVersionKey(entry.Key))
                    .FirstOrDefault();
                if (notAbove.File is not null) return notAbove.File;
            }

            return available.OrderByDescending(entry => ParseVersionKey(entry.Key)).First().File;
        }

        private static Version ParseVersionKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            var pieces = key.Trim().Split('.');
            var numbers = new List<int>(pieces.Length);
            foreach (var piece in pieces)
            {
                if (!int.TryParse(piece, out var number)) return null;
                numbers.Add(number);
            }
            while (numbers.Count < 4) numbers.Add(0);
            return new Version(numbers[0], numbers[1], numbers[2], numbers[3]);
        }

        /// <summary>
        /// 资产格式：{ "方块id": [ { 属性名: [候选值…] }, { 默认状态 } ] }。
        /// 元素[0] 是属性全集；元素[1] 是默认状态，这里不用（属性本来就都是可选的）。
        /// </summary>
        private static IReadOnlyDictionary<string, McdocType> Parse(string path)
        {
            var result = new Dictionary<string, McdocType>(StringComparer.Ordinal);
            var root = JObject.Parse(File.ReadAllText(path));
            foreach (var block in root.Properties())
            {
                if (block.Value is not JArray states || states.Count == 0 || states[0] is not JObject properties) continue;
                var id = block.Name.StartsWith(BlockPrefix, StringComparison.Ordinal)
                    ? block.Name.Substring(BlockPrefix.Length)
                    : block.Name;

                var structure = McdocType.Create(McdocTypeKind.Struct);
                structure.Fields = [];
                foreach (var property in properties.Properties())
                {
                    if (property.Value is not JArray candidates) continue;
                    var values = candidates
                        .Select(candidate => Convert.ToString(candidate))
                        .Where(value => !string.IsNullOrEmpty(value))
                        .ToList();
                    if (values.Count == 0) continue;

                    var enumType = McdocType.Create(McdocTypeKind.Enum);
                    enumType.EnumKind = McdocTypeKind.String;
                    enumType.EnumValues = values
                        .Select(value => new McdocEnumValue { Identifier = value, Value = value })
                        .ToList();
                    structure.Fields.Add(new McdocStructField
                    {
                        Name = property.Name,
                        Key = McdocType.LiteralString(property.Name),
                        Type = enumType,
                        // 方块状态的每个属性都是可选的：默认状态才要求全给
                        Optional = true
                    });
                }
                result[id] = structure;
            }
            return result;
        }
    }
}
