using System;
using MinecraftLanguageModelLibrary.Data;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 新增内容的默认值只有一个出口：先把声明类型按求解代数求值成归一形态，再取默认值。
    /// 缺省字段 ⊕ / 列表追加 / 动态键提交 / 页面默认文档都必须走这里 —— 任何入口自己拼 GetDefault、
    /// 或按类型写"struct 就给空对象"这类特例，都会把必选字段悄悄丢掉，产出与文档声明的结构不符。
    /// </summary>
    public static class McdocDefaults
    {
        /// <summary>默认值生成的可变输入：#[pack_format] 跟版本走，并且字段类型先求值（与建树同口径）。</summary>
        public static McdocDefaultOptions Options(McdocRuntime runtime)
        {
            var options = new McdocDefaultOptions { PackFormat = McdocHelpers.ResolvePackFormat(runtime?.GameVersion) };
            return runtime is null ? options : McdocHelpers.BindSimplify(options, runtime);
        }

        /// <summary>声明类型 → 归一形态。求值失败退 any（不是空联合），保持"解不开也要看得见"的既有约定。</summary>
        public static McdocType ResolveType(McdocType declared, McdocRuntime runtime, McdocValueNode node = null)
        {
            if (declared is null)
            {
                return McdocType.Any();
            }

            try
            {
                return McdocSimplifier.Simplify(declared, new McdocSimplifyContext
                {
                    Runtime = runtime,
                    Node = node ?? DefaultNode(),
                }).TypeDef;
            }
            catch (Exception)
            {
                return McdocType.Any();
            }
        }

        /// <summary>
        /// 声明类型 → 该结构的默认值（必选字段递归带上）。
        /// 解不开的类型按 any 处理（README §四.10）：any 的默认值就是 {}，绝不返回 null ——
        /// null 不是"默认值"，它只会让新增的条目变成文档里的空值。
        /// </summary>
        public static McdocJsonValue Resolve(McdocType declared, McdocRuntime runtime, McdocValueNode node = null)
        {
            var options = Options(runtime);
            return McdocHelpers.GetDefault(ResolveType(declared, runtime, node), options)
                ?? McdocHelpers.GetDefault(McdocType.Any(), options);
        }

        /// <summary>默认上下文：没有具体键、没有父级约束。</summary>
        public static McdocValueNode DefaultNode()
            => McdocValueNode.Of(new McdocRuntimeNode(null, McdocType.Any()));

        /// <summary>代数给出的值是否可物化（null 只可能来自"解不开"，不是合法的文档值）。</summary>
        public static bool CanMaterialize(McdocJsonValue value)
            => value is not null && value.Kind != McdocJsonKind.Null;

        /// <summary>声明类型是否没解出具体形态（退化成了 any）—— 用来挂"解不开"的可见诊断。</summary>
        public static bool IsUnresolved(McdocType declared, McdocRuntime runtime, McdocValueNode node = null)
            => declared is not null
                && declared.Kind != McdocTypeKind.Any
                && ResolveType(declared, runtime, node).Kind == McdocTypeKind.Any;
    }
}
