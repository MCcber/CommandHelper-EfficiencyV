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
using CBHK.GeneralTool;
using System.Data;
using CBHK.GeneralTool.MessageTip;
using CBHK.View;
using Prism.Ioc;
using CBHK.ViewModel.Component.Villager;
using CBHK.Model.Common;
using CBHK.View.Component.Villager;
using CBHK.Domain;

namespace CBHK.ViewModel.Generator
{
    public partial class VillagerViewModel:ObservableObject
    {
        private CBHKDataContext _context = null;

        #region 处理拖拽
        public static bool IsGrabingItem = false;
        Image drag_source = null;
        Image GrabedImage = null;
        #endregion

        #region 版本源
        private TextComboBoxItem selectedVersion;
        public TextComboBoxItem SelectedVersion
        {
            get => selectedVersion;
            set
            {
                SetProperty(ref selectedVersion, value);
                if ((CanEditBrain || CanEditGossips) && SelectedVersion.Text == "1.20.2")
                    CanEditBrain = CanEditGossips = false;
                CanTouchBrain = CanTouchGossips = SelectedVersion.Text != "1.20.2";
                CurrentMinVersion = int.Parse(selectedVersion.Text.Split('-')[0].Replace(".", ""));
            }
        }

        int CurrentMinVersion = 0;

        public ObservableCollection<TextComboBoxItem> VersionSource { get; set; } = [
            new TextComboBoxItem() { Text = "1.20.2" },
            new TextComboBoxItem() { Text = "1.13.0" }
            ];
        #endregion

        #region 是否显示结果
        public bool ShowResult { get; set; }
        #endregion

        #region 存储生成结果
        public string Result { get; set; }
        #endregion

        #region 字段与引用
        /// <summary>
        /// 主页引用
        /// </summary>
        private Window home = null;
        /// <summary>
        /// 本生成器的图标路径
        /// </summary>
        string iconPath = "pack://application:,,,/CBHK;component/Resource/Common/Image/SpawnerIcon/IconVillagers.png";
        string emptyIcon = "pack://application:,,,/CBHK;component/Resource/CBHK/Image/empty.png";
        string GossipTypesFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Villager\Data\GossipTypes.ini";
        private SolidColorBrush transparentBrush = new((Color)ColorConverter.ConvertFromString("Transparent"));
        private SolidColorBrush whiteBrush = new((Color)ColorConverter.ConvertFromString("#FFFFFF"));
        private SolidColorBrush blackBrush = new((Color)ColorConverter.ConvertFromString("#000000"));

        private Dictionary<string, string> ItemIDAndNameMap = [];
        private List<string> ItemKeyList = [];

        private IProgress<ItemStructure> AddOriginalItemProgress = null;
        private IProgress<(int, string, string, string)> SetOriginalItemProgress = null;
        private IProgress<ItemStructure> AddCustomItemProgress = null;
        private IProgress<(int, string, string, string, string)> SetCustomItemProgress = null;

        private DataService _dataService = null;
        private IContainerProvider _container;

        private string ImageSetFolderPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
        /// <summary>
        /// 加载物品集合
        /// </summary>
        private string ItemSaveFolderPath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Saves\Item";

        public ObservableCollection<TextComboBoxItem> GossipTypes { get; set; } = [];
        /// <summary>
        /// 左侧交易项数据源
        /// </summary>
        public ObservableCollection<TransactionItemView> TransactionItemList { get; set; } = [];
        /// <summary>
        /// 言论数据源
        /// </summary>
        public ObservableCollection<GossipsItemsView> gossipItems { get; set; } = [];
        #endregion

        #region 当前选中的物品
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

        #region 已选中的搜索言论成员
        [ObservableProperty]
        private TextComboBoxItem _selectedSearchGossipItem;

        #endregion

        //言论搜索目标引用
        TextBox GossipSearchTarget = null;
        //言论搜索类型引用
        ComboBox GossipSearchTypeBox = null;
        //言论数据源所在视图引用
        ScrollViewer GossipViewer = null;
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
        //维度数据源
        public ObservableCollection<TextComboBoxItem> DimensionTypeSource { get; set; } = [];
        //维度数据源配置文件路径
        string dimensionTypeFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Villager\Data\DimensionTypes.ini";
        //维度类型数据库
        Dictionary<string, string> DimensionDataBase = [];

        /// <summary>
        /// 原版物品库数据视图
        /// </summary>
        private CollectionViewSource OriginalViewSource = new ();
        /// <summary>
        /// 自定义物品库数据视图
        /// </summary>
        private CollectionViewSource CustomViewSource = new();

        //背包引用
        ListView Bag = null;
        ListView CustomBag = null;
        #endregion

        #region 交易项数据面板可见性
        [ObservableProperty]
        private Visibility _transactionDataGridVisibility = Visibility.Collapsed;
        #endregion

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

        #region 言论面板收放
        [ObservableProperty]
        private Visibility _isEditGossips = Visibility.Collapsed;
        #endregion

