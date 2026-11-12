// 视图节点 + 构建器 + JSON 解析器：把"文本 → 求值 → 可绑定树"这条路径压在一个文件里。
// 设计目标：让 CBHK 既有的 13 个 TreeView DataTemplate 零改动复用，
// 因此属性名严格对齐 TreeViewCommon.xaml 的绑定（Children/Items/SelectedUnionChildren/
// FieldName/IsVisible/IsRequired/Value/IsTrue/IsFalse/EnumOptionList/SelectedEnumOption/
// UnionTypeNameList/SelectedUnionTypeName）。
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MinecraftLanguageModelLibrary.Data;

namespace CBHK.Utility.Data.Mcdoc
{
    public sealed class McdocTreeNode : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string FieldName { get; set; } = "";

        /// <summary>该 struct 字段是动态键（如 [criterion]: Criterion），树上是"键输入"行而非值行。</summary>
        public bool IsDynamicKey { get; set; }

        /// <summary>联合类型当前选中的成员（按 JSON 节点挑默认分支；切换分支时由 UI 改成别的成员）。</summary>
        public McdocType SelectedUnionMember { get; set; }

        /// <summary>
        /// 由 <see cref="McdocTreeBuilder"/> 注入：用"给定成员类型 + 当前 JSON 值"重建一个分支子节点。
        /// 切换分支时靠它重算子树（成员是自包含类型，所以平铺后的列表也能正确切换）。
        /// </summary>
        public Func<McdocType, McdocJsonValue, McdocType, McdocTreeNode> UnionBranchBuilder { get; set; }

        /// <summary>值/分支被用户改动时回调宿主（宿主据此回写文本并重算）。</summary>
        public Action<McdocTreeNode> ValueChanged { get; set; }

        /// <summary>该节点在 JSON 里的路径（如 <c>$.display.title</c>、<c>$.rewards[0]</c>），回写时用来定位。</summary>
        public string JsonPath { get; set; } = "$";

        /// <summary>模板里当 ToolTip 用（旧 DTO 模型的 Key）：这里放字段的 mcdoc 文档注释。</summary>
        public string Key { get; set; } = "";

        /// <summary>字段的 mcdoc 文档注释（`/// ...`），UI 上作为说明显示。</summary>
        public string Desc { get; set; } = "";

        /// <summary>
        /// 该节点虽然类型是 string，但带 #[id]/#[tree{registry}] 且注册表在 misode 的 selectRegistries 白名单里：
        /// UI 上是**闭合下拉**（只能选候选，等价 misode 的 &lt;select&gt;）。
        /// 类型本身保持 string，避免影响求值与对拍。
        /// </summary>
        public bool UseSelect { get; set; }

        /// <summary>
        /// 允许输入候选之外的值（等价 misode 白名单外的 &lt;input list=datalist&gt;：自由输入 + 候选提示）。
        /// 与 UseSelect 互斥：UseSelect=true 是闭合下拉，AllowCustomValue=true 是可编辑下拉。
        /// </summary>
        public bool AllowCustomValue { get; set; }

        /// <summary>union 行从标量分支镜像过来的"物品图标位"（misode 的 ItemIdPreview 在并排的 StringHead 里）。</summary>
        public bool MirrorIconSelect { get; set; }

        /// <summary>右键"复制"：深拷贝本节点的 JSON 值并插到同层（仅列表项有意义）。</summary>
        public System.Windows.Input.ICommand DuplicateCommand { get; set; }

        /// <summary>缺省结构的 ⊕ 设默认值（misode StructHead）。</summary>
        public System.Windows.Input.ICommand SetDefaultCommand { get; set; }

        /// <summary>数值行的 -/+（模板的数据上下文是节点，不是行）。</summary>
        // 命令按节点缓存：每次读取都新造一个对象的话，WPF 的可用性重问会一直换实例（按钮状态跟着抖）
        private System.Windows.Input.ICommand decreaseCommand;
        public System.Windows.Input.ICommand DecreaseCommand
            => decreaseCommand ??= new McdocCommand(() => McdocCollectionEditor.Default.StepNumber(this, -1m));

