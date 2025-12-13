using CBHK.CustomControl;
using CBHK.Domain;
using CBHK.Model.Common;
using CBHK.Utility.Common;
using CBHK.Utility.MessageTip;
using CBHK.View;
using CBHK.View.Component.Recipe;
using CBHK.ViewModel.Component.Recipe;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static CBHK.Model.Common.Enums;

namespace CBHK.ViewModel.Generator
{
    public partial class RecipeViewModel : ObservableObject
    {
        #region Field
        /// <summary>
        /// 主页引用
        /// </summary>
        private Window home = null;
        /// <summary>
        /// 被抓取的物品
        /// </summary>
        public static Image GrabedImage = new();
        /// <summary>
        /// 是否选择物品
        /// </summary>
        public static bool IsGrabingItem = false;
        /// <summary>
        /// 原版物品库视图引用
        /// </summary>
        public ListView originalItemViewer = null;
        /// <summary>
        /// 自定义物品库视图引用
        /// </summary>
        public ListView customItemViewer = null;
        /// <summary>
        /// 拖拽源
        /// </summary>
        public static Image drag_source = null;
        /// <summary>
        /// 原版物品库数据源
        /// </summary>
        public CollectionViewSource OriginalItemViewSource = new();
        /// <summary>
        /// 自定义物品库数据源
        /// </summary>
        public CollectionViewSource CustomItemViewSource = new();

        private Dictionary<string, string> ItemIDAndNameMap = [];
        private List<string> ItemKeyList = [];

        private CBHKDataContext context = null;
        private IContainerProvider container = null;
        private DataService _dataService = null;

        private IProgress<ItemStructure> AddOriginalItemProgress = null;
        private IProgress<(int, string, string, string)> SetOriginalItemProgress = null;
        private IProgress<ItemStructure> AddCustomItemProgress = null;
        private IProgress<(int, string, string, string, string)> SetCustomItemProgress = null;

        private string ImageSetFolderPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
        /// <summary>
        /// 加载物品集合
        /// </summary>
        private string ItemSaveFolderPath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Saves\Item";
        #endregion

        #region Property
        /// <summary>
        /// 原版物品库
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<ItemStructure> _originalItemList = [];
        /// <summary>
        /// 自定义物品库
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<ItemStructure> _customItemList = [];
        /// <summary>
        /// 已选中的成员
        /// </summary>
        [ObservableProperty]
        private ItemStructure _selectedItem = null;

        /// <summary>
        /// 已选中的物品库索引
        /// </summary>
        [ObservableProperty]
        private int _selectedItemListIndex;

        /// <summary>
        /// 搜索值
        /// </summary>
        [ObservableProperty]
        private string _searchText = "";

        /// <summary>
        /// 配方标签页数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<RichTabItems> _recipeList = 
            [ 
                new RichTabItems()
                {
                    Header = "工作台",
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
                }
            ];

        #region 已选择的版本
        private TextComboBoxItem selectedVersion;
        public TextComboBoxItem SelectedVersion
        {
            get => selectedVersion;
            set
            {
                SetProperty(ref selectedVersion, value);
                CurrentMinVersion = int.Parse(SelectedVersion.Text.Replace(".", "").Replace("+", "").Split('-')[0]);
            }
        }

        public int CurrentMinVersion = 1202;
        #endregion

        #region 版本数据源
        [ObservableProperty]
        private ObservableCollection<TextComboBoxItem> _versionSource =
            [
                new TextComboBoxItem()
                {
                    Text = "1.20.4"
                }
            ];
        #endregion

        #endregion

        #region Method
        public RecipeViewModel(IContainerProvider container,DataService dataService,MainView mainView,CBHKDataContext context)
        {
            context = context;
            container = container;
            _dataService = dataService;

            home = mainView;

            RecipeList[0].Content = new CraftingTableView() { FontWeight = FontWeights.Normal };

            AddOriginalItemProgress = new Progress<ItemStructure>(OriginalItemList.Add);
            SetOriginalItemProgress = new Progress<(int, string, string, string)>(item =>
            {
                OriginalItemList[item.Item1].IDAndName = item.Item2 + ':' + item.Item3;
                bool haveImage = File.Exists(item.Item4);
                OriginalItemList[item.Item1].ImagePath = haveImage ? new BitmapImage(new Uri(item.Item4, UriKind.Absolute)) : null;
            });

            AddCustomItemProgress = new Progress<ItemStructure>(CustomItemList.Add);
            SetCustomItemProgress = new Progress<(int, string, string, string, string)>(item =>
            {
                CustomItemList[item.Item1].IDAndName = item.Item2 + ':' + item.Item3;
                CustomItemList[item.Item1].ImagePath = File.Exists(item.Item4) ? new BitmapImage(new Uri(item.Item4, UriKind.Absolute)) : null;
                CustomItemList[item.Item1].NBT = item.Item5;
            });

            ItemIDAndNameMap = _dataService.GetItemIDAndNameGroupByVersionMap()
            .Where(pair => pair.Key <= CurrentMinVersion)
            .SelectMany(pair => pair.Value)
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value
            );

            List<string> HaveNoIImageList = [];
            foreach (var item in ItemIDAndNameMap)
            {
                if (!File.Exists(ImageSetFolderPath + item.Key + ".png") && !File.Exists(ImageSetFolderPath + item.Key + "_spawn_egg.png"))
                {
                    HaveNoIImageList.Add(item.Key);
                }
            }

