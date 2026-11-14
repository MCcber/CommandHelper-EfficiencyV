using CBHK.Interface.Component.Recipe;
using CBHK.Model.Data;
using System;
using System.Collections.Generic;

namespace CBHK.ViewModel.Component.Recipe.Strategy
{
    public sealed class RecipeStrategyRegistry
    {
        private readonly Dictionary<RecipeType, IRecipeStrategy> byKind = [];
        private readonly Dictionary<string, IRecipeStrategy> byUid = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, IRecipeStrategy> byJsonType = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<IRecipeStrategy> all = [];

        public IReadOnlyList<IRecipeStrategy> All => all;

        public void Register(IRecipeStrategy strategy)
        {
            byKind[strategy.Kind] = strategy;
            byUid[strategy.Uid] = strategy;
            foreach (string jsonType in strategy.JsonTypes)
                byJsonType[jsonType] = strategy;
            all.Add(strategy);
        }

        public IRecipeStrategy Get(RecipeType kind)
        {
            return byKind.TryGetValue(kind, out IRecipeStrategy strategy) ? strategy : null;
        }

        /// <summary>
        /// 按菜单项 Uid 取策略，未知返回 null
        /// </summary>
        public IRecipeStrategy GetByUid(string uid)
        {
            if (string.IsNullOrWhiteSpace(uid))
                return null;
            return byUid.TryGetValue(uid, out IRecipeStrategy strategy) ? strategy : null;
        }

        /// <summary>
        /// 按 minecraft 配方类型串取策略，统一去掉 minecraft: 前缀
        /// </summary>
        public bool TryGetByJsonType(string jsonType, out IRecipeStrategy strategy)
        {
            strategy = null;
            if (string.IsNullOrWhiteSpace(jsonType))
                return false;
            string key = jsonType.Replace("minecraft:", "").Trim();
            return byJsonType.TryGetValue(key, out strategy);
        }

        public static RecipeStrategyRegistry Create()
        {
            RecipeStrategyRegistry registry = new();
            registry.Register(new CraftingTableStrategy());
            registry.Register(new FurnaceStrategy());
            registry.Register(new SmokerStrategy());
            registry.Register(new BlastFurnaceStrategy());
            registry.Register(new CampfireStrategy());
            registry.Register(new SmithingTableStrategy());
            registry.Register(new StonecutterStrategy());
            return registry;
        }
    }
}
