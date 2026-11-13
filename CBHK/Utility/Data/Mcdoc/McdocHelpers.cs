// 与 UI 无关的 mcdoc 纯逻辑 —— 移植自 misode.github.io 的
// src/app/components/generator/McdocHelpers.ts（getRootType/getDefault/getChange/谓词/格式化）。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>默认值生成的可变外部输入（版本 pack_format 与随机数）。</summary>
    public sealed class McdocDefaultOptions
    {
        public int PackFormat;
        public Func<long> RandomSeed = () => 0;
        public Func<int> RandomInt = () => 0;

        /// <summary>
        /// 字段/元素类型的求值回调：misode 的 getDefault 在 struct 字段、list 项、tuple 项上都是
        /// "先 simplifyType 再取值"（McdocHelpers.ts:103/125/144）。为 null 时按**声明类型**取值——
        /// 那样 union 会取到已被 #[until] 过滤掉的成员，写出来的就是 null（predicate.condition 的 old 行为）。
        /// </summary>
        public Func<McdocType, McdocType> Simplify;
    }

    public static class McdocHelpers
    {
        /// <summary>
        /// 数据包格式（#[pack_format] 的默认值）。misode 是远端拉 data_pack_version，这里只维护
        /// 有把握的版本；未收录的版本回退到"不高于它的最近一条"，都没有时用 48（1.21）。
        /// </summary>
        private static readonly (string Version, int PackFormat)[] KnownPackFormats =
        [
            ("1.20", 15), ("1.20.1", 15), ("1.20.2", 18), ("1.20.3", 26), ("1.20.4", 26),
            ("1.20.5", 41), ("1.20.6", 41), ("1.21", 48), ("1.21.1", 48), ("1.21.2", 57),
            ("1.21.3", 57), ("1.21.4", 61), ("1.21.5", 71),
        ];

        /// <summary>版本 → 数据包格式；未知版本按最近的下界回退，再不行用 48。</summary>
        /// <summary>misode ANY_TYPES：AnyHead 类型下拉的五个候选。</summary>
        public static McdocType AnyTypeOf(string name) => name switch
        {
            "boolean" => McdocType.Create(McdocTypeKind.Boolean),
            "double" => McdocType.Create(McdocTypeKind.Double),
            "string" => McdocType.Create(McdocTypeKind.String),
            "list" => new McdocType { Kind = McdocTypeKind.List, Item = McdocType.Any() },
            "struct" => StructWithAnyField(),
            _ => null,
        };

        private static McdocType StructWithAnyField()
        {
            var type = McdocType.Create(McdocTypeKind.Struct);
            type.Fields =
            [
                new McdocStructField { Key = McdocType.Create(McdocTypeKind.String), Type = McdocType.Any() },
            ];
            return type;
        }

        public static int ResolvePackFormat(string gameVersion)
        {
            if (string.IsNullOrWhiteSpace(gameVersion)) return 48;
            var exact = KnownPackFormats.FirstOrDefault(entry => entry.Version == gameVersion.Trim());
            if (exact.Version is not null) return exact.PackFormat;
            var lower = KnownPackFormats
                .Where(entry => McdocAttributeRegistry.CompareVersions(gameVersion, entry.Version) >= 0)
                .OrderByDescending(entry => entry.Version, Comparer<string>.Create(McdocAttributeRegistry.CompareVersions))
                .FirstOrDefault();
            return lower.Version is null ? 48 : lower.PackFormat;
        }

        /// <summary>生成器 id → 根类型。对应 misode 的 getRootType（三分支）。</summary>
        public static McdocType GetRootType(string id)
        {
            if (id == "pack_mcmeta")
            {
                return new McdocType { Kind = McdocTypeKind.Reference, Path = "::java::pack::Pack" };
            }
            if (id == "text_component")
            {
                return new McdocType { Kind = McdocTypeKind.Reference, Path = "::java::util::text::Text" };
            }
            if (id != null && id.StartsWith("tag/", StringComparison.Ordinal))
            {
                var tagType = McdocType.Create(McdocTypeKind.String);
                tagType.Attributes =
                [
                    new McdocAttribute
                    {
                        Name = "id",
                        Value = new McdocAttrValue
                        {
                            Tree = new Dictionary<string, McdocAttrValue>
                            {
                                ["registry"] = McdocAttrValue.Of(id.Substring(4)),
                                ["tags"] = McdocAttrValue.Of("allowed")
                            }
                        }
                    }
                ];
                return new McdocType
                {
                    Kind = McdocTypeKind.Concrete,
                    Item = new McdocType { Kind = McdocTypeKind.Reference, Path = "::java::data::tag::Tag" },
                    Items = [tagType]
                };
            }
            return new McdocType
            {
                Kind = McdocTypeKind.Dispatcher,
                Registry = "minecraft:resource",
                Indices = [McdocIndex.Static(id ?? "")]
            };
        }

        /// <summary>把字段/元素类型先求值一次（options.Simplify 未接时原样返回）。</summary>
        private static McdocType Evaluate(McdocType type, McdocDefaultOptions options)
            => type is null || options?.Simplify is null ? type : options.Simplify(type) ?? type;

        /// <summary>
        /// 给默认值生成接上"字段类型也求值"的回调。用一个与建树同源的上下文（同 runtime、无 JSON 节点）——
        /// misode 那边用的也是 getCheckerContext() 这份无节点的上下文。
        /// </summary>
        public static McdocDefaultOptions BindSimplify(McdocDefaultOptions options, McdocRuntime runtime, McdocValueNode node = null)
        {
            options ??= new McdocDefaultOptions();
            var context = new McdocSimplifyContext
            {
                Runtime = runtime,
                Node = node ?? McdocValueNode.Of(new McdocRuntimeNode(null, McdocType.Any())),
            };
            options.Simplify = type => type is null ? null : McdocSimplifier.Simplify(type, context).TypeDef;
            return options;
        }

        /// <summary>按类型造一个默认 JSON 值。对应 misode 的 getDefault。</summary>
        /// <summary>
        /// 这个 JSON 值在新声明下还成不成立（切成员/切依赖键时决定"旧值能不能带过去"）。
        /// 只做形态与取值域的判定，不猜语义：枚举必须命中候选、字面量必须相等、
        /// 数字/布尔/字符串/对象/数组要求种类一致；any / 未知类型一律放过（判不了就不判）。
        /// </summary>
        /// <summary>
        /// 文档根的**形状**是否与声明一致：结构要对象、列表/元组/原始数组要数组、any/解不开不判。
        /// 用途：拦住"入口模板本身就是另一种资源"这类坏数据（现场：loot_table 的模板是一份函数数组），
        /// 否则整页会退化成 any 原始行，看着像求解/渲染坏了。
        /// </summary>
        public static bool RootShapeMatches(McdocJsonValue root, McdocType declared, out string mismatch)
        {
            mismatch = "";
            if (root is null || declared is null) return true;
            if (declared.Kind is McdocTypeKind.Any or McdocTypeKind.Unsafe) return true;
            if (declared.Kind is not (McdocTypeKind.Struct or McdocTypeKind.List or McdocTypeKind.Tuple
                or McdocTypeKind.ByteArray or McdocTypeKind.IntArray or McdocTypeKind.LongArray)) return true;
            var wantsArray = declared.Kind is McdocTypeKind.List or McdocTypeKind.Tuple
                or McdocTypeKind.ByteArray or McdocTypeKind.IntArray or McdocTypeKind.LongArray;
            var expected = wantsArray ? McdocJsonKind.Array : McdocJsonKind.Object;
            if (root.Kind == expected) return true;
            mismatch = "期望 " + declared.Kind + "，实际 " + root.Kind;
            return false;
        }

        public static bool ValueFits(McdocJsonValue value, McdocType type)
        {
            if (value is null) return true;
            if (type is null || type.Kind is McdocTypeKind.Any or McdocTypeKind.Unsafe) return true;
            switch (type.Kind)
            {
                case McdocTypeKind.Enum:
                    if (value.Kind != McdocJsonKind.String) return type.EnumKind != McdocTypeKind.String;
                    var text = value.StringValue ?? "";
                    return (type.EnumValues ?? []).Any(entry => string.Equals(entry.Identifier, text, StringComparison.Ordinal)
                        || string.Equals(Convert.ToString(entry.Value), text, StringComparison.Ordinal));
                case McdocTypeKind.Literal:
                    return type.LiteralKind switch
                    {
                        McdocLiteralKind.String => value.Kind == McdocJsonKind.String
                            && string.Equals(value.StringValue, type.LiteralText(), StringComparison.Ordinal),
                        McdocLiteralKind.Boolean => value.Kind == McdocJsonKind.Boolean && Equals(value.BooleanValue, type.Value),
                        _ => value.Kind == McdocJsonKind.Number,
                    };
                case McdocTypeKind.Boolean:
                    return value.Kind == McdocJsonKind.Boolean;
                case McdocTypeKind.Byte:
                case McdocTypeKind.Short:
                case McdocTypeKind.Int:
                case McdocTypeKind.Long:
                case McdocTypeKind.Float:
                case McdocTypeKind.Double:
                    return value.Kind == McdocJsonKind.Number;
                case McdocTypeKind.String:
                    return value.Kind == McdocJsonKind.String;
                case McdocTypeKind.List:
                case McdocTypeKind.ByteArray:
                case McdocTypeKind.IntArray:
                case McdocTypeKind.LongArray:
                case McdocTypeKind.Tuple:
                    return value.Kind == McdocJsonKind.Array;
                case McdocTypeKind.Struct:
                    return value.Kind == McdocJsonKind.Object;
                default:
                    return true;
            }
        }

        public static McdocJsonValue GetDefault(McdocType type, McdocDefaultOptions options = null)
        {
            options ??= new McdocDefaultOptions();
            if (type == null) return McdocJsonValue.Null();

            switch (type.Kind)
            {
                case McdocTypeKind.String:
                    return McdocJsonValue.String("");

                case McdocTypeKind.Boolean:
                    return McdocJsonValue.Boolean(false);

                case McdocTypeKind.Byte:
                case McdocTypeKind.Short:
                case McdocTypeKind.Int:
                case McdocTypeKind.Long:
                case McdocTypeKind.Float:
                case McdocTypeKind.Double:
                {
                    decimal number = 0m;
                    if (type.Range != null)
                    {
                        // 最佳尝试：优先 0，其次 1，最后取左边界
                        if (type.Range.Contains(0m)) number = 0m;
                        else if (type.Range.Contains(1m)) number = 1m;
                        else if (type.Range.Min.HasValue && type.Range.Min.Value > 0)
                        {
                            number = type.Range.Min.Value;
                            if (type.Range.MinExclusive) number += 1m;
                        }
                    }
                    if (HasAttribute(type, "pack_format")) number = options.PackFormat;
                    if (HasAttribute(type, "random")) number = type.Kind == McdocTypeKind.Long ? options.RandomSeed() : options.RandomInt();
                    var isInteger = number == decimal.Truncate(number);
                    return McdocJsonValue.Number(number, isInteger);
                }

                case McdocTypeKind.Struct:
                case McdocTypeKind.Any:
                case McdocTypeKind.Unsafe:
                {
                    var result = McdocJsonValue.Object();
                    if (type.Kind == McdocTypeKind.Struct)
                    {
                        foreach (var field in type.Fields ?? [])
                        {
                            if (field == null || field.Optional || field.IsSpread || field.Name == null) continue;
                            var fieldDefault = GetDefault(Evaluate(field.Type, options), options);
                            // 解不出可物化的默认值（字面量以外的东西、空枚举、未解开的指令…）时**不许写 null**：
                            // null 不是合法值，会把必填键钉成"值与类型不符"（现场：attribute modifier 的
                            // `modifier: null`（期望 Literal）、`argument: null`（期望 Struct））。
                            // 宁可缺这个键，让校验报"缺少必填键"—— 那是真话，用户也知道该填什么。
                            if (fieldDefault is null || fieldDefault.Kind == McdocJsonKind.Null) continue;
                            result.Add(field.Name, fieldDefault);
                        }
                    }
                    return result;
                }

                case McdocTypeKind.List:
                case McdocTypeKind.ByteArray:
                case McdocTypeKind.IntArray:
                case McdocTypeKind.LongArray:
                {
                    var result = McdocJsonValue.Array();
                    var minLength = type.LengthRange?.Min ?? 0m;
                    var itemType = Evaluate(GetItemType(type), options);
                    for (var i = 0; i < (int)minLength; i++) result.Add(GetDefault(itemType, options));
                    return result;
                }

                case McdocTypeKind.Tuple:
                {
                    var result = McdocJsonValue.Array();
                    foreach (var item in type.Items ?? []) result.Add(GetDefault(Evaluate(item, options), options));
                    return result;
                }

                case McdocTypeKind.Union:
                {
                    var members = type.Members ?? [];
                    return members.Count == 0 ? McdocJsonValue.Null() : GetDefault(members[0], options);
                }

                case McdocTypeKind.Enum:
                {
                    var values = type.EnumValues ?? [];
                    if (values.Count == 0) return McdocJsonValue.Null();
                    var kind = type.EnumKind ?? McdocTypeKind.String;
                    return kind == McdocTypeKind.String
                        ? McdocJsonValue.String(Convert.ToString(values[0].Value))
                        : McdocJsonValue.Number(ToDecimal(values[0].Value), true);
                }

                case McdocTypeKind.Literal:
                {
                    switch (type.LiteralKind)
                    {
                        case McdocLiteralKind.String:
                            return McdocJsonValue.String(type.LiteralText());
                        case McdocLiteralKind.Boolean:
                            return McdocJsonValue.Boolean(type.Value is bool b && b);
                        default:
                            return McdocJsonValue.Number(ToDecimal(type.Value), true);
                    }
                }

                default:
                    return McdocJsonValue.Null();
            }
        }

        /// <summary>
        /// 切换类型时尽量保留旧值。对应 misode 的 getChange：
        /// X→[X]、[X]→X、X→{k:X}、{k:X}→X 四种可搬运形态。
        /// </summary>
        public static McdocJsonValue GetChange(McdocType newType, McdocType oldType, McdocJsonValue oldValue, McdocDefaultOptions options = null)
        {
            options ??= new McdocDefaultOptions();
            var node = GetDefault(newType, options);
            if (node.Kind == McdocJsonKind.Array && IsListOrArray(newType))
            {
                var newItemType = GetItemType(newType);
                List<McdocType> possibleTypes = newItemType.Kind == McdocTypeKind.Union
                    ? newItemType.Members ?? new List<McdocType>()
                    : new List<McdocType> { newItemType };
                foreach (var possible in possibleTypes)
                {
                    if (!QuickEqualTypes(oldType, possible)) continue;
                    node.Items = new List<McdocJsonValue> { oldValue };
                    return node;
                }
            }
            if (oldValue != null && oldValue.Kind == McdocJsonKind.Array && IsListOrArray(oldType))
            {
                var oldItemType = GetItemType(oldType);
                if (oldItemType.Kind != McdocTypeKind.Union && QuickEqualTypes(newType, oldItemType)
                    && oldValue.Items != null && oldValue.Items.Count > 0)
                {
                    return oldValue.Items[0];
                }
            }
            if (node.Kind == McdocJsonKind.Object && newType.Kind == McdocTypeKind.Struct)
            {
                foreach (var field in newType.Fields ?? [])
                {
                    if (field == null || field.Optional || field.Name == null) continue;
                    var fieldType = field.Type;
                    if (fieldType.Kind == McdocTypeKind.Union || !QuickEqualTypes(fieldType, oldType)) continue;
                    node.Properties?.RemoveAll(pair => pair.Key == field.Name);
                    node.Add(field.Name, oldValue);
                    return node;
                }
            }
            if (oldValue != null && oldValue.Kind == McdocJsonKind.Object && oldType.Kind == McdocTypeKind.Struct)
            {
                foreach (var oldField in oldType.Fields ?? [])
                {
                    if (oldField == null || oldField.Name == null) continue;
                    if (oldField.Type.Kind == McdocTypeKind.Union || !QuickEqualTypes(oldField.Type, newType)) continue;
                    var kept = oldValue.Get(oldField.Name);
                    if (kept != null) return kept;
                }
            }
            return node;
        }

        /// <summary>宽松类型相等：同 kind 即视为可搬运；literal 比 value，struct 只比第一个字段的 key。</summary>
        public static bool QuickEqualTypes(McdocType a, McdocType b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            // 指令不是归一形态（Mapped/Template/Reference…）：两个指令之间没有"同型"可言。
            // 按 kind 比会退化成"任何 Mapped 都等于任何 Mapped"，getChange 的 {k:X} 搬运就会把
            // 整个旧值塞进第一个必填字段（现场：Modifier 支的 modifier 行拿到整个父对象）。
            if (a.IsDirective || b.IsDirective) return false;
            if (a.Kind != b.Kind) return false;
            if (a.Kind == McdocTypeKind.Literal && b.Kind == McdocTypeKind.Literal)
            {
                return a.LiteralKind == b.LiteralKind && Equals(a.Value, b.Value);
            }
            if (a.Kind == McdocTypeKind.Struct && b.Kind == McdocTypeKind.Struct)
            {
                var keyA = (a.Fields ?? []).FirstOrDefault()?.Name;
                var keyB = (b.Fields ?? []).FirstOrDefault()?.Name;
                return (keyA == null && keyB == null) || (keyA != null && keyA == keyB);
            }
            return true;
        }

        public static bool IsNumeric(McdocType type) => type != null && type.IsNumeric;

        public static bool IsListOrArray(McdocType type) => type != null && type.IsListLike;

        /// <summary>list / 原始数组 的元素类型。</summary>
        public static McdocType GetItemType(McdocType type)
        {
            switch (type.Kind)
            {
                case McdocTypeKind.List:
                    return type.Item ?? McdocType.Any();
                case McdocTypeKind.ByteArray:
                    return McdocType.Create(McdocTypeKind.Byte);
                case McdocTypeKind.IntArray:
                    return McdocType.Create(McdocTypeKind.Int);
                case McdocTypeKind.LongArray:
                    return McdocType.Create(McdocTypeKind.Long);
                default:
                    return McdocType.Any();
            }
        }

        /// <summary>固定长度数组（上下界相等）。</summary>
        public static bool IsFixedList(McdocType type)
            => type?.LengthRange != null && type.LengthRange.Min.HasValue
            && type.LengthRange.Max.HasValue && type.LengthRange.Min == type.LengthRange.Max;

        /// <summary>≤4 项且全数值的元组 → 值内联显示，不展开子行。</summary>
        public static bool IsInlineTuple(McdocType type)
            => type?.Items != null && type.Items.Count <= 4 && type.Items.All(item => item != null && item.IsNumeric);

        /// <summary>
        /// 这一行能不能真的内联：类型够小、全是数值之外，文档里那份值也正好是那么多项。
        /// 内联编辑器画的就是项节点，缺值或长度不符时一个框都画不出来，而内联又按设计不产子行 ——
        /// 于是行上没有编辑器、没有展开箭头、也没有 ⊕（必填的 pos 定长三元组就是这行死节点）。
        /// 这种值必须退回普通元组的元素行。
        /// </summary>
        public static bool IsInlineTuple(McdocTreeNode node)
            => node?.Type is { } type && IsInlineTuple(type)
               && node.Json is { Kind: McdocJsonKind.Array } array
               && (array.Items?.Count ?? 0) == (type.Items?.Count ?? 0);

        /// <summary>把固定长度 list 视为 tuple（每个下标独立成节点）。</summary>
        public static McdocType AsFixedTuple(McdocType type)
        {
            if (!IsFixedList(type)) return null;
            var count = (int)(type.LengthRange.Min ?? 0m);
            var itemType = GetItemType(type);
            var items = new List<McdocType>(count);
            for (var i = 0; i < count; i++) items.Add(itemType);
            // 长度区间要跟着走：丢了它，tuple 上就查不到原来的 #[length] 约束
            return new McdocType { Kind = McdocTypeKind.Tuple, Items = items, Attributes = type.Attributes, LengthRange = type.LengthRange };
        }

        /// <summary>id → 显示文本（去命名空间、下划线转空格、驼峰拆词、首字母大写、#[starred] 加星）。</summary>
        public static string FormatIdentifier(string id, IEnumerable<McdocAttribute> attributes = null)
        {
            if (string.IsNullOrEmpty(id)) return "";
            if (id.StartsWith("!", StringComparison.Ordinal))
            {
                return "! " + FormatIdentifier(id.Substring(1), attributes);
            }
            var starred = attributes != null && attributes.Any(a => a != null && a.Name == "starred");
            var text = id.StartsWith("minecraft:", StringComparison.Ordinal) ? id.Substring("minecraft:".Length) : id;
            text = text.Replace('_', ' ');
            var builder = new StringBuilder(text.Length + 8);
            for (var i = 0; i < text.Length; i++)
            {
                var current = text[i];
                if (i + 1 < text.Length && char.IsLower(current) && char.IsUpper(text[i + 1]))
                {
                    builder.Append(current).Append(' ').Append(char.ToLowerInvariant(text[i + 1]));
                    i++;
                }
                else
                {
                    builder.Append(current);
                }
            }
            var joined = builder.ToString();
            if (joined.Length == 0) return starred ? "✨ " : "";
            var result = char.ToUpperInvariant(joined[0]) + joined.Substring(1);
            return (starred ? "✨ " : "") + result;
        }

        /// <summary>节点配色分类（pool / predicate / function），看的是引用路径而非简化后的结构。</summary>
        public static string GetCategory(McdocType type)
        {
            if (type == null || type.Kind != McdocTypeKind.Reference || string.IsNullOrEmpty(type.Path)) return null;
            switch (type.Path)
            {
                case "::java::data::loot::LootPool":
                case "::java::data::worldgen::dimension::Dimension":
                case "::java::data::worldgen::surface_rule::SurfaceRule":
                case "::java::data::worldgen::template_pool::WeightedElement":
                    return "pool";
                case "::java::data::loot::LootCondition":
                case "::java::data::advancement::AdvancementCriterion":
                case "::java::data::worldgen::dimension::biome_source::BiomeSource":
                case "::java::data::worldgen::processor_list::ProcessorRule":
                case "::java::data::worldgen::feature::placement::PlacementModifier":
                    return "predicate";
                case "::java::data::loot::LootFunction":
                case "::java::data::worldgen::density_function::CubicSpline":
                case "::java::data::worldgen::processor_list::Processor":
                case "::java::data::number_provider::FloatNumberProviderRef":
                case "::java::data::number_provider::IntegerNumberProviderRef":
                    return "function";
                default:
                    return null;
            }
        }

        /// <summary>默认折叠的类型（目前只有 SurfaceRule）。</summary>
        public static bool IsDefaultCollapsedType(McdocType type)
            => type != null && type.Kind == McdocTypeKind.Reference
            && type.Path == "::java::data::worldgen::surface_rule::SurfaceRule";

        /// <summary>取某个属性的配置字符串（没有该属性或配置为空时返回 null）。</summary>
        public static string AttributeValue(McdocType type, string name)
        {
            var attribute = type?.Attributes?.FirstOrDefault(item => item != null && item.Name == name);
            return attribute?.Value?.AsString();
        }

        public static bool HasAttribute(McdocType type, string name)
            => type?.Attributes != null && type.Attributes.Any(a => a != null && a.Name == name);

        internal static decimal ToDecimal(object value)
        {
            if (value == null) return 0m;
            if (value is decimal d) return d;
            if (value is bool b) return b ? 1m : 0m;
            return decimal.TryParse(Convert.ToString(value), out var parsed) ? parsed : 0m;
        }
    }
}
