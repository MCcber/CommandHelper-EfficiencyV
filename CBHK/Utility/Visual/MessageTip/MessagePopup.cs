using System;
using System.Linq;
using System.Windows.Documents;
using System.Windows;
using CBHK.Model.Data;

namespace CBHK.Utility.Visual.MessageTip
{
    public class MessagePopup
    {
        #region Field
        MessageAdorner messageAdorner = null;
        #endregion

        #region Method
        /// <summary>
        /// 提示条挂在窗口的 AdornerLayer 上。启动早期（主窗体还在构造、没有任何激活窗口）拿不到层，
        /// 这时**降级成"不发提示"**而不是抛异常 —— 以前这里会一路抛到 MainViewModel 的构造函数，
        /// 结果是 Prism 建不出主窗体、双击 exe 什么都不弹。
        /// </summary>
        public MessagePopup(Window window = null)
        {
            Window target = window;
            if (target is null && Application.Current is { } application && application.Windows.Count > 0)
            {
                target = application.Windows.OfType<Window>().FirstOrDefault(item => item.IsActive)
                    ?? application.Windows.OfType<Window>().FirstOrDefault();
            }
            if (target is null)
            {
                return;
            }

            AdornerLayer adornerLayer = AdornerLayerHelper.GetAdornerLayer(target);
            if (adornerLayer is null)
            {
                return;
            }

            messageAdorner = new(adornerLayer);
            adornerLayer.Add(messageAdorner);
        }

        public void PushMessage(GeneratorMessage generatorMessage)
        {
            // 没有装饰层（窗口还没建好 / 已关闭）：这次提示直接丢弃，调用方不需要感知
            messageAdorner?.PushMessage(generatorMessage);
        }
        #endregion
    }
}
