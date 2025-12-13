using CBHK.CustomControl;
using CBHK.Domain;
using CBHK.Interface;
using CBHK.Model.Common;
using CBHK.Model.Generator.Entity;
using CBHK.Utility.Common;
using CBHK.Utility.MessageTip;
using CBHK.View;
using CBHK.View.Component.Entity;
using CBHK.View.Generator;
using CBHK.ViewModel.Generator;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using Prism.Events;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CBHK.ViewModel.Component.Entity
{
    public partial class EntityPageViewModel : ObservableObject,IGenerator
    {
        #region Field
        public bool HaveCustomName = false;
        public int CurrentMinVersion = 1202;
        private string iconPath = "pack://application:,,,/CBHK;component/Resource/Common/Image/SpawnerIcon/IconEntities.png";
        private string buttonNormalImage = "pack://application:,,,/CBHK;component/Resource/Common/Image/ButtonNormal.png";
        private string buttonPressedImage = "pack://application:,,,/CBHK;component/Resource/Common/Image/ButtonPressed.png";
        private ImageBrush buttonNormalBrush;
        private ImageBrush buttonPressedBrush;
        private UpdateEntityComponentVersionEvent versionUpdateEvent;
        private string NBTStructureFolderPath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Entity\Data\";
        private string CommonNBTStructureFolderPath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Entity\Data\Common";
        private string DependencyNBTStructureFolderPath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Entity\Data\Dependency";
        //专属结果集合
        public Dictionary<string, ObservableCollection<NBTDataStructure>> SpecialTagsResult { get; set; } = [];
        /// <summary>
        /// 在生成时标记当前实体拥有哪些共通标签
        /// </summary>
        private List<string> CurrentCommonTags = [];
        /// <summary>
        /// 白色画刷
        /// </summary>
        SolidColorBrush whiteBrush = new((Color)ColorConverter.ConvertFromString("#FFFFFF"));
        /// <summary>
        /// 黑色画刷
        /// </summary>
        SolidColorBrush blackBrush = new((Color)ColorConverter.ConvertFromString("#000000"));
        /// <summary>
        /// 橙色画刷
        /// </summary>
        SolidColorBrush orangeBrush = new((Color)ColorConverter.ConvertFromString("#FFE5B663"));
        private IContainerProvider container;
        private DataService dataService;
        private IEventAggregator eventAggregator;
        private CBHKDataContext context = null;
        /// <summary>
        /// 当前实体页引用
        /// </summary>
        private EntityPageView currentEntityPage = null;
        /// <summary>
        /// 无专属NBT时提示
        /// </summary>
        VisualBrush emptyDataTip = new()
        {
            AlignmentX = AlignmentX.Center,
            AlignmentY = AlignmentY.Center,
            Stretch = Stretch.None
        };

        private Style textCheckBoxStyle = Application.Current.Resources["TextCheckBox"] as Style;
        private Style RadiusToggleButtonStyle = Application.Current.Resources["RadiusToggleButton"] as Style;
        private Style NumberBoxStyle = Application.Current.Resources["NumberBoxStyle"] as Style;
        private Style AccordionStyle = Application.Current.Resources["AccordionStyle"] as Style;
        
        /// <summary>
        /// 缓存面板
        /// </summary>
        private Grid CacheGrid = null;

        private string SpecialNBTStructureFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Entity\Data\SpecialTags.json";

        /// <summary>
        /// 特殊实体的共通标签链表
        /// </summary>
        List<string> specialEntityCommonTagList = [];
        /// <summary>
        /// 特殊实体专属标签字典,用于动态切换内容
        /// </summary>
        public Dictionary<string, Grid> SpecialDataDictionary = [];
        /// <summary>
        /// 特殊标签面板
        /// </summary>
        ScrollViewer SpecialViewer = null;

        #endregion

        #region Property
        /// <summary>
        /// 指示是否需要展示生成结果
        /// </summary>
        [ObservableProperty]
        private bool _showResult = false;
        /// <summary>
        /// 生成方式
        /// </summary>
        [ObservableProperty]
        private bool _give = false;
        /// <summary>
        /// 实体ID
        /// </summary>
        [ObservableProperty]
        private IconComboBoxItem _selectedEntityId = null;
        /// <summary>
        /// 实体数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<IconComboBoxItem> _entityIDList = [];

        /// <summary>
        /// 属性数据
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<NBTDataStructure> _attributeResult = [];
        /// <summary>
        /// 存储当前实体的乘客
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<NBTDataStructure> _passengerResult = [];

        /// <summary>
        /// 实体、活体、生物结果集合
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<NBTDataStructure> _commonResult = [];
        //存储外部读取进来的实体数据
        public JObject ExternallyReadEntityData { get; set; } = null;

        /// <summary>
        /// 标记当前是否为导入外部数据的模式
        /// </summary>
        [ObservableProperty]
        private bool _importMode = false;

        /// <summary>
        /// 获取实体英文id
        /// </summary>
        /// <returns></returns>
        [GeneratedRegex("[a-zA-Z_]+")]
        private partial Regex GetEntityID();

        /// <summary>
        /// 版本数据源
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<TextComboBoxItem> _versionSource = [];

        /// <summary>
        /// 已选版本
        /// </summary>
        [ObservableProperty]
        private TextComboBoxItem _selectedVersion;

        #region 作为工具或引用
        [ObservableProperty]
        public bool _useForTool = false;
        [ObservableProperty]
        public bool _useForReference = false;
        #endregion

        #region 通用NBT

        /// <summary>
        /// 允许编辑实体共通标签
        /// </summary>
        [ObservableProperty]
        private bool _entityCommonTagsEnabled = true;
        [ObservableProperty]
        /// <summary>
        /// 允许编辑活体共通标签
        /// </summary>
        private bool _livingBodyCommonTagsEnabled = true;
        [ObservableProperty]
        /// <summary>
        /// 允许编辑活体共通标签
        /// </summary>
        private bool _mobCommonTagsEnabled = true;
        /// <summary>
        /// 实体共通标签可见性
        /// </summary>
        [ObservableProperty]
        private Visibility _entityCommonTagsVisibility = Visibility.Visible;
        /// <summary>
        /// 活体共通标签可见性
        /// </summary>
        [ObservableProperty]
        private Visibility _livingBodyCommonTagsVisibility = Visibility.Visible;
        /// <summary>
        /// 生物共通标签可见性
        /// </summary>
        [ObservableProperty]
        private Visibility _mobCommonTagsVisibility = Visibility.Visible;
        /// <summary>
        /// 可愤怒生物额外字段可见性
        /// </summary>
        [ObservableProperty]
        private Visibility _angryCreatureExtraFieldVisibility = Visibility.Collapsed;
        /// <summary>
        /// 可繁殖生物额外字段可见性
        /// </summary>
        [ObservableProperty]
        private Visibility _breedableMobExtraFieldsVisibility = Visibility.Collapsed;
        /// <summary>
        /// 可在袭击中生成的生物共通标签可见性
        /// </summary>
        [ObservableProperty]
        private Visibility _commonTagsForMobsSpawnedInRaidsVisibility = Visibility.Collapsed;
        /// <summary>
        /// 可骑乘生物共通标签可见性
        /// </summary>
        [ObservableProperty]
        private Visibility _commonTagsForRideableEntitiesVisibility = Visibility.Collapsed;
        /// <summary>
        /// 僵尸类生物共通标签可见性
        /// </summary>
        [ObservableProperty]
        private Visibility _commonTagsForZombiesVisibility = Visibility.Collapsed;

        /// <summary>
        /// 可驯服生物共通标签可见性
        /// </summary>
        [ObservableProperty]
        private Visibility _tameableMobExtraFieldsVisibility = Visibility.Collapsed;

        /// <summary>
        /// 箭类投掷物共通标签可见性
        /// </summary>
        [ObservableProperty]
        private Visibility _arrowProjectileCommonTagsVisibility = Visibility.Collapsed;

        /// <summary>
        /// 容器实体共通标签可见性
        /// </summary>
        [ObservableProperty]
        private Visibility _containerEntityCommonTagsVisibility = Visibility.Collapsed;

        /// <summary>
        /// 火球类投掷物共通标签可见性
        /// </summary>
        [ObservableProperty]
        private Visibility _fireballProjectileCommonTagsVisibility = Visibility.Collapsed;

        /// <summary>
        /// 物品类投掷物共通标签可见性
        /// </summary>
        [ObservableProperty]
        private Visibility _itemProjectileCommonTagsVisibility = Visibility.Collapsed;

        /// <summary>
        /// 矿车共通标签可见性
        /// </summary>
        [ObservableProperty]
        private Visibility _mineCartCommonTagsVisibility = Visibility.Collapsed;

        /// <summary>
        /// 药水效果共通标签可见性
        /// </summary>
        [ObservableProperty]
        private Visibility _potionEffectCommonTagsVisibility = Visibility.Collapsed;

        /// <summary>
        /// 投掷物共通标签可见性
        /// </summary>
        [ObservableProperty]
        private Visibility _projectileCommonTagsVisibility = Visibility.Collapsed;

        #endregion

        #region 是否同步到文件
        public bool SyncToFile { get; set; }
        public string ExternFilePath { get; set; }
        public IEventAggregator EventAggregator { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        string IGenerator.SelectedVersion { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public JToken ExternallyData { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        #endregion

        #endregion

        #region Method
        public EntityPageViewModel(IContainerProvider Container,CBHKDataContext Context,IEventAggregator EventAggregator,DataService DataService)
        {
            context = Context;
            container = Container;
            eventAggregator = EventAggregator;
            dataService = DataService;
            buttonNormalBrush = new ImageBrush(new BitmapImage(new Uri(buttonNormalImage, UriKind.RelativeOrAbsolute)));
            Grid contentGrid = new();
            Binding widthBinding = new()
            {
                Path = new PropertyPath("ActualWidth"),
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor,typeof(ScrollViewer),1)
            };
            Binding heightBinding = new()
            {
                Path = new PropertyPath("ActualHeight"),
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ScrollViewer), 1)
            };
            BindingOperations.SetBinding(contentGrid, FrameworkElement.WidthProperty, widthBinding);
            BindingOperations.SetBinding(contentGrid, FrameworkElement.HeightProperty, heightBinding);
            TextBlock emptyTip = new()
            {
                Text = "此实体无专属NBT",
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 15,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D6D6D6")),
                TextAlignment = TextAlignment.Center
            };
            contentGrid.Children.Add(emptyTip);
            emptyDataTip.Visual = contentGrid;
            buttonPressedBrush = new ImageBrush(new BitmapImage(new Uri(buttonPressedImage, UriKind.RelativeOrAbsolute)));

            versionUpdateEvent = eventAggregator.GetEvent<UpdateEntityComponentVersionEvent>();
        }

        /// <summary>
        /// 更新控件集合
        /// </summary>
        /// <param name="RawDataArray">数据列表</param>
        /// <param name="container">目标容器</param>
        private void UpdateSpecialNBTPage(JArray RawDataArray, Grid container)
        {
            List<string> sortOrder = ["EntityCommonTags", "LivingBodyCommonTags", "MobCommonTags"];
            //currentResult.Sort((x, y) => sortOrder.IndexOf(x.Uid).CompareTo(sortOrder.IndexOf(y.Uid)));
            foreach (var nbt in RawDataArray)
            {
                if(nbt is null)
                {
                    continue;
                }

                container.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });

                string tag = JArray.Parse(nbt["Tag"].ToString())[0].ToString().Replace("TAG_","");
                string key = nbt["Key"].ToString();
                JToken displayToken = nbt["Display"];
                string display = displayToken is not null ? displayToken.ToString() : "";
                JArray branch = nbt["Branch"] as JArray;
                JToken descriptionObj = nbt["Description"];
                string description = descriptionObj is not null ? descriptionObj.ToString() : "";
                JToken toolTipObj = nbt["PrelimToolTip"];
                string toolTip = toolTipObj is not null ? toolTipObj.ToString() : "";
                JToken dependencyObj = nbt["Dependency"];
                string dependency = dependencyObj is not null ? dependencyObj.ToString() : "";
                JToken referenceObj = nbt["Reference"];
                string reference = referenceObj is not null ? referenceObj.ToString() : "";

                TextBlock displayText = null;
                Accordion accordion = null;
                if(tag != "Array" && tag != "List" && tag != "Compound")
                {
                    displayText = new()
                    {
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    if (display is not null)
                    {
                        displayText.Text = display;
                    }
                    else
                    {
                        displayText.Text = string.Join(' ', key.Split('_').Select(item => item[0].ToString().ToUpper() + item[1..]));
                    }
                }
                else
                {
                    accordion = new()
                    {
                        Header = display
                    };
                }

                switch (tag)
                {
                    case "Boolean":
                        {
                            TextCheckBoxs textCheckBoxs = new()
                            {
                                Style = textCheckBoxStyle
                            };
                            break;
                        }
                    case "Short":
                    case "Int":
                    case "Float":
                    case "Double":
                    case "Long":
                        {
                            JArray rangeArray = nbt["Range"] as JArray;
                            break;
                        }
                    case "String":
                        {
                            break;
                        }
                    case "Compound":
                    case "Array":
                    case "List":
                        {
                            JArray children = nbt["Children"] as JArray;
                            break;
                        }
                }
            }
        }

        /// <summary>
        /// 根据当前切换的实体ID来动态切换UI元素的显隐
        /// </summary>
        public async Task UpdateUILayOut(Task task = null)
        {
            SelectedEntityId ??= EntityIDList[0];
            string data = File.ReadAllText(SpecialNBTStructureFilePath);
            JArray specialDataArray = JArray.Parse(data);
            //更新标签页显示文本
            if (currentEntityPage.Parent is TabItem currentTab)
            {
                currentTab.Header = SelectedEntityId.ComboBoxItemId + ":" + SelectedEntityId.ComboBoxItemText;
            }

            #region 搜索当前实体ID对应的JSON对象
            JToken targetEntityObject = specialDataArray.First(item =>
            {
                return item["Type"] is not null && item["Type"].ToString() == SelectedEntityId.ComboBoxItemId;
            });
            #endregion

            if (targetEntityObject is not null)
            {
                JArray commonTags = JArray.Parse(targetEntityObject["Common"].ToString());
                List<string> commonTagList = commonTags.ToList().ConvertAll(item => item.ToString());
                //计算本次与上次共通标签的差集,关闭指定菜单，而不是全部关闭再依次判断打开
                List<string> closedCommonTagList = specialEntityCommonTagList.Except(commonTagList).ToList();

                #region 处理专属NBT
                if (!SpecialDataDictionary.TryGetValue(SelectedEntityId.ComboBoxItemId, out Grid newGrid))
                {
                    JArray children = JArray.Parse(targetEntityObject["Children"].ToString());
                    CacheGrid = new();
                    CacheGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
                    CacheGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                    UpdateSpecialNBTPage(children, CacheGrid);
                }
                else
                {
                    if (newGrid.Children.Count == 0)
                        newGrid.Background = emptyDataTip;
                    if (SpecialViewer is not null)
                        SpecialViewer.Content = newGrid;
                }
                #endregion

                #region 处理额外字段与共通标签的显示隐藏
                Type currentClassType = GetType();

                #region 需要隐藏的共通标签
                TabControl tabControl = (SpecialViewer?.Parent as TextTabItems).Parent as TabControl;
                foreach (var item in closedCommonTagList)
                {
                    PropertyInfo visibilityPropertyInfo = currentClassType.GetProperty(item + "Visibility");
                    if (visibilityPropertyInfo is not null)
                    {
                        object visibility = Convert.ChangeType(Visibility.Collapsed, visibilityPropertyInfo.PropertyType);
                        currentClassType.GetProperty(item + "Visibility")?.SetValue(this, visibility, null);
                    }
                    PropertyInfo enabledPropertyInfo = currentClassType.GetProperty(item + "Enabled");
                    if (enabledPropertyInfo is not null)
                    {
                        object enable = Convert.ChangeType(false, enabledPropertyInfo.PropertyType);
                        currentClassType.GetProperty(item + "Enabled")?.SetValue(this, enable, null);
                    }
                }
                if (closedCommonTagList.Count == 0)
                {
                    for (int i = 6; i < tabControl.Items.Count; i++)
                        (tabControl.Items[i] as FrameworkElement).Visibility = Visibility.Collapsed;
                }
                #endregion

                #region 需要显示的共通标签
                foreach (var item in commonTagList)
                {
                    PropertyInfo visibilityPropertyInfo = currentClassType.GetProperty(item + "Visibility");
                    if (visibilityPropertyInfo is not null)
                    {
                        object visibility = Convert.ChangeType(Visibility.Visible, visibilityPropertyInfo.PropertyType);
                        if (currentClassType.GetProperty(item + "Visibility") is PropertyInfo propertyInfo)
                            propertyInfo.SetValue(this, visibility, null);
                    }
                    PropertyInfo enabledPropertyInfo = currentClassType.GetProperty(item + "Enabled");
                    if (enabledPropertyInfo is not null)
                    {
                        object enable = Convert.ChangeType(true, enabledPropertyInfo.PropertyType);
                        if (currentClassType.GetProperty(item + "Enabled") is PropertyInfo propertyInfo)
                            propertyInfo.SetValue(this, enable, null);
                    }
                }
                #endregion

                #region 同步本次计算后的特殊实体共通标签链表
                if (specialEntityCommonTagList.Count > 0)
                    specialEntityCommonTagList.Clear();
                specialEntityCommonTagList.AddRange(commonTagList);
                #endregion

                #endregion
            }
        }
        #endregion

        #region Event
        /// <summary>
        /// 载入实体页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void EntityPages_Loaded(object sender,RoutedEventArgs e)
        {
            currentEntityPage = sender as EntityPageView;
            EntityViewModel entityDataContext = Window.GetWindow(currentEntityPage).DataContext as EntityViewModel;
            VersionSource = entityDataContext.VersionSource;
            string entityImageFolderPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
            List<string> currentEntityIDList = [..context.EntitySet.SelectMany(item =>
            {
                if(VersionComparer.IsInRange(item.Key, CurrentMinVersion.ToString()))
                {
                    return item.Value;
                }
                return [];
            })];
            foreach (var item in currentEntityIDList)
            {
                #region 设置实体图标、名称和ID
                if (item is not null)
                {
                    string iconPath = File.Exists(entityImageFolderPath + item + "_spawn_egg.png") ? entityImageFolderPath + item + "_spawn_egg.png" : entityImageFolderPath + item + ".png";
                    IconComboBoxItem iconComboBoxItem = new()
                    {
                        ComboBoxItemIcon = File.Exists(iconPath) ? new BitmapImage(new Uri(iconPath, UriKind.Absolute)) : null,
                        ComboBoxItemText = item ?? "",
                        ComboBoxItemId = item
                    };
                    EntityIDList.Add(iconComboBoxItem);
                }
                #endregion
            }
        }

        /// <summary>
        /// 实体ID更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void EntityIDBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //更新专属和共通标签、额外字段
            Task.Run(async () =>
            {
                await currentEntityPage.Dispatcher.InvokeAsync(async () =>
                {
                    await UpdateUILayOut();
                });
            });
        }

        /// <summary>
        /// 版本更新事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Version_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedVersion is not null)
            {
                CurrentMinVersion = int.Parse(SelectedVersion.Text.Replace(".", "").Replace("+", "").Split('-')[0]);
            }

            //处理版本控件的数据更新
            versionUpdateEvent.Publish(SelectedVersion.Text);

            #region 处理版本实体ID的更新
            EntityIDList.Clear();
            foreach (var pair in dataService.GetEntityIDAndNameGroupByVersionMap())
            {
                if(CurrentMinVersion >= pair.Key)
                {
                    EntityIDList.AddRange(pair.Value.Select(item => new IconComboBoxItem() { ComboBoxItemId = item.Key, ComboBoxItemText = item.Value }));
                }
            }
            SelectedEntityId = EntityIDList[0];
            #endregion
        }

        [RelayCommand]
        /// <summary>
        /// 运行保存
        /// </summary>
        private void Save()
        {
            //执行生成
            Microsoft.Win32.SaveFileDialog saveFileDialog = new()
            {
                AddExtension = true,
                RestoreDirectory = true,
                CheckPathExists = true,
                DefaultExt = ".command",
                Filter = "Command files (*.command;)|*.command;",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Title = "保存为命令文件"
            };
            if (saveFileDialog.ShowDialog().Value)
            {
                StringBuilder Result = new();
                Create();
                CollectionData(Result);
                Build(Result);

                if (Directory.Exists(Path.GetDirectoryName(saveFileDialog.FileName)))
                {
                    _ = File.WriteAllTextAsync(saveFileDialog.FileName, Result.ToString());
                }
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Saves\Entity\");
                {
                    _ = File.WriteAllTextAsync(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Saves\Entity\" + Path.GetFileName(saveFileDialog.FileName), Result.ToString());
                }
            }
        }

        [RelayCommand]
        /// <summary>
        /// 运行生成
        /// </summary>
        private void Run()
        {
            StringBuilder Result = Create();
            CollectionData(Result);
            Build(Result);
            if (ShowResult)
            {
                DisplayerView displayer = container.Resolve<DisplayerView>();
                if (displayer is not null && displayer.DataContext is DisplayerViewModel displayerViewModel)
                {
                    displayer.Show();
                    displayerViewModel.GeneratorResult(Result.ToString(), "实体", iconPath);
                }
            }
            else
            {
                Clipboard.SetText(Result.ToString());
                Message.PushMessage("实体生成成功！数据已复制",MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 根据菜单UID分配可见性绑定器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void AccordionVisibilitylLoaded(object sender, RoutedEventArgs e)
        {
            Accordion accordion = sender as Accordion;
            if(accordion.ModifyVisibility == Visibility.Collapsed)
            {
                accordion.ModifyVisibility = Visibility.Hidden;
                accordion.ModifyName = "";
            }
            Binding visibilityBinder = new()
            {
                Path = new PropertyPath(accordion.Uid + "Visibility"),
                Mode = BindingMode.OneWay
            };
            BindingOperations.SetBinding(accordion, UIElement.VisibilityProperty, visibilityBinder);
        }

        /// <summary>
        /// 加入专属数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SpecialTagsPanelLoaded(object sender, RoutedEventArgs e)
        {
            TabControl tabControl = sender as TabControl;
            foreach (TabItem item in tabControl.Items)
                item.DataContext = this;
            SpecialViewer = (tabControl.Items[0] as TabItem).Content as ScrollViewer;
            if (SpecialViewer.Content is Grid grid && grid.Children.Count == 0)
                SpecialViewer.Content = CacheGrid;
        }

        /// <summary>
        /// 切换专属或共通标签顶级页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TagsTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabControl tabControl = sender as TabControl;
            if (tabControl.SelectedItem is not TextTabItems textTabItem) return;
            string currentUID = textTabItem.Uid;
            ScrollViewer tabContent = textTabItem.Content as ScrollViewer;
            Grid subGrid = tabContent.Content as Grid;
            if (subGrid.Children.Count > 0) return;
            Grid newGrid = new();
            newGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            newGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            //分辨当前是实体、生物还是活体,都不是则为其它共通标签
            bool IsSubContainer = tabControl.SelectedIndex > 5;
            //检查实体、活体、生物三大共通标签是否都被添加
            List<string> AddedCommonTags = [];
            for (int i = 0; i < subGrid.Children.Count; i++)
            {
                FrameworkElement frameworkElement = subGrid.Children[i] as FrameworkElement;
                if (frameworkElement.Tag is NBTDataStructure dataStructure && !AddedCommonTags.Contains(dataStructure.NBTGroup))
                    AddedCommonTags.Add(dataStructure.NBTGroup);
            }
            if ((currentUID == "string" || currentUID == "number" || currentUID == "boolean" || currentUID == "list" || currentUID == "compound") && AddedCommonTags.Count >= 3)
            {
                return;
            }

            #region 搜索当前实体ID对应的JSON对象
            string data = File.ReadAllText(SpecialNBTStructureFilePath);
            JArray array = JArray.Parse(data);
            JToken targetObj = array.First(item =>
            {
                return item["Type"].ToString() == SelectedEntityId.ComboBoxItemId;
            });
            string type = targetObj["Type"].ToString();
            string commonTagData = targetObj["Common"].ToString();
            JArray commonTagArray = JArray.Parse(commonTagData);
            #endregion


            #region 处理共通标签
            if(IsSubContainer)
            {
                UpdateSpecialNBTPage(commonTagArray, null);
            }
            #endregion
        }

        [RelayCommand]
        /// <summary>
        /// 反选所有bool型NBT
        /// </summary>
        /// <param name="ele"></param>
        private void ReverseAllBoolNBTs(FrameworkElement ele)
        {
            Accordion accordion = ele as Accordion;
            ScrollViewer scrollViewer = accordion.Content as ScrollViewer;
            Grid grid = scrollViewer.Content as Grid;
            foreach (var item in grid.Children)
            {
                if (item is TextCheckBoxs)
                {
                    TextCheckBoxs textCheckBoxs = item as TextCheckBoxs;
                    textCheckBoxs.Focus();
                    textCheckBoxs.IsChecked = !textCheckBoxs.IsChecked.Value;
                }
            }
        }

        [RelayCommand]
        /// <summary>
        /// 全选所有bool型NBT
        /// </summary>
        private void SelectAllBoolNBTs(FrameworkElement ele)
        {
            Accordion accordion = ele as Accordion;
            ScrollViewer scrollViewer = accordion.Content as ScrollViewer;
            Grid grid = scrollViewer.Content as Grid;
            foreach (var item in grid.Children)
            {
                if(item is TextCheckBoxs)
                {
                    TextCheckBoxs textCheckBoxs = item as TextCheckBoxs;
                    textCheckBoxs.Focus();
                    textCheckBoxs.IsChecked = true;
                }
            }
        }

        public StringBuilder Create()
        {
            StringBuilder Result = new();
            return Result;
        }

        public void CollectionData(StringBuilder Result)
        {
            string data = File.ReadAllText(SpecialNBTStructureFilePath);
            JArray array = JArray.Parse(data);
            List<JToken> result = [.. array.Where(item =>
            {
                JObject currentObj = item as JObject;
                if (currentObj["Type"].ToString() == SelectedEntityId.ComboBoxItemId)
                    return true;
                return false;
            })];
            if (result.Count == 0)
            {
                return;
            }
            JObject targetObj = result[0] as JObject;

            CurrentCommonTags.Clear();
            CurrentCommonTags = JArray.Parse(targetObj["Common"].ToString()).ToList().ConvertAll(item => item.ToString());
        }

        public void Build(StringBuilder Result)
        {
            Result = new((SpecialTagsResult.TryGetValue(SelectedEntityId.ComboBoxItemId, out ObservableCollection<NBTDataStructure> value) ? string.Join(",", value.Select(item =>
            {
                if (item is not null && item.Result.Length > 0)
                    return item.Result;
                return "";
            })) + "," : "") + string.Join(",", CommonResult.Select(item =>
            {
                if (CurrentCommonTags.Contains(item.NBTGroup) && item.Result.Length > 0)
                    return item.Result;
                else
                    return "";
            })));
            if (Result.Length > 0 && (Result[^1] == ' ' || Result[^1] == ','))
            {
                Result.Length--;
            }

            if (UseForTool)
            {
                Result = new("{id:\"minecraft:" + SelectedEntityId.ComboBoxItemId + "\"" + (Result is not null && Result.Length > 0 ? "," + Result : "") + "}");
                EntityView entity = Window.GetWindow(currentEntityPage) as EntityView;
                entity.DialogResult = true;
                return;
            }

            if (!Give)
            {
                if (CurrentMinVersion < 1130 && HaveCustomName)
                    Result = new(@"give @p minecraft:sign 1 0 {BlockEntityTag:{Text1:""{\""text\"":\""Right Click To Run\"",\""clickEvent\"":{\""action\"":\""run_command\"",\""newGrid\"":\""/setblock ~ ~ ~ minecraft:command_block 0 replace {Command:\\\""" + (Result.Length > 0 ? "summon minecraft:" + SelectedEntityId.ComboBoxItemId + " ~ ~ ~ {" + Result + "}" : "summon minecraft:" + SelectedEntityId.ComboBoxItemId + " ~ ~ ~") + @"\\\""}\""}}""}}");
                else
                    Result = new(Result.ToString().Trim() != "" ? "summon minecraft:" + SelectedEntityId.ComboBoxItemId + " ~ ~ ~ {" + Result + "}" : "summon minecraft:" + SelectedEntityId.ComboBoxItemId + " ~ ~ ~");
            }
            else
            {
                if (CurrentMinVersion < 1130)
                {
                    if (!HaveCustomName)
                    {
                        Result = new("give @p minecraft:spawner_egg 1 0 {EntityTag:{id:\"minecraft:" + SelectedEntityId.ComboBoxItemId + "\" " + (Result.Length > 0 ? "," + Result : "") + "}}");
                    }
                    else
                    {
                        Result = new(@"give @p minecraft:sign 1 0 {BlockEntityTag:{Text1:""{\""text\"":\""Right Click To Run\"",\""clickEvent\"":{\""action\"":\""run_command\"",\""newGrid\"":\""/setblock ~ ~ ~ minecraft:command_block 0 replace {Command:\\\""give @p minecraft:spawner_egg 1 0 {EntityTag:{id:""minecraft:" + SelectedEntityId.ComboBoxItemId + "\" " + (Result.Length > 0 ? "," + Result : "") + @"}}\\\""}\""}}""}}");
                    }
                }
                else
                    Result = new("give @p minecraft:pig_spawner_egg{EntityTag:{id:\"minecraft:" + SelectedEntityId.ComboBoxItemId + "\"" + (Result.Length > 0 ? "," + Result : "") + "}} 1");
            }

            if (SyncToFile && ExternFilePath.Length > 0 && File.Exists(ExternFilePath))
            {
                File.WriteAllText(ExternFilePath, Result.ToString());
            }
        }
        #endregion
    }
}