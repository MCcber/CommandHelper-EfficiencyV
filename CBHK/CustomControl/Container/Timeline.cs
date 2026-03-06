using CBHK.CustomControl.Input;
using CBHK.Utility.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Converters;
using System.Windows.Shapes;

namespace CBHK.CustomControl.Container
{
    public class Timeline : Control
    {
        #region Field
        private Line previewLine;
        private Thumb playHeadThumb;
        private Grid playHeadGrid;
        private Canvas canvas;
        private AnimationTimelineTool animationTimelineTool = new();
        private ScrollViewer headScrollViewer;
        private ScrollViewer contentViewer;
        private TimelineTrack lastSelectedTrack;
        private List<ItemsControl> trackPanelList = [];
        #endregion

        #region Property
        public TimeRulerElement Ruler { get; set; }
        public Action TimeUpdateAction { get; set; }

        public bool IsSplitModeOpened { get; set; }

        public TimelineClip CurrentSplitTimelineClip { get; set; }

        public ObservableCollection<TimelineTrack> TrackList
        {
            get { return (ObservableCollection<TimelineTrack>)GetValue(TrackListProperty); }
            set { SetValue(TrackListProperty, value); }
        }

        public static readonly DependencyProperty TrackListProperty =
            DependencyProperty.Register("TrackList", typeof(ObservableCollection<TimelineTrack>), typeof(Timeline), new PropertyMetadata(default(ObservableCollection<TimelineTrack>)));

        public Thickness TrackPanelMargin
        {
            get { return (Thickness)GetValue(TrackPanelMarginProperty); }
            set { SetValue(TrackPanelMarginProperty, value); }
        }

        public static readonly DependencyProperty TrackPanelMarginProperty =
            DependencyProperty.Register("TrackPanelMargin", typeof(Thickness), typeof(Timeline), new PropertyMetadata(default(Thickness)));

        public double TrackPanelWidth
        {
            get { return (double)GetValue(TrackPanelWidthProperty); }
            set { SetValue(TrackPanelWidthProperty, value); }
        }

        public static readonly DependencyProperty TrackPanelWidthProperty =
            DependencyProperty.Register("TrackPanelWidth", typeof(double), typeof(Timeline), new PropertyMetadata(default(double)));

        public TimeSpan CurrentTime
        {
            get { return (TimeSpan)GetValue(CurrentTimeProperty); }
            set { SetValue(CurrentTimeProperty, value); }
        }

        public static readonly DependencyProperty CurrentTimeProperty =
            DependencyProperty.Register("CurrentTime", typeof(TimeSpan), typeof(Timeline), new PropertyMetadata(default(TimeSpan), OnCurrentTimeChanged));

        public TimelineTrack CurrentTrack
        {
            get { return (TimelineTrack)GetValue(CurrentTrackProperty); }
            set { SetValue(CurrentTrackProperty, value); }
        }

        public static readonly DependencyProperty CurrentTrackProperty =
            DependencyProperty.Register("CurrentTrack", typeof(TimelineTrack), typeof(Timeline), new PropertyMetadata(default(TimelineTrack)));

        public bool IsShowPreviewLine
        {
            get { return (bool)GetValue(IsShowPreviewLineProperty); }
            set { SetValue(IsShowPreviewLineProperty, value); }
        }

        public static readonly DependencyProperty IsShowPreviewLineProperty =
            DependencyProperty.Register("IsShowPreviewLine", typeof(bool), typeof(Timeline), new PropertyMetadata(default(bool)));
        #endregion

        #region Method
        public Timeline()
        {
            Loaded += Timeline_Loaded;
        }

        private void Timeline_Loaded(object sender, RoutedEventArgs e)
        {
            TrackPanelMargin = new(0, 30, 0, 0);
        }

        public void UpdateTotalWidth()
        {
            if (canvas is null || Ruler is null)
            {
                return;
            }

            // 基础宽度 = 总秒数 * 每秒像素 * 缩放
            double baseWidth = Ruler.Maximum * Ruler.BasePixelsPerSecond * Ruler.ZoomFactor;

            // 加上 80px 补偿（最后一帧的 "19t" 文字宽度 + 播放头 Thumb 宽度）
            canvas.Width = baseWidth + 80;
            Ruler.Width = canvas.Width;

            canvas.UpdateLayout();
        }

