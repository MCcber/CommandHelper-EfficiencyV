using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：slot_source（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class SlotSourceViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "slot_source")
    {
        [ObservableProperty]
        private string title = "槽位源编辑器";
    }
}