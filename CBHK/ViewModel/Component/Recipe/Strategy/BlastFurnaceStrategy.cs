using CBHK.Interface.Component.Recipe;
using CBHK.Model.Data;
using Prism.Ioc;
using System.Collections.Generic;

namespace CBHK.ViewModel.Component.Recipe.Strategy
{
    public sealed class BlastFurnaceStrategy : IRecipeStrategy
    {
        public RecipeType Kind => RecipeType.BlastFurnace;
        public string Uid => "blast_furnace";
        public string Title => "高炉";
        public IReadOnlyList<string> JsonTypes { get; } = ["blasting"];

        public RecipeEditorViewModelBase CreateEditor(IContainerProvider container)
        {
            return (RecipeEditorViewModelBase)container.Resolve(typeof(BlastFurnaceViewModel));
        }
    }
}
