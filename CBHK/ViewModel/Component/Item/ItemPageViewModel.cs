using CBHK.CustomControl.VectorComboBox;
using CBHK.Interface.Visual;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using CBHK.Utility.Data;
using CBHK.Utility.Common;
using CBHK.Utility.Data.Mcdoc;
using CBHK.Utility.Visual.MessageTip;
using CBHK.View;
using CBHK.ViewModel.Generator;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CBHK.ViewModel.Component.Item
{
    public partial class ItemPageViewModel(IContainerProvider Container, Resource Resource) : McdocGeneratorViewModelBase(Resource, dispatcherRegistry, null), IPageViewModel
    {
        #region Field
        private const string dispatcherRegistry = "minecraft:item";

        private readonly IContainerProvider container = Container;
        private readonly Resource resource = Resource;
        private readonly string iconPath =
            "pack://application:,,,/CBHK;component/Resource/Common/Image/SpawnerIcon/IconItem.png";

        private bool loadingItemIds;
        #endregion

        #region Property
        public MessagePopup MessagePopup { get; set; } = new();

        /// <summary>
        /// 召唤（掉落物）模式；不勾就是给予。
        /// </summary>
        [ObservableProperty]
        private bool summon;

        /// <summary>
        /// 给予模式下允许纯文本样式（旧调用面保留）。
        /// </summary>
        [ObservableProperty]
        private bool isNoStyleText = true;

        /// <summary>
        /// 显示结果
        /// </summary>
        [ObservableProperty]
        private bool showGeneratorResult;

        /// <summary>
        /// 保存物品ID
        /// </summary>
        [ObservableProperty]
        private VectorIconTextComboBoxItem selectedItem;

        /// <summary>
        /// 物品ID列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<VectorIconTextComboBoxItem> itemList = [];

        /// <summary>
        /// 最近一次生成的命令（旧调用面在读取它）。
        /// </summary>
        public string Result { get; set; }
        #endregion

        #region Method

        #region 搜索当前物品ID对应的JSON对象

        // ── 物品图标：构造期一张都不解码 ────────────────────────────────────────────
        // 1505 个 id 里 1487 个有 png；以前对每个 id 都 new BitmapImage（实测 ~1.0 s，UI 线程、首屏之前）。
        // 现在：进列表只查进程内缓存（命中才给图），页面可交互之后由后台线程按 32px 降采样补齐并分批回填。
        private static readonly Dictionary<string, ImageSource> itemIconCache = new(StringComparer.Ordinal);
        private static HashSet<string> itemIconNames;
        private static string itemIconDirectory;
        private static CancellationTokenSource itemIconFill;

        /// <summary>ImageSet 目录的 png 名单（一次目录列举，替掉每个 id 一次 File.Exists）。</summary>
        private static HashSet<string> ItemIconNames(string directory)
        {
            if (itemIconNames is not null && string.Equals(itemIconDirectory, directory, StringComparison.Ordinal))
            {
                return itemIconNames;
            }
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (Directory.Exists(directory))
                {
                    foreach (var file in Directory.GetFiles(directory, "*.png")) names.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
            catch (Exception)
            {
                // 目录不可读就当"没有图标"：回落到纯文本，不抛
            }
            itemIconNames = names;
            itemIconDirectory = directory;
            return names;
        }

        /// <summary>只查缓存，不读盘（列表构造走这条）。</summary>
        private static ImageSource CachedItemIcon(string id)
            => id is not null && itemIconCache.TryGetValue(id, out var cached) ? cached : null;

        /// <summary>解码一张 30×30 图标：降采样 + OnLoad + 冻结（冻结后才能跨线程交给 UI）。</summary>
        private static ImageSource DecodeItemIcon(string iconPath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 32;          // 行模板里是 30×30，没必要解原图
                bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch (Exception)
            {
                return null;   // 损坏/不支持的 png：这一项就没图标，不抛
            }
        }

        /// <summary>后台补齐这次列表的图标（分批回填；换版本/关页面直接取消上一轮）。</summary>
        private void ScheduleItemIcons(List<VectorIconTextComboBoxItem> targets, HashSet<string> names, string directory)
        {
            itemIconFill?.Cancel();
            var source = new CancellationTokenSource();
            itemIconFill = source;
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is null) return;   // 无 UI 线程（探针/测试）：不加载图标
            var token = source.Token;
            _ = Task.Run(() =>
            {
                var batch = new List<(VectorIconTextComboBoxItem Item, ImageSource Image)>(16);
                void Flush()
                {
                    if (batch.Count == 0) return;
                    var chunk = batch.ToArray();
                    batch.Clear();
                    dispatcher.BeginInvoke(new Action(() =>
                    {
                        foreach (var (item, image) in chunk) item.Image = image;
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
                foreach (var item in targets)
                {
                    if (token.IsCancellationRequested) return;
                    if (item.Image is not null) continue;
                    var id = item.Text;
                    if (id is null || !names.Contains(id)) continue;
                    var cached = CachedItemIcon(id);
                    if (cached is null)
                    {
                        cached = DecodeItemIcon(Path.Combine(directory, id + ".png"));
                        if (cached is not null) itemIconCache[id] = cached;
                    }
                    if (cached is null) continue;
                    batch.Add((item, cached));
                    if (batch.Count >= 16) Flush();
                }
                Flush();
            }, token);
        }

        /// <summary>按当前版本重建物品下拉（id 来自注册表 item，图标在页面可交互之后补齐）。</summary>
        private void LoadItemIdsForVersion()
        {
            var version = SelectedGameVersionOption?.Name;
            if (string.IsNullOrEmpty(version)) return;
            loadingItemIds = true;
            try
            {
                var previous = SelectedItem?.Text;
                itemIconFill?.Cancel();
                ItemList.Clear();
                SelectedItem = null;
                string imageSetFolderPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
                var names = ItemIconNames(imageSetFolderPath);
                var built = new List<VectorIconTextComboBoxItem>();
                foreach (string item in resource.GetIds("item", version))
                {
                    var entry = new VectorIconTextComboBoxItem
                    {
                        // ItemID 必须给：VectorTextComboBoxItem.Equals 只在有 ItemID 时才自反/对称，
                        // 否则 WPF 的 Selector 校验"选中项在不在集合里"会失败，代码设的选中项直接被丢掉
                        ItemID = item,
                        Image = CachedItemIcon(item),   // 只查缓存：首屏不再解码任何 png
                        Text = item
                    };
                    built.Add(entry);
                    ItemList.Add(entry);
                }
                SelectedItem = ItemList.FirstOrDefault(candidate => candidate.Text == previous) ?? ItemList.FirstOrDefault();
                // 选中项那一张同步给（收起状态的下拉会画它，别让它先空一拍）；耗时 ~1 ms
                if (SelectedItem is { Image: null } shown && shown.Text is { } shownId && names.Contains(shownId))
                {
                    var icon = CachedItemIcon(shownId) ?? DecodeItemIcon(imageSetFolderPath + shownId + ".png");
                    if (icon is not null)
                    {
                        itemIconCache[shownId] = icon;
                        shown.Image = icon;
                    }
                }
                // 其余的等首屏出来再在后台补（切版本会取消上一轮）
                ScheduleItemIcons(built, names, imageSetFolderPath);
            }
            finally
            {
                loadingItemIds = false;
            }
        }

        /// <summary>
        /// 窗口上的"从剪切板/文件导入"走这里：把 give 命令里的 NBT 直接灌进 mcdoc 文本，
        /// 之后由引擎（文本 → 防抖 → 重建）同步到左侧树 —— 不再造旧的物品页面。
        /// 返回 false 表示命令里没有可用的 NBT（调用方负责提示）。
        /// </summary>
        public bool ImportFromCommand(string textOrPath, bool isPath = false, Action<string> report = null)
        {
            var data = ExternalCommandParser.Read(textOrPath, isPath);
            if (!ExternalCommandParser.TryGetItemNbt(data, out var nbt, out var itemId, out var summon))
            {
                report?.Invoke("文件内容格式不合法");
                return false;
            }
            var version = SelectedGameVersionOption?.Name;
            if (string.IsNullOrEmpty(version))
            {
                report?.Invoke("还没选版本，先选一个版本再导入");
                return false;
            }
            // 命令里的 id 带命名空间（minecraft:x），下拉候选是裸 id：归一之后再找。
            // 找不到就什么都不做 —— 灌进"上一次选中的"那一支 schema 比不导入更糟。
            var bare = ExternalCommandParser.StripNamespace(itemId);
            var target = string.IsNullOrEmpty(bare)
                ? null
                : ItemList.FirstOrDefault(item => string.Equals(item?.Text, bare, StringComparison.Ordinal));
            if (target is null)
            {
                // 命令里的 id 不在当前版本的物品注册表里：不猜 schema，直接说清楚
                report?.Invoke("命令里的物品不在注册表里：" + (string.IsNullOrEmpty(bare) ? "<没有 id>" : bare));
                return false;
            }
            Summon = summon;
            // 选中项一变，视图的 SelectionChanged 会回调 UpdateItemDataTree：把 NBT 挂成"待落盘"，
            // 由那一条出口带进 dispatcher，否则会先按默认文档建一次、把导入内容冲掉。
            pendingImportNbt = nbt;
            SelectedItem = target;
            // 选中项本来就是这个物品时不会触发事件：这里再走一次，保证 NBT 一定落盘
            UpdateItemDataTree();
            return true;
        }

        /// <summary>导入时"待落盘"的 NBT：选中项变化引起的那次重建要把它一起带进去。</summary>
        private string pendingImportNbt;

        /// <summary>已经按哪个物品开过树：同样的选择不再重建（否则会把导入的 NBT 冲成默认文档）。</summary>
        private string openedItemId;

        /// <summary>换物品：按 dispatcher 重新开一棵树（schema 决定这一页显示什么）。</summary>
        public void UpdateItemDataTree()
        {
            var version = SelectedGameVersionOption?.Name;
            if (loadingItemIds || SelectedItem is null || string.IsNullOrEmpty(version)) return;
            var json = pendingImportNbt;
            pendingImportNbt = null;
            if (json is null && string.Equals(SelectedItem.Text, openedItemId, StringComparison.Ordinal)) return;
            openedItemId = SelectedItem.Text;
            OpenMcdocTreeFromDispatcher(version, dispatcherRegistry,
                McdocIndex.Static(SelectedItem.Text), resource.McdocTypes, json);
        }

        #endregion

        public StringBuilder Create() => new();

        /// <summary>
        /// 把缓冲里的物品 NBT 包成命令：**给予**（默认）或 **召唤掉落物**。
        /// - 给予 1.13+：`give @p minecraft:&lt;id&gt;{&lt;nbt&gt;} 1`（没有 NBT 就不带花括号）
        /// - 给予 ≤1.12：`give @p minecraft:&lt;id&gt; 1 0 {&lt;nbt&gt;}`
        /// - 召唤：`summon item ~ ~ ~ {Item:{id:"minecraft:&lt;id&gt;",Count:1b,tag:{&lt;nbt&gt;}}}`
        /// </summary>
        public void BuildResult(StringBuilder Result)
        {
            if (Result is null) return;
            var body = Result.ToString().Trim().Replace("\r", "").Replace("\n", "");
            var item = SelectedItem?.Text ?? "stone";
            var legacy = VersionRangeChecker.IsLessThan(SelectedGameVersionOption?.Name ?? "", "1.13");
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

        #region Event

        public void ItemPages_Loaded(object sender, RoutedEventArgs e)
        {
            LoadItemIdsForVersion();
            UpdateItemDataTree();
        }

        public void ItemID_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateItemDataTree();
        }

        public void Version_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (loadingItemIds) return;
            LoadItemIdsForVersion();
            UpdateItemDataTree();
        }

        [RelayCommand]
        private void Run() => _ = Run(ShowGeneratorResult);

        /// <summary>
        /// 生成：把当前文档包成命令并按"显示结果"分流；同时把命令留在 <see cref="Result"/> 上，
        /// 供 ItemViewModel / ArmorStandViewModel 这些旧调用方直接取用。
        /// </summary>
        public Task<string> Run(bool showResult)
        {
            var buffer = Create();
            BuildResult(buffer);
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

        /// <summary>
        /// 另存为 .command：内容同样是那条命令（同时留一份到 Resource\Saves\Item\）。
        /// </summary>
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
            BuildResult(buffer);
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
    }
}