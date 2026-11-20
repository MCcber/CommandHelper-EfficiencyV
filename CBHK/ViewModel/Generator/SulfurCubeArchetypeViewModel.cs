using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：sulfur_cube_archetype（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class SulfurCubeArchetypeViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "sulfur_cube_archetype")
    {
        [ObservableProperty]
        private string title = "硫方怪原型编辑器";
    }
}