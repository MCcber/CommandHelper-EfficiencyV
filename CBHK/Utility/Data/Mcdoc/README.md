# Mcdoc 树渲染管线：业务模型与踩坑备忘

> 只写三件事：「业务怎么理解」（含两个视图必须同时成立的契约）、「哪里容易踩坑」、「下一步要做什么」。
> 不写功能清单、不写实现方案、不写进度、不写实测数字、不写文件目录 —— 写完的功能从「下一步」挪走，只有踩过的坑留下来。

## 一、术语速查

| 词 | 含义 |
|---|---|
| dispatcher | mcdoc 的 `minecraft:resource[<registry>]`：按资源 id 落到具体 schema。本项目拿它当"页面入口" |
| EntryId | 页面传入的 dispatcher id（如 `dimension_type`）；`McdocGeneratorViewModelBase` 构造参数 |
| spread | `...SomeType`：把另一个结构的字段摊平进来，simplify 阶段落地 |
| `#[id]` | 字段值的取值来源标注（registry / tags / include / exclude / prefix），决定下拉候选 |
| union / unset | 联合类型；下拉里的"（未设置）"哨兵表示**删掉这个键** |
| inline tuple | ≤4 且全数值的元组，一行内渲染完，不产子行 |
| Head / Row | Head 是 presenter 产出的"这行怎么画、能点哪些按钮"；Row 是投影出来的扁平行对象 |
| 懒建 | 子级到展开时才建（`EnsureChildren`），默认展开深度 3 |
| NF / 指令 | 归一形态 vs 求值指令（Reference/Concrete/Template/Mapped/Indexed/Dispatcher），见 §二 |
| RebuildAnchor | 最近的容器祖先：运行期依赖变化时"从哪一层重建" |
| 键身份 | 一条 entry 在 JSON 里的位置：`FieldName` + `JsonPath` + `KeyNode`(`RuntimeKey`)。`%key` 类动态索引全靠它 |

## 二、求解模型：混合结构怎么归一

### 2.1 归一形态（NF）

引擎里"能被渲染 / 被索引 / 被合并"的只有：

`Any / Boolean / String / 数值 / Literal / Enum / Struct / Union / List / Tuple / 字节数组`

其余六种是**求值指令**，必须在求值期消掉、不许出现在节点上：

`Reference / Concrete / Template / Mapped / Indexed / Dispatcher`

### 2.2 两个上下文

| 上下文 | 作用 | 生命周期 |
|---|---|---|
| `TypeArgs` | 排队中的类型实参，只被"下一个 Template / 泛型 Dispatcher"消费一次 | `Concrete` 写入 → 被消费后清空 |
| `TypeMapping` | 形参→类型的替换环境；结构体字段以 `Mapped{copy}` 自带环境（唯一惰性边界） | Template/Mapped 写入，可继承 |

规则：**任何指令都不许丢弃它不消费的上下文**。

### 2.3 指令消去规则

| 指令 | 规则 |
|---|---|
| Reference | 命中 `TypeMapping` → 直接替换；否则按"声明模块 + use 别名 + 祖先模块"解析后**带同一上下文**继续 |
| Concrete | 实参先按当前上下文求值 → 写入 `TypeArgs` → 求值 Item（保留 `TypeMapping`） |
| Template | `TypeArgs` → `Mapping`（外层为底、内层覆盖；缺省 `any`）→ 清 `TypeArgs` → 求值 body |
| Mapped | 合并 mapping（外层为底、内层覆盖）→ 求值 Item |
| Indexed | **带同一上下文**求值 child（不清 `TypeArgs`）→ 按索引代数作用在 NF 上；无索引时等价恒等 |
| Dispatcher | 解析成员目标；`TypeParams` 非空时用当前实参替换后再求值 |

### 2.4 索引代数（定义域 = 全部 NF）

| 被索引的 NF | 语义 |
|---|---|
| Struct | 按字段名 / 动态键（`Tuple[键,值]`）取类型；未命中走 `%unknown` / fallback |
| Union | 逐成员索引后取**并集**（去重保序，非 struct 成员跳过并报告，上限 64，全失败才 `any`） |
| Tuple | 静态下标取项；动态索引对所有项求值后并集；越界 → `any` |
| List | 取 `Item` |
| Any | `any` |
| 其它 | `any` + 诊断（可见，不静默） |

### 2.5 节点边界与诊断

- 工厂求值后若仍是指令 → **换成 `any` + 保留诊断**（"未解开的类型包装：X"）。渲染层因此只看到 NF：Struct/Union 永远不会是 Text/Raw，`any` 是唯一 Raw 兜底且**必须带诊断**。
- 求值期的诊断（引用解不开 / 未知调度器 / 不可索引 / 联合成员未解出）统一挂到行的 `Error` 上。
- 空联合 `()`（文档里真的声明成空）是合法 NF，单独给"存在性谓词"诊断。

### 2.6 两条容易被忘的语义

- **spread 的 union 语义**：`...U`（U 是 union）合并所有 struct 成员的字段；非 struct 成员报告跳过；一个都没合并时补"未知字段"占位。
- **mapping 合并**：`Template`/`Mapped` 的映射以外层为底、内层覆盖，模板体里引用外层形参不丢键。

### 2.7 一份文档、两个视图：必须同时成立的契约

用户只看得见"左边树、右边文本、和一份文档"，所以这些不是"测试断言"，而是这个编辑器对用户承诺的东西：

