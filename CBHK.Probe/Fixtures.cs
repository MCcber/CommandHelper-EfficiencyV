using System.Collections.Generic;
using System.Text;
using CBHK.Utility.Data.Mcdoc;

namespace CBHK.Probe
{
    /// <summary>
    /// 确定性夹具：合成 schema + 内联 JSON，不依赖语言服务器 / Registries / 界面。
    /// 懒建与分页都由调用方关掉（门禁必须"行数 == 节点数"）。
    /// </summary>
    internal static class Fixtures
    {
        public const int NestedEntries = 200;
        public const int ListItems = 60;

        public static McdocType RootType()
        {
            var root = McdocType.Create(McdocTypeKind.Struct);
            root.Fields =
            [
                Static("format_version", McdocType.Create(McdocTypeKind.Int)),
                Static("alpha", McdocType.Create(McdocTypeKind.String), optional: true),
                Static("nested", NestedType()),
                Static("list", ListType())
            ];
            return root;
        }

        private static McdocStructField Static(string name, McdocType type, bool optional = false)
            => new() { Name = name, Key = McdocType.LiteralString(name), Type = type, Optional = optional };

        private static McdocStructField Map(McdocType valueType)
            => new() { Key = McdocType.Create(McdocTypeKind.String), Type = valueType };

        private static McdocType NestedType()
        {
            var type = McdocType.Create(McdocTypeKind.Struct);
            type.Fields =
            [
                Static("one", McdocType.Create(McdocTypeKind.Int)),
                Static("two", McdocType.Create(McdocTypeKind.Int)),
                Static("three", McdocType.Create(McdocTypeKind.Int)),
                Map(McdocType.Create(McdocTypeKind.Int))
            ];
            return type;
        }

        private static McdocType ListType()
        {
            var item = McdocType.Create(McdocTypeKind.Struct);
            item.Fields =
            [
                Static("id", McdocType.Create(McdocTypeKind.String)),
                Static("weight", McdocType.Create(McdocTypeKind.Double)),
                Static("flag", McdocType.Create(McdocTypeKind.Boolean)),
                Static("label", McdocType.Create(McdocTypeKind.String))
            ];
            var list = McdocType.Create(McdocTypeKind.List);
            list.Item = item;
            return list;
        }

        /// <summary>列表第 i 项的文本块（不含分隔逗号），编辑脚本按它做增删。</summary>
        public static string ItemBlock(int index)
            => "    {\n"
               + "      \"id\": \"i" + index + "\",\n"
               + "      \"weight\": 1.5,\n"
               + "      \"flag\": true,\n"
               + "      \"label\": \"L" + index + "\"\n"
               + "    }";

        public static string Text()
        {
            var builder = new StringBuilder();
            builder.Append("{\n");
            builder.Append("  \"format_version\": 1,\n");
            builder.Append("  \"alpha\": \"alpha-value\",\n");
            builder.Append("  \"nested\": {\n");
            builder.Append("    \"one\": 1,\n");
            builder.Append("    \"two\": 2,\n");
            builder.Append("    \"three\": 3");
            for (var index = 0; index < NestedEntries; index++)
            {
                builder.Append(",\n    \"k").Append(index).Append("\": ").Append(index);
            }
            builder.Append("\n  },\n");
            builder.Append("  \"list\": [\n");
            for (var index = 0; index < ListItems; index++)
            {
                builder.Append(ItemBlock(index));
                builder.Append(index == ListItems - 1 ? "\n" : ",\n");
            }
            builder.Append("  ]\n");
            builder.Append("}\n");
            return builder.ToString();
        }

        /// <summary>必填枚举用例：必填结构 → 里面一个必填枚举（带闭合候选）；靠它的兄弟节点按值索引。</summary>
        public static McdocType RequiredEnumHostType()
        {
            var level = McdocType.Create(McdocTypeKind.Enum);
            level.EnumKind = McdocTypeKind.String;
            level.EnumValues =
            [
                new McdocEnumValue { Identifier = "one", Value = "one" },
                new McdocEnumValue { Identifier = "two", Value = "two" }
            ];
            var inner = McdocType.Create(McdocTypeKind.Struct);
            inner.Fields = [ new McdocStructField { Name = "level", Key = McdocType.LiteralString("level"), Type = level } ];
            var root = McdocType.Create(McdocTypeKind.Struct);
            root.Fields =
            [
                new McdocStructField { Name = "mode", Key = McdocType.LiteralString("mode"), Type = inner },
                new McdocStructField { Name = "tail", Key = McdocType.LiteralString("tail"), Type = McdocType.Create(McdocTypeKind.Int), Optional = true }
            ];
            return root;
        }

