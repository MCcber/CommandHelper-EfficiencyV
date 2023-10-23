using cbhk.CustomControls;
using cbhk.CustomControls.ColorPickers;
using cbhk.GeneralTools.MessageTip;
using cbhk.GenerateResultDisplayer;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
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

namespace cbhk.Generators.FireworkRocketGenerator.Components
{
    public class FireworkRocketPagesDataContext:ObservableObject
    {
        #region 保存和运行等指令
        public RelayCommand Run { get; set; }
        public RelayCommand Save { get; set; }
        public RelayCommand PreviewEffect { get; set; }
        #endregion

        #region 版本
        private string selectedVersion = "";
        public string SelectedVersion
        {
            get => selectedVersion;
            set => SetProperty(ref selectedVersion, value);
        }

        //数据源
        private ObservableCollection<string> VersionSource = new ObservableCollection<string> { "1.12-", "1.13+" };
        #endregion

        #region 生成行为
        private bool give = false;
        public bool Give
        {
            get => give;
            set => SetProperty(ref give,value);
        }
        #endregion

        #region 生成烟花火箭
        private bool generatorFireStar = false;
        public bool GeneratorFireStar
        {
            get
            {
                return generatorFireStar;
            }
            set
            {
                generatorFireStar = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 是否展示生成结果
        private bool showGeneratorResult = false;
        public bool ShowGeneratorResult
        {
            get => showGeneratorResult;
            set => SetProperty(ref showGeneratorResult,value);
        }
        #endregion

        #region 导入模式
        private bool importMode = false;
        public bool ImportMode
        {
            get
            {
                return importMode;
            }
            set
            {
                importMode = value;
            }
        }
        private string externFilePath = "";
        public string ExternFilePath
        {
            get
            {
                return externFilePath;
            }
            set
            {
                externFilePath = value;
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

        #region 闪烁
        public bool Flicker { get; set; } = false;
        #endregion

        #region 拖尾
        public bool Trail { get; set; } = false;
        #endregion

        #region 时长
        private double duration = 1;
        public double Duration
        {
            get { return duration; }
            set
            {
                duration = value;
                OnPropertyChanged();
            }
        }

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

        #region 已飞行刻数
        private double life = 0;
        public double Life
        {
            get
            {
                return life;
            }
            set
            {
                life = value;
                OnPropertyChanged();
            }
        }
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
        private double lifeTime = 20;
        public double LifeTime
        {
            get
            {
                return lifeTime;
            }
            set
            {
                lifeTime = value;
                OnPropertyChanged();
            }
        }
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
                if (MainColors.Count > 0)
                {
                    result += string.Join(',', MainColors.Select(item => Convert.ToUInt64(item.Background.ToString()[2..], 16)));
                    result += "],";
                }
                else
                    result = "";
                return result;
            }
        }
        #endregion

        #region 备选颜色
        private string FadeColorsString
        {
            get
            {
                string result = "FadeColors:[I;";
                if (FadeColors.Count > 0)
                {
                    result += string.Join(',', FadeColors.Select(item => Convert.ToUInt64(item.Background.ToString()[2..], 16)));
                    result += "],";
                }
                else
                    result = "";
                return result;
            }
        }
        #endregion

        #region 按角度飞出
        //按角度飞出
        public bool FlyAngle { get; set; }
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

        #region 清除淡入/淡出
        public RelayCommand<FrameworkElement> ClearMainColor { get; set; }
        public RelayCommand<FrameworkElement> ClearFadeColor { get; set; }
        #endregion

        #region 已选择的形状
        private int selectedShape = 0;
        public int SelectedShape
        {
            get
            {
                return selectedShape;
            }
            set
            {
                selectedShape = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 点/连续(模式切换)
        private bool selectedModeLock = true;
        private bool pointMode = true;
        public bool PointMode
        {
            get
            {
                return pointMode;
            }
            set
            {
                pointMode = value;
                if (selectedModeLock)
                {
                    selectedModeLock = false;
                    ContinuousMode = !pointMode;
                    selectedModeLock = true;
                }
                OnPropertyChanged();
            }
        }

        private bool continuousMode = false;
        public bool ContinuousMode
        {
            get
            {
                return continuousMode;
            }
            set
            {
                continuousMode = value;
                if (selectedModeLock)
                {
                    selectedModeLock = false;
                    PointMode = !continuousMode;
                    selectedModeLock = true;
                }
                OnPropertyChanged();
            }
        }
        #endregion

        #region 已选择颜色
        private SolidColorBrush selectedColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0000"));
        public SolidColorBrush SelectedColor
        {
            get
            {
                return selectedColor;
            }
            set
            {
                selectedColor = value;
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
                        MainColors.Add(border);
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
                        FadeColors.Add(border);
                    }
                }
                OnPropertyChanged();
            }
        }
        #endregion

        #region 最终形状数据
        private string FireworkShapeString
        {
            get
            {
                string result = "Type:" + SelectedShape + ",";
                return result;
            }
        }
        #endregion

        #region 主颜色库
        private ObservableCollection<Border> mainColors = new ObservableCollection<Border> { };
        public ObservableCollection<Border> MainColors
        {
            get
            {
                return mainColors;
            }
            set
            {
                mainColors = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 备选颜色库
        private ObservableCollection<Border> fadeColors = new ObservableCollection<Border> { };
        public ObservableCollection<Border> FadeColors
        {
            get
            {
                return fadeColors;
            }
            set
            {
                fadeColors = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 加入淡入或淡出
        private bool addInMain = true;
        public bool AddInMain
        {
            get
            {
                return addInMain;
            }
            set
            {
                addInMain = value;
                OnPropertyChanged();
            }
        }
        private bool addInFade = false;
        public bool AddInFade
        {
            get
            {
                return addInFade;
            }
            set
            {
                addInFade = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 淡入淡出滚动视图引用
        ScrollViewer mainScrollViewer = null;
        ScrollViewer fadeScrollViewer = null;
        #endregion

        #region 全选和反选原版颜色库
        public RelayCommand<FrameworkElement> SelectedAllStructureColor { get; set; }
        public RelayCommand<FrameworkElement> ReverseAllStructureColor { get; set; }
        #endregion

        #region 滚轮缩放倍率
        double viewScale = 0;
        double ViewScale
        {
            get
            {
                return viewScale;
            }
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

        #region 存储生成结果
        public string Result { get; set; }
        #endregion

        /// <summary>
        /// 形状数据源
        /// </summary>
        public ObservableCollection<string> ShapeList { get; set; } = new ObservableCollection<string> { };
        /// <summary>
        /// 外部数据
        /// </summary>
        public JObject ExternallyReadEntityData { get; set; }
        /// <summary>
        /// 预览效果的粒子集合
        /// </summary>
        public List<ModelVisual3D> Particles { get; set; } = new();
        /// <summary>
        /// 原版颜色库路径
        /// </summary>
        string colorStoragePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\FireworkRocket\\images";
        /// <summary>
        /// 形状路径
        /// </summary>
        string shapePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\FireworkRocket\\data\\shapes.ini";
        /// <summary>
        /// 颜色映射表路径
        /// </summary>
        string colorTablePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\FireworkRocket\\data\\structureColorDictionary.ini";
        /// <summary>
        /// 本生成器的图标路径
        /// </summary>
        string icon_path = "pack://application:,,,/cbhk;component/resources/common/images/spawnerIcons/IconFireworks.png";
        /// <summary>
        /// 原版颜色映射库
        /// </summary>
        private Dictionary<string, string> OriginColorDictionary = new() { };
        /// <summary>
        /// 原版颜色库面板
        /// </summary>
        UniformGrid structureColorGrid = null;
        /// <summary>
        /// 拾色器
        /// </summary>
        ColorPickers colorpicker = null;
        //烟花粒子纹理文件路径
        BitmapImage particleTexture = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\FireworkRocket\\images\\x.png", UriKind.Absolute)) { CacheOption = BitmapCacheOption.OnLoad };
        //烟花粒子尺寸
        double particleScale = 1;
        //烟上升高度倍率
        int fireworkFlyHeight = 10;
        //烟花爆炸持续时间
        double fireworkExplosionDuration = 0.1;
        //烟花爆炸粒子渲染组
        Model3DGroup particleContainer = new();

        public FireworkRocketPagesDataContext()
        {
            #region 连接指令
            Run = new RelayCommand(run_command);
            Save = new RelayCommand(SaveCommand);
            SelectedAllStructureColor = new RelayCommand<FrameworkElement>(SelectedAllStructureColorCommand);
            ReverseAllStructureColor = new RelayCommand<FrameworkElement>(ReverseAllStructureColorCommand);
            ClearMainColor = new RelayCommand<FrameworkElement>(ClearMainColorCommand);
            ClearFadeColor = new RelayCommand<FrameworkElement>(ClearFadeColorCommand);
            PreviewEffect = new RelayCommand(PreviewEffectCommand);
            #endregion
            #region 载入形状数据
            if (ShapeList.Count == 0 && File.Exists(shapePath))
            {
                string[] shapes = File.ReadAllLines(shapePath);
                foreach (string shape in shapes)
                {
                    ShapeList.Add(shape.Substring(shape.LastIndexOf(':') + 1));
                }
            }
            #endregion
        }

        /// <summary>
        /// 载入时读取外部数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void FireworkRocketLoaded(object sender,RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                if (ImportMode)
                {
                    Give = ExternallyReadEntityData["Item"] == null;
                    JObject Explosion = ExternallyReadEntityData.SelectToken("Item.tag.Explosion") as JObject;
                    JArray Explosions = ExternallyReadEntityData.SelectToken("Item.tag.Fireworks.Explosions") as JArray;
                    GeneratorFireStar = Explosion != null;

                    string rootPath = "";
                    if (Give)
                    {
                        if (generatorFireStar)
                            rootPath = "Item.tag";
                        else
                            rootPath = "Item.tag.Fireworks";
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
                    fadeColors = Explosion.SelectToken("FadeColors") as JArray;
                    flight = ExternallyReadEntityData.SelectToken("Item.Flight");
                    flicker = Explosion.SelectToken("Flicker");
                    trail = Explosion.SelectToken("Trail");
                    type = Explosion.SelectToken("Type");
                    life = ExternallyReadEntityData.SelectToken("Item.Life");
                    lifeTime = ExternallyReadEntityData.SelectToken("Item.LifeTime");
                    shotAtAngle = ExternallyReadEntityData.SelectToken("Item.ShotAtAngle");

                    #region 添加主淡入颜色和淡出颜色集合
                    FireworkRocketPages page = sender as FireworkRocketPages;
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
                                MainColors.Add(border);
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
                                FadeColors.Add(border);
                            }
                    });
                    #endregion
                    #region 设置剩余数据
                    if (flight != null)
                        Duration = double.Parse(flight.ToString());
                    if (flicker != null)
                        Flicker = flicker.ToString() == "1" || flicker.ToString().ToLower() == "true";
                    if (trail != null)
                        Trail = trail.ToString() == "1" || trail.ToString().ToLower() == "true";
                    if (type != null)
                        SelectedShape = int.Parse(type.ToString());

                    if (!Give)
                    {
                        if (life != null)
                            Life = double.Parse(life.ToString());
                        if (lifeTime != null)
                            LifeTime = double.Parse(lifeTime.ToString());
                        if (shotAtAngle != null)
                            FlyAngle = shotAtAngle.ToString() == "1" || shotAtAngle.ToString().ToLower() == "true";
                    }
                    #endregion
                }
            });
        }

        /// <summary>
        /// 保存烟花火箭或烟火之星
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void SaveCommand()
        {
            run_command(false);

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
                _ = File.WriteAllTextAsync(AppDomain.CurrentDomain.BaseDirectory + "resources\\saves\\Entity\\" + Path.GetFileName(saveFileDialog.FileName), Result);
            }
        }

        /// <summary>
        /// 预览效果
        /// </summary>
        private void PreviewEffectCommand()
        {
            if (MainColors.Count + FadeColors.Count == 0) return;
            FireworkRocket fireworkRocket = Window.GetWindow(colorpicker) as FireworkRocket;
            FireworkRocketDataContext context = fireworkRocket.DataContext as FireworkRocketDataContext;

            //烟花火箭模型引用
            TranslateTransform3D fireworkModel = fireworkRocket.FireworkModel;
            //获取用户摄像机
            TranslateTransform3D userCamera = fireworkRocket.UserCamera;
            //清除上一个烟花粒子簇
            if (particleContainer.Children.Count > 0)
                particleContainer.Children.Clear();

            #region 初始化动画
            DoubleAnimation cameraAnimation = new()
            {
                From = 10,
                By = fireworkFlyHeight * (Duration <= 0 ? 1 : Duration),
                Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                AutoReverse = false,
                FillBehavior = FillBehavior.HoldEnd
            };
            DoubleAnimation fireworkRocketAnimation = new()
            {
                From = 1,
                By = fireworkFlyHeight * (Duration <= 0 ? 1 : Duration),
                Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                AutoReverse = false,
                FillBehavior = FillBehavior.Stop
            };
            #endregion

            #region 执行动画
            fireworkRocketAnimation.Completed += FireworkExploded;
            userCamera.BeginAnimation(TranslateTransform3D.OffsetYProperty, cameraAnimation);
            fireworkModel.BeginAnimation(TranslateTransform3D.OffsetYProperty, fireworkRocketAnimation);
            #endregion
        }

        /// <summary>
        /// 执行烟花爆炸
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void FireworkExploded(object sender, EventArgs e)
        {
            FireworkRocket fireworkRocket = Window.GetWindow(colorpicker) as FireworkRocket;
            FireworkRocketDataContext context = fireworkRocket.DataContext as FireworkRocketDataContext;
            await fireworkRocket.Dispatcher.InvokeAsync(() =>
            {
                #region 获取相关引用、实例化网格模型
                //模型父容器
                ModelVisual3D modelVisual3D = new()
                {
                    Content = particleContainer
                };
                //模型顶点、顶点索引、纹理uv坐标数据
                MeshGeometry3D meshGeometry3D = new()
                {
                    Positions = new Point3DCollection(new List<Point3D> { new Point3D(0, 0, 0), new Point3D(particleScale, 0, 0), new Point3D(particleScale, particleScale, 0), new Point3D(0, particleScale, 0) }),
                    TextureCoordinates = new PointCollection(new List<Point> { new Point(0, 1), new Point(1, 1), new Point(1, 0), new Point(0, 0) }),
                    TriangleIndices = new Int32Collection(new List<int> { 0, 1, 2, 0, 2, 3 }) 
                };

                PerspectiveCamera userCamera = context.viewport3D.Camera as PerspectiveCamera;
                TranslateTransform3D cameraTransform3D = userCamera.Transform as TranslateTransform3D;
                Random random = new();
                #endregion

                for (int i = 0; i < 350; i++)
                {
                    //实例化变换属性
                    Vector3D randomPos = context.GetSphereRandom(new float[] { (float)random.NextDouble(), (float)random.NextDouble() }, 1);
                    //实例化纹理笔刷
                    ImageBrush imageBrush = new();

                    #region 初始化设置
                    //设置为点过滤模式，让像素纹理高清
                    RenderOptions.SetBitmapScalingMode(imageBrush, BitmapScalingMode.HighQuality);
                    //开启抗锯齿
                    RenderOptions.SetEdgeMode(imageBrush, EdgeMode.Aliased);
                    #endregion

                    #region 构造法线信息，执行光反射，让纹理深度信息更清晰
                    // Calculate the normal vectors for each vertex
                    var normals = new Vector3DCollection();
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
                        OffsetY = 1 + fireworkFlyHeight * (Duration <= 0 ? 1 : Duration),
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
                        imageBrush.ImageSource = ColoringBitmapImage(particleTexture, new SolidColorBrush((Color)ColorConverter.ConvertFromString(MainColors[random.Next(0,MainColors.Count - 1)].Background.ToString())));
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
                        From = 1 + fireworkFlyHeight * (Duration <= 0 ? 1 : Duration),
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
                    particleContainer.Children.Add(geometryModel3D);
                    translateTransform3D.BeginAnimation(TranslateTransform3D.OffsetXProperty, posXAnimation);
                    translateTransform3D.BeginAnimation(TranslateTransform3D.OffsetYProperty, posYAnimation);
                    translateTransform3D.BeginAnimation(TranslateTransform3D.OffsetZProperty, posZAnimation);
                    #endregion
                }

                if (!context.viewport3D.Children.Contains(modelVisual3D))
                context.viewport3D.Children.Add(modelVisual3D);
            });
            if (FadeColors.Count > 0)
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
            //粒子颜色迭代索引
            //int particleColorIndex = 0;
            Random random = new();
            for (int i = 0; i < particleContainer.Children.Count; i++)
            {
                #region 处理烟花粒子缓降动画
                TranslateTransform3D translateTransform3D = (particleContainer.Children[i].Transform as Transform3DGroup).Children[^1] as TranslateTransform3D;
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
                    ImageSource = ColoringBitmapImage(particleTexture, new SolidColorBrush((Color)ColorConverter.ConvertFromString(FadeColors[random.Next(0, MainColors.Count - 1)].Background.ToString())))
                };
                (particleContainer.Children[i] as GeometryModel3D).Material = new DiffuseMaterial() { Brush = imageBrush };
                translateTransform3D.BeginAnimation(TranslateTransform3D.OffsetYProperty, posYAnimation);
                #endregion
            }
        }

        /// <summary>
        /// 执行生成
        /// </summary>
        private void run_command()
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
                    if (SelectedVersion == "1.13+")
                        Result = "give @p " + "firework_rocket{Fireworks:{" + Result + "}}";
                    else
                        Result = "give @p " + "firework_rocket" + " 1 0 {Fireworks:{" + Result + "}}";
                }
                else
                {
                    if (SelectedVersion == "1.13+")
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
                    Result = "summon item ~ ~ ~ {Item:{" + (LifeTimeString + LifeString + FlyAngleString + Result + FireworkDurationString).Trim(',') + "}}";
                }
                else
                {
                    Result = "id:\"minecraft:firework_star\",Count:1b,tag:{" + Result + "},";
                    Result = "summon item ~ ~ ~ {Item:{" + Result + "}}";
                }
            }

