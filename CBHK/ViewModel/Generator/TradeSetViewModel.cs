using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：trade_set（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class TradeSetViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "trade_set")
    {
        [ObservableProperty]
        private string title = "交易集编辑器";
    }
}