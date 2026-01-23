using System.Windows;
using System.Windows.Controls;

namespace CBHK.CustomControl.Container
{
    public class VectorTabControl:TabControl
    {
        #region Property
        public object LastSelectedItem
        {
            get { return (object)GetValue(LastSelectedItemProperty); }
            set { SetValue(LastSelectedItemProperty, value); }
        }

        public static readonly DependencyProperty LastSelectedItemProperty =
            DependencyProperty.Register("LastSelectedItem", typeof(object), typeof(VectorTabControl), new PropertyMetadata(default(object)));
        #endregion

        #region Method
        public VectorTabControl()
        {
            Loaded += VectorTabControl_Loaded;
        }
        #endregion

        #region Event
        private void VectorTabControl_Loaded(object sender, RoutedEventArgs e)
        {
            SelectionChanged -= VectorTabControl_SelectionChanged;
            SelectionChanged += VectorTabControl_SelectionChanged;
            LastSelectedItem = SelectedItem;
        }

        private void VectorTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(LastSelectedItem is VectorTextTabItem lastSelectedItem)
            {
                lastSelectedItem.Margin = lastSelectedItem.OriginMargin;
                lastSelectedItem.Background = lastSelectedItem.OriginBackground;
                lastSelectedItem.UpdateBackground();
            }

            if (SelectedItem is VectorTextTabItem vectorTextTabItem)
            {
                vectorTextTabItem.Margin = new(vectorTextTabItem.OriginMargin.Left, vectorTextTabItem.OriginMargin.Top - 2, vectorTextTabItem.OriginMargin.Right, vectorTextTabItem.OriginMargin.Bottom);
                if (vectorTextTabItem.SelectedBackground is not null)
                {
                    vectorTextTabItem.Background = vectorTextTabItem.SelectedBackground;
                }
                vectorTextTabItem.UpdateBackground();
            }

            if (SelectedItem is TabItem tabItem)
            {
                LastSelectedItem = tabItem;
            }
        }
        #endregion
    }
}