// #[id] 的复合形式（对照 spyglass runtime/attribute/builtin.ts 的 IdConfig）：
//   #[id="registry"]                       —— 简单形式
//   #[id{registry:"x", path:"a/", include:[...], exclude:[...], tags:"allowed", prefix:"!"}]
//   #[id=Type]                             —— 值是一个类型表达式时取其字面量
// 这里只做"解析 + 过滤"，真正的数据（注册表成员、tag 列表）由 IMcdocRegistryOptions 提供。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using System.Collections.ObjectModel;
using MinecraftLanguageModelLibrary.Data;

namespace CBHK.Utility.Data.Mcdoc
{
    public sealed class McdocIdSpec
    {
        public string Registry;
        public string Path;
        public List<string> Include;
        public List<string> Exclude;
        /// <summary>目前 spyglass 只允许 '!'（表示 tag 前缀）。</summary>
        public string Prefix;
        /// <summary>allowed | implicit | required</summary>
        public string Tags;
        /// <summary>裸 #[id]（没有 registry 配置）：候选只来自类型自身，且允许输入候选外的值。</summary>
        public bool Bare;

        /// <summary>只扫描给定属性列表里的 #[id]（不穿透引用）。求值期用它从"当前类型自带的属性"取规格。</summary>
        public static McdocIdSpec FromAttributes(IEnumerable<McdocAttribute> attributes)
        {
            if (attributes is null) return null;
            McdocIdSpec bare = null;
            foreach (var attribute in attributes)
            {
                var spec = FromAttribute(attribute);
                if (spec is null) continue;
                if (!string.IsNullOrEmpty(spec.Registry)) return spec;
                bare ??= spec;
            }
            return bare;
        }

        /// <summary>从 #[id] 属性解析；不是 id 形状时返回 null。</summary>
        public static McdocIdSpec FromAttribute(McdocAttribute attribute)
        {
            if (attribute?.Name is not "id")
            {
                return null;
            }
            // 裸 #[id]：没有配置。spyglass 里它照样进 handler（mapType 生效），
            // 候选只来自类型自身且 allowUnknown（builtin.js:18-33）。
            if (attribute.Value is null)
            {
                return new McdocIdSpec { Bare = true };
            }
            var value = attribute.Value;

            // 简单形式：字面量字符串
            var literal = value.AsString();
            if (!string.IsNullOrEmpty(literal))
            {
                return new McdocIdSpec { Registry = literal.Trim('"') };
            }

            // 复合形式：树成员
            if (value.Tree is { } tree)
            {
                var spec = new McdocIdSpec
                {
                    Registry = ReadString(tree, "registry"),
                    Path = ReadString(tree, "path"),
                    Prefix = ReadString(tree, "prefix"),
                    Tags = ReadString(tree, "tags"),
                    Include = ReadList(tree, "include"),
                    Exclude = ReadList(tree, "exclude"),
                };
                // 没有 registry 也可能是合法配置：#[id{prefix="!"}] / #[id{exclude=[…]}]（builtin.js idValidator）。
                // 这类规格不产生候选，但 prefix/exclude 参与取值判定，所以不能丢。
                var hasConfig = !string.IsNullOrEmpty(spec.Registry) || !string.IsNullOrEmpty(spec.Prefix)
                    || !string.IsNullOrEmpty(spec.Path) || !string.IsNullOrEmpty(spec.Tags)
                    || spec.Include is { Count: > 0 } || spec.Exclude is { Count: > 0 };
                return hasConfig ? spec : null;
            }

            // 类型表达式形式：#[id=some::Type]
            var typeLiteral = value.Type?.LiteralText();
            return string.IsNullOrEmpty(typeLiteral) ? null : new McdocIdSpec { Registry = typeLiteral };
        }

        private static string ReadString(Dictionary<string, McdocAttrValue> tree, string key)
            => tree.TryGetValue(key, out var value) ? value?.AsString()?.Trim('"') : null;

