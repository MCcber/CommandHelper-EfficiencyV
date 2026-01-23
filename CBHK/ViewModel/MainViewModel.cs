using CBHK.Common.Model;
using CBHK.CustomControl.Container;
using CBHK.CustomControl.VectorButton;
using CBHK.Domain;
using CBHK.Domain.Model.Database;
using CBHK.Utility;
using CBHK.View.Common;
using CBHK.ViewModel.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        private bool isContextMenuCloseCommand = false;
        /// <summary>
        /// 主页可见性
        /// </summary>
        public EnvironmentConfig config = null;
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
        [RelayCommand]
        private void MainWindowLoaded()
        {
            for (int i = 0; i < 1000; i++)
            {
                ComboBoxItemList.Add(new CustomControl.VectorComboBox.VectorIconTextComboBoxItem()
                {
                    Text = "Item" + i,
                    DisplayPanelBrush = Brushes.Black,
                    MemberBrush = Brushes.White,
                    Image = new BitmapImage(new Uri("pack://application:,,,/CBHK;component/Resource/CBHK/Image/Generator/armorstand.png", UriKind.Absolute))
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
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E2E2E")),
                        Style = Application.Current.Resources["GeneratorVectorButtonStyle"] as Style
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
                    IRelayCommand behavior = generatorFunction.GetGeneratorClickCommand(currentId);
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

            ReadDataSource();
        }

        [RelayCommand]
        private void MainWindowClosing() => context.SaveChanges();

        [RelayCommand]
        private void MainWindowActivated(object sender)
        {
            var window = sender as VectorWindow;
            config = context.EnvironmentConfigSet.FirstOrDefault();
            window.ThemeType = (WindowThemeType)Enum.Parse(typeof(WindowThemeType), config.ThemeType);
            window.VisualType = (WindowVisualType)Enum.Parse(typeof(WindowVisualType), config.VisualType);
            window.CornerPreference = (WindowCornerPreference)Enum.Parse(typeof(WindowCornerPreference), config.CornerPreferenceType);
        }

        [RelayCommand]
        private void GeneratorTableLoaded(object sender) => GeneratorTable = sender as Grid;

        [RelayCommand]
        private void SkeletonGridLoaded(object sender) => SkeletonGrid = sender as Grid;
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
            if (bool.TryParse(config.ShowNotice,out bool showNotice) && showNotice)
            {
                NoticeToUsersView noticeToUsers = container.Resolve<NoticeToUsersView>();
                noticeToUsers.Topmost = true;
                NoticeToUsersViewModel notichViewModel = noticeToUsers.DataContext as NoticeToUsersViewModel;
                if (noticeToUsers.ShowDialog().Value)
                {
                    config.ShowNotice = (!notichViewModel.DonotShowNextTime).ToString();
                }
            }
        }

        /// <summary>
        /// 读取启动器配置
        /// </summary>
        private void ReadDataSource() => InitUIDataProgress.Report(0);
        #endregion
    }
}