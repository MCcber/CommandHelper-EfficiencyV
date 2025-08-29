using CBHK.CustomControl;
using CBHK.Utility.Common;
using CBHK.Utility.MessageTip;
using CBHK.View;
using CBHK.View.Component.Spawner;
using CBHK.ViewModel.Generator;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Prism.Ioc;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CBHK.ViewModel.Component.Spawner
{
    public partial class SpawnerPageViewModel(IContainerProvider container) : ObservableObject
    {
        #region Field
        private IContainerProvider container = container;
        private int CurrentMinVersion = 1205;
        /// <summary>
        /// 存储结果
        /// </summary>
        public string Result = "";
        /// <summary>
        /// 存储外部数据
        /// </summary>
        public JObject ExternalSpawnerData = null;
        /// <summary>
        /// 导入模式
        /// </summary>
        public bool ImportMode = false;
        /// <summary>
        /// 本生成器的图标路径
        /// </summary>
        string iconPath = "pack://application:,,,/CBHK;component/Resource/Common/Image/SpawnerIcon/IconSpawner.png";
        #endregion

        #region Property
        /// <summary>
        /// 显示结果
        /// </summary>
        [ObservableProperty]
        private bool _showResult = false;
        /// <summary>
        /// 选中版本以及版本数据源
        /// </summary>
        [ObservableProperty]
        private TextComboBoxItem _selectedVersion;
        [ObservableProperty]
        private ObservableCollection<TextComboBoxItem> _versionSource = [];
        /// <summary>
        /// 潜在实体数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<SpawnPotential> _spawnPotentialList = [];
        [ObservableProperty]
        private short _delay = 0;
        [ObservableProperty]
        private bool? _isDefaultDelay = true;

        [ObservableProperty]
        private short _maxNearbyEntities = 0;
        [ObservableProperty]
        private bool? _isDefaultMaxNearbyEntities = true;

        [ObservableProperty]
        private short _maxSpawnDelay = 0;
        [ObservableProperty]
        private bool? _isDefaultMaxSpawnDelay = true;

        [ObservableProperty]
        private short _minSpawnDelay = 0;
        [ObservableProperty]
        private bool? _isDefaultMinSpawnDelay = true;

        [ObservableProperty]
        private short _requiredPlayerRange = 0;
        [ObservableProperty]
        private bool? _isDefaultRequiredPlayerRange = true;

        [ObservableProperty]
        private short _spawnCount = 0;
        [ObservableProperty]
        private bool? _isDefaultSpawnCount = true;

        [ObservableProperty]
        private short _spawnRange = 0;
        [ObservableProperty]
        private bool? _isDefaultSpawnRange = true;

        [ObservableProperty]
        private object _referenceEntityTag = null;
        #endregion

        #region  Event
        /// <summary>
        /// 载入刷怪笼页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SpawnerPage_Loaded(object sender,RoutedEventArgs e)
        {
            SpawnerViewModel spawnerDataContext = Window.GetWindow(sender as SpawnerPageView).DataContext as SpawnerViewModel;
            VersionSource = spawnerDataContext.VersionSource;
        }

        /// <summary>
        /// 载入控件面板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ComponentsGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (ImportMode && ExternalSpawnerData is not null)
            {
                if(ExternalSpawnerData.SelectToken("Delay") is JToken delayToken)
                {
                    Delay = short.Parse(delayToken.ToString());
                }
                if (ExternalSpawnerData.SelectToken("MaxNearbyEntities") is JToken maxNearbyEntitiesToken)
                {
                    MaxNearbyEntities = short.Parse(maxNearbyEntitiesToken.ToString());
                }
                if (ExternalSpawnerData.SelectToken("MaxSpawnDelay") is JToken maxSpawnDelayToken)
                {
                    MaxSpawnDelay = short.Parse(maxSpawnDelayToken.ToString());
                }
                if (ExternalSpawnerData.SelectToken("MinSpawnDelay") is JToken minSpawnDelayToken)
                {
                    MinSpawnDelay = short.Parse(minSpawnDelayToken.ToString());
                }
                if (ExternalSpawnerData.SelectToken("RequiredPlayerRange") is JToken requiredPlayerRangeToken)
                {
                    RequiredPlayerRange = short.Parse(requiredPlayerRangeToken.ToString());
                }
                if (ExternalSpawnerData.SelectToken("SpawnCount") is JToken spawnCountToken)
                {
                    SpawnCount = short.Parse(spawnCountToken.ToString());
                }
                if (ExternalSpawnerData.SelectToken("SpawnRange") is JToken spawnRangeToken)
                {
                    SpawnRange = short.Parse(spawnRangeToken.ToString());
                }
                if (ExternalSpawnerData.SelectToken("SpawnData") is JToken spawnDataToken)
                {
                    ReferenceEntityTag = spawnDataToken.ToString();
                }
            }
        }

        public void VersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentMinVersion = int.Parse(SelectedVersion.Text.Replace(".", "").Replace("+", "").Split('-')[0]);
        }

        public void SliderValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UIElement element = sender as UIElement;
            switch (element.Uid)
            {
                case "Delay":
                    {
                        IsDefaultDelay = Delay == 0;
                        break;
                    }
                case "MaxNearbyEntities":
                    {
                        IsDefaultMaxNearbyEntities = MaxNearbyEntities == 0;
                        break;
                    }
                case "MaxSpawnDelay":
                    {
                        IsDefaultMaxSpawnDelay = MaxSpawnDelay == 0;
                        break;
                    }
                case "MinSpawnDelay":
                    {
                        IsDefaultMinSpawnDelay = MinSpawnDelay == 0;
                        break;
                    }
                case "RequiredPlayerRange":
                    {
                        IsDefaultRequiredPlayerRange = RequiredPlayerRange == 0;
                        break;
                    }
                case "SpawnCount":
                    {
                        IsDefaultSpawnCount = SpawnCount == 0;
                        break;
                    }
                case "SpawnRange":
                    {
                        IsDefaultSpawnRange = SpawnRange == 0;
                        break;
                    }
            }
        }

        [RelayCommand]
        /// <summary>
        /// 添加潜在实体
        /// </summary>
        public void AddSpawnPotential(FrameworkElement ele) => SpawnPotentialList.Add(new SpawnPotential());

        [RelayCommand]
        /// <summary>
        /// 清空潜在实体
        /// </summary>
        private void ClearSpawnPotential(FrameworkElement ele) => SpawnPotentialList.Clear();

        [RelayCommand]
        /// <summary>
        /// 执行生成
        /// </summary>
        public void Run()
        {
            Result = $"Delay:{Delay},MaxNearbyEntities:{MaxNearbyEntities},MaxSpawnDelay:{MaxSpawnDelay},RequiredPlayerRange:{RequiredPlayerRange},SpawnCount:{SpawnCount},SpawnRange:{SpawnRange}";
            Result += "SpawnData:{entity:" + (ReferenceEntityTag ?? "") + "},";

            string SpawnPotentialsData = "";
            if (SpawnPotentialList.Count > 0)
            {
                SpawnPotentialsData = "SpawnPotentialList:[" + string.Join(",", SpawnPotentialList.Select(item => item.Result)) + "],";
            }
            Result += SpawnPotentialsData;

            if (CurrentMinVersion >= 1130)
            {
                Result = "/setblock ~ ~ ~ minecraft:spawner{" + Result.Trim(',') + "}";
            }
            else
            {
                Result = "/setblock ~ ~ ~ minecraft:mob_spawner 0 replace {" + Result.Trim(',') + "}";
            }

            #region 显示生成结果
            if (ShowResult)
            {
                DisplayerView displayer = container.Resolve<DisplayerView>();
                if (displayer is not null && displayer.DataContext is DisplayerViewModel displayerViewModel)
                {
                    displayer.Show();
                    displayerViewModel.GeneratorResult(Result.Trim(','), "刷怪笼", iconPath);
                }
            }
            else
            {
                Clipboard.SetText(Result.Trim(','));
                Message.PushMessage("刷怪笼生成成功！", MessageBoxImage.Information);
            }
            #endregion
        }

        [RelayCommand]
        /// <summary>
        /// 执行保存
        /// </summary>
        private void Save()
        {
            ShowResult = false;
            Run();
            OpenFolderDialog openFolderDialog = new()
            {
                Title = "请选择要保存的目录",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                ShowHiddenItems = true
            };
            if (openFolderDialog.ShowDialog().Value)
            {
                string entityID = "";
                string data = ExternalDataImportManager.GetSpawnerDataHandler(Result, false);
                if (JObject.Parse(data).SelectToken("SpawnData.entity.oldID") is JToken id)
                    entityID = id.ToString().Replace("minecraft:", "");
                File.WriteAllTextAsync(openFolderDialog.FolderName + entityID + "SpawnerPageView" + ".command", Result);
            }
        }
        #endregion
    }
}