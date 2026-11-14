using CBHK.Interface.Component.Recipe;
using System.Collections.Generic;

namespace CBHK.ViewModel.Component.Recipe.Strategy
{
    public sealed class SlotDropStrategyRegistry
    {
        private readonly Dictionary<RecipeSlotRole, ISlotDropStrategy> strategies = [];

        public void Register(RecipeSlotRole role, ISlotDropStrategy strategy)
        {
            strategies[role] = strategy;
        }

        public ISlotDropStrategy Get(RecipeSlotRole role)
        {
            return strategies.TryGetValue(role, out ISlotDropStrategy strategy) ? strategy : strategies[RecipeSlotRole.Material];
        }

        public static SlotDropStrategyRegistry Create()
        {
            SlotDropStrategyRegistry registry = new();
            registry.Register(RecipeSlotRole.Material, new MaterialSlotDropStrategy());
            registry.Register(RecipeSlotRole.Result, new ResultSlotDropStrategy());
            return registry;
        }
    }
}
