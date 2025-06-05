using CBHK.CustomControl;
using CBHK.GeneralTool;
using CBHK.GeneralTool.MessageTip;
using CBHK.Model.Common;
using CBHK.View;
using CBHK.View.Component.Recipe;
using CBHK.ViewModel.Component.Recipe;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Prism.Ioc;
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

namespace CBHK.ViewModel.Generator
{
    public partial class RecipeViewModel : ObservableObject
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
        private Window home = null;

        //被抓取的物品
        public static Image GrabedImage = new();

        //是否选择物品
        public static bool IsGrabingItem = false;
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
        private IContainerProvider _container;

        public RecipeViewModel(IContainerProvider container,MainView mainView)
        {
            _container = container;
            home = mainView;

            BindingOperations.EnableCollectionSynchronization(OriginalItemSource, new object());
            BindingOperations.EnableCollectionSynchronization(CustomItemSource, new object());
            Task.Run(async () =>{
                #region 初始化数据表
                DataCommunicator dataCommunicator = DataCommunicator.GetDataCommunicator();
                ItemTable = await dataCommunicator.GetData("SELECT * FROM Items");
                #endregion
                #region 初始化数据
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    RecipeList[0].Content = new CraftingTableView() { FontWeight = FontWeights.Normal };
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
                    {
                        BitmapImage bitmapImage = new(new Uri(urlPath, UriKind.Absolute))
                        {
                            CacheOption = BitmapCacheOption.None
                        };
                        OriginalItemSource.Add(new ItemStructure(new ImageSourceConverter().ConvertFromString(urlPath) as ImageSource, row["id"].ToString() + ":" + row["name"].ToString(), "{id:\"minecraft:" + row["id"].ToString() + "\",Count:1b}"));
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    }
                }
                #endregion
                #region 异步载入自定义物品序列
                //加载物品集合
                uriDirectoryPath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Saves\Item";
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
                            if (id is null)
                            {
                                continue;
                            }
                            string itemID = id.ToString().Replace("\"", "").Replace("minecraft:", "");
                            urlPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + itemID + ".png";
                            string itemName = ItemTable.Select("id='" + itemID + "'").First()["name"].ToString();
                            CustomItemSource.Add(new ItemStructure(new ImageSourceConverter().ConvertFromString(urlPath) as ImageSource, itemID + ":" + itemName, nbt));
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
                    richTabItems.Content = new CraftingTableView() { FontWeight = FontWeights.Normal };
                    break;
                case 1:
                    richTabItems.Content = new FurnaceView() { FontWeight = FontWeights.Normal };
                    break;
                case 2:
                    richTabItems.Content = new SmokerView() { FontWeight = FontWeights.Normal };
                    break;
                case 3:
                    richTabItems.Content = new BlastFurnaceView() { FontWeight = FontWeights.Normal };
                    break;
                case 4:
                    richTabItems.Content = new CampfireView() { FontWeight = FontWeights.Normal };
                    break;
                case 5:
                    richTabItems.Content = new SmithingTableView() { FontWeight = FontWeights.Normal };
                    break;
                case 6:
                    richTabItems.Content = new StonecutterView() { FontWeight = FontWeights.Normal };
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
                    CraftingTableView craftingTable = new() { FontWeight = FontWeights.Normal };
                    result = craftingTable;
                    richTabItems.Header = "工作台";
                    richTabItems.Content = craftingTable;
                    break;
                case RecipeType.Furnace:
                    FurnaceView furnace = new() { FontWeight = FontWeights.Normal };
                    result = furnace;
                    richTabItems.Header = "熔炉";
                    richTabItems.Content = furnace;
                    break;
                case RecipeType.BlastFurnace:
                    BlastFurnaceView blastFurnace = new() { FontWeight = FontWeights.Normal };
                    result = blastFurnace;
                    richTabItems.Header = "高炉";
                    richTabItems.Content = blastFurnace;
                    break;
                case RecipeType.Campfire:
                    CampfireView campfire = new() { FontWeight = FontWeights.Normal};
                    result = campfire;
                    richTabItems.Header = "篝火";
                    richTabItems.Content = campfire;
                    break;
                case RecipeType.SmithingTable:
                    SmithingTableView smithingTable = new() { FontWeight = FontWeights.Normal };
                    result = smithingTable;
                    richTabItems.Header = "锻造台";
                    richTabItems.Content = smithingTable;
                    break;
                case RecipeType.Smoker:
                    SmokerView smoker = new() { FontWeight = FontWeights.Normal };
                    result = smoker;
                    richTabItems.Header = "烟熏炉";
                    richTabItems.Content = smoker;
                    break;
                case RecipeType.Stonecutter:
                    StonecutterView stonecutter = new() { FontWeight = FontWeights.Normal };
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
                        if (RecipeList[i].Content is CraftingTableView)
                        {
                            RecipeList.RemoveAt(i);
                            i--;
                        }
                        break;
                    case 2:
                        if (RecipeList[i].Content is FurnaceView)
                        {
                            RecipeList.RemoveAt(i);
                            i--;
                        }
                        break;
                    case 3:
                        if (RecipeList[i].Content is SmokerView)
                        {
                            RecipeList.RemoveAt(i);
                            i--;
                        }
                        break;
                    case 4:
                        if (RecipeList[i].Content is BlastFurnaceView)
                        {
                            RecipeList.RemoveAt(i);
                            i--;
                        }
                        break;
                    case 5:
                        if (RecipeList[i].Content is CampfireView)
                        {
                            RecipeList.RemoveAt(i);
                            i--;
                        }
                        break;
                    case 6:
                        if (RecipeList[i].Content is SmithingTableView)
                        {
                            RecipeList.RemoveAt(i);
                            i--;
                        }
                        break;
                    case 7:
                        if (RecipeList[i].Content is StonecutterView)
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
            RecipeViewModel context = this;
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
                RecipeViewModel context = this;
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
            string iconPath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Recipe\Image\\" + menuItem.Uid + "_icon.png";
            if(!File.Exists(iconPath))
                iconPath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Recipe\Image\\" + menuItem.Uid + ".png";
            Image image = new()
            {
                Source = new BitmapImage(new Uri(iconPath))
            };
            menuItem.Icon = image;
        }

        [RelayCommand]
        private void Return(Window win)
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
                    if(item.Content is CraftingTableView craftingTable)
                    {
                        CraftingTableViewModel context = craftingTable.DataContext as CraftingTableViewModel;
                        context.NeedSave = false;
                        context.Run();
                        context.NeedSave = false;
                        _ = File.WriteAllTextAsync(selectedPath + context.FileName + ".json", context.Result);
                    }
                    if (item.Content is FurnaceView furnace)
                    {
                        FurnaceViewModel context = furnace.DataContext as FurnaceViewModel;
                        context.NeedSave = false;
                        context.Run();
                        context.NeedSave = false;
                        _ = File.WriteAllTextAsync(selectedPath + context.FileName + ".json", context.Result);
                    }
                    if (item.Content is BlastFurnaceView blastFurnace)
                    {
                        BlastFurnaceViewModel context = blastFurnace.DataContext as BlastFurnaceViewModel;
                        context.NeedSave = false;
                        context.Run();
                        context.NeedSave = false;
                        _ = File.WriteAllTextAsync(selectedPath + context.FileName + ".json", context.Result);
                    }
                    if (item.Content is CampfireView campfire)
                    {
                        CampfireViewModel context = campfire.DataContext as CampfireViewModel;
                        context.NeedSave = false;
                        context.Run();
                        context.NeedSave = false;
                        _ = File.WriteAllTextAsync(selectedPath + context.FileName + ".json", context.Result);
                    }
                    if (item.Content is SmithingTableView smithingTable)
                    {
                        SmithingTableViewModel context = smithingTable.DataContext as SmithingTableViewModel;
                        context.NeedSave = false;
                        context.Run();
                        context.NeedSave = false;
                        _ = File.WriteAllTextAsync(selectedPath + context.FileName + ".json", context.Result);
                    }
                    if (item.Content is StonecutterView stonecutter)
                    {
                        StonecutterViewModel context = stonecutter.DataContext as StonecutterViewModel;
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
            if (SelectedItem is not null)
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
                    Source = SelectedItem.ImagePath,
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