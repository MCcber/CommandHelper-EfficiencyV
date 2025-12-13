using CBHK.CustomControl;
using CBHK.Domain;
using CBHK.Domain.Model;
using CBHK.Model.Common;
using CBHK.Utility.Common;
using CBHK.Utility.MessageTip;
using CBHK.View;
using CBHK.View.Component.Item;
using CBHK.View.Generator;
using CBHK.ViewModel.Generator;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryIoc;
using DryIoc.ImTools;
using Newtonsoft.Json.Linq;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CBHK.ViewModel.Component.Item
{
    public partial class ItemPageViewModel : ObservableObject
    {
        #region Field
        private object obj = new();
        string SpecialNBTStructureFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Item\Data\SpecialTags.json";
        JArray SpecialArray = null;
        private IContainerProvider container;
        private CBHKDataContext context;
        private DataService dataService = null;

        private IProgress<IconComboBoxItem> AddItemProgress = null;
        private IProgress<(int, string, string, string)> SetItemProgress = null;
        /// <summary>
        /// 需要适应版本变化的特指数据所属控件的事件
        /// </summary>
        public Dictionary<FrameworkElement, Action<FrameworkElement, RoutedEventArgs>> VersionNBTList = [];
        //特殊实体特指标签字典,用于动态切换内容
        public Dictionary<string, Grid> specialDataDictionary = [];
        //白色画刷
        SolidColorBrush whiteBrush = new((Color)ColorConverter.ConvertFromString("#FFFFFF"));
        //黑色画刷
        SolidColorBrush blackBrush = new((Color)ColorConverter.ConvertFromString("#000000"));
        //橙色画刷
        SolidColorBrush orangeBrush = new((Color)ColorConverter.ConvertFromString("#FFE5B663"));
        //存储外部读取进来的实体数据
        public JObject ExternallyReadEntityData { get; set; } = null;

        string buttonNormalImage = "pack://application:,,,/CBHK;component/Resource/Common/Image/ButtonNormal.png";
        string buttonPressedImage = "pack://application:,,,/CBHK;component/Resource/Common/Image/ButtonPressed.png";
        ImageBrush buttonNormalBrush;
        ImageBrush buttonPressedBrush;

        //最终结果
        public string Result { get; set; }

        //本生成器的图标路径
        string iconPath = "pack://application:,,,/CBHK;component/Resource/Common/Image/SpawnerIcon/IconItems.png";
        private string ImageSetFolderPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
        //Data页
        //public Data data = null;
        //Function页
        //public Function function = null;
        //Common页
        //public View.Component.Item.Common common = null;
        //特指标签页视图容器
        ScrollViewer SpecialViewer = null;
        /// <summary>
        /// 物品页
        /// </summary>
        RichTabItems currentItemPages = null;

        ItemViewModel itemDataContext = null;
        #endregion

        #region Property

        #region 给予
        [ObservableProperty]
        private bool _summon = false;

        private bool isNoStyleText = true;

        public bool IsNoStyleText
        {
            get => isNoStyleText;
            set
            {
                SetProperty(ref isNoStyleText, value);
                if (!IsNoStyleText)
                {
                    Summon = false;
                }
            }
        }

        #endregion

        #region 已选择的版本
        private TextComboBoxItem selectedVersion;
        public TextComboBoxItem SelectedVersion
        {
            get => selectedVersion;
            set
            {
                SetProperty(ref selectedVersion, value);
                CurrentMinVersion = int.Parse(SelectedVersion.Text.Replace(".", "").Replace("+", "").Split('-')[0]);
            }
        }

        public int CurrentMinVersion = 1202;
        private bool IsVersionUpdating = false;
        #endregion

        #region 版本数据源
        [ObservableProperty]
        private ObservableCollection<TextComboBoxItem> _versionSource = [];
        #endregion

        #region 物品ID列表
        [ObservableProperty]
        private ObservableCollection<IconComboBoxItem> _itemList = [];
        public Dictionary<int, List<IconComboBoxItem>> ItemIDListCopy = [];
        #endregion

        #region 方块ID列表
        [ObservableProperty]
        public ObservableCollection<IconComboBoxItem> _blockList = [];
        #endregion

        #region 附魔ID列表
        [ObservableProperty]
        public ObservableCollection<IconComboBoxItem> _enchantmentIDList = [];
        #endregion

        #region 属性相关的列表
        [ObservableProperty]
        public ObservableCollection<TextComboBoxItem> _attributeIDList = [];
        [ObservableProperty]
        public ObservableCollection<TextComboBoxItem> _attributeSlotList = [];
        [ObservableProperty]
        public ObservableCollection<TextComboBoxItem> _attributeValueTypeList = [];
        #endregion

        #region 药水效果ID列表
        [ObservableProperty]
        public ObservableCollection<IconComboBoxItem> _effectItemList = [];
        #endregion

        #region 保存物品ID
        private IconComboBoxItem selectedItem;
        public IconComboBoxItem SelectedItem
        {
            get => selectedItem;
            set
            {
                SetProperty(ref selectedItem, value);
                SpecialViewer?.Dispatcher.Invoke(UpdateUILayOut);
                LowVersionId = "";
                if (CurrentMinVersion < 1130)
                {
                    List<VersionID> matchVersionIDList = [.. VersionIDList.Where(item => item.HighVersionID == selectedItem.ComboBoxItemId)];
                    if (matchVersionIDList.Count > 0)
                        LowVersionId = matchVersionIDList.First().LowVersionID;
                }
            }
        }

        private string LowVersionId = "";
        #endregion

        #region 版本ID数据源
        [ObservableProperty]
        public ObservableCollection<VersionID> _versionIDList = [];
        #endregion

        #region 显示结果
        [ObservableProperty]
        private bool _showGeneratorResult = false;
        #endregion

        #region 是否作为工具或引用
        [ObservableProperty]
        public bool _useForTool = false;
        [ObservableProperty]
        public bool _useForReference = false;
        #endregion


        #region 是否同步到文件、存储外部数据
        public bool SyncToFile { get; set; }
        public string ExternFilePath { get; set; }
        #endregion

        #region 导入外部数据模式
        [ObservableProperty]
        private bool _importMode = false;
        #endregion

        /// <summary>
        /// 特指结果集合
        /// </summary>
        public Dictionary<string, ObservableCollection<NBTDataStructure>> SpecialTagsResult { get; set; } = [];

        #endregion

        #region Method
        public ItemPageViewModel(IContainerProvider Container,CBHKDataContext Context, DataService DataService)
        {
            #region 初始化数据
            container = Container;
            context = Context;
            dataService = DataService;
            buttonNormalBrush = new ImageBrush(new BitmapImage(new Uri(buttonNormalImage, UriKind.RelativeOrAbsolute)));
            buttonPressedBrush = new ImageBrush(new BitmapImage(new Uri(buttonPressedImage, UriKind.RelativeOrAbsolute)));
            string SpecialData = File.ReadAllText(SpecialNBTStructureFilePath);
            SpecialArray = JArray.Parse(SpecialData);
            #endregion

            AddItemProgress = new Progress<IconComboBoxItem>(ItemList.Add);
            SetItemProgress = new Progress<(int, string, string, string)>(item =>
            {
                ItemList[item.Item1].ComboBoxItemId = item.Item2;
                ItemList[item.Item1].ComboBoxItemText = item.Item3;
                ItemList[item.Item1].ComboBoxItemIcon = File.Exists(item.Item4) ? new BitmapImage(new Uri(item.Item4, UriKind.Absolute)) : null;
                if (item.Item1 == 0)
                {
                    SelectedItem = ItemList[0];
                }
            });
        }

        /// <summary>
        /// 初始化物品ID与版本物品ID列表
        /// </summary>
        private async void InitItemList()
        {
            if (!IsVersionUpdating)
            {
                return;
            }

            ItemList.Clear();
            Dictionary<string, string> ItemIDAndNameMap = dataService.GetItemIDAndNameGroupByVersionMap()
            .Where(pair => pair.Key <= CurrentMinVersion)
            .SelectMany(pair => pair.Value)
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value
            );

            List<string> ItemKeyList = [.. ItemIDAndNameMap.Select(item => item.Key)];
            ItemKeyList.Sort();

            ParallelOptions parallelOptions = new();
            await Parallel.ForAsync(0, ItemIDAndNameMap.Count, parallelOptions, (i, cancellationToken) =>
            {
                AddItemProgress.Report(new IconComboBoxItem());
                return new ValueTask();
            });

            Parallel.For(0, ItemList.Count, (i) =>
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
                SetItemProgress.Report(new ValueTuple<int, string, string, string>(i, currentKey, ItemIDAndNameMap[currentKey], imagePath));
            });
        }

        /// <summary>
        /// 初始化药水效果列表
        /// </summary>
        private void InitEffectList()
        {
            if (!IsVersionUpdating)
            {
                return;
            }
            EffectItemList.Clear();

            List<string> currentEffectIDList = [..context.MobEffectSet.SelectMany(item =>
            {
                if(VersionComparer.IsInRange(item.Key, CurrentMinVersion.ToString()))
                {
                    return item.Value;
                }
                return [];
            })];
            foreach (var item in currentEffectIDList)
            {
                string imagePath = ImageSetFolderPath + item + ".png";
                if (item is not null && File.Exists(imagePath))
                {
                    imagePath = ImageSetFolderPath + item + ".png";
                    IconComboBoxItem iconComboBoxItem = new()
                    {
                        ComboBoxItemId = item,
                        ComboBoxItemText = item,
                        ComboBoxItemIcon = imagePath.Length > 0 ? new BitmapImage(new Uri(imagePath, UriKind.Absolute)) : new BitmapImage()
                    };
                    EffectItemList.Add(iconComboBoxItem);
                }
            }
        }

        /// <summary>
        /// 初始化附魔列表
        /// </summary>
        private void InitEnchantmentList()
        {
            if (!IsVersionUpdating)
            {
                return;
            }
            EnchantmentIDList.Clear();

            List<string> currentEnchantmentIDList = [..context.MobEffectSet.SelectMany(item =>
            {
                if(VersionComparer.IsInRange(item.Key, CurrentMinVersion.ToString()))
                {
                    return item.Value;
                }
                return [];
            })];

            foreach (var item in currentEnchantmentIDList)
            {
                string imagePath = ImageSetFolderPath + item + ".png";
                if (item is not null && File.Exists(imagePath))
                {
                    imagePath = ImageSetFolderPath + item + ".png";
                    IconComboBoxItem iconComboBoxItem = new()
                    {
                        ComboBoxItemId = item,
                        ComboBoxItemText = item,
                        ComboBoxItemIcon = imagePath.Length > 0 ? new BitmapImage(new Uri(imagePath, UriKind.Absolute)) : new BitmapImage()
                    };
                    EnchantmentIDList.Add(iconComboBoxItem);
                }
            }
        }

        /// <summary>
        /// 初始化属性ID和值类型列表
        /// </summary>
        private void InitAttributeIDAndValueTypeAndSlotList()
        {
            if (!IsVersionUpdating)
            {
                return;
            }
            AttributeIDList.Clear();

            foreach (var item in context.MobAttributeSet)
            {
                if (item.Version is not null && int.Parse(item.Version.Replace(".", "")) > CurrentMinVersion)
                {
                    continue;
                }
                if (item.ID is not null)
                {
                    TextComboBoxItem textComboBoxItem = new()
                    {
                        Text = item.ID
                    };
                    AttributeIDList.Add(textComboBoxItem);
                }
            }

            AttributeSlotList.Clear();

            foreach (var item in context.AttributeSlotSet)
            {
                if (item.Value is not null && item.Value.Length > 0)
                {
                    AttributeSlotList.Add(new TextComboBoxItem()
                    {
                        Text = item.Value
                    });
                }
            }

            AttributeValueTypeList.Clear();

            foreach (var item in context.AttributeSlotSet)
            {
                if (item.Value is not null)
                {
                    AttributeValueTypeList.Add(new TextComboBoxItem()
                    {
                        Text = item.ID
                    });
                }
            }
        }

        [RelayCommand]
        /// <summary>
        /// 保存指令
        /// </summary>
        private async Task Save()
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
                if (Directory.Exists(Path.GetDirectoryName(saveFileDialog.FileName)))
                    File.WriteAllText(saveFileDialog.FileName, Result);
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "resources\\saves\\ItemView\\");
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "resources\\saves\\ItemView\\" + Path.GetFileName(saveFileDialog.FileName), Result);
            }
        }

        [RelayCommand]
        /// <summary>
        /// 执行生成
        /// </summary>
        private async Task Run()
        {
            StringBuilder nbt = new();

            if (SpecialTagsResult.Count > 0)
            {
                ObservableCollection<NBTDataStructure> SpecialData = SpecialTagsResult[SelectedItem.ComboBoxItemId];
                nbt.Append(string.Join(',', SpecialData.Select(item => item.Result)));
            }

            if (nbt.ToString().EndsWith(','))
                nbt.Remove(nbt.ToString().Length - 1, 1);

            string CurrentItemID = LowVersionId.Length > 0 ? LowVersionId : SelectedItem.ComboBoxItemId;

            if (Summon)
            {
                bool HaveTagNBT = nbt.Length > 0;
                string appendId = "id:\"minecraft:" + CurrentItemID + "\",";
                nbt.Insert(0, appendId);
                if (HaveTagNBT)
                {
                    nbt.Insert(appendId.Length, "tag:{");
                    nbt.Append("},");
                }
            }

            if (UseForTool)
            {
                //Result = "{id:\"minecraft:" + CurrentItemID + "\",Count:" + data?.ItemCount.Value + "b" + (nbt.ToString().Trim(',').Length > 0 ? ",tag:{" + nbt.ToString().Trim(',') + "}" : "") + "}";
                ItemView item = Window.GetWindow(SpecialViewer) as ItemView;
                item.DialogResult = true;
                return;
            }

            if (nbt.ToString().Length > 0)
            {
                nbt.Insert(0, '{');
                nbt.Append('}');
            }
            if (!Summon)
            {
                //if (CurrentMinVersion < 1130)
                //    Result = "give @p " + CurrentItemID + " " + data?.ItemCount.Value + " " + data?.ItemDamage.Value + " " + nbt;
                //else
                //    Result = "give @p " + CurrentItemID + nbt + " " + data?.ItemCount.Value;
            }
            else
                Result = "summon item" + " ~ ~ ~ {ItemView:" + nbt + "}";

            if (CurrentMinVersion < 1130 /*&& (common.ItemNameValue.Length > 0 || common.ItemLoreValue.Length > 0)*/)
                Result = @"give @p minecraft:sign 1 0 {BlockEntityTag:{Text1:""{\""text\"":\""右击执行\"",\""clickEvent\"":{\""action\"":\""run_command\"",\""value\"":\""/setblock ~ ~ ~ minecraft:command_block 0 replace {Command:\\\""" + Result + @"\\\""}\""}}""}}";

            if (ShowGeneratorResult)
            {
                DisplayerView displayer = container.Resolve<DisplayerView>();
                if (displayer is not null && displayer.DataContext is DisplayerViewModel displayerViewModel)
                {
                    displayer.Show();
                    displayerViewModel.GeneratorResult(Result, "物品", iconPath);
                }
            }
            else
            {
                Clipboard.SetText(Result);
                Message.PushMessage("物品生成成功!数据已进入剪切板", MessageBoxImage.Information);
            }
        }

        public async Task<string> Run(bool showResult)
        {
            return "";
        }

        /// <summary>
        /// JSON转控件
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="nbtStructure"></param>
        private List<FrameworkElement> JsonToComponentConverter(JObject nbtStructure, string NBTType = "")
        {
            string tag = JArray.Parse(nbtStructure["tag"].ToString())[0].ToString();
            JToken resultObj = nbtStructure["ResultType"];
            string result = resultObj is not null ? resultObj.ToString() : "";
            string key = nbtStructure["Key"].ToString();
            JToken children = nbtStructure["Children"];
            JToken descriptionObj = nbtStructure["Description"];
            string description = descriptionObj is not null ? descriptionObj.ToString() : "";
            JToken toolTipObj = nbtStructure["PrelimToolTip"];
            string toolTip = toolTipObj is not null ? toolTipObj.ToString() : "";
            JToken dependencyObj = nbtStructure["Dependency"];
            string dependency = dependencyObj is not null ? dependencyObj.ToString() : "";
            ComponentData componentData = new()
            {
                dataType = tag,
                key = key,
                resultType = result,
                toolTip = toolTip,
                description = description,
                dependency = dependency,
                nbtType = NBTType
            };
            if (children is not null)
                componentData.children = children.ToString();
            List<FrameworkElement> componentGroup = [];
            return componentGroup;
        }

        /// <summary>
        /// 根据物品ID实时显示隐藏特指数据
        /// </summary>
        private async Task UpdateUILayOut()
        {
            SelectedItem ??= ItemList[0];
            currentItemPages.Header = SelectedItem.ComboBoxItemId + ":" + SelectedItem.ComboBoxItemText;

            #region 搜索当前物品ID对应的JSON对象
            List<JToken> targetList = SpecialArray.Where(item =>
            {
                JObject currentObj = item as JObject;
                if (currentObj["type"].ToString() == SelectedItem.ComboBoxItemId)
                    return true;
                return false;
            }).ToList();
            #endregion

            if (targetList.Count > 0)
            {
                await Task.Run(async () =>
                {
                    JObject targetObj = targetList.First() as JObject;
                    #region 处理特指NBT
                    if (!specialDataDictionary.TryGetValue(SelectedItem.ComboBoxItemId, out Grid value))
                    {
                        JArray children = JArray.Parse(targetObj["Children"].ToString());
                        List<FrameworkElement> components = [];
                        Grid newGrid = null;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            newGrid = new();
                            newGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
                            newGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                        });

                        #region 更新控件集合
                        foreach (JObject nbtStructure in children.Cast<JObject>())
                        {
                            List<FrameworkElement> result = JsonToComponentConverter(nbtStructure);
                            components.AddRange(result);
                        }
                        #endregion
                        #region 应用更新后的集合
                        bool LeftIndex = true;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            for (int j = 0; j < components.Count; j++)
                            {
                                components[j].DataContext = this;
                                if (LeftIndex || newGrid.RowDefinitions.Count == 0)
                                    newGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                                newGrid.Children.Add(components[j]);
                                if (components[j] is Accordion || components[j] is TextCheckBoxs)
                                {
                                    Grid.SetRow(components[j], newGrid.RowDefinitions.Count - 1);
                                    Grid.SetColumn(components[j], 0);
                                    Grid.SetColumnSpan(components[j], 2);
                                    LeftIndex = true;
                                }
                                else
                                {
                                    Grid.SetRow(components[j], newGrid.RowDefinitions.Count - 1);
                                    Grid.SetColumn(components[j], LeftIndex ? 0 : 1);
                                    LeftIndex = !LeftIndex;
                                }
                            }
                        });
                        #endregion

                        if (!specialDataDictionary.ContainsKey(SelectedItem.ComboBoxItemId))
                            specialDataDictionary.Add(SelectedItem.ComboBoxItemId, newGrid);
                        await SpecialViewer.Dispatcher.InvokeAsync(() =>
                        {
                            SpecialViewer.Content = newGrid;
                        });
                    }
                    else
                    {
                        await SpecialViewer.Dispatcher.InvokeAsync(() =>
                        {
                            Grid cacheGrid = value;
                            SpecialViewer.Content = cacheGrid;
                        });
                    }
                    #endregion
                });
            }
            else
                SpecialViewer.Content = null;
        }

        private void UpdateItemDamageAndID()
        {
        }

        #endregion

        #region Event

        /// <summary>
        /// 载入物品页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ItemPages_Loaded(object sender, RoutedEventArgs e)
        {
            currentItemPages ??= (sender as ItemPageView).Parent as RichTabItems;
            itemDataContext ??= Window.GetWindow(sender as ItemPageView).DataContext as ItemViewModel;
            if (VersionSource.Count == 0)
            {
                VersionSource = itemDataContext.VersionList;
                SelectedVersion = VersionSource[0];
            }
        }

        /// <summary>
        /// 载入标签页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SpecialTagsPanelLoaded(object sender, RoutedEventArgs e)
        {
            TabControl tabControl = sender as TabControl;
            ItemView window = Window.GetWindow(tabControl) as ItemView;
            ItemViewModel datacontext = window.DataContext as ItemViewModel;
            foreach (TabItem item in tabControl.Items)
            {
                item.DataContext = this;
            }
            SpecialViewer = (tabControl.Items[0] as TabItem).Content as ScrollViewer;
        }

        /// <summary>
        /// 切换后载入共通标签数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabControl tabControl = sender as TabControl;
            JObject externData = ExternallyReadEntityData;
            if (ImportMode && ExternallyReadEntityData is not null)
            {
            }
        }

        /// <summary>
        /// 版本更新事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void Version_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            ItemViewModel itemDataContext = Window.GetWindow(comboBox).DataContext as ItemViewModel;
            CancellationTokenSource cancellationTokenSource = new();

            IsVersionUpdating = true;
            InitItemList();
            InitEffectList();
            InitAttributeIDAndValueTypeAndSlotList();
            InitEnchantmentList();
            IsVersionUpdating = false;

            #region 更新子控件的版本以及执行数据更新事件
            await Parallel.ForAsync(0, /*VersionComponents.Count*/1, async (i, cancellationTokenSource) =>
            {
                //if (VersionComponents[i] is StylizedTextBox && CurrentMinVersion < 1130)
                //{
                //    StylizedTextBox stylizedTextBox = VersionComponents[i] as StylizedTextBox;
                //    if (stylizedTextBox.richTextBox.Document.Blocks.Count > 0)
                //    {
                //        Paragraph paragraph = stylizedTextBox.richTextBox.Document.Blocks.FirstBlock as Paragraph;
                //        if (paragraph.Inlines.Count > 0)
                //        {
                //            RichRun richRun = paragraph.Inlines.FirstInline as RichRun;
                //            Application.Current.Dispatcher.Invoke(() =>
                //            {
                //                IsNoStyleText = richRun.Text.Trim().Length > 0;
                //            });
                //        }
                //        else
                //            IsNoStyleText = false;
                //    }
                //    else
                //        IsNoStyleText = false;
                //}
                //else
                //    IsNoStyleText = true;
                //更新版本控件的版本
                //await VersionComponents[i].Upgrade(CurrentMinVersion);
            });
            await Parallel.ForEachAsync(VersionNBTList, async (item, cancellationToken) =>
            {
                await Task.Delay(0, cancellationToken);
                item.Value.Invoke(item.Key, null);
            });
            #endregion
        }

        /// <summary>
        /// 物品ID更新事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ItemID_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(!IsVersionUpdating)
            {
                UpdateItemDamageAndID();
            }
        }

        #endregion
    }

    /// <summary>
    /// 动态控件数据结构
    /// </summary>
    public class ComponentData
    {
        public string children { get; set; }
        public string dataType { get; set; }
        public string resultType { get; set; }
        public string nbtType { get; set; }
        public string key { get; set; }
        public string description { get; set; }
        public string toolTip { get; set; }
        public string dependency { get; set; }
    }
}