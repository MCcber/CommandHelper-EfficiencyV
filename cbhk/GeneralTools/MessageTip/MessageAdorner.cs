using System;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using System.Linq;

namespace cbhk.GeneralTools.MessageTip
{
    public class MessageAdorner : Adorner
    {
        private ListBox listBox;
        private ObservableCollection<MessageData> source { get; set; } = [];
        private UIElement _child;
        private FrameworkElement adornedElement;
        public MessageAdorner(UIElement adornedElement) : base(adornedElement)
        {
            this.adornedElement = adornedElement as FrameworkElement;
        }

        public void PushMessage(string message, MessageBoxImage type = MessageBoxImage.Information)
        {
            if (listBox is null)
            {
                listBox = new ListBox
                {
                    IsHitTestVisible = false,
                    Style = null,
                    BorderThickness = new Thickness(0),
                    Background = Brushes.Transparent,
                    Margin = new(0, 20, 0, 0),
                    ItemTemplate = Application.Current.Resources["MessageTipItemTemplate"] as DataTemplate,
                    ItemsSource = source
                };
                Child = listBox;
            }
            ImageSource icon = new BitmapImage();
            if (type == MessageBoxImage.Information)
                icon = Application.Current.Resources["InfomationIcon"] as DrawingImage;
            else
                if (type == MessageBoxImage.Error)
                icon = Application.Current.Resources["ErrorIcon"] as DrawingImage;
            MessageData messageData = new()
            {
                MessageString = message,
                Icon = icon
            };
            DispatcherTimer timer = new()
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            timer.Tick += (sender, e) =>
            {
                source.Remove(messageData);
                timer.Stop();
            };
            if (source.Count >= 5)
                source.RemoveAt(4);
            source.Insert(0, messageData);
            timer.Start();
        }

        public UIElement Child
        {
            get => _child;
            set
            {
                if (value is null)
                {
                    RemoveVisualChild(_child);
                    _child = value;
                    return;
                }
                AddVisualChild(value);
                _child = value;
            }
        }
        protected override int VisualChildrenCount
        {
            get
            {
                return _child != null ? 1 : 0;
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var x = (adornedElement.ActualWidth - _child.DesiredSize.Width) / 2;
            _child.Arrange(new Rect(new Point(x, 0), _child.DesiredSize));
            return finalSize;
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index == 0 && _child != null) return _child;
            return base.GetVisualChild(index);
        }
    }

    public class MessageData
    {
        public ImageSource Icon { get; set; }
        public string MessageString { get; set; }

        public string CollapseCount { get; set; } = "1";

        public Visibility CollapseBlockVisibility { get; set; } = Visibility.Collapsed;
    }
}
