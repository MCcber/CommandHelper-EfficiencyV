using CBHK.CustomControl.Container;
using CBHK.CustomControl.VectorButton;
using CBHK.Domain;
using CBHK.Domain.Model.Database;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using CBHK.Utility;
using CBHK.Utility.Visual;
using CBHK.View.Common;
using CBHK.ViewModel.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ICSharpCode.AvalonEdit;
using MinecraftLanguageModelLibrary.Data;
using Prism.Ioc;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CBHK.ViewModel
{
    public partial class MainViewModel(IContainerProvider container, CBHKDataContext context,Resource resource) : ObservableObject
    {
        #region Field
        private readonly IContainerProvider container = container;
        private readonly CBHKDataContext context = context;
        private readonly Resource resource = resource;
        private Color SkeletonLighterColor = new();
        private Color SkeletonDarkerColor = new();
        /// <summary>
        /// 主页可见性
        /// </summary>
        public EnvironmentConfig config = null;
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
        private ObservableCollection<MetaTypeEditorFieldDTO> metaTypeDTOList = [];
        [ObservableProperty]
        public WindowState _windowState = WindowState.Normal;
        [ObservableProperty]
        private bool showInTaskBar = true;
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

                foreach (var data in context.GeneratorSet)
                {
                    GeneratorVectorButton button = new()
                    {
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

            InitUIDataProgress = new Progress<byte>(async (number) =>
            {
                await resource.Init();

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

                StopSkeletonScreen();
            });

            ReadDataSource();
        }

        public void TreeView_Loaded(object sender,RoutedEventArgs e)
        {
            if(sender is VectorTreeView vectorTreeView)
            {
                //treeView = vectorTreeView;
                //treeView.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(OnTreeViewItemExpanded));
                //MCDocumentTreeViewTemplateSelector dataTemplate = Application.Current.Resources["MCDocumentTreeViewTemplateSelector"] as MCDocumentTreeViewTemplateSelector;
            }
        }

        public async void TextEditor_Loaded(object sender,RoutedEventArgs e)
        {
            if (sender is TextEditor editor)
            {
                //textEditor = editor;
                //textEditor.Document.Changed += Document_Changed;
            }
        }

        private void OnTreeViewItemExpanded(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeViewItem item && item.DataContext is MetaTypeEditorFieldDTO dto)
            {

            }
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
        private void GeneratorTableLoaded(object sender)
        {
            GeneratorTable = sender as Grid;
            //将内部
            if (!GeneratorTable.Children.Contains(InnerGeneratorTable))
            {
                GeneratorTable.Children.Add(InnerGeneratorTable);
            }
        }

        [RelayCommand]
        private void SkeletonGridLoaded(object sender)
        {
            SkeletonGrid = sender as Grid;
            if (Application.Current.Resources[Theme.CommonBackground] is SolidColorBrush commonBackgroundBrush)
            {
                Color darken = ColorTool.Darken(commonBackgroundBrush.Color, 0.4f);
                SkeletonLighterColor = ColorTool.Lighten(commonBackgroundBrush.Color, 0.4f);
                SkeletonDarkerColor = darken;
                for (int i = 0; i < SkeletonGrid.Children.Count; i++)
                {
                    if (SkeletonGrid.Children[i] is Rectangle rectangle)
                    {
                        if (SkeletonGrid.Children[i].Uid == "SkeletonBlock")
                        {
                            rectangle.ApplyBreathAnimation(SkeletonLighterColor, SkeletonDarkerColor, new Duration(TimeSpan.FromSeconds(0.5)));
                        }
                        else if (SkeletonGrid.Children[i].Uid == "SkeletonLine")
                        {
                            rectangle.ApplySweepAnimation(SkeletonLighterColor, SkeletonDarkerColor, new Duration(TimeSpan.FromSeconds(0.5)));
                        }
                    }
                }
            }
        }
        #endregion

        #region Method

        /// <summary>
        /// 骨架屏持续时间
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopSkeletonScreen()
        {
            SkeletonGrid.Visibility = Visibility.Collapsed;
            GeneratorTable.Visibility = Visibility.Visible;
            if (bool.TryParse(config.ShowNotice, out bool showNotice) && showNotice)
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