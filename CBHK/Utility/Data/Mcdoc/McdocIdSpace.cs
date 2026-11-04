// #[id] 的"取值/取键空间"解析（求值层，不是渲染层）。
// 两个 id 空间：注册表（Registries/*.json）与 mcdoc 调度器（dispatch minecraft:X[...] 的索引成员）。
// 规则（管线级）：
//   1) 注册表有数据 → 用它（item/block/advancement/trigger_type 这些既有空间保持原状）
//   2) 注册表没有数据 → 用同名调度器的索引成员（environment_attribute 这类"文档里有全部 Index 成员、但没有注册表数据"的空间）
//      名字匹配顺序：minecraft:X → X → 去掉尾部 _type 再试
//   3) 两个空间同名但不是一回事（dispatcher 覆盖不了注册表，如 block 1105 vs 57）→ 仍用注册表
//   4) 调度器成员里的 %none/%unknown/%fallback 永远不进候选
// 候选在 simplify 阶段解析一次、挂到 McdocType 上（IdMembers/IdSource），presenter 只读结果。
using System;
using System.Collections.Generic;
using System.Linq;

namespace CBHK.Utility.Data.Mcdoc
{
    public enum McdocIdSpaceSource
    {
        None,
        Registry,
        Dispatcher,
    }

    public sealed class McdocIdSpaceResult
    {
        public IReadOnlyList<string> Members = [];
        public McdocIdSpaceSource Source = McdocIdSpaceSource.None;
        public string Space = "";
    }

    /// <summary>一次求值会话内的 id 空间解析器（缓存随 runtime 生命周期）。</summary>
    public sealed class McdocIdSpaceService
    {
        private readonly Dictionary<string, McdocIdSpaceResult> cache = new(StringComparer.Ordinal);

        public McdocIdSpaceResult Resolve(McdocIdSpec spec, McdocRuntime runtime)
        {
            if (spec is null || string.IsNullOrEmpty(spec.Registry) || runtime is null)
            {
                return new McdocIdSpaceResult();
            }

            var key = string.Join("|",
                runtime.DocumentUri ?? "",
                runtime.GameVersion ?? "",
                spec.Registry,
                spec.Prefix ?? "",
                string.Join(",", spec.Include ?? []),
                string.Join(",", spec.Exclude ?? []));
            if (cache.TryGetValue(key, out var cached)) return cached;

            var result = new McdocIdSpaceResult { Space = spec.Registry };
            var registry = ReadRegistryOptions(spec, runtime);
            var dispatcher = ReadDispatcherOptions(spec, runtime);

            // 优先级：**先调度器、再符号表/注册表**。
            // dispatcher 的索引成员来自文档本身，通常比本地注册表快照全（trigger 就多 4 个）。
            // 唯一的例外：两个空间只是同名却不是一回事（block 1105 vs 同名 dispatcher 57），
            // 这时 dispatcher 覆盖不了注册表，仍然用注册表，否则候选会凭空缩水。
            if (dispatcher.Count > 0 && (registry.Count == 0 || CoversAll(registry, dispatcher, spec)))
            {
                result.Members = StyleDispatcher(dispatcher, registry, spec);
                result.Source = McdocIdSpaceSource.Dispatcher;
            }
            else if (registry.Count > 0)
            {
                result.Members = registry;
                result.Source = McdocIdSpaceSource.Registry;
            }

            cache[key] = result;
            return result;
        }

