using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：dialog（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class DialogViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "dialog")
    {
        [ObservableProperty]
        private string title = "对话框编辑器";
    }
}