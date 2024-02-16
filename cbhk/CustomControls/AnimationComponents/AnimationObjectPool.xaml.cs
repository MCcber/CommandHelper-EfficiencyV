using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace cbhk.CustomControls.AnimationComponents
{
    /// <summary>
    /// AnimationObjectPool.xaml 的交互逻辑
    /// </summary>
    public partial class AnimationObjectPool : UserControl
    {
        public ObservableCollection<AnimationApplyObject> ObjectiveList
        {
            get { return (ObservableCollection<AnimationApplyObject>)GetValue(ObjectiveListProperty); }
            set { SetValue(ObjectiveListProperty, value); }
        }

        public static readonly DependencyProperty ObjectiveListProperty =
            DependencyProperty.Register("ObjectiveList", typeof(ObservableCollection<AnimationApplyObject>), typeof(AnimationObjectPool), new PropertyMetadata(default(ObservableCollection<AnimationApplyObject>)));

        public AnimationObjectPool()
        {
            InitializeComponent();
        }

        private void ObjectItems_Loaded(object sender, RoutedEventArgs e)
        {
            ItemsControl itemsControl = sender as ItemsControl;
            itemsControl.ItemsSource = ObjectiveList;
        }
    }
}