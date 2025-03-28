using CBHK.CustomControls;
using CBHK.GeneralTools;
using CBHK.GeneralTools.MessageTip;
using CBHK.Generators.FireworkRocketGenerator;
using CBHK.Generators.FireworkRocketGenerator.Components;
using CBHK.View;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace CBHK.ViewModel.Generators
{
    public partial class FireworkRocketViewModel : ObservableObject
    {
        #region 是否展示生成结果
        private bool showGeneratorResult = false;
        public bool ShowGeneratorResult
        {
            get => showGeneratorResult;
            set => SetProperty(ref showGeneratorResult,value);
        }
        #endregion

        /// <summary>
        /// 本生成器的图标路径
        /// </summary>
        string iconPath = "pack://application:,,,/CBHK;component/Resource/Common/Image/SpawnerIcon/IconFireworks.png";

        /// <summary>
        /// 主页引用
        /// </summary>
        private Window home = null;

        #region 正方体侧面、上面和下面的纹理，烟花火箭纹理
        public BitmapImage GrassBlockSide { get; set; }
        public BitmapImage GrassBlockTop { get; set; }
        public BitmapImage GrassBlockBottom { get; set; }
        public BitmapImage FireworkImage { get; set; }

        private IContainerProvider _container;
        #endregion

        /// <summary>
        /// 烟花火箭标签页
        /// </summary>
        public ObservableCollection<RichTabItems> FireworkRocketPageList { get; set; } = [ new RichTabItems() { Header = "烟花",
                IsContentSaved = true,
                BorderThickness = new(4, 4, 4, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC6B23")),
                Foreground = new SolidColorBrush(Colors.White),
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                LeftBorderTexture = Application.Current.Resources["TabItemLeft"] as Brush,
                RightBorderTexture = Application.Current.Resources["TabItemRight"] as Brush,
                TopBorderTexture = Application.Current.Resources["TabItemTop"] as Brush,
                SelectedLeftBorderTexture = Application.Current.Resources["SelectedTabItemLeft"] as Brush,
                SelectedRightBorderTexture = Application.Current.Resources["SelectedTabItemRight"] as Brush,
                SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as Brush, } ];

        private RichTabItems selectedFireworkRocketPage;

        public RichTabItems SelectedFireworkRocketPage
        {
            get => selectedFireworkRocketPage;
            set => SetProperty(ref selectedFireworkRocketPage, value);
        }

        #region 版本数据源
        public ObservableCollection<TextComboBoxItem> VersionSource { get; set; } = [
            new TextComboBoxItem() { Text = "1.20.2" },
            new TextComboBoxItem() { Text = "1.12.0" }
            ] ;
        #endregion

        /// <summary>
        /// 原版颜色库面板
        /// </summary>
        public List<IconCheckBoxs> StructureColorList = [];
        /// <summary>
        /// 三维视图
        /// </summary>
        public Viewport3D View = null;

        /// <summary>
        /// 形状数据源
        /// </summary>
        public ObservableCollection<TextComboBoxItem> ShapeList { get; set; } = [];

        /// <summary>
        /// 形状路径
        /// </summary>
        string shapePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\FireworkRocket\Data\shapes.ini";

        public FireworkRocketViewModel(IContainerProvider container,MainView mainView)
        {
            #region 初始化数据
            if (ShapeList.Count == 0 && File.Exists(shapePath))
            {
                string[] shapes = File.ReadAllLines(shapePath);
                foreach (string shape in shapes)
                {
                    ShapeList.Add(new TextComboBoxItem() { Text = shape[(shape.LastIndexOf(':') + 1)..] });
                }
            }
            #endregion
            #region 初始化成员
            FireworkRocketPagesView fireworkRocketPages = new() { FontWeight = FontWeights.Normal };
            FireworkRocketPageList[0].Content = fireworkRocketPages;
            GrassBlockSide = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\FireworkRocket\Image\grass_block_side.png", UriKind.Absolute));
            GrassBlockTop = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\FireworkRocket\Image\grass_block_top.png", UriKind.Absolute));
            GrassBlockBottom = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\FireworkRocket\Image\grass_block_bottom.png", UriKind.Absolute));
            FireworkImage = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\FireworkRocket\Image\firework_rocket.png", UriKind.Absolute));
            #endregion
            _container = container;
            home = mainView;
        }

        public void FireworkRocket_Loaded(object sender,RoutedEventArgs e)
        {

        }

        /// <summary>
        /// 载入视图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void View_Loaded(object sender,RoutedEventArgs e) => View = sender as Viewport3D;

        [RelayCommand]
        /// <summary>
        /// 预览效果
        /// </summary>
        private void PreviewEffect(FrameworkElement frameworkElement)
        {
            FireworkRocketView fireworkRocket = Window.GetWindow(frameworkElement) as FireworkRocketView;
            FireworkRocketPagesView fireworkRocketPages = SelectedFireworkRocketPage.Content as FireworkRocketPagesView;
            FireworkRocketPagesViewModel fireworkRocketPagesDataContext = fireworkRocketPages.DataContext as FireworkRocketPagesViewModel;
            if (fireworkRocketPagesDataContext.MainColors.Count + fireworkRocketPagesDataContext.FadeColors.Count == 0) return;

            //烟花火箭模型引用
            TranslateTransform3D fireworkModel = fireworkRocket.FireworkModel;
            //获取用户摄像机
            TranslateTransform3D userCamera = fireworkRocket.UserCamera;
            //清除上一个烟花粒子簇
            fireworkRocketPagesDataContext.ParticleContainer.Children.Clear();

            #region 初始化动画
            DoubleAnimation cameraAnimation = new()
            {
                From = 10,
                By = fireworkRocketPagesDataContext.FireworkFlyHeight * (fireworkRocketPagesDataContext.Duration <= 0 ? 1 : fireworkRocketPagesDataContext.Duration),
                Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                AutoReverse = false,
                FillBehavior = FillBehavior.HoldEnd
            };
            DoubleAnimation fireworkRocketAnimation = new()
            {
                From = 1,
                By = fireworkRocketPagesDataContext.FireworkFlyHeight * (fireworkRocketPagesDataContext.Duration <= 0 ? 1 : fireworkRocketPagesDataContext.Duration),
                Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                AutoReverse = false,
                FillBehavior = FillBehavior.Stop
            };
            #endregion
            #region 执行动画
            fireworkRocketAnimation.Completed += fireworkRocketPagesDataContext.FireworkExploded;
            userCamera.BeginAnimation(TranslateTransform3D.OffsetYProperty, cameraAnimation);
            fireworkModel.BeginAnimation(TranslateTransform3D.OffsetYProperty, fireworkRocketAnimation);
            #endregion
        }

        [RelayCommand]
        /// <summary>
        /// 添加烟花火箭
        /// </summary>
        private void AddFireworkRocket()
        {
            FireworkRocketPagesView fireworkRocketPages = new() { FontWeight = FontWeights.Normal };
            RichTabItems richTabItems = new()
            {
                Header = "烟花",
                Content = fireworkRocketPages,
                IsContentSaved = true,
                BorderThickness = new(4, 4, 4, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC6B23")),
                Foreground = new SolidColorBrush(Colors.White),
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                LeftBorderTexture = Application.Current.Resources["TabItemLeft"] as Brush,
                RightBorderTexture = Application.Current.Resources["TabItemRight"] as Brush,
                TopBorderTexture = Application.Current.Resources["TabItemTop"] as Brush,
                SelectedLeftBorderTexture = Application.Current.Resources["SelectedTabItemLeft"] as Brush,
                SelectedRightBorderTexture = Application.Current.Resources["SelectedTabItemRight"] as Brush,
                SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as Brush,
            };
            FireworkRocketPageList.Add(richTabItems);

            if (FireworkRocketPageList.Count == 1)
            {
                TabControl tabControl = richTabItems.FindParent<TabControl>();
                tabControl.SelectedIndex = 0;
            }
        }

        [RelayCommand]
        /// <summary>
        /// 清空烟花火箭
        /// </summary>
        private void ClearFireworkRocket()
        {
            FireworkRocketPageList.Clear();
        }

        [RelayCommand]
        /// <summary>
        /// 从文件导入烟花火箭
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void ImportFireworkRocketFromFile()
        {
            Microsoft.Win32.OpenFileDialog dialog = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                RestoreDirectory = true,
                DefaultExt = ".command",
                Multiselect = false,
                Title = "请选择一个Minecraft烟花火箭数据文件"
            };
            if (dialog.ShowDialog().Value)
                if (File.Exists(dialog.FileName))
                {
                    ObservableCollection<RichTabItems> result = FireworkRocketPageList;
                    ExternalDataImportManager.ImportFireworkDataHandler(dialog.FileName, ref result);
                }
        }

        [RelayCommand]
        /// <summary>
        /// 从剪切板导入烟花火箭
        /// </summary>
        private void ImportFireworkRocketFromClipboard()
        {
            ObservableCollection<RichTabItems> result = FireworkRocketPageList;
            ExternalDataImportManager.ImportFireworkDataHandler(Clipboard.GetText(), ref result, false);
        }

        [RelayCommand]
        /// <summary>
        /// 返回主页
        /// </summary>
        /// <param name="win"></param>
        private void Return(Window win)
        {
            home.WindowState = WindowState.Normal;
            home.Show();
            home.ShowInTaskbar = true;
            home.Focus();
            win.Close();
        }

        [RelayCommand]
        /// <summary>
        /// 执行生成
        /// </summary>
        private async Task Run()
        {
            StringBuilder Result = new();
            foreach (var itemPage in FireworkRocketPageList)
            {
                await itemPage.Dispatcher.InvokeAsync(() =>
                {
                    FireworkRocketPagesViewModel context = (itemPage.Content as FireworkRocketPagesView).DataContext as FireworkRocketPagesViewModel;
                    string result = context.Run(false) + "\r\n";
                    Result.Append(result);
                });
            }
            if (ShowGeneratorResult)
            {
                DisplayerView displayer = _container.Resolve<DisplayerView>();
                if (displayer is not null && displayer.DataContext is DisplayerViewModel displayerViewModel)
                {
                    displayer.Show();
                    displayerViewModel.GeneratorResult(Result.ToString(), "烟花", iconPath);
                }
            }
            else
            {
                Clipboard.SetText(Result.ToString());
                Message.PushMessage("烟花全部生成成功！数据已进入剪切板", MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        /// <summary>
        /// 保存所有烟花火箭
        /// </summary>
        private async Task SaveAll()
        {
            List<string> Result = [];
            List<string> FileNameList = [];

            foreach (var itemPage in FireworkRocketPageList)
            {
                await itemPage.Dispatcher.InvokeAsync(() =>
                {
                    FireworkRocketPagesViewModel context = (itemPage.Content as FireworkRocketPagesView).DataContext as FireworkRocketPagesViewModel;
                    string result = context.Run(false);
                    FileNameList.Add(context.Give ? "FireworkStar" : "FireworkRocket");
                    Result.Add(result);
                });
            }
            OpenFolderDialog openFolderDialog = new()
            {
                Title = "请选择要保存的目录",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                ShowHiddenItems = true
            };
            if (openFolderDialog.ShowDialog().Value)
            {
                if (Directory.Exists(openFolderDialog.FolderName))
                    for (int i = 0; i < Result.Count; i++)
                        File.WriteAllText(openFolderDialog.FolderName + FileNameList[i] + ".command", Result[i]);
            }
        }

        /// <summary>
        /// 斐波那契网格采样算法，用于生成球状烟花爆炸粒子的坐标
        /// </summary>
        /// <param name="count">需要生成的坐标数量</param>
        /// <param name="r">球体半径</param>
        /// <returns></returns>
        public List<Vector3D> GenerateFibonacciSphere(int count, double r)
        {
            List<Vector3D> points = [];
            float phi = (float)(Math.PI * (3 - Math.Sqrt(5))); // 黄金角
            float y;
            float radius;
            float theta;

            for (int i = 0; i < count; i++)
            {
                y = 1 - (i / (float)(count - 1)) * 2; // y值在[-1, 1]之间
                radius = (float)Math.Sqrt(1 - y * y); // 半径

                theta = phi * i; // 角度

                double x = (float)Math.Cos(theta) * radius * r;
                double z = (float)Math.Sin(theta) * radius * r;

                points.Add(new Vector3D(x, y * r, z));
            }
            return points;
        }

        /// <summary>
        /// 五角星算法，生成三个相互之间夹角一致位置重叠的五角星
        /// </summary>
        /// <param name="numPoints"></param>
        /// <param name="innerAngle"></param>
        /// <param name="center"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public List<Vector3D> GenerateStars(int numPoints, double outerRadius, double innerRadius)
        {
            List<Vector3D> vertices = [];
            float angleStep = (float)(Math.PI * 2 / numPoints);
            float angle = (float)(-Math.PI / 2);

            for (int i = 0; i < numPoints; i++)
            {
                // Outer vertex
                vertices.Add(new Vector3D(outerRadius * Math.Cos(angle), outerRadius * Math.Sin(angle), 0));
                angle += angleStep / 2;

                // Inner vertex
                vertices.Add(new Vector3D(innerRadius * Math.Cos(angle), innerRadius * Math.Sin(angle), 0));
                angle += angleStep / 2;
            }

            return vertices;
        }

        /// <summary>
        /// 生成苦力怕面部样式的表面坐标
        /// </summary>
        /// <param name="numPoints"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public List<Vector3D> GetCreeperFaceRandom(int numPoints, double radius)
        {
            List<Vector3D> result = [];

            return result;
        }
    }
}