using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：timeline（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class TimelineViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "timeline")
    {
        [ObservableProperty]
        private string title = "时间线编辑器";
    }
}