| 契约 | 含义（用户视角） |
|---|---|
| 文本单个根 | 右边的文本永远是一份合法 JSON。语法错误期间树冻结、**程序不替你改文本**；但你一旦在左侧动了树，就按下面那条把脏区域对齐成合法文本 |
| 视图同代 | 屏幕上文本 = 模型里的文本，不能一个改了另一个没跟上 |
| AST 同源 | 树的节点与文本共用同一批对象，否则"树上的编辑永远进不了文本" |
| 行 → 文档 | 一行显示的值就是文档里该路径的值；**比"值"不比"对象身份"**（整篇重排会重建对象） |
| 无孤儿行 | 每一行都能挂到一个存在的父行上 |
| 缩进契约 | 子行必须比父行深（缺省/压缩层可以跳，但绝不许同层） |
| 缺省字段不摊开 | 文档里没有的可选字段只有一行 ⊕，不许挂子级 |
| 键 ↔ 行双射 | 文档里每个键都有对应行，且只有一行（不许"文件里有、树里没有"或残骸行） |
| 无重复键 | 同一个对象里不许出现两个同名键（物化/追加最容易造出来） |
| 导出 = 屏幕 | 复制/保存出来的就是屏幕上那份文本 |
| 换版本不改文档 | 切游戏版本只换 schema 求值口径，不许动用户已经写的键值 |
| 会话干净 | 关窗体再开是干净的一份默认文档（折叠态、未消化输入都不许带过来） |
| 树侧操作 × 右侧语法错误 | 定位"能包含住脏区域的最小完整结构"，用它**左侧模型**里的同一份整段替换 → 严格解析验收 → 整篇写回 → 排版；定位不到可替换的结构就一个字符都不改（不猜 `}`、不补引号） |

## 三、踩坑清单（都真踩过）

- **非法形状必须给"诚实的形状"**：声明/求值是容器却拿到标量（或反过来）时，节点一律降级成 `any` + 错误诊断（`值与类型不符：期望 X，实际 Y`），**绝不按声明类型摊开子级**。否则树会长出"父是字符串、子却在展开"的幽灵行（`$.criteria.gate.trigger`），"每个键恰好一行"当场就不成立。子级继承也要钉死：父 JSON 不是 Object 时 `fieldJson` 必须是 null。
- **行与文档比"值"，不比"对象身份"**：一次整篇重排会重建对象，引用相等不是用户看得见的契约；门禁里"行的值 = 文档里该路径的值"用内容比较（缓存残骸"行有值、文档没有"照样会红）。真正的身份契约只在"树节点与编辑器共用同一批 AST"那条（S8 / I3）上守。
- **跨资源存在性只给 warn**：字段带 `#[id=<registry>]` 且值不在注册表里 → 行上挂 warn（不 error、不改值、不弹选择）。拿不到注册表数据时**不判**（离线/版本没有该注册表），但要在 trace 里说清楚，别把"没验过"当成"验过了"。判定走 `IMcdocRegistryOptions.Contains`（实例级缓存，id 空间不许跨实例/跨版本串）。

### WPF / XAML

- **`ListCollectionView` 不支持范围操作**：多行 Add/Remove 抛"不支持范围操作"。拼接必须"改数据 + 单次 Reset"，滚动位置另做锚定。
- **`ContentTemplate` 会压掉 `ContentTemplateSelector`**：同时设 `ContentTemplate` 后选择器不再被调用（所有编辑器突然消失）。
- **`Path.Data` / `VectorFlatIconButton.Icon` 只吃 `Geometry`**：`Icon.xaml` 里多是 `DrawingImage`，部分条目还引用 png，离开宿主程序会抛"找不到资源"。
- **同程序集 xmlns 不要写 `assembly=CBHK`**：只写 `clr-namespace:...`。
- **`StaticResource` 找不到就抛，`DynamicResource` 静默降级**：跨词典引用统一 Dynamic。
- **`Style.BasedOn` / `TargetType` 不能用 `DynamicResource`**：它们是 CLR 属性，写成 DynamicResource 的 Style 一实例化就抛 `XamlParseException`（图标下拉就这么坏过）。
- **`VectorTextBox` 自带的 `WaterMarkerText` 在行内看不见**：内容宿主的不透明底盖住水印；行内空值占位要自己叠一层 `TextBlock`。
- **附加属性做不了事件触发器**：挂行为用附加属性 + `Loaded` 回调；`CallMethodAction` 指向不存在的方法只静默报绑定错。
- **`TextBlock.Text` 与 `Inlines` 不能同时设**：doc 面板要富文本就得删 `Text`、代码赋 `Inlines`；`FlowDocument` 被复用容器抢挂会抛异常。
- **`Documentation` 带 `///` 前缀、多行还是逗号拼的**：渲染前必须逐行剥 `///`/`//!` 与行首逗号。
- **附加属性 DP 值不变就不会重渲染**：断言里判附加属性比判文本稳。
- **必填默认值的物化必须跑到「不动点」，而且每条重建路径都要跑**：物化写一个键就会重建文档，重建又让派发器/动态索引解出新的必填字段（`type_specific.type` → `...entity_sub_predicate[[type]]` → `BoatPredicate.variant`）。只跑一轮的结果是「行上显示 Oak、文档里没有 variant」，依赖它的下游索引永远停在未求解。
- **懒建节点展开必须先 `EnsureChildren`**：展开走的是 `SpliceExpansion → AppendChildren`，不经过 `Collect` 的 EnsureChildren；漏了这一步，深度 ≥ 3 的节点点箭头只会得到 0 行（「展开没反应」）。补在 `CollectChildren` 里，同时给出「刚建出子级」的钩子（补物化 → 延迟一拍重建）。
- **行模板里的裸 `Button` 会吃系统主题 chrome**：仓里没有隐式 Button 样式，`Background="Transparent"` 压不过主题模板 → 标签变成白色小方块；只有本地 ControlTemplate（仅 `ContentPresenter`）才彻底。
- **`VectorTextBox` 的底色在构造函数里是「本地值」**：`SetResourceReference(ThemeBackground, CommonBackground)` 压过一切 Style setter（包括 `BasedOn` 派生的透明样式）—— 行内想要无底色，只能用「XAML 上显式的本地值」或「改控件本身把默认值挪进样式」；`UpdateBorderColorByBackgroundColor` 遇到透明主题必须直接跳过重刷，否则会把底色再刷回去。
- **展开箭头的判据用「真的建出了子级」**：按类型猜（`CanHaveChildren`）会让有子级的行没箭头、空 union 挂死箭头。已建就数 `Children().Count`，没建才按能力预测；可选且文档里没这个键的仍然只给一行 ⊕。

