using cbhk.CustomControls;
using cbhk.GeneralTools;
using cbhk.GeneralTools.MessageTip;
using cbhk.Generators.FireworkRocketGenerator.Components;
using cbhk.WindowDictionaries;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace cbhk.Generators.FireworkRocketGenerator
{
    public class FireworkRocketDataContext: ObservableObject
    {
        #region 返回和运行等指令
        public RelayCommand<CommonWindow> Return { get; set; }
        public RelayCommand Run { get; set; }
        public RelayCommand SaveAll { get; set; }
        #endregion

        #region 添加、清空、导入烟花等指令
        public RelayCommand AddFireworkRocket { get; set; }
        public RelayCommand ClearFireworkRocket { get; set; }
        public RelayCommand ImportFireworkRocketFromClipboard { get; set; }
        public RelayCommand ImportFireworkRocketFromFile { get; set; }
        #endregion

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
        string icon_path = "pack://application:,,,/cbhk;component/resources/common/images/spawnerIcons/IconFireworks.png";

        /// <summary>
        /// 主页引用
        /// </summary>
        public Window home = null;

        #region 正方体侧面、上面和下面的纹理，烟花火箭纹理
        public BitmapImage GrassBlockSide { get; set; }
        public BitmapImage GrassBlockTop { get; set; }
        public BitmapImage GrassBlockBottom { get; set; }
        public BitmapImage FireworkImage { get; set; }
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

        /// <summary>
        /// 三维视图
        /// </summary>
        public Viewport3D viewport3D = null;

        public FireworkRocketDataContext()
        {
            #region 连接指令
            Return = new RelayCommand<CommonWindow>(return_command);
            Run = new RelayCommand(run_command);
            SaveAll = new RelayCommand(SaveAllCommand);
            AddFireworkRocket = new RelayCommand(AddFireworkRocketCommand);
            ClearFireworkRocket = new RelayCommand(ClearFireworkRocketCommand);
            ImportFireworkRocketFromFile = new RelayCommand(ImportFireworkRocketFromFileCommand);
            ImportFireworkRocketFromClipboard = new RelayCommand(ImportFireworkRocketFromClipboardCommand);
            #endregion

            #region 初始化成员
            FireworkRocketPages fireworkRocketPages = new() { FontWeight = FontWeights.Normal };
            FireworkRocketPageList[0].Content = fireworkRocketPages;
            GrassBlockSide = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\FireworkRocket\\images\\grass_block_side.png", UriKind.Absolute));
            GrassBlockTop = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\FireworkRocket\\images\\grass_block_top.png", UriKind.Absolute));
            GrassBlockBottom = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\FireworkRocket\\images\\grass_block_bottom.png", UriKind.Absolute));
            FireworkImage = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\FireworkRocket\\images\\firework_rocket.png", UriKind.Absolute));
            #endregion
        }

        /// <summary>
        /// 添加烟花火箭
        /// </summary>
        private void AddFireworkRocketCommand()
        {
            FireworkRocketPages fireworkRocketPages = new() { FontWeight = FontWeights.Normal };
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

        /// <summary>
        /// 清空烟花火箭
        /// </summary>
        private void ClearFireworkRocketCommand()
        {
            FireworkRocketPageList.Clear();
        }

        /// <summary>
        /// 从文件导入烟花火箭
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void ImportFireworkRocketFromFileCommand()
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

        /// <summary>
        /// 从剪切板导入烟花火箭
        /// </summary>
        private void ImportFireworkRocketFromClipboardCommand()
        {
            ObservableCollection<RichTabItems> result = FireworkRocketPageList;
            ExternalDataImportManager.ImportFireworkDataHandler(Clipboard.GetText(), ref result, false);
        }

        /// <summary>
        /// 返回主页
        /// </summary>
        /// <param name="win"></param>
        private void return_command(CommonWindow win)
        {
            home.WindowState = WindowState.Normal;
            home.Show();
            home.ShowInTaskbar = true;
            home.Focus();
            win.Close();
        }

        /// <summary>
        /// 执行生成
        /// </summary>
        private async void run_command()
        {
            StringBuilder Result = new();
            foreach (var itemPage in FireworkRocketPageList)
            {
                await itemPage.Dispatcher.InvokeAsync(() =>
                {
                    FireworkRocketPagesDataContext context = (itemPage.Content as FireworkRocketPages).DataContext as FireworkRocketPagesDataContext;
                    string result = context.run_command(false) + "\r\n";
                    Result.Append(result);
                });
            }
            if (ShowGeneratorResult)
            {
                GenerateResultDisplayer.Displayer displayer = GenerateResultDisplayer.Displayer.GetContentDisplayer();
                displayer.GeneratorResult(Result.ToString(), "烟花", icon_path);
                displayer.Show();
            }
            else
            {
                Clipboard.SetText(Result.ToString());
                Message.PushMessage("烟花全部生成成功！数据已进入剪切板", MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 保存所有烟花火箭
        /// </summary>
        private async void SaveAllCommand()
        {
            await GeneratorAndSaveAllFireworks();
        }

        private async Task GeneratorAndSaveAllFireworks()
        {
            List<string> Result = [];
            List<string> FileNameList = [];

            foreach (var itemPage in FireworkRocketPageList)
            {
                await itemPage.Dispatcher.InvokeAsync(() =>
                {
                    FireworkRocketPagesDataContext context = (itemPage.Content as FireworkRocketPages).DataContext as FireworkRocketPagesDataContext;
                    string result = context.run_command(false);
                    FileNameList.Add(context.Give? "FireworkStar" : "FireworkRocket");
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
        public Vector3D GetCreeperFaceRandom(int numPoints, double radius)
        {
            Vector3D result = new();

            return result;
        }

        /// <summary>
        /// 获取Viewport3D引用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void GetViewport3DLoaded(object sender,RoutedEventArgs e)
        {
            viewport3D = sender as Viewport3D;
        }
    }
}
