using CBHK.Interface.Component.Recipe;
using CBHK.Model.Data;
using Prism.Ioc;
using System.Collections.Generic;

namespace CBHK.ViewModel.Component.Recipe.Strategy
{
    public sealed class StonecutterStrategy : IRecipeStrategy
    {
        public RecipeType Kind => RecipeType.Stonecutter;
        public string Uid => "stonecutter";
        public string Title => "切石机";
        public IReadOnlyList<string> JsonTypes { get; } = ["stonecutting"];

        public RecipeEditorViewModelBase CreateEditor(IContainerProvider container)
        {
            return (RecipeEditorViewModelBase)container.Resolve(typeof(StonecutterViewModel));
        }
    }
}
