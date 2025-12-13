using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using CBHK.WindowDictionaries;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Data;
using System.Windows.Media;
using System.Threading.Tasks;
using CBHK.CustomControl;
using Newtonsoft.Json.Linq;
using System.Data;
using CBHK.View;
using Prism.Ioc;
using CBHK.ViewModel.Component.Villager;
using CBHK.Model.Common;
using CBHK.View.Component.Villager;
using CBHK.Domain;
using CBHK.Utility.MessageTip;
using CBHK.Utility.Common;

namespace CBHK.ViewModel.Generator
{
    public partial class VillagerViewModel:ObservableObject
    {
        #region Field

        private CBHKDataContext context = null;
        int CurrentMinVersion = 0;
        /// <summary>
        /// 存储生成结果
        /// </summary>
        public string Result;
        /// <summary>
        /// 主页引用
        /// </summary>
        private Window home = null;
        /// <summary>
        /// 本生成器的图标路径
        /// </summary>
        string iconPath = "pack://application:,,,/CBHK;component/Resource/Common/Image/SpawnerIcon/IconVillagers.png";
        string VillagerTypeSourceFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Villager\Data\VillagerTypes.ini";
        string VillagerProfessionsSourceFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Villager\Data\VillagerProfessionTypes.ini";
        string VillagerLevelSourceFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Villager\Data\VillagerLevels.ini";

        public Dictionary<string, string> VillagerTypeDataBase = [];
        public Dictionary<string, string> VillagerProfessionTypeDataBase = [];
        string emptyIcon = "pack://application:,,,/CBHK;component/Resource/CBHK/Image/empty.png";
        string GossipTypesFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Villager\Data\GossipTypeList.ini";
        private readonly SolidColorBrush transparentBrush = new((Color)ColorConverter.ConvertFromString("Transparent"));
        private readonly SolidColorBrush whiteBrush = new((Color)ColorConverter.ConvertFromString("#FFFFFF"));
        private readonly SolidColorBrush blackBrush = new((Color)ColorConverter.ConvertFromString("#000000"));

        private Dictionary<string, string> ItemIDAndNameMap = [];
        private List<string> ItemKeyList = [];

        private IProgress<ItemStructure> AddOriginalItemProgress = null;
        private IProgress<(int, string, string, string)> SetOriginalItemProgress = null;
        private IProgress<ItemStructure> AddCustomItemProgress = null;
        private IProgress<(int, string, string, string, string)> SetCustomItemProgress = null;

        private DataService dataService = null;
        private IContainerProvider container;

        private string ImageSetFolderPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
        /// <summary>
        /// 加载物品集合
        /// </summary>
        private string ItemSaveFolderPath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Saves\Item";
        /// <summary>
        /// 言论搜索目标引用
        /// </summary>
        TextBox GossipSearchTarget = null;
        /// <summary>
        /// 言论搜索类型引用
        /// </summary>
        ComboBox GossipSearchTypeBox = null;
        /// <summary>
        /// 言论数据源所在视图引用
        /// </summary>
        ScrollViewer GossipViewer = null;
        /// <summary>
        /// 维度数据源配置文件路径
        /// </summary>
        string dimensionTypeFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Villager\Data\DimensionTypes.ini";
        /// <summary>
        /// 维度类型数据库
        /// </summary>
        Dictionary<string, string> DimensionDataBase = [];

        /// <summary>
        /// 原版物品库数据视图
        /// </summary>
        private CollectionViewSource OriginalViewSource = new();
        /// <summary>
        /// 自定义物品库数据视图
        /// </summary>
        private CollectionViewSource CustomViewSource = new();

        //背包引用
        ListView Bag = null;
        ListView CustomBag = null;

        #region 处理拖拽
        public static bool IsGrabingItem = false;
        Image drag_source = null;
        Image GrabedImage = null;
        #endregion

        #endregion

