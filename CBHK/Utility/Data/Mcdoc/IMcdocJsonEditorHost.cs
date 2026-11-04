using CBHK.Utility.Data.JsonText;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>页面 VM 实现它即可拿到页面的 JSON 编辑器（用于手术式回写）。</summary>
    public interface IMcdocJsonEditorHost
    {
        void AttachJsonEditor(JsonTextEditor editor);

        /// <summary>
        /// 编辑器里的文本被用户改动时回调（AvalonEdit 的 Text 不是依赖属性，不能用 Binding；
        /// 文本 → VM 的方向统一走这条回调，VM 自己做防抖）。
        /// </summary>
        void OnJsonEditorTextChanged(string text);
    }

    /// <summary>
    /// 页面级文档动作（misode 的 SourcePanel / 下载 JSON 那类交互）。
    /// 由行视图通过快捷键调用，避免 9 个页面各写一遍按钮。
    /// </summary>
    public interface IMcdocDocumentActions
    {
        System.Windows.Input.ICommand CopyJsonCommand { get; }
        System.Windows.Input.ICommand SaveJsonCommand { get; }
    }
}
