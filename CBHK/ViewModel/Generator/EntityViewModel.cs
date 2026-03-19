using CBHK.CustomControl.Container;
using CBHK.CustomControl.VectorComboBox;
using CBHK.Domain;
using CBHK.Model.Common;
using CBHK.Utility.Common;
using CBHK.Utility.MessageTip;
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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CBHK.ViewModel.Generator
{
    public partial class EntityViewModel(IContainerProvider container,CBHKDataContext Context, MainView mainView) : ObservableObject
    {
        #region Field
        private MessagePopup messagePopup = new();
        private IContainerProvider container = container;
        private CBHKDataContext context = Context;
        /// <summary>
        /// 主页引用
        /// </summary>
        private MainView home = mainView;
        //本生成器的图标路径
        string iconPath = "pack://application:,,,/CBHK;component/Resource/Common/Image/SpawnerIcon/IconEntities.png";
        private string ModifierOperationTypeFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Entity\Data\AttributeModifierOperationType.ini";
        string SpecialNBTStructureFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Entity\Data\SpecialTags.json";
        public ObservableCollection<string> ModifierOperationTypeSource = [];
        public JArray SpecialArray = null;
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
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                Header = "实体",
                FontWeight = FontWeights.Normal
            } ];

        /// <summary>
        /// 已选中的实体页
        /// </summary>
        [ObservableProperty]
        private VectorRichTabItem _selectedEntityPage;

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
                    pageContext.Build(currentResult);
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
                    EntityPageView entityPages = container.Resolve<EntityPageView>();
                    EntityPageViewModel entityPagesDataContext = entityPages.DataContext as EntityPageViewModel;
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
            EntityPageView entityPages = SelectedEntityPage.Content as EntityPageView;
            EntityPageViewModel entityPagesDataContext = entityPages.DataContext as EntityPageViewModel;
            Grid currentGrid = entityPagesDataContext.SpecialDataDictionary[entityPagesDataContext.SelectedEntityId.Text];
            entityPagesDataContext.SpecialDataDictionary.Clear();
            entityPagesDataContext.SpecialDataDictionary.Add(entityPagesDataContext.SelectedEntityId.Text, currentGrid);
            foreach (var item in entityPagesDataContext.SpecialTagsResult)
            {
                if (item.Key != entityPagesDataContext.SelectedEntityId.Text)
                {
                    item.Value.Clear();
                }
            }
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
                ObservableCollection<VectorRichTabItem> result = EntityPageList;
                ExternalDataImportManager.ImportEntityDataHandler(dialog.FileName, ref result,messagePopup);
            }
        }

        [RelayCommand]
        /// <summary>
        /// 从剪切板导入实体
        /// </summary>
        private void ImportEntityFromClipboard()
        {
            ObservableCollection<VectorRichTabItem> result = EntityPageList;
            ExternalDataImportManager.ImportEntityDataHandler(Clipboard.GetText(), ref result,messagePopup,false);
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
            if(EntityPageList.Count > 0)
            {
                EntityPageViewModel entityPagesDataContext = (EntityPageList[0].Content as EntityPageView).DataContext as EntityPageViewModel;
                if (entityPagesDataContext.UseForTool)
                {
                    return;
                }
            }
            VectorRichTabItem richTabItems = new()
            {
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                Header = "实体"
            };
            EntityPageView entityPages = new() { FontWeight = FontWeights.Normal };
            EntityPageViewModel pageContext = entityPages.DataContext as EntityPageViewModel;
            pageContext.UseForTool = !IsCloseable;
            richTabItems.Content = entityPages;
            EntityPageList.Add(richTabItems);
            if (EntityPageList.Count == 1)
            {
                TabControl tabControl = richTabItems.FindParent<TabControl>();
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
                pageContext.Build(currentResult);
                string result = currentResult.ToString();
                Result.AppendLine(result);
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