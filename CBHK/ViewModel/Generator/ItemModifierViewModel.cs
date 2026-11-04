using CBHK.Model.Constant;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：item_modifier（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class ItemModifierViewModel : McdocGeneratorViewModelBase
    {
        public ItemModifierViewModel(Resource resource) : base(resource, "item_modifier")
        {
        }
    }
}