        /// <summary>贴近现场的形状：criteria → conditions → location(列表) → 条目 → condition(union)。</summary>
        public static McdocType CriteriaLikeHostType()
        {
            var first = McdocType.Create(McdocTypeKind.Struct);
            first.Fields = [ Static("a", McdocType.Create(McdocTypeKind.Int), optional: true) ];
            var second = McdocType.Create(McdocTypeKind.Struct);
            second.Fields = [ Static("b", McdocType.Create(McdocTypeKind.Int), optional: true) ];
            var conditionUnion = McdocType.Create(McdocTypeKind.Union);
            conditionUnion.Members = [ first, second ];
            var entry = McdocType.Create(McdocTypeKind.Struct);
            entry.Fields = [ new McdocStructField { Name = "condition", Key = McdocType.LiteralString("condition"), Type = conditionUnion, Optional = true } ];
            var location = McdocType.Create(McdocTypeKind.List);
            location.Item = entry;
            var conditions = McdocType.Create(McdocTypeKind.Struct);
            conditions.Fields = [ new McdocStructField { Name = "location", Key = McdocType.LiteralString("location"), Type = location, Optional = true } ];
            var criterion = McdocType.Create(McdocTypeKind.Struct);
            criterion.Fields = [ new McdocStructField { Name = "conditions", Key = McdocType.LiteralString("conditions"), Type = conditions, Optional = true } ];
            var criteria = McdocType.Create(McdocTypeKind.Struct);
            criteria.Fields = [ new McdocStructField { Key = McdocType.Create(McdocTypeKind.String), Type = criterion } ];
            var root = McdocType.Create(McdocTypeKind.Struct);
            root.Fields = [ new McdocStructField { Name = "criteria", Key = McdocType.LiteralString("criteria"), Type = criteria } ];
            return root;
        }

        /// <summary>一支能解、一支解不出来（声明是 Reference 但符号表没有）—— 等价 block_state_property 那种"要靠兄弟键才解得出"的成员。</summary>
        public static McdocType UnionWithUnresolvedHostType()
        {
            var first = McdocType.Create(McdocTypeKind.Struct);
            first.Fields = [ Static("a", McdocType.Create(McdocTypeKind.Int), optional: true) ];
            var broken = McdocType.Create(McdocTypeKind.Reference);
            broken.Path = "::missing_module::NotThere";
            var union = McdocType.Create(McdocTypeKind.Union);
            union.Members = [ first, broken ];
            var root = McdocType.Create(McdocTypeKind.Struct);
            root.Fields = [ Static("u", union) ];
            return root;
        }

        /// <summary>列表项里挂 union：等价 conditions.location[i].condition（Entry 的身份来自列表项）。</summary>
        public static McdocType EntryWithUnionHostType()
        {
            var first = McdocType.Create(McdocTypeKind.Struct);
            first.Fields = [ Static("a", McdocType.Create(McdocTypeKind.Int), optional: true) ];
            var second = McdocType.Create(McdocTypeKind.Struct);
            second.Fields = [ Static("b", McdocType.Create(McdocTypeKind.Int), optional: true) ];
            var union = McdocType.Create(McdocTypeKind.Union);
            union.Members = [ first, second ];
            var item = McdocType.Create(McdocTypeKind.Struct);
            item.Fields = [ new McdocStructField { Name = "condition", Key = McdocType.LiteralString("condition"), Type = union, Optional = true } ];
            var list = McdocType.Create(McdocTypeKind.List);
            list.Item = item;
            var root = McdocType.Create(McdocTypeKind.Struct);
            root.Fields = [ Static("items", list) ];
            return root;
        }