        #region 是否可以编辑言论
        private bool canEditGossips = false;
        public bool CanEditGossips
        {
            get { return canEditGossips; }
            set
            {
                SetProperty(ref canEditGossips, value);
                IsEditGossips = CanEditGossips ? Visibility.Visible:Visibility.Collapsed;
                //恢复所有交易项的价格
                if (!CanEditGossips)
                    TransactionItemList.All(item => { (item.DataContext as TransactionItemViewModel).HideDiscountData();return true; });
                else
                    TransactionItemList.All(item => { (item.DataContext as TransactionItemViewModel).HideDiscountData(false); return true; });
                OnlyEditItem = !CanEditBrain && !CanEditGossips ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        #endregion

        #region 是否可以点击言论
        [ObservableProperty]
        private bool _canTouchGossips = true;
        #endregion

        #region 是否可以点击记忆
        [ObservableProperty]
        private bool _canTouchBrain = true;
        #endregion

        #region 记忆面板收放
        [ObservableProperty]
        private Visibility _isEditBrain = Visibility.Collapsed;
        #endregion

        #region 是否可以编辑记忆
        private bool canEditBrain = false;
        public bool CanEditBrain
        {
            get => canEditBrain;
            set
            {
                SetProperty(ref canEditBrain, value);
                IsEditBrain = CanEditBrain ? Visibility.Visible : Visibility.Collapsed;
                OnlyEditItem = !CanEditBrain && !CanEditGossips ?Visibility.Collapsed:Visibility.Visible;
            }
        }
        #endregion

        #region 言论与记忆面板收放
        [ObservableProperty]
        private Visibility _onlyEditItem = Visibility.Visible;
        #endregion

        #region 已选中的成员
        [ObservableProperty]
        private ItemStructure _selectedItem = null;
        #endregion

        #region 已选中的物品库索引
        [ObservableProperty]
        private int _selectedItemListIndex;
        #endregion

        #region 搜索内容
        [ObservableProperty]
        private string _searchText = "";
        #endregion

        #region 村民数据

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
                if (!CanEditGossips || OnlyEditItem == Visibility.Collapsed || gossipItems.Count == 0)
                {
                    return "";
                }
                string result = "Gossips:[";
                result += string.Join(",", gossipItems.Select(item => (item.DataContext as GossipsItemsViewModel).GossipData));
                result = result.TrimEnd(',') + "],";
                return result;
            }
        }
        #endregion

        #region Brain

        #region 聚集点
        private double meeting_pointX = 0;
        public double MeetingPointX
        {
            set => SetProperty(ref meeting_pointX, value);
            get => meeting_pointX;
        }
        private double meeting_pointY = 0;
        public double MeetingPointY
        {
            set => SetProperty(ref meeting_pointY, value);
            get => meeting_pointY;
        }
        private double meeting_pointZ = 0;
        public double MeetingPointZ
        {
            set => SetProperty(ref meeting_pointZ, value);
            get => meeting_pointZ;
        }
        private TextComboBoxItem meetingPointDimension = null;
        public TextComboBoxItem MeetingPointDimension
        {
            get => meetingPointDimension;
            set => SetProperty(ref meetingPointDimension, value);
        }
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
        private double homeX = 0;
        public double HomeX
        {
            set { homeX = value; OnPropertyChanged(); }
            get
            {
                return homeX;
            }
        }
        private double homeY = 0;
        public double HomeY
        {
            set { homeY = value; OnPropertyChanged(); }
            get
            {
                return homeY;
            }
        }
        private double homeZ = 0;
        public double HomeZ
        {
            set { homeZ = value; OnPropertyChanged(); }
            get
            {
                return homeZ;
            }
        }
        private TextComboBoxItem homeDimension = null;
        public TextComboBoxItem HomeDimension
        {
            get { return homeDimension; }
            set
            {
                homeDimension = value;
                OnPropertyChanged();
            }
        }
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
        private double job_siteX = 0;
        public double JobSiteX
        {
            set { job_siteX = value; OnPropertyChanged(); }
            get
            {
                return job_siteX;
            }
        }
        private double job_siteY = 0;
        public double JobSiteY
        {
            set { job_siteY = value; OnPropertyChanged(); }
            get
            {
                return job_siteY;
            }
        }
        private double job_siteZ = 0;
        public double JobSiteZ
        {
            set { job_siteZ = value; OnPropertyChanged(); }
            get
            {
                return job_siteZ;
            }
        }
        private TextComboBoxItem jobSiteDimension = null;
        public TextComboBoxItem JobSiteDimension
        {
            get { return jobSiteDimension; }
            set
            {
                jobSiteDimension = value;
                OnPropertyChanged();
            }
        }
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
                string pos = JobSiteX.ToString().Trim() != "" && JobSiteY.ToString().Trim() !="" && JobSiteZ.ToString().Trim() != ""? "pos:[" + JobSiteX + "," + JobSiteY + "," + JobSiteZ + "],":"";
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

