using CBHK.Interface.TreeView;
using System.Windows;
using System.Windows.Controls;

namespace CBHK.Utility.Data
{
    public class NumericTemplateSelector : DataTemplateSelector
    {
        #region Field
        private DataTemplate numericTemplate;
        #endregion

        #region Method
        #endregion

        #region Event
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            numericTemplate ??= Application.Current.Resources["CommonNumberTemplate"] as DataTemplate;

            if (item is INumberItem)
            {
                return numericTemplate;
            }
            return null;
        }
        #endregion
    }
}
