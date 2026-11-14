using CBHK.Model.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CBHK.Utility.Visual
{
    /// <summary>
    /// 让拖放走命令：附加后自动开启 AllowDrop，并从拖拽数据里取出物品结构
    /// </summary>
    public static class DropCommandHelper
    {
        public static readonly DependencyProperty DropCommandProperty =
            DependencyProperty.RegisterAttached("DropCommand", typeof(ICommand), typeof(DropCommandHelper), new PropertyMetadata(default(ICommand), OnDropCommandChanged));

        public static ICommand GetDropCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(DropCommandProperty);
        }

        public static void SetDropCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(DropCommandProperty, value);
        }

        private static void OnDropCommandChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is not UIElement element)
                return;

            element.AllowDrop = true;
            element.Drop -= OnDrop;
            element.Drop += OnDrop;
        }

        private static void OnDrop(object sender, DragEventArgs e)
        {
            if (sender is not DependencyObject obj)
                return;

            ICommand command = GetDropCommand(obj);
            if (command is null)
                return;

            if (e.Data.GetData(typeof(Image)) is not Image draggedItem || draggedItem.Tag is not ItemStructure itemStructure)
                return;

            if (command.CanExecute(itemStructure))
                command.Execute(itemStructure);
            e.Handled = true;
        }
    }
}
