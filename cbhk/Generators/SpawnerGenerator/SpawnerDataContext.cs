using cbhk.CustomControls;
using cbhk.GeneralTools;
using cbhk.GeneralTools.MessageTip;
using cbhk.GenerateResultDisplayer;
using cbhk.Generators.SpawnerGenerator.Components;
using cbhk.WindowDictionaries;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace cbhk.Generators.SpawnerGenerator
{
    public class SpawnerDataContext: ObservableObject
    {
        #region 返回和运行等指令
        public RelayCommand<CommonWindow> Return { get; set; }
        public RelayCommand Run { get; set; }
        public RelayCommand SaveAll { get; set; }
        public RelayCommand AddSpawner { get; set; }
        public RelayCommand ClearSpawner { get; set; }
        public RelayCommand ImportFromFile { get; set; }
        public RelayCommand ImportFromClipboard { get; set; }
        #endregion

        #region 存储结果
        public StringBuilder Result { get; set; }
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
        public ObservableCollection<string> VersionSource { get; set; } = new() { "1.13+","1.12-" };
        #endregion

        #region 字段与引用
        /// <summary>
        /// 主页引用
        /// </summary>
        public Window home = null;
        //刷怪笼标签页集合
        public ObservableCollection<RichTabItems> SpawnerPages { get; set; } = new() { new RichTabItems() { Header = "刷怪笼",
                IsContentSaved = true,
                BorderThickness = new(4, 4, 4, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC6B23")),
                Foreground = new SolidColorBrush(Colors.White),
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                LeftBorderTexture = Application.Current.Resources["TabItemLeft"] as Brush,
                RightBorderTexture = Application.Current.Resources["TabItemRight"] as Brush,
                TopBorderTexture = Application.Current.Resources["TabItemTop"] as Brush,
                SelectedLeftBorderTexture = Application.Current.Resources["SelectedTabItemLeft"] as Brush,
                SelectedRightBorderTexture = Application.Current.Resources["SelectedTabItemRight"] as Brush,
                SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as Brush,} };

        //本生成器的图标路径
        string icon_path = "pack://application:,,,/cbhk;component/resources/common/images/spawnerIcons/IconSpawner.png";

        #region 已选中的刷怪笼
        private RichTabItems selectedItem = null;
        public RichTabItems SelectedItem
        {
            get => selectedItem;
            set => SetProperty(ref selectedItem,value);
        }
        #endregion

        #endregion

        public SpawnerDataContext()
        {
            #region 绑定指令
            Return = new RelayCommand<CommonWindow>(return_command);
            Run = new RelayCommand(run_command);
            SaveAll = new RelayCommand(SaveAllCommand);
            AddSpawner = new RelayCommand(AddSpawnerCommand);
            ClearSpawner = new RelayCommand(ClearSpawnerCommand);
            ImportFromClipboard = new RelayCommand(ImportFromClipboardCommand);
            ImportFromFile = new RelayCommand(ImportFromFileCommand);
            #endregion

            #region 初始化数据
            SpawnerPages[0].Content = new SpawnerPage() { FontWeight = FontWeights.Normal };
            #endregion
        }

        /// <summary>
        /// 添加刷怪笼
        /// </summary>
        private void AddSpawnerCommand()
        {
            RichTabItems richTabItems = new()
            {
                Header = "刷怪笼",
                Content = new SpawnerPage() { FontWeight = FontWeights.Normal },
                IsContentSaved = true,
                BorderThickness = new(4, 4, 4, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC6B23")),
                Foreground = new SolidColorBrush(Colors.White),
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                LeftBorderTexture = Application.Current.Resources["TabItemLeft"] as Brush,
                RightBorderTexture = Application.Current.Resources["TabItemRight"] as Brush,
                TopBorderTexture = Application.Current.Resources["TabItemTop"] as Brush,
                SelectedLeftBorderTexture = Application.Current.Resources["SelectedTabItemLeft"] as Brush,
                SelectedRightBorderTexture = Application.Current.Resources["SelectedTabItemRight"] as Brush,
                SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as Brush,
            };
            SpawnerPages.Add(richTabItems);
            if(SpawnerPages.Count == 1)
            SelectedItem = richTabItems;
        }

        /// <summary>
        /// 清空刷怪笼
        /// </summary>
        private void ClearSpawnerCommand()
        {
            SpawnerPages.Clear();
        }

        /// <summary>
        /// 从剪切板导入刷怪笼
        /// </summary>
        private void ImportFromClipboardCommand()
        {
            ObservableCollection<RichTabItems> items = SpawnerPages;
            ExternalDataImportManager.ImportSpawnerDataHandler(Clipboard.GetText(),ref items,false);
        }

        /// <summary>
        /// 从文件导入刷怪笼
        /// </summary>
        private void ImportFromFileCommand()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
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
                ObservableCollection<RichTabItems> items = SpawnerPages;
                ExternalDataImportManager.ImportSpawnerDataHandler(openFileDialog.FileName,ref items);
            }
        }

        /// <summary>
        /// 返回主页
        /// </summary>
        /// <param name="win"></param>
        private void return_command(CommonWindow win)
        {
            home.WindowState = WindowState.Normal;
            home.Show();
            home.ShowInTaskbar = true;
            home.Focus();
            win.Close();
        }

        /// <summary>
        /// 执行生成
        /// </summary>
        private void run_command()
        {
            Result = new();
            //await Task.Run(() =>
            //{
                foreach (RichTabItems item in SpawnerPages)
                {
                    SpawnerPage spawnerPage = item.Content as SpawnerPage;
                    SpawnerPageDataContext context = spawnerPage.DataContext as SpawnerPageDataContext;
                    context.ShowResult = false;
                    context.run_command();
                    Result.Append(context.Result + "\r\n");
                }
            //});

            #region 显示生成结果
            if (ShowResult)
            {
                Displayer display = Displayer.GetContentDisplayer();
                display.GeneratorResult(Result.ToString().Trim(','), "刷怪笼", icon_path);
                display.Show();
                display.Focus();
            }
            else
            {
                Clipboard.SetText(Result.ToString().Trim(','));
                Message.PushMessage("刷怪笼全部生成成功！", MessageBoxImage.Information);
            }
            #endregion
        }

        /// <summary>
        /// 保存所有刷怪笼
        /// </summary>
        private async void SaveAllCommand()
        {
            List<string> Results = [];
            await Task.Run(() =>
            {
                foreach (RichTabItems item in SpawnerPages)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SpawnerPage spawnerPage = item.Content as SpawnerPage;
                        SpawnerPageDataContext context = spawnerPage.DataContext as SpawnerPageDataContext;
                        context.ShowResult = false;
                        context.run_command();
                        Results.Add(context.Result);
                    });
                }

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
                    int index = 0;
                    foreach (string item in Results)
                    {
                        string entityID = "";
                        if (item.Contains('{'))
                        {
                            if (JObject.Parse(item).SelectToken("SpawnData.entity.id") is JToken id)
                                entityID = id.ToString().Replace("minecraft:", "");
                        }
                        File.WriteAllTextAsync(folderBrowserDialog.SelectedPath + "spawner" + entityID + index + ".command", item);
                        index++;
                    }
                }
            });
        }
    }
}
