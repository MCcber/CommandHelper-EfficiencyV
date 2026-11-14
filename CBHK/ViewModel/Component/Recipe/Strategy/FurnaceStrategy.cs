using CBHK.Interface.Component.Recipe;
using CBHK.Model.Data;
using Prism.Ioc;
using System.Collections.Generic;

namespace CBHK.ViewModel.Component.Recipe.Strategy
{
    public sealed class FurnaceStrategy : IRecipeStrategy
    {
        public RecipeType Kind => RecipeType.Furnace;
        public string Uid => "furnace";
        public string Title => "熔炉";
        public IReadOnlyList<string> JsonTypes { get; } = ["smelting"];

        public RecipeEditorViewModelBase CreateEditor(IContainerProvider container)
        {
            return (RecipeEditorViewModelBase)container.Resolve(typeof(FurnaceViewModel));
        }
    }
}
