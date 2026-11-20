using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：test_environment（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class TestEnvironmentViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "test_environment")
    {
        [ObservableProperty]
        private string title = "测试环境编辑器";
    }
}