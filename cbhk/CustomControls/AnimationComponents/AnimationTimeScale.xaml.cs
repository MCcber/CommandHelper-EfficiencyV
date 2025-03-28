using CBHK.GeneralTools;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace CBHK.CustomControls.AnimationComponents
{
    /// <summary>
    /// AnimationTimeScale.xaml 的交互逻辑
    /// </summary>
    public partial class AnimationTimeScale : UserControl
    {
        public ObservableCollection<AnimationUnitTimePoint> ScaleList { get; set; } = [];
        private AnimationContainer animationContainer = null;
        public AnimationTimeScale()
        {
            InitializeComponent();
        }

        private void timeScale_Loaded(object sender, RoutedEventArgs e)
        {
            ItemsControl scaleListPanel = sender as ItemsControl;
            scaleListPanel.ItemsSource = ScaleList;
            animationContainer = scaleListPanel.FindParent<AnimationContainer>();
            animationContainer.slider.ValueChanged += Slider_ValueChanged;
            animationContainer.slider.Value = animationContainer.slider.Maximum;
        }

        /// <summary>
        /// 更新末尾秒
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ScaleList.Clear();
            for (int i = 1; i <= animationContainer.slider.Value; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    AnimationUnitTimePoint animationUnitTimePoint = new();
                    animationUnitTimePoint.UnitTimeDisplayLine.Y2 = 10;
                    animationUnitTimePoint.UnitTimeNumber.Visibility = Visibility.Hidden;
                    ScaleList.Add(animationUnitTimePoint);
                }
                AnimationUnitTimePoint animationUnitTimePoint3 = new();
                animationUnitTimePoint3.UnitTimeDisplayLine.Y2 = 15;
                animationUnitTimePoint3.UnitTimeNumber.Visibility = Visibility.Hidden;
                ScaleList.Add(animationUnitTimePoint3);
            }
        }
    }
}
