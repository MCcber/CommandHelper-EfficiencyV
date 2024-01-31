using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using cbhk.Generators.VillagerGenerator.Components;
using cbhk.WindowDictionaries;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Data;
using System.Windows.Media;
using System.Threading.Tasks;
using cbhk.GeneralTools.Displayer;
using cbhk.CustomControls;
using Newtonsoft.Json.Linq;
using cbhk.GeneralTools;
using System.Data;
using cbhk.GeneralTools.MessageTip;

namespace cbhk.Generators.VillagerGenerator
{
    public partial class VillagerDataContext:ObservableObject
    {
        public DataTable ItemTable = null;

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
            }
        }

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
        public Window home = null;
        //本生成器的图标路径
        string icon_path = "pack://application:,,,/cbhk;component/resources/common/images/spawnerIcons/IconVillagers.png";
        string emptyIcon = "pack://application:,,,/cbhk;component/resources/cbhk/images/empty.png";
        string GossipTypesFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Villager\\data\\GossipTypes.ini";
        SolidColorBrush orangeBrush = new((Color)ColorConverter.ConvertFromString("#D77933"));
        SolidColorBrush transparentBrush = new((Color)ColorConverter.ConvertFromString("Transparent"));
        /// <summary>
        /// 排版索引
        /// </summary>
        public int compositionIndex = 1;
        public ObservableCollection<string> GossipTypes { get; set; } = [];
        //左侧交易项数据源
        public ObservableCollection<TransactionItems> transactionItems { get; set; } = [];
        //言论数据源
        public ObservableCollection<GossipsItems> gossipItems { get; set; } = [];

        #region 当前选中的物品
        private TransactionItems currentItem = null;
        public TransactionItems CurrentItem
        {
            get => currentItem;
            set => SetProperty(ref currentItem,value);
        }
        #endregion

        #region 已选中的搜索言论成员
        private TextComboBoxItem selectedSearchGossipItem;

        public TextComboBoxItem SelectedSearchGossipItem
        {
            get => selectedSearchGossipItem;
            set => SetProperty(ref selectedSearchGossipItem, value);
        }

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
        public ObservableCollection<ItemStructure> OriginalItemList { get; set; } = [];
        /// <summary>
        /// 自定义物品库
        /// </summary>
        public ObservableCollection<ItemStructure> CustomItemList { get; set; } = [];
        //物品描述引用
        public ObservableCollection<string> BagItemToolTips { get; set; } = [];
        //言论搜索类型数据源
        ObservableCollection<TextComboBoxItem> GossipSearchType = [];
        //言论搜索类型配置文件路径
        string gossipSearchTypeFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Villager\\data\\GossipSearchTypes.ini";
        //维度数据源
        ObservableCollection<string> DimensionTypeSource = [];
        //维度数据源配置文件路径
        string dimensionTypeFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Villager\\data\\DimensionTypes.ini";
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
        private Visibility transactionDataGridVisibility = Visibility.Collapsed;
        public Visibility TransactionDataGridVisibility
        {
            get => transactionDataGridVisibility;
            set => SetProperty(ref transactionDataGridVisibility,value);
        }
        #endregion

        #region 主、副、结果物品图像源
        private ImageSource buyItemIcon = null;
        public ImageSource BuyItemIcon
        {
            get => buyItemIcon;
            set => SetProperty(ref buyItemIcon,value);
        }
        private ImageSource buyBItemIcon = null;
        public ImageSource BuyBItemIcon
        {
            get => buyBItemIcon;
            set => SetProperty(ref buyBItemIcon, value);
        }
        private ImageSource sellItemIcon = null;
        public ImageSource SellItemIcon
        {
            get => sellItemIcon;
            set => SetProperty(ref sellItemIcon, value);
        }
        #endregion

        #region 主、副、结果物品数据
        private object buyItemData = null;
        public object BuyItemData
        {
            get => buyItemData;
            set => SetProperty(ref buyItemData, value);
        }
        private object buyBItemData = null;
        public object BuyBItemData
        {
            get => buyBItemData;
            set => SetProperty(ref buyBItemData, value);
        }
        private object sellItemData = null;
        public object SellItemData
        {
            get => sellItemData;
            set => SetProperty(ref sellItemData, value);
        }
        #endregion

        #region 主、副、结果数量
        private object buyItemCount = null;
        public object BuyItemCount
        {
            get => buyItemCount;
            set => SetProperty(ref buyItemCount, value);
        }
        private object buyBItemCount = null;
        public object BuyBItemCount
        {
            get => buyBItemCount;
            set => SetProperty(ref buyBItemCount, value);
        }
        private object sellItemCount = null;
        public object SellItemCount
        {
            get => sellItemCount;
            set => SetProperty(ref sellItemCount, value);
        }
        #endregion

        #region 交易特指数据
        private bool? rewardExp = null;
        public bool? RewardExp
        {
            get => rewardExp;
            set => SetProperty(ref rewardExp, value);
        }
        private int maxUses = 1;
        public int MaxUses
        {
            get => maxUses;
            set => SetProperty(ref maxUses, value);
        }
        private int uses = 0;
        public int Uses
        {
            get => uses;
            set => SetProperty(ref uses, value);
        }
        private int villagerGetXp = 0;
        public int VillagerGetXp
        {
            get => villagerGetXp;
            set => SetProperty(ref villagerGetXp, value);
        }
        private int demand = 0;
        public int Demand
        {
            get => demand;
            set => SetProperty(ref demand, value);
        }
        private int specialPrice = 0;
        public int SpecialPrice
        {
            get => specialPrice;
            set => SetProperty(ref specialPrice, value);
        }
        private float priceMultiplier = 0;
        public float PriceMultiplier
        {
            get => priceMultiplier;
            set => SetProperty(ref priceMultiplier, value);
        }
        #endregion

        #region 言论面板收放
        private Visibility isEditGossips = Visibility.Collapsed;
        public Visibility IsEditGossips
        {
            get => isEditGossips;
            set => SetProperty(ref isEditGossips, value);
        }
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
                    transactionItems.All(item => { item.HideDiscountData();return true; });
                else
                    transactionItems.All(item => { item.HideDiscountData(false); return true; });
                OnlyEditItem = !CanEditBrain && !CanEditGossips ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        #endregion

        #region 是否可以点击言论
        private bool canTouchGossips = true;
        public bool CanTouchGossips
        {
            get => canTouchGossips;
            set => SetProperty(ref canTouchGossips, value);
        }
        #endregion

        #region 是否可以点击记忆
        private bool canTouchBrain = true;
        public bool CanTouchBrain
        {
            get => canTouchBrain;
            set => SetProperty(ref canTouchBrain, value);
        }
        #endregion

        #region 记忆面板收放
        private Visibility isEditBrain = Visibility.Collapsed;
        public Visibility IsEditBrain
        {
            get => isEditBrain;
            set => SetProperty(ref isEditBrain,value);
        }
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
        private Visibility onlyEditItem = Visibility.Visible;
        public Visibility OnlyEditItem
        {
            get => onlyEditItem;
            set => SetProperty(ref onlyEditItem, value);
        }
        #endregion

        #region 已选中的成员
        private ItemStructure selectedItem = null;
        public ItemStructure SelectedItem
        {
            get => selectedItem;
            set => SetProperty(ref selectedItem,value);
        }
        #endregion

        #region 已选中的物品库索引
        private int selectedItemListIndex;

        public int SelectedItemListIndex
        {
            get => selectedItemListIndex;
            set => SetProperty(ref selectedItemListIndex, value);
        }

        #endregion

        #region 搜索内容
        private string searchText = "";
        public string SearchText
        {
            get => searchText;
            set
            {
                SetProperty(ref searchText, value);
                if (SelectedItemListIndex == 0)
                    OriginalViewSource.View?.Refresh();
                else
                    CustomViewSource.View?.Refresh();
            }
        }
        #endregion

        #region 村民数据

        #region Offers
        private string Offers
        {
            get
            {
                if (transactionItems.Count == 0) return "";
                string result = "Offers:{Recipes:[";
                string transactionItemData = string.Join("", transactionItems.Select(item => item.TransactionItemData + ","));
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
                if (!CanEditGossips || OnlyEditItem == Visibility.Collapsed || gossipItems.Count == 0) return "";
                string result = "Gossips:[";
                result += string.Join("", gossipItems.Select(item => "{" + item.GossipData + "},"));
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
        private string meetingPointDimension = null;
        public string MeetingPointDimension
        {
            get => meetingPointDimension;
            set => SetProperty(ref meetingPointDimension, value);
        }
        private string MeetingPointDimensionString
        {
            get
            {
                string DimensionId = DimensionDataBase.Where(item => item.Value == MeetingPointDimension).First().Key;
                return MeetingPointDimension.Trim() != "" ? "dimension:\"minecraft:" + DimensionId + "\"" : "";
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
        private string homeDimension = null;
        public string HomeDimension
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
                string DimensionId = DimensionDataBase.Where(item=>item.Value == HomeDimension).First().Key;
                return HomeDimension.Trim() != "" ? "dimension:\"minecraft:" + DimensionId + "\"" : "";
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
        private string jobSiteDimension = null;
        public string JobSiteDimension
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
                string DimensionId = DimensionDataBase.Where(item => item.Value == jobSiteDimension).First().Key;
                return JobSiteDimension.Trim() !=""? "dimension:\"minecraft:" + DimensionId + "\",":"";
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
        string VillagerTypeSourceFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Villager\\data\\VillagerTypes.ini";
        string VillagerProfessionsSourceFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Villager\\data\\VillagerProfessionTypes.ini";
        string VillagerLevelSourceFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Villager\\data\\VillagerLevels.ini";

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
                result = result + VillagerTypeDataBase.Where(item => item.Value == VillagerType.Text).First().Key + "\",";
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
                result = result + VillagerProfessionTypeDataBase.Where(item => item.Value == VillagerProfessionType.Text).First().Key + "\",";
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
                result = result + VillagerLevel.Text + ",";
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
                result += VillagerTypeString + VillagerProfessionTypeString + VillagerLevelString;
                result = result.TrimEnd(',') + "},";
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
        private double xp = 1;
        public double Xp
        {
            get { return xp; }
            set { xp = value; OnPropertyChanged(); }
        }
        private string XpString
        {
            get
            {
                return Xp.ToString().Trim() != "" ? "Xp:" + Xp + "," : "";
            }
        }
        #endregion

        #endregion

        #region 黑白画刷
        private SolidColorBrush whiteBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
        private SolidColorBrush blackBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000"));
        #endregion

        public VillagerDataContext()
        {
            #region 初始化主、副、结果空图像
            BuyItemIcon = BuyBItemIcon = SellItemIcon = new BitmapImage(new Uri(emptyIcon));
            #endregion

            #region 读取言论类型
            if (File.Exists(GossipTypesFilePath))
            {
                string[] types = File.ReadAllLines(GossipTypesFilePath);
                for (int i = 0; i < types.Length; i++)
                    GossipTypes.Add(types[i]);
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

            #region 初始化数据表
            Task.Run(async () =>
            {
                DataCommunicator dataCommunicator = DataCommunicator.GetDataCommunicator();
                ItemTable = await dataCommunicator.GetData("SELECT * FROM Items");

                #region 异步载入原版物品序列
                //加载物品集合
                string uriDirectoryPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
                string urlPath = "";
                foreach (DataRow row in ItemTable.Rows)
                {
                    urlPath = uriDirectoryPath + row["id"].ToString() + ".png";
                    if (File.Exists(urlPath))
                        OriginalItemList.Add(new ItemStructure(new Uri(urlPath, UriKind.Absolute), row["id"].ToString() + ":" + row["name"].ToString(), "{id:\"minecraft:" + row["id"].ToString() + "\",Count:1b}"));
                }
                #endregion
                #region 异步载入自定义物品序列
                //加载物品集合
                uriDirectoryPath = AppDomain.CurrentDomain.BaseDirectory + "resources\\saves\\Item\\";
                string[] itemFileList = Directory.GetFiles(uriDirectoryPath);
                foreach (var item in itemFileList)
                {
                    if (File.Exists(item))
                    {
                        string nbt = ExternalDataImportManager.GetItemDataHandler(item);
                        if (nbt.Length > 0)
                        {
                            JObject data = JObject.Parse(nbt);
                            JToken id = data.SelectToken("id");
                            if (id == null) continue;
                            string itemID = id.ToString().Replace("\"", "").Replace("minecraft:", "");
                            string iconPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + itemID + ".png";
                            string itemName = ItemTable.Select("id='" + itemID + "'").First()["name"].ToString();
                            if (File.Exists(iconPath))
                                CustomItemList.Add(new ItemStructure(new Uri(iconPath, UriKind.Absolute), itemID + ":" + itemName, nbt));
                        }
                    }
                }
                #endregion
            });
            #endregion
        }

        /// <summary>
        /// 原版物品库视图载入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OriginalItemListView_Loaded(object sender,RoutedEventArgs e)
        {
            Window parent = Window.GetWindow(sender as ListView);
            OriginalViewSource = parent.FindResource("OriginalItemView") as CollectionViewSource;
            OriginalViewSource.Filter += CollectionViewSource_Filter;
        }

        /// <summary>
        /// 自定义物品库视图载入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CustomItemListView_Loaded(object sender, RoutedEventArgs e)
        {
            Window parent = Window.GetWindow(sender as ListView);
            CustomViewSource = parent.FindResource("CustomItemView") as CollectionViewSource;
            CustomViewSource.Filter += CollectionViewSource_Filter;
        }

        [RelayCommand]
        /// <summary>
        /// 关闭交易数据设置面板
        /// </summary>
        private void CloseTransactionDataGrid()
        {
            TransactionDataGridVisibility = Visibility.Collapsed;
            //更新当前交易项的数量显示
            CurrentItem.BuyCountDisplay.Text = "x" + CurrentItem.BuyCount.ToString();
            CurrentItem.BuyBCountDisplay.Text = "x" + CurrentItem.BuyBCount.ToString();
            CurrentItem.SellCountDisplay.Text = "x" + CurrentItem.SellCount.ToString();
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
        private void SaveCommand()
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
                _ = File.WriteAllTextAsync(saveFileDialog.FileName, Result);
                _ = File.WriteAllTextAsync(AppDomain.CurrentDomain.BaseDirectory + "resources\\saves\\Villager\\" + Path.GetFileName(saveFileDialog.FileName), Result);
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
                string currentItemID = Path.GetFileNameWithoutExtension(itemStructure.ImagePath.ToString());
                string IDAndName = itemStructure.IDAndName;

                if ((currentItemID.Contains(SearchText) && IDAndName.Contains(SearchText)) || (IDAndName.Contains(SearchText) && IDAndName.StartsWith(currentItemID)))
                    e.Accepted = true;
            }
            else
                e.Accepted = true;
        }

        /// <summary>
        /// 载入维度种类数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void DimensionTypeLoaded(object sender, RoutedEventArgs e)
        {
            ComboBox box = sender as ComboBox;
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

                        DimensionTypeSource.Add(name);
                    }
                }
            }
            box.ItemsSource = DimensionTypeSource;
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
                GenerateResultDisplayer.Displayer displayer = GenerateResultDisplayer.Displayer.GetContentDisplayer();
                displayer.GeneratorResult(Result, "村民", icon_path);
                displayer.Show();
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
                GenerateResultDisplayer.Displayer displayer = GenerateResultDisplayer.Displayer.GetContentDisplayer();
                displayer.GeneratorResult(Result, "村民", icon_path);
                displayer.Show();
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
            TransactionItems transaction = new()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            transactionItems.Add(transaction);
        }

        [RelayCommand]
        /// <summary>
        /// 清空交易项控件
        /// </summary>
        private void ClearTransactionItem()
        {
            transactionItems.Clear();
        }

        [RelayCommand]
        /// <summary>
        /// 添加一个言论控件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void AddGossipItem()
        {
            GossipsItems gossipsItem = new()
            {
                Background = compositionIndex%2 ==0? orangeBrush : transparentBrush,
                Margin = new Thickness(12, 0, 0, 5),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            compositionIndex++;
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
                List<GossipsItems> target_gossip = gossipItems.Where(gossip =>
                {
                    string type = gossip.Type.SelectedItem as string;
                    if (gossip.Target.Text == GossipSearchTarget.Text.Trim() && type == current_type)
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
                    Source = new BitmapImage(SelectedItem.ImagePath),
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