            if (ShowGeneratorResult)
            {
                Displayer displayer = Displayer.GetContentDisplayer();
                displayer.GeneratorResult(Result, "烟花火箭", icon_path);
                displayer.Show();
            }
            else
            {
                Clipboard.SetText(Result);
                Message.PushMessage("烟花生成成功！数据已进入剪切板", MessageBoxImage.Information);
            }
        }

        public string run_command(bool showResult)
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
                    if (SelectedVersion == "1.13+")
                        Result = "give @p " + "firework_rocket{Fireworks:{" + Result + "}}";
                    else
                        Result = "give @p " + "firework_rocket" + " 1 0 {Fireworks:{" + Result + "}}";
                }
                else
                {
                    if (SelectedVersion == "1.13+")
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
                    Result = "summon item ~ ~ ~ {Item:{" + (LifeTimeString + LifeString + FlyAngleString + Result + FireworkDurationString).Trim(',') + "}}";
                }
                else
                {
                    Result = "id:\"minecraft:firework_star\",Count:1b,tag:{" + Result + "},";
                    Result = "summon item ~ ~ ~ {Item:{" + Result + "}}";
                }
            }

            if (showResult)
            {
                Displayer displayer = Displayer.GetContentDisplayer();
                displayer.GeneratorResult(Result, "烟花火箭", icon_path);
                displayer.Show();
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
                ScaleTransform scaleTransform = new ScaleTransform
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
                    MainColors.Add(border);
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
                    FadeColors.Add(border);
                }
            }
        }

        /// <summary>
        /// 清空淡出颜色
        /// </summary>
        private void ClearFadeColorCommand(FrameworkElement obj)
        {
            FadeColors.Clear();
        }

        /// <summary>
        /// 清空淡入颜色
        /// </summary>
        private void ClearMainColorCommand(FrameworkElement obj)
        {
            MainColors.Clear();
        }

        /// <summary>
        /// 反选所有结构色
        /// </summary>
        private void ReverseAllStructureColorCommand(FrameworkElement obj)
        {
            foreach (var item in structureColorGrid.Children)
            {
                IconCheckBoxs iconCheckBoxs = item as IconCheckBoxs;
                iconCheckBoxs.IsChecked = !iconCheckBoxs.IsChecked.Value;
            }
        }

        /// <summary>
        /// 全选所有结构色
        /// </summary>
        private void SelectedAllStructureColorCommand(FrameworkElement obj)
        {
            foreach (var item in structureColorGrid.Children)
            {
                IconCheckBoxs iconCheckBoxs = item as IconCheckBoxs;
                iconCheckBoxs.IsChecked = true;
            }
        }

        /// <summary>
        /// 载入原版颜色库
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void StructureColorList_Loaded(object sender, RoutedEventArgs e)
        {
            if (structureColorGrid != null) return;
            ScrollViewer scrollViewer = sender as ScrollViewer;
            structureColorGrid = scrollViewer.Content as UniformGrid;

            string[] colorArray = Directory.GetFiles(colorStoragePath);
            string[] colorTable = File.ReadAllLines(colorTablePath);
            foreach (var item in colorArray)
            {
                if (item.Contains("dye"))
                {
                    string colorName = Path.GetFileNameWithoutExtension(item);
                    colorName = colorName.Substring(0, colorName.LastIndexOf('_'));
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
                    structureColorGrid.Children.Add(iconCheckBoxs);
                }
            }

            string colorID = "";
            string colorString = "";
            foreach (var item in colorTable)
            {
                colorID = item.Split(':')[0];
                colorString = item.Split(':')[1];
                if(!OriginColorDictionary.ContainsKey(colorID))
                OriginColorDictionary.Add(colorID, colorString);
            }
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
            if (AddInMain)
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
                MainColors.Add(border);
            }
            if(AddInFade)
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
                FadeColors.Add(border);
            }

        }

        /// <summary>
        /// 淡入颜色视图滚动到最右侧
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MainColorItemsControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            mainScrollViewer.ScrollToHorizontalOffset(mainScrollViewer.ExtentWidth);
        }

        /// <summary>
        /// 淡出颜色视图滚动到最右侧
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void FadeColorItemsControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            fadeScrollViewer.ScrollToHorizontalOffset(fadeScrollViewer.ExtentWidth);
        }

        /// <summary>
        /// 右击删除指定颜色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteColorMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Border border = sender as Border;
            if (border.Uid == "Main")
                MainColors.Remove(border);
            else
                FadeColors.Remove(border);
        }

        /// <summary>
        /// 载入淡入颜色滚动视图引用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MainColorGridScrollViewerLoaded(object sender, RoutedEventArgs e)
        {
            mainScrollViewer = sender as ScrollViewer;
        }

        /// <summary>
        /// 载入淡出颜色滚动视图引用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void FadeColorGridScrollViewerLoaded(object sender, RoutedEventArgs e)
        {
            fadeScrollViewer = sender as ScrollViewer;
        }

        /// <summary>
        /// 为拾色器的矩形选择面板订阅左击抬起事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ColorPickerLoaded(object sender, RoutedEventArgs e)
        {
            colorpicker = sender as ColorPickers;
            colorpicker.rectColorGrid.PreviewMouseLeftButtonUp += ColorPickers_PreviewMouseLeftButtonUp;
        }

        /// <summary>
        /// 载入版本
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void VersionLoaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            comboBox.ItemsSource = VersionSource;
        }

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
    }
}