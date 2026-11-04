using CBHK.Model.Constant;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：loot_table（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class LootTableViewModel : McdocGeneratorViewModelBase
    {
        public LootTableViewModel(Resource resource) : base(resource, "loot_table")
        {
        }
    }
}
