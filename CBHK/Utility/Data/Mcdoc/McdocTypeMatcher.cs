// union 收窄：判断"某个 JSON 节点在结构上是否可能匹配这个（已简化的）类型"。
// spyglass 里这件事由 checker 做（渲染层再读 node.typeDef）；C# 侧没有 checker，
// 所以在求值阶段补这一步，等价于 checker 的收窄判定。
using System;

namespace CBHK.Utility.Data.Mcdoc
{
    public static class McdocTypeMatcher
    {
        public const int MaxDepth = 8;

        /// <summary>
        /// 渲染层的成员收窄（等价 spyglass checker 的判定）：
        /// 唯一命中才落成该成员，多命中保留 union 交给渲染层做分支选择。
        /// 注意它不属于 simplify —— 保持 simplify 与 spyglass 逐函数一致，才能用 oracle 对拍。
        /// </summary>
        public static McdocType Narrow(McdocType type, McdocJsonValue json, McdocSimplifyContext context)
            => SelectMember(type, json, context) ?? type;

        /// <summary>
        /// 选出"当前 JSON 节点唯一匹配的联合成员"，不唯一或无法判定时返回 null。
        /// 用途是给渲染层挑**默认分支**——联合本身要保留（UI 上还要能切成别的分支）。
        /// </summary>
        public static McdocType SelectMember(McdocType type, McdocJsonValue json, McdocSimplifyContext context,
            bool preferShapeOverAny = false)
        {
            if (type is null || type.Kind != McdocTypeKind.Union || json is null) return null;
            var members = type.Members;
            if (members is null || members.Count <= 1) return null;
            // 同一个值常常能匹配多个成员（"black" 既是 string 也落在 TextColor 里）：按"具体程度"取最高的那个。
            // 只看"命中了几个"会在这种联合上放弃选择，渲染层只能退回声明序第一支 —— 表现是
            // "选了 Enum 支，任何一次重建后又回到 String 支"。
            McdocType best = null;
            var bestSpecificity = int.MinValue;
            var bestCount = 0;
            foreach (var member in members)
            {
                if (!Matches(member, json, context)) continue;
                var specificity = Specificity(member);
                if (specificity > bestSpecificity)
                {
                    bestSpecificity = specificity;
                    best = member;
                    bestCount = 1;
                }
                else if (specificity == bestSpecificity)
                {
                    bestCount++;
                }
            }
            // Any 成员"匹配一切"但最不具体：它挤压掉真正对得上的结构支时，显示层也要走形状兜底。
            // 只对"挑默认分支"（渲染）生效；Narrow（求值收窄）保持原语义，否则 union 会被塌成单支、行再也切不了。
            if (best is null
                || (preferShapeOverAny && best.Kind is McdocTypeKind.Any or McdocTypeKind.Unsafe))
            {
                best = null;
                bestSpecificity = int.MinValue;
                bestCount = 0;
                // 精确匹配一个都没有：退到"只看 JSON 形状"这一层。
                // 典型现场 dimension_type.monster_spawn_light_level = (int | struct{ type, ...spread })：
                // 文档写成 { "type": "minecraft:clamped" }，结构支的必填键（min/max）还没填，
                // MatchesStruct 判它不匹配 → 整个联合没有候选 → 渲染层退回声明序第一支 Int，
                // 于是"对象被当成 int 渲染"（原始 JSON）并且每次重建都弹回 Int。
                // 这里只放宽"选哪一支显示"，取值的合法性仍由校验器按缺必填键报。
                foreach (var member in members)
                {
                    if (!MatchesKind(member, json)) continue;
                    var specificity = Specificity(member);
                    if (specificity <= 0) continue;   // Any/Unsafe 不参与形状兜底
                    if (specificity > bestSpecificity)
                    {
                        bestSpecificity = specificity;
                        best = member;
                        bestCount = 1;
                    }
                    else if (specificity == bestSpecificity)
                    {
                        bestCount++;
                    }
                }
            }
            // 唯一命中，或者"更具体的那个"是唯一最高分 → 选它；并列最高仍然不猜
            return best is not null && bestCount == 1 ? best : null;
        }

