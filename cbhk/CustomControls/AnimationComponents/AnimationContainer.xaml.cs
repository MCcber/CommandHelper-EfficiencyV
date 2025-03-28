using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace CBHK.CustomControls.AnimationComponents
{
    /// <summary>
    /// AnimationContainer.xaml 的交互逻辑
    /// </summary>
    public partial class AnimationContainer : UserControl
    {
        #region Field
        private bool IsMouseDown = false;
        private bool IsMouseDraging = false;
        private bool IsPlaying = false;
        /// <summary>
        /// 探针每一毫秒的移动距离
        /// </summary>
        private double StepSize = 0.2;

        IProgress<Vector3DInfo> HeadPoseProgress = null;
        IProgress<Vector3DInfo> BodyPoseProgress = null;
        IProgress<Vector3DInfo> LeftArmPoseProgress = null;
        IProgress<Vector3DInfo> RightArmPoseProgress = null;
        IProgress<Vector3DInfo> LeftLegPoseProgress = null;
        IProgress<Vector3DInfo> RightLegPoseProgress = null;

        private double[] HeadDelta = [0.0,0.0,0.0];
        private double[] BodyDelta = [0.0,0.0,0.0];
        private double[] LeftArmDelta = [0.0,0.0,0.0];
        private double[] RightArmDelta = [0.0, 0.0, 0.0];
        private double[] LeftLegDelta = [0.0, 0.0, 0.0];
        private double[] RightLegDelta = [0.0, 0.0, 0.0];

        private double lastLinePositionX = 0.0;

        Point CurrentPoint = new();
        #endregion

        #region DependencyProperty
        public Vector3D HeadPose
        {
            get { return (Vector3D)GetValue(HeadPoseProperty); }
            set { SetValue(HeadPoseProperty, value); }
        }

        public static readonly DependencyProperty HeadPoseProperty =
            DependencyProperty.Register("HeadPose", typeof(Vector3D), typeof(AnimationContainer), new PropertyMetadata(default(Vector3D)));

        public Vector3D BodyPose
        {
            get { return (Vector3D)GetValue(BodyPoseProperty); }
            set { SetValue(BodyPoseProperty, value); }
        }

        public static readonly DependencyProperty BodyPoseProperty =
            DependencyProperty.Register("BodyPose", typeof(Vector3D), typeof(AnimationContainer), new PropertyMetadata(default(Vector3D)));

        public Vector3D LeftArmPose
        {
            get { return (Vector3D)GetValue(LeftArmPoseProperty); }
            set { SetValue(LeftArmPoseProperty, value); }
        }

        public static readonly DependencyProperty LeftArmPoseProperty =
            DependencyProperty.Register("LeftArmPose", typeof(Vector3D), typeof(AnimationContainer), new PropertyMetadata(default(Vector3D)));

        public Vector3D RightArmPose
        {
            get { return (Vector3D)GetValue(RightArmPoseProperty); }
            set { SetValue(RightArmPoseProperty, value); }
        }

        public static readonly DependencyProperty RightArmPoseProperty =
            DependencyProperty.Register("RightArmPose", typeof(Vector3D), typeof(AnimationContainer), new PropertyMetadata(default(Vector3D)));

        public Vector3D LeftLegPose
        {
            get { return (Vector3D)GetValue(LeftLegPoseProperty); }
            set { SetValue(LeftLegPoseProperty, value); }
        }

        public static readonly DependencyProperty LeftLegPoseProperty =
            DependencyProperty.Register("LeftLegPose", typeof(Vector3D), typeof(AnimationContainer), new PropertyMetadata(default(Vector3D)));

        public Vector3D RightLegPose
        {
            get { return (Vector3D)GetValue(RightLegPoseProperty); }
            set { SetValue(RightLegPoseProperty, value); }
        }

        public static readonly DependencyProperty RightLegPoseProperty =
            DependencyProperty.Register("RightLegPose", typeof(Vector3D), typeof(AnimationContainer), new PropertyMetadata(default(Vector3D)));

        public string HeadItem
        {
            get { return (string)GetValue(HeadItemProperty); }
            set { SetValue(HeadItemProperty, value); }
        }

        public static readonly DependencyProperty HeadItemProperty =
            DependencyProperty.Register("HeadItem", typeof(string), typeof(AnimationContainer), new PropertyMetadata(default(string)));

        public string BodyItem
        {
            get { return (string)GetValue(BodyItemProperty); }
            set { SetValue(BodyItemProperty, value); }
        }

        public static readonly DependencyProperty BodyItemProperty =
            DependencyProperty.Register("BodyItem", typeof(string), typeof(AnimationContainer), new PropertyMetadata(default(string)));

        public string LeftArmItem
        {
            get { return (string)GetValue(LeftArmItemProperty); }
            set { SetValue(LeftArmItemProperty, value); }
        }

        public static readonly DependencyProperty LeftArmItemProperty =
            DependencyProperty.Register("LeftArmItem", typeof(string), typeof(AnimationContainer), new PropertyMetadata(default(string)));

        public string RightArmItem
        {
            get { return (string)GetValue(RightArmItemProperty); }
            set { SetValue(RightArmItemProperty, value); }
        }

        public static readonly DependencyProperty RightArmItemProperty =
            DependencyProperty.Register("RightArmItem", typeof(string), typeof(AnimationContainer), new PropertyMetadata(default(string)));

        public string LeftLegItem
        {
            get { return (string)GetValue(LeftLegItemProperty); }
            set { SetValue(LeftLegItemProperty, value); }
        }

        public static readonly DependencyProperty LeftLegItemProperty =
            DependencyProperty.Register("LeftLegItem", typeof(string), typeof(AnimationContainer), new PropertyMetadata(default(string)));

        public string RightLegItem
        {
            get { return (string)GetValue(RightLegItemProperty); }
            set { SetValue(RightLegItemProperty, value); }
        }

        public static readonly DependencyProperty RightLegItemProperty =
            DependencyProperty.Register("RightLegItem", typeof(string), typeof(AnimationContainer), new PropertyMetadata(default(string)));
        #endregion

        public AnimationContainer()
        {
            InitializeComponent();

            HeadPoseProgress = new Progress<Vector3DInfo>(UpdateHeadPose);
            BodyPoseProgress = new Progress<Vector3DInfo>(UpdateBodyPose);
            LeftArmPoseProgress = new Progress<Vector3DInfo>(UpdateLeftArmPose);
            RightArmPoseProgress = new Progress<Vector3DInfo>(UpdateRightArmPose);
            LeftLegPoseProgress = new Progress<Vector3DInfo>(UpdateLeftLegPose);
            RightLegPoseProgress = new Progress<Vector3DInfo>(UpdateRightLegPose);
        }

        #region Event
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            AnimationUnitTimePoint animationUnitTimePoint1 = new() { Position = new() };
            animationUnitTimePoint1.UnitTimeDisplayLine.Y2 = 10;
            animationUnitTimePoint1.UnitTimeNumber.Visibility = Visibility.Hidden;
            timeScale.ScaleList.Add(animationUnitTimePoint1);

            for (int i = 1; i <= slider.Value; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    AnimationUnitTimePoint animationUnitTimePoint = new();
                    animationUnitTimePoint.UnitTimeDisplayLine.Y2 = 10;
                    animationUnitTimePoint.UnitTimeNumber.Visibility = Visibility.Hidden;
                    timeScale.ScaleList.Add(animationUnitTimePoint);
                }
                AnimationUnitTimePoint animationUnitTimePoint3 = new();
                animationUnitTimePoint3.UnitTimeDisplayLine.Y2 = 15;
                animationUnitTimePoint3.UnitTimeNumber.Visibility = Visibility.Hidden;
                timeScale.ScaleList.Add(animationUnitTimePoint3);
            }
        }

        private void TimeScale_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point point = e.GetPosition(timeScale);
            line.X1 = point.X;
            line.X2 = point.X;
            IsMouseDown = true;
        }

        private void TimeScale_MouseUp(object sender, MouseButtonEventArgs e) => IsMouseDown = IsMouseDraging = false;

        private void TimeScale_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsMouseDown)
            {
                Point point = e.GetPosition(timeScale);
                line.X1 = point.X;
                line.X2 = point.X;
                CurrentPoint = point;
                IsMouseDraging = true;
                if (timeLine.HeadAnimationPointPanel.Items.Count > 0 || 
                    timeLine.BodyAnimationPointPanel.Items.Count > 0 ||
                    timeLine.LeftArmAnimationPointPanel.Items.Count > 0 ||
                    timeLine.RightArmAnimationPointPanel.Items.Count > 0 ||
                    timeLine.LeftLegAnimationPointPanel.Items.Count > 0 ||
                    timeLine.RightLegAnimationPointPanel.Items.Count > 0)
                    UpdateRotationAndData();
                lastLinePositionX = line.X1;
            }
        }

        private void TimeScale_MouseLeave(object sender, MouseEventArgs e)
        {
            if (e.LeftButton is MouseButtonState.Released)
                IsMouseDown = false;
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            IsPlaying = !IsPlaying;
            UpdateRotationAndData();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            IsPlaying = false;
            line.X1 = line.X2 = 0;
            HeadDelta = BodyDelta = LeftArmDelta = RightArmDelta = LeftLegDelta = RightLegDelta = [0.0, 0.0, 0.0];
        }

        #endregion

        #region Method
        private async void UpdateRotationAndData()
        {
            while (IsPlaying)
            {
                if (timeScale.ScaleList[^1].Position.X == 0)
                {
                    line.X1 = line.X2 = 0;
                    break;
                }

                for (double i = line.X1; i < timeScale.ScaleList[^1].Position.X; i += StepSize)
                {
                    if (!IsPlaying || timeScale.ScaleList.Count == 0)
                    {
                        break;
                    }

                    line.X1 = i;
                    line.X2 = i;

                    if (line.X1 > timeScale.ScaleList[^1].Position.X)
                        line.X1 = timeScale.ScaleList[^1].Position.X;
                    if (line.X2 > timeScale.ScaleList[^1].Position.X)
                        line.X2 = timeScale.ScaleList[^1].Position.X;

                    #region 处理动作与数据的更迭
                    ProcessingRotationAndData(timeLine.HeadAnimationPointPanel.Items, HeadDelta, HeadPoseProgress);
                    ProcessingRotationAndData(timeLine.BodyAnimationPointPanel.Items, BodyDelta, BodyPoseProgress);
                    ProcessingRotationAndData(timeLine.LeftArmAnimationPointPanel.Items, LeftArmDelta, LeftArmPoseProgress);
                    ProcessingRotationAndData(timeLine.RightArmAnimationPointPanel.Items, RightArmDelta, RightArmPoseProgress);
                    ProcessingRotationAndData(timeLine.LeftLegAnimationPointPanel.Items, LeftLegDelta, LeftLegPoseProgress);
                    ProcessingRotationAndData(timeLine.RightLegAnimationPointPanel.Items, RightLegDelta, RightLegPoseProgress);
                    #endregion

                    await Task.Delay(1);
                }
            }

            if(IsMouseDraging)
            {
                #region 处理动作与数据的更迭
                ProcessingRotationAndData(timeLine.HeadAnimationPointPanel.Items, HeadDelta, HeadPoseProgress);
                ProcessingRotationAndData(timeLine.BodyAnimationPointPanel.Items, BodyDelta, BodyPoseProgress);
                ProcessingRotationAndData(timeLine.LeftArmAnimationPointPanel.Items, LeftArmDelta, LeftArmPoseProgress);
                ProcessingRotationAndData(timeLine.RightArmAnimationPointPanel.Items, RightArmDelta, RightArmPoseProgress);
                ProcessingRotationAndData(timeLine.LeftLegAnimationPointPanel.Items, LeftLegDelta, LeftLegPoseProgress);
                ProcessingRotationAndData(timeLine.RightLegAnimationPointPanel.Items, RightLegDelta, RightLegPoseProgress);
                #endregion
                await Task.Delay(1);
            }
        }

        /// <summary>
        /// 计算旋转与数据更新
        /// </summary>
        /// <param name="TimePointItemList">头部关键帧集合</param>
        /// <param name="Delta">旋转率</param>
        /// <param name="Pose">动作</param>
        private void ProcessingRotationAndData(ItemCollection TimePointItemList, double[] Delta,IProgress<Vector3DInfo> progress)
        {
            if (TimePointItemList.Count > 0 && TimePointItemList[^1] is AnimationTimePointItem lastItem && line.X1 < lastItem.Point.Position.X)
            {
                for (int j = 0; j < TimePointItemList.Count; j++)
                {
                    if (TimePointItemList[j] is AnimationTimePointItem currentItem &&
                        Math.Abs(line.X1 - (currentItem.Offset +15)) < 0.5)
                    {
                        #region 计算旋转变化率
                        //打中关键帧后立即校准角度值
                        progress.Report(new(currentItem.Pose,false));
                        if (currentItem.IsChecked.Value && j + 1 < TimePointItemList.Count && TimePointItemList[j + 1] is AnimationTimePointItem nextItem && nextItem.IsChecked.Value)
                        {
                            double frameSpan = timeScale.ScaleList[1].Position.X;
                            double frameLength = (timeScale.ScaleList.IndexOf(nextItem.Point) - timeScale.ScaleList.IndexOf(currentItem.Point)) / 5;
                            double differenceInXValues = nextItem.Pose.X - currentItem.Pose.X;
                            double differenceInYValues = nextItem.Pose.Y - currentItem.Pose.Y;
                            double differenceInZValues = nextItem.Pose.Z - currentItem.Pose.Z;

                            Delta[0] = differenceInXValues / frameLength / (frameSpan / 0.02);
                            Delta[1] = differenceInYValues / frameLength / (frameSpan / 0.02);
                            Delta[2] = differenceInZValues / frameLength / (frameSpan / 0.02);
                        }
                        #endregion
                    }
                    double direction = line.X1 - lastLinePositionX;
                    progress.Report(new(new Vector3D(Delta[0] * (direction < 0 ? -1 : 1), Delta[1] * (direction < 0 ? -1 : 1), Delta[2] * (direction < 0 ? -1 : 1))));
                }
            }
        }

        #region 更新Pose值
        private void UpdateHeadPose(Vector3DInfo vector3D)
        {
            if (vector3D.IsAdd)
                HeadPose = new(HeadPose.X + vector3D.Pose.X, HeadPose.Y + vector3D.Pose.Y, HeadPose.Z + vector3D.Pose.Z);
            else
                HeadPose = new(vector3D.Pose.X, vector3D.Pose.Y, vector3D.Pose.Z);
        }

        private void UpdateBodyPose(Vector3DInfo vector3D)
        {
            if (vector3D.IsAdd)
                BodyPose = new(BodyPose.X + vector3D.Pose.X, BodyPose.Y + vector3D.Pose.Y, BodyPose.Z + vector3D.Pose.Z);
            else
                BodyPose = new(vector3D.Pose.X, vector3D.Pose.Y, vector3D.Pose.Z);
        }

        private void UpdateLeftArmPose(Vector3DInfo vector3D)
        {
            if (vector3D.IsAdd)
                LeftArmPose = new(LeftArmPose.X + vector3D.Pose.X, LeftArmPose.Y + vector3D.Pose.Y, LeftArmPose.Z + vector3D.Pose.Z);
            else
                LeftArmPose = new(vector3D.Pose.X, vector3D.Pose.Y, vector3D.Pose.Z);
        }

        private void UpdateRightArmPose(Vector3DInfo vector3D)
        {
            if (vector3D.IsAdd)
                RightArmPose = new(RightArmPose.X + vector3D.Pose.X, RightArmPose.Y + vector3D.Pose.Y, RightArmPose.Z + vector3D.Pose.Z);
            else
                RightArmPose = new(vector3D.Pose.X, vector3D.Pose.Y, vector3D.Pose.Z);
        }

        private void UpdateLeftLegPose(Vector3DInfo vector3D)
        {
            if (vector3D.IsAdd)
                LeftLegPose = new(LeftLegPose.X + vector3D.Pose.X, LeftLegPose.Y + vector3D.Pose.Y, LeftLegPose.Z + vector3D.Pose.Z);
            else
                LeftLegPose = new(vector3D.Pose.X, vector3D.Pose.Y, vector3D.Pose.Z);
        }

        private void UpdateRightLegPose(Vector3DInfo vector3D)
        {
            if (vector3D.IsAdd)
                RightLegPose = new(RightLegPose.X + vector3D.Pose.X, RightLegPose.Y + vector3D.Pose.Y, RightLegPose.Z + vector3D.Pose.Z);
            else
                RightLegPose = new(vector3D.Pose.X, vector3D.Pose.Y, vector3D.Pose.Z);
        }
        #endregion

        #endregion
    }

    public class Vector3DInfo(Vector3D vector3D,bool isAdd = true)
    {
        public Vector3D Pose { get; set; } = vector3D;
        public bool IsAdd { get; set; } = isAdd;
    }
}