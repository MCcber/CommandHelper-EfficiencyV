using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Data;
using System.Windows.Media;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Data;
using CBHK.View;
using Prism.Ioc;
using CBHK.Model.Data;
using CBHK.Domain;
using CBHK.Utility.Visual.MessageTip;
using CBHK.Utility.Common;
using CBHK.CustomControl.VectorComboBox;
using CBHK.Model.Constant;
using Prism.Events;
using CBHK.ViewModel.Component.Villager.Event;
using CBHK.ViewModel.Component.Villager.Control;
using CBHK.Utility.Data;
using System.Text.RegularExpressions;

namespace CBHK.ViewModel.Generator
{
    public partial class VillagerViewModel:ObservableObject
    {
        #region Field
        private Resource resource;
        private MessagePopup messagePopup = new();
        private CBHKDataContext context = null;
        private IEventAggregator eventAggregator = null;

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
        string iconPath = "pack://application:,,,/CBHK;component/Resource/Common/Image/SpawnerIcon/IconVillager.png";
        string VillagerTypeSourceFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Villager\Data\VillagerTypes.ini";
        string VillagerProfessionsSourceFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Villager\Data\VillagerProfessionTypes.ini";
        string VillagerLevelSourceFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Villager\Data\VillagerLevels.ini";

        public Dictionary<string, string> VillagerTypeDataBase = [];
        public Dictionary<string, string> VillagerProfessionTypeDataBase = [];
        string emptyIcon = "pack://application:,,,/CBHK;component/Resource/CBHK/Image/empty.png";
        string gossipTypesFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Villager\Data\GossipTypeList.ini";
        private readonly SolidColorBrush transparentBrush = new((Color)ColorConverter.ConvertFromString("Transparent"));
        private readonly SolidColorBrush whiteBrush = new((Color)ColorConverter.ConvertFromString("#FFFFFF"));
        private readonly SolidColorBrush blackBrush = new((Color)ColorConverter.ConvertFromString("#000000"));

        private DataService dataService = null;
        private IContainerProvider container;

        /// <summary>
        /// 加载物品集合
        /// </summary>
        private string ItemSaveFolderPath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Saves\Item";

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
        private Visibility leastPlayerUUIDVisibility = Visibility.Collapsed;
        [ObservableProperty]
        private Visibility mostPlayerUUIDVisibility = Visibility.Collapsed;
        [ObservableProperty]
        private Visibility playerUUIDVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private string leastPlayerUUID = "";
        [ObservableProperty]
        private string mostPlayerUUID = "";

        [ObservableProperty]
        private string playerUUID = "";
        [ObservableProperty]
        private VectorTextComboBoxItem _selectedVersion;
        /// <summary>
        /// 版本源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<VectorTextComboBoxItem> _versionSource = [
            new VectorTextComboBoxItem() { Text = "1.20.2+" },
            new VectorTextComboBoxItem() { Text = "1.16" },
            new VectorTextComboBoxItem() { Text = "1.13.0" }
            ];
        /// <summary>
        /// 是否显示结果
        /// </summary>
        [ObservableProperty]
        public bool _showResult;
        [ObservableProperty]
        public ObservableCollection<VectorTextComboBoxItem> _gossipTypeList = [];

        [ObservableProperty]
        private string searchGossipText = "";

        /// <summary>
        /// 左侧交易项数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<TransactionItemViewModel> _transactionItemList = [];
        /// <summary>
        /// 言论数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<GossipsItemViewModel> _gossipItemList = [];
        /// <summary>
        /// 当前选中的交易项
        /// </summary>
        [ObservableProperty]
        private TransactionItemViewModel currentTransactionItem = null;
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
        private VectorTextComboBoxItem _selectedSearchGossipItem;
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
        ObservableCollection<VectorTextComboBoxItem> _gossipSearchTypeList = [];

        /// <summary>
        /// 维度数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<VectorTextComboBoxItem> _dimensionTypeSource = [];
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
        /// 搜索原版物品
        /// </summary>
        [ObservableProperty]
        private string searchOriginItemText = "";

        /// <summary>
        /// 搜索自定义物品
        /// </summary>
        [ObservableProperty]
        private string searchCustomItemText = "";

