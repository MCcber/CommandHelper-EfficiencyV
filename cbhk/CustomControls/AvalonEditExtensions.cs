using ICSharpCode.AvalonEdit;
using System.Windows;

namespace cbhk.CustomControls
{
    public static class AvalonEditExtensions
    {
        public static readonly DependencyProperty CodeText =
            DependencyProperty.RegisterAttached("CodeText", typeof(string), typeof(AvalonEditExtensions),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBindableTextChanged));

        public static string GetCodeText(DependencyObject obj)
        {
            return (string)obj.GetValue(CodeText);
        }

        public static void SetCodeText(DependencyObject obj, string value)
        {
            obj.SetValue(CodeText, value);
        }

        private static void OnBindableTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextEditor textEditor)
            {
                textEditor.Text = e.NewValue as string;
            }
        }
    }
}
