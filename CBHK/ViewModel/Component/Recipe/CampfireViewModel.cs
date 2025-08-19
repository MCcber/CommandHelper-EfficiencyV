using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using CommunityToolkit.Mvvm.Input;
using CBHK.Model.Common;
using CBHK.View.Component.Recipe;
using CBHK.Domain;
using System.Collections.Generic;
using CBHK.Utility.MessageTip;
using CBHK.Utility.Common;

namespace CBHK.ViewModel.Component.Recipe
{
    public partial class CampfireViewModel : ObservableObject
    {
        #region Field
        private DataService _dataService = null;
        private CBHKDataContext _context = null;
        private Dictionary<string, string> ItemIDAndNameMap;
        /// <summary>
        /// 存储最终结果
        /// </summary>
        public string Result = "";
        /// <summary>
        /// 需要保存
        /// </summary>
        public bool NeedSave = true;
        public Image MaterialItem = null;
        /// <summary>
        /// 结果物品
        /// </summary>
        public Image ResultItem = null;
        BitmapImage emptyImage = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Recipe\Image\Empty.png"));
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

        #region 存储外部导入的数据
        public bool ImportMode = false;
        public JObject ExternalData = null;
        #endregion

        #endregion
        
        #region Property
        /// <summary>
        /// 材料列表
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<ItemStructure> _materialList = [];
        /// <summary>
        /// 材料数据
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<string> _materialTag = [];

        /// <summary>
        /// 记录当前的Tag
        /// </summary>
        [ObservableProperty]
        private string _currentTag = "";

        /// <summary>
        /// 物品搜索字符串
        /// </summary>
        [ObservableProperty]
        private string _searchText = "";

        /// <summary>
        /// 多选与单选
        /// </summary>
        [ObservableProperty]
        private bool _multiSelect = false;

        /// <summary>
        /// 组标识符
        /// </summary>
        [ObservableProperty]
        private string _groupName = "";

        /// <summary>
        /// 配方文件名
        /// </summary>
        [ObservableProperty]
        private string _fileName = "";

        /// <summary>
        /// 烧制时间
        /// </summary>
        [ObservableProperty]
        private double _cookingtime = 10;

        /// <summary>
        /// 获得的经验
        /// </summary>
        [ObservableProperty]
        private double _experience = 0;
        [ObservableProperty]
        private Visibility _materialMultiItemVisibility = Visibility.Collapsed;

        #endregion

        #region Method
        public CampfireViewModel(CBHKDataContext context,DataService dataService)
        {
            _dataService = dataService;
            _context = context;
            MaterialTag.Add("");
            ItemIDAndNameMap = _dataService.GetItemIDAndNameGroupByVersionMap().SelectMany(item => item.Value).ToDictionary();
        }