        private static TimeSpan SnapToTick(double seconds)
        {
            // 20fps 对应 0.05s 一个 Tick
            // 算法：秒数 * 20 -> 四舍五入取整 -> 再除以 20
            double tickCount = Math.Round(seconds * 20);
            return TimeSpan.FromSeconds(tickCount / 20.0);
        }

        public double GetTrackHeight()
        {
            double headHeight = 50.0;
            if (TrackList.Count > 0)
            {
                headHeight = contentViewer.ActualHeight / TrackList.Count;
            }
            if (headHeight < 50)
            {
                headHeight = 50;
            }
            return headHeight;
        }

        /// <summary>
        /// 在当前时间刻度下添加关键帧
        /// </summary>
        public void AddTimelineClip()
        {
            double trackheight = GetTrackHeight();
            TimelineClip timelineClip = new()
            {
                Style = Application.Current.Resources["TimelineClipStyle"] as Style,
                OriginStartTime = CurrentTime,
                OriginCanvasTop = trackheight / 2,
                Ruler = Ruler,
                ParentTimeline = this,
                ParentPanel = trackPanelList[TrackList.IndexOf(CurrentTrack)]
            };

            CurrentTrack.TimelineClipList.Add(timelineClip);
        }

        /// <summary>
        /// 在指定时间刻度下添加动画关键帧
        /// </summary>
        /// <param name="time"></param>
        public void AddTimelineClip(TimeSpan time)
        {
            double trackheight = GetTrackHeight();
            TimelineClip timelineClip = new() 
            {
                Style = Application.Current.Resources["TimelineClipStyle"] as Style,
                OriginCanvasTop = trackheight / 2,
                OriginStartTime = time,
                Ruler = Ruler,
                ParentTimeline = this,
                ParentPanel = trackPanelList[TrackList.IndexOf(CurrentTrack)]
            };

            double positionX = animationTimelineTool.ConvertTimeToPixel(time, Ruler);
            Canvas.SetLeft(timelineClip, positionX);
            CurrentTrack.TimelineClipList.Add(timelineClip);
        }

        /// <summary>
        /// 合并为动画片段
        /// </summary>
        /// <param name="title"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="timePointList"></param>
        public void AddTimelineClip(string title,TimeSpan start,TimeSpan end,List<double> timePointList)
        {
            double trackheight = GetTrackHeight();

            TimelineClip rectangleTimelineClip = new()
            {
                ZoomFactor = Ruler.ZoomFactor,
                RectangleModeHeight = trackheight,
                OriginCanvasTop = trackheight / 2,
                OriginStartTime = start,
                OriginEndTime = end,
                CurrentClipMode = Model.Common.ClipMode.Rectangle,
                Title = title,
                Style = Application.Current.Resources["TimelineClipStyle"] as Style,
                Ruler = Ruler,
                ParentTimeline = this,
                ParentPanel = trackPanelList[TrackList.IndexOf(CurrentTrack)]
            };

            foreach (var timePointItem in timePointList)
            {
                TimeSpan timeSpan = animationTimelineTool.ConvertPixelToTime(timePointItem, Ruler);
                ContinuousKeyframeItem continuousKeyframeItem = new()
                {
                    Width = 16,
                    Height = 16,
                    CurrentTime = timeSpan,
                    X = timePointItem,
                    Style = Application.Current.Resources["ContinuousKeyframeItemStyle"] as Style
                };
                rectangleTimelineClip.InnerKeyFrameList.Add(continuousKeyframeItem);
            }
            CurrentTrack.TimelineClipList.Add(rectangleTimelineClip);
        }

        public void RemoveTimelineClip()
        {
            for (int j = 0; j < CurrentTrack.TimelineClipList.Count; j++)
            {
                if (CurrentTrack.TimelineClipList[j].IsChecked is bool isChecked && isChecked)
                {
                    CurrentTrack.TimelineClipList.RemoveAt(j);
                    j--;
                }
            }
        }

        public void AddTrack(string title)
        {
            SolidColorBrush trackBackground = new((Color)ColorConverter.ConvertFromString("#CCD5F0"));
            SolidColorBrush trackHeadBackground = new((Color)ColorConverter.ConvertFromString("#CCD5F0"));
            TimelineTrack timelineTrack = new() { Padding = new(5), TrackName = title, Foreground = Brushes.Black, Background = trackBackground,TrackHeadBackground = trackHeadBackground };
            TrackList.Add(timelineTrack);

            double headHeight = GetTrackHeight();
            foreach (var item in TrackList)
            {
                item.Height = headHeight;
                item.HeadHeight = headHeight;
            }
        }

