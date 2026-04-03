using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    public class VectorTreeViewItem : TreeViewItem
    {
        #region Property
        public Brush ConnectingLineBrush
        {
            get { return (Brush)GetValue(ConnectingLineBrushProperty); }
            set { SetValue(ConnectingLineBrushProperty, value); }
        }

        public static readonly DependencyProperty ConnectingLineBrushProperty =
            DependencyProperty.Register("ConnectingLineBrush", typeof(Brush), typeof(VectorTreeViewItem), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorTreeViewItem()
        {
            var connectingLineBrushSource = DependencyPropertyHelper.GetValueSource(this, ConnectingLineBrushProperty);
            if (connectingLineBrushSource.BaseValueSource is BaseValueSource.DefaultStyle || connectingLineBrushSource.BaseValueSource is BaseValueSource.Style || ConnectingLineBrush is null)
            {
                ConnectingLineBrush = new BrushConverter().ConvertFromString("#686868") as Brush;
            }
        }
        #endregion

        #region Event
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new VectorTreeViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is VectorTreeViewItem;
        } 
        #endregion
    }
}