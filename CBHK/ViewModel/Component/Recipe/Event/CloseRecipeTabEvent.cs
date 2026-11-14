using Prism.Events;

namespace CBHK.ViewModel.Component.Recipe.Event
{
    /// <summary>
    /// 请求关闭一个配方页签
    /// </summary>
    public sealed class CloseRecipeTabEvent : PubSubEvent<RecipeTabViewModel>
    {
    }
}
