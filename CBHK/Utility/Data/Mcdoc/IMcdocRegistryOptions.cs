// 注册表候选值来源（#[id] 引用的资源名集合）。
// Mcdoc 命名空间保持零依赖：真正实现由客户端包装 Resource.GetRegistriesData 提供。
using System.Collections.Generic;
using System.Linq;

namespace CBHK.Utility.Data.Mcdoc
{
    public interface IMcdocRegistryOptions
    {
        /// <summary>按 #[id] 规格返回候选值（已按 include/exclude/prefix/path/tags 处理）。</summary>
        IReadOnlyList<string> GetOptions(McdocIdSpec spec, string version);

        /// <summary>
        /// 值是否在该 #[id] 规格的候选集合内（跨资源存在性校验用）。
        /// 默认实现退回 GetOptions；拿不到数据（离线/该版本没有这个注册表）时返回 true —— 判不了就不判。
        /// </summary>
        bool Contains(McdocIdSpec spec, string value, string version)
        {
            if (spec is null || string.IsNullOrEmpty(value)) return true;
            var options = GetOptions(spec, version);
            return options is null || options.Count == 0 || options.Contains(value);
        }
    }
}
