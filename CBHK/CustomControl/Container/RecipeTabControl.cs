using System.Windows;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    /// <summary>
    /// 配方页签容器：数据源里放 ViewModel，容器仍是 VectorRichTabItem
    /// </summary>
    public class RecipeTabControl : VectorTabControl
    {
        protected override DependencyObject GetContainerForItemOverride()
        {
            // VectorTextTabItem 的构造里已把 Foreground 绑到主题色，样式设不进去，
            // 这里补本地值，保持配方页签改造前的白字。
            return new VectorRichTabItem
            {
                Foreground = new SolidColorBrush(Colors.White)
            };
        }
    }
}
