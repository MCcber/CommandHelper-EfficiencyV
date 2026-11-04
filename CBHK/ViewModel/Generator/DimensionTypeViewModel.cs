using CBHK.Model.Constant;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：dimension_type（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class DimensionTypeViewModel : McdocGeneratorViewModelBase
    {
        public DimensionTypeViewModel(Resource resource) : base(resource, "dimension_type")
        {
        }
    }
}
