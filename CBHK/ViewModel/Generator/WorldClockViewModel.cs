using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：world_clock（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class WorldClockViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "world_clock")
    {
        [ObservableProperty]
        private string title = "世界时钟编辑器";
    }
}