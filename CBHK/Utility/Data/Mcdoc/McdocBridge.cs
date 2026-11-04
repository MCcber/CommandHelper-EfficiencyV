// 语言服务器模型 → mcdoc 类型模型的桥接（含符号索引）。
// 存在的理由：语言服务器目前返回的是重排过的 MetaType 树，客户端新求值器需要原始 mcdoc 类型。
// 有了这一层，今天就能用新求值器，而不必先改协议；后续语言服务器直接产出 mcdoc 类型后可逐步退役。
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using MinecraftLanguageModelLibrary.Data;

namespace CBHK.Utility.Data.Mcdoc
{
    public static class McdocBridge
    {
        /// <summary>MetaType → mcdoc 类型。Generic 近似映射为 Concrete（形参/实参按位置对应）。</summary>
        public static McdocType FromMetaType(MetaType type) => FromMetaType(type, false);

        private static McdocType FromMetaType(MetaType type, bool ignoreTypeParams)
        {
            if (type == null) return McdocType.Any();

            // 带形参的定义（`type IntProvider<T> = (...)`、`dispatch ...<T> to ...`）在 mcdoc 里是 template：
            // 泛型应用（IntProvider<int @ 0..15>）靠它做形参替换。语言服务器只留了形参表，
            // 所以这里补出 Template 外壳，否则 T 永远替换不掉，整个成员会被求值成空。
            if (!ignoreTypeParams && type.MetaTypeParameterNameList is { Count: > 0 } typeParams)
            {
                var inner = FromMetaType(type, true);
                return new McdocType
                {
                    Kind = McdocTypeKind.Template,
                    Item = inner,
                    TypeParams = typeParams.Keys.ToList(),
                    Attributes = inner?.Attributes
                };
            }

            switch (type.Kind)
            {
                case MetaTypeKind.Any:
                case MetaTypeKind.None:
                case MetaTypeKind.Identifier:
                case MetaTypeKind.Definition:
                    return McdocType.Any();

                case MetaTypeKind.Boolean:
                    // 语言服务器把 boolValue 记成 Boolean + LiteralValue：有值就说明是字面量
                    if (UnwrapLiteral(type.LiteralValue) is bool booleanLiteral)
                    {
                        return new McdocType
                        {
                            Kind = McdocTypeKind.Literal,
                            LiteralKind = McdocLiteralKind.Boolean,
                            Value = booleanLiteral,
                            Attributes = FromAttributeList(type.AttributeList, type.BareAttributeList)
                        };
                    }
                    return McdocType.Create(McdocTypeKind.Boolean);
                case MetaTypeKind.Byte:
                case MetaTypeKind.Short:
                case MetaTypeKind.Int:
                case MetaTypeKind.Long:
                case MetaTypeKind.Float:
                case MetaTypeKind.Double:
                {
                    var numeric = McdocType.Create(type.Kind switch
                    {
                        MetaTypeKind.Byte => McdocTypeKind.Byte,
                        MetaTypeKind.Short => McdocTypeKind.Short,
                        MetaTypeKind.Int => McdocTypeKind.Int,
                        MetaTypeKind.Long => McdocTypeKind.Long,
                        MetaTypeKind.Float => McdocTypeKind.Float,
                        _ => McdocTypeKind.Double
                    });
                    numeric.Range = ReadRange(type.AttributeList, "Value");
                    numeric.Attributes = FromAttributeList(type.AttributeList, type.BareAttributeList);
                    return numeric;
                }

                case MetaTypeKind.String:
                {
                    var result = McdocType.Create(McdocTypeKind.String);
                    result.LengthRange = ReadRange(type.AttributeList, "Length") ?? ToRange(type.MinLength, type.MaxLength);
                    result.Attributes = FromAttributeList(type.AttributeList, type.BareAttributeList);
                    return result;
                }

                case MetaTypeKind.Literal:
                {
                    if (type.IsIdentifierReference && UnwrapLiteral(type.LiteralValue) is string referencePath && referencePath.Length > 0)
                    {
                        // 类型位置的裸标识符 = 引用；真正的字面量（带引号的字符串/数字）仍走 Literal
                        return new McdocType
                        {
                            Kind = McdocTypeKind.Reference,
                            Path = referencePath,
                            Attributes = FromAttributeList(type.AttributeList, type.BareAttributeList)
                        };
                    }
                    var result = McdocType.Create(McdocTypeKind.Literal);
                    FillLiteral(result, type.LiteralValue);
                    result.Attributes = FromAttributeList(type.AttributeList, type.BareAttributeList);
                    return result;
                }

                case MetaTypeKind.Enum:
                {
                    var result = McdocType.Create(McdocTypeKind.Enum);
                    result.EnumKind = ToKind(type.EnumUnderlyingType?.Kind) ?? McdocTypeKind.String;
                    result.EnumValues = [];
                    foreach (var member in type.EnumMemberList ?? [])
                    {
                        if (member == null) continue;
                        result.EnumValues.Add(new McdocEnumValue
                        {
                            Identifier = member.Name ?? "",
                            Value = UnwrapLiteral(member.Value?.LiteralValue),
                            Desc = member.Documentation,
                            Attributes = FromAttributeList(member.FeatureMap)
                        });
                    }
                    result.Attributes = FromAttributeList(type.AttributeList, type.BareAttributeList);
                    return result;
                }

                case MetaTypeKind.Struct:
                {
                    var result = McdocType.Create(McdocTypeKind.Struct);
                    result.Fields = [];
                    foreach (var field in type.FieldList ?? [])
                    {
                        if (field == null) continue;
                        var mapped = new McdocStructField
                        {
                            Name = string.IsNullOrEmpty(field.Name) ? null : field.Name,
                            Key = string.IsNullOrEmpty(field.Name) ? null : McdocType.LiteralString(field.Name),
                            Type = FromMetaType(field.Type),
                            Optional = !field.IsRequired,
                            IsSpread = field.IsSpread,
                            Desc = field.DocumentComments,
                            Attributes = FieldAttributes(field)
                        };
                        result.Fields.Add(mapped);
                    }
                    result.Attributes = FromAttributeList(type.AttributeList, type.BareAttributeList);
                    return result;
                }

                case MetaTypeKind.Union:
                {
                    var result = McdocType.Create(McdocTypeKind.Union);
                    result.Members = [];
                    foreach (var member in type.UnionOptionList ?? [])
                    {
                        if (member != null) result.Members.Add(FromMetaType(member));
                    }
                    result.Attributes = FromAttributeList(type.AttributeList, type.BareAttributeList);
                    return result;
                }

                case MetaTypeKind.List:
                {
                    var result = McdocType.Create(McdocTypeKind.List);
                    result.Item = FromMetaType(type.ElementType);
                    result.LengthRange = ReadRange(type.AttributeList, "Length") ?? ToRange(type.MinLength, type.MaxLength);
                    result.Attributes = FromAttributeList(type.AttributeList, type.BareAttributeList);
                    return result;
                }

                case MetaTypeKind.ByteArray:
                case MetaTypeKind.IntArray:
                case MetaTypeKind.LongArray:
                {
                    var kind = type.Kind == MetaTypeKind.ByteArray ? McdocTypeKind.ByteArray
                        : type.Kind == MetaTypeKind.IntArray ? McdocTypeKind.IntArray
                        : McdocTypeKind.LongArray;
                    var result = McdocType.Create(kind);
                    result.LengthRange = ReadRange(type.AttributeList, "Length") ?? ToRange(type.MinLength, type.MaxLength);
                    result.Range = ReadRange(type.AttributeList, "Value") ?? ToRange(type.MemberMinValue, type.MemberMaxValue);
                    result.Attributes = FromAttributeList(type.AttributeList, type.BareAttributeList);
                    return result;
                }

                case MetaTypeKind.Tuple:
                {
                    var result = McdocType.Create(McdocTypeKind.Tuple);
                    result.Items = [];
                    foreach (var item in type.TupleElementTypeList ?? [])
                    {
                        if (item != null) result.Items.Add(FromMetaType(item));
                    }
                    result.Attributes = FromAttributeList(type.AttributeList, type.BareAttributeList);
                    return result;
                }

                case MetaTypeKind.Reference:
                {
                    var result = McdocType.Create(McdocTypeKind.Reference);
                    result.Path = type.ReferencePath;
                    result.Attributes = FromAttributeList(type.AttributeList, type.BareAttributeList);
                    return result;
                }

                case MetaTypeKind.Dispatch:
                {
                    var result = McdocType.Create(McdocTypeKind.Dispatcher);
                    result.Registry = ReadFeatureString(type.AttributeList, "Resource");
                    result.Indices = ReadIndices(type.AttributeList);
                    result.Attributes = FromAttributeList(type.AttributeList, type.BareAttributeList);
                    return result;
                }

                case MetaTypeKind.Indexed:
                {
                    var result = McdocType.Create(McdocTypeKind.Indexed);
                    result.Item = FromMetaType(type.BaseType);
                    result.Indices = [];
                    foreach (var key in type.IndexKeyList ?? [])
                    {
                        result.Indices.Add(IsDynamicAccessor(key) ? ToDynamicIndex(key) : McdocIndex.Static(key));
                    }
                    result.Attributes = MergeAttributes(type.AttributeList, type.BaseType?.AttributeList, type.BareAttributeList, type.BaseType?.BareAttributeList);
                    return result;
                }

                case MetaTypeKind.Generic:
                {
                    // 近似：spyglass 用 template/concrete 两级表达，MetaType 只有一个 Generic。
                    var result = McdocType.Create(McdocTypeKind.Concrete);
                    result.Item = FromMetaType(type.BaseType);
                    result.Items = [];
                    foreach (var argument in type.TypeArgumentList ?? [])
                    {
                        if (argument != null) result.Items.Add(FromMetaType(argument));
                    }
                    result.Attributes = MergeAttributes(type.AttributeList, type.BaseType?.AttributeList, type.BareAttributeList, type.BaseType?.BareAttributeList);
                    return result;
                }

                default:
                    return McdocType.Any();
            }
        }