- **派发器判别键要在「求值前」落盘**：`...minecraft:entity_sub_predicate[[type]]` 是在 `McdocSimplifier.Simplify` 里消费同层取值的，等节点建完再物化就已经晚了一步。`McdocNodeFactory.CreateNode` 现在先按已求值的结构写「必填 + 有闭合候选」的字段，再用同一份 JSON 重求值一次；候选必须走 `McdocIdSpec.FromAttributes` → `runtime.IdSpaces.Resolve` 这条出口（也就是 `McdocType.IdMembers` 的来源）：拿一次性演示节点去问 `McdocSelectOptions.Apply`，在 union 标量支上会静默拿不到候选 —— 表现是「判别键根本没写进文档、父结构的成员一个都没解出来」。

- **整篇写回只能有一个执行者**：`Reformat`（保 AST 身份）与 `Reload`（换一代 AST）在同一拍互相覆盖时，文本会变成"用户文本 + 树回写文本"（报 `Expected depth to be zero at the end of the JSON payload`，屏幕上那段文本却完全合法）。现在两者都走 `JsonTextState.WriteWholeText`：重入只记"最后一次"（排队中的 `Reload` 不许被"用即将作废的 AST 算出的重排快照"顶掉）、写完校验文档真的等于目标文本（不中就重算边界再写一遍）、写回与锚点重建在同一次调用里完成。判据是写完 `TryParse` 仍是单个根（门禁 `--text-generation` / ui-gate `S35`）。
- **`Reload` 不许换 `JsonTextState` 实例**：换实例后外部持有者（树、上一次调用的栈帧）继续写在旧代上，文本与 AST 从此不同代（表现是"改了值 JSON 不动 / 再敲一个字就丢"）。原地重解析即可，写回与重解析必须同一次调用。
- **"键被删掉"必须折叠，投影侧也要兜底**：可选字段的 `Json` 变成 null（右侧把整段结构删了）时，节点若还留着 `IsExpanded = true`，投影就会把**早就建好的子级**重新摊到行下面 —— 现象是"右侧删了整段 `display`，左侧 `display` 的子级还在"。两处都要守：`McdocTreeNode.Json` 变 null 且非必选时立刻折叠；`McdocRowProjector.Collect` 对"可选 + 没有 JSON"的节点直接不拼子级（折叠状态脏了也不会漏）。判据见 ui-gate `S42`。
- **折叠态是静态表，必须按"页面实例"分会话**：只按 EntryId 分会话时，关窗体再开（新 VM、新编辑器）会把上一个窗口的展开态带进来 —— 于是"文档里根本没有的字段"被撑开、子行凭空出现，父行还是个 ⊕，看着像"子结构摊到了同层"。会话键 = (EntryId, 页面实例)；同时 `json == null` 的字段一律只留一行 ⊕（记住的展开态不许撑开它）。判据见 ui-gate `S41`。
- **整篇写回必须保住光标**：整篇替换（Reformat / Reload）会把光标夹到文末，用户**逐字符**继续敲的字就落到根外面，报 `Expected depth to be zero at the end of the JSON payload`（行号正是根结束那行）——看起来像"写了 `criteria.test` 但左侧模板崩了"，其实左侧渲染的是上一代树（文本非法期间树按设计不动）。修法：写回前把光标定位到"所在的最内层 JSON 节点 + 相对偏移"，写回后按同一节点（保身份的 Reformat）的新区间落点；换 AST 的 Reload 才退化成夹紧。判据见 ui-gate `S38`。
- **文本→树的差量同步必须先 Relink 再重建锚点**：`McdocSyncEngine` 若先 `RebuildNode(锚点)` 再 `RelinkToRoot`，重建用的是**旧** `node.Json` —— 重建出来的子树里没有新键（现象：JSON 里多了 `criteria.test`，树上只有"新键"占位行，条目行不存在）。顺序固定为"值补丁 → Relink/AdoptRoot → 重建锚点子树"。判据见 ui-gate `S37`。
- **消化完文本后 `JsonText` 要取编辑器当前文本**：消化过程中树侧可能已经整篇回写（物化必填键 / 重建后重排），拿进来时的快照回写会让 `JsonText` 与屏幕上的 JSON 不同代 —— 复制/保存导出的就是上一份。
- **整篇替换文本会杀掉 `ITextAnchor`**：`JsonTextEditor.Reload` / `JsonTextState.Reformat` 都是 `Document.Replace(0, len, text)`，语法波浪线的锚点随即失效，下一次绘制在 `WaveUnderlineBackgroundRenderer.Draw` 里抛 `InvalidOperationException`（删节点必崩就是这么来的）。整篇写之前先 `overlay.Clear()`，renderer 自己也要判 `IsDeleted`。
- **右侧语法错误 ≠ 永远不许动文本**：今天分成两档 —— 用户在**右侧打字**时，树侧一个字符都不许改（等他自己修括号）；用户一旦操作**左侧树**，就要把脏区域对齐成合法文本，否则"我点了树却什么都没发生"比改错更糟。对齐只做**容器级替换**（找包住脏区域的最小完整结构，用模型里同一份整段换掉），绝不猜字符；局部拼接的前提是"脏容器之外词法完整"（祖先也没闭合就只能整篇以模型为准）。判据见 ui-gate `S43 文本非法时树侧改值 / 脏容器被完整容器包住 / 定位不到可替换结构`。
- **右侧打字期间，树侧不许覆盖用户还没消化的文本**：「有未消化输入（`textPendingSync`）或右侧语法错误（`textParseFailed`）」时，`SyncJsonText` 必须先消化文本再回写（这条只管"文本通道"；**左侧树操作**走下面那条容器级对齐）；消化不了就放弃这一拍并给状态提示。以前无条件 `Reformat`，用户删掉整行 + 末尾逗号会被旧 JSON 立刻顶回去。
- **新建祖先容器键也必须走 `SetMemberOrdered`**：`EnsurePath` 用 `parentJson.Add` 是纯追加，后出现的 `parent` 会落到 `sends_telemetry_event` 后面；树里找不到后续兄弟时按 `Type.Fields` 的声明序插入。

