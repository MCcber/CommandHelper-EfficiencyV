using CBHK.CustomControl;
using CBHK.GeneralTool;
using CBHK.GeneralTool.MessageTip;
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
    public partial class SpawnerPageViewModel:ObservableObject
    {
        #region 存储结果
        public string Result { get; set; }
        #endregion

        #region 显示结果
        private bool showResult = false;
        public bool ShowResult
        {
            get => showResult;
            set => SetProperty(ref showResult, value);
        }
        #endregion

        #region 选中版本以及版本数据源
        private TextComboBoxItem selectedVersion;
        public TextComboBoxItem SelectedVersion
        {
            get => selectedVersion;
            set
            {
                SetProperty(ref selectedVersion, value);
                CurrentMinVersion = int.Parse(selectedVersion.Text.Replace(".", "").Replace("+", "").Split('-')[0]);
            }
        }

        private ObservableCollection<TextComboBoxItem> versionSource = [];
        public ObservableCollection<TextComboBoxItem> VersionSource
        {
            get => versionSource;
            set => SetProperty(ref versionSource, value);
        }

        private int CurrentMinVersion = 1205;
        #endregion

        #region 字段与引用
        /// <summary>
        /// 控件面板
        /// </summary>
        public Grid componentsGrid = null;

        //潜在实体数据源
        public ObservableCollection<SpawnPotential> SpawnPotentials { get; set; } = [];

        /// <summary>
        /// 存储外部数据
        /// </summary>
        public JObject ExternalSpawnerData { get; set; }
        /// <summary>
        /// 导入模式
        /// </summary>
        public bool ImportMode { get; set; } = false;

        //本生成器的图标路径
        string icon_path = "pack://application:,,,/CBHK;component/Resource/Common/Image/SpawnerIcon/IconSpawner.png";
        private IContainerProvider _container;
        #endregion

        public SpawnerPageViewModel(IContainerProvider container)
        {
            _container = container;
        }

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
            componentsGrid = sender as Grid;

            if (ImportMode)
            {
                foreach (FrameworkElement item in componentsGrid.Children)
                {
                    if (item is Slider slider)
                    {
                        JToken currentObj = ExternalSpawnerData.SelectToken(slider.Uid);
                        if (currentObj != null && currentObj.ToString() != "0")
                            slider.Value = short.Parse(currentObj.ToString());
                    }
                    if (item is RadiusToggleButtons radiusToggleButtons)
                    {
                        JToken currentObj = ExternalSpawnerData.SelectToken(radiusToggleButtons.Uid);
                        radiusToggleButtons.IsChecked = currentObj is null;
                    }
                }
            }
        }

        [RelayCommand]
        /// <summary>
        /// 添加潜在实体
        /// </summary>
        public void AddSpawnPotential(FrameworkElement ele) => SpawnPotentials.Add(new SpawnPotential());

        [RelayCommand]
        /// <summary>
        /// 清空潜在实体
        /// </summary>
        private void ClearSpawnPotential(FrameworkElement ele) => SpawnPotentials.Clear();

        [RelayCommand]
        /// <summary>
        /// 执行生成
        /// </summary>
        public void Run()
        {
            Result = "";
            foreach (FrameworkElement item in componentsGrid.Children)
            {
                if (item is Slider slider)
                {
                    RadiusToggleButtons radiusToggleButtons = null;
                    foreach (FrameworkElement ele in componentsGrid.Children)
                    {
                        if (Grid.GetRow(ele) == Grid.GetRow(slider) && Grid.GetColumn(ele) == (Grid.GetColumn(slider) + 1) && ele is RadiusToggleButtons)
                        {
                            radiusToggleButtons = ele as RadiusToggleButtons;
                            break;
                        }
                    }
                    if (!radiusToggleButtons.IsChecked.Value)
                        Result += slider.Uid + ":" + slider.Value + ",";
                }
                if (item is ReferenceEntity reference && reference.Tag != null)
                    Result += reference.Uid + ":{entity:" + reference.Tag.ToString() + "},";
            }
            string SpawnPotentialsData = "";
            if (SpawnPotentials.Count > 0)
                SpawnPotentialsData = "SpawnPotentials:[" + string.Join(",", SpawnPotentials.Select(item => item.Result)) + "],";
            Result += SpawnPotentialsData;

            if (CurrentMinVersion >= 1130)
                Result = "/setblock ~ ~ ~ minecraft:spawner{" + Result.Trim(',') + "}";
            else
                Result = "/setblock ~ ~ ~ minecraft:mob_spawner 0 replace {" + Result.Trim(',') + "}";

            #region 显示生成结果
            if (ShowResult)
            {
                DisplayerView displayer = _container.Resolve<DisplayerView>();
                if (displayer is not null && displayer.DataContext is DisplayerViewModel displayerViewModel)
                {
                    displayer.Show();
                    displayerViewModel.GeneratorResult(Result.Trim(','), "刷怪笼", icon_path);
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
                if (JObject.Parse(data).SelectToken("SpawnData.entity.id") is JToken id)
                    entityID = id.ToString().Replace("minecraft:", "");
                File.WriteAllTextAsync(openFolderDialog.FolderName + entityID + "SpawnerPageView" + ".command", Result);
            }
        }
    }
}