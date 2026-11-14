using CBHK.Interface.Component.Recipe;
using CBHK.Model.Data;
using Prism.Ioc;
using System.Collections.Generic;

namespace CBHK.ViewModel.Component.Recipe.Strategy
{
    public sealed class CampfireStrategy : IRecipeStrategy
    {
        public RecipeType Kind => RecipeType.Campfire;
        public string Uid => "campfire";
        public string Title => "篝火";
        public IReadOnlyList<string> JsonTypes { get; } = ["campfire_cooking"];

        public RecipeEditorViewModelBase CreateEditor(IContainerProvider container)
        {
            return (RecipeEditorViewModelBase)container.Resolve(typeof(CampfireViewModel));
        }
    }
}
