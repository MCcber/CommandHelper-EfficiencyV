using System;
using System.Linq;

namespace CBHK.Probe
{
    /// <summary>
    /// mcdoc 渲染管线的判据探针（门禁落点）：
    /// 退出码 0 = 全绿，1 = 有红，2 = 环境不足（不许当成 SKIP 混过去）。
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            // --check-trace 带一个路径参数，单独分派（其它命令是"两段式"，如 --gate sync）
            if (args.Length >= 1 && args[0] == "--check-trace")
            {
                return Gates.CheckTrace(args.Length >= 2
                    ? args[1]
                    : System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mcdoc-trace.log"));
            }
            var mode = args.Length >= 2 ? args[0] + " " + args[1] : "";
            try
            {
                return mode switch
                {
                    "--gate sync" => Gates.Sync(),
                    "--gate parse" => Gates.Parse(),
                    "--gate caret" => Gates.Caret(),
                    "--case criterion-empty" => Gates.CriterionEmpty(args.Contains("--with-lsp")),
                    "--case unknown-field" => Gates.UnknownField(),
                    "--case sequential-edit" => Gates.SequentialEdit(),
                    "--case rebuild-semantics" => Gates.RebuildSemantics(),
                    "--case legacy-delete" => Gates.LegacyDelete(),
                    "--case legacy-delete-top" => Gates.LegacyDeleteTop(),
                    "--case no-selfstart" => Gates.NoSelfStart(),

                    "--case projector-idle" => Gates.ProjectorIdle(),
                    "--case diagnostics-refresh" => Gates.DiagnosticsRefresh(),
                    "--case registry-id-namespace" => Gates.RegistryIdNamespace(),
                    "--case union-branch-stale" => Gates.UnionBranchStale(),
                    "--case blockstate-enum-first-build" => Gates.BlockStateEnumFirstBuild(),
                    "--case blockstate-enum-after-value" => Gates.BlockStateEnumAfterValue(),
                    "--case dependents-declaration-shape" => Gates.DependentsDeclarationShape(),
                    "--case text-edit-reevaluates-dependents" => Gates.TextEditReevaluatesDependents(),
                    "--case value-gate-scope" => Gates.ValueGateScope(),
                    "--case union-switch-shape" => Gates.UnionSwitchShape(),
                    "--case required-candidate-not-blank" => Gates.RequiredCandidateNotBlank(),
                    "--case benign-skip-no-badge" => Gates.BenignSkipNoBadge(),
                    "--case structure-change-reevaluates" => Gates.StructureChangeReevaluates(),
                    "--case rebuild-entry-identity" => Gates.RebuildEntryIdentity(),
                    "--case union-display-materializes" => Gates.UnionDisplayMaterializes(),
 "--case unset-keeps-parent-shape" => Gates.UnsetKeepsParentShape(),
                    "--case blockstate-properties" => Gates.BlockstateProperties(),
                    "--case entry-child-switch" => Gates.EntryChildSwitch(),
                    "--case union-unresolved" => Gates.UnionUnresolved(),
                    "--case union-stable" => Gates.UnionStable(),
                    "--case union-identity" => Gates.UnionIdentity(),
                    "--case required-enum" => Gates.RequiredEnum(),
                    "--case memory" => Gates.Memory(),
                    _ => Usage()
                };
            }
            catch (Exception error)
            {
                Console.WriteLine("[红] 探针异常：" + error);
                return 1;
            }
        }

        private static int Usage()
        {
            Console.WriteLine("用法：CBHK.Probe --gate sync | --gate parse | --gate caret | --case criterion-empty [--with-lsp] | --case unknown-field | --check-trace <日志路径>");
            return 2;
        }
    }
}