        #region Property
        [ObservableProperty]
        private TextComboBoxItem _selectedVersion;
        /// <summary>
        /// 版本源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<TextComboBoxItem> _versionSource = [
            new TextComboBoxItem() { Text = "1.20.2" },
            new TextComboBoxItem() { Text = "1.13.0" }
            ];
        /// <summary>
        /// 是否显示结果
        /// </summary>
        [ObservableProperty]
        public bool _showResult;
        [ObservableProperty]
        public ObservableCollection<TextComboBoxItem> _gossipTypeList = [];
        /// <summary>
        /// 左侧交易项数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<TransactionItemView> _transactionItemList = [];
        /// <summary>
        /// 言论数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<GossipsItemsView> _gossipItemList = [];
        /// <summary>
        /// 当前选中的交易项
        /// </summary>
        [ObservableProperty]
        private TransactionItemView _currentItem = null;
        /// <summary>
        /// 主项数量
        /// </summary>
        [ObservableProperty]
        public int _buyCount = 1;
        /// <summary>
        /// 副项数量
        /// </summary>
        [ObservableProperty]
        public int _buyBCount = 1;
        /// <summary>
        /// 售卖数量
        /// </summary>
        [ObservableProperty]
        public int _sellCount = 1;
        /// <summary>
        /// 已选中的搜索言论成员
        /// </summary>
        [ObservableProperty]
        private TextComboBoxItem _selectedSearchGossipItem;
        /// <summary>
        /// 原版物品库
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<ItemStructure> _originalItemList = [];
        /// <summary>
        /// 自定义物品库
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<ItemStructure> _customItemList = [];
        //物品描述引用
        [ObservableProperty]
        public ObservableCollection<string> _bagItemToolTips = [];
        //言论搜索类型数据源
        [ObservableProperty]
        ObservableCollection<TextComboBoxItem> _gossipSearchType = [];
        //言论搜索类型配置文件路径
        string gossipSearchTypeFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Villager\Data\GossipSearchTypes.ini";
        /// <summary>
        /// 维度数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<TextComboBoxItem> _dimensionTypeSource = [];
        /// <summary>
        /// 交易项数据面板可见性
        /// </summary>
        [ObservableProperty]
        private Visibility _transactionDataGridVisibility = Visibility.Collapsed;
        /// <summary>
        /// 言论面板收放
        /// </summary>
        [ObservableProperty]
        private Visibility _isEditGossips = Visibility.Collapsed;
        /// <summary>
        /// 是否可以编辑言论
        /// </summary>
        [ObservableProperty]
        private bool _canEditGossip = false;
        /// <summary>
        /// 是否可以点击言论
        /// </summary>
        [ObservableProperty]
        private bool _canTouchGossip = true;
        /// <summary>
        /// 是否可以点击记忆
        /// </summary>
        [ObservableProperty]
        private bool _canTouchBrain = true;
        /// <summary>
        /// 记忆面板收放
        /// </summary>
        [ObservableProperty]
        private Visibility _isEditBrain = Visibility.Collapsed;
        /// <summary>
        /// 是否可以编辑记忆
        /// </summary>
        [ObservableProperty]
        private bool _canEditBrain = false;
        /// <summary>
        /// 言论与记忆面板收放
        /// </summary>
        [ObservableProperty]
        private Visibility _onlyEditItem = Visibility.Visible;
        /// <summary>
        /// 已选中的成员
        /// </summary>
        [ObservableProperty]
        private ItemStructure _selectedItem = null;
        /// <summary>
        /// 已选中的物品库索引
        /// </summary>
        [ObservableProperty]
        private int _selectedItemListIndex;
        /// <summary>
        /// 搜索内容
        /// </summary>
        [ObservableProperty]
        private string _searchText = "";
        /// <summary>
        /// 村民类型数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<TextComboBoxItem> _villagerTypeSource = [];
        /// <summary>
        /// 村民职业数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<TextComboBoxItem> _villagerProfessionTypeSource = [];
        /// <summary>
        /// 村民交易等级数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<TextComboBoxItem> _villagerLevelSource = [];
        /// <summary>
        /// 村民数据
        /// </summary>
        private string VillagerData
        {
            get
            {
                string result = "VillagerData:{";
                if (VillagerTypeString.Trim().Length > 0 && VillagerProfessionTypeString.Trim().Length > 0 && VillagerLevelString.Trim().Length > 0)
                {
                    result += VillagerTypeString + VillagerProfessionTypeString + VillagerLevelString;
                    result = result.TrimEnd(',') + "},";
                }
                else
                    result = "";
                return result;
            }
        }

        #region 主、副、结果物品图像源
        [ObservableProperty]
        private ImageSource _buyItemIcon = null;

        [ObservableProperty]
        private ImageSource _buyBItemIcon = null;

        [ObservableProperty]
        private ImageSource _sellItemIcon = null;
        #endregion

        #region 主、副、结果物品数据
        [ObservableProperty]
        private object _buyItemData = null;
        [ObservableProperty]
        private object _buyBItemData = null;
        [ObservableProperty]
        private object _sellItemData = null;
        #endregion

        #region 主、副、结果数量
        [ObservableProperty]
        private object _buyItemCount = null;
        [ObservableProperty]
        private object _buyBItemCount = null;
        [ObservableProperty]
        private object _sellItemCount = null;
        #endregion

