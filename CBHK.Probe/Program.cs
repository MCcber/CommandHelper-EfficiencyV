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
                    "--case tag-required-candidates" => Gates.TagRequiredCandidates(),
                    "--case real-dimension-default-branch" => Gates.RealDimensionDefaultBranch(),
                    "--case default-tag-value" => Gates.DefaultTagValue(),
                    "--case switch-clears-old-members" => Gates.SwitchClearsOldMembers(),
                    "--case list-item-remove-self" => Gates.ListItemRemoveSelf(),
                    "--case real-dimension-dependent-clear" => Gates.RealDimensionDependentClear(),
                    "--case attributes-entry-union" => Gates.AttributesEntryUnion(),
                    "--case attributes-modifier-branch" => Gates.AttributesModifierBranch(),
                    "--case solve-algebra" => Gates.SolveAlgebra(),
                    "--case optional-struct-add" => Gates.OptionalStructAdd(),
                    "--case tuple-item-actions" => Gates.TupleItemActions(),
                    "--case leftover-key-deletable" => Gates.LeftoverKeyDeletable(),
                    "--case music-volume-entry" => Gates.MusicVolumeEntry(),
                    "--case required-shell-expand" => Gates.RequiredShellExpand(),
                    "--case loot-table-array-root" => Gates.LootTableArrayRoot(),
                    "--case entry-document-shape" => Gates.EntryDocumentShape(),
                    "--case list-item-commands" => Gates.ListItemCommands(),
                    "--case root-union-branch" => Gates.RootUnionBranch(),
                    "--case editor-channel-writes" => Gates.EditorChannelWrites(),
                    "--case loot-table-functions-move" => Gates.LootTableFunctionsMove(),
                    "--case block-properties-resolve" => Gates.BlockPropertiesResolve(),
 "--case unset-keeps-parent-shape" => Gates.UnsetKeepsParentShape(),
                    "--case blockstate-properties" => Gates.BlockstateProperties(),
                    "--case entry-child-switch" => Gates.EntryChildSwitch(),
                    "--case union-unresolved" => Gates.UnionUnresolved(),
                    "--case union-stable" => Gates.UnionStable(),
                    "--case union-identity" => Gates.UnionIdentity(),
                    "--case required-enum" => Gates.RequiredEnum(),
                    "--case memory" => Gates.Memory(),
                    "--case condition-dispatch-load" => Gates.ConditionDispatchOnLoad(),
                    "--case condition-switch-keeps-value" => Gates.ConditionSwitchKeepsValue(),
                    "--case generic-dispatcher-spread" => Gates.GenericDispatcherSpread(),
                    "--case real-dimension-light-level" => Gates.RealDimensionLightLevel(),
                    "--case real-dimension-default" => Gates.RealDimensionDefault(),
                    "--case vm-union-switch" => Gates.VmUnionSwitch(),
                    "--case generic-arg-dynamic-index" => Gates.GenericArgDynamicIndex(),
                    "--case discriminator-empty-string" => Gates.DiscriminatorEmptyString(),
                    "--case list-item-row-state" => Gates.ListItemRowState(),
                    "--case loot-entry-load" => Gates.LootEntryLoad(),
                    "--case vm-loot-add-entry" => Gates.VmLootAddEntry(),
                    "--case vm-loot-entry-actions" => Gates.VmLootEntryActions(),
                    "--case vm-loot-entry-enum-switch" => Gates.VmLootEntryEnumSwitch(),
                    "--case union-list-branch-actions" => Gates.UnionListBranchActions(),
                    "--case vm-item-modifier-list-move" => Gates.VmItemModifierListMove(),
                    "--case vm-item-modifier-list-remove" => Gates.VmItemModifierListRemove(),
                    "--case vm-item-modifier-object-enum-switch" => Gates.VmItemModifierObjectEnumSwitch(),
                    "--case vm-item-modifier-single-entry-remove" => Gates.VmItemModifierSingleEntryRemove(),
                    "--case vm-item-modifier-list-to-object" => Gates.VmItemModifierListToObject(),
                    "--case loot-blockstate-properties" => Gates.LootBlockstateProperties(),
                    "--case text-invisible-whitespace" => Gates.TextInvisibleWhitespace(),
                    "--case map-entry-has-no-list-item-commands" => Gates.MapEntryHasNoListItemCommands(),
                    "--case perf-stages" => Gates.PerfStages(),
                    "--case number-step-quiet" => Gates.NumberStepQuiet(),
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
