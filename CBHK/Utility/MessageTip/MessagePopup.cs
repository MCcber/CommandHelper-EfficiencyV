using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;
using CBHK.Model.Common;

namespace CBHK.Utility.MessageTip
{
    public class MessagePopup
    {
        #region Field
        MessageAdorner messageAdorner = null;
        #endregion

        #region Method
        public MessagePopup()
        {
            AdornerLayer adornerLayer = null;
            if (Application.Current.Windows.Count > 0)
            {
                Window win = Application.Current.Windows.OfType<Window>().FirstOrDefault(o => o.IsActive);
                adornerLayer = GetAdornerLayer(win) ?? throw new Exception("not AdornerLayer is null");
            }
            if (adornerLayer is not null)
            {
                messageAdorner = new(adornerLayer);
                adornerLayer.Add(messageAdorner);
            }
        }

        public void PushMessage(GeneratorMessage generatorMessage)
        {
            messageAdorner.PushMessage(generatorMessage);
        }

        private AdornerLayer GetAdornerLayer(System.Windows.Media.Visual visual)
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
