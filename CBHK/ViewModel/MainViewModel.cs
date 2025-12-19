using CBHK.CustomControl.VectorButton;
using CBHK.Domain;
using CBHK.Domain.Model.Database;
using CBHK.Utility;
using CBHK.View.Common;
using CBHK.ViewModel.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hardcodet.Wpf.TaskbarNotification;
using Prism.Ioc;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CBHK.ViewModel
{
    public partial class MainViewModel(IContainerProvider container,CBHKDataContext context) : ObservableObject
    {
        #region Field
        private IContainerProvider container = container;
        private readonly CBHKDataContext context = context;
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
        [ObservableProperty]
        private ObservableCollection<CustomControl.VectorComboBox.VectorTextComboBoxItem> comboBoxItemList = [];
        #endregion

        #region Event
        /// <summary>
        /// 载入窗体内容
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 100; i++)
            {
                ComboBoxItemList.Add(new CustomControl.VectorComboBox.VectorTextComboBoxItem()
                {
                    Text = "Item" + i,
                    DisplayPanelBrush = Brushes.Black,
                    MemberBrush = Brushes.White
                });
            }
            SetGeneratorButtonProgress = new Progress<byte>((state) =>
            {
                DistributorGenerator generatorFunction = container.Resolve<DistributorGenerator>();
                string baseImagePath = "pack://application:,,,/CBHK;component/Resource/CBHK/Image/Generator/";
                int rowIndex = 0;
                int columnIndex = 0;

                GeneratorTable.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(80, GridUnitType.Pixel) });
                GeneratorTable.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                GeneratorTable.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                GeneratorTable.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                GeneratorTable.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

                foreach (var data in context.GeneratorSet)
                {
                    GeneratorVectorButton button = new()
                    {
                        Style = Application.Current.Resources["GeneratorVectorButton"] as Style
                    };
                    string currentId = data.ID;
                    currentId = currentId[0].ToString().ToUpper() + currentId[1..];
                    string currentName = data.ZH;
                    string imagePath = baseImagePath + currentId + ".png";
                    Uri uri = new(imagePath, UriKind.Absolute);
                    if (Application.GetResourceStream(uri) is not null)
                    {
                        button.Icon = new BitmapImage(uri);
                    }
                    if (currentId is not null)
                    {
                        button.Title = currentName;
                    }
                    if (currentName is not null)
                    {
                        button.SubTitle = currentId;
                    }
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

            _config = context.EnvironmentConfigSet.FirstOrDefault();
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
            await context.SaveChangesAsync();
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
                NoticeToUsersView noticeToUsers = container.Resolve<NoticeToUsersView>();
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
            _config = context.EnvironmentConfigSet.FirstOrDefault();

            InitUIDataProgress.Report(0);
        }
        #endregion
    }
}