        /// <summary>
        /// 异步载入外部材料数据
        /// </summary>
        /// <returns></returns>
        private async Task MaterialsLoaded()
        {
            CampfireView campfire = MaterialItem.FindParent<CampfireView>();
            await campfire.Dispatcher.InvokeAsync(() =>
            {
                if (ExternalData.SelectToken("ingredient") is JObject ingredient)
                {
                    JToken itemIDObj = ExternalData.SelectToken("ingredient.item");
                    JToken itemTagObj = ExternalData.SelectToken("ingredient.tag");
                    if (itemIDObj != null)
                    {
                        string itemID = itemIDObj.ToString().Replace("minecraft:", "");
                        Uri iconUri = new(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + itemID.ToString() + ".png");
                        string itemName = ItemIDAndNameMap.First(item => item.Key == itemID).Value;
                        MaterialList.Add(new()
                        {
                            ImagePath = new BitmapImage(iconUri),
                            IDAndName = itemID + ":" + itemName
                        });
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
                            string itemName = ItemIDAndNameMap.First(item => item.Key == itemID).Value;
                            MaterialList.Add(new()
                            {
                                ImagePath = new BitmapImage(iconUri),
                                IDAndName = itemID + ":" + itemName
                            });
                            if (itemTagObj != null)
                                MaterialTag.Add(itemTagObj.ToString());
                        }
                    }
                    MaterialMultiItemVisibility = Visibility.Visible;
                }
            });
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
                string itemName = ItemIDAndNameMap.First(item => item.Key == itemID).Value;
                ResultItem.Source = new BitmapImage(iconUri);
                ResultItem.Tag = new ItemStructure()
                {
                    ImagePath = new BitmapImage(iconUri),
                    IDAndName = itemID + ":" + itemName
                };
            }
        }

        #endregion

        #region Event
        public void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (MultiMaterialGrid?.Visibility == Visibility.Visible && MultiMaterialViewer?.Visibility == Visibility.Visible)
            {
                MultiMaterialSource.View?.Refresh();
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
            Result = "{\r\n  \"type\": \"minecraft:campfire_cooking\",";
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
            string cookingTimeData = ",\"cookingtime\":" + int.Parse(Cookingtime.ToString());
            string experienceData = ",\"experience\":" + int.Parse(Experience.ToString()) + "}";
            string resultData = "\"result\":\"minecraft:" + resultItemID + "\"";
            #endregion
            #endregion

            #region 合并最终结果
            Result += groupData + keyData + resultData + cookingTimeData + experienceData;
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
                    Message.PushMessage("篝火配方生成成功！", MessageBoxImage.Information);
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
            CampfireView campfire = MultiMaterialGrid.FindParent<CampfireView>();
            MultiMaterialSource = campfire.Resources["MultiModeItemViewSource"] as CollectionViewSource;
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
            if (ImportMode)
                await MaterialsLoaded();
            else
                MaterialItem.Source ??= emptyImage;
        }

        /// <summary>
        /// 载入结果物品
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ResultItem_Loaded(object sender, RoutedEventArgs e)
        {
            ResultItem = sender as Image;
            if (ImportMode)
                ResultItemLoaded();
            else
                ResultItem.Source ??= emptyImage;
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

        [RelayCommand]
        public void SwitchMultipleMode()
        {
            MaterialMultiItemVisibility = Visibility.Collapsed;
            if (MaterialList.Count > 0 && !MultiSelect)
            {
                MaterialItem.Source = MaterialList[0].ImagePath;
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
                MaterialMultiItemVisibility = Visibility.Visible;
                ToolTip toolTip = new()
                {
                    Foreground = whiteBrush,
                    Background = grayBrush,
                    Content = "物品组，左击编辑"
                };
                MaterialItem.ToolTip = toolTip;
            }
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

            MaterialMultiItemVisibility = Visibility.Collapsed;
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
                {
                    MaterialList.Add(itemStructure);
                }
                if (MaterialList.Count > 1 && currentImage.Uid.Length > 0)
                {
                    MaterialMultiItemVisibility = Visibility.Visible;
                    toolTip.Content = "物品组，左击编辑";
                }
                else
                {
                    currentImage.Source = image.Source;
                }
            }
            currentImage.Tag = itemStructure;
            currentImage.ToolTip = toolTip;
        }

        [RelayCommand]
        /// <summary>
        /// 左击槽位打开槽位数据页面
        /// </summary>
        public void SetSlotData()
        {
            MultiMaterialViewer.SelectedIndex = MultiModeCurrentSelectedItemIndex = 0;
            CurrentTag = MaterialTag[0];
            MultiMaterialGrid.Visibility = Visibility.Visible;
            MultiMaterialViewer.Visibility = Visibility.Visible;
        }

        [RelayCommand]
        /// <summary>
        /// 更新并关闭多选模式面板
        /// </summary>
        public void UpdateAndCloseSlotGrid()
        {
            MultiMaterialGrid.Visibility = Visibility.Collapsed;
            MaterialMultiItemVisibility = Visibility.Collapsed;
            if (!MultiSelect)
            {
                MaterialTag[0] = CurrentTag;
            }
            else
            {
                if(MaterialList.Count > 1)
                {
                    MaterialMultiItemVisibility = Visibility.Visible;
                }
                MaterialTag[MultiModeCurrentSelectedItemIndex] = CurrentTag;
            }
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

            //GenerateBubbleChart.Generator(ref MaterialItem, MaterialList);
            if (MaterialList.Count == 1)
            {
                MaterialItem.Source = MaterialList[0].ImagePath;
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
        #endregion
    }
}
