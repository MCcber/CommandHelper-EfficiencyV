using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：context_int_provider（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class ContextIntProviderViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "context_int_provider")
    {
        [ObservableProperty]
        private string title = "整数数值提供器编辑器";
    }
}