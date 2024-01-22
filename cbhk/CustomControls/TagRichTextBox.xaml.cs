using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;

namespace cbhk.CustomControls
{
    /// <summary>
    /// TagRichTextBox.xaml 的交互逻辑
    /// </summary>
    public partial class TagRichTextBox : UserControl
    {
        public string Result
        {
            get { return (string)GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        public static readonly DependencyProperty ResultProperty =
            DependencyProperty.Register("Result", typeof(string), typeof(TagRichTextBox), new PropertyMetadata(default(string)));

        public TagRichTextBox()
        {
            InitializeComponent();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if(e.Key == Key.Enter && textBox.Text.Trim().Length > 0)
            {
                TagBlock tagBlock = new();
                tagBlock.TextBlock.Text = textBox.Text;
                Paragraph paragraph = tagBox.Document.Blocks.FirstBlock as Paragraph;
                paragraph.Inlines.Add(tagBlock);
                textBox.Text = "";
            }
        }

        public async Task GetResult()
        {
            StringBuilder result = new();
            Paragraph paragraph = tagBox.Document.Blocks.FirstBlock as Paragraph;
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
                            result.Append("\"" + (tagBlocks[i].Child as TagBlock).TextBlock.Text + "\",");
                        });
                        await Task.Delay(0, cts);
                    });
                    result = result.Remove(result.Length - 1, 1).Append("],");
                });
            }
            Result = result.ToString();
        }
    }

    public class TagTextBlockWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double width = (double)value;
            return width - 25;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}