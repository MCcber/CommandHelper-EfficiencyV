using CBHK.Utility;
using System;
using System.Threading.Tasks;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using System.Windows.Controls;
using CBHK.CustomControl;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using Prism.Ioc;
using CBHK.View.Common;
using CBHK.ViewModel.Common;
using System.Linq;
using CBHK.Domain;
using CBHK.Domain.Model;

namespace CBHK.ViewModel
{
    public partial class MainViewModel(IContainerProvider container,CBHKDataContext context) : ObservableObject
    {
        #region Field
        private IContainerProvider _container = container;
        private readonly CBHKDataContext _context = context;
        /// <summary>
        /// 主页可见性
        /// </summary>
        public EnvironmentConfig _config = null;
        private Grid SkeletonGrid = null;
        private Grid GeneratorTable = null;
        private IProgress<byte> SetGeneratorButtonProgress = null;
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
        private bool _showInTaskBar = true;
        #endregion

        #region Event
        /// <summary>
        /// 载入窗体内容
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SetGeneratorButtonProgress = new Progress<byte>((state) =>
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

                foreach (var data in _context.GeneratorSet)
                {
                    GeneratorButtons button = new()
                    {
                        Style = Application.Current.Resources["GeneratorButtons"] as Style,
                        BorderThickness = new Thickness(0)
                    };
                    string currentId = data.ID;
                    currentId = currentId[0].ToString().ToUpper() + currentId[1..];
                    string currentName = data.ZH;
                    string imagePath = baseImagePath + currentId + ".png";
                    BitmapImage bitmapImage = new(new Uri("pack://application:,,,/CBHK;component/Resource/CBHK/Image/GeneratorButtonBackground.png", UriKind.RelativeOrAbsolute));
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

            InitUIDataProgress = new Progress<byte>((number) =>
            {
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
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Minecraft.db"))
                {
                    SetGeneratorButtonProgress.Report(0);
                }
                #endregion

                StopSkeletonScreen(Task.CompletedTask);
            });

            _config = _context.EnvironmentConfigSet.FirstOrDefault();
            ReadDataSource();
        }

        public void GeneratorTable_Loaded(object sender, RoutedEventArgs e) => GeneratorTable = sender as Grid;

        public void SkeletonGrid_Loaded(object sender, RoutedEventArgs e) => SkeletonGrid = sender as Grid;

        public void TaskBarIcon_Loaded(object sender, RoutedEventArgs e)
        {
            TaskBarIcon = sender as TaskbarIcon;
            TaskBarIcon.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 主窗体关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = bool.Parse(_config.CloseToTray);
            await _context.SaveChangesAsync();
            if (e.Cancel)
            {
                WindowState = WindowState.Minimized;
                ShowInTaskBar = false;
            }
            else
            {
                WindowState = WindowState.Minimized;
                TaskBarIcon.Dispose();
                Environment.Exit(0);
            }
        }

        [RelayCommand]
        /// <summary>
        /// 显示管家
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowMainWindow(object sender)
        {
            Window window = sender as Window;
            window.ShowInTaskbar = true;
            window.WindowState = WindowState.Normal;
            window.Show();
            window.Focus();
        }

        /// <summary>
        /// 关闭管家
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [RelayCommand]
        public void ExitApplication()
        {
            TaskBarIcon.Dispose();
            Environment.Exit(0);
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
            SkeletonGrid.Visibility = Visibility.Collapsed;
            GeneratorTable.Visibility = Visibility.Visible;
            if (bool.TryParse(_config.ShowNotice,out bool showNotice) && showNotice)
            {
                NoticeToUsersView noticeToUsers = _container.Resolve<NoticeToUsersView>();
                noticeToUsers.Topmost = true;
                NoticeToUsersViewModel notichViewModel = noticeToUsers.DataContext as NoticeToUsersViewModel;
                if (noticeToUsers.ShowDialog().Value)
                {
                    _config.ShowNotice = (!notichViewModel.DonotShowNextTime).ToString();
                }
            }
        }

        /// <summary>
        /// 读取启动器配置
        /// </summary>
        private void ReadDataSource()
        {
            _config = _context.EnvironmentConfigSet.FirstOrDefault();

            InitUIDataProgress.Report(0);
        }
        #endregion
    }
}