- **缺省成员不许在"饿建期"把子结构建出来**：`LazyChildren = false`（sweep / pipe 为了"行数==节点数"都关掉懒建）时，presenter 里 `node.Parent is not null && json is null` 这类守卫**恒为 false** —— `Parent` 只在本节点的 `RefreshChildren` 返回之后才由父级赋值。于是缺省的可选成员照样把整棵子树建出来，而投影对这类节点一列子行都不画（只留一行 ⊕）→ "节点建出来却不可见"，`行数 == 节点数` 从 1151 掉到 850。修法：由**父级**在 `context.Child(..., deferChildren: ...)` 上说清"这是缺省成员"，工厂据此跳过饿建（必填成员不 defer：非法文档仍要摊开子行 + 挂诊断）。
- **判据要按形态**：union 切到**列表/元组**支时"0 子级"不是空壳（列表没值就没有项，入口是 add）；空壳指的是结构支里的同名行 / 空 `Any` 行。R5 早写过同一政策（结构体是 ⊕、列表是 add、联合是 unset），U2 现在照同一口径判。
- **根层的"新增成员"不许再进差分锚点**：`Diff` 对"旧值不存在"的键会加锚点，锚点循环随即把它重建一遍 —— 而重建只有声明类型，拿不会建树期的语义（`IsUnknownField`、动态键身份、`IsRequired`、"缺少必填键"诊断）。现象是"未声明键的行退化成 Plain 行，占位子行却还在"。根层成员由 `CreateRootMember` 建好后就跳过递归差分，锚点只留给"确实换了形状的老节点"。
- **`RebuildNode` 必须把父级写的字段语义带回来**：必选性 / 文档注释 / 键提示是**父级表现器建完子节点之后**才写的，重建只按声明类型重跑一遍，`IsRequired` 就会退回默认的 `true` —— 现象是"可选字段突然变必填，行上的「清空」让位给 ⊕"（ui-gate `S20-4`）。以前没露出来是因为重建结果被投影丢掉了（见下一条），接上之后当场显形。
- **摊平根的顶层节点重建必须同时换 `Nodes` 里的那一项**：`Nodes` 是投影与一致性守卫的来源，只换 `rootNode.Children` 的话视图还指着旧实例（子行永不更新），而 `RefreshAround` 又因为父行是合成根而返回 false → 整表重投影。判据是 ui-gate `S43 根层` 的"整表重投影 = 0"。
- **顶层区的行重拼只能按"第几个 `Depth == 0` 行"定位**：重建过的顶层行实例已经换掉了，按行对象/节点查索引会落空；按路径找又会命中同路径的占位行（D1 同款）。`McdocRowProjector.SpliceTopLevels` 拿"上一代/这一代顶层列表"求公共前后缀，再按序号算行区间。
- **`RebuildNode` 不能把 ⊕ 行的折叠态继承给新长出来的值**：旧节点是 `json == null` 的占位行时，`rebuilt.IsExpanded = node.IsExpanded`（false）会让"刚补上值的 display"永远折叠着，而整树重建出来却是展开的 —— 两个视图观感不同代。只有旧节点本来有值时才继承。
- **`Diff` 里"对象键集合变了就整体当锚点"对根对象不成立**：根没有行可挂，按老规则只剩整树重建一条路。根层改成"成员增删单独处理 + 公共键继续往下差分"，公共键的相对顺序一旦变了就直接回退（树序按声明序 + JSON 序，局部同步不负责重排）。
- **行视图的 `OnPreviewKeyDown` 在隧道阶段抢在行内编辑器前面**：判归属必须按"事件目标"（`TextBoxBase` / 可输入 `ComboBox` / `McdocKeyBox`），并且**没有目标行时不许兜底到第一行**——行上的按钮不选中行，`SelectedIndex` 是 -1，兜底就等于"重命名框里的左右键和回车全作用到第一行 display"，把它展开后子结构紧跟在它后面，看着像"子结构摊到了同层"（投影没问题，子行本来就是 depth+1）。回车在 `McdocKeyBox` 里 = 点「加入」。

### 求值 / 数据

