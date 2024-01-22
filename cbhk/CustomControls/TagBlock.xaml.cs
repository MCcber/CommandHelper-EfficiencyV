using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace cbhk.CustomControls
{
    /// <summary>
    /// TagBlock.xaml 的交互逻辑
    /// </summary>
    public partial class TagBlock : UserControl
    {
        public TagBlock()
        {
            InitializeComponent();
        }

        private void DeleteButtons_Click(object sender, RoutedEventArgs e)
        {
            InlineUIContainer parentContainer = Parent as InlineUIContainer;
            Paragraph paragraph = parentContainer.Parent as Paragraph;
            paragraph.Inlines.Remove(parentContainer);
        }
    }
}
