using CBHK.Interface.Component.Recipe;
using CBHK.Model.Data;
using CBHK.ViewModel.Component.Recipe;

namespace CBHK.ViewModel.Component.Recipe.Strategy
{
    /// <summary>
    /// 材料槽策略：多选时追加，单选时覆盖首个
    /// </summary>
    public sealed class MaterialSlotDropStrategy : ISlotDropStrategy
    {
        public bool CanHandle(RecipeSlotViewModel slot) => slot.Role == RecipeSlotRole.Material;

        public void Apply(RecipeSlotViewModel slot, ItemStructure item)
        {
            if (slot.Owner.MultiSelect || slot.Items.Count == 0)
                slot.Items.Add(item);
            else
                slot.Items[0] = item;

            slot.EnsureTagSlots();
            slot.Refresh();
        }
    }
}
