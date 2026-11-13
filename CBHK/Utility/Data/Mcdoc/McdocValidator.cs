using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>一条结构诊断（checker 的替代物：C# 侧没有 spyglass checker，这里做结构级校验）。</summary>
    public sealed class McdocDiagnostic
    {
        public string Path = "";
        public string Severity = "warn";
        public string Message = "";
    }

    /// <summary>
    /// 结构校验：必填缺键 / 未知字段 / 类型不匹配 / 未解析引用 / 长度越界。
    /// 只做"文档自身可判定"的检查，不猜语义（那是 checker 的活）。
    /// </summary>
    /// <summary>正则缓存：校验器每次 Load 都会跑，模式高度重复，编译结果必须复用。</summary>
    internal static class RegexCache
    {
        private static readonly ConcurrentDictionary<string, Regex> Patterns = new(StringComparer.Ordinal);

        public static bool IsValid(string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return false;
            try { Compile(pattern); return true; }
            catch (ArgumentException) { return false; }
        }

        public static bool Match(string pattern, string text)
        {
            try { return Compile(pattern).IsMatch(text ?? ""); }
            catch (ArgumentException) { return true; }   // 模式本身非法 → 不在这一条上判错
        }

        private static Regex Compile(string pattern)
            => Patterns.GetOrAdd(pattern, static value => new Regex(value, RegexOptions.Compiled));
    }

    public static class McdocValidator
    {
        public static IReadOnlyList<McdocDiagnostic> Validate(McdocTreeDocument document)
        {
            var result = new List<McdocDiagnostic>();
            if (document is null) return result;
            var missed = new HashSet<string>(document.MissedReferences ?? [], StringComparer.Ordinal);

            void Walk(McdocTreeNode node)
            {
                if (node is null) return;
                ValidateNodeCore(node, missed, result, document.RegistryOptions, document.GameVersion);
                foreach (var child in McdocPresenterRegistry.Default.Children(node)) Walk(child);
            }

            foreach (var node in document.Nodes) Walk(node);
            return result;
        }

        /// <summary>
        /// 只重算**一个节点**的诊断。文本→树的值补丁不走重建，节点诊断会停在上一次校验的结果上
        /// （表现：JSON 里改成合法值，感叹号去不掉）；值只可能影响它自己，所以按节点重算就够。
        /// </summary>
        /// <summary>
        /// 只重算一棵子树（切分支 / 值补丁之后用）。文档级的 <see cref="Validate"/> 每次全量走一遍太贵，
        /// 但**子树**必须一起走：只算根节点的话，子节点还挂着上一支形状下的旧诊断，
        /// 行上的黄色感叹号就再也下不去。
        /// </summary>
        public static IReadOnlyList<McdocDiagnostic> ValidateSubtree(McdocTreeNode root, IReadOnlyCollection<string> missed,
            IMcdocRegistryOptions registryOptions = null, string version = null)
        {
            var result = new List<McdocDiagnostic>();
            if (root is null) return result;
            var missedSet = missed as HashSet<string> ?? new HashSet<string>(missed ?? []);
            var stack = new Stack<McdocTreeNode>();
            stack.Push(root);
            var guard = 0;
            while (stack.Count > 0 && guard++ < 20000)
            {
                var node = stack.Pop();
                ValidateNodeCore(node, missedSet, result, registryOptions, version);
                foreach (var child in McdocPresenterRegistry.Default.Children(node)) stack.Push(child);
            }
            return result;
        }

        public static IReadOnlyList<McdocDiagnostic> ValidateNode(McdocTreeNode node,
            IReadOnlyCollection<string> missedReferences = null,
            IMcdocRegistryOptions registryOptions = null, string version = null)
        {
            var result = new List<McdocDiagnostic>();
            if (node is null) return result;
            var missed = new HashSet<string>(missedReferences ?? [], StringComparer.Ordinal);
            ValidateNodeCore(node, missed, result, registryOptions, version);
            return result;
        }

        private static void ValidateNodeCore(McdocTreeNode node, HashSet<string> missed, List<McdocDiagnostic> result,
            IMcdocRegistryOptions registryOptions = null, string version = null)
        {
            node.Diagnostics = [];

                if (node.IsRequired && node.Json is null && node.Parent is not null)
                {
                    Add(node, "error", "缺少必填键：" + node.FieldName);
                }
                if (node.IsUnknownField)
                {
                    Add(node, "warn", "未在 schema 中声明的键：" + node.FieldName);
                }
                // 声明不是 any、求值却退成 any：这就是"静默降级"。最典型的现场是**靠同级/父级键取值的
                // dispatcher**（`minecraft:block[[BlockState.Name]]`、`mcdoc:block_item_states[[%parent.id]]`）
                // 在根上求值 —— 解不出来是诚实的，但用户必须看得见，否则就是"这一行退成原始 JSON、没有任何提示"。
                if (node.Type?.Kind == McdocTypeKind.Any
                    && node.DeclaredType is { Kind: not (McdocTypeKind.Any or McdocTypeKind.Unsafe) }
                    && string.IsNullOrEmpty(node.Error)
                    && node.Diagnostics.Count == 0)
                {
                    Add(node, "warn", "声明在当前上下文解不出具体类型（已按 any 显示）：" + node.DeclaredType.Kind);
                }
                if (!string.IsNullOrEmpty(node.DeclaredType?.Path) && missed.Contains(node.DeclaredType.Path))
                {
                    Add(node, "error", "未解析的类型引用：" + node.DeclaredType.Path);
                }
                if (node.Type?.LengthRange is { } range && node.Json is { Kind: McdocJsonKind.Array } array)
                {
                    var count = array.Items?.Count ?? 0;
                    if (range.Min is { } min && count < min)
                    {
                        Add(node, "warn", "列表长度 " + count + " < 下限 " + min);
                    }
                    if (range.Max is { } max && count > max)
                    {
                        Add(node, "warn", "列表长度 " + count + " > 上限 " + max);
                    }
                }
                if (node.Json is { Kind: McdocJsonKind.Number } number
                    && (node.Type?.Range ?? node.DeclaredType?.Range) is { } numberRange
                    && !numberRange.Contains(number.NumberValue))
                {
                    Add(node, "warn", RangeWarning(number.NumberValue, numberRange));
                }
                foreach (var message in AttributeViolations(node, registryOptions, version)) Add(node, "warn", message);
                if (Mismatch(node))
                {
                    Add(node, "error", "值与类型不符：" + (node.Type?.Kind.ToString() ?? "?") + " ← "
                        + (node.Json?.Kind.ToString() ?? "null"));
                }
                // 取值域（misode checker 里"文档自身可判定"的部分）：枚举值 / literal / 定长表元素数
                // 有值但不在候选里：只告警，不改 JSON、不动选中项、不触发重建（卡住不动）
                if (!node.IsDynamicKey && !node.IsMapEntry
                    && node.EnumOptionList is { Count: > 0 } candidates
                    && node.Json is { Kind: McdocJsonKind.String } text
                    && !string.IsNullOrEmpty(text.StringValue)
                    && candidates.All(option => Convert.ToString(option.Value?.LiteralValue) != text.StringValue))
                {
                    Add(node, "warn", "值不在候选内：" + text.StringValue);
                }

                if (!node.IsDynamicKey && !node.IsMapEntry && !McdocTypeMatcher.MatchesValueDomain(node.Type, node.Json))
                {
                    Add(node, "error", "值不在取值域：" + (node.Type?.Kind.ToString() ?? "?") + " ← "
                        + (node.Json?.Kind == McdocJsonKind.String ? node.Json.StringValue : node.Json?.Kind.ToString()));
                }


            void Add(McdocTreeNode node, string severity, string message)
            {
                node.Diagnostics.Add(message);
                result.Add(new McdocDiagnostic { Path = node.JsonPath ?? "$", Severity = severity, Message = message });
            }
        }

        /// <summary>
        /// 数值超出声明 Range：只给 warning，不夹取、不改 JSON（用户可以保留越界值）。
        /// 走行右侧的黄色 DiagnosticBadge，Tooltip 就是这个字符串。
        /// </summary>
        private static string RangeWarning(decimal value, McdocRange range)
        {
            var current = FormatNumber(value);
            if (range.Min is { } min && range.Max is { } max)
            {
                var left = range.MinExclusive ? "(" : "";
                var right = range.MaxExclusive ? ")" : "";
                return "数值 " + current + " 超出范围，应在 " + left + FormatNumber(min)
                    + "-" + FormatNumber(max) + right + " 之间";
            }
            if (range.Min is { } onlyMin)
            {
                return "数值 " + current + " 超出范围，应" + (range.MinExclusive ? "大于 " : "不小于 ")
                    + FormatNumber(onlyMin);
            }
            if (range.Max is { } onlyMax)
            {
                return "数值 " + current + " 超出范围，应" + (range.MaxExclusive ? "小于 " : "不大于 ")
                    + FormatNumber(onlyMax);
            }
            return "数值 " + current + " 超出范围";
        }

        private static string FormatNumber(decimal value)
            => value.ToString("0.############################", CultureInfo.InvariantCulture);

        /// <summary>
        /// 属性取值域校验（spyglass 内建属性里"文档自身可判定"的那几条）：
        /// #[integer] / #[divisible_by] / #[regex_pattern] / #[match_regex]。
        /// canonical / dispatcher_key 属组包与语义用途，这里不判定。
        /// </summary>
        private static IEnumerable<string> AttributeViolations(McdocTreeNode node,
            IMcdocRegistryOptions registryOptions, string version)
        {
            var json = node?.Json;
            if (json is null) yield break;
            var type = node.DeclaredType ?? node.Type;

            if (McdocHelpers.HasAttribute(type, "integer") && json.Kind == McdocJsonKind.Number
                && json.NumberValue != decimal.Truncate(json.NumberValue))
            {
                yield return "要求整数，实际是 " + json.NumberValue;
            }

            var divisor = McdocHelpers.AttributeValue(type, "divisible_by");
            if (divisor is not null && json.Kind == McdocJsonKind.Number
                && decimal.TryParse(divisor, NumberStyles.Any, CultureInfo.InvariantCulture, out var step) && step != 0m
                && json.NumberValue % step != 0m)
            {
                yield return "要求能被 " + divisor + " 整除，实际是 " + json.NumberValue;
            }

            var pattern = McdocHelpers.AttributeValue(type, "regex_pattern");
            if (pattern is not null && json.Kind == McdocJsonKind.String
                && !RegexCache.Match(pattern, json.StringValue ?? ""))
            {
                yield return "不符合模式 " + pattern + "：" + json.StringValue;
            }

            var rawPattern = McdocHelpers.AttributeValue(type, "match_regex");
            if (rawPattern is not null && json.Kind == McdocJsonKind.String && !RegexCache.IsValid(json.StringValue))
            {
                yield return "不是合法的正则：" + json.StringValue;
            }

            // #[id{prefix}] 前缀不符 / #[id{exclude}] 命中（builtin.js idValidator 的 prefix/exclude 语义）
            var idSpec = McdocSelectOptions.Resolve(type);
            if (idSpec is not null && json.Kind == McdocJsonKind.String)
            {
                if (idSpec.Prefix is { Length: > 0 } prefix
                    && !(json.StringValue ?? "").StartsWith(prefix, StringComparison.Ordinal))
                {
                    yield return "缺少 #[id] 要求的 " + prefix + " 前缀：" + json.StringValue;
                }
                if (idSpec.Exclude is { Count: > 0 })
                {
                    var current = McdocIdSpec.Lengthen(json.StringValue);
                    if (idSpec.Exclude.Any(item => McdocIdSpec.Lengthen(item) == current))
                    {
                        yield return "#[id] 排除的取值：" + json.StringValue;
                    }
                }
                // 跨资源存在性（misode checker 的对应物）：只给 warn，不改值、不弹选择；
                // 拿不到注册表数据时 Contains 返回 true（不判），避免离线环境全红
                if (registryOptions is not null && !string.IsNullOrEmpty(idSpec.Registry)
                    && !string.IsNullOrEmpty(json.StringValue)
                    && !registryOptions.Contains(idSpec, json.StringValue, version))
                {
                    yield return "值不在注册表 " + idSpec.Registry + " 内：" + json.StringValue;
                }
            }
        }

        /// <summary>JSON 形态与求值类型是否明显不相容（只判断互斥的几类，避免误报）。</summary>
        private static bool Mismatch(McdocTreeNode node)
        {
            if (node.Json is null || node.Type is null) return false;
            if (node.Type.Kind is McdocTypeKind.Any or McdocTypeKind.Unsafe or McdocTypeKind.Union
                or McdocTypeKind.Dispatcher or McdocTypeKind.Reference or McdocTypeKind.Indexed) return false;
            if (node.IsDynamicKey || node.IsMapEntry) return false;

            return node.Json.Kind switch
            {
                McdocJsonKind.Object => node.Type.Kind is not (McdocTypeKind.Struct or McdocTypeKind.Mapped),
                McdocJsonKind.Array => node.Type.Kind is not (McdocTypeKind.List or McdocTypeKind.Tuple
                    or McdocTypeKind.ByteArray or McdocTypeKind.IntArray or McdocTypeKind.LongArray),
                McdocJsonKind.String => node.Type.Kind is McdocTypeKind.Boolean or McdocTypeKind.Int
                    or McdocTypeKind.Long or McdocTypeKind.Float or McdocTypeKind.Double
                    or McdocTypeKind.Byte or McdocTypeKind.Short,
                McdocJsonKind.Boolean => node.Type.Kind is McdocTypeKind.String or McdocTypeKind.Int
                    or McdocTypeKind.Float or McdocTypeKind.Double,
                McdocJsonKind.Number => node.Type.Kind is McdocTypeKind.Boolean or McdocTypeKind.String,
                _ => false,
            };
        }
    }
}
