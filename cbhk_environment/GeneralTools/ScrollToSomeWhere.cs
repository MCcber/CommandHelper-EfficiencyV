using System.Windows;
using System.Windows.Controls;

namespace cbhk_environment.GeneralTools
{
    public class ScrollToSomeWhere
    {
        /// <summary>
        /// 用于动态布局，滚动指定viewer到某个子级可视化控件处
        /// </summary>
        /// <param name="target">目标控件</param>
        /// <param name="viewer">滚动视图</param>
        public static void Scroll(FrameworkElement target,ScrollViewer viewer)
        {
            // 获取要定位之前 ScrollViewer 目前的滚动位置
            var currentScrollPosition = viewer.VerticalOffset;
            var point = new Point(0, currentScrollPosition);

            // 计算出目标位置并滚动
            var targetPosition = target.TransformToVisual(viewer).Transform(point);
            viewer.ScrollToVerticalOffset(targetPosition.Y);
        }
    }
}
