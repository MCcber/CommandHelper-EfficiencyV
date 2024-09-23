using cbhk.CustomControls;
using cbhk.GeneralTools;
using cbhk.GeneralTools.MessageTip;
using cbhk.Generators.ItemGenerator.Components;
using cbhk.Generators.ItemGenerator;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using cbhk.ViewModel.Generators;

namespace cbhk.Generators.EntityGenerator.Components
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
            try
            {
                JObject nbtData = JObject.Parse(itemData);
                JToken itemID = nbtData.SelectToken("ItemView.id");
                itemID ??= nbtData.SelectToken("id");
                ItemIcon.Tag = nbtData;
                string uri = AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\" + itemID.ToString() + ".png";
                if (File.Exists(uri))
                    (ItemIcon.Child as Image).Source = new BitmapImage(new Uri(uri, UriKind.Absolute));
            }
            catch(Exception exception)
            {
                Message.PushMessage(exception.Message,MessageBoxImage.Error);
            }
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
                JToken itemID = nbtData.SelectToken("ItemView.id");
                itemID ??= nbtData.SelectToken("id");
                ItemIcon.Tag = nbtData;
                string uri = AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\" + itemID.ToString() + ".png";
                if(File.Exists(uri))
                (ItemIcon.Child as Image).Source = new BitmapImage(new Uri(uri, UriKind.Absolute));
            }
        }

        /// <summary>
        /// 生成部位的穿戴物
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void generator_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            EntityView entity = Window.GetWindow(button) as EntityView;
            EntityViewModel entityContext = entity.DataContext as EntityViewModel;
            int currentRow = Grid.GetRow(button);
            ItemView item = new();
            ItemViewModel context = item.DataContext as ItemViewModel;
            context.IsCloseable = false;
            if (item.ShowDialog().Value && context.ItemPageList.Count > 0)
            {
                ItemPageViewModel itemPageDataContext = (context.ItemPageList[0].Content as ItemPagesView).DataContext as ItemPageViewModel;
                Border currentImage = null;
                DockPanel dockPanel = button.Parent as DockPanel;
                currentImage = dockPanel.Children[0] as Border;
                string data = ExternalDataImportManager.GetItemDataHandler(itemPageDataContext.Result, false);
                JObject json = JObject.Parse(data);
                string itemID = "";
                if (json.SelectToken("id") is JToken itemIDToken)
                    itemID = json.SelectToken("id")?.ToString();
                if (itemID.Length == 0)
                    itemID = json.SelectToken("ItemView.id")?.ToString();
                currentImage.Tag = itemPageDataContext.Result;
                if (itemID.Length > 0)
                {
                    itemID = itemID.Replace("minecraft:", "");
                    ImageBrush imageBrush = new(new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\" + itemID + ".png", UriKind.Absolute)));
                    currentImage.Background = imageBrush;
                }
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