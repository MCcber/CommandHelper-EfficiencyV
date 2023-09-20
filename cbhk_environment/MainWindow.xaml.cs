using cbhk_environment.Distributor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using cbhk_environment.More;
using cbhk_environment.GeneralTools;
using System.Data;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace cbhk_environment
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// 主页可见性
        /// </summary>
        public static MainWindowProperties.Visibility cbhk_visibility = MainWindowProperties.Visibility.MinState;

        public TaskbarIcon taskbarButton = null;

        //用户数据
        Dictionary<string, string> UserData = new();

        public MainWindow(Dictionary<string,string> userInfo,TaskbarIcon taskbarButton)
        {
            InitializeComponent();
            UserData = userInfo;
            #region 初始化托盘
            this.taskbarButton = taskbarButton;
            this.taskbarButton.DataContext = new resources.MainFormDataContext.NotifyIconViewModel(this);
            #endregion
        }

        /// <summary>
        /// 载入窗体内容
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(async () =>
            {
                await ReadDataSource();
                await InitUIData();
            }).ContinueWith(StopSkeletonScreen);
        }

        /// <summary>
        /// 骨架屏持续时间
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopSkeletonScreen(Task task)
        {
            Dispatcher.Invoke(() =>
            {
                SkeletonGrid.Visibility = Visibility.Collapsed;
                GeneratorTable.Visibility = Visibility.Visible;
                if (MainWindowProperties.ShowNotice)
                {
                    NoticeToUsers noticeToUsers = new();
                    if (noticeToUsers.ShowDialog().Value)
                        MainWindowProperties.ShowNotice = !noticeToUsers.donotShowNextTime.IsChecked.Value;
                }
            });
        }

        /// <summary>
        /// 读取启动器配置
        /// </summary>
        private async Task ReadDataSource()
        {
            //case "AutoStart":
            //    {
            //        MainWindowProperties.AutoStart = bool.Parse(data[1]);
            //        if (MainWindowProperties.AutoStart)
            //            GeneralTools.AutoStart.CreateShortcut("C:\\Users\\" + Environment.UserName + "\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs\\Startup", "命令管家", AppDomain.CurrentDomain.BaseDirectory + "cbhk.exe", "命令管家1.19", AppDomain.CurrentDomain.BaseDirectory + "cb.ico");
            //        else
            //            File.Delete("C:\\Users\\"+Environment.UserName+"\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs\\Startup\\命令管家");
            //        break;
            //    }

            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Minecraft.db"))
            {
                DataCommunicator dataCommunicator = DataCommunicator.GetDataCommunicator();
                DataTable dataTable = await dataCommunicator.GetData("SELECT * FROM EnvironmentConfigs");
                cbhk_visibility = dataTable.Rows[0]["Visibility"].ToString() switch
                {
                    "KeepState" => MainWindowProperties.Visibility.KeepState,
                    "MinState" => MainWindowProperties.Visibility.MinState,
                    "Close" => MainWindowProperties.Visibility.Close,
                    _ => throw new NotImplementedException()
                };
                if (dataTable.Rows[0]["ShowNotice"] is decimal showNotice)
                    MainWindowProperties.ShowNotice = showNotice == 1;
                if (dataTable.Rows[0]["CloseToTray"] is decimal closeToTray)
                    MainWindowProperties.CloseToTray = closeToTray == 1;
                if (dataTable.Rows[0]["LinkAnimationDelay"] is decimal linkAnimationDelay)
                    MainWindowProperties.LinkAnimationDelay = (int)linkAnimationDelay;
            }
        }

        /// <summary>
        /// 初始化界面数据
        /// </summary>
        private async Task InitUIData()
        {
            #region 加载用户数据
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\userHead.png"))
            {
                Dispatcher.Invoke(() =>
                {
                    userHead.Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\userHead.png", UriKind.Absolute));
                    userHead.MouseLeftButtonUp += (a, b) => { if (UserData.TryGetValue("UserID", out string value)) System.Diagnostics.Process.Start("explorer.exe", "https://mc.metamo.cn/u/" + value); };
                });
            }
            #endregion

            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Minecraft.db"))
            {
                DataCommunicator dataCommunicator = DataCommunicator.GetDataCommunicator();
                #region 初始化生成器按钮
                await Dispatcher.Invoke(async () =>
                {
                    DataTable generatorTable = await dataCommunicator.GetData("SELECT * FROM Generators");
                    GeneratorFunction generatorFunction = new(this);
                    string baseImagePath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
                    int rowIndex = 0;
                    int columnIndex = 0;
                    GeneratorTable.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(80, GridUnitType.Pixel) });
                    GeneratorTable.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(200, GridUnitType.Pixel) });
                    GeneratorTable.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(200, GridUnitType.Pixel) });
                    GeneratorTable.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(200, GridUnitType.Pixel) });
                    GeneratorTable.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(200, GridUnitType.Pixel) });
                    foreach (DataRow row in generatorTable.Rows)
                    {
                        Button button = new()
                        {
                            Style = Application.Current.Resources["StrokeButton"] as Style
                        };
                        string currentId = row["id"].ToString();
                        string imagePath = baseImagePath + currentId + ".png";
                        if (File.Exists(imagePath))
                            button.Background = new ImageBrush(new BitmapImage(new Uri(imagePath, UriKind.Absolute)));
                        RelayCommand behavior = SetGeneratorClickEvent(currentId, generatorFunction);
                        button.Command = behavior;
                        GeneratorTable.Children.Add(button);
                        if (columnIndex > GeneratorTable.ColumnDefinitions.Count - 1)
                        {
                            GeneratorTable.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(80, GridUnitType.Pixel) });
                            columnIndex = 0;
                            rowIndex++;
                        }
                        Grid.SetColumn(button, columnIndex);
                        Grid.SetRow(button, rowIndex);
                        columnIndex++;
                    }
                });
                #endregion
            }
        }

        /// <summary>
        /// 为生成器按钮分配方法
        /// </summary>
        /// <param name="id"></param>
        /// <param name="function"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private RelayCommand SetGeneratorClickEvent(string id, GeneratorFunction function)
        {
            RelayCommand result = id switch
            {
                "ooc" => function.StartOoc,
                "datapack" => function.StartDatapack,
                "armorstand" => function.StartArmorStand,
                "writtenbook" => function.StartWrittenBook,
                "spawners" => function.StartSpawner,
                "recipes" => function.StartRecipes,
                "villagers" => function.StartVillagers,
                "tags" => function.StartTags,
                "items" => function.StartItems,
                "fireworks" => function.StartFireworks,
                "entities" => function.StartEntities,
                "signs" => function.StartSign,
                _ => null
            };
            return result;
        }

        /// <summary>
        /// 主窗体关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = MainWindowProperties.CloseToTray;
            if(e.Cancel)
            {
                ShowInTaskbar = false;
                WindowState = WindowState.Minimized;
            }
            else
            {
                taskbarButton.Visibility = Visibility.Collapsed;
                Environment.Exit(0);
            }
        }
    }

    /// <summary>
    /// 管家的启动属性
    /// </summary>
    public static class MainWindowProperties
    {
        /// <summary>
        /// 开机自启
        /// </summary>
        //public static bool AutoStart { get; set; } = false;

        /// <summary>
        /// 关闭后缩小到托盘
        /// </summary>
        public static bool CloseToTray { get; set; } = true;

        /// <summary>
        /// 轮播图播放延迟
        /// </summary>
        public static int LinkAnimationDelay { get; set; } = 3;

        public static bool ShowNotice { get; set; } = true;

        /// <summary>
        /// 主页可见性
        /// </summary>
        public enum Visibility
        {
            KeepState,
            MinState,
            Close
        }
    }
}
