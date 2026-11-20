using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：enchantment（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class EnchantmentViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "enchantment")
    {
        [ObservableProperty]
        private string title = "魔咒编辑器";
    }
}