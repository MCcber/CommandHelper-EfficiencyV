using CBHK.ViewModel.Component.Recipe;

namespace CBHK.Interface.Component.Recipe
{
    /// <summary>
    /// 顶替页签内容的子页面（设置页/搜索页）
    /// </summary>
    public interface IRecipePanel
    {
        RecipeEditorViewModelBase Owner { get; }
    }
}
