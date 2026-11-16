using System;
using System.Collections.Generic;
using System.Linq;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 物化：把文档里"缺了的必填值"写进去，并且只重解**被写了值的那一层**，多轮跑到不动点。
    /// 它只动文档与 loading 闸门 —— 什么时候物化、物化完投影怎么收口、文本何时回写，全部由
    /// <see cref="McdocPipeline"/> 决定；它不碰投影、不回写文本。
    /// </summary>
    public sealed class McdocMaterializer
    {
        private readonly McdocPipeline pipeline;

        internal McdocMaterializer(McdocPipeline pipeline)
        {
            this.pipeline = pipeline;
        }

        public int MaterializeUntilStable(bool includeDisplayedDefaults = false, int maxPasses = 3)
        {
            var total = 0;
            for (var pass = 0; pass < maxPasses; pass++)
            {
                // includeDisplayedDefaults 只对第一轮有效：之后是"重建新长出来的字段"，不是载入补默认值
                var written = MaterializeOnce(includeDisplayedDefaults && pass == 0);
                if (written == 0) return total;
                total += written;
            }
            McdocWriteTrace.Log("物化未收敛", null, "已写 " + total + " 个键，到达 " + maxPasses + " 轮上限");
            return total;
        }

        /// <summary>
        /// 物化写值之后只重解"被写了值的那一层"（默认）。设 <c>MCDOC_TARGETED_MATERIALIZE=0</c>
        /// 退回"每轮整篇 Load"的旧行为 —— 定向重建是性能改动，这是它的一键逃生阀。
        /// </summary>
        public static bool TargetedMaterialize { get; set; } = !string.Equals(
            Environment.GetEnvironmentVariable("MCDOC_TARGETED_MATERIALIZE"), "0", StringComparison.Ordinal);

        public int MaterializeOnce(bool includeDisplayedDefaults)
        {
            var wasLoading = pipeline.GetLoading?.Invoke() ?? false;
            pipeline.SetLoading?.Invoke(true);
            try
            {
                int materialized;
                var writtenNodes = new List<McdocMaterializeWrite>();
                using (McdocWriteTrace.Stage("mat.walk"))
                {
                    materialized = McdocCollectionEditor.Default.MaterializeRequiredSelections(
                        pipeline.Document.Nodes, includeDisplayedDefaults, pipeline.Document.CreateRuntime(), writtenNodes);
                }
                if (materialized == 0) return 0;
                if (!TargetedMaterialize)
                {
                    using (McdocWriteTrace.Stage("mat.load")) pipeline.Document.Load(pipeline.RootType?.Invoke(), pipeline.Document.RootJson);
                    return materialized;
                }
                RebuildWritten(writtenNodes);
                return materialized;
            }
            finally
            {
                pipeline.SetLoading?.Invoke(wasLoading);
            }
        }

        /// <summary>
        /// 物化写完之后：只重解**被写了值的那一层**，而不是整篇 <see cref="McdocTreeDocument.Load"/>。
        /// 写值只是改文档，重建才会让派发器/动态索引按新键析出新的必填字段；
        /// 不动点由 MaterializeUntilStable 的多轮保证。先浅后深（祖先重建会连带子孙），
        /// 任何一个重解不出来就回落整篇 Load —— 语义与"每轮全量重解"等价，只是范围更小。
        /// </summary>
        private void RebuildWritten(List<McdocMaterializeWrite> writtenNodes)
        {
            if (writtenNodes.Count == 0 || pipeline.Document is null) return;
            var ordered = writtenNodes
                .Where(write => write?.Node is not null && pipeline.Document.IsAttached(write.Node))
                .OrderBy(write => (write.Node.JsonPath ?? "").Length)
                .ToList();
            var rebuiltPaths = new List<string>();
            foreach (var write in ordered)
            {
                // 只重解**被写了值的那一层**：上提到父容器看到起来更"安全"，
                // 但实测会把"根列表唯一元素删除"这类删除手势吃掉（父容器的 Json 还是删之前那一代），
                // 兄弟分支的依赖由 RebuildDependents 那条既有出口负责，不在这里兜。
                var path = write.Node.JsonPath ?? "";
                if (rebuiltPaths.Any(ancestor => IsBelow(path, ancestor))) continue;
                if (pipeline.Document.RebuildNode(write.Node) is null)
                {
                    using (McdocWriteTrace.Stage("mat.load")) pipeline.Document.Load(pipeline.RootType?.Invoke(), pipeline.Document.RootJson);
                    return;
                }
                rebuiltPaths.Add(path);
            }
            // 空壳是这一笔补出来的：新节点要按"建节点时的默认展开口径"撑开（depth < 默认深度 + 有值），
            // 否则定向重建会把整篇 Load 那条路默认会展开的新容器建出来却是折叠的（行上少一整片子行）。
            foreach (var write in ordered.Where(write => write.WasEmpty))
            {
                var node = pipeline.Document.FindNode(write.Node.JsonPath ?? "");
                if (node is null || node.Json is null || node.IsExpanded) continue;
                if (node.Depth < McdocNodeFactory.DefaultExpandDepth && McdocPresenterRegistry.Default.CanHaveChildren(node))
                {
                    node.IsExpanded = true;
                }
            }
        }

        /// <summary>只给一个节点写它缺的必填值（投影刚给它建出子级时补跑）。返回写了几个键。</summary>
        public int MaterializeChildDefaults(McdocTreeNode node)
        {
            if (node is null || pipeline.Document?.IsAttached(node) != true) return 0;
            var wasLoading = pipeline.GetLoading?.Invoke() ?? false;
            pipeline.SetLoading?.Invoke(true);
            try
            {
                return McdocCollectionEditor.Default.MaterializeRequiredSelections([node], false, pipeline.Document.CreateRuntime());
            }
            catch (Exception)
            {
                return 0;
            }
            finally
            {
                pipeline.SetLoading?.Invoke(wasLoading);
            }
        }

        private static bool IsBelow(string path, string ancestor)
            => !string.IsNullOrEmpty(ancestor) && path.Length > ancestor.Length
               && path.StartsWith(ancestor, StringComparison.Ordinal)
               && (path[ancestor.Length] == '.' || path[ancestor.Length] == '[');

    }
}