        private System.Windows.Input.ICommand increaseCommand;
        public System.Windows.Input.ICommand IncreaseCommand
            => increaseCommand ??= new McdocCommand(() => McdocCollectionEditor.Default.StepNumber(this, 1m));

        /// <summary>列表头部的 ⊕（插到 0）。</summary>
        public System.Windows.Input.ICommand AddItemTopCommand { get; set; }

        /// <summary>列表条目的自身操作（misode ListItem 的 remove / move up / move down）。</summary>
        public System.Windows.Input.ICommand RemoveSelfCommand { get; set; }
        public System.Windows.Input.ICommand MoveUpCommand { get; set; }
        public System.Windows.Input.ICommand MoveDownCommand { get; set; }

        /// <summary>
        /// 列表项的可用性变了（Items 增删/移动/重建之后）：WPF 只在 CanExecuteChanged 时重问 CanExecute，
        /// 不通知就会出现"新加进来的项上移按钮一直灰着 / 点了没反应"。
        /// </summary>
        public void RaiseListItemCommandStateChanged()
        {
            (RemoveSelfCommand as McdocCommand)?.RaiseCanExecuteChanged();
            (MoveUpCommand as McdocCommand)?.RaiseCanExecuteChanged();
            (MoveDownCommand as McdocCommand)?.RaiseCanExecuteChanged();
        }

        /// <summary>可选字段的"移除"（misode 的 unset）：把自己从父对象里删掉。</summary>
        public System.Windows.Input.ICommand RemoveFieldCommand { get; set; }

        private string editingKey = "";
        /// <summary>
        /// 这是 map 的一个已有条目（键是动态的）。与 IsDynamicKey 区分：后者只用于「新增占位行」。
        /// 差分 dump 会跳过这些行（oracle 对动态键只给一个 dynamic 占位）。
        /// </summary>
        public bool IsMapEntry { get; set; }

