using cbhk.CustomControls;
using cbhk.GeneralTools;
using cbhk.GeneralTools.MessageTip;
using cbhk.Generators.ItemGenerator.Components;
using cbhk.WindowDictionaries;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
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
using System.Windows.Data;
using System.Windows.Media;

namespace cbhk.Generators.ItemGenerator
{
    public class ItemDataContext:ObservableObject
    {
        #region 返回、运行和保存等指令
        public RelayCommand<CommonWindow> ReturnCommand { get; set; }
        public RelayCommand RunCommand { get; set; }
        public RelayCommand SaveAll { get; set; }
        public RelayCommand AddItem { get; set; }
        public RelayCommand ClearItem { get; set; }
        public RelayCommand ImportItemFromClipboard { get; set; }
        public RelayCommand ImportItemFromFile { get; set; }
        #endregion

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
        public Window home = null;
        //物品页数据源
        public ObservableCollection<RichTabItems> ItemPageList { get; set; } = new()
        {
            new RichTabItems()
                    {
                        Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                        Header = "物品",
                        FontWeight = FontWeights.Normal,
                        IsContentSaved = true,
                        BorderThickness = new(4, 3, 4, 0),
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                        SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC6B23")),
                        LeftBorderTexture = Application.Current.Resources["TabItemLeft"] as ImageBrush,
                        RightBorderTexture = Application.Current.Resources["TabItemRight"] as ImageBrush,
                        TopBorderTexture = Application.Current.Resources["TabItemTop"] as ImageBrush,
                        SelectedLeftBorderTexture = Application.Current.Resources["SelectedTabItemLeft"] as ImageBrush,
                        SelectedRightBorderTexture = Application.Current.Resources["SelectedTabItemRight"] as ImageBrush,
                        SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as ImageBrush
                    }
        };

        #region 当前选中的物品页
        private TabItem selectedItemPage = null;
        public TabItem SelectedItemPage
        {
            get => selectedItemPage;
            set => SetProperty(ref selectedItemPage,value);
        }
        #endregion

        /// <summary>
        /// 本生成器的图标路径
        /// </summary>
        string icon_path = "pack://application:,,,/cbhk;component/resources/common/images/spawnerIcons/IconItems.png";
        #endregion

        #region 数据表
        public DataTable EnchantmentTable = null;
        public DataTable BlockTable = null;
        public DataTable BlockStateTable = null;
        public DataTable ItemTable = null;
        public DataTable AttributeTable = null;
        public DataTable AttributeSlotTable = null;
        public DataTable AttributeValueTypeTable = null;
        public DataTable EffectTable = null;
        public DataTable HideInfomationTable = null;
        #endregion

        public ItemDataContext()
        {
            #region 链接指令
            RunCommand = new RelayCommand(run_command);
            ReturnCommand = new RelayCommand<CommonWindow>(return_command);
            SaveAll = new RelayCommand(SaveAllCommand);
            AddItem = new RelayCommand(AddItemCommand);
            ClearItem = new RelayCommand(ClearItemCommand);
            ImportItemFromClipboard = new RelayCommand(ImportItemFromClipboardCommand);
            ImportItemFromFile = new RelayCommand(ImportItemFromFileCommand);
            #endregion
            #region 初始化数据表
            DataCommunicator dataCommunicator = DataCommunicator.GetDataCommunicator();
            Task.Run(async () =>
            {
                EnchantmentTable = await dataCommunicator.GetData("SELECT * FROM Enchantments");
                BlockTable = await dataCommunicator.GetData("SELECT * FROM Blocks");
                BlockStateTable = await dataCommunicator.GetData("SELECT * FROM BlockStates");
                ItemTable = await dataCommunicator.GetData("SELECT * FROM Items");
                AttributeTable = await dataCommunicator.GetData("SELECT * FROM Attributes");
                AttributeSlotTable = await dataCommunicator.GetData("SELECT * FROM AttributeSlots");
                AttributeValueTypeTable = await dataCommunicator.GetData("SELECT * FROM AttributeValueTypes");
                EffectTable = await dataCommunicator.GetData("SELECT * FROM MobEffects");
                HideInfomationTable = await dataCommunicator.GetData("SELECT * FROM HideInfomation");
            });
            #endregion
            #region 初始化物品页
            object obj = new();
            BindingOperations.EnableCollectionSynchronization(ItemPageList, obj);
            Task.Run(async () =>
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ItemPageList[0].Content = new ItemPages();
                });
            });
            #endregion
        }

        /// <summary>
        /// 添加物品
        /// </summary>
        private void AddItemCommand()
        {
            RichTabItems richTabItems = new()
            {
                Header = "物品",
                FontWeight = FontWeights.Normal,
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
            ItemPages itemPage = new() { FontWeight = FontWeights.Normal };
            richTabItems.Content = itemPage;
            ItemPageList.Add(richTabItems);
            if (ItemPageList.Count == 1)
            {
                TabControl tabControl = richTabItems.FindParent<TabControl>();
                tabControl.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 清空物品
        /// </summary>
        private void ClearItemCommand()
        {
            ItemPageList.Clear();
        }

        /// <summary>
        /// 从文件导入
        /// </summary>
        private void ImportItemFromFileCommand()
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

        /// <summary>
        /// 从剪切板导入
        /// </summary>
        private void ImportItemFromClipboardCommand()
        {
            ObservableCollection<RichTabItems> result = ItemPageList;
            ExternalDataImportManager.ImportItemDataHandler(Clipboard.GetText(), ref result, false);
        }

        /// <summary>
        /// 保存所有物品
        /// </summary>
        private async void SaveAllCommand()
        {
            List<string> Result = new();
            List<string> FileNameList = new();

            foreach (var itemPage in ItemPageList)
            {
                await itemPage.Dispatcher.InvokeAsync(() =>
                {
                    ItemPages itemPages = itemPage.Content as ItemPages;
                    ItemPageDataContext context = itemPages.DataContext as ItemPageDataContext;
                    string result = context.run_command(false);
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
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new()
            {
                Description = "请选择要保存的目录",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (Directory.Exists(folderBrowserDialog.SelectedPath))
                {
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "resources\\saves\\Item\\");
                    for (int i = 0; i < Result.Count; i++)
                    {
                        _ = File.WriteAllTextAsync(folderBrowserDialog.SelectedPath + FileNameList[i] + ".command", Result[i]);
                        _ = File.WriteAllTextAsync(AppDomain.CurrentDomain.BaseDirectory + "resources\\saves\\Item\\" + FileNameList[i] + ".command", Result[i]);
                    }
                }
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
        /// 全部生成
        /// </summary>
        private async void run_command()
        {
            StringBuilder Result = new();
            foreach (var itemPage in ItemPageList)
            {
                await itemPage.Dispatcher.InvokeAsync(() =>
                {
                    ItemPageDataContext context = (itemPage.Content as ItemPages).DataContext as ItemPageDataContext;
                    string result = context.run_command(false) + "\r\n";
                    Result.Append(result);
                });
            }
            if (ShowGeneratorResult)
            {
                GenerateResultDisplayer.Displayer displayer = GenerateResultDisplayer.Displayer.GetContentDisplayer();
                displayer.GeneratorResult(Result.ToString(), "物品", icon_path);
                displayer.Show();
            }
            else
            {
                Clipboard.SetText(Result.ToString());
                Message.PushMessage("物品全部生成成功！数据已进入剪切板", MessageBoxImage.Information);
            }
        }
    }
}
