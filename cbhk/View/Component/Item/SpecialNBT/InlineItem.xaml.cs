using CBHK.CustomControl;
using CBHK.GeneralTool;
using CBHK.View.Generator;
using CBHK.ViewModel.Component.Item;
using CBHK.ViewModel.Generator;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace CBHK.View.Component.Item.SpecialNBT
{
    /// <summary>
    /// InlineItem.xaml 的交互逻辑
    /// </summary>
    public partial class InlineItem : UserControl
    {
        public InlineItem()
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
                    JToken itemTag = JObject.Parse(itemData)["ItemView"];
                    if (itemTag != null)
                        itemData = itemTag.ToString();
                    else
                        itemTag = JObject.Parse(itemData);
                    JToken itemID = itemTag["id"];
                    DisplayItem.Tag = itemData;
                    if (itemID != null)
                        (DisplayItem.Child as Image).Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\" + itemID + ".png", UriKind.RelativeOrAbsolute));
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
            JToken itemTag = JObject.Parse(itemData)["ItemView"];
            if (itemTag != null)
                itemData = itemTag.ToString();
            else
                itemTag = JObject.Parse(itemData);
            JToken itemID = itemTag["id"];
            DisplayItem.Tag = itemData;
            if (itemID != null)
                (DisplayItem.Child as Image).Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\" + itemID + ".png", UriKind.RelativeOrAbsolute));
        }

        /// <summary>
        /// 更新引用的物品索引
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ReferenceIndex_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            ItemViewModel itemContext = (Window.GetWindow(slider) as ItemView).DataContext as ItemViewModel;
            RichTabItems richTabItems = this.FindParent<RichTabItems>();
            int currentIndex = itemContext.ItemPageList.IndexOf(richTabItems);
            int index = int.Parse(slider.Value.ToString());
            ItemPageViewModel pageContext = (itemContext.ItemPageList[index].Content as ItemPageView).DataContext as ItemPageViewModel;
            if (ReferenceMode.IsChecked.Value)
            {
                pageContext.UseForReference = true;
                string imagePath = "";
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\" + pageContext.SelectedItem.ComboBoxItemId + ".png"))
                    imagePath = AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\" + pageContext.SelectedItem.ComboBoxItemId + ".png";
                else
                    if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\" + pageContext.SelectedItem.ComboBoxItemId + "_spawn_egg.png"))
                    imagePath = AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\" + pageContext.SelectedItem.ComboBoxItemId + "_spawn_egg.png";

                if (slider.Value < itemContext.ItemPageList.Count && currentIndex != index)
                {
                    DisplayItem.Tag = await pageContext.Run(false);
                    (DisplayItem.Child as Image).Source = imagePath.Length > 0 ? new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute)) : null;
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