using CBHK.CustomControl.VectorComboBox;
using CBHK.Interface.Visual;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using CBHK.Model.Data;
using CBHK.Utility.Data;
using CBHK.Utility.Data.Mcdoc;
using CBHK.View;
using CBHK.ViewModel;
using CBHK.Utility.Visual.MessageTip;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using Prism.Ioc;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CBHK.ViewModel.Component.Entity
{
    public partial class EntityPageViewModel(IContainerProvider Container, Resource Resource) : Generator.McdocGeneratorViewModelBase(Resource, dispatcherRegistry, null), IPageViewModel
    {
        #region Field
        private const string dispatcherRegistry = "minecraft:entity";
        private readonly IContainerProvider container = Container;
        private readonly Resource resource = Resource;
        private string iconPath = @"pack://application:,,,/CBHK;component/Resource/Common/Image/SpawnerIcon/IconEntity.png";
        private bool loadingEntityIds;
        #endregion

        #region Property
        public MessagePopup MessagePopup { get; set; } = new();

        #region 给予与召唤
        /// <summary>生成方式：true = 给予（刷怪蛋），false = 召唤。</summary>
        [ObservableProperty]
        private bool give;

        /// <summary>召唤与否（视图里与 Give 互斥的一组按钮）。</summary>
        [ObservableProperty]
        private bool summon = true;

        #endregion

        #region 显示结果
        [ObservableProperty]
        private bool showResult;
        #endregion

        #region 实体ID列表

        [ObservableProperty]
        private VectorTextComboBoxItem selectedEntityId;

        [ObservableProperty]
        private ObservableCollection<VectorIconTextComboBoxItem> entityIDList = [];
        #endregion

        #region 作为工具或引用
        [ObservableProperty]
        private bool useForTool;

        [ObservableProperty]
        private bool useForReference;

        #endregion

        #region 导入外部数据模式
        /// <summary>外部导入模式：导入的实体数据由 mcdoc 树承载（不再是 NBT 面板）。</summary>
        [ObservableProperty]
        private bool importMode;

        /// <summary>外部读号进来的实体数据（导入模式用）。</summary>
        public JObject ExternallyReadEntityData { get; set; }

        #endregion

        #region 是否同步到文件
        public bool SyncToFile { get; set; }
        public string ExternFilePath { get; set; }

        /// <summary>页面上"是否有自定义名"的开关（旧调用面在用）。</summary>
        public bool HaveCustomName { get; set; }

        #endregion

        #endregion

        #region Method
        #region 搜索当前实体ID对应的JSON对象
        /// <summary>
        /// 按当前版本重建实体下拉（id 来自注册表 entity_type，A-Z 排序，带刷怪蛋图标）。
        /// </summary>
        private void LoadEntityIdsForVersion()
        {
            string version = SelectedGameVersionOption?.Name ?? GameVersion;
            if (string.IsNullOrEmpty(version)) return;
            loadingEntityIds = true;
            try
            {
                var previous = SelectedEntityId?.Text;
                EntityIDList.Clear();
                SelectedEntityId = null;
                string entityImageFolderPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
                foreach (string item in resource.GetIds("entity_type", version))
                {
                    string iconPath = File.Exists(entityImageFolderPath + item + "_spawn_egg.png")
                        ? entityImageFolderPath + item + "_spawn_egg.png"
                        : entityImageFolderPath + item + ".png";
                    EntityIDList.Add(new VectorIconTextComboBoxItem
                    {
                        Image = File.Exists(iconPath) ? new BitmapImage(new Uri(iconPath, UriKind.Absolute)) : null,
                        Text = item
                    });
                }
                SelectedEntityId = EntityIDList.FirstOrDefault(candidate => candidate.Text == previous) ?? EntityIDList.FirstOrDefault();
            }
            finally
            {
                loadingEntityIds = false;
            }
        }

        /// <summary>
        /// 切实体：按 dispatcher 重新开一棵树（schema 决定这一页显示什么）。
        /// </summary>
        public void UpdateEntityDataTree()
        {
            if (loadingEntityIds || SelectedEntityId is null || string.IsNullOrEmpty(GameVersion)) return;
            OpenMcdocTreeFromDispatcher(GameVersion, dispatcherRegistry,
                McdocIndex.Static(SelectedEntityId.Text), resource.McdocTypes);
        }

        #endregion

        #region 旧调用面（命令拼装）
        /// <summary>旧"开一个空缓冲"入口：命令拼装从空串开始。</summary>
        public StringBuilder Create() => new();

        /// <summary>旧"收集数据"入口：实体数据现在就在 JsonText（mcdoc 文档）里，直接交出去。</summary>
        public void CollectionData(StringBuilder Result) => Result?.Append(JsonText ?? "");

        /// <summary>
        /// 把缓冲里的实体数据包成命令（实体页的业务：**召唤** 与 **给予刷怪蛋** 两种模式）：
        /// - 召唤：`summon minecraft:&lt;id&gt; ~ ~ ~ {&lt;数据&gt;}`
        /// - 给予：1.13+ `give @p minecraft:pig_spawner_egg{EntityTag:{id:"minecraft:&lt;id&gt;",&lt;数据&gt;}} 1`；
        ///   1.12 及以前是 `spawner_egg`，带自定义名时改成"右键执行的牌子"（旧实现的同一套写法）。
        /// </summary>
        public void BuildResult(StringBuilder Result)
        {
            if (Result is null) return;
            var body = Result.ToString().Trim();
            var entity = SelectedEntityId?.Text ?? "entity";
            var legacy = VersionRangeChecker.IsLessThan(GameVersion, "1.13");
            var hasCustomName = HaveCustomName
                || (JsonText?.Contains("\"CustomName\"", StringComparison.Ordinal) ?? false);
            var tag = body.Length > 0 ? "," + body : "";
            string command;
            if (!Give)
            {
                command = body.Length > 0
                    ? "summon minecraft:" + entity + " ~ ~ ~ {" + body + "}"
                    : "summon minecraft:" + entity + " ~ ~ ~";
                if (legacy && hasCustomName) command = RightClickSignCommand(command);
            }
            else if (legacy)
            {
                command = hasCustomName
                    ? RightClickSignCommand("give @p minecraft:spawner_egg 1 0 {EntityTag:{id:\"minecraft:" + entity + "\"" + tag + "}}")
                    : "give @p minecraft:spawner_egg 1 0 {EntityTag:{id:\"minecraft:" + entity + "\"" + tag + "}}";
            }
            else
            {
                command = "give @p minecraft:pig_spawner_egg{EntityTag:{id:\"minecraft:" + entity + "\"" + tag + "}} 1";
            }
            command = command.Replace("\\n","").Replace("\\r","");
            Result.Clear();
            Result.Append(command);
        }

        /// <summary>
        /// 1.13 以前"右键执行"的牌子：把命令塞进牌子的 run_command 点击事件里（与旧实现同形）。
        /// </summary>
        private static string RightClickSignCommand(string command)
        {
            var value = "\"/setblock ~ ~ ~ minecraft:command_block 0 replace {Command:" + Quoted(command) + "}\"";
            var text = "{\"text\":\"Right Click To Run\",\"clickEvent\":{\"action\":\"run_command\",\"value\":" + value + "}}";
            return "give @p minecraft:sign 1 0 {BlockEntityTag:{Text1:" + Quoted(text) + "}}";
        }

        /// <summary>
        /// 1.13 以前的 NBT 字符串写法：外层双引号、内部引号与反斜杠转义。
        /// </summary>
        private static string Quoted(string text)
            => "\"" + (text ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        #endregion

        #endregion

        #region Event
        #region 更新子控件的版本以及执行数据更新事件
        public void EntityPages_Loaded(object sender, RoutedEventArgs e)
        {
            LoadEntityIdsForVersion();
            UpdateEntityDataTree();
        }

        public void EntityIDBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateEntityDataTree();
        }

        public void Version_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 版本切换本身由基类收口（重建会话 + 同一份 JSON 重解）；这里只跟着换实体 id 列表
            if (loadingEntityIds) return;
            LoadEntityIdsForVersion();
            UpdateEntityDataTree();
        }

        #endregion

        #region 生成与保存
        [RelayCommand]
        /// <summary>
        /// 运行生成
        /// </summary>
        private void Run()
        {
            StringBuilder Result = Create();
            CollectionData(Result);
            BuildResult(Result);
            if (ShowResult)
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
                string entityID = SelectedEntityId is not null ? SelectedEntityId.Text : "entity";
                MessagePopup.PushMessage(new GeneratorMessage()
                {
                    Message = "实体生成成功！数据已复制",
                    MessageBrush = Brushes.Red,
                    SubMessage = "实体生成器",
                    SubMessageBrush = Brushes.DarkGray,
                    Icon = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\ImageSet\" + entityID + ".png", UriKind.Relative))
                });
            }
        }

        /// <summary>另存为 .command：内容同样是那条命令（同时留一份到 Resource\Saves\Entity\）。</summary>
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
            StringBuilder Result = Create();
            CollectionData(Result);
            BuildResult(Result);
            string folder = Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                _ = File.WriteAllTextAsync(dialog.FileName, Result.ToString());
            }
            string cacheFolder = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Saves\Entity\";
            Directory.CreateDirectory(cacheFolder);
            _ = File.WriteAllTextAsync(cacheFolder + Path.GetFileName(dialog.FileName), Result.ToString());
        }
        #endregion
        #endregion
    }
}