- **ComboBox 转换器会改字面量类型**：往返后 `LiteralValue` 从 `int` 变 `string`；unset 判断必须同时认 int / decimal / `"-1"`。
- **转换器必须返回"稳定实例"**：`SelectedItem` 不是集合里同一对象引用就会被清空（"选完弹回"）。转换器按「名字 + 字面量」缓存。
- **`VectorColorPicker.HexCode` 是私有 setter**：只能绑 `SelectedColor`，字符串字段配双向转换器。
- **"行上显示了默认值"不等于"JSON 里有这个值"**：必填 + 白名单闭合下拉会自动选中第一个候选但建树期不落盘；依赖它的动态 spread 会解析成 `%none`（`advancement.criteria.<name>.trigger` 的子级整片空）。修法：只对**必填**字段在载入/结构变更后物化默认选中值再重建。
- **物化写成员不能用纯追加**：父对象里常有同名的空值（`trigger: ""`），追加后成 `{"trigger":"","trigger":"minecraft:…"}`，dispatcher 读第一个 → spread 依旧空，行上却"已选中候选"。
- **物化的容器不许"往上找最近有 JSON 的祖先"**：那是把子键提到根上的唯一通道（删 `display` 却出现 `{ "title": "x" }`）。只有 union 摊平出来的分支层可以借容器。
- **删字段要连 JSON 一起断**：只清子级不清 `node.Json` 时，残留值会被后续物化/回写当成"还在文档里"。
- **值从"没有"变"有"之后必须放开子级**：删字段会把 `ChildrenBuilt` 冻结，再 ⊕ 时 `{}` 回来了但子行回不来（"结构有了、行没回来"）。
- **`IsDocumentRoot` 必须有赋值点**：只有"在 `Nodes` 里且没有父级"的节点才是文档根；漏赋值时根标量编辑会走进"替换根"的拒绝分支，值只留在节点上、文档没动。
- **摊平后合成根不在 `Nodes` 里**：`Nodes` 的顶层节点是它的子级，按路径重指碰不到它 → 顶层增删写进旧对象。
- **动态键在 MetaType 侧是"字段类型 = `Tuple[键类型, 值类型]`"**：泛型形参落在键上时 `field.Key` 为空，模板替换必须先拆这个 Tuple 分别求值，否则键退化成 `Any`、候选与泛型替换一起丢。
- **动态键的 key 类型常常是 `type` 别名**：属性挂在别名目标上，解析必须按符号表穿透 `Reference` 再取属性。
- **union 选「未设置」不等于只删键**：删键之外还得清**分支镜像**（`ScalarEditorKind` / `EnumOptionList` / `SelectedEnumOption` / `UnionListLike` / `UseSelect`）、清 `SelectedUnionChildren`、折叠，并且**返回"真的变了"**让行重投影（`RefreshHead` + `ReplaceRow`）。少任何一步，行上就留一个写着旧值、点不动的死编辑器（"选了未设置，行里还有 `minecraft:allay`，右边 JSON 里却已经没有这个键"）。判据见 ui-gate `S43 树上把 union 切回未设置`。
- **`McdocRow.Head` 是"构造时求值 + 缓存"**：union 行必须不缓存（切分支会改编辑器/分支候选），否则"切分支后编辑器不回来"。
- **`indexed` 不许清 `TypeArgs`**：`Concrete{Item=Indexed(Dispatcher), Items=[…]}` 是"泛型 dispatcher"的唯一写法，清掉实参目标模板就拿不到。
- **模板形参引用要按最后一段名匹配**：`Mapped` 里的 `Reference` 可能带模块前缀（`::mod::T`）。
- **`SimplifyStruct` 对"已归一的动态键字段"必须原样保留**：再走一遍会把"键是 union"的字段按成员展开（3 支 → 9 支），破坏幂等。
- **id 空间不能做跨实例静态缓存**：按 (registry, version) 当键会把不同实例的结果串在一起。
- **`use a::b as Alias` 的别名要由 visitor 带出来**：别名是 `useStatement` 的独立子节点，只取 `path()` 会丢掉 `as Alias`（表现为 `Alias` 解析成 `any`）。
- **单字母 `B` 不能当类型形参名**：`type X<A, B>` 只会解析出一个形参；用 `A, C` 或 `Base, Spread`。
- **动态 map 占位行每轮只能加一个键**：否则 walk 永不收敛。
- **`EnsureContainer` 只挂一层父容器**：给"祖先是缺省可选字段"的 map 加第一条键时无处挂载。
- **分页上限与"全部"冲突**：`ItemsShown == int.MaxValue` 要绕过 `PageSize` 截断；More 行的重投影要落在**列表行**上。
- **懒建后不能用 `Children.Count` 判有无子级**：要问 presenter 的 `CanHaveChildren`。
- **`McdocJsonTextParser` 从不抛异常**：坏文本返回部分结果，直接建树会覆盖旧树。
- **`Reformat()` 不许换 AST 身份**：换掉 `State.Root` 后树上节点还引用旧 AST，树上的编辑永远进不了文本。
- **建树期不能回写文本**：`loading` 期不仅挡值回写，文本同步也要跳过。
- **文本相等 ≠ AST 新鲜**：编辑器里敲完字 `State.Root` 还停在上次解析，重建前必须先重解析。
- **改了参与动态索引的值必须重建，但只能延迟一拍**：同步重建会打断动态键输入（占位节点被换掉 → "加了键但 JSON 里没有"）。
- **定向重建按路径找锚点会命中"同路径的兄弟节点"**：动态键占位行的 `JsonPath` 与父容器相同，`map[path] = node` 的索引又是子覆盖父 → `FindNode(锚点路径)` 拿到占位行，`RebuildNode` 把它就地重建成"没有键身份"的匿名条目行（表现：文件里有、树里没有；`%key` 空 → 成员退化成 `any`）。修法：索引先写优先 + 跳过占位行、锚点本体优先、`RebuildNode` 拒绝无 `ValueNode` 的节点。
- **求值诊断只写日志＝用户永远看不到**：索引类诊断（不可索引 / 越界 / 超 64 / spread 非 struct / 形参数量不符 / 动态键没命中成员）原先只进 `Runtime.Warn`。改成同时写 `SimplifyContext.Diagnostics`，由工厂统一挂到行的 `Error` 上（只留前 5 条，其余给计数）。
- **建树期分支不许走 `GetChange`**：`UnionBranchBuilder` 里做值转换，等于把分支子树建在"新成员的默认值"上——文档里那份 JSON 一个字段都没传下去，于是所有"靠同级键的动态索引"（`...dispatch[[type]]`）在分支内部全部取不到键。分支子树必须用**文档值**建；`GetChange` 只在用户真的换分支时用（`SwitchTo` → `ConvertJsonToSelectedBranch`）。
- **`#[id]` 候选必须过版本过滤**：调度器成员带 `#[until=1.21.5]` 这类标注时，`GetDispatcherMembers` 仍然会把它列出来；候选不过滤就会让用户选到"求解期会被滤掉"的键——spread 解不出、只剩一条"没有结构成员"的诊断，且切走再切回也不会自愈（值一直在文档里）。
- **解不出来的成员不许粘在选择上**：声明不是 `any`、求值却退成 `any` 的分支（例如 NBT 分支要靠同级 `type` 才能解）切过去要**立刻回 unset + 挂诊断**（misode 同款）；否则行上留一个"看着有编辑器、其实写不出合法值"的死分支。判据用**声明形态 vs 求值形态**，不许按名字特判。
- **`Indexed` 全被过滤掉时不许返回空联合**：空联合会被下游当成"文档里声明为 `()`"，也会让 spread 走进"目标联合没有结构成员"的假象；解不开就退 `any` + 诊断。
- **`Prepare` 里读不到字段的必选性**：`child.IsRequired` 是父级建完子节点之后才写的，`Prepare` 期间还是默认值 —— 拿它判断"必填 bool 默认 false"会把可选 bool 也落成 false（且行上假装 True 已选中）。bool 的默认值判定放到物化阶段（那一刻 `IsRequired` 已就位）。
- **清空语义必须走同一条出口**：数字清空 / bool 两个都取消 / 可选字符串清空 = unset（删键），必填 = 挂诊断；否则会出现"行上没了、JSON 还在"或反过来的假象。
- **成员写入按树序，不按到达顺序**：`SetMember` 的纯追加会让新键永远排到末尾，和左侧树的相对位置对不上（切 Enum、补默认值、⊕ 都受影响）。
- **值补丁不重建，诊断必须按节点重算**：文本→树的值补丁只"改 `node.Json` + 重指向"，`McdocValidator` 的整树校验不会跑 —— 表现是"JSON 里改回合法值，行上的感叹号去不掉"。值只影响它自己，所以补丁后按节点跑一次 `ValidateNode`，并把它在文档级清单里的旧条目换掉（状态栏计数才不停在上一次）。

