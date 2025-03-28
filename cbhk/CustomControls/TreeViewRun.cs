using System.Windows;
using System.Windows.Documents;

namespace CBHK.CustomControls
{
    public class TreeViewRun:Run
    {
        public RichTreeViewItems ViewItem
        {
            get { return (RichTreeViewItems)GetValue(ViewItemProperty); }
            set { SetValue(ViewItemProperty, value); }
        }

        public static readonly DependencyProperty ViewItemProperty =
            DependencyProperty.Register("ViewItem", typeof(RichTreeViewItems), typeof(TreeViewRun), new PropertyMetadata(default(RichTreeViewItems)));
    }
}
