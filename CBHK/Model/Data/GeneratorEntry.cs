using CBHK.Model.Constant;

namespace CBHK.Model.Data
{
    /// <summary>
    /// 主页生成器入口。Id 同时是 DistributorGenerator.commandDictionary 的键和图标文件名；
    /// FunctionCategory / OutputCategory 供主页的分类过滤使用，两者都是位标志，FunctionCategory
    /// 可同时挂多个类别（例如魔咒既在数据包里又在资源里）；McdocEntryId 是入口在 mcdoc
    /// dispatcher 里的键；SinceVersion / UntilVersion 是主页版本过滤的静态区间，保持与 mcdoc
    /// dispatch 上的 #[since]/#[until] 一致，旧式生成器留空表示所有版本可用。
    /// </summary>
    public sealed record GeneratorEntry(string Id, string Name)
    {
        public GeneratorCategory FunctionCategory { get; init; } = GeneratorCategory.Resource;
        public GeneratorCategory OutputCategory { get; init; } = GeneratorCategory.JsonFile;
        public string McdocEntryId { get; init; }
        public string SinceVersion { get; init; }
        public string UntilVersion { get; init; }

        public string ChinaPinyin { get; init; }
        public string EnglishWord { get; init; }
    }
}