        /// <summary>union 稳定性用例：两个成员都是"全可选结构"，空对象/单键都能同时命中 → 按值猜必然并列。</summary>
        public static McdocType UnionHostType()
        {
            var first = McdocType.Create(McdocTypeKind.Struct);
            first.Fields = [ Static("a", McdocType.Create(McdocTypeKind.Int), optional: true) ];
            var second = McdocType.Create(McdocTypeKind.Struct);
            second.Fields = [ Static("b", McdocType.Create(McdocTypeKind.Int), optional: true) ];
            var union = McdocType.Create(McdocTypeKind.Union);
            union.Members = [ first, second ];
            var root = McdocType.Create(McdocTypeKind.Struct);
            root.Fields = [ Static("u", union) ];
            return root;
        }

        /// <summary>重建语义用例：一个 map（动态键条目）与一个列表（列表项），两种"行身份"。</summary>
        public static McdocType EntryHostType()
        {
            var mapType = McdocType.Create(McdocTypeKind.Struct);
            mapType.Fields = [ Map(McdocType.Create(McdocTypeKind.Int)) ];
            var itemType = McdocType.Create(McdocTypeKind.Struct);
            itemType.Fields = [ Static("z", McdocType.Create(McdocTypeKind.Int)) ];
            var listType = McdocType.Create(McdocTypeKind.List);
            listType.Item = itemType;
            var root = McdocType.Create(McdocTypeKind.Struct);
            root.Fields =
            [
                Static("a", McdocType.Create(McdocTypeKind.Int)),
                Static("map", mapType),
                Static("list", listType)
            ];
            return root;
        }

        /// <summary>遗留键用例的宿主 schema：只声明 a，其余三个键在当前版本都不存在。</summary>
        public static McdocType LegacyHostType()
        {
            var nested = McdocType.Create(McdocTypeKind.Struct);
            nested.Fields = [ Static("b1", McdocType.Create(McdocTypeKind.Int)) ];
            var root = McdocType.Create(McdocTypeKind.Struct);
            root.Fields =
            [
                Static("a", McdocType.Create(McdocTypeKind.Int)),
                Static("b", nested)
            ];
            return root;
        }

        /// <summary>同一份文档在"新版本"里的 schema：keepObject / keepArray 被声明，keepScalar 被声明成结构（形状不符）。</summary>
        public static McdocType LegacyHostTypeV2()
        {
            var objectType = McdocType.Create(McdocTypeKind.Struct);
            objectType.Fields =
            [
                Static("x", McdocType.Create(McdocTypeKind.Int)),
                Static("y", McdocType.Create(McdocTypeKind.Int), optional: true)
            ];
            var arrayItem = McdocType.Create(McdocTypeKind.Struct);
            arrayItem.Fields = [ Static("z", McdocType.Create(McdocTypeKind.Int)) ];
            var arrayType = McdocType.Create(McdocTypeKind.List);
            arrayType.Item = arrayItem;
            var scalarAsStruct = McdocType.Create(McdocTypeKind.Struct);
            scalarAsStruct.Fields = [ Static("n", McdocType.Create(McdocTypeKind.Int), optional: true) ];

            var root = McdocType.Create(McdocTypeKind.Struct);
            root.Fields =
            [
                Static("a", McdocType.Create(McdocTypeKind.Int)),
                Static("keepObject", objectType),
                Static("keepArray", arrayType),
                Static("keepScalar", scalarAsStruct)
            ];
            return root;
        }

        /// <summary>§七-1：criterion 形态——map 的值类是"只由 struct 组成的联合"。</summary>
        public static McdocType CriterionRootType()
        {
            var criteria = McdocType.Create(McdocTypeKind.Struct);
            criteria.Fields = [ Map(CriterionUnion()) ];

            var root = McdocType.Create(McdocTypeKind.Struct);
            root.Fields = [ Static("criteria", criteria) ];
            return root;
        }

        private static McdocType CriterionUnion()
        {
            var members = new List<McdocType>();
            // 三支结构：其中一支带必填键，另外两支只有可选键（空对象 {} 在"有必填"的那支上才算不匹配）
            var first = McdocType.Create(McdocTypeKind.Struct);
            first.Fields =
            [
                Static("type", McdocType.LiteralString("a")),
                Static("note", McdocType.Create(McdocTypeKind.String), optional: true)
            ];
            members.Add(first);

            var second = McdocType.Create(McdocTypeKind.Struct);
            second.Fields = [ Static("count", McdocType.Create(McdocTypeKind.Int), optional: true) ];
            members.Add(second);

            var third = McdocType.Create(McdocTypeKind.Struct);
            third.Fields = [ Static("flag", McdocType.Create(McdocTypeKind.Boolean), optional: true) ];
            members.Add(third);

            var union = McdocType.Create(McdocTypeKind.Union);
            union.Members = members;
            return union;
        }

