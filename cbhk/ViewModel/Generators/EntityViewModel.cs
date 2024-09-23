using cbhk.CustomControls;
using cbhk.GeneralTools;
using cbhk.GeneralTools.MessageTip;
using cbhk.Generators.EntityGenerator.Components;
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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace cbhk.ViewModel.Generators
{
    public partial class EntityViewModel : ObservableObject
    {
        #region 是否展示生成结果
        private bool showGeneratorResult = false;
        public bool ShowGeneratorResult
        {
            get => showGeneratorResult;
            set => SetProperty(ref showGeneratorResult, value);
        }
        #endregion

        #region 字段与引用
        /// <summary>
        /// 主页引用
        /// </summary>
        private MainView home = null;
        //本生成器的图标路径
        string icon_path = "pack://application:,,,/cbhk;component/Resource/Common/Image/SpawnerIcon/IconEntities.png";
        private string ModifierOperationTypeFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Entity\Data\AttributeModifierOperationType.ini";
        string SpecialNBTStructureFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Entity\Data\SpecialTags.json";
        public ObservableCollection<string> ModifierOperationTypeSource = [];
        public JArray SpecialArray = null;
        //实体标签页的数据源
        public ObservableCollection<RichTabItems> EntityPageList { get; set; } = [
            new RichTabItems()
            {
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                Header = "实体",
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
            } ];

        private RichTabItems selectedEntityPage;

        public RichTabItems SelectedEntityPage
        {
            get => selectedEntityPage;
            set => SetProperty(ref selectedEntityPage, value);
        }

        #region 版本数据源
        public ObservableCollection<TextComboBoxItem> VersionSource { get; set; } = [
            new TextComboBoxItem() { Text = "1.20.5" },
            new TextComboBoxItem() { Text = "1.20.2" },
            new TextComboBoxItem() { Text = "1.20.1" },
            new TextComboBoxItem() { Text = "1.20.0" },
            new TextComboBoxItem() { Text = "1.19.0" },
            new TextComboBoxItem() { Text = "1.17.0" },
            new TextComboBoxItem() { Text = "1.16.2" },
            new TextComboBoxItem() { Text = "1.16.0" },
            new TextComboBoxItem() { Text = "1.15.0" },
            new TextComboBoxItem() { Text = "1.14.0" },
            new TextComboBoxItem() { Text = "1.13.0" },
            new TextComboBoxItem() { Text = "1.12.0" }
        ];
        #endregion

        #region 能否关闭标签页
        private bool isCloseable = true;

        public bool IsCloseable
        {
            get => isCloseable;
            set => SetProperty(ref isCloseable, value);
        }
        #endregion

        //实体标签页数量,用于为共通标签提供数据
        private int passengerMaxIndex = 0;
        public int PassengerMaxIndex
        {
            get => passengerMaxIndex;
            set
            {
                passengerMaxIndex = EntityPageList.Count - 1;
                OnPropertyChanged();
            }
        }

        public DataTable MobEffectTable = null;
        public DataTable BlockTable = null;
        public DataTable BlockStateTable = null;
        public DataTable MobAttributeTable = null;
        public DataTable EntityIdTable = null;
        private IContainerProvider _container;
        #endregion

        public EntityViewModel(IContainerProvider container,MainView mainView)
        {
            _container = container;
            home = mainView;
        }

        /// <summary>
        /// 实体载入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void Entity_Loaded(object sender,RoutedEventArgs e)
        {
            #region 初始化数据表
            DataCommunicator dataCommunicator = DataCommunicator.GetDataCommunicator();
            await Task.Run(async () =>
            {
                BlockTable = await dataCommunicator.GetData("SELECT * FROM Blocks");
                BlockStateTable = await dataCommunicator.GetData("SELECT * FROM BlockStates");
                MobEffectTable = await dataCommunicator.GetData("SELECT * FROM MobEffects");
                MobAttributeTable = await dataCommunicator.GetData("SELECT * FROM MobAttributes");
                EntityIdTable = await dataCommunicator.GetData("SELECT * FROM Entities");
            });
            #endregion
            #region 载入实体预设数据
            await Task.Run(() =>
            {
                if (File.Exists(ModifierOperationTypeFilePath))
                {
                    List<string> data = File.ReadLines(ModifierOperationTypeFilePath).ToList();
                    for (int i = 0; i < data.Count; i++)
                    {
                        ModifierOperationTypeSource.Add(data[i]);
                    }
                }
                string SpecialData = File.ReadAllText(SpecialNBTStructureFilePath);
                SpecialArray = JArray.Parse(SpecialData);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    EntityPagesView entityPages = _container.Resolve<EntityPagesView>();
                    EntityPagesViewModel entityPagesDataContext = entityPages.DataContext as EntityPagesViewModel;
                    entityPagesDataContext.UseForTool = !IsCloseable;
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
            EntityPagesView entityPages = SelectedEntityPage.Content as EntityPagesView;
            EntityPagesViewModel entityPagesDataContext = entityPages.DataContext as EntityPagesViewModel;
            Grid currentGrid = entityPagesDataContext.SpecialDataDictionary[entityPagesDataContext.SelectedEntityIDString];
            entityPagesDataContext.SpecialDataDictionary.Clear();
            entityPagesDataContext.SpecialDataDictionary.Add(entityPagesDataContext.SelectedEntityIDString, currentGrid);
            foreach (var item in entityPagesDataContext.SpecialTagsResult)
            {
                if (item.Key != entityPagesDataContext.SelectedEntityIDString)
                    item.Value.Clear();
            }
        }

        [RelayCommand]
        /// <summary>
        /// 从文件导入实体
        /// </summary>
        private void ImportEntityFromFile()
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
                    ObservableCollection<RichTabItems> result = EntityPageList;
                    ExternalDataImportManager.ImportEntityDataHandler(dialog.FileName, ref result);
                }
        }

        [RelayCommand]
        /// <summary>
        /// 从剪切板导入实体
        /// </summary>
        private void ImportEntityFromClipboard()
        {
            ObservableCollection<RichTabItems> result = EntityPageList;
            ExternalDataImportManager.ImportEntityDataHandler(Clipboard.GetText(), ref result,false);
        }

        [RelayCommand]
        /// <summary>
        /// 清空实体
        /// </summary>
        private void ClearEntity()
        {
            if (IsCloseable)
                EntityPageList.Clear();
        }

        [RelayCommand]
        /// <summary>
        /// 添加实体
        /// </summary>
        public void AddEntity()
        {
            if(EntityPageList.Count > 0)
            {
                EntityPagesViewModel entityPagesDataContext = (EntityPageList[0].Content as EntityPagesView).DataContext as EntityPagesViewModel;
                if (entityPagesDataContext.UseForTool)
                {
                    return;
                }
            }
            RichTabItems richTabItems = new()
            {
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                Header = "实体",
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
            };
            EntityPagesView entityPages = new() { FontWeight = FontWeights.Normal };
            EntityPagesViewModel pageContext = entityPages.DataContext as EntityPagesViewModel;
            pageContext.UseForTool = !IsCloseable;
            richTabItems.Content = entityPages;
            EntityPageList.Add(richTabItems);
            if (EntityPageList.Count == 1)
            {
                TabControl tabControl = richTabItems.FindParent<TabControl>();
                if(tabControl != null)
                tabControl.SelectedIndex = 0;
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
            foreach (var entityPage in EntityPageList)
            {
                await entityPage.Dispatcher.InvokeAsync(() =>
                {
                    EntityPagesView entityPages = entityPage.Content as EntityPagesView;
                    EntityPagesViewModel pageContext = entityPages.DataContext as EntityPagesViewModel;
                    string result = pageContext.Run(false) + "\r\n";
                    Result.Append(result);
                });
            }
            if (ShowGeneratorResult)
            {
                DisplayerView displayer = _container.Resolve<DisplayerView>();
                if (displayer is not null && displayer.DataContext is DisplayerViewModel displayerViewModel)
                {
                    displayerViewModel.GeneratorResult(Result.ToString(), "实体", icon_path);
                }
            }
            else
            {
                Clipboard.SetText(Result.ToString());
                Message.PushMessage("实体全部生成成功！数据已进入剪切板", MessageBoxImage.Information);
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
                    EntityPagesView entityPages = entityPage.Content as EntityPagesView;
                    EntityPagesViewModel pageContext = entityPages.DataContext as EntityPagesViewModel;
                    string result = pageContext.Run(false);
                    string nbt = "";
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
                        if(nbt.Length > 0)
                        entityIDPath = "CustomName";
                    JToken name = resultJSON.SelectToken(entityIDPath);
                    FileNameList.Add(pageContext.SelectedEntityIDString + (name != null?"-" + name.ToString():""));
                    Result.Add(result);
                });
            }
            OpenFolderDialog openFolderDialog = new()
            {
                Title = "请选择要保存的目录",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                ShowHiddenItems = true
            };
            if(openFolderDialog.ShowDialog().Value)
            {
                for (int i = 0; i < Result.Count; i++)
                {
                    if (Directory.Exists(openFolderDialog.FolderName))
                        _ = File.WriteAllTextAsync(openFolderDialog.FolderName + FileNameList[i] + ".command", Result[i]);
                    _ = File.WriteAllTextAsync(AppDomain.CurrentDomain.BaseDirectory + "resources\\saves\\EntityView\\" + openFolderDialog.FolderName + FileNameList[i] + ".command", Result[i]);
                }
            }
        }
    }
}