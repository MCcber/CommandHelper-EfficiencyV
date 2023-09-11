using cbhk_environment.CustomControls;
using cbhk_environment.GeneralTools;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace cbhk_environment.Generators.ItemGenerator.Components.SpecialNBT
{
    /// <summary>
    /// InlineItems.xaml 的交互逻辑
    /// </summary>
    public partial class InlineItems : UserControl
    {
        public InlineItems()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 引用存档
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReferenceSaveClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Multiselect = false,
                RestoreDirectory = true,
                Title = "请选择一个物品文件"
            };
            if (openFileDialog.ShowDialog().Value)
            {
                if (File.Exists(openFileDialog.FileName))
                {
                    string itemData = ExternalDataImportManager.GetItemDataHandler(openFileDialog.FileName);
                    JToken itemTag = JObject.Parse(itemData)["Item"];
                    if (itemTag != null)
                        itemData = itemTag.ToString();
                    else
                        itemTag = JObject.Parse(itemData);
                    JToken itemID = itemTag["id"];
                    DisplayItem.Tag = itemData;
                    if (itemID != null)
                        (DisplayItem.Child as Image).Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "resources\\data_sources\\item_and_block_images\\" + itemID + ".png", UriKind.RelativeOrAbsolute));
                }
            }
        }

        /// <summary>
        /// 从剪切板导入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportFromClipboardClick(object sender, RoutedEventArgs e)
        {
            string itemData = ExternalDataImportManager.GetItemDataHandler(Clipboard.GetText(), false);
            itemData = itemData[itemData.IndexOf('{')..itemData.LastIndexOf('}')];
            //补齐缺失双引号对的key
            itemData = Regex.Replace(itemData, @"([\{\[,])([\s+]?\w+[\s+]?):", "$1\"$2\":");
            //清除数值型数据的单位
            itemData = Regex.Replace(itemData, @"(\d+[\,\]\}]?)([a-zA-Z])", "$1").Replace("I;", "");
            if (itemData.Trim().Length == 0) return;
            JToken itemTag = JObject.Parse(itemData)["Item"];
            if (itemTag != null)
                itemData = itemTag.ToString();
            else
                itemTag = JObject.Parse(itemData);
            JToken itemID = itemTag["id"];
            DisplayItem.Tag = itemData;
            if (itemID != null)
                (DisplayItem.Child as Image).Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "resources\\data_sources\\item_and_block_images\\" + itemID + ".png", UriKind.RelativeOrAbsolute));
        }

        /// <summary>
        /// 更新引用的物品索引
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReferenceIndex_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            item_datacontext itemContext = (Window.GetWindow(slider) as Item).DataContext as item_datacontext;
            RichTabItems richTabItems = this.FindParent<RichTabItems>();
            int currentIndex = itemContext.ItemPageList.IndexOf(richTabItems);
            int index = int.Parse(slider.Value.ToString());
            ItemPageDataContext pageContext = (itemContext.ItemPageList[index].Content as ItemPages).DataContext as ItemPageDataContext;
            if (ReferenceMode.IsChecked.Value)
            {
                pageContext.UseForReference = true;
                if (slider.Value < itemContext.ItemPageList.Count && currentIndex != index)
                {
                    DisplayItem.Tag = pageContext.run_command(false);
                    (DisplayItem.Child as Image).Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "resources\\data_sources\\item_and_block_images\\" + pageContext.SelectedItemId.ComboBoxItemId + "_spawn_egg.png", UriKind.RelativeOrAbsolute));
                }
                else
                {
                    DisplayItem.Tag = "";
                    (DisplayItem.Child as Image).Source = new BitmapImage();
                }
            }
            else
            {
                DisplayItem.Tag = "";
                (DisplayItem.Child as Image).Source = new BitmapImage();
                pageContext.UseForReference = false;
            }
        }

        /// <summary>
        /// 删除当前内联物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseButtons_Click(object sender, RoutedEventArgs e)
        {
            IconTextButtons iconTextButtons = sender as IconTextButtons;
            StackPanel stackPanel = iconTextButtons.FindParent<StackPanel>();
            stackPanel.Children.Remove(this);
            Accordion accordion = (stackPanel.Parent as ScrollViewer).Parent as Accordion;
            accordion.FindChild<IconButtons>().Focus();
        }
    }
}
