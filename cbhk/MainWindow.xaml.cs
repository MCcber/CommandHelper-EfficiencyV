using cbhk.Distributor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using cbhk.GeneralTools;
using System.Data;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using cbhk.CustomControls;

namespace cbhk
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// 主页可见性
        /// </summary>
        public static MainWindowProperties.Visibility cbhkVisibility = MainWindowProperties.Visibility.MinState;

        /// <summary>
        /// 用户数据
        /// </summary>
        Dictionary<string, string> UserData = [];

        public MainWindow(Dictionary<string, string> userInfo)
        {
            InitializeComponent();
            UserData = userInfo;
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
        private static async Task ReadDataSource()
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
                cbhkVisibility = dataTable.Rows[0]["Visibility"].ToString() switch
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
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "resources\\userHead.png"))
            {
                Dispatcher.Invoke(() =>
                {
                    userHead.Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "resources\\userHead.png", UriKind.Absolute));
                    if (UserData.TryGetValue("UserID", out string value))
                        userHead.MouseLeftButtonUp += (a, b) => { System.Diagnostics.Process.Start("explorer.exe", "https://mc.metamo.cn/u/" + value); };
                });
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (UserData.TryGetValue("UserID", out string UserID))
                    userId.Text = UserID;
                if (UserData.TryGetValue("description", out string description))
                    userDescription.Text = description;
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "resources\\userBackground.png"))
                    userBackground.Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "resources\\userBackground.png"));
                if (userId.Text.Length == 0)
                    UserGrid.Background = Resources["BackgroundBrush"] as Brush;
            });
            #endregion
            #region 载入生成器按钮
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Minecraft.db"))
            {
                DataCommunicator dataCommunicator = DataCommunicator.GetDataCommunicator();
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
                        GeneratorButtons button = new()
                        {
                            Style = Application.Current.Resources["GeneratorButtons"] as Style,
                            BorderThickness = new Thickness(0)
                        };
                        string currentId = row["id"].ToString();
                        currentId = currentId[0].ToString().ToUpper() + currentId[1..];
                        string currentName = row["zh"].ToString();
                        string imagePath = baseImagePath + currentId + ".png";
                        BitmapImage bitmapImage = new(new Uri("pack://application:,,,/cbhk;component/resources/cbhk/images/GeneratorButtonBackground.png",UriKind.RelativeOrAbsolute));
                        if (bitmapImage is not null)
                            button.Background = new ImageBrush(bitmapImage);
                        if (File.Exists(imagePath))
                            button.Icon = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
                        if (currentId is not null)
                            button.Title = currentName;
                        if (currentName is not null)
                            button.SubTitle = currentId;
                        IRelayCommand behavior = GeneratorClickEvent.Set(currentId, generatorFunction);
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
            }
            #endregion
        }

        /// <summary>
        /// 主窗体关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = MainWindowProperties.CloseToTray;
            if (e.Cancel)
            {
                ShowInTaskbar = false;
                WindowState = WindowState.Minimized;
            }
            else
            {
                cbhkTaskbar.Visibility = Visibility.Collapsed;
                WindowState = WindowState.Minimized;
                SystemCommands.CloseWindow(this);
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// 显示管家
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ShowWindowCommand(object sender, RoutedEventArgs e)
        {
            ShowInTaskbar = true;
            WindowState = WindowState.Normal;
            Show();
            Focus();
        }

        /// <summary>
        /// 关闭管家
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ExitApplicationCommand(object sender, RoutedEventArgs e)
        {
            cbhkTaskbar.Visibility = Visibility.Collapsed;
            Environment.Exit(0);
        }

        private void cbhkTaskbar_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowWindowCommand(null, null);
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
        public static bool CloseToTray { get; set; } = false;

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