        public void RemoveTrack()
        {
            if(CurrentTrack is not null)
            {
                TrackList.Remove(CurrentTrack);
            }
        }

        /// <summary>
        /// 拆分动画片段
        /// </summary>
        public void SplitClip()
        {
            if(CurrentSplitTimelineClip is not null)
            {
                #region 字段
                TimeSpan keyFrameItemStartTime = CurrentSplitTimelineClip.StartTime;
                TimeSpan splitTime = animationTimelineTool.ConvertPixelToTime(CurrentSplitTimelineClip.SplitPreviewLineOffsetX, Ruler) + keyFrameItemStartTime;
                double trackheight = GetTrackHeight();
                bool isOnKeyFrame = false;
                int splitKeyFrameIndex = -1;
                List<ContinuousKeyframeItem> keyFrameList = [];
                TimeSpan leftTime = TimeSpan.FromHours(24);
                #endregion

                #region 搜索分割位置，正好在帧成员上或者在它们之间
                for (int i = 0; i < CurrentSplitTimelineClip.InnerKeyFrameList.Count; i++)
                {
                    TimeSpan currentKeyFrameTime = CurrentSplitTimelineClip.InnerKeyFrameList[i].CurrentTime + keyFrameItemStartTime;
                    if (currentKeyFrameTime >= CurrentTime && leftTime > currentKeyFrameTime)
                    {
                        splitKeyFrameIndex = i;
                        leftTime = currentKeyFrameTime;
                    }

                    isOnKeyFrame = currentKeyFrameTime == CurrentTime;
                    if (isOnKeyFrame)
                    {
                        splitKeyFrameIndex = i;
                        leftTime = currentKeyFrameTime;
                        break;
                    }
                }
                #endregion

                #region 保留旧的帧成员，标记当前选中的片段
                keyFrameList = [.. CurrentSplitTimelineClip.InnerKeyFrameList.Skip(splitKeyFrameIndex)];
                CurrentSplitTimelineClip.IsDivided = true;
                #endregion

                #region 生成新的动画片段以及它的左侧第一帧
                TimelineClip newClip = new()
                {
                    IsSplitOut = true,
                    InnerKeyFrameList = new(keyFrameList),
                    ZoomFactor = Ruler.ZoomFactor,
                    RectangleModeHeight = trackheight,
                    CurrentClipMode = Model.Common.ClipMode.Rectangle,
                    Style = Application.Current.Resources["TimelineClipStyle"] as Style,
                    OriginStartTime = splitTime,
                    OriginEndTime = CurrentSplitTimelineClip.EndTime,
                    OriginCanvasTop = CurrentSplitTimelineClip.OriginCanvasTop,
                    Ruler = Ruler,
                    Title = CurrentSplitTimelineClip.Title,
                    ParentTimeline = this,
                    ParentPanel = trackPanelList[TrackList.IndexOf(CurrentTrack)]
                };

                ContinuousKeyframeItem newKeyFrameItem = new()
                {
                    IsBorderKeyFrame = true,
                    Width = 16,
                    Height = 16,
                    CurrentTime = splitTime,
                    X = animationTimelineTool.ConvertTimeToPixel(CurrentTime, Ruler),
                    Style = Application.Current.Resources["ContinuousKeyframeItemStyle"] as Style
                }; 

                newClip.InnerKeyFrameList.Add(newKeyFrameItem);
                newClip.LeftBorderFrameItem = newKeyFrameItem;
                #endregion

                #region 没有在帧成员上分割时
                if (!isOnKeyFrame)
                {
                    ContinuousKeyframeItem selectedEndKeyFrameItem = new()
                    {
                        IsBorderKeyFrame = true,
                        Width = 16,
                        Height = 16,
                        CurrentTime = splitTime,
                        X = animationTimelineTool.ConvertTimeToPixel(CurrentTime, Ruler),
                        Style = Application.Current.Resources["ContinuousKeyframeItemStyle"] as Style
                    };
                    CurrentSplitTimelineClip.UpdateStateAction = () =>
                    {
                        if (CurrentSplitTimelineClip.RightBorderFrameItem is not null)
                        {
                            CurrentSplitTimelineClip.RightBorderFrameItem.IsBorderKeyFrame = false;
                        }
                        CurrentSplitTimelineClip.RightBorderFrameItem = selectedEndKeyFrameItem;
                    };
                    CurrentSplitTimelineClip.InnerKeyFrameList = [.. CurrentSplitTimelineClip.InnerKeyFrameList.Take(splitKeyFrameIndex)];
                    CurrentSplitTimelineClip.InnerKeyFrameList.Add(selectedEndKeyFrameItem);
                }
                #endregion

                #region 处理动画片段分割后的宽度
                CurrentTrack.TimelineClipList.Add(newClip);
                CurrentSplitTimelineClip.EndTime = splitTime;
                double startValue = animationTimelineTool.ConvertTimeToPixel(CurrentSplitTimelineClip.StartTime, Ruler);
                double endValue = animationTimelineTool.ConvertTimeToPixel(CurrentSplitTimelineClip.EndTime, Ruler);
                CurrentSplitTimelineClip.RectangleModeWidth = endValue - startValue; 
                #endregion
            }
        }
        #endregion

