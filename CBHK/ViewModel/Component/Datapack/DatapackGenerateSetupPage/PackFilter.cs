using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Component.Datapack.DatapackGenerateSetupPage
{
    /// <summary>
    /// 包过滤器数据结构
    /// </summary>
    public partial class PackFilter:ObservableObject
    {
        [ObservableProperty]
        private string _nameSpace = "";

        [ObservableProperty]
        private string _path = "";
    }
}
