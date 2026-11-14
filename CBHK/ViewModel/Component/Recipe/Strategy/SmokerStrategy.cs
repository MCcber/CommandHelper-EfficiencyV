using CBHK.Interface.Component.Recipe;
using CBHK.Model.Data;
using Prism.Ioc;
using System.Collections.Generic;

namespace CBHK.ViewModel.Component.Recipe.Strategy
{
    public sealed class SmokerStrategy : IRecipeStrategy
    {
        public RecipeType Kind => RecipeType.Smoker;
        public string Uid => "smoker";
        public string Title => "烟熏炉";
        public IReadOnlyList<string> JsonTypes { get; } = ["smoking", "smoker"];

        public RecipeEditorViewModelBase CreateEditor(IContainerProvider container)
        {
            return (RecipeEditorViewModelBase)container.Resolve(typeof(SmokerViewModel));
        }
    }
}