        #region 交易特指数据
        [ObservableProperty]
        private bool? _rewardExp = null;
        [ObservableProperty]
        private int _maxUses = 1;
        [ObservableProperty]
        private int _uses = 0;
        [ObservableProperty]
        private int _villagerGetXp = 0;
        [ObservableProperty]
        private int _demand = 0;
        [ObservableProperty]
        private int _specialPrice = 0;
        [ObservableProperty]
        private float _priceMultiplier = 0;
        #endregion

        #region Offers
        private string Offers
        {
            get
            {
                if (TransactionItemList.Count == 0) return "";
                string result = "Offers:{Recipes:[";
                string transactionItemData = string.Join("", TransactionItemList.Select(item => (item.DataContext as TransactionItemViewModel).TransactionItemData + ","));
                result += transactionItemData.TrimEnd(',') + "]},";
                return result;
            }
        }
        #endregion

        #region Gossips
        private string Gossips
        {
            get
            {
                if (!CanEditGossip || OnlyEditItem == Visibility.Collapsed || GossipItemList.Count == 0)
                {
                    return "";
                }
                string result = "Gossips:[";
                result += string.Join(",", GossipItemList.Select(item => (item.DataContext as GossipsItemsViewModel).GossipData));
                result = result.TrimEnd(',') + "],";
                return result;
            }
        }
        #endregion

        #region Brain

        #region 聚集点
        [ObservableProperty]
        private double _meetingPointX = 0;
        [ObservableProperty]
        private double _meetingPointY = 0;
        [ObservableProperty]
        private double _meetingPointZ = 0;
        [ObservableProperty]
        private TextComboBoxItem _meetingPointDimension = null;
        private string MeetingPointDimensionString
        {
            get
            {
                string DimensionId = DimensionDataBase.Where(item => item.Value == MeetingPointDimension.Text).First().Key;
                return MeetingPointDimension.Text.Trim() != "" ? "dimension:\"minecraft:" + DimensionId + "\"" : "";
            }
        }
        private string MeetingPoint
        {
            get
            {
                string result = "meeting_point:{";
                string pos = MeetingPointX.ToString().Trim() != "" && MeetingPointY.ToString().Trim() != "" && MeetingPointZ.ToString().Trim() != "" ? "pos:[" + MeetingPointX + "," + MeetingPointY + "," + MeetingPointZ + "]," : "";
                if(MeetingPointDimensionString != "" && pos != "")
                result += pos + MeetingPointDimensionString;
                if (result.Trim() == "meeting_point:{") return "";
                return result.TrimEnd(',') + "},";
            }
        }
        #endregion

        #region 床位置
        [ObservableProperty]
        private double _homeX = 0;
        [ObservableProperty]
        private double homeY = 0;
        [ObservableProperty]
        private double homeZ = 0;
        [ObservableProperty]
        private TextComboBoxItem _homeDimension = null;
        private string HomeDimensionString
        {
            get
            {
                string DimensionId = DimensionDataBase.Where(item=>item.Value == HomeDimension.Text).First().Key;
                return HomeDimension.Text.Trim() != "" ? "dimension:\"minecraft:" + DimensionId + "\"" : "";
            }
        }
        private string Home
        {
            get
            {
                string result = "home:{";
                string pos = HomeX.ToString().Trim() != "" && HomeY.ToString().Trim() != "" && HomeZ.ToString().Trim() != "" ? "pos:[" + HomeX + "," + HomeY + "," + HomeZ + "]," : "";
                if(pos != "" && HomeDimensionString != "")
                result += pos + HomeDimensionString;
                if (result.Trim() == "home:{") return "";
                return result.TrimEnd(',') + "},";
            }
        }
        #endregion

        #region 工作站点
        [ObservableProperty]
        private double _jobSiteX = 0;
        [ObservableProperty]
        private double _jobSiteY = 0;
        [ObservableProperty]
        private double _jobSiteZ = 0;
        [ObservableProperty]
        private TextComboBoxItem _jobSiteDimension = null;
        private string JobSiteDimensionString
        {
            get
            {
                string DimensionId = DimensionDataBase.Where(item => item.Value == JobSiteDimension.Text).First().Key;
                return JobSiteDimension.Text.Trim() !=""? "dimension:\"minecraft:" + DimensionId + "\",":"";
            }
        }
        private string JobSite
        {
            get
            {
                string result = "job_site:{";
                string pos = JobSiteX.ToString().Trim() != "" && JobSiteY.ToString().Trim() != "" && JobSiteZ.ToString().Trim() != "" ? "pos:[" + JobSiteX + "," + JobSiteY + "," + JobSiteZ + "]," : "";
                if(pos != "" && JobSiteDimensionString != "")
                result += pos + JobSiteDimensionString;
                if (result.Trim() == "job_site:{") return "";
                return result.TrimEnd(',')+"},";
            }
        }
        #endregion

