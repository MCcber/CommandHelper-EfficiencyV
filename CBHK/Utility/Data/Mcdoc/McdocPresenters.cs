using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MinecraftLanguageModelLibrary.Data;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// misode 的 AnyHead + AnyBody：行上是"类型下拉 + 该类型的编辑器"，
    /// 选中的类型是 object/array 时，子级按该类型真正展开（不是只能手写 JSON）。
    /// </summary>
    public sealed class AnyNodePresenter : ScalarNodePresenter
    {
        public override bool CanPresent(McdocType type)
            => type?.Kind is McdocTypeKind.Any or McdocTypeKind.Unsafe;

        public override bool CanHaveChildren(McdocTreeNode node)
            => node?.Json?.Kind switch
            {
                McdocJsonKind.Object => (node.Json.Properties?.Count ?? 0) > 0,
                McdocJsonKind.Array => (node.Json.Items?.Count ?? 0) > 0,
                _ => false,
            };

        public override bool IsListLikeFor(McdocTreeNode node)
            => node?.Json?.Kind == McdocJsonKind.Array;

        /// <summary>数组形态的 any：子行就是数组项（list Body 把它们挂在 Items 上）。</summary>
        public override IReadOnlyList<McdocTreeNode> Children(McdocTreeNode node)
            => node.Items.Count > 0 ? node.Items : node.Children;

        public override void PopulateChildren(McdocTreeNode node, McdocType type, McdocJsonValue json, McdocBuildContext context)
        {
            // misode selectAnyType：按 JSON 的类型挑 ANY_TYPES 里的一项，然后走对应 Body
            var synthetic = json?.Kind switch
            {
                McdocJsonKind.Object => McdocHelpers.AnyTypeOf("struct"),
                McdocJsonKind.Array => McdocHelpers.AnyTypeOf("list"),
                _ => null,
            };
            if (synthetic is null) return;
            McdocPresenterRegistry.Default.For(synthetic).PopulateChildren(node, synthetic, json, context);
        }
    }

    /// <summary>
    /// 版本更迭后遗留的键（schema 里没有这个键）：只说明"这一行存在" —— 标签 + 黄色感叹号。
    /// 不建子级、不给编辑器、不给任何按钮；文档里那份值原样保留（删不删由行首的删除入口决定）。
    /// 只按**节点**分派（<see cref="McdocTreeNode.IsUnknownField"/>），不参与类型竞争。
    /// </summary>
    public sealed class UnknownFieldPresenter : McdocNodePresenter
    {
        public override bool CanPresent(McdocType type) => false;

        public override bool CanHaveChildren(McdocTreeNode node) => false;

        public override IReadOnlyList<McdocTreeNode> Children(McdocTreeNode node) => [];

        public override bool IsListLike => false;

        /// <summary>子结构一律不建：遗留键的整棵子树在树上就是一格（值还在文档里，没被删）</summary>
        public override void PopulateChildren(McdocTreeNode node, McdocType type, McdocJsonValue json, McdocBuildContext context)
        {
        }

        public override McdocNodeHead Describe(McdocTreeNode node) => new()
        {
            Label = node?.FieldName ?? "",
            RowKind = McdocRowKind.Plain,
            EditorKind = McdocEditorKind.None,
            CanAddTop = false,
            CanAddBottom = false,
            CanSetDefault = false,
        };
    }

    /// <summary>标量行：string / enum / 数值 / bool / literal。map 条目与新增占位行也落在这里（行形状由标志决定）。</summary>
    public class ScalarNodePresenter : McdocNodePresenter
    {
        public override bool CanHaveChildren(McdocTreeNode node) => false;

        public override bool CanPresent(McdocType type)
            => type is null || type.Kind is McdocTypeKind.String or McdocTypeKind.Enum or McdocTypeKind.Boolean
                or McdocTypeKind.Byte or McdocTypeKind.Short or McdocTypeKind.Int or McdocTypeKind.Long
                or McdocTypeKind.Float or McdocTypeKind.Double or McdocTypeKind.Literal
                or McdocTypeKind.Reference
                or McdocTypeKind.Mapped or McdocTypeKind.Concrete or McdocTypeKind.Template or McdocTypeKind.Indexed;

        /// <summary>misode ANY_TYPES：boolean / double / string / list / struct（可选再加 unset）。</summary>
        private static void FillAnyTypeOptions(McdocTreeNode node, McdocJsonValue json)
        {
            string[] names = ["boolean", "double", "string", "list", "struct"];
            var options = new ObservableCollection<EnumMember>();
            if (!node.IsRequired || json is null)
            {
                options.Add(new EnumMember { Name = UnsetLabel, Value = new MetaValue { LiteralValue = -1 } });
            }
            foreach (var name in names)
            {
                options.Add(new EnumMember { Name = name, Value = new MetaValue { LiteralValue = name } });
            }
            node.AnyTypeOptions = options;
            var current = json?.Kind switch
            {
                McdocJsonKind.Boolean => "boolean",
                McdocJsonKind.Number => "double",
                McdocJsonKind.String => "string",
                McdocJsonKind.Array => "list",
                McdocJsonKind.Object => "struct",
                _ => null,
            };
            node.SetAnyTypeSilently(options.FirstOrDefault(option => option.Name == current));
        }

        public override McdocNodeHead Describe(McdocTreeNode node)
        {
            var colorKind = ColorKindFor(node);
            var mirrored = MirroredEditorKind(node);
            return new McdocNodeHead
            {
                Label = node.FieldName,
                Documentation = node.Desc,
                RowKind = RowKindFor(node),
                EditorKind = mirrored == McdocEditorKind.None ? EditorKindFor(node.Type, node.UseSelect) : mirrored,
                IsSelect = node.UseSelect,
                AllowCustomValue = node.AllowCustomValue,
                Options = node.EnumOptionList,
                Watermark = PlaceholderFor(node),
                IsRawKey = IsRawKeyNode(node),
                TypeHint = TypeHintFor(node),
                ColorKind = colorKind,
                IsColorPicker = colorKind == "hex_rgb",
                IsColorEnum = !string.IsNullOrEmpty(colorKind) && colorKind != "hex_rgb",
                IsIconSelect = IsIconSelectFor(node),
                Category = McdocHelpers.GetCategory(node.Type),
                Diagnostics = DiagnosticsFor(node),
            };
        }

        public override void Prepare(McdocTreeNode node, McdocType type, McdocJsonValue json, McdocBuildContext context)
        {
            // #[id] / #[tree{registry}] 的候选值（misode getValues 的注册表分支）
            McdocSelectOptions.Apply(node, type, context.Runtime);

            // #[random]：默认值是随机值，行上给一个"重掷"
            if (McdocHelpers.HasAttribute(type, "random"))
            {
                node.HasRandom = true;
                node.RegenerateSeedCommand = new McdocCommand(() => node.Value = type.Kind == McdocTypeKind.Long
                    ? (object)Random.Shared.NextInt64()
                    : Random.Shared.Next(0, int.MaxValue));
            }
            switch (type.Kind)
            {
                // misode LiteralHead：等价于"只有一个成员的 union"，渲染成下拉
                case McdocTypeKind.Literal:
                    node.EnumOptionList = new ObservableCollection<EnumMember>
                    {
                        new() { Name = type.LiteralText(), Value = new MetaValue { LiteralValue = type.Value } },
                    };
                    node.SetEnumOptionSilently(node.EnumOptionList[0]);
                    break;

                // misode AnyHead：先选 JSON 类型，再给该类型的编辑器（list/struct 用 Raw 文本框兜底）
                case McdocTypeKind.Any or McdocTypeKind.Unsafe:
                    FillAnyTypeOptions(node, json);
                    break;

                case McdocTypeKind.Enum:
                    node.EnumOptionList = new ObservableCollection<EnumMember>();
                    foreach (var value in type.EnumValues ?? [])
                    {
                        node.EnumOptionList.Add(new EnumMember
                        {
                            Name = value.Identifier,
                            Value = new MetaValue { LiteralValue = value.Value }
                        });
                    }
                    // 选中项按"字面量值"匹配（JSON 里存的是 Block = "block" 的 "block"，不是标识符 Block）
                    var text = json switch
                    {
                        { Kind: McdocJsonKind.String } => json.StringValue,
                        { Kind: McdocJsonKind.Number } => Convert.ToString(json.NumberValue),
                        { Kind: McdocJsonKind.Boolean } => Convert.ToString(json.BooleanValue),
                        _ => null,
                    };
                    node.SetEnumOptionSilently((text is null
                        ? null
                        : node.EnumOptionList.FirstOrDefault(o => Convert.ToString(o.Value?.LiteralValue) == text))
                        ?? node.EnumOptionList.FirstOrDefault());
                    break;

                case McdocTypeKind.Boolean:
                    if (json != null && json.Kind == McdocJsonKind.Boolean)
                    {
                        node.SetBoolSilently(json.BooleanValue);
                    }
                    // 缺键时两个都不选：这里**不能**看 IsRequired —— 字段的必选性是父级建完子节点后才写的，
                    // 此刻还是默认值，用它判断会把"可选 bool"也当成必填落成 false。
                    break;

                default:
                    // 标量：把 JSON 值摊到 Value 上，模板直接绑它
                    switch (json?.Kind)
                    {
                        case McdocJsonKind.String:
                            node.SetValueSilently(json.StringValue);
                            break;
                        case McdocJsonKind.Number:
                            node.SetValueSilently(json.IsInteger
                                ? (object)(long)json.NumberValue
                                : (double)json.NumberValue);
                            break;
                    }
                    break;
            }
        }
    }

    /// <summary>结构行：字段子行（静态字段 / 动态键条目 / 新增占位行）。</summary>
    public sealed class StructNodePresenter : McdocNodePresenter
    {
        public override bool CanPresent(McdocType type) => type?.Kind == McdocTypeKind.Struct;

        public override McdocNodeHead Describe(McdocTreeNode node) => new()
        {
            Label = node.FieldName,
            Documentation = node.Desc,
            RowKind = RowKindFor(node),
            Diagnostics = DiagnosticsFor(node),
            IsRawKey = IsRawKeyNode(node),
            CanSetDefault = CanSetDefaultField(node),
        };

        public override bool FlattensRoot => true;

        public override bool CanHaveChildren(McdocTreeNode node)
            => node.Type?.Fields is { Count: > 0 } || node.Children.Count > 0;

        public override void PopulateChildren(McdocTreeNode node, McdocType type, McdocJsonValue json, McdocBuildContext context)
        {
            // 字段本身还没进文档（可选字段没填 / 刚被删掉）：不摊开子级。
            // 否则删掉一个结构字段以后，它的子结构还整片挂在树上（"点了删除按钮没反应"就是这么来的）；
            // 用户点行上的 ⊕ 后 JSON 出现，再重建就会长出来。
            // 可选且文档里没有这个键：只留一行 ⊕（子级不摊开）。
            // 必选缺值是**非法文档**：照常摊开声明出来的子字段行，每行各自挂"缺少必填键"，
            // 否则这一行既没有编辑器、也没有 ⊕，展开还建不出子级 —— 用户面前就是一行死节点。
            if (node.Parent is not null && json is null && !node.IsRequired)
            {
                return;
            }

            var isObject = json is { Kind: McdocJsonKind.Object };
            var fields = type.Fields ?? [];
            // 静态字段 = 字面量键；动态字段 = 其余（misode：field.key.kind === 'literal'）
            var staticFields = fields.Where(field => field is not null && LiteralName(field) is not null).ToList();
            var dynamicFields = fields
                .Where(field => field is not null && LiteralName(field) is null && !IsDispatchFallback(field))
                .ToList();
            var dynamicSpecs = dynamicFields
                .Select(field => McdocSelectOptions.Resolve(UnwrapDynamicFieldKeyType(field)))
                .ToList();

            var consumed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in staticFields) consumed.Add(LiteralName(field));

            // 动态字段在「静态字段序列」里的声明位置：对拍时用来插回 {name:null,dynamic:true} 占位
            node.DynamicFieldPositions.Clear();
            var staticBefore = 0;
            foreach (var field in fields)
            {
                if (field is null) continue;
                if (LiteralName(field) is not null)
                {
                    staticBefore++;
                    continue;
                }
                // 派发兜底字段（key any + type any）渲染时仍然隐藏，但位置要记下来：
                // oracle 的 simplify 输出里这类字段是 {key:any,type:any}，对拍时按这个位置还原成占位
                node.DynamicFieldPositions.Add(staticBefore);
            }

            // 1) 静态字段：声明序
            foreach (var field in staticFields)
            {
                node.Children.Add(BuildStaticField(field, isObject ? json.Get(LiteralName(field)) : null, context));
            }

            // 2) 每个动态字段一行"新键"（misode：DynamicKey，先于已有条目）
            for (var index = 0; index < dynamicFields.Count; index++)
            {
                node.Children.Add(BuildDynamicPlaceholder(node, dynamicFields[index], index, dynamicFields.Count,
                    index < dynamicSpecs.Count ? dynamicSpecs[index] : null, json, context));
            }

            // 3) 已有条目：按 JSON 顺序；没有归属动态字段的键 → UnknownField
            if (!isObject) return;
            foreach (var pair in json.Properties ?? [])
            {
                if (consumed.Contains(pair.Key)) continue;
                node.Children.Add(BuildEntry(node, pair.Key, pair.Value, dynamicFields, dynamicSpecs, json, context));
            }
        }

        /// <summary>
        /// 按一个键建出成员行：这个键在 PopulateChildren 里走哪条分支，这里就走哪条
        /// （文本 → 树的根层成员差分只建这一行，不能为一行重跑整层）。
        /// </summary>
        public McdocTreeNode CreateMemberNode(McdocTreeNode container, McdocType structType, McdocJsonValue containerJson,
            string key, McdocBuildContext context)
        {
            if (container is null || string.IsNullOrEmpty(key)) return null;
            var fields = structType?.Fields ?? [];
            var value = containerJson is { Kind: McdocJsonKind.Object } ? containerJson.Get(key) : null;
            var field = fields.FirstOrDefault(item => item is not null
                && string.Equals(LiteralName(item), key, StringComparison.Ordinal));
            if (field is not null) return BuildStaticField(field, value, context);
            var dynamicFields = fields
                .Where(item => item is not null && LiteralName(item) is null && !IsDispatchFallback(item))
                .ToList();
            if (dynamicFields.Count == 0) return BuildUnknownField(key, value, context);
            var dynamicSpecs = dynamicFields
                .Select(item => McdocSelectOptions.Resolve(UnwrapDynamicFieldKeyType(item)))
                .ToList();
            return BuildEntry(container, key, value, dynamicFields, dynamicSpecs, containerJson, context);
        }

        /// <summary>这个键是不是 struct 里声明过的字面量字段：决定"删键留一行 ⊕"还是"整行摘掉"。</summary>
        internal static bool IsStaticField(McdocType structType, string key)
            => (structType?.Fields ?? []).Any(field => field is not null
                && string.Equals(LiteralName(field), key, StringComparison.Ordinal));

        /// <summary>静态字段：必选性 / 文档注释 / 「缺少必填键」诊断都在这里。</summary>
        private static McdocTreeNode BuildStaticField(McdocStructField field, McdocJsonValue fieldJson, McdocBuildContext context)
        {
            var name = LiteralName(field);
            var keyNode = new McdocRuntimeNode(name, McdocType.LiteralString(name));
            // 可选 + 文档里没这个键：只留一行 ⊕，别在饿建期把它的子结构建出来（投影对这类节点一列子行都不画）。
            // 必填缺值是非法文档，仍要摊开（行上还要挂"缺少必填键"），所以只对可选成员 defer。
            var child = context.Child(name, field.Type, fieldJson, keyNode, context.Depth + 1, context.JsonPath + "." + name,
                deferChildren: fieldJson is null && field.Optional);
            child.IsRequired = !field.Optional;
            // 可选字段的下拉第 0 项要有（未设置）：选中即删键。顺序是硬约束（IsRequired 落定之后）
            McdocSelectOptions.EnsureUnsetOption(child);
            child.Desc = field.Desc ?? "";
            if (!string.IsNullOrEmpty(child.Desc)) child.Key = child.Desc;
            // misode：必填字段缺键 → missing key 错误指示
            if (!field.Optional && fieldJson is null)
            {
                child.Error = "缺少必填键：" + name;
            }
            return child;
        }

        /// <summary>动态字段的「新键」占位行：只负责起键（键输入 + 候选 + ⊕）。</summary>
        private static McdocTreeNode BuildDynamicPlaceholder(McdocTreeNode container, McdocStructField field, int index,
            int dynamicFieldCount, McdocIdSpec spec, McdocJsonValue json, McdocBuildContext context)
        {
            var keyType = UnwrapDynamicFieldKeyType(field);
            var valueType = UnwrapDynamicFieldValueType(field);
            var placeholder = new McdocTreeNode
            {
                FieldName = "",
                Type = McdocType.Any(),
                DeclaredType = valueType,     // 提交时按这个类型 + 新键求默认值
                Json = null,
                JsonPath = context.JsonPath,
                IsRequired = false,
                IsDynamicKey = true,
                Watermark = "新键",
                EditingKey = "",
                DynamicFieldIndex = index,
                ValueChanged = context.OnValueChanged,
                StructureChanged = context.OnStructureChanged,
            };
            if (dynamicFieldCount > 1 && spec is not null)
            {
                placeholder.Desc = spec.Prefix is { Length: > 0 }
                    ? "新键（" + spec.Prefix + " 前缀）"
                    : "新键（" + spec.Registry + "）";
            }
            McdocSelectOptions.ApplyKeyOptions(placeholder, keyType, context.Runtime,
                (json?.Properties ?? []).Select(pair => pair.Key).ToList());
            placeholder.AddEntryCommand = new McdocCommand(
                () => context.Editor.CommitEntry(container, placeholder, context.Runtime),
                () => context.Editor.CanCommitEntry(container, placeholder));
            return placeholder;
        }

        /// <summary>已有条目：没有动态字段 → UnknownField，否则按前缀/候选把键归到一个动态字段。</summary>
        private static McdocTreeNode BuildEntry(McdocTreeNode container, string key, McdocJsonValue value,
            List<McdocStructField> dynamicFields, List<McdocIdSpec> dynamicSpecs, McdocJsonValue json,
            McdocBuildContext context)
        {
            if (dynamicFields.Count == 0) return BuildUnknownField(key, value, context);
            var itemKeyNode = new McdocRuntimeNode(key, McdocType.LiteralString(key));
            var owner = PickDynamicFieldForKey(key, dynamicSpecs);
            if (owner < 0 || owner >= dynamicFields.Count) owner = dynamicFields.Count - 1;
            var field = dynamicFields[owner];
            var child = context.Child(key, UnwrapDynamicFieldValueType(field), value, itemKeyNode,
                context.Depth + 1, context.JsonPath + "." + key, deferChildren: value is null);
            child.IsRequired = false;
            child.IsDynamicKey = false;
            child.IsMapEntry = true;
            child.EditingKey = key;
            child.DynamicFieldIndex = owner;
            McdocSelectOptions.ApplyKeyOptions(child, UnwrapDynamicFieldKeyType(field), context.Runtime,
                (json?.Properties ?? []).Select(pair => pair.Key).ToList());
            child.AddEntryCommand = new McdocCommand(
                () => context.Editor.CommitEntry(container, child, context.Runtime),
                () => context.Editor.CanCommitEntry(container, child));
            // 条目随时可以删：值被（未设置）清掉时键已经不在文档里，删除就退化成"摘掉这一行"
            child.RemoveEntryCommand = new McdocCommand(
                () => context.Editor.RemoveEntry(container, child),
                () => child.IsMapEntry);
            return child;
        }

        /// <summary>
        /// 当前版本不存在的键（misode 的 UnknownField，版本更迭后遗留的那种）：管线对它只保留"这一行"，
        /// 不解析、不建子级、不给编辑器（见 <see cref="UnknownFieldPresenter"/>），文档里那份值原样不动。
        /// </summary>
        private static McdocTreeNode BuildUnknownField(string key, McdocJsonValue value, McdocBuildContext context)
        {
            var itemKeyNode = new McdocRuntimeNode(key, McdocType.LiteralString(key));
            // unknownField 必须在建节点时给：表现器/工厂都按它改道，事后再打标记已经晚了
            var unknown = context.Child(key, McdocType.Any(), value, itemKeyNode,
                context.Depth + 1, context.JsonPath + "." + key, deferChildren: true, unknownField: true);
            // 没有哪份 schema 声明过这个键，"必选"对它是无意义的：不置 false 就永远删不掉
            // （删除原语自己守着 !IsRequired，见 McdocNodeFactory.RemoveFieldCommand）
            unknown.IsRequired = false;
            unknown.Desc = "当前版本不存在该节点（版本更迭后遗留的键）";
            return unknown;
        }

        /// <summary>字面量键名（misode 的 field.key.kind === 'literal'）。</summary>
        private static string LiteralName(McdocStructField field)
            => field.Name ?? (field.Key is { Kind: McdocTypeKind.Literal } key ? key.LiteralText() : null);

        /// <summary>派发兜底字段（key any + type any）：misode 直接隐藏，不出"新键"行。</summary>
        private static bool IsDispatchFallback(McdocStructField field)
            => UnwrapDynamicFieldKeyType(field)?.Kind == McdocTypeKind.Any
                && UnwrapDynamicFieldValueType(field)?.Kind == McdocTypeKind.Any;

        /// <summary>动态键归哪个字段：带前缀的规格最具体（"!移除"形式），其次是普通规格，都没有 #[id] 时按声明顺序。</summary>
        private static int PickDynamicFieldForKey(string key, IReadOnlyList<McdocIdSpec> specs)
        {
            for (var i = 0; i < specs.Count; i++)
            {
                if (specs[i]?.Prefix is { Length: > 0 } && specs[i].AcceptsKey(key)) return i;
            }
            for (var i = 0; i < specs.Count; i++)
            {
                if (specs[i]?.AcceptsKey(key) == true) return i;
            }
            for (var i = 0; i < specs.Count; i++)
            {
                if (specs[i] is null) return i;
            }
            return 0;
        }

        /// <summary>
        /// 这个结构里有没有"能收下这个键"的动态字段（map 成员）。判定必须走 #[id] 的 prefix 规则，
        /// 不能只看"有没有无名字段"：**spread 字段与派发兜底字段也长成无名字段**，按它们放宽，
        /// 旧成员的残影键就再也剪不掉了（切成员后 terms 与 block 并排的现场会回来）。
        /// </summary>
        internal static bool AcceptsDynamicKey(McdocType structType, string key)
        {
            if (structType?.Fields is null || string.IsNullOrEmpty(key)) return false;
            foreach (var field in structType.Fields)
            {
                if (field is null || field.IsSpread || IsDispatchFallback(field)) continue;
                if (LiteralName(field) is not null) continue;          // 静态字段不归动态键管
                var spec = McdocSelectOptions.Resolve(UnwrapDynamicFieldKeyType(field));
                if (spec is null || spec.AcceptsKey(key)) return true; // 没有 #[id] 约束的 map 收任意键
            }
            return false;
        }

        /// <summary>动态字段的键类型：Tuple〔keyType, valueType〕的第一项，或字段自身的 Key。</summary>
        private static McdocType UnwrapDynamicFieldKeyType(McdocStructField field)
        {
            if (field?.Type is { Kind: McdocTypeKind.Tuple, Items: { Count: >= 1 } elements })
            {
                return elements[0];
            }
            return field?.Key ?? McdocType.Create(McdocTypeKind.String);
        }

        /// <summary>动态字段的取值类型：Tuple 的第二项；不是 Tuple 时原样返回。</summary>
        private static McdocType UnwrapDynamicFieldValueType(McdocStructField field)
        {
            if (field?.Type is { Kind: McdocTypeKind.Tuple, Items: { Count: >= 2 } elements })
            {
                return elements[1];
            }
            return field?.Type ?? McdocType.Any();
        }
    }

    /// <summary>union 行：标签 + 分支下拉 + 当前分支的标量编辑器（misode 的 UnionHead 同款布局）。</summary>
    public sealed class UnionNodePresenter : McdocNodePresenter
    {
        public override bool CanPresent(McdocType type) => type?.Kind == McdocTypeKind.Union;

        public override McdocNodeHead Describe(McdocTreeNode node)
        {
            // misode UnionHead：可选时下拉首项是 unset（选中即删除该字段）
            // misode UnionHead：{(!selectedType || optional) && <option value=SPECIAL_UNSET>}
            // 已经匹配到成员的可选/必选都不给 unset；必选但没匹配到时要给（并且默认就落在它上面）
            var canUnset = node.Parent is not null && (!node.IsRequired || node.SelectedUnionMember is null);
            // 空联合（文档里真的声明成 ()）也是合法形态：分支列表可能还没建出来，这里一律给非 null 集合
            var branches = node.UnionTypeNameList ?? new System.Collections.ObjectModel.ObservableCollection<EnumMember>();
            if (canUnset)
            {
                branches = new System.Collections.ObjectModel.ObservableCollection<EnumMember>
                {
                    new() { Name = UnsetLabel, Value = new MetaValue { LiteralValue = -1 } }
                };
                foreach (var item in node.UnionTypeNameList ?? []) branches.Add(item);
            }
            return new McdocNodeHead
            {
                Label = node.FieldName,
                Documentation = node.Desc,
                // 新建键占位行是输入框（Keyed）；但**已有条目**的值若是 union，必须给分支下拉 ——
                // 以前按"行种类"一律走 Keyed 模板，于是 attributes.<键> 只见字段不见选择器（现场）。
                RowKind = node.IsDynamicKey && !node.IsMapEntry
                    ? RowKindFor(node)
                    : node.Type?.Kind == McdocTypeKind.Union
                        ? McdocRowKind.Union
                        : RowKindFor(node),
            // 列表分支的子级摊在 union 行下面：⊕ / 尾部追加也由这一行出（原来只有列表 presenter 会给能力位）
            CanAddTop = node.UnionListLike && CanAppendToList(node),
            CanAddBottom = node.UnionListLike && CanAppendToList(node),
                EditorKind = MirroredEditorKind(node),
                // 颜色/候选/下拉都是**选中分支**的属性：结构/列表分支上必须收起，
                // 否则会出现"切到元组支还显示色板"这种串味（分支切换后镜像状态没清干净）。
                IsSelect = node.ScalarEditorKind is not null && node.UseSelect,
                AllowCustomValue = node.ScalarEditorKind is not null && node.AllowCustomValue,
                IsIconSelect = node.ScalarEditorKind is not null && node.MirrorIconSelect,
                Options = node.ScalarEditorKind is not null ? node.EnumOptionList : null,
                BranchOptions = branches,
                IsRawKey = IsRawKeyNode(node),
                CanUnset = canUnset,
                Diagnostics = DiagnosticsFor(node),
            };
        }

        public override IReadOnlyList<McdocTreeNode> Children(McdocTreeNode node)
            => node.SelectedUnionChildren.Count > 0 ? node.SelectedUnionChildren
                : node.Items.Count > 0 ? node.Items
                : node.Children;

        public override void PopulateChildren(McdocTreeNode node, McdocType type, McdocJsonValue json, McdocBuildContext context)
        {
            // union 不塌陷：保留它（UI 上要能切换分支），用 JSON 节点挑默认分支
            var members = type.Members ?? [];
            if (members.Count == 0)
            {
                return;
            }
            // JSON 里没有这个键（新建条目/缺省）→ 不自动挑分支，落到 unset（misode 的 selectUnionMember 返回 -1）
            // JSON 里已经有值 → 按值匹配；匹配不上时退回首个成员（保持既有文档的渲染稳定）
            // 用户显式切过的分支优先：只要它对当前值仍然成立就别顶掉。
            // 否则并列（两个成员都能吃这个值）时"按值猜"会退回首个成员，表现是切完一重建就弹回去。
            var explicitMember = node.UnionSelectionExplicit ? node.SelectedUnionMember : null;
            if (explicitMember is null
                && context.Runtime?.UnionSelectionHints is { } hints
                && hints.TryGetValue(context.JsonPath ?? "", out var hinted)
                && hinted >= 0 && hinted < members.Count)
            {
                // 局部重建：节点是新建的，标志还在老节点上；提示由文档随本次构建传进来
                explicitMember = members[hinted];
            }
            var selected = explicitMember is not null
                && (json is null || McdocTypeMatcher.Matches(explicitMember, json, context.SimplifyContext))
                ? explicitMember
                : json is null
                    ? null
                    // 显示层挑默认分支：Any 成员不许压掉真正对得上的结构支（见 SelectMember 的 preferShapeOverAny）
                    : McdocTypeMatcher.SelectMember(type, json, context.SimplifyContext, preferShapeOverAny: true)
                        ?? members[0];
            // 解不出来的成员（声明不是 any、求值却是 any）不当默认分支：
            // 否则文档里一个它解不了的值会把整行钉死在一个"看着能编辑、其实写不出合法值"的分支上。
            if (selected is not null && McdocUnionSwitch.IsUnresolvedMember(node, selected))
            {
                node.Error = "该值的类型在当前上下文解不出来（分支回到未设置）";
                selected = null;
            }
            node.SelectedUnionMember = selected;
            // 这里**不许**按 IsRequired 挂"必选却未选分支"：建子级时子节点的必选性还没被父级写进来
            // （默认 true），可选字段（chat_type 的 color）会被扣一顶假诊断且永远清不掉。
            // 真正缺必填键由 McdocValidator 报（"缺少必填键"），它在全树建完之后才跑。
            // misode formatUnionMember：名字只取决于"自身 + 是否存在同类成员"
            // 重建从零开始：分支节点与上提的字段行都属于"当前选中的成员"，
            // 不先清掉就会在旧状态上叠加（现场：切到 block_state_property 后 terms 与 block 同时存在）。
            node.Children.Clear();
            node.SelectedUnionChildren.Clear();
            var memberNames = members
                .Select((member, index) => FormatUnionMember(member, members.Where((_, other) => other != index).ToList()))
                .ToList();
            // 不加数字序号：实在分不开就允许重名（misode 也这样），切换靠 EnumMember.Value 里的下标
            node.UnionTypeNameList = new ObservableCollection<EnumMember>();
            for (var i = 0; i < members.Count; i++)
            {
                node.UnionTypeNameList.Add(new EnumMember
                {
                    Name = memberNames[i],
                    Value = new MetaValue { LiteralValue = i }
                });
            }
            node.UnionBranchBuilder = (member, branchJson, previousMember) =>
            {
                // 分支子树一律建在**文档里那份值**上（键 ↔ 行双射）：值能落进新成员就原样用，
                // 落不进才做一次形状转换。以前无条件走 GetChange，而且没带 runtime：
                //   · `argument: T` 这类泛型字段在缺省代数里取不到默认值 → 整支只剩 {}；
                //   · QuickEqualTypes 把两个指令当"同型" → {k:X} 搬运把整个旧值塞进第一个字段。
                // 现场（1.21.11 dimension_type 的 audio/ambient_sounds 切 Modifier 支）：modifier 行拿到
                // 整个父对象（"值与类型不符：期望 Literal"），argument 行没有值（"缺少必填键"）。
                var value = branchJson ?? node.Json;
                if (value is not null && !McdocUnionSwitch.ValueFitsMember(member, value, context.SimplifyContext))
                {
                    // 旧类型取"切走的那个成员"：拿 union 声明来比，四条搬运规则（X↔[X]、{k:X}↔X）全都不成立
                    value = McdocHelpers.GetChange(member, previousMember ?? node.DeclaredType ?? type, value,
                        McdocDefaults.Options(context.Runtime));
                }
                var branch = context.Child(node.FieldName, member, value, context.KeyNode, context.Depth + 1, context.JsonPath);
                // 分支是枚举/候选下拉时，第 0 项也要有（未设置）：否则镜像到 union 行上的下拉
                // 只能显示"第一个有意义的成员"，用户没法把它退回去
                McdocSelectOptions.EnsureUnsetOption(branch, true);
                return branch;
            };
            var selectedIndex = members.IndexOf(selected);
            // 没匹配到就落到 unset 哨兵（不是 members[0]）：新建的 criterion.trigger 应该显示"（未设置）"
            // 建树期必须走静默落项：setter 会触发 RebuildUnionBranch → UnionBranchBuilder → GetChange，
            // 那条路把文档值换成"新成员的默认值"，分支子树于是在没有 JSON 的默认值上建起来，
            // 之后所有"靠同级键的动态索引"（如 EntitySubPredicate 的 ...dispatch[[type]]）都取不到键。
            node.SetUnionTypeSilently(selectedIndex >= 0
                ? node.UnionTypeNameList[selectedIndex]
                : new EnumMember { Name = UnsetLabel, Value = new MetaValue { LiteralValue = -1 } });
            // 分支子树一律用文档里的那份 JSON 建（局部重建会先清空分支子级，这里同时负责补建）。
            if (selected is not null && node.SelectedUnionChildren.Count == 0)
            {
                node.ApplyUnionBranch(context.Child(node.FieldName, selected, json,
                    context.KeyNode, context.Depth + 1, context.JsonPath));
            }
        }

        /// <summary>撤销分支：分支子级与镜像状态一并清掉，行回到"只是 union 下拉"的状态。</summary>
        public override void ClearBranch(McdocTreeNode node)
        {
            if (node is null) return;
            node.SelectedUnionChildren.Clear();
            node.ScalarEditorKind = null;
            node.UnionListLike = false;
            node.MirrorIconSelect = false;
            node.UseSelect = false;
            node.AllowCustomValue = false;
            node.EnumOptionList = null;
            node.SelectedEnumOption = null;
        }

        /// <summary>切换分支后的行状态：标量分支镜像到本行，结构分支摊平成字段行，列表/元组各占一行。</summary>
        public override void ApplyBranch(McdocTreeNode node, McdocTreeNode branchNode)
        {
            if (branchNode is null) return;
            // 分支节点自己也要挂到 union 上：列表/结构分支的子级会上提到 union（子级 Parent=node），
            // 但分支节点本体没被 Add，Parent 一直是 null —— 条目按钮上的"删这一项"捕获的是分支节点，
            // 它抬 StructureChanged 时宿主顺着 Parent 找不到能重建的祖先，于是"右侧 JSON 少了、左侧树不动"。
            branchNode.Parent = node;
            node.SelectedUnionChildren.Clear();
            // 结构/列表分支的字段行是**上提到 union 节点**上的：不清掉的话，切支后新旧两支的字段会合并
            // （现场：condition 从 any_of 切到 block_state_property 后，$..u 下面同时留着 terms 和 block）
            node.Children.Clear();

            // 标量分支：把编辑状态镜像到 union 行上，UI 上就是"标签 + 分支下拉 + 编辑器"一行
            var scalarKind = branchNode.Type?.Kind switch
            {
                McdocTypeKind.String or McdocTypeKind.Reference => branchNode.UseSelect ? "Enum" : "Text",
                McdocTypeKind.Enum => "Enum",
                McdocTypeKind.Boolean => "Bool",
                McdocTypeKind.Int or McdocTypeKind.Float or McdocTypeKind.Double
                    or McdocTypeKind.Long or McdocTypeKind.Byte or McdocTypeKind.Short => "Number",
                _ => null,
            };
            if (scalarKind is not null)
            {
                // 镜像只是"让行上显示分支的当前状态"，不是用户编辑：整段静默，
                // 否则选中 value 支会把 true/false 写进文档，把对象覆盖成标量。
                var wasSuppressed = node.SuppressValueNotifications;
                node.SuppressValueNotifications = true;
                try
                {
                    node.ScalarEditorKind = scalarKind;
                    node.UseSelect = branchNode.UseSelect;
                    node.AllowCustomValue = branchNode.AllowCustomValue;
                    // 图标位也来自分支（misode：union 行的 StringHead 分支同样有 ItemIdPreview）
                    node.MirrorIconSelect = IsIconSelectFor(branchNode);
                    node.EnumOptionList = branchNode.EnumOptionList;
                    node.SelectedEnumOption = branchNode.SelectedEnumOption;
                    node.Value = branchNode.Value;
                    node.IsTrue = branchNode.IsTrue;
                    node.IsFalse = branchNode.IsFalse;
                    node.Json ??= branchNode.Json;
                }
                finally
                {
                    node.SuppressValueNotifications = wasSuppressed;
                }
                return;
            }

            node.ScalarEditorKind = null;
            // 上一支是标量时留下的镜像状态必须清掉：颜色位/候选/可输入标记只属于那一支
            node.MirrorIconSelect = false;
            node.UseSelect = false;
            node.AllowCustomValue = false;
            node.EnumOptionList = null;
            node.SelectedEnumOption = null;
            if (branchNode.Type?.Kind == McdocTypeKind.Struct)
            {
                // 结构分支与 union 共用同一个 JSON 对象：分支层自己往往是"没有 JSON"的中间层，
                // 补上以后它的必选字段（frame=Task 这类）才有落点可写
                branchNode.Json ??= node.Json;
                // 懒建：分支节点的子级可能还没建，直接看 Children.Count 会把结构分支误判成"没有字段"，
                // 于是落进下面那条"整支当子行"的分支 —— 表现就是 player → player 这种同名空壳
                branchNode.EnsureChildren();
                if (branchNode.Children.Count > 0)
                {
                    foreach (var child in branchNode.Children)
                    {
                        child.Parent = node;
                        node.SelectedUnionChildren.Add(child);
                    }
                    return;
                }
                // 结构分支确实一个字段都没有：不留空壳
                return;
            }
            // 列表/元组分支：把分支自己的子级直接上提到 union 行（少一层容器行），
            // 列表动作（⊕ / 分页 / 尾部追加）由 union 行自己出
            if (branchNode.Type?.Kind is McdocTypeKind.List or McdocTypeKind.ByteArray
                or McdocTypeKind.IntArray or McdocTypeKind.LongArray or McdocTypeKind.Tuple)
            {
                branchNode.Json ??= node.Json;
                branchNode.EnsureChildren();
                var items = branchNode.Items.Count > 0 ? branchNode.Items : branchNode.Children;
                foreach (var child in items)
                {
                    child.Parent = node;
                    node.SelectedUnionChildren.Add(child);
                }
                node.UnionListLike = true;
                return;
            }

            // any 分支：仍作为子行（JSON 落点还在父对象上，FieldName 不能改），
            // 但标签要带分支形态，否则和父行同名，看起来像多了一层空壳
            branchNode.UnionBranchKind = branchNode.Type?.Kind switch
            {
                McdocTypeKind.List or McdocTypeKind.ByteArray or McdocTypeKind.IntArray or McdocTypeKind.LongArray => "列表",
                McdocTypeKind.Tuple => "元组",
                // any 分支本来就没有结构，落成一行原始 JSON 编辑器（不是空壳）
                McdocTypeKind.Any or McdocTypeKind.Unsafe => "原始 JSON",
                _ => "分支",
            };
            if (string.IsNullOrEmpty(branchNode.FieldName))
            {
                branchNode.FieldName = branchNode.Type?.Kind.ToString() ?? "";
            }
            branchNode.Parent = node;
            node.SelectedUnionChildren.Add(branchNode);
        }

        /// <summary>
        /// misode 的 formatUnionMember：misode_member_name → 字面量 → "没有同类成员"用 kind 名 →
        /// struct 取第一个字面量键 → kind 名。注意 misode 不追加序号，也不看判别字段。
        /// </summary>
        /// <summary>列表分支还能不能再加一条（定长 tuple / #[length] 上限时不给 ⊕）。</summary>
        private static bool CanAppendToList(McdocTreeNode node)
        {
            var listType = node?.SelectedUnionMember;
            if (listType is null) return false;
            if (listType.Kind == McdocTypeKind.Tuple) return false;
            var max = listType.LengthRange?.Max;
            var count = node.Json is { Kind: McdocJsonKind.Array } array ? (array.Items?.Count ?? 0) : 0;
            return !max.HasValue || count < max.Value;
        }

        private static string FormatUnionMember(McdocType member, IReadOnlyList<McdocType> others)
        {
            var explicitName = member?.Attributes?.FirstOrDefault(item => item?.Name == "misode_member_name")?.Value?.AsString();
            if (!string.IsNullOrEmpty(explicitName)) return explicitName;

            if (member is null) return "any";
            if (member.Kind == McdocTypeKind.Literal) return McdocHelpers.FormatIdentifier(member.LiteralText());

            var kindName = KindName(member.Kind);
            if (!others.Any(other => other is not null && other.Kind == member.Kind))
            {
                return McdocHelpers.FormatIdentifier(kindName);
            }
            if (member.Kind == McdocTypeKind.Struct)
            {
                var firstKey = (member.Fields ?? [])
                    .FirstOrDefault(field => field?.Key is { Kind: McdocTypeKind.Literal });
                if (firstKey?.Key is { } key) return McdocHelpers.FormatIdentifier(key.LiteralText());
            }
            return McdocHelpers.FormatIdentifier(kindName);
        }

        /// <summary>spyglass 简化类型名（struct 在 UI 上叫 object）。</summary>
        private static string KindName(McdocTypeKind kind) => kind switch
        {
            McdocTypeKind.Struct => "object",
            McdocTypeKind.List or McdocTypeKind.ByteArray or McdocTypeKind.IntArray or McdocTypeKind.LongArray => "list",
            McdocTypeKind.Reference => "reference",
            _ => kind.ToString().ToLowerInvariant(),
        };

    }

    /// <summary>列表行：元素子行在 Items 里。</summary>
    public sealed class ListNodePresenter : McdocNodePresenter
    {
        public override bool CanPresent(McdocType type) => type?.Kind == McdocTypeKind.List;

        public override McdocNodeHead Describe(McdocTreeNode node)
        {
            var count = node.Json?.Items?.Count ?? 0;
            var max = node.Type?.LengthRange?.Max ?? decimal.MaxValue;
            return new McdocNodeHead
            {
                Label = node.FieldName,
                Documentation = node.Desc,
                RowKind = RowKindFor(node),
                Diagnostics = DiagnosticsFor(node),
                IsRawKey = IsRawKeyNode(node),
                CanAddTop = count < max,
                CanAddBottom = count < max,
            };
        }

        public override bool IsListLike => true;

        public override IReadOnlyList<McdocTreeNode> Children(McdocTreeNode node)
            => node.Items.Count > 0 ? node.Items : node.Children;

        public override void PopulateChildren(McdocTreeNode node, McdocType type, McdocJsonValue json, McdocBuildContext context)
        {
            var itemType = McdocHelpers.GetItemType(type);
            var items = json?.Items;
            if (items == null) return;
            for (var i = 0; i < items.Count; i++)
            {
                var itemKeyNode = new McdocRuntimeNode(i, new McdocType
                {
                    Kind = McdocTypeKind.Literal,
                    LiteralKind = McdocLiteralKind.Int,
                    Value = i
                });
                // 列表项用下标做标签（misode 一样），别再用 "entry" 这种假名字
                var item = context.Child(i.ToString(), itemType, items[i], itemKeyNode,
                    context.Depth + 1, context.JsonPath + "[" + i + "]");
                item.Parent = node;
                node.Items.Add(item);
                // 命令在 Add 之后挂：它的 CanExecute 要问 Items.IndexOf(item)
                context.Editor.AttachListItemCommands(node, item);
            }
        }
    }

    /// <summary>元组 / 原始数组行：定长元素，同样走 Items。</summary>
    public sealed class TupleNodePresenter : McdocNodePresenter
    {
        public override bool CanPresent(McdocType type)
            => type?.Kind is McdocTypeKind.Tuple or McdocTypeKind.ByteArray or McdocTypeKind.IntArray or McdocTypeKind.LongArray;

        public override McdocNodeHead Describe(McdocTreeNode node) => new()
        {
            Label = node.FieldName,
            Documentation = node.Desc,
            RowKind = RowKindFor(node),
            Diagnostics = DiagnosticsFor(node),
            IsRawKey = IsRawKeyNode(node),
            IsInlineTuple = McdocHelpers.IsInlineTuple(node.Type),
            EditorKind = McdocHelpers.IsInlineTuple(node.Type) ? McdocEditorKind.InlineTuple : McdocEditorKind.None,
            // 定长结构：不提供增删/默认值（显式置 false，别依赖 init 默认值——曾经把 tuple 当列表给了 ⊕）
            CanAddTop = false,
            CanAddBottom = false,
            CanSetDefault = false,
        };

        /// <summary>
        /// tuple 的子行是"按下标编号的项"（行标签用下标，不是字段名），投影层靠这个标记决定标签与分页，
        /// 所以这里必须保持 true；"定长、不能增删"由 CanAdd*/CanRemove=false 表达。
        /// </summary>
        public override bool IsListLike => true;

        /// <summary>misode 的非 inline tuple 子行标签是 entry，用下标会和列表混在一起。</summary>
        public override string ChildLabel(McdocTreeNode parent, int index) => index.ToString();

        public override IReadOnlyList<McdocTreeNode> Children(McdocTreeNode node)
            // inline tuple 的项节点只供行内编辑器使用，不作为子行投影
            => McdocHelpers.IsInlineTuple(node.Type) ? Array.Empty<McdocTreeNode>()
                : node.Items.Count > 0 ? node.Items : node.Children;

        public override bool CanHaveChildren(McdocTreeNode node)
            => !McdocHelpers.IsInlineTuple(node.Type) && (node.Type?.Items?.Count ?? 0) > 0;

        public override void PopulateChildren(McdocTreeNode node, McdocType type, McdocJsonValue json, McdocBuildContext context)
        {
            // 与投影同一判据：可选且文档里没有这个键的元组只留一行 ⊕（否则元素行一个都画不出来）。
            // 必填 tuple 缺值仍要摊开（非法文档要看得到元素行）。
            if (json is null && !node.IsRequired && node.Parent is not null) return;
            var tupleItems = type.Items ?? [];
            var jsonItems = json?.Items;
            for (var i = 0; i < tupleItems.Count; i++)
            {
                var itemJson = jsonItems != null && i < jsonItems.Count ? jsonItems[i] : null;
                var item = context.Child(i.ToString(), tupleItems[i], itemJson, null,
                    context.Depth + 1, context.JsonPath + "[" + i + "]");
                var index = i;
                // 定长：行上的删除按钮同步不可用（只靠编辑器守卫，用户点了会"没反应"）
                item.RemoveSelfCommand = new McdocCommand(() => context.Editor.RemoveItemAt(node, index),
                    () => node.Type?.Kind != McdocTypeKind.Tuple);
                node.Items.Add(item);
            }
        }
    }
}
