using cbhk.CustomControls;
using cbhk.GeneralTools;
using cbhk.GeneralTools.MessageTip;
using cbhk.Generators.FireworkRocketGenerator.Components;
using cbhk.WindowDictionaries;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        public ObservableCollection<RichTabItems> FireworkRocketPageList { get; set; } = new() { new RichTabItems() { Header = "烟花",
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
                SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as Brush, } };

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
            List<string> Result = new();
            List<string> FileNameList = new();

            foreach (var itemPage in FireworkRocketPageList)
            {
                await itemPage.Dispatcher.InvokeAsync(() =>
                {
                    FireworkRocketPagesDataContext context = (itemPage.Content as FireworkRocketPages).DataContext as FireworkRocketPagesDataContext;
                    string result = context.run_command(false);
                    //string nbt = "";
                    //if (result.Contains('{'))
                    //{
                    //    nbt = result[result.IndexOf('{')..(result.IndexOf('}') + 1)];
                    //    //补齐缺失双引号对的key
                    //    nbt = Regex.Replace(nbt, @"([\{\[,])([\s+]?\w+[\s+]?):", "$1\"$2\":");
                    //    //清除数值型数据的单位
                    //    nbt = Regex.Replace(nbt, @"(\d+[\,\]\}]?)([a-zA-Z])", "$1").Replace("I;", "");
                    //}
                    FileNameList.Add(context.Give? "FireworkStar" : "FireworkRocket");
                    Result.Add(result);
                });
            }
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new()
            {
                Description = "请选择要保存的目录",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (Directory.Exists(folderBrowserDialog.SelectedPath))
                    for (int i = 0; i < Result.Count; i++)
                        File.WriteAllText(folderBrowserDialog.SelectedPath + FileNameList[i] + ".command", Result[i]);
            }
        }

        /// <summary>
        /// 生成一个球体表面坐标
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public Vector3D GetSphereRandom(float[] random, double radius)
        {
            double phi = 2 * Math.PI * random[0];
            double cosTheta = 1 - 2 * random[1];
            double sinTheta = Math.Sqrt(1 - cosTheta * cosTheta);
            return new Vector3D(radius * sinTheta * Math.Cos(phi), radius * sinTheta * Math.Sin(phi), radius * cosTheta);
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
            GaussianRandom random = new();

            double x = random.NextDouble() * radius;
            double y = random.NextDouble() * radius;
            double z = random.NextDouble() * radius;

            if (Math.Abs(x) < radius / 2 && Math.Abs(y) < radius / 2 && Math.Abs(z) < radius / 2)
                result = new Vector3D(x, y, z);

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
    public class GaussianRandom : Random
    {
        private double nextGaussian;
        private bool hasNextGaussian = false;

        public override double NextDouble()
        {
            if (hasNextGaussian)
            {
                hasNextGaussian = false;
                return nextGaussian;
            }
            else
            {
                double v1, v2, s;
                do
                {
                    v1 = 2 * base.NextDouble() - 1; // between -1.0 and 1.0
                    v2 = 2 * base.NextDouble() - 1; // between -1.0 and 1.0
                    s = v1 * v1 + v2 * v2;
                } while (s >= 1 || s == 0);
                double multiplier = Math.Sqrt(-2 * Math.Log(s) / s);
                nextGaussian = v2 * multiplier;
                hasNextGaussian = true;
                return v1 * multiplier;
            }
        }
    }
}
