using System;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CBHK.Model.Constant;
using CBHK.Utility.Data;
using CBHK.Utility.Data.Mcdoc;
using CBHK.Utility.Data.JsonText;
using CommunityToolkit.Mvvm.ComponentModel;
using MinecraftLanguageModelLibrary.Data;
using Newtonsoft.Json.Linq;

namespace CBHK.ViewModel.Generator
{
    /// <summary>
    /// 生成器页面 VM 的公共骨架：引擎就位后，各页面只差一个 dispatcher 入口名。
    /// 负责：建会话（符号索引来自 Resource）+ 默认 JSON + 行投影 + 值/结构回写。
    /// </summary>
    public abstract partial class McdocGeneratorViewModelBase : ObservableObject, IMcdocJsonEditorHost, IMcdocDocumentActions
    {
        private readonly Resource resource;
        private readonly McdocRowProjector projector = new();

        private McdocTreeDocument document;

        /// <summary>视图绑定这个集合（可见节点投影成的行）。</summary>
        public McdocRowCollection Rows => projector.Rows;

        /// <summary>dispatcher 入口名（如 advancement / dimension_type / loot_table）。</summary>
        public string EntryId { get; }

        /// <summary>
        /// #[since]/#[until] 过滤用的游戏版本（McdocAttributeRegistry 在每次 Load 时按它重建）。
        /// 改动后调用 LoadMcdocTree 生效；页面暂时都固定 1.21。
        /// </summary>
        protected string GameVersion { get; set; } = "1.21";

        /// <summary>页面 chrome 的版本下拉候选（运行数据里实际可用的注册表版本，倒序）。</summary>
        public ObservableCollection<EnumMember> GameVersionOptions { get; } = [];

        /// <summary>版本下拉的选中项：改它就换版本重新求值（misode 的版本选择器同款），文档内容保留。</summary>
        [ObservableProperty]
        private EnumMember selectedGameVersionOption;

        partial void OnSelectedGameVersionOptionChanged(EnumMember value)
        {
            if (value?.Name is { Length: > 0 } version)
            {
                ApplyGameVersion(version);
            }
        }

