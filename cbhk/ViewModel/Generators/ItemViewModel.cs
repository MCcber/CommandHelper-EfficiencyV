using cbhk.CustomControls;
using cbhk.GeneralTools;
using cbhk.GeneralTools.MessageTip;
using cbhk.Generators.ItemGenerator.Components;
using cbhk.View;
using cbhk.WindowDictionaries;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace cbhk.ViewModel.Generators
{
    public partial class ItemViewModel:ObservableObject
    {
        #region 显示结果
        private bool showGeneratorResult = false;
        public bool ShowGeneratorResult
        {
            get => showGeneratorResult;
            set => SetProperty(ref showGeneratorResult, value);
        }
        #endregion

        #region 字段
        /// <summary>
        /// 主页引用
        /// </summary>
        private Window home = null;
        //物品页数据源
        public ObservableCollection<RichTabItems> ItemPageList { get; set; } =
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

        #region 能否关闭标签页
        private bool isCloseable = true;

        public bool IsCloseable
        {
            get => isCloseable;
            set => SetProperty(ref isCloseable, value);
        }
        #endregion

        #region 版本列表
        public ObservableCollection<TextComboBoxItem> VersionList { get; set; } = [
            new TextComboBoxItem() { Text = "1.20.5" }, 
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
        #endregion

        #region 版本ID数据源
        private ObservableCollection<VersionID> versionIDList = [];

        public ObservableCollection<VersionID> VersionIDList
        {
            get => versionIDList;
            set => SetProperty(ref versionIDList, value);
        }
        #endregion

        #region 当前选中的物品页
        private RichTabItems selectedItemPage = null;
        public RichTabItems SelectedItemPage
        {
            get => selectedItemPage;
            set => SetProperty(ref selectedItemPage,value);
        }
        #endregion

        #region 数据表
        public DataTable ItemTable = null;
        public DataTable BlockVersionIDTable = null;
        public DataTable EnchantmentTable = null;
        public DataTable AttributeTable = null;
        public DataTable AttributeSlotTable = null;
        public DataTable AttributeValueTypeTable = null;
        public DataTable EffectTable = null;
        public DataTable HideInfomationTable = null;
        public DataTable BlockTable = null;
        public DataTable BlockStateTable = null;
        #endregion

        /// <summary>
        /// 本生成器的图标路径
        /// </summary>
        string icon_path = "pack://application:,,,/cbhk;component/Resource/Common/Image/SpawnerIcon/IconItems.png";
        private IContainerProvider _container;
        #endregion

        public ItemViewModel(IContainerProvider container,MainView mainView)
        {
            _container = container;
            home = mainView;
        }

        public async void Item_Loaded(object sender,RoutedEventArgs e)
        {
            #region 初始化数据表与物品页
            DataCommunicator dataCommunicator = DataCommunicator.GetDataCommunicator();
            await Task.Run(async () =>
            {
                BlockVersionIDTable = await dataCommunicator.GetData("SELECT * FROM VersionBlock");
                ItemTable = await dataCommunicator.GetData("SELECT * FROM Items");
                EnchantmentTable = await dataCommunicator.GetData("SELECT * FROM Enchantments");
                BlockTable = await dataCommunicator.GetData("SELECT * FROM Blocks");
                BlockStateTable = await dataCommunicator.GetData("SELECT * FROM BlockStates");
                AttributeTable = await dataCommunicator.GetData("SELECT * FROM MobAttributes");
                AttributeSlotTable = await dataCommunicator.GetData("SELECT * FROM AttributeSlots");
                AttributeValueTypeTable = await dataCommunicator.GetData("SELECT * FROM AttributeValueTypes");
                EffectTable = await dataCommunicator.GetData("SELECT * FROM MobEffects");
                HideInfomationTable = await dataCommunicator.GetData("SELECT * FROM HideInfomation");
            });
            #endregion
            #region 初始化物品页
            ItemPagesView itemPages = new();
            ItemPageViewModel itemPageDataContext = itemPages.DataContext as ItemPageViewModel;
            itemPageDataContext.UseForTool = !IsCloseable;
            ItemPageList[0].Content = itemPages;
            #endregion
        }

        [RelayCommand]
        /// <summary>
        /// 清除不需要的特指数据
        /// </summary>
        private void ClearUnnecessaryData()
        {
            ItemPageViewModel itemPageDataContext = (SelectedItemPage.Content as ItemPagesView).DataContext as ItemPageViewModel;
            if (itemPageDataContext.specialDataDictionary.TryGetValue(itemPageDataContext.SelectedItemId.ComboBoxItemId, out Grid grid))
                grid = itemPageDataContext.specialDataDictionary[itemPageDataContext.SelectedItemId.ComboBoxItemId];
            itemPageDataContext.specialDataDictionary.Clear();
            grid ??= new();
            itemPageDataContext.specialDataDictionary.Add(itemPageDataContext.SelectedItemId.ComboBoxItemId, grid);
        }

        [RelayCommand]
        /// <summary>
        /// 添加物品
        /// </summary>
        private void AddItem()
        {
            if(ItemPageList.Count > 0)
            {
                ItemPageViewModel itemPageDataContext = (ItemPageList[0].Content as ItemPagesView).DataContext as ItemPageViewModel;
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
            ItemPagesView itemPage = new() { FontWeight = FontWeights.Normal };
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
                ItemPageList.Clear();
        }

        [RelayCommand]
        /// <summary>
        /// 从文件导入
        /// </summary>
        private void ImportItemFromFile()
        {
            Microsoft.Win32.OpenFileDialog dialog = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                RestoreDirectory = true,
                DefaultExt = ".command",
                Multiselect = false,
                Title = "请选择一个Minecraft实体数据文件"
            };
            if (dialog.ShowDialog().Value)
                if (File.Exists(dialog.FileName))
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
                    ItemPagesView itemPages = itemPage.Content as ItemPagesView;
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
                    FileNameList.Add(context.SelectedItemId.ComboBoxItemId + (name != null ? "-" + name.ToString() : ""));
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
        /// 全部生成
        /// </summary>
        private async Task Run()
        {
            StringBuilder Result = new();
            foreach (var itemPage in ItemPageList)
            {
                ItemPageViewModel context = (itemPage.Content as ItemPagesView).DataContext as ItemPageViewModel;
                string currentResult = await context.Run(false);
                Result.Append(currentResult + "\r\n");
            }
            if (ShowGeneratorResult)
            {
                DisplayerView displayer = _container.Resolve<DisplayerView>();
                if (displayer is not null && displayer.DataContext is DisplayerViewModel displayerViewModel)
                {
                    displayer.Show();
                    displayerViewModel.GeneratorResult(Result.ToString(), "物品", icon_path);
                }
            }
            else
            {
                Clipboard.SetText(Result.ToString());
                Message.PushMessage("物品全部生成成功！数据已进入剪切板", MessageBoxImage.Information);
            }
        }
    }

    public class VersionID
    {
        public string HighVersionID { get; set; } = "";
        public string LowVersionID { get; set; } = "";
        public int Damage { get; set; } = 0;
    }
}