        /// <summary>
        /// 成员的具体程度（值越被限定越高）。用于"同一个值匹配多个成员"时挑默认分支：
        /// literal &gt; enum &gt; 普通同型 &gt; 其它 &gt; any/unsafe。
        /// </summary>
        private static int Specificity(McdocType type) => type?.Kind switch
        {
            McdocTypeKind.Literal => 4,
            McdocTypeKind.Enum => 3,
            McdocTypeKind.Boolean or McdocTypeKind.Byte or McdocTypeKind.Short or McdocTypeKind.Int
                or McdocTypeKind.Long or McdocTypeKind.Float or McdocTypeKind.Double
                or McdocTypeKind.String or McdocTypeKind.List or McdocTypeKind.Tuple
                or McdocTypeKind.Struct => 2,
            McdocTypeKind.Any or McdocTypeKind.Unsafe => 0,
            _ => 1,
        };

        /// <summary>结构上是否可能匹配。无法判定时返回 true（保守保留，宁可让用户选分支）。</summary>
        public static bool Matches(McdocType type, McdocJsonValue json, McdocSimplifyContext context, int depth = 0)
        {
            if (type == null || json == null) return false;
            if (depth > MaxDepth) return true;
            try
            {
                switch (type.Kind)
                {
                    case McdocTypeKind.Any:
                    case McdocTypeKind.Unsafe:
                        return true;

                    case McdocTypeKind.String:
                        return json.Kind == McdocJsonKind.String;

                    case McdocTypeKind.Byte:
                    case McdocTypeKind.Short:
                    case McdocTypeKind.Int:
                    case McdocTypeKind.Long:
                    case McdocTypeKind.Float:
                    case McdocTypeKind.Double:
                        return json.Kind == McdocJsonKind.Number;

                    case McdocTypeKind.Boolean:
                        return json.Kind == McdocJsonKind.Boolean;

                    case McdocTypeKind.Literal:
                        return MatchesLiteral(type, json);

                    case McdocTypeKind.Enum:
                        return MatchesEnum(type, json);

                    case McdocTypeKind.List:
                    case McdocTypeKind.ByteArray:
                    case McdocTypeKind.IntArray:
                    case McdocTypeKind.LongArray:
                    case McdocTypeKind.Tuple:
                        return json.Kind == McdocJsonKind.Array;

                    case McdocTypeKind.Struct:
                        return MatchesStruct(type, json, context, depth);

                    case McdocTypeKind.Union:
                        foreach (var member in type.Members ?? [])
                        {
                            if (Matches(member, json, context, depth + 1)) return true;
                        }
                        return false;

                    default:
                        // Reference / Dispatcher / Concrete / Template / Mapped / Indexed：
                        // 到这一步通常已被求值展平，剩余情况不判定
                        return true;
                }
            }
            catch (Exception)
            {
                return true;
            }
        }

        /// <summary>
        /// #[id] 的取值归一（spyglass builtin.js:94-123 的 checkInferred）：
        /// 带 prefix → 值必须以 prefix 开头，比较用 prefix + lengthen(去掉 prefix 的部分)；
        /// 不带 prefix → 值以 '!' 开头视为不匹配（那是"移除/tag"形态）；
        /// 其余一律 lengthen。返回 null 表示"与这个字段的取值形态不符"。
        /// 只用于**比较**，不改写任何值（写回仍然取字面量值）。
        /// </summary>
        public static string NormalizeIdValue(string value, McdocIdSpec spec)
        {
            var text = value ?? "";
            if (spec?.Prefix is { Length: > 0 } prefix)
            {
                if (!text.StartsWith(prefix, StringComparison.Ordinal)) return null;
                return prefix + McdocIdSpec.LengthenResourceLocation(text.Substring(prefix.Length));
            }
            if (text.StartsWith("!", StringComparison.Ordinal)) return null;
            return McdocIdSpec.LengthenResourceLocation(text);
        }

        /// <summary>
        /// 取值域判定（只覆盖"文档自身可判定"的三类）：enum 值是否在取值域、literal 是否匹配、定长表元素数是否超限。
        /// 供 McdocValidator 复用来产 E1 的诊断；不参与联合收窄。
        /// </summary>
        public static bool MatchesValueDomain(McdocType type, McdocJsonValue json)
        {
            if (type is null || json is null) return true;
            if (type.Kind == McdocTypeKind.Enum) return MatchesEnum(type, json);
            if (type.Kind == McdocTypeKind.Literal) return MatchesLiteral(type, json);
            if (type.Kind == McdocTypeKind.Tuple)
            {
                return json.Kind != McdocJsonKind.Array || (json.Items?.Count ?? 0) <= (type.Items?.Count ?? 0);
            }
            if (type.IsPrimitiveArray && type.LengthRange?.Max is { } max)
            {
                return json.Kind != McdocJsonKind.Array || (json.Items?.Count ?? 0) <= (int)max;
            }
            return true;
        }

