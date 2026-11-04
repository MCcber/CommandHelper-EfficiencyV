using CBHK.Model.Constant;

namespace CBHK.ViewModel.Generator
{
    /// <summary>入口：chat_type（其余逻辑全在 McdocGeneratorViewModelBase）。</summary>
    public sealed partial class ChatTypeViewModel : McdocGeneratorViewModelBase
    {
        public ChatTypeViewModel(Resource resource) : base(resource, "chat_type")
        {
        }
    }
}
