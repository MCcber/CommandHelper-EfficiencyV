using CBHK.CustomControl.Container;
using CBHK.CustomControl.VectorComboBox;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using CBHK.Utility.Data;
using CBHK.Utility.Visual.MessageTip;
using CBHK.View;
using CBHK.Interface.Component.Recipe;
using CBHK.ViewModel.Component.Recipe;
using CBHK.ViewModel.Component.Recipe.Event;
using CBHK.ViewModel.Component.Recipe.Strategy;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Prism.Events;
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


        private IContainerProvider container = null;
        private RecipeStrategyRegistry strategies = null;
        private IEventAggregator eventAggregator = null;
        private Resource resource = null;
        #endregion

        #region Property
        public MessagePopup MessagePopup { get; set; }
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
        public ObservableCollection<RecipeTabViewModel> _recipeList = [];

        /// <summary>
        /// 当前选中的配方页签
        /// </summary>
        [ObservableProperty]
        private RecipeTabViewModel _selectedTab;

        /// <summary>
        /// 配方格式版本下拉数据源
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<VectorTextComboBoxItem> _versionSource = [];

        /// <summary>
        /// 当前选中的配方格式版本
        /// </summary>
        [ObservableProperty]
        private VectorTextComboBoxItem _selectedVersion;

        #endregion

        #region Method
        public RecipeViewModel(IContainerProvider Container, MainView mainView, Resource Resource, RecipeStrategyRegistry Strategies, IEventAggregator EventAggregator)
        {
            container = Container;
            resource = Resource;
            strategies = Strategies;
            eventAggregator = EventAggregator;

            home = mainView;

            eventAggregator.GetEvent<CloseRecipeTabEvent>().Subscribe(RemoveTab, ThreadOption.UIThread);

            RecipeTabViewModel firstTab = CreateTab(strategies.Get(RecipeType.CraftingTable));
            RecipeList.Add(firstTab);
            SelectedTab = firstTab;

            VersionSource = new ObservableCollection<VectorTextComboBoxItem>(RecipeVersionCatalog.CreateMembers(resource));
            SelectedVersion = VersionSource.Count > 0 ? VersionSource[^1] : null;

        }

        /// <summary>
        /// 切换配方格式版本：裁掉多选槽位里该版本不存在的物品，并刷新两栏物品库
        /// </summary>
        public async Task ApplyVersionAsync(VectorTextComboBoxItem version)
        {
            string dataKey = RecipeVersionCatalog.ResolveDataKey(resource, version?.Text);
            if (dataKey is null)
                return;

            try
            {
                // 版本物品数据随切随读，不做缓存
                HashSet<string> validItemIds = new(ItemLibraryLoader.LoadItemIds(resource, dataKey), StringComparer.OrdinalIgnoreCase);

                RecipeFormat format = RecipeFormat.ForVersion(version?.Text);
                foreach (RecipeTabViewModel tab in RecipeList)
                {
                    tab.Editor.Format = format;
                    tab.Editor.PruneItemsToVersion(validItemIds);
                }

                await RefreshOriginalItemsAsync(dataKey);
                await RefreshCustomItemsAsync(dataKey);
            }
            catch
            {
                // 版本数据不可用时不打断界面
            }
        }

        partial void OnSelectedVersionChanged(VectorTextComboBoxItem value)
        {
            _ = ApplyVersionAsync(value);
        }

        /// <summary>
        /// 载入当前版本的原版物品库（与村民页共用同一套后台）
        /// </summary>
        private async Task InitOriginItemList()
        {
            await RefreshOriginalItemsAsync(RecipeVersionCatalog.ResolveDataKey(resource, SelectedVersion?.Text));
        }

        /// <summary>
        /// 载入当前版本的自定义物品库（与村民页共用同一套后台）
        /// </summary>
        private async Task InitCustomItemList()
        {
            await RefreshCustomItemsAsync(RecipeVersionCatalog.ResolveDataKey(resource, SelectedVersion?.Text));
        }

        private async Task RefreshOriginalItemsAsync(string dataKey)
        {
            if (dataKey is null)
                return;

            List<ItemStructure> items = await ItemLibraryLoader.LoadOriginalItemsAsync(resource, dataKey);
            OriginalItemList.Clear();
            foreach (ItemStructure item in items)
                OriginalItemList.Add(item);

            OriginalItemViewSource?.View?.Refresh();
        }

        private async Task RefreshCustomItemsAsync(string dataKey)
        {
            if (dataKey is null)
                return;

            List<ItemStructure> items = await ItemLibraryLoader.LoadCustomItemsAsync(resource);
            List<string> itemIds = ItemLibraryLoader.LoadItemIds(resource, dataKey);
            if (itemIds.Count > 0)
            {
                HashSet<string> validItemIds = new(itemIds, StringComparer.OrdinalIgnoreCase);
                items = [.. items.Where(item => validItemIds.Contains(RecipeSlotViewModel.GetItemId(item)))];
            }

            CustomItemList = new ObservableCollection<ItemStructure>(items);
            CustomItemViewSource?.View?.Refresh();
        }

        /// <summary>
        /// 按策略创建页签
        /// </summary>
        private RecipeTabViewModel CreateTab(IRecipeStrategy strategy)
        {
            RecipeEditorViewModelBase editor = strategy.CreateEditor(container);
            editor.MessagePopup = MessagePopup;
            editor.Format = RecipeFormat.ForVersion(SelectedVersion?.Text);
            return new RecipeTabViewModel(editor, strategy, eventAggregator);
        }

        /// <summary>
        /// 移除页签并重选相邻页签
        /// </summary>
        private void RemoveTab(RecipeTabViewModel tab)
        {
            if (tab is null || !RecipeList.Contains(tab))
                return;

            int index = RecipeList.IndexOf(tab);
            tab.Dispose();
            RecipeList.Remove(tab);

            SelectedTab = RecipeList.Count > 0 ? RecipeList[Math.Min(index, RecipeList.Count - 1)] : null;
        }

        #endregion

        #region Event
        public void RecipeView_Loaded(object sender,RoutedEventArgs e)
        {
            if(MessagePopup is null && sender is Window window)
            {
                MessagePopup = new(window);
            }
        }

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
        /// 添加配方：所有菜单项共用一个入口，按菜单 Uid 找策略
        /// </summary>
        private void AddRecipe(string uid)
        {
            IRecipeStrategy strategy = strategies.GetByUid(uid) ?? strategies.Get(RecipeType.CraftingTable);
            if (strategy is null)
                return;

            RecipeTabViewModel tab = CreateTab(strategy);
            RecipeList.Add(tab);
            SelectedTab = tab;
        }

        [RelayCommand]
        /// <summary>
        /// 清空配方：uid 为空表示全部
        /// </summary>
        private void ClearRecipes(string uid)
        {
            if (string.IsNullOrWhiteSpace(uid))
            {
                foreach (RecipeTabViewModel tab in RecipeList.ToList())
                    RemoveTab(tab);
                return;
            }

            foreach (RecipeTabViewModel tab in RecipeList.Where(item => item.Strategy.Uid == uid).ToList())
                RemoveTab(tab);
        }

        [RelayCommand]
        /// <summary>
        /// 从剪切板导入配方
        /// </summary>
        private void ImportFromClipboard()
        {
            ImportRecipeData(Clipboard.GetText(), MessagePopup, isPath: false);
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
                ImportRecipeData(openFileDialog.FileName, MessagePopup);
            }
        }

        /// <summary>
        /// 载入添加配方按钮的图标
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void AddRecipeMenuIconLoaded(object sender,RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem)
                return;

            string iconFolder = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Recipe\Image\";
            string iconPath = iconFolder + menuItem.Uid + "_icon.png";
            if (!File.Exists(iconPath))
                iconPath = iconFolder + menuItem.Uid + ".png";
            // 图标缺失时留空，不要用不存在的路径构造 BitmapImage
            if (!File.Exists(iconPath))
                return;

            menuItem.Icon = new Image
            {
                Source = new BitmapImage(new Uri(iconPath))
            };
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
        /// 执行生成：按每个子页设置的文件名，把全部页签各写一份到所选目录
        /// </summary>
        private async Task Run()
        {
            OpenFolderDialog openFolderDialog = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Title = "请选择配方文件生成路径",
                ShowHiddenItems = true,
            };
            if (!openFolderDialog.ShowDialog().Value)
                return;

            string selectedPath = openFolderDialog.FolderName;
            if (!selectedPath.EndsWith("\\"))
                selectedPath += "\\";

            HashSet<string> usedNames = new(StringComparer.OrdinalIgnoreCase);
            List<string> failedNames = [];
            List<string> missingMaterials = [];
            int index = 0;

            foreach (RecipeTabViewModel tab in RecipeList)
            {
                index++;
                // 只取值不落盘，也不动子页自己的保存开关（否则子页的运行按钮会被永久禁用）
                RecipeEditorViewModelBase editor = tab.Editor;
                string fileName = BuildRecipeFileName(editor.FileName, tab.Title, index, usedNames);
                try
                {
                    string content = editor.BuildResult();
                    if (editor.BuildWarnings.Count > 0)
                        missingMaterials.Add(tab.Title + "：" + string.Join("；", editor.BuildWarnings));
                    await File.WriteAllTextAsync(selectedPath + fileName + ".json", content);
                }
                catch
                {
                    failedNames.Add(fileName);
                }
            }

            if (missingMaterials.Count > 0)
            {
                MessagePopup.PushMessage(new GeneratorMessage()
                {
                    Message = string.Join("｜", missingMaterials),
                    MessageBrush = RecipeEditorViewModelBase.WarningMessageBrush,
                    SubMessage = "有点配方缺材料，已用 stone 占位",
                    SubMessageBrush = Brushes.DarkGray,
                    Icon = RecipeEditorViewModelBase.RecipeToastIcon
                });
                // 给缺料提示留出阅读时间，避免被随后的成功提示挤在一起看不清
                await Task.Delay(1500);
            }

            MessagePopup.PushMessage(new GeneratorMessage()
            {
                Message = failedNames.Count == 0
                    ? "配方全部生成成功！"
                    : "有 " + failedNames.Count + " 个配方生成失败：" + string.Join("、", failedNames),
                MessageBrush = failedNames.Count == 0 ? RecipeEditorViewModelBase.SuccessMessageBrush : Brushes.Red,
                SubMessage = "配方生成器",
                SubMessageBrush = Brushes.DarkGray,
                Icon = RecipeEditorViewModelBase.RecipeToastIcon
            });
        }

        /// <summary>
        /// 生成安全的文件名：优先用子页设置的名字，为空时退回页签标题，再重复就加序号
        /// </summary>
        private static string BuildRecipeFileName(string fileName, string title, int index, HashSet<string> usedNames)
        {
            string name = string.IsNullOrWhiteSpace(fileName) ? title : fileName.Trim();
            name = string.Concat((name ?? "").Split(Path.GetInvalidFileNameChars()));
            if (string.IsNullOrWhiteSpace(name))
                name = "recipe" + index;

            string candidate = name;
            int suffix = 2;
            while (!usedNames.Add(candidate))
            {
                candidate = name + "_" + suffix;
                suffix++;
            }
            return candidate;
        }

        /// <summary>
        /// 物品过滤事件（物品库的 IDAndName 只存裸 id）
        /// </summary>
        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            string searchText = SearchText.Trim();
            if (searchText.Length == 0)
            {
                e.Accepted = true;
                return;
            }

            e.Accepted = e.Item is ItemStructure itemStructure
                && !string.IsNullOrEmpty(itemStructure.IDAndName)
                && itemStructure.IDAndName.Contains(searchText, StringComparison.OrdinalIgnoreCase);
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
            originalItemViewer = (tabControl.Items[0] as VectorTextTabItem).Content as ListView;
            originalItemViewer.DataContext = this;
            originalItemViewer.MouseMove += Bag_MouseMove;
            originalItemViewer.PreviewMouseLeftButtonDown += SelectItemClickDown;
            originalItemViewer.MouseLeave += ListBox_MouseLeave;
            #endregion

            #region 自定义物品库
            customItemViewer = (tabControl.Items[1] as VectorTextTabItem).Content as ListView;
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

        #region 外部数据导入（并入本类，不再单独成文件）
        public void ImportRecipeData(string filePathOrData, MessagePopup messagePopup, bool isPath = true)
        {
            string data = isPath ? File.ReadAllText(filePathOrData) : filePathOrData;
            try
            {
                JObject json = JObject.Parse(data);
                if (json["type"] is JToken recipeType && strategies.TryGetByJsonType(recipeType.ToString(), out IRecipeStrategy strategy))
                {
                    RecipeTabViewModel tab = CreateTab(strategy);
                    tab.Editor.LoadExternalData(json);
                    RecipeList.Add(tab);
                    SelectedTab = tab;
                }
            }
            catch
            {
                messagePopup.PushMessage(new GeneratorMessage()
                {
                    Message = "导入失败！文件内容格式不合法",
                    MessageBrush = Brushes.Red,
                    SubMessage = "配方生成器",
                    SubMessageBrush = Brushes.DarkGray,
                    Icon = RecipeEditorViewModelBase.RecipeToastIcon
                });
            }
        }
        #endregion
    }
}
