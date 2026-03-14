using CBHK.CustomControl.Input;
using CBHK.Interface;
using CBHK.Model.Common;
using CBHK.Utility.Common;
using CBHK.Utility.MessageTip;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CBHK.CustomControl.Container
{
    public class Timeline : Control
    {
        #region Field
        private bool isSampling = false;
        private Line previewLine;
        private Thumb playHeadThumb;
        private Grid playHeadGrid;
        private Canvas canvas;
        private AnimationTimelineTool animationTimelineTool = new();
        private ScrollViewer headScrollViewer;
        private ScrollViewer contentViewer;
        private TimelineTrack lastSelectedTrack;
        private List<ItemsControl> trackPanelList = [];
        private Dictionary<ITimelineElement, (TimeSpan, List<IKeyFrameData>)> TimelineElementMarkerMap = [];
        private IComparer<ITimelineElement> timeComparer = Comparer<ITimelineElement>.Create((a, b) => a.StartTime.CompareTo(b.StartTime));
        private List<ITimelineElement> TimelineElementMarkerList = [];
        private List<IKeyFrameData> randomKeyFrameDataList = [];
        private IProgress<ObservableCollection<IKeyFrameData>> DataSampleProgress = null;

        private TimeSpan lastRenderTime = TimeSpan.Zero;
        private int playDirection = 1; // 1 为正向，-1 为倒放
        private const double TickStep = 0.05; // mcJava 步进基准 (1 Tick = 0.05s)
        #endregion

        #region Property
        public TimeRulerElement Ruler { get; set; }
        public Action TimeUpdateAction { get; set; }

        public bool IsSplitModeOpened { get; set; }

        public TimelineClip CurrentSplitTimelineClip { get; set; }
        public bool IsPlaying { get; set; }
        public TimeSpan MemoryCurrentTime { get; set; }

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
            CompositionTarget.Rendering += Timeline_Rendering;
            DataSampleProgress = new Progress<ObservableCollection<IKeyFrameData>>(DataSample);
            for (int i = 0; i < 100; i++)
            {
                byte[] bytes = [0,1,2,3,4,5,6,7,8,9,10];
                randomKeyFrameDataList.Add(new KeyFrameData<byte>() { Easing = InterpolationType.Linear, Value = bytes[Random.Shared.Next(0, 10)] });
            }
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
                    for (int j = 0; j < TrackList[i].TimelineElementList.Count; j++)
                    {
                        if (TrackList[i].TimelineElementList[j] is TimelineClip clip)
                        {
                            clip.UpdateClipByCurrentTime();
                        }
                    }
                }
            }
            #endregion

            #region 更新内部帧成员的相对位置
            double offset = 0.0;
            for (int i = 0; i < TrackList.Count; i++)
            {
                for (int j = 0; j < TrackList[i].TimelineElementList.Count; j++)
                {
                    if (TrackList[i].TimelineElementList[j] is TimelineClip clip)
                    {
                        for (int k = 0; k < clip.InnerKeyFrameList.Count; k++)
                        {
                            ContinuousKeyframeItem continuousKeyframeItem = clip.InnerKeyFrameList[k];
                            double currentTimeValue = animationTimelineTool.ConvertTimeToPixel(continuousKeyframeItem.CurrentTime, Ruler);
                            if (continuousKeyframeItem.CurrentTime.TotalSeconds == 0)
                            {
                                offset = currentTimeValue;
                            }

                            Canvas.SetLeft(continuousKeyframeItem, currentTimeValue - offset - (continuousKeyframeItem.Width / 2));
                        }
                    }
                }
            }
            #endregion
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
        public void AddTimelineElement()
        {
            double trackheight = GetTrackHeight();
            TimelineKeyFrame timelineKeyFrame = new()
            {
                Style = Application.Current.Resources["TimelineKeyFrameStyle"] as Style,
                StartTime = CurrentTime,
                OriginCanvasTop = trackheight / 2,
                Ruler = Ruler,
                ParentTimeline = this,
                ParentPanel = trackPanelList[TrackList.IndexOf(CurrentTrack)]
            };

            double positionX = animationTimelineTool.ConvertTimeToPixel(CurrentTime, Ruler);
            Canvas.SetLeft(timelineKeyFrame, positionX);
            CurrentTrack.TimelineElementList.Add(timelineKeyFrame);

            timelineKeyFrame.DataList.Add(randomKeyFrameDataList[Random.Shared.Next(0, randomKeyFrameDataList.Count)]);
            TimelineElementMarkerMap.Add(timelineKeyFrame, (CurrentTime, [.. timelineKeyFrame.DataList]));
            int index = TimelineElementMarkerList.BinarySearch(timelineKeyFrame, timeComparer);
            if (index < 0)
            {
                index = ~index;
            }
            TimelineElementMarkerList.Insert(index, timelineKeyFrame);
        }

        /// <summary>
        /// 在指定时间刻度下添加动画关键帧
        /// </summary>
        /// <param name="time"></param>
        public void AddTimelineElement(TimeSpan time)
        {
            double trackheight = GetTrackHeight();
            TimelineKeyFrame timelineKeyFrame = new() 
            {
                Style = Application.Current.Resources["timelineKeyFrameStyle"] as Style,
                OriginCanvasTop = trackheight / 2,
                StartTime = time,
                Ruler = Ruler,
                ParentTimeline = this,
                ParentPanel = trackPanelList[TrackList.IndexOf(CurrentTrack)]
            };

            double positionX = animationTimelineTool.ConvertTimeToPixel(time, Ruler);
            Canvas.SetLeft(timelineKeyFrame, positionX);
            CurrentTrack.TimelineElementList.Add(timelineKeyFrame);

            timelineKeyFrame.DataList.Add(randomKeyFrameDataList[Random.Shared.Next(0, randomKeyFrameDataList.Count)]);
            TimelineElementMarkerMap.Add(timelineKeyFrame, (time, [.. timelineKeyFrame.DataList]));
            int index = TimelineElementMarkerList.BinarySearch(timelineKeyFrame, timeComparer);
            if (index < 0)
            {
                index = ~index;
            }
            TimelineElementMarkerList.Insert(index, timelineKeyFrame);
        }

        /// <summary>
        /// 合并为动画片段
        /// </summary>
        /// <param name="title"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="timePointList"></param>
        public void AddTimelineClip(string title,TimeSpan start,TimeSpan end,List<double> timePointList,List<ObservableCollection<IKeyFrameData>> keyFrameDataList)
        {
            double trackheight = GetTrackHeight();

            TimelineClip rectangleTimelineClip = new()
            {
                ZoomFactor = Ruler.ZoomFactor,
                RectangleModeHeight = trackheight,
                OriginCanvasTop = trackheight / 2,
                OriginStartTime = start,
                OriginEndTime = end,
                Title = title,
                Style = Application.Current.Resources["TimelineClipStyle"] as Style,
                Ruler = Ruler,
                ParentTimeline = this,
                ParentPanel = trackPanelList[TrackList.IndexOf(CurrentTrack)]
            };

            for (int i = 0; i < timePointList.Count; i++)
            {
                TimeSpan timeSpan = animationTimelineTool.ConvertPixelToTime(timePointList[i], Ruler);
                bool isBorderKeyFrame = timeSpan == TimeSpan.Zero || timeSpan + start == end;
                ContinuousKeyframeItem continuousKeyframeItem = new()
                {
                    DataList = new(keyFrameDataList[i]),
                    IsBorderKeyFrame = isBorderKeyFrame,
                    Width = 16,
                    Height = 16,
                    CurrentTime = timeSpan,
                    X = timePointList[i],
                    Style = Application.Current.Resources["ContinuousKeyframeItemStyle"] as Style
                };
                rectangleTimelineClip.InnerKeyFrameList.Add(continuousKeyframeItem);
            }
            CurrentTrack.TimelineElementList.Add(rectangleTimelineClip);

            //rectangleTimelineClip.DataList.Add(randomKeyFrameDataList[Random.Shared.Next(0, randomKeyFrameDataList.Count)]);
            //TimelineElementMarkerMap.Add(rectangleTimelineClip, (start, [.. rectangleTimelineClip.DataList]));
            int index = TimelineElementMarkerList.BinarySearch(rectangleTimelineClip, timeComparer);
            if (index < 0)
            {
                index = ~index;
            }
            TimelineElementMarkerList.Insert(index, rectangleTimelineClip);
        }

        public void RemoveTimelineElement()
        {
            for (int j = 0; j < CurrentTrack.TimelineElementList.Count; j++)
            {
                if (CurrentTrack.TimelineElementList[j] is TimelineClip clip && clip.IsChecked is bool isChecked && isChecked)
                {
                    CurrentTrack.TimelineElementList.RemoveAt(j);
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
                ContinuousKeyframeItem splitKeyFrameItem = null;
                TimeSpan localStartTime = CurrentSplitTimelineClip.StartTime;
                TimeSpan localSplitTime = animationTimelineTool.ConvertPixelToTime(CurrentSplitTimelineClip.SplitPreviewLineOffsetX, Ruler);
                double trackheight = GetTrackHeight();
                bool isOnKeyFrame = false;
                List<ContinuousKeyframeItem> keyFrameList = [];
                #endregion

                #region 搜索分割位置，正好在帧成员上或者在它们之间
                TimeSpan globalSplitTime = localSplitTime + localStartTime;
                TimeSpan currentKeyFrameTime = new(); 
                
                for (int i = 0; i < CurrentSplitTimelineClip.InnerKeyFrameList.Count; i++)
                {
                    currentKeyFrameTime = CurrentSplitTimelineClip.InnerKeyFrameList[i].CurrentTime;
                    if (currentKeyFrameTime > localSplitTime)
                    {
                        CurrentSplitTimelineClip.InnerKeyFrameList[i].CurrentTime -= localSplitTime;
                        CurrentSplitTimelineClip.InnerKeyFrameList[i].X = animationTimelineTool.ConvertTimeToPixel(CurrentSplitTimelineClip.InnerKeyFrameList[i].CurrentTime, Ruler);
                        keyFrameList.Add(CurrentSplitTimelineClip.InnerKeyFrameList[i]);
                    }

                    if (!isOnKeyFrame)
                    {
                        isOnKeyFrame = currentKeyFrameTime == localSplitTime;
                        if (isOnKeyFrame)
                        {
                            splitKeyFrameItem = CurrentSplitTimelineClip.InnerKeyFrameList[i];
                        }
                    }
                }

                //如果分割点在动画片段的边缘则不处理
                if(splitKeyFrameItem is not null && splitKeyFrameItem.IsBorderKeyFrame)
                {
                    return;
                }
                #endregion

                #region 生成新的动画片段以及它的左侧第一帧
                TimelineClip newClip = new()
                {
                    IsSplitOut = true,
                    InnerKeyFrameList = new(keyFrameList),
                    ZoomFactor = Ruler.ZoomFactor,
                    RectangleModeHeight = trackheight,
                    Style = Application.Current.Resources["TimelineClipStyle"] as Style,
                    OriginStartTime = globalSplitTime,
                    OriginEndTime = CurrentSplitTimelineClip.EndTime,
                    OriginCanvasTop = CurrentSplitTimelineClip.OriginCanvasTop,
                    Ruler = Ruler,
                    Title = CurrentSplitTimelineClip.Title,
                    ParentTimeline = this,
                    ParentPanel = trackPanelList[TrackList.IndexOf(CurrentTrack)]
                };
                double globalNewClipStartPositionX = animationTimelineTool.ConvertTimeToPixel(newClip.OriginStartTime, Ruler);
                double positionX = 0.0;
                for (int i = 0; i < keyFrameList.Count; i++)
                {
                    CurrentSplitTimelineClip.RemoveKeyFrameItem(keyFrameList[i]);
                    if (keyFrameList[i].IsBorderKeyFrame)
                    {
                        positionX = animationTimelineTool.ConvertTimeToPixel(newClip.OriginEndTime - globalSplitTime, Ruler);
                        newClip.RightBorderFrameItem = keyFrameList[i];
                    }
                    else
                    {
                        positionX = animationTimelineTool.ConvertTimeToPixel(keyFrameList[i].CurrentTime, Ruler);
                    }
                    keyFrameList[i].X = positionX;
                }

                ContinuousKeyframeItem newKeyFrameItem = new()
                {
                    IsBorderKeyFrame = true,
                    Width = 16,
                    Height = 16,
                    CurrentTime = TimeSpan.Zero,
                    Style = Application.Current.Resources["ContinuousKeyframeItemStyle"] as Style
                };

                newClip.InnerKeyFrameList.Insert(0, newKeyFrameItem);
                newClip.LeftBorderFrameItem = newKeyFrameItem;
                #endregion

                #region 没有在帧成员上分割时以及在帧成员上分割两种情况
                if (CurrentSplitTimelineClip.RightBorderFrameItem is not null)
                {
                    CurrentSplitTimelineClip.RightBorderFrameItem.IsBorderKeyFrame = false;
                }
                if (!isOnKeyFrame)
                {
                    double size = 16;
                    ContinuousKeyframeItem selectedEndKeyFrameItem = new()
                    {
                        IsChecked = true,
                        IsBorderKeyFrame = true,
                        Width = size,
                        Height = size,
                        CurrentTime = localSplitTime,
                        X = animationTimelineTool.ConvertTimeToPixel(localSplitTime, Ruler) - (Math.Sqrt(Math.Pow(size, 2) + Math.Pow(size, 2)) / 2),
                        Style = Application.Current.Resources["ContinuousKeyframeItemStyle"] as Style
                    };

                    CurrentSplitTimelineClip.RightBorderFrameItem = selectedEndKeyFrameItem;
                    CurrentSplitTimelineClip.InnerKeyFrameList = [.. CurrentSplitTimelineClip.InnerKeyFrameList.Except(keyFrameList)];
                    for (int i = 0; i < keyFrameList.Count; i++)
                    {
                        CurrentSplitTimelineClip.RemoveKeyFrameItem(keyFrameList[i]);
                    }
                    CurrentSplitTimelineClip.InnerKeyFrameList.Add(selectedEndKeyFrameItem);
                    CurrentSplitTimelineClip.AddKeyFrameItem(selectedEndKeyFrameItem, selectedEndKeyFrameItem.X);
                }
                else
                if(splitKeyFrameItem is not null)
                {
                    CurrentSplitTimelineClip.RightBorderFrameItem = splitKeyFrameItem;
                    splitKeyFrameItem.IsChecked = true;
                    splitKeyFrameItem.IsBorderKeyFrame = true;
                    splitKeyFrameItem.CurrentTime = localStartTime;
                    splitKeyFrameItem.X = animationTimelineTool.ConvertTimeToPixel(localStartTime, Ruler) - (Math.Sqrt(Math.Pow(splitKeyFrameItem.Width, 2) + Math.Pow(splitKeyFrameItem.Width, 2)) / 2);
                    Canvas.SetLeft(splitKeyFrameItem, splitKeyFrameItem.X);
                    CurrentSplitTimelineClip.ProcessDivided();
                }
                #endregion

                #region 处理动画片段分割后的宽度
                CurrentTrack.TimelineElementList.Add(newClip);
                CurrentSplitTimelineClip.EndTime = globalSplitTime;
                double startValue = animationTimelineTool.ConvertTimeToPixel(CurrentSplitTimelineClip.StartTime, Ruler);
                double endValue = animationTimelineTool.ConvertTimeToPixel(CurrentSplitTimelineClip.EndTime, Ruler);
                CurrentSplitTimelineClip.RectangleModeWidth = endValue - startValue;
                #endregion
            }
        }

        /// <summary>
        /// 水平翻转动画片段
        /// </summary>
        public void HorizontalFlip()
        {
            #region 检测是否可以翻转
            List<ITimelineElement> selectedElementList = [.. CurrentTrack.TimelineElementList.Where(item => item is TimelineClip clip && clip.IsChecked is not null && clip.IsChecked.Value)];

            if (selectedElementList.Count == 0)
            {
                Message.PushMessage(new GeneratorMessage()
                {
                    Message = "翻转失败，请选择至少一个动画片段！",
                    SubMessage = "盔甲架生成器",
                    Icon = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\armor_stand.png", UriKind.RelativeOrAbsolute)),
                    MessageBrush = Brushes.Red
                });
                return;
            }
            #endregion

            #region 计算翻转后的时间和偏移量
            foreach (var selectedElement in selectedElementList)
            {
                #region 跳过当前片段
                if(selectedElement.CurrentClipMode is ClipMode.Point)
                {
                    continue;
                }
                #endregion

                #region 计算中间时间点和一半的时间跨度
                TimeSpan halfDuration = selectedElement.Duration / 2;
                TimeSpan middleTimePoint = selectedElement.StartTime + halfDuration; 
                #endregion

                #region 互换左右两侧起始帧成员的数据
                TimeSpan leftBorderFrameItemTime = selectedElement.LeftBorderFrameItem.CurrentTime;
                TimeSpan rightBorderFrameItemTime = selectedElement.RightBorderFrameItem.CurrentTime;
                double leftBorderFrameItemPositionX = selectedElement.LeftBorderFrameItem.X;
                double rightBorderFrameItemPositionX = selectedElement.RightBorderFrameItem.X;

                selectedElement.LeftBorderFrameItem.CurrentTime = rightBorderFrameItemTime;
                selectedElement.LeftBorderFrameItem.X = rightBorderFrameItemPositionX;
                selectedElement.RightBorderFrameItem.CurrentTime = leftBorderFrameItemTime;
                selectedElement.RightBorderFrameItem.X = leftBorderFrameItemPositionX;
                #endregion

                #region 执行数据翻转并更新帧成员渲染位置
                foreach (var keyFrameItem in selectedElement.InnerKeyFrameList)
                {
                    TimeSpan minusTime = TimeSpan.Zero;
                    if (keyFrameItem.IsBorderKeyFrame)
                    {
                        continue;
                    }
                    if (keyFrameItem.CurrentTime < middleTimePoint)
                    {
                        minusTime = halfDuration - keyFrameItem.CurrentTime;
                        keyFrameItem.CurrentTime = halfDuration + minusTime;
                    }
                    else
                    if (keyFrameItem.CurrentTime > middleTimePoint)
                    {
                        keyFrameItem.CurrentTime = halfDuration - minusTime;
                    }
                    keyFrameItem.X = animationTimelineTool.ConvertTimeToPixel(keyFrameItem.CurrentTime, Ruler);
                }
                selectedElement.UpdateInnerKeyFrameList(); 
                #endregion
            }
            #endregion
        }

        /// <summary>
        /// 提取选中的帧成员为单独的关键帧
        /// </summary>
        public void ExtractKeyFrameItem()
        {
            List<TimelineClip> selectedClipList = [.. CurrentTrack.TimelineElementList.Where(item => item.IsChecked is bool isChecked && isChecked)];
            foreach (var timelineClip in selectedClipList)
            {
                for (int i = 0; i < timelineClip.InnerKeyFrameList.Count; i++)
                {
                    ContinuousKeyframeItem keyFrameItem = timelineClip.InnerKeyFrameList[i];
                    if (keyFrameItem.IsChecked is bool isChecked && isChecked && !keyFrameItem.IsBorderKeyFrame)
                    {
                        timelineClip.RemoveKeyFrameItem(keyFrameItem);
                        AddTimelineElement(keyFrameItem.CurrentTime + timelineClip.StartTime);
                        i--;
                    }
                }
            }
        }

        private void DataSample(ObservableCollection<IKeyFrameData> keyFrameDataList)
        {
            for (int i = 0; i < keyFrameDataList.Count; i++)
            {
                Message.PushMessage(new GeneratorMessage()
                {
                    Message = "当前数据为" + keyFrameDataList[i].Value,
                    SubMessage = "盔甲架生成器",
                    Icon = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\armor_stand.png", UriKind.RelativeOrAbsolute))
                });
            }
        }

        private void SwitchPlayState()
        {
            if(!IsPlaying)
            {
                SnapToTick();
            }
            else
            {
                Task.Run(() =>
                {
                    TimeSpan lastTime = TimeSpan.Zero;
                    while (true)
                    {
                        for (int clipIndex = 0; clipIndex < TimelineElementMarkerList.Count; clipIndex++)
                        {
                            TimelineClip clip = TimelineElementMarkerList[clipIndex];
                            if (TimelineElementMarkerMap.TryGetValue(clip, out (TimeSpan, List<IKeyFrameData>) dataItem))
                            {
                                TimeSpan currentStartTime = dataItem.Item1;
                                bool isHiting = currentStartTime > lastTime && currentStartTime <= MemoryCurrentTime;
                                if (!isHiting)
                                {
                                    continue;
                                }

                                //if (clip.CurrentClipMode is ClipMode.Point)
                                //{
                                    DataSample(clip.DataList);
                                    lastTime = clip.StartTime;
                                //}
                                //else
                                //{
                                //    for (int keyFrameIndex = 0; keyFrameIndex < clip.InnerKeyFrameList.Count; keyFrameIndex++)
                                //    {
                                //        ContinuousKeyframeItem keyframeItem = clip.InnerKeyFrameList[keyFrameIndex];
                                //        DataSample(keyframeItem.DataList);
                                //        lastTime = keyframeItem.CurrentTime;
                                //    }
                                //}
                            }
                            if (!isSampling)
                            {
                                break;
                            }
                        }
                        if(!isSampling)
                        {
                            isSampling = true;
                            break;
                        }
                    }
                });
            }
        }

        public void ReversePlay(bool isPlaying)
        {
            lastRenderTime = TimeSpan.Zero;
            IsPlaying = isPlaying;
            playDirection = -1;
            isSampling = IsPlaying;
            SwitchPlayState();
        }

        public void Play(bool isPlaying)
        {
            lastRenderTime = TimeSpan.Zero;
            IsPlaying = isPlaying;
            playDirection = 1;
            isSampling = IsPlaying;
            SwitchPlayState();
        }

        public void MergeClip()
        {
            #region 检测是否可以合并
            if (CurrentTrack.TimelineElementList.Select(item => item.IsChecked is not null && item.IsChecked.Value).Count() < 2)
            {
                Message.PushMessage(new GeneratorMessage()
                {
                    Message = "合并失败，请选择更多关键帧！",
                    SubMessage = "盔甲架生成器",
                    Icon = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\armor_stand.png", UriKind.RelativeOrAbsolute)),
                    MessageBrush = Brushes.Red
                });
                return;
            }
            #endregion

            #region 搜索选中队列里最左侧和最右侧的关键帧并删除它们以及之间的所有关键帧

            #region 字段
            double currentTimeValue = 0.0;
            List<double> timePointList = [];
            List<ObservableCollection<IKeyFrameData>> keyFrameDataCollectionList = [];
            AnimationTimelineTool animationTimelineTool = new();
            TimeSpan leftTime = TimeSpan.FromHours(24);
            TimeSpan rightTime = TimeSpan.Zero;
            //存储上一个迭代过的时间
            TimeSpan previousTime = TimeSpan.Zero;
            int loopCount = CurrentTrack.TimelineElementList.Count;
            #endregion

            #region 搜索最小和最大的时间点
            for (int i = 0; i < CurrentTrack.TimelineElementList.Count; i++)
            {
                if (CurrentTrack.TimelineElementList[i].IsChecked is bool isChecked && isChecked)
                {
                    if (CurrentTrack.TimelineElementList[i].StartTime < leftTime)
                    {
                        leftTime = CurrentTrack.TimelineElementList[i].StartTime;
                    }
                    if (CurrentTrack.TimelineElementList[i].EndTime > rightTime)
                    {
                        rightTime = CurrentTrack.TimelineElementList[i].EndTime;
                    }
                }
            }
            #endregion

            #region 收集需要的关键帧
            for (int i = 0; i < CurrentTrack.TimelineElementList.Count; i++)
            {
                if (CurrentTrack.TimelineElementList[i].StartTime >= leftTime && CurrentTrack.TimelineElementList[i].EndTime <= rightTime)
                {
                    TimelineClip timelineclip = CurrentTrack.TimelineElementList[i];

                    if (timelineclip.CurrentClipMode is ClipMode.Point)
                    {
                        currentTimeValue = animationTimelineTool.ConvertTimeToPixel(timelineclip.StartTime, Ruler);
                        timePointList.Add(currentTimeValue);
                        keyFrameDataCollectionList.Add(new(timelineclip.DataList.ToList().Select(item => item.Clone())));
                        previousTime = timelineclip.EndTime;
                    }
                    else
                    {
                        double startTimeValue = animationTimelineTool.ConvertTimeToPixel(timelineclip.StartTime, Ruler);
                        TimeSpan startTime = timelineclip.StartTime;
                        TimeSpan endTime = timelineclip.EndTime;
                        for (int j = 0; j < timelineclip.InnerKeyFrameList.Count; j++)
                        {
                            if (timelineclip.InnerKeyFrameList[j].CurrentTime + startTime == previousTime)
                            {
                                continue;
                            }
                            currentTimeValue = animationTimelineTool.ConvertTimeToPixel(timelineclip.InnerKeyFrameList[j].CurrentTime + startTime, Ruler);
                            timePointList.Add(currentTimeValue);
                            keyFrameDataCollectionList.Add(new(timelineclip.DataList.ToList().Select(item => item.Clone())));
                            if (j == timelineclip.InnerKeyFrameList.Count - 1)
                            {
                                previousTime = endTime;
                            }
                        }
                    }
                    CurrentTrack.TimelineElementList.Remove(timelineclip);
                    i--;
                }
            }
            #endregion

            #region 从第一个开始整体偏移到0
            if (timePointList.Count > 0)
            {
                double minValue = timePointList.Min();
                for (int i = 0; i < timePointList.Count; i++)
                {
                    timePointList[i] -= minValue;
                }
            }
            #endregion

            #endregion

            //把选区中的关键帧全部放进一个全新的连续模式动画片段实例中
            AddTimelineClip("连续关键帧", leftTime, rightTime, timePointList, keyFrameDataCollectionList);
        }

        public void CopyTimelineClip()
        {
            List<TimelineClip> timelineClipList = [.. CurrentTrack.TimelineElementList.Where(item => item.IsChecked is bool isChecked && isChecked)];
            foreach (var selectedClip in timelineClipList)
            {
                TimelineClip newClip = selectedClip.Clone() as TimelineClip;
                CurrentTrack.TimelineElementList.Add(newClip);
            }
        }

        /// <summary>
        /// 磁吸逻辑：将当前时间对齐到最近的 0.05s
        /// </summary>
        private void SnapToTick()
        {
            double current = CurrentTime.TotalSeconds;
            // 四舍五入到最近的步进倍数
            double snapped = Math.Round(current / TickStep) * TickStep;

            // 确保不超出标尺范围
            if (Ruler is not null)
            {
                snapped = Math.Clamp(snapped, 0, Ruler.Maximum);
            }

            CurrentTime = TimeSpan.FromSeconds(snapped);
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
                        foreach (var item in CurrentTrack.TimelineElementList)
                        {
                            if (item is TimelineClip clip)
                            {
                                clip.IsChecked = false;
                                clip.BorderBrush = Brushes.White;
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
                foreach (var timelineClip in track.TimelineElementList)
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
        /// 让WPF内置的渲染时钟驱动播放逻辑，计算每一帧的时间差并更新当前时间，从而实现播放指针的移动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timeline_Rendering(object sender, EventArgs e)
        {
            if(!IsPlaying)
            {
                return;
            }

            RenderingEventArgs args = (RenderingEventArgs)e;

            //初始化计时基准
            if (lastRenderTime == TimeSpan.Zero)
            {
                lastRenderTime = args.RenderingTime;
                return;
            }

            //计算两帧之间的时间差 (DeltaTime)
            double delta = (args.RenderingTime - lastRenderTime).TotalSeconds;
            lastRenderTime = args.RenderingTime;

            //计算下一帧的时间点
            double nextSeconds = CurrentTime.TotalSeconds + (delta * playDirection);

            //边界判定
            if (nextSeconds < 0 && playDirection == -1)
            {
                IsPlaying = false;
                CurrentTime = TimeSpan.Zero;
                return;
            }

            if (Ruler is not null && nextSeconds > Ruler.Maximum)
            {
                CurrentTime = TimeSpan.FromSeconds(Ruler.Maximum);
                IsPlaying = false;
                return;
            }

            //更新时间
            CurrentTime = TimeSpan.FromSeconds(nextSeconds);
            MemoryCurrentTime = CurrentTime;
        }
        #endregion
    }
}