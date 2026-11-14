using CBHK.Interface.Component.Recipe;
using CBHK.Model.Data;
using Prism.Ioc;
using System.Collections.Generic;

namespace CBHK.ViewModel.Component.Recipe.Strategy
{
    public sealed class SmithingTableStrategy : IRecipeStrategy
    {
        public RecipeType Kind => RecipeType.SmithingTable;
        public string Uid => "smithing_table";
        public string Title => "锻造台";
        public IReadOnlyList<string> JsonTypes { get; } = ["smithing_transform", "smithing_trim"];

        public RecipeEditorViewModelBase CreateEditor(IContainerProvider container)
        {
            return (RecipeEditorViewModelBase)container.Resolve(typeof(SmithingTableViewModel));
        }
    }
}
