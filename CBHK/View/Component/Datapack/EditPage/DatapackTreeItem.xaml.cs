using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CBHK.View.Component.Datapack.EditPage
{
    /// <summary>
    /// DatapackTreeItem.xaml 的交互逻辑
    /// </summary>
    public partial class DatapackTreeItem : UserControl
    {
        public DatapackTreeItem()
        {
            InitializeComponent();
            FileNameEditor.KeyDown += FileNameEditor_KeyDown;
        }

        /// <summary>
        /// 文件重命名
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileNameEditor_KeyDown(object sender, KeyEventArgs e)
        {
            TreeViewItem parent = Parent as TreeViewItem;
            int oldNameIndex = parent.Uid.LastIndexOf(HeadText.Text);
            HeadText.Text = FileNameEditor.Text;
            HeadText.Visibility = Visibility.Visible;
            FileNameEditor.Visibility = Visibility.Collapsed;
            parent.Uid = parent.Uid[0..oldNameIndex];
            parent.Uid += FileNameEditor.Text;
        }
    }
}