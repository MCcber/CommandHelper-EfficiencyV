using System.Windows;
using CBHK.Utility.Data.JsonText;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 附加属性：XAML 里给 JsonTextEditor 挂 `Host.IsEnabled="True"`，Loaded 时把自己交给 DataContext 的 VM。
    /// </summary>
    public static class McdocJsonEditorHost
    {
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(McdocJsonEditorHost), new PropertyMetadata(false, OnChanged));

        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);
        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not JsonTextEditor editor || e.NewValue is not true) return;
            if (editor.IsLoaded) Attach(editor);
            else editor.Loaded += OnLoaded;
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not JsonTextEditor editor) return;
            editor.Loaded -= OnLoaded;
            Attach(editor);
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<JsonTextEditor, object> Attached = new();

        private static void Attach(JsonTextEditor editor)
        {
            if (editor.DataContext is IMcdocJsonEditorHost host)
            {
                host.AttachJsonEditor(editor);
                if (Attached.TryGetValue(editor, out _)) return;   // 只订阅一次
                Attached.Add(editor, new object());
                // 文本 → VM 的唯一通道（Text 不是 DP，绑不了）
                editor.TextChanged += (_, _) =>
                    (editor.DataContext as IMcdocJsonEditorHost)?.OnJsonEditorTextChanged(editor.Text);
            }
        }
    }
}