        #region 记忆
        private string Brain
        {
            get
            {
                if (!CanEditBrain || OnlyEditItem == Visibility.Collapsed) return "";
                string memoriesContent = MeetingPoint + Home + JobSite;
                string result = "Brain:{memories:{" + memoriesContent.TrimEnd(',') + "}},";
                if (result.Trim() == "Brain:{memories:{}},") return "";
                return result;
            }
        }
        #endregion

        #endregion

        #region 村民种类
        [ObservableProperty]
        private TextComboBoxItem _villagerType;
        private string VillagerTypeString
        {
            get
            {
                string result = "type:\"minecraft:";
                if (VillagerTypeDataBase.Count > 0)
                    result = result + VillagerTypeDataBase.Where(item => item.Value == VillagerType.Text).First().Key + "\",";
                else
                    result = "";
                return result;
            }
        }
        #endregion

        #region 村民职业
        [ObservableProperty]
        private TextComboBoxItem _villagerProfessionType;
        private string VillagerProfessionTypeString
        {
            get
            {
                string result = "profession:\"minecraft:";
                if (VillagerProfessionTypeDataBase.Count > 0)
                    result = result + VillagerProfessionTypeDataBase.Where(item => item.Value == VillagerProfessionType.Text).First().Key + "\",";
                else
                    result = "";
                return result;
            }
        }
        #endregion

        #region 村民交易等级
        [ObservableProperty]
        private TextComboBoxItem _villagerLevel;
        private string VillagerLevelString
        {
            get
            {
                string result = "level:";
                if (VillagerLevel is not null)
                    result = result + VillagerLevel.Text + ",";
                else
                    result = "";
                return result;
            }
        }

        #endregion

        #region 是否愿意交配
        [ObservableProperty]
        private bool _willing = false;
        private string WillingString
        {
            get => Willing ? "Willing:1b," : "";
        }
        #endregion

        #region 此村民最后一次前往工作站点重新供应交易的刻
        [ObservableProperty]
        private double _lastRestock = 0;
        private string LastRestockString
        {
            get
            {
                return LastRestock.ToString().Trim() != "" ? "LastRestock:" + LastRestock + "," : "";
            }
        }
        #endregion

        #region 此村民当前的经验值
        [ObservableProperty]
        private double _xp = 1;
        private string XpString
        {
            get
            {
                return Xp.ToString().Trim() != "" ? "Xp:" + Xp + "," : "";
            }
        }
        #endregion

        #endregion

