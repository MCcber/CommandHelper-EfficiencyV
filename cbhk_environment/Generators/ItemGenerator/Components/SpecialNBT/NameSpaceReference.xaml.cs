using cbhk_environment.CustomControls;
using cbhk_environment.GeneralTools;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace cbhk_environment.Generators.ItemGenerator.Components.SpecialNBT
{
    /// <summary>
    /// NameSpaceReference.xaml 的交互逻辑
    /// </summary>
    public partial class NameSpaceReference : UserControl
    {
        public NameSpaceReference()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 引用路径
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Reference_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Description = "请选择一个配方的命名空间"
            };
            if(folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if(Directory.Exists(folderBrowserDialog.SelectedPath))
                    ReferenceBox.Text = Path.GetDirectoryName(folderBrowserDialog.SelectedPath);
            }
        }

        private void IconTextButtons_Click(object sender, RoutedEventArgs e)
        {
            StackPanel stackPanel = this.FindParent<StackPanel>();
            stackPanel.Children.Remove(this);
            stackPanel.FindParent<Accordion>().FindChild<IconButtons>().Focus();
        }
    }
}
