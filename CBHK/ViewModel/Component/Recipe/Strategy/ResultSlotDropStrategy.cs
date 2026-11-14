using CBHK.Interface.Component.Recipe;
using CBHK.Model.Data;
using CBHK.ViewModel.Component.Recipe;

namespace CBHK.ViewModel.Component.Recipe.Strategy
{
    /// <summary>
    /// 结果槽策略：恒覆盖首个
    /// </summary>
    public sealed class ResultSlotDropStrategy : ISlotDropStrategy
    {
        public bool CanHandle(RecipeSlotViewModel slot) => slot.Role == RecipeSlotRole.Result;

        public void Apply(RecipeSlotViewModel slot, ItemStructure item)
        {
            if (slot.Items.Count == 0)
                slot.Items.Add(item);
            else
                slot.Items[0] = item;

            slot.Refresh();
        }
    }
}