### 页面 / 宿主

- **`Console.ReadKey()` 会让被拉起的服务器秒退**：宿主是 WinExe（**没有控制台**），`ReadKey` 直接抛异常——表现为"进程起来了但管道永远不出现"。
- **`VisualTreeHelper.GetParent(Run)` 会抛异常**：doc 面板的 `Run`（`FrameworkContentElement`，**不是 Visual**），鼠标划过就崩。上溯父级必须 `ContentOperations.GetParent` 兜底。
- **局部重建会清空 union 的分支子级，而"选中项没变"时 setter 会早退**：表现是"点一次 ⊕，父节点的子级整片消失"。`PopulateChildren` 末尾必须无条件补建一次。
- **union 结构分支是懒建的，不能只看 `Children.Count`**：会误判成"没有字段"，于是整支自己挂成子行 → `player → player` 同名空壳。必须先 `EnsureChildren`；列表/元组/any 分支保留子行但标签带形态后缀。
- **候选外的值不猜、不补候选**：只挂 warn 诊断，JSON 与选中项都不动。为了"让它显示出来"在校准里回写选择，会把用户刚选的选择弹回去。
- **无超时的 `ConnectAsync()` 比崩溃更糟**：服务器没起时 `Init()` 永远不返回 → 骨架屏不消失、符号表为空、所有 `#[id]` 变文本框，而且一条日志都没有。
- **生成器页面只声明 EntryId**：所有入口页统一继承基类，页面里不许自带引擎逻辑（同步/默认值/防抖重建只有一份）。
- **`Resource` 是 Prism 单例**，VM 走构造注入；`Init()` 没跑过，任何页面都建不出树。
- **AvalonEdit 必须用 `JsonTextEditor`**：只有它持有 `JsonTextState`，树与文本才能共用同一批 `JsonValue`。
- **编辑器 `Text` 不是依赖属性**：禁止在 XAML 里绑 `Text`（真机 `XamlParseException`）。
- **visitor 是"new + 逐属性拷"**：新增的属性载体**必须加进拷贝清单**，否则只活到中间对象上（裸 `#[id]` 就这样丢过一轮）。
- **批量改 XAML/代码时锚点会因缩进静默失配**：替换后必须打印命中数（0 = 没改到）。
- **旧管线还在仓里**：XAML 里 `VectorTreeView` + `MCDocumentTreeViewTemplateSelector` 是旧管线，`McdocTreeRowView` + `McdocRowTemplateSelector` 才是新管线；新功能只加到 Mcdoc 目录，旧模板只许增量。

### 验证

