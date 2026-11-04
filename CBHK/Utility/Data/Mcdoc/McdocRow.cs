using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using MinecraftLanguageModelLibrary.Data;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 行集合：WPF 的 ListCollectionView 拒绝多元素 Add/Remove（"不支持范围操作"），
    /// 所以拼接一律"改集合 + 单次 Reset"。虚拟化下 Reset 只重建可见的几十个容器。
    /// </summary>
    public sealed class McdocRowCollection : ObservableCollection<McdocRow>
    {
        public void SetAll(IReadOnlyList<McdocRow> rows)
        {
            CheckReentrancy();
            foreach (var row in Items) row.Detach();
            Items.Clear();
            foreach (var row in rows) Items.Add(row);
            RaiseReset();
        }

        public void InsertRange(int index, IReadOnlyList<McdocRow> rows)
        {
            if (rows.Count == 0) return;
            CheckReentrancy();
            for (var i = 0; i < rows.Count; i++) Items.Insert(index + i, rows[i]);
            RaiseReset();
        }

        public void RemoveRange(int index, int count)
        {
            if (count <= 0) return;
            CheckReentrancy();
            for (var i = 0; i < count; i++) Items.RemoveAt(index);
            RaiseReset();
        }

        private void RaiseReset()
        {
            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    /// <summary>
    /// 一行 = 一个可见节点。视图只认识行：展开/折叠/结构变更都只动行列表，
    /// 不重建容器（misode 的"整体重渲染"在 WPF 里做不动，这里换成行级拼接）。
    /// </summary>
    public sealed class McdocRow : INotifyPropertyChanged
    {
        private readonly McdocRowProjector projector;

        public McdocRow(McdocTreeNode node, int depth, McdocRowProjector project, int index, bool listChild,
            McdocRowKind? kindOverride = null)
        {
            Node = node;
            Depth = depth;
            projector = project;
            Index = index;
            IsListChild = listChild;

            this.kindOverride = kindOverride;
            node.PropertyChanged += OnNodePropertyChanged;
        }

        public McdocTreeNode Node { get; }
        private McdocNodeHead head;

        /// <summary>
        /// misode 的 Head 是"每次渲染重算"的纯函数；这里缓存，但必须能被刷新
        /// （union 切分支后编辑器种类/分支选项都变了，不刷新就表现为"切换没反应"）。
        /// </summary>
        /// <summary>
        /// union 行不走缓存：分支切换会改 ScalarEditorKind/分支列表，而 Head 是构造时求值的；
        /// 缓存住就会出现"切分支后编辑器不回来"。非 union 行仍按节点缓存（Describe 是纯函数）。
        /// </summary>
        public McdocNodeHead Head
            => Node.Type?.Kind == McdocTypeKind.Union
                ? McdocPresenterRegistry.Default.Describe(Node)
                : head ??= McdocPresenterRegistry.Default.Describe(Node);
        public int Depth { get; }
        public int Index { get; }
        public bool IsListChild { get; }
        private readonly McdocRowKind? kindOverride;

        /// <summary>
        /// 行的形状跟着 Head 走（union 的 Head 每次重算，切分支后 RowKind 会变）。
        /// 只在构造时定死的话，行拼接复用旧行对象时模板会停在旧形状（"加了键，下一个兄弟模板垮掉"）。
        /// </summary>
        public McdocRowKind Kind => kindOverride ?? Head.RowKind;

        /// <summary>投影层显式指定的形状（More/AddTail 这类伪行）；为空表示跟随 Head。</summary>
        public McdocRowKind? KindOverride => kindOverride;

        public Thickness Indent => new(Depth * 16.0, 0, 0, 0);
        public string Label => IsListChild
            ? McdocPresenterRegistry.Default.ChildLabel(Node.Parent, Index)
            : Head.Label;

        /// <summary>展示用标签：动态键/未知字段保持原样，普通字段走 misode 的 formatIdentifier。</summary>
        public string DisplayLabel
        {
            get
            {
                var text = Head.IsRawKey ? Label : McdocHelpers.FormatIdentifier(Label);
                // union 的列表/元组分支以子行形态挂在同名父行下：加后缀区分，避免看着像空壳
                return string.IsNullOrEmpty(Node.UnionBranchKind) ? text : text + "（" + Node.UnionBranchKind + "）";
            }
        }
        public string Tooltip
        {
            get
            {
                var text = string.IsNullOrEmpty(Head.Documentation) ? Node.Key : Head.Documentation;
                if (Head.IsRawKey) text = (text + "（动态键）").Trim();
                if (Head.Diagnostics is { Count: > 0 }) text = (text + "\n⚠ " + string.Join("\n⚠ ", Head.Diagnostics)).Trim();
                return text;
            }
        }
        /// <summary>
        /// 展开按钮的判据 = "真的有子节点"：先看已建出来的子级（union 分支 / 列表 / struct 统一走 presenter 的
        /// Children），建过且确实空的就不再猜；只有还没建的懒建节点才按"能装 + 文档里有位置"预测，
        /// 免得可选空字段挂一个点不开的死箭头。
        /// </summary>
        public bool HasChildren
        {
            get
            {
                // 文档里没有这个键、也不是必选：只有一行 ⊕，不许有展开按钮（缺省字段不摊开子级）
                if (!Node.IsRequired && Node.Json is null) return false;
                var presenter = McdocPresenterRegistry.Default;
                if (presenter.Children(Node).Count > 0) return true;
                // 建过且确实空的节点不再猜"可能能装"，免得留一个点不开的死箭头
                return !Node.ChildrenBuilt && presenter.CanHaveChildren(Node);
            }
        }
        public Visibility ExpanderVisibility => HasChildren ? Visibility.Visible : Visibility.Hidden;

        public bool IsExpanded
        {
            get => Node.IsExpanded;
            set
            {
                if (Node.IsExpanded == value) return;
                Node.IsExpanded = value;
                McdocExpandState.Set(Node.JsonPath, value);
                OnPropertyChanged();
                projector?.SpliceExpansion(this);
            }
        }

        public bool IsListLike => McdocPresenterRegistry.Default.IsListLike(Node);

        /// <summary>列表行才显示 ⊕/⊖（misode 的 ListHead）；具体能不能加由 head 的能力位决定（tuple/定长两侧都 false）。</summary>
        public Visibility ListButtonsVisibility
            => IsListLike && (Head.CanAddTop || Head.CanAddBottom) ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>条目删除优先用 map 条目的命令，其次用字段移除（unset）。</summary>
        /// <summary>是否启用延迟实化（false 时所有行一开始就用真编辑器，便于对照测量）。</summary>
        /// <summary>默认关闭：实测摘要行更矮 → 视口物化更多容器，首屏反而更慢（668 vs 481 ms）。</summary>
        public static bool DeferEditors { get; set; } = false;

        private bool isEditorActive = !DeferEditors;

        /// <summary>行内编辑器是否已"实化"：未激活时只显示只读摘要，避免每行都造一套 Vector 控件。</summary>
        public bool IsEditorActive
        {
            get => isEditorActive;
            private set
            {
                if (isEditorActive == value) return;
                isEditorActive = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EditorContent));
            }
        }

        /// <summary>ContentControl 的内容：未激活传行本身（选择只读摘要模板），激活后传节点（真编辑器）。</summary>
        public object EditorContent
            => !IsEditorActive || Kind == McdocRowKind.Placeholder ? (object)this : Node;

        public void ActivateEditor() => IsEditorActive = true;

        /// <summary>只读摘要的文本（未激活时显示）。</summary>
        public string PreviewText
        {
            get
            {
                var value = Node.Value;
                if (value is not null) return Convert.ToString(value);
                if (Node.Json is null) return "";
                return Node.Json.Kind switch
                {
                    McdocJsonKind.String => Node.Json.StringValue,
                    McdocJsonKind.Number => Node.Json.NumberValue.ToString(),
                    McdocJsonKind.Boolean => Node.Json.BooleanValue ? "true" : "false",
                    _ => "",
                };
            }
        }

        public bool IsInlineTuple => Head.IsInlineTuple;

        /// <summary>任意 JSON 的推断类型提示（misode selectAnyType）。</summary>
        public string TypeHint => Head.TypeHint;

        public string Category => Head.Category;

        /// <summary>行尾角标：诊断 / 未知字段 / 颜色（error 压制 warning 之后还有内容才显示）。</summary>
        public Visibility DiagnosticBadgeVisibility => Diagnostics.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility UnknownBadgeVisibility => Node.IsUnknownField ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ColorBadgeVisibility => Head.IsColorPicker || Head.IsColorEnum ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>ToolTip 摘要：不截断（弹层里也要看全部）。</summary>
        public string DiagnosticSummary => Diagnostics.Count > 0 ? string.Join("\n", Diagnostics) : "";

        /// <summary>Ctrl+点击展开按钮：整棵子树展开/折叠（misode 的 expand_all / collapse_all）。</summary>
        public System.Windows.Input.ICommand ExpandAllCommand => new McdocCommand(() =>
        {
            McdocExpandState.SetAll(true);
            SetExpandedDeep(true);
        });

        public System.Windows.Input.ICommand CollapseAllCommand => new McdocCommand(() =>
        {
            McdocExpandState.SetAll(false);
            SetExpandedDeep(false);
        });

        private void SetExpandedDeep(bool expanded)
        {
            void Walk(McdocTreeNode node)
            {
                if (node is null) return;
                node.IsExpanded = expanded;
                node.EnsureChildren();
                foreach (var child in McdocPresenterRegistry.Default.Children(node)) Walk(child);
            }
            Walk(Node);
            projector?.SpliceSubtree(this);
        }

        /// <summary>颜色字段：用"色块 + 十六进制 + 随机"编辑器。</summary>
        public bool IsColor => Head.ColorKind is "hex_rgb" or "named" or "composite_rgb" or "composite_argb" or "dec_rgb";

        private static readonly Dictionary<string, Color> NamedColors = new(StringComparer.OrdinalIgnoreCase)
        {
            ["white"] = Color.FromRgb(0xF9, 0xFF, 0xFE), ["orange"] = Color.FromRgb(0xF9, 0x80, 0x1D),
            ["magenta"] = Color.FromRgb(0xC7, 0x4E, 0xBD), ["light_blue"] = Color.FromRgb(0x3A, 0xB3, 0xDA),
            ["yellow"] = Color.FromRgb(0xFE, 0xD8, 0x3D), ["lime"] = Color.FromRgb(0x80, 0xC7, 0x19),
            ["pink"] = Color.FromRgb(0xF3, 0x8D, 0xAA), ["gray"] = Color.FromRgb(0x47, 0x4F, 0x52),
            ["light_gray"] = Color.FromRgb(0x9D, 0x9D, 0x97), ["cyan"] = Color.FromRgb(0x16, 0x9C, 0x9C),
            ["purple"] = Color.FromRgb(0x89, 0x32, 0xB2), ["blue"] = Color.FromRgb(0x3C, 0x44, 0xAA),
            ["brown"] = Color.FromRgb(0x83, 0x54, 0x32), ["green"] = Color.FromRgb(0x5E, 0x7C, 0x16),
            ["red"] = Color.FromRgb(0xB0, 0x2E, 0x26), ["black"] = Color.FromRgb(0x1D, 0x1D, 0x21),
        };

        /// <summary>色块用：hex 直接解析，named 查 16 色表，dec_rgb 由整数拆 RGB；composite_* 给中性灰。</summary>
        public Brush ColorSwatchBrush
        {
            get
            {
                var text = Convert.ToString(Node.Value)?.Trim();
                if (string.IsNullOrEmpty(text)) return Brushes.Transparent;
                if (text.StartsWith("#", StringComparison.Ordinal)) return ParseHex(text);
                if (NamedColors.TryGetValue(text, out var named)) return new SolidColorBrush(named);
                if (Head.ColorKind == "dec_rgb" && int.TryParse(text, out var packed))
                {
                    return new SolidColorBrush(Color.FromRgb((byte)((packed >> 16) & 0xFF), (byte)((packed >> 8) & 0xFF), (byte)(packed & 0xFF)));
                }
                if (Head.ColorKind is "composite_rgb" or "composite_argb") return new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80));
                return Brushes.Transparent;
            }
        }

        private static Brush ParseHex(string text)
        {
            try
            {
                return (Brush)new BrushConverter().ConvertFromString(text);
            }
            catch
            {
                return Brushes.Transparent;
            }
        }

        public Brush ColorBrush
        {
            get
            {
                var text = Convert.ToString(Node.Value)?.Trim();
                if (string.IsNullOrEmpty(text) || !text.StartsWith("#")) return Brushes.Transparent;
                try
                {
                    return (Brush)new BrushConverter().ConvertFromString(text);
                }
                catch
                {
                    return Brushes.Transparent;
                }
            }
        }

        public System.Windows.Input.ICommand RandomColorCommand => new McdocCommand(() =>
        {
            var random = new Random();
            var hex = "#" + random.Next(0, 0x1000000).ToString("X6");
            Node.Value = hex;
        });

        /// <summary>More 行：还有多少项没展示。</summary>
        public int MoreRemaining => Math.Max(0, McdocPresenterRegistry.Default.Children(Node).Count - Node.ItemsShown);
        public string MoreLabel => "还有 " + MoreRemaining + " 项";
        public System.Windows.Input.ICommand ShowMoreCommand => new McdocCommand(() =>
        {
            Node.ItemsShown += Math.Max(1, McdocRowProjector.PageSize);
            projector?.SpliceSubtree(OwnerRow());
        });
        public System.Windows.Input.ICommand ShowAllCommandRow => new McdocCommand(() =>
        {
            Node.ItemsShown = int.MaxValue;
            projector?.SpliceSubtree(OwnerRow());
        });
        /// <summary>More 行对应的"列表行"：重投影要落在它身上（深度才对）。</summary>
        private McdocRow OwnerRow()
            => projector?.Rows.FirstOrDefault(row => ReferenceEquals(row.Node, Node) && row.Kind != McdocRowKind.More) ?? this;

        public IReadOnlyList<McdocTreeNode> Items => Node.Items;

        public System.Windows.Input.ICommand AddBottomCommand => Node.AddItemCommand;
        public System.Windows.Input.ICommand AddTopCommand => Node.AddItemTopCommand;
        public System.Windows.Input.ICommand SetDefaultCommand => Node.SetDefaultCommand;
        public System.Windows.Input.ICommand RemoveSelfCommand => Node.RemoveSelfCommand;
        public System.Windows.Input.ICommand MoveUpCommand => Node.MoveUpCommand;
        public System.Windows.Input.ICommand MoveDownCommand => Node.MoveDownCommand;
        /// <summary>列表/tuple 子行的"复制这一项"（Node.DuplicateCommand → DuplicateItem）。
        /// 模板绑的就是这个名字：少这个属性时按钮可见但 Command 为 null，点了什么也不发生（绑错属性不抛异常）。</summary>
        public System.Windows.Input.ICommand DuplicateCommand => Node.DuplicateCommand;

        public Visibility ListAddVisibility => Head.CanAddBottom ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ListAddTopVisibility => Head.CanAddTop ? Visibility.Visible : Visibility.Collapsed;
        /// <summary>可选且文档里还没有这个键：只给一个 ⊕（添加）。</summary>
        public Visibility SetDefaultVisibility => Head.CanSetDefault ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>可选且已经有值、并且带子级：⊕ 让位给「清空」（把值从文档里去掉）。</summary>
        public Visibility ClearVisibility
            => !Node.IsRequired && Node.Json is not null && HasChildren && Node.RemoveFieldCommand is not null
                ? Visibility.Visible
                : Visibility.Collapsed;

        public System.Windows.Input.ICommand ClearCommand => Node.RemoveFieldCommand;
        public Visibility ItemActionsVisibility => IsListChild ? Visibility.Visible : Visibility.Collapsed;

        public System.Windows.Input.ICommand RemoveCommand => Node.RemoveEntryCommand ?? Node.RemoveFieldCommand;
        public System.Windows.Input.ICommand RemoveFieldCommand => Node.RemoveFieldCommand;
        public System.Windows.Input.ICommand RemoveEntryCommand => Node.RemoveEntryCommand;

        /// <summary>misode：只有可选字段/列表项才给删除按钮（必选字段删了就是非法文档，不提供入口）。</summary>
        /// <summary>misode StructHead：可选且已存在 → 红垃圾桶（删字段）。</summary>
        /// <summary>垃圾桶只处理"没有子级"的可选字段；带子级的走「清空」，避免同一行两个删除入口。</summary>
        public Visibility RemoveFieldVisibility
            => Head.CanRemove && !HasChildren ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>动态键条目（map 里的已有键）删除。</summary>
        public Visibility RemoveEntryVisibility
            => Node.RemoveEntryCommand?.CanExecute(null) == true ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>兼容聚合：任一"删除"入口可用即视为可见。</summary>
        public Visibility RemoveVisibility
            => RemoveFieldVisibility == Visibility.Visible || RemoveEntryVisibility == Visibility.Visible
                ? Visibility.Visible
                : Visibility.Collapsed;

        /// <summary>misode ListBody 尾部的 add_bottom：一条"追加"行。</summary>
        public System.Windows.Input.ICommand AddTailCommand => Node.AddItemCommand;
        public Visibility AddTailVisibility => Head.CanAddBottom ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>misode ErrorIndicator：点开看诊断明细。</summary>
        public System.Windows.Input.ICommand ToggleDiagnosticsCommand
            => new McdocCommand(() => IsDiagnosticsOpen = !IsDiagnosticsOpen);

        private bool isDiagnosticsOpen;
        public bool IsDiagnosticsOpen
        {
            get => isDiagnosticsOpen;
            set
            {
                if (isDiagnosticsOpen == value) return;
                isDiagnosticsOpen = value;
                OnPropertyChanged();
            }
        }

        /// <summary>misode Key 的 doc 面板：点标签打开。</summary>
        public System.Windows.Input.ICommand ToggleDocCommand
            => new McdocCommand(() => IsDocOpen = !IsDocOpen, () => HasDocumentation);

        private bool isDocOpen;
        public bool IsDocOpen
        {
            get => isDocOpen;
            set
            {
                if (isDocOpen == value) return;
                isDocOpen = value;
                OnPropertyChanged();
            }
        }

        public bool HasDocumentation => !string.IsNullOrEmpty(Head.Documentation);
        public string Documentation => Head.Documentation ?? "";

        /// <summary>
        /// 展示用诊断（misode ErrorIndicator 的规则，McdocRenderer.tsx:1250-1282）：
        /// 1) 有 error 就只留 error（warning 被压制）；2) 必填缺键的空壳行不挂标记；
        /// 3) 条目不截断（弹层要显示全部）；4) 去掉 "(rule: xxx)" 后缀。
        /// 规则本体（哪条算 error）由求值层产出，这里只按消息前缀区分。
        /// </summary>
        public IReadOnlyList<string> Diagnostics
        {
            get
            {
                var raw = Head.Diagnostics;
                if (raw is null || raw.Count == 0) return Array.Empty<string>();
                if (IsMissingKeyShell()) return Array.Empty<string>();
                var errors = new List<string>();
                var warnings = new List<string>();
                foreach (var text in raw)
                {
                    if (string.IsNullOrEmpty(text)) continue;
                    var line = StripRuleSuffix(text);
                    if (IsErrorLine(text)) errors.Add(line);
                    else warnings.Add(line);
                }
                return errors.Count > 0 ? errors : warnings;
            }
        }

        /// <summary>必填缺键的空壳行：唯一的诊断就是"缺少必填键"，且这一行本身没有值（misode 不挂 ! 标记）。</summary>
        private bool IsMissingKeyShell()
        {
            if (Node.Json is not null) return false;
            var raw = Head.Diagnostics;
            if (raw is null || raw.Count == 0) return false;
            foreach (var text in raw)
            {
                if (text is null || !text.StartsWith("缺少必填键", StringComparison.Ordinal)) return false;
            }
            return true;
        }

        /// <summary>求值层的 error 级消息前缀（warning：未声明键 / 列表长度 / 属性取值域）。</summary>
        private static bool IsErrorLine(string text)
            => text is not null && (text.StartsWith("缺少必填键", StringComparison.Ordinal)
                || text.StartsWith("未解析的类型引用", StringComparison.Ordinal)
                || text.StartsWith("值与类型不符", StringComparison.Ordinal)
                || text.StartsWith("必选字段", StringComparison.Ordinal));

        /// <summary>去掉 misode 会 strip 的 "(rule: xxx)" 后缀。</summary>
        private static string StripRuleSuffix(string text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";
            var start = text.LastIndexOf("(rule:", StringComparison.OrdinalIgnoreCase);
            if (start < 0) return text;
            var close = text.IndexOf(')', start);
            if (close < 0) return text;
            return (text.Substring(0, start) + text.Substring(close + 1)).Trim();
        }

        /// <summary>
        /// misode ItemIdPreview：有真实纹理就画图（McdocItemTextures 从 Resource/Textures/item 取），
        /// 没有就用"确定性瓷砖"（id 的稳定 hash → 色相 + 首字母），ToolTip 仍是完整 id。
        /// </summary>
        public bool ShowItemPreview => Head.IsIconSelect;
        public string ItemId => Convert.ToString(Node.Value) ?? "";
        public System.Windows.Media.ImageSource ItemTexture => ShowItemPreview ? McdocItemTextures.Get(ItemId) : null;
        public Visibility ItemImageVisibility => ItemTexture is not null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ItemTileVisibility => ItemTexture is null ? Visibility.Visible : Visibility.Collapsed;
        public Brush ItemTileBrush => new SolidColorBrush(McdocItemTextures.TileColor(ItemId));
        public string ItemPreviewText => McdocItemTextures.TileText(ItemId);

        /// <summary>misode Errors：错误红 / 警告琥珀（缺必填键算错误）。</summary>
        public Brush ErrorBrush
            => Head.Diagnostics is { Count: > 0 } diagnostics
                && diagnostics.Any(text => text is not null && text.StartsWith("缺少必填键", StringComparison.Ordinal))
                ? new SolidColorBrush(Color.FromRgb(0xD9, 0x53, 0x4F))
                : new SolidColorBrush(Color.FromRgb(0xF0, 0xAD, 0x4E));

        /// <summary>misode .node-error ~ input：有诊断时编辑器外框用错误色，否则透明。</summary>
        public Brush ErrorBorderBrush
            => Head.Diagnostics is { Count: > 0 } ? ErrorBrush : Brushes.Transparent;

        /// <summary>
        /// 可输入下拉（misode 的 &lt;input list=datalist&gt;，白名单外 #[id]）的手输值：
        /// 写回只有一条出口 —— Node.Value → ValueChanged → McdocCollectionEditor.ApplyNodeValue。
        /// 不往 SelectedItem 里塞候选外的值（那会被"稳定实例"限制弹回空白）。
        /// </summary>
        public string CustomSelectText
        {
            get => Convert.ToString(Node.Value) ?? "";
            set
            {
                var text = value ?? "";
                if (string.Equals(Convert.ToString(Node.Value) ?? "", text, StringComparison.Ordinal)) return;
                if (text.Length == 0)
                {
                    // 空串 = unset：可选字段直接把键删掉（misode 的 unset 语义），必选字段就写空串
                    Node.Value = "";
                    if (!Node.IsRequired && Node.RemoveFieldCommand?.CanExecute(null) == true)
                    {
                        Node.RemoveFieldCommand.Execute(null);
                    }
                    OnPropertyChanged();
                    return;
                }
                Node.Value = text;
                OnPropertyChanged();
            }
        }

        public Visibility DuplicateVisibility
            => Node.DuplicateCommand?.CanExecute(null) == true ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>列表/tuple 子行的"删掉这一项"（Node.RemoveSelfCommand → RemoveItemAt）。</summary>
        public Visibility RemoveSelfVisibility
            => ItemActionsVisibility == Visibility.Visible && Node.RemoveSelfCommand?.CanExecute(null) == true
                ? Visibility.Visible
                : Visibility.Collapsed;

        public Visibility DiagnosticsVisibility => DiagnosticBadgeVisibility;

        public IReadOnlyList<EnumMember> UnionOptions => Head.BranchOptions;

        public EnumMember SelectedUnionOption
        {
            get => Node.SelectedUnionTypeName;
            set
            {
                // 分支切换的节点级语义收在 McdocUnionSwitch（unset / 同支 / 展开 / getChange 改写文档）
                if (!McdocUnionSwitch.SwitchTo(Node, value)) return;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsExpanded));
                // 先让 Head 跟上新分支，再整行重建：ContentControl 只有换内容才会重新选编辑器模板
                RefreshHead();
                if (projector is not null) projector.ReplaceRow(this);
                NotifyLayout();
            }
        }

        private bool renamingKey;

        /// <summary>
        /// 键编辑框只给"新键占位"行；条目行的键已经是文档里的真实键，
        /// 常驻文本框既没用又会把键名盖成空白。要改名就点行上的「重命名」显式进入编辑。
        /// </summary>
        public bool IsRenamingKey
        {
            get => renamingKey;
            set
            {
                if (renamingKey == value) return;
                renamingKey = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(KeyTextVisibility));
                OnPropertyChanged(nameof(KeyLabelVisibility));
                OnPropertyChanged(nameof(AddEntryVisibility));
            }
        }

        public Visibility KeyTextVisibility
            => !Node.UseKeySelect && (IsRenamingKey || (Node.IsDynamicKey && !Node.IsMapEntry))
                ? Visibility.Visible
                : Visibility.Collapsed;

        /// <summary>键以只读标签显示（条目行）：键编辑框收起时必须有东西顶上去，否则整行没有键名。</summary>
        public Visibility KeyLabelVisibility
            => KeyTextVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

        /// <summary>"加入"按钮只在需要提交键时才有意义：新键占位行，或条目行正在重命名。</summary>
        public Visibility AddEntryVisibility
            => !Node.IsMapEntry || IsRenamingKey ? Visibility.Visible : Visibility.Collapsed;

        public System.Windows.Input.ICommand RenameKeyCommand => new McdocCommand(() => IsRenamingKey = true);



        /// <summary>条目行常驻"重命名"入口（键以只读标签显示，改名要点它）。</summary>
        public Visibility RenameVisibility => Node.IsMapEntry ? Visibility.Visible : Visibility.Collapsed;
        /// <summary>键下拉只给"新键占位行"，以及条目行正在重命名的时候。</summary>
        public Visibility KeyComboVisibility
            => Node.UseKeySelect && (!Node.IsMapEntry || IsRenamingKey) ? Visibility.Visible : Visibility.Collapsed;
        public IReadOnlyList<EnumMember> KeyOptions => Node.KeyOptionList;

        public EnumMember SelectedKey
        {
            get => Node.SelectedKey;
            set
            {
                if (ReferenceEquals(Node.SelectedKey, value)) return;
                Node.SelectedKey = value;
                OnPropertyChanged();
            }
        }

        public void NotifyLayout()
        {
            OnPropertyChanged(nameof(HasChildren));
            OnPropertyChanged(nameof(ExpanderVisibility));
            OnPropertyChanged(nameof(IsExpanded));
        }

        /// <summary>重算 Head（union 切分支 / 结构变化后必须调用），并通知所有绑定重读。</summary>
        public void RefreshHead()
        {
            head = McdocPresenterRegistry.Default.Describe(Node);
            OnPropertyChanged(nameof(Kind));
            OnPropertyChanged(nameof(Head));
            OnPropertyChanged(string.Empty);
        }

        /// <summary>行被丢弃时退订，避免节点长期持有已死行。</summary>
        public void Detach() => Node.PropertyChanged -= OnNodePropertyChanged;

        private void OnNodePropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(McdocTreeNode.SelectedEnumOption) && Node.AllowCustomValue)
            {
                // 从候选里选了一项：把值同步到 Node.Value（同一个写回出口），输入框才不会"弹回"
                var text = Convert.ToString(Node.SelectedEnumOption?.Value?.LiteralValue)
                    ?? Convert.ToString(Node.SelectedEnumOption?.Name) ?? "";
                if (!string.Equals(Convert.ToString(Node.Value) ?? "", text, StringComparison.Ordinal))
                {
                    Node.Value = text;
                }
                OnPropertyChanged(nameof(CustomSelectText));
                return;
            }
            if (args.PropertyName == nameof(McdocTreeNode.Value))
            {
                // 值变了：预览格（纹理/瓷砖）与手输框都要重读
                OnPropertyChanged(nameof(ItemId));
                OnPropertyChanged(nameof(ItemTexture));
                OnPropertyChanged(nameof(ItemImageVisibility));
                OnPropertyChanged(nameof(ItemTileVisibility));
                OnPropertyChanged(nameof(ItemTileBrush));
                OnPropertyChanged(nameof(ItemPreviewText));
                OnPropertyChanged(nameof(CustomSelectText));
                return;
            }
            if (string.IsNullOrEmpty(args.PropertyName))
            {
                // 节点级"整体变了"（union 切分支）：Head 是构造时缓存的，必须按新分支重算，
                // 否则切回标量支后行上还是旧 Head，编辑器不回来。
                RefreshHead();
                NotifyLayout();
                return;
            }
            if (args.PropertyName != nameof(McdocTreeNode.IsExpanded)) return;
            OnPropertyChanged(nameof(IsExpanded));
            OnPropertyChanged(nameof(HasChildren));
            OnPropertyChanged(nameof(ExpanderVisibility));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
