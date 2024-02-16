using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace cbhk.CustomControls.AnimationComponents
{
    /// <summary>
    /// AnimationTimeScale.xaml 的交互逻辑
    /// </summary>
    public partial class AnimationTimeScale : UserControl
    {
        public ObservableCollection<AnimationUnitTimePoint> ScaleList = [];
        public AnimationTimeScale()
        {
            InitializeComponent();
        }

        private void timeScale_Loaded(object sender, RoutedEventArgs e)
        {
            ItemsControl scaleListPanel = sender as ItemsControl;
            scaleListPanel.ItemsSource = ScaleList;
        }
    }
}
