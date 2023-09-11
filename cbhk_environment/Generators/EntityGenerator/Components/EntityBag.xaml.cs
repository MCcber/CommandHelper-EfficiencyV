using cbhk_environment.CustomControls;
using cbhk_environment.GeneralTools;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace cbhk_environment.Generators.EntityGenerator.Components
{
    /// <summary>
    /// EntityBag.xaml 的交互逻辑
    /// </summary>
    public partial class EntityBag : UserControl
    {
        public EntityBag()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 从剪切板导入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportFromClipboard_Click(object sender, RoutedEventArgs e)
        {
            string itemData = ExternalDataImportManager.GetItemDataHandler(Clipboard.GetText(),false);
            JObject nbtData = JObject.Parse(itemData);
            JToken itemID = nbtData.SelectToken("Item.id");
            if (itemID == null)
                itemID = nbtData.SelectToken("id");
            ItemIcon.Tag = nbtData;
            string uri = AppDomain.CurrentDomain.BaseDirectory + "resources\\data_sources\\item_and_block_images\\" + itemID.ToString() + ".png";
            if(File.Exists(uri))
            (ItemIcon.Child as Image).Source = new BitmapImage(new Uri(uri, UriKind.Absolute));
        }

        /// <summary>
        /// 从文件导入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportFromFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                AddExtension = false,
                CheckFileExists = true,
                CheckPathExists = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Multiselect = false,
                RestoreDirectory = true,
                Title = "请选择一个物品文件"
            };
            if(openFileDialog.ShowDialog().Value)
            {
                string itemData = ExternalDataImportManager.GetItemDataHandler(openFileDialog.FileName);
                JObject nbtData = JObject.Parse(itemData);
                JToken itemID = nbtData.SelectToken("Item.id");
                itemID ??= nbtData.SelectToken("id");
                ItemIcon.Tag = nbtData;
                string uri = AppDomain.CurrentDomain.BaseDirectory + "resources\\data_sources\\item_and_block_images\\" + itemID.ToString() + ".png";
                if(File.Exists(uri))
                (ItemIcon.Child as Image).Source = new BitmapImage(new Uri(uri, UriKind.Absolute));
            }
        }

        /// <summary>
        /// 删除本实例
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IconTextButtons_Click(object sender, RoutedEventArgs e)
        {
            StackPanel stackPanel = Parent as StackPanel;
            stackPanel.Children.Remove(this);
            ScrollViewer scrollViewer = stackPanel.Parent as ScrollViewer;
            Accordion accordion = scrollViewer.Parent as Accordion;
            accordion.FindChild<IconButtons>().Focus();
        }
    }
}