        /// <summary>
        /// 村民类型数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<VectorTextComboBoxItem> _villagerTypeSource = [];
        /// <summary>
        /// 村民职业数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<VectorTextComboBoxItem> _villagerProfessionTypeSource = [];
        /// <summary>
        /// 村民交易等级数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<VectorTextComboBoxItem> _villagerLevelSource = [];
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
                string transactionItemData = string.Join("", TransactionItemList.Select(item => item.GetTransactionItemData() + ","));
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
                result += string.Join(",", GossipItemList.Select(item => item.GetGossipData()));
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
        private VectorTextComboBoxItem _meetingPointDimension = null;
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
        private VectorTextComboBoxItem _homeDimension = null;
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
        private VectorTextComboBoxItem _jobSiteDimension = null;
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
        private VectorTextComboBoxItem _villagerType;
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
        private VectorTextComboBoxItem _villagerProfessionType;
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
        private VectorTextComboBoxItem _villagerLevel;
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
        public VillagerViewModel(IContainerProvider Container,IEventAggregator EventAggregator,MainView mainView,CBHKDataContext Context,DataService DataService,Resource resource)
        {
            #region Inject
            this.resource = resource;
            dataService = DataService;
            context = Context;
            container = Container;
            home = mainView;
            eventAggregator = EventAggregator;
            #endregion

            #region 初始化主、副、结果空图像
            BuyItemIcon = BuyBItemIcon = SellItemIcon = new BitmapImage(new Uri(emptyIcon));
            #endregion

            #region 读取言论类型与搜索言论类型
            if (File.Exists(gossipTypesFilePath))
            {
                string[] types = File.ReadAllLines(gossipTypesFilePath);
                for (int i = 0; i < types.Length; i++)
                {
                    GossipTypeList.Add(new() { Text = types[i] });
                }
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
                    VillagerTypeSource.Add(new VectorTextComboBoxItem() { Text = name });
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

                    VillagerProfessionTypeSource.Add(new VectorTextComboBoxItem() { Text = name });
                }
            }
            if (File.Exists(VillagerLevelSourceFilePath))
            {
                int level = int.Parse(File.ReadAllText(VillagerLevelSourceFilePath));
                for (int i = 1; i <= level; i++)
                {
                    VillagerLevelSource.Add(new VectorTextComboBoxItem() { Text = i.ToString() });
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

                        DimensionTypeSource.Add(new VectorTextComboBoxItem() { Text = name });
                    }
                }
            }
            MeetingPointDimension = HomeDimension = JobSiteDimension = DimensionTypeSource[0];
            #endregion

            #region 订阅事件
            eventAggregator.GetEvent<RemoveTransactionItemEvent>().Subscribe(RemoveTransactionItem);
            eventAggregator.GetEvent<OpenTransactionEditPanelEvent>().Subscribe(OpenTransactionEditPanel);
            eventAggregator.GetEvent<RemoveGossipItemEvent>().Subscribe(RemoveGossipItem);
            eventAggregator.GetEvent<UpdateGossipEffectEvent>().Subscribe(UpdateGossipEffect);
            #endregion
        }

        /// <summary>
        /// 初始化原版物品列表（物品库后台与配方页共用）
        /// </summary>
        private void InitOriginItemList()
        {
            _ = ApplyOriginalItemsAsync();
        }

        private async Task ApplyOriginalItemsAsync()
        {
            List<ItemStructure> items = await ItemLibraryLoader.LoadOriginalItemsAsync(resource);
            Application.Current.Dispatcher.Invoke(() =>
            {
                OriginalItemList.Clear();
                foreach (ItemStructure item in items)
                    OriginalItemList.Add(item);
            });
        }

        /// <summary>
        /// 初始化自定义物品列表（物品库后台与配方页共用）
        /// </summary>
        private void InitCustomItemList()
        {
            _ = ApplyCustomItemsAsync();
        }

        private async Task ApplyCustomItemsAsync()
        {
            List<ItemStructure> items = await ItemLibraryLoader.LoadCustomItemsAsync(resource);
            Application.Current.Dispatcher.Invoke(() =>
            {
                CustomItemList = new ObservableCollection<ItemStructure>(items);
            });
        }

        /// <summary>
        /// 删除指定交易项
        /// </summary>
        /// <param name="transactionItem"></param>
        private void RemoveTransactionItem(TransactionItemViewModel transactionItem) => TransactionItemList.Remove(transactionItem);

        /// <summary>
        /// 删除指定言论项
        /// </summary>
        /// <param name="gossipsItem"></param>
        private void RemoveGossipItem(GossipsItemViewModel gossipsItem) => GossipItemList.Remove(gossipsItem);

        private void UpdateGossipEffect(GossipsItemViewModel gossipsItem)
        {
            //创建字典，让字典的键来自动匹配对应的值，自动执行搜索和带入公式两个行为
            Dictionary<string, int> currentGossipTypes = [];
            //已处理的标记
            Dictionary<string, bool> handedMarkers = [];
            foreach (var item in GossipTypeList)
            {
                currentGossipTypes.Add(item.Text, 0);
                handedMarkers.Add(item.Text, false);
            }
            string currentType = gossipsItem.SelectedTypeItem.Text;
            string currentUID = gossipsItem.TargetText;
            currentGossipTypes[currentType] = int.Parse(gossipsItem.GossipValue.ToString());
            handedMarkers[currentType] = true;

            _ = GossipItemList.Where(gossipsItem =>
            {
                string currentGossipType = gossipsItem.SelectedTypeItem.Text;
                if (currentGossipTypes.ContainsKey(currentGossipType) && gossipsItem.TargetText == currentUID && !handedMarkers[currentGossipType])
                {
                    currentGossipTypes[currentGossipType] = int.Parse(gossipsItem.GossipValue.ToString());
                    handedMarkers[currentGossipType] = true;
                }
                return true;
            });

            if (currentGossipTypes.Count == 5)
                _ = TransactionItemList.All(item => { item.UpdateDiscountData(currentGossipTypes["minor_negative"], currentGossipTypes["minor_positive"], currentGossipTypes["major_negative"], currentGossipTypes["major_positive"], currentGossipTypes["trading"]); return true; });
        }

        /// <summary>
        /// 打开交易项编辑面板
        /// </summary>
        /// <param name="targetItem"></param>
        private void OpenTransactionEditPanel(TransactionItemViewModel targetItem)
        {
            TransactionDataGridVisibility = Visibility.Visible;
            RewardExp = targetItem.RewardExp;
            VillagerGetXp = targetItem.Xp;

            BuyItemIcon = targetItem.BuyImageSource.Clone();

            BuyBItemIcon = targetItem.BuyBImageSource.Clone();
            SellItemIcon = targetItem.SellImageSource.Clone();

            BuyItemData = targetItem.BuyTag;
            BuyBItemData = targetItem.BuyBTag;
            SellItemData = targetItem.SellTag;

            BuyCount = int.Parse(targetItem.BuyCountDisplayText.Replace("x", ""));
            BuyBCount = int.Parse(targetItem.BuyBCountDisplayText.Replace("x", ""));
            SellCount = int.Parse(targetItem.SellCountDisplayText.Replace("x", ""));

            MaxUses = targetItem.MaxUses;
            Uses = targetItem.Uses;
            Demand = targetItem.Demand;
            SpecialPrice = targetItem.SpecialPrice;
            PriceMultiplier = targetItem.PriceMultiplier;
            CurrentTransactionItem = targetItem;
        }

        /// <summary>
        /// 根据版本更新不同UUID文本框的可见性
        /// </summary>
        private void UpdateUUIDVisibility()
        {
            string selectedVersion = SelectedVersion.Text;
            LeastPlayerUUIDVisibility = MostPlayerUUIDVisibility = PlayerUUIDVisibility = Visibility.Collapsed;
            if (VersionRangeChecker.IsLessThan(selectedVersion, "1.16") && IsEditGossips is Visibility.Visible)
            {
                LeastPlayerUUIDVisibility = MostPlayerUUIDVisibility = Visibility.Visible;
            }
            else
            {
                LeastPlayerUUIDVisibility = MostPlayerUUIDVisibility = Visibility.Collapsed;
            }

            if ((VersionRangeChecker.IsGreaterThan(selectedVersion, "1.16") || selectedVersion == "1.16") && IsEditGossips is Visibility.Visible)
            {
                PlayerUUIDVisibility = Visibility.Visible;
            }
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
        public async void OriginalItemListView_Loaded(object sender,RoutedEventArgs e)
        {
            Bag = sender as ListView;
            Bag.DataContext = this;
            Bag.PreviewMouseLeftButtonDown += SelectItemClickDown;
            Bag.MouseMove += Bag_MouseMove;
            Bag.MouseLeave += ListBox_MouseLeave;
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
            CustomBag = sender as ListView;
            CustomBag.DataContext = this;
            CustomBag.PreviewMouseLeftButtonDown += SelectItemClickDown;
            CustomBag.MouseMove += Bag_MouseMove;
            CustomBag.MouseLeave += ListBox_MouseLeave;
            if (CustomItemList.Count == 0)
            {
                Window parent = Window.GetWindow(sender as ListView);
                CustomViewSource = parent.FindResource("CustomItemView") as CollectionViewSource;
                InitCustomItemList();
                CustomViewSource.Filter += CollectionViewSource_Filter;
            }
        }

        /// <summary>
        /// 版本更新事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void VersionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateUUIDVisibility();
        }

        /// <summary>
        /// 展开/折叠言论面板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [RelayCommand]
        public void ClickGossipEditButton()
        {
            IsEditGossips = CanEditGossip ? Visibility.Visible : Visibility.Collapsed;
            //恢复所有交易项的价格
            if (!CanEditGossip)
            {
                _ = TransactionItemList.All(item => { item.HideDiscountData(); return true; });
            }
            else
            {
                _ = TransactionItemList.All(item => { item.HideDiscountData(false); return true; });
            }
            OnlyEditItem = !CanEditBrain && !CanEditGossip ? Visibility.Collapsed : Visibility.Visible;
            if (IsEditGossips is Visibility.Collapsed)
            {
                SelectedItemListIndex = 2;
            }
            UpdateUUIDVisibility();
        }

        /// <summary>
        /// 展开/折叠记忆面板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [RelayCommand]
        public void ClickBrainEditButton()
        {
            IsEditBrain = CanEditBrain ? Visibility.Visible : Visibility.Collapsed;
            OnlyEditItem = !CanEditBrain && !CanEditGossip ? Visibility.Collapsed : Visibility.Visible;
            if (IsEditBrain is Visibility.Collapsed && IsEditGossips is Visibility.Collapsed)
            {
                SelectedItemListIndex = 2;
            }
            else if (IsEditBrain is Visibility.Collapsed && IsEditGossips is Visibility.Visible)
            {
                SelectedItemListIndex = 3;
            }
        }

        [RelayCommand]
        private void RespawnPlayerUUID()
        {
            if (LeastPlayerUUIDVisibility is Visibility.Visible || MostPlayerUUIDVisibility is Visibility.Visible)
            {
                var (Most, Least) = UUIDHelper.SplitGuid(Guid.NewGuid());
                LeastPlayerUUID = Least;
                MostPlayerUUID = Most;
            }
            else if (PlayerUUIDVisibility is Visibility.Visible)
            {
                int[] UUIDIntArray = UUIDHelper.GuidToMinecraftIntArray(Guid.NewGuid());
                PlayerUUID = string.Join(',', UUIDIntArray);
            }
        }

        /// <summary>
        /// 搜索原版物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SearchOriginItemTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            OriginalViewSource.View?.Refresh();
        }

        /// <summary>
        /// 搜索自定义物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SearchCustomItemTextBox_TextChanged(object sender,TextChangedEventArgs e)
        {
            CustomViewSource.View?.Refresh();
        }

        [RelayCommand]
        /// <summary>
        /// 关闭交易数据设置面板
        /// </summary>
        public void CloseTransactionPanel()
        {
            TransactionDataGridVisibility = Visibility.Collapsed;
            //更新当前交易项的数量显示
            CurrentTransactionItem.RewardExp = RewardExp is not null && RewardExp.Value;
            CurrentTransactionItem.BuyCountDisplayText = "x" + BuyCount.ToString();
            CurrentTransactionItem.BuyBCountDisplayText = "x" + BuyBCount.ToString();
            CurrentTransactionItem.SellCountDisplayText = "x" + SellCount.ToString();
            CurrentTransactionItem.MaxUses = MaxUses;
            CurrentTransactionItem.Uses = Uses;
            CurrentTransactionItem.Xp = VillagerGetXp;
            CurrentTransactionItem.Demand = Demand;
            CurrentTransactionItem.SpecialPrice = SpecialPrice;
            CurrentTransactionItem.PriceMultiplier = PriceMultiplier;
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
                ImportVillagerData(dialog.FileName, messagePopup);
        }

        [RelayCommand]
        /// <summary>
        /// 从剪切板导入
        /// </summary>
        private void ImportFromClipboard()
        {
            ImportVillagerData(Clipboard.GetText(), messagePopup, isPath: false);
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
            if (SearchOriginItemText.Trim().Length > 0)
            {
                e.Accepted = false;
                ItemStructure itemStructure = e.Item as ItemStructure;
                if (itemStructure.IDAndName is not null && itemStructure.ImagePath is not null)
                {
                    string currentItemID = Path.GetFileNameWithoutExtension(itemStructure.ImagePath.ToString());
                    string currentItemName = "";
                    if (itemStructure.IDAndName.Contains(':'))
                    {
                        currentItemName = itemStructure.IDAndName.Split(':')[1];
                    }

                    if (currentItemID.StartsWith(SearchOriginItemText) || currentItemName.StartsWith(SearchOriginItemText))
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
        private void Run()
        {
            #region UUID
            string UUIDString = "";
            string selectedVersion = SelectedVersion.Text;
            if (VersionRangeChecker.IsLessThan(selectedVersion, "1.16"))
            {
                UUIDString = "UUIDLeast:" + LeastPlayerUUID + ",UUIDMost:" + MostPlayerUUID;
            }
            else if (VersionRangeChecker.IsGreaterThan(selectedVersion, "1.16") || selectedVersion == "1.16")
            {
                UUIDString = "UUID:[I;" + PlayerUUID + ']';
            } 
            #endregion

            Result = "";
            Result += WillingString + VillagerData + Offers + Gossips + Brain + LastRestockString + XpString + UUIDString;
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
                messagePopup.PushMessage(new GeneratorMessage()
                {
                    Message = "生成成功！",
                    SubMessage = "村民生成器",
                    Icon = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\villager.png", UriKind.RelativeOrAbsolute))
                });
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
            TransactionItemViewModel transactionItem = container.Resolve<TransactionItemViewModel>();
            TransactionItemList.Add(transactionItem);
        }

        [RelayCommand]
        /// <summary>
        /// 清空交易项控件
        /// </summary>
        private void ClearTransactionItem()
        {
            TransactionDataGridVisibility = Visibility.Collapsed;
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
            GossipsItemViewModel gossipsItem = container.Resolve<GossipsItemViewModel>();
            GossipItemList.Add(gossipsItem);
            gossipsItem.GossipTypeList = GossipTypeList;
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
        /// 查找目标言论并更新价格数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SearchGossipsTextChanged(object sender, TextChangedEventArgs e)
        {
            if(CanEditGossip)
            {
                string currentType = SelectedSearchGossipItem.Text;
                if (!string.IsNullOrEmpty(SearchGossipText.Trim()))
                {
                    SearchGossipText = SearchGossipText.Trim();
                    for (int i = 0; i < GossipItemList.Count; i++)
                    {
                        string type = GossipItemList[i].SelectedTypeItem.Text;
                        GossipItemList[i].Visibility = Visibility.Collapsed;
                        if (GossipItemList[i].TargetText == SearchGossipText && type == currentType)
                        {
                            GossipItemList[i].Visibility = Visibility.Visible;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < GossipItemList.Count; i++)
                    {
                        GossipItemList[i].Visibility = Visibility.Visible;
                    }
                }
            }
        }

        /// <summary>
        /// 搜索言论类型变更事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SearchType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            SearchGossipsTextChanged(null, null);
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

        #region 外部数据导入（并入本类，不再单独成文件）
        public void ImportVillagerData(string filePathOrData, MessagePopup messagePopup, bool isPath = true)
        {
            string data = isPath ? File.ReadAllText(filePathOrData) : filePathOrData;

            #region 提取可用NBT数据和村民ID
            string nbtData = "";
            if (data.Contains('{') && data.Contains('}'))
                nbtData = data[data.IndexOf('{')..(data.LastIndexOf('}') + 1)];
            //补齐缺失双引号对的key
            nbtData = Regex.Replace(nbtData, @"([\{\[,])([\s+]?\w+[\s+]?):", "$1\"$2\":");
            //清除数值型数据的单位
            nbtData = Regex.Replace(nbtData, @"(\d+[\,\]\}]?)([a-zA-Z])", "$1").Replace("I;", "");
            #endregion

            try
            {
                JObject nbtObj = JObject.Parse(nbtData);
                if (nbtObj.SelectToken("EntitTag") is JObject entityTag)
                    nbtObj = JObject.Parse(entityTag.ToString());
                AddVillagerData(nbtObj);
            }
            catch
            {
                messagePopup.PushMessage(new GeneratorMessage()
                {
                    Message = "导入失败！文件内容格式不合法",
                    MessageBrush = Brushes.Red,
                    SubMessage = "村民生成器",
                    SubMessageBrush = Brushes.DarkGray,
                    Icon = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\ImageSet\villager.png", UriKind.Relative))
                });
            }
        }

        /// <summary>
        /// 设置村民数据
        /// </summary>
        /// <param name="nbtObj"></param>
        /// <param name="context"></param>
        private void AddVillagerData(JObject nbtObj)
        {
            #region 处理交易数据
            if (nbtObj.SelectToken("Offers.Recipes") is JArray Recipes)
            {
                string rootPath = AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\";
                foreach (JObject recipe in Recipes.Cast<JObject>())
                {
                    #region 读取数据
                    #region buy,buyB,sell
                    string buyID = "";
                    JToken buyCountObj = recipe.SelectToken("buy.Count");

                    string buyBID = "";
                    JToken buyBCountObj = recipe.SelectToken("buyB.Count");

                    string sellID = "";
                    JToken sellCountObj = recipe.SelectToken("sell.Count");
                    #endregion
                    #region other
                    JToken demand = recipe.SelectToken("demand");
                    JToken maxUses = recipe.SelectToken("maxUses");
                    JToken priceMultiplier = recipe.SelectToken("priceMultiplier");
                    JToken rewardExp = recipe.SelectToken("rewardExp");
                    JToken specialPrice = recipe.SelectToken("specialPrice");
                    JToken uses = recipe.SelectToken("uses");
                    JToken xp = recipe.SelectToken("xp");
                    #endregion
                    #endregion

                    #region 应用数据
                    #region buy
                    if (recipe.SelectToken("buy.oldID") is JToken buyIDObj)
                        buyID = buyIDObj.ToString().Replace("minecraft:", "");
                    bool ExistItem = false;
                    if (buyID.Length > 0)
                    {
                        ExistItem = true;
                        AddTransactionItem();
                        TransactionItemViewModel transactionItemsViewModel = TransactionItemList[^1];
                        string iconPath = rootPath + buyID + ".png";
                        if (File.Exists(iconPath))
                            transactionItemsViewModel.BuyImageSource = new BitmapImage(new Uri(iconPath, UriKind.Absolute));

                        if (buyCountObj is not null)
                        {
                            transactionItemsViewModel.BuyCountDisplayText = "x" + int.Parse(buyCountObj.ToString());
                        }
                        Uri iconUri = new(iconPath, UriKind.Absolute);
                        ItemStructure imageTag = new()
                        {
                            ImagePath = new BitmapImage(iconUri),
                            IDAndName = buyID,
                            NBT = recipe.SelectToken("buy.tag") is JObject buyTagObj ? buyTagObj.ToString() : ""
                        };
                        transactionItemsViewModel.BuyImageSource = new BitmapImage(iconUri);
                        transactionItemsViewModel.BuyTag = imageTag;
                    }
                    #endregion
                    #region buyB
                    if (ExistItem)
                    {
                        if (recipe.SelectToken("buyB.oldID") is JToken buyBIDObj)
                            buyBID = buyBIDObj.ToString().Replace("minecraft:", "");
                        if (buyBID.Length > 0)
                        {
                            string iconPath = rootPath + buyBID + ".png";
                            TransactionItemViewModel transactionItemsViewModel = TransactionItemList[^1];
                            if (File.Exists(iconPath))
                                transactionItemsViewModel.BuyBImageSource = new BitmapImage(new Uri(iconPath, UriKind.Absolute));

                            if (buyBCountObj is not null)
                            {
                                transactionItemsViewModel.BuyBCountDisplayText = "x" + int.Parse(buyCountObj.ToString());
                            }
                            Uri iconUri = new(iconPath, UriKind.Absolute);
                            ItemStructure imageTag = new()
                            {
                                ImagePath = new BitmapImage(iconUri),
                                IDAndName = buyBID,
                                NBT = recipe.SelectToken("buyB.tag") is JObject buyTagObj ? buyTagObj.ToString() : ""
                            };
                            transactionItemsViewModel.BuyBImageSource = new BitmapImage(iconUri);
                            transactionItemsViewModel.BuyBTag = imageTag;
                        }
                    }
                    #endregion
                    #region sell
                    if (ExistItem)
                    {
                        if (recipe.SelectToken("sell.oldID") is JToken sellIDObj)
                            sellID = sellIDObj.ToString().Replace("minecraft:", "");
                        if (sellID.Length > 0)
                        {
                            string iconPath = rootPath + sellID + ".png";
                            TransactionItemViewModel transactionItemsViewModel = TransactionItemList[^1];
                            if (File.Exists(iconPath))
                                transactionItemsViewModel.SellImageSource = new BitmapImage(new Uri(iconPath, UriKind.Absolute));

                            if (sellCountObj is not null)
                            {
                                transactionItemsViewModel.SellCountDisplayText = "x" + int.Parse(sellCountObj.ToString());
                            }
                            Uri iconUri = new(iconPath, UriKind.Absolute);
                            ItemStructure imageTag = new()
                            {
                                ImagePath = new BitmapImage(iconUri),
                                IDAndName = buyBID,
                                NBT = recipe.SelectToken("buyB.tag") is JObject buyTagObj ? buyTagObj.ToString() : ""
                            };
                            transactionItemsViewModel.SellImageSource = new BitmapImage(iconUri);
                            transactionItemsViewModel.SellTag = imageTag;
                        }
                    }
                    #endregion
                    #region other
                    if (ExistItem)
                    {
                        TransactionItemViewModel transactionItemsViewModel = TransactionItemList[^1];
                        transactionItemsViewModel.Demand = int.Parse(demand.ToString());
                        transactionItemsViewModel.MaxUses = int.Parse(maxUses.ToString());
                        transactionItemsViewModel.PriceMultiplier = int.Parse(priceMultiplier.ToString());
                        transactionItemsViewModel.RewardExp = rewardExp.ToString() == "1" || rewardExp.ToString() == "true";
                        transactionItemsViewModel.SpecialPrice = int.Parse(specialPrice.ToString());
                        transactionItemsViewModel.Uses = int.Parse(uses.ToString());
                        transactionItemsViewModel.Xp = int.Parse(xp.ToString());
                    }
                    #endregion
                    #endregion
                }
            }
            #endregion

            #region 处理言论数据
            if (nbtObj.SelectToken("Gossips") is JArray Gossips)
            {
                foreach (JObject gossip in Gossips.Cast<JObject>())
                {
                    AddGossipItem();
                    JToken value = gossip.SelectToken("Text");
                    GossipsItemViewModel gossipsItemsViewModel = GossipItemList[^1];
                    if (gossip.SelectToken("Target") is JArray targetUID)
                        gossipsItemsViewModel.TargetText = targetUID[0].ToString() + "," + targetUID[1].ToString() + "," + targetUID[2].ToString() + "," + targetUID[3].ToString();
                    if (gossip.SelectToken("Type") is JObject type)
                        gossipsItemsViewModel.SelectedTypeItemPath = type.ToString();
                    if (value is not null)
                        gossipsItemsViewModel.GossipValue = int.Parse(value.ToString());
                }
            }
            #endregion

            #region 处理种类、职业、等级等数据
            if (nbtObj.SelectToken("VillagerData") is JObject VillagerData)
            {
                JToken level = VillagerData.SelectToken("level");
                JToken profession = VillagerData.SelectToken("profession");
                JToken type = VillagerData.SelectToken("type");
                if (level is not null)
                    VillagerLevel = VillagerLevelSource.Where(item => item.Text == level.ToString()).First();
                if (profession is not null)
                {
                    string professionType = VillagerProfessionTypeDataBase.Where(item => item.Key == profession.ToString().Replace("minecraft:", "")).First().Value;
                    VillagerProfessionType = VillagerProfessionTypeSource.Where(item => item.Text == professionType).First();
                }
                if (type is not null)
                {
                    string villagerType = VillagerTypeDataBase.Where(item => item.Key == type.ToString().Replace("minecraft:", "")).First().Value;
                    VillagerType = VillagerTypeSource.Where(item => item.Text == villagerType).First();
                }
            }
            #endregion

            #region 交配意愿
            if (nbtObj.SelectToken("Willing") is JToken Willing)
                Willing = Willing.ToString() == "1" || Willing.ToString() == "true";
            #endregion

            #region 补货间隔
            if (nbtObj.SelectToken("LastRestock") is JToken LastRestock)
                LastRestock = double.Parse(LastRestock.ToString());
            #endregion

            #region 当前经验
            if (nbtObj.SelectToken("Xp") is JToken Xp)
                Xp = int.Parse(Xp.ToString());
            #endregion
        }
        #endregion
    }
}
