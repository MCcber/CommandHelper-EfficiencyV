using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CBHK.Utility.Visual
{
    /// <summary>
    /// 把列表项的左右键点击转成命令，命令参数是该项的数据对象
    /// </summary>
    public static class ItemClickCommandHelper
    {
        public static readonly DependencyProperty LeftClickCommandProperty =
            DependencyProperty.RegisterAttached("LeftClickCommand", typeof(ICommand), typeof(ItemClickCommandHelper), new PropertyMetadata(default(ICommand), OnCommandChanged));

        public static readonly DependencyProperty RightClickCommandProperty =
            DependencyProperty.RegisterAttached("RightClickCommand", typeof(ICommand), typeof(ItemClickCommandHelper), new PropertyMetadata(default(ICommand), OnCommandChanged));

        public static ICommand GetLeftClickCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(LeftClickCommandProperty);
        }

        public static void SetLeftClickCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(LeftClickCommandProperty, value);
        }

        public static ICommand GetRightClickCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(RightClickCommandProperty);
        }

        public static void SetRightClickCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(RightClickCommandProperty, value);
        }

        private static void OnCommandChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is not ItemsControl itemsControl)
                return;

            itemsControl.PreviewMouseLeftButtonDown -= OnLeftClick;
            itemsControl.PreviewMouseLeftButtonDown += OnLeftClick;
            itemsControl.PreviewMouseRightButtonUp -= OnRightClick;
            itemsControl.PreviewMouseRightButtonUp += OnRightClick;
        }

        private static void OnLeftClick(object sender, MouseButtonEventArgs e)
        {
            Execute(sender, e, GetLeftClickCommand);
        }

        private static void OnRightClick(object sender, MouseButtonEventArgs e)
        {
            Execute(sender, e, GetRightClickCommand);
        }

        private static void Execute(object sender, MouseButtonEventArgs e, System.Func<DependencyObject, ICommand> getCommand)
        {
            if (sender is not ItemsControl itemsControl)
                return;

            ICommand command = getCommand(itemsControl);
            if (command is null)
                return;

            object item = ResolveItem(itemsControl, e.OriginalSource as DependencyObject);
            if (item is null || !command.CanExecute(item))
                return;

            command.Execute(item);
            e.Handled = true;
        }

        private static object ResolveItem(ItemsControl itemsControl, DependencyObject source)
        {
            if (source is null)
                return null;

            DependencyObject container = ItemsControl.ContainerFromElement(itemsControl, source);
            return container is null ? null : itemsControl.ItemContainerGenerator.ItemFromContainer(container);
        }
    }
}
