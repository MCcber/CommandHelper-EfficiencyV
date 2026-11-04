using CBHK.Model.Constant;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：predicate（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class PredicateViewModel : McdocGeneratorViewModelBase
    {
        public PredicateViewModel(Resource resource) : base(resource, "predicate")
        {
        }
    }
}
