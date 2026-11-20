using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    public partial class BannerPatternViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "banner_pattern")
    {
        [ObservableProperty]
        private string title = "旗帜图案";
    }
}
