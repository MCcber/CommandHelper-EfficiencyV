using CBHK.Utility.Common;
using CBHK.View.Component.Entity;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace CBHK.CustomControl
{
    /// <summary>
    /// TagBlock.xaml 的交互逻辑
    /// </summary>
    public partial class TagBlock : UserControl
    {
        TagRichTextBox tagRichTextBox = null;

        public TagBlock()
        {
            InitializeComponent();
            Loaded += TagBlock_Loaded;
            Unloaded += TagBlock_Unloaded;
        }

        private void TagBlock_Loaded(object sender, RoutedEventArgs e)
        {
            tagRichTextBox = (sender as TagBlock).FindParent<TagRichTextBox>();
        }

        private async void TagBlock_Unloaded(object sender, RoutedEventArgs e)
        {
            await tagRichTextBox.GetResult();
            if(tagRichTextBox.Tag is NBTDataStructure nbt)
            {
                nbt.Result = tagRichTextBox.Result;
            }
        }

        private void DeleteButtons_Click(object sender, RoutedEventArgs e)
        {
            InlineUIContainer parentContainer = Parent as InlineUIContainer;
            Paragraph paragraph = parentContainer.Parent as Paragraph;
            paragraph.Inlines.Remove(parentContainer);
        }
    }
}