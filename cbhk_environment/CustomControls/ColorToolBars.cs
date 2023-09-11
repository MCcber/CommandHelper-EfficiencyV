using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace cbhk_environment.CustomControls
{
    public class ColorToolBars:ToolBar
    {
        public Brush ToolBarOverPopupBackground
        {
            get { return (Brush)GetValue(ToolBarOverPopupBackgroundProperty); }
            set { SetValue(ToolBarOverPopupBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ToolBarOverPopupBackgroundProperty =
            DependencyProperty.Register("ToolBarOverPopupBackground", typeof(Brush), typeof(ColorToolBars), new PropertyMetadata(default(Brush)));



        public Thickness TooBarOverPopupMargin
        {
            get { return (Thickness)GetValue(TooBarOverPopupMarginProperty); }
            set { SetValue(TooBarOverPopupMarginProperty, value); }
        }

        public static readonly DependencyProperty TooBarOverPopupMarginProperty =
            DependencyProperty.Register("TooBarOverPopupMargin", typeof(Thickness), typeof(ColorToolBars), new PropertyMetadata(default(Thickness)));

        public UIElement ToolBarPopupTarget
        {
            get { return (UIElement)GetValue(ToolBarPopupTargetProperty); }
            set { SetValue(ToolBarPopupTargetProperty, value); }
        }

        public static readonly DependencyProperty ToolBarPopupTargetProperty =
            DependencyProperty.Register("ToolBarPopupTarget", typeof(UIElement), typeof(ColorToolBars), new PropertyMetadata(default(UIElement)));
    }
}