            foreach (var item in HaveNoIImageList)
            {
                ItemIDAndNameMap.Remove(item);
            }

            ItemKeyList = [.. ItemIDAndNameMap.Select(item => item.Key)];
            ItemKeyList.Sort();
        }

        /// <summary>
        /// 初始化物品ID与版本物品ID列表
        /// </summary>
        private async Task InitOriginItemList()
        {
            OriginalItemList.Clear();

            ParallelOptions parallelOptions = new();
            await Parallel.ForAsync(0, ItemKeyList.Count, parallelOptions, (i, cancellationToken) =>
            {
                AddOriginalItemProgress.Report(new());
                return new ValueTask();
            });

            Parallel.For(0, ItemKeyList.Count, (i) =>
            {
                string currentKey = ItemKeyList[i];
                string imagePath = "";
                if (File.Exists(ImageSetFolderPath + currentKey + ".png"))
                {
                    imagePath = ImageSetFolderPath + currentKey + ".png";
                }
                else
                if (File.Exists(ImageSetFolderPath + currentKey + "_spawn_egg.png"))
                {
                    imagePath = ImageSetFolderPath + currentKey + "_spawn_egg.png";
                }

                if (imagePath.Length > 0)
                {
                    SetOriginalItemProgress.Report(new ValueTuple<int, string, string, string>(i, currentKey, ItemIDAndNameMap[currentKey], imagePath));
                }
            });
        }

        /// <summary>
        /// 初始化自定义物品列表
        /// </summary>
        private async Task InitCustomItemList()
        {
            CustomItemList.Clear();
            string[] itemFileList = Directory.GetFiles(ItemSaveFolderPath);
            ParallelOptions parallelOptions = new();
            await Parallel.ForAsync(0, itemFileList.Length, parallelOptions, (i, cancellationToken) =>
            {
                if (File.Exists(ImageSetFolderPath + ItemKeyList[i] + ".png") || File.Exists(ImageSetFolderPath + ItemKeyList[i] + "_spawn_egg.png"))
                {
                    AddCustomItemProgress.Report(new ItemStructure());
                }
                return new ValueTask();
            });

            Parallel.For(0, itemFileList.Length, (i) =>
            {
                if (File.Exists(itemFileList[i]))
                {
                    string nbt = ExternalDataImportManager.GetItemDataHandler(itemFileList[i], true);
                    string currentKey = "";
                    if (JObject.Parse(nbt)["oldID"] is JToken IDToken)
                    {
                        currentKey = IDToken.Value<string>().Replace("minecraft:", "");
                    }
                    string imagePath = "";
                    if (File.Exists(ImageSetFolderPath + currentKey + ".png"))
                    {
                        imagePath = ImageSetFolderPath + currentKey + ".png";
                    }
                    else
                    if (File.Exists(ImageSetFolderPath + currentKey + "_spawn_egg.png"))
                    {
                        imagePath = ImageSetFolderPath + currentKey + "_spawn_egg.png";
                    }

                    if (imagePath.Length > 0)
                    {
                        SetCustomItemProgress.Report(new ValueTuple<int, string, string, string, string>(i, currentKey, ItemIDAndNameMap[currentKey], imagePath, nbt));
                    }
                }
            });
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
                    CampfireView campfire = new() { FontWeight = FontWeights.Normal };
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

        #endregion

        #region Event
        /// <summary>
        /// 订阅原版物品库过滤事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void OriginalListView_Loaded(object sender,RoutedEventArgs e)
        {
            if (OriginalItemList.Count == 0)
            {
                Window window = Window.GetWindow(sender as ListView);
                OriginalItemViewSource = window.FindResource("OriginalItemView") as CollectionViewSource;
                OriginalItemViewSource.Filter += CollectionViewSource_Filter;
                await InitOriginItemList();
                OriginalItemViewSource.View?.Refresh();
            }
        }

        /// <summary>
        /// 订阅自定义物品库过滤事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void CustomListView_Loaded(object sender, RoutedEventArgs e)
        {
            if (CustomItemList.Count == 0)
            {
                Window window = Window.GetWindow(sender as ListView);
                CustomItemViewSource = window.FindResource("CustomItemView") as CollectionViewSource;
                CustomItemViewSource.Filter += CollectionViewSource_Filter;
                await InitCustomItemList();
                CustomItemViewSource.View?.Refresh();
            }
        }

        /// <summary>
        /// 搜索文本更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SelectedItemListIndex == 0)
            {
                OriginalItemViewSource.View?.Refresh();
            }
            else
            {
                CustomItemViewSource.View?.Refresh();
            }
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
                while (obj is not null && obj is not ListViewItem)
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

                if (IsGrabingItem && drag_source is not null)
                {
                    DataObject dataObject = new(typeof(Image), GrabedImage);
                    if (dataObject is not null)
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
            if (IsGrabingItem && drag_source is not null && GrabedImage is not null)
            {
                DataObject dataObject = new(typeof(Image), GrabedImage);
                if(dataObject is not null)
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
            if(hitTestResult is not null)
            {
                var item = hitTestResult.VisualHit;
                while (item is not null && item is not ListViewItem)
                    item = VisualTreeHelper.GetParent(item);

                if (item is not null)
                {
                    int i = listView.Items.IndexOf(((ListViewItem)item).DataContext);
                    if (i >= 0 && i < listView.Items.Count)
                        listView.SelectedIndex = i;
                }
            }
        }
        #endregion
    }
}