        // ── 现场形状（截图 1/2）：condition 判别键 + 同层 spread 调度器 + block 的动态索引兄弟 ──────

        /// <summary>condition 调度器空间名：与真 schema 的 minecraft:loot_condition 同形（成员键不带前缀）。</summary>
        public const string ConditionRegistry = "mcdoc:probe_condition";

        /// <summary>
        /// criteria(map) → tset → conditions → location(列表) → 条目 → condition(必选枚举 + ...调度器[[condition]])。
        /// 与既有夹具的差别只有一处，但就是现场那一处：分支字段靠**同层兄弟键**做动态索引。
        /// </summary>
        public static McdocType ConditionRootType()
        {
            var location = McdocType.Create(McdocTypeKind.List);
            location.Item = ConditionEntryType();
            var conditions = McdocType.Create(McdocTypeKind.Struct);
            conditions.Fields = [ Static("location", location, optional: true) ];
            var criterion = McdocType.Create(McdocTypeKind.Struct);
            criterion.Fields = [ Static("conditions", conditions, optional: true) ];
            var criteria = McdocType.Create(McdocTypeKind.Struct);
            criteria.Fields = [ Map(criterion) ];
            var root = McdocType.Create(McdocTypeKind.Struct);
            root.Fields = [ Static("criteria", criteria) ];
            return root;
        }

        private static McdocType ConditionEntryType()
        {
            var entry = McdocType.Create(McdocTypeKind.Struct);
            var spread = McdocType.Create(McdocTypeKind.Dispatcher);
            spread.Registry = ConditionRegistry;
            spread.Indices = [ McdocIndex.Dynamic(new List<string> { "condition" }) ];
            entry.Fields =
            [
                new McdocStructField
                {
                    Name = "condition", Key = McdocType.LiteralString("condition"),
                    Type = IdString(ConditionRegistry)
                },
                new McdocStructField { IsSpread = true, Type = spread }
            ];
            return entry;
        }

        /// <summary>把 condition 两个成员与 block_states 真资产喂进索引；返回注册条数（0 = 资产缺失）。</summary>
        public static int RegisterConditionDispatchers(McdocTypeIndex index, string blockStateDir)
        {
            var terms = McdocType.Create(McdocTypeKind.List);
            terms.Item = ConditionEntryType();
            var allOf = McdocType.Create(McdocTypeKind.Struct);
            allOf.Fields = [ new McdocStructField { Name = "terms", Key = McdocType.LiteralString("terms"), Type = terms } ];

            var properties = McdocType.Create(McdocTypeKind.Dispatcher);
            properties.Registry = McdocBlockStates.Registry;
            properties.Indices = [ McdocIndex.Dynamic(new List<string> { "block" }) ];
            var blockStateProperty = McdocType.Create(McdocTypeKind.Struct);
            blockStateProperty.Fields =
            [
                new McdocStructField { Name = "block", Key = McdocType.LiteralString("block"), Type = IdString("block") },
                new McdocStructField { Name = "properties", Key = McdocType.LiteralString("properties"), Type = properties, Optional = true }
            ];

            var registered = McdocBlockStates.Register(index, "1.20.5", blockStateDir) + 2;
            index.RegisterDispatch(ConditionRegistry, "all_of", allOf);
            index.RegisterDispatch(ConditionRegistry, "block_state_property", blockStateProperty);
            return registered;
        }

        /// <summary>#[id=registry] 的 string：候选走 IdSpec → IdSpaces（调度器成员或注册表），不是写死的枚举。</summary>
        private static McdocType IdString(string registry)
        {
            var type = McdocType.Create(McdocTypeKind.String);
            type.Attributes = [ new McdocAttribute { Name = "id", Value = McdocAttrValue.Of(registry) } ];
            return type;
        }
        // ── 真形状：IntProvider<T> = (T | struct { type: #[id]; ...int_provider[[type]]<T> }) ──────
        // dimension_type.monster_spawn_light_level 就是这一句（泛型 dispatcher 的 spread 落在 union 的一支里）。

        public const string IntProviderRegistry = "mcdoc:probe_int_provider";

