using cbhk.ControlsDataContexts;
using cbhk.CustomControls;
using cbhk.CustomControls.Interfaces;
using cbhk.GeneralTools;
using cbhk.GeneralTools.MessageTip;
using cbhk.GenerateResultDisplayer;
using cbhk.Generators.ItemGenerator.Components.SpecialNBT;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
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
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace cbhk.Generators.ItemGenerator.Components
{
    public class ItemPageDataContext:ObservableObject
    {
        #region 给予
        private bool summon = false;
        public bool Summon
        {
            get => summon;
            set => SetProperty(ref summon, value);
        }

        private bool isNoStyleText;

        public bool IsNoStyleText
        {
            get => isNoStyleText;
            set
            {
                SetProperty(ref isNoStyleText, value);
                if (!IsNoStyleText)
                    Summon = false;
            }
        }

        #endregion

        #region 数据表
        DataTable ItemTable = null;
        DataTable BlockTable = null;
        DataTable EffectTable = null;
        DataTable EnchantmentTable = null;
        #endregion

        #region 返回、运行和保存等指令
        public RelayCommand Run { get; set; }
        public RelayCommand Save { get; set; }
        #endregion

        #region 已选择的版本
        private string selectedVersion = "";
        public string SelectedVersion
        {
            get => selectedVersion;
            set
            {
                SetProperty(ref selectedVersion, value);
                CurrentMinVersion = int.Parse(selectedVersion.Replace(".", "").Replace("+", "").Split('-')[0]);
            }
        }
        public ObservableCollection<string> VersionSource { get; set; } = ["1.20.2+", "1.16-1.20.1", "1.13-1.15", "1.12-"];

        private int currentMinVersion = 1202;
        public int CurrentMinVersion
        {
            get => currentMinVersion;
            set =>currentMinVersion = int.Parse(SelectedVersion.Replace(".", "").Replace("+", "").Split('-')[0]);
        }
        #endregion

        #region 保存物品ID
        private IconComboBoxItem selectedItemId;
        public IconComboBoxItem SelectedItemId
        {
            get => selectedItemId;
            set
            {
                SetProperty(ref selectedItemId, value);
                SpecialViewer?.Dispatcher.Invoke(UpdateUILayOut);
            }
        }
        #endregion

        #region 版本
        private bool behavior_lock = true;
        private bool version1_12 = false;
        public bool Version1_12
        {
            get => version1_12;
            set
            {
                version1_12 = value;
                if (behavior_lock)
                {
                    behavior_lock = false;
                    Version1_13 = !version1_12;
                    behavior_lock = true;
                }
            }
        }
        private bool version1_13 = true;
        public bool Version1_13
        {
            get => version1_13;
            set
            {
                version1_13 = value;
                if (behavior_lock)
                {
                    behavior_lock = false;
                    Version1_12 = !version1_13;
                    behavior_lock = true;
                }
            }
        }
        #endregion

        #region 显示结果
        private bool showGeneratorResult = false;
        public bool ShowGeneratorResult
        {
            get => showGeneratorResult;
            set => showGeneratorResult = value;
        }
        #endregion

        #region 是否作为工具或引用
        private bool userForTool = false;
        public bool UseForTool
        {
            get => userForTool;
            set => userForTool = value;
        }
        public bool UseForReference { get; set; } = false;
        #endregion

        #region 字段与引用
        string SpecialNBTStructureFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Item\\data\\SpecialTags.json";
        JArray SpecialArray = null;
        /// <summary>
        /// 特指结果集合
        /// </summary>
        public Dictionary<string, ObservableCollection<NBTDataStructure>> SpecialTagsResult { get; set; } = [];
        /// <summary>
        /// 需要适应版本变化的特指数据所属控件的事件
        /// </summary>
        public Dictionary<FrameworkElement, Action<FrameworkElement,RoutedEventArgs>> VersionNBTList = [];
        /// <summary>
        /// 版本控件
        /// </summary>
        public List<IVersionUpgrader> VersionComponents { get; set; } = [];
        //特殊实体特指标签字典,用于动态切换内容
        public Dictionary<string, Grid> specialDataDictionary = [];
        public ObservableCollection<IconComboBoxItem> EffectItems { get; set; } = [];
        //白色画刷
        SolidColorBrush whiteBrush = new((Color)ColorConverter.ConvertFromString("#FFFFFF"));
        //黑色画刷
        SolidColorBrush blackBrush = new((Color)ColorConverter.ConvertFromString("#000000"));
        //橙色画刷
        SolidColorBrush orangeBrush = new((Color)ColorConverter.ConvertFromString("#FFE5B663"));
        //存储外部读取进来的实体数据
        public JObject ExternallyReadEntityData { get; set; } = null;
        /// <summary>
        /// 方块列表
        /// </summary>
        public ObservableCollection<IconComboBoxItem> BlockList { get; set; } = [];
        string buttonNormalImage = "pack://application:,,,/cbhk;component/resources/common/images/ButtonNormal.png";
        string buttonPressedImage = "pack://application:,,,/cbhk;component/resources/common/images/ButtonPressed.png";
        ImageBrush buttonNormalBrush;
        ImageBrush buttonPressedBrush;

        //最终结果
        public string Result { get; set; }

        //本生成器的图标路径
        string icon_path = "pack://application:,,,/cbhk;component/resources/common/images/spawnerIcons/IconItems.png";
        //Data页
        public Data data = null;
        //Function页
        public Function function = null;
        //Common页
        public Common common = null;
        //特指标签页视图容器
        ScrollViewer SpecialViewer = null;
        /// <summary>
        /// 物品ID下拉框
        /// </summary>
        ComboBox ItemIDComboBox = null;
        /// <summary>
        /// 物品页
        /// </summary>
        RichTabItems currentItemPages = null;
        #endregion

        #region 是否同步到文件、存储外部数据
        public bool SyncToFile { get; set; }
        public string ExternFilePath { get; set; }
        #endregion

        #region 导入外部数据模式
        private bool importMode = false;
        public bool ImportMode
        {
            get => importMode;
            set => importMode = value;
        }
        #endregion

        public ItemPageDataContext()
        {
            #region 连接指令
            Run = new RelayCommand(run_command);
            Save = new RelayCommand(SaveCommand);
            #endregion

            #region 初始化数据
            buttonNormalBrush = new ImageBrush(new BitmapImage(new Uri(buttonNormalImage, UriKind.RelativeOrAbsolute)));
            buttonPressedBrush = new ImageBrush(new BitmapImage(new Uri(buttonPressedImage, UriKind.RelativeOrAbsolute)));
            string SpecialData = File.ReadAllText(SpecialNBTStructureFilePath);
            SpecialArray = JArray.Parse(SpecialData);
            #endregion
        }


        /// <summary>
        /// 载入物品ID下拉框
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void ItemIDComboBoxLoaded(object sender,RoutedEventArgs e)
        {
            ItemIDComboBox = sender as ComboBox;
            if (ItemIDComboBox.ItemsSource is null)
            {
                ItemDataContext parentContext = null;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    parentContext = Window.GetWindow(sender as ComboBox).DataContext as ItemDataContext;
                });
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ItemIDComboBox.ItemsSource = parentContext.ItemIDs;
                    SelectedItemId = parentContext.ItemIDs[0];
                });
            }
        }

        /// <summary>
        /// 初始化数据表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void ItemPages_Loaded(object sender, RoutedEventArgs e)
        {
            currentItemPages = (sender as ItemPages).Parent as RichTabItems;
            string currentPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
            ItemDataContext context = Window.GetWindow(sender as ItemPages).DataContext as ItemDataContext;
            ItemTable = context.ItemTable;
            BlockTable = context.BlockTable;
            EffectTable = context.EffectTable;
            EnchantmentTable = context.EnchantmentTable;
            await Task.Run(async () =>
            {
                if (EffectItems.Count == 0 && EffectTable.Rows.Count > 0)
                    foreach (DataRow row in EffectTable.Rows)
                    {
                        string id = row["id"].ToString();
                        string imagePath = "";
                        if (File.Exists(currentPath + id + ".png"))
                            imagePath = currentPath + id + ".png";
                        if (File.Exists(imagePath))
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                EffectItems.Add(new IconComboBoxItem()
                                {
                                    ComboBoxItemId = id,
                                    ComboBoxItemText = row["name"].ToString(),
                                    ComboBoxItemIcon = imagePath.Length > 0 ? new BitmapImage(new Uri(imagePath, UriKind.Absolute)) : new BitmapImage()
                                });
                            });
                    }

                if (BlockList.Count == 0 && BlockTable.Rows.Count > 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (DataRow row in BlockTable.Rows)
                        {
                            string id = row["id"].ToString();
                            string name = row["name"].ToString();
                            string imagePath = id + ".png";
                            if (File.Exists(currentPath + imagePath))
                                BlockList.Add(new IconComboBoxItem()
                                {
                                    ComboBoxItemId = id,
                                    ComboBoxItemText = name,
                                    ComboBoxItemIcon = new BitmapImage(new Uri(currentPath + imagePath, UriKind.Absolute))
                                });
                        }
                    });
                }
            });
        }

        /// <summary>
        /// 载入标签页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SpecialTagsPanelLoaded(object sender,RoutedEventArgs e)
        {
            TabControl tabControl = sender as TabControl;
            Item window = Window.GetWindow(tabControl) as Item;
            ItemDataContext datacontext = window.DataContext as ItemDataContext;
            foreach (TabItem item in tabControl.Items)
            {
                if (item.Uid == "Common")
                {
                    common = new()
                    {
                        HideInfomationTable = datacontext.HideInfomationTable
                    };
                    ObservableCollection<string> HideFlagsSource = [];
                    (item.Content as ScrollViewer).Content = common;
                    foreach (DataRow row in datacontext.HideInfomationTable.Rows)
                    {
                        string name = row["name"].ToString();
                        HideFlagsSource.Add(name);
                    }
                    common.HideFlagsBox.ItemsSource = HideFlagsSource;
                }
                if(item.Uid == "Data")
                {
                    data = new();
                    (item.Content as ScrollViewer).Content = data;
                    data.AttributeSlotTable = datacontext.AttributeSlotTable;
                    data.AttributeTable = datacontext.AttributeTable;
                }
                if(item.Uid == "Function")
                {
                    function = new();
                    VersionComponents.Add(function as IVersionUpgrader);
                    (item.Content as ScrollViewer).Content = function;
                    VersionComponents.Add(function as IVersionUpgrader);
                }
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
            if (ImportMode && ExternallyReadEntityData != null)
            {
                if (tabControl.SelectedIndex == 1)
                    common.GetExternData(ref externData);
                if (tabControl.SelectedIndex == 2)
                    function.GetExternData(ref externData);
                if (tabControl.SelectedIndex == 3)
                    data.GetExternData(ref externData);
            }
        }

        /// <summary>
        /// 版本更新事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void Version_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CancellationTokenSource cancellationTokenSource = new();
            CancellationToken cancellationToken = new();
            await Parallel.ForAsync(0,VersionComponents.Count,async (i, cancellationTokenSource) =>
            {
                if (VersionComponents[i] is StylizedTextBox && CurrentMinVersion < 113)
                {
                    StylizedTextBox stylizedTextBox = VersionComponents[i] as StylizedTextBox;
                    if (stylizedTextBox.richTextBox.Document.Blocks.Count > 0)
                    {
                        Paragraph paragraph = stylizedTextBox.richTextBox.Document.Blocks.FirstBlock as Paragraph;
                        if (paragraph.Inlines.Count > 0)
                        {
                            RichRun richRun = paragraph.Inlines.FirstInline as RichRun;
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                IsNoStyleText = richRun.Text.Trim().Length > 0;
                            });
                        }
                        else
                            IsNoStyleText = false;
                    }
                    else
                        IsNoStyleText = false;
                }
                else
                    IsNoStyleText = true;
                await VersionComponents[i].Upgrade(CurrentMinVersion);
            });
            await Parallel.ForEachAsync(VersionNBTList,async (item, cancellationToken) =>
            {
                await Task.Delay(0, cancellationToken);
                item.Value.Invoke(item.Key, null);
            });
        }

        /// <summary>
        /// 最终结算
        /// </summary>
        /// <param name="MultipleMode"></param>
        private async Task FinalSettlement(object MultipleOrExtern)
        {
            StringBuilder nbt = new();
            if (SpecialTagsResult.Count > 0)
            {
                ObservableCollection<NBTDataStructure> SpecialData = SpecialTagsResult[SelectedItemId.ComboBoxItemId];
                nbt.Append(string.Join(',', SpecialData.Select(item => item.Result)));
            }
            string commonResult = await common.GetResult();
            string functionResult = await (function as IVersionUpgrader).Result();
            string dataResult = await data.Result();
            nbt.Append(commonResult + functionResult + dataResult);
            if (nbt.ToString().EndsWith(','))
                nbt.Remove(nbt.ToString().Length - 1, 1);

            if (!Summon)
            {
                nbt.Insert(0, "id:\"minecraft:" + SelectedItemId.ComboBoxItemId + "\",tag:{");
                nbt.Append("},Count:" + data.ItemCount.Value + "b");
            }

            if (nbt.ToString().Length > 0)
            {
                nbt.Insert(0, '{');
                nbt.Append('}');
            }

            if (!Summon)
            {
                if (SelectedVersion == "1.12-")
                    Result = "give @p " + SelectedItemId.ComboBoxItemId + " " + data.ItemCount.Value + " " + data.ItemDamage.Value + " " + nbt;
                else
                    Result = "give @p " + SelectedItemId.ComboBoxItemId + nbt + " " + data.ItemCount.Value;
            }
            else
                Result = "summon item" + " ~ ~ ~ {Item:" + nbt + "}";

            if (bool.Parse(MultipleOrExtern.ToString()))
            {
                Displayer displayer = Displayer.GetContentDisplayer();
                displayer.GeneratorResult(Result, "实体", icon_path);
                displayer.Show();
                displayer.Focus();
            }
        }

        /// <summary>
        /// 保存指令
        /// </summary>
        private void SaveCommand()
        {
            //执行生成
            FinalSettlement(false);
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
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "resources\\saves\\Item\\");
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "resources\\saves\\Item\\" + Path.GetFileName(saveFileDialog.FileName), Result);
            }
        }

        /// <summary>
        /// 执行生成
        /// </summary>
        private async void run_command()
        {
            StringBuilder nbt = new();
            if (SpecialTagsResult.Count > 0)
            {
                ObservableCollection<NBTDataStructure> SpecialData = SpecialTagsResult[SelectedItemId.ComboBoxItemId];
                nbt.Append(string.Join(',', SpecialData.Select(item => item.Result)));
            }
            string commonData = await common?.GetResult();
            string functionData = await (function as IVersionUpgrader).Result();
            string dataResult = await data.Result();
            nbt.Append(commonData + functionData + dataResult + (!Summon && SelectedVersion == "1.13+" && data?.ItemDamage.Value > 0 ? "Damage:" + data?.ItemDamage.Value : ""));
            if (nbt.ToString().EndsWith(','))
                nbt.Remove(nbt.ToString().Length - 1, 1);

            if (Summon)
            {
                bool HaveTagNBT = nbt.Length > 0;
                string appendId = "id:\"minecraft:" + SelectedItemId.ComboBoxItemId + "\",";
                nbt.Insert(0, appendId);
                if (HaveTagNBT)
                {
                    nbt.Insert(appendId.Length,"tag:{");
                    nbt.Append("},");
                }
                nbt.Append("Count:" + data?.ItemCount.Value + "b");
            }

            if (UseForTool)
            {
                Result = "{id:\"minecraft:" + SelectedItemId.ComboBoxItemId + "\",Count:" + data?.ItemCount.Value + "b" + (nbt.ToString().Trim(',').Length > 0 ? ",tag:{" + nbt.ToString().Trim(',') + "}" : "") + "}";
                Item item = Window.GetWindow(SpecialViewer) as Item;
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
                if (SelectedVersion == "1.12-")
                    Result = "give @p " + SelectedItemId.ComboBoxItemId + " " + data?.ItemCount.Value + " " + data?.ItemDamage.Value + " " + nbt;
                else
                    Result = "give @p " + SelectedItemId.ComboBoxItemId + nbt + " " + data?.ItemCount.Value;
            }
            else
                Result = "summon item" + " ~ ~ ~ {Item:" + nbt + "}";

            if (ShowGeneratorResult)
            {
                Displayer displayer = Displayer.GetContentDisplayer();
                displayer.GeneratorResult(Result, "物品", icon_path);
                displayer.Show();
            }
            else
            {
                Clipboard.SetText(Result);
                Message.PushMessage("物品生成成功!数据已进入剪切板", MessageBoxImage.Information);
            }
        }

        public async Task<string> run_command(bool showResult)
        {
            StringBuilder nbt = new();
            if (SpecialTagsResult.Count > 0)
            {
                ObservableCollection<NBTDataStructure> SpecialData = SpecialTagsResult[SelectedItemId.ComboBoxItemId];
                nbt.Append(string.Join(',', SpecialData.Select(item => item.Result)));
            }
            string CommonResult = await common.GetResult();
            string FunctionResult = await (function as IVersionUpgrader).Result();
            string dataResult = await data.Result();
            nbt.Append(CommonResult + FunctionResult + dataResult + (!Summon && SelectedVersion == "1.13+" && data.ItemDamage.Value > 0 ? "Damage:" + data.ItemDamage.Value : ""));
            if (nbt.ToString().EndsWith(','))
                nbt.Remove(nbt.ToString().Length - 1, 1);
            if (!Summon)
            {
                nbt.Insert(0, "id:\"minecraft:" + SelectedItemId.ComboBoxItemId + "\",tag:{");
                nbt.Append("},Count:" + data.ItemCount.Value + "b");
            }
            if (nbt.ToString().Length > 0)
            {
                nbt.Insert(0, '{');
                nbt.Append('}');
            }

            if (UseForReference)
            {
                Result = "";
                Result = "{id:\"minecraft:" + SelectedItemId.ComboBoxItemId + "\"" + (Result.Length > 0 ? ",tag:{" + Result + ",Count:" + data.ItemCount.Value + "b" + "}" : "") + "}";
                return Result;
            }

            if (!Summon)
            {
                if (SelectedVersion == "1.12-")
                    Result = "give @p " + SelectedItemId.ComboBoxItemId + " " + data.ItemCount.Value + " " + data.ItemDamage.Value + " " + nbt;
                else
                    Result = "give @p " + SelectedItemId.ComboBoxItemId + nbt + " " + data.ItemCount.Value;
            }
            else
                Result = "summon item" + " ~ ~ ~ {Item:" + nbt + "}";

            if (showResult)
            {
                Displayer displayer = Displayer.GetContentDisplayer();
                displayer.GeneratorResult(Result, "物品", icon_path);
                displayer.Show();
            }
            else
                Clipboard.SetText(Result);
            return Result;
        }

        /// <summary>
        /// 根据需求构造控件
        /// </summary>
        /// <param name="Request"></param>
        /// <returns></returns>
        private List<FrameworkElement> ComponentsGenerator(ComponentData Request)
        {
            List<FrameworkElement> result = [];
            TextBlock displayText = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                displayText = new()
                {
                    Uid = Request.nbtType,
                    Text = Request.description.TrimEnd('。').TrimEnd('.'),
                    Foreground = whiteBrush,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };
            });
            if (Request.toolTip.Length > 0)
            {
                ToolTip toolTip = null;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    toolTip = new()
                    {
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")),
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Transparent")),
                        Content = Request.toolTip
                    };
                    displayText.ToolTip = toolTip;
                    ToolTipService.SetInitialShowDelay(displayText, 0);
                    ToolTipService.SetBetweenShowDelay(displayText, 0);
                });
            }

            result.Add(displayText);
            ComponentEvents componentEvents = new();
            Application.Current.Dispatcher.Invoke(() =>
            {
                switch (Request.dataType)
                {
                    case "TAG_List":
                        {
                            if (Request.dependency != null && Request.dependency.Length > 0)
                            {
                                switch (Request.dependency)
                                {
                                    case "InlineItems":
                                        {
                                            Accordion itemAccordion = new()
                                            {
                                                MaxHeight = 200,
                                                Uid = "Items",
                                                Name = Request.key,
                                                Style = Application.Current.Resources["AccordionStyle"] as Style,
                                                Background = orangeBrush,
                                                Title = Request.description,
                                                BorderThickness = new Thickness(0),
                                                Margin = new Thickness(10, 2, 10, 0),
                                                TitleForeground = blackBrush,
                                                ModifyName = "添加",
                                                FreshName = "清空",
                                                ModifyForeground = blackBrush,
                                                FreshForeground = blackBrush,
                                                Tag = new NBTDataStructure() { Result = "", Visibility = Visibility.Collapsed, DataType = Request.dataType, NBTGroup = Request.nbtType }
                                            };
                                            StackPanel itemPanel = new() { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F2F2F")) };
                                            ScrollViewer scrollViewer = new()
                                            {
                                                MaxHeight = 200,
                                                Content = itemPanel,
                                                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                                            };
                                            itemAccordion.Content = scrollViewer;
                                            itemAccordion.GotFocus += componentEvents.ValueChangedHandler;
                                            result.Add(itemAccordion);
                                            result.Remove(displayText);
                                            #region 分析是否需要代入导入的数据
                                            if (ImportMode)
                                            {
                                                string key = Request.key;
                                                if (!Summon)
                                                    key = "Item." + key;
                                                JToken data = ExternallyReadEntityData.SelectToken(key);
                                                if (data != null)
                                                {
                                                    JArray Items = JArray.Parse(data.ToString());
                                                    string itemImageFilePath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
                                                    string imagePath = "";
                                                    for (int i = 0; i < Items.Count; i++)
                                                    {
                                                        string itemID = JObject.Parse(Items[i].ToString())["id"].ToString();
                                                        Image image = new() { Tag = new NBTDataStructure() { Result = Items[i].ToString(), Visibility = Visibility.Collapsed, DataType = Request.dataType, NBTGroup = Request.nbtType } };
                                                        imagePath = itemImageFilePath + itemID + ".png";
                                                        if (File.Exists(imagePath))
                                                            image.Source = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute));
                                                        InlineItems itemBag = new();
                                                        (itemBag.DisplayItem.Child as Image).Source = image.Source;
                                                        itemPanel.Children.Add(itemBag);
                                                    }
                                                    itemAccordion.Focus();
                                                }
                                            }
                                            #endregion
                                        }
                                        break;
                                    case "StoredEnchantments":
                                        {
                                            Accordion itemAccordion = new()
                                            {
                                                MaxHeight = 200,
                                                Uid = Request.dependency,
                                                Name = Request.key,
                                                Style = Application.Current.Resources["AccordionStyle"] as Style,
                                                Background = orangeBrush,
                                                Title = Request.description,
                                                BorderThickness = new Thickness(0),
                                                Margin = new Thickness(2, 2, 2, 0),
                                                TitleForeground = blackBrush,
                                                ModifyName = "添加",
                                                FreshName = "清空",
                                                ModifyForeground = blackBrush,
                                                FreshForeground = blackBrush,
                                                Tag = new NBTDataStructure() { Result = "", Visibility = Visibility.Collapsed, DataType = Request.dataType, NBTGroup = Request.nbtType }
                                            };
                                            StackPanel itemPanel = new();
                                            ScrollViewer scrollViewer = new()
                                            {
                                                MaxHeight = 200,
                                                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F2F2F")),
                                                Content = itemPanel,
                                                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                                            };
                                            itemAccordion.Content = scrollViewer;
                                            itemAccordion.GotFocus += componentEvents.ValueChangedHandler;
                                            result.Add(itemAccordion);
                                            result.Remove(displayText);
                                            #region 分析是否需要代入导入的数据
                                            if (ImportMode)
                                            {
                                                string key = Request.key;
                                                if (!Summon)
                                                    key = "Item." + key;
                                                if (ExternallyReadEntityData.SelectToken(key) is JArray data)
                                                {
                                                    foreach (JObject item in data.Cast<JObject>())
                                                    {
                                                        EnchantmentItems enchantment = new();
                                                        JToken idObj = item["id"];
                                                        JToken lvlObj = item["lvl"];
                                                        if (idObj != null)
                                                        {
                                                            string idString = idObj.ToString();
                                                            if (idString.Contains(':'))
                                                                idString = idString[(idString.IndexOf(':') + 1)..];
                                                            string id = EnchantmentTable.Select("id='" + idString + "'").First()["name"].ToString();
                                                            enchantment.ID.SelectedValue = id;
                                                        }
                                                        if (lvlObj != null)
                                                            enchantment.Level.Value = double.Parse(lvlObj.ToString());
                                                        itemPanel.Children.Add(enchantment);
                                                    }
                                                    componentEvents.StoredEnchantments_LostFocus(itemAccordion, null);
                                                }
                                            }
                                            #endregion
                                        }
                                        break;
                                    case "NameSpaceReference":
                                        {
                                            Accordion itemAccordion = new()
                                            {
                                                MaxHeight = 200,
                                                Uid = Request.dependency,
                                                Name = Request.key,
                                                Style = Application.Current.Resources["AccordionStyle"] as Style,
                                                Background = orangeBrush,
                                                Title = Request.description,
                                                BorderThickness = new Thickness(0),
                                                Margin = new Thickness(10, 2, 10, 0),
                                                TitleForeground = blackBrush,
                                                ModifyName = "添加",
                                                FreshName = "清空",
                                                ModifyForeground = blackBrush,
                                                FreshForeground = blackBrush,
                                                Tag = new NBTDataStructure() { Result = "", Visibility = Visibility.Collapsed, DataType = Request.dataType, NBTGroup = Request.nbtType }
                                            };
                                            StackPanel itemPanel = new() { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F2F2F")) };
                                            ScrollViewer scrollViewer = new()
                                            {
                                                MaxHeight = 200,
                                                Content = itemPanel,
                                                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                                            };
                                            itemAccordion.Content = scrollViewer;
                                            itemAccordion.GotFocus += componentEvents.ValueChangedHandler;
                                            result.Add(itemAccordion);
                                            result.Remove(displayText);
                                            #region 分析是否需要代入导入的数据
                                            if (ImportMode)
                                            {
                                                string key = Request.key;
                                                if (!Summon)
                                                    key = "Item." + key;
                                                if (ExternallyReadEntityData.SelectToken(key) is JArray data)
                                                {
                                                    foreach (JValue item in data.Cast<JValue>())
                                                    {
                                                        NameSpaceReference nameSpaceReference = new();
                                                        nameSpaceReference.ReferenceBox.Text = item.Value<string>();
                                                        itemPanel.Children.Add(nameSpaceReference);
                                                    }
                                                    componentEvents.NameSpaceReference_LostFocus(itemAccordion, null);
                                                }
                                            }
                                            #endregion
                                        }
                                        break;
                                    case "MapDecorations":
                                        {
                                            Accordion itemAccordion = new()
                                            {
                                                MaxHeight = 200,
                                                Uid = Request.dependency,
                                                Name = Request.key,
                                                Style = Application.Current.Resources["AccordionStyle"] as Style,
                                                Background = orangeBrush,
                                                Title = Request.description,
                                                BorderThickness = new Thickness(0),
                                                Margin = new Thickness(2, 2, 2, 0),
                                                TitleForeground = blackBrush,
                                                ModifyName = "添加",
                                                FreshName = "清空",
                                                ModifyForeground = blackBrush,
                                                FreshForeground = blackBrush,
                                                Tag = new NBTDataStructure() { Result = "", Visibility = Visibility.Collapsed, DataType = Request.dataType, NBTGroup = Request.nbtType }
                                            };
                                            StackPanel itemPanel = new();
                                            ScrollViewer scrollViewer = new()
                                            {
                                                MaxHeight = 200,
                                                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F2F2F")),
                                                Content = itemPanel,
                                                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                                            };
                                            itemAccordion.Content = scrollViewer;
                                            itemAccordion.GotFocus += componentEvents.ValueChangedHandler;
                                            result.Add(itemAccordion);
                                            result.Remove(displayText);
                                            #region 分析是否需要代入导入的数据
                                            if (ImportMode)
                                            {
                                                string key = Request.key;
                                                if (!Summon)
                                                    key = "Item." + key;
                                                if (ExternallyReadEntityData.SelectToken(key) is JArray data)
                                                {
                                                    foreach (JObject item in data.Cast<JObject>())
                                                    {
                                                        MapDecorations mapDecorations = new();
                                                        JToken idObj = item["id"];
                                                        JToken typeObj = item["type"];
                                                        JToken xObj = item["x"];
                                                        JToken zObj = item["z"];
                                                        JToken rotObj = item["rot"];
                                                        if (idObj != null)
                                                            mapDecorations.Uid = idObj.ToString();
                                                        if (typeObj != null)
                                                            mapDecorations.type.SelectedIndex = byte.Parse(typeObj.ToString());
                                                        if (xObj != null)
                                                            mapDecorations.pos.number0.Value = double.Parse(xObj.ToString());
                                                        if (zObj != null)
                                                            mapDecorations.pos.number2.Value = double.Parse(zObj.ToString());
                                                        if (rotObj != null)
                                                            mapDecorations.rot.Value = double.Parse(rotObj.ToString());
                                                        itemPanel.Children.Add(mapDecorations);
                                                    }
                                                    componentEvents.MapDecorations_LostFocus(itemAccordion, null);
                                                }
                                            }
                                            #endregion
                                        }
                                        break;
                                    case "CustomPotionEffectList":
                                        {
                                            Accordion itemAccordion = new()
                                            {
                                                MaxHeight = 200,
                                                Uid = Request.dependency,
                                                Name = Request.key,
                                                Style = Application.Current.Resources["AccordionStyle"] as Style,
                                                Background = orangeBrush,
                                                Title = Request.description,
                                                BorderThickness = new Thickness(0),
                                                Margin = new Thickness(2, 2, 2, 0),
                                                TitleForeground = blackBrush,
                                                ModifyName = "添加",
                                                FreshName = "清空",
                                                ModifyForeground = blackBrush,
                                                FreshForeground = blackBrush,
                                                Tag = new NBTDataStructure() { Result = "", Visibility = Visibility.Collapsed, DataType = Request.dataType, NBTGroup = Request.nbtType }
                                            };
                                            StackPanel itemPanel = new();
                                            ScrollViewer scrollViewer = new()
                                            {
                                                MaxHeight = 200,
                                                Content = itemPanel,
                                                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F2F2F")),
                                                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                                            };
                                            itemAccordion.Content = scrollViewer;
                                            itemAccordion.GotFocus += componentEvents.ValueChangedHandler;
                                            result.Add(itemAccordion);
                                            result.Remove(displayText);
                                            #region 分析是否需要代入导入的数据
                                            if (ImportMode)
                                            {
                                                string key = Request.key;
                                                if (!Summon)
                                                    key = "Item." + key;
                                                if (ExternallyReadEntityData.SelectToken(key) is JArray Effects)
                                                {
                                                    for (int i = 0; i < Effects.Count; i++)
                                                    {
                                                        componentEvents.AddCustomPotionEffectCommand(itemAccordion);
                                                        CustomPotionEffects customPotionEffects = itemPanel.Children[i] as CustomPotionEffects;
                                                        StackPanel contentPanel = customPotionEffects.EffectListPanel;
                                                        #region 提取数据
                                                        JToken Ambient = Effects[i]["Ambient"];
                                                        JToken Amplifier = Effects[i]["Amplifier"];
                                                        JToken Duration = Effects[i]["Duration"];
                                                        JToken Id = Effects[i]["Id"];
                                                        JToken ShowIcon = Effects[i]["ShowIcon"];
                                                        JToken ShowParticles = Effects[i]["ShowParticles"];
                                                        JToken effect_changed_timestamp = Effects[i].SelectToken("FactorCalculationData.effect_changed_timestamp");
                                                        JToken factor_current = Effects[i].SelectToken("FactorCalculationData.factor_current");
                                                        JToken factor_previous_frame = Effects[i].SelectToken("FactorCalculationData.factor_previous_frame");
                                                        JToken factor_start = Effects[i].SelectToken("FactorCalculationData.factor_start");
                                                        JToken factor_target = Effects[i].SelectToken("FactorCalculationData.factor_target");
                                                        JToken had_effect_last_tick = Effects[i].SelectToken("FactorCalculationData.had_effect_last_tick");
                                                        JToken padding_duration = Effects[i].SelectToken("FactorCalculationData.padding_duration");
                                                        #endregion
                                                        #region 应用数据
                                                        if (Ambient != null)
                                                            contentPanel.FindChild<TextCheckBoxs>("Ambient").IsChecked = Ambient.ToString() == "1";
                                                        if (Amplifier != null)
                                                            contentPanel.FindChild<Slider>("Amplifier").Value = byte.Parse(Amplifier.ToString());
                                                        if (Duration != null)
                                                            contentPanel.FindChild<Slider>("Duration").Value = int.Parse(Duration.ToString());
                                                        if (Id != null)
                                                        {
                                                            string id = Id.ToString().Replace("minecraft:", "");
                                                            string currentID = EffectTable.Select("num='" + id + "'").First()["name"].ToString();
                                                            ComboBox idBox = contentPanel.FindChild<ComboBox>("Id");
                                                            idBox.ItemsSource = EffectItems;
                                                            idBox.SelectedValuePath = "ComboBoxItemId";
                                                            idBox.SelectedValue = EffectTable.Select("id='" + currentID + "'").First()["id"].ToString();
                                                        }
                                                        if (ShowIcon != null)
                                                            contentPanel.FindChild<TextCheckBoxs>("ShowIcon").IsChecked = ShowIcon.ToString() == "1";
                                                        if (ShowParticles != null)
                                                            contentPanel.FindChild<TextCheckBoxs>("ShowParticles").IsChecked = ShowParticles.ToString() == "1";
                                                        Grid grid = customPotionEffects.FactorCalculationDataGrid;
                                                        if (effect_changed_timestamp != null)
                                                            grid.FindChild<Slider>("effect_changed_timestamp").Value = int.Parse(effect_changed_timestamp.ToString());
                                                        if (factor_current != null)
                                                            grid.FindChild<Slider>("factor_current").Value = int.Parse(factor_current.ToString());
                                                        if (factor_previous_frame != null)
                                                            grid.FindChild<Slider>("factor_previous_frame").Value = int.Parse(factor_previous_frame.ToString());
                                                        if (factor_start != null)
                                                            grid.FindChild<Slider>("factor_start").Value = int.Parse(factor_start.ToString());
                                                        if (factor_target != null)
                                                            grid.FindChild<Slider>("factor_target").Value = int.Parse(factor_target.ToString());
                                                        if (had_effect_last_tick != null)
                                                            grid.FindChild<TextCheckBoxs>("had_effect_last_tick").IsChecked = had_effect_last_tick.ToString() == "1";
                                                        if (padding_duration != null)
                                                            grid.FindChild<Slider>("padding_duration").Value = int.Parse(padding_duration.ToString());
                                                        #endregion
                                                    }
                                                    componentEvents.CustomPotionEffects_LostFocus(itemAccordion, null);
                                                }
                                            }
                                            #endregion
                                        }
                                        break;
                                    case "StewEffectList":
                                        {
                                            Accordion itemAccordion = new()
                                            {
                                                MaxHeight = 200,
                                                Uid = Request.dependency,
                                                Name = Request.key,
                                                Style = Application.Current.Resources["AccordionStyle"] as Style,
                                                Background = orangeBrush,
                                                Title = Request.description,
                                                BorderThickness = new Thickness(0),
                                                Margin = new Thickness(10, 2, 10, 0),
                                                TitleForeground = blackBrush,
                                                ModifyName = "添加",
                                                FreshName = "清空",
                                                ModifyForeground = blackBrush,
                                                FreshForeground = blackBrush,
                                                Tag = new NBTDataStructure() { Result = "", Visibility = Visibility.Collapsed, DataType = Request.dataType, NBTGroup = Request.nbtType }
                                            };
                                            StackPanel itemPanel = new();
                                            ScrollViewer scrollViewer = new()
                                            {
                                                MaxHeight = 200,
                                                Content = itemPanel,
                                                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F2F2F")),
                                                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                                            };
                                            itemAccordion.Content = scrollViewer;
                                            itemAccordion.GotFocus += componentEvents.ValueChangedHandler;
                                            result.Add(itemAccordion);
                                            result.Remove(displayText);
                                            #region 分析是否需要代入导入的数据
                                            if (ImportMode)
                                            {
                                                string key = Request.key;
                                                if (!Summon)
                                                    key = "Item." + key;
                                                if (ExternallyReadEntityData.SelectToken(key) is JArray data)
                                                {
                                                    foreach (JObject item in data.Cast<JObject>())
                                                    {
                                                        JToken durationObj = item["EffectDuration"];
                                                        JToken idObj = item["EffectId"];

                                                        string id = idObj.ToString();
                                                        var currentID = EffectTable.Select("num='" + id + "'").First()["id"].ToString();
                                                        SuspiciousStewEffects suspiciousStewEffects = new();
                                                        itemPanel.Children.Add(suspiciousStewEffects);
                                                        if (durationObj != null)
                                                            suspiciousStewEffects.EffectDuration.Value = int.Parse(durationObj.ToString());
                                                        if (idObj != null)
                                                        {
                                                            suspiciousStewEffects.EffectID.SelectedValuePath = "ComboBoxItemId";
                                                            suspiciousStewEffects.EffectID.SelectedValue = currentID;
                                                        }
                                                    }
                                                    componentEvents.StewEffectList_LostFocus(itemAccordion, null);
                                                }
                                            }
                                            #endregion
                                        }
                                        break;
                                }
                            }
                        }
                        break;
                    case "TAG_Compound":
                        {
                            if (Request.dependency != null && Request.dependency.Length > 0)
                            {
                                switch (Request.dependency)
                                {
                                    case "InlineItems":
                                        {
                                            Accordion itemAccordion = new()
                                            {
                                                MaxHeight = 200,
                                                Uid = "Items",
                                                Name = Request.key,
                                                Style = Application.Current.Resources["AccordionStyle"] as Style,
                                                Background = orangeBrush,
                                                Title = Request.description,
                                                BorderThickness = new Thickness(0),
                                                Margin = new Thickness(0, 0, 0, 2),
                                                TitleForeground = blackBrush,
                                                ModifyName = "添加",
                                                FreshName = "清空",
                                                ModifyForeground = blackBrush,
                                                FreshForeground = blackBrush,
                                                Tag = new NBTDataStructure() { Result = "", Visibility = Visibility.Collapsed, DataType = Request.dataType, NBTGroup = Request.nbtType }
                                            };
                                            StackPanel itemPanel = new() { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F2F2F")) };
                                            ScrollViewer scrollViewer = new()
                                            {
                                                MaxHeight = 200,
                                                Content = itemPanel,
                                                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                                            };
                                            itemAccordion.Content = scrollViewer;
                                            itemAccordion.GotFocus += componentEvents.ValueChangedHandler;
                                            result.Add(itemAccordion);
                                            result.Remove(displayText);
                                            #region 分析是否需要代入导入的数据
                                            if (ImportMode)
                                            {
                                                string key = Request.key;
                                                if (!Summon)
                                                    key = "Item." + key;
                                                JToken data = ExternallyReadEntityData.SelectToken(key);
                                                if (data != null)
                                                {
                                                    JArray Items = JArray.Parse(data.ToString());
                                                    string itemImageFilePath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
                                                    string imagePath = "";
                                                    for (int i = 0; i < Items.Count; i++)
                                                    {
                                                        string itemID = JObject.Parse(Items[i].ToString())["id"].ToString();
                                                        Image image = new() { Tag = new NBTDataStructure() { Result = Items[i].ToString(), Visibility = Visibility.Collapsed, DataType = Request.dataType, NBTGroup = Request.nbtType } };
                                                        imagePath = itemImageFilePath + itemID + ".png";
                                                        if (File.Exists(imagePath))
                                                            image.Source = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute));
                                                        InlineItems itemBag = new();
                                                        (itemBag.DisplayItem.Child as Image).Source = image.Source;
                                                        itemPanel.Children.Add(itemBag);
                                                    }
                                                }
                                            }
                                            #endregion
                                        }
                                        break;
                                    case "LodestonePos":
                                        {
                                            Accordion itemAccordion = new()
                                            {
                                                MaxHeight = 200,
                                                Uid = Request.dependency,
                                                Name = Request.key,
                                                Style = Application.Current.Resources["AccordionStyle"] as Style,
                                                Background = orangeBrush,
                                                Title = Request.description,
                                                BorderThickness = new Thickness(0),
                                                Margin = new Thickness(2, 2, 2, 0),
                                                TitleForeground = blackBrush,
                                                ModifyVisibility = Visibility.Collapsed,
                                                FreshVisibility = Visibility.Collapsed,
                                                Tag = new NBTDataStructure() { Result = "", Visibility = Visibility.Collapsed, DataType = Request.dataType, NBTGroup = Request.nbtType }
                                            };
                                            UUIDOrPosGroup uUIDOrPosGroup = new() { IsUUID = false };
                                            ScrollViewer scrollViewer = new()
                                            {
                                                MaxHeight = 200,
                                                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F2F2F")),
                                                Content = uUIDOrPosGroup,
                                                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                                            };
                                            itemAccordion.Content = scrollViewer;
                                            itemAccordion.GotFocus += componentEvents.ValueChangedHandler;
                                            result.Add(itemAccordion);
                                            result.Remove(displayText);
                                            #region 分析是否需要代入导入的数据
                                            if (ImportMode)
                                            {
                                                string key = Request.key;
                                                if (!Summon)
                                                    key = "Item." + key;
                                                JToken data = ExternallyReadEntityData.SelectToken(key);
                                                if (data != null)
                                                {
                                                    JToken x = data["X"];
                                                    JToken y = data["Y"];
                                                    JToken z = data["Z"];
                                                    uUIDOrPosGroup.EnableButton.IsChecked = true;
                                                    if (x != null)
                                                        uUIDOrPosGroup.number0.Value = int.Parse(x.ToString());
                                                    if (y != null)
                                                        uUIDOrPosGroup.number1.Value = int.Parse(y.ToString());
                                                    if (z != null)
                                                        uUIDOrPosGroup.number2.Value = int.Parse(z.ToString());
                                                    componentEvents.LodestonePos_LostFocus(itemAccordion, null);
                                                }
                                            }
                                            #endregion
                                        }
                                        break;
                                    case "DebugProperty":
                                        {
                                            Accordion itemAccordion = new()
                                            {
                                                MaxHeight = 200,
                                                Uid = Request.dependency,
                                                Name = Request.key,
                                                Style = Application.Current.Resources["AccordionStyle"] as Style,
                                                Background = orangeBrush,
                                                Title = Request.description,
                                                BorderThickness = new Thickness(0),
                                                ModifyName = "添加",
                                                FreshName = "清空",
                                                ModifyForeground = blackBrush,
                                                FreshForeground = blackBrush,
                                                Margin = new Thickness(2, 2, 2, 0),
                                                TitleForeground = blackBrush,
                                                Tag = new NBTDataStructure() { Result = "", Visibility = Visibility.Collapsed, DataType = Request.dataType, NBTGroup = Request.nbtType }
                                            };
                                            StackPanel stackPanel = new();
                                            ScrollViewer scrollViewer = new()
                                            {
                                                MaxHeight = 200,
                                                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F2F2F")),
                                                Content = stackPanel,
                                                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                                            };
                                            itemAccordion.Content = scrollViewer;
                                            itemAccordion.GotFocus += componentEvents.ValueChangedHandler;
                                            result.Add(itemAccordion);
                                            result.Remove(displayText);
                                            #region 分析是否需要代入导入的数据
                                            if (ImportMode)
                                            {
                                                string key = Request.key;
                                                if (!Summon)
                                                    key = "Item." + key;
                                                if (ExternallyReadEntityData.SelectToken(key) is JObject data)
                                                {
                                                    List<JProperty> properties = data.Properties().ToList();
                                                    for (int i = 0; i < properties.Count; i++)
                                                    {
                                                        string currentId = BlockTable.Select("id='" + properties[i].Name + "'").First()["id"].ToString();
                                                        DebugProperties debugProperties = new();
                                                        debugProperties.BlockId.SelectedValuePath = "ComboBoxItemId";
                                                        debugProperties.BlockId.SelectedValue = currentId;
                                                        debugProperties.BlockProperty.SelectedValue = properties[i].Value.ToString();
                                                        stackPanel.Children.Add(debugProperties);
                                                    }
                                                    componentEvents.DebugProperties_LostFocus(itemAccordion, null);
                                                }
                                            }
                                            #endregion
                                        }
                                        break;
                                    case "MapDisplay":
                                        {
                                            Accordion itemAccordion = new()
                                            {
                                                MaxHeight = 200,
                                                Uid = Request.dependency,
                                                Name = Request.key,
                                                Style = Application.Current.Resources["AccordionStyle"] as Style,
                                                Background = orangeBrush,
                                                Title = Request.description,
                                                BorderThickness = new Thickness(0),
                                                Margin = new Thickness(2, 2, 2, 0),
                                                TitleForeground = blackBrush,
                                                ModifyName = "添加",
                                                FreshName = "清空",
                                                ModifyVisibility = Visibility.Collapsed,
                                                FreshVisibility = Visibility.Collapsed,
                                                ModifyForeground = blackBrush,
                                                FreshForeground = blackBrush,
                                                Tag = new NBTDataStructure() { Result = "", Visibility = Visibility.Collapsed, DataType = Request.dataType, NBTGroup = Request.nbtType }
                                            };
                                            StackPanel itemPanel = new();
                                            MapDisplay mapDisplay = new();
                                            mapDisplay.EnableButton.IsChecked = true;
                                            itemPanel.Children.Add(mapDisplay);
                                            ScrollViewer scrollViewer = new()
                                            {
                                                MaxHeight = 200,
                                                Content = itemPanel,
                                                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F2F2F")),
                                                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                                            };
                                            itemAccordion.Content = scrollViewer;
                                            itemAccordion.GotFocus += componentEvents.ValueChangedHandler;
                                            result.Add(itemAccordion);
                                            result.Remove(displayText);
                                            #region 分析是否需要代入导入的数据
                                            if (ImportMode)
                                            {
                                                string key = Request.key;
                                                if (!Summon)
                                                    key = "Item." + key;
                                                if (ExternallyReadEntityData.SelectToken(key) is JObject data)
                                                {
                                                    JToken colorObj = data["MapColor"];
                                                    if (colorObj != null)
                                                    {
                                                        mapDisplay.color.Value = int.Parse(colorObj.ToString());
                                                        componentEvents.MapDisplay_LostFocus(itemAccordion, null);
                                                    }
                                                }
                                            }
                                            #endregion
                                        }
                                        break;
                                    case "BannerBlockEntityTag":
                                        {
                                            Accordion itemAccordion = new()
                                            {
                                                MaxHeight = 200,
                                                Uid = Request.dependency,
                                                Name = Request.key,
                                                Style = Application.Current.Resources["AccordionStyle"] as Style,
                                                Background = orangeBrush,
                                                Title = Request.description,
                                                BorderThickness = new Thickness(0),
                                                ModifyVisibility = Visibility.Collapsed,
                                                FreshVisibility = Visibility.Collapsed,
                                                Margin = new Thickness(2, 2, 2, 0),
                                                TitleForeground = blackBrush,
                                                Tag = new NBTDataStructure() { Result = "", Visibility = Visibility.Collapsed, DataType = Request.dataType, NBTGroup = Request.nbtType }
                                            };
                                            StackPanel stackPanel = new();
                                            ShieldBlockEntityTag shieldBlockEntityTag = new();
                                            stackPanel.Children.Add(shieldBlockEntityTag);
                                            ScrollViewer scrollViewer = new()
                                            {
                                                MaxHeight = 200,
                                                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F2F2F")),
                                                Content = stackPanel,
                                                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                                            };
                                            itemAccordion.Content = scrollViewer;
                                            itemAccordion.GotFocus += componentEvents.ValueChangedHandler;
                                            result.Add(itemAccordion);
                                            result.Remove(displayText);
                                            #region 分析是否需要代入导入的数据
                                            if (ImportMode)
                                            {
                                                string key = Request.key;
                                                if (!Summon)
                                                    key = "Item." + key;
                                                if (ExternallyReadEntityData.SelectToken(key) is JObject data)
                                                {
                                                    JToken baseObj = ExternallyReadEntityData.SelectToken("Base");
                                                    if (baseObj != null)
                                                        shieldBlockEntityTag.Base.SelectedIndex = int.Parse(baseObj.ToString());
                                                    StackPanel bannerPanel = (shieldBlockEntityTag.BannerAccordion.Content as ScrollViewer).Content as StackPanel;
                                                    BannerBlockEntityTag bannerBlockEntityTag = new();
                                                    JToken customname = data["CustomName"];
                                                    if (customname != null)
                                                    {
                                                        if (customname is JValue)
                                                        {
                                                            JObject customNameObj = JObject.Parse((customname as JValue).Value<string>());
                                                            bannerBlockEntityTag.CustomName.Text = customNameObj["text"].ToString();
                                                        }
                                                        else
                                                            bannerBlockEntityTag.CustomName.Text = Regex.Match(customname.ToString(), @"[a-zA-Z_]+").ToString();
                                                    }

                                                    if (data["Patterns"] is JArray PatternList)
                                                    {
                                                        StackPanel patternListPanel = (bannerBlockEntityTag.BannerPatternAccordion.Content as ScrollViewer).Content as StackPanel;
                                                        foreach (JObject Apattern in PatternList.Cast<JObject>())
                                                        {
                                                            BannerPatterns bannerPatterns = new();
                                                            patternListPanel.Children.Add(bannerPatterns);
                                                            JToken color = Apattern["Color"];
                                                            JToken pattern = Apattern["Pattern"];
                                                            if (color != null)
                                                                bannerPatterns.Color.SelectedIndex = int.Parse(color.ToString());
                                                            if (pattern != null)
                                                            {
                                                                bannerPatterns.Pattern.SelectedValuePath = "ComboBoxItemText";
                                                                bannerPatterns.Pattern.SelectedValue = pattern.ToString();
                                                            }
                                                        }
                                                    }
                                                    bannerPanel.Children.Add(bannerBlockEntityTag);
                                                    componentEvents.ShieldBlockEntityTag_LostFocus(itemAccordion, null);
                                                }
                                            }
                                            #endregion
                                        }
                                        break;
                                }
                            }
                        }
                        break;
                    case "TAG_Byte":
                    case "TAG_Int":
                    case "TAG_Float":
                    case "TAG_Double":
                    case "TAG_Short":
                    case "TAG_Pos":
                    case "TAG_UUID":
                        {
                            double minValue = 0;
                            double maxValue = 0;
                            if (Request.dataType == "TAG_Byte")
                            {
                                minValue = byte.MinValue;
                                maxValue = byte.MaxValue;
                            }
                            else
                                if (Request.dataType == "TAG_Int" || Request.dataType == "TAG_UUID")
                            {
                                minValue = int.MinValue;
                                maxValue = int.MaxValue;
                            }
                            else
                                if (Request.dataType == "TAG_Float")
                            {
                                minValue = float.MinValue;
                                maxValue = float.MaxValue;
                            }
                            else
                                if (Request.dataType == "TAG_Double" || Request.dataType == "TAG_Pos")
                            {
                                minValue = double.MinValue;
                                maxValue = double.MaxValue;
                            }
                            else
                                if (Request.dataType == "TAG_Short")
                            {
                                minValue = short.MinValue;
                                maxValue = short.MaxValue;
                            }

                            if (Request.dataType == "TAG_Pos" || Request.dataType == "TAG_UUID")
                            {
                                UUIDOrPosGroup uUIDOrPosGroup = new()
                                {
                                    Uid = Request.nbtType,
                                    Name = Request.key,
                                    IsUUID = Request.dataType == "TAG_UUID",
                                    Tag = new NBTDataStructure() { Result = "", Visibility = Visibility.Collapsed, DataType = Request.dataType, NBTGroup = Request.nbtType }
                                };
                                result.Add(uUIDOrPosGroup);
                                uUIDOrPosGroup.GotFocus += componentEvents.ValueChangedHandler;
                                #region 分析是否需要代入导入的数据
                                if (ImportMode)
                                {
                                    string key = Request.key;
                                    if (!Summon)
                                        key = "Item." + key;
                                    JToken currentObj = ExternallyReadEntityData.SelectToken(key);
                                    if (currentObj != null)
                                    {
                                        JArray dataArray = JArray.Parse(currentObj.ToString());
                                        uUIDOrPosGroup.number0.Value = int.Parse(dataArray[0].ToString());
                                        uUIDOrPosGroup.number1.Value = int.Parse(dataArray[1].ToString());
                                        uUIDOrPosGroup.number2.Value = int.Parse(dataArray[2].ToString());
                                        uUIDOrPosGroup.number3.Value = int.Parse(dataArray[3].ToString());
                                    }
                                }
                                #endregion
                            }
                            else
                            {
                                Slider numberBox1 = new()
                                {
                                    Name = Request.key,
                                    Uid = Request.nbtType,
                                    Minimum = minValue,
                                    Maximum = maxValue,
                                    Value = 0,
                                    Style = Application.Current.Resources["NumberBoxStyle"] as Style,
                                    Tag = new NBTDataStructure() { Result = "", Visibility = Visibility.Collapsed, DataType = Request.dataType, NBTGroup = Request.nbtType }
                                };
                                numberBox1.GotFocus += componentEvents.ValueChangedHandler;
                                result.Add(numberBox1);
                                #region 分析是否需要代入导入的数据
                                if (ImportMode)
                                {
                                    string key = Request.key;
                                    if (!Summon)
                                        key = "Item." + key;
                                    JToken currentObj = ExternallyReadEntityData.SelectToken(key);
                                    if (currentObj != null)
                                    {
                                        numberBox1.Value = int.Parse(currentObj.ToString());
                                        componentEvents.NumberBoxValueChanged(numberBox1, null);
                                    }
                                }
                                #endregion
                            }
                        }
                        break;
                    case "TAG_String_List":
                    case "TAG_String":
                    case "TAG_Long":
                        {
                            TextBox stringBox = new() { BorderBrush = blackBrush, Foreground = whiteBrush, Uid = Request.nbtType, Name = Request.key, Tag = new NBTDataStructure() { Result = "", Visibility = Visibility.Collapsed, DataType = Request.dataType, NBTGroup = Request.nbtType } };
                            stringBox.GotFocus += componentEvents.ValueChangedHandler;
                            if (Request.dataType == "TAG_String_List")
                            {
                                string NewToolTip = Request.toolTip + "(以,分割成员,请遵守NBT语法)";
                                displayText.ToolTip = NewToolTip;
                            }
                            result.Add(stringBox);
                            #region 分析是否需要代入导入的数据
                            if (ImportMode)
                            {
                                string key = Request.key;
                                if (!Summon)
                                    key = "Item." + key;
                                JToken currentObj = ExternallyReadEntityData.SelectToken(key);
                                if (currentObj != null)
                                {
                                    stringBox.Text = currentObj.ToString().Replace("\"", "");
                                    if (Request.dataType == "TAG_String")
                                        componentEvents.StringBox_LostFocus(stringBox, null);
                                    else
                                    if (Request.dataType == "TAG_String_List")
                                        componentEvents.StringListBox_LostFocus(stringBox, null);
                                    else
                                    if (Request.dataType == "TAG_Long")
                                        componentEvents.LongNumberBox_LostFocus(stringBox, null);
                                }
                            }
                            #endregion
                        }
                        break;
                    case "TAG_NameSpaceReference":
                        {
                            Grid grid = new() { Uid = Request.nbtType, Name = Request.key, Tag = new NBTDataStructure() { Result = "", Visibility = Visibility.Collapsed, DataType = Request.dataType, NBTGroup = Request.nbtType } };
                            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Star) });
                            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) });
                            TextBox stringBox = new() { IsReadOnly = true, CaretBrush = whiteBrush, BorderBrush = blackBrush, Foreground = whiteBrush };
                            IconTextButtons textButtons = new()
                            {
                                Style = Application.Current.Resources["IconTextButton"] as Style,
                                Background = buttonNormalBrush,
                                PressedBackground = buttonPressedBrush,
                                Content = "设置引用",
                                Padding = new Thickness(5, 2, 5, 2)
                            };
                            grid.Children.Add(stringBox);
                            grid.Children.Add(textButtons);
                            Grid.SetColumn(stringBox, 0);
                            Grid.SetColumn(textButtons, 1);
                            grid.GotFocus += componentEvents.ValueChangedHandler;
                            result.Add(grid);
                            #region 分析是否需要代入导入的数据
                            if (ImportMode)
                            {
                                string key = Request.key;
                                if (!Summon)
                                    key = "Item." + key;
                                JToken currentObj = ExternallyReadEntityData.SelectToken(key);
                                if (currentObj != null)
                                {
                                    stringBox.Text = currentObj.ToString().Replace("\"", "");
                                    componentEvents.NameSpaceReference_LostFocus(stringBox, null);
                                }
                            }
                            #endregion
                        }
                        break;
                    case "TAG_Boolean":
                        {
                            TextCheckBoxs textCheckBoxs = new()
                            {
                                Uid = Request.nbtType,
                                Name = Request.key,
                                Foreground = whiteBrush,
                                VerticalContentAlignment = VerticalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                HorizontalAlignment = HorizontalAlignment.Stretch,
                                HeaderWidth = 20,
                                HeaderHeight = 20,
                                Style = Application.Current.Resources["TextCheckBox"] as Style,
                                Content = Request.description,
                                Tag = new NBTDataStructure() { Result = "", Visibility = Visibility.Collapsed, DataType = Request.dataType, NBTGroup = Request.nbtType }
                            };
                            Grid.SetColumnSpan(textCheckBoxs, 2);
                            if (Request.toolTip.Length > 0)
                            {
                                textCheckBoxs.ToolTip = Request.toolTip;
                                ToolTipService.SetBetweenShowDelay(textCheckBoxs, 0);
                                ToolTipService.SetInitialShowDelay(textCheckBoxs, 0);
                            }
                            result.Remove(displayText);
                            textCheckBoxs.GotFocus += componentEvents.ValueChangedHandler;
                            result.Add(textCheckBoxs);
                            #region 分析是否需要代入导入的数据
                            if (ImportMode)
                            {
                                string key = Request.key;
                                if (!Summon)
                                    key = "Item." + key;
                                JToken currentObj = ExternallyReadEntityData.SelectToken(key);
                                if (currentObj != null)
                                {
                                    textCheckBoxs.IsChecked = currentObj.ToString() == "1" || currentObj.ToString() == "true";
                                    componentEvents.CheckBox_Checked(textCheckBoxs, null);
                                }
                            }
                            #endregion
                        }
                        break;
                    case "TAG_Enum":
                        {
                            MatchCollection matchCollection = Regex.Matches(Request.toolTip, @"[a-zA-Z_]+");
                            List<string> enumValueList = matchCollection.ToList().ConvertAll(item => item.ToString());
                            if (enumValueList.Count == 0)
                                enumValueList =
                                [
                                    .. File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Item\\data\\" + Request.dependency + ".ini"),
                                ];
                            ComboBox comboBox = new()
                            {
                                ItemsSource = enumValueList,
                                Height = 25,
                                Uid = Request.nbtType,
                                Foreground = whiteBrush,
                                VerticalContentAlignment = VerticalAlignment.Center,
                                Style = Application.Current.Resources["TextComboBoxStyle"] as Style,
                                Name = Request.key,
                                SelectedIndex = 0,
                                Tag = new NBTDataStructure() { resultType = Request.resultType, Result = "", Visibility = Visibility.Collapsed, DataType = Request.dataType, NBTGroup = Request.nbtType }
                            };
                            comboBox.GotFocus += componentEvents.ValueChangedHandler;
                            result.Add(comboBox);
                            #region 分析是否需要代入导入的数据
                            if (ImportMode)
                            {
                                string key = Request.key;
                                if (!Summon)
                                    key = "Item." + key;
                                JToken currentObj = ExternallyReadEntityData.SelectToken(key);
                                if (currentObj != null)
                                {
                                    comboBox.SelectedIndex = enumValueList.IndexOf(currentObj.ToString());
                                    componentEvents.EnumBox_SelectionChanged(comboBox, null);
                                }
                            }
                            #endregion
                        }
                        break;
                }
            });

            #region 删除已读取的键
            if (ImportMode)
                ExternallyReadEntityData.Remove(Request.key);
            if (ExternallyReadEntityData != null)
            {
                string RemainData = ExternallyReadEntityData.ToString();
                if (RemainData == "[]" || RemainData == "{}")
                {
                    ExternallyReadEntityData = null;
                    ImportMode = false;
                }
            }
            #endregion

            return result;
        }

        /// <summary>
        /// JSON转控件
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="nbtStructure"></param>
        private List<FrameworkElement> JsonToComponentConverter(JObject nbtStructure, string NBTType = "")
        {
            string tag = JArray.Parse(nbtStructure["tag"].ToString())[0].ToString();
            JToken resultObj = nbtStructure["resultType"];
            string result = resultObj != null ? resultObj.ToString() : "";
            string key = nbtStructure["key"].ToString();
            JToken children = nbtStructure["children"];
            JToken descriptionObj = nbtStructure["description"];
            string description = descriptionObj != null ? descriptionObj.ToString() : "";
            JToken toolTipObj = nbtStructure["toolTip"];
            string toolTip = toolTipObj != null ? toolTipObj.ToString() : "";
            JToken dependencyObj = nbtStructure["dependency"];
            string dependency = dependencyObj != null ? dependencyObj.ToString() : "";
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
            if (children != null)
                componentData.children = children.ToString();
            List<FrameworkElement> componentGroup = ComponentsGenerator(componentData);
            return componentGroup;
        }

        /// <summary>
        /// 根据物品ID实时显示隐藏特指数据
        /// </summary>
        private async Task UpdateUILayOut()
        {
            currentItemPages.Header = SelectedItemId.ComboBoxItemId + ":" + SelectedItemId.ComboBoxItemText;

            #region 搜索当前物品ID对应的JSON对象
            List<JToken> targetList = SpecialArray.Where(item =>
            {
                JObject currentObj = item as JObject;
                if (currentObj["type"].ToString() == SelectedItemId.ComboBoxItemId)
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
                    if (!specialDataDictionary.TryGetValue(SelectedItemId.ComboBoxItemId, out Grid value))
                    {
                        JArray children = JArray.Parse(targetObj["children"].ToString());
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

                        if (!specialDataDictionary.ContainsKey(SelectedItemId.ComboBoxItemId))
                            specialDataDictionary.Add(SelectedItemId.ComboBoxItemId, newGrid);
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