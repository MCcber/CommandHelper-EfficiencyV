using CBHK.CustomControl.Container;
using CBHK.CustomControl.VectorButton;
using CBHK.Domain;
using CBHK.Domain.DataContext;
using CBHK.Domain.Model;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using CBHK.Utility;
using CBHK.Utility.Visual;
using CBHK.Utility.Visual.MessageTip;
using CBHK.View.Common;
using CBHK.ViewModel.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Ioc;
using System;
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
        private IProgress<byte> SetGeneratorButtonProgress = null;
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
        #endregion

        #region Method
        public MainViewModel(IContainerProvider Container, Resource Resource, EnvironmentSettingsService SettingsService, RegistryDataService DataRegistry)
        {
            container = Container;
            resource = Resource;
            settingsService = SettingsService;
            dataRegistry = DataRegistry;
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

            #region 设置生成器按钮面板
            SetGeneratorButtonProgress = new Progress<byte>((state) =>
            {
                DistributorGenerator generatorFunction = container.Resolve<DistributorGenerator>();
                string baseImagePath = "pack://application:,,,/CBHK;component/Resource/CBHK/Image/Generator/";
                int rowIndex = 0;
                int columnIndex = 0;

                InnerGeneratorTable.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(80, GridUnitType.Pixel) });
                InnerGeneratorTable.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                InnerGeneratorTable.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                InnerGeneratorTable.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                InnerGeneratorTable.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

                foreach (GeneratorEntry entry in GeneratorCatalog.Entries)
                {
                    GeneratorVectorButton button = new()
                    {
                        Style = Application.Current.Resources["GeneratorVectorButtonStyle"] as Style
                    };
                    // 清单钉在 GeneratorCatalog：Id 同时是命令表的键和图标名（大小写不敏感）
                    string currentId = entry.Id;
                    string currentName = entry.Name;
                    string imagePath = baseImagePath + currentId + ".png";
                    Uri uri = new(imagePath, UriKind.Absolute);
                    try
                    {
                        button.Icon = new BitmapImage(uri);
                    }
                    catch {}
                    if (currentId is not null)
                    {
                        button.Title = currentName;
                    }
                    if (currentName is not null)
                    {
                        button.SubTitle = currentId;
                    }
                    IRelayCommand behavior = generatorFunction.GetGeneratorClickCommand(currentId);
                    button.Command = behavior;
                    InnerGeneratorTable.Children.Add(button);
                    if (columnIndex > InnerGeneratorTable.ColumnDefinitions.Count - 1)
                    {
                        InnerGeneratorTable.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(80, GridUnitType.Pixel) });
                        columnIndex = 0;
                        rowIndex++;
                    }
                    Grid.SetColumn(button, columnIndex);
                    Grid.SetRow(button, rowIndex);
                    columnIndex++;
                }
            });
            #endregion

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
        if (dataRegistry.IsLoaded)
        {
            SetGeneratorButtonProgress.Report(0);
        }
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
            //将内部
            if (!GeneratorTable.Children.Contains(InnerGeneratorTable))
            {
                GeneratorTable.Children.Add(InnerGeneratorTable);
            }
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
        #endregion
    }
}