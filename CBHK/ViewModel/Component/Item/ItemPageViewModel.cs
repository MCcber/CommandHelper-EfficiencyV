using CBHK.CustomControl.VectorComboBox;
using CBHK.Domain;
using CBHK.Interface.Visual;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using CBHK.Utility.Data;
using CBHK.Utility.Data.Mcdoc;
using CBHK.Utility.Visual.MessageTip;
using CBHK.View;
using CBHK.ViewModel.Generator;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinecraftLanguageModelLibrary.Data;
using Newtonsoft.Json.Linq;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CBHK.ViewModel.Component.Item
{
    public partial class ItemPageViewModel : McdocGeneratorViewModelBase, IPageViewModel
    {
        #region Field
        private const string dispatcherRegistry = "minecraft:item";

        private readonly IContainerProvider container;
        private readonly CBHKDataContext context;
        private readonly DataService dataService;
        private readonly Resource resource;
        private readonly string iconPath =
            "pack://application:,,,/CBHK;component/Resource/Common/Image/SpawnerIcon/IconItem.png";

        private bool loadingItemIds;
        #endregion

        #region Property
        public MessagePopup MessagePopup { get; set; } = new();

        #region 给予
        /// <summary>召唤（掉落物）模式；不勾就是给予。</summary>
        [ObservableProperty]
        private bool summon;

        /// <summary>给予模式下允许纯文本样式（旧调用面保留）。</summary>
        [ObservableProperty]
        private bool isNoStyleText = true;
        #endregion

        #region 显示结果
        [ObservableProperty]
        private bool showGeneratorResult;
        #endregion

        #region 已选择的版本

        [ObservableProperty]
        private VectorTextComboBoxItem selectedVersion;
        #endregion

        #region 保存物品ID
        [ObservableProperty]
        private VectorIconTextComboBoxItem selectedItem;
        #endregion

        #region 版本数据源
        /// <summary>版本下拉候选（旧视图绑的是这个名字，内容来自基类的 GameVersionOptions）。</summary>
        [ObservableProperty]
        private ObservableCollection<VectorTextComboBoxItem> versionSource = [];
        #endregion

        #region 物品ID列表
        [ObservableProperty]
        private ObservableCollection<VectorIconTextComboBoxItem> itemList = [];
        #endregion

        #region 是否作为工具或引用

        [ObservableProperty]
        private bool useForTool;

        [ObservableProperty]
        private bool useForReference;
        #endregion

        #region 导入外部数据模式
        [ObservableProperty]
        private bool importMode;

        /// <summary>外部读进来的数据（导入模式用）。</summary>
        public JObject ExternallyReadEntityData { get; set; }
        #endregion

        #region 是否同步到文件、存储外部数据
        public string ExternFilePath { get; set; }

        /// <summary>最近一次生成的命令（旧调用面在读取它）。</summary>
        public string Result { get; set; }
        #endregion
        #endregion

        #region Method
        public ItemPageViewModel(IContainerProvider Container, CBHKDataContext Context, DataService DataService, Resource Resource)
            : base(Resource, dispatcherRegistry, null)
        {
            container = Container;
            context = Context;
            dataService = DataService;
            resource = Resource;
            // 旧调用方（ExternalDataImportManager）会直接写 context.SelectedVersion.Text，
            // 所以在构造期就把它建出来：版本候选来自基类已经填好的 GameVersionOptions。
            foreach (var option in GameVersionOptions)
                VersionSource.Add(new VectorTextComboBoxItem { Text = option.Name });
            SelectedVersion = VersionSource.FirstOrDefault(candidate => candidate.Text == GameVersion)
                ?? VersionSource.FirstOrDefault();
        }

        #region 搜索当前物品ID对应的JSON对象

        /// <summary>按当前版本重建物品下拉（id 来自注册表 item，带物品图标）。</summary>
        private void LoadItemIdsForVersion()
        {
            string version = SelectedVersion?.Text ?? GameVersion;
            if (string.IsNullOrEmpty(version)) return;
            loadingItemIds = true;
            try
            {
                var previous = SelectedItem?.Text;
                ItemList.Clear();
                SelectedItem = null;
                string imageSetFolderPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
                foreach (string item in resource.GetIds("item", version))
                {
                    string iconPath = imageSetFolderPath + item + ".png";
                    ItemList.Add(new VectorIconTextComboBoxItem
                    {
                        Image = File.Exists(iconPath) ? new BitmapImage(new Uri(iconPath, UriKind.Absolute)) : null,
                        Text = item
                    });
                }
                SelectedItem = ItemList.FirstOrDefault(candidate => candidate.Text == previous) ?? ItemList.FirstOrDefault();
            }
            finally
            {
                loadingItemIds = false;
            }
        }

        /// <summary>换物品：按 dispatcher 重新开一棵树（schema 决定这一页显示什么）。</summary>
        public void UpdateItemDataTree()
        {
            if (loadingItemIds || SelectedItem is null || string.IsNullOrEmpty(GameVersion)) return;
            OpenMcdocTreeFromDispatcher(GameVersion, dispatcherRegistry,
                McdocIndex.Static(SelectedItem.Text), resource.McdocTypes);
        }

        #endregion

        #region 旧调用面（命令拼装）
        public StringBuilder Create() => new();

        /// <summary>实体数据就在 JsonText（mcdoc 文档）里，直接交出去。</summary>
        public void CollectionData(StringBuilder Result) => Result?.Append(JsonText ?? "");

        public void Build(StringBuilder Result) => BuildResult(Result);

        /// <summary>
        /// 把缓冲里的物品 NBT 包成命令：**给予**（默认）或 **召唤掉落物**。
        /// - 给予 1.13+：`give @p minecraft:&lt;id&gt;{&lt;nbt&gt;} 1`（没有 NBT 就不带花括号）
        /// - 给予 ≤1.12：`give @p minecraft:&lt;id&gt; 1 0 {&lt;nbt&gt;}`
        /// - 召唤：`summon item ~ ~ ~ {Item:{id:"minecraft:&lt;id&gt;",Count:1b,tag:{&lt;nbt&gt;}}}`
        /// </summary>
        public void BuildResult(StringBuilder Result)
        {
            if (Result is null) return;
            var body = Result.ToString().Trim();
            var item = SelectedItem?.Text ?? "stone";
            var legacy = VersionRangeChecker.IsLessThan(GameVersion, "1.13");
            string command;
            if (Summon)
            {
                command = "summon item ~ ~ ~ {Item:{id:\"minecraft:" + item + "\",Count:1b"
                    + (body.Length > 0 ? ",tag:{" + body + "}" : "") + "}}";
            }
            else if (legacy)
            {
                command = "give @p minecraft:" + item + " 1 0" + (body.Length > 0 ? " {" + body + "}" : "");
            }
            else
            {
                command = "give @p minecraft:" + item + (body.Length > 0 ? "{" + body + "}" : "") + " 1";
            }
            Result.Clear();
            Result.Append(command);
        }

        #endregion
        #endregion

        #region Event
        #region 更新子控件的版本以及执行数据更新事件

        public void ItemPages_Loaded(object sender, RoutedEventArgs e)
        {
            // 旧视图的版本下拉绑 VersionSource：用它包一层基类的 GameVersionOptions
            VersionSource.Clear();
            foreach (var option in GameVersionOptions)
                VersionSource.Add(new VectorTextComboBoxItem { Text = option.Name });
            SelectedVersion = VersionSource.FirstOrDefault(candidate => candidate.Text == GameVersion)
                ?? VersionSource.FirstOrDefault();
            LoadItemIdsForVersion();
            UpdateItemDataTree();
        }

        public void ItemID_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateItemDataTree();
        }

        public void Version_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (loadingItemIds || SelectedVersion is null) return;
            // 版本只有一个出口（基类）：把旧下拉的选择映射回 mcdoc 的版本选项
            var target = GameVersionOptions.FirstOrDefault(option => option.Name == SelectedVersion.Text);
            if (target is not null) SelectedGameVersionOption = target;
            LoadItemIdsForVersion();
            UpdateItemDataTree();
        }

        #endregion

        #region 生成与保存
        [RelayCommand]
        private void Run() => _ = Run(ShowGeneratorResult);

        /// <summary>
        /// 生成：把当前文档包成命令并按"显示结果"分流；同时把命令留在 <see cref="Result"/> 上，
        /// 供 ItemViewModel / ArmorStandViewModel 这些旧调用方直接取用。
        /// </summary>
        public Task<string> Run(bool showResult)
        {
            var buffer = Create();
            CollectionData(buffer);
            Build(buffer);
            Result = buffer.ToString();
            if (showResult)
            {
                DisplayerView displayer = container.Resolve<DisplayerView>();
                if (displayer is not null && displayer.DataContext is DisplayerViewModel displayerViewModel)
                {
                    displayer.Show();
                    displayerViewModel.GeneratorResult(Result, "物品", iconPath);
                }
            }
            else
            {
                Clipboard.SetText(Result);
                MessagePopup.PushMessage(new GeneratorMessage
                {
                    Message = "物品生成成功！数据已复制",
                    MessageBrush = Brushes.Red,
                    SubMessage = "物品生成器",
                    SubMessageBrush = Brushes.DarkGray,
                    Icon = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\item_frame.png", UriKind.RelativeOrAbsolute))
                });
            }
            return Task.FromResult(Result);
        }

        /// <summary>另存为 .command：内容同样是那条命令（同时留一份到 Resource\Saves\Item\）。</summary>
        [RelayCommand]
        private void Save()
        {
            Microsoft.Win32.SaveFileDialog dialog = new()
            {
                AddExtension = true,
                RestoreDirectory = true,
                CheckPathExists = true,
                DefaultExt = ".command",
                Filter = "Command files (*.command;)|*.command;",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
                Title = "保存为命令文件"
            };
            if (dialog.ShowDialog() != true) return;
            var buffer = Create();
            CollectionData(buffer);
            Build(buffer);
            Result = buffer.ToString();
            string folder = Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                _ = File.WriteAllTextAsync(dialog.FileName, Result);
            }
            string cacheFolder = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Saves\Item\";
            Directory.CreateDirectory(cacheFolder);
            _ = File.WriteAllTextAsync(cacheFolder + Path.GetFileName(dialog.FileName), Result);
        }
        #endregion
        #endregion
    }
}
