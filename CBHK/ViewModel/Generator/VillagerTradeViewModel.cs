using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：villager_trade（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class VillagerTradeViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "villager_trade")
    {
        [ObservableProperty]
        private string title = "村民交易编辑器";
    }
}