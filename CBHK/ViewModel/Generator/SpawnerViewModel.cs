using CBHK.CustomControl.Container;
using CBHK.CustomControl.VectorComboBox;
using CBHK.Model.Common;
using CBHK.Utility.Common;
using CBHK.Utility.MessageTip;
using CBHK.View;
using CBHK.View.Component.Spawner;
using CBHK.ViewModel.Component.Spawner;
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
                Header = "刷怪笼",
                Foreground = new SolidColorBrush(Colors.White),
                Style = Application.Current.Resources["RichTabItemStyle"] as Style
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
            VectorRichTabItem richTabItems = new()
            {
                Header = "刷怪笼",
                Content = container.Resolve<SpawnerPageView>(),
                Foreground = new SolidColorBrush(Colors.White),
                Style = Application.Current.Resources["RichTabItemStyle"] as Style
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
            ObservableCollection<VectorRichTabItem> items = SpawnerPageList;
            ExternalDataImportManager.ImportSpawnerDataHandler(Clipboard.GetText(),ref items,messagePopup,false);
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
                ObservableCollection<VectorRichTabItem> items = SpawnerPageList;
                ExternalDataImportManager.ImportSpawnerDataHandler(openFileDialog.FileName,ref items, messagePopup);
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
    }
}