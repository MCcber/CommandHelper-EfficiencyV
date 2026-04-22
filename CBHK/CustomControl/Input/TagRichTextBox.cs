using CBHK.CustomControl.VectorButton;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace CBHK.CustomControl.Input
{
    public partial class TagRichTextBox:Control
    {
        #region Field
        private RichTextBox contentRichTextBox = null;
        #endregion

        #region Property
        public string Result
        {
            get { return (string)GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        public static readonly DependencyProperty ResultProperty =
            DependencyProperty.Register("Result", typeof(string), typeof(TagRichTextBox), new PropertyMetadata(default(string)));
        #endregion

        #region Method
        public async Task GetResult()
        {
            if(contentRichTextBox is null)
            {
                return;
            }
            StringBuilder result = new();
            Paragraph paragraph = contentRichTextBox.Document.Blocks.FirstBlock as Paragraph;
            if (paragraph.Inlines.Count > 0)
            {
                CancellationTokenSource cts = new();
                result.Append("Tags:[");
                List<InlineUIContainer> tagBlocks = paragraph.Inlines.ToList().ConvertAll(item => item as InlineUIContainer);
                await Task.Run(async () =>
                {
                    await Parallel.ForAsync(0, tagBlocks.Count, async (i, cts) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            result.Append("\"" + (tagBlocks[i].Child as TagBlock).Text + "\",");
                        });
                        await Task.Delay(0, cts);
                    });
                    result = result.Remove(result.Length - 1, 1).Append("],");
                });
            }
            Result = result.ToString();
        }
        #endregion

        #region Event
        public void ContentRichTextBox_Loaded(object sender,RoutedEventArgs e)
        {
            if(sender is RichTextBox richTextBox)
            {
                contentRichTextBox = richTextBox;
            }
        }

        public void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (e.Key == Key.Enter && textBox.Text.Trim().Length > 0 && contentRichTextBox is not null)
            {
                TagBlock tagBlock = new()
                {
                    Text = textBox.Text,
                    Style = Application.Current.Resources["TagBlockStyle"] as Style
                };
                Paragraph paragraph = contentRichTextBox.Document.Blocks.FirstBlock as Paragraph;
                paragraph.Inlines.Add(tagBlock);
                textBox.Text = "";
            }
        }
        #endregion
    }
}