        #region Event
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            playHeadThumb = GetTemplateChild("playerHeadThumb") as Thumb;
            playHeadGrid = GetTemplateChild("playHeadGrid") as Grid;
            Ruler = GetTemplateChild("Ruler") as TimeRulerElement;
            canvas = GetTemplateChild("canvas") as Canvas;
            previewLine = GetTemplateChild("previewLine") as Line;
            TrackList = [];

            if (canvas is not null)
            {
                canvas.MouseEnter += (sender,e) => 
                {
                    if (previewLine is not null && IsShowPreviewLine)
                    {
                        previewLine.Visibility = Visibility.Visible;
                    }
                };
                canvas.MouseLeave += (sender, e) =>
                {
                    if (previewLine is not null)
                    {
                        previewLine.Visibility = Visibility.Hidden;
                    }
                };
                canvas.MouseMove += Canvas_MouseMove;
                canvas.PreviewMouseLeftButtonUp += Canvas_PreviewMouseLeftButtonUp;
            }

            if (playHeadThumb is not null)
            {
                playHeadThumb.DragDelta += PlayHeadThumb_DragDelta;
            }

            if(playHeadGrid is not null)
            {
                playHeadGrid.MouseEnter += (sender,e) => { Cursor = Cursors.ScrollWE; };
                playHeadGrid.MouseLeave += (sender,e) => { Cursor = Cursors.Arrow; };
            }

            UpdateTotalWidth();
        }