        /// <summary>
        /// 裸属性（#[id] 这种没写值的形态）是否参与求值。
        /// 默认开：裸 #[id] 的 mapType 会给 literal/enum 值补 minecraft:，与 spyglass 一致。
        /// 一旦这个开关引起业务损坏，把它置 false 就回到改动前的行为（不需要改别的代码）。
        /// </summary>
        public static bool UseBareAttributes = true;

        public static List<McdocAttribute> FromAttributeList(Dictionary<string, MetaValue> attributes,
            List<string> bareAttributes = null)
        {
            var bare = UseBareAttributes ? bareAttributes : null;
            var hasValues = attributes is { Count: > 0 };
            var hasBare = bare is { Count: > 0 };
            if (!hasValues && !hasBare) return null;

            var result = new List<McdocAttribute>((attributes?.Count ?? 0) + (bare?.Count ?? 0));
            if (hasValues)
            {
                foreach (var pair in attributes)
                {
                    result.Add(new McdocAttribute { Name = pair.Key, Value = FromMetaValue(pair.Value) });
                }
            }
            if (hasBare)
            {
                foreach (var name in bare)
                {
                    if (string.IsNullOrEmpty(name)) continue;
                    // 同名既有有值形态时不重复（有值的那条信息更多）
                    if (result.Any(item => item?.Name == name)) continue;
                    result.Add(new McdocAttribute { Name = name });
                }
            }
            return result;
        }

