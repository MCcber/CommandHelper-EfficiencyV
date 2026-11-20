using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    public partial class BlockTransformerViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "block_transformer")
    {
        [ObservableProperty]
        public string title = "方块转换器";
    }
}