        /// <summary>
        /// dispatcher 完全覆盖注册表时，返回"注册表没写、但文档索引里有"的成员（按注册表风格补前缀）；
        /// 不能完全覆盖则返回空（说明两个空间不是一回事，例如 block_type ↔ block）。
        /// </summary>
        /// <summary>dispatcher 的索引成员是否覆盖注册表（去前缀、去命名空间后比较）。</summary>
        private static bool CoversAll(IReadOnlyList<string> registry, IReadOnlyList<string> dispatcher, McdocIdSpec spec)
        {
            if (registry is not { Count: > 0 } || dispatcher is not { Count: > 0 }) return false;
            static string StripPrefix(string value, string prefix)
                => !string.IsNullOrEmpty(prefix) && value.StartsWith(prefix, StringComparison.Ordinal)
                    ? value.Substring(prefix.Length)
                    : value;
            static string StripNamespace(string value) => value.StartsWith("minecraft:", StringComparison.Ordinal)
                ? value.Substring("minecraft:".Length)
                : value;
            string Norm(string value) => StripNamespace(StripPrefix(value, spec?.Prefix));
            var dispatcherSet = new HashSet<string>(dispatcher.Select(Norm), StringComparer.Ordinal);
            return registry.All(member => dispatcherSet.Contains(Norm(member)));
        }

        /// <summary>把 dispatcher 的索引成员写成"注册表风格"（注册表带命名空间时补 minecraft:，带 prefix 时补 prefix）。</summary>
        private static IReadOnlyList<string> StyleDispatcher(IReadOnlyList<string> dispatcher, IReadOnlyList<string> registry,
            McdocIdSpec spec)
        {
            if (dispatcher is not { Count: > 0 }) return [];
            static string StripPrefix(string value, string prefix)
                => !string.IsNullOrEmpty(prefix) && value.StartsWith(prefix, StringComparison.Ordinal)
                    ? value.Substring(prefix.Length)
                    : value;
            var prefixed = registry is { Count: > 0 }
                && registry.Any(item => StripPrefix(item, spec?.Prefix).Contains(':', StringComparison.Ordinal));
            var result = new List<string>(dispatcher.Count);
            foreach (var member in dispatcher)
            {
                var value = (spec?.Prefix ?? "") + (prefixed && !member.Contains(':', StringComparison.Ordinal)
                    ? "minecraft:" + member
                    : member);
                if (!result.Contains(value)) result.Add(value);
            }
            return result;
        }

        private static IReadOnlyList<string> ReadRegistryOptions(McdocIdSpec spec, McdocRuntime runtime)
        {
            if (runtime.RegistryOptions is null) return [];
            try
            {
                var raw = runtime.RegistryOptions.GetOptions(spec, runtime.GameVersion ?? "1.21");
                return raw is null ? [] : raw.ToList();
            }
            catch (Exception)
            {
                return [];
            }
        }

        private static IReadOnlyList<string> ReadDispatcherOptions(McdocIdSpec spec, McdocRuntime runtime)
        {
            var symbols = runtime.Symbols;
            if (symbols is null) return [];
            try
            {
                var members = symbols.GetDispatcherMembers("minecraft:" + spec.Registry, runtime.DocumentUri)
                    ?? symbols.GetDispatcherMembers(spec.Registry, runtime.DocumentUri);
                if (members is null && spec.Registry.EndsWith("_type", StringComparison.Ordinal))
                {
                    var stripped = spec.Registry.Substring(0, spec.Registry.Length - "_type".Length);
                    members = symbols.GetDispatcherMembers("minecraft:" + stripped, runtime.DocumentUri)
                        ?? symbols.GetDispatcherMembers(stripped, runtime.DocumentUri);
                }
                if (members is null) return [];

                var result = new List<string>();
                foreach (var pair in members)
                {
                    var name = pair.Key;
                    if (string.IsNullOrEmpty(name) || name.StartsWith("%", StringComparison.Ordinal)) continue;
                    if (!spec.AcceptsDispatcherMember(name)) continue;
                    // 成员也要过版本过滤：#[until="1.21.5"] 的成员在 1.21.8 里已经不存在了，
                    // 放进候选就会让用户选到一个"求解期会被滤掉"的键（spread 解不出、只剩一条诊断）。
                    if (!runtime.Attributes.ShouldKeep(pair.Value?.Attributes, runtime)) continue;
                    if (!result.Contains(name)) result.Add(name);
                }
                result.Sort(StringComparer.Ordinal);
                return result;
            }
            catch (Exception)
            {
                return [];
            }
        }
    }
}
