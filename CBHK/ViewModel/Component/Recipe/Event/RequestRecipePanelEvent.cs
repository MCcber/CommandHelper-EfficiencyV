using CBHK.Interface.Component.Recipe;
using Prism.Events;

namespace CBHK.ViewModel.Component.Recipe.Event
{
    /// <summary>
    /// 请求把页签内容替换成设置页/搜索页
    /// </summary>
    public sealed class RequestRecipePanelEvent : PubSubEvent<IRecipePanel>
    {
    }
}