        /// <summary>
        /// 动态键行里正在编辑的键名（两向绑定到模板的输入框）。
        /// 变化时要通知 UI（属性通知）并让 ⊕ 重新求值 —— 否则输入了键，按钮还是灰的。
        /// </summary>
        public string EditingKey
        {
            get => editingKey;
            set
            {
                if (editingKey == value) return;
                editingKey = value ?? "";
                OnPropertyChanged();
                (AddEntryCommand as McdocCommand)?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// union 的当前分支是"标量"时，编辑器直接并到 union 这一行上（Text / Number / Bool / Enum），
        /// 不再多出一行空壳。null 表示分支是结构/列表（内容照常作为子行展开）。
        /// </summary>
        public string ScalarEditorKind { get; set; }

        /// <summary>union 选中的是列表/元组分支：子行直接摊在 union 行下（不再多一层容器行）。</summary>
        public bool UnionListLike { get; set; }

        /// <summary>键类型带 #[id] 且能取到候选值时，键用下拉而不是文本框。</summary>
        public bool UseKeySelect { get; set; }

        /// <summary>供模板绑定的反向属性（文本框显隐用，省掉一个取反转换器）。</summary>
        public bool UseKeyText => !UseKeySelect;

        /// <summary>
        /// 动态字段在"静态子行序列"里的位置（有几个动态字段就有几项）。
        /// 差分 dump 用它把动态键还原成 oracle 的 {name:null,dynamic:true} 占位。
        /// </summary>
        public List<int> DynamicFieldPositions { get; } = [];

        /// <summary>该行所属的动态字段下标（一个结构可能有多个 `[k]: V` 字段）；非动态行为 -1。</summary>
        public int DynamicFieldIndex { get; set; } = -1;

        /// <summary>键的候选项（来自注册表，如 data_component_type 的 111 个组件名）。</summary>
        public ObservableCollection<EnumMember> KeyOptionList { get; set; }

        private EnumMember selectedKey;
        public EnumMember SelectedKey
        {
            get => selectedKey;
            set
            {
                if (Equals(selectedKey, value)) return;
                selectedKey = value;
                if (value?.Name is { Length: > 0 } name)
                {
                    EditingKey = name;   // 选了下拉项就等于把键填好了
                }
                OnPropertyChanged();
            }
        }

        /// <summary>动态键行的"确认"：占位行是新增键，已有行是改名。</summary>
        public System.Windows.Input.ICommand AddEntryCommand { get; set; }

        /// <summary>动态键行的"删除该键"。</summary>
        public System.Windows.Input.ICommand RemoveEntryCommand { get; set; }

        /// <summary>
        /// 求值时读过运行期数据（动态索引 spread、%parent/%key 等）：同级字段的值一变，
        /// 这里的子级就可能是另一套结构，宿主必须重新求值（否则 conditions 这类字段永远不出来）。
        /// </summary>
        public bool DependsOnRuntimeData { get; set; }

        /// <summary>父节点（结构编辑时需要知道往谁身上动）。</summary>
        public McdocTreeNode Parent { get; set; }

        /// <summary>建树时的求值上下文节点：重建这棵子树要复用它，%parent/%key 的链条才保持一致。</summary>
        internal McdocValueNode ValueNode;

        /// <summary>树内深度（重建时按同样深度构建，深度上限/预算行为不变）。</summary>
        internal int Depth;

        /// <summary>容器节点：结构/联合/元组/列表/数组/映射条目。运行期依赖的"重建锚点"就是最近的容器祖先。</summary>
        internal static bool IsContainer(McdocTreeNode node)
            => node is not null
               && (node.IsMapEntry || node.IsDynamicKey
                   || node.Type?.Kind is McdocTypeKind.Struct or McdocTypeKind.Union or McdocTypeKind.Tuple
                       or McdocTypeKind.List or McdocTypeKind.ByteArray or McdocTypeKind.IntArray
                       or McdocTypeKind.LongArray);

        /// <summary>最近的容器祖先（含自身）；运行期依赖变化时只从这一层重建。</summary>
        internal McdocTreeNode RebuildAnchor
        {
            get
            {
                for (var current = this; current is not null; current = current.Parent)
                {
                    if (IsContainer(current)) return current;
                }
                return this;
            }
        }

        /// <summary>
        /// 到最近的容器祖先为止是否有"依赖运行期数据"的标记。只看这一段：
        /// 一路查到根会把整棵树都算成依赖（根结构里通常都有动态索引），重建就又变成全量了。
        /// </summary>
        internal bool IsRuntimeDependent
        {
            get
            {
                for (var current = this; current is not null; current = current.Parent)
                {
                    if (current.DependsOnRuntimeData) return true;
                    if (IsContainer(current)) return false;
                }
                return false;
            }
        }

        /// <summary>该标量带 #[random]：默认值是随机种子/随机数，行上要能"重掷"（misode 的 generate_new_seed）。</summary>
        public bool HasRandom { get; set; }
        public System.Windows.Visibility RandomButtonVisibility
            => HasRandom ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        public System.Windows.Input.ICommand RegenerateSeedCommand { get; set; }

        /// <summary>根节点的值被编辑时替换整份文档 JSON（根标量没有父容器可写）。</summary>
        public Action<McdocJsonValue> ReplaceRoot { get; set; }

        /// <summary>列表的"+"：追加一个默认项。</summary>
        public System.Windows.Input.ICommand AddItemCommand { get; set; }

        /// <summary>列表的"−"：删掉最后一项（逐项删除走右键/后续菜单）。</summary>
        public System.Windows.Input.ICommand RemoveItemCommand { get; set; }

        /// <summary>结构被改动（增删项）时回调宿主：需要整体重排文本并重建树。</summary>
        public Action<McdocTreeNode> StructureChanged { get; set; }

        /// <summary>构建期直接赋值：不触发回写回调（初始值不该被当成"用户改动"）。</summary>
        internal void SetValueSilently(object newValue)
        {
            if (Equals(value, newValue)) return;
            value = newValue;
            OnPropertyChanged(nameof(Value));
        }

        private bool suppressUnionRebuild;

        /// <summary>
        /// 置位时所有"值变了"的通知都不发出。union 切分支要把标量分支的状态镜像到行走上，
        /// 那是显示同步、不是用户编辑——不静默就会把 value 的布尔写进文档，把对象覆盖成 true/false。
        /// </summary>
        internal bool SuppressValueNotifications;

        /// <summary>把"值真的变了"通知出去（同一个闸门：建树期/分支镜像期一律不发）。</summary>
        internal void RaiseValueChanged()
        {
            if (!SuppressValueNotifications) ValueChanged?.Invoke(this);
        }
        /// <summary>求值后的类型（模板分派依据）。</summary>
        public McdocType Type { get; set; } = McdocType.Any();

        /// <summary>misode 的 AnyHead：任意 JSON 的原始文本编辑（解析失败就保留旧值）。</summary>
        public string RawJson
        {
            get => Json is null ? "" : McdocJsonFormatter.Format(Json);
            set
            {
                try
                {
                    Json = McdocJsonParser.Parse(string.IsNullOrWhiteSpace(value) ? "null" : value);
                }
                catch (Exception)
                {
                    return;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(Value));
                ValueChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// 文本改了标量值：把节点状态从 Json 重新派生（不换节点、不触发 ValueChanged）。
        /// 只用于"容器形状没变、只有叶子值变了"的快路径。
        /// </summary>
        internal void SyncScalarFromJson()
        {
            var json = Json;
            value = json?.Kind switch
            {
                McdocJsonKind.String => json.StringValue,
                McdocJsonKind.Number => json.NumberValue,
                McdocJsonKind.Boolean => json.BooleanValue,
                _ => null,
            };
            isTrue = json?.Kind == McdocJsonKind.Boolean && json.BooleanValue;
            isFalse = json?.Kind == McdocJsonKind.Boolean && !json.BooleanValue;
            if (EnumOptionList is { Count: > 0 } options)
            {
                if (json is { Kind: McdocJsonKind.String } text)
                {
                    // 只跟"候选里真有这个值"的情况；候选外的值不猜、不补候选，由校验器挂警告
                    var match = options.FirstOrDefault(option =>
                        Convert.ToString(option.Value?.LiteralValue) == text.StringValue);
                    if (match is not null)
                    {
                        selectedEnumOption = match;
                    }
                }
                else if (json is null && !IsRequired && McdocUnionSwitch.IsUnsetOption(options[0]))
                {
                    // 键被删掉了：行上回到（未设置），不许留着上一份选中
                    selectedEnumOption = options[0];
                }
            }
            OnPropertyChanged(null);
        }

        /// <summary>JSON 里有、schema 里没声明的键（misode 的 UnknownField）。</summary>
        public bool IsUnknownField { get; set; }

        /// <summary>结构诊断（McdocValidator 写入）。</summary>
        public List<string> Diagnostics = [];

        /// <summary>求值失败的原因：不抛给上层，挂在节点上由行显示（misode 的 ErrorIndicator）。</summary>
        public string Error { get; set; }

        /// <summary>探针/诊断用的唯一键（路径 + 类型）。</summary>
        public string NodeKey() => (JsonPath ?? "$") + "|" + (Type?.Kind.ToString() ?? "?");

        /// <summary>按名字选 union 分支（探针深挖用；UI 走 McdocRow.SelectedUnionOption）。</summary>
        public void SelectedUnionOptionName(EnumMember option)
        {
            SelectedUnionTypeName = option;
        }

        /// <summary>列表已展示的条目数（misode ListBody 的 maxShown）。</summary>
        public int ItemsShown { get; set; } = 50;

        /// <summary>
        /// 这个节点是不是"文档根"（dispatcher 直接把整份文档解成一个标量时才有）。
        /// 只有它能替换根：parent 为空的其它节点都是脱挂的陈旧节点，写进去就成了"删 display 却把内容提到根上"。
        /// </summary>
        public bool IsDocumentRoot { get; set; }

        /// <summary>局部队列重建：由工厂用同一份构建上下文挂上（结构变更时避免整树重建）。</summary>
        public System.Action RefreshChildren { get; set; }

        /// <summary>子级是否已构建（懒建）。</summary>
        public bool ChildrenBuilt { get; set; }

        /// <summary>
        /// union 的非结构分支（列表/元组）会以子行形态挂在自己下面，标签会和父行同名；
        /// 这里记下分支形态，行标签据此加后缀，避免看起来像"多了一层空壳"。
        /// </summary>
        public string UnionBranchKind { get; set; }

        /// <summary>
        /// 节点是否仍挂在文档上：沿 parent 链逐级校验"这一层还有 JSON、且中线以上父级确实含有我这个键"。
        /// 直接父级放宽（正要写入的键可能还不存在）。删除/重建之后还伸手写回的陈旧节点一律被这条挡住。
        /// </summary>
        public bool IsAttachedToRoot(out string brokenAt)
        {
            brokenAt = null;
            var child = this;
            var depth = 0;
            for (var parent = Parent; parent is not null; child = parent, parent = parent.Parent, depth++)
            {
                if (parent.Json is null)
                {
                    brokenAt = parent.JsonPath ?? "<root>";
                    return false;
                }
                if (depth == 0) continue;
                if (parent.Json.Kind == McdocJsonKind.Object)
                {
                    if ((parent.Json.Properties ?? []).All(pair => pair.Key != child.FieldName))
                    {
                        brokenAt = parent.JsonPath;
                        return false;
                    }
                }
                else if (parent.Json.Kind == McdocJsonKind.Array)
                {
                    if (!int.TryParse(child.FieldName, out var index) || index >= (parent.Json.Items?.Count ?? 0))
                    {
                        brokenAt = parent.JsonPath;
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>首次需要子级时构建一次（投影展开时调用）。</summary>
        public void EnsureChildren()
        {
            if (ChildrenBuilt) return;
            ChildrenBuilt = true;
            RefreshChildren?.Invoke();
        }

        public System.Windows.Input.ICommand ShowMoreCommand { get; set; }
        public System.Windows.Input.ICommand ShowAllCommand { get; set; }

        /// <summary>行描述（misode 的 Head）：由 presenter 族产出，视图层不再判断类型。</summary>
        public McdocNodeHead Head => McdocPresenterRegistry.Default.Describe(this);
        /// <summary>求值前的声明类型，供"切换分支时保留旧值"使用。</summary>
        public McdocType DeclaredType { get; set; }
        /// <summary>对应的 JSON 值节点（编辑回环的落点）。</summary>
        public McdocJsonValue Json
        {
            get => json;
            set
            {
                json = value;
                // 键被删掉（值变 null）时必须折叠：否则"文档里没有它"却还展开着，
                // 投影会把老早就建好的子级重新摊到行下面（现象：右侧删了整段 display，左侧子级还在）
                if (value is null && !IsRequired && isExpanded)
                {
                    isExpanded = false;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }

        private McdocJsonValue json;

        public bool IsVisible { get; set; } = true;
        public bool IsRequired { get; set; } = true;

        /// <summary>折叠状态。由 ItemContainerStyle 双向绑定，替代旧管线"在 Loaded 事件里手动设"的做法。</summary>
        private bool isExpanded;
        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (isExpanded == value) return;
                isExpanded = value;
                OnPropertyChanged();
            }
        }

        /// <summary>选中状态。同样由 ItemContainerStyle 双向绑定。</summary>
        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected == value) return;
                isSelected = value;
                OnPropertyChanged();
            }
        }

        /// <summary>列表项/必选项的占位提示（对应旧模型 Watermark）。</summary>
        public string Watermark { get; set; } = "";

        public ObservableCollection<McdocTreeNode> Children { get; set; } = [];
        public ObservableCollection<McdocTreeNode> Items { get; set; } = [];
        public ObservableCollection<McdocTreeNode> SelectedUnionChildren { get; set; } = [];
        public ObservableCollection<EnumMember> EnumOptionList { get; set; }
        public ObservableCollection<EnumMember> UnionTypeNameList { get; set; }

        private object value;
        public object Value
        {
            get => value;
            set
            {
                if (Equals(this.value, value)) return;
                this.value = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NumberText));
                if (!SuppressValueNotifications) ValueChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// 数字行绑定的文本（模板里的 TextBox 直接绑它）：空串/不可解析 = 没有值 ⇒ 可选字段删键。
        /// 行模板不再用带内置步进的数值控件（它只改自己的 Text，不通知绑定，JSON 永远不动）。
        /// </summary>
        public string NumberText
        {
            get => value is null ? "" : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
            set
            {
                var text = (value ?? "").Trim();
                if (text.Length == 0)
                {
                    if (value is not null) Value = null;
                    return;
                }
                if (!decimal.TryParse(text, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                {
                    // 敲到一半的非法输入：不动文档，也不抹掉用户正在敲的内容
                    OnPropertyChanged(nameof(NumberText));
                    return;
                }
                Value = parsed == Math.Truncate(parsed) ? (object)(long)parsed : (double)parsed;
            }
        }

        // 缺省的 bool 行"两个都不选"：以前默认 isTrue=true，会在界面上伪装成 True 已选中
        private bool isTrue = false;
        public bool IsTrue
        {
            get => isTrue;
            set
            {
                if (isTrue == value) return;
                isTrue = value;
                OnPropertyChanged();
                // 与旧模型一致的互斥语义：必选节点二选一，可选节点允许都不选
                if (IsRequired && !value) SetFalse(true);
                else if (value) SetFalse(false);
                if (!SuppressValueNotifications) ValueChanged?.Invoke(this);
            }
        }

        private bool isFalse;
        public bool IsFalse
        {
            get => isFalse;
            set
            {
                if (isFalse == value) return;
                isFalse = value;
                OnPropertyChanged();
                if (IsRequired && !value) SetTrue(true);
                else if (value) SetTrue(false);
                if (!SuppressValueNotifications) ValueChanged?.Invoke(this);
            }
        }

        private EnumMember selectedEnumOption;

        public EnumMember SelectedEnumOption
        {
            get => selectedEnumOption;
            set
            {
                if (Equals(selectedEnumOption, value)) return;
                selectedEnumOption = value;
                OnPropertyChanged();
                // union 行上镜像的枚举分支选「（未设置）」= 这个键整个不存在：走 union 的 unset 出口
                // （删键 + 清分支镜像 + 回未设置），否则这个选项点了没反应、行上却显示（未设置）
                if (Type?.Kind == McdocTypeKind.Union && !SuppressValueNotifications
                    && McdocUnionSwitch.IsUnsetOption(value)
                    && McdocUnionSwitch.SwitchTo(this, value))
                {
                    return;
                }
                ValueChanged?.Invoke(this);
            }
        }

        /// <summary>misode AnyHead：先选 JSON 类型（boolean/double/string/list/struct），再给该类型的编辑器。</summary>
        public ObservableCollection<EnumMember> AnyTypeOptions { get; set; }

        private EnumMember selectedAnyType;
        public EnumMember SelectedAnyType
        {
            get => selectedAnyType;
            set
            {
                if (Equals(selectedAnyType, value)) return;
                selectedAnyType = value;
                OnPropertyChanged();
                var type = McdocHelpers.AnyTypeOf(value?.Name);
                if (type is not null)
                {
                    McdocCollectionEditor.Default.ApplyValue(this, McdocHelpers.GetDefault(type));
                }
            }
        }

        /// <summary>建树期直接落枚举选中项（不触发 ValueChanged）。</summary>
        public void SetEnumOptionSilently(EnumMember option)
        {
            selectedEnumOption = option;
            OnPropertyChanged(nameof(SelectedEnumOption));
        }

        /// <summary>建树期直接落选中项（不触发回写）。</summary>
        public void SetAnyTypeSilently(EnumMember option)
        {
            selectedAnyType = option;
            OnPropertyChanged(nameof(SelectedAnyType));
        }

        /// <summary>
        /// 建树期直接落 union 选中项（不触发分支重建）。分支子树必须用"文档里那份 JSON"建，
        /// 走 setter 会经过 GetChange（把值换成默认值），靠同级键的动态索引（[[type]] 之类）就全失效。
        /// </summary>
        internal void SetUnionTypeSilently(EnumMember option)
        {
            selectedUnionTypeName = option;
            OnPropertyChanged(nameof(SelectedUnionTypeName));
        }

        /// <summary>
        /// 这次分支选中是**用户在行上选的**（而不是按 JSON 值猜出来的默认支）。
        /// 重建时"按值猜"会在并列时退回第一个成员，把用户的选择顶掉 —— 有了这个标志，显式选择优先。
        /// </summary>
        public bool UnionSelectionExplicit { get; set; }

        private EnumMember selectedUnionTypeName;
        public EnumMember SelectedUnionTypeName
        {
            get => selectedUnionTypeName;
            set
            {
                if (Equals(selectedUnionTypeName, value)) return;
                var previous = selectedUnionTypeName;
                // 切走之前的成员类型：分支子树的 getChange 要拿它当"旧类型"，否则 X↔[X] / {k:X}↔X
                // 的搬运规则永远不成立（旧类型是 union 声明，跟哪个成员都不"同型"）。
                var previousMember = SelectedUnionMember;
                selectedUnionTypeName = value;
                // 切成员只有一条规则：旧成员声明、新成员不声明的键要剪掉。
                // 以前只有 SwitchTo 里做，走下拉/重建这条 setter 路径时旧键会留在文档里（现场：terms 残影）。
                var changed = McdocUnionSwitch.ResetSelectedBranch(this, previous, value);
                // 先重建再通知：行上的 Head 是按当前分支算的，通知里重算时必须已经能看到新分支
                if (!suppressUnionRebuild) RebuildUnionBranch(previousMember);
                OnPropertyChanged();
                // 分支改写真的动过文档（剪键/补默认结构）→ 必须走值通知：文本同步与"这一支的重建"
                // 都挂在这条通道上。以前这里一声不响，下拉切到 Modifier 支后右侧 JSON 停在 {}、
                // 行也停在上一支（截图现场）。SwitchTo 会压住这条、在自己的收尾里只发一次。
                if (changed) RaiseValueChanged();
            }
        }

        /// <summary>
        /// 把分支节点摆进 SelectedUnionChildren：结构分支摊平成字段行（与旧管线一致），
        /// 其它（标量/列表/枚举）作为一行无标签的壳。初始化与"切换分支"共用这一处，避免两套行为。
        /// </summary>
        /// <summary>切换分支的行状态由 UnionNodePresenter 负责（节点不再按类型分派）。</summary>
        internal void ApplyUnionBranch(McdocTreeNode branchNode)
            => McdocPresenterRegistry.Default.ApplyBranch(this, branchNode);

        /// <summary>撤销分支（成员解不出来时回到 unset）：分支子级与镜像一起清掉。</summary>
        internal void ClearUnionBranch()
            => McdocPresenterRegistry.Default.ClearBranch(this);

        /// <summary>
        /// 用户切了分支：把文档值转成该分支的形状（标量 ↔ 结构）。
        /// 不做这一步，结构分支上的子字段没有一个合法的落点——编辑会被静静地丢掉
        /// （表现：切到 modifier 支看得见字段，改完保存的却还是 value 支的布尔）。
        /// 形状相同就不动，避免覆盖用户已有数据。
        /// </summary>
        internal void ConvertJsonToSelectedBranch(McdocType previousMember = null, bool replaceWhenUnfit = false)
        {
            var members = Type?.Members;
            var index = SelectedUnionMemberIndex();
            if (members is null || index < 0 || index >= members.Count) return;
            // 旧类型 = 切走的那个成员（不是 union 声明）：getChange 的四条搬运规则全靠它判"同型"
            var converted = McdocHelpers.GetChange(members[index], previousMember ?? DeclaredType ?? Type, Json);
            if (converted is null) return;
            if (Json is not null)
            {
                // 形状相同：默认一个字都不动（不替用户改数据）。但**值落不进这一支**时必须转换 ——
                // 否则文档与分支各说一套（[int] 切到 [string] 支，行上一条条"值与类型不符"）。
                if (converted.Kind == Json.Kind && !replaceWhenUnfit) return;
                if (replaceWhenUnfit && McdocUnionSwitch.ValueFitsMember(members[index], Json, null)) return;
            }
            // 顺序与值回写一致：EnsurePath → 改 node.Json → WriteIntoParent（颠倒会把父级 null 写回去）
            McdocCollectionEditor.Default.EnsurePath(this);
            Json = converted;
            McdocCollectionEditor.Default.WriteIntoParent(this, converted);
            // 与 misode 一致：onSelect 里就是 makeEdit(getChange(newBranch, oldBranch, node))——
            // 切分支当场把文档整成新分支的形状。文本同步由宿主做（不再触发重建）。
            if (!SuppressValueNotifications) ValueChanged?.Invoke(this);
        }

        /// <summary>
        /// 切支把值写进文档之后，重新派生分支节点与行上的镜像。
        /// 建支那一刻文档里还没有值（分支是用 null JSON 建的），镜像会停在（未设置）；值一落盘必须跟着刷新，
        /// 否则"选了 Enum 支、行上的枚举下拉却显示（未设置）"。
        /// </summary>
        internal void RefreshUnionBranchMirror()
        {
            if (Type?.Kind != McdocTypeKind.Union) return;
            var wasSuppressed = SuppressValueNotifications;
            SuppressValueNotifications = true;   // 这不是用户编辑
            try
            {
                // 结构分支：分支节点有自己的 JSON，派生完再镜像过来
                if (SelectedUnionChildren.FirstOrDefault() is { } branch)
                {
                    if (branch.Json is null && Json is not null) branch.Json = Json;
                    branch.SyncScalarFromJson();
                    SelectedEnumOption = branch.SelectedEnumOption;
                    return;
                }
                // 标量分支是镜像到本行的（没有分支子级）：直接按本行的值挑选中项，缺值就回（未设置）
                if (EnumOptionList is not { Count: > 0 } options) return;
                var text = Json is { Kind: McdocJsonKind.String } value ? value.StringValue : null;
                var match = text is null
                    ? null
                    : options.FirstOrDefault(option => Convert.ToString(option.Value?.LiteralValue) == text);
                if (match is not null) SelectedEnumOption = match;
                else if (Json is null) SelectedEnumOption = options[0];
            }
            finally
            {
                SuppressValueNotifications = wasSuppressed;
            }
        }

        /// <summary>按当前选中的成员名重建分支子树；值不兼容时用 GetChange 尽量保留旧值。</summary>
        private void RebuildUnionBranch(McdocType previousMember)
        {
            var members = Type?.Members;
            if (members is null || members.Count == 0 || UnionBranchBuilder is null) return;

            var index = SelectedUnionMemberIndex();
            if (index < 0 || index >= members.Count) return;
            var member = members[index];

            suppressUnionRebuild = true;
            try
            {
                SelectedUnionMember = member;
                ApplyUnionBranch(UnionBranchBuilder(member, Json, previousMember));
            }
            finally
            {
                suppressUnionRebuild = false;
            }
        }

        /// <summary>成员下拉项对应 Type.Members 的下标（Value 里存的就是下标）。</summary>
        private int SelectedUnionMemberIndex()
        {
            var raw = selectedUnionTypeName?.Value?.LiteralValue;
            if (raw is int index) return index;
            if (raw is decimal number) return (int)number;
            if (raw is string text && int.TryParse(text, out var parsed)) return parsed;
            return -1;
        }

        /// <summary>建树期/物化期直接落 bool 选中态（不触发回写）。</summary>
        internal void SetBoolSilently(bool value)
        {
            isTrue = value;
            isFalse = !value;
            OnPropertyChanged(nameof(IsTrue));
            OnPropertyChanged(nameof(IsFalse));
        }

        private void SetTrue(bool newValue)
        {
            if (isTrue == newValue) return;
            isTrue = newValue;
            OnPropertyChanged(nameof(IsTrue));
        }

        private void SetFalse(bool newValue)
        {
            if (isFalse == newValue) return;
            isFalse = newValue;
            OnPropertyChanged(nameof(IsFalse));
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public override string ToString() => FieldName + " : " + (Type?.Kind.ToString() ?? "?");
    }
}
