using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：worldgen/block_state_provider（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class BlockStateProviderViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "worldgen/block_state_provider")
    {
        [ObservableProperty]
        private string title = "方块状态提供商编辑器";
    }
}