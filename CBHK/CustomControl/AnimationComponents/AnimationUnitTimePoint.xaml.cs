using CBHK.Utility.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.AnimationComponents
{
    /// <summary>
    /// AnimationUnitTimePoint.xaml 的交互逻辑
    /// </summary>
    public partial class AnimationUnitTimePoint : UserControl
    {
        public Point Position { get; set; }
        public AnimationUnitTimePoint()
        {
            InitializeComponent();
            Loaded += AnimationUnitTimePoint_Loaded;
        }

        private void AnimationUnitTimePoint_Loaded(object sender, RoutedEventArgs e)
        {
            AnimationTimeScale animationTimeScale = this.FindParent<AnimationTimeScale>();
            if(animationTimeScale is not null)
            {
                GeneralTransform generalTransform = TransformToAncestor(animationTimeScale);
                if (generalTransform is not null)
                    Position = generalTransform.Transform(new(0, 0));
            }
        }
    }
}
