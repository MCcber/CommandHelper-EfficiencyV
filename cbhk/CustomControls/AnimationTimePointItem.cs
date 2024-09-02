using cbhk.CustomControls.AnimationComponents;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace cbhk.CustomControls
{
    public class AnimationTimePointItem:ToggleButton
    {
        #region DependencyProperty
        public AnimationUnitTimePoint Point
        {
            get { return (AnimationUnitTimePoint)GetValue(PointProperty); }
            set { SetValue(PointProperty, value); }
        }

        public static readonly DependencyProperty PointProperty =
            DependencyProperty.Register("Point", typeof(AnimationUnitTimePoint), typeof(AnimationTimePointItem), new PropertyMetadata(default(AnimationUnitTimePoint)));

        public Brush SelectedColor
        {
            get { return (Brush)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Brush), typeof(AnimationTimePointItem), new PropertyMetadata(default(Brush)));

        public double Offset
        {
            get { return (double)GetValue(OffsetProperty); }
            set { SetValue(OffsetProperty, value); }
        }

        public static readonly DependencyProperty OffsetProperty =
            DependencyProperty.Register("Offset", typeof(double), typeof(AnimationTimePointItem), new PropertyMetadata(default(double)));

        public Vector3D Pose
        {
            get { return (Vector3D)GetValue(PoseProperty); }
            set { SetValue(PoseProperty, value); }
        }

        public static readonly DependencyProperty PoseProperty =
            DependencyProperty.Register("Pose", typeof(Vector3D), typeof(AnimationTimePointItem), new PropertyMetadata(default(Vector3D)));

        public string HeadItem
        {
            get { return (string)GetValue(HeadItemProperty); }
            set { SetValue(HeadItemProperty, value); }
        }

        public static readonly DependencyProperty HeadItemProperty =
            DependencyProperty.Register("HeadItem", typeof(string), typeof(AnimationTimePointItem), new PropertyMetadata(default(string)));

        public string BodyItem
        {
            get { return (string)GetValue(BodyItemProperty); }
            set { SetValue(BodyItemProperty, value); }
        }

        public static readonly DependencyProperty BodyItemProperty =
            DependencyProperty.Register("BodyItem", typeof(string), typeof(AnimationTimePointItem), new PropertyMetadata(default(string)));

        public string LeftArmItem
        {
            get { return (string)GetValue(LeftArmItemProperty); }
            set { SetValue(LeftArmItemProperty, value); }
        }

        public static readonly DependencyProperty LeftArmItemProperty =
            DependencyProperty.Register("LeftArmItem", typeof(string), typeof(AnimationTimePointItem), new PropertyMetadata(default(string)));

        public string RightArmItem
        {
            get { return (string)GetValue(RightArmItemProperty); }
            set { SetValue(RightArmItemProperty, value); }
        }

        public static readonly DependencyProperty RightArmItemProperty =
            DependencyProperty.Register("RightArmItem", typeof(string), typeof(AnimationTimePointItem), new PropertyMetadata(default(string)));

        public string LeftLegItem
        {
            get { return (string)GetValue(LeftLegItemProperty); }
            set { SetValue(LeftLegItemProperty, value); }
        }

        public static readonly DependencyProperty LeftLegItemProperty =
            DependencyProperty.Register("LeftLegItem", typeof(string), typeof(AnimationTimePointItem), new PropertyMetadata(default(string)));

        public string RightLegItem
        {
            get { return (string)GetValue(RightLegItemProperty); }
            set { SetValue(RightLegItemProperty, value); }
        }

        public static readonly DependencyProperty RightLegItemProperty =
            DependencyProperty.Register("RightLegItem", typeof(string), typeof(AnimationTimePointItem), new PropertyMetadata(default(string)));
        #endregion
        #region Method
        public AnimationTimePointItem()
        {

        }
        public AnimationTimePointItem(double offset)
        {
            Offset = offset;
        }
        #endregion
    }
}
