using CBHK.CustomControl;
using CBHK.Utility.Common;
using CBHK.Utility.MessageTip;
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

namespace CBHK.ViewModel.Generator
{
    public partial class ItemViewModel(IContainerProvider container, MainView mainView) : ObservableObject
    {
        #region Field
        /// <summary>
        /// 主页引用
        /// </summary>
        private Window home = mainView;
        private IContainerProvider container = container;
        /// <summary>
        /// 本生成器的图标路径
        /// </summary>
        string iconPath = "pack://application:,,,/CBHK;component/Resource/Common/Image/SpawnerIcon/IconItems.png";
        #endregion

        #region Property
        /// <summary>
        /// 显示结果
        /// </summary>
        [ObservableProperty]
        private bool _showGeneratorResult = false;

        /// <summary>
        /// 能否关闭标签页
        /// </summary>
        [ObservableProperty]
        private bool _isCloseable = true;

        /// <summary>
        /// 当前选中的物品页
        /// </summary>
        [ObservableProperty]
        private RichTabItems _selectedItemPage = null;

        /// <summary>
        /// 物品页数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<RichTabItems> _itemPageList =
        [
            new RichTabItems()
            {
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                Header = "物品",
                FontWeight = FontWeights.Normal,
                IsContentSaved = true,
                BorderThickness = new(4, 4, 4, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC6B23")),
                LeftBorderTexture = Application.Current.Resources["TabItemLeft"] as ImageBrush,
                RightBorderTexture = Application.Current.Resources["TabItemRight"] as ImageBrush,
                TopBorderTexture = Application.Current.Resources["TabItemTop"] as ImageBrush,
                SelectedLeftBorderTexture = Application.Current.Resources["SelectedTabItemLeft"] as ImageBrush,
                SelectedRightBorderTexture = Application.Current.Resources["SelectedTabItemRight"] as ImageBrush,
                SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as ImageBrush
            }
        ];


        /// <summary>
        /// 版本列表
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<TextComboBoxItem> _versionList = [
            new TextComboBoxItem() { Text = "1.20.4" }, 
            new TextComboBoxItem() { Text = "1.20.2" },
            new TextComboBoxItem() { Text = "1.20.0" },
            new TextComboBoxItem() { Text = "1.19.4" },
            new TextComboBoxItem() { Text = "1.19.3" },
            new TextComboBoxItem() { Text = "1.19.0" },
            new TextComboBoxItem() { Text = "1.16.2" },
            new TextComboBoxItem() { Text = "1.16.0" },
            new TextComboBoxItem() { Text = "1.15.0" },
            new TextComboBoxItem() { Text = "1.14.0" },
            new TextComboBoxItem() { Text = "1.13.1" },
            new TextComboBoxItem() { Text = "1.13.0" }, 
            new TextComboBoxItem() { Text = "1.12.0" }];

        /// <summary>
        /// 版本ID数据源
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<VersionID> _versionIDList = [];
        #endregion

        #region Event
        public void Item_Loaded(object sender,RoutedEventArgs e)
        {
            ItemPageView itemPages = new();
            ItemPageViewModel itemPageDataContext = itemPages.DataContext as ItemPageViewModel;
            itemPageDataContext.UseForTool = !IsCloseable;
            ItemPageList[0].Content = itemPages;
        }

        [RelayCommand]
        /// <summary>
        /// 清除不需要的特指数据
        /// </summary>
        private void ClearUnnecessaryData()
        {
            ItemPageViewModel itemPageDataContext = (SelectedItemPage.Content as ItemPageView).DataContext as ItemPageViewModel;
            if (itemPageDataContext.specialDataDictionary.TryGetValue(itemPageDataContext.SelectedItem.ComboBoxItemId, out Grid grid))
                grid = itemPageDataContext.specialDataDictionary[itemPageDataContext.SelectedItem.ComboBoxItemId];
            itemPageDataContext.specialDataDictionary.Clear();
            grid ??= new();
            itemPageDataContext.specialDataDictionary.Add(itemPageDataContext.SelectedItem.ComboBoxItemId, grid);
        }

        [RelayCommand]
        /// <summary>
        /// 添加物品
        /// </summary>
        private void AddItem()
        {
            if(ItemPageList.Count > 0)
            {
                ItemPageViewModel itemPageDataContext = (ItemPageList[0].Content as ItemPageView).DataContext as ItemPageViewModel;
                if (itemPageDataContext.UseForTool)
                {
                    return;
                }
            }
            RichTabItems richTabItems = new()
            {
                Header = "物品",
                FontWeight = FontWeights.Normal,
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                IsContentSaved = true,
                BorderThickness = new(4, 4, 4, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC6B23")),
                Foreground = new SolidColorBrush(Colors.White),
                LeftBorderTexture = Application.Current.Resources["TabItemLeft"] as Brush,
                RightBorderTexture = Application.Current.Resources["TabItemRight"] as Brush,
                TopBorderTexture = Application.Current.Resources["TabItemTop"] as Brush,
                SelectedLeftBorderTexture = Application.Current.Resources["SelectedTabItemLeft"] as Brush,
                SelectedRightBorderTexture = Application.Current.Resources["SelectedTabItemRight"] as Brush,
                SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as Brush,
            };
            ItemPageView itemPage = new() 
            { 
                FontWeight = FontWeights.Normal
            };
            ItemPageViewModel pageContext = itemPage.DataContext as ItemPageViewModel;
            pageContext.UseForReference = false;
            pageContext.UseForTool = !IsCloseable;
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
        private void ClearItem()
        {
            if (IsCloseable)
            {
                ItemPageList.Clear();
            }
        }

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
                ObservableCollection<RichTabItems> result = ItemPageList;
                ExternalDataImportManager.ImportItemDataHandler(dialog.FileName, ref result);
            }
        }

        [RelayCommand]
        /// <summary>
        /// 从剪切板导入
        /// </summary>
        private void ImportItemFromClipboard()
        {
            ObservableCollection<RichTabItems> result = ItemPageList;
            ExternalDataImportManager.ImportItemDataHandler(Clipboard.GetText(), ref result, false);
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
                    FileNameList.Add(context.SelectedItem.ComboBoxItemId + (name is not null ? "-" + name.ToString() : ""));
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
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "resources\\saves\\ItemView\\");
                    for (int i = 0; i < Result.Count; i++)
                    {
                        _ = File.WriteAllTextAsync(openFolderDialog.FolderName + FileNameList[i] + ".command", Result[i]);
                        _ = File.WriteAllTextAsync(AppDomain.CurrentDomain.BaseDirectory + "resources\\saves\\ItemView\\" + FileNameList[i] + ".command", Result[i]);
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
                Message.PushMessage("物品全部生成成功！数据已进入剪切板", MessageBoxImage.Information);
            }
        }
        #endregion
    }

    public class VersionID
    {
        public string HighVersionID { get; set; } = "";
        public string LowVersionID { get; set; } = "";
        public int Damage { get; set; } = 0;
    }
}