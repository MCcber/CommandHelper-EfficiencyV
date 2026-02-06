using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;
using CBHK.Model.Common;

namespace CBHK.Utility.MessageTip
{
    public static class Message
    {
        #region Method
        public static void PushMessage(GeneratorMessage generatorMessage)
        {
            Window win = null;
            if (Application.Current.Windows.Count > 0)
            {
                win = Application.Current.Windows.OfType<Window>().FirstOrDefault(o => o.IsActive);
            }

            var layer = GetAdornerLayer(win) ?? throw new Exception("not AdornerLayer is null");
            MessageAdorner messageAdorner = new(layer);
            layer.Add(messageAdorner);
            messageAdorner.PushMessage(generatorMessage);
        }

        static AdornerLayer GetAdornerLayer(System.Windows.Media.Visual visual)
        {
            var decorator = visual as AdornerDecorator;
            if (decorator is not null)
                return decorator.AdornerLayer;
            var presenter = visual as ScrollContentPresenter;
            if (presenter is not null)
                return presenter.AdornerLayer;
            var visualContent = (visual as Window)?.Content as System.Windows.Media.Visual;
            AdornerLayer result = AdornerLayer.GetAdornerLayer(visualContent ?? visual);
            return result;
        } 
        #endregion
    }
}
