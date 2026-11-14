using CBHK.Interface.Component.Recipe;
using CBHK.Model.Data;
using Prism.Ioc;
using System.Collections.Generic;

namespace CBHK.ViewModel.Component.Recipe.Strategy
{
    public sealed class CraftingTableStrategy : IRecipeStrategy
    {
        public RecipeType Kind => RecipeType.CraftingTable;
        public string Uid => "crafting_table";
        public string Title => "工作台";
        public IReadOnlyList<string> JsonTypes { get; } = ["crafting_shaped", "crafting_shapeless", "crafting_shapeness"];

        public RecipeEditorViewModelBase CreateEditor(IContainerProvider container)
        {
            return (RecipeEditorViewModelBase)container.Resolve(typeof(CraftingTableViewModel));
        }
    }
}