        private static McdocAttrValue FromMetaValue(MetaValue value)
        {
            if (value == null) return null;
            switch (value.Kind)
            {
                case MetaValueKind.List:
                {
                    var items = new List<McdocAttrValue>();
                    foreach (var item in value.Items ?? []) items.Add(FromMetaValue(item));
                    return new McdocAttrValue { Literal = items };
                }
                // #[id(registry="item",tags="allowed")] 这种写法在模型里是元组，
                // 语义上与对象成员一样，统一映射成 Tree，否则最常见的复合 id 形式会丢成员。
                case MetaValueKind.Tuple:
                case MetaValueKind.Object:
                {
                    var tree = new Dictionary<string, McdocAttrValue>(StringComparer.Ordinal);
                    foreach (var member in value.Members ?? [])
                    {
                        if (member?.Name == null) continue;
                        tree[member.Name] = FromMetaValue(member.Value);
                    }
                    return new McdocAttrValue { Tree = tree };
                }
                case MetaValueKind.Type:
                {
                    var bridged = FromMetaType(value.TypeValue);
                    // #[since="1.20"] / #[color=hex_rgb] 这类值在语言服务器里是"类型表达式"，
                    // 但 mcdoc 语义上就是字面量（spyglass 用 validator.string 取值），这里折叠掉
                    if (bridged != null && bridged.Kind == McdocTypeKind.Literal)
                    {
                        return McdocAttrValue.Of(bridged.Value);
                    }
                    return new McdocAttrValue { Type = bridged };
                }
                default:
                    return McdocAttrValue.Of(UnwrapLiteral(value.LiteralValue));
            }
        }

        /// <summary>
        /// 合并多层类型的注解。
        /// 语言服务器的 VisitTypeSentence 把 `#[until="1.17"] X&lt;Y&gt;` 的注解挂在内层 X 上，
        /// 而 spyglass 挂在最外层包装（concrete/indexed）上；只有并到外层，union 成员的版本门控才生效。
        /// </summary>
        private static List<McdocAttribute> MergeAttributes(Dictionary<string, MetaValue> outer, Dictionary<string, MetaValue> inner,
            List<string> outerBare = null, List<string> innerBare = null)
        {
            var fromOuter = FromAttributeList(outer, outerBare) ?? [];
            var fromInner = FromAttributeList(inner, innerBare);
            if (fromInner == null || fromInner.Count == 0) return fromOuter.Count == 0 ? null : fromOuter;
            var result = new List<McdocAttribute>(fromOuter);
            foreach (var attribute in fromInner)
            {
                if (attribute == null) continue;
                if (result.Any(existing => existing != null && existing.Name == attribute.Name)) continue;
                result.Add(attribute);
            }
            return result;
        }

