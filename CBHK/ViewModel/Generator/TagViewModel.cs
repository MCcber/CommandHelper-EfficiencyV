using CBHK.CustomControl;
using CBHK.Domain;
using CBHK.Model.Generator.Tag;
using CBHK.Utility.Common;
using CBHK.Utility.MessageTip;
using CBHK.View;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace CBHK.ViewModel.Generator
{
    public partial class TagViewModel : ObservableObject
    {
        #region Field
        ListView TagZone = null;
        SolidColorBrush LightOrangeBrush = new((Color)ColorConverter.ConvertFromString("#F0D08C"));
        private IContainerProvider container = null;
        private CBHKDataContext context = null;
        private DataService _dataService;
        private IProgress<TagItemTemplate> AddItemProgress = null;
        private IProgress<(int, string, string, string, string, bool)> SetItemProgress = null;
        private IProgress<bool> InitSelectedTypeItemProgress = null;
        private int LastSelectedIndex = 0;
        /// <summary>
        /// 主页引用
        /// </summary>
        private Window home = null;
        /// <summary>
        /// 对象数据源
        /// </summary>
        CollectionViewSource TagViewSource = null;

        #region 存储最终生成的列表
        public List<string> Blocks = [];
        public List<string> Items = [];
        public List<string> Entities = [];
        public List<string> GameEvent = [];
        public List<string> Biomes = [];
        #endregion

        #endregion

        #region Property
        /// <summary>
        /// 标签生成器的过滤类型数据源
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<TextComboBoxItem> _typeItemSource = [];
        /// <summary>
        /// 是否替换
        /// </summary>
        [ObservableProperty]
        private bool _replace = false;
        /// <summary>
        /// 所有标签成员
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<TagItemTemplate> _tagItemList = [];
        /// <summary>
        /// 版本列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<TextComboBoxItem> _versionList = [new TextComboBoxItem() { Text = "1.20.4" }];
        /// <summary>
        /// 已选中版本
        /// </summary>
        [ObservableProperty]
        private TextComboBoxItem _selectedVersion = null;
        /// <summary>
        /// 当前选中的值成员
        /// </summary>
        [ObservableProperty]
        private TagItemTemplate _selectedItem = null;
        /// <summary>
        /// 搜索内容
        /// </summary>
        [ObservableProperty]
        private string _searchText;
        /// <summary>
        /// 当前选中的类型成员
        /// </summary>
        [ObservableProperty]
        private TextComboBoxItem _selectedTypeItem = null;
        /// <summary>
        /// 全选
        /// </summary>
        [ObservableProperty]
        private bool _selectedAll = false;
        /// <summary>
        /// 反选
        /// </summary>
        [ObservableProperty]
        private bool _reverseAll = false;
        #endregion

        #region Method
        public TagViewModel(IContainerProvider Container,DataService DataService, CBHKDataContext Context, MainView mainView)
        {
            home = mainView;
            container = Container;
            context = Context;
            _dataService = DataService;

            InitSelectedTypeItemProgress = new Progress<bool>(item =>
            {
                SelectedTypeItem = TypeItemSource[0];
            });
            AddItemProgress = new Progress<TagItemTemplate>(TagItemList.Add);
            SetItemProgress = new Progress<(int, string, string, string, string, bool)>(item =>
            {
                Uri uri = null;
                if (File.Exists(item.Item2 + ".png"))
                {
                    uri = new Uri(item.Item2 + ".png", UriKind.RelativeOrAbsolute);
                }
                else
                if (File.Exists(item.Item2 + "_spawn_egg.png"))
                {
                    uri = new Uri(item.Item2 + "_spawn_egg.png", UriKind.RelativeOrAbsolute);
                }
                TagItemList[item.Item1].Icon = uri;
                TagItemList[item.Item1].DisplayId = item.Item3;
                TagItemList[item.Item1].DisplayName = item.Item4;
                TagItemList[item.Item1].DataType = item.Item5;
                TagItemList[item.Item1].BeChecked = item.Item6;
            });
        }

        /// <summary>
        /// 反转目标成员的值
        /// </summary>
        /// <param name="CurrentItem"></param>
        private void ReverseValue(TagItemTemplate CurrentItem)
        {
            int index = TagItemList.IndexOf(CurrentItem);
            int itemCount = context.ItemSet.Count();
            int entityCount = context.EntitySet.Count();
            string itemString = CurrentItem.DisplayId;
            if (itemString.Trim().Length > 0)
            {
                CurrentItem.BeChecked = !CurrentItem.BeChecked;
                if (CurrentItem.BeChecked.Value)
                    CurrentItem.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0D08C"));
                else
                    CurrentItem.Background = null;
                if (CurrentItem.BeChecked.Value)
                {
                    if (CurrentItem.DataType == "ItemView" && !Items.Contains("\"minecraft:" + itemString + "\","))
                        Items.Add("\"minecraft:" + itemString + "\",");
                    else
                    if (CurrentItem.DataType == "EntityView" && !Entities.Contains("\"minecraft:" + itemString + "\","))
                        Entities.Add("\"minecraft:" + itemString + "\",");
                    else
                    if (CurrentItem.DataType == "Block&ItemView" && !Blocks.Contains("\"minecraft:" + itemString + "\","))
                        Blocks.Add("\"minecraft:" + itemString + "\",");
                    else
                    if (CurrentItem.DataType == "Biome" && !Biomes.Contains("\"minecraft:" + itemString + "\","))
                        Biomes.Add("\"minecraft:" + itemString + "\",");
                    else
                    if (CurrentItem.DataType == "GameEvent" && !GameEvent.Contains("\"minecraft:" + itemString + "\","))
                        GameEvent.Add("\"minecraft:" + itemString + "\",");
                }
                else
                {
                    if (CurrentItem.DataType == "ItemView" && Items.Contains("\"minecraft:" + itemString + "\","))
                        Items.Remove("\"minecraft:" + itemString + "\",");
                    if (CurrentItem.DataType == "EntityView" && Entities.Contains("\"minecraft:" + itemString + "\","))
                        Entities.Remove("\"minecraft:" + itemString + "\",");
                    if (CurrentItem.DataType == "Block&ItemView" && Blocks.Contains("\"minecraft:" + itemString + "\","))
                        Blocks.Remove("\"minecraft:" + itemString + "\",");
                    if (CurrentItem.DataType == "Biome" && Biomes.Contains("\"minecraft:" + itemString + "\","))
                        Biomes.Remove("\"minecraft:" + itemString + "\",");
                    if (CurrentItem.DataType == "GameEvent" && GameEvent.Contains("\"minecraft:" + itemString + "\","))
                        GameEvent.Remove("\"minecraft:" + itemString + "\",");
                }
            }
        }

        #endregion

        #region Event
        [RelayCommand]
        /// <summary>
        /// 从剪切板导入标签数据
        /// </summary>
        private void ImportFromClipboard()
        {
            ObservableCollection<TagItemTemplate> items = TagItemList;
            TagViewModel context = this;
            ExternalDataImportManager.ImportTagDataHandler(Clipboard.GetText(),ref items,ref context,false);
        }

        [RelayCommand]
        /// <summary>
        /// 从文件导入标签数据
        /// </summary>
        private void ImportFromFile()
        {
            ObservableCollection<TagItemTemplate> items = TagItemList;
            TagViewModel context = this;
            OpenFileDialog dialog = new()
            {
                Filter = "Json文件|*.json;",
                AddExtension = true,
                DefaultExt = ".json",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Multiselect = false,
                RestoreDirectory = true,
                Title = "请选择一个标签文件"
            };
            if (dialog.ShowDialog().Value && File.Exists(dialog.FileName))
                ExternalDataImportManager.ImportTagDataHandler(dialog.FileName, ref items, ref context);
        }

        /// <summary>
        /// 过滤与搜索内容不相关的成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (SelectedTypeItem is null)
            {
                return;
            }
            TagItemTemplate currentItem = e.Item as TagItemTemplate;

            #region 执行搜索
            if (currentItem is not null)
            {
                bool needDisplay = e.Accepted = (currentItem.DataType is not null && currentItem.DataType.Contains(SelectedTypeItem.Text)) || SelectedTypeItem.Text == "All";

                if (needDisplay)
                {
                    if (SearchText.Trim().Length > 0 && currentItem.DisplayId is not null)
                    {
                        string currentItemIDAndName = currentItem.DisplayId;
                        e.Accepted = currentItemIDAndName.Contains(SearchText);
                    }
                    else
                        e.Accepted = true;
                }
            }
            #endregion
        }

        /// <summary>
        /// 载入类型过滤列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TagView_Loaded(object sender, RoutedEventArgs e)
        {
            #region 加载过滤类型
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Tag\Data\TypeFilter.ini"))
            {
                string[] Types = File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Tag\Data\TypeFilter.ini");
                for (int i = 0; i < Types.Length; i++)
                {
                    TypeItemSource.Add(new TextComboBoxItem() { Text = Types[i] });
                }
            }
            #endregion

            #region 启动异步载入标签成员任务
            
            SelectedVersion = VersionList[0];
            int ItemIDCount = _dataService.GetItemIDList().Count;
            List<string> BlockIDList = [.. context.BlockSet.SelectMany(item =>
            {
                if(VersionComparer.IsInRange(SelectedVersion.Text, item.Key))
                {
                    return item.Value;
                }
                return [];
            })];
            int EntityIDCount = _dataService.GetEntityIDList().Count;
            int BiomeIDCount = context.BiomeIDSet.Count;
            int GameEventValueCount = context.GameEventTagSet.Count;
            List<string> IDOrValueList = [.. context.ItemSet.SelectMany(item =>
            {
                if(VersionComparer.IsInRange(SelectedVersion.Text, item.Key))
                {
                    return item.Value;
                }
                return [];
            }),
                .. context.EntitySet.SelectMany(item =>
            {
                if(VersionComparer.IsInRange(SelectedVersion.Text, item.Key))
                {
                    return item.Value;
                }
                return [];
            }),
                .. context.BiomeIDSet.SelectMany(item =>
            {
                if(VersionComparer.IsInRange(SelectedVersion.Text, item.Key))
                {
                    return item.Value;
                }
                return [];
            }),
                .. context.GameEventTagSet.SelectMany(item =>
            {
                if(VersionComparer.IsInRange(SelectedVersion.Text, item.Key))
                {
                    return item.Value;
                }
                return [];
            })];

            Task.Run(async () =>
            {
                ParallelOptions parallelOptions = new();
                await Parallel.ForAsync(0, IDOrValueList.Count, parallelOptions, (i, cancellationToken) =>
                {
                    AddItemProgress.Report(new TagItemTemplate());
                    return new ValueTask();
                });

                #region 物品
                string currentPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";

                Dictionary<string, string> ItemIDAndNameMap = _dataService.GetItemIDAndNameGroupByVersionMap()
                .SelectMany(pair => pair.Value)
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value
                );

                List<string> ItemKeyList = [.. ItemIDAndNameMap.Select(item => item.Key)];
                ItemKeyList.Sort();

                Parallel.For(0, context.ItemSet.Count, i =>
                {
                    SetItemProgress.Report(new ValueTuple<int, string, string, string, string, bool>(i, currentPath + IDOrValueList[i], IDOrValueList[i], ItemIDAndNameMap[IDOrValueList[i]], BlockIDList.Contains(IDOrValueList[i]) ? "Block&ItemView" : "ItemView", false));
                });
                #endregion

                #region 实体
                Dictionary<string, string> EntityIDAndNameMap = _dataService.GetItemIDAndNameGroupByVersionMap()
                .SelectMany(pair => pair.Value)
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value
                );

                await Parallel.ForAsync(ItemIDCount, ItemIDCount + EntityIDCount,parallelOptions, (i,cancellationToken) =>
                {
                    SetItemProgress.Report(new ValueTuple<int, string, string, string, string, bool>(i, currentPath + IDOrValueList[i], IDOrValueList[i], EntityIDAndNameMap[IDOrValueList[i]], "EntityView", false));
                    return new ValueTask();
                });
                #endregion

                #region 生物群系
                Parallel.For(ItemIDCount + EntityIDCount, ItemIDCount + EntityIDCount + BiomeIDCount, i =>
                {
                    SetItemProgress.Report(new ValueTuple<int, string, string, string, string, bool>(i, null, IDOrValueList[i], "", "Biome", false));
                });
                #endregion

                #region 游戏事件
                Parallel.For(ItemIDCount + EntityIDCount + BiomeIDCount, ItemIDCount + EntityIDCount + BiomeIDCount + GameEventValueCount, (i) =>
                {
                    SetItemProgress.Report(new ValueTuple<int, string, string, string, string, bool>(i, null, IDOrValueList[i], "", "GameEvent", false));
                });
                #endregion

                InitSelectedTypeItemProgress.Report(true);
            });

            #endregion
        }

        /// <summary>
        /// 背包视图载入事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ListViewLoaded(object sender, RoutedEventArgs e)
        {
            TagZone = sender as ListView;
            Window parent = Window.GetWindow(TagZone);
            TagViewSource = parent.FindResource("TagItemSource") as CollectionViewSource;
            TagViewSource.Filter += CollectionViewSource_Filter;
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
            string result = (string.Join("\r\n",Items) + "\r\n" + string.Join("\r\n",Blocks) + "\r\n" + string.Join("\r\n",Entities) + "\r\n" + string.Join("\r\n",Biomes) + "\r\n" + string.Join("\r\n",GameEvent)).TrimEnd().TrimEnd(',');
            result = "{\r\n  \"replace\": " + Replace.ToString().ToLower() + ",\r\n\"values\": [\r\n" + result.Trim('\n').Trim('\r') + "\r\n]\r\n}";
            JToken resultToken = JToken.Parse(result);
            result = resultToken.ToString(Newtonsoft.Json.Formatting.Indented);

            SaveFileDialog saveFileDialog = new()
            {
                AddExtension = true,
                CheckPathExists = true,
                DefaultExt = "json",
                Filter = "JSON files (*.json;)|*.json;",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Title = "保存为JSON文件"
            };
            if (saveFileDialog.ShowDialog().Value)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(saveFileDialog.FileName));
                File.WriteAllText(saveFileDialog.FileName, result);
                Message.PushMessage("标签生成成功！", MessageBoxImage.Information);
                //OpenFolderThenSelectFiles.ExplorerFile(saveFileDialog.FileName);
            }
        }

        /// <summary>
        /// 左击选中成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ListBoxClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not Border)
            {
                LastSelectedIndex = TagZone.SelectedIndex;
                SelectedItem = null;
            }
            else
                if(SelectedItem is null && LastSelectedIndex > 0 && LastSelectedIndex < TagZone.Items.Count)
                SelectedItem = TagZone.Items[LastSelectedIndex] as TagItemTemplate;
            if (SelectedItem is not null)
                ReverseValue(SelectedItem);
        }

        /// <summary>
        /// 更新过滤类型
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => TagViewSource?.View?.Refresh();

        /// <summary>
        /// 更新搜索结果视图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => TagViewSource?.View?.Refresh();

        /// <summary>
        /// 全选
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            SolidColorBrush brush;
            if (SelectedAll)
            {
                brush = LightOrangeBrush;
            }
            else
            {
                brush = null;
            }
            if (TagViewSource is not null)
            {
                foreach (TagItemTemplate tagItemTemplate in TagViewSource.View)
                {
                    if (tagItemTemplate.DisplayId.Trim().Length > 0)
                    {
                        string itemString = tagItemTemplate.DisplayId;
                        tagItemTemplate.BeChecked = SelectedAll;
                        tagItemTemplate.Background = brush;

                        if (tagItemTemplate.BeChecked.Value)
                        {
                            if (tagItemTemplate.DataType == "ItemView" && !Items.Contains("\"minecraft:" + itemString + "\","))
                                Items.Add("\"minecraft:" + itemString + "\",");
                            else
                            if (tagItemTemplate.DataType == "EntityView" && !Entities.Contains("\"minecraft:" + itemString + "\","))
                                Entities.Add("\"minecraft:" + itemString + "\",");
                            else
                            if (tagItemTemplate.DataType == "Block&ItemView" && !Blocks.Contains("\"minecraft:" + itemString + "\","))
                                Blocks.Add("\"minecraft:" + itemString + "\",");
                            else
                            if (tagItemTemplate.DataType == "Biome" && !Biomes.Contains("\"minecraft:" + itemString + "\","))
                                Biomes.Add("\"minecraft:" + itemString + "\",");
                            else
                            if (tagItemTemplate.DataType == "GameEvent" && !GameEvent.Contains("\"minecraft:" + itemString + "\","))
                                GameEvent.Add("\"minecraft:" + itemString + "\",");
                        }
                        else
                        {
                            if (tagItemTemplate.DataType == "ItemView" && Items.Contains("\"minecraft:" + itemString + "\","))
                                Items.Remove("\"minecraft:" + itemString + "\",");
                            if (tagItemTemplate.DataType == "EntityView" && Entities.Contains("\"minecraft:" + itemString + "\","))
                                Entities.Remove("\"minecraft:" + itemString + "\",");
                            if (tagItemTemplate.DataType == "Block&ItemView" && Blocks.Contains("\"minecraft:" + itemString + "\","))
                                Blocks.Remove("\"minecraft:" + itemString + "\",");
                            if (tagItemTemplate.DataType == "Biome" && Biomes.Contains("\"minecraft:" + itemString + "\","))
                                Biomes.Remove("\"minecraft:" + itemString + "\",");
                            if (tagItemTemplate.DataType == "GameEvent" && GameEvent.Contains("\"minecraft:" + itemString + "\","))
                                GameEvent.Remove("\"minecraft:" + itemString + "\",");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 反选
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ReverseAll_Click(object sender, RoutedEventArgs e)
        {
            if (TagViewSource is not null)
            {
                foreach (TagItemTemplate tagItemTemplate in TagViewSource.View)
                {
                    if (tagItemTemplate.DisplayId.Trim().Length > 0)
                    {
                        string itemString = tagItemTemplate.DisplayId.Contains(' ') ? tagItemTemplate.DisplayId[..tagItemTemplate.DisplayId.IndexOf(' ')] : tagItemTemplate.DisplayId;
                        tagItemTemplate.BeChecked = !tagItemTemplate.BeChecked.Value;
                        if (tagItemTemplate.BeChecked.Value)
                            tagItemTemplate.Background = LightOrangeBrush;
                        else
                            tagItemTemplate.Background = null;
                        if (tagItemTemplate.BeChecked.Value)
                        {
                            if (tagItemTemplate.DataType == "ItemView" && !Items.Contains("\"minecraft:" + itemString + "\","))
                                Items.Add("\"minecraft:" + itemString + "\",");
                            else
                            if (tagItemTemplate.DataType == "EntityView" && !Entities.Contains("\"minecraft:" + itemString + "\","))
                                Entities.Add("\"minecraft:" + itemString + "\",");
                            else
                            if (tagItemTemplate.DataType == "Block&ItemView" && !Blocks.Contains("\"minecraft:" + itemString + "\","))
                                Blocks.Add("\"minecraft:" + itemString + "\",");
                            else
                            if (tagItemTemplate.DataType == "Biome" && !Biomes.Contains("\"minecraft:" + itemString + "\","))
                                Biomes.Add("\"minecraft:" + itemString + "\",");
                            else
                            if (tagItemTemplate.DataType == "GameEvent" && !GameEvent.Contains("\"minecraft:" + itemString + "\","))
                                GameEvent.Add("\"minecraft:" + itemString + "\",");
                        }
                        else
                        {
                            if (tagItemTemplate.DataType == "ItemView")
                                Items.Remove("\"minecraft:" + itemString + "\",");
                            else
                            if (tagItemTemplate.DataType == "EntityView")
                                Entities.Remove("\"minecraft:" + itemString + "\",");
                            else
                            if (tagItemTemplate.DataType == "Block&ItemView")
                                Blocks.Remove("\"minecraft:" + itemString + "\",");
                            else
                            if (tagItemTemplate.DataType == "Biome")
                                Biomes.Remove("\"minecraft:" + itemString + "\",");
                            else
                            if (tagItemTemplate.DataType == "GameEvent")
                                GameEvent.Remove("\"minecraft:" + itemString + "\",");
                        }
                    }
                }
            }
        }
        #endregion
    }
}