        #region Method
        public VillagerViewModel(IContainerProvider Container,MainView mainView,CBHKDataContext Context,DataService DataService)
        {
            dataService = DataService;
            context = Context;
            container = Container;
            home = mainView;

            #region 初始化主、副、结果空图像
            BuyItemIcon = BuyBItemIcon = SellItemIcon = new BitmapImage(new Uri(emptyIcon));
            #endregion

            #region 读取言论类型
            if (File.Exists(GossipTypesFilePath))
            {
                string[] types = File.ReadAllLines(GossipTypesFilePath);
                for (int i = 0; i < types.Length; i++)
                    GossipTypeList.Add(new() { Text = types[i] });
            }
            #endregion

            #region 初始化数据源
            if (File.Exists(VillagerTypeSourceFilePath))
            {
                string[] data = File.ReadAllLines(VillagerTypeSourceFilePath);
                for (int i = 0; i < data.Length; i++)
                {
                    string[] item = data[i].Split('.');
                    string id = item[0];
                    string name = item[1];
                    VillagerTypeDataBase.TryAdd(id, name);
                    VillagerTypeSource.Add(new TextComboBoxItem() { Text = name });
                }
            }
            if (File.Exists(VillagerProfessionsSourceFilePath))
            {
                string[] data = File.ReadAllLines(VillagerProfessionsSourceFilePath);
                for (int i = 0; i < data.Length; i++)
                {
                    string[] item = data[i].Split('.');
                    string id = item[0];
                    string name = item[1];

                    if (!VillagerProfessionTypeDataBase.ContainsKey(id))
                        VillagerProfessionTypeDataBase.Add(id, name);

                    VillagerProfessionTypeSource.Add(new TextComboBoxItem() { Text = name });
                }
            }
            if (File.Exists(VillagerLevelSourceFilePath))
            {
                int level = int.Parse(File.ReadAllText(VillagerLevelSourceFilePath));
                for (int i = 1; i <= level; i++)
                {
                    VillagerLevelSource.Add(new TextComboBoxItem() { Text = i.ToString() });
                }
            }
            #endregion

            #region 初始化维度列表、设置三个点位数据
            if (DimensionTypeSource.Count == 0)
            {
                if (File.Exists(dimensionTypeFilePath))
                {
                    string[] data = File.ReadAllLines(dimensionTypeFilePath);
                    for (int i = 0; i < data.Length; i++)
                    {
                        string[] item = data[i].Split('.');
                        string id = item[0];
                        string name = item[1];

                        if (!DimensionDataBase.ContainsKey(id))
                            DimensionDataBase.Add(id, name);

                        DimensionTypeSource.Add(new TextComboBoxItem() { Text = name });
                    }
                }
            }
            MeetingPointDimension = HomeDimension = JobSiteDimension = DimensionTypeSource[0];
            #endregion

            AddOriginalItemProgress = new Progress<ItemStructure>(OriginalItemList.Add);
            SetOriginalItemProgress = new Progress<(int, string, string, string)>(item =>
            {
                if (File.Exists(item.Item4))
                {
                    OriginalItemList[item.Item1].IDAndName = item.Item2 + ':' + item.Item3;
                    OriginalItemList[item.Item1].ImagePath = new BitmapImage(new Uri(item.Item4, UriKind.Absolute));
                }
            });

            AddCustomItemProgress = new Progress<ItemStructure>(CustomItemList.Add);
            SetCustomItemProgress = new Progress<(int, string, string, string, string)>(item =>
            {
                if (File.Exists(item.Item4))
                {
                    CustomItemList[item.Item1].IDAndName = item.Item2 + ':' + item.Item3;
                    CustomItemList[item.Item1].NBT = item.Item5;
                    CustomItemList[item.Item1].ImagePath = new BitmapImage(new Uri(item.Item4, UriKind.Absolute));
                }
            });

            ItemIDAndNameMap = dataService.GetItemIDAndNameGroupByVersionMap()
            .Where(pair => pair.Key <= CurrentMinVersion)
            .SelectMany(pair => pair.Value)
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value
            );

            List<string> HaveNoIImageList = [];
            foreach (var item in ItemIDAndNameMap)
            {
                if (!File.Exists(ImageSetFolderPath + item.Key + ".png") && !File.Exists(ImageSetFolderPath + item.Key + "_spawn_egg.png"))
                {
                    HaveNoIImageList.Add(item.Key);
                }
            }

            foreach (var item in HaveNoIImageList)
            {
                ItemIDAndNameMap.Remove(item);
            }