- **"改一个值"和"换一份文档"是两种代价，不能混着量**：整篇重排（格式化 + 整段替换 + 重解析）在 600 行文档上是秒级；改一个已存在的标量值必须走"只替换那一段文本 + 只刷那一行 + 只校那一个节点"，否则用户每次点一下都要等一秒。反过来，值变成容器/多行、可选 bool 的"都不选"（那是删键）、锚点缺失、union 结构分支这四类**必须**退回整篇，硬做手术式会写坏文本。
- **投影守卫不能只看"树换代"**：拼接（`SpliceSubtree`）也会造出"行指向的节点不在树上"的孤儿行，而它不换树代 —— 守卫的触发键要含局部拼接/整表重投影的次数。
- **文本非法或未消化时，树侧改动只许动模型、不许动文本**：此时锚点区间与文本已经对不上，任何"就地替换那一段"都会啃掉用户写的括号；判断依据是"有没有未消化的输入 / 上次解析是否失败"。
- **门禁判据只能往"操作 × 契约"的矩阵里加**：一次性用例红了只会说"这条红了"，说不出破坏了哪条契约；往矩阵里加操作，失败信息才指得准（契约清单见 §2.7）。
- **断言要打 UI 路径**：直接给行模型赋哨兵测不到真 bug（转换器会改类型）；有转换器/模板参与就必须走 Convert/ConvertBack 或真实控件。
- **sweep 必须关闭分页与懒建**，否则"行数==节点数"不成立。
- **探针与 harness 都要显式设定确定性开关**（懒建 / 分页 / DeferEditors）。
- **纯 `Dispatcher.Invoke` 空转不物化容器**：只认 `ItemContainerGenerator`，别拿 `Items.Count` 当渲染证据。
- **`Popup` 只有真实外部鼠标按下才会关**：合成 `RaiseEvent` 关不掉（独立 HWND），要真点；Ctrl+点击同理要真按键。
- **换 JSON 之后要等防抖重建**：抓行/抓节点前等"行数与文本都不再变"，否则拿到上一代行对象。
- **虚拟化复用要双判**：既比容器下标，也比"渲染文本属于本行模型"。
- **`ContentControl.ContentTemplate` 读不到选择器结果**：要断言编辑器换没换，只能看 `ContentPresenter` 里实际物化出的控件类型。
- **绑到不存在的属性不抛异常**：`{Binding Missing}` 只记一条绑定错误、按钮照样可见可点；靠静态守卫兜。
- **剪贴板会被人占住**：`SetText` 抛 `CLRBRD_E_CANT_OPEN`；门禁两次尝试，都失败记 skip 并打印原因。
- **门禁的 SKIP 会掩盖整条链**：`dimension_type.attributes` 在快照里是 1.21.9+ 才有，而版本下拉只列本地 Registries（最高 1.21.8），于是 attributes 那条 UI 链长期整段跳过（后续断言全是死代码）。修法：门禁走页面自己的换版本出口直接切到快照版本，别让"环境没有"变成"验过了"。
- **原生进程必须带工作目录**：对拍脚本里 node 侧用相对路径，工作目录设错就 MODULE_NOT_FOUND。
- **门禁判据要按"身份"而不是"形状"**：map 条目的行形状恒为 `Keyed/Placeholder`（`RowKindFor`），用 `Kind == Union` 筛条目行永远不命中，只能靠 `??` 兜底 —— 兜底让那条断言对缺陷零约束。收紧后的判据是"每个 JSON 键都有唯一且 `JsonPath == 键路径` 的条目行"（双射）。
- **"行数 == 节点数"要求两边吃同一份 `Children(node)`**：计数一律走 presenter 的 `Children(node)`（union 走分支、列表走 Items），并且先 `EnsureChildren` 再数；失败信息要打出第一个"建出来却没有行"的节点的路径/类型/父级状态，否则只能说"红了 301 条"。
- **"恰好一行"的判据要先问"这条路径本该有几行"**：未声明键（`Any`/struct）自己会带一条"新键"占位子行，两者共用父级 `JsonPath` —— 按路径数行会恒等于 2。判据按"有键身份的条目行"（`FieldName` 非空）数，占位行不计。
- **结构类操作的判据要连"代价"一起量**：`SyncTextToTree` 是文本 → 树的唯一出口（防抖 tick 与门禁共用），门禁直接调它跑模型段，才量得到真实工作量（不然测到的是 120ms 防抖窗口）。判据是"整表重投影 = 0 次 + 模型段 ≤ 150ms"两条一起。
- **断言不许可空过**：`if (row is not null) Gate.Check(...)` 这种写法在行消失时整条断言蒸发（`S16-2b` 就这么空过过一轮），必须有"找不到行 = 红"。
- **现象要能复现成"一份内联文档"**：`--entity-predicate` 这类模式用**内联常量**把用户现场（`criteria.<名>.conditions.player`）钉进门禁，比截图/口述可靠；配套的索引 trace（`McdocSimplifier.IndexTrace`，默认关）能把"同一层级该取哪份 JSON"直接打出来。
- **性能预算要按"修好之后真正干的活"标定**：`S10 结构补丁 ≤50ms` 原本标在"锚点命中占位行、只重建 1 个节点"的错误行为上；修好锚点后同一条补丁变成重建整片容器（3.5k 节点的文档 67ms），旧阈值就成了"用 bug 的速度给新代码设限"。预算只拦量级退化，**结构正确性另配断言**（不许 fallback / 不牵动无关节点）。

## 四、一次真实缺陷的完整证据链

### D1　map entry 的键身份在定向重建时丢失 → 泛型实参代成 `any`

**现象（修前）**：`dimension_type.attributes` 提交 `audio/music_volume` 后，把该条目的 union 切到结构支，`argument` 行变成 Raw 文本框、类型 `any`，只挂一条"缺少必填键"诊断（门禁 `S16-2b` 红）。

**证据链（修前逐条可复现）**：
- 提交后树里根本没有条目行：现场 4 行是 `$.attributes`（struct）、`$.attributes`（`field=` 空、`Type=Union`、`DeclaredType=Mapped`）、`$.attributes.modifier`、`$.attributes.argument` —— 也就是"文件里有键、树里没有条目行"。
- 求值期：`[[%key]]` 先落 `%none`、再由 dispatcher 的 `[%unknown]` 成员（`DiscreteAttribute<any>`）兜底，说明键上下文是空的；随后 `OverrideModifier<T>` 的实参 `Reference T` 在 `{K:String, T:Any}` 下求值 → `any`。
- 反证只在"不经 UI 提交/切换"的路径上成立：`--attributes-deep` 的 A7 是 `CommitEntry` 之后**显式 `RefreshChildren()` 并按 `FieldName` 找条目**，既不走宿主的按路径重找、也不切分支，所以它绿不代表这条链绿。

**根因**：`EnvironmentAttributeMap<K>` 的值类型是 `environment_attribute[[%key]]`，成员选择依赖**这条 entry 自己的键**。而宿主在"运行期依赖变化 → 定向重建"时是这样定位锚点的：动态键占位行的 `JsonPath` 与父容器相同、路径索引又是子覆盖父 → `FindNode(锚点路径)` 命中占位行 → `RebuildNode` 用占位行重建（它没有求值现场，`ValueNode == null`）→ 造出一个没有 `FieldName`/键节点的匿名 union 行，真正的条目行从未产生，`%key` 自然为空。

**教训**：
- 判据要按"身份"（`FieldName` / `JsonPath` / `KeyNode`）而不是"行形状"：条目行的形状恒为 `Keyed/Placeholder`，用 `Kind == Union` 筛永远不命中，`??` 兜底会让断言对缺陷零约束。
- 定向重建按路径找锚点会命中"同路径的兄弟节点"（动态键占位行与父容器同 `JsonPath`）：拿它重建就造出没有键身份的空壳 —— 这类失败在行上只表现为"缺少必填键"，**只能靠门禁抓**。
- 给它特判放行等于把缺陷写进契约：判据必须是"键身份不变 → `%key` 能解析"。

## 五、与 misode 的有意偏差

