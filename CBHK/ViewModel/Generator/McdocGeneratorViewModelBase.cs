using CBHK.Model.Constant;
using CBHK.Utility.Data;
using CBHK.Utility.Data.JsonText;
using CBHK.Utility.Data.Mcdoc;
using CommunityToolkit.Mvvm.ComponentModel;
using MinecraftLanguageModelLibrary.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace CBHK.ViewModel.Generator
{
    /// <summary>
    /// 生成器页面 VM 的公共骨架：引擎就位后，各页面只差一个 dispatcher 入口名。
    /// 负责：建会话（符号索引来自 Resource）+ 默认 JSON + 行投影 + 值/结构回写。
    /// </summary>
    public abstract partial class McdocGeneratorViewModelBase : ObservableObject, IMcdocJsonEditorHost, IMcdocDocumentActions
    {
        #region Field
        private readonly Resource resource;
        private readonly McdocRowProjector projector = new();
        private McdocTypeIndex sessionSymbols;
        private McdocType sessionRootType;

        private McdocTreeDocument document;
        /// <summary>同步时序（结构变更 / 物化不动点 / 定向重建）的唯一实现，探针跑同一份。</summary>
        private McdocPipeline pipeline;

        /// <summary>版本下拉的选中项：改它就换版本重新求值（misode 的版本选择器同款），文档内容保留。</summary>
        [ObservableProperty]
        private EnumMember selectedGameVersionOption;

        /// <summary>等运行数据最多等这么久（Init 给索引留了 90 秒预算，这里比它再宽松一点）。</summary>
        private const int RuntimeRefreshAttempts = 120;

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

        /// <summary>最近一次"树 ↔ 文本"已经同步过的文本：编辑器回调命中它就直接返回（抑制环 + 省一次 diff）。</summary>
        private string lastSyncedText;

        /// <summary>编辑器里还有树没消化的文本（用户正在打字 / 防抖还没跑）。</summary>
        private bool textPendingSync;

        /// <summary>上一次解析失败（右侧 JSON 现在是语法错误）。</summary>
        private bool textParseFailed;

        /// <summary>正在把编辑器文本吃进树：期间的树侧回写不再触发二次消化。</summary>
        private bool digestingText;

        /// <summary>树 → 文本：不走防抖，直接回写（并抑制反向重建）。</summary>
        private JsonTextEditor jsonEditor;
        /// <summary>JSON 语法错误的波浪线（复用 WaveUnderlineBackgroundRenderer）。</summary>
        private McdocJsonSyntaxErrorOverlay syntaxOverlay;

        /// <summary>整篇写回串行化：写回过程中又被请求时只记下来，外层收尾时补跑一次（取最后一次状态）。</summary>
        private bool syncInFlight;
        private bool syncPending;

        /// <summary>
        /// AST 与文本必须同代：不同代就按当前文本重解析（并留痕）。这是"重解析只能来自当前文本"的执行点。
        /// </summary>
        private int lastTextGeneration = -1;
        private int lastProjectionGeneration = -1;

        /// <summary>正在把"右侧脏区域"对齐成合法文本：期间不再触发新的修复/同步。</summary>
        private bool resolvingText;

        private bool rebuildScheduled;

        /// <summary>因为"正在打字"被推迟的重建：失焦时补跑一次。</summary>
        private bool rebuildDeferred;

        private System.Windows.Threading.DispatcherTimer deferredFlushTimer;

        /// <summary>焦点在行内编辑器里被推迟的"文本 → 树"同步。</summary>
        private bool syncDeferred;

        private McdocTreeNode pendingRebuildAnchor;

        /// <summary>配置给的是目录时，里面优先取这个名字（旧管线就是"目录 + empty.json"）。</summary>
        private const string EntryFileName = "empty.json";

        /// <summary>这份默认文档是在"运行数据没就绪"的情况下求出来的（等数据到了要重生成）。</summary>
        private bool defaultDocumentDegraded;

        /// <summary>
        /// 这份文档是在运行数据（注册表/符号表）就绪**之前**建起来的：那时 #[id] 候选为空，
        /// 必选判别键会被落成空串（现场：loot_table 的 entries[].type = ""，行上却显示着候选）。
        /// 数据到位后必须按同一份来源重来一次 —— 只认 defaultDocumentDegraded 时，从入口文件载入的
        /// 那份文档永远不会重来，空串就留在 JSON 里（"有的能落盘有的不行"就是这么来的）。
        /// </summary>
        private bool needsRuntimeRefresh;

        /// <summary>最近一次生成默认文档的文本：用来判断用户有没有动过内容（动过就不许顶掉）。</summary>
        private string defaultDocumentText;

        /// <summary>等运行数据就绪的轮询定时器（每个页面只挂一个）。</summary>
        private System.Windows.Threading.DispatcherTimer runtimeRefreshTimer;
        #endregion

        #region Property
        /// <summary>视图绑定这个集合（可见节点投影成的行）。</summary>
        public McdocRowCollection Rows => projector.Rows;

        /// <summary>dispatcher 入口名（如 advancement / dimension_type / loot_table）。</summary>
        public string EntryId { get; private set; }

        /// <summary>
        /// #[since]/#[until] 过滤用的游戏版本（McdocAttributeRegistry 在每次 Load 时按它重建）。
        /// 改动后调用 LoadMcdocTree 生效；页面暂时都固定 1.21。
        /// </summary>
        protected string GameVersion { get; set; } = "1.20.5";

        /// <summary>页面 chrome 的版本下拉候选（运行数据里实际可用的注册表版本，倒序）。</summary>
        public ObservableCollection<EnumMember> GameVersionOptions { get; } = [];

        /// <summary>跟随跳转带过来的资源名：另存为时的默认文件名（misode 的 ?preset=）。</summary>
        public string SuggestedFileName { get; private set; } = "";
        #endregion

        #region Method
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
                // 新版本 schema 里没有的键：整份值原样留在文档里（只数不删，也不回写文本）。
                // 切到声明它的版本时，同一份 JSON 整树重建就能把子树就地建出来 —— 数据从没丢过。
                var kept = document.CountUnknownFields();
                MdocStatus = "已切换到 " + version + " | mcdoc | " + EntryId
                    + " | 字段 " + document.Nodes.Count + " 个顶层"
                    + (kept > 0 ? " | 保留了 " + kept + " 个当前版本不存在的键（左侧黄色 !，导出会带上）" : "");
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
        /// 运行数据是否齐了：注册表（#[id] 候选）+ 符号表（mcdoc 类型索引）缺一不可 ——
        /// 只有注册表时每条 #[id] 都有候选、可所有结构仍是 any，页面照样"没有内容"。
        /// </summary>
        private bool IsRuntimeReady()
            => resource?.RunningDataObject is { Count: > 0 } && resource.McdocTypes.TypeDefCount > 0;


        /// <summary>
        /// Resource.Init() 是异步的（236 个 mcdoc 走命名管道），页面可能在它完成前就被点开：
        /// 那时默认文档只能是降级品（数据没到时 dispatcher 解不开 → any → {}）。
        /// 这里每秒看一次"运行数据是否就绪"，就绪后按"用户动没动过文档"重生成一次，之后靠用户操作；
        /// 等不到也要留一句可见的归因（页面"一个字段都没有"不许静默）。
        /// </summary>
        private void ScheduleRuntimeRefresh()
        {
            if (resource is null) return;
            if (IsRuntimeReady())
            {
                RefreshWithReadyRuntime();
                return;
            }
            if (runtimeRefreshTimer is not null) return;
            var attempts = 0;
            runtimeRefreshTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            runtimeRefreshTimer.Tick += (_, _) =>
            {
                var ready = IsRuntimeReady();
                if (!ready && ++attempts <= RuntimeRefreshAttempts)
                {
                    return;
                }
                runtimeRefreshTimer?.Stop();
                runtimeRefreshTimer = null;
                if (!ready)
                {
                    MdocStatus = "运行数据未就绪（注册表或符号表为空），当前只有降级默认文档 | " + MdocStatus;
                    return;
                }
                RefreshWithReadyRuntime();
            };
            runtimeRefreshTimer.Start();
        }

        /// <summary>
        /// 运行数据刚到位：重生成那份降级的默认文档；用户已经写过内容就只按新 schema 重建，
        /// 一个键都不许改动（README §2.7「换版本不改文档」的同一条口径）。
        /// </summary>
        private void RefreshWithReadyRuntime()
        {
            if (!needsRuntimeRefresh) return;   // 建树时数据就是齐的：正常路径一次都不用重来
            needsRuntimeRefresh = false;
            defaultDocumentDegraded = false;
            FillGameVersionOptions();
            var untouched = string.IsNullOrWhiteSpace(JsonText)
                || (defaultDocumentText is not null && string.Equals(JsonText, defaultDocumentText, StringComparison.Ordinal));
            if (untouched)
            {
                // 入口文件就是这份文档：按同一份来源重来（换成 LoadDefault 会把文件内容换成代数默认值）
                if (defaultDocumentText is not null) LoadMcdocTree(defaultDocumentText);
                else LoadDefault();
                return;
            }
            CreateDocument();
            try
            {
                LoadMcdocTree(JsonText);
            }
            catch (Exception error)
            {
                MdocStatus = "运行数据就绪后重建失败：" + error.Message;
            }
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
            // 粘贴常常带进"看不见的空白"（BOM / 不换行空格 / 全角空格）：它们会让严格解析直接判非法，
            // 现象就是"粘贴了一大段合法 JSON，左侧树一动不动"。这里先归一再解析，并把归一后的文本写回编辑器
            // （只动空白类字符，不碰任何值，状态栏说明发生了归一化）。
            var normalized = McdocJsonParser.NormalizeInvisible(text, out var replaced);
            if (replaced > 0)
            {
                McdocWriteTrace.Log("文本含不可见空白字符，按空格归一", null, "replaced=" + replaced);
                text = normalized;
                var wasSuppressed = suppressReload;
                suppressReload = true;
                try
                {
                    PushJsonText(text);
                    jsonEditor.Reload(text);
                }
                finally
                {
                    suppressReload = wasSuppressed;
                }
                MdocStatus = "已把 " + replaced + " 个不可见字符（BOM / 不换行空格 / 全角空格）按空格处理，树按归一后的文本更新";
            }
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
                    McdocWriteTrace.Log("消化文本快照过期", null,
                        "快照=" + (text?.Length ?? 0) + " 编辑器=" + liveText.Length + "，以编辑器为准");
                    text = liveText;
                }
                // 增量优先：只重解析改动落点所在的顶层成员，其余 AST 原样复用；
                // 跨成员 / 换形状 / 整篇替换由它自己回落成整篇解析
                if (!jsonEditor.State.TryIncrementalReparse(text)) jsonEditor.State.Reparse();
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
                var dependents = new System.Collections.Generic.List<McdocTreeNode>();
                var parentRebuilds = 0;
                var droppedDerivedContainers = 0;
                try
                {
                    document.Validate();
                    // 值补丁只改 node.Json，靠这个键求值的兄弟子树不会自己重新求值：
                    // 右侧把 block 改成 "" 之后，左侧 properties 还按旧方块渲染（键↔行双射当场破）。
                    // 必须在投影之前跑，投影才量得到新子树。
                    dependents = pipeline?.RebuildDependents(stats.PatchedNodes,
                        out parentRebuilds, out droppedDerivedContainers) ?? dependents;
                }
                finally
                {
                    loading = wasLoading;
                }
                foreach (var node in stats.PatchedNodes)
                {
                    projector.Find(node)?.RefreshHead();
                }
                foreach (var node in dependents)
                {
                    projector.Find(node)?.RefreshHead();
                }
                McdocProjectionSync.Apply(projector, document, stats);
                if (parentRebuilds > 0 || droppedDerivedContainers > 0)
                {
                    // 派生容器/父级形状换了：局部拼接不足以换掉已展开子行与行命令，整表对账。
                    projector.Reset(document.Nodes.ToList());
                }
                textPendingSync = false;
                textParseFailed = false;
                if (dependents.Count > 0)
                {
                    // 重建会物化判别键（往文档里写值）：屏幕上的文本必须整篇对齐这一代，
                    // 只 Push 一份快照会让两侧不同代（复制/保存导出的是上一份）
                    SyncJsonText();
                }
                else
                {
                    // 消化过程中树侧可能已经整篇回写过（物化必填键 / 重建后重排）：以编辑器当前文本为准，
                    // 拿进来时的快照回写会让 JsonText 与屏幕上的 JSON 不同代（复制/保存导出的是上一份）
                    PushJsonText(jsonEditor?.Text ?? text);
                    lastSyncedText = JsonText;
                }
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
            // 粘贴带进的"看不见的空白"（BOM / 不换行空格 / 全角空格）在这里**必须**先归一：
            // 防抖 tick 走的就是这个出口（OnJsonEditorTextChanged → ScheduleReload → SyncTextToTree），
            // 严格解析会直接判非法 → 树按契约冻死 → 用户看到的是"粘贴整篇，左侧一动不动"。
            // 归一后写回编辑器：否则屏幕留着脏字符、模型是归一文本，下一次比对又判"不同代"。
            var normalizedText = McdocJsonParser.NormalizeInvisible(text, out var replacedInvisible);
            if (replacedInvisible > 0)
            {
                McdocWriteTrace.Log("文本含不可见空白字符，按空格归一", null,
                    "replaced=" + replacedInvisible + " 来源=SyncTextToTree");
                text = normalizedText;
                if (jsonEditor is not null)
                {
                    var wasSuppressed = suppressReload;
                    suppressReload = true;
                    try
                    {
                        PushJsonText(text);
                        jsonEditor.Reload(text);
                    }
                    finally
                    {
                        suppressReload = wasSuppressed;
                    }
                }
                MdocStatus = "已把 " + replacedInvisible + " 个不可见字符（BOM / 不换行空格 / 全角空格）按空格处理，树按归一后的文本更新";
            }
            // 语法检查用 System.Text.Json（TryParse 里就是它）：错误就只画波浪线 + 状态，树一动不动
            if (!McdocJsonParser.TryParse(text, out var parsedText, out var syntaxError))
            {
                ShowSyntaxError(syntaxError);
                return false;
            }
            // 焦点还在行内编辑器里：这一刻只验证语法，别把树拼回去 —— 拼接/重建会把承载光标的行换掉
            // （用户"每敲一个字符就丢焦点"）。改由焦点离开时的补跑接手。
            if (IsTextEditorFocused())
            {
                syncDeferred = true;
                ArmDeferredFlush();
                return false;
            }
            syntaxOverlay?.Clear();
            ApplyTextToTree(text, parsedText);
            // 消化完最后一次落地：AST 与文本、投影与文档都必须同代
            EnsureTextGeneration();
            EnsureProjectionMatchesDocument();
            return true;
        }


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
            // 运行数据可能刚到位：先把降级的默认文档重生成一次（内部已经建过树），别再按旧文本建第二遍
            if (IsRuntimeReady() && (needsRuntimeRefresh || defaultDocumentDegraded))
            {
                RefreshWithReadyRuntime();
            }
            else if (!string.IsNullOrEmpty(JsonText))
            {
                // 编辑器已经解析了一份 AST：让树改用同一份，否则树上改的值与文本里的 JSON 是两棵树，
                // Reformat 输出永远是挂载那一刻的内容
                LoadMcdocTree(JsonText);
            }
            else
            {
                SyncJsonText();
            }
            ScheduleRuntimeRefresh();
        }


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

        private void EnsureTextGeneration()
        {
            if (jsonEditor is null) return;
            // 没换代就不可能不同代：O(1) 跳过（大文档上每次防抖都扫全树是纯浪费）
            if (jsonEditor.State.Generation == lastTextGeneration) return;
            lastTextGeneration = jsonEditor.State.Generation;
            if (jsonEditor.State.IsInStepWithDocument) return;
            McdocWriteTrace.Log("AST 与文本不同代，按当前文本重解析", null,
                "AST文本长=" + (jsonEditor.State.RootText?.Length ?? 0) + " 编辑器=" + (jsonEditor.Text ?? "").Length);
            jsonEditor.State.Reparse();
        }

        private void VerifyJsonTextGeneration(string source)
        {
            if (!McdocWriteTrace.Enabled) return;   // 诊断：不进生产热路径
            if (string.IsNullOrWhiteSpace(JsonText)) return;
            if (McdocJsonParser.TryParse(JsonText, out _, out var syntaxError)) return;
            MdocStatus = "文本与树不同代（" + source + "）：" + syntaxError.Message;
            McdocWriteTrace.Log(source + " 写回后文本不可解析", null,
                syntaxError.Message + " | 长度=" + JsonText.Length);
        }

        private void SyncJsonTextOnce()
        {
            using var stage = McdocWriteTrace.Stage("text.sync");
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
                    // 模型根被**整篇替换**过时（根 union 切支 / 根标量编辑走 ReplaceRoot），
                    // 编辑器里还是上一代 AST —— 直接 Reformat 只会把旧文本重排一遍，右侧就停在旧形状上。
                    // 这里按"视图同代"先对账：不一致就先把模型这一代灌进编辑器。
                    string modelText;
                    using (McdocWriteTrace.Stage("text.format"))
                    {
                        modelText = McdocJsonFormatter.Format(document.RootJson);
                    }
                    // 模型这一代格式化后与"上次落到屏幕上的那份文本"逐字相同 ⇒ 右侧已经是同代文本，
                    // 不必再全文 Parse 一遍对账、也不必 Reformat（实测这两步占每次交互 2–7ms）。
                    if (jsonEditor.State.IsInStepWithDocument
                        && string.Equals(modelText, lastSyncedText, StringComparison.Ordinal))
                    {
                        JsonText = jsonEditor.Text;
                        EnsureProjectionMatchesDocument();
                        return;
                    }
                    var reconciled = false;
                    using (McdocWriteTrace.Stage("text.reconcile"))
                    {
                        reconciled = !McdocJsonParser.TryParse(jsonEditor.Text ?? "", out var editorRoot, out _)
                            || !string.Equals(McdocJsonFormatter.Format(editorRoot), modelText, StringComparison.Ordinal);
                    }
                    if (reconciled)
                    {
                        McdocWriteTrace.Log("编辑器文本落后于模型，按模型重灌", null, "model=" + modelText.Length);
                        jsonEditor.Reload(modelText);
                    }
                    using (McdocWriteTrace.Stage("text.reformat")) jsonEditor.Reformat();
                    JsonText = jsonEditor.Text;
                }
                finally
                {
                    suppressReload = false;
                }
                lastSyncedText = JsonText;
                EnsureProjectionMatchesDocument();
                VerifyJsonTextGeneration("SyncJsonText");
                McdocWriteTrace.LogText("SyncJsonText 编辑器文本",
                    JsonText, true);
                McdocWriteTrace.LogText("SyncJsonText document.RootJson 键",
                    McdocWriteTrace.Keys(document.RootJson));
                return;
            }
            PushJsonText(McdocJsonFormatter.Format(document.RootJson));
            lastSyncedText = JsonText;
        }


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
                    using (McdocWriteTrace.Stage("vm.dirtyReload"))
                    {
                        document.Load(CurrentRootType(), jsonEditor.State.Root);
                    }
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
                McdocWriteTrace.Log("右侧脏区域已对齐", edited,
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
        public void CopyJson()
        {
            if (string.IsNullOrWhiteSpace(JsonText))
            {
                return;
            }
            // 复制出去的必须是"屏幕这一代"：先补跑因焦点/输入被推迟的文本消化与重建，
            // 再让投影与文档对账。否则右侧 JSON 已经是新结构、左侧树还停在上一代，
            // 复制动作反而把这份不一致暴露给用户（"点了复制才出现结构，树里没有"）。
            FlushDeferredRebuild();
            EnsureProjectionMatchesDocument();

            const int MaxRetries = 10;
            const int RetryDelayMs = 50;

            for (int i = 0; i < MaxRetries; i++)
            {
                try
                {
                    Clipboard.SetText(JsonText);
                    MdocStatus = "已复制 JSON (长度 " + JsonText.Length + ")";
                    return; // 成功则直接返回
                }
                catch (COMException ex) when (ex.HResult == unchecked((int)0x800401D0))
                {
                    // 是剪贴板被占用的异常，则进行重试
                    if (i == MaxRetries - 1)
                    {
                        // 最后一次仍然失败，可选择通知用户或记录日志
                        MdocStatus = "复制失败：剪贴板被其他程序占用";
                        // 可选：MessageBox.Show("复制失败，请稍后重试。");
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(RetryDelayMs);
                    }
                }
            }
        }

        /// <summary>另存为 JSON。</summary>
        [CommunityToolkit.Mvvm.Input.RelayCommand]
        private void SaveJson()
        {
            // 与复制同一条"导出 = 屏幕"：先把被推迟的文本/重建补跑，再写文件
            FlushDeferredRebuild();
            EnsureProjectionMatchesDocument();
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
            File.WriteAllText(path, JsonText);
            return true;
        }

        protected McdocGeneratorViewModelBase(Resource resource, string entryId)
        {
            this.resource = resource;
            EntryId = entryId;
            InitializeBaseSession(null, null);
        }

        /// <summary>
        /// 简化构造重载：Resource 已经带版本、符号表和实体 ID；这里只需要 dispatcher 名和初始化 JSON。
        /// dispatcherName 按原始 dispatcher registry 解释（如 minecraft:entity），
        /// 默认 index 取 Resource.EntityIds 排序后的第一个；真实实体切换后仍走
        /// OpenMcdocTreeFromDispatcher 重置。
        /// </summary>
        protected McdocGeneratorViewModelBase(Resource resource, string dispatcherName, string json)
        {
            this.resource = resource;
            EntryId = string.IsNullOrWhiteSpace(dispatcherName) ? "mcdoc" : dispatcherName;
            sessionSymbols = resource?.McdocTypes;
            if (!string.IsNullOrWhiteSpace(dispatcherName))
            {
                var defaultEntityId = resource?.GetIds(dispatcherName.Replace("minecraft:",""), null).FirstOrDefault();
                sessionRootType = new McdocType
                {
                    Kind = McdocTypeKind.Dispatcher,
                    Registry = dispatcherName,
                    Indices = string.IsNullOrEmpty(defaultEntityId)
                        ? []
                        : [McdocIndex.Static(defaultEntityId)]
                };
            }
            InitializeBaseSession(null, json);
        }

        private void InitializeBaseSession(string gameVersion, string json)
        {
            // 懒建节点第一次展开时补一次物化：判别键落盘后，相邻的派发器 spread 才解得出来
            projector.AfterChildrenBuilt = OnChildrenBuilt;
            // 折叠状态是页面级静态表：换入口必须清掉，否则上一个页面的展开状态会串过来
            McdocExpandState.BeginSession(EntryId, this);
            FillGameVersionOptions();
            if (!string.IsNullOrWhiteSpace(gameVersion)) GameVersion = gameVersion;
            if (json is not null)
            {
                // 构造期直接灌数据：不读入口配置，按当前 root 求值这一份 JSON
                CreateDocument();
                LoadMcdocTree(json);
            }
            else
            {
                LoadDefault();
            }
        }

        /// <summary>当前会话的根类型：外部入口可覆盖，否则按入口名解析。</summary>
        private McdocType CurrentRootType()
            => sessionRootType ?? McdocHelpers.GetRootType(EntryId);

        /// <summary>
        /// 用指定入口名 + 符号 Index 重新建立 mcdoc 树会话。
        /// 供实体/物品等生成器复用：不复制 CreateDocument / 默认值 / 文本同步。
        /// </summary>
        public void OpenMcdocTree(string gameVersion, string dispatcherName,
            McdocTypeIndex dispatcherSymbols, string json = null)
        {
            if (string.IsNullOrWhiteSpace(dispatcherName))
            {
                throw new ArgumentException("dispatcherName 不能为空", nameof(dispatcherName));
            }
            ResetMcdocSession(gameVersion, dispatcherName, dispatcherSymbols,
                McdocHelpers.GetRootType(dispatcherName), json);
        }

        /// <summary>
        /// 用原始 dispatcher registry + McdocIndex 建立会话；dispatcherSymbols 为空时使用 Resource 的符号表。
        /// </summary>
        public void OpenMcdocTreeFromDispatcher(string gameVersion, string dispatcherRegistry,
            McdocIndex dispatcherIndex, McdocTypeIndex dispatcherSymbols = null, string json = null)
        {
            if (string.IsNullOrWhiteSpace(dispatcherRegistry))
            {
                throw new ArgumentException("dispatcherRegistry 不能为空", nameof(dispatcherRegistry));
            }
            var root = new McdocType
            {
                Kind = McdocTypeKind.Dispatcher,
                Registry = dispatcherRegistry,
                Indices = dispatcherIndex is null ? [] : [dispatcherIndex]
            };
            ResetMcdocSession(gameVersion, dispatcherRegistry, dispatcherSymbols, root, json);
        }

        private void ResetMcdocSession(string gameVersion, string entryId,
            McdocTypeIndex symbols, McdocType rootType, string json)
        {
            sessionSymbols = symbols ?? resource?.McdocTypes;
            sessionRootType = rootType;
            EntryId = entryId;
            // 版本候选只在第一次建立；之后切实体/切 dispatcher 不能清空重填：
            // XAML 的版本下拉会在 Clear/Repopulate 期间连续触发 SelectionChanged，
            // 页面侧会跟着清实体列表，表现就是启动/切换时"假死"。
            if (GameVersionOptions.Count == 0) FillGameVersionOptions();
            if (!string.IsNullOrWhiteSpace(gameVersion))
            {
                GameVersion = gameVersion;
                if (!GameVersionOptions.Any(option => option.Name == gameVersion))
                {
                    GameVersionOptions.Insert(0, new EnumMember
                    {
                        Name = gameVersion,
                        Value = new MetaValue { LiteralValue = gameVersion },
                    });
                }
                // GameVersion 已经是目标值，setter 里的 ApplyGameVersion 会按同版本直接返回
                SelectedGameVersionOption = GameVersionOptions.FirstOrDefault(option => option.Name == gameVersion);
            }
            McdocExpandState.BeginSession(EntryId, this);
            CreateDocument();
            if (json is not null)
            {
                LoadMcdocTree(json);
            }
            else
            {
                LoadDefault();
            }
        }

        /// <summary>建一个会话（符号索引 + 版本 + 回写回调）。换版本时只换这一层，不重建文档内容。</summary>
        private void CreateDocument()
        {
            document = new McdocTreeDocument(sessionSymbols ?? resource.McdocTypes)
            {
                GameVersion = GameVersion,
                DocumentUri = "file:///generator/" + EntryId + ".json",
                RegistryOptions = new MdocRegistryOptions(resource),
            };
            // 行命令（遗留键/幽灵行删除）要按活文档路径找落点；父级换代时由这里整篇重载
            projector.Document = document;
            projector.RequestReload = ReloadKeepingEditor;
            // BlockState 资产 → mcdoc:block_states 调度器成员：schema 自带 [[block]] 索引，这里只提供成员表。
            // 换版本会重新走 CreateDocument，所以注册一次即可按版本刷新。
            McdocBlockStates.Register(sessionSymbols ?? resource.McdocTypes, GameVersion);
            // 数据没就绪就建的文档：必选判别键只能落成空串，等数据到位要按同一份来源重来一次
            if (!IsRuntimeReady()) needsRuntimeRefresh = true;
            // 日志第一行就暴露"跑的是哪一版"：VS 调试用的是启动那一刻装进内存的 DLL，重新生成不会换掉它，
            // 没有这行标记，就分不清"代码没生效"和"跑的是旧进程"。
            McdocWriteTrace.Trace("mcdoc-build", "marker=" + McdocWriteTrace.BuildMarker
                + " entry=" + EntryId + " version=" + GameVersion);
            // 同步时序只有一份实现（McdocPipeline），探针跑的也是它：探针自己抄一份的话，绿了也说明不了界面绿
            pipeline = new McdocPipeline
            {
                Document = document,
                Projector = projector,
                RootType = CurrentRootType,
                GetLoading = () => loading,
                SetLoading = value => loading = value,
                TextDirty = SyncJsonText,
                Fallback = ReloadKeepingEditor,
                ScheduleRebuild = anchor => ScheduleRebuild(anchor)
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
                    // 这一段文本是**程序写的**：不压住回写通道，编辑器抬的 TextChanged 会被当成用户输入 →
                    // 防抖 → 文本→树同步 → 锚点重建 → 承载光标的行被换掉 → "每敲一个字符就丢焦点"。
                    var echoSuppressed = suppressReload;
                    suppressReload = true;
                    bool rewritten;
                    try
                    {
                        rewritten = node.Json is not null && jsonEditor.State.TryRewriteMember(slot, node.Json);
                        if (rewritten)
                        {
                            JsonText = jsonEditor.Text;
                            lastSyncedText = JsonText;

                        }
                    }
                    finally
                    {
                        suppressReload = echoSuppressed;
                    }
                    if (rewritten)
                    {
                        // 只改这一段文本（成功时 span 通常只有几十个字符）。
                        // 标量行的 Head 跟值无关，不必整行重读绑定；union 行镜像的编辑器/分支候选会变，要刷。
                        // 用户还在行内编辑框里连续键入：先只刷诊断/文本，依赖重建延后到失焦补跑 ——
                        // 同步重建会把承载光标的行换掉（"每敲一个字符就丢焦点"）。
                        if (IsTextEditorFocused())
                        {
                            pipeline.RefreshValueOnly(node);
                            if (pipeline.ValueDrivesDependents(node)) ScheduleRebuild(node.RebuildAnchor);
                        }
                        else
                        {
                            // 统一入口：刷诊断 + 重建"靠这个键解出来"的兄弟子树（dispatcher 索引）
                            pipeline.OnValueChanged(node);
                            // 只有"这个键真的有依赖方"才排重建：否则一次数字/布尔改动会白重建一层（行拼接 → 滚动视图跳）
                            if (node.IsRuntimeDependent && (pipeline?.ValueDrivesDependents(node) ?? true)) ScheduleRebuild(node.RebuildAnchor);
                        }
                        McdocWriteTrace.Log("值编辑走手术式回写", node,
                            "span=" + slot.Start + "+" + slot.Length);
                        return;
                    }
                    // 手术式落地失败（值变成容器/多行/锚点丢失）：AST 已经写过，走整篇回写即可，不再二次 ApplyNodeValue
                    McdocWriteTrace.Log("值编辑退回整篇", node, "路径=" + (node.JsonPath ?? "?"));
                    SyncJsonText();
                    if (IsTextEditorFocused()) pipeline.RefreshValueOnly(node);
                    if (node.Type?.Kind != McdocTypeKind.Union && node.IsRuntimeDependent
                        && (pipeline?.ValueDrivesDependents(node) ?? true)) ScheduleRebuild(node.RebuildAnchor);
                    return;
                }
                if (!unionStructuralBranch && jsonEditor is not null && node.Json is not null)
                {
                    McdocWriteTrace.Log("值编辑未走快路径", node,
                        "捕获定位失败 路径=" + (node.JsonPath ?? "?") + " 值=" + node.Json.Kind);
                }
                if (!unionStructuralBranch) McdocCollectionEditor.Default.ApplyNodeValue(node);
                // 右侧有语法错误 / 还没消化：先用"最小完整容器"把它收敛成合法的一份（值已经落进模型了），
                // 成功后文本与树同代，这次编辑也就同时进了右侧 —— 不再停在"只改树、文本冻结"。
                if (!textReady && TryResolveDirtyText(node.Parent ?? node)) return;
                // 这个值参与过动态索引（例如 advancement 的 trigger 决定 ...minecraft:trigger[[trigger]] 展开什么）：
                // 只重排文本会让 spread 停在旧分支，必须按新 JSON 重建
                SyncJsonText();
                // 切 union 结构分支：分支子树是按 getChange 的默认形状建的，必选判别键还没落盘、
                // 同层 spread 也还没重新求值 —— 只同步文本会让行停在「只有 type 一行」（截图现场）。
                // 延后一拍重建这一支：重建会重写判别键并补物化到不动点。
                if (unionStructuralBranch)
                {
                    pipeline.OnUnionBranchSwitched(node);
                    return;
                }
                // 值变化的**唯一**出口：重算诊断 + 重建"靠这个键解出来的兄弟/父级"（清旧成员、解新成员）。
                // 以前只有手术式快路径接了 pipeline.OnValueChanged，慢路径（没有编辑器 / 快路径失手）
                // 只排一个 IsRuntimeDependent 重建 —— 于是"切完 Enum，同层旧成员的键还在、新成员也不解析"。
                pipeline.OnValueChanged(node);
            };
            document.NodeStructureChanged = node =>
            {
                // 右侧有语法错误 / 还没消化：结构编辑的 JSON 已经由编辑器写完，
                // 先按"最小完整容器"把文本收敛成合法的一份（= 左侧这次改动同步到右侧），再谈重建。
                if ((textParseFailed || textPendingSync) && !digestingText && TryResolveDirtyText(node)) return;
                // 结构变更只有一份实现（McdocPipeline）：运行期依赖 / 再入 / 刷新次序都在里面
                pipeline.OnStructureChanged(node);
            };
        }

        /// <summary>
        /// 物化跑到不动点：每一轮写入都会重建一次文档，重建又会把派发器/动态索引解出新的必填字段
        /// （type_specific.type → ...entity_sub_predicate[[type]] → BoatPredicate.variant），
        /// 只跑一轮就会停在"解出来了但没落盘"。上限防环，超限必须留痕。
        /// </summary>
        private int MaterializeUntilStable(bool includeDisplayedDefaults = false, int maxPasses = 3)
        {
            return pipeline?.MaterializeUntilStable(includeDisplayedDefaults, maxPasses) ?? 0;
        }

        /// <summary>
        /// 懒建节点第一次展开时补物化：刚建出来的子级里若有"必填 + 闭合候选"的字段（往往正是派发器的判别键），
        /// 立刻落盘再排一次重建，否则依赖它的兄弟 spread 会一直停在没有成员的状态。
        /// 这里只写值不重载（正在投影中），重建交给 ScheduleRebuild 延后一拍。
        /// </summary>
        private void OnChildrenBuilt(McdocTreeNode node)
        {
            if (loading || node is null) return;
            // 闸门内不丢弃、只登记：闸门结束后由管线补跑一次（McdocPipeline.DrainPendingChildBuilds）。
            // 丢弃会让新子树里的必填判别键永远不落盘 —— 表现是"加了元素，同层调度器成员一个都不出现"。
            pipeline?.NoteChildrenBuilt(node);
        }


        /// <summary>
        /// 键盘焦点是否真的在"文本输入"里（只有这种情形才允许推迟重建）。
        /// 按钮 / 展开器 / 收起状态的下拉都不算：点一次 ⊕ 就把整棵树停在上一代是上一版的真实 bug。
        /// </summary>
        private static bool IsTextEditorFocused()
        {
            DependencyObject focused;
            try
            {
                // Keyboard.FocusedElement 依赖 WPF 的 InputManager：非 STA 线程（探针 / 门禁的后台线程）上
                // 会抛"调用线程必须为 STA"。问不到焦点就当没焦点 —— 这条只是"要不要推迟"，不能把调用方打崩。
                focused = System.Windows.Input.Keyboard.FocusedElement as DependencyObject;
            }
            catch (Exception)
            {
                return false;
            }
            if (focused is null) return false;
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
                if (!rebuildDeferred && !syncDeferred)
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


        /// <summary>焦点离开行视图时把被推迟的重建 / 文本同步补跑一次（行视图与定时器都走这里）。</summary>
        public void FlushDeferredRebuild()
        {
            if (rebuildDeferred)
            {
                rebuildDeferred = false;
                var target = pendingRebuildAnchor;
                pendingRebuildAnchor = null;
                RebuildTargetedOrFallback(target);
            }
            if (syncDeferred)
            {
                syncDeferred = false;
                SyncTextToTree();
            }
        }


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
                    // 锚点必须由**这次调度**自己带上：只读共享字段的话，别处清掉它之后这里就只剩 null，
                    // 而 null 的含义是"整篇重载"（实测一次标量编辑因此多花 50ms 全量重解）。
                    var target = pendingRebuildAnchor ?? anchor;
                    rebuildScheduled = false;
                    // 用户正在行内编辑器里打字：这一刻重建会把承载光标的行换掉（焦点掉到列表上，第二个字符就丢了）。
                    // 记下来，等焦点离开行视图时再补跑。
                    if (IsTextEditorFocused())
                    {
                        rebuildDeferred = true;
                        pendingRebuildAnchor = target;
                        ArmDeferredFlush();
                        return;
                    }
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
            using var stage = McdocWriteTrace.Stage("vm.rebuild");
            if (anchor is null)
            {
                using (McdocWriteTrace.Stage("vm.rebuildNull")) ReloadKeepingEditor();
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
                // 锚点这一层换不成（它的父容器已被上一拍的重建换掉：IsAttached 只按父链判，
                // 旧父链照样通到树根）——先按路径找本代节点，再逐级往上，实在不行才整篇重载。
                // 少了这段梯子，一次标量编辑会因为"锚点这一层重解不出来"退化成整篇重解（实测 55ms）。
                if (rebuilt is null && anchor.JsonPath is { Length: > 0 } path)
                {
                    var byPath = document.FindNode(path);
                    if (byPath is not null && !ReferenceEquals(byPath, current))
                    {
                        current = byPath;
                        rebuilt = document.RebuildNode(byPath);
                    }
                }
                for (var hop = 0; rebuilt is null && hop < 2; hop++)
                {
                    current = current?.Parent;
                    if (current is null) break;
                    rebuilt = document.RebuildNode(current);
                }
            }
            finally
            {
                loading = wasLoading;
            }
            if (rebuilt is null)
            {
                McdocWriteTrace.Log("定向重建失败→整篇重载", current,
                    "anchor=" + (anchor?.JsonPath ?? "<null>") + " valueNode=" + (current?.ValueNode is not null)
                    + " parent=" + (current?.Parent?.JsonPath ?? "<null>") + " root=" + document.FlattenedRoot);
                using (McdocWriteTrace.Stage("vm.rebuildMiss")) ReloadKeepingEditor();
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
            using var stage = McdocWriteTrace.Stage("vm.reload");
            McdocWriteTrace.LogText("整篇重载入口", null, withCaller: true);
            var wasLoading = loading;
            loading = true;
            try
            {
                document.Load(CurrentRootType(), document.RootJson);
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
            var suffix = failures is { Count: > 0 }
                ? " | ⚠ 索引失败 " + failures.Count + "：" + string.Join("；", failures.Take(2))
                : "";
            // 符号表为空 = 任何 schema 都解不出来（页面只剩一份空文档）：这条必须看得见，
            // 否则"载入后没有内容"永远归因不到数据源上
            if (resource?.McdocTypes is { TypeDefCount: 0 })
            {
                suffix += " | ⚠ 符号表为空：mcdoc 索引还没建好（字段与候选都不可用）";
            }
            return suffix;
        }

        /// <summary>
        /// 起一份默认文档。文档从哪来由配置说了算（<c>Generator.&lt;EntryId&gt;</c>）：
        /// 配了入口文件就用它，没配 / 读不到才退回"代数按声明类型算出的默认值"。
        /// </summary>
        public void LoadDefault()
        {
            CreateDocument();
            // 运行数据（注册表 + 符号表）没就绪时求出来的默认文档只是降级品（调度器解不开 → any → {}）：
            // 记下来，等数据到位由 ScheduleRuntimeRefresh 重生成一次，否则页面会一直停在"没有内容"
            defaultDocumentDegraded = !IsRuntimeReady();
            var sourceNote = "";
            try
            {
                var entryAccepted = false;
                if (TryLoadEntryDocument(out var entryText, out var entrySource))
                {
                    // 入口文件的**根形状**必须与声明一致：模板数据坏了（现场：loot_table 的模板是一份
                    // "函数数组"，而根声明是 struct）时，整页会退化成 any 原始行，看着像"求解/渲染坏了"。
                    // 这种情形不许拿坏数据建树 —— 退回代数默认值并把原因摆在状态栏。
                    if (EntryDocumentMatchesRoot(entryText, out var mismatch))
                    {
                        // 入口文件就是这份文档本身：树/文本/默认值快照都按它来
                        defaultDocumentText = entryText;
                        LoadMcdocTree(entryText);
                        sourceNote = "入口文件：" + entrySource;
                        entryAccepted = true;
                    }
                    else
                    {
                        defaultDocumentDegraded = true;
                        sourceNote = "入口文件根形状与声明不符（" + mismatch + "，来自 " + entrySource + "），已按代数默认值生成";
                    }
                }
                if (!entryAccepted)
                {
                    var root = CurrentRootType();
                    // 求值与默认值生成必须共用同一个 runtime：misode 的 getDefault 在 struct 字段 / list 项 /
                    // tuple 项上也会先 simplifyType，否则 union 字段按"声明类型"取到的是被版本门控滤掉的成员（结果是 null）
                    var runtime = document.CreateRuntime();
                    // 默认文档与"缺省字段 ⊕ / 列表追加 / 动态键提交"同一个出口：写出来的就是代数对该类型的解。
                    // 解不出可物化的值（null）时也绝不写进文档：整份文档变 null 就是"载入后没有内容"
                    var value = McdocDefaults.Resolve(root, runtime);
                    defaultDocumentText = McdocJsonFormatter.Format(
                        McdocDefaults.CanMaterialize(value) ? value : McdocJsonValue.Object());
                    LoadMcdocTree(defaultDocumentText);
                    if (sourceNote.Length == 0) sourceNote = "已按代数默认值生成";
                }
            }
            catch (Exception error)
            {
                defaultDocumentDegraded = true;
                MdocStatus = "建树失败：" + error.Message;
                return;
            }
            // 文档来源必须摆在状态栏：只看到"没有内容"却不知道它本该从哪个文件来，等于没归因
            if (sourceNote.Length > 0)
            {
                MdocStatus = MdocStatus.Length > 0 ? sourceNote + " | " + MdocStatus : sourceNote;
            }
        }

        /// <summary>
        /// 入口文件的根形状是否与声明一致：结构要对象、列表/元组/原始数组要数组、any/解不开不判。
        /// 只做形状判（读文档第一层），不看字段 —— 目的是拦住"模板本身就是另一种资源"这类坏数据。
        /// </summary>
        private bool EntryDocumentMatchesRoot(string text, out string mismatch)
        {
            mismatch = "";
            var declared = McdocDefaults.ResolveType(CurrentRootType(),
                document?.CreateRuntime(), McdocDefaults.DefaultNode());
            if (declared is null || declared.Kind is McdocTypeKind.Any or McdocTypeKind.Unsafe) return true;
            if (!MdococJsonTryParse(text, out var parsed)) return true;   // 解析不了交给 LoadMcdocTree 去报语法错
            return McdocHelpers.RootShapeMatches(parsed, declared, out mismatch);
        }

        private static bool MdococJsonTryParse(string text, out McdocJsonValue value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(text)) return false;
            try
            {
                value = McdocJsonTextParser.Parse(text).Root;
                return value is not null;
            }
            catch (Exception)
            {
                return false;
            }
        }


        /// <summary>
        /// 入口文件：<c>Configuration.json</c> 的 <c>Generator</c> 段按"每个生成器一个键"给出初始文档位置
        /// —— 键 = <see cref="EntryId"/>（大小写不敏感），值 = 相对 BaseDirectory 的文件，
        /// 或"里面取 <see cref="EntryFileName"/>"的目录。
        /// 每次建默认文档都重新读配置文件：改完配置重开窗口即生效，不用重启。
        /// </summary>
        private bool TryLoadEntryDocument(out string text, out string source)
        {
            text = null;
            var configured = LookupEntryPath(EntryId);
            if (string.IsNullOrWhiteSpace(configured))
            {
                source = "入口文件未配置（Generator." + EntryId + "）";
                return false;
            }
            var full = Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(AppContext.BaseDirectory ?? "", configured);
            if (Directory.Exists(full))
            {
                full = Path.Combine(full, EntryFileName);
            }
            source = ShowPath(full);
            if (!File.Exists(full))
            {
                source = "入口文件不存在：" + source;
                return false;
            }
            try
            {
                var raw = File.ReadAllText(full);
                // 0 字节的 empty.json 只是占位模板：它按"空文本 = {}"解析出来比代数默认值还空
                // （damage_type 会凭空多两条"缺少必填键"）。空文件按"没配"处理，真要空文档就显式写 {}。
                if (string.IsNullOrWhiteSpace(raw))
                {
                    source = "入口文件是空文件（按未配置处理）：" + source;
                    return false;
                }
                // 入口文件是"文档本身"：坏文本不许把页面顶成半成品（单根 + 可解析才收）
                if (!McdocJsonParser.TryParse(raw, out var parsed, out var syntaxError))
                {
                    source = "入口文件不是合法的单个 JSON 根：" + source + "（" + syntaxError?.Message + "）";
                    return false;
                }
                text = McdocJsonFormatter.Format(parsed);
                return true;
            }
            catch (Exception error)
            {
                source = "入口文件读取失败：" + source + "（" + error.Message + "）";
                return false;
            }
        }

        /// <summary>
        /// 取 <c>Generator</c> 段里这个入口的路径：**每次都从文件读**（配置改了立即生效），
        /// 配置文件不在了才退回 Init 已经解析好的那一份。
        /// </summary>
        private string LookupEntryPath(string entryId)
        {
            var entries = ReadGeneratorSection() ?? resource?.GenertorConfiguration;
            if (entries is null || entries.Count == 0)
            {
                return null;
            }
            if (entries.TryGetValue(entryId, out var exact) && !string.IsNullOrWhiteSpace(exact))
            {
                return exact;
            }
            // 入口名大小写各页面写法不一（旧管线按 ToLowerInvariant 存键），大小写不敏感地再找一次
            foreach (var pair in entries)
            {
                if (!string.IsNullOrWhiteSpace(pair.Value)
                    && string.Equals(pair.Key, entryId, StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value;
                }
            }
            return null;
        }

        /// <summary>读配置文件的 <c>Generator</c> 段；文件缺失/坏了返回 null（调用方退回 Init 解析的那一份）。</summary>
        private static Dictionary<string, string> ReadGeneratorSection()
        {
            try
            {
                var configPath = Path.Combine(AppContext.BaseDirectory ?? "", "Configuration.json");
                if (!File.Exists(configPath)) return null;
                return JObject.Parse(File.ReadAllText(configPath))["Generator"]?.ToObject<Dictionary<string, string>>();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>状态栏里显示"相对 BaseDirectory"的路径（用户对着资源目录找得到）。</summary>
        private static string ShowPath(string fullPath)
        {
            var baseDirectory = AppContext.BaseDirectory;
            if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(baseDirectory)) return fullPath;
            return fullPath.StartsWith(baseDirectory, StringComparison.OrdinalIgnoreCase)
                ? fullPath.Substring(baseDirectory.Length)
                : fullPath;
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
            McdocWriteTrace.LogText(
                "LoadMcdocTree由" + (new System.Diagnostics.StackTrace(1, false).GetFrame(0)?.GetMethod()?.Name ?? "?")
                + "调用 loading=" + loading + " isDocumentRoot=" + (document?.RootJson is not null), text);
            // 与 DigestEditorText 同一条口径：先归一"看不见的空白"（粘贴来源），再严格解析。
            var normalizedText = McdocJsonParser.NormalizeInvisible(text, out var replacedInvisible);
            if (replacedInvisible > 0)
            {
                McdocWriteTrace.Log("文本含不可见空白字符，按空格归一", null,
                    "replaced=" + replacedInvisible + " 来源=LoadMcdocTree");
                text = normalizedText;
                MdocStatus = "已把 " + replacedInvisible + " 个不可见字符（BOM / 不换行空格 / 全角空格）按空格处理";
            }
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
                using (McdocWriteTrace.Stage("vm.loadTree"))
                {
                    document.Load(CurrentRootType(),
                        jsonEditor is not null ? jsonEditor.State.Root : parsedText);
                }
            }
            finally
            {
                loading = wasLoading;
            }
            // 必填闭合下拉的"默认选中"落盘 + 重建：依赖它的 spread/调度器才会展开（advancement.criteria.*.trigger）。
            // 这一次是"渲染完毕"：行上显示的数字/bool/union 标量默认值也一起落盘（所见即所得）
            var materializedOnLoad = MaterializeUntilStable(includeDisplayedDefaults: true);
            projector.Reset(document.Nodes.ToList());
            // 投影会把"记住的展开态"里的子树就地建出来（补物化的钩子只在**展开手势**那条路上跑），
            // 于是载入这一拍是"行上已经显示、物化还没跑"：派发器成员解出的必填字段（type_specific.variant）
            // 只在行上。这里再跑一次不动点，才算真的"渲染完毕"→ 文本也才是所见即所得。
            var materializedAfterProjection = MaterializeUntilStable();
            if (materializedAfterProjection > 0)
            {
                materializedOnLoad += materializedAfterProjection;
                projector.Reset(document.Nodes.ToList());
            }
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
        #endregion

        #region Event
        partial void OnSelectedGameVersionOptionChanged(EnumMember value)
        {
            if (value?.Name is { Length: > 0 } version)
            {
                ApplyGameVersion(version);
            }
        }

        /// <summary>编辑器里改了文本：防抖 300ms 后重建树。</summary>
        partial void OnJsonTextChanged(string value)
        {
            if (suppressReload) return;
            ScheduleReload();
        }
        #endregion
    }
}
