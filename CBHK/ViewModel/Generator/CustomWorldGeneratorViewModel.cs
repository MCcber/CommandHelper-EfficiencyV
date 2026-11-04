using CBHK.Model.Constant;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：dimension（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class CustomWorldGeneratorViewModel : McdocGeneratorViewModelBase
    {
        public CustomWorldGeneratorViewModel(Resource resource) : base(resource, "dimension")
        {
        }
    }
}