        private static bool MatchesLiteral(McdocType type, McdocJsonValue json)
        {
            switch (type.LiteralKind)
            {
                case McdocLiteralKind.String:
                    // spyglass 的 #[id] checkInferred（builtin.js:94-123）：比较前按 #[id] 的 prefix/`!` 规则归一两侧
                    // （prefix 不符 / 无 prefix 却带 '!' 都算不匹配，其余 lengthen）。"写 block / 写 minecraft:block /
                    // 写 !foo" 都能落到正确分支，且不改写任何值。
                    if (json.Kind != McdocJsonKind.String) return false;
                    var literalSpec = McdocSelectOptions.Resolve(type);
                    var actualText = NormalizeIdValue(json.StringValue, literalSpec);
                    var expectedText = NormalizeIdValue(type.LiteralText(), literalSpec);
                    return actualText is not null && expectedText is not null && actualText == expectedText;
                case McdocLiteralKind.Boolean:
                    return json.Kind == McdocJsonKind.Boolean
                        && json.BooleanValue == (type.Value is bool flag && flag);
                default:
                    return json.Kind == McdocJsonKind.Number
                        && json.NumberValue == McdocHelpers.ToDecimal(type.Value);
            }
        }

        private static bool MatchesEnum(McdocType type, McdocJsonValue json)
        {
            var values = type.EnumValues;
            if (values == null || values.Count == 0) return true;
            if (type.EnumKind == McdocTypeKind.String)
            {
                if (json.Kind != McdocJsonKind.String) return false;
                // spyglass 比的是成员的**字面量值**（`${v.value}`，builtin.js:129-135 的 id.mapType 还会先 lengthen），
                // 不是 identifier。与 MatchesLiteral 同一口径：两侧都 lengthen，所以 "block" 与 "minecraft:block" 等价；
                // 值同样只在比较时补齐，不改写任何数据（写回仍然取字面量值）。
                var enumSpec = McdocSelectOptions.Resolve(type);
                var text = NormalizeIdValue(json.StringValue, enumSpec);
                if (text is null) return false;
                foreach (var value in values)
                {
                    if (value?.Value is null) continue;
                    if (NormalizeIdValue(Convert.ToString(value.Value), enumSpec) == text) return true;
                }
                return false;
            }
            if (json.Kind != McdocJsonKind.Number) return false;
            foreach (var value in values)
            {
                if (value != null && McdocHelpers.ToDecimal(value.Value) == json.NumberValue) return true;
            }
            return false;
        }

        /// <summary>只看 JSON 形状对不对得上声明（不看必填键齐不齐），用于"精确匹配一个都没有"时的兜底选支。</summary>
        private static bool MatchesKind(McdocType type, McdocJsonValue json)
        {
            if (type is null || json is null) return false;
            switch (type.Kind)
            {
                case McdocTypeKind.Any:
                case McdocTypeKind.Unsafe:
                    return true;
                case McdocTypeKind.Struct:
                case McdocTypeKind.Mapped:
                    return json.Kind == McdocJsonKind.Object;
                case McdocTypeKind.List:
                case McdocTypeKind.Tuple:
                case McdocTypeKind.ByteArray:
                case McdocTypeKind.IntArray:
                case McdocTypeKind.LongArray:
                    return json.Kind == McdocJsonKind.Array;
                case McdocTypeKind.String:
                case McdocTypeKind.Enum:
                case McdocTypeKind.Literal:
                    return json.Kind == McdocJsonKind.String;
                case McdocTypeKind.Byte:
                case McdocTypeKind.Short:
                case McdocTypeKind.Int:
                case McdocTypeKind.Long:
                case McdocTypeKind.Float:
                case McdocTypeKind.Double:
                    return json.Kind == McdocJsonKind.Number;
                case McdocTypeKind.Boolean:
                    return json.Kind == McdocJsonKind.Boolean;
                case McdocTypeKind.Union:
                    foreach (var member in type.Members ?? [])
                    {
                        if (MatchesKind(member, json)) return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        private static bool MatchesStruct(McdocType type, McdocJsonValue json, McdocSimplifyContext context, int depth)
        {
            if (json.Kind != McdocJsonKind.Object) return false;
            foreach (var field in type.Fields ?? [])
            {
                if (field == null || field.Name == null) continue;   // 动态键不参与判定
                var child = json.Get(field.Name);
                if (child == null)
                {
                    if (!field.Optional) return false;                // 必需字段缺失 → 不匹配
                    continue;
                }
                if (!Matches(field.Type, child, context, depth + 1)) return false;
            }
            return true;
        }
    }
}
