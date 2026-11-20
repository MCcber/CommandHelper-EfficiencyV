using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：instrument（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class InstrumentViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "instrument")
    {
        [ObservableProperty]
        private string title = "山羊角乐器编辑器";
    }
}