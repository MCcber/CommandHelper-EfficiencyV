using System;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 诊断调度：调 <see cref="McdocValidator"/> 重算校验，然后把**行上的快照**刷掉（徽标读的是 Head）。
    /// 只读文档、只写行 —— 不重建、不物化、不回写文本；什么时候刷新由 <see cref="McdocPipeline"/> 决定。
    /// </summary>
    public sealed class McdocDiagnostics
    {
        private readonly IMcdocPipelineHost host;

        internal McdocDiagnostics(IMcdocPipelineHost host)
        {
            this.host = host;
        }

        /// <summary>
        /// 重算诊断，并把**行上的快照**刷掉。
        /// 行的徽标读的是 Head（构造/RefreshHead 时算的），只调 host.Document.ValidateNode 的话
        /// 文档级清单变了、行上那个黄色/红色感叹号却还在（现场就是这么卡住的）。
        /// </summary>
        public void Refresh(McdocTreeNode node, bool refreshHead = false, bool nodeValidated = false)
        {
            if (node is null || host.Document is null) return;
            using var stage = McdocWriteTrace.Stage("diag.refresh");
            // 自己这一行一定刷；祖先行只在"文字真的变了"时继续往上（必填缺键那类记在父行上）
            var current = node;
            for (var depth = 0; current is not null && depth < 64; depth++)
            {
                var row = host.Projector?.Find(current);
                var before = row?.DiagnosticSummary;
                // 脏节点自己的子树校验在 RefreshNode 里已经做过（nodeValidated）；祖先只算它自己 ——
                // 对祖先走子树等于把整棵子树按每一级重算一遍，1MB 档在根上就是 9.4 万节点。
                if (nodeValidated && depth == 0) { }
                else if (depth == 0) host.Document.ValidateNode(current);
                else host.Document.ValidateSelf(current);
                if (row is null)
                {
                    current = current.Parent;
                    continue;
                }
                if (refreshHead) row.RefreshHead(); else row.RefreshDiagnosticsOnly();
                if (depth >= 1 && string.Equals(before, row.DiagnosticSummary, StringComparison.Ordinal)) break;
                current = current.Parent;
            }
            // 子树里的行也要重读：ValidateNode 现在连子树一起算，子行不刷就还挂着旧提醒。
            // 走行而不是走节点：1MB 档这棵子树有 8 万多个节点，其中的行只有几十个 ——
            // 按节点遍历每次结构编辑都要撞 20000 的守卫上限（实测 20~155ms），而没行的节点根本没有要刷的东西。
            if (host.Projector is null) return;
            var path = node.JsonPath ?? "$";
            var rows = new McdocRow[host.Projector.Rows.Count];
            host.Projector.Rows.CopyTo(rows, 0);
            foreach (var row in rows)
            {
                if (row?.Node is null || ReferenceEquals(row.Node, node)) continue;
                if (!McdocTreeDocument.SameOrBelow(row.Node.JsonPath, path)) continue;
                if (refreshHead) row.RefreshHead(); else row.RefreshDiagnosticsOnly();
            }
        }

    }
}
