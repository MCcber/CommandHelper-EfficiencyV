using cbhk.GeneralTools;
using cbhk.GeneralTools.Displayer;
using cbhk.GeneralTools.MessageTip;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using System;
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
    /// Stonecutter.xaml 的交互逻辑
    /// </summary>
    public partial class Stonecutter : UserControl
    {
        public Stonecutter()
        {
            InitializeComponent();
        }
    }

    public class stonecutterDataContext : ObservableObject
    {
        /// <summary>
        /// 运行配方
        /// </summary>
        public RelayCommand Run { get; set; }

        #region 字段与引用
        /// <summary>
        /// 存储最终结果
        /// </summary>
        public string Result { get; set; } = "";

        #region 存储外部导入的数据
        public bool ImportMode { get; set; } = false;
        public JObject ExternalData { get; set; } = null;
        #endregion

        /// <summary>
        /// 需要保存
        /// </summary>
        public bool NeedSave { get; set; } = true;
        public Image MaterialItem = null;
        /// <summary>
        /// 结果物品
        /// </summary>
        public Image ResultItem = null;
        /// <summary>
        /// 材料列表
        /// </summary>
        public ObservableCollection<ItemStructure> MaterialList { get; set; } = [];
        public ObservableCollection<string> MaterialTag { get; set; } = [];
        BitmapImage emptyImage = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Recipe\\images\\Empty.png"));
        SolidColorBrush whiteBrush = new((Color)ColorConverter.ConvertFromString("#FFFFFF"));
        SolidColorBrush grayBrush = new((Color)ColorConverter.ConvertFromString("#484848"));
        /// <summary>
        /// 多选材质面板
        /// </summary>
        private Grid MultiMaterialGrid = null;
        /// <summary>
        /// 多选材质视图
        /// </summary>
        private ListView MultiMaterialViewer = null;
        /// <summary>
        /// 记录多选模式当前选中的物品索引,用于设置Tag数据
        /// </summary>
        int MultiModeCurrentSelectedItemIndex = 0;
        /// <summary>
        /// 多选模式材料视图数据源
        /// </summary>
        private CollectionViewSource MultiMaterialSource = new();
        #region 记录当前的Tag
        private string currentTag = "";
        public string CurrentTag
        {
            get => currentTag;
            set => SetProperty(ref currentTag, value);
        }
        #endregion
        #region 物品搜索字符串
        private string searchText = "";
        public string SearchText
        {
            get => searchText;
            set
            {
                SetProperty(ref searchText, value);
                if (MultiMaterialGrid?.Visibility == Visibility.Visible && MultiMaterialViewer?.Visibility == Visibility.Visible)
                    MultiMaterialSource.View?.Refresh();
            }
        }
        #endregion
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
                if (MaterialList.Count > 0 && !MultiSelect)
                {
                    MaterialItem.Source = new BitmapImage(MaterialList[0].ImagePath);
                    ToolTip toolTip = new()
                    {
                        Foreground = whiteBrush,
                        Background = grayBrush,
                        Content = MaterialList[0].IDAndName
                    };
                    MaterialItem.ToolTip = toolTip;
                }
                else
                if (MaterialList.Count > 1 && MultiSelect)
                {
                    GenerateBubbleChart.Generator(ref MaterialItem, MaterialList);
                    ToolTip toolTip = new()
                    {
                        Foreground = whiteBrush,
                        Background = grayBrush,
                        Content = "物品组，左击编辑"
                    };
                    MaterialItem.ToolTip = toolTip;
                }
            }
        }
        #endregion
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
        #region 结果数量
        private double count = 1;
        public double Count
        {
            get => count;
            set => SetProperty(ref count, value);
        }
        #endregion

        DataTable ItemTable = null;

        public stonecutterDataContext()
        {
            #region 绑定指令
            Run = new RelayCommand(RunCommand);
            #endregion
            #region 初始化数据
            MaterialTag.Add("");
            #endregion
        }

        /// <summary>
        /// 执行配方
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void RunCommand()
        {
            #region 合成最终数据
            Result = "{\r\n  \"type\": \"minecraft:stonecutting\",";
            #region 组标识符
            string groupData = GroupName.Length > 0 ? "\"group\":\"" + GroupName + "\"," : "";
            #endregion
            #region 根据配方类型合并材料数据
            StringBuilder keyData = new();
            #region 合并无序数据
            if (MultiSelect)
            {
                keyData.Append("\"ingredient\":[");
                for (int i = 0; i < MaterialList.Count; i++)
                {
                    string itemID = MaterialList[i].IDAndName[..MaterialList[i].IDAndName.IndexOf(':')];
                    keyData.Append("{\"item\":\"minecraft:" + itemID + "\"" + (MaterialTag.Count > 0 && i < MaterialTag.Count && MaterialTag[i].Length > 0 ? ",\"tag\":\"" + MaterialTag[i] + "\"}," : "},"));
                }
                //去除末尾逗号并追加闭中括号
                if (keyData.ToString().EndsWith(","))
                    keyData.Remove(keyData.ToString().Length - 1, 1);
                keyData.Append("],");
            }
            else
            {
                keyData.Append("\"ingredient\":");
                string itemID = MaterialList[0].IDAndName[..MaterialList[0].IDAndName.IndexOf(':')];
                keyData.Append("{\"item\":\"minecraft:" + itemID + "\"" + (MaterialTag.Count > 0 && MaterialTag[0].Length > 0 ? ",\"tag\":\"" + MaterialTag[0] + "\"}," : "},"));
            }
            ItemStructure resultItemStructure = ResultItem.Tag as ItemStructure;
            string resultItemID = resultItemStructure.IDAndName[..resultItemStructure.IDAndName.IndexOf(':')];
            string countData = ",\"count\":" + int.Parse(count.ToString()) + "}";
            string resultData = "\"result\":\"minecraft:" + resultItemID + "\"";
            #endregion
            #endregion
            #region 合并最终结果
            Result += groupData + keyData + resultData + countData;
            #endregion
            #endregion
            #region 选择生成路径，执行生成
            if (NeedSave)
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
                    Message.PushMessage("切石机配方生成成功！", MessageBoxImage.Information);
                    //OpenFolderThenSelectFiles.ExplorerFile(saveFileDialog.FileName);
                }
            }
            #endregion
        }

        /// <summary>
        /// 载入多选材质面板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MultiMaterialGrid_Loaded(object sender, RoutedEventArgs e)
        {
            MultiMaterialGrid = sender as Grid;
            Stonecutter stonecutter = MultiMaterialGrid.FindParent<Stonecutter>();
            MultiMaterialSource = stonecutter.Resources["MultiModeItemViewSource"] as CollectionViewSource;
            MultiMaterialSource.Filter += MultiMaterialSource_Filter;
        }

        /// <summary>
        /// 载入材料
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void Material_Loaded(object sender, RoutedEventArgs e)
        {
            MaterialItem = sender as Image;
            MaterialItem.Source ??= emptyImage;
            if (ImportMode)
                await MaterialsLoaded();
        }

        /// <summary>
        /// 异步载入外部材料数据
        /// </summary>
        /// <returns></returns>
        private async Task MaterialsLoaded()
        {
            Stonecutter stonecutter = MaterialItem.FindParent<Stonecutter>();
            await stonecutter.Dispatcher.InvokeAsync(() =>
            {
                if (ExternalData.SelectToken("ingredient") is JObject ingredient)
                {
                    JToken itemIDObj = ExternalData.SelectToken("ingredient.item");
                    JToken itemTagObj = ExternalData.SelectToken("ingredient.tag");
                    if (itemIDObj != null)
                    {
                        string itemID = itemIDObj.ToString().Replace("minecraft:", "");
                        Uri iconUri = new(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + itemID.ToString() + ".png");
                        string itemName = ItemTable.Select("id='" + itemID + "'").First()["name"].ToString();
                        MaterialList.Add(new ItemStructure(iconUri, itemID + ":" + itemName));
                        if (itemTagObj != null)
                            MaterialTag.Add(itemTagObj.ToString());
                    }
                }
                else
                    if (ExternalData.SelectToken("ingredient") is JArray ingredients)
                {
                    foreach (JToken item in ingredients)
                    {
                        JToken itemIDObj = item.SelectToken("item");
                        JToken itemTagObj = item.SelectToken("tag");
                        if (itemIDObj != null)
                        {
                            string itemID = itemIDObj.ToString().Replace("minecraft:", "");
                            Uri iconUri = new(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + itemID + ".png");
                            string itemName = ItemTable.Select("id='" + itemID + "'").First()["name"].ToString();
                            MaterialList.Add(new ItemStructure(iconUri, itemID + ":" + itemName));
                            if (itemTagObj != null)
                                MaterialTag.Add(itemTagObj.ToString());
                        }
                    }
                    GenerateBubbleChart.Generator(ref MaterialItem, MaterialList);
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
            ResultItem = sender as Image;
            ResultItem.Source ??= emptyImage;
            if (ImportMode)
                ResultItemLoaded();
        }

        /// <summary>
        /// 异步载入外部结果数据
        /// </summary>
        /// <returns></returns>
        private void ResultItemLoaded()
        {
            if (ExternalData.SelectToken("result") is JToken recipeResult)
            {
                string itemID = recipeResult.ToString().Replace("minecraft:", "");
                Uri iconUri = new(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + itemID.ToString() + ".png");
                string itemName = ItemTable.Select("id='" + itemID + "'").First()["name"].ToString();
                ResultItem.Source = new BitmapImage(iconUri);
                ResultItem.Tag = new ItemStructure(iconUri, itemID + ":" + itemName);
            }
        }

        /// <summary>
        /// 载入多选模式视图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MultiMaterialViewer_Loaded(object sender, RoutedEventArgs e)
        {
            MultiMaterialViewer = sender as ListView;
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

            ToolTip toolTip = new()
            {
                Foreground = whiteBrush,
                Background = grayBrush,
                Content = itemStructure.IDAndName
            };
            ToolTipService.SetBetweenShowDelay(currentImage, 0);
            ToolTipService.SetInitialShowDelay(currentImage, 0);
            if (!MultiSelect)
            {
                if (currentImage.Uid.Length > 0 && MaterialList.Count > 0)
                    MaterialList[0] = itemStructure;
                else
                    if (currentImage.Uid.Length > 0)
                    MaterialList.Add(itemStructure);
                currentImage.Source = image.Source;
            }
            else
            {
                if (currentImage.Uid.Length > 0)
                    MaterialList.Add(itemStructure);
                if (MaterialList.Count > 1 && currentImage.Uid.Length > 0)
                {
                    GenerateBubbleChart.Generator(ref currentImage, MaterialList);
                    toolTip.Content = "物品组，左击编辑";
                }
                else
                    currentImage.Source = image.Source;
            }
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
            MultiMaterialViewer.SelectedIndex = MultiModeCurrentSelectedItemIndex = 0;
            CurrentTag = MaterialTag[0];
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
            if (!MultiSelect)
                MaterialTag[0] = CurrentTag;
            else
                MaterialTag[MultiModeCurrentSelectedItemIndex] = CurrentTag;
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
                if (MultiModeCurrentSelectedItemIndex >= 0 && MultiModeCurrentSelectedItemIndex < MaterialTag.Count)
                    MaterialTag[MultiModeCurrentSelectedItemIndex] = CurrentTag;
                //填充标签集合
                while ((MaterialTag.Count - 1) < index)
                    MaterialTag.Add("");
                //更新当前选中的Tag进行编辑
                CurrentTag = MaterialTag[index];
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
                if (index >= 0 && index < MaterialList.Count)
                    MaterialList.RemoveAt(index);
            }

            GenerateBubbleChart.Generator(ref MaterialItem, MaterialList);
            if (MaterialList.Count == 1)
            {
                MaterialItem.Source = new BitmapImage(MaterialList[0].ImagePath);
                ToolTip toolTip = new()
                {
                    Foreground = whiteBrush,
                    Background = grayBrush,
                    Content = MaterialList[0].IDAndName
                };
                MaterialItem.ToolTip = toolTip;
            }
            else
                if (MaterialList.Count == 0)
                MaterialItem.ToolTip = null;
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
