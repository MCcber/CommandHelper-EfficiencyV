﻿using cbhk.CustomControls;
using cbhk.GeneralTools;
using cbhk.Generators.ItemGenerator;
using cbhk.Generators.ItemGenerator.Components;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace cbhk.Generators.EntityGenerator.Components
{
    /// <summary>
    /// ArmorItems.xaml 的交互逻辑
    /// </summary>
    public partial class ArmorItems : UserControl
    {
        #region 合并数据
        public string Result
        {
            get
            {
                string result = "";
                if (EnableButton.IsChecked.Value)
                {
                    string data = (boots.Tag != null? boots.Tag.ToString() : "") + "," + (legs.Tag != null? legs.Tag.ToString() : "") + "," + (chest.Tag != null? chest.Tag.ToString() : "") + "," + (helmet.Tag != null? helmet.Tag.ToString() : "");
                    if(data.Trim(',').Length > 0)
                    result = "ArmorItems:[" + data.Trim(',') + "]";
                }
                return result;
            }
        }
        #endregion

        public ArmorItems()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 从剪切板导入盔甲
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void referenceFromClipboard_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int currentRow = Grid.GetRow(button);
            Grid grid = button.Parent as Grid;
            Image currentImage = null;
            foreach (FrameworkElement item in grid.Children)
            {
                if(currentRow == Grid.GetRow(item) && Grid.GetColumn(item) == 1)
                {
                    currentImage = item as Image;
                    break;
                }
            }
            string data = ExternalDataImportManager.GetItemDataHandler(Clipboard.GetText(), false);
            JObject json = JObject.Parse(data);
            if (json.SelectToken("id") is not JObject itemID)
                itemID = json.SelectToken("Item.id") as JObject;
            currentImage.Tag = data;
            currentImage.Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\"+itemID.ToString() + ".png", UriKind.Absolute));
        }

        /// <summary>
        /// 从文件导入盔甲
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void referenceFromFile_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int currentRow = Grid.GetRow(button);
            Grid grid = button.Parent as Grid;
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
            if(openFileDialog.ShowDialog().Value)
            {
                string data = ExternalDataImportManager.GetItemDataHandler(openFileDialog.FileName);
                JObject json = JObject.Parse(data);
                if (json.SelectToken("id") is not JObject itemID)
                    itemID = json.SelectToken("Item.id") as JObject;
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
            Button button = sender as Button;
            Entity entity = Window.GetWindow(button) as Entity;
            EntityDataContext entityContext = entity.DataContext as EntityDataContext;
            int currentRow = Grid.GetRow(button);
            Item item = new();
            ItemDataContext context = item.DataContext as ItemDataContext;
            context.IsCloseable = false;
            context.home = entity;
            if(item.ShowDialog().Value && context.ItemPageList.Count > 0)
            {
                ItemPageDataContext itemPageDataContext = (context.ItemPageList[0].Content as ItemPages).DataContext as ItemPageDataContext;
                Image currentImage = null;
                Grid grid = button.Parent as Grid;
                foreach (FrameworkElement ele in grid.Children)
                {
                    if (currentRow == Grid.GetRow(ele) && Grid.GetColumn(ele) == 1)
                    {
                        currentImage = ele as Image;
                        break;
                    }
                }
                string data = ExternalDataImportManager.GetItemDataHandler(Clipboard.GetText(), false);
                JObject json = JObject.Parse(data);
                if (json.SelectToken("id") is not JObject itemID)
                    itemID = json.SelectToken("Item.id") as JObject;
                currentImage.Tag = itemPageDataContext.Result;
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
