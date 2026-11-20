using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：trim_pattern（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class TrimPatternViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "trim_pattern")
    {
        [ObservableProperty]
        private string title = "纹饰图案编辑器";
    }
}