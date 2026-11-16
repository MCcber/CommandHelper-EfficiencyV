using System;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 诊断调度：调 <see cref="McdocValidator"/> 重算校验，然后把**行上的快照**刷掉（徽标读的是 Head）。
    /// 只读文档、只写行 —— 不重建、不物化、不回写文本；什么时候刷新由 <see cref="McdocPipeline"/> 决定。
    /// </summary>
    public sealed class McdocDiagnostics
    {
        private readonly McdocPipeline pipeline;

        internal McdocDiagnostics(McdocPipeline pipeline)
        {
            this.pipeline = pipeline;
        }

        /// <summary>
        /// 重算诊断，并把**行上的快照**刷掉。
        /// 行的徽标读的是 Head（构造/RefreshHead 时算的），只调 pipeline.Document.ValidateNode 的话
        /// 文档级清单变了、行上那个黄色/红色感叹号却还在（现场就是这么卡住的）。
        /// </summary>
        public void Refresh(McdocTreeNode node, bool refreshHead = false)
        {
            if (node is null || pipeline.Document is null) return;
            using var stage = McdocWriteTrace.Stage("diag.refresh");
            // 自己这一行一定刷；祖先行只在"文字真的变了"时继续往上（必填缺键那类记在父行上）
            var current = node;
            for (var depth = 0; current is not null && depth < 64; depth++)
            {
                var row = pipeline.Projector?.Find(current);
                var before = row?.DiagnosticSummary;
                pipeline.Document.ValidateNode(current);
                if (row is null)
                {
                    current = current.Parent;
                    continue;
                }
                if (refreshHead) row.RefreshHead(); else row.RefreshDiagnosticsOnly();
                if (depth >= 1 && string.Equals(before, row.DiagnosticSummary, StringComparison.Ordinal)) break;
                current = current.Parent;
            }
            // 子树里的行也要重读：ValidateNode 现在连子树一起算，子行不刷就还挂着旧提醒
            var stack = new System.Collections.Generic.Stack<McdocTreeNode>();
            stack.Push(node);
            var guard = 0;
            while (stack.Count > 0 && guard++ < 20000)
            {
                var child = stack.Pop();
                foreach (var next in McdocPresenterRegistry.Default.Children(child)) stack.Push(next);
                if (!ReferenceEquals(child, node))
                {
                    var row = pipeline.Projector?.Find(child);
                    if (row is null) continue;
                    if (refreshHead) row.RefreshHead(); else row.RefreshDiagnosticsOnly();
                }
            }
        }

    }
}
