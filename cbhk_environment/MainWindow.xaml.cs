using cbhk_environment.Distributor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using cbhk_environment.SettingForm;
using Point = System.Windows.Point;
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
    public partial class MainWindow:Window
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
            #region 初始化托盘
            this.taskbarButton = taskbarButton;
            this.taskbarButton.DataContext = new resources.MainFormDataContext.NotifyIconViewModel(this);
            #endregion
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
                    userFrameBorder.MouseLeftButtonUp += (a, b) => { if (UserData.TryGetValue("UserID", out string value)) System.Diagnostics.Process.Start("explorer.exe", "https://mc.metamo.cn/u/" + value); };
                });
            }
            #endregion

            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Minecraft.db"))
            {
                DataCommunicator dataCommunicator = DataCommunicator.GetDataCommunicator();

                #region 加载轮播图数据
                await Dispatcher.Invoke(async () =>
                {
                    DataTable chartTable = await dataCommunicator.GetData("SELECT * FROM RotationChart");
                    foreach (DataRow row in chartTable.Rows)
                    {
                        string id = row["id"].ToString();
                        string url = row["url"].ToString();
                        string description = row["description"].ToString();
                        if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + id + ".png"))
                            rotationChartBody.Append(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + id + ".png", url, description);
                    }
                });
                #endregion
                #region 初始化生成器按钮
                await Dispatcher.Invoke(async () =>
                {
                    DataTable generatorTable = await dataCommunicator.GetData("SELECT * FROM Generators");
                    GeneratorFunction generatorFunction = new(this);
                    string baseImagePath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
                    int rowIndex = 0;
                    int columnIndex = 0;
                    GeneratorTable.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(85, GridUnitType.Pixel) });
                    GeneratorTable.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(225, GridUnitType.Pixel) });
                    GeneratorTable.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(225, GridUnitType.Pixel) });
                    GeneratorTable.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(225, GridUnitType.Pixel) });
                    GeneratorTable.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(225, GridUnitType.Pixel) });
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
                            GeneratorTable.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(85, GridUnitType.Pixel) });
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
                _ => null
            };
            return result;
        }

        /// <summary>
        /// 个性化设置
        /// </summary>
        private void IndividualizationForm(object sender, EventArgs e)
        {
            IndividualizationForm indivi_form = new();
            if(indivi_form.ShowDialog() == true)
            {

            }
        }

        /// <summary>
        /// 启动项设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StatupItemClick(object sender, RoutedEventArgs e)
        {
            StartupItemForm sif = new();

            #region 把当前的启动数据传递给启动项设置窗体
            //sif.AutoStartup.IsChecked = MainWindowProperties.AutoStart;

            sif.CloseToTray.IsChecked = MainWindowProperties.CloseToTray;
            sif.StateComboBox.SelectedIndex = 0;
            if (cbhk_visibility == MainWindowProperties.Visibility.MinState)
                sif.StateComboBox.SelectedIndex = 1;
            else
                if (cbhk_visibility == MainWindowProperties.Visibility.Close)
                sif.StateComboBox.SelectedIndex = 2;
            #endregion

            if (sif.ShowDialog() == true)
            {
                //主页可见性
                cbhk_visibility = sif.StateComboBox.SelectedIndex == 0? MainWindowProperties.Visibility.KeepState:(sif.StateComboBox.SelectedIndex == 1?MainWindowProperties.Visibility.MinState:MainWindowProperties.Visibility.Close);
                //是否开机自启
                //MainWindowProperties.AutoStart = sif.AutoStartup.IsChecked.Value;
                //是否关闭后缩小到托盘
                MainWindowProperties.CloseToTray = sif.CloseToTray.IsChecked.Value;
                //轮播图播放速度
            }
        }

        #region 窗体行为
        /// <summary>
        /// 由于是主窗体，所以退出应用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            SaveConfigs();
            taskbarButton.Visibility = MainWindowProperties.CloseToTray ? Visibility.Visible : Visibility.Collapsed;
            ShowInTaskbar = cbhk_visibility != MainWindowProperties.Visibility.MinState;
            WindowState = WindowState.Minimized;

            if (!MainWindowProperties.CloseToTray)
            {
                taskbarButton.Visibility = Visibility.Collapsed;
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// 保存启动器配置
        /// </summary>
        private async void SaveConfigs()
        {
            //保存的配置
            //if (MainWindowProperties.AutoStart)
            //    GeneralTools.AutoStart.CreateShortcut("C:\\Users\\" + Environment.UserName + "\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs\\Startup", "命令管家", AppDomain.CurrentDomain.BaseDirectory + "cbhk.exe", "命令管家1.19", AppDomain.CurrentDomain.BaseDirectory + "cb.ico");
            //else
            //    File.Delete("C:\\Users\\" + Environment.UserName + "\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs\\Startup\\命令管家.lnk");

            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Minecraft.db"))
            {
                DataCommunicator dataCommunicator = DataCommunicator.GetDataCommunicator();
                await dataCommunicator.ExecuteNonQuery("UPDATE EnvironmentConfigs SET Visibility = '"+ cbhk_visibility.ToString() + 
                    "',ShowNotice='"+(MainWindowProperties.ShowNotice?1:0) + 
                    "',LinkAnimationDelay = '"+MainWindowProperties.LinkAnimationDelay+ 
                    "',CloseToTray='"+(MainWindowProperties.CloseToTray?1:0) +"'");
            }
        }

        /// <summary>
        /// 最小化窗体
        /// </summary>
        private void MinFormSize(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 鼠标拖拽窗体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Point title_range = e.GetPosition(TitleStack);
            if (title_range.X >= 0 && title_range.X < TitleStack.ActualWidth && title_range.Y >= 0 && title_range.Y < TitleStack.ActualHeight && e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        /// <summary>
        /// 最大化窗体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_StateChanged(object sender, EventArgs e)
        {
            switch (WindowState)
            {
                case WindowState.Maximized:
                    WindowState = WindowState.Normal;
                    MaxWidth = SystemParameters.WorkArea.Width + 16;
                    MaxHeight = SystemParameters.WorkArea.Height + 16;
                    //BorderThickness = new Thickness(5); //最大化后需要调整
                    //Margin = new Thickness(0);
                    break;
                case WindowState.Normal:
                    WindowState = WindowState.Maximized;
                    TitleStack.Margin = new Thickness(100, 0, 0, 0);
                    //BorderThickness = new Thickness(5);
                    //Margin = new Thickness(10);
                    break;
            }

            //switch (WindowState)
            //{
            //    case WindowState.Maximized:
            //        //MaxWidth = SystemParameters.WorkArea.Width + 16;
            //        //MaxHeight = SystemParameters.WorkArea.Height + 16;
            //        //BorderThickness = new Thickness(5); //最大化后需要调整
            //        Left = Top = 0;
            //        MaxHeight = SystemParameters.WorkArea.Height;
            //        MaxWidth = SystemParameters.WorkArea.Width;
            //        break;
            //    case WindowState.Normal:
            //        BorderThickness = new Thickness(0);
            //        break;
            //}
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StackPanel this_Panel = null;
            if (Equals(typeof(StackPanel), e.Source.GetType()))
                this_Panel = e.Source as StackPanel;
            else
                return;
            if (e.ClickCount == 2 && this_Panel.Name == "TitleStack")
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
        }
        #endregion

        /// <summary>
        /// 切换标签页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabControl tabControl = sender as TabControl;
            rotationChartBody.StopTimer.IsEnabled = rotationChartBody.SwitchTimer.IsEnabled = tabControl.SelectedIndex == 1;
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