        #region 村民属性

        #region 数据源
        string VillagerTypeSourceFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Villager\Data\VillagerTypes.ini";
        string VillagerProfessionsSourceFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Villager\Data\VillagerProfessionTypes.ini";
        string VillagerLevelSourceFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Villager\Data\VillagerLevels.ini";

        public Dictionary<string, string> VillagerTypeDataBase = [];
        public Dictionary<string, string> VillagerProfessionTypeDataBase = [];

        public ObservableCollection<TextComboBoxItem> VillagerTypeSource { get; set; } = [];
        public ObservableCollection<TextComboBoxItem> VillagerProfessionTypeSource { get; set; } = [];
        public ObservableCollection<TextComboBoxItem> VillagerLevelSource { get; set; } = [];
        #endregion

        #region 村民种类
        private TextComboBoxItem villagerType;
        public TextComboBoxItem VillagerType
        {
            get
            {
                return villagerType;
            }
            set
            {
                villagerType = value;
                OnPropertyChanged();
            }
        }
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
        private TextComboBoxItem villagerProfessionType;
        public TextComboBoxItem VillagerProfessionType
        {
            get { return villagerProfessionType; }
            set
            {
                villagerProfessionType = value;
                OnPropertyChanged();
            }
        }
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
        private TextComboBoxItem villagerLevel;
        public TextComboBoxItem VillagerLevel
        {
            get { return villagerLevel; }
            set
            {
                villagerLevel = value;
                OnPropertyChanged();
            }
        }
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

        #region 村民数据
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
        #endregion

        #endregion

        #region 是否愿意交配
        private bool willing = false;
        public bool Willing
        {
            get => willing;
            set => SetProperty(ref willing, value);
        }
        private string WillingString
        {
            get => Willing ? "Willing:1b," : "";
        }
        #endregion

        #region 此村民最后一次前往工作站点重新供应交易的刻
        private double lastRestock = 0;
        public double LastRestock
        {
            get { return lastRestock; }
            set { lastRestock = value; OnPropertyChanged(); }
        }
        private string LastRestockString
        {
            get
            {
                return LastRestock.ToString().Trim() != ""? "LastRestock:" +LastRestock+",":"";
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

        public VillagerViewModel(IContainerProvider container,MainView mainView,CBHKDataContext context,DataService dataService)
        {
            _dataService = dataService;
            _context = context;
            _container = container;
            home = mainView;

            #region 初始化主、副、结果空图像
            BuyItemIcon = BuyBItemIcon = SellItemIcon = new BitmapImage(new Uri(emptyIcon));
            #endregion

            #region 读取言论类型
            if (File.Exists(GossipTypesFilePath))
            {
                string[] types = File.ReadAllLines(GossipTypesFilePath);
                for (int i = 0; i < types.Length; i++)
                    GossipTypes.Add(new() { Text = types[i] });
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

            ItemIDAndNameMap = _dataService.ItemGroupByVersionDicionary
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
                        if (JObject.Parse(nbt)["id"] is JToken IDToken)
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
        private async void SaveCommand()
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
                await File.WriteAllTextAsync(saveFileDialog.FileName, Result);
                await File.WriteAllTextAsync(AppDomain.CurrentDomain.BaseDirectory + "resources\\saves\\Villager\\" + Path.GetFileName(saveFileDialog.FileName), Result);
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
                DisplayerView displayer = _container.Resolve<DisplayerView>();
                if (displayer is not null && displayer.DataContext is DisplayerViewModel displayerViewModel)
                {
                    displayerViewModel.GeneratorResult(Result, "村民", iconPath);
                }
            }
            else
                Clipboard.SetText(Result);
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
                DisplayerView displayer = _container.Resolve<DisplayerView>();
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
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            gossipItems.Add(gossipsItem);
        }

        [RelayCommand]
        /// <summary>
        /// 清空言论控件
        /// </summary>
        private void ClearGossipItem()
        {
            gossipItems.Clear();
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
            if(CanEditGossips)
            {
                string current_type = SelectedSearchGossipItem.Text;
                List<GossipsItemsView> target_gossip = gossipItems.Where(gossip =>
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

            if (hitTestResult != null)
            {
                var item = hitTestResult.VisualHit;

                while (item != null && item is not ListViewItem)
                    item = VisualTreeHelper.GetParent(item);

                if (item != null)
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
            if (SelectedItem != null)
            {
                IsGrabingItem = true;

                DependencyObject obj = (DependencyObject)e.OriginalSource;
                while (obj != null && obj is not ListViewItem)
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

                if (IsGrabingItem && drag_source != null)
                {
                    DataObject dataObject = new(typeof(Image), GrabedImage);
                    if (dataObject != null)
                        DragDrop.DoDragDrop(drag_source, dataObject, DragDropEffects.Move);
                    IsGrabingItem = false;
                }
            }
        }
    }
}