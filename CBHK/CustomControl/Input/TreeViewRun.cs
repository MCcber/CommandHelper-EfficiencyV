using System.Windows;
using System.Windows.Documents;

namespace CBHK.CustomControl.Input
{
    public class TreeViewRun:Run
    {
        public RichTreeViewItem ViewItem
        {
            get { return (RichTreeViewItem)GetValue(ViewItemProperty); }
            set { SetValue(ViewItemProperty, value); }
        }

        public static readonly DependencyProperty ViewItemProperty =
            DependencyProperty.Register("ViewItem", typeof(RichTreeViewItem), typeof(TreeViewRun), new PropertyMetadata(default(RichTreeViewItem)));
    }
}
