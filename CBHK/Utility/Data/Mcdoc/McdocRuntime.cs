// 求值运行时：把 spyglass 的 RuntimeNode<T> / SimplifyContext<T> / McdocCheckerContext<T>
// 压成一组最小类型。T 在 C# 里退化为 object，原始节点由调用方自己解释（WPF 侧是 JSON AST 节点）。
using System;
using System.Collections.Generic;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>等值于 spyglass RuntimeNode&lt;T&gt;：原始节点 + 对它的类型推断。</summary>
    public sealed class McdocRuntimeNode
    {
        public object OriginalNode;
        public McdocType InferredType = new();

        public McdocRuntimeNode() { }
        public McdocRuntimeNode(object originalNode, McdocType inferredType)
        {
            OriginalNode = originalNode;
            InferredType = inferredType;
        }

        public static McdocRuntimeNode Unknown() => new(null, McdocType.Any());
    }

    /// <summary>等值于 spyglass RuntimePair&lt;T&gt;：一个 key 及其可能的值。</summary>
    public sealed class McdocChildPair
    {
        public McdocRuntimeNode Key = McdocRuntimeNode.Unknown();
        public List<McdocRuntimeNode> PossibleValues = [];
    }

    /// <summary>由宿主实现的"取子节点"访问器（JSON AST → 键值对）。</summary>
    public interface IMcdocNodeAccessor
    {
        IEnumerable<McdocChildPair> GetChildren(object node, McdocType simplified);
    }

    /// <summary>符号查询：类型定义与 dispatcher 成员。</summary>
    public interface IMcdocSymbols
    {
        McdocType GetTypeDef(string path, string documentUri, string modulePath = null);
        IReadOnlyDictionary<string, McdocType> GetDispatcherMembers(string registry, string documentUri);
    }

    /// <summary>一次求值所需的全部外部依赖（等价 spyglass 的 McdocCheckerContext 的可移植子集）。</summary>
    public sealed class McdocRuntime
    {
        public string DocumentUri = "";
        /// <summary>当前解析上下文所在的 mcdoc 模块（相对名 / use 别名都以它为基准）。</summary>
        public string ModulePath = "";
        public IMcdocSymbols Symbols;
        /// <summary>可选：注册表候选值来源（#[id] 的下拉选项）。为 null 时退化成自由输入。</summary>
        public IMcdocRegistryOptions RegistryOptions;
        /// <summary>查注册表用的游戏版本（如 "1.21"）。</summary>
        public string GameVersion = "1.21";
        public IMcdocNodeAccessor Accessor;
        public McdocAttributeRegistry Attributes = new();
        public bool AllowMissingKeys;
        public bool RequireCanonical;

        /// <summary>
        /// 本次构建要沿用的"用户显式选过的 union 分支"（JsonPath → 成员下标）。
        /// 局部重建是"按声明类型重跑一遍"，不带这个提示就只能按 JSON 值猜；并列时猜会退回首个成员，
        /// 表现是切完分支一重建就自己弹回去。由 <see cref="McdocTreeDocument.RebuildNode"/> 填入，
        /// Runtime 一次构建一个，不会跨会话串味。
        /// </summary>
        public IReadOnlyDictionary<string, int> UnionSelectionHints;
        public Action<string> Warn;
        /// <summary>#[id] 候选空间解析器（注册表 / 调度器索引成员），缓存随 runtime 生命周期。</summary>
        public McdocIdSpaceService IdSpaces = new();

        public void Report(string message) => Warn?.Invoke(message);

        /// <summary>复制一份运行时并切换模块上下文（用于"进入某个类型自己的命名空间"）。</summary>
        public McdocRuntime WithModule(string modulePath)
        {
            var clone = (McdocRuntime)MemberwiseClone();
            clone.ModulePath = modulePath ?? "";
            return clone;
        }
    }

    /// <summary>正在求值的节点：自身 + 父级 + 父级的 key。动态键/%parent 全靠这三个字段。</summary>
    public sealed class McdocValueNode
    {
        public McdocValueNode Parent;
        public McdocRuntimeNode RuntimeKey;
        public McdocRuntimeNode Node = McdocRuntimeNode.Unknown();

        public static McdocValueNode Of(McdocRuntimeNode node, McdocValueNode parent = null, McdocRuntimeNode runtimeKey = null)
            => new() { Node = node, Parent = parent, RuntimeKey = runtimeKey };
    }

    /// <summary>等价 spyglass SimplifyContext&lt;T&gt;。</summary>
    public sealed class McdocSimplifyContext
    {
        public McdocRuntime Runtime;
        public McdocValueNode Node;
        public bool IsMember;
        public IReadOnlyList<McdocType> TypeArgs;
        public IReadOnlyDictionary<string, McdocType> TypeMapping;

        /// <summary>
        /// 正在解析中的引用路径（用于打断 A→B→A 的引用环）。
        /// Clone() 是浅拷贝，所以派生上下文共享同一个集合。
        /// </summary>
        public HashSet<string> ResolvingReferences { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// 求值诊断（引用解不开 / 不可索引 / 成员丢失）。Clone 共享同一个列表，
        /// 工厂求值完统一取走挂到行上——"any 兜底"必须可见，不能只写日志。
        /// </summary>
        public List<string> Diagnostics { get; } = new();

        /// <summary>追踪用标签（节点路径），只在开了 IndexTrace 时有意义。</summary>
        public string Tag;

        public McdocSimplifyContext Clone()
        {
            var copy = (McdocSimplifyContext)MemberwiseClone();
            return copy;
        }
    }

    public readonly struct McdocSimplifyResult
    {
        public readonly McdocType TypeDef;
        /// <summary>true 表示结果依赖运行期节点（不能被缓存）。</summary>
        public readonly bool DynamicData;

        public McdocSimplifyResult(McdocType typeDef, bool dynamicData = false)
        {
            TypeDef = typeDef;
            DynamicData = dynamicData;
        }
    }
}
