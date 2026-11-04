// misode 的 isSelectRegistry 白名单（McdocHelpers.ts:326-402 逐条抄录，67 条）。
// 语义：McdocRenderer.tsx:163 `const isSelect = idRegistry && isSelectRegistry(idRegistry, version)` ——
// 只有白名单里的注册表才渲染 <select>；其余是 <input list=datalist>（自由文本 + 候选提示）。
// 注意：misode 的集合里**没有** item / block / advancement —— 那些字段在 misode 上是自由文本。
using System;
using System.Collections.Generic;

namespace CBHK.Utility.Data.Mcdoc
{
    public static class McdocSelectRegistries
    {
        /// <summary>selectRegistries（严格照抄，不许加料）: McdocHelpers.ts:326-393</summary>
        private static readonly HashSet<string> Registries = new(StringComparer.Ordinal)
        {
            "block_predicate_type",
            "chunk_status",
            "consume_effect_type",
            "context_float_provider_type",
            "context_int_provider_type",
            "creative_mode_tab",
            "data_component_predicate_type",
            "data_component_type",
            "dialog_action_type",
            "dialog_body_type",
            "dialog_type",
            "enchantment_effect_component_type",
            "enchantment_entity_effect_type",
            "enchantment_level_based_value_type",
            "enchantment_location_based_effect_type",
            "enchantment_provider_type",
            "enchantment_value_effect_type",
            "entity_sub_predicate_type",
            "float_provider_type",
            "frog_variant",
            "height_provider_type",
            "input_control_type",
            "int_provider_type",
            "item_sub_predicate_type",
            "loot_condition_type",
            "loot_function_type",
            "loot_nbt_provider_type",
            "loot_number_provider_type",
            "loot_pool_entry_type",
            "loot_score_provider_type",
            "map_decoration_type",
            "number_format_type",
            "pos_rule_test",
            "position_source_type",
            "recipe_book_category",
            "recipe_display",
            "recipe_serializer",
            "recipe_type",
            "rule_block_entity_modifier",
            "rule_test",
            "slot_display",
            "slot_source_type",
            "spawn_condition_type",
            "stat_type",
            "test_instance_type",
            "test_environment_definition_type",
            "trigger_type",
            "worldgen/biome_source",
            "worldgen/block_state_provider_type",
            "worldgen/carver_type",
            "worldgen/chunk_generator",
            "worldgen/density_function_type",
            "worldgen/feature_type",
            "worldgen/feature_size_type",
            "worldgen/foliage_placer_type",
            "worldgen/material_condition_type",
            "worldgen/material_rule_type",
            "worldgen/placement_modifier_type",
            "worldgen/pool_alias_binding",
            "worldgen/root_placer_type",
            "worldgen/structure_placement",
            "worldgen/structure_pool_element",
            "worldgen/structure_processor",
            "worldgen/structure_type",
            "worldgen/tree_decorator_type",
            "worldgen/trunk_placer_type",
        };

        public static int Count => Registries.Count;

        public static bool Contains(string registry)
            => !string.IsNullOrEmpty(registry) && Registries.Contains(registry);

        /// <summary>
        /// 对应 misode 的 isSelectRegistry(registry, version)（McdocHelpers.ts:395-402）。
        /// 它开头的 `if (!checkVersion(version, '26.3') &amp;&amp; [])` 分支条件是空数组（恒 false），
        /// 原样照抄成"该分支不成立"，不自创版本语义。
        /// </summary>
        public static bool IsSelect(string registry, string version) => Contains(registry);
    }
}