        public void HorizontalScrollViewer_Loaded(object sender,RoutedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollChanged += HorizontalScrollViewer_ScrollChanged;
            }
        }

        public void HeadScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            headScrollViewer = sender as ScrollViewer;
        }

        /// <summary>
        /// 设置高度、绑定头标滚动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ContentScrollViewer_Loaded(object sender,RoutedEventArgs e)
        {
            if(sender is ScrollViewer scrollViewer)
            {
                contentViewer = scrollViewer;
                scrollViewer.ScrollChanged += ContentScrollViewer_ScrollChanged;
            }
        }

        private void HorizontalScrollViewer_ScrollChanged(object sender,ScrollChangedEventArgs e)
        {
            contentViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
        }

        /// <summary>
        /// 让头标视图的垂直轴与内容视图同步
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContentScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            headScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
        }

        /// <summary>
        /// 绑定轨道宽度
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Canvas_Loaded(object sender,RoutedEventArgs e)
        {
            if (sender is Canvas canvas)
            {
                TrackPanelWidth = canvas.ActualWidth - TrackPanelMargin.Left;
            }
        }

        public void Track_Loaded(object sender,RoutedEventArgs e)
        {
            if(sender is ItemsControl itemsControl)
            {
                trackPanelList.Add(itemsControl);
            }
        }

        /// <summary>
        /// 左键按下后更新当前所选的轨道
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Track_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ItemsControl itemsControl && itemsControl.DataContext is TimelineTrack timelineTrack)
            {
                #region 切换选中的轨道
                if (timelineTrack != CurrentTrack)
                {

                    if (CurrentTrack is not null)
                    {
                        foreach (var item in CurrentTrack.TimelineClipList)
                        {
                            if (item.CurrentClipMode is Model.Common.ClipMode.Rectangle)
                            {
                                item.IsChecked = false;
                                item.BorderBrush = Brushes.White;
                            }
                        }
                    }
                    timelineTrack.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EAEAF2"));
                    if (lastSelectedTrack is not null)
                    {
                        lastSelectedTrack.BorderBrush = Brushes.Transparent;
                    }
                    CurrentTrack = timelineTrack;
                    lastSelectedTrack = CurrentTrack;
                }
                #endregion

                #region 切割动画片段
                if (IsSplitModeOpened)
                {
                    SplitClip();
                } 
                #endregion
            }
        }

        /// <summary>
        /// 左击抬起后取消所有轨道的所有连续关键这的调整标记
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Track_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            foreach (var track in TrackList)
            {
                foreach (var timelineClip in track.TimelineClipList)
                {
                    timelineClip.IsAdjustingFirstSize = timelineClip.IsAdjustingLastSize = false;
                }
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if(previewLine is null || !IsShowPreviewLine)
            {
                return;
            }
            double rawSeconds = animationTimelineTool.ConvertPixelToTime(e.GetPosition(canvas).X,Ruler).TotalSeconds;
            TimeSpan currentTime = SnapToTick(rawSeconds);
            double pixelX = animationTimelineTool.ConvertTimeToPixel(currentTime, Ruler);
            pixelX = Math.Clamp(pixelX, 0, canvas.ActualWidth);

            Canvas.SetLeft(previewLine, pixelX);
        }

        /// <summary>
        /// 播放指针拖拽
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayHeadThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (canvas is null || Ruler is null || playHeadGrid is null)
            {
                return;
            }

            double currentLeft = Canvas.GetLeft(playHeadGrid);
            if (double.IsNaN(currentLeft)) currentLeft = 0;

            // 1. 计算鼠标位移后的临时位置
            double newX = currentLeft + e.HorizontalChange;
            // 2. 将临时位置转换为“吸附后”的时间
            CurrentTime = animationTimelineTool.ConvertPixelToTime(newX, Ruler);
        }

        /// <summary>
        /// 移动播放指针到指定位置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Canvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (canvas is not null && Ruler is not null)
            {
                double rawSeconds = animationTimelineTool.ConvertPixelToTime(e.GetPosition(canvas).X, Ruler).TotalSeconds;
                //同样执行吸附
                CurrentTime = SnapToTick(rawSeconds);
            }
        }

        private static void OnCurrentTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Timeline timeline && e.NewValue is TimeSpan newValue)
            {
                timeline.OnCurrentTimeChanged(newValue);
            }
        }

        private void OnCurrentTimeChanged(TimeSpan newValue)
        {
            double newX = animationTimelineTool.ConvertTimeToPixel(newValue, Ruler);
            Canvas.SetLeft(playHeadGrid, newX);
            TimeUpdateAction?.Invoke();
        }

        /// <summary>
        /// 根据时间更新播放指针的位置
        /// </summary>
        public void UpdateStateByCurrentTime()
        {
            // 每次更新位置前，先确保画布宽度是对的
            UpdateTotalWidth();

            #region 更新播放指针的位置
            if (playHeadGrid is not null && Ruler is not null)
            {
                double positionX = animationTimelineTool.ConvertTimeToPixel(CurrentTime, Ruler);
                Canvas.SetLeft(playHeadGrid, positionX);
                for (int i = 0; i < TrackList.Count; i++)
                {
                    for (int j = 0; j < TrackList[i].TimelineClipList.Count; j++)
                    {
                        TrackList[i].TimelineClipList[j].UpdateClipByCurrentTime();
                    }
                }
            }
            #endregion

            #region 更新内部帧成员的相对位置
            double offset = 0.0;
            for (int i = 0; i < TrackList.Count; i++)
            {
                for (int j = 0; j < TrackList[i].TimelineClipList.Count; j++)
                {
                    for (int k = 0; k < TrackList[i].TimelineClipList[j].InnerKeyFrameList.Count; k++)
                    {
                        ContinuousKeyframeItem continuousKeyframeItem = TrackList[i].TimelineClipList[j].InnerKeyFrameList[k];
                        double currentTimeValue = animationTimelineTool.ConvertTimeToPixel(continuousKeyframeItem.CurrentTime, Ruler);
                        if(continuousKeyframeItem.CurrentTime.TotalSeconds == 0)
                        {
                            offset = currentTimeValue;
                        }

                        Canvas.SetLeft(continuousKeyframeItem, currentTimeValue - offset - (continuousKeyframeItem.Width / 2));
                    }
                }
            } 
            #endregion
        }
        #endregion
    }
}