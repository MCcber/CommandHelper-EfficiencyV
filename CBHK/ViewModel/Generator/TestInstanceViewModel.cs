using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：test_instance（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class TestInstanceViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "test_instance")
    {
        [ObservableProperty]
        private string title = "测试实例编辑器";
    }
}