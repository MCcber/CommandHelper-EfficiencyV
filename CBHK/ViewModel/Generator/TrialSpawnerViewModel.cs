using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：trial_spawner（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class TrialSpawnerViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "trial_spawner")
    {
        [ObservableProperty]
        private string title = "试炼刷怪笼配置编辑器";
    }
}