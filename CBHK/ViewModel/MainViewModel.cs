using CBHK.CustomControl.Container;
using CBHK.CustomControl.VectorButton;
using CBHK.Domain;
using CBHK.Domain.DataContext;
using CBHK.Domain.Model;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using CBHK.Utility;
using CBHK.Utility.Data.Mcdoc;
using CBHK.Utility.Visual;
using CBHK.Utility.Visual.MessageTip;
using CBHK.View.Common;
using CBHK.ViewModel.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CBHK.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        #region Field
        private readonly IContainerProvider container;
        private readonly EnvironmentSettingsService settingsService;
        private readonly RegistryDataService dataRegistry;
        private readonly Resource resource;

        /// <summary>提示器要等窗口存在才建（AdornerLayer 挂在窗口上），构造期直接 new 会在启动阶段拿不到层。</summary>
        private MessagePopup messagePopup;
        private MessagePopup Messages => messagePopup ??= new MessagePopup(Application.Current?.MainWindow);
        private int categoryKindIndex;
        /// <summary>分类维度 → 标题、按钮工厂、过滤谓词。新增维度只往这里加一项。</summary>
        private readonly Dictionary<int, CategoryStrategy> categoryStrategies;
        /// <summary>当前分类维度下选中的过滤项；null/All 表示不筛选。</summary>
        private string selectedCategoryId;
        /// <summary>「全部」按钮的 Id，三个分类维度共用（版本维度也用它）。</summary>
        private const string AllCategoryId = "All";

        private sealed record CategoryStrategy(
            string Title,
            Func<List<CategoryButtonModel>> CreateButtons,
            Func<GeneratorEntry, string, bool> Matches);

        private int sortKindIndex;
        /// <summary>排序维度 → 标题、排序键。新增排序只往这里加一项。</summary>
        private readonly List<SortStrategy> sortStrategies;
        /// <summary>
        /// 拼音比较走 zh-CN 文化序：ChinaPinyin 填了就是字段本身的字母序，没填时退回中文名也还是拼音序。
        /// </summary>
        private static readonly StringComparer PinyinComparer =
            StringComparer.Create(CultureInfo.GetCultureInfo("zh-CN"), ignoreCase: true);

        private sealed record SortStrategy(
            string Title,
            Func<IEnumerable<GeneratorEntry>, IEnumerable<GeneratorEntry>> Order);

        private bool languageServerStartupTriggered;
        private bool startupFinished;
        private bool startupNoticeShown;
        private Color SkeletonLighterColor = new();
        private Color SkeletonDarkerColor = new();
        /// <summary>
        /// 窗体属性设置
        /// </summary>
        public EnvironmentSettings config = null;
        private Grid SkeletonGrid;
        private Grid GeneratorTable;
        private Grid InnerGeneratorTable = new();
        /// <summary>
        /// 初始化界面数据
        /// </summary>
        private IProgress<byte> InitUIDataProgress = null;
        #endregion

        #region Property
        [ObservableProperty]
        public WindowState windowState = WindowState.Normal;
        [ObservableProperty]
        private bool showInTaskBar = true;
        [ObservableProperty]
        private string searchString = string.Empty;
        [ObservableProperty]
        private ObservableCollection<CategoryButtonModel> categoryButtonList = [];
        [ObservableProperty]
        private string selectedCategoryTitle = string.Empty;
        [ObservableProperty]
        private string selectedSortTitle = string.Empty;
        #endregion

        #region Method
        public MainViewModel(IContainerProvider Container, Resource Resource, EnvironmentSettingsService SettingsService, RegistryDataService DataRegistry)
        {
            container = Container;
            resource = Resource;
            settingsService = SettingsService;
            dataRegistry = DataRegistry;
            sortStrategies =
            [
                new("默认排序", entries => entries),
                new("按英文字母升序", entries => entries.OrderBy(SortEnglish, StringComparer.OrdinalIgnoreCase)),
                new("按英文字母降序", entries => entries.OrderByDescending(SortEnglish, StringComparer.OrdinalIgnoreCase)),
                new("按中文拼音升序", entries => entries.OrderBy(SortPinyin, PinyinComparer)),
                new("按中文拼音降序", entries => entries.OrderByDescending(SortPinyin, PinyinComparer))
            ];
            categoryStrategies = new()
            {
                [0] = new(
                    "按功能分类",
                    () => BuildCategoryButtons(
                        ("实体", GeneratorCategory.Entity, "Creeper"),
                        ("物品", GeneratorCategory.Item, "Diamond"),
                        ("资源", GeneratorCategory.Resource, "ResourceChest"),
                        ("世界", GeneratorCategory.World, "World"),
                        ("数据包", GeneratorCategory.Datapack, "Folder"),
                        ("命令", GeneratorCategory.Command, "Command")),
                    (entry, id) => HasAnyCategory(entry.FunctionCategory, id)),
                [1] = new(
                    "按产出分类",
                    () => BuildCategoryButtons(
                        ("命令", GeneratorCategory.Command, "Command"),
                        ("Json文件", GeneratorCategory.JsonFile, "JsonFile"),
                        ("NBT文件", GeneratorCategory.NbtFile, "NBTFile")),
                    (entry, id) => HasAnyCategory(entry.OutputCategory, id)),
                [2] = new("按版本分类", BuildVersionCategoryButtons, IsAvailableInVersion)
            };
            ShowCategoryButtons();
            ShowSortMode();
        }

        private void ShowCategoryButtons()
        {
            CategoryStrategy strategy = categoryStrategies[categoryKindIndex];
            SelectedCategoryTitle = strategy.Title;
            selectedCategoryId = null;
            CategoryButtonList = new(strategy.CreateButtons());
            SyncCategoryButtonSelection();
            RebuildGeneratorButtons();
        }

        private void ShowSortMode()
        {
            SelectedSortTitle = sortStrategies[sortKindIndex].Title;
            RebuildGeneratorButtons();
        }

        /// <summary>
        /// ToggleButton 会自行翻转被点的那一项，所以过滤状态变化后要统一回写：
        /// 当前过滤项保持选中，其余项全部取消。没有具体过滤项时回到“全部”。
        /// </summary>
        private void SyncCategoryButtonSelection()
        {
            string targetId = string.IsNullOrEmpty(selectedCategoryId) ? AllCategoryId : selectedCategoryId;
            CategoryButtonModel selected = CategoryButtonList.FirstOrDefault(
                item => string.Equals(item.ID, targetId, StringComparison.Ordinal));
            foreach (CategoryButtonModel item in CategoryButtonList)
            {
                item.IsChecked = ReferenceEquals(item, selected);
            }
        }

        /// <summary>功能/产出两个维度共用「全部」按钮；其余按钮的 Id 就是枚举名。</summary>
        private static List<CategoryButtonModel> BuildCategoryButtons(params (string Text, GeneratorCategory Category, string IconKey)[] definitions)
            => [CreateCategoryButton("全部", AllCategoryId, "Category"),
                .. definitions.Select(definition => CreateCategoryButton(definition.Text, definition.Category, definition.IconKey))];

        /// <summary>
        /// 版本按钮按“可用生成器集合完全相同”合并：连续且集合一致的版本显示成一个区间，
        /// 集合只覆盖一个版本的仍单独显示，避免 19 个版本按钮铺满分类栏。
        /// 区间的 ID 取区间内最低版本，过滤时用该版本判断即可（集合一致）。
        /// </summary>
        private List<CategoryButtonModel> BuildVersionCategoryButtons()
        {
            List<CategoryButtonModel> buttons = [CreateCategoryButton("全部", AllCategoryId, "Version")];
            List<string> versions = [.. dataRegistry.Versions
                .Where(version => !string.IsNullOrWhiteSpace(version))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(version => version, Comparer<string>.Create(McdocAttributeRegistry.CompareVersions))];

            if (versions.Count == 0)
            {
                return buttons;
            }

            List<List<string>> groups = [];
            List<string> currentGroup = null;
            string currentSignature = null;
            foreach (string version in versions)
            {
                string signature = string.Join("|", GeneratorCatalog.Entries
                    .Where(entry => IsAvailableInVersion(entry, version))
                    .Select(entry => entry.Id));
                if (currentGroup is null || !string.Equals(signature, currentSignature, StringComparison.Ordinal))
                {
                    currentGroup = [];
                    groups.Add(currentGroup);
                    currentSignature = signature;
                }
                currentGroup.Add(version);
            }

            for (int i = groups.Count - 1; i >= 0; i--)
            {
                List<string> group = groups[i];
                string text = group.Count == 1 ? group[0] : group[0] + " – " + group[^1];
                buttons.Add(CreateCategoryButton(text, group[0], "Version"));
            }
            return buttons;
        }

        private static CategoryButtonModel CreateCategoryButton(string text, string id, string iconKey)
            => new() { Text = text, ID = id, Icon = CategoryIcon(iconKey) };

        private static CategoryButtonModel CreateCategoryButton(string text, GeneratorCategory category, string iconKey)
            => CreateCategoryButton(text, category.ToString(), iconKey);

        /// <summary>入口挂了多个类别时，任一类别的按钮都该列出它（魔咒既在资源下也在数据包下）。</summary>
        private static bool HasAnyCategory(GeneratorCategory categories, string categoryId)
            => Enum.TryParse(categoryId, out GeneratorCategory filter)
                && (categories & filter) != GeneratorCategory.None;

        /// <summary>英文单词没填时退回入口 Id（本身就是英文），空值不会全挤到最前面。</summary>
        private static string SortEnglish(GeneratorEntry entry)
            => string.IsNullOrWhiteSpace(entry.EnglishWord) ? entry.Id : entry.EnglishWord;

        /// <summary>拼音没填时退回中文名，配合 zh-CN 文化序仍是拼音序。</summary>
        private static string SortPinyin(GeneratorEntry entry)
            => string.IsNullOrWhiteSpace(entry.ChinaPinyin) ? entry.Name : entry.ChinaPinyin;

        private static ImageSource CategoryIcon(string key)
            => Application.Current?.TryFindResource(key) as ImageSource;

        /// <summary>
        /// 按当前分类/版本重建生成器面板。分类切换和 mcdoc 索引就绪都会走到这里，
        /// 所以必须先清空旧的 RowDefinition/按钮，不能像原来那样只追加一次。
        /// </summary>
        private void RebuildGeneratorButtons()
        {
            if (GeneratorTable is null || !dataRegistry.IsLoaded)
            {
                return;
            }

            if (!GeneratorTable.Children.Contains(InnerGeneratorTable))
            {
                GeneratorTable.Children.Add(InnerGeneratorTable);
            }

            InnerGeneratorTable.Children.Clear();
            InnerGeneratorTable.RowDefinitions.Clear();
            InnerGeneratorTable.ColumnDefinitions.Clear();
            for (int i = 0; i < 4; i++)
            {
                InnerGeneratorTable.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            }

            DistributorGenerator generatorFunction = container.Resolve<DistributorGenerator>();
            string baseImagePath = "pack://application:,,,/CBHK;component/Resource/CBHK/Image/Generator/";
            int rowIndex = 0;
            int columnIndex = 0;

            IEnumerable<GeneratorEntry> entries = GeneratorCatalog.Entries.Where(MatchesSelectedCategory);
            foreach (GeneratorEntry entry in sortStrategies[sortKindIndex].Order(entries))
            {
                if (columnIndex == 0)
                {
                    InnerGeneratorTable.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(80, GridUnitType.Pixel) });
                }

                GeneratorVectorButton button = new()
                {
                    Style = Application.Current.Resources["GeneratorVectorButtonStyle"] as Style,
                    Title = entry.Name,
                    SubTitle = entry.Id
                };
                try
                {
                    button.Icon = new BitmapImage(new Uri(baseImagePath + entry.Id + ".png", UriKind.Absolute));
                }
                catch
                {
                }
                button.Command = generatorFunction.GetGeneratorClickCommand(entry.Id);

                InnerGeneratorTable.Children.Add(button);
                Grid.SetRow(button, rowIndex);
                Grid.SetColumn(button, columnIndex);

                columnIndex++;
                if (columnIndex == InnerGeneratorTable.ColumnDefinitions.Count)
                {
                    columnIndex = 0;
                    rowIndex++;
                }
            }
        }

        private bool MatchesSelectedCategory(GeneratorEntry entry)
        {
            if (string.IsNullOrEmpty(selectedCategoryId) || string.Equals(selectedCategoryId, AllCategoryId, StringComparison.Ordinal))
            {
                return true;
            }

            return categoryStrategies[categoryKindIndex].Matches(entry, selectedCategoryId);
        }

        private bool IsAvailableInVersion(GeneratorEntry entry, string version)
        {
            (string since, string until) = ResolveEntryVersionRange(entry);
            return (string.IsNullOrEmpty(since) || McdocAttributeRegistry.CompareVersions(version, since) >= 0)
                && (string.IsNullOrEmpty(until) || McdocAttributeRegistry.CompareVersions(version, until) < 0);
        }

        /// <summary>
        /// 入口的版本区间。优先用 GeneratorCatalog 上的静态 since/until：主页启动时 mcdoc 索引可能还没就绪，
        /// 而且部分入口的 dispatch 不在 minecraft:resource 下。没有静态元数据时才退回 mcdoc 索引。
        /// since 是包含下限，until 是排除上限；都取不到时返回 (null, null) 并按所有版本可用处理。
        /// </summary>
        private (string Since, string Until) ResolveEntryVersionRange(GeneratorEntry entry)
        {
            if (!string.IsNullOrEmpty(entry.SinceVersion) || !string.IsNullOrEmpty(entry.UntilVersion))
            {
                return (entry.SinceVersion, entry.UntilVersion);
            }

            if (string.IsNullOrEmpty(entry.McdocEntryId) || resource?.McdocTypes is null)
            {
                return (null, null);
            }

            McdocType root = McdocHelpers.GetRootType(entry.McdocEntryId);
            if (root is null || root.Kind != McdocTypeKind.Dispatcher)
            {
                return (null, null);
            }

            string index = root.Indices?.FirstOrDefault(item => item is { Kind: McdocIndexKind.Static })?.Value;
            if (string.IsNullOrEmpty(index))
            {
                return (null, null);
            }

            IReadOnlyDictionary<string, McdocType> members = resource.McdocTypes.GetDispatcherMembers(root.Registry, null);
            if (members is null || !members.TryGetValue(index, out McdocType member) || member?.Attributes is null)
            {
                return (null, null);
            }

            string since = null;
            string until = null;
            foreach (McdocAttribute attribute in member.Attributes)
            {
                if (attribute is null)
                {
                    continue;
                }
                if (string.Equals(attribute.Name, "since", StringComparison.Ordinal))
                {
                    since = attribute.Value?.AsString();
                }
                else if (string.Equals(attribute.Name, "until", StringComparison.Ordinal))
                {
                    until = attribute.Value?.AsString();
                }
            }
            return (since, until);
        }


        /// <summary>
        /// 发布版在后台把语言服务器拉起来（Debug 保持惰性启动，免得每次调试都多拉一个进程）。
        /// </summary>
        private void StartLanguageServerInBackground()
        {
            if (languageServerStartupTriggered)
            {
                return;
            }
            languageServerStartupTriggered = true;
            _ = System.Threading.Tasks.Task.Run(() =>
            {
                McdocLanguageServerStatus status = McdocLanguageServerHost.Shared.EnsureRunning(
                    AppDomain.CurrentDomain.BaseDirectory, TimeSpan.FromSeconds(15));
                if (status.IsRunning)
                {
                    return;
                }
                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                    Messages.PushMessage(new GeneratorMessage()
                    {
                        MessageBrush = Brushes.Red,
                        SubMessage = "启动失败",
                        Message = "语言服务器不可用：" + status.Reason,
                        SubMessageBrush = Brushes.Gray
                    })));
            });
        }

        /// <summary>
        /// 收起骨架屏。索引可能极快结束（语言服务器不可用时 40ms 就返回），
        /// 那时视图的 Loaded 还没跑，SkeletonGrid/GeneratorTable 还是 null —— 直接解引用会在 async void 回调里
        /// 抛未处理异常把应用打死。所以这里只记状态，真正收起由两个 Loaded 回调补。
        /// </summary>
        private void StopSkeletonScreen()
        {
            startupFinished = true;
            TryFinishSkeleton();
        }

        private void TryFinishSkeleton()
        {
            if (!startupFinished || SkeletonGrid is null || GeneratorTable is null)
            {
                return;
            }
            SkeletonGrid.Visibility = Visibility.Collapsed;
            GeneratorTable.Visibility = Visibility.Visible;
            ShowStartupNotice();
        }

        private void ShowStartupNotice()
        {
            if (startupNoticeShown || config is null)
            {
                return;
            }
            startupNoticeShown = true;
            if (!bool.TryParse(config.ShowNotice, out bool showNotice) || !showNotice)
            {
                return;
            }
            NoticeToUsersView noticeToUsers = container.Resolve<NoticeToUsersView>();
            noticeToUsers.Topmost = true;
            NoticeToUsersViewModel notichViewModel = noticeToUsers.DataContext as NoticeToUsersViewModel;
            if (noticeToUsers.ShowDialog().Value)
            {
                config.ShowNotice = (!notichViewModel.DonotShowNextTime).ToString();
            }
        }

        /// <summary>
        /// 读取启动器配置
        /// </summary>
        private void ReadDataSource() => InitUIDataProgress.Report(0);

        /// <summary>
        /// 按当前的 CommonBackground 重算骨架屏两档颜色并重挂动画。
        /// 颜色是代码算出来的（不是 DynamicResource），所以主题变化时只能主动重算。
        /// </summary>
        private void RefreshSkeletonColors()
        {
            if (SkeletonGrid is null
                || Application.Current.Resources[Theme.CommonBackground] is not SolidColorBrush commonBackgroundBrush)
            {
                return;
            }

            SkeletonDarkerColor = ColorTool.Darken(commonBackgroundBrush.Color, 0.4f);
            SkeletonLighterColor = ColorTool.Lighten(commonBackgroundBrush.Color, 0.4f);

            for (int i = 0; i < SkeletonGrid.Children.Count; i++)
            {
                if (SkeletonGrid.Children[i] is not Rectangle rectangle)
                {
                    continue;
                }

                if (rectangle.Uid == "SkeletonBlock")
                {
                    rectangle.ApplyBreathAnimation(SkeletonLighterColor, SkeletonDarkerColor, new Duration(TimeSpan.FromSeconds(0.5)));
                }
                else if (rectangle.Uid == "SkeletonLine")
                {
                    rectangle.ApplySweepAnimation(SkeletonLighterColor, SkeletonDarkerColor, new Duration(TimeSpan.FromSeconds(0.5)));
                }
            }
        }
        #endregion

        #region Event
        /// <summary>
        /// 载入窗体内容
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [RelayCommand]
        private void MainWindowLoaded()
        {
#if !DEBUG
            StartLanguageServerInBackground();
#endif

            #region 处理界面
            InitUIDataProgress = new Progress<byte>(async (number) =>
            {
                try
                {
                    // Init 内部自带语言服务器拉起、单文档记账与总预算；这里只保证启动流程一定往下走
                    await resource.Init();
                }
                catch (Exception error)
                {
                    System.Diagnostics.Debug.WriteLine("mcdoc 索引失败：" + error);
                }
                finally
                {
                    StopSkeletonScreen();
                }

                #region 加载用户数据
                //if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"Resource\UserHead.png"))
                //{
                //    UserHead = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Resource\UserHead.png", UriKind.RelativeOrAbsolute));
                //}
                //if (UserData.TryGetValue("UserID", out string userID))
                //    UserID = userID;
                //if (UserData.TryGetValue("Description", out string Description))
                //    UserDescription = Description;
                //if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"Resource\UserBackground.png"))
                //    UserBackground = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Resource\UserBackground.png"));
                #endregion

                #region 载入生成器按钮
                // mcdoc 索引就绪后重算一次：版本分类要用到各入口 dispatch 的 since/until
                RebuildGeneratorButtons();
                #endregion
            });
            #endregion

            ReadDataSource();
        }

        /// <summary>
        /// 关窗时把本进程内改过的设置写回 Configuration.json 的 Environment 段。
        /// </summary>
        [RelayCommand]
        private void MainWindowClosing()
        {
            settingsService.Save(settingsService.Current);
            // 语言服务器进程不在这里关：McdocLanguageServerHost 注册了 ProcessExit/DomainUnload，
            // 只回收"本进程起的那个实例"，用户手工起的不会被误杀。
        }

        [RelayCommand]
        private void MainWindowActivated(object sender)
        {
            var window = sender as VectorWindow;
            config = settingsService.Current;
            // 配置里可能是空串或旧版本留下的未知名字：解析不出来就保留窗口当前外观。
            // 直接把 null 交给 Enum.Parse 会抛 ArgumentNullException，窗口在第一帧就没了。
            if (Enum.TryParse(config.ThemeType, out WindowThemeType themeType))
            {
                // 赋值会同步触发 OnThemeType_Changed → ApplyNewTheme，CustomTheme.xaml 此时已合并，
                // 所以紧接着把设置里存的自定义配色写进去即可
                window.ThemeType = themeType;
                if (themeType is WindowThemeType.Custom)
                {
                    ThemeManager.TryApplyCustomColors(config.CustomForeground, config.CustomBackground);
                }
            }
            if (Enum.TryParse(config.VisualType, out WindowVisualType visualType))
            {
                window.VisualType = visualType;
            }
            if (Enum.TryParse(config.CornerPreferenceType, out WindowCornerPreference cornerPreference))
            {
                window.CornerPreference = cornerPreference;
            }

            // 窗体背景色跟主题无关（它定义在一直合并着的 Visual.xaml 里），所以不看 ThemeType，直接还原
            if (ThemeManager.TryParseColor(config.CustomWindowBackground, out Color windowBackground))
            {
                ThemeManager.ApplyCustomWindowBackground(windowBackground);
            }

            // 主题字典一旦被重建（子窗体激活会抄主窗体的 ThemeType 并触发 ApplyNewTheme），
            // 写进 CustomTheme.xaml 的自定义配色就没了；这里跟着补回去。
            ThemeManager.ThemeChanged -= RestoreCustomColors;
            ThemeManager.ThemeChanged += RestoreCustomColors;
        }

        public void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        /// <summary>
        /// 主题字典被重建后，把设置里的自定义前景/背景重新写进新的 CustomTheme.xaml。
        /// 当前不是 Custom 主题时 ApplyCustomColors 自己会返回 false，不用额外判断。
        /// </summary>
        private void RestoreCustomColors()
        {
            if (config is null || !string.Equals(config.ThemeType, nameof(WindowThemeType.Custom), StringComparison.Ordinal))
            {
                return;
            }

            ThemeManager.TryApplyCustomColors(config.CustomForeground, config.CustomBackground);
        }

        [RelayCommand]
        private void GeneratorTableLoaded(object sender)
        {
            GeneratorTable = sender as Grid;
            if (GeneratorTable is null)
            {
                return;
            }
            RebuildGeneratorButtons();
            TryFinishSkeleton();
        }

        [RelayCommand]
        private void SkeletonGridLoaded(object sender)
        {
            SkeletonGrid = sender as Grid;
            TryFinishSkeleton();

            // Loaded 早于主题应用（MainWindowActivated 要等 Activated），所以不能只算一次：
            // 主题换了必须重算，否则骨架屏会一直停在 App.xaml 里默认那套颜色上。
            ThemeManager.ThemeChanged -= RefreshSkeletonColors;
            ThemeManager.ThemeChanged += RefreshSkeletonColors;
            RefreshSkeletonColors();
        }

        [RelayCommand]
        private void SearchGenerator()
        {
            if (string.IsNullOrWhiteSpace(SearchString))
            {
                return;
            }
        }

        [RelayCommand]
        private void SwitchCategoryList()
        {
            categoryKindIndex = (categoryKindIndex + 1) % categoryStrategies.Count;
            ShowCategoryButtons();
        }

        [RelayCommand]
        private void SwitchSortList()
        {
            sortKindIndex = (sortKindIndex + 1) % sortStrategies.Count;
            ShowSortMode();
        }

        [RelayCommand]
        private void SwitchCategory(CategoryButtonModel model)
        {
            if (model?.ID is null)
            {
                return;
            }

            // 再点一次当前项等于取消过滤，省掉一个额外的“全部”状态。
            selectedCategoryId = string.Equals(model.ID, AllCategoryId, StringComparison.Ordinal)
                || string.Equals(selectedCategoryId, model.ID, StringComparison.Ordinal)
                ? null
                : model.ID;
            SyncCategoryButtonSelection();
            RebuildGeneratorButtons();
        }
        #endregion
    }
}