using System.Windows;
using System.Windows.Controls;

namespace CBHK.CustomControl.Container
{
    public class VectorListView : ListView
    {
        #region Event
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new VectorListViewItemContainer();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is VectorListViewItemContainer;
        } 
        #endregion
    }
}
