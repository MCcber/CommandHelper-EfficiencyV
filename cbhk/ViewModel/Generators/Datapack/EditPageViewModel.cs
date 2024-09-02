using cbhk.CustomControls;
using cbhk.GeneralTools;
using cbhk.Generators.DataPackGenerator;
using cbhk.View;
using cbhk.View.Components.Datapack.EditPage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ICSharpCode.AvalonEdit;
using Newtonsoft.Json.Linq;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace cbhk.ViewModel.Generators.Datapack
{
    public partial class EditPageViewModel : ObservableObject
    {
        #region 数据包搜索文本框内容
        private string datapackSeacherValue = "";
        public string DatapackSeacherValue
        {
            get => datapackSeacherValue;
            set
            {
                SetProperty(ref datapackSeacherValue, value);
                SearchForSpecifyDatapackNode();
            }
        }
        #endregion

        #region 文本编辑器标签页数据源、数据包管理器树视图等数据源
        public ObservableCollection<RichTabItems> FunctionModifyTabItems { get; set; } = [];
        public ObservableCollection<TreeViewItem> DatapackTreeViewItems { get; set; } = [];
        public ObservableCollection<TreeViewItem> DatapackTreeViewSearchResult { get; set; } = [];
        #endregion

        #region 文本编辑器标签页容器可见性
        private Visibility functionModifyTabControlVisibility = Visibility.Visible;
        public Visibility FunctionModifyTabControlVisibility
        {
            get => functionModifyTabControlVisibility;
            set => SetProperty(ref functionModifyTabControlVisibility,value);
        }
        #endregion

        #region 当前选中的文本编辑器
        private RichTabItems selectedFileItem = null;
        public RichTabItems SelectedFileItem
        {
            get => selectedFileItem;
            set => SetProperty(ref selectedFileItem, value);
        }
        #endregion

        #region 初始标签页
        private RichTabItems WelComeTab = new()
        {
            Style = Application.Current.Resources["RichTabItemStyle"] as Style,
            Uid = "DemonstrationPage",
            FontSize = 12,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")),
            Header = "欢迎使用",
            FontWeight = FontWeights.Normal,
            IsContentSaved = true,
            BorderThickness = new(4, 3, 4, 0),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
            SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC6B23")),
            LeftBorderTexture = Application.Current.Resources["TabItemLeft"] as ImageBrush,
            RightBorderTexture = Application.Current.Resources["TabItemRight"] as ImageBrush,
            TopBorderTexture = Application.Current.Resources["TabItemTop"] as ImageBrush,
            SelectedLeftBorderTexture = Application.Current.Resources["SelectedTabItemLeft"] as ImageBrush,
            SelectedRightBorderTexture = Application.Current.Resources["SelectedTabItemRight"] as ImageBrush,
            SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as ImageBrush
        };
        #endregion

        #region 符号数据文件路径
        /// <summary>
        /// 符号结构文件
        /// </summary>
        private string symbolStructureFilePath = AppDomain.CurrentDomain.BaseDirectory + "SymbolStructure.json";
        private string databaseFilePath = AppDomain.CurrentDomain.BaseDirectory + "Minecraft.db";
        /// <summary>
        /// 语法字典
        /// </summary>
        public Dictionary<string, Dictionary<string, List<SyntaxTreeItem>>> SyntaxItemDicionary = [];
        #endregion

        #region 初始化数据结构所需标记
        /// <summary>
        /// 初始化语法字典记录命令部首
        /// </summary>
        public string initRadical = "";
        /// <summary>
        /// 记录递归前存储的键长度
        /// </summary>
        public int initKeyLength = 0;
        /// <summary>
        /// 语法树数组
        /// </summary>
        public JArray DataArray = [];
        #endregion

        #region 画刷
        private SolidColorBrush whiteBrush = new((Color)ColorConverter.ConvertFromString("#FFFFFF"));
        private SolidColorBrush transparentBrush = new((Color)ColorConverter.ConvertFromString("Transparent"));
        private SolidColorBrush darkGrayBrush = new((Color)ColorConverter.ConvertFromString("#1E1E1E"));
        #endregion

        #region 语言客户端
        private const int port = 5500;
        private const string ipString = "127.0.0.1";
        // 创建一个Socket对象
        public readonly Socket client = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        #endregion

        #region 补全数据中的常量
        [GeneratedRegex(@"(?<={)[0-9\-]+(?=})")]
        private static partial Regex ItemSlotMatcher();
        public Dictionary<string, string> CodeSnippetList = new() { { "dataModifyStorageFromSelf", "data modify storage id path set from entity @s path" },
            { "executeIfScoreSet","execute if score score_holder objective = score_holder objective" },
            { "scoreboardPlayersOperation","scoreboard players operation target_score_holder target_objective += source_score_holder source_objective" },
            { "scoreboardPlayersSet","scoreboard players set score_holder objective 0"},
            { "summonAec","summon minecraft:area_effect_cloud ~ ~ ~ {Age: -2147483648, Duration: -1, WaitTime: -2147483648, Tags: [\"tag\"]}"},
            { "summonMarker","summon minecraft:marker ~ ~ ~ {data:{}}"},
            { "tagAdd","tag target add tag"},
            { "tagRemove","tag target remove tag"},
            { "param","### <param name=\"sender\"></param>"} };
        public Dictionary<string, List<string>> ResourceFilePathes = new() { { "advancementValue", [] }, { "predicateValue", [] } };
        public List<string> ParticleIds = [];
        public List<string> TeamColors = [];
        public List<string> BossbarColors = [];
        public List<string> BossbarStyles = [];
        public List<string> ScoreboardTypes = [];
        public List<string> ScoreboardCustomIds = [];
        public List<string> DamageTypes = [];
        public List<string> Enchantments = [];
        public List<string> Effects = [];
        public List<string> ItemSlots = [];
        public List<string> LootTools = [];
        public List<string> ItemIds = [];
        public List<string> BlockIds = [];
        public List<string> EntityIds = [];
        public List<string> SoundFilePath = [];
        public List<string> selectors = ["@a", "@e", "@p", "@r", "@s"];
        public List<string> singleSelectors = ["@a[limit=1]", "@e[limit=1]", "@p", "@r", "@s"];
        public List<string> SelectorParameterValueTypes = ["Number", "PositiveNumber", "Double", "DoubleInterval", "PositiveDouble", "PositiveDoubleInterval", "IntInterval", "AdvancementsValue", "ScoresValue", "Int", "TagValue", "TeamValue", "NameValue", "EntityId", "FileReferrerValue", "JsonValue"];
        public List<byte> bytes = [0, 255];
        public List<short> shorts = [-32768, 0, 32767];
        public List<int> ints = [-2147483648, 2147483647];
        public List<long> longs = [-9223372036854775808, 9223372036854775807];
        public List<float> floats = [0.0f];
        public List<double> doubles = [0.0];
        public List<string> dataTypes = ["byte", "double", "float", "int", "long", "short"];
        public List<string> axesTypes = ["x", "y", "z", "xy", "xz", "yz", "xyz"];
        public List<string> pos3DTypes = ["~", "~ ~", "~ ~ ~", "^", "^ ^", "^ ^ ^"];
        public List<string> pos2DTypes = ["~", "~ ~", "^", "^ ^"];
        public List<string> mobAttributes = [];
        public List<string> dimensionIds = [];
        public Dictionary<string, string> SelectorParameterValues = [];
        public List<string> SelectorParameters = [];
        public List<string> CompoundSelectorParameters = ["advancements", "scores"];
        public Dictionary<string, GameruleItem> gamerules = [];
        #endregion

        private IContainerProvider _container;
        private MainView home;

        /// <summary>
        /// 记录剪切状态
        /// </summary>
        bool IsCuted = false;
        /// <summary>
        /// 被剪切的节点
        /// </summary>
        TreeViewItem BeCopyOrCutNode = null;
        /// <summary>
        /// 解决方案视图被选中的成员
        /// </summary>
        TreeViewItem SolutionViewSelectedItem { get; set; } = null;

        private DatapackView datapack = null;

        public Dictionary<string, Dictionary<int, string>> RuntimeVariables { get; set; } = new() { { "bossbarID", [] }, { "storageID", [] }, { "targetObjective", [] }, { "tagValue", [] }, { "triggerObjective", [] }, { "teamID", [] } };

        public EditPageViewModel(IContainerProvider container,MainView mainView)
        {
            #region 客户端连接语言服务器
            client.Connect(new IPEndPoint(IPAddress.Parse(ipString), port));
            #endregion
            _container = container;
            home = mainView;
        }

        [RelayCommand]
        /// <summary>
        /// 返回主页
        /// </summary>
        private void Return()
        {
            DatapackViewModel context = datapack.DataContext as DatapackViewModel;
            home.WindowState = WindowState.Normal;
            home.ShowInTaskbar = true;
            home.Show();
            home.Focus();
            datapack.Close();
        }

        [RelayCommand]
        /// <summary>
        /// 返回起始页
        /// </summary>
        private async Task BackToHomePage()
        {
            DatapackViewModel context = datapack.DataContext as DatapackViewModel;
            await datapack.Dispatcher.InvokeAsync(() =>
            {
                context.datapackGenerateSetupPage ??= new();
                DatapackTreeViewItems.Clear();
                FunctionModifyTabItems.Clear();
                System.Windows.Navigation.NavigationService.GetNavigationService(context.frame).Navigate(context.homePage);
            });
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void InitDataLoaded(object sender, RoutedEventArgs e)
        {
            datapack = Window.GetWindow(sender as Page) as DatapackView;

            #region 文本编辑区
            if(WelComeTab.Content is null)
            {
                FunctionModifyTabItems.Add(WelComeTab);
                FlowDocument document = WelComeTab.FindParent<Page>().FindResource("WelcomeDocument") as FlowDocument;
                RichTextBox richTextBox = new()
                {
                    Document = document,
                    IsReadOnly = true,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                    BorderThickness = new Thickness(0),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Transparent"))
                };
                WelComeTab.Content = richTextBox;
            }
            else
            {
                FunctionModifyTabItems.Add(WelComeTab);
                FunctionModifyTabControlVisibility = Visibility.Visible;
                SelectedFileItem = FunctionModifyTabItems[0];
            }
            #endregion

            #region 构建数据结构
            Task.Run(() =>
            {
                if (File.Exists(symbolStructureFilePath))
                {
                    DataArray = JArray.Parse(File.ReadAllText(symbolStructureFilePath));

                    if (DataArray is not null)
                    {
                        InitSyntaxTree(DataArray, new StringBuilder());
                        DataArray.Clear();
                    }
                }
            });
            #endregion

            #region 从数据库中读取所需变量
            Task.Run(async () =>
            {
                if (File.Exists(databaseFilePath))
                {
                    DataCommunicator dataCommunicator = DataCommunicator.GetDataCommunicator();

                    #region 添加物品槽位编号
                    DataTable itemSlotTable = await dataCommunicator.GetData("Select * From ItemSlots");
                    foreach (DataRow row in itemSlotTable.Rows)
                    {
                        if (row["value"] is string value)
                        {
                            string subContent = ItemSlotMatcher().Match(value).ToString();
                            if (subContent is not null && subContent.Length > 0)
                            {
                                #region 处理区间,与后面引用的数据拼接成完整的数据
                                string prefix = value[..(value.LastIndexOf('.') + 1)];
                                int dashIndex = subContent.IndexOf('-');
                                int start = int.Parse(subContent[..dashIndex]);
                                int end = int.Parse(subContent[(dashIndex + 1)..]);
                                for (int i = start; i <= end; i++)
                                    ItemSlots.Add(prefix + i);
                                #endregion
                            }
                            else
                                ItemSlots.Add(value);
                        }
                    }
                    #endregion
                    #region 添加附魔ID
                    DataTable enchantmentIdTable = await dataCommunicator.GetData("Select * From Enchantments");
                    foreach (DataRow row in enchantmentIdTable.Rows)
                    {
                        if (row["id"] is string value)
                            Enchantments.Add(value);
                    }
                    #endregion
                    #region 添加伤害类型
                    DataTable damageTypeTable = await dataCommunicator.GetData("Select * From DamageTypes");
                    foreach (DataRow row in damageTypeTable.Rows)
                    {
                        if (row["value"] is string value)
                            DamageTypes.Add(value);
                    }
                    #endregion
                    #region 添加维度
                    dimensionIds.Add("minecraft:over_world");
                    dimensionIds.Add("minecraft:nether");
                    dimensionIds.Add("minecraft:the_end");
                    #endregion
                    #region 添加选择器参数
                    DataTable selectorParameterTable = await dataCommunicator.GetData("Select * From SelectorParameters");
                    foreach (DataRow row in selectorParameterTable.Rows)
                    {
                        if (row["value"] is string value)
                            SelectorParameters.Add(value);
                    }
                    #endregion
                    #region 添加选择器参数值
                    DataTable selectorParameterValueTable = await dataCommunicator.GetData("Select * From SelectorParameterValues");
                    foreach (DataRow row in selectorParameterValueTable.Rows)
                    {
                        if (row["name"] is string name && row["value"] is string value)
                            SelectorParameterValues.Add(name, value);
                    }
                    #endregion
                    #region 添加游戏规则名称
                    DataTable gameruleTable = await dataCommunicator.GetData("Select * From GameRules");
                    foreach (DataRow row in gameruleTable.Rows)
                    {
                        if (row["name"] is string name)
                        {
                            GameruleItem gameruleItem = new();
                            gamerules.Add(name, gameruleItem);
                            if (row["description"] is string description)
                                gameruleItem.Description = description;
                            if (row["defaultValue"] is string defaultValue)
                                gameruleItem.Value = defaultValue;
                            if (row["dataType"] is string dataType)
                                gameruleItem.ItemType = dataType == "Bool" ? GameruleItem.DataType.Bool : GameruleItem.DataType.Int;
                        }
                    }
                    #endregion
                    #region 添加队伍颜色
                    DataTable teamColorTable = await dataCommunicator.GetData("Select * From TeamColors");
                    foreach (DataRow row in teamColorTable.Rows)
                    {
                        if (row["value"] is string value)
                            TeamColors.Add(value);
                    }
                    #endregion
                    #region 添加bossbar颜色
                    DataTable bossbarColorTable = await dataCommunicator.GetData("Select * From BossbarColors");
                    foreach (DataRow row in bossbarColorTable.Rows)
                    {
                        if (row["value"] is string value)
                            BossbarColors.Add(value);
                    }
                    #endregion
                    #region 添加bossbar样式
                    DataTable bossbarStyleTable = await dataCommunicator.GetData("Select * From BossbarStyles");
                    foreach (DataRow row in bossbarStyleTable.Rows)
                    {
                        if (row["value"] is string value)
                            BossbarStyles.Add(value);
                    }
                    #endregion
                    #region 添加物品Id
                    DataTable itemIdTable = await dataCommunicator.GetData("Select * From Items");
                    foreach (DataRow row in itemIdTable.Rows)
                    {
                        if (row["id"] is string id)
                            ItemIds.Add(id);
                    }
                    #endregion
                    #region 添加方块Id
                    DataTable blockIdTable = await dataCommunicator.GetData("Select * From Blocks");
                    foreach (DataRow row in blockIdTable.Rows)
                    {
                        if (row["id"] is string id)
                            BlockIds.Add(id);
                    }
                    #endregion
                    #region 添加实体Id
                    DataTable entityIdTable = await dataCommunicator.GetData("Select * From Entities");
                    foreach (DataRow row in entityIdTable.Rows)
                    {
                        if (row["id"] is string id)
                            EntityIds.Add(id);
                    }
                    #endregion
                    #region 添加粒子路径
                    DataTable particleTable = await dataCommunicator.GetData("Select * From Particles");
                    foreach (DataRow row in particleTable.Rows)
                    {
                        if (row["value"] is string value)
                            ParticleIds.Add(value);
                    }
                    #endregion
                    #region 添加音效路径
                    DataTable soundFilePathTable = await dataCommunicator.GetData("Select * From Sounds");
                    foreach (DataRow row in soundFilePathTable.Rows)
                    {
                        if (row["id"] is string id)
                            SoundFilePath.Add(id);
                    }
                    #endregion
                    #region 添加生物属性
                    DataTable mobAttributeTable = await dataCommunicator.GetData("Select * From MobAttributes");
                    foreach (DataRow row in mobAttributeTable.Rows)
                    {
                        if (row["id"] is string id)
                            mobAttributes.Add(id);
                    }
                    #endregion
                    #region 添加药水id/生物状态
                    DataTable mobEffectTable = await dataCommunicator.GetData("Select * From MobEffects");
                    foreach (DataRow row in mobEffectTable.Rows)
                    {
                        if (row["id"] is string id)
                            Effects.Add(id);
                    }
                    #endregion
                    #region 添加进度列表
                    DataTable advancementTable = await dataCommunicator.GetData("Select * From Advancements");
                    foreach (DataRow row in advancementTable.Rows)
                    {
                        if (row["value"] is string value)
                            ResourceFilePathes["advancementValue"].Add("minecraft:" + value);
                    }
                    #endregion
                    #region 添加战利品表工具
                    LootTools.Add("mainhand");
                    LootTools.Add("offhand");
                    LootTools.AddRange([.. ItemIds]);
                    #endregion
                    #region 添加记分板准则
                    DataTable scoreboardIdTable = await dataCommunicator.GetData("Select * From ScoreboardTypes");
                    foreach (DataRow row in scoreboardIdTable.Rows)
                    {
                        if (row["value"] is string value)
                            ScoreboardTypes.Add(value);
                    }
                    #endregion
                    #region 添加custom命令空间下的ID
                    DataTable scoreboardCustomIdTable = await dataCommunicator.GetData("Select * From ScoreboardCustomIds");
                    foreach (DataRow row in scoreboardCustomIdTable.Rows)
                    {
                        if (row["value"] is string value)
                            ScoreboardCustomIds.Add(value);
                    }
                    #endregion
                }
            });
            #endregion

            //#region 数据包管理器树
            //TreeViewItem item = new();
            //DatapackTreeViewItems.Add(item);
            //DatapackTreeViewItems.Remove(item);
            //#endregion
        }

        /// <summary>
        /// 初始化语法树
        /// </summary>
        /// <param name="currentArray"></param>
        /// <param name="parent"></param>
        private void InitSyntaxTree(JArray currentArray, StringBuilder currentKey, SyntaxTreeItem currentTreeItem = null)
        {
            string value = "";
            currentKey ??= new();
            for (int i = 0; i < currentArray.Count; i++)
            {
                if (currentArray[i] is JObject jobject)
                {
                    #region 检查是否为部首
                    SyntaxTreeItem.SyntaxTreeItemType type = SyntaxTreeItem.SyntaxTreeItemType.Literal;
                    if (jobject["radical"] is JToken radical)
                    {
                        value = radical.ToString();
                        initRadical = value + "Radical";
                        if (!currentKey.ToString().StartsWith("commands."))
                            currentKey.Append("commands.");
                        currentKey.Append(radical.ToString() + '.');
                        if (!SyntaxItemDicionary.ContainsKey(initRadical))
                            SyntaxItemDicionary.Add(initRadical, []);
                    }
                    else
                    if (jobject["path"] is JToken token)
                        value = token.ToString();
                    #endregion
                    #region 确定节点的类型
                    if (jobject.ContainsKey("radical"))
                        type = SyntaxTreeItem.SyntaxTreeItemType.Radical;
                    else
                    if (jobject.ContainsKey("type"))
                    {
                        if (jobject["type"]!.ToString() == "reference")
                            type = SyntaxTreeItem.SyntaxTreeItemType.Reference;
                        else
                            if (jobject["type"]!.ToString() == "dataType")
                            type = SyntaxTreeItem.SyntaxTreeItemType.DataType;
                        else
                            if (jobject["type"]!.ToString() == "redirect")
                            type = SyntaxTreeItem.SyntaxTreeItemType.Redirect;
                    }
                    #endregion
                    #region 把key或path添加进语法字典当作键
                    SyntaxTreeItem syntaxTreeItem = new()
                    {
                        Text = value,
                        Type = type,
                        Children = []
                    };

                    if (jobject["key"] is JToken key)
                    {
                        syntaxTreeItem.Key = key.ToString();
                        currentKey.Append(key.ToString() + ".");
                    }
                    else
                        if (jobject["path"] is JToken path)
                    {
                        currentKey.Append(path.ToString() + ".");
                    }
                    if (jobject["format"] is JToken format)
                        syntaxTreeItem.Description = format.ToString();
                    #endregion
                    #region 添加子级作为父级的补全数据并加入语法字典
                    if (SyntaxItemDicionary.TryGetValue(initRadical, out Dictionary<string, List<SyntaxTreeItem>>? parameterDictionaries))
                    {
                        if (!parameterDictionaries.ContainsKey(currentKey.ToString()))
                            parameterDictionaries.Add(currentKey.ToString(), [syntaxTreeItem]);
                        else
                            parameterDictionaries[currentKey.ToString()].Add(syntaxTreeItem);
                        currentTreeItem?.Children.Add(syntaxTreeItem);
                    }
                    #endregion
                    #region 处理递归
                    if (currentArray[i]["children"] is JArray subChildren)
                    {
                        initKeyLength = currentKey.Length;
                        InitSyntaxTree(subChildren, currentKey, syntaxTreeItem);
                    }
                    #endregion
                    #region 不执行递归时把最后一层的key删掉避免错误的拼接
                    if (currentKey.Length > 0)
                        currentKey.Remove(currentKey.Length - 1, 1);
                    int lastDotIndex = currentKey.ToString().LastIndexOf('.') + 1;
                    if (lastDotIndex != -1)
                        currentKey.Remove(lastDotIndex, currentKey.Length - lastDotIndex);
                    else
                        currentKey.Clear();
                    #endregion
                }
            }
        }

        /// <summary>
        /// 左侧编辑区选中标签页更新事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void FunctionModifyTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FunctionModifyTabControlVisibility = FunctionModifyTabItems.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// 解决方案视图选中成员更新事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SolutionViewer_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SolutionViewSelectedItem = (sender as TreeView).SelectedItem as TreeViewItem;
        }

        /// <summary>
        /// 搜索拥有指定数据的节点
        /// </summary>
        private async void SearchForSpecifyDatapackNode()
        {
            if (datapack != null)
            {
                List<string> allSolutionEntries = [];
                List<string> searchResult = [];
                // Collect all file system entries
                foreach (TreeViewItem item in DatapackTreeViewItems)
                {
                    if (Directory.Exists(item.Uid) || File.Exists(item.Uid))
                        allSolutionEntries.AddRange(Directory.GetFileSystemEntries(item.Uid, "*.*", SearchOption.AllDirectories));
                }
                // Search for matching items
                foreach (string item in allSolutionEntries)
                {
                    if (item[(item.LastIndexOf('\\') + 1)..].Contains(DatapackSeacherValue))
                        searchResult.Add(item);
                }
                await SearchForSpecifyDatapackNodeAsync(searchResult);
            }
        }

        /// <summary>
        /// 搜索拥有指定数据的节点
        /// </summary>
        /// <returns></returns>
        private async Task SearchForSpecifyDatapackNodeAsync(List<string> searchResult)
        {
            await Task.Run(() =>
            {
                // Invoke UI-related logic on the UI thread
                datapack.Dispatcher.Invoke(() =>
                {
                    List<TreeViewItem> nodesToDisplay = [];

                    // Perform breadth-first search
                    Queue<TreeViewItem> queue = new(DatapackTreeViewItems);
                    while (queue.Count > 0)
                    {
                        TreeViewItem currentNode = queue.Dequeue();
                        // If the node is null, skip it
                        if (currentNode.Header is null) continue;
                        // Check if the current node matches the search criteria
                        if (NodeMatchesSearchCriteria(currentNode, searchResult))
                        {
                            currentNode.IsExpanded = searchResult.Select(item => item.Contains(currentNode.Uid)).Any();
                            DatapackTreeItems header = currentNode.Header as DatapackTreeItems;
                            string test = header.HeadText.Text;
                            nodesToDisplay.Add(currentNode);
                        }

                        // Enqueue child nodes for further exploration
                        foreach (TreeViewItem childNode in currentNode.Items)
                            queue.Enqueue(childNode);
                    }

                    // Update the UI after processing all nodes
                    queue = new(DatapackTreeViewItems);
                    while (queue.Count > 0)
                    {
                        TreeViewItem currentNode = queue.Dequeue();
                        if (nodesToDisplay.Contains(currentNode))
                            currentNode.Visibility = Visibility.Visible;
                        else
                            currentNode.Visibility = Visibility.Collapsed;
                        foreach (TreeViewItem childNode in currentNode.Items)
                            queue.Enqueue(childNode);
                    }
                });
            });
        }

        /// <summary>
        /// 判断节点是否可以显示
        /// </summary>
        /// <param name="currentNode"></param>
        /// <param name="searchResult"></param>
        /// <returns></returns>
        private bool NodeMatchesSearchCriteria(TreeViewItem currentNode, List<string> searchResult)
        {
            DatapackTreeItems header = currentNode.Header as DatapackTreeItems;
            string targetValue = header.HeadText.Text;
            bool matchHeaderContent = false;
            for (int i = 0; i < searchResult.Count; i++)
            {
                if ((searchResult[i][(searchResult[i].LastIndexOf('\\') + 1)..].Contains(DatapackSeacherValue) && searchResult[i].StartsWith(currentNode.Uid)) || DatapackSeacherValue.Length == 0)
                {
                    matchHeaderContent = true;
                    break;
                }
            }
            return matchHeaderContent;
        }

        [RelayCommand]
        /// <summary>
        /// 新建项
        /// </summary>
        private void AddItem()
        {
            if(Directory.Exists(SolutionViewSelectedItem.Uid))
            {
                AddFileForm addFileForm = new();
                AddFileFormDataContext context = addFileForm.DataContext as AddFileFormDataContext;
                DatapackTreeItems header = SolutionViewSelectedItem.Header as DatapackTreeItems;
                TreeViewItem root = addFileForm.FileTypeViewer.Items[0] as TreeViewItem;
                foreach (TreeViewItem item in root.Items)
                {
                    if ((item.Uid.Contains('\\') && item.Uid.Contains(header.HeadText.Text)) || (!item.Uid.Contains('\\') && item.Uid == header.HeadText.Text))
                    {
                        context.DefaultSelectedNewFile = "new " + (header.HeadText.Text.EndsWith('s')? header.HeadText.Text[0..(header.HeadText.Text.Length - 1)] : header.HeadText.Text);
                        break;
                    }
                }
                if (addFileForm.ShowDialog().Value)
                {
                    if(File.Exists(context.SelectedNewFile.Path) && Directory.Exists(SolutionViewSelectedItem.Uid))
                    {
                        File.Copy(context.SelectedNewFile.Path,SolutionViewSelectedItem.Uid + "\\" + context.NewFileName);
                        DatapackTreeItems datapackTreeItems = new();
                        datapackTreeItems.HeadText.Text = context.NewFileName;
                        datapackTreeItems.DatapackMarker.Visibility = Visibility.Collapsed;
                        datapackTreeItems.Icon.Visibility = Visibility.Visible;
                        TreeViewItem newViewItem = new()
                        {
                            Header = datapackTreeItems,
                            Uid = SolutionViewSelectedItem.Uid + "\\" + context.NewFileName
                        };
                        newViewItem.MouseDoubleClick += DoubleClickAnalysisAndOpenAsync;
                        SolutionViewSelectedItem.Items.Add(newViewItem);
                    }
                }
            }
        }

        [RelayCommand]
        /// <summary>
        /// 现有项
        /// </summary>
        private void AddExistingItems()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                Filter = "Json文件|*.json;|Mcfunction文件|*.mcfunction;|Mcmeta文件|*.mcmeta;",
                DefaultExt = ".json",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Multiselect = true,
                RestoreDirectory = true,
                Title = "添加现有项"
            };
            if(openFileDialog.ShowDialog().Value && Directory.Exists(SolutionViewSelectedItem.Uid))
            {
                string extension;
                foreach (string item in openFileDialog.FileNames)
                {
                    if(File.Exists(item))
                    {
                        DatapackTreeItems datapackTreeItems = new();
                        datapackTreeItems.DatapackMarker.Visibility = Visibility.Collapsed;
                        datapackTreeItems.Icon.Visibility = Visibility.Visible;
                        extension = Path.GetFileNameWithoutExtension(item);
                        if (Application.Current.Resources[extension] is DrawingImage drawingImage)
                            datapackTreeItems.Icon.Source = drawingImage;
                        TreeViewItem newItem = new()
                        {
                            Uid = SolutionViewSelectedItem.Uid + "\\" + Path.GetFileName(item),
                            Header = datapackTreeItems
                        };
                        SolutionViewSelectedItem.Items.Add(newItem);
                    };
                }
            }
        }

        [RelayCommand]
        /// <summary>
        /// 添加文件夹
        /// </summary>
        private void AddFolder()
        {
            if(Directory.Exists(SolutionViewSelectedItem.Uid))
            {
                string newUid = SolutionViewSelectedItem.Uid + (SolutionViewSelectedItem.Uid.EndsWith('\\') ? "" : "\\") + "新文件夹";
                DatapackTreeItems datapackTreeItems = new();
                datapackTreeItems.DatapackMarker.Visibility = Visibility.Collapsed;
                datapackTreeItems.Icon.Visibility = Visibility.Visible;
                datapackTreeItems.Icon.Source = Application.Current.Resources["FolderClosed"] as ImageSource;
                datapackTreeItems.HeadText.Visibility = Visibility.Collapsed;
                datapackTreeItems.FileNameEditor.Visibility = Visibility.Visible;
                datapackTreeItems.FileNameEditor.Focus();
                datapackTreeItems.FileNameEditor.Text = datapackTreeItems.HeadText.Text = "新文件夹";
                TreeViewItem folderItem = new()
                {
                    Uid = newUid,
                    Header = datapackTreeItems
                };
                folderItem.MouseDoubleClick += DoubleClickAnalysisAndOpenAsync;
                SolutionViewSelectedItem.Items.Add(folderItem);
            }
        }

        [RelayCommand]
        /// <summary>
        /// 剪切
        /// </summary>
        private void Cut()
        {
            Clipboard.SetText(SolutionViewSelectedItem.Uid);
            BeCopyOrCutNode = SolutionViewSelectedItem;
            IsCuted = true;
        }

        [RelayCommand]
        /// <summary>
        /// 复制对象
        /// </summary>
        private void Copy()
        {
            Clipboard.SetText(SolutionViewSelectedItem.Uid);
            BeCopyOrCutNode = SolutionViewSelectedItem;
        }

        [RelayCommand]
        /// <summary>
        /// 粘贴对象
        /// </summary>
        private void Paste()
        {
            string path = Clipboard.GetText();
            TreeViewItem currentParent;
            if (!Directory.Exists(SolutionViewSelectedItem.Uid) && File.Exists(SolutionViewSelectedItem.Uid) && SolutionViewSelectedItem.Parent is TreeViewItem)
                currentParent = SolutionViewSelectedItem.Parent as TreeViewItem;
            else
                currentParent = SolutionViewSelectedItem;
            if ((Directory.Exists(path) || File.Exists(path)) && path != currentParent.Uid)
            {
                if (IsCuted)
                {
                    //移动被剪切的节点到当前节点的子级
                    if(BeCopyOrCutNode != null && BeCopyOrCutNode.Parent != null)
                    {
                        #region 操作被剪切的节点
                        if (BeCopyOrCutNode.Parent is TreeViewItem beCutParent)
                            beCutParent.Items.Remove(BeCopyOrCutNode);
                        else
                        {
                            TreeView parent = BeCopyOrCutNode.Parent as TreeView;
                            parent.Items.Remove(BeCopyOrCutNode);
                        }
                        currentParent.Items.Add(BeCopyOrCutNode);
                        #endregion

                        //更新本地磁盘的文件
                        File.Move(path,currentParent.Uid + Path.GetFileName(path));

                        #region 更新编辑区文件的UID路径数据
                        foreach (RichTabItems tab in FunctionModifyTabItems)
                        {
                            if (tab.Uid == path)
                            {
                                if (!Directory.Exists(SolutionViewSelectedItem.Uid))
                                    Directory.CreateDirectory(SolutionViewSelectedItem.Uid);
                                tab.Uid = SolutionViewSelectedItem.Uid + "\\" + tab.Header.ToString();
                                break;
                            }
                        }
                        #endregion
                    }
                    IsCuted = false;
                }
                else
                {
                    TreeViewItem treeViewItem = new()
                    {
                        Uid = path,
                        Header = BeCopyOrCutNode.Header
                    };
                    SolutionViewSelectedItem.Items.Add(treeViewItem);
                    //复制本地磁盘的文件
                    File.Copy(path, currentParent.Uid + Path.GetFileName(path));
                }
            }
        }

        [RelayCommand]
        /// <summary>
        /// 复制完整路径
        /// </summary>
        private void CopyFullPath()
        {
            Clipboard.SetText(SolutionViewSelectedItem.Uid);
        }

        [RelayCommand]
        /// <summary>
        /// 从资源管理器打开
        /// </summary>
        private void OpenWithResourceManagement()
        {
            if (File.Exists(SolutionViewSelectedItem.Uid))
                OpenFolderThenSelectFiles.ExplorerFile(SolutionViewSelectedItem.Uid);
            else
                Process.Start("explorer.exe",SolutionViewSelectedItem.Uid);
        }

        [RelayCommand]
        /// <summary>
        /// 从项目中排除
        /// </summary>
        private void ExcludeFromProject()
        {
            #region 编辑区对应标签页改为未保存
            foreach (RichTabItems tab in FunctionModifyTabItems)
            {
                if (tab.Uid == SolutionViewSelectedItem.Uid)
                {
                    tab.IsContentSaved = false;
                    break;
                }
            }
            #endregion
            if (SolutionViewSelectedItem.Parent != null)
            {
                TreeViewItem parent = SolutionViewSelectedItem.Parent as TreeViewItem;
                parent.Items.Remove(SolutionViewSelectedItem);
            }
            else
            {
                TreeView parent = SolutionViewSelectedItem.Parent as TreeView;
                parent.Items.Remove(SolutionViewSelectedItem);
            }
        }

        [RelayCommand]
        /// <summary>
        /// 在终端打开
        /// </summary>
        private void OpenWithTerminal()
        {
            //TemplateItems templateItems = TreeViewItem.Header as TemplateItems;
            //if(Directory.Exists(templateItems.Uid))
            //Process.Start(@"explorer.exe", "cd " + templateItems.Uid);
        }

        [RelayCommand]
        /// <summary>
        /// 删除对象
        /// </summary>
        private void Delete()
        {
            #region 删除文件或文件夹
            if (Directory.Exists(SolutionViewSelectedItem.Uid))
                Directory.Delete(SolutionViewSelectedItem.Uid, true);
            else
                if(File.Exists(SolutionViewSelectedItem.Uid))
                File.Delete(SolutionViewSelectedItem.Uid);
            #endregion
            //删除右侧树视图中对应的节点
            ExcludeFromProject();
        }

        [RelayCommand]
        /// <summary>
        /// 查看属性
        /// </summary>
        private void Attribute()
        {

        }

        /// <summary>
        /// 双击分析文件并打开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void DoubleClickAnalysisAndOpenAsync(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem currentItem = sender as TreeViewItem;
            DatapackView datapack = Window.GetWindow(currentItem) as DatapackView;
            if (File.Exists(currentItem.Uid))
                await datapack.Dispatcher.InvokeAsync(() =>
                {
                    string fileContent = File.ReadAllText(currentItem.Uid);
                    RichTabItems item = new()
                    {
                        Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                        Uid = currentItem.Uid,
                        FontSize = 12,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")),
                        Header = Path.GetFileName(currentItem.Uid),
                        IsContentSaved = true,
                        FontWeight = FontWeights.Normal,
                        BorderThickness = new(4, 3, 4, 0),
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                        SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC6B23")),
                        LeftBorderTexture = Application.Current.Resources["TabItemLeft"] as ImageBrush,
                        RightBorderTexture = Application.Current.Resources["TabItemRight"] as ImageBrush,
                        TopBorderTexture = Application.Current.Resources["TabItemTop"] as ImageBrush,
                        SelectedLeftBorderTexture = Application.Current.Resources["SelectedTabItemLeft"] as ImageBrush,
                        SelectedRightBorderTexture = Application.Current.Resources["SelectedTabItemRight"] as ImageBrush,
                        SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as ImageBrush
                    };

                    EditPageView editPageView = _container.Resolve<EditPageView>();
                    McfunctionIntellisenseCodeEditor textEditor = new(editPageView)
                    {
                        ShowLineNumbers = true,
                        Background = transparentBrush,
                        Foreground = whiteBrush,
                        LineNumbersForeground = whiteBrush,
                        BorderThickness = new Thickness(0),
                        Text = fileContent,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                    };
                    textEditor.TextChanged += TextEditor_TextChanged;
                    textEditor.KeyDown += TextEditor_KeyDown;
                    item.Content = textEditor;
                    FunctionModifyTabItems.Add(item);
                    SelectedFileItem = item;
                });
        }

        /// <summary>
        /// 检测快捷键
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TextEditor_KeyDown(object sender, KeyEventArgs e)
        {
            TextEditor textEditor = sender as TextEditor;
            RichTabItems parent = textEditor.Parent as RichTabItems;
            DatapackView datapack = Window.GetWindow(parent) as DatapackView;
            #region 保存
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.S)
            {
                await datapack.Dispatcher.InvokeAsync(() =>
                {
                    string folder = Path.GetDirectoryName(parent.Uid);
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);
                    _ = File.WriteAllTextAsync(parent.Uid, textEditor.Text);
                    parent.IsContentSaved = true;
                });
            }
            #endregion
        }

        /// <summary>
        /// 文件编辑事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextEditor_TextChanged(object sender, EventArgs e)
        {
            TextEditor textEditor = sender as TextEditor;
            RichTabItems parent = textEditor.Parent as RichTabItems;
            parent.IsContentSaved = false;
        }
    }
}