        /// <summary>
        /// 换版本：重建会话（McdocAttributeRegistry 与 pack_format 默认值都按 GameVersion 走），
        /// 再用同一份 JSON 重新建树 —— misode 切版本时也不重置用户文档。
        /// </summary>
        private void ApplyGameVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version) || string.Equals(version, GameVersion, StringComparison.Ordinal))
            {
                return;
            }
            GameVersion = version;
            var text = JsonText;
            CreateDocument();
            if (string.IsNullOrWhiteSpace(text))
            {
                LoadDefault();
                return;
            }
            try
            {
                LoadMcdocTree(text);
                MdocStatus = "已切换到 " + version + " | mcdoc | " + EntryId
                    + " | 字段 " + document.Nodes.Count + " 个顶层";
            }
            catch (Exception error)
            {
                // 文本正在编辑中（非法 JSON）时切版本不能把页面打崩：保住旧树，只报状态
                MdocStatus = "切换到 " + version + " 失败：" + error.Message;
            }
        }

        /// <summary>版本下拉候选：注册表数据里有的版本（降序），当前版本不在其中时补在最前，保证不出现空下拉。</summary>
        private void FillGameVersionOptions()
        {
            GameVersionOptions.Clear();
            var versions = new List<string>();
            foreach (var property in resource?.RunningDataObject?.Properties() ?? Enumerable.Empty<JProperty>())
            {
                if (Version.TryParse(property.Name, out _) && !versions.Contains(property.Name))
                {
                    versions.Add(property.Name);
                }
            }
            versions.Sort((left, right) => Version.Parse(right).CompareTo(Version.Parse(left)));
            // 默认版本跟注册表走：写死 1.21 会把 #[since="1.21.x"] 的字段静默隐掉（页面看起来"少字段"）
            if (versions.Count > 0 && !versions.Contains(GameVersion))
            {
                GameVersion = versions[0];
            }
            if (!versions.Contains(GameVersion))
            {
                versions.Insert(0, GameVersion);
            }
            foreach (var version in versions)
            {
                GameVersionOptions.Add(new EnumMember
                {
                    Name = version,
                    Value = new MetaValue { LiteralValue = version },
                });
            }
            // 走属性：赋值时 GameVersion 还没变，OnSelectedGameVersionOptionChanged 会按"同版本"直接返回
            SelectedGameVersionOption = GameVersionOptions.FirstOrDefault(item => item.Name == GameVersion);
        }

        /// <summary>
        /// Resource.Init() 是异步的（236 个 mcdoc 走命名管道），页面可能在它完成前就被点开：
        /// 那时注册表数据还没进 RunningDataObject，版本下拉只有一项、#[id] 字段也会退化成文本框。
        /// 这里最多轮询 15 秒，数据一到就补候选并按新数据重建一次（只做一次，之后靠用户操作）。
        /// </summary>
        private void ScheduleRegistriesRefresh()
        {
            if (resource is null || resource.RunningDataObject is null || resource.RunningDataObject.Count > 0)
            {
                return;
            }
            var attempts = 0;
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += (_, _) =>
            {
                var ready = resource.RunningDataObject is { Count: > 0 };
                if (!ready && ++attempts <= 15)
                {
                    return;
                }
                timer.Stop();
                if (!ready)
                {
                    return;
                }
                FillGameVersionOptions();
                var text = JsonText;
                if (string.IsNullOrWhiteSpace(text))
                {
                    return;
                }
                CreateDocument();
                try
                {
                    LoadMcdocTree(text);
                    MdocStatus = "注册表数据就绪，已按完整数据重建 | " + MdocStatus;
                }
                catch (Exception error)
                {
                    MdocStatus = "注册表数据就绪后重建失败：" + error.Message;
                }
            };
            timer.Start();
        }

        [ObservableProperty]
        private string mdocStatus = "";

        /// <summary>当前 JSON 文本：编辑器输入会触发重建，树上编辑也会回写这里。</summary>
        [ObservableProperty]
        private string jsonText = "";

        private System.Windows.Threading.DispatcherTimer reloadTimer;
        /// <summary>回写文本时置位，避免"文本变化 → 重建 → 回写 → 再重建"的环。</summary>
        private bool suppressReload;

        /// <summary>
        /// 建树 / 局部重建期间置位。构建期给节点赋值（枚举默认项、bool 初值、union 分支镜像）也会触发
        /// NodeValueChanged，此时若按"用户编辑"回写，就会把可选字段凭空插进用户的 JSON。
        /// </summary>
        private bool loading;

        /// <summary>编辑器里改了文本：防抖 300ms 后重建树。</summary>
        partial void OnJsonTextChanged(string value)
        {
            if (suppressReload) return;
            ScheduleReload();
        }

        /// <summary>
        /// 编辑器文本 → VM 的通道。AvalonEdit 的 Text 不是依赖属性，没法用 Binding，
        /// 由 McdocJsonEditorHost 在 Attach 时挂 TextChanged 回调进来。
        /// </summary>
        public void OnJsonEditorTextChanged(string text)
        {
            if (suppressReload) return;
            if (text == JsonText) return;
            if (text == lastSyncedText)
            {
                textPendingSync = false;
                return;
            }
            // 用户输入还没被树消化：这期间树侧任何"整篇重排"都会把它顶回去
            textPendingSync = true;
            PushJsonText(text);      // 置 suppressReload，避免 OnJsonTextChanged 重复调度
            ScheduleReload();
        }

        /// <summary>
        /// 把编辑器里挂着的文本吃进树（树侧要写文本之前先调它）。
        /// 返回 false = 文本现在是语法错误，树侧必须放弃这次回写，不许把旧 JSON 顶回去。
        /// </summary>
        private bool DigestEditorText()
        {
            if (jsonEditor is null || digestingText) return !textParseFailed;
            var text = jsonEditor.Text;
            if (!McdocJsonParser.TryParse(text, out _, out var syntaxError))
            {
                textParseFailed = true;
                textPendingSync = true;
                ShowSyntaxError(syntaxError);
                return false;
            }
            textParseFailed = false;
            var wasDigesting = digestingText;
            digestingText = true;
            try
            {
                ApplyTextToTree(text, null);
            }
            finally
            {
                digestingText = wasDigesting;
            }
            return true;
        }

        private void ScheduleReload()
        {
            if (reloadTimer is null && !EnsureTimer()) return;
            // 小文档 120ms 就够；大文档（连续输入 + 长文本）放宽到 300ms，避免每次击键都 diff 整篇
            reloadTimer.Interval = TimeSpan.FromMilliseconds((JsonText?.Length ?? 0) > 40000 ? 300 : 120);
            reloadTimer.Stop();
            reloadTimer.Start();
        }

        /// <summary>最近一次"树 ↔ 文本"已经同步过的文本：编辑器回调命中它就直接返回（抑制环 + 省一次 diff）。</summary>
        private string lastSyncedText;

        /// <summary>编辑器里还有树没消化的文本（用户正在打字 / 防抖还没跑）。</summary>
        private bool textPendingSync;

        /// <summary>上一次解析失败（右侧 JSON 现在是语法错误）。</summary>
        private bool textParseFailed;

        /// <summary>正在把编辑器文本吃进树：期间的树侧回写不再触发二次消化。</summary>
        private bool digestingText;

        /// <summary>
        /// 文本 → 树：先校验（调用方已做），再走增量同步（值快路径 / 子树重建）。
        /// 差量不可用（补丁过多、路径对不上）时回退整树重建。
        /// </summary>
        private void ApplyTextToTree(string text, McdocJsonValue parsedText)
        {
            if (document is null)
            {
                LoadMcdocTree(text);
                return;
            }
            // 文本没有真的变（树编辑回写引起的排版）：不要重解析，更不要换 AST
            if (text == lastSyncedText) return;
            var newRoot = parsedText;
            if (jsonEditor is not null)
            {
                syntaxOverlay?.Clear();
                // 快照可能落后于编辑器（防抖 tick 读的是 VM 副本）：绝不能用旧文本覆盖编辑器/AST，
                // 一律以编辑器当前文本为准重解析 —— "重解析只能来自当前文本"
                var liveText = jsonEditor.Text ?? "";
                if (!string.Equals(liveText, text, StringComparison.Ordinal))
                {
                    CBHK.Utility.Data.Mcdoc.McdocWriteTrace.Log("消化文本快照过期", null,
                        "快照=" + (text?.Length ?? 0) + " 编辑器=" + liveText.Length + "，以编辑器为准");
                    text = liveText;
                }
                jsonEditor.State.Reparse();
                newRoot = jsonEditor.State.Root;
            }

            McdocSyncStats stats;
            bool applied;
            // 根层成员差分会在同步期间**新建节点**（新键 / 未声明键）：那些 Prepare 里的赋值不是用户编辑，
            // 不许写回文本（"建树期不能回写文本"）。整段用 loading 压住，跟 RebuildTargeted 一致。
            var wasSyncLoading = loading;
            loading = true;
            try
            {
                applied = McdocSyncEngine.TryApply(document, newRoot, out stats) && !stats.Fallback;
            }
            finally
            {
                loading = wasSyncLoading;
            }
            if (applied)
            {
                var wasLoading = loading;
                loading = true;
                try
                {
                    document.Validate();
                }
                finally
                {
                    loading = wasLoading;
                }
                foreach (var node in stats.PatchedNodes)
                {
                    projector.Find(node)?.RefreshHead();
                }
                // 根层成员增删：顶层那一段（被重建的顶层节点 / 新插的 / 被摘掉的）按上一代清单分段重拼；
                // 其余重建节点仍然只重拼父行。两步都不做整表 Reset。
                var splicedTopLevel = stats.PreviousTopLevel is not null;
                if (splicedTopLevel)
                {
                    projector.SpliceTopLevels(stats.PreviousTopLevel, document.Nodes.ToList());
                }
                foreach (var node in stats.RebuiltNodeList)
                {
                    if (!projector.RefreshAround(node) && !splicedTopLevel)
                    {
                        projector.Reset(document.Nodes.ToList());
                    }
                }
                // 重建了节点却没拿到对应的行（局部拼接没覆盖到）：整表重置，绝不让投影停在上一代
                if (!splicedTopLevel && stats.RebuiltNodes > 0 && stats.RebuiltNodeList.Count != stats.RebuiltNodes)
                {
                    projector.Reset(document.Nodes.ToList());
                }
                textPendingSync = false;
                textParseFailed = false;
                // 消化过程中树侧可能已经整篇回写过（物化必填键 / 重建后重排）：以编辑器当前文本为准，
                // 拿进来时的快照回写会让 JsonText 与屏幕上的 JSON 不同代（复制/保存导出的是上一份）
                PushJsonText(jsonEditor?.Text ?? text);
                lastSyncedText = JsonText;
                return;
            }
            LoadMcdocTree(text);
            lastSyncedText = JsonText;
        }

        private bool EnsureTimer()
        {
            reloadTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(120)
            };
            reloadTimer.Tick += (_, _) =>
            {
                reloadTimer.Stop();
                try
                {
                    SyncTextToTree();
                }
                catch (Exception error)
                {
                    MdocStatus = "解析失败：" + error.Message;
                }
            };
            return true;
        }

        /// <summary>
        /// 文本 → 树的唯一出口（防抖 tick 与门禁探针共用）：解析 + 差量同步 + 投影收敛。
        /// 返回 false = 文本还是语法错误（树按设计一动不动）。
        /// </summary>
        public bool SyncTextToTree()
        {
            // 优先读编辑器当前文本：VM 的 JsonText 副本可能落后（落后就会用旧文本重解析）
            var text = jsonEditor is not null ? jsonEditor.Text : JsonText;
            if (string.IsNullOrWhiteSpace(text)) return false;
            // 语法检查用 System.Text.Json（TryParse 里就是它）：错误就只画波浪线 + 状态，树一动不动
            if (!McdocJsonParser.TryParse(text, out var parsedText, out var syntaxError))
            {
                ShowSyntaxError(syntaxError);
                return false;
            }
            syntaxOverlay?.Clear();
            ApplyTextToTree(text, parsedText);
            // 消化完最后一次落地：AST 与文本、投影与文档都必须同代
            EnsureTextGeneration();
            EnsureProjectionMatchesDocument();
            return true;
        }

        /// <summary>树 → 文本：不走防抖，直接回写（并抑制反向重建）。</summary>
        private JsonTextEditor jsonEditor;
        /// <summary>JSON 语法错误的波浪线（复用 WaveUnderlineBackgroundRenderer）。</summary>
        private McdocJsonSyntaxErrorOverlay syntaxOverlay;

        private void ShowSyntaxError(McdocJsonParser.SyntaxError error)
        {
            if (error is null) return;
            syntaxOverlay?.Show(error);
            MdocStatus = "JSON 语法错误：第 " + error.Line + " 行第 " + error.Column + " 列 — " + error.Message;
        }

        /// <summary>页面把 JSON 编辑器交给 VM：之后值编辑只重排文本，不整树重建。</summary>
        public void AttachJsonEditor(JsonTextEditor editor)
        {
            if (!ReferenceEquals(jsonEditor, editor))
            {
                syntaxOverlay?.Clear();
                syntaxOverlay = null;
            }
            jsonEditor = editor;
            syntaxOverlay ??= new McdocJsonSyntaxErrorOverlay(editor);
            // 挂载时把当前 JSON 灌进编辑器（原来靠 Binding 的单向推送，现在改为显式）
            if (!string.IsNullOrEmpty(JsonText))
            {
                suppressReload = true;
                try
                {
                    if (editor.Text != JsonText) editor.Reload(JsonText);
                }
                finally
                {
                    suppressReload = false;
                }
            }
            if (!string.IsNullOrEmpty(JsonText))
            {
                // 编辑器已经解析了一份 AST：让树改用同一份，否则树上改的值与文本里的 JSON 是两棵树，
                // Reformat 输出永远是挂载那一刻的内容
                LoadMcdocTree(JsonText);
            }
            else
            {
                SyncJsonText();
            }
            ScheduleRegistriesRefresh();
        }

        /// <summary>整篇写回串行化：写回过程中又被请求时只记下来，外层收尾时补跑一次（取最后一次状态）。</summary>
        private bool syncInFlight;
        private bool syncPending;

        /// <summary>树 → 文本：一拍内只允许一次整篇写回，写回过程中再来的请求合并到收尾那一次。</summary>
        private void SyncJsonText()
        {
            if (syncInFlight)
            {
                syncPending = true;
                return;
            }
            syncInFlight = true;
            try
            {
                SyncJsonTextOnce();
                if (!syncPending) return;
                syncPending = false;
                SyncJsonTextOnce();
            }
            finally
            {
                syncInFlight = false;
                syncPending = false;
            }
        }

        /// <summary>写回之后文本必须仍是一个根：不许靠事后修，必须留痕。</summary>
        /// <summary>
        /// AST 与文本必须同代：不同代就按当前文本重解析（并留痕）。这是"重解析只能来自当前文本"的执行点。
        /// </summary>
        private int lastTextGeneration = -1;
        private int lastProjectionGeneration = -1;

        private void EnsureTextGeneration()
        {
            if (jsonEditor is null) return;
            // 没换代就不可能不同代：O(1) 跳过（大文档上每次防抖都扫全树是纯浪费）
            if (jsonEditor.State.Generation == lastTextGeneration) return;
            lastTextGeneration = jsonEditor.State.Generation;
            if (jsonEditor.State.IsInStepWithDocument) return;
            CBHK.Utility.Data.Mcdoc.McdocWriteTrace.Log("AST 与文本不同代，按当前文本重解析", null,
                "AST文本长=" + (jsonEditor.State.RootText?.Length ?? 0) + " 编辑器=" + (jsonEditor.Text ?? "").Length);
            jsonEditor.State.Reparse();
        }

        private void VerifyJsonTextGeneration(string source)
        {
            if (!CBHK.Utility.Data.Mcdoc.McdocWriteTrace.Enabled) return;   // 诊断：不进生产热路径
            if (string.IsNullOrWhiteSpace(JsonText)) return;
            if (McdocJsonParser.TryParse(JsonText, out _, out var syntaxError)) return;
            MdocStatus = "文本与树不同代（" + source + "）：" + syntaxError.Message;
            CBHK.Utility.Data.Mcdoc.McdocWriteTrace.Log(source + " 写回后文本不可解析", null,
                syntaxError.Message + " | 长度=" + JsonText.Length);
        }

        private void SyncJsonTextOnce()
        {
            // 树 → 文本：这一刻两边是一致的，记下来（用户输入不会被误判成"已同步"）
            if (jsonEditor is not null)
            {
                EnsureTextGeneration();
                // 有未消化的输入：先吃进树再回写；吃不进去（语法错误）就放弃 ——
                // 以前这里无条件 Reformat，用户删掉整行 + 末尾逗号的编辑会被旧 JSON 立刻顶回去
                if ((textPendingSync || textParseFailed) && !DigestEditorText())
                {
                    MdocStatus = "右侧 JSON 有语法错误，已跳过同步（改对后会自动接上）";
                    return;
                }
                // 自己重排文本不是"用户在编辑器里输入"：整段压住回写通道。
                // 否则 Reformat 抬起的 TextChanged 会撞上旧的 JsonText → 防抖重建，
                // 把行实例整批换掉（滚动/展开/选中全丢，ui-gate S3 的行原位断言直接红）。
                suppressReload = true;
                try
                {
                    // 整篇替换会让语法波浪线的锚点失效（下一次绘制直接抛异常），先摘掉
                    syntaxOverlay?.Clear();
                    jsonEditor.Reformat();
                    JsonText = jsonEditor.Text;
                }
                finally
                {
                    suppressReload = false;
                }
                lastSyncedText = JsonText;
                EnsureProjectionMatchesDocument();
                VerifyJsonTextGeneration("SyncJsonText");
                CBHK.Utility.Data.Mcdoc.McdocWriteTrace.LogText("SyncJsonText 编辑器文本",
                    JsonText, true);
                CBHK.Utility.Data.Mcdoc.McdocWriteTrace.LogText("SyncJsonText document.RootJson 键",
                    CBHK.Utility.Data.Mcdoc.McdocWriteTrace.Keys(document.RootJson));
                return;
            }
            PushJsonText(McdocJsonFormatter.Format(document.RootJson));
            lastSyncedText = JsonText;
        }

        /// <summary>正在把"右侧脏区域"对齐成合法文本：期间不再触发新的修复/同步。</summary>
        private bool resolvingText;

        /// <summary>
        /// 树侧操作撞上右侧语法错误时的唯一修复出口（README §2.7 的契约）：
        /// 定位"能包含住脏区域的最小完整结构"→ 用左侧（模型）里同一份结构整段替换 → 严格解析验收 → 整篇写回 → 排版。
        /// 不猜字符、不补逗号引号；定位不到可替换的结构就返回 false（调用方保持"只动模型、不动文本"的旧行为）。
        /// 值编辑必须在调用前先把值落进模型（ApplyNodeValue），否则替换出去的还是旧值。
        /// </summary>
        private bool TryResolveDirtyText(McdocTreeNode edited)
        {
            if (jsonEditor is null || resolvingText || loading) return false;
            if (!textParseFailed && !textPendingSync) return false;
            // 用户正在右侧打字：这一刻不动他的文本（与"推迟重建"同一条政策）
            if (IsTextEditorFocused()) return false;
            var text = jsonEditor.Text ?? "";
            // 文本其实是合法的（只是还没消化）：交给既有的消化/同步通道
            if (McdocJsonParser.TryParse(text, out _, out var error)) return false;

            var offset = error is null ? text.Length : McdocJsonRegions.OffsetOf(text, error.Line, error.Column);
            if (!McdocJsonRegions.TryFindEnclosing(text, offset, out var dirty)) return false;

            // 本次改动落在脏容器里 → 只换这一段（容器外的用户文本原样保留）；
            // 落在外面 → 整篇以模型为准（两侧只能二选一，硬拼会写出半成品）。
            var editedSegments = SegmentsOf(edited);
            var localMerge = McdocJsonRegions.CommonPrefixLength(dirty.Segments, editedSegments) == dirty.Segments.Count;
            string resolved = null;
            if (localMerge)
            {
                var replacement = McdocJsonRegions.Resolve(document.RootJson, dirty.Segments);
                if (replacement is not null) resolved = McdocJsonRegions.Splice(text, dirty, replacement);
            }
            resolved ??= McdocJsonFormatter.Format(document.RootJson);
            // 写出去之前再验收一次：宁可不动，也不给用户一份半成品
            if (!McdocJsonParser.TryParse(resolved, out _, out _)) return false;

            resolvingText = true;
            try
            {
                // 整篇替换会杀掉语法波浪线的锚点，先摘掉
                syntaxOverlay?.Clear();
                suppressReload = true;
                try
                {
                    jsonEditor.Reload(resolved);      // 换一代 AST；写完校验与锚点重建都在 WriteWholeText 里
                }
                finally
                {
                    suppressReload = false;
                }
                JsonText = jsonEditor.Text;
                textPendingSync = false;
                textParseFailed = false;
                var wasLoading = loading;
                loading = true;
                try
                {
                    document.Load(McdocHelpers.GetRootType(EntryId), jsonEditor.State.Root);
                }
                finally
                {
                    loading = wasLoading;
                }
                MaterializeUntilStable();
                projector.Reset(document.Nodes.ToList());
                SyncJsonText();     // 最后排版：Reformat 一次并记下"已同步的那一代"
                EnsureProjectionMatchesDocument();
                MdocStatus = "右侧 JSON 有语法错误，已按左侧对齐脏区域（" + dirty.Path + "）并排版";
                CBHK.Utility.Data.Mcdoc.McdocWriteTrace.Log("右侧脏区域已对齐", edited,
                    "容器=" + dirty.Path + " 闭合=" + dirty.Closed + " 原长=" + text.Length + " 新长=" + resolved.Length);
                return true;
            }
            finally
            {
                resolvingText = false;
            }
        }

        /// <summary>节点 → 段路径（<c>$.a.b[0]</c>）。解析不出来就返回空列表（= 走"整篇以模型为准"口径）。</summary>
        private static List<object> SegmentsOf(McdocTreeNode node)
        {
            var segments = new List<object>();
            var path = node?.JsonPath;
            if (string.IsNullOrEmpty(path) || path[0] != '$') return segments;
            var index = 1;
            while (index < path.Length)
            {
                if (path[index] == '.')
                {
                    var end = index + 1;
                    while (end < path.Length && path[end] != '.' && path[end] != '[') end++;
                    segments.Add(path.Substring(index + 1, end - index - 1));
                    index = end;
                }
                else if (path[index] == '[')
                {
                    var close = path.IndexOf(']', index);
                    if (close < 0) return [];
                    if (!int.TryParse(path.Substring(index + 1, close - index - 1), out var slot)) return [];
                    segments.Add(slot);
                    index = close + 1;
                }
                else
                {
                    index++;
                }
            }
            return segments;
        }

        /// <summary>
        /// 投影与文档必须同代：任一行的 Json 不是文档里该路径的那个对象，就整表重投影。
        /// 文档（RootJson）是唯一来源 —— 局部拼接漏掉一次，行上就会留着上一代的残骸
        /// （现象：行有值、文档里没有）；这里只在"证明确实不同代"时动手，正常路径零开销。
        /// </summary>
        private void EnsureProjectionMatchesDocument()
        {
            var root = document?.RootJson;
            if (root is null) return;
            var projectionSignature = document.Generation * 397 ^ projector.SpliceCount ^ (projector.ResetCount << 8);
            if (projectionSignature == lastProjectionGeneration) return;
            lastProjectionGeneration = projectionSignature;
            // 树节点自己的 Json 也要以 RootJson 为准：局部拼接可能让它指向上一代 AST 的同值对象 ——
            // 只重置投影是修不好的（重建出来的行还是指向旧对象），必须先按文档把节点重指
            var repaired = 0;
            var staleNodes = new List<McdocTreeNode>();
            void Walk(McdocTreeNode node)
            {
                if (node is null) return;
                var field = node.FieldName;
                if (node.Json is not null && field is { Length: > 0 } && node.JsonPath is { Length: > 0 } path
                    && path.EndsWith("." + field, StringComparison.Ordinal))
                {
                    var expected = McdocTreeDocument.ResolvePath(root, path);
                    if (expected is not null && !ReferenceEquals(expected, node.Json))
                    {
                        McdocWriteTrace.Log("节点 Json 不是文档里该路径的对象（按文档重指）", node, path);
                        node.Json = expected;
                        node.SyncScalarFromJson();
                        repaired++;
                    }
                    else if (expected is null)
                    {
                        // 文档里已经没有这个键：节点从父级摘掉（否则行上留一条"文档里没有"的残骸）
                        McdocWriteTrace.Log("节点值已不在文档里（摘掉）", node, path);
                        staleNodes.Add(node);
                    }
                }
                foreach (var child in node.Children) Walk(child);
                foreach (var child in node.Items) Walk(child);
                foreach (var child in node.SelectedUnionChildren) Walk(child);
            }
            var visited = new HashSet<McdocTreeNode>();
            void Collect(McdocTreeNode node)
            {
                if (node is null || !visited.Add(node)) return;
                foreach (var child in node.Children) Collect(child);
                foreach (var child in node.Items) Collect(child);
                foreach (var child in node.SelectedUnionChildren) Collect(child);
            }
            foreach (var node in document.Nodes) Collect(node);
            var orphans = projector.Rows.Any(row => row.Node is not null && !visited.Contains(row.Node));
            if (orphans)
            {
                // 行指向的节点已经不在文档树里：只有整表重投影能把它清掉
                McdocWriteTrace.Log("投影里有不在树上的行", null, "行数=" + projector.Rows.Count + " 节点=" + visited.Count);
                projector.Reset(document.Nodes.ToList());
                return;
            }
            foreach (var node in document.Nodes) Walk(node);
            if (staleNodes.Count > 0)
            {
                foreach (var node in staleNodes)
                {
                    var parent = node.Parent;
                    if (parent is null) continue;
                    parent.Children.Remove(node);
                    parent.Items.Remove(node);
                    parent.SelectedUnionChildren.Remove(node);
                    parent.ChildrenBuilt = false;
                }
                projector.Reset(document.Nodes.ToList());
                return;
            }
            if (repaired == 0) return;
            // 值变了：行的 Head（编辑器/候选/标签）必须按新值重算
            foreach (var row in projector.Rows) row.RefreshHead();
        }

        protected void PushJsonText(string text)
        {
            suppressReload = true;
            JsonText = text;
            suppressReload = false;
        }

        /// <summary>跟随跳转带过来的资源名：另存为时的默认文件名（misode 的 ?preset=）。</summary>
        public string SuggestedFileName { get; private set; } = "";

        /// <summary>被别的页"跟随"过来时调用。</summary>
        public void ApplyPreset(string value)
        {
            SuggestedFileName = (value ?? "").StartsWith("minecraft:", StringComparison.Ordinal)
                ? value.Substring("minecraft:".Length)
                : value ?? "";
            if (!string.IsNullOrEmpty(SuggestedFileName))
            {
                MdocStatus = "已带入资源名：" + SuggestedFileName;
            }
        }

        /// <summary>复制当前 JSON（misode 的 SourcePanel 里"复制"那一档）。</summary>
        [CommunityToolkit.Mvvm.Input.RelayCommand]
        private void CopyJson()
        {
            if (!string.IsNullOrWhiteSpace(JsonText))
            {
                System.Windows.Clipboard.SetText(JsonText);
                MdocStatus = "已复制 JSON（" + JsonText.Length + " 字符）";
            }
        }

        /// <summary>另存为 JSON。</summary>
        [CommunityToolkit.Mvvm.Input.RelayCommand]
        private void SaveJson()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON|*.json",
                FileName = (string.IsNullOrEmpty(SuggestedFileName) ? EntryId : SuggestedFileName) + ".json",
            };
            if (dialog.ShowDialog() == true && WriteJsonFile(dialog.FileName))
            {
                MdocStatus = "已保存：" + dialog.FileName;
            }
        }

        // [RelayCommand] 生成的是 IRelayCommand，接口要求 ICommand，这里显式转发
        System.Windows.Input.ICommand IMcdocDocumentActions.CopyJsonCommand => CopyJsonCommand;
        System.Windows.Input.ICommand IMcdocDocumentActions.SaveJsonCommand => SaveJsonCommand;

        /// <summary>把当前 JSON 写到指定路径；探针走这条，避免依赖剪贴板/对话框。</summary>
        public bool WriteJsonFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(JsonText)) return false;
            System.IO.File.WriteAllText(path, JsonText);
            return true;
        }

        protected McdocGeneratorViewModelBase(Resource resource, string entryId)
        {
            this.resource = resource;
            EntryId = entryId;
            // 懒建节点第一次展开时补一次物化：判别键落盘后，相邻的派发器 spread 才解得出来
            projector.AfterChildrenBuilt = OnChildrenBuilt;
            // 折叠状态是页面级静态表：换入口必须清掉，否则上一个页面的展开状态会串过来
            McdocExpandState.BeginSession(entryId, this);
            FillGameVersionOptions();
            LoadDefault();
        }

        /// <summary>建一个会话（符号索引 + 版本 + 回写回调）。换版本时只换这一层，不重建文档内容。</summary>
        private void CreateDocument()
        {
            document = new McdocTreeDocument(resource.McdocTypes)
            {
                GameVersion = GameVersion,
                DocumentUri = "file:///generator/" + EntryId + ".json",
                RegistryOptions = new MdocRegistryOptions(resource),
            };
            // 值编辑只有一个出口：行 → JSON（EnsurePath/node.Json/WriteIntoParent）→ 重排文本
            document.NodeValueChanged = node =>
            {
                // 建树期给节点赋默认值（枚举默认项、bool 初值、union 分支镜像）也会触发这个回调：
                // 既不能当用户编辑写进 JSON，也不能回写文本（回写会让编辑器再解析一份 AST，树与文本从此分离）
                if (loading) return;
                // union 行的 JSON 由"切分支"维护（FollowSelectedBranchShape），分支镜像只是显示，不能再算一遍。
                // 但**标量分支是镜像到本行编辑器的**（ScalarEditorKind != null）：用户敲的就是这一行，必须回写。
                var unionStructuralBranch = node.Type?.Kind == McdocTypeKind.Union && node.ScalarEditorKind is null;
                // 快路径：**已存在键的标量编辑**只改那一段文本、只刷这一行（不整篇重排、不整表重投影）。
                // 定位必须在写 AST 之前抓（写完之后旧对象就不在树上了）。
                // 右侧文本必须处于"合法且已消化"的那一代：用户正在打字（未消化）或文本暂时非法时，
                // 锚点区间跟文本已经对不上，这时候做手术式替换会把用户写的括号/内容啃掉。
                // 这种情形一律退回老路径（它会因为 digest 失败而只改树、不动文本）。
                var textReady = !textPendingSync && !textParseFailed && !digestingText;
                if (textReady && !unionStructuralBranch && jsonEditor is not null && node.Json is not null
                    && jsonEditor.State.TryCaptureMember(node.Json, out var slot))
                {
                    var wasSuppressed = suppressReload;
                    suppressReload = true;
                    try
                    {
                        McdocCollectionEditor.Default.ApplyNodeValue(node);
                    }
                    finally
                    {
                        suppressReload = wasSuppressed;
                    }
                    if (node.Json is not null && jsonEditor.State.TryRewriteMember(slot, node.Json))
                    {
                        // 只改这一段文本（成功时 span 通常只有几十个字符）
                        JsonText = jsonEditor.Text;
                        lastSyncedText = JsonText;
                        projector.Find(node)?.RefreshHead();
                        document.ValidateNode(node);
                        CBHK.Utility.Data.Mcdoc.McdocWriteTrace.Log("值编辑走手术式回写", node,
                            "span=" + slot.Start + "+" + slot.Length);
                        if (node.IsRuntimeDependent) ScheduleRebuild(node.RebuildAnchor);
                        return;
                    }
                    // 手术式落地失败（值变成容器/多行/锚点丢失）：AST 已经写过，走整篇回写即可，不再二次 ApplyNodeValue
                    CBHK.Utility.Data.Mcdoc.McdocWriteTrace.Log("值编辑退回整篇", node, "路径=" + (node.JsonPath ?? "?"));
                    SyncJsonText();
                    if (node.Type?.Kind != McdocTypeKind.Union && node.IsRuntimeDependent) ScheduleRebuild(node.RebuildAnchor);
                    return;
                }
                if (!unionStructuralBranch && jsonEditor is not null && node.Json is not null)
                {
                    CBHK.Utility.Data.Mcdoc.McdocWriteTrace.Log("值编辑未走快路径", node,
                        "捕获定位失败 路径=" + (node.JsonPath ?? "?") + " 值=" + node.Json.Kind);
                }
                if (!unionStructuralBranch) McdocCollectionEditor.Default.ApplyNodeValue(node);
                // 右侧有语法错误 / 还没消化：先用"最小完整容器"把它收敛成合法的一份（值已经落进模型了），
                // 成功后文本与树同代，这次编辑也就同时进了右侧 —— 不再停在"只改树、文本冻结"。
                if (!textReady && TryResolveDirtyText(node.Parent ?? node)) return;
                // 这个值参与过动态索引（例如 advancement 的 trigger 决定 ...minecraft:trigger[[trigger]] 展开什么）：
                // 只重排文本会让 spread 停在旧分支，必须按新 JSON 重建
                SyncJsonText();
                // 这个值参与过动态索引（例如 advancement 的 trigger 决定 ...minecraft:trigger[[trigger]] 展开什么）：
                // 文本已经同步，重建延后一拍再做 —— 同步重建会打断进行中的编辑（动态键正在输入时节点会被换掉，
                // 表现就是"加了键但 JSON 里没有"）。
                // union 行自己已经把分支换好了（ConvertJsonToSelectedBranch + 原位重建行），
                // 再排一次整树重建反而会把行实例整批换掉（滚动/展开/选中全丢）。
                if (node.Type?.Kind != McdocTypeKind.Union && node.IsRuntimeDependent)
                {
                    ScheduleRebuild(node.RebuildAnchor);
                }
            };
            document.NodeStructureChanged = node =>
            {
                // 右侧有语法错误 / 还没消化：结构编辑的 JSON 已经由编辑器写完，
                // 先按"最小完整容器"把文本收敛成合法的一份（= 左侧这次改动同步到右侧），再谈重建。
                if ((textParseFailed || textPendingSync) && !digestingText && TryResolveDirtyText(node)) return;
                // 依赖运行期数据（动态索引/调度器）→ 只重建最近容器子树；否则只刷新这一层子级。
                // 两条路都不再整表 Reset：父行还在就只重拼父行的子行，其它行对象保持原样。
                if (node.IsRuntimeDependent)
                {
                    ScheduleRebuild(node.RebuildAnchor);
                    SyncJsonText();
                    return;
                }
                var wasLoading = loading;
                loading = true;
                try
                {
                    document.RefreshNode(node);
                }
                finally
                {
                    loading = wasLoading;
                }
                MaterializeUntilStable();
                if (!projector.RefreshAround(node))
                {
                    projector.Reset(document.Nodes.ToList());
                }
                SyncJsonText();
            };
        }

        /// <summary>
        /// 把"必填 + 闭合下拉 + 已经替用户选中"的值写进文档，并重建一次让依赖它的 spread 解析到位。
        /// <paramref name="includeDisplayedDefaults"/> 只在"载入渲染完毕"时开：那一刻行上显示的
        /// 数字/bool/union 标量默认值要落盘（所见即所得）；结构变更后不补，免得编辑时凭空长出键。
        /// </summary>
        private int MaterializeRequiredSelections(bool includeDisplayedDefaults = false)
        {
            var wasLoading = loading;
            // 整段都在 loading 里：落盘值与随后的重建都不能当成"用户编辑"再触发一轮回写
            loading = true;
            try
            {
                var materialized = McdocCollectionEditor.Default.MaterializeRequiredSelections(document.Nodes, includeDisplayedDefaults);
                if (materialized == 0) return 0;
                document.Load(McdocHelpers.GetRootType(EntryId), document.RootJson);
                return materialized;
            }
            finally
            {
                loading = wasLoading;
            }
        }

        /// <summary>
        /// 物化跑到不动点：每一轮写入都会重建一次文档，重建又会把派发器/动态索引解出新的必填字段
        /// （type_specific.type → ...entity_sub_predicate[[type]] → BoatPredicate.variant），
        /// 只跑一轮就会停在"解出来了但没落盘"。上限防环，超限必须留痕。
        /// </summary>
        private int MaterializeUntilStable(bool includeDisplayedDefaults = false, int maxPasses = 3)
        {
            var total = 0;
            for (var pass = 0; pass < maxPasses; pass++)
            {
                // includeDisplayedDefaults 只对第一轮有效：之后的轮次是"重建新长出来的字段"，不是载入补默认值
                var written = MaterializeRequiredSelections(includeDisplayedDefaults && pass == 0);
                if (written == 0) return total;
                total += written;
            }
            McdocWriteTrace.Log("物化未收敛", null, "已写 " + total + " 个键，到达 " + maxPasses + " 轮上限");
            return total;
        }

        /// <summary>
        /// 懒建节点第一次展开时补物化：刚建出来的子级里若有"必填 + 闭合候选"的字段（往往正是派发器的判别键），
        /// 立刻落盘再排一次重建，否则依赖它的兄弟 spread 会一直停在没有成员的状态。
        /// 这里只写值不重载（正在投影中），重建交给 ScheduleRebuild 延后一拍。
        /// </summary>
        private void OnChildrenBuilt(McdocTreeNode node)
        {
            if (loading || node is null) return;
            var wasLoading = loading;
            loading = true;
            var written = 0;
            try
            {
                written = McdocCollectionEditor.Default.MaterializeRequiredSelections([node]);
            }
            catch (Exception)
            {
                // 投影期间补物化失败只影响这一拍的树形，绝不能让建树/展开整段崩掉
                written = 0;
            }
            finally
            {
                loading = wasLoading;
            }
            if (written > 0) ScheduleRebuild(node);
        }

        private bool rebuildScheduled;

        /// <summary>因为"正在打字"被推迟的重建：失焦时补跑一次。</summary>
        private bool rebuildDeferred;

        /// <summary>
        /// 键盘焦点是否真的在"文本输入"里（只有这种情形才允许推迟重建）。
        /// 按钮 / 展开器 / 收起状态的下拉都不算：点一次 ⊕ 就把整棵树停在上一代是上一版的真实 bug。
        /// </summary>
        private static bool IsTextEditorFocused()
        {
            if (System.Windows.Input.Keyboard.FocusedElement is not DependencyObject focused) return false;
            for (var current = focused; current is not null; current = ParentOf(current))
            {
                if (current is System.Windows.Controls.ComboBox combo) return combo.IsDropDownOpen;   // 下拉展开时正在选，别换行
                if (current is System.Windows.Controls.Primitives.TextBoxBase) return true;
                if (current is McdocTreeRowView) return false;
            }
            return false;

            static DependencyObject ParentOf(DependencyObject current)
            {
                try
                {
                    if (current is System.Windows.Media.Visual or System.Windows.Media.Media3D.Visual3D)
                        return System.Windows.Media.VisualTreeHelper.GetParent(current);
                    if (current is FrameworkContentElement element) return element.Parent;
                }
                catch (Exception)
                {
                }
                return null;
            }
        }

        private System.Windows.Threading.DispatcherTimer deferredFlushTimer;

        /// <summary>
        /// 推迟之后必须有收敛出口：挂一个 60ms 的补跑定时器。
        /// 仍在文本输入里就继续等，一旦不是打字（点了按钮、下拉收起、失焦）就补跑。
        /// </summary>
        private void ArmDeferredFlush()
        {
            if (deferredFlushTimer is not null) return;
            deferredFlushTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(60),
            };
            deferredFlushTimer.Tick += (_, _) =>
            {
                if (!rebuildDeferred)
                {
                    deferredFlushTimer?.Stop();
                    deferredFlushTimer = null;
                    return;
                }
                if (IsTextEditorFocused()) return;    // 还在打字：继续等下一拍
                deferredFlushTimer.Stop();
                deferredFlushTimer = null;
                FlushDeferredRebuild();
            };
            deferredFlushTimer.Start();
        }

        /// <summary>焦点离开行视图时把被推迟的重建补跑一次（行视图调用）。</summary>
        public void FlushDeferredRebuild()
        {
            if (!rebuildDeferred) return;
            rebuildDeferred = false;
            var target = pendingRebuildAnchor;
            pendingRebuildAnchor = null;
            RebuildTargetedOrFallback(target);
        }

        private McdocTreeNode pendingRebuildAnchor;

        /// <summary>
        /// 把重建排到当前输入事件之后（同一拍内多次请求只跑一次）。
        /// 传入 anchor 时只重建那一棵子树；anchor 为空或已失效时回退整树重建。
        /// </summary>
        private void ScheduleRebuild(McdocTreeNode anchor = null)
        {
            if (anchor is not null) pendingRebuildAnchor = anchor;
            if (rebuildScheduled) return;
            var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            if (dispatcher is null || dispatcher.HasShutdownStarted) return;
            rebuildScheduled = true;
            try
            {
                dispatcher.BeginInvoke(new Action(() =>
                {
                    rebuildScheduled = false;
                    // 用户正在行内编辑器里打字：这一刻重建会把承载光标的行换掉（焦点掉到列表上，第二个字符就丢了）。
                    // 记下来，等焦点离开行视图时再补跑。
                    if (IsTextEditorFocused())
                    {
                        rebuildDeferred = true;
                        ArmDeferredFlush();
                        return;
                    }
                    var target = pendingRebuildAnchor;
                    pendingRebuildAnchor = null;
                    RebuildTargetedOrFallback(target);
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception)
            {
                rebuildScheduled = false;
                pendingRebuildAnchor = null;
            }
        }

        /// <summary>定向重建：只重造 anchor 子树并重拼父行的子行；锚点失效/根级替换时回退整树。</summary>
        private void RebuildTargetedOrFallback(McdocTreeNode anchor)
        {
            if (anchor is null)
            {
                ReloadKeepingEditor();
                return;
            }
            // 锚点已被上一次重建换掉时才按路径重找：路径索引里同路径的可能是动态键占位行（无键身份），
            // 优先用还挂在树上的锚点本体。
            var current = document.IsAttached(anchor) ? anchor : document.FindNode(anchor.JsonPath) ?? anchor;
            var wasLoading = loading;
            loading = true;
            McdocTreeNode rebuilt = null;
            try
            {
                rebuilt = document.RebuildNode(current);
            }
            finally
            {
                loading = wasLoading;
            }
            if (rebuilt is null)
            {
                ReloadKeepingEditor();
                return;
            }
            if (!projector.RefreshAround(rebuilt))
            {
                projector.Reset(document.Nodes.ToList());
            }
            // 派发器刚解出来的成员里可能有"必填 + 闭合候选"的字段：不在这里补物化，
            // 这一行就会停在"行上有值、文档里没有"（type_specific.variant 就是这么丢的）。
            // 写过值说明文档已被重新 Load，旧行全部作废，必须整表重投影。
            if (MaterializeUntilStable() > 0)
            {
                projector.Reset(document.Nodes.ToList());
            }
            SyncJsonText();
            EnsureProjectionMatchesDocument();
        }

        /// <summary>按当前 JSON 重建树，但不重解析编辑器文本（值变化/结构变化的共同出口）。</summary>
        private void ReloadKeepingEditor()
        {
            var wasLoading = loading;
            loading = true;
            try
            {
                document.Load(McdocHelpers.GetRootType(EntryId), document.RootJson);
            }
            finally
            {
                loading = wasLoading;
            }
            MaterializeUntilStable();
            projector.Reset(document.Nodes.ToList());
            SyncJsonText();
        }

        /// <summary>
        /// 符号表残缺必须在界面上看得见：缺一份文档会让别处对它的引用与 #[id] 候选整片退化，
        /// 而这类失败以前只写进 Resource.McdocIndexFailures，没人读。
        /// </summary>
        private string IndexFailureSuffix()
        {
            var failures = resource?.McdocIndexFailures;
            if (failures is null || failures.Count == 0)
            {
                return "";
            }
            return " | ⚠ 索引失败 " + failures.Count + "：" + string.Join("；", failures.Take(2));
        }

        /// <summary>用入口的默认值起一棵树，页面打开即可看到结构。</summary>
        public void LoadDefault()
        {
            CreateDocument();
            try
            {
                var root = McdocHelpers.GetRootType(EntryId);
                // 求值与默认值生成必须共用同一个 runtime：misode 的 getDefault 在 struct 字段 / list 项 /
                // tuple 项上也会先 simplifyType，否则 union 字段按"声明类型"取到的是被版本门控滤掉的成员（结果是 null）
                var runtime = document.CreateRuntime();
                // 默认文档与"缺省字段 ⊕ / 列表追加 / 动态键提交"同一个出口：写出来的就是代数对该类型的解
                LoadMcdocTree(McdocJsonFormatter.Format(McdocDefaults.Resolve(root, runtime)));
            }
            catch (Exception error)
            {
                MdocStatus = "建树失败：" + error.Message;
            }
        }

        /// <summary>从文本重建树（切换版本 / 载入草稿时用）。</summary>
        public void LoadMcdocTree(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }
            // 兜底校验：任何入口（切换版本/载入草稿/防抖重建）都先过 System.Text.Json，
            // 语法错就只画线、不碰树——宽容解析器会返回部分结果，直接建树会把旧内容覆盖掉。
            CBHK.Utility.Data.Mcdoc.McdocWriteTrace.LogText(
                "LoadMcdocTree由" + (new System.Diagnostics.StackTrace(1, false).GetFrame(0)?.GetMethod()?.Name ?? "?")
                + "调用 loading=" + loading + " isDocumentRoot=" + (document?.RootJson is not null), text);
            if (!McdocJsonParser.TryParse(text, out var parsedText, out var syntaxError))
            {
                ShowSyntaxError(syntaxError);
                return;
            }
            syntaxOverlay?.Clear();
            // 有编辑器时用它的 AST 建树：树节点与文本共用同一批 JsonValue，回写才能只重排文本
            if (jsonEditor is not null && !string.IsNullOrWhiteSpace(jsonEditor.Text))
            {
                if (jsonEditor.Text != text)
                {
                    jsonEditor.Reload(text);
                }
                else
                {
                    // 文本相等说明是用户刚在编辑器里敲出来的：State.Root 还停在上一次解析，必须重解析，
                    // 否则防抖重建的树是旧内容（真 bug：编辑器里改完值，树上还显示旧值）
                    jsonEditor.State.Reparse();
                }
            }
            var wasLoading = loading;
            loading = true;
            try
            {
                document.Load(McdocHelpers.GetRootType(EntryId),
                    jsonEditor is not null ? jsonEditor.State.Root : parsedText);
            }
            finally
            {
                loading = wasLoading;
            }
            // 必填闭合下拉的"默认选中"落盘 + 重建：依赖它的 spread/调度器才会展开（advancement.criteria.*.trigger）。
            // 这一次是"渲染完毕"：行上显示的数字/bool/union 标量默认值也一起落盘（所见即所得）
            var materializedOnLoad = MaterializeUntilStable(includeDisplayedDefaults: true);
            projector.Reset(document.Nodes.ToList());
            // 物化写过键：物化改的是 AST，编辑器里的文本还是旧的 —— 必须以根 JSON 为准推一次，
            // 否则右侧 JSON 视图看不到刚落的默认值（"渲染完毕但 JSON 没有"就是这么来的）。
            if (materializedOnLoad == 0) PushJsonText(jsonEditor?.Text ?? McdocJsonFormatter.Format(document.RootJson));
            else SyncJsonText();
            EnsureTextGeneration();
            EnsureProjectionMatchesDocument();
            // 文本此刻就是"树已经消化过的那一代"：不记下来的话，编辑器抬一次同文本的 TextChanged 就会被当成用户输入
            lastSyncedText = JsonText;
            // 诊断数与未解析数一起报：只报一个会掩盖另一类残缺
            MdocStatus = "mcdoc | " + EntryId + " | 字段 " + document.Nodes.Count + " 个顶层"
                + (document.DiagnosticsCount > 0 ? " | ⚠ " + document.DiagnosticsCount + " 条诊断" : "")
                + (document.MissedReferences.Count > 0 ? " | ⚠ 未解析 " + document.MissedReferences.Count : "")
                + IndexFailureSuffix();
        }
    }
}
