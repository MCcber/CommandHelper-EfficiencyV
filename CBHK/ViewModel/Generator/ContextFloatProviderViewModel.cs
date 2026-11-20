using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：context_float_provider（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class ContextFloatProviderViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "context_float_provider")
    {
        [ObservableProperty]
        private string title = "浮点数数值提供器编辑器";
    }
}