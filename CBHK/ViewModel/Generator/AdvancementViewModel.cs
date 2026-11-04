using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：advancement（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class AdvancementViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "advancement")
    {
        [ObservableProperty]
        private string viewName = "进度编辑器";
    }
}