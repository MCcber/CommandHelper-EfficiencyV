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
    }
}
