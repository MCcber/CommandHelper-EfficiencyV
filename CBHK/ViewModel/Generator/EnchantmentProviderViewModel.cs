using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：enchantment_provider（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class EnchantmentProviderViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "enchantment_provider")
    {
        [ObservableProperty]
        private string title = "魔咒提供器编辑器";
    }
}