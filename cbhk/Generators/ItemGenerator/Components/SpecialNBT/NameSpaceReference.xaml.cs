using cbhk.CustomControls;
using cbhk.GeneralTools;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace cbhk.Generators.ItemGenerator.Components.SpecialNBT
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
            OpenFolderDialog openFolderDialog = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Title = "请选择一个配方的命名空间"
            };
            if(openFolderDialog.ShowDialog().Value)
            {
                if(Directory.Exists(openFolderDialog.FolderName))
                    ReferenceBox.Text = Path.GetDirectoryName(openFolderDialog.FolderName);
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