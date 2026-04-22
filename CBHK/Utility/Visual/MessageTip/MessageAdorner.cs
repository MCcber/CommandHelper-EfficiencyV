using CBHK.Model.Common;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace CBHK.Utility.Visual.MessageTip
{
    public class MessageAdorner : Adorner
    {
        #region Field
        private ListBox listBox;
        DispatcherTimer timer = new()
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        private UIElement _child;
        private readonly FrameworkElement adornedElement;
        #endregion

        #region Property
        private ObservableCollection<GeneratorMessage> Source { get; set; } = [];
        #endregion

        #region Method
        public MessageAdorner(UIElement adornedElement) : base(adornedElement)
        {
            this.adornedElement = adornedElement as FrameworkElement;
            timer.Tick += (sender, e) =>
            {
                if (Source.Count > 0)
                {
                    Source.RemoveAt(Source.Count - 1);
                }
            };
            timer.Start();
            if (listBox is null)
            {
                listBox = new ListBox
                {
                    HorizontalAlignment = HorizontalAlignment.Right,
                    IsHitTestVisible = false,
                    Style = null,
                    BorderThickness = new Thickness(0),
                    Background = Brushes.Transparent,
                    Margin = new(0, 10, 10, 0),
                    ItemTemplate = Application.Current.Resources["MessageTipItemTemplate"] as DataTemplate,
                    ItemsSource = Source
                };
                Child = listBox;
            }
        }

        public void PushMessage(GeneratorMessage generatorMessage)
        {
            GeneratorMessage messageData = new()
            {
                Message = generatorMessage.Message,
                MessageBrush = generatorMessage.MessageBrush,
                SecondBackground = generatorMessage.SecondBackground,
                FirstBackground = generatorMessage.FirstBackground,
                SubMessage = generatorMessage.SubMessage,
                SubMessageBrush = generatorMessage.SubMessageBrush,
                Icon = generatorMessage.Icon
            };
            while (Source.Count >= 5)
            {
                Source.RemoveAt(4);
            }
            Source.Insert(0, messageData);
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
                return _child is not null ? 1 : 0;
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            // 计算右上角位置
            double rightMargin = 10;
            double topMargin = 10;
            var x = adornedElement.ActualWidth - _child.DesiredSize.Width - rightMargin;
            // 确保不超出左边界
            if (x < 0) x = 0;
            var y = topMargin; // 设置顶部边距
            _child.Arrange(new Rect(new Point(x, y), _child.DesiredSize));
            return finalSize;
        }

        protected override System.Windows.Media.Visual GetVisualChild(int index)
        {
            if (index == 0 && _child is not null) return _child;
            return base.GetVisualChild(index);
        } 
        #endregion
    }
}
