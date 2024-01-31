using cbhk.GeneralTools;
using cbhk.GeneralTools.Displayer;
using cbhk.GeneralTools.MessageTip;
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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace cbhk.Generators.RecipeGenerator.Components
{
    /// <summary>
    /// CraftingTable.xaml 的交互逻辑
    /// </summary>
    public partial class CraftingTable : UserControl
    {
        public CraftingTable()
        {
            InitializeComponent();
        }
    }

    public partial class craftingTableDataContext:ObservableObject
    {
        #region 字段与引用
        BitmapImage emptyImage = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Recipe\\images\\Empty.png"));
        SolidColorBrush whiteBrush = new((Color)ColorConverter.ConvertFromString("#FFFFFF"));
        SolidColorBrush grayBrush = new((Color)ColorConverter.ConvertFromString("#484848"));
        #region 存储外部导入的数据
        public bool ImportMode { get; set; } = false;
        public JObject ExternalData { get; set; } = null;
        #endregion
        #region 多选与单选
        private bool multiSelect = false;
        public bool MultiSelect
        {
            get => multiSelect;
            set
            {
                multiSelect = value;
                OnPropertyChanged();
                if (!MultiSelect)
                    for (int i = 0; i < MaterialGrid.Children.Count; i++)
                    {
                        if (MaterialMap[i].Count > 0)
                        {
                            Image currentImage = MaterialGrid.Children[i] as Image;
                            currentImage.Source = new BitmapImage(MaterialMap[i][0].ImagePath);
                            ToolTip toolTip = new()
                            {
                                Foreground = whiteBrush,
                                Background = grayBrush,
                                Content = MaterialMap[i][0].IDAndName
                            };
                            currentImage.ToolTip = toolTip;
                        }
                    }
                else
                    for (int i = 0; i < MaterialGrid.Children.Count; i++)
                    {
                        if (MaterialMap[i].Count > 1)
                        {
                            Image currentImage = MaterialGrid.Children[i] as Image;
                            CurrentMaterialCollection = MaterialMap[i];
                            GenerateBubbleChart.Generator(ref currentImage,CurrentMaterialCollection);
                            ToolTip toolTip = new()
                            {
                                Foreground = whiteBrush,
                                Background = grayBrush,
                                Content = "物品组，左击编辑"
                            };
                            currentImage.ToolTip = toolTip;
                        }
                    }
            }
        }
        #endregion
        #region 有序与无序
        private bool shaped = false;
        public bool Shaped
        {
            get => shaped;
            set => SetProperty(ref shaped, value);
        }
        #endregion
        /// <summary>
        /// 9个槽位的合成材料的多选列表
        /// </summary>
        public Dictionary<int, ObservableCollection<ItemStructure>> MaterialMap = [];
        /// <summary>
        /// 多选模式材料视图数据源
        /// </summary>
        private CollectionViewSource MultiMaterialSource = new();
        #region 用于为前台绑定多选模式下的物品视图数据源
        private ObservableCollection<ItemStructure> currentMaterialCollection = [];
        public ObservableCollection<ItemStructure> CurrentMaterialCollection
        {
            get => currentMaterialCollection;
            set => SetProperty(ref currentMaterialCollection,value);
        }
        #endregion
        #region 9个槽位的key
        private ObservableCollection<string> materialKeys = [];
        public ObservableCollection<string> MaterialKeys
        {
            get => materialKeys;
            set=>SetProperty(ref materialKeys, value);
        }
        #region 记录当前的Key
        private string currentKey = "";
        public string CurrentKey
        {
            get => currentKey;
            set => SetProperty(ref currentKey,value);
        }
        #endregion
        #endregion
        #region 9个槽位的Tag
        private Dictionary<int, ObservableCollection<string>> materialTag = [];
        public Dictionary<int, ObservableCollection<string>> MaterialTag
        {
            get => materialTag;
            set => SetProperty(ref materialTag, value);
        }
        /// <summary>
        /// 记录多选模式当前选中的物品索引,用于设置Tag数据
        /// </summary>
        int MultiModeCurrentSelectedItemIndex = 0;
        #region 记录当前的Tag
        private string currentTag = "";
        public string CurrentTag
        {
            get => currentTag;
            set => SetProperty(ref currentTag, value);
        }
        #endregion
        #endregion
        /// <summary>
        /// 顺位枚举key
        /// </summary>
        private string[] EnumKeys = new string[] { "m", "o", "j", "a", "n", "g", "d", "s", "b" };
        /// <summary>
        /// 枚举Key索引
        /// </summary>
        private int EnumKeyIndex = 0;
        /// <summary>
        /// 当前所选定的槽位索引
        /// </summary>
        private int CurrentSelectedSlotIndex = 0;
        #region 物品搜索字符串
        private string searchText = "";
        public string SearchText
        {
            get => searchText;
            set
            {
                SetProperty(ref searchText,value);
                if(MultiMaterialGrid?.Visibility == Visibility.Visible && MultiMaterialViewer?.Visibility == Visibility.Visible)
                MultiMaterialSource.View?.Refresh();
            }
        }
        #endregion
        /// <summary>
        /// 存储最终结果
        /// </summary>
        public string Result { get; set; } = "";
        /// <summary>
        /// 需要保存
        /// </summary>
        public bool NeedSave { get; set; } = true;
        /// <summary>
        /// 材质面板
        /// </summary>
        private Grid MaterialGrid = null;
        /// <summary>
        /// 结果物品
        /// </summary>
        public Image ResultItem = null;
        /// <summary>
        /// 多选材质面板
        /// </summary>
        private Grid MultiMaterialGrid = null;
        /// <summary>
        /// 多选材质视图
        /// </summary>
        private ListView MultiMaterialViewer = null;
        #region 组标识符
        private string groupName = "";
        public string GroupName
        {
            get => groupName;
            set => SetProperty(ref groupName, value);
        }
        #endregion
        #region 配方文件名
        private string fileName = "";
        public string FileName
        {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }
        #endregion
        #region 配方结果数量
        private double count = 1;
        public double Count
        {
            get => count;
            set => SetProperty(ref count, value);
        }
        #endregion
        #endregion

        DataTable ItemTable = null;

        public craftingTableDataContext()
        {
            #region 初始化数据
            for (int i = 0; i < 9; i++)
            {
                MaterialMap.Add(i, []);
                MaterialKeys.Add("");
                ObservableCollection<string> tags = [""];
                MaterialTag.Add(i, tags);
            }
            #endregion
        }

        /// <summary>
        /// 多选材质视图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MultiMaterialViewer_Loaded(object sender,RoutedEventArgs e)
        {
            MultiMaterialViewer = sender as ListView;
        }

        /// <summary>
        /// 载入多选材质面板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MultiMaterialGrid_Loaded(object sender,RoutedEventArgs e)
        {
            MultiMaterialGrid = sender as Grid;
            CraftingTable craftingTable = MultiMaterialGrid.FindParent<CraftingTable>();
            MultiMaterialSource = craftingTable.Resources["MultiModeItemViewSource"] as CollectionViewSource;
            MultiMaterialSource.Filter += MultiMaterialSource_Filter;

            RecipeDataContext context = Window.GetWindow(MultiMaterialGrid).DataContext as RecipeDataContext;
            ItemTable = context.ItemTable;
        }

        /// <summary>
        /// 载入材料面板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void MaterialGrid_Loaded(object sender,RoutedEventArgs e)
        {
            MaterialGrid = sender as Grid;
            if (ImportMode)
                await MaterialsLoaded();
            else
                foreach (Image item in MaterialGrid.Children)
                {
                    item.Source ??= emptyImage;
                }
        }

        /// <summary>
        /// 异步载入外部材料数据
        /// </summary>
        /// <returns></returns>
        private async Task MaterialsLoaded()
        {
            CraftingTable craftingTable = MaterialGrid.FindParent<CraftingTable>();
            await craftingTable.Dispatcher.InvokeAsync(() =>
            {
                string craftingType = ExternalData.SelectToken("type").ToString().Replace("minecraft:","");
                if(craftingType == "crafting_shaped")
                {
                    Shaped = true;
                    if(ExternalData.SelectToken("pattern") is JArray pattern)
                    {
                        string[] OneToThree = pattern[0].ToString().Split(' ');
                        string[] FourToSix = pattern[1].ToString().Split(' ');
                        string[] SevenToNine = pattern[2].ToString().Split(' ');
                        for (int i = 0; i < MaterialKeys.Count; i++)
                        {
                            if (i < 3)
                                MaterialKeys[i] = OneToThree[i];
                            if (i >= 3 && i < 6)
                                MaterialKeys[i] = FourToSix[i - 3];
                            if (i >= 6 && i < 9)
                                MaterialKeys[i] = SevenToNine[i - 6];
                        }
                    }
                    if(ExternalData.SelectToken("key") is JObject keyObj)
                    {
                        for (int i = 0; i < MaterialKeys.Count; i++)
                        {
                            if (MaterialKeys[i].Length > 0)
                            {
                                if (keyObj[MaterialKeys[i]] is JObject keyMean)
                                {
                                    if (keyMean["item"] is JToken item)
                                    {
                                        string itemID = item.ToString().Replace("minecraft:", "");
                                        Uri iconUri = new(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + itemID.ToString() + ".png");
                                        string itemName = ItemTable.Select("id='" + itemID + "'").First()["name"].ToString();
                                        MaterialMap[i].Add(new ItemStructure(iconUri, itemID + ":" + itemName));
                                        (MaterialGrid.Children[i] as Image).Source = new BitmapImage(iconUri);
                                    }
                                    if (keyMean["tag"] is JToken tag)
                                    {
                                        MaterialTag[i].Add(tag.ToString());
                                    }
                                }
                                else
                                    if (keyObj[MaterialKeys[i]] is JArray keyMeans)
                                {
                                    MultiSelect = true;
                                    foreach (var itemObj in keyMeans)
                                    {
                                        if (itemObj["item"] is JToken item)
                                        {
                                            string itemID = item.ToString().Replace("minecraft:", "");
                                            Uri iconUri = new(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + itemID.ToString() + ".png");
                                            string itemName = ItemTable.Select("id='" + itemID + "'").First()["name"].ToString();
                                            MaterialMap[i].Add(new ItemStructure(iconUri, itemID + ":" + itemName));
                                        }
                                        if (itemObj["tag"] is JToken tag)
                                        {
                                            MaterialTag[i].Add(tag.ToString());
                                        }
                                    }
                                    CurrentMaterialCollection = MaterialMap[i];
                                    Image currentImage = MaterialGrid.Children[i] as Image;
                                    GenerateBubbleChart.Generator(ref currentImage, CurrentMaterialCollection);
                                }
                            }
                        }
                    }
                    ExternalData.Remove("pattern");
                    ExternalData.Remove("key");
                }
                else
                {
                    if (ExternalData.SelectToken("ingredients") is JArray ingredients)
                    {
                        for (int i = 0; i < ingredients.Count; i++)
                        {
                            if (ingredients[i] is JArray items)
                            {
                                foreach (var item in items)
                                {
                                    JToken itemIDObj = item[i].SelectToken("item");
                                    JToken itemTagObj = item[i].SelectToken("tag");
                                    if (itemIDObj != null)
                                    {
                                        string itemID = itemIDObj.ToString().Replace("minecraft:", "");
                                        Uri iconUri = new(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + itemID + ".png");
                                        string itemName = ItemTable.Select("id='" + itemID + "'").First()["name"].ToString();
                                        MaterialMap[i].Add(new ItemStructure(iconUri, itemID + ":" + itemName));
                                        if (itemTagObj != null)
                                            MaterialTag[i].Add(itemTagObj.ToString());
                                    }
                                }
                                CurrentMaterialCollection = MaterialMap[i];
                                Image currentImage = MaterialGrid.Children[i] as Image;
                                GenerateBubbleChart.Generator(ref currentImage, CurrentMaterialCollection);
                            }
                            else
                            if(ingredients[i] is JObject item)
                            {
                                JToken itemIDObj = item[i].SelectToken("item");
                                JToken itemTagObj = item[i].SelectToken("tag");
                                if (itemIDObj != null)
                                {
                                    string itemID = itemIDObj.ToString().Replace("minecraft:", "");
                                    Uri iconUri = new(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + itemID + ".png");
                                    string itemName = ItemTable.Select("id='" + itemID + "'").First()["name"].ToString();
                                    MaterialMap[i].Add(new ItemStructure(iconUri, itemID + ":" + itemName));
                                    (MaterialGrid.Children[i] as Image).Source = new BitmapImage(iconUri);
                                    if (itemTagObj != null)
                                        MaterialTag[i].Add(itemTagObj.ToString());
                                }
                            }
                        }
                        ExternalData.Remove("ingredients");
                    }
                }
            });
        }

        /// <summary>
        /// 载入结果物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ResultItem_Loaded(object sender, RoutedEventArgs e)
        {
            ResultItem ??= sender as Image;
            if (ImportMode)
                ResultItemLoaded();
            else
                ResultItem.Source ??= emptyImage;
        }

        /// <summary>
        /// 异步载入外部结果数据
        /// </summary>
        /// <returns></returns>
        private void ResultItemLoaded()
        {
            if (ExternalData.SelectToken("result") is JToken recipeResult)
            {
                string itemID = recipeResult.SelectToken("item").ToString().Replace("minecraft:", "");
                JToken itemCountObj = ExternalData.SelectToken("result.count");
                Uri iconUri = null;
                string iconPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + itemID.ToString() + ".png";
                if (File.Exists(iconPath))
                iconUri = new(iconPath);
                string itemName = ItemTable.Select("id='" + itemID + "'").First()["name"].ToString();
                ResultItem.Source = new BitmapImage(iconUri);
                ResultItem.Tag = new ItemStructure(iconUri, itemID + ":" + itemName);
                if (itemCountObj != null)
                    Count = int.Parse(itemCountObj.ToString());
                ExternalData.Remove("result");
            }
        }

        [RelayCommand]
        /// <summary>
        /// 执行配方
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Run()
        {
            #region 合成最终数据
            Result = "";
            #region 有序还是无序
            string shapedData = Shaped ? "{\"type\": \"minecraft:crafting_shaped\"," : "{\"type\": \"minecraft:crafting_shapeless\",";
            #endregion
            #region 组标识符
            string groupData = GroupName.Length > 0 ? "\"group\":\"" + GroupName + "\"," : "";
            #endregion
            #region 根据配方类型合并材料数据
            StringBuilder keyData = new();
            string patternData = "";
            if (Shaped)
            {
                #region 收集Key数据
                patternData = "\"pattern\":[\"";
                for (int i = 0; i < MaterialKeys.Count; i++)
                {
                    if (MaterialKeys[i].Trim() == "")
                        MaterialKeys[i] = EnumKeys[i];
                    patternData += (MaterialMap[i].Count > 0 && MaterialMap[i][0].IDAndName.Length > 0 ? MaterialKeys[i] : "") + " ";
                    if ((i + 1) % 3 == 0 && i < (MaterialKeys.Count - 1))
                    {
                        if (patternData.EndsWith(' '))
                            patternData = patternData[..(patternData.Length - 1)];
                        patternData += "\",\r\n\"";
                    }
                    if (i == (MaterialKeys.Count - 1))
                    {
                        patternData = patternData[..(patternData.Length - 1)];
                        patternData += "\"";
                    }
                }

                patternData += "],";
                #endregion
                #region 合并Key代表的物品数据
                keyData.Append("\"key\":{");
                int index = 0;
                foreach (KeyValuePair<int,ObservableCollection<ItemStructure>> material in MaterialMap)
                {
                    ObservableCollection<ItemStructure> itemStructureList = material.Value;
                    int tagIndex = -1;
                    string itemData = "";
                    string itemID = "";
                    if (MultiSelect)
                    {
                        itemData = string.Join(",", itemStructureList.Select(item =>
                        {
                            tagIndex++;
                            itemID = item.IDAndName[..item.IDAndName.IndexOf(':')];
                            return "{\"item\":\"minecraft:" + itemID + "\"" + (MaterialTag[index].Count > 0 && tagIndex < MaterialTag[index].Count && MaterialTag[index][tagIndex].Length > 0 ? "\",\"tag\":\"" + MaterialTag[index][tagIndex] + "\"}" : "}");
                        }));
                        if(itemData.Length > 0)
                        keyData.Append("\"" + MaterialKeys[material.Key] + "\":[" + itemData + "],");
                    }
                    else
                    if(itemStructureList.Count > 0)
                    {
                        itemID = itemStructureList[0].IDAndName[..itemStructureList[0].IDAndName.IndexOf(':')];
                        itemData = "{\"item\":\"minecraft:" + itemID + "\"" + (MaterialTag[index].Count > 0 && MaterialTag[index][0].Length > 0 ? "\",\"tag\":\"" + MaterialTag[index][0] + "\"}" : "}");
                        if(itemData.Length > 0)
                        keyData.Append("\"" + MaterialKeys[material.Key] + "\":" + itemData + ",");
                    }
                    index++;
                }
                //去除末尾逗号并追加闭中括号
                if (keyData.ToString().EndsWith(","))
                    keyData.Remove(keyData.ToString().Length - 1, 1);
                keyData.Append("},");
                #endregion
            }
            else
            {
                #region 合并无序数据
                keyData.Append("\"ingredients\":[");
                for (int i = 0; i < MaterialMap.Count; i++)
                {
                    ObservableCollection<ItemStructure> itemStructures = MaterialMap[i];
                    string itemID = "";
                    if (itemStructures.Count > 0 && !MultiSelect)
                    {
                        itemID = itemStructures[0].IDAndName[..itemStructures[0].IDAndName.IndexOf(':')];
                        keyData.Append("{\"item\":\"minecraft:" + itemID + "\"" + (MaterialTag[i].Count > 0 && MaterialTag[i].Count > 0 && MaterialTag[i][0].Length > 0 ? "\",\"tag\":\"" + MaterialTag[i][0] + "\"}," : "},"));
                    }
                    else
                        if(itemStructures.Count > 1 && MultiSelect)
                    {
                        keyData.Append("[");
                        for (int j = 0; j < itemStructures.Count; j++)
                        {
                            itemID = itemStructures[j].IDAndName[..itemStructures[j].IDAndName.IndexOf(':')];
                            keyData.Append("{\"item\":\"minecraft:" + itemID + "\"" + (MaterialTag[i].Count > 0 && j <= (MaterialTag[i].Count - 1) && MaterialTag[i][j].Length > 0 ? "\",\"tag\":\"" + MaterialTag[i][j] + "\"}," : "},"));
                        }
                        //去除末尾逗号并追加闭中括号
                        if (keyData.ToString().EndsWith(","))
                            keyData.Remove(keyData.ToString().Length - 1, 1);
                        keyData.Append("],");
                    }
                }
                //去除末尾逗号并追加闭中括号
                if (keyData.ToString().EndsWith(","))
                    keyData.Remove(keyData.ToString().Length - 1, 1);
                keyData.Append("],");
                #endregion
            }
            #region 合并合成结果数据
            StringBuilder resultData = new("\"result\":{\"item\":");
            ItemStructure resultStructure = ResultItem.Tag as ItemStructure;
            string resultItemID = resultStructure != null?"\"minecraft:" + resultStructure.IDAndName[..resultStructure.IDAndName.IndexOf(':')] + "\"":"\"\"";
            string count = Count > 1 ?",\"count\":"+int.Parse(Count.ToString()) + "}}":"}}";
            resultData.Append(resultItemID);
            resultData.Append(count);
            #endregion
            #endregion
            #region 合并最终结果
            Result += shapedData + groupData + patternData + keyData + resultData.ToString();
            #endregion
            #endregion
            #region 选择生成路径，执行生成
            if(NeedSave)
            {
                Microsoft.Win32.SaveFileDialog saveFileDialog = new()
                {
                    AddExtension = true,
                    Filter = "Json文件|*.json;",
                    DefaultExt = ".json",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                    RestoreDirectory = true,
                    Title = "选择配方文件存储路径"
                };
                if (saveFileDialog.ShowDialog().Value)
                {
                    _ = File.WriteAllTextAsync(saveFileDialog.FileName, Result);
                    Message.PushMessage("工作台配方生成成功！", MessageBoxImage.Information);
                    //OpenFolderThenSelectFiles.ExplorerFile(saveFileDialog.FileName);
                }
            }
            #endregion
        }

        /// <summary>
        /// 处理被拖拽的物品数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void GetDropData(object sender, DragEventArgs e)
        {
            Image image = e.Data.GetData(typeof(Image).ToString()) as Image;
            Image currentImage = sender as Image;
            ItemStructure itemStructure = image.Tag as ItemStructure;

            int rowIndex = Grid.GetRow(currentImage);
            int columnIndex = Grid.GetColumn(currentImage);
            int multiMaterialIndex = columnIndex + (rowIndex * 3);
            CurrentSelectedSlotIndex = multiMaterialIndex;

            ToolTip toolTip = new()
            {
                Foreground = whiteBrush,
                Background = grayBrush,
                Content = itemStructure.IDAndName
            };
            ToolTipService.SetBetweenShowDelay(currentImage, 0);
            ToolTipService.SetInitialShowDelay(currentImage, 0);
            if (currentImage.Parent == MaterialGrid)
            {
                if (!MultiSelect)
                {
                    if (MaterialMap[CurrentSelectedSlotIndex].Count == 0)
                        MaterialMap[CurrentSelectedSlotIndex].Add(itemStructure);
                    else
                        MaterialMap[CurrentSelectedSlotIndex][0] = itemStructure;
                    currentImage.Source = image.Source;
                }
                else
                {
                    MaterialMap[CurrentSelectedSlotIndex].Add(itemStructure);
                    if (MaterialMap[CurrentSelectedSlotIndex].Count > 1)
                    {
                        CurrentMaterialCollection = MaterialMap[CurrentSelectedSlotIndex];
                        GenerateBubbleChart.Generator(ref currentImage, CurrentMaterialCollection);
                        toolTip.Content = "物品组，左击编辑";
                    }
                    else
                        currentImage.Source = image.Source;
                }
            }
            else
                currentImage.Source = image.Source;
            currentImage.Tag = itemStructure;
            currentImage.ToolTip = toolTip;
        }

        /// <summary>
        /// 左击槽位打开槽位数据页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SetSlotData_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Image currentImage = sender as Image;
            int rowIndex = Grid.GetRow(currentImage);
            int columnIndex = Grid.GetColumn(currentImage);
            CurrentSelectedSlotIndex = columnIndex + (rowIndex * 3);
            MultiMaterialViewer.SelectedIndex = MultiModeCurrentSelectedItemIndex = 0;
            CurrentKey = MaterialKeys[CurrentSelectedSlotIndex];
            CurrentTag = MaterialTag[CurrentSelectedSlotIndex][0];

            if (CurrentKey == "")
            {
                CurrentKey = EnumKeys[EnumKeyIndex];
                EnumKeyIndex++;
            }

            CurrentMaterialCollection = MaterialMap[CurrentSelectedSlotIndex];
            MultiMaterialGrid.Visibility = Visibility.Visible;
            MultiMaterialViewer.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 更新并关闭多选模式面板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UpdateAndCloseSlotGrid_Click(object sender, RoutedEventArgs e)
        {
            MultiMaterialGrid.Visibility = Visibility.Collapsed;
            MaterialKeys[CurrentSelectedSlotIndex] = CurrentKey.Replace(" ","").Replace("\r","").Replace("\n","");
            if (!MultiSelect)
                MaterialTag[CurrentSelectedSlotIndex][0] = CurrentTag;
            else
                MaterialTag[CurrentSelectedSlotIndex][MultiModeCurrentSelectedItemIndex] = CurrentTag;
        }

        /// <summary>
        /// 选中物品设置Tag数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SelectItemToSetTag_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 获取鼠标指针下的元素
            var element = e.OriginalSource as DependencyObject;

            // 遍历可视化树，直到找到ListView的子项
            while (element != null && element is not ListViewItem)
                element = VisualTreeHelper.GetParent(element);

            // 获取子项
            if (element is ListViewItem item)
            {
                // 获取子项在ListView中的索引
                int index = MultiMaterialViewer.ItemContainerGenerator.IndexFromContainer(item);
                //切换时赋值Tag数据
                if(MultiModeCurrentSelectedItemIndex >= 0 && MultiModeCurrentSelectedItemIndex < MaterialTag[CurrentSelectedSlotIndex].Count)
                MaterialTag[CurrentSelectedSlotIndex][MultiModeCurrentSelectedItemIndex] = CurrentTag;
                //填充标签集合
                while ((MaterialTag[CurrentSelectedSlotIndex].Count - 1) < index)
                    MaterialTag[CurrentSelectedSlotIndex].Add("");
                //更新当前选中的Tag进行编辑
                CurrentTag = MaterialTag[CurrentSelectedSlotIndex][index];
                MultiModeCurrentSelectedItemIndex = index;
            }
        }

        /// <summary>
        /// 右击删除多选库中的指定物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void DeleteAppointItem_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 获取鼠标指针下的元素
            var element = e.OriginalSource as DependencyObject;

            // 遍历可视化树，直到找到ListView的子项
            while (element != null && element is not ListViewItem)
                element = VisualTreeHelper.GetParent(element);

            // 获取子项
            if (element is ListViewItem item)
            {
                // 获取子项在ListView中的索引
                int index = MultiMaterialViewer.ItemContainerGenerator.IndexFromContainer(item);
                if (index >= 0 && index < MaterialMap[CurrentSelectedSlotIndex].Count)
                    MaterialMap[CurrentSelectedSlotIndex].RemoveAt(index);
            }

            Image currentImage = MaterialGrid.Children[CurrentSelectedSlotIndex] as Image;
            GenerateBubbleChart.Generator(ref currentImage, CurrentMaterialCollection);
            if (MaterialMap[CurrentSelectedSlotIndex].Count == 1)
            {
                currentImage.Source = new BitmapImage(MaterialMap[CurrentSelectedSlotIndex][0].ImagePath);
                ToolTip toolTip = new()
                {
                    Foreground = whiteBrush,
                    Background = grayBrush,
                    Content = MaterialMap[CurrentSelectedSlotIndex][0].IDAndName
                };
                currentImage.ToolTip = toolTip;
            }
            else
                if (MaterialMap[CurrentSelectedSlotIndex].Count == 0)
                currentImage.ToolTip = null;
        }

        /// <summary>
        /// 多选模式物品过滤事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void MultiMaterialSource_Filter(object sender, FilterEventArgs e)
        {
            if (MultiMaterialGrid?.Visibility == Visibility.Collapsed || MultiMaterialViewer?.Visibility == Visibility.Collapsed) return;
            if (SearchText.Length > 0)
            {
                string[] searchList = SearchText.ToString().Split(' ');
                ItemStructure itemStructure = e.Item as ItemStructure;
                string itemID = itemStructure.IDAndName.Split(':')[0];
                bool result = (searchList.Length > 1 && (itemStructure.IDAndName.StartsWith(searchList[0]) || itemStructure.IDAndName.EndsWith(searchList[1]))) || (searchList.Length == 1 && itemID.Contains(SearchText.ToString()));
                e.Accepted = result;
            }
            else
                e.Accepted = true;
        }
    }
}