| 偏差 | 原因 |
|---|---|
| 跨资源存在性只到 warn（misode checker 部分具备） | 不做 checker 级 error/自动修复：资源名/标签只做"在不在注册表"的存在性提示，语义校验（能否被引用、引用的目标合不合法）不猜 |
| 不做 follow 跳转（misode 的 follow_reference） | 用户明确不需要跨页联动：行上的"跟随"按钮与管线里的回调（`NodeFollowRequested` / `MdocGeneratorLauncher` / 注册表→入口映射）已整条删除 |
| 默认展开深度 3（misode 全展开） | WPF 虚拟化下全展开=全物化，首屏直接崩 |
| 缺省字段只留一行 ⊕，不摊开子级 | 与 misode 一致；"摊开"会让删掉的结构字段把子结构留在树上（点删除像没反应） |
| 无 checker 级 Errors | C# 侧没有 spyglass checker，用结构 + 取值域校验替代；跨资源/tag 存在性不做 |
| 物品图标退化成确定性瓷砖 | 仓内 png 全是 UI 图、没有 item/block 贴图（misode 拉远端）；宁可画瓷砖也不静默空白 |
| `DeferEditors` 默认关 | 摘要行更矮 → 视口物化更多容器，首屏更慢 |
| 角标用文本字形 | `Icon.xaml` 图标依赖 png 资源 |
| 候选外的值：不伪造候选、不显示成选中项，只挂 warn 诊断「值不在候选内：X」，JSON 与选中项都保持不动 | 判断只看"该位置有没有值"；伪造候选会把用户选择弹回去（换 trigger 后 spread 不跟就是这么踩的） |
| 白名单外 `#[id]` 也只给一件控件（单个下拉），手输自定义值走右侧 JSON 面板 | 用户明确要求"有下拉就别再放文本框"；可编辑下拉在这个控件里没有 |
| 渲染完毕后把行上显示的默认值（数字 / bool / union 标量支）落盘；编辑器清空即删键 | 用户明确要求"所见即所得"：右侧 JSON 必须和左侧树一致；代价是打开页面后文档会带上这些默认键 |
| map 条目行的键以只读标签显示，改名走「重命名」入口 | 键名是用户自己起的，不该在条目行上再出现候选下拉；下拉只属于"新键"占位行 |
| 解不出来的 union 成员切过去会**回 unset**（附诊断） | 与 misode 一致：渲染不了的分支不该被"选中"，否则行上会留一个写不出合法值的编辑器 |
| i18n 中文硬编码 | CBHK 只面向中文用户 |
| 对拍白名单 14 条 | 数据里同名类型重复 + spyglass 符号碰撞；我们按"模块+文件名"解析更精确，有意不模仿 |

## 六、上游参考与已知对拍差异

- misode 渲染器：`D:\C#Project\MisodeGithub\src\app\components\generator\McdocRenderer.tsx`（组件递归、`getChange`/`getDefault`/`isSelectRegistry`）。
- spyglass：`D:\C#Project\SpyglassMC\packages\mcdoc`（`simplify` 是冻结层对照物；`runtime/attribute/builtin.ts` 是 `#[id]` 等属性的语义源）。
- 对拍剩下的差异（两类，均为已知且不改）：
  - 渲染字段序：misode 先静态字段再动态占位，spyglass 按声明序。
  - `enumValues`+`kind`：数据里 `BlockInteraction` 在 `enchantment.mcdoc` 与 `enchantment/effect.mcdoc` 各有一份，spyglass 登记到同一符号路径（后者覆盖前者），我们按"模块+文件名"区分。受控实验证明两侧版本过滤都对，差异只来自同名类型解析到哪一份。

## 七、未收口的坑（下一轮从这里接）

- **只写 definition、不写 trigger 的文档本身是合法的**：`{"criteria":{"test":{}}}` 载入后 `criteria` 是 Struct、条目类型是 criterion 结构（不是 `Any`）。所以再看到"空壳条目"，先怀疑文本被污染（上一条），不要再往求值层找。
- **动态键行的添加入口只有一条**：数据结构（map/struct）的 ⊕ 只能是「起键 → 用该键作 `%key` 求值」（`CommitEntry`）；列表语义的追加（`AddItem/AppendItem`）只对 `List` / union 的列表分支成立。给所有节点无差别挂 `AddItemCommand`（没有 `CanExecute` 守卫）会让 map 行出现一个永远可点、点了什么也写不出来的按钮。
- **"新键"占位行不该有值编辑器**：它只负责起键（键输入 + 候选下拉 + ⊕）。一旦它渲染出值编辑器，用户看到的就是"键框旁边蹲着一段裸 JSON"，看起来像模板崩坏。

## 八、下一步要做的事：文本 → 树的最小范围同步

**目标**：右侧改一处，左侧只重建那一处；不论文档多大，**更新代价只跟"改动范围"有关，与文档规模无关**。判据三条（缺一不可）：① 文本仍是单个根；② 整表重投影 = 0 次；③ 600 行文档里"局部结构变化"≤ 150ms、"值变化"≤ 20ms。

**现状**：

- 差分器能定位到**最深的变化容器**并只重建那一个子树，投影也只重拼那几行。
- **变化落在根对象**（加键 / 删键 / 删掉一层包装）也已经是局部同步：按"根的直接子键"逐个差分，顶层行按"第几个顶层行"分段重拼，不走整树重建。判据是 ui-gate `S43 根层` 五步（整表重投影 = 0、单次结构变化 ≤ 150ms）。
- 解析仍是整篇递归下降：没有"只重解析改动区间"的能力，所以做不到"识别最小合法结构的起止位置"。
- 为了不留下残骸行，现在对"未被锚点覆盖的作废子树"一律整篇回退 —— 这是安全优先，但也把能局部的情形一起牺牲了。

**还剩两步，风险递增**：

1. **拆掉"一有作废就整篇回退"**：改成"覆盖不到的子树各自重建"，并按契约清单（§2.7）重新守一遍残骸行与双射。风险在"行 ↔ 文档"的对应关系，必须有对应操作盯着。
2. **增量解析**：保留上次解析的区间表，用编辑偏移定位受影响的顶层成员，只重解析那段文本、其余 AST 复用。这才是字面意义上的"最小合法结构区间"；贴一大段只改一处也能接近常数，但需要解析器支持片段解析 + 偏移修正。

**每步都必须先补一条会红的判据**（加在"操作 × 契约"的矩阵里，而不是新开一次性用例）：覆盖不到的子树局部重建时，行数/双射/无残骸行三条同时成立；增量解析那步再加"文本与 AST 同代"。
