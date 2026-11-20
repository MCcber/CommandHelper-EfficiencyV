using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：decorated_pot_pattern（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class DecoratedPotPatternViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "decorated_pot_pattern")
    {
        [ObservableProperty]
        private string title = "饰纹陶罐图案编辑器";
    }
}