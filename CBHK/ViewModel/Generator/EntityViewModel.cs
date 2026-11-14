using CBHK.CustomControl.Container;
using CBHK.CustomControl.VectorComboBox;
using CBHK.Model.Data;
using CBHK.Utility.Common;
using CBHK.Utility.Visual.MessageTip;
using CBHK.View;
using CBHK.View.Component.Entity;
using CBHK.ViewModel.Component.Entity;
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CBHK.ViewModel.Generator
{
    public partial class EntityViewModel(IContainerProvider container, MainView mainView) : ObservableObject
    {
        #region Field
        private MessagePopup messagePopup = new();
        private IContainerProvider container = container;
        /// <summary>
        /// 主页引用
        /// </summary>
        private MainView home = mainView;
        //本生成器的图标路径
        string iconPath = "pack://application:,,,/CBHK;component/Resource/Common/Image/SpawnerIcon/IconEntity.png";
        #endregion

        #region Property
        /// <summary>
        /// 是否展示生成结果
        /// </summary>
        [ObservableProperty]
        private bool _showGeneratorResult = false;

        /// <summary>
        /// 实体标签页的数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<VectorRichTabItem> _entityPageList = [
            new VectorRichTabItem()
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                Style = Application.Current.Resources["VectorRichTabItemStyle"] as Style,
                Title = "实体",
                FontWeight = FontWeights.Normal
            } ];

        /// <summary>
        /// 已选中的实体页
        /// </summary>
        [ObservableProperty]
        private VectorRichTabItem selectedEntityPage;

        /// <summary>
        /// 版本数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<VectorTextComboBoxItem> _versionSource = [
            new VectorTextComboBoxItem() { Text = "1.20.5" },
            new VectorTextComboBoxItem() { Text = "1.20.2" },
            new VectorTextComboBoxItem() { Text = "1.20.1" },
            new VectorTextComboBoxItem() { Text = "1.20.0" },
            new VectorTextComboBoxItem() { Text = "1.19.0" },
            new VectorTextComboBoxItem() { Text = "1.17.0" },
            new VectorTextComboBoxItem() { Text = "1.16.2" },
            new VectorTextComboBoxItem() { Text = "1.16.0" },
            new VectorTextComboBoxItem() { Text = "1.15.0" },
            new VectorTextComboBoxItem() { Text = "1.14.0" },
            new VectorTextComboBoxItem() { Text = "1.13.0" },
            new VectorTextComboBoxItem() { Text = "1.12.0" }
        ];

        /// <summary>
        /// 能否关闭标签页
        /// </summary>
        [ObservableProperty]
        private bool _isCloseable = true;
        #endregion

        #region Method
        /// <summary>
        /// 生成并保存所有实体到本地文件
        /// </summary>
        /// <returns></returns>
        private async Task GeneratorAndSaveAllEntites()
        {
            List<string> Result = [];
            List<string> FileNameList = [];

            foreach (var entityPage in EntityPageList)
            {
                await entityPage.Dispatcher.InvokeAsync(() =>
                {
                    EntityPageView entityPages = entityPage.Content as EntityPageView;
                    EntityPageViewModel pageContext = entityPages.DataContext as EntityPageViewModel;
                    StringBuilder currentResult = pageContext.Create();
                    pageContext.CollectionData(currentResult);
                    pageContext.BuildResult(currentResult);
                    string nbt = "";
                    string result = currentResult.ToString();
                    if (result.Contains('{'))
                    {
                        nbt = result[result.IndexOf('{')..(result.IndexOf('}') + 1)];
                        //补齐缺失双引号对的key
                        nbt = Regex.Replace(nbt, @"([\{\[,])([\s+]?\w+[\s+]?):", "$1\"$2\":");
                        //清除数值型数据的单位
                        nbt = Regex.Replace(nbt, @"(\d+[\,\]\}]?)([a-zA-Z])", "$1").Replace("I;", "");
                    }
                    if (nbt.Length == 0) return;
                    JObject resultJSON = JObject.Parse(nbt);
                    string entityIDPath = "";
                    if (result.StartsWith("give"))
                        entityIDPath = "EntityTag.CustomName";
                    else
                        if (nbt.Length > 0)
                        entityIDPath = "CustomName";
                    JToken name = resultJSON.SelectToken(entityIDPath);
                    FileNameList.Add(pageContext.SelectedEntityId.Text + (name is not null ? "-" + name.ToString() : ""));
                    Result.Add(result);
                });
            }
            OpenFolderDialog openFolderDialog = new()
            {
                Title = "请选择要保存的目录",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                ShowHiddenItems = true
            };
            if (openFolderDialog.ShowDialog().Value)
            {
                for (int i = 0; i < Result.Count; i++)
                {
                    if (Directory.Exists(openFolderDialog.FolderName))
                        _ = File.WriteAllTextAsync(openFolderDialog.FolderName + FileNameList[i] + ".command", Result[i]);
                    _ = File.WriteAllTextAsync(AppDomain.CurrentDomain.BaseDirectory + "resources\\saves\\EntityView\\" + openFolderDialog.FolderName + FileNameList[i] + ".command", Result[i]);
                }
            }
        }
        #endregion

        #region Event
        /// <summary>
        /// 实体载入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void Entity_Loaded(object sender,RoutedEventArgs e)
        {
            #region 载入实体预设数据
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    EntityPageView entityPages = container.Resolve<EntityPageView>();
                    EntityPageList[0].Content = entityPages;
                });
            });
            #endregion
        }

        [RelayCommand]
        /// <summary>
        /// 清除不需要的数据
        /// </summary>
        private void ClearUnnecessaryData()
        {
            // 实体页已经是 mcdoc 树：原来的"切换实体时缓存过的 NBT 面板 / 标签页"不存在了，
            // 文档本身就是唯一状态，没有需要清理的陈旧缓存。保留入口（视图上还有这颗按钮）。
        }

        [RelayCommand]
        /// <summary>
        /// 从文件导入实体
        /// </summary>
        private void ImportEntityFromFile()
        {
            OpenFileDialog dialog = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                RestoreDirectory = true,
                DefaultExt = ".command",
                Multiselect = false,
                Title = "请选择一个Minecraft实体数据文件"
            };
            if (dialog.ShowDialog().Value && File.Exists(dialog.FileName))
            {
                ImportIntoMcDocPage(dialog.FileName, true);
            }
        }

        [RelayCommand]
        /// <summary>
        /// 从剪切板导入实体
        /// </summary>
        private void ImportEntityFromClipboard()
        {
            ImportIntoMcDocPage(Clipboard.GetText(), false);
        }

        /// <summary>导入只灌实体页的 JsonText，不再造旧页面；失败提示沿用窗口原来的样式。</summary>
        private void ImportIntoMcDocPage(string textOrPath, bool isPath)
        {
            var page = (SelectedEntityPage?.Content as FrameworkElement)?.DataContext as EntityPageViewModel;
            if (page is not null)
            {
                var failure = "";
                if (page.ImportFromCommand(textOrPath, isPath, message => failure = message)) return;
                PushImportFailure(failure);
                return;
            }
            PushImportFailure("页面还没就绪，稍后再试");
        }

        /// <summary>导入失败只给一句话原因（解析失败 / 注册表里没有这个 id / 页面没就绪）。</summary>
        private void PushImportFailure(string reason)
        {
            messagePopup.PushMessage(new GeneratorMessage()
            {
                Message = "导入失败！" + reason,
                MessageBrush = Brushes.Red,
                SubMessage = "实体生成器",
                SubMessageBrush = Brushes.DarkGray,
                Icon = new BitmapImage(new Uri(iconPath))
            });
        }

        [RelayCommand]
        /// <summary>
        /// 清空实体
        /// </summary>
        private void ClearEntity()
        {
            if (IsCloseable)
            {
                EntityPageList.Clear();
            }
        }

        [RelayCommand]
        /// <summary>
        /// 添加实体
        /// </summary>
        public void AddEntity()
        {
            VectorRichTabItem richTabItem = new()
            {
                Style = Application.Current.Resources["VectorRichTabItemStyle"] as Style,
                Title = "实体"
            };
            EntityPageView entityPages = new() { FontWeight = FontWeights.Normal };
            richTabItem.Content = entityPages;
            EntityPageList.Add(richTabItem);
            if (EntityPageList.Count == 1)
            {
                TabControl tabControl = richTabItem.FindParent<TabControl>();
                if(tabControl is not null)
                tabControl.SelectedIndex = 0;
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
        /// 全部生成
        /// </summary>
        private void Run()
        {
            StringBuilder Result = new();
            foreach (var entityPage in EntityPageList)
            {
                EntityPageView entityPages = entityPage.Content as EntityPageView;
                EntityPageViewModel pageContext = entityPages.DataContext as EntityPageViewModel;
                StringBuilder currentResult = pageContext.Create();
                pageContext.CollectionData(currentResult);
                pageContext.BuildResult(currentResult);
                string result = currentResult.ToString().Replace("\r","").Replace("\n","");
                Result.Append(result);
            }
            if (ShowGeneratorResult)
            {
                DisplayerView displayer = container.Resolve<DisplayerView>();
                if (displayer is not null && displayer.DataContext is DisplayerViewModel displayerViewModel)
                {
                    displayer.Show();
                    displayerViewModel.GeneratorResult(Result.ToString(), "实体", iconPath);
                }
            }
            else
            {
                Clipboard.SetText(Result.ToString());
                messagePopup.PushMessage(new GeneratorMessage()
                {
                    Message = "实体全部生成成功！数据已进入剪切板",
                    SubMessage = "实体生成器",
                    Icon = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\entity.png", UriKind.Relative))
                });
            }
        }

        [RelayCommand]
        /// <summary>
        /// 生成并保存所有的实体数据为文件
        /// </summary>
        private async Task SaveAll()
        {
            await GeneratorAndSaveAllEntites();
        }
        #endregion
    }
}