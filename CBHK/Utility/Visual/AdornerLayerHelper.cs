using System.Windows.Controls;
using System.Windows.Documents;

namespace CBHK.Utility.Visual
{
    public static class AdornerLayerHelper
    {
        public static AdornerLayer GetAdornerLayer(System.Windows.Media.Visual visual)
        {
            // 启动早期（主窗体还在构造、没有任何激活窗口）传进来的就是 null：
            // 这里必须返回 null，让调用方自己决定"这次提示先不发"，而不是把 ArgumentNullException 抛到构造函数上。
            if (visual is null)
            {
                return null;
            }
            var decorator = visual as AdornerDecorator;
            if (decorator is not null)
                return decorator.AdornerLayer;
            var presenter = visual as ScrollContentPresenter;
            if (presenter is not null)
                return presenter.AdornerLayer;
            var visualContent = (visual as System.Windows.Window)?.Content as System.Windows.Media.Visual;
            AdornerLayer result = AdornerLayer.GetAdornerLayer(visualContent ?? visual);
            return result;
        }
    }
}