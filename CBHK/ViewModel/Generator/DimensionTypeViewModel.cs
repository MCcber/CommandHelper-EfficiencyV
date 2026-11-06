using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：dimension_type（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class DimensionTypeViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "dimension_type")
    {
        [ObservableProperty]
        private string viewName = "维度类型编辑器";
    }
}
