using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：trim_material（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class TrimMaterialViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "trim_material")
    {
        [ObservableProperty]
        private string title = "纹饰材料编辑器";
    }
}