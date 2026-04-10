using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace CBHK.CustomControl.Container
{
    public class BindableStackPanel : StackPanel
    {
        #region Property
        public IEnumerable ItemsSource { get => (IEnumerable)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(BindableStackPanel),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public DataTemplateSelector ItemTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(ItemTemplateSelectorProperty); }
            set { SetValue(ItemTemplateSelectorProperty, value); }
        }

        public static readonly DependencyProperty ItemTemplateSelectorProperty =
            DependencyProperty.Register("ItemTemplateSelector", typeof(DataTemplateSelector), typeof(BindableStackPanel), new PropertyMetadata(default(DataTemplateSelector), OnTemplateSelectorChanged));
        #endregion

        #region Method
        private void Refresh()
        {
            Children.Clear();
            if (ItemsSource is null)
            {
                return;
            }

            foreach (var item in ItemsSource)
            {
                var contentPresenter = new ContentPresenter
                {
                    Content = item,
                    ContentTemplateSelector = ItemTemplateSelector
                };
                Children.Add(contentPresenter);
            }
        }
        #endregion

        #region Event
        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BindableStackPanel panel)
            {
                // 释放旧事件监听，防止内存泄漏
                if (e.OldValue is INotifyCollectionChanged oldList)
                {
                    oldList.CollectionChanged -= panel.OnCollectionChanged;
                }

                panel.Refresh();

                if (e.NewValue is INotifyCollectionChanged newList)
                {
                    newList.CollectionChanged += panel.OnCollectionChanged;
                }
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => Refresh();

        private static void OnTemplateSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BindableStackPanel panel)
            {
                panel.Refresh();
            }
        }
        #endregion
    }
}