        public static McdocType DimensionLikeRootType()
        {
            var value = McdocType.Create(McdocTypeKind.Int);
            value.Range = new McdocRange { Min = 0, Max = 15 };

            var dispatcher = McdocType.Create(McdocTypeKind.Dispatcher);
            dispatcher.Registry = IntProviderRegistry;
            dispatcher.Indices = [ McdocIndex.Dynamic(new List<string> { "type" }) ];
            var indexed = McdocType.Create(McdocTypeKind.Indexed);
            indexed.Item = dispatcher;
            var concrete = McdocType.Create(McdocTypeKind.Concrete);
            concrete.Items = [ value ];
            concrete.Item = indexed;

            var provider = McdocType.Create(McdocTypeKind.Struct);
            provider.Fields =
            [
                new McdocStructField { Name = "type", Key = McdocType.LiteralString("type"), Type = IdString(IntProviderRegistry) },
                new McdocStructField { IsSpread = true, Type = concrete }
            ];
            var union = McdocType.Create(McdocTypeKind.Union);
            union.Members = [ value, provider ];

            var root = McdocType.Create(McdocTypeKind.Struct);
            root.Fields = [ Static("monster_spawn_light_level", union) ];
            return root;
        }

        /// <summary>dispatch minecraft:int_provider[uniform]&lt;T&gt; to struct { min_inclusive: T, max_inclusive: T }。</summary>
        public static int RegisterIntProviderDispatchers(McdocTypeIndex index)
        {
            var typeArg = new McdocType { Kind = McdocTypeKind.Reference, Path = "T" };
            var uniform = McdocType.Create(McdocTypeKind.Struct);
            uniform.Fields = [ Static("min_inclusive", typeArg), Static("max_inclusive", typeArg) ];
            var template = McdocType.Create(McdocTypeKind.Template);
            template.TypeParams = new List<string> { "T" };
            template.Item = uniform;

            var constant = McdocType.Create(McdocTypeKind.Struct);
            constant.Fields = [ Static("value", typeArg) ];
            var constantTemplate = McdocType.Create(McdocTypeKind.Template);
            constantTemplate.TypeParams = new List<string> { "T" };
            constantTemplate.Item = constant;

            index.RegisterDispatch(IntProviderRegistry, "uniform", template);
            index.RegisterDispatch(IntProviderRegistry, "constant", constantTemplate);
            return 2;
        }

        // ── 泛型实参自带动态索引：NonEmptyWeightedList<T> / WeightedEntry<T> 的真形状 ──────────
        // 现场：distribution: NonEmptyWeightedList<IntProvider<T>>，实参 IntProvider 自己带 [[type]]。

        public const string WeightedEntryPath = "::probe::WeightedEntry";
        public const string WeightedListPath = "::probe::NonEmptyWeightedList";
        public const string IntProviderPath = "::probe::IntProvider";
        public const string UniformProviderPath = "::probe::UniformIntProvider";

        /// <summary>type UniformIntProvider&lt;T&gt; = struct { min_inclusive: T, max_inclusive: T }。</summary>
        private static McdocType UniformProviderTemplate()
        {
            var typeArg = new McdocType { Kind = McdocTypeKind.Reference, Path = "T" };
            var shape = McdocType.Create(McdocTypeKind.Struct);
            shape.Fields = [ Static("min_inclusive", typeArg), Static("max_inclusive", typeArg) ];
            var template = McdocType.Create(McdocTypeKind.Template);
            template.TypeParams = new List<string> { "T" };
            template.Item = shape;
            return template;
        }

        /// <summary>dispatch ...[uniform]&lt;T&gt; to ( #[until="1.20.5"] struct{value: Alias&lt;T&gt;} | #[since="1.20.5"] Alias&lt;T&gt; )。</summary>
        private static McdocType UniformAliasMember()
        {
            var typeArg = new McdocType { Kind = McdocTypeKind.Reference, Path = "T" };
            var legacyInner = McdocType.Create(McdocTypeKind.Concrete);
            legacyInner.Items = [ new McdocType { Kind = McdocTypeKind.Reference, Path = "T" } ];
            legacyInner.Item = new McdocType { Kind = McdocTypeKind.Reference, Path = UniformProviderPath };
            var legacy = McdocType.Create(McdocTypeKind.Struct);
            legacy.Fields = [ Static("value", legacyInner) ];

            var alias = McdocType.Create(McdocTypeKind.Concrete);
            alias.Items = [ new McdocType { Kind = McdocTypeKind.Reference, Path = "T" } ];
            alias.Item = new McdocType { Kind = McdocTypeKind.Reference, Path = UniformProviderPath };

            var union = McdocType.Create(McdocTypeKind.Union);
            union.Members = [ legacy, alias ];
            var template0 = McdocType.Create(McdocTypeKind.Template);
            template0.TypeParams = new List<string> { "T" };
            template0.Item = union;
            return template0;

            // 先只复现"目标是泛型别名"这一跳（版本过滤那层单独再说，避免一次改两个变量）
            var template = McdocType.Create(McdocTypeKind.Template);
            template.TypeParams = new List<string> { "T" };
            template.Item = alias;
            return template;
        }