        private static List<string> ReadList(Dictionary<string, McdocAttrValue> tree, string key)
        {
            if (!tree.TryGetValue(key, out var value) || value is null)
            {
                return null;
            }
            var result = new List<string>();
            // 数组字面量在模型里是 List<McdocAttrValue>（McdocBridge 把它放进 Literal）
            if (value.Literal is List<McdocAttrValue> list)
            {
                foreach (var item in list)
                {
                    var text = item?.AsString()?.Trim('"');
                    if (!string.IsNullOrEmpty(text)) result.Add(text);
                }
                return result.Count == 0 ? null : result;
            }
            if (value.Tree is { } nested)
            {
                // 树形数组在属性里是 {0:"a",1:"b"} 或 {"0":"a"}，值取字面量
                foreach (var pair in nested)
                {
                    var item = pair.Value?.AsString()?.Trim('"');
                    if (!string.IsNullOrEmpty(item)) result.Add(item);
                }
            }
            else
            {
                var single = value.AsString()?.Trim('"');
                if (!string.IsNullOrEmpty(single)) result.Add(single);
            }
            return result.Count == 0 ? null : result;
        }

        /// <summary>
        /// spyglass 的 ResourceLocation.lengthen（core/common/util.js:95-105）：
        /// 没有 ':' → 补 "minecraft:"；以 ':' 开头 → 前面补 "minecraft"；其余原样。
        /// 与下面 Lengthen（#[id] 候选值用，会跳过 '#' 标签前缀）语义不同，别合并。
        /// </summary>
        public static string LengthenResourceLocation(string value)
        {
            var text = value ?? "";
            var separator = text.IndexOf(':');
            if (separator < 0) return "minecraft:" + text;
            if (separator == 0) return "minecraft" + text;
            return text;
        }

        /// <summary>裸 id 补默认命名空间（spyglass 的 ResourceLocation.lengthen）。</summary>
        public static string Lengthen(string id)
            => string.IsNullOrEmpty(id) || id.StartsWith("#", StringComparison.Ordinal) || id.Contains(':')
                ? id
                : "minecraft:" + id;

        /// <summary>
        /// 一个键是否属于本规格：带 prefix 的规格只认该前缀，不带前缀的规格不认任何前缀形态。
        /// 结构里有多个动态字段时（DataComponentPatch 的普通形式与 "!" 移除形式），靠它把键派给正确的字段。
        /// </summary>
        public bool AcceptsKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            if (Prefix is { Length: > 0 })
            {
                return key.StartsWith(Prefix, StringComparison.Ordinal);
            }
            return !key.StartsWith("!", StringComparison.Ordinal)
                && !key.StartsWith("#", StringComparison.Ordinal);
        }

        /// <summary>
        /// 调度器成员名是否属于本规格。与 <see cref="Apply"/> 的区别：调度器成员的"id 文本"就是键本身
        /// （`gameplay/bees_stay_in_hive`），不做 lengthen、不加命名空间。
        /// </summary>
        public bool ApplyToDispatcherKey(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (Prefix is { Length: > 0 })
            {
                if (!name.StartsWith(Prefix, StringComparison.Ordinal)) return false;
            }
            else if (!AcceptsKey(name))
            {
                return false;
            }
            if (Include is { Count: > 0 } && !Include.Any(item => SameId(item, name))) return false;
            if (Exclude is { Count: > 0 } && Exclude.Any(item => SameId(item, name))) return false;
            return true;
        }

        /// <summary>
        /// 调度器成员是否属于本规格（**不考虑 prefix**：dispatcher 的索引成员永远不带前缀，
        /// 前缀在生成候选/写回时才加）。Include/Exclude 按去前缀、去命名空间的 id 比较。
        /// </summary>
        public bool AcceptsDispatcherMember(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (string.IsNullOrEmpty(Prefix) && !AcceptsKey(name)) return false;
            if (Include is { Count: > 0 } && !Include.Any(item => SameId(item, name))) return false;
            if (Exclude is { Count: > 0 } && Exclude.Any(item => SameId(item, name))) return false;
            return true;
        }

        private static bool SameId(string left, string right)
            => string.Equals(left, right, StringComparison.Ordinal)
               || string.Equals(Lengthen(left), Lengthen(right), StringComparison.Ordinal);

