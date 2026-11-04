using CBHK.Model.Constant;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：damage_type（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class DamageTypeViewModel : McdocGeneratorViewModelBase
    {
        public DamageTypeViewModel(Resource resource) : base(resource, "damage_type")
        {
        }
    }
}