        /// <summary>value: IntProvider&lt;int @ 0..15&gt; —— 其中 weighted 支带 distribution: NonEmptyWeightedList&lt;IntProvider&lt;…&gt;&gt;。</summary>
        public static McdocType GenericArgRootType()
        {
            var root = McdocType.Create(McdocTypeKind.Struct);
            root.Fields = [ Static("value", IntProviderUsage()) ];
            return root;
        }

        /// <summary>带泛型列表字段的成员（weighted_list 的形状）。</summary>
        public static McdocType GenericArgDistributionType()
        {
            var distribution = McdocType.Create(McdocTypeKind.Concrete);
            distribution.Items = [ IntProviderUsage() ];
            distribution.Item = new McdocType { Kind = McdocTypeKind.Reference, Path = WeightedListPath };
            var member = McdocType.Create(McdocTypeKind.Struct);
            member.Fields = [ Static("distribution", distribution) ];
            return member;
        }

        /// <summary>IntProvider&lt;int @ 0..15&gt; 的用法：Concrete{ Items=[…], Item=Reference }。</summary>
        private static McdocType IntProviderUsage()
        {
            var value = McdocType.Create(McdocTypeKind.Int);
            value.Range = new McdocRange { Min = 0, Max = 15 };
            var usage = McdocType.Create(McdocTypeKind.Concrete);
            usage.Items = [ value ];
            usage.Item = new McdocType { Kind = McdocTypeKind.Reference, Path = IntProviderPath };
            return usage;
        }

        /// <summary>type IntProvider&lt;T&gt; = ( T | struct { type: #[id]; ...dispatcher[[type]]&lt;T&gt; } )。</summary>
        private static McdocType IntProviderTemplate()
        {
            var typeArg = new McdocType { Kind = McdocTypeKind.Reference, Path = "T" };
            var dispatcher = McdocType.Create(McdocTypeKind.Dispatcher);
            dispatcher.Registry = IntProviderRegistry;
            dispatcher.Indices = [ McdocIndex.Dynamic(new List<string> { "type" }) ];
            var indexed = McdocType.Create(McdocTypeKind.Indexed);
            indexed.Item = dispatcher;
            var spread = McdocType.Create(McdocTypeKind.Concrete);
            spread.Items = [ typeArg ];
            spread.Item = indexed;

            var provider = McdocType.Create(McdocTypeKind.Struct);
            provider.Fields =
            [
                new McdocStructField { Name = "type", Key = McdocType.LiteralString("type"), Type = IdString(IntProviderRegistry) },
                new McdocStructField { IsSpread = true, Type = spread }
            ];
            var union = McdocType.Create(McdocTypeKind.Union);
            union.Members = [ typeArg, provider ];
            var template = McdocType.Create(McdocTypeKind.Template);
            template.TypeParams = new List<string> { "T" };
            template.Item = union;
            return template;
        }

