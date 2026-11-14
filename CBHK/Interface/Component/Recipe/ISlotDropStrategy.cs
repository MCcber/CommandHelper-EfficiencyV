using CBHK.Model.Data;
using CBHK.ViewModel.Component.Recipe;

namespace CBHK.Interface.Component.Recipe
{
    /// <summary>
    /// 槽位角色：材料槽参与多选，结果槽恒覆盖
    /// </summary>
    public enum RecipeSlotRole
    {
        Material,
        Result
    }

    /// <summary>
    /// 拖放策略：决定被拖入的物品如何写入槽位
    /// </summary>
    public interface ISlotDropStrategy
    {
        bool CanHandle(RecipeSlotViewModel slot);

        void Apply(RecipeSlotViewModel slot, ItemStructure item);
    }
}
