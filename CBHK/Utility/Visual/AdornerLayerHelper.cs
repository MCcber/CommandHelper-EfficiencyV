using System.Windows.Controls;
using System.Windows.Documents;

namespace CBHK.Utility.Visual
{
    public static class AdornerLayerHelper
    {
        public static AdornerLayer GetAdornerLayer(System.Windows.Media.Visual visual)
        {
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