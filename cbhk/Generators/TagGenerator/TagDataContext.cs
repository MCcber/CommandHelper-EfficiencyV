﻿using cbhk.CustomControls;
using cbhk.GeneralTools;
using cbhk.GeneralTools.MessageTip;
using cbhk.WindowDictionaries;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace cbhk.Generators.TagGenerator
{
    public partial class TagDataContext: ObservableObject
    {
        #region 替换
        private bool replace;
        public bool Replace
        {
            get => replace;
            set => SetProperty(ref replace,value);
        }
        #endregion

        #region 存储最终生成的列表
        public List<string> Blocks = [];
        public List<string> Items = [];
        public List<string> Entities = [];
        public List<string> GameEvent = [];
        public List<string> Biomes = [];
        #endregion

        #region 搜索内容
        private string searchText;
        public string SearchText
        {
            get => searchText;
            set
            {
                searchText = value;
                TagViewSource?.View.Refresh();
            }
        }
        #endregion

        #region 所有标签成员
        private ObservableCollection<TagItemTemplate> tagItems = [];
        public ObservableCollection<TagItemTemplate> TagItems
        {
            get => tagItems;
            set => SetProperty(ref tagItems,value);
        }
        #endregion

        #region 字段
        ListView TagZone = null;
        SolidColorBrush LightOrangeBrush = new((Color)ColorConverter.ConvertFromString("#F0D08C"));
        #endregion

        #region 当前选中的值成员
        private TagItemTemplate selectedItem = null;
        public TagItemTemplate SelectedItem
        {
            get => selectedItem;
            set => SetProperty(ref selectedItem,value);
        }

        private int LastSelectedIndex = 0;
        #endregion

        #region 当前选中的类型成员
        private TextComboBoxItem selectedTypeItem = null;
        public TextComboBoxItem SelectedTypeItem
        {
            get => selectedTypeItem;
            set
            {
                selectedTypeItem = value;
                OnPropertyChanged();
                TypeSelectionChanged();
            }
        }
        #endregion

        #region 全选或反选
        private bool selectedAll = false;
        public bool SelectedAll
        {
            get => selectedAll;
            set
            {
                selectedAll = value;
                SetProperty(ref selectedAll,value);
                SolidColorBrush brush;
                if (selectedAll)
                    brush = LightOrangeBrush;
                else
                    brush = null;
                if (TagViewSource != null)
                {
                    foreach (TagItemTemplate tagItemTemplate in TagViewSource.View)
                    {
                        if (tagItemTemplate.DisplayId.Trim().Length > 0)
                        {
                            string itemString = tagItemTemplate.DisplayId;
                            tagItemTemplate.BeChecked = selectedAll;
                            tagItemTemplate.Background = brush;

                            if (tagItemTemplate.BeChecked.Value)
                            {
                                if (tagItemTemplate.DataType == "Item" && !Items.Contains("\"minecraft:" + itemString + "\","))
                                    Items.Add("\"minecraft:" + itemString + "\",");
                                else
                                if (tagItemTemplate.DataType == "Entity" && !Entities.Contains("\"minecraft:" + itemString + "\","))
                                    Entities.Add("\"minecraft:" + itemString + "\",");
                                else
                                if (tagItemTemplate.DataType == "Block&Item" && !Blocks.Contains("\"minecraft:" + itemString + "\","))
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
                                if (tagItemTemplate.DataType == "Item" && Items.Contains("\"minecraft:" + itemString + "\","))
                                    Items.Remove("\"minecraft:" + itemString + "\",");
                                if (tagItemTemplate.DataType == "Entity" && Entities.Contains("\"minecraft:" + itemString + "\","))
                                    Entities.Remove("\"minecraft:" + itemString + "\",");
                                if (tagItemTemplate.DataType == "Block&Item" && Blocks.Contains("\"minecraft:" + itemString + "\","))
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
        }
        private bool reverseAll = false;
        public bool ReverseAll
        {
            get => reverseAll;
            set
            {
                reverseAll = value;
                SetProperty(ref reverseAll,value);
                if (TagViewSource != null)
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
                                if (tagItemTemplate.DataType == "Item" && !Items.Contains("\"minecraft:" + itemString + "\","))
                                    Items.Add("\"minecraft:" + itemString + "\",");
                                else
                                if (tagItemTemplate.DataType == "Entity" && !Entities.Contains("\"minecraft:" + itemString + "\","))
                                    Entities.Add("\"minecraft:" + itemString + "\",");
                                else
                                if (tagItemTemplate.DataType == "Block&Item" && !Blocks.Contains("\"minecraft:" + itemString + "\","))
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
                                if (tagItemTemplate.DataType == "Item")
                                    Items.Remove("\"minecraft:" + itemString + "\",");
                                else
                                if (tagItemTemplate.DataType == "Entity")
                                    Entities.Remove("\"minecraft:" + itemString + "\",");
                                else
                                if (tagItemTemplate.DataType == "Block&Item")
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
        }
        #endregion

        /// <summary>
        /// 主页引用
        /// </summary>
        public Window home = null;

        /// <summary>
        /// 对象数据源
        /// </summary>
        CollectionViewSource TagViewSource = null;
        //标签生成器的过滤类型数据源
        public ObservableCollection<TextComboBoxItem> TypeItemSource = [];

        public TagDataContext()
        {
            #region 异步载入标签成员
            Task.Run(async () =>
            {
                DataCommunicator dataCommunicator = DataCommunicator.GetDataCommunicator();
                #region 物品
                DataTable ItemTable = await dataCommunicator.GetData("SELECT * FROM Items");
                string currentPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
                Dictionary<string, TagItemTemplate> ItemData = [];
                foreach (DataRow item in ItemTable.Rows)
                {
                    string id = item["id"].ToString();
                    string name = Regex.Unescape(item["name"].ToString());
                    Uri uri = null;
                    if (File.Exists(currentPath + id + ".png"))
                        uri = new(currentPath + id + ".png", UriKind.Absolute);
                    else
                    if (File.Exists(currentPath + id + "_spawn_egg.png"))
                        uri = new(currentPath + id + "_spawn_egg.png", UriKind.Absolute);
                    TagItemTemplate tagItemTemplate = new()
                    {
                        Icon = uri,
                        DisplayId = id,
                        DisplayName = name,
                        DataType = "Item",
                        BeChecked = false
                    };
                    ItemData.Add(id, tagItemTemplate);
                    TagItems.Add(tagItemTemplate);
                }
                #endregion
                #region 方块
                DataTable BlockTable = await dataCommunicator.GetData("SELECT * FROM Blocks");
                foreach (DataRow item in BlockTable.Rows)
                {
                    string id = item["id"].ToString();
                    string name = Regex.Unescape(item["name"].ToString());
                    if (!ItemData.TryGetValue(id, out TagItemTemplate value))
                    {
                        Uri uri = null;
                        if (File.Exists(currentPath + id + ".png"))
                            uri = new(currentPath + id + ".png", UriKind.Absolute);
                        else
                        if (File.Exists(currentPath + id + "_spawn_egg.png"))
                            uri = new(currentPath + id + "_spawn_egg.png", UriKind.Absolute);
                        TagItems.Add(new TagItemTemplate()
                        {
                            Icon = uri,
                            DisplayId = id,
                            DisplayName = name,
                            DataType = "Block&Item",
                            BeChecked = false
                        });
                    }
                    else
                        value.DataType = "Block&Item";
                }
                #endregion
                #region 实体
                DataTable EntityTable = await dataCommunicator.GetData("SELECT * FROM Entities");
                foreach (DataRow item in EntityTable.Rows)
                {
                    string id = item["id"].ToString();
                    string name = Regex.Unescape(item["name"].ToString());
                    Uri uri = null;
                    if (File.Exists(currentPath + id + ".png"))
                        uri = new(currentPath + id + ".png", UriKind.Absolute);
                    else
                    if (File.Exists(currentPath + id + "_spawn_egg.png"))
                        uri = new(currentPath + id + "_spawn_egg.png", UriKind.Absolute);
                    TagItems.Add(new TagItemTemplate()
                    {
                        Icon = uri,
                        DisplayId = id,
                        DisplayName = name,
                        DataType = "Entity",
                        BeChecked = false
                    });
                }
                #endregion
                #region 生物群系
                DataTable BiomeIdTable = await dataCommunicator.GetData("SELECT * FROM BiomeIds");
                foreach (DataRow item in BiomeIdTable.Rows)
                {
                    string id = item["id"].ToString();
                    TagItems.Add(new TagItemTemplate()
                    {
                        DisplayId = id,
                        DataType = "Biome",
                        BeChecked = false
                    });
                }
                #endregion
                #region 游戏事件
                DataTable GameEventTagsTable = await dataCommunicator.GetData("SELECT * FROM GameEventTags");
                foreach (DataRow item in GameEventTagsTable.Rows)
                {
                    string id = item["value"].ToString();
                    TagItems.Add(new TagItemTemplate()
                    {
                        DisplayId = id,
                        DataType = "GameEvent",
                        BeChecked = false
                    });
                }
                #endregion
            });
            #endregion
        }

        [RelayCommand]
        /// <summary>
        /// 从剪切板导入标签数据
        /// </summary>
        private void ImportFromClipboard()
        {
            ObservableCollection<TagItemTemplate> items = TagItems;
            TagDataContext context = this;
            ExternalDataImportManager.ImportTagDataHandler(Clipboard.GetText(),ref items,ref context,false);
        }

        [RelayCommand]
        /// <summary>
        /// 从文件导入标签数据
        /// </summary>
        private void ImportFromFile()
        {
            ObservableCollection<TagItemTemplate> items = TagItems;
            TagDataContext context = this;
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
            #region 执行过滤
            TagItemTemplate currentItem = e.Item as TagItemTemplate;
            bool needDisplay = e.Accepted = currentItem.DataType.Contains(SelectedTypeItem.Text) || SelectedTypeItem.Text == "All";
            #endregion
            #region 执行搜索
            if (needDisplay)
            {
                if (SearchText.Trim().Length > 0)
                {
                    string currentItemIDAndName = currentItem.DisplayId;
                    e.Accepted = currentItemIDAndName.Contains(SearchText);
                }
                else
                    e.Accepted = true;
            }
            #endregion
        }

        /// <summary>
        /// 载入类型过滤列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TypeFilterLoaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBoxs = sender as ComboBox;
            #region 加载过滤类型
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Tag\\data\\TypeFilter.ini"))
            {
                string[] Types = File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Tag\\data\\TypeFilter.ini");
                for (int i = 0; i < Types.Length; i++)
                {
                    TypeItemSource.Add(new TextComboBoxItem() { Text = Types[i] });
                }
            }
            comboBoxs.ItemsSource = TypeItemSource;
            comboBoxs.SelectedIndex = 0;
            #endregion
        }

        /// <summary>
        /// 更新过滤类型
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TypeSelectionChanged()
        {
            TagViewSource?.View.Refresh();
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

        [RelayCommand]
        /// <summary>
        /// 执行生成
        /// </summary>
        private void Run()
        {
            string result = (string.Join("\r\n",Items) + "\r\n" + string.Join("\r\n",Blocks) + "\r\n" + string.Join("\r\n",Entities) + "\r\n" + string.Join("\r\n",Biomes) + "\r\n" + string.Join("\r\n",GameEvent)).TrimEnd().TrimEnd(',');
            result = "{\r\n  \"replace\": " + Replace.ToString().ToLower() + ",\r\n\"values\": [\r\n" + result.Trim('\n').Trim('\r') + "\r\n]\r\n}";
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
                if(SelectedItem == null && LastSelectedIndex > 0 && LastSelectedIndex < TagZone.Items.Count)
                SelectedItem = TagZone.Items[LastSelectedIndex] as TagItemTemplate;
            if (SelectedItem != null)
                ReverseValue(SelectedItem);
        }

        /// <summary>
        /// 背包视图载入事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ListViewLoaded(object sender, RoutedEventArgs e)
        {
            #region 获取数据源引用，订阅过滤事件
            TagZone = sender as ListView;
            Window parent = Window.GetWindow(TagZone);
            TagViewSource = parent.FindResource("TagItemSource") as CollectionViewSource;
            TagViewSource.Filter += CollectionViewSource_Filter;
            #endregion
        }

        /// <summary>
        /// 反转目标成员的值
        /// </summary>
        /// <param name="CurrentItem"></param>
        private void ReverseValue(TagItemTemplate CurrentItem)
        {
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
                    if (CurrentItem.DataType == "Item" && !Items.Contains("\"minecraft:" + itemString + "\","))
                        Items.Add("\"minecraft:" + itemString + "\",");
                    else
                    if (CurrentItem.DataType == "Entity" && !Entities.Contains("\"minecraft:" + itemString + "\","))
                        Entities.Add("\"minecraft:" + itemString + "\",");
                    else
                    if (CurrentItem.DataType == "Block&Item" && !Blocks.Contains("\"minecraft:" + itemString + "\","))
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
                    if (CurrentItem.DataType == "Item" && Items.Contains("\"minecraft:" + itemString + "\","))
                        Items.Remove("\"minecraft:" + itemString + "\",");
                    if (CurrentItem.DataType == "Entity" && Entities.Contains("\"minecraft:" + itemString + "\","))
                        Entities.Remove("\"minecraft:" + itemString + "\",");
                    if (CurrentItem.DataType == "Block&Item" && Blocks.Contains("\"minecraft:" + itemString + "\","))
                        Blocks.Remove("\"minecraft:" + itemString + "\",");
                    if (CurrentItem.DataType == "Biome" && Biomes.Contains("\"minecraft:" + itemString + "\","))
                        Biomes.Remove("\"minecraft:" + itemString + "\",");
                    if (CurrentItem.DataType == "GameEvent" && GameEvent.Contains("\"minecraft:" + itemString + "\","))
                        GameEvent.Remove("\"minecraft:" + itemString + "\",");
                }
            }
        }
    }

    /// <summary>
    /// 载入成员的属性模板
    /// </summary
    public class TagItemTemplate:ObservableObject
    {
        private SolidColorBrush background = null;
        public SolidColorBrush Background
        {
            get => background;
            set => SetProperty(ref background, value);
        }

        public Uri Icon { get; set; }
        public string DataType { get; set; }
        public string DisplayId { get; set; }
        public string DisplayName { get; set; }

        private bool? beChecked = false;
        public bool? BeChecked
        {
            get => beChecked;
            set => SetProperty(ref beChecked,value);
        }
    }
}