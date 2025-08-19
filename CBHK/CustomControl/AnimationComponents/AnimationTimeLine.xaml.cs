using CBHK.Utility.Common;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace CBHK.CustomControl.AnimationComponents
{
    /// <summary>
    /// AnimationTimeLine.xaml 的交互逻辑
    /// </summary>
    public partial class AnimationTimeLine : UserControl
    {
        AnimationTimeScale animationTimeScale = null;
        public AnimationTimeLine()
        {
            InitializeComponent();
        }

        private void Grid_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                Grid grid = sender as Grid;
                Point point = e.GetPosition(grid);
                AnimationContainer animationContainer = grid.FindParent<AnimationContainer>();
                animationTimeScale = animationContainer.timeScale;
                Point p = e.GetPosition(animationContainer.timeScale);
                if (animationContainer is not null)
                {
                    point.X -= 15;
                }

                AnimationUnitTimePoint nearbyPoint = GetNearbyFrameItem(point.X + 15);
                AnimationUnitTimePoint currentPoint = GetNearbyFrameItem(point.X);
                int index = animationTimeScale.ScaleList.IndexOf(nearbyPoint);
                Vector3D currentPose = new();

                switch (grid.Uid)
                {
                    case "Head":
                        {
                            currentPose = animationContainer.HeadPose;
                            AnimationTimePointItem animationTimePointItem = new(currentPoint.Position.X + 15 / 4)
                            {
                                Pose = currentPose,
                                ToolTip = "Head=" + ((index + 1) / 5.0).ToString("F1") + "s\r\nX=" + currentPose.X + "\r\nY=" + currentPose.Y + "\r\nZ=" + currentPose.Z,
                                Point = nearbyPoint
                            };
                            ToolTipService.SetBetweenShowDelay(animationTimePointItem,0);
                            ToolTipService.SetInitialShowDelay(animationTimePointItem,0);
                            animationTimePointItem.MouseRightButtonUp += AnimationTimePointItem_MouseRightButtonUp;
                            HeadAnimationPointPanel.Items.Add(animationTimePointItem);
                            break;
                        }
                    case "Body":
                        {
                            currentPose = animationContainer.BodyPose;
                            AnimationTimePointItem animationTimePointItem = new(currentPoint.Position.X + 15 / 4)
                            {
                                Pose = currentPose,
                                ToolTip = "Body=" + ((index + 1) / 5.0).ToString("F1") + "s\r\nX=" + currentPose.X + "\r\nY=" + currentPose.Y + "\r\nZ=" + currentPose.Z,
                                Point = nearbyPoint
                            };
                            ToolTipService.SetBetweenShowDelay(animationTimePointItem, 0);
                            ToolTipService.SetInitialShowDelay(animationTimePointItem, 0);
                            animationTimePointItem.MouseRightButtonUp += AnimationTimePointItem_MouseRightButtonUp;
                            BodyAnimationPointPanel.Items.Add(animationTimePointItem);
                            break;
                        }
                    case "LeftArm":
                        {
                            currentPose = animationContainer.LeftArmPose;
                            AnimationTimePointItem animationTimePointItem = new(currentPoint.Position.X + 15 / 4)
                            {
                                Pose = currentPose,
                                ToolTip = "LeftArm=" + ((index + 1) / 5.0).ToString("F1") + "s\r\nX=" + currentPose.X + "\r\nY=" + currentPose.Y + "\r\nZ=" + currentPose.Z,
                                Point = nearbyPoint
                            };
                            ToolTipService.SetBetweenShowDelay(animationTimePointItem, 0);
                            ToolTipService.SetInitialShowDelay(animationTimePointItem, 0);
                            animationTimePointItem.MouseRightButtonUp += AnimationTimePointItem_MouseRightButtonUp;
                            LeftArmAnimationPointPanel.Items.Add(animationTimePointItem);
                            break;
                        }
                    case "RightArm":
                        {
                            currentPose = animationContainer.RightArmPose;
                            AnimationTimePointItem animationTimePointItem = new(currentPoint.Position.X + 15 / 4)
                            {
                                Pose = currentPose,
                                ToolTip = "RightArm=" + ((index + 1) / 5.0).ToString("F1") + "s\r\nX=" + currentPose.X + "\r\nY=" + currentPose.Y + "\r\nZ=" + currentPose.Z,
                                Point = nearbyPoint
                            };
                            ToolTipService.SetBetweenShowDelay(animationTimePointItem, 0);
                            animationTimePointItem.MouseRightButtonUp += AnimationTimePointItem_MouseRightButtonUp;
                            ToolTipService.SetInitialShowDelay(animationTimePointItem, 0);
                            RightArmAnimationPointPanel.Items.Add(animationTimePointItem);
                            break;
                        }
                    case "LeftLeg":
                        {
                            currentPose = animationContainer.LeftLegPose;
                            AnimationTimePointItem animationTimePointItem = new(currentPoint.Position.X + 15 / 4)
                            {
                                Pose = currentPose,
                                ToolTip = "LeftLeg=" + ((index + 1) / 5.0).ToString("F1") + "s\r\nX=" + currentPose.X + "\r\nY=" + currentPose.Y + "\r\nZ=" + currentPose.Z,
                                Point = nearbyPoint
                            };
                            ToolTipService.SetBetweenShowDelay(animationTimePointItem, 0);
                            ToolTipService.SetInitialShowDelay(animationTimePointItem, 0);
                            animationTimePointItem.MouseRightButtonUp += AnimationTimePointItem_MouseRightButtonUp;
                            LeftLegAnimationPointPanel.Items.Add(animationTimePointItem);
                            break;
                        }
                    case "RightLeg":
                        {
                            currentPose = animationContainer.RightLegPose;
                            AnimationTimePointItem animationTimePointItem = new(currentPoint.Position.X + 15 / 4)
                            {
                                Pose = currentPose,
                                ToolTip = "RightLeg=" + ((index + 1) / 5.0).ToString("F1") + "s\r\nX=" + currentPose.X + "\r\nY=" + currentPose.Y + "\r\nZ=" + currentPose.Z,
                                Point = nearbyPoint
                            };
                            ToolTipService.SetBetweenShowDelay(animationTimePointItem, 0);
                            ToolTipService.SetInitialShowDelay(animationTimePointItem, 0);
                            animationTimePointItem.MouseRightButtonUp += AnimationTimePointItem_MouseRightButtonUp;
                            RightLegAnimationPointPanel.Items.Add(animationTimePointItem);
                            break;
                        }
                }
            }
        }

        private AnimationUnitTimePoint GetNearbyFrameItem(double offset)
        {
            AnimationUnitTimePoint result = null;
            double value = double.MaxValue;
            foreach (var item in animationTimeScale.ScaleList)
            {
                if (Math.Abs(item.Position.X - offset) < Math.Abs(value))
                {
                    value = item.Position.X - offset;
                    result = item;
                }
            }
            return result;
        }

        /// <summary>
        /// 右击删除关键帧
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnimationTimePointItem_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AnimationTimePointItem animationTimePointItem = sender as AnimationTimePointItem;
            ItemsControl itemsControl = animationTimePointItem.FindParent<ItemsControl>();
            itemsControl?.Items.Remove(animationTimePointItem);
        }
    }
}
