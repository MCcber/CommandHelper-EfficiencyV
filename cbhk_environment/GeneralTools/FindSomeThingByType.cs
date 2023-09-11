using System.Windows.Media;
using System.Windows;

namespace cbhk_environment.GeneralTools
{
    public static class FindSomeThingByType
    {
        /// <summary>
        /// WPF中查找元素的子元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="depObj"></param>
        /// <returns></returns>
        public static T FindChild<T>(this DependencyObject depObj,string targetUid = "") where T : DependencyObject
        {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                var result = (child as T) ?? FindChild<T>(child, targetUid);
                if (result != null && (targetUid.Length == 0 || (result as FrameworkElement).Uid == targetUid)) return result;
            }
            return null;
        }

        /// <summary>
        /// WPF中查找元素的父元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element"></param>
        /// <returns></returns>
        public static T FindParent<T>(this DependencyObject element, string targetUid = "") where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(element);
            if (parent != null)
            {
                if (parent is T)
                {
                    return (T)parent;
                }
                else
                {
                    parent = FindParent<T>(parent, targetUid);
                    if (parent is not null and T && (targetUid.Length == 0 || (parent as FrameworkElement).Uid == targetUid))
                    {
                        return (T)parent;
                    }
                }
            }
            return null;
        }
    }
}
