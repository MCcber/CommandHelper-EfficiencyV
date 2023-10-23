using cbhk.CustomControls;
using cbhk.GeneralTools;
using cbhk.GeneralTools.MessageTip;
using cbhk.GenerateResultDisplayer;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace cbhk.Generators.SpawnerGenerator.Components
{
    public class SpawnerPageDataContext:ObservableObject
    {
        #region 保存与运行等指令
        public RelayCommand Run { get; set; }
        public RelayCommand Save { get; set; }
        public RelayCommand<FrameworkElement> AddSpawnPotential { get; set; }
        public RelayCommand<FrameworkElement> ClearSpawnPotential { get; set; }
        #endregion

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
        private string selectedVersion = "1.13+";
        public string SelectedVersion
        {
            get => selectedVersion;
            set => SetProperty(ref selectedVersion, value);
        }
        public ObservableCollection<string> VersionSource { get; set; } = new() { "1.13+", "1.12-" };
        #endregion

        #region 字段与引用
        /// <summary>
        /// 控件面板
        /// </summary>
        public Grid componentsGrid = null;

        //潜在实体数据源
        public ObservableCollection<SpawnPotential> SpawnPotentials { get; set; } = new();

        /// <summary>
        /// 存储外部数据
        /// </summary>
        public JObject ExternalSpawnerData { get; set; }
        /// <summary>
        /// 导入模式
        /// </summary>
        public bool ImportMode { get; set; } = false;

        //本生成器的图标路径
        string icon_path = "pack://application:,,,/cbhk;component/resources/common/images/spawnerIcons/IconSpawner.png";
        #endregion

        public SpawnerPageDataContext()
        {
            #region 绑定指令
            Run = new RelayCommand(run_command);
            Save = new RelayCommand(SaveCommand);
            AddSpawnPotential = new RelayCommand<FrameworkElement>(AddSpawnPotentialCommand);
            ClearSpawnPotential = new RelayCommand<FrameworkElement>(ClearSpawnPotentialCommand);
            #endregion
        }

        /// <summary>
        /// 添加潜在实体
        /// </summary>
        public void AddSpawnPotentialCommand(FrameworkElement ele)
        {
            SpawnPotentials.Add(new SpawnPotential());
        }

        /// <summary>
        /// 清空潜在实体
        /// </summary>
        private void ClearSpawnPotentialCommand(FrameworkElement ele)
        {
            SpawnPotentials.Clear();
        }

        /// <summary>
        /// 执行生成
        /// </summary>
        public void run_command()
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

            if (SelectedVersion == "1.13+")
                Result = "/setblock ~ ~ ~ minecraft:spawner{" + Result.Trim(',') + "}";
            else
                Result = "/setblock ~ ~ ~ minecraft:mob_spawner 0 replace {" + Result.Trim(',') + "}";

            #region 显示生成结果
            if (ShowResult)
            {
                Displayer display = Displayer.GetContentDisplayer();
                display.GeneratorResult(Result.Trim(','), "刷怪笼", icon_path);
                display.Show();
                display.Focus();
            }
            else
            {
                Clipboard.SetText(Result.Trim(','));
                Message.PushMessage("刷怪笼生成成功！", MessageBoxImage.Information);
            }
            #endregion
        }

        /// <summary>
        /// 执行保存
        /// </summary>
        private void SaveCommand()
        {
            ShowResult = false;
            run_command();
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new()
            {
                Description = "请选择要保存的目录",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                ShowHiddenFiles = true,
                ShowNewFolderButton = true,
                UseDescriptionForTitle = true
            };
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string entityID = "";
                string data = ExternalDataImportManager.GetSpawnerDataHandler(Result, false);
                if (JObject.Parse(data).SelectToken("SpawnData.entity.id") is JToken id)
                    entityID = id.ToString().Replace("minecraft:", "");
                File.WriteAllTextAsync(folderBrowserDialog.SelectedPath + entityID + "Spawner" + ".command", Result);
            }
        }

        /// <summary>
        /// 载入控件面板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ComponentsGridLoaded(object sender, RoutedEventArgs e)
        {
            componentsGrid = sender as Grid;
            
            if(ImportMode)
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
                        radiusToggleButtons.IsChecked = currentObj == null;
                    }
                }
            }
        }
    }
}
