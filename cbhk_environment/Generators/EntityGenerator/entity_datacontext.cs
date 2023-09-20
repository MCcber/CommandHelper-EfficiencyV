using cbhk_environment.CustomControls;
using cbhk_environment.GeneralTools;
using cbhk_environment.GeneralTools.MessageTip;
using cbhk_environment.Generators.EntityGenerator.Components;
using cbhk_environment.WindowDictionaries;
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
using System.Windows.Threading;

namespace cbhk_environment.Generators.EntityGenerator
{
    public class entity_datacontext : ObservableObject
    {
        #region 返回和运行指令等指令
        public RelayCommand<CommonWindow> ReturnCommand { get; set; }
        public RelayCommand RunCommand { get; set; }
        public RelayCommand SaveAll { get; set; }
        #endregion

        #region 添加实体、清空实体、导入实体
        public RelayCommand AddEntity { get; set; }
        public RelayCommand ClearEntity { get; set; }
        public RelayCommand ImportEntityFromClipboard { get; set; }
        public RelayCommand ImportEntityFromFile { get; set; }
        #endregion

        #region 是否展示生成结果
        private bool showGeneratorResult = false;
        public bool ShowGeneratorResult
        {
            get => showGeneratorResult;
            set => SetProperty(ref showGeneratorResult, value);
        }
        #endregion

        #region 字段与引用
        //本生成器的图标路径
        string icon_path = "pack://application:,,,/cbhk_environment;component/resources/common/images/spawnerIcons/IconEntities.png";
        //实体标签页的数据源
        public ObservableCollection<RichTabItems> EntityPageList { get; set; } = new();
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
        #endregion

        public entity_datacontext()
        {
            #region 连接指令
            ReturnCommand = new RelayCommand<CommonWindow>(return_command);
            RunCommand = new RelayCommand(run_command);
            SaveAll = new RelayCommand(SaveAllCommand);
            AddEntity = new RelayCommand(AddEntityCommand);
            ClearEntity = new RelayCommand(ClearEntityCommand);
            ImportEntityFromClipboard = new RelayCommand(ImportEntityFromClipboardCommand);
            ImportEntityFromFile = new RelayCommand(ImportEntityFromFileCommand);
            #endregion
            #region 初始化数据表
            DataCommunicator dataCommunicator = DataCommunicator.GetDataCommunicator();
            Task.Run(async () =>
            {
                BlockTable = await dataCommunicator.GetData("SELECT * FROM Blocks");
                BlockStateTable = await dataCommunicator.GetData("SELECT * FROM BlockStates");
                MobEffectTable = await dataCommunicator.GetData("SELECT * FROM MobEffects");
                MobAttributeTable = await dataCommunicator.GetData("SELECT * FROM MobAttributes");
                EntityIdTable = await dataCommunicator.GetData("SELECT * FROM Entities");
            });
            #endregion
            #region 载入实体预设数据
            EntityPageList.Add(new RichTabItems()
            {
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                Header = "实体",
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
            });
            EntityPageList[0].Content = new EntityPages();
            #endregion
        }

        /// <summary>
        /// 从文件导入实体
        /// </summary>
        private void ImportEntityFromFileCommand()
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

        /// <summary>
        /// 从剪切板导入实体
        /// </summary>
        private void ImportEntityFromClipboardCommand()
        {
            ObservableCollection<RichTabItems> result = EntityPageList;
            ExternalDataImportManager.ImportEntityDataHandler(Clipboard.GetText(), ref result,false);
        }

        /// <summary>
        /// 清空实体
        /// </summary>
        private void ClearEntityCommand()
        {
            EntityPageList.Clear();
        }

        /// <summary>
        /// 添加实体
        /// </summary>
        public void AddEntityCommand()
        {
            RichTabItems richTabItems = new()
            {
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                Header = "实体",
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
            };
            EntityPages entityPages = new() { FontWeight = FontWeights.Normal };
            richTabItems.Content = entityPages;
            EntityPageList.Add(richTabItems);
            if (EntityPageList.Count == 1)
            {
                TabControl tabControl = richTabItems.FindParent<TabControl>();
                if(tabControl != null)
                tabControl.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 返回主页
        /// </summary>
        /// <param name="win"></param>
        private void return_command(CommonWindow win)
        {
            Entity.cbhk.Topmost = true;
            Entity.cbhk.WindowState = WindowState.Normal;
            Entity.cbhk.Show();
            Entity.cbhk.Topmost = false;
            Entity.cbhk.ShowInTaskbar = true;
            win.Close();
        }

        /// <summary>
        /// 全部生成
        /// </summary>
        private async void run_command()
        {
            StringBuilder Result = new();
            foreach (var entityPage in EntityPageList)
            {
                await entityPage.Dispatcher.InvokeAsync(() =>
                {
                    EntityPages entityPages = entityPage.Content as EntityPages;
                    entityPagesDataContext pageContext = entityPages.DataContext as entityPagesDataContext;
                    string result = pageContext.run_command(false) + "\r\n";
                    Result.Append(result);
                });
            }
            if (ShowGeneratorResult)
            {
                GenerateResultDisplayer.Displayer displayer = GenerateResultDisplayer.Displayer.GetContentDisplayer();
                displayer.GeneratorResult(Result.ToString(), "实体", icon_path);
                displayer.Show();
            }
            else
            {
                Clipboard.SetText(Result.ToString());
                Message.PushMessage("实体全部生成成功！数据已进入剪切板", MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 生成并保存所有的实体数据为文件
        /// </summary>
        private async void SaveAllCommand()
        {
            await GeneratorAndSaveAllEntites();
        }

        /// <summary>
        /// 生成并保存所有实体到本地文件
        /// </summary>
        /// <returns></returns>
        private async Task GeneratorAndSaveAllEntites()
        {
            List<string> Result = new();
            List<string> FileNameList = new();

            foreach (var entityPage in EntityPageList)
            {
                await entityPage.Dispatcher.InvokeAsync(() =>
                {
                    EntityPages entityPages = entityPage.Content as EntityPages;
                    entityPagesDataContext pageContext = entityPages.DataContext as entityPagesDataContext;
                    string result = pageContext.run_command(false);
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
                    FileNameList.Add(pageContext.SelectedEntityIdString + (name != null?"-" + name.ToString():""));
                    Result.Add(result);
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
            if(folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                for (int i = 0; i < Result.Count; i++)
                {
                    if (Directory.Exists(folderBrowserDialog.SelectedPath))
                        _ = File.WriteAllTextAsync(folderBrowserDialog.SelectedPath + FileNameList[i] + ".command", Result[i]);
                    _ = File.WriteAllTextAsync(AppDomain.CurrentDomain.BaseDirectory + "resources\\saves\\Entity\\" + folderBrowserDialog.SelectedPath + FileNameList[i] + ".command", Result[i]);
                }
            }
        }
    }
}