        /// <summary>登记 WeightedEntry&lt;T&gt; = struct { weight: int, data: T } 与 NonEmptyWeightedList&lt;T&gt; = [WeightedEntry&lt;T&gt;]。</summary>
        public static void RegisterGenericArgTypeDefs(McdocTypeIndex index)
        {
            var typeArg = new McdocType { Kind = McdocTypeKind.Reference, Path = "T" };

            var entry = McdocType.Create(McdocTypeKind.Struct);
            entry.Fields = [ Static("weight", McdocType.Create(McdocTypeKind.Int)), Static("data", typeArg) ];
            var entryTemplate = McdocType.Create(McdocTypeKind.Template);
            entryTemplate.TypeParams = new List<string> { "T" };
            entryTemplate.Item = entry;
            index.RegisterTypeDef(WeightedEntryPath, entryTemplate);

            var entryConcrete = McdocType.Create(McdocTypeKind.Concrete);
            entryConcrete.Items = [ new McdocType { Kind = McdocTypeKind.Reference, Path = "T" } ];
            entryConcrete.Item = new McdocType { Kind = McdocTypeKind.Reference, Path = WeightedEntryPath };
            var list = McdocType.Create(McdocTypeKind.List);
            list.Item = entryConcrete;
            var listTemplate = McdocType.Create(McdocTypeKind.Template);
            listTemplate.TypeParams = new List<string> { "T" };
            listTemplate.Item = list;
            index.RegisterTypeDef(WeightedListPath, listTemplate);

            index.RegisterTypeDef(IntProviderPath, IntProviderTemplate());
            RegisterIntProviderDispatchers(index);
            // 真 schema 里 uniform/biased_to_bottom 的目标是**泛型别名**：
            //   type UniformIntProvider<T> = struct { min_inclusive: T, max_inclusive: T }
            //   dispatch ...[uniform]<T> to ( #[until] struct{value: Alias<T>} | #[since] Alias<T> )
            // 这里照抄这个"别名 + 版本过滤后的 union"形状，用来复现真 schema 里那一层。
            index.RegisterTypeDef(UniformProviderPath, UniformProviderTemplate());
            index.RegisterDispatch(IntProviderRegistry, "weighted", GenericArgDistributionType());
            index.RegisterDispatch(IntProviderRegistry, "uniform_alias", UniformAliasMember());
        }

        // ── 现场形状（bug 1/3）：必填描别键的代数默认值是空串 → 同层 spread 解成 %none ──────
        // 与真 schema java/data/loot/mod.mcdoc:124-130 同形：type 是 #[id="<space>_type"] string，
        // 同层 ...dispatch[[type]] 靠它做动态索引。两者差的只有"空串算不算没有值"。

        public const string LootEntryRegistry = "mcdoc:probe_loot_entry";
        public const string LootEntryTypeRegistry = "mcdoc:probe_loot_entry_type";

        /// <summary>struct { entries: [ struct { type: #[id="…_type"] string, ...dispatch[[type]] } ] }。</summary>
        public static McdocType LootEntryRootType()
        {
            var entries = McdocType.Create(McdocTypeKind.List);
            entries.Item = LootEntryShape();
            // 刻意多包一层：条目落在 depth 3，正是"懒建 + 投影首次展开才建出来"的那一档，
            // 也就是界面里"结构变更闸门内建出子级"的现场。
            var pool = McdocType.Create(McdocTypeKind.Struct);
            pool.Fields = [ Static("entries", entries) ];
            var root = McdocType.Create(McdocTypeKind.Struct);
            root.Fields = [ Static("pool", pool) ];
            return root;
        }

        private static McdocType LootEntryShape()
        {
            var spread = McdocType.Create(McdocTypeKind.Dispatcher);
            spread.Registry = LootEntryRegistry;
            spread.Indices = [ McdocIndex.Dynamic(new List<string> { "type" }) ];
            var entry = McdocType.Create(McdocTypeKind.Struct);
            entry.Fields =
            [
                new McdocStructField
                {
                    Name = "type", Key = McdocType.LiteralString("type"),
                    Type = IdString(LootEntryTypeRegistry)
                },
                new McdocStructField { IsSpread = true, Type = spread }
            ];
            return entry;
        }

        /// <summary>两个调度器成员各带一个"只有自己才有"的字段，用来断言 spread 真的解出来了。</summary>
        public static int RegisterLootEntryDispatchers(McdocTypeIndex index)
        {
            var alpha = McdocType.Create(McdocTypeKind.Struct);
            alpha.Fields = [ Static("alpha_value", McdocType.Create(McdocTypeKind.Int)) ];
            var beta = McdocType.Create(McdocTypeKind.Struct);
            beta.Fields = [ Static("beta_value", McdocType.Create(McdocTypeKind.Int)) ];
            index.RegisterDispatch(LootEntryRegistry, "alpha", alpha);
            index.RegisterDispatch(LootEntryRegistry, "beta", beta);
            return 2;
        }

    }
}