        /// <summary>把注册表原始成员 + tag 列表按本规格过滤/修饰成下拉选项。</summary>
        public List<string> Apply(IEnumerable<string> registryOptions, IEnumerable<string> tagOptions = null)
        {
            var result = new List<string>();

            foreach (var raw in registryOptions ?? [])
            {
                var id = Lengthen(raw);
                if (string.IsNullOrEmpty(id)) continue;
                if (Path is { Length: > 0 } path && !id.Contains('/')
                    && !id.EndsWith("/" + path.TrimEnd('/'), StringComparison.Ordinal))
                {
                    // path 是"隐式路径"：只保留该路径下的成员
                    id = Lengthen(path.TrimEnd('/') + "/" + raw);
                }
                if (Include is { Count: > 0 } && !Include.Any(item => Lengthen(item) == id)) continue;
                if (Exclude is { Count: > 0 } && Exclude.Any(item => Lengthen(item) == id)) continue;
                result.Add(Prefix is { Length: > 0 } ? Prefix + id : id);
            }

            // tags="required"：值**必须**是 tag，注册表成员一个都不合法（1.18.2..26.2 的 infiniburn 就是这条）。
            // 以前不认 required：候选里全是方块 id，文档里合法写着的 "#minecraft:infiniburn_overworld"
            // 于是被判成"候选外的值"挂 warn，下拉里还会把方块 id 当选项显示。
            if (Tags is "required")
            {
                result.Clear();
            }

            // tag 形式（required / allowed / implicit）：'#' 或属性里的 prefix 前缀
            if (Tags is "required" or "allowed" or "implicit")
            {
                var tagPrefix = Prefix is { Length: > 0 } ? Prefix : "#";
                foreach (var raw in tagOptions ?? [])
                {
                    var id = Lengthen(raw);
                    if (string.IsNullOrEmpty(id)) continue;
                    if (Exclude is { Count: > 0 } && Exclude.Any(item => Lengthen(item) == id)) continue;
                    var option = tagPrefix + id;
                    if (result.Contains(option)) continue;
                    result.Add(option);
                }
            }

            return result;
        }
    }

    /// <summary>哑下拉自报：只在 trace 打开时写，供 --check-trace 判定。</summary>
    internal static class McdocOptionTrace
    {
        public static void Report(McdocTreeNode node, string reason)
            => McdocWriteTrace.Trace("enum-empty", "path=" + (node?.JsonPath ?? "?") + " reason=" + reason);
    }

    /// <summary>#[id] 规格解析与候选值填充（misode 的 getValues / isSelectRegistry 分支）。</summary>
    internal static class McdocSelectOptions
    {
        /// <summary>带 #[id] / #[tree{registry}] 的字符串节点：填候选值让 UI 渲染成下拉。</summary>
        public static void Apply(McdocTreeNode node, McdocType type, McdocRuntime runtime)
        {
            if (node is null || type is null) return;
            if (type.Kind is not (McdocTypeKind.String or McdocTypeKind.Enum or McdocTypeKind.Literal)) return;

            // #[dispatcherkey="<registry>"]：候选 = 该 dispatcher 的成员键（builtin.js:61-77）
            var dispatcherKey = McdocHelpers.AttributeValue(type, "dispatcher_key")?.Trim('"');
            if (!string.IsNullOrEmpty(dispatcherKey))
            {
                ApplyDispatcherKey(node, dispatcherKey, runtime);
                return;
            }

            var spec = Resolve(type, runtime);
            if (spec?.Bare == true)
            {
                ApplyBareId(node, type);
                return;
            }
            if (string.IsNullOrEmpty(spec?.Registry) || runtime is null)
            {
                // 没有 #[id] 注册表：**裸枚举的候选来自 EnumValues**（表现器自己填，下拉是好的），
                // 在这里报一条就是假阳性 —— 以前每个裸枚举都写，`--check-trace` 的 enum-empty=0
                // 永远到不了（skybox / cardinal_light 这种场次全都计入）。只有"声明了注册表候选
                // 却拿不到数据源"才是真正的哑下拉。
                if (type.Kind == McdocTypeKind.Enum && runtime is null && spec?.Registry is { Length: > 0 })
                {
                    McdocOptionTrace.Report(node, "enum-without-registry");
                }
                return;
            }

            // 候选在求值期就解析好了（McdocSimplifier.ResolveIdMembers）：presenter 不再回查数据源
            var options = type.IdMembers;
            if (options is not { Count: > 0 })
            {
                // 声明有注册表/是枚举，求值却给不出候选：行上就是一个"哑下拉"（点了没反应）
                McdocOptionTrace.Report(node, "no-members registry=" + spec.Registry);
                return;
            }

            // misode McdocRenderer.tsx:163：isSelect = idRegistry && isSelectRegistry(idRegistry, version)。
            // 白名单内 → 闭合下拉（<select>，只能选候选）；白名单外 → 可编辑下拉（<input list=datalist>，
            // 自由输入 + 候选提示）。两种都挂候选，差别只在"允不允许输入候选外的值"。
            node.UseSelect = McdocSelectRegistries.IsSelect(spec.Registry, runtime.GameVersion ?? "1.21");
            node.AllowCustomValue = !node.UseSelect;
            var sharedOptions = SharedOptions(options);
            var current = node.Json is { Kind: McdocJsonKind.String } ? node.Json.StringValue : null;
            var selected = current is null ? null : Array.Find(sharedOptions, item => item.Name == current);
            if (current is not null && selected is null)
            {
                // 文档里的值不在候选内（旧版本写的 / 手改过的键）：把它本身当成一个候选项挂上并选中。
                // **不能**回落到 FirstOrDefault —— 那一项可能是「（未设置）」哨兵，选中即删键；
                // 也不能不选：行上显示的值必须等于文档里的值（否则就是"行说没设、文档里有"）。
                selected = new EnumMember { Name = current, Value = new MetaValue { LiteralValue = current } };
                // 这一份是**该节点私有**的：候选外的值只属于它自己，写进共享集合会污染别的节点
                var own = new ObservableCollection<EnumMember>(sharedOptions);
                own.Insert(0, selected);
                node.EnumOptionList = own;
            }
            else
            {
                node.EnumOptionList = SharedCollection(sharedOptions);
            }
            // 建树期**静默**落选中项：走属性 setter 会触发 ValueChanged → 整条编辑管线（诊断/依赖重建/
            // 物化/写回文本）在建树中途对这份 JSON 再跑一遍。实测这条在 1657 项的 item 注册表上
            // 每个节点 5~75ms，S0 的 doc.build 里 41.5/51.6ms 全花在这儿，而且哨兵回落会把用户的键删掉。
            node.SetEnumOptionSilently(selected ?? node.EnumOptionList.FirstOrDefault());
        }

        /// <summary>
        /// 候选实例按「候选列表实例」缓存：同一份 (文档, 版本, registry) 的 IdMembers 是同一个列表
        /// （<see cref="McdocIdSpaceService"/> 按 key 缓存解析结果），所以跨节点可以复用同一批 EnumMember。
        /// 不缓存的话每建一个 item 注册表节点就新建 1657 个 EnumMember + MetaValue，实测 Apply 正常
        /// 0.1ms、偶发 6~15ms 的尖峰全是 GC（S0 的 doc.build 里一半时间耗在这批分配上）。
        /// 共享是安全的：全仓没有就地改 EnumMember.Name/Value 的代码，换值一律是新建实例。
        /// </summary>
        private static readonly ConditionalWeakTable<IReadOnlyList<string>, EnumMember[]> optionCache = new();

        private static EnumMember[] SharedOptions(IReadOnlyList<string> options)
        {
            return optionCache.GetValue(options, static list =>
            {
                var members = new EnumMember[list.Count];
                for (var index = 0; index < list.Count; index++)
                {
                    members[index] = new EnumMember
                    {
                        Name = list[index],
                        Value = new MetaValue { LiteralValue = list[index] },
                    };
                }
                return members;
            });
        }

        /// <summary>
        /// 候选集合本身也共享：每节点 <c>new ObservableCollection&lt;EnumMember&gt;(sharedOptions)</c> 是一次
        /// 1657 元素的数组拷贝，1MB 档 5.4 万个节点量出来是 prepare.options 725ms，几乎全在 GC。
        /// **只读约定**：挂"候选外的值"的节点必须自己 new 一份（见上），谁都不许往共享集合里 Add/Remove。
        /// </summary>
        private static readonly ConditionalWeakTable<EnumMember[], ObservableCollection<EnumMember>> collectionCache = new();

        private static ObservableCollection<EnumMember> SharedCollection(EnumMember[] options)
            => collectionCache.GetValue(options, static list => new ObservableCollection<EnumMember>(list));

        /// <summary>
        /// #[dispatcherkey="&lt;registry&gt;"]：候选 = 该 dispatcher 的成员键（去掉 % 开头的兜底项 + 过属性过滤），
        /// 对应 spyglass builtin.js:61-77 的 stringMocker（LiteralNode.mock 的 pool）。
        /// 未知 registry / 没有候选 → 不挂下拉，保持自由文本（不抛）。
        /// </summary>
        private static void ApplyDispatcherKey(McdocTreeNode node, string registry, McdocRuntime runtime)
        {
            var members = runtime?.Symbols?.GetDispatcherMembers(registry, runtime.DocumentUri ?? "", runtime.GameVersion);
            if (members is null || members.Count == 0) return;

            var options = new List<string>();
            foreach (var pair in members)
            {
                if (string.IsNullOrEmpty(pair.Key)) continue;
                if (pair.Key.StartsWith("%", StringComparison.Ordinal)) continue;
                if (runtime.Attributes != null && !runtime.Attributes.ShouldKeep(pair.Value?.Attributes, runtime)) continue;
                if (!options.Contains(pair.Key)) options.Add(pair.Key);
            }
            if (options.Count == 0) return;

            // misode 的 StringHead 只按 #[id] 的 registry 决定 <select>；#[dispatcherkey] 只贡献候选
            // （builtin.js:61-77 的 stringMocker → datalist），渲染上就是"可输入 + 候选提示"。
            node.UseSelect = false;
            node.AllowCustomValue = true;
            node.EnumOptionList = new ObservableCollection<EnumMember>();
            foreach (var option in options)
            {
                node.EnumOptionList.Add(new EnumMember
                {
                    Name = option,
                    Value = new MetaValue { LiteralValue = option },
                });
            }
            SelectCurrent(node, node.Json is { Kind: McdocJsonKind.String } ? node.Json.StringValue : null);
        }

        /// <summary>
        /// 建树期落选中项，且**行上显示的值必须等于文档里的值**：文档里的值不在候选内（旧版本写的 /
        /// 手改过的 / 注册表里没有的自定义 id）时把它本身挂成一项并选中；回落 FirstOrDefault 会让
        /// 行上显示**另一个值**，用户以为文档里写的就是那个。静默落项：走 setter 会在建树中途触发
        /// ValueChanged → 整条编辑管线（诊断/依赖重建/物化/写回文本）对这份还没建完的 JSON 再跑一遍。
        /// </summary>
        private static void SelectCurrent(McdocTreeNode node, string current, string match = null)
        {
            if (node?.EnumOptionList is not { Count: > 0 } options) return;
            var selected = current is null
                ? null
                : options.FirstOrDefault(item => item.Name == (match ?? current));
            if (current is not null && selected is null)
            {
                // 这一份是**该节点私有**的（调用点的候选集合本来就按节点新建），插进去不会污染别的节点
                selected = new EnumMember { Name = current, Value = new MetaValue { LiteralValue = current } };
                options.Insert(0, selected);
            }
            node.SetEnumOptionSilently(selected ?? options.FirstOrDefault());
        }

        /// <summary>
        /// 裸 #[id]（没有 registry）：候选只来自类型自身，并允许输入候选外的值。
        /// 对应 spyglass getResourceLocationOptions 的无 registry 分支（builtin.js:18-33）：
        /// enum(string) → 各成员值 lengthen；literal(string) → 该值 lengthen；其它 → 空候选。
        /// </summary>
        private static void ApplyBareId(McdocTreeNode node, McdocType type)
        {
            var options = new List<string>();
            if (type.Kind == McdocTypeKind.Enum)
            {
                foreach (var value in type.EnumValues ?? [])
                {
                    var text = Convert.ToString(value?.Value);
                    if (!string.IsNullOrEmpty(text)) options.Add(McdocIdSpec.LengthenResourceLocation(text));
                }
            }
            else if (type.Kind == McdocTypeKind.Literal && type.LiteralKind == McdocLiteralKind.String)
            {
                var text = Convert.ToString(type.Value);
                if (!string.IsNullOrEmpty(text)) options.Add(McdocIdSpec.LengthenResourceLocation(text));
            }

            // 没有候选时保持纯文本：spyglass 那边是"空池 + allowUnknown"，
            // 界面上同样就是一个自由输入框，不必额外挂一个空下拉（行门禁的不变量：可输入 ⇒ 有候选）
            if (options.Count == 0) return;
            node.UseSelect = false;
            node.AllowCustomValue = true;

            node.EnumOptionList = new ObservableCollection<EnumMember>();
            foreach (var option in options.Distinct())
            {
                node.EnumOptionList.Add(new EnumMember
                {
                    Name = option,
                    Value = new MetaValue { LiteralValue = option },
                });
            }
            var current = node.Json is { Kind: McdocJsonKind.String } ? node.Json.StringValue : null;
            // 候选项是加长过的（minecraft: 前缀），文档里的值可能没写前缀：比对用加长形态，
            // 挂"候选外的值"时仍挂文档原文，行上显示的值才等于 JSON。
            SelectCurrent(node, current, current is null ? null : McdocIdSpec.LengthenResourceLocation(current));
        }

        /// <summary>
        /// 可选字段的下拉第 0 项补「（未设置）」：选中即删键（与 union 分支下拉同一语义）。
        /// **必须在父级写完 <see cref="McdocTreeNode.IsRequired"/> 之后调用** —— 建子级期那个值还是默认 true，
        /// 那时判"是否可选"会恒真（同一个坑见 bool 默认值）。幂等：重复调用只留一个哨兵。
        /// </summary>
        internal static void EnsureUnsetOption(McdocTreeNode node) => EnsureUnsetOption(node, node is not null && !node.IsRequired);

        /// <summary>同上；<paramref name="optional"/> 由调用方显式给（union 分支节点的 IsRequired 没有"可选"含义）。</summary>
        internal static void EnsureUnsetOption(McdocTreeNode node, bool optional)
        {
            if (node?.EnumOptionList is not { Count: > 0 } options) return;
            if (!optional) return;
            if (!McdocUnionSwitch.IsUnsetOption(options[0]))
            {
                options.Insert(0, new EnumMember
                {
                    Name = McdocNodePresenter.UnsetLabel,
                    Value = new MetaValue { LiteralValue = -1 },
                });
            }
            // 文档里没有这个键 → 显示（未设置）；有值就不动选中项（候选外的值不伪造选中）
            if (node.Json is null)
            {
                node.SetEnumOptionSilently(options[0]);
            }
        }

        /// <summary>
        /// 动态键行的键候选：键类型带 #[id] 且注册表取得到值（开放键），或键是闭合枚举（[MobCategory]）时，把键变成下拉。
        /// </summary>
        public static void ApplyKeyOptions(McdocTreeNode row, McdocType keyType, McdocRuntime runtime,
            IReadOnlyCollection<string> exclude = null)
        {
            if (row is null || keyType is null || runtime is null) return;
            var options = KeyOrder(keyType);
            if (options is not { Count: > 0 }) return;

            row.UseKeySelect = true;
            row.KeyOptionList = new ObservableCollection<EnumMember>();
            foreach (var option in options)
            {
                if (exclude is not null && exclude.Contains(option)) continue;
                row.KeyOptionList.Add(new EnumMember
                {
                    Name = option,
                    Value = new MetaValue { LiteralValue = option },
                });
            }
            var current = row.IsMapEntry ? row.FieldName : row.EditingKey;
            if (!string.IsNullOrEmpty(current))
            {
                var selected = row.KeyOptionList.FirstOrDefault(item => string.Equals(item.Name, current, StringComparison.Ordinal));
                if (selected is null && row.IsMapEntry)
                {
                    // 文档里的键可能不在候选表里（旧版本写的 / 手改的），补一条顶上去，别让下拉空着
                    selected = new EnumMember { Name = current, Value = new MetaValue { LiteralValue = current } };
                    row.KeyOptionList.Insert(0, selected);
                }
                row.SelectedKey = selected;
            }
        }

        /// <summary>拆掉指令壳（#[id]、#[since] 之类）取里面的形状。</summary>
        private static McdocType Unwrap(McdocType keyType)
        {
            var shape = keyType;
            for (var depth = 0; shape?.IsDirective == true && shape.Item is not null && depth < 8; depth++)
            {
                shape = shape.Item;
            }
            return shape;
        }

        /// <summary>键候选的排序依据：带 #[id] 的用注册表成员表，闭合枚举键用枚举成员值。</summary>
        private static IReadOnlyList<string> KeyOrder(McdocType keyType)
        {
            if (keyType?.IdMembers is { Count: > 0 } members) return members;
            var shape = Unwrap(keyType);
            if (shape?.Kind != McdocTypeKind.Enum) return null;
            var values = new List<string>();
            foreach (var value in shape.EnumValues ?? [])
            {
                var text = Convert.ToString(value?.Value);
                if (!string.IsNullOrEmpty(text)) values.Add(text);
            }
            return values;
        }

        /// <summary>
        /// 占位行的键下拉预选：从上一次用 ⊕ 起出来的键的下一位开始，取第一个还在候选里的成员（绕回开头）。
        /// 没有记忆就从第一个候选开始 —— 点完添加不该让用户对着一个空下拉（候选刚刚被用掉一项）。
        /// </summary>
        public static void PreselectNextKey(McdocTreeNode row, McdocType keyType, string lastKey)
        {
            if (row?.KeyOptionList is not { Count: > 0 } options) return;
            if (!string.IsNullOrEmpty(row.EditingKey)) return;   // 已经有键就不抢
            var order = KeyOrder(keyType);
            var start = 0;
            if (order is { Count: > 0 } && !string.IsNullOrEmpty(lastKey))
            {
                for (var i = 0; i < order.Count; i++)
                {
                    if (!string.Equals(order[i], lastKey, StringComparison.Ordinal)) continue;
                    start = (i + 1) % order.Count;
                    break;
                }
            }
            var pick = options[0];
            if (order is { Count: > 0 })
            {
                for (var step = 0; step < order.Count; step++)
                {
                    var name = order[(start + step) % order.Count];
                    var hit = row.KeyOptionList.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.Ordinal));
                    if (hit is null) continue;
                    pick = hit;
                    break;
                }
            }
            row.SelectedKey = pick;
        }

        /// <summary>
        /// 从属性里取 #[id] 规格（简单形式 / 复合形式 / #[tree{registry}]）。
        /// 引用/别名（例如 `type PersistentDataComponent = #[id(registry=...)] string`）上没有属性，
        /// 必须按符号表解析出目标类型再看，否则 `DataComponentPatch` 这类"动态键用别名声明"的字段拿不到候选。
        /// </summary>
        public static McdocIdSpec Resolve(McdocType type, McdocRuntime runtime = null)
        {
            var direct = ResolveDirect(type);
            if (direct is not null || type?.Kind != McdocTypeKind.Reference || runtime?.Symbols is null) return direct;
            try
            {
                var resolved = McdocSimplifier.Simplify(type, new McdocSimplifyContext
                {
                    Runtime = runtime,
                    Node = McdocValueNode.Of(new McdocRuntimeNode(null, McdocType.Any())),
                }).TypeDef;
                return resolved is null || ReferenceEquals(resolved, type) ? null : ResolveDirect(resolved);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static McdocIdSpec ResolveDirect(McdocType type)
        {
            foreach (var attribute in type.Attributes ?? [])
            {
                // 裸 #[id]（Value == null）也要走到：它决定 mapType 与"可输入 + 候选"形态
                var spec = McdocIdSpec.FromAttribute(attribute);
                if (spec?.Bare == true) return spec;
                // registry 之外，prefix / exclude 这类"取值形态"配置也要交出去（无候选，但参与比较与诊断）
                if (!string.IsNullOrEmpty(spec?.Registry) || !string.IsNullOrEmpty(spec?.Prefix)) return spec;

                if (attribute.Name is "tree"
                    && attribute.Value.Tree is { } tree
                    && tree.TryGetValue("registry", out var registry)
                    && !string.IsNullOrEmpty(registry?.AsString()))
                {
                    return new McdocIdSpec { Registry = registry.AsString().Trim('"') };
                }
            }
            return null;
        }
    }
}
