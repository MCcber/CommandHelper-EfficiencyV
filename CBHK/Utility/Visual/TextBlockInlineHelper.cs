using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using CBHK.Model.Common;

namespace CBHK.Utility.Visual
{
    public static class TextBlockInlineHelper
    {
        public static readonly DependencyProperty BindableInlinesProperty =
            DependencyProperty.RegisterAttached(
                "BindableInlines",
                typeof(IEnumerable<InlineData>),
                typeof(TextBlockInlineHelper),
                new PropertyMetadata(null, OnBindableInlinesChanged));

        public static void SetBindableInlines(DependencyObject element, IEnumerable<InlineData> value)
        {
            element.SetValue(BindableInlinesProperty, value);
        }

        public static IEnumerable<InlineData> GetBindableInlines(DependencyObject element)
        {
            return (IEnumerable<InlineData>)element.GetValue(BindableInlinesProperty);
        }

        private static void OnBindableInlinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock textBlock)
            {
                textBlock.Inlines.Clear();

                if (e.NewValue is IEnumerable<InlineData> inlineDataList)
                {
                    foreach (var data in inlineDataList)
                    {
                        var run = new Run
                        {
                            Text = data.Text,
                            Foreground = data.Foreground,
                            FontStyle = data.FontStyle,
                            FontWeight = data.FontWeight,
                            TextDecorations = data.TextDecorationCollection
                        };
                        if(data.FontFamily is not null)
                        {
                            run.FontFamily = data.FontFamily;
                        }
                        textBlock.Inlines.Add(run);
                    }
                }
            }
        }
    }
}