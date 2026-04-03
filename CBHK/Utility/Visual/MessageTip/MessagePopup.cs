using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;
using CBHK.Model.Common;

namespace CBHK.Utility.Visual.MessageTip
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
                adornerLayer = AdornerLayerHelper.GetAdornerLayer(win) ?? throw new Exception("not AdornerLayer is null");
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
        #endregion
    }
}
