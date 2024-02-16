using System.Windows;
using System.Windows.Controls;

namespace cbhk.CustomControls.AnimationComponents
{
    /// <summary>
    /// AnimationContainer.xaml 的交互逻辑
    /// </summary>
    public partial class AnimationContainer : UserControl
    {
        public AnimationContainer()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            objectPool.ObjectiveList = [];
            objectPool.ObjectiveList.Add(new AnimationApplyObject() { ObjectiveName = "左手" });
            objectPool.ObjectiveList.Add(new AnimationApplyObject() { ObjectiveName = "右手" });
            objectPool.ObjectiveList.Add(new AnimationApplyObject() { ObjectiveName = "头部" });
            objectPool.ObjectiveList.Add(new AnimationApplyObject() { ObjectiveName = "胸部" });
            objectPool.ObjectiveList.Add(new AnimationApplyObject() { ObjectiveName = "腿部" });
            objectPool.ObjectiveList.Add(new AnimationApplyObject() { ObjectiveName = "脚部" });

            AnimationUnitTimePoint animationUnitTimePoint1 = new();
            animationUnitTimePoint1.UnitTimeDisplayLine.Y2 = 15;
            animationUnitTimePoint1.UnitTimeNumber.Text = "0";
            timeScale.ScaleList.Add(animationUnitTimePoint1);

            for (int i = 1; i <= 15; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    AnimationUnitTimePoint animationUnitTimePoint = new();
                    animationUnitTimePoint.UnitTimeDisplayLine.Y2 = 5;
                    animationUnitTimePoint.UnitTimeNumber.Text = "0";
                    animationUnitTimePoint.UnitTimeNumber.Visibility = Visibility.Hidden;
                    timeScale.ScaleList.Add(animationUnitTimePoint);
                }
                AnimationUnitTimePoint animationUnitTimePoint2 = new();
                animationUnitTimePoint2.UnitTimeDisplayLine.Y2 = 10;
                animationUnitTimePoint2.UnitTimeNumber.Text = "0";
                animationUnitTimePoint2.UnitTimeNumber.Visibility = Visibility.Hidden;
                timeScale.ScaleList.Add(animationUnitTimePoint2);
                for (int j = 0; j < 4; j++)
                {
                    AnimationUnitTimePoint animationUnitTimePoint = new();
                    animationUnitTimePoint.UnitTimeDisplayLine.Y2 = 5;
                    animationUnitTimePoint.UnitTimeNumber.Text = "0";
                    animationUnitTimePoint.UnitTimeNumber.Visibility = Visibility.Hidden;
                    timeScale.ScaleList.Add(animationUnitTimePoint);
                }
                AnimationUnitTimePoint animationUnitTimePoint3 = new();
                animationUnitTimePoint3.UnitTimeDisplayLine.Y2 = 15;
                animationUnitTimePoint3.UnitTimeNumber.Text = i.ToString();
                timeScale.ScaleList.Add(animationUnitTimePoint3);
            }
        }
    }

    public class AnimationApplyObject
    {
        public string ObjectiveName { get; set; } = "";
    }
}