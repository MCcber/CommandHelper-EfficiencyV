using CBHK.CustomControl;
using CBHK.CustomControl.ColorPickerComponents;
using CBHK.Utility.MessageTip;
using CBHK.View;
using CBHK.View.Component.FireworkRocket;
using CBHK.View.Generator;
using CBHK.ViewModel.Generator;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace CBHK.ViewModel.Component.FireworkRocket
{
    public partial class FireworkRocketPageViewModel(IContainerProvider container) : ObservableObject
    {
        #region Field

        /// <summary>
        /// 存储生成结果
        /// </summary>
        public string Result = "";
        /// <summary>
        /// 本生成器的图标路径
        /// </summary>
        string iconPath = "pack://application:,,,/CBHK;component/Resource/Common/Image/SpawnerIcon/IconFireworks.png";
        /// <summary>
        /// 原版颜色库路径
        /// </summary>
        string colorStoragePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\FireworkRocket\Image";
        /// <summary>
        /// 颜色映射表路径
        /// </summary>
        string colorTablePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\FireworkRocket\Data\structureColorDictionary.ini";
        /// <summary>
        /// 拾色器
        /// </summary>
        ColorPickers colorpicker = null;
        /// <summary>
        /// 烟花粒子纹理文件路径
        /// </summary>
        BitmapImage particleTexture = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\FireworkRocket\Image\x.png", UriKind.Absolute)) { CacheOption = BitmapCacheOption.OnLoad };
        /// <summary>
        /// 烟花粒子尺寸
        /// </summary>
        double particleScale = 0.5;
        /// <summary>
        /// 烟上升高度倍率
        /// </summary>
        public int FireworkFlyHeight = 10;
        /// <summary>
        /// 烟花爆炸持续时间
        /// </summary>
        double fireworkExplosionDuration = 0.1;
        /// <summary>
        /// 烟花爆炸粒子渲染组
        /// </summary>
        public Model3DGroup ParticleContainer = new();
        /// <summary>
        /// 淡入和淡出的粒子数量
        /// </summary>
        public int ParticleCount = 350;
        UniformGrid StructureColorGrid = null;
        List<IconCheckBoxs> StructureColorList = [];
        /// <summary>
        /// 原版颜色映射库
        /// </summary>
        private Dictionary<string, string> OriginColorDictionary = [];
        private IContainerProvider _container = container;

        #region 淡入淡出滚动视图引用
        ScrollViewer mainScrollViewer = null;
        ScrollViewer fadeScrollViewer = null;
        #endregion

        #endregion

        #region Property
        /// <summary>
        /// 已选版本
        /// </summary>
        [ObservableProperty]
        private TextComboBoxItem _selectedVersion;

        /// <summary>
        /// 版本数据源
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<TextComboBoxItem> _versionSource = [];

        /// <summary>
        /// 形状数据源
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<TextComboBoxItem> _shapeList = [];

        /// <summary>
        /// 生成行为
        /// </summary>
        [ObservableProperty]
        private bool _give = false;

        /// <summary>
        /// 生成烟花火箭
        /// </summary>
        [ObservableProperty]
        private bool _generatorFireStar = false;

        /// <summary>
        /// 是否展示生成结果
        /// </summary>
        [ObservableProperty]
        private bool _showGeneratorResult = false;

        /// <summary>
        /// 导入模式
        /// </summary>
        public bool ImportMode { get; set; } = false;
        /// <summary>
        /// 外部文件路径
        /// </summary>
        public string ExternFilePath { get; set; } = "";
        /// <summary>
        /// 闪烁
        /// </summary>
        public bool Flicker { get; set; } = false;
        /// <summary>
        /// 拖尾
        /// </summary>
        public bool Trail { get; set; } = false;
        /// <summary>
        /// 时长
        /// </summary>
        [ObservableProperty]
        private double _duration = 1;

        #region 时长字符串
        private string FireworkDurationString
        {
            get
            {
                string result;
                result = "Flight:" + Duration;
                return result;
            }
        }
        #endregion

        #region 轨迹
        private string FireworkTrajectoryString
        {
            get
            {
                string result = (Flicker ? "Flicker:1b," : "") + (Trail ? "Trail:1b," : "");
                return result;
            }
        }
        #endregion

        #region 已飞行刻数
        [ObservableProperty]
        private double _life = 0;
        private string LifeString
        {
            get
            {
                string result = Life > 0 ? "Life:" + Life + "," : "";
                return result;
            }
        }
        #endregion

        #region 发射时长
        [ObservableProperty]
        private double _lifeTime = 20;
        private string LifeTimeString
        {
            get
            {
                string result = LifeTime > 0 ? "LifeTime:" + LifeTime + "," : "";
                return result;
            }
        }
        #endregion

        #region 主颜色
        private string MainColorsString
        {
            get
            {
                string result = "Colors:[I;";
                if (MainColorList.Count > 0)
                {
                    result += string.Join(',', MainColorList.Select(item => Convert.ToUInt64(item.Background.ToString()[2..], 16)));
                    result += "],";
                }
                else
                {
                    result = "";
                }
                return result;
            }
        }
        #endregion

        #region 备选颜色
        private string FadeColorsString
        {
            get
            {
                string result = "FadeColorList:[I;";
                if (FadeColorList.Count > 0)
                {
                    result += string.Join(',', FadeColorList.Select(item => Convert.ToUInt64(item.Background.ToString()[2..], 16)));
                    result += "],";
                }
                else
                {
                    result = "";
                }
                return result;
            }
        }
        #endregion

        #region 按角度飞出
        /// <summary>
        /// 按角度飞出
        /// </summary>
        [ObservableProperty]
        public bool _flyAngle = false;
        private string FlyAngleString
        {
            get
            {
                string result;
                result = FlyAngle ? "ShotAtAngle:true," : "";
                return result;
            }
        }
        #endregion

        #region 点/连续(模式切换)
        private bool selectedModeLock = true;
        private bool pointMode = true;
        public bool PointMode
        {
            get => pointMode;
            set
            {
                SetProperty(ref pointMode, value);
                if (selectedModeLock)
                {
                    selectedModeLock = false;
                    ContinuousMode = !pointMode;
                    selectedModeLock = true;
                }
            }
        }

        private bool continuousMode = false;
        public bool ContinuousMode
        {
            get => continuousMode;
            set
            {
                SetProperty(ref continuousMode, value);
                if (selectedModeLock)
                {
                    selectedModeLock = false;
                    PointMode = !continuousMode;
                    selectedModeLock = true;
                }
            }
        }
        #endregion

        #region 已选择颜色
        private SolidColorBrush selectedColor = new((Color)ColorConverter.ConvertFromString("#FF0000"));
        public SolidColorBrush SelectedColor
        {
            get => selectedColor;
            set
            {
                SetProperty(ref selectedColor, value);
                if (ContinuousMode)
                {
                    if (AddInMain)
                    {
                        Border border = new()
                        {
                            Width = 25,
                            Background = selectedColor
                        };
                        border.MouseRightButtonUp += DeleteColorMouseRightButtonUp;
                        border.Uid = "Main";
                        MainColorList.Add(border);
                    }
                    if(AddInFade)
                    {
                        Border border = new()
                        {
                            Width = 25,
                            Background = selectedColor
                        };
                        border.MouseRightButtonUp += DeleteColorMouseRightButtonUp;
                        border.Uid = "Fade";
                        FadeColorList.Add(border);
                    }
                }
            }
        }
        #endregion

        #region 最终形状数据
        /// <summary>
        /// 已选择的形状
        /// </summary>
        [ObservableProperty]
        private int _selectedShape = 0;
        private string FireworkShapeString
        {
            get
            {
                string result = "Type:" + SelectedShape + ",";
                return result;
            }
        }
        #endregion

        #region 主颜色库与备选颜色库
        [ObservableProperty]
        private ObservableCollection<Border> _mainColorList = [];
        [ObservableProperty]
        private ObservableCollection<Border> _fadeColorList = [];
        #endregion

        #region 加入淡入或淡出
        [ObservableProperty]
        private bool _addInMain = true;
        [ObservableProperty]
        private bool addInFade = false;
        #endregion

        #region 滚轮缩放倍率
        private double viewScale = 0;
        private double ViewScale
        {
            get => viewScale;
            set
            {
                viewScale = value;
                if (viewScale < 0.1)
                    viewScale = 0.1;
                if (viewScale > 2)
                    viewScale = 2;
            }
        }
        #endregion

        /// <summary>
        /// 外部数据
        /// </summary>
        public JObject ExternallyReadEntityData { get; set; }
        /// <summary>
        /// 预览效果的粒子集合
        /// </summary>
        public List<ModelVisual3D> Particles { get; set; } = [];
        #endregion

        #region Method
        /// <summary>
        /// 为图像对象着色
        /// </summary>
        /// <param name="bitmapImage"></param>
        /// <param name="brush"></param>
        public BitmapImage ColoringBitmapImage(BitmapImage bitmapImage,SolidColorBrush brush)
        {
            // Get the pixel data from the original image
            int[] pixelData = new int[bitmapImage.PixelWidth * bitmapImage.PixelHeight];
            bitmapImage.CopyPixels(pixelData, bitmapImage.PixelWidth * 4, 0);

            // Create a new WriteableBitmap with the same dimensions as the original image
            WriteableBitmap coloredBitmap = new(bitmapImage.PixelWidth, bitmapImage.PixelHeight, bitmapImage.DpiX, bitmapImage.DpiY, PixelFormats.Bgra32, null);
            coloredBitmap.Lock();
            unsafe
            {
                int* pBackBuffer = (int*)coloredBitmap.BackBuffer;
                int backBufferStride = coloredBitmap.BackBufferStride / 4;
                // Get the alpha value from the original image
                int alpha = 0;
                for (int y = 0; y < coloredBitmap.PixelHeight; y++)
                {
                    for (int x = 0; x < coloredBitmap.PixelWidth; x++)
                    {
                        alpha = (pixelData[y * backBufferStride + x] >> 24) & 0xff;
                        pBackBuffer[y * backBufferStride + x] = (alpha << 24) | (brush.Color.R << 16) | (brush.Color.G << 8) | brush.Color.B;
                    }
                }
            }
            coloredBitmap.Unlock();

            // Convert the colored WriteableBitmap back to a BitmapImage
            using MemoryStream memoryStream = new();
            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(coloredBitmap));
            encoder.Save(memoryStream);
            memoryStream.Position = 0;
            BitmapImage coloredImage = new();
            coloredImage.BeginInit();
            coloredImage.StreamSource = memoryStream;
            coloredImage.CacheOption = BitmapCacheOption.OnLoad;
            coloredImage.EndInit();
            return coloredImage;
        }

        private Vector3D CalculateNormal(MeshGeometry3D mesh, int vertexIndex)
        {
            var normal = new Vector3D();

            // Find all triangles that share the vertex
            var triangles = new List<int>();
            for (int i = 0; i < mesh.TriangleIndices.Count; i += 3)
            {
                if (mesh.TriangleIndices[i] == vertexIndex || mesh.TriangleIndices[i + 1] == vertexIndex || mesh.TriangleIndices[i + 2] == vertexIndex)
                {
                    triangles.Add(i);
                }
            }

            // Calculate the normal vector as the average of the normals of the triangles
            foreach (var triangleIndex in triangles)
            {
                var p1 = mesh.Positions[mesh.TriangleIndices[triangleIndex]];
                var p2 = mesh.Positions[mesh.TriangleIndices[triangleIndex + 1]];
                var p3 = mesh.Positions[mesh.TriangleIndices[triangleIndex + 2]];
                var triangleNormal = Vector3D.CrossProduct(p2 - p1, p3 - p1);
                triangleNormal.Normalize();
                normal += triangleNormal;
            }
            normal.Normalize();

            return normal;
        }
        #endregion

        #region Event
        /// <summary>
        /// 载入时读取外部数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void FireworkRocket_Loaded(object sender,RoutedEventArgs e)
        {
            #region 载入形状数据
            FireworkRocketPageView fireworkRocketPage = sender as FireworkRocketPageView;
            FireworkRocketViewModel fireworkRocketDataContext = Window.GetWindow(fireworkRocketPage).DataContext as FireworkRocketViewModel;
            ShapeList = fireworkRocketDataContext.ShapeList;
            #endregion

            #region 载入版本数据
            VersionSource = fireworkRocketDataContext.VersionSource;
            #endregion

            #region 是否需要导入数据
            if (ImportMode)
                await Task.Run(() =>
            {
                Give = ExternallyReadEntityData["ItemView"] is null;
                JObject Explosion = ExternallyReadEntityData.SelectToken("ItemView.tag.Explosion") as JObject;
                JArray Explosions = ExternallyReadEntityData.SelectToken("ItemView.tag.Fireworks.Explosions") as JArray;
                GeneratorFireStar = Explosion != null;

                if (Give)
                {
                    Explosion = ExternallyReadEntityData.SelectToken("Explosion") as JObject;
                    Explosions = ExternallyReadEntityData.SelectToken("Fireworks.Explosions") as JArray;
                }

                JArray colors = null;
                JArray fadeColors = null;
                JToken flight = null;
                JToken flicker = null;
                JToken trail = null;
                JToken type = null;
                JToken life = null;
                JToken lifeTime = null;
                JToken shotAtAngle = null;

                if (Explosions.Count > 0)
                    Explosion = Explosions[0] as JObject;

                colors = Explosion.SelectToken("Colors") as JArray;
                fadeColors = Explosion.SelectToken("FadeColorList") as JArray;
                flight = ExternallyReadEntityData.SelectToken("ItemView.Flight");
                flicker = Explosion.SelectToken("Flicker");
                trail = Explosion.SelectToken("Trail");
                type = Explosion.SelectToken("Type");
                life = ExternallyReadEntityData.SelectToken("ItemView.Life");
                lifeTime = ExternallyReadEntityData.SelectToken("ItemView.LifeTime");
                shotAtAngle = ExternallyReadEntityData.SelectToken("ItemView.ShotAtAngle");

                #region 添加主淡入颜色和淡出颜色集合
                FireworkRocketPageView page = sender as FireworkRocketPageView;
                page.Dispatcher.InvokeAsync(() =>
                {
                    if (colors != null)
                        foreach (JValue item in colors.Cast<JValue>())
                        {
                            string colorString = item.Value<long>().ToString("X");
                            Border border = new()
                            {
                                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F" + colorString)),
                                Width = 25
                            };
                            MainColorList.Add(border);
                        }
                    if (fadeColors != null)
                        foreach (JValue item in fadeColors.Cast<JValue>())
                        {
                            string colorString = item.Value<long>().ToString("X");
                            Border border = new()
                            {
                                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F" + colorString)),
                                Width = 25
                            };
                            FadeColorList.Add(border);
                        }
                });
                #endregion
                #region 设置剩余数据
                if (flight != null)
                    Duration = double.Parse(flight.ToString());
                if (flicker != null)
                    Flicker = flicker.ToString() == "1" || flicker.ToString().Equals("true", StringComparison.CurrentCultureIgnoreCase);
                if (trail != null)
                    Trail = trail.ToString() == "1" || trail.ToString().Equals("true", StringComparison.CurrentCultureIgnoreCase);
                if (type != null)
                    SelectedShape = int.Parse(type.ToString());

                if (!Give)
                {
                    if (life != null)
                        Life = double.Parse(life.ToString());
                    if (lifeTime != null)
                        LifeTime = double.Parse(lifeTime.ToString());
                    if (shotAtAngle != null)
                        FlyAngle = shotAtAngle.ToString() == "1" || shotAtAngle.ToString().Equals("true", StringComparison.CurrentCultureIgnoreCase);
                }
                #endregion
            });
            #endregion
        }

        /// <summary>
        /// 载入结构色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void StructureColor_Loaded(object sender,RoutedEventArgs e)
        {
            #region 载入预设颜色
            if(StructureColorGrid is not null)
            {
                return;
            }
            StructureColorGrid = sender as UniformGrid;
            string[] colorArray = Directory.GetFiles(colorStoragePath);
            string[] colorTable = File.ReadAllLines(colorTablePath);
            foreach (var item in colorArray)
            {
                if (item.Contains("dye"))
                {
                    string colorName = Path.GetFileNameWithoutExtension(item);
                    colorName = colorName[..colorName.LastIndexOf('_')];
                    BitmapImage bitmapImage = new(new Uri(item, UriKind.Absolute));
                    IconCheckBoxs iconCheckBoxs = new()
                    {
                        ContentImage = bitmapImage,
                        HeaderHeight = 25,
                        HeaderWidth = 25,
                        SnapsToDevicePixels = true,
                        UseLayoutRounding = true,
                        ToolTip = colorName,
                        Tag = colorName,
                        Style = Application.Current.Resources["IconCheckBox"] as Style
                    };
                    iconCheckBoxs.Checked += StructureColorChecked;
                    RenderOptions.SetBitmapScalingMode(iconCheckBoxs, BitmapScalingMode.NearestNeighbor);
                    RenderOptions.SetClearTypeHint(iconCheckBoxs, ClearTypeHint.Enabled);
                    ToolTipService.SetShowDuration(iconCheckBoxs, 1000);
                    ToolTipService.SetInitialShowDelay(iconCheckBoxs, 0);
                    StructureColorGrid.Children.Add(iconCheckBoxs);
                }
            }

            string colorID = "";
            string colorString = "";
            foreach (var item in colorTable)
            {
                colorID = item.Split(':')[0];
                colorString = item.Split(':')[1];
                OriginColorDictionary.TryAdd(colorID, colorString);
            }
            #endregion
        }

        /// <summary>
        /// 已选择结构色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StructureColorChecked(object sender, RoutedEventArgs e)
        {
            IconCheckBoxs iconCheckBoxs = sender as IconCheckBoxs;
            string searchTarget = iconCheckBoxs.Tag.ToString();
            string colorValue = OriginColorDictionary.Where(item => item.Value == searchTarget).Select(item => item.Key).First();
            if (AddInMain && MainColorList.Count < ParticleCount)
            {
                Border border = new()
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorValue)),
                    Width = 25,
                    ToolTip = "右击删除"
                };
                ToolTipService.SetBetweenShowDelay(border, 0);
                ToolTipService.SetInitialShowDelay(border, 0);
                border.MouseRightButtonUp += DeleteColorMouseRightButtonUp;
                border.Uid = "Main";
                MainColorList.Add(border);
            }
            if (AddInFade && FadeColorList.Count < ParticleCount)
            {
                Border border = new()
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorValue)),
                    Width = 25,
                    ToolTip = "右击删除"
                };
                ToolTipService.SetBetweenShowDelay(border, 0);
                ToolTipService.SetInitialShowDelay(border, 0);
                border.MouseRightButtonUp += DeleteColorMouseRightButtonUp;
                border.Uid = "Fade";
                FadeColorList.Add(border);
            }

        }

        /// <summary>
        /// 载入淡入颜色滚动视图引用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MainColorGridScrollViewer_Loaded(object sender, RoutedEventArgs e) => mainScrollViewer = sender as ScrollViewer;

        /// <summary>
        /// 载入淡出颜色滚动视图引用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void FadeColorGridScrollViewer_Loaded(object sender, RoutedEventArgs e) => fadeScrollViewer = sender as ScrollViewer;

        /// <summary>
        /// 为拾色器的矩形选择面板订阅左击抬起事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ColorPicker_Loaded(object sender, RoutedEventArgs e)
        {
            colorpicker = sender as ColorPickers;
            colorpicker.rectColorGrid.PreviewMouseLeftButtonUp += ColorPickers_PreviewMouseLeftButtonUp;
        }

        /// <summary>
        /// 右击删除指定颜色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void DeleteColorMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Border border = sender as Border;
            if (border.Uid == "Main")
                MainColorList.Remove(border);
            else
                FadeColorList.Remove(border);
        }

        [RelayCommand]
        /// <summary>
        /// 保存烟花火箭或烟火之星
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void Save()
        {
            Run(false);

            Microsoft.Win32.SaveFileDialog saveFileDialog = new()
            {
                AddExtension = true,
                RestoreDirectory = true,
                CheckPathExists = true,
                DefaultExt = "command",
                Filter = "Command files (*.command;)|*.command;",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Title = "保存为命令文件"
            };
            if (saveFileDialog.ShowDialog().Value)
            {
                if(Directory.Exists(Path.GetDirectoryName(saveFileDialog.FileName)))
                _ = File.WriteAllTextAsync(saveFileDialog.FileName, Result);
                _ = File.WriteAllTextAsync(AppDomain.CurrentDomain.BaseDirectory + "resources\\saves\\EntityView\\" + Path.GetFileName(saveFileDialog.FileName), Result);
            }
        }

        /// <summary>
        /// 执行烟花爆炸
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void FireworkExploded(object sender, EventArgs e)
        {
            FireworkRocketView fireworkRocket = Window.GetWindow(colorpicker) as FireworkRocketView;
            FireworkRocketViewModel context = fireworkRocket.DataContext as FireworkRocketViewModel;
            await fireworkRocket.Dispatcher.InvokeAsync(() =>
            {
                #region 获取相关引用、实例化网格模型
                //模型父容器
                ModelVisual3D modelVisual3D = new()
                {
                    Content = ParticleContainer
                };
                //模型顶点、顶点索引、纹理uv坐标数据
                MeshGeometry3D meshGeometry3D = new()
                {
                    Positions = new Point3DCollection(new List<Point3D> { new(0, 0, 0), new(particleScale, 0, 0), new(particleScale, particleScale, 0), new(0, particleScale, 0) }),
                    TextureCoordinates = new PointCollection(new List<Point> { new(0, 1), new(1, 1), new(1, 0), new(0, 0) }),
                    TriangleIndices = new Int32Collection(new List<int> { 0, 1, 2, 0, 2, 3 })
                };

                Random random = new();
                #endregion

                //计算对应的图像点阵坐标
                List<Vector3D> results = [];
                switch (SelectedShape)
                {
                    case 2:
                        results = context.GenerateStars(ParticleCount, 1, 0.38);
                        break;
                    case 3:
                        break;
                    case 4:
                        break;
                    default:
                        results = context.GenerateFibonacciSphere(ParticleCount, 1);
                        break;
                }
                for (int i = 0; i < ParticleCount; i++)
                {
                    Vector3D randomPos = results[i];
                    //实例化纹理笔刷
                    ImageBrush imageBrush = new();

                    #region 初始化设置
                    //设置为点过滤模式，让像素纹理高清
                    RenderOptions.SetBitmapScalingMode(imageBrush, BitmapScalingMode.NearestNeighbor);
                    //开启抗锯齿
                    RenderOptions.SetEdgeMode(imageBrush, EdgeMode.Aliased);
                    #endregion

                    #region 构造法线信息，执行光反射，让纹理深度信息更清晰
                    Vector3DCollection normals = [];
                    for (int j = 0; j < meshGeometry3D.Positions.Count; j++)
                    {
                        var normal = CalculateNormal(meshGeometry3D, j);
                        normals.Add(normal);
                    }
                    meshGeometry3D.Normals = normals;
                    #endregion

                    #region 处理平移、旋转、缩放等变换
                    Transform3DGroup transform3DGroup = new();
                    TranslateTransform3D translateTransform3D = new()
                    {
                        OffsetX = 1.5,
                        OffsetY = 1 + FireworkFlyHeight * (Duration <= 0 ? 1 : Duration),
                        OffsetZ = 1.5
                    };
                    RotateTransform3D rotateTransform3D = new()
                    {
                        CenterX = 0.5,
                        CenterY = 0.5,
                        CenterZ = 0.5,
                        Rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 45)
                    };

                    ScaleTransform3D scaleTransform3D = new()
                    {
                        ScaleX = 0.2,
                        ScaleY = 0.25,
                        ScaleZ = 0.25,
                        CenterX = meshGeometry3D.Bounds.X + meshGeometry3D.Bounds.SizeX / 2,
                        CenterY = meshGeometry3D.Bounds.Y + meshGeometry3D.Bounds.SizeY / 2,
                        CenterZ = meshGeometry3D.Bounds.Z + meshGeometry3D.Bounds.SizeZ / 2,
                    };
                    transform3DGroup.Children.Add(scaleTransform3D);
                    transform3DGroup.Children.Add(rotateTransform3D);
                    transform3DGroup.Children.Add(translateTransform3D);
                    #endregion

                    #region 处理淡入颜色变换
                    if (MainColorList.Count > 0)
                        imageBrush.ImageSource = ColoringBitmapImage(particleTexture, new SolidColorBrush((Color)ColorConverter.ConvertFromString(MainColorList[random.Next(0, MainColorList.Count - 1)].Background.ToString())));
                    else
                        imageBrush.ImageSource = ColoringBitmapImage(particleTexture, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")));
                    #endregion

                    #region 应用模型，设置几何数据和材质数据、执行爆炸动画
                    GeometryModel3D geometryModel3D = new()
                    {
                        Geometry = meshGeometry3D,
                        Material = new DiffuseMaterial() { Brush = imageBrush }
                    };
                    DoubleAnimation posXAnimation = new()
                    {
                        From = 1.5,
                        By = randomPos.X,
                        AutoReverse = false,
                        Duration = new Duration(TimeSpan.FromSeconds(fireworkExplosionDuration))
                    };
                    DoubleAnimation posYAnimation = new()
                    {
                        From = 1 + FireworkFlyHeight * (Duration <= 0 ? 1 : Duration),
                        By = randomPos.Y,
                        AutoReverse = false,
                        Duration = new Duration(TimeSpan.FromSeconds(fireworkExplosionDuration))
                    };
                    DoubleAnimation posZAnimation = new()
                    {
                        From = 1.5,
                        By = randomPos.Z,
                        AutoReverse = false,
                        Duration = new Duration(TimeSpan.FromSeconds(fireworkExplosionDuration))
                    };
                    geometryModel3D.Transform = transform3DGroup;
                    ParticleContainer.Children.Add(geometryModel3D);
                    translateTransform3D.BeginAnimation(TranslateTransform3D.OffsetXProperty, posXAnimation);
                    translateTransform3D.BeginAnimation(TranslateTransform3D.OffsetYProperty, posYAnimation);
                    translateTransform3D.BeginAnimation(TranslateTransform3D.OffsetZProperty, posZAnimation);
                    #endregion
                }

                if (!context.View.Children.Contains(modelVisual3D))
                    context.View.Children.Add(modelVisual3D);
            });
            if (FadeColorList.Count > 0)
            {
                //等待烟花爆炸动画结束，执行淡出动画
                await Task.Delay(TimeSpan.FromSeconds(fireworkExplosionDuration));
                await Task.Run(async () =>
                {
                    await fireworkRocket.Dispatcher.InvokeAsync(() =>
                    {
                        FireworkPosAnimationCompleted();
                    });
                });
            }
        }

        /// <summary>
        /// 执行烟花火箭的淡出效果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FireworkPosAnimationCompleted()
        {
            if (FadeColorList.Count == 0)
                return;
            Random random = new();
            ParticleCount = FadeColorList.Count < ParticleCount ? FadeColorList.Count : 350;
            for (int i = 0; i < ParticleCount; i++)
            {
                #region 处理烟花粒子缓降和淡出颜色变换动画
                Transform3DGroup transform3DGroup = ParticleContainer.Children[i].Transform as Transform3DGroup;
                TranslateTransform3D translateTransform3D = transform3DGroup.Children[^1] as TranslateTransform3D;
                DoubleAnimation posYAnimation = new()
                {
                    From = translateTransform3D.OffsetY,
                    By = -0.5,
                    AutoReverse = false,
                    Duration = new Duration(TimeSpan.FromSeconds(0.5))
                };
                #endregion

                #region 处理淡出颜色变换
                //实例化纹理笔刷
                ImageBrush imageBrush = new()
                {
                    ImageSource = ColoringBitmapImage(particleTexture, new SolidColorBrush((Color)ColorConverter.ConvertFromString(FadeColorList[random.Next(0, FadeColorList.Count - 1)].Background.ToString())))
                };
                (ParticleContainer.Children[i] as GeometryModel3D).Material = new DiffuseMaterial() { Brush = imageBrush };
                translateTransform3D.BeginAnimation(TranslateTransform3D.OffsetYProperty, posYAnimation);
                #endregion
            }
        }

        [RelayCommand]
        /// <summary>
        /// 执行生成
        /// </summary>
        private void Run()
        {
            string ExplosionsData = FireworkShapeString + FireworkTrajectoryString + MainColorsString + FadeColorsString;
            if (ExplosionsData.Length > 0)
            {
                if (!GeneratorFireStar)
                    ExplosionsData = "Explosions:[{" + ExplosionsData.Trim(',') + "}],";
                else
                    ExplosionsData = "Explosion:{" + ExplosionsData.Trim(',') + "},";
            }
            else
                ExplosionsData = "";
            Result = ExplosionsData;
            if (Give)
            {
                if (ExplosionsData.Length > 0)
                    Result += FireworkDurationString;
                Result = Result.Trim(',');
                if (!GeneratorFireStar)
                {
                    if (SelectedVersion.Text == "1.13+")
                        Result = "give @p " + "firework_rocket{Fireworks:{" + Result + "}}";
                    else
                        Result = "give @p " + "firework_rocket" + " 1 0 {Fireworks:{" + Result + "}}";
                }
                else
                {
                    if (SelectedVersion.Text == "1.13+")
                        Result = "give @p " + "firework_star{" + Result + "}";
                    else
                        Result = "give @p " + "firework_star" + " 1 0 {" + Result + "}";
                }
            }
            else
            if (!Give)
            {
                Result = Result.Trim(',');
                if (!GeneratorFireStar)
                {
                    Result = "id:\"minecraft:firework_rocket\",Count:1b,tag:{Fireworks:{" + Result + "}},";
                    Result = "summon item ~ ~ ~ {ItemView:{" + (LifeTimeString + LifeString + FlyAngleString + Result + FireworkDurationString).Trim(',') + "}}";
                }
                else
                {
                    Result = "id:\"minecraft:firework_star\",Count:1b,tag:{" + Result + "},";
                    Result = "summon item ~ ~ ~ {ItemView:{" + Result + "}}";
                }
            }

            if (ShowGeneratorResult)
            {
                DisplayerView displayer = _container.Resolve<DisplayerView>();
                if (displayer is not null && displayer.DataContext is DisplayerViewModel displayerViewModel)
                {
                    displayer.Show();
                    displayerViewModel.GeneratorResult(Result, "烟花火箭", iconPath);
                }
            }
            else
            {
                Clipboard.SetText(Result);
                Message.PushMessage("烟花生成成功！数据已进入剪切板", MessageBoxImage.Information);
            }
        }

        public string Run(bool showResult)
        {
            string ExplosionsData = FireworkShapeString + FireworkTrajectoryString + MainColorsString + FadeColorsString;
            if (ExplosionsData.Length > 0)
            {
                if (!GeneratorFireStar)
                    ExplosionsData = "Explosions:[{" + ExplosionsData.Trim(',') + "}],";
                else
                    ExplosionsData = "Explosion:{" + ExplosionsData.Trim(',') + "},";
            }
            else
                ExplosionsData = "";
            Result = ExplosionsData;
            if (Give)
            {
                if (ExplosionsData.Length > 0)
                    Result += FireworkDurationString;
                Result = Result.Trim(',');
                if (!GeneratorFireStar)
                {
                    if (SelectedVersion.Text == "1.13+")
                        Result = "give @p " + "firework_rocket{Fireworks:{" + Result + "}}";
                    else
                        Result = "give @p " + "firework_rocket" + " 1 0 {Fireworks:{" + Result + "}}";
                }
                else
                {
                    if (SelectedVersion.Text == "1.13+")
                        Result = "give @p " + "firework_star{" + Result + "}";
                    else
                        Result = "give @p " + "firework_star" + " 1 0 {" + Result + "}";
                }
            }
            else
            if (!Give)
            {
                Result = Result.Trim(',');
                if (!GeneratorFireStar)
                {
                    Result = "id:\"minecraft:firework_rocket\",Count:1b,tag:{Fireworks:{" + Result + "}},";
                    Result = "summon item ~ ~ ~ {ItemView:{" + (LifeTimeString + LifeString + FlyAngleString + Result + FireworkDurationString).Trim(',') + "}}";
                }
                else
                {
                    Result = "id:\"minecraft:firework_star\",Count:1b,tag:{" + Result + "},";
                    Result = "summon item ~ ~ ~ {ItemView:{" + Result + "}}";
                }
            }

            if (showResult)
            {
                DisplayerView displayer = _container.Resolve<DisplayerView>();
                if (displayer is not null && displayer.DataContext is DisplayerViewModel displayerViewModel)
                {
                    displayerViewModel.GeneratorResult(Result, "烟花火箭", iconPath);
                }
            }
            else
                Clipboard.SetText(Result);
            return Result;
        }

        /// <summary>
        /// 滚轮缩放色谱视图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Canvas_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                if (e.Delta < 0)
                    ViewScale -= 0.1;
                else
                    ViewScale += 0.1;
                ScaleTransform scaleTransform = new()
                {
                    ScaleX = ViewScale
                };
                Canvas canvas = sender as Canvas;
                ScrollViewer scrollViewer = canvas.Children[0] as ScrollViewer;
                scrollViewer.RenderTransform = scaleTransform;
            }
        }

        /// <summary>
        /// 左击并抬起拾色器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ColorPickers_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (colorpicker.pop.IsOpen && PointMode)
            {
                if (AddInMain)
                {
                    Border border = new()
                    {
                        Width = 25,
                        Background = colorpicker.SelectColor,
                        ToolTip = "右击删除"
                    };
                    ToolTipService.SetBetweenShowDelay(border, 0);
                    ToolTipService.SetInitialShowDelay(border, 0);
                    border.MouseRightButtonUp += DeleteColorMouseRightButtonUp;
                    border.Uid = "Main";
                    MainColorList.Add(border);
                }
                if (AddInFade)
                {
                    Border border = new()
                    {
                        Width = 25,
                        Background = colorpicker.SelectColor,
                        ToolTip = "右击删除"
                    };
                    ToolTipService.SetBetweenShowDelay(border, 0);
                    ToolTipService.SetInitialShowDelay(border, 0);
                    border.MouseRightButtonUp += DeleteColorMouseRightButtonUp;
                    border.Uid = "Fade";
                    FadeColorList.Add(border);
                }
            }
        }

        [RelayCommand]
        /// <summary>
        /// 清空淡出颜色
        /// </summary>
        private void ClearFadeColor(FrameworkElement obj)
        {
            FadeColorList.Clear();
        }

        [RelayCommand]
        /// <summary>
        /// 清空淡入颜色
        /// </summary>
        private void ClearMainColor(FrameworkElement obj)
        {
            MainColorList.Clear();
        }

        [RelayCommand]
        /// <summary>
        /// 反选所有结构色
        /// </summary>
        private void ReverseAllStructureColor(FrameworkElement obj)
        {
            foreach (var item in StructureColorGrid.Children)
            {
                IconCheckBoxs iconCheckBoxs = item as IconCheckBoxs;
                iconCheckBoxs.IsChecked = !iconCheckBoxs.IsChecked.Value;
            }
        }

        [RelayCommand]
        /// <summary>
        /// 全选所有结构色
        /// </summary>
        private void SelectedAllStructureColor(FrameworkElement obj)
        {
            foreach (var item in StructureColorGrid.Children)
            {
                IconCheckBoxs iconCheckBoxs = item as IconCheckBoxs;
                iconCheckBoxs.IsChecked = true;
            }
        }

        /// <summary>
        /// 淡入颜色视图滚动到最右侧
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MainColorItemsControl_SizeChanged(object sender, SizeChangedEventArgs e) => mainScrollViewer.ScrollToHorizontalOffset(mainScrollViewer.ExtentWidth);

        /// <summary>
        /// 淡出颜色视图滚动到最右侧
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void FadeColorItemsControl_SizeChanged(object sender, SizeChangedEventArgs e) => fadeScrollViewer.ScrollToHorizontalOffset(fadeScrollViewer.ExtentWidth);
        #endregion
    }
}