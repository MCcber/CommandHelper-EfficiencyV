using CBHK.Interface.Component.Recipe;
using Prism.Events;

namespace CBHK.ViewModel.Component.Recipe.Event
{
    /// <summary>
    /// 请求关闭当前子页面，把页签内容还给配方页
    /// </summary>
    public sealed class CloseRecipePanelEvent : PubSubEvent<IRecipePanel>
    {
    }
}
