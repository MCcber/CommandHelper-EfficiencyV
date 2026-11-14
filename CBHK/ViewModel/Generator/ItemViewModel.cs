using CBHK.CustomControl.Container;
using CBHK.Model.Data;
using CBHK.Utility.Common;
using CBHK.Utility.Visual.MessageTip;
using CBHK.View;
using CBHK.View.Component.Item;
using CBHK.ViewModel.Component.Item;
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
    public partial class ItemViewModel(IContainerProvider container, MainView mainView) : ObservableObject
    {
        #region Field
        private MessagePopup messagePopup = new();
        /// <summary>
        /// 主页引用
        /// </summary>
        private Window home = mainView;
        private IContainerProvider container = container;
        /// <summary>
        /// 本生成器的图标路径
        /// </summary>
        string iconPath = "pack://application:,,,/CBHK;component/Resource/Common/Image/SpawnerIcon/IconItem.png";
        #endregion

        #region Property
        /// <summary>
        /// 显示结果
        /// </summary>
        [ObservableProperty]
        private bool _showGeneratorResult = false;

        /// <summary>
        /// 当前选中的物品页
        /// </summary>
        [ObservableProperty]
        private VectorRichTabItem selectedItemPage = null;

        /// <summary>
        /// 物品页数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<VectorRichTabItem> _itemPageList =
        [
            new VectorRichTabItem()
            {
                Title = "物品",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                Style = Application.Current.Resources["VectorRichTabItemStyle"] as Style,
                FontWeight = FontWeights.Normal
            }
        ];
        #endregion

        #region Event
        public void Item_Loaded(object sender,RoutedEventArgs e)
        {
            ItemPageList[0].Content = container.Resolve<ItemPageView>();
        }

        [RelayCommand]
        /// <summary>
        /// 添加物品
        /// </summary>
        private void AddItem()
        {
            VectorRichTabItem richTabItems = new()
            {
                Title = "物品",
                FontWeight = FontWeights.Normal,
                Style = Application.Current.Resources["VectorRichTabItemStyle"] as Style
            };
            ItemPageView itemPage = new() 
            { 
                FontWeight = FontWeights.Normal
            };
            richTabItems.Content = itemPage;
            ItemPageList.Add(richTabItems);
            if (ItemPageList.Count == 1)
            {
                TabControl tabControl = richTabItems.FindParent<TabControl>();
                tabControl.SelectedIndex = 0;
            }
        }

        [RelayCommand]
        /// <summary>
        /// 清空物品
        /// </summary>
        private void ClearItem() => ItemPageList.Clear();

        [RelayCommand]
        /// <summary>
        /// 从文件导入
        /// </summary>
        private void ImportItemFromFile()
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
        /// 从剪切板导入
        /// </summary>
        private void ImportItemFromClipboard()
        {
            ImportIntoMcDocPage(Clipboard.GetText(), false);
        }

        /// <summary>
        /// 导入只灌 mcdoc 页的 JsonText（命令里的 NBT 原样进去，引擎自己同步树）；
        /// 不再造旧的物品页面。失败提示沿用窗口原来的样式。
        /// </summary>
        private void ImportIntoMcDocPage(string textOrPath, bool isPath)
        {
            var page = (SelectedItemPage?.Content as FrameworkElement)?.DataContext as ItemPageViewModel;
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
                SubMessage = "物品生成器",
                SubMessageBrush = Brushes.DarkGray,
                Icon = new BitmapImage(new Uri(iconPath))
            });
        }

        [RelayCommand]
        /// <summary>
        /// 保存所有物品
        /// </summary>
        private async Task SaveAll()
        {
            List<string> Result = [];
            List<string> FileNameList = [];

            foreach (var itemPage in ItemPageList)
            {
                await itemPage.Dispatcher.InvokeAsync(async () =>
                {
                    ItemPageView itemPages = itemPage.Content as ItemPageView;
                    ItemPageViewModel context = itemPages.DataContext as ItemPageViewModel;
                    string result = await context.Run(false);
                    string nbt = "";
                    if (result.Contains('{'))
                    {
                        nbt = result[result.IndexOf('{')..(result.IndexOf('}') + 1)];
                        //补齐缺失双引号对的key
                        nbt = Regex.Replace(nbt, @"([\{\[,])([\s+]?\w+[\s+]?):", "$1\"$2\":");
                        //清除数值型数据的单位
                        nbt = Regex.Replace(nbt, @"(\d+[\,\]\}]?)([a-zA-Z])", "$1").Replace("I;", "");
                    }
                    JObject resultJSON = JObject.Parse(nbt);
                    string entityIDPath = "";
                    if (result.StartsWith("give"))
                        entityIDPath = "EntityTag.CustomName";
                    else
                        if (nbt.Length > 0)
                        entityIDPath = "CustomName";
                    JToken name = resultJSON.SelectToken(entityIDPath);
                    FileNameList.Add(context.SelectedItem.Text + (name is not null ? "-" + name.ToString() : ""));
                    Result.Add(result);
                });
            }
            OpenFolderDialog openFolderDialog = new()
            {
                Title = "请选择要保存的目录",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            if (openFolderDialog.ShowDialog().Value)
            {
                if (Directory.Exists(openFolderDialog.FolderName))
                {
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "resources\\saves\\itemView\\");
                    for (int i = 0; i < Result.Count; i++)
                    {
                        _ = File.WriteAllTextAsync(openFolderDialog.FolderName + FileNameList[i] + ".command", Result[i]);
                        _ = File.WriteAllTextAsync(AppDomain.CurrentDomain.BaseDirectory + "resources\\saves\\itemView\\" + FileNameList[i] + ".command", Result[i]);
                    }
                }
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
            home.ShowInTaskbar = true;
            home.Show();
            home.Focus();
            win.Close();
        }

        [RelayCommand]
        /// <summary>
        /// 全部生成
        /// </summary>
        private async Task Run()
        {
            StringBuilder Result = new();
            foreach (var itemPage in ItemPageList)
            {
                ItemPageViewModel context = (itemPage.Content as ItemPageView).DataContext as ItemPageViewModel;
                string currentResult = await context.Run(false);
                Result.Append(currentResult + "\r\n");
            }
            if (ShowGeneratorResult)
            {
                DisplayerView displayer = container.Resolve<DisplayerView>();
                if (displayer is not null && displayer.DataContext is DisplayerViewModel displayerViewModel)
                {
                    displayer.Show();
                    displayerViewModel.GeneratorResult(Result.ToString(), "物品", iconPath);
                }
            }
            else
            {
                Clipboard.SetText(Result.ToString());
                messagePopup.PushMessage(new GeneratorMessage()
                {
                    Message = "物品全部生成成功！数据已进入剪切板",
                    SubMessage = "物品生成器",
                    Icon = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\item_frame.png", UriKind.RelativeOrAbsolute))
                });
            }
        }
        #endregion
    }
}