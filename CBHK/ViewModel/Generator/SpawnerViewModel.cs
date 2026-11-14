using CBHK.CustomControl.Container;
using CBHK.CustomControl.VectorComboBox;
using CBHK.Model.Data;
using CBHK.Utility.Common;
using CBHK.Utility.Visual.MessageTip;
using CBHK.View;
using CBHK.View.Component.Spawner.Control;
using CBHK.ViewModel.Component.Spawner.Control;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace CBHK.ViewModel.Generator
{
    public partial class SpawnerViewModel: ObservableObject
    {
        #region Field
        private MessagePopup messagePopup = new();
        /// <summary>
        /// 存储结果
        /// </summary>
        public StringBuilder Result = new();
        /// <summary>
        /// 主页引用
        /// </summary>
        private Window home = null;
        /// <summary>
        /// 本生成器的图标路径
        /// </summary>
        string iconPath = "pack://application:,,,/CBHK;component/Resource/Common/Image/SpawnerIcon/IconSpawner.png";
        private IContainerProvider container;
        #endregion

        #region Property
        /// <summary>
        /// 刷怪笼标签页集合
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<VectorRichTabItem> _spawnerPageList =
        [
            new VectorRichTabItem()
            {
                Title = "刷怪笼",
                Foreground = new SolidColorBrush(Colors.White),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                Style = Application.Current.Resources["VectorRichTabItemStyle"] as Style
            }
        ];

        /// <summary>
        /// 已选中的刷怪笼
        /// </summary>
        [ObservableProperty]
        private VectorRichTabItem _selectedItem = null;

        /// <summary>
        /// 显示结果
        /// </summary>
        [ObservableProperty]
        private bool _showResult = false;

        /// <summary>
        /// 选中版本以及版本数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<VectorTextComboBoxItem> _versionSource = [
            new VectorTextComboBoxItem() { Text = "1.20.5" },
            new VectorTextComboBoxItem() { Text = "1.12.0" }
        ];
        #endregion

        #region Method
        public SpawnerViewModel(IContainerProvider Container, MainView mainView)
        {
            container = Container;
            SpawnerPageView spawnerPageView = container.Resolve<SpawnerPageView>();
            spawnerPageView.FontWeight = FontWeights.Normal;
            SpawnerPageList[0].Content = spawnerPageView;
            SelectedItem = SpawnerPageList[0];
            home = mainView;
        }
        #endregion

        #region Event
        [RelayCommand]
        /// <summary>
        /// 添加刷怪笼
        /// </summary>
        private void AddSpawner()
        {
            VectorRichTabItem richTabItem = new()
            {
                Title = "刷怪笼",
                Content = container.Resolve<SpawnerPageView>(),
                Foreground = new SolidColorBrush(Colors.White),
                Style = Application.Current.Resources["VectorRichTabItemStyle"] as Style
            };
            SpawnerPageList.Add(richTabItem);
            if(SpawnerPageList.Count == 1)
            SelectedItem = richTabItem;
        }

        [RelayCommand]
        /// <summary>
        /// 清空刷怪笼
        /// </summary>
        private void ClearSpawner() => SpawnerPageList.Clear();

        [RelayCommand]
        /// <summary>
        /// 从剪切板导入刷怪笼
        /// </summary>
        private void ImportFromClipboard()
        {
            ImportSpawnerData(Clipboard.GetText(), messagePopup, isPath: false);
        }

        [RelayCommand]
        /// <summary>
        /// 从文件导入刷怪笼
        /// </summary>
        private void ImportFromFile()
        {
            OpenFileDialog openFileDialog = new()
            {
                AddExtension = true,
                DefaultExt = ".command",
                Filter = "Command文件|*.command;",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Multiselect = false,
                RestoreDirectory = true,
                Title = "请选择一个刷怪笼文件"
            };
            if (openFileDialog.ShowDialog().Value)
            {
                ImportSpawnerData(openFileDialog.FileName, messagePopup);
            }
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
            Result = new();
            foreach (VectorRichTabItem item in SpawnerPageList)
            {
                SpawnerPageView spawnerPage = item.Content as SpawnerPageView;
                SpawnerPageViewModel context = spawnerPage.DataContext as SpawnerPageViewModel;
                context.ShowResult = false;
                context.Run();
                Result.Append(context.Result + "\r\n");
            }

            #region 显示生成结果
            if (ShowResult)
            {
                DisplayerView displayer = container.Resolve<DisplayerView>();
                if (displayer is not null && displayer.DataContext is DisplayerViewModel displayerViewModel)
                {
                    displayerViewModel.GeneratorResult(Result.ToString().Trim(','), "刷怪笼", iconPath);
                }
            }
            else
            {
                Clipboard.SetText(Result.ToString().Trim(','));
                messagePopup.PushMessage(new GeneratorMessage()
                {
                    Message = "全部生成成功！",
                    SubMessage = "刷怪笼生成器",
                    Icon = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\spawner.png", UriKind.RelativeOrAbsolute))
                });
            }
            #endregion
        }

        [RelayCommand]
        /// <summary>
        /// 保存所有刷怪笼
        /// </summary>
        private async Task SaveAll()
        {
            List<string> Results = [];
            await Task.Run(() =>
            {
                foreach (VectorRichTabItem item in SpawnerPageList)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SpawnerPageView spawnerPage = item.Content as SpawnerPageView;
                        SpawnerPageViewModel context = spawnerPage.DataContext as SpawnerPageViewModel;
                        context.ShowResult = false;
                        context.Run();
                        Results.Add(context.Result);
                    });
                }
                
                OpenFolderDialog openFolderDialog = new()
                {
                    Title = "请选择要保存的目录",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                    ShowHiddenItems = true,
                };
                if (openFolderDialog.ShowDialog().Value)
                {
                    int index = 0;
                    foreach (string item in Results)
                    {
                        string entityID = "";
                        if (item.Contains('{'))
                        {
                            if (JObject.Parse(item).SelectToken("SpawnData.entity.oldID") is JToken id)
                                entityID = id.ToString().Replace("minecraft:", "");
                        }
                        File.WriteAllTextAsync(openFolderDialog.FolderName + "spawner" + entityID + index + ".command", item);
                        index++;
                    }
                }
            });
        }
        #endregion

        #region 外部数据导入（并入本类，不再单独成文件）
        public void ImportSpawnerData(string filePathOrData, MessagePopup messagePopup, bool isPath = true)
        {
            string data = isPath ? File.ReadAllText(filePathOrData) : filePathOrData;

            #region 提取可用NBT数据
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
                //确定版本
                bool version1_12 = Regex.IsMatch(data, @"^/setblock (minecraft:)?mob_spawner");
                AddSpawnerData(nbtObj, version1_12 ? "1.12.0" : "1.13.0");
            }
            catch
            {
                messagePopup.PushMessage(new GeneratorMessage()
                {
                    Message = "导入失败！文件内容格式不合法",
                    MessageBrush = Brushes.Red,
                    SubMessage = "刷怪笼生成器",
                    SubMessageBrush = Brushes.DarkGray,
                    Icon = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\ImageSet\spawner.png", UriKind.Relative))
                });
            }
        }

        /// <summary>
        /// 添加刷怪笼
        /// </summary>
        /// <param name="nbtObj"></param>
        /// <param name="itemPageList"></param>
        private void AddSpawnerData(JObject nbtObj, string version)
        {
            SpawnerPageView spawnerPage = new() { FontWeight = FontWeights.Normal };
            VectorRichTabItem richTabItems = new()
            {
                Header = "刷怪笼",
                Content = spawnerPage,
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                BorderThickness = new(4, 4, 4, 0),
                Foreground = new SolidColorBrush(Colors.White)
            };
            SpawnerPageViewModel context = spawnerPage.DataContext as SpawnerPageViewModel;
            SpawnerPageList.Add(richTabItems);
            context.SelectedVersion = version == "1.12.0" ? context.VersionSource[1] : context.VersionSource[0];

            #region 开启导入模式，为外部数据赋值
            context.ExternalSpawnerData = nbtObj;
            context.ImportMode = true;
            #endregion

            #region 处理潜在实体数据
            if (nbtObj.SelectToken("SpawnPotentialList") is JArray spawnPotentials)
            {
                foreach (JObject spawnPotential in spawnPotentials.Cast<JObject>())
                {
                    context.AddSpawnPotential();
                    SpawnPotentialViewModel potential = context.SpawnPotentialList[^1];
                    if (spawnPotential.SelectToken("weight") is JToken weight)
                        potential.Weight = weight.ToString();

                    // 光照：只写 ViewModel 的属性（数字 / 区间 / 用默认值三态），可见性由绑定跟
                    if (spawnPotential.SelectToken("data.custom_spawn_rules.block_light_limit") is JArray blockRange)
                    {
                        potential.UseDefaultBlockLight = false;
                        potential.BlockLightUseSingleValue = false;
                        potential.BlockLightMin = blockRange[0].ToString();
                        potential.BlockLightMax = blockRange[1].ToString();
                    }
                    else if (spawnPotential.SelectToken("data.custom_spawn_rules.block_light_limit") is JToken blockSingle)
                    {
                        potential.UseDefaultBlockLight = false;
                        potential.BlockLightUseSingleValue = true;
                        potential.BlockLightValue = blockSingle.ToString();
                    }

                    if (spawnPotential.SelectToken("data.custom_spawn_rules.sky_light_limit") is JArray skyRange)
                    {
                        potential.UseDefaultSkyLight = false;
                        potential.SkyLightUseSingleValue = false;
                        potential.SkyLightMin = skyRange[0].ToString();
                        potential.SkyLightMax = skyRange[1].ToString();
                    }
                    else if (spawnPotential.SelectToken("data.custom_spawn_rules.sky_light_limit") is JToken skySingle)
                    {
                        potential.UseDefaultSkyLight = false;
                        potential.SkyLightUseSingleValue = true;
                        potential.SkyLightValue = skySingle.ToString();
                    }

                    // 实体：走实体槽 ViewModel 的唯一入口（文本与图标都由它自己算）
                    if (spawnPotential.SelectToken("data.entity") is JObject entity)
                        potential.EntitySlot.ApplyEntityData(entity.ToString());
                }
            }
            #endregion

            TabControl tabControl = richTabItems.FindParent<TabControl>();
            tabControl.SelectedIndex = SpawnerPageList.Count - 1;
        }
        #endregion
    }
}
