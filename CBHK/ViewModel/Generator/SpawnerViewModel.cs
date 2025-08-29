using CBHK.CustomControl;
using CBHK.Utility.Common;
using CBHK.Utility.MessageTip;
using CBHK.View;
using CBHK.View.Component.Spawner;
using CBHK.ViewModel.Component.Spawner;
using CBHK.WindowDictionaries;
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

namespace CBHK.ViewModel.Generator
{
    public partial class SpawnerViewModel: ObservableObject
    {
        #region Field
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
        public ObservableCollection<RichTabItems> _spawnerPageList =
        [
            new RichTabItems()
            {
                Header = "刷怪笼",
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
                SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as Brush
            }
        ];

        /// <summary>
        /// 已选中的刷怪笼
        /// </summary>
        [ObservableProperty]
        private RichTabItems _selectedItem = null;

        /// <summary>
        /// 显示结果
        /// </summary>
        [ObservableProperty]
        private bool _showResult = false;

        /// <summary>
        /// 选中版本以及版本数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<TextComboBoxItem> _versionSource = [
            new TextComboBoxItem() { Text = "1.20.5" },
            new TextComboBoxItem() { Text = "1.12.0" }
        ];
        #endregion

        #region Method
        public SpawnerViewModel(IContainerProvider container, MainView mainView)
        {
            container = container;
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
            RichTabItems richTabItems = new()
            {
                Header = "刷怪笼",
                Content = container.Resolve<SpawnerPageView>(),
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
            SpawnerPageList.Add(richTabItems);
            if(SpawnerPageList.Count == 1)
            SelectedItem = richTabItems;
        }

        [RelayCommand]
        /// <summary>
        /// 清空刷怪笼
        /// </summary>
        private void ClearSpawner()
        {
            SpawnerPageList.Clear();
        }

        [RelayCommand]
        /// <summary>
        /// 从剪切板导入刷怪笼
        /// </summary>
        private void ImportFromClipboard()
        {
            ObservableCollection<RichTabItems> items = SpawnerPageList;
            ExternalDataImportManager.ImportSpawnerDataHandler(Clipboard.GetText(),ref items,false);
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
                ObservableCollection<RichTabItems> items = SpawnerPageList;
                ExternalDataImportManager.ImportSpawnerDataHandler(openFileDialog.FileName,ref items);
            }
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
            Result = new();
            foreach (RichTabItems item in SpawnerPageList)
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
                Message.PushMessage("刷怪笼全部生成成功！", MessageBoxImage.Information);
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
                foreach (RichTabItems item in SpawnerPageList)
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
    }
}