using CBHK.Utility.Common;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CBHK.CustomControl.VectorComboBox
{
    public partial class VectorTextComboBox : ComboBox
    {
        #region Property
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public Thickness ArrowMargin
        {
            get { return (Thickness)GetValue(ArrowMarginProperty); }
            set { SetValue(ArrowMarginProperty, value); }
        }

        public static readonly DependencyProperty ArrowMarginProperty =
            DependencyProperty.Register("ArrowMargin", typeof(Thickness), typeof(VectorTextComboBox), new PropertyMetadata(default(Thickness)));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(VectorTextComboBox), new PropertyMetadata(default(string)));

        public Brush ItemContainerBackground
        {
            get { return (Brush)GetValue(ItemContainerBackgroundProperty); }
            set { SetValue(ItemContainerBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ItemContainerBackgroundProperty =
            DependencyProperty.Register("ItemContainerBackground", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));

        public Brush ItemSelectedMarkerBrush
        {
            get { return (Brush)GetValue(ItemSelectedMarkerBrushProperty); }
            set { SetValue(ItemSelectedMarkerBrushProperty, value); }
        }

        public static readonly DependencyProperty ItemSelectedMarkerBrushProperty =
            DependencyProperty.Register("ItemSelectedMarkerBrush", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));

        public Brush PopupItemPanelBackground
        {
            get { return (Brush)GetValue(PopupItemPanelBackgroundProperty); }
            set { SetValue(PopupItemPanelBackgroundProperty, value); }
        }

        public static readonly DependencyProperty PopupItemPanelBackgroundProperty =
            DependencyProperty.Register("PopupItemPanelBackground", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));

        public Brush ArrowBrush
        {
            get { return (Brush)GetValue(ArrowBrushProperty); }
            set { SetValue(ArrowBrushProperty, value); }
        }

        public static readonly DependencyProperty ArrowBrushProperty =
            DependencyProperty.Register("ArrowBrush", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));

        public Brush TitleBrush
        {
            get { return (Brush)GetValue(TitleBrushProperty); }
            set { SetValue(TitleBrushProperty, value); }
        }

        public static readonly DependencyProperty TitleBrushProperty =
            DependencyProperty.Register("TitleBrush", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));

        public Brush SearchBoxForeground
        {
            get { return (Brush)GetValue(SearchBoxForegroundProperty); }
            set { SetValue(SearchBoxForegroundProperty, value); }
        }

        public static readonly DependencyProperty SearchBoxForegroundProperty =
            DependencyProperty.Register("SearchBoxForeground", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));

        public RelayCommand ClosePopupCommand
        {
            get { return (RelayCommand)GetValue(ClosePopupCommandProperty); }
            set { SetValue(ClosePopupCommandProperty, value); }
        }

        public static readonly DependencyProperty ClosePopupCommandProperty =
            DependencyProperty.Register("ClosePopupCommand", typeof(RelayCommand), typeof(VectorTextComboBox), new PropertyMetadata(default(RelayCommand)));
        #endregion

        #region Method
        public VectorTextComboBox()
        {
            Loaded += VectorTextComboBox_Loaded;
            DropDownClosed += VectorTextComboBox_DropDownClosed;
        }
        #endregion

        #region Event
        private void VectorTextComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            MaxDropDownHeight -= 11.5;
            SearchBoxForeground = Brushes.White;
            TitleBrush = Brushes.White;
            ClosePopupCommand = ClosePopupClickCommand as RelayCommand;
            ArrowBrush = Brushes.Black;
            ItemSelectedMarkerBrush = Brushes.White;
            ItemContainerBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#58585A"));
            PopupItemPanelBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8C8D90"));
        }

        private void VectorTextComboBox_DropDownClosed(object sender, System.EventArgs e)
        {
            // 查找模板中的按钮
            var toggleButton = FindSomeThingByType.FindVisualChildByName<ToggleButton>(this, "toggleButton");
            if (toggleButton != null)
            {
                // 创建并触发MouseLeave事件
                var args = new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount)
                {
                    RoutedEvent = MouseLeaveEvent
                };
                toggleButton.RaiseEvent(args);
            }
        }

        [RelayCommand]
        private void ClosePopupClick() => IsDropDownOpen = false;
        #endregion
    }
}
