using cbhk.GeneralTools;
using cbhk.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using System.Windows.Controls;
using cbhk.CustomControls;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using Prism.Ioc;
using cbhk.View.Common;
using cbhk.ViewModel.Common;
using cbhk.View;

namespace cbhk.ViewModel
{
    public partial class MainViewModel(IContainerProvider container) : ObservableObject
    {
        #region Field
        /// <summary>
        /// 主页可见性
        /// </summary>
        public MainWindowProperties.Visibility MainViewVisibility = MainWindowProperties.Visibility.MinState;
        /// <summary>
        /// 用户数据
        /// </summary>
        public Dictionary<string, string> UserData = [];
        private Grid SkeletonGrid = null;
        private Grid GeneratorTable = null;
        private Grid UserGrid = null;
        private IProgress<DataTable> SetGeneratorButtonHandler = null;
        DataCommunicator dataCommunicator = DataCommunicator.GetDataCommunicator();
        /// <summary>
        /// 初始化界面数据
        /// </summary>
        private IProgress<byte> InitUIDataProgress = null;
        #endregion

        #region Property
        [ObservableProperty]
        public TaskbarIcon _taskBarIcon = null;
        [ObservableProperty]
        public WindowState _windowState = WindowState.Normal;

        [ObservableProperty]
        public BitmapImage _userHead = null;

        [ObservableProperty]
        public string _userID = "";

        [ObservableProperty]
        public string _userDescription = "";

        [ObservableProperty]
        public BitmapImage _userBackground = null;
        private IContainerProvider _container = container;

        #endregion

        #region Event
        /// <summary>
        /// 载入窗体内容
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SetGeneratorButtonHandler = new Progress<DataTable>((dataTable) =>
            {
                DistributorGenerator generatorFunction = _container.Resolve<DistributorGenerator>();
                string baseImagePath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
                int rowIndex = 0;
                int columnIndex = 0;

                GeneratorTable.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(80, GridUnitType.Pixel) });
                GeneratorTable.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                GeneratorTable.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                GeneratorTable.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                GeneratorTable.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

                foreach (DataRow row in dataTable.Rows)
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
                    BitmapImage bitmapImage = new(new Uri("pack://application:,,,/cbhk;component/Resource/CBHK/Image/GeneratorButtonBackground.png", UriKind.RelativeOrAbsolute));
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

            InitUIDataProgress = new Progress<byte>(async (number) =>
            {
                #region 加载用户数据
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"Resource\UserHead.png"))
                {
                    UserHead = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Resource\UserHead.png", UriKind.RelativeOrAbsolute));
                }
                if (UserData.TryGetValue("UserID", out string userID))
                    UserID = userID;
                if (UserData.TryGetValue("description", out string description))
                    UserDescription = description;
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"Resource\UserBackground.png"))
                    UserBackground = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Resource\UserBackground.png"));
                #endregion
                #region 载入生成器按钮
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Minecraft.db"))
                {
                    DataTable generatorTable = await dataCommunicator.GetData("SELECT * FROM Generators");
                    SetGeneratorButtonHandler.Report(generatorTable);
                }
                #endregion

                StopSkeletonScreen(Task.CompletedTask);
            });

            await ReadDataSource();
        }

        public void GeneratorTable_Loaded(object sender, RoutedEventArgs e) => GeneratorTable = sender as Grid;

        public void SkeletonGrid_Loaded(object sender, RoutedEventArgs e) => SkeletonGrid = sender as Grid;

        public void UserGrid_Loaded(object sender, RoutedEventArgs e)
        {
            UserGrid = sender as Grid;
            Brush BackgroundBrush = _container.Resolve<MainView>().FindResource("BackgroundBrush") as Brush;
            if (UserID.Length == 0 && BackgroundBrush is not null)
                UserGrid.Background = BackgroundBrush;
        }

        public void UserHead_Loaded(object sender, RoutedEventArgs e) => UserHead = sender as BitmapImage;

        public void UserBackground_Loaded(object sender, RoutedEventArgs e) => UserBackground = sender as BitmapImage;

        public void TaskBarIcon_Loaded(object sender, RoutedEventArgs e)
        {
            TaskBarIcon = sender as TaskbarIcon;
            TaskBarIcon.Visibility = Visibility.Visible;
        }

        [RelayCommand]
        private void UserHeadClick()
        {
            if (UserData.TryGetValue("UserID", out string value))
                System.Diagnostics.Process.Start("explorer.exe", "https://mc.metamo.cn/u/" + value);
        }

        /// <summary>
        /// 主窗体关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = MainWindowProperties.CloseToTray;
            if (e.Cancel)
            {
                //ShowInTaskbar = false;
                WindowState = WindowState.Minimized;
            }
            else
            {
                WindowState = WindowState.Minimized;
                //SystemCommands.CloseWindow(this);
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// 显示管家
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ShowWindow(object sender, RoutedEventArgs e)
        {
            //ShowInTaskbar = true;
            //WindowState = WindowState.Normal;
            //Show();
            //Focus();
        }

        /// <summary>
        /// 关闭管家
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [RelayCommand]
        public void ExitApplication()
        {
            TaskBarIcon.Visibility = Visibility.Collapsed;
            Environment.Exit(0);
        }

        [RelayCommand]
        public void ShowMainView()
        {

        }

        public void TaskbarIcon_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowWindow(null, null);
        }
        #endregion

        #region Method
        /// <summary>
        /// 骨架屏持续时间
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopSkeletonScreen(Task task)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SkeletonGrid.Visibility = Visibility.Collapsed;
                GeneratorTable.Visibility = Visibility.Visible;
                if (MainWindowProperties.ShowNotice)
                {
                    NoticeToUsersView noticeToUsers = _container.Resolve<NoticeToUsersView>();
                    NoticeToUsersViewModel notichViewModel = noticeToUsers.DataContext as NoticeToUsersViewModel;
                    if (noticeToUsers.ShowDialog().Value)
                        MainWindowProperties.ShowNotice = !notichViewModel.DonotShowNextTime;
                }
            });
        }

        /// <summary>
        /// 读取启动器配置
        /// </summary>
        private async Task ReadDataSource()
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Minecraft.db"))
            {
                DataTable dataTable = await dataCommunicator.GetData("SELECT * FROM EnvironmentConfigs");
                if (dataTable.Rows.Count > 0)
                {
                    if (dataTable.Rows[0]["Visibility"] is string Visibility)
                    {
                        MainViewVisibility = Visibility switch
                        {
                            "KeepState" => MainWindowProperties.Visibility.KeepState,
                            "MinState" => MainWindowProperties.Visibility.MinState,
                            "Close" => MainWindowProperties.Visibility.Close,
                            _ => throw new NotImplementedException()
                        };
                    }
                    if (dataTable.Rows[0]["ShowNotice"] is decimal showNotice)
                        MainWindowProperties.ShowNotice = showNotice == 1;
                    if (dataTable.Rows[0]["CloseToTray"] is decimal closeToTray)
                        MainWindowProperties.CloseToTray = closeToTray == 1;
                }
            }

            InitUIDataProgress.Report(0);
        }
        #endregion
    }
}