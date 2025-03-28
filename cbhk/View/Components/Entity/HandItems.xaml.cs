using CBHK.CustomControls;
using CBHK.GeneralTools;
using CBHK.Generators.ItemGenerator;
using CBHK.Generators.ItemGenerator.Components;
using CBHK.ViewModel.Generators;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace CBHK.Generators.EntityGenerator.Components
{
    /// <summary>
    /// HandItems.xaml 的交互逻辑
    /// </summary>
    public partial class HandItems : UserControl
    {
        #region 合并数据
        public string Result
        {
            get
            {
                string result = "";
                if (EnableButton.IsChecked.Value)
                {
                    string data = (mainhand.Tag != null? mainhand.Tag.ToString() : "") + "," + (offhand.Tag != null? offhand.Tag.ToString() : "");
                    if(data.Trim(',').Length > 0)
                    result = "HandItems:["+data.Trim(',')+"]";
                }
                return result;
            }
        }
        #endregion

        public HandItems()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 从剪切板导入装备
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void referenceFromClipboard_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement iconTextButtons = sender as FrameworkElement;
            int currentRow = Grid.GetRow(iconTextButtons);
            Grid grid = iconTextButtons.Parent as Grid;
            Image currentImage = null;
            foreach (FrameworkElement item in grid.Children)
            {
                if (currentRow == Grid.GetRow(item) && Grid.GetColumn(item) == 1)
                {
                    currentImage = item as Image;
                    break;
                }
            }
            string data = ExternalDataImportManager.GetItemDataHandler(Clipboard.GetText(), false);
            if (data.Trim().Length > 0)
            {
                JObject json = JObject.Parse(data);
                if (json.SelectToken("id") is not JObject itemID)
                    itemID = json.SelectToken("ItemView.id") as JObject;
                currentImage.Tag = data;
                currentImage.Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\" + itemID.ToString() + ".png", UriKind.Absolute));
            }
        }

        /// <summary>
        /// 从文件导入装备
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void referenceFromFile_Click(object sender, RoutedEventArgs e)
        {
            Slider slider = sender as Slider;
            int currentRow = Grid.GetRow(slider);
            Grid grid = slider.Parent as Grid;
            Image currentImage = null;
            foreach (FrameworkElement item in grid.Children)
            {
                if (currentRow == Grid.GetRow(item) && Grid.GetColumn(item) == 1)
                {
                    currentImage = item as Image;
                    break;
                }
            }
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                RestoreDirectory = true,
                Title = "请选择一个物品文件"
            };
            if (openFileDialog.ShowDialog().Value)
            {
                string data = ExternalDataImportManager.GetItemDataHandler(openFileDialog.FileName);
                JObject json = JObject.Parse(data);
                if (json.SelectToken("id") is not JObject itemID)
                    itemID = json.SelectToken("ItemView.id") as JObject;
                currentImage.Tag = data;
                currentImage.Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\" + itemID.ToString() + ".png", UriKind.Absolute));
            }
        }

        /// <summary>
        /// 生成部位的穿戴物
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void generator_Click(object sender, RoutedEventArgs e)
        {
            IconTextButtons iconTextButtons = sender as IconTextButtons;
            EntityView entity = Window.GetWindow(iconTextButtons) as EntityView;
            EntityViewModel entityContext = entity.DataContext as EntityViewModel;
            int currentRow = Grid.GetRow(iconTextButtons);
            ItemView item = new();
            ItemViewModel context = item.DataContext as ItemViewModel;
            if (item.ShowDialog().Value)
            {
                Image currentImage = null;
                Grid grid = iconTextButtons.Parent as Grid;
                foreach (FrameworkElement ele in grid.Children)
                {
                    if (currentRow == Grid.GetRow(ele) && Grid.GetColumn(ele) == 1)
                    {
                        currentImage = ele as Image;
                        break;
                    }
                }
                ItemPagesView itemPages = context.ItemPageList.First().Content as ItemPagesView;
                ItemPageViewModel pageContext = itemPages.DataContext as ItemPageViewModel;
                string data = ExternalDataImportManager.GetItemDataHandler(Clipboard.GetText(), false);
                JObject json = JObject.Parse(data);
                if (json.SelectToken("id") is not JObject itemID)
                    itemID = json.SelectToken("ItemView.id") as JObject;
                currentImage.Tag = pageContext.Result;
                currentImage.Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\" + itemID.ToString() + ".png", UriKind.Absolute));
            }
        }

        /// <summary>
        /// 设置为空
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void setEmpty_Click(object sender, RoutedEventArgs e)
        {
            IconTextButtons iconTextButtons = sender as IconTextButtons;
            int currentRow = Grid.GetRow(iconTextButtons);
            Image currentImage = null;
            Grid grid = iconTextButtons.Parent as Grid;
            foreach (FrameworkElement ele in grid.Children)
            {
                if (currentRow == Grid.GetRow(ele) && Grid.GetColumn(ele) == 1)
                {
                    currentImage = ele as Image;
                    break;
                }
            }
            currentImage.Tag = "{}";
            currentImage.Source = new BitmapImage();
        }
    }
}