            ItemKeyList = [.. ItemIDAndNameMap.Select(item => item.Key)];
            ItemKeyList.Sort();
        }

        /// <summary>
        /// 初始化物品ID与版本物品ID列表
        /// </summary>
        private void InitOriginItemList()
        {
            OriginalItemList.Clear();

            Task.Run(async () =>
            {
                ParallelOptions parallelOptions = new();
                await Parallel.ForAsync(0, ItemKeyList.Count, parallelOptions, (i, cancellationToken) =>
                {
                    AddOriginalItemProgress.Report(new());
                    return new ValueTask();
                });

                Parallel.For(0, ItemKeyList.Count, (i) =>
                {
                    string currentKey = ItemKeyList[i];
                    string imagePath = "";
                    if (File.Exists(ImageSetFolderPath + currentKey + ".png"))
                    {
                        imagePath = ImageSetFolderPath + currentKey + ".png";
                    }
                    else
                    if (File.Exists(ImageSetFolderPath + currentKey + "_spawn_egg.png"))
                    {
                        imagePath = ImageSetFolderPath + currentKey + "_spawn_egg.png";
                    }
                    SetOriginalItemProgress.Report(new ValueTuple<int, string, string, string>(i, currentKey, ItemIDAndNameMap[currentKey], imagePath));
                });
            });
        }

        /// <summary>
        /// 初始化自定义物品列表
        /// </summary>
        private void InitCustomItemList()
        {
            CustomItemList.Clear();

            Task.Run(async () =>
            {
                string[] itemFileList = Directory.GetFiles(ItemSaveFolderPath);
                ParallelOptions parallelOptions = new();
                await Parallel.ForAsync(0, itemFileList.Length, parallelOptions, (i, cancellationToken) =>
                {
                    if (File.Exists(ImageSetFolderPath + ItemKeyList[i] + ".png") || File.Exists(ImageSetFolderPath + ItemKeyList[i] + "_spawn_egg.png"))
                    {
                        AddCustomItemProgress.Report(new ItemStructure());
                    }
                    return new ValueTask();
                });

                Parallel.For(0, itemFileList.Length, (i) =>
                {
                    if (File.Exists(itemFileList[i]))
                    {
                        string nbt = ExternalDataImportManager.GetItemDataHandler(itemFileList[i], true);
                        string currentKey = "";
                        if (JObject.Parse(nbt)["oldID"] is JToken IDToken)
                        {
                            currentKey = IDToken.Value<string>().Replace("minecraft:", "");
                        }
                        string imagePath = "";
                        if (File.Exists(ImageSetFolderPath + currentKey + ".png"))
                        {
                            imagePath = ImageSetFolderPath + currentKey + ".png";
                        }
                        else
                        if (File.Exists(ImageSetFolderPath + currentKey + "_spawn_egg.png"))
                        {
                            imagePath = ImageSetFolderPath + currentKey + "_spawn_egg.png";
                        }
                        if (imagePath.Length > 0)
                        {
                            SetCustomItemProgress.Report(new ValueTuple<int, string, string, string, string>(i, currentKey, ItemIDAndNameMap[currentKey], imagePath, nbt));
                        }
                    }
                });
            });
        }

        /// <summary>
        /// 为保存行为执行生成
        /// </summary>
        /// <param name="showResult"></param>
        private void Run(bool showResult)
        {
            Result = "";
            Result += WillingString + VillagerData + Offers + Gossips + Brain + LastRestockString + XpString;
            Result = "/summon villager ~ ~1 ~ {" + Result.TrimEnd(',') + "}";

            if (showResult)
            {
                DisplayerView displayer = container.Resolve<DisplayerView>();
                if (displayer is not null && displayer.DataContext is DisplayerViewModel displayerViewModel)
                {
                    displayerViewModel.GeneratorResult(Result, "村民", iconPath);
                }
            }
            else
                Clipboard.SetText(Result);
        }
        #endregion

        #region Event
        /// <summary>
        /// 原版物品库视图载入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OriginalItemListView_Loaded(object sender,RoutedEventArgs e)
        {
            if (OriginalItemList.Count == 0)
            {
                Window parent = Window.GetWindow(sender as ListView);
                OriginalViewSource = parent.FindResource("OriginalItemView") as CollectionViewSource;
                InitOriginItemList();
                OriginalViewSource.Filter += CollectionViewSource_Filter;
            }
        }

        /// <summary>
        /// 自定义物品库视图载入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CustomItemListView_Loaded(object sender, RoutedEventArgs e)
        {
            if (CustomItemList.Count == 0)
            {
                Window parent = Window.GetWindow(sender as ListView);
                CustomViewSource = parent.FindResource("CustomItemView") as CollectionViewSource;
                InitCustomItemList();
                CustomViewSource.Filter += CollectionViewSource_Filter;
            }
        }

        public void VersionBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        /// <summary>
        /// 展开/折叠言论面板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void GossipEditButton_Click(object sender,RoutedEventArgs e)
        {
            IsEditGossips = CanEditGossip ? Visibility.Visible : Visibility.Collapsed;
            //恢复所有交易项的价格
            if (!CanEditGossip)
            {
                TransactionItemList.All(item => { (item.DataContext as TransactionItemViewModel).HideDiscountData(); return true; });
            }
            else
            {
                TransactionItemList.All(item => { (item.DataContext as TransactionItemViewModel).HideDiscountData(false); return true; });
            }
            OnlyEditItem = !CanEditBrain && !CanEditGossip ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// 展开/折叠记忆面板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void BrainEditButton_Click(object sender, RoutedEventArgs e)
        {
            IsEditBrain = CanEditBrain ? Visibility.Visible : Visibility.Collapsed;
            OnlyEditItem = !CanEditBrain && !CanEditGossip ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// 搜索文本更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (SelectedItemListIndex == 0)
            {
                OriginalViewSource.View?.Refresh();
            }
            else
            {
                CustomViewSource.View?.Refresh();
            }
        }

        [RelayCommand]
        /// <summary>
        /// 关闭交易数据设置面板
        /// </summary>
        public void CloseTransactionDataGrid()
        {
            TransactionDataGridVisibility = Visibility.Collapsed;
            TransactionItemViewModel transactionItemsViewModel = CurrentItem.DataContext as TransactionItemViewModel;
            //更新当前交易项的数量显示
            transactionItemsViewModel.RewardExp = RewardExp is not null ? RewardExp.Value : false;
            transactionItemsViewModel.BuyCountDisplayText = "x" + BuyCount.ToString();
            transactionItemsViewModel.BuyBCountDisplayText = "x" + BuyBCount.ToString();
            transactionItemsViewModel.SellCountDisplayText = "x" + SellCount.ToString();
            transactionItemsViewModel.MaxUses = MaxUses;
            transactionItemsViewModel.Uses = Uses;
            transactionItemsViewModel.Xp = VillagerGetXp;
            transactionItemsViewModel.Demand = Demand;
            transactionItemsViewModel.SpecialPrice = SpecialPrice;
            transactionItemsViewModel.PriceMultiplier = PriceMultiplier;
        }

        [RelayCommand]
        /// <summary>
        /// 从文件导入
        /// </summary>
        private void ImportFromFile()
        {
            Microsoft.Win32.OpenFileDialog dialog = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                RestoreDirectory = true,
                DefaultExt = ".command",
                Multiselect = false,
                Title = "请选择一个Minecraft村民数据文件"
            };
            if (dialog.ShowDialog().Value && File.Exists(dialog.FileName))
                ExternalDataImportManager.ImportVillagerDataHandler(dialog.FileName, this);
        }

        [RelayCommand]
        /// <summary>
        /// 从剪切板导入
        /// </summary>
        private void ImportFromClipboard()
        {
            ExternalDataImportManager.ImportVillagerDataHandler(Clipboard.GetText(),this,false);
        }

        /// <summary>
        /// 保存村民
        /// </summary>
        [RelayCommand]
        public void Save()
        {
            Run(false);
            Microsoft.Win32.SaveFileDialog saveFileDialog = new()
            {
                Title = "请选择村民保存路径",
                AddExtension = true,
                DefaultExt = ".command",
                Filter = "Command文件|*.command;",
                CheckPathExists = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                RestoreDirectory = true
            };
            if(saveFileDialog.ShowDialog().Value)
            {
                Task.Run(async () =>
                {
                    await File.WriteAllTextAsync(saveFileDialog.FileName, Result);
                    await File.WriteAllTextAsync(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Saves\Villager\" + Path.GetFileName(saveFileDialog.FileName), Result);
                });
            }
        }

        /// <summary>
        /// 过滤与搜索内容不相关的成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (SearchText.Trim().Length > 0)
            {
                e.Accepted = false;
                ItemStructure itemStructure = e.Item as ItemStructure;
                if (itemStructure.IDAndName is not null && itemStructure.ImagePath is not null)
                {
                    string currentItemID = Path.GetFileNameWithoutExtension(itemStructure.ImagePath.ToString());
                    string currentItemName = itemStructure.IDAndName.Split(':')[1];

                    if (currentItemID.StartsWith(SearchText) || currentItemName.StartsWith(SearchText))
                    {
                        e.Accepted = true;
                    }
                }
            }
            else
            {
                e.Accepted = true;
            }
        }

        /// <summary>
        /// 获取言论所在的视图引用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void GossipViewerLoaded(object sender, RoutedEventArgs e)
        {
            GossipViewer = sender as ScrollViewer;
        }

        [RelayCommand]
        /// <summary>
        /// 返回主页
        /// </summary>
        /// <param name="win"></param>
        private void Return(CommonWindow win)
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
        private void Run()
        {
            Result = "";
            Result += WillingString + VillagerData + Offers + Gossips + Brain + LastRestockString + XpString;
            Result = "/summon villager ~ ~1 ~ {" + Result.TrimEnd(',') + "}";

            if (ShowResult)
            {
                DisplayerView displayer = container.Resolve<DisplayerView>();
                if (displayer is not null && displayer.DataContext is DisplayerViewModel displayerViewModel)
                {
                    displayer.Show();
                    displayerViewModel.GeneratorResult(Result, "村民", iconPath);
                }
            }
            else
            {
                Clipboard.SetText(Result);
                Message.PushMessage("村民生成成功！", MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        /// <summary>
        /// 添加一个交易项控件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void AddTransactionItem()
        {
            TransactionItemView transaction = new()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            TransactionItemList.Add(transaction);
        }

        [RelayCommand]
        /// <summary>
        /// 清空交易项控件
        /// </summary>
        private void ClearTransactionItem()
        {
            TransactionItemList.Clear();
        }

        [RelayCommand]
        /// <summary>
        /// 添加一个言论控件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void AddGossipItem()
        {
            GossipsItemsView gossipsItem = new()
            {
                Margin = new Thickness(12, 0, 0, 5),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            GossipItemList.Add(gossipsItem);
        }

        [RelayCommand]
        /// <summary>
        /// 清空言论控件
        /// </summary>
        private void ClearGossipItem()
        {
            GossipItemList.Clear();
        }

        /// <summary>
        /// 载入物品库
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Items_Loaded(object sender, RoutedEventArgs e)
        {
            Bag = ((sender as TabControl).Items[0] as TextTabItems).Content as ListView;
            CustomBag = ((sender as TabControl).Items[1] as TextTabItems).Content as ListView;

            Bag.DataContext = this;
            CustomBag.DataContext = this;

            Bag.PreviewMouseLeftButtonDown += SelectItemClickDown;
            Bag.MouseMove += Bag_MouseMove;
            Bag.MouseLeave += ListBox_MouseLeave;

            CustomBag.PreviewMouseLeftButtonDown += SelectItemClickDown;
            CustomBag.MouseMove += Bag_MouseMove;
            CustomBag.MouseLeave += ListBox_MouseLeave;
        }

        /// <summary>
        /// 获取言论搜索目标的引用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void GossipSearchTargetLoaded(object sender, RoutedEventArgs e)
        {
            GossipSearchTarget = sender as TextBox;
        }

        /// <summary>
        /// 获取言论搜索类型的引用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void GossipSearchTypeLoaded(object sender, RoutedEventArgs e)
        {
            GossipSearchTypeBox = sender as ComboBox;
            if(File.Exists(gossipSearchTypeFilePath))
            {
                string[] types = File.ReadAllLines(gossipSearchTypeFilePath);
                for (int i = 0; i < types.Length; i++)
                {
                    GossipSearchType.Add(new TextComboBoxItem() { Text = types[i] });
                }
            }
            GossipSearchTypeBox.ItemsSource = GossipSearchType;
            GossipSearchTypeBox.SelectedIndex = 0;
        }

        /// <summary>
        /// 查找目标言论并更新价格数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SearchGossipsTextChanged(object sender, TextChangedEventArgs e)
        {
            if(CanEditGossip)
            {
                string current_type = SelectedSearchGossipItem.Text;
                List<GossipsItemsView> target_gossip = GossipItemList.Where(gossip =>
                {
                    GossipsItemsViewModel gossipsItemsViewModel = gossip.DataContext as GossipsItemsViewModel;
                    string type = gossipsItemsViewModel.SelectedTypeItem.Text;
                    if (gossipsItemsViewModel.TargetText == GossipSearchTarget.Text.Trim() && type == current_type)
                        return true;
                    else
                        return false;
                }).ToList();

                if (target_gossip.Count > 0)
                    ScrollToSomeWhere.Scroll(target_gossip[0], GossipViewer);
            }
        }

        /// <summary>
        /// 鼠标移入背包后选中对应的成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Bag_MouseMove(object sender, MouseEventArgs e)
        {
            ListView listView = sender as ListView;
            HitTestResult hitTestResult = VisualTreeHelper.HitTest(listView, Mouse.GetPosition(listView));

            if (hitTestResult is not null)
            {
                var item = hitTestResult.VisualHit;

                while (item is not null && item is not ListViewItem)
                    item = VisualTreeHelper.GetParent(item);

                if (item is not null)
                {
                    int i = listView.Items.IndexOf(((ListViewItem)item).DataContext);
                    if (i >= 0 && i < listView.Items.Count)
                        listView.SelectedIndex = i;
                }
            }
        }

        public void ListBox_MouseLeave(object sender, MouseEventArgs e)
        {
            SelectedItem = null;
        }

        /// <summary>
        /// 处理开始拖拽物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SelectItemClickDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not Image)
                SelectedItem = null;
            if (SelectedItem is not null)
            {
                IsGrabingItem = true;

                DependencyObject obj = (DependencyObject)e.OriginalSource;
                while (obj is not null && obj is not ListViewItem)
                {
                    obj = VisualTreeHelper.GetParent(obj);
                }
                ListViewItem item = obj as ListViewItem;
                ItemStructure itemStructure = item.Content as ItemStructure;

                drag_source = new Image
                {
                    Source = SelectedItem.ImagePath,
                    Tag = itemStructure
                };
                GrabedImage = drag_source;

                if (IsGrabingItem && drag_source is not null)
                {
                    DataObject dataObject = new(typeof(Image), GrabedImage);
                    if (dataObject is not null)
                        DragDrop.DoDragDrop(drag_source, dataObject, DragDropEffects.Move);
                    IsGrabingItem = false;
                }
            }
        }
        #endregion
    }
}