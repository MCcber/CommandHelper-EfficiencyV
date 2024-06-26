﻿using cbhk.ControlsDataContexts;
using cbhk.CustomControls;
using cbhk.GeneralTools;
using cbhk.GeneralTools.Displayer;
using cbhk.GeneralTools.MessageTip;
using cbhk.Generators.RecipeGenerator.Components;
using cbhk.WindowDictionaries;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace cbhk.Generators.RecipeGenerator
{
    public partial class RecipeDataContext : ObservableObject
    {
        #region 保存物品ID
        private IconComboBoxItem select_item_id_source;
        public IconComboBoxItem SelectItemIdSource
        {
            get { return select_item_id_source; }
            set
            {
                select_item_id_source = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 配方类型
        public enum RecipeType
        {
            CraftingTable,
            Furnace,
            BlastFurnace,
            Campfire,
            SmithingTable,
            Smoker,
            Stonecutter
        }
        #endregion

        #region 字段
        /// <summary>
        /// 主页引用
        /// </summary>
        public Window home = null;

        //被抓取的物品
        public static Image GrabedImage = new Image();

        //是否选择物品
        public static bool IsGrabingItem = false;

        //本生成器的图标路径
        string icon_path = "pack://application:,,,/cbhk;component/resources/common/images/spawnerIcons/IconRecipes.png";
        //原版物品库视图引用
        public ListView originalItemViewer = null;
        //自定义物品库视图引用
        public ListView customItemViewer = null;
        //拖拽源
        public static Image drag_source = null;

        /// <summary>
        /// 原版物品库
        /// </summary>
        public ObservableCollection<ItemStructure> OriginalItemSource { get; set; } = [];
        /// <summary>
        /// 自定义物品库
        /// </summary>
        public ObservableCollection<ItemStructure> CustomItemSource { get; set; } = [];
        /// <summary>
        /// 原版物品库数据源
        /// </summary>
        public CollectionViewSource OriginalItemViewSource = new();
        /// <summary>
        /// 自定义物品库数据源
        /// </summary>
        public CollectionViewSource CustomItemViewSource = new();
        #endregion

        #region 已选中的成员
        private ItemStructure selectedItem = null;
        public ItemStructure SelectedItem
        {
            get => selectedItem;
            set => SetProperty(ref selectedItem,value);
        }
        #endregion

        #region 已选中的物品库索引
        private int selectedItemListIndex;

        public int SelectedItemListIndex
        {
            get => selectedItemListIndex;
            set => SetProperty(ref selectedItemListIndex,value);
        }

        #endregion

        #region 搜索值
        private string searchText = "";
        public string SearchText
        {
            get => searchText;
            set
            {
                searchText = value;
                if (SelectedItemListIndex == 0)
                    OriginalItemViewSource.View?.Refresh();
                else
                    CustomItemViewSource.View?.Refresh();
            }
        }
        #endregion

        //配方标签页数据源
        public ObservableCollection<RichTabItems> RecipeList { get; set; } = [ new RichTabItems() {Header = "工作台",               
                IsContentSaved = true,
                BorderThickness = new(4, 4, 4, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC6B23")),
                Foreground = new SolidColorBrush(Colors.White),
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                LeftBorderTexture = Application.Current.Resources["TabItemLeft"] as Brush,
                RightBorderTexture = Application.Current.Resources["TabItemRight"] as Brush,
                TopBorderTexture = Application.Current.Resources["TabItemTop"] as Brush,
                SelectedLeftBorderTexture = Application.Current.Resources["SelectedTabItemLeft"] as Brush,
                SelectedRightBorderTexture = Application.Current.Resources["SelectedTabItemRight"] as Brush,
                SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as Brush, } ];

        public DataTable ItemTable = new();

        public RecipeDataContext()
        {
            Task.Run(async () =>{
                #region 初始化数据表
                DataCommunicator dataCommunicator = DataCommunicator.GetDataCommunicator();
                ItemTable = await dataCommunicator.GetData("SELECT * FROM Items");
                #endregion
                #region 初始化数据
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    RecipeList[0].Content = new CraftingTable() { FontWeight = FontWeights.Normal };
                });
                #endregion
                #region 异步载入原版物品序列
                //加载物品集合
                string uriDirectoryPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
                string urlPath = "";
                foreach (DataRow row in ItemTable.Rows)
                {
                    urlPath = uriDirectoryPath + row["id"].ToString() + ".png";
                    if (File.Exists(urlPath))
                        OriginalItemSource.Add(new ItemStructure(new Uri(urlPath, UriKind.Absolute), row["id"].ToString() + ":" + row["name"].ToString(), "{id:\"minecraft:" + row["id"].ToString() + "\",Count:1b}"));
                }
                #endregion
                #region 异步载入自定义物品序列
                //加载物品集合
                uriDirectoryPath = AppDomain.CurrentDomain.BaseDirectory + "resources\\saves\\Item\\";
                string[] itemFileList = Directory.GetFiles(uriDirectoryPath);
                foreach (string item in itemFileList)
                {
                    if (File.Exists(item))
                    {
                        string nbt = ExternalDataImportManager.GetItemDataHandler(item);
                        if (nbt.Length > 0)
                        {
                            JObject data = JObject.Parse(nbt);
                            JToken id = data.SelectToken("id");
                            if (id == null) continue;
                            string itemID = id.ToString().Replace("\"", "").Replace("minecraft:", "");
                            string iconPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + itemID + ".png";
                            string itemName = ItemTable.Select("id='" + itemID + "'").First()["name"].ToString();
                            CustomItemSource.Add(new ItemStructure(new Uri(iconPath, UriKind.Absolute), itemID + ":" + itemName, nbt));
                        }
                    }
                }
                #endregion
            });
        }

        /// <summary>
        /// 订阅原版物品库过滤事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OriginalListView_Loaded(object sender,RoutedEventArgs e)
        {
            Window window = Window.GetWindow(sender as ListView);
            OriginalItemViewSource = window.FindResource("OriginalItemView") as CollectionViewSource;
            OriginalItemViewSource.Filter += CollectionViewSource_Filter;
        }

        /// <summary>
        /// 订阅自定义物品库过滤事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CustomListView_Loaded(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(sender as ListView);
            CustomItemViewSource = window.FindResource("CustomItemView") as CollectionViewSource;
            CustomItemViewSource.Filter += CollectionViewSource_Filter;
        }

        [RelayCommand]
        /// <summary>
        /// 添加配方
        /// </summary>
        /// <param name="obj"></param>
        private void AddRecipe(MenuItem obj)
        {
            MenuItem menu = obj.Parent as MenuItem;
            int index = menu.Items.IndexOf(obj);
            RichTabItems richTabItems = new()
            {
                Header = obj.Header,
                IsContentSaved = true,
                BorderThickness = new(4, 4, 4, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC6B23")),
                Foreground = new SolidColorBrush(Colors.White),
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                LeftBorderTexture = Application.Current.Resources["TabItemLeft"] as Brush,
                RightBorderTexture = Application.Current.Resources["TabItemRight"] as Brush,
                TopBorderTexture = Application.Current.Resources["TabItemTop"] as Brush,
                SelectedLeftBorderTexture = Application.Current.Resources["SelectedTabItemLeft"] as Brush,
                SelectedRightBorderTexture = Application.Current.Resources["SelectedTabItemRight"] as Brush,
                SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as Brush,
            };
            RecipeList.Add(richTabItems);
            richTabItems.FindParent<TabControl>().SelectedItem = richTabItems;

            switch (index)
            {
                default:
                    richTabItems.Content = new CraftingTable() { FontWeight = FontWeights.Normal };
                    break;
                case 1:
                    richTabItems.Content = new Furnace() { FontWeight = FontWeights.Normal };
                    break;
                case 2:
                    richTabItems.Content = new Smoker() { FontWeight = FontWeights.Normal };
                    break;
                case 3:
                    richTabItems.Content = new BlastFurnace() { FontWeight = FontWeights.Normal };
                    break;
                case 4:
                    richTabItems.Content = new Campfire() { FontWeight = FontWeights.Normal };
                    break;
                case 5:
                    richTabItems.Content = new SmithingTable() { FontWeight = FontWeights.Normal };
                    break;
                case 6:
                    richTabItems.Content = new Stonecutter() { FontWeight = FontWeights.Normal };
                    break;
            }
        }

        /// <summary>
        /// 外部添加配方
        /// </summary>
        public object AddExternRecipe(RecipeType recipeType)
        {
            object result = "";
            RichTabItems richTabItems = new()
            {
                IsContentSaved = true,
                BorderThickness = new(4, 4, 4, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC6B23")),
                Foreground = new SolidColorBrush(Colors.White),
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                LeftBorderTexture = Application.Current.Resources["TabItemLeft"] as Brush,
                RightBorderTexture = Application.Current.Resources["TabItemRight"] as Brush,
                TopBorderTexture = Application.Current.Resources["TabItemTop"] as Brush,
                SelectedLeftBorderTexture = Application.Current.Resources["SelectedTabItemLeft"] as Brush,
                SelectedRightBorderTexture = Application.Current.Resources["SelectedTabItemRight"] as Brush,
                SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as Brush,
            };
            RecipeList.Add(richTabItems);
            richTabItems.FindParent<TabControl>().SelectedItem = richTabItems;
            switch (recipeType)
            {
                case RecipeType.CraftingTable:
                    CraftingTable craftingTable = new() { FontWeight = FontWeights.Normal };
                    result = craftingTable;
                    richTabItems.Header = "工作台";
                    richTabItems.Content = craftingTable;
                    break;
                case RecipeType.Furnace:
                    Furnace furnace = new() { FontWeight = FontWeights.Normal };
                    result = furnace;
                    richTabItems.Header = "熔炉";
                    richTabItems.Content = furnace;
                    break;
                case RecipeType.BlastFurnace:
                    BlastFurnace blastFurnace = new() { FontWeight = FontWeights.Normal };
                    result = blastFurnace;
                    richTabItems.Header = "高炉";
                    richTabItems.Content = blastFurnace;
                    break;
                case RecipeType.Campfire:
                    Campfire campfire = new() { FontWeight = FontWeights.Normal};
                    result = campfire;
                    richTabItems.Header = "篝火";
                    richTabItems.Content = campfire;
                    break;
                case RecipeType.SmithingTable:
                    SmithingTable smithingTable = new() { FontWeight = FontWeights.Normal };
                    result = smithingTable;
                    richTabItems.Header = "锻造台";
                    richTabItems.Content = smithingTable;
                    break;
                case RecipeType.Smoker:
                    Smoker smoker = new() { FontWeight = FontWeights.Normal };
                    result = smoker;
                    richTabItems.Header = "烟熏炉";
                    richTabItems.Content = smoker;
                    break;
                case RecipeType.Stonecutter:
                    Stonecutter stonecutter = new() { FontWeight = FontWeights.Normal };
                    result = stonecutter;
                    richTabItems.Header = "切石机";
                    richTabItems.Content = stonecutter;
                    break;
            }
            return result;
        }

        [RelayCommand]
        /// <summary>
        /// 清空配方
        /// </summary>
        /// <param name="obj"></param>
        private void ClearRecipes(MenuItem obj)
        {
            MenuItem menu = obj.Parent as MenuItem;
            int index = menu.Items.IndexOf(obj);
            for (int i = 0; i < RecipeList.Count; i++)
            {
                switch (index)
                {
                    case 1:
                        if (RecipeList[i].Content is CraftingTable)
                        {
                            RecipeList.RemoveAt(i);
                            i--;
                        }
                        break;
                    case 2:
                        if (RecipeList[i].Content is Furnace)
                        {
                            RecipeList.RemoveAt(i);
                            i--;
                        }
                        break;
                    case 3:
                        if (RecipeList[i].Content is Smoker)
                        {
                            RecipeList.RemoveAt(i);
                            i--;
                        }
                        break;
                    case 4:
                        if (RecipeList[i].Content is BlastFurnace)
                        {
                            RecipeList.RemoveAt(i);
                            i--;
                        }
                        break;
                    case 5:
                        if (RecipeList[i].Content is Campfire)
                        {
                            RecipeList.RemoveAt(i);
                            i--;
                        }
                        break;
                    case 6:
                        if (RecipeList[i].Content is SmithingTable)
                        {
                            RecipeList.RemoveAt(i);
                            i--;
                        }
                        break;
                    case 7:
                        if (RecipeList[i].Content is Stonecutter)
                        {
                            RecipeList.RemoveAt(i);
                            i--;
                        }
                        break;
                    default:
                        RecipeList.Clear();
                        break;
                }
            }
        }

        [RelayCommand]
        /// <summary>
        /// 从剪切板导入配方
        /// </summary>
        private void ImportFromClipboard()
        {
            RecipeDataContext context = this;
            ExternalDataImportManager.ImportRecipeDataHandler(Clipboard.GetText(), ref context,false);
        }

        [RelayCommand]
        /// <summary>
        /// 从文件导入配方
        /// </summary>
        private void ImportFromFile()
        {
            OpenFileDialog openFileDialog = new()
            {
                AddExtension = true,
                DefaultExt = ".json",
                Filter = "Json文件|*.json;",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Multiselect = false,
                Title = "请选择配方文件"
            };
            if (openFileDialog.ShowDialog().Value)
            {
                RecipeDataContext context = this;
                ExternalDataImportManager.ImportRecipeDataHandler(openFileDialog.FileName,ref context);
            }
        }

        /// <summary>
        /// 载入添加配方按钮的图标
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void AddRecipeMenuIconLoaded(object sender,RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            string iconPath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Recipe\\images\\" + menuItem.Uid + "_icon.png";
            if(!File.Exists(iconPath))
                iconPath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Recipe\\images\\" + menuItem.Uid + ".png";
            Image image = new()
            {
                Source = new BitmapImage(new Uri(iconPath))
            };
            menuItem.Icon = image;
        }

        [RelayCommand]
        private void Return(CommonWindow win)
        {
            home.WindowState = WindowState.Normal;
            home.Show();
            home.ShowInTaskbar = true;
            home.Focus();
            win.Close();
        }

        [RelayCommand]
        /// <summary>
        /// 执行生成
        /// </summary>
        private void Run()
        {
            OpenFolderDialog openFolderDialog = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Title = "请选择配方文件生成路径",
                ShowHiddenItems = true,
            };
            if (openFolderDialog.ShowDialog().Value)
            {
                string selectedPath = openFolderDialog.FolderName;
                if (!selectedPath.EndsWith("\\"))
                    selectedPath += "\\";
                foreach (var item in RecipeList)
                {
                    if(item.Content is CraftingTable craftingTable)
                    {
                        craftingTableDataContext context = craftingTable.DataContext as craftingTableDataContext;
                        context.NeedSave = false;
                        context.Run();
                        context.NeedSave = false;
                        _ = File.WriteAllTextAsync(selectedPath + context.FileName + ".json", context.Result);
                    }
                    if (item.Content is Furnace furnace)
                    {
                        furnaceDataContext context = furnace.DataContext as furnaceDataContext;
                        context.NeedSave = false;
                        context.Run();
                        context.NeedSave = false;
                        _ = File.WriteAllTextAsync(selectedPath + context.FileName + ".json", context.Result);
                    }
                    if (item.Content is BlastFurnace blastFurnace)
                    {
                        blastFurnaceDataContext context = blastFurnace.DataContext as blastFurnaceDataContext;
                        context.NeedSave = false;
                        context.Run();
                        context.NeedSave = false;
                        _ = File.WriteAllTextAsync(selectedPath + context.FileName + ".json", context.Result);
                    }
                    if (item.Content is Campfire campfire)
                    {
                        campfireDataContext context = campfire.DataContext as campfireDataContext;
                        context.NeedSave = false;
                        context.Run();
                        context.NeedSave = false;
                        _ = File.WriteAllTextAsync(selectedPath + context.FileName + ".json", context.Result);
                    }
                    if (item.Content is SmithingTable smithingTable)
                    {
                        smithingTableDataContext context = smithingTable.DataContext as smithingTableDataContext;
                        context.NeedSave = false;
                        context.Run();
                        context.NeedSave = false;
                        _ = File.WriteAllTextAsync(selectedPath + context.FileName + ".json", context.Result);
                    }
                    if (item.Content is Stonecutter stonecutter)
                    {
                        stonecutterDataContext context = stonecutter.DataContext as stonecutterDataContext;
                        context.NeedSave = false;
                        context.Run();
                        context.NeedSave = false;
                        _ = File.WriteAllTextAsync(selectedPath + context.FileName + ".json", context.Result);
                    }
                }
                //OpenFolderThenSelectFiles.ExplorerFile(selectedPath);
                Message.PushMessage("配方全部生成成功！", MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 物品过滤事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (SearchText.Trim().Length > 0)
            {
                e.Accepted = false;
                ItemStructure itemStructure = e.Item as ItemStructure;
                string currentItemID = Path.GetFileNameWithoutExtension(itemStructure.ImagePath.ToString());
                string IDAndName = itemStructure.IDAndName;

                if ((currentItemID.Contains(SearchText) && IDAndName.Contains(SearchText)) || (IDAndName.Contains(SearchText) && IDAndName.StartsWith(currentItemID)))
                    e.Accepted = true;
            }
            else
                e.Accepted = true;
        }

        /// <summary>
        /// 载入物品库
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Items_Loaded(object sender, RoutedEventArgs e)
        {
            TabControl tabControl = sender as TabControl;

            #region 原版物品库
            originalItemViewer = (tabControl.Items[0] as TextTabItems).Content as ListView;
            originalItemViewer.DataContext = this;
            originalItemViewer.MouseMove += Bag_MouseMove;
            originalItemViewer.PreviewMouseLeftButtonDown += SelectItemClickDown;
            originalItemViewer.MouseLeave += ListBox_MouseLeave;
            #endregion

            #region 自定义物品库
            customItemViewer = (tabControl.Items[1] as TextTabItems).Content as ListView;
            customItemViewer.DataContext = this;
            customItemViewer.MouseMove += Bag_MouseMove;
            customItemViewer.PreviewMouseLeftButtonDown += SelectItemClickDown;
            customItemViewer.MouseLeave += ListBox_MouseLeave;
            #endregion
        }

        /// <summary>
        /// 处理开始拖拽物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SelectItemClickDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not Image)
                SelectedItem = null;
            if (SelectedItem != null)
            {
                IsGrabingItem = true;
                DependencyObject obj = (DependencyObject)e.OriginalSource;
                while (obj != null && obj is not ListViewItem)
                {
                    obj = VisualTreeHelper.GetParent(obj);
                }
                ListViewItem item = obj as ListViewItem;
                ItemStructure itemStructure = item.Content as ItemStructure;

                drag_source = new Image
                {
                    Source = new BitmapImage(SelectedItem.ImagePath),
                    Tag = itemStructure
                };
                GrabedImage = drag_source;

                if (IsGrabingItem && drag_source != null)
                {
                    DataObject dataObject = new(typeof(Image), GrabedImage);
                    if (dataObject != null)
                        DragDrop.DoDragDrop(drag_source, dataObject, DragDropEffects.Move);
                    IsGrabingItem = false;
                }
            }
        }

        /// <summary>
        /// 松开后停止拖拽
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void RecipeZoneMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            IsGrabingItem = false;
        }

        /// <summary>
        /// 处理拖拽物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectItemMove(object sender, MouseEventArgs e)
        {
            if (IsGrabingItem && drag_source != null && GrabedImage != null)
            {
                DataObject dataObject = new(typeof(Image), GrabedImage);
                if(dataObject != null)
                DragDrop.DoDragDrop(drag_source, dataObject, DragDropEffects.Move);
            }
        }

        public void ListBox_MouseLeave(object sender, MouseEventArgs e)
        {
            SelectedItem = null;
        }

        /// <summary>
        /// 鼠标移入背包后选中对应的成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Bag_MouseMove(object sender, MouseEventArgs e)
        {
            ListView listView = sender as ListView;
            HitTestResult hitTestResult = VisualTreeHelper.HitTest(listView, Mouse.GetPosition(listView));
            if(hitTestResult != null)
            {
                var item = hitTestResult.VisualHit;
                while (item != null && item is not ListViewItem)
                    item = VisualTreeHelper.GetParent(item);

                if (item != null)
                {
                    int i = listView.Items.IndexOf(((ListViewItem)item).DataContext);
                    if (i >= 0 && i < listView.Items.Count)
                        listView.SelectedIndex = i;
                }
            }
        }
    }
}