using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Media;

namespace cbhk.GeneralTools.MessageTip
{
    public static class Message
    {
        public static void PushMessage(string message, MessageBoxImage type = MessageBoxImage.Information)
        {
            Window win = null;
            if (Application.Current.Windows.Count > 0)
            {
                win = Application.Current.Windows.OfType<Window>().FirstOrDefault(o => o.IsActive);
                win ??= Application.Current.Windows.OfType<Window>().First(o => o.IsActive);
            }

            var layer = GetAdornerLayer(win) ?? throw new Exception("not AdornerLayer is null");
            MessageAdorner messageAdorner = new(layer);
            layer.Add(messageAdorner);
            messageAdorner.PushMessage(message, type);
        }
        static AdornerLayer GetAdornerLayer(Visual visual)
        {
            var decorator = visual as AdornerDecorator;
            if (decorator != null)
                return decorator.AdornerLayer;
            var presenter = visual as ScrollContentPresenter;
            if (presenter != null)
                return presenter.AdornerLayer;
            var visualContent = (visual as Window)?.Content as Visual;
            AdornerLayer result = AdornerLayer.GetAdornerLayer(visualContent ?? visual);
            return result;
        }
    }
}
