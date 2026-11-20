using CBHK.Model.Constant;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：jukebox_song（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class JukeboxSongViewModel(Resource resource) : McdocGeneratorViewModelBase(resource, "jukebox_song")
    {
        [ObservableProperty]
        private string title = "唱片机曲目编辑器";
    }
}