        /// <summary>
        /// 把 dispatch 语句自身的注解（#[since]/#[until] 等）并到成员类型上。
        /// spyglass 在 bindDispatchStatement 里做同样的事；漏了它，整条 dispatch 在某个版本被门控掉时，
        /// 客户端仍会把它当成可用的成员（表现为 oracle 空联合、我们这边有内容）。
        /// Resource/Index 这类是管线内部的特征键，不属于 mcdoc 注解，要排除。
        /// </summary>
        internal static McdocType ApplyDispatchAttributes(McdocType target, Dictionary<string, MetaValue> attributes)
        {
            if (target == null || attributes == null || attributes.Count == 0) return target;
            var merged = FromAttributeList(attributes);
            if (merged == null) return target;
            merged = merged.Where(attribute => attribute != null && !SyntheticAttributeNames.Contains(attribute.Name)).ToList();
            if (merged.Count == 0) return target;
            var clone = target.Clone();
            var list = target.Attributes == null ? new List<McdocAttribute>() : new List<McdocAttribute>(target.Attributes);
            list.AddRange(merged);
            clone.Attributes = list;
            return clone;
        }

        private static readonly HashSet<string> SyntheticAttributeNames = new(StringComparer.Ordinal)
        {
            "Resource", "Index", "Accessor", "ArrayType", "Generic", "DispatchTarget",
            "LengthFrom", "LengthTo", "ValueFrom", "ValueTo",
        };

        /// <summary>从 AttributeList 读区间端点（空串表示该侧开区间）。</summary>
        private static decimal? ReadRangeValue(Dictionary<string, MetaValue> features, string key)
        {
            if (features == null || !features.TryGetValue(key, out var value) || value == null) return null;
            var text = Convert.ToString(UnwrapLiteral(value.LiteralValue));
            if (string.IsNullOrWhiteSpace(text)) return null;
            return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
        }

        private static McdocRange ReadRange(Dictionary<string, MetaValue> features, string prefix)
        {
            var from = ReadRangeValue(features, prefix + "From");
            var to = ReadRangeValue(features, prefix + "To");
            if (from == null && to == null) return null;
            return new McdocRange { Min = from, Max = to };
        }

        /// <summary>
        /// 只取字段自身的注解（prelim 位置）。
        /// 冒号之后的 #[since]/#[until] 在 mcdoc 里属于"类型"，spyglass 不会用它过滤字段，
        /// 差分测试（oracle 对拍）确认过这一点，因此这里不做提升。
        /// </summary>
        private static List<McdocAttribute> FieldAttributes(MetaField field)
            => FromAttributeList(field.AttributeList, field.BareAttributeList);


        private static McdocRange ToRange(int? min, int? max)
        {
            if (!min.HasValue && !max.HasValue) return null;
            return new McdocRange
            {
                Min = min.HasValue ? min.Value : null,
                Max = max.HasValue ? max.Value : null
            };
        }

        private static McdocTypeKind? ToKind(MetaTypeKind? kind)
        {
            if (!kind.HasValue) return null;
            return kind.Value switch
            {
                MetaTypeKind.Byte => McdocTypeKind.Byte,
                MetaTypeKind.Short => McdocTypeKind.Short,
                MetaTypeKind.Int => McdocTypeKind.Int,
                MetaTypeKind.Long => McdocTypeKind.Long,
                MetaTypeKind.Float => McdocTypeKind.Float,
                MetaTypeKind.Double => McdocTypeKind.Double,
                MetaTypeKind.Boolean => McdocTypeKind.Boolean,
                _ => McdocTypeKind.String
            };
        }

        private static void FillLiteral(McdocType target, object literalValue)
        {
            var value = UnwrapLiteral(literalValue);
            switch (value)
            {
                case bool boolean:
                    target.LiteralKind = McdocLiteralKind.Boolean;
                    target.Value = boolean;
                    break;
                case string text:
                    target.LiteralKind = McdocLiteralKind.String;
                    target.Value = text;
                    break;
                default:
                    target.LiteralKind = McdocLiteralKind.Int;
                    target.Value = McdocHelpers.ToDecimal(value);
                    break;
            }
        }

        /// <summary>
        /// 统一把 object 型字面量还原成 CLR 基元。
        /// 两个必要转换：
        /// 1) typedNumber 会带回 (decimal, char) 元组 → 拆成标量；
        /// 2) 经命名管道 JSON 往返后，object 型字段被反序列化成正的 JsonElement（不是 string/decimal），
        ///    不还原的话 FillLiteral / 属性读取里的 is string / is bool 判断会全部静默失效。
        /// </summary>
        private static object UnwrapLiteral(object value)
        {
            if (value is ValueTuple<decimal, char> pair) return pair.Item1;
            // 值类型可空装箱只能用 as 判，不能写成模式匹配
            var nullablePair = value as ValueTuple<decimal, char>?;
            if (nullablePair.HasValue) return nullablePair.Value.Item1;
            if (value is JsonElement element)
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.String:
                        return element.GetString();
                    case JsonValueKind.Number:
                        return element.TryGetDecimal(out var number) ? number : (decimal)element.GetDouble();
                    case JsonValueKind.True:
                        return true;
                    case JsonValueKind.False:
                        return false;
                    case JsonValueKind.Null:
                        return null;
                    default:
                        return element.GetRawText();
                }
            }
            return value;
        }

        private static string ReadFeatureString(Dictionary<string, MetaValue> features, string key)
        {
            if (features == null || !features.TryGetValue(key, out var value) || value == null) return null;
            var literal = UnwrapLiteral(value.LiteralValue);
            return literal == null ? null : Convert.ToString(literal);
        }

        private static List<McdocIndex> ReadIndices(Dictionary<string, MetaValue> features)
        {
            var result = new List<McdocIndex>();
            if (features == null || !features.TryGetValue("Index", out var value) || value == null) return result;
            if (value.Kind == MetaValueKind.List)
            {
                foreach (var item in value.Items ?? [])
                {
                    var text = item == null ? null : Convert.ToString(UnwrapLiteral(item.LiteralValue));
                    if (text == null) continue;
                    result.Add(IsDynamicAccessor(text) ? ToDynamicIndex(text) : McdocIndex.Static(text));
                }
            }
            else
            {
                var text = Convert.ToString(UnwrapLiteral(value.LiteralValue));
                if (text != null) result.Add(IsDynamicAccessor(text) ? ToDynamicIndex(text) : McdocIndex.Static(text));
            }
            return result;
        }

        /// <summary>动态索引两种写法：`%key/%parent` 特化访问器，或 `[[a.b]]` 访问器链。</summary>
        private static bool IsDynamicAccessor(string text)
            => !string.IsNullOrEmpty(text)
            && (text.StartsWith("%", StringComparison.Ordinal)
                || (text.StartsWith("[[", StringComparison.Ordinal) && text.EndsWith("]]", StringComparison.Ordinal)));

        /// <summary>%parent / %key 这类特化访问器与普通键名混排，按点号切成访问器链。</summary>
        private static McdocIndex ToDynamicIndex(string text)
        {
            // [[after_action]] → 访问器链 after_action
            var inner = text.StartsWith("[[", StringComparison.Ordinal) && text.EndsWith("]]", StringComparison.Ordinal)
                ? text.Substring(2, text.Length - 4)
                : text;
            var accessor = new List<string>();
            foreach (var part in inner.Split('.'))
            {
                var trimmed = part.Trim();
                if (trimmed.Length > 0) accessor.Add(trimmed);
            }
            return McdocIndex.Dynamic(accessor);
        }
    }

    /// <summary>
    /// 字典驱动的符号索引：既能承载语言服务器返回的类型定义，也能承载 dispatcher 成员。
    /// 路径解析先精确匹配，再退化为末段名匹配（对齐 spyglass 的 use/相对名便利行为）。
    /// </summary>
    public sealed class McdocTypeIndex : IMcdocSymbols
    {
        private readonly Dictionary<string, McdocType> typeDefs = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, McdocType>> dispatchers = new(StringComparer.Ordinal);
        /// <summary>模块 → 该模块的 use 路径列表。</summary>
        private readonly Dictionary<string, List<string>> usePaths = new(StringComparer.Ordinal);
        /// <summary>注册发生在 Resource.Init 的并行循环里，读写都必须串行化。</summary>
        private readonly object gate = new();

        public int TypeDefCount { get { lock (gate) return typeDefs.Count; } }

        /// <summary>已登记的类型定义路径（扫描/诊断用）。</summary>
        public IReadOnlyCollection<string> TypeDefPaths
        {
            get { lock (gate) return typeDefs.Keys.OrderBy(name => name, StringComparer.Ordinal).ToList(); }
        }

        /// <summary>已注册的 dispatcher 空间名（扫描/诊断用；不含 % 成员）。</summary>
        public IReadOnlyCollection<string> DispatcherRegistries
        {
            get { lock (gate) return dispatchers.Keys.OrderBy(name => name, StringComparer.Ordinal).ToList(); }
        }

        public void RegisterTypeDef(string path, McdocType type)
        {
            if (string.IsNullOrEmpty(path) || type == null) return;
            lock (gate)
            {
                // 记住声明模块：相对名解析要以"定义所在的模块"为基准
                if (string.IsNullOrEmpty(type.Module))
                {
                    var index = path.LastIndexOf("::", StringComparison.Ordinal);
                    type.Module = index > 0 ? path.Substring(0, index) : "";
                }
                // 嵌套引用也要带上声明模块，否则跨文件引用会按"文档模块"解析，
                // 撞上同名类型就静默选错（particle::Particle 被解析到 assets 的那个）。
                StampModule(type, type.Module, 0);
                typeDefs[path] = type;
            }
        }

        /// <summary>把声明模块递归盖到所有嵌套的类型/引用上（只填空值，不覆盖已有模块）。</summary>
        private static void StampModule(McdocType type, string module, int depth)
        {
            if (type == null || string.IsNullOrEmpty(module) || depth > 64) return;
            if (string.IsNullOrEmpty(type.Module)) type.Module = module;
            foreach (var field in type.Fields ?? []) StampModule(field?.Type, module, depth + 1);
            foreach (var member in type.Members ?? []) StampModule(member, module, depth + 1);
            StampModule(type.Item, module, depth + 1);
            foreach (var item in type.Items ?? []) StampModule(item, module, depth + 1);
            if (type.Mapping != null)
            {
                foreach (var pair in type.Mapping) StampModule(pair.Value, module, depth + 1);
            }
        }

        /// <summary>登记某个模块的 use 列表（引用解析的第二优先级）。</summary>
        public void RegisterUsePaths(string modulePath, IEnumerable<string> paths)
        {
            if (modulePath == null || paths == null) return;
            lock (gate)
            {
                // 同一个模块下可能有多个文件（mod.mcdoc + 同名 .mcdoc），use 列表必须合并，
                // 直接覆盖会让后注册的那份把前面的 use 全冲掉（表现为"use 里明明写了却解析不到"）。
                if (!usePaths.TryGetValue(modulePath, out var existing))
                {
                    usePaths[modulePath] = new List<string>();
                    existing = usePaths[modulePath];
                }
                foreach (var path in paths)
                {
                    if (!string.IsNullOrEmpty(path) && !existing.Contains(path)) existing.Add(path);
                }
            }
        }

        public void RegisterDispatch(string registry, string memberIndex, McdocType type)
        {
            if (string.IsNullOrEmpty(registry) || string.IsNullOrEmpty(memberIndex) || type == null) return;
            lock (gate)
            {
                if (!dispatchers.TryGetValue(registry, out var members))
                {
                    members = new Dictionary<string, McdocType>(StringComparer.Ordinal);
                    dispatchers[registry] = members;
                }
                members[memberIndex] = type;
            }
        }

        /// <summary>从语言服务器返回的 mcdoc 类型列表建立索引（dispatch 语句进 dispatcher 表，其余进类型表）。</summary>
        public void RegisterAll(IEnumerable<(string Path, MetaType Type)> entries)
        {
            foreach (var entry in entries)
            {
                if (entry.Type == null) continue;
                if (entry.Type.Kind == MetaTypeKind.Dispatch)
                {
                    var registry = ReadResource(entry.Type);
                    var indices = ReadIndexValues(entry.Type);
                    var target = McdocBridge.FromMetaType(entry.Type.BaseType);
                    target = McdocBridge.ApplyDispatchAttributes(target, entry.Type.AttributeList);
                    // dispatch ...<T> to X<T>：形参挂在 dispatch 语句上，目标要套 Template 外壳
                    if (entry.Type.MetaTypeParameterNameList is { Count: > 0 } dispatchParams && target != null)
                    {
                        target = new McdocType
                        {
                            Kind = McdocTypeKind.Template,
                            Item = target,
                            TypeParams = dispatchParams.Keys.ToList(),
                        };
                    }
                    // dispatch 目标结构里也有引用：把 dispatch 语句所在模块盖到目标上
                    var dispatchModule = entry.Path is { Length: > 0 } dispatchPath
                        ? dispatchPath.Substring(0, Math.Max(0, dispatchPath.LastIndexOf("::", StringComparison.Ordinal)))
                        : null;
                    StampModule(target, dispatchModule, 0);
                    foreach (var index in indices) RegisterDispatch(registry, index, target);

                    // dispatch 的目标常常是"具名内联结构体"（to struct TestEnvironment { ... }），
                    // 它同时也是文档里可被引用的类型；只放进 dispatcher 表会让别处的引用解析不到。
                    var targetName = entry.Type.BaseType?.Name ?? entry.Type.BaseType?.MetaTypeName;
                    if (!string.IsNullOrEmpty(targetName))
                    {
                        var pathPrefix = entry.Path ?? "";
                        var splitIndex = pathPrefix.LastIndexOf("::", StringComparison.Ordinal);
                        if (splitIndex > 0) RegisterTypeDef(pathPrefix.Substring(0, splitIndex) + "::" + targetName, target);
                    }
                    continue;
                }
                RegisterTypeDef(entry.Path, McdocBridge.FromMetaType(entry.Type));
            }
        }

        /// <summary>
        /// 解析类型名，优先级对齐 spyglass 的符号解析：
        /// 1) 绝对路径（:: 开头）直接精确查；
        /// 2) 相对名先在"当前模块"里查（含 super 前缀回退）；
        /// 3) 再查该模块 use 列表里的别名（按最后一段匹配）；
        /// 4) 再沿模块路径逐级向上；
        /// 5) 最后才退回"末段名全局匹配"（只是兜底，会记录在 README 的已知近似里）。
        /// </summary>
        /// <summary>最近一次构建里"查不到"的类型名（诊断用，界面会显示）。</summary>
        public HashSet<string> MissedReferences { get; } = new(StringComparer.Ordinal);

        public McdocType GetTypeDef(string path, string documentUri, string modulePath = null)
        {
            var resolved = ResolveTypeDef(path, documentUri, modulePath);
            if (resolved is null && !string.IsNullOrEmpty(path))
            {
                lock (gate)
                {
                    if (MissedReferences.Count < 100) MissedReferences.Add(path);
                }
            }
            return resolved;
        }

        private McdocType ResolveTypeDef(string path, string documentUri, string modulePath = null)
        {
            if (string.IsNullOrEmpty(path)) return null;
            lock (gate)
            {
                if (path.StartsWith("::", StringComparison.Ordinal))
                {
                    var absolute = path.TrimEnd(':');
                    if (typeDefs.TryGetValue(absolute, out var exactAbsolute)) return exactAbsolute;
                    return LastSegmentFallback(absolute, modulePath);
                }

                var module = modulePath ?? "";
                var segments = path.Split("::", StringSplitOptions.RemoveEmptyEntries);
                var superCount = 0;
                while (superCount < segments.Length && segments[superCount] == "super") superCount++;
                var baseModule = module;
                for (var i = 0; i < superCount; i++) baseModule = ParentModule(baseModule);
                var rest = string.Join("::", segments.Skip(superCount));

                // 2) 当前模块（及祖先模块）下的相对名
                for (var walker = baseModule; ; walker = ParentModule(walker))
                {
                    var candidate = walker.Length > 0 ? walker + "::" + rest : "::" + rest;
                    if (typeDefs.TryGetValue(candidate, out var found)) return found;
                    if (walker.Length == 0) break;
                }

                // 3) use 别名：先查当前模块，再逐级向上（与第 2 步的相对名回退一致）
                var wanted = segments[^1];
                for (var walker = module; ; walker = ParentModule(walker))
                {
                    if (usePaths.TryGetValue(walker, out var uses))
                    {
                        foreach (var use in uses)
                        {
                            // use 语句支持 `use a::b as Alias`：匹配名用别名，解析路径用 as 左边
                            var (usePath, useName) = SplitUseAlias(use);
                            if (useName != wanted && usePath != wanted
                                && !usePath.EndsWith("::" + wanted, StringComparison.Ordinal)) continue;
                            var resolved = ResolveUsePath(usePath, walker);
                            if (resolved != null && typeDefs.TryGetValue(resolved, out var byUse)) return byUse;
                        }
                    }
                    if (walker.Length == 0) break;
                }

                // 5) 兜底：按"声明模块越接近越优先"挑；并列则判为未解析（宁可报诊断，也不静默选错同名类型）
                return LastSegmentFallback(rest, module);
            }
        }

        /// <summary>把一次名字解析的过程打印出来（探针 --resolve 与界面诊断共用）：候选、命中的步骤、最终选中。</summary>
        public string ExplainResolve(string path, string modulePath)
        {
            if (string.IsNullOrEmpty(path)) return "path 为空";
            lock (gate)
            {
                var trace = new System.Text.StringBuilder();
                var module = modulePath ?? "";
                trace.AppendLine("path=" + path + "  module=" + (module.Length == 0 ? "<empty>" : module));

                if (path.StartsWith("::", StringComparison.Ordinal))
                {
                    trace.AppendLine("  绝对路径：" + (typeDefs.ContainsKey(path.TrimEnd(':')) ? "命中" : "未命中"));
                    return trace.ToString();
                }

                var segments = path.Split("::", StringSplitOptions.RemoveEmptyEntries);
                var rest = string.Join("::", segments.SkipWhile(s => s == "super"));
                var superCount = segments.Length - segments.SkipWhile(s => s == "super").Count();
                var baseModule = module;
                for (var i = 0; i < superCount; i++) baseModule = ParentModule(baseModule);

                trace.Append("  相对名（逐级向上）：");
                for (var walker = baseModule; ; walker = ParentModule(walker))
                {
                    var candidate = walker.Length > 0 ? walker + "::" + rest : "::" + rest;
                    trace.Append(candidate + (typeDefs.ContainsKey(candidate) ? " ✓ " : " ✗ "));
                    if (walker.Length == 0) break;
                }
                trace.AppendLine();

                var name = segments[^1];
                trace.Append("  use 列表（按模块）：");
                for (var walker = module; ; walker = ParentModule(walker))
                {
                    var found = usePaths.TryGetValue(walker, out var uses);
                    trace.Append(walker + (found ? "(" + uses.Count + ")" : "✗") + " ");
                    if (walker.Length == 0) break;
                }
                trace.AppendLine("  目标名=" + name);

                var target = LastSegment(path);
                var candidates = typeDefs.Where(pair => LastSegment(pair.Key) == target).Select(pair => pair.Key).OrderBy(key => key, StringComparer.Ordinal).ToList();
                trace.AppendLine("  末段名候选（" + candidates.Count + "）：" + string.Join(", ", candidates));
                foreach (var key in candidates)
                {
                    trace.AppendLine("    " + key + "  声明模块=" + (typeDefs[key].Module ?? "<null>") + "  共前缀=" + SharedModuleSegments(typeDefs[key].Module, module));
                }
                var resolved = ResolveTypeDef(path, null, module);
                trace.AppendLine("  最终选中：" + (resolved is null ? "<null>" : (resolved.Module ?? "<no module>") + " kind=" + resolved.Kind));
                return trace.ToString();
            }
        }

        private McdocType LastSegmentFallback(string path, string module)
        {
            var target = LastSegment(path);
            McdocType best = null;
            var bestScore = -1;
            foreach (var pair in typeDefs)
            {
                if (LastSegment(pair.Key) != target) continue;
                var score = SharedModuleSegments(pair.Value?.Module, module);
                if (score <= bestScore) continue;
                bestScore = score;
                best = pair.Value;
            }
            // 并列时仍返回一个候选：同名类型到处都是，返回 null 会制造大量"未解析"，
            // 但优先级已按"声明模块更近"排序，比原来的字典遍历顺序稳定。
            return best;
        }

        /// <summary>两个模块路径有多少段相同的公共前缀（用来在同名类型之间选"更近的一份"）。</summary>
        private static int SharedModuleSegments(string left, string right)
        {
            var a = (left ?? "").Split("::", StringSplitOptions.RemoveEmptyEntries);
            var b = (right ?? "").Split("::", StringSplitOptions.RemoveEmptyEntries);
            var shared = 0;
            while (shared < a.Length && shared < b.Length && a[shared] == b[shared]) shared++;
            return shared;
        }

        /// <summary>拆分 `use X as Y`：返回（路径, 生效名）；没有 as 时名字取路径末段。</summary>
        private static (string Path, string Name) SplitUseAlias(string use)
        {
            if (string.IsNullOrEmpty(use)) return (use, null);
            var index = use.LastIndexOf(" as ", StringComparison.Ordinal);
            if (index < 0) return (use, LastSegment(use));
            var path = use.Substring(0, index).Trim();
            var alias = use.Substring(index + 4).Trim();
            return (path, alias.Length > 0 ? alias : LastSegment(path));
        }

        /// <summary>
        /// 展开 use 语句里的路径：`::a::b` 绝对；`super::a::b` 相对上一级模块；`a::b` 相对当前模块。
        /// </summary>
        private static string ResolveUsePath(string use, string module)
        {
            if (string.IsNullOrEmpty(use)) return null;
            if (use.StartsWith("::", StringComparison.Ordinal)) return use.TrimEnd(':');
            var segments = use.Split("::", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var current = module ?? "";
            var index = 0;
            while (index < segments.Length && segments[index] == "super")
            {
                current = ParentModule(current);
                index++;
            }
            var rest = string.Join("::", segments.Skip(index));
            if (rest.Length == 0) return current.Length > 0 ? current : null;
            return current.Length > 0 ? current + "::" + rest : "::" + rest;
        }

        private static string ParentModule(string modulePath)
        {
            if (string.IsNullOrEmpty(modulePath)) return "";
            var index = modulePath.LastIndexOf("::", StringComparison.Ordinal);
            return index <= 0 ? "" : modulePath.Substring(0, index);
        }

        public IReadOnlyDictionary<string, McdocType> GetDispatcherMembers(string registry, string documentUri)
        {
            if (string.IsNullOrEmpty(registry)) return null;
            // 返回快照：调用方（求值器）会枚举它，不能和并行注册共用同一份可变字典
            lock (gate)
            {
                return dispatchers.TryGetValue(registry, out var members)
                    ? new Dictionary<string, McdocType>(members, StringComparer.Ordinal)
                    : null;
            }
        }

        private static string LastSegment(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            var index = path.LastIndexOf("::", StringComparison.Ordinal);
            return index < 0 ? path : path.Substring(index + 2);
        }

        private static string ReadResource(MetaType dispatch)
        {
            if (dispatch.AttributeList == null) return null;
            return dispatch.AttributeList.TryGetValue("Resource", out var value) && value != null
                ? Convert.ToString(value.LiteralValue)
                : null;
        }

        private static List<string> ReadIndexValues(MetaType dispatch)
        {
            var result = new List<string>();
            if (dispatch.AttributeList == null || !dispatch.AttributeList.TryGetValue("Index", out var value) || value == null)
            {
                return result;
            }
            if (value.Kind == MetaValueKind.List)
            {
                foreach (var item in value.Items ?? [])
                {
                    var text = item?.LiteralValue?.ToString();
                    if (text != null) result.Add(text);
                }
            }
            else
            {
                var text = value.LiteralValue?.ToString();
                if (text != null) result.Add(text);
            }
            return result;
        }
    }
}
