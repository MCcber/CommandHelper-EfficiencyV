using CBHK.CustomControl.Container;
using CBHK.Interface;
using CBHK.Utility.Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CBHK.CustomControl.Input
{
    public class TimelineClip : ToggleButton, ITimelineElement
    {
        #region Field
        private AnimationTimelineTool animationTimelineTool = new();
        public ContinuousKeyframeItem LeftBorderFrameItem;
        public ContinuousKeyframeItem RightBorderFrameItem;
        private bool isDragging = false;
        private Point dragPoint;
        private Point dragStartPoint;
        private bool isInKeyFrameItem = false;
        private bool isReFreshingBrush = false;
        private Canvas innerTimeCanvas;
        private Canvas parentCanvas;
        private TimeRulerElement timeRulerElement;
        private bool isTimelineClipLoaded;
        private ContinuousKeyframeItem currentMemberFrameItem;
        private Cursor splitCursor;
        #endregion

        #region Property
        public double OriginCanvasTop { get; set; }
        public TimeSpan OriginStartTime { get; set; }
        public TimeSpan OriginEndTime { get; set; }
        public bool IsAdjustingFirstSize { get; set; }
        public bool IsAdjustingLastSize { get; set; }

        public bool IsMoveMemberKeyFrame { get; set; }
        public Timeline ParentTimeline { get; set; }
        public object ParentPanel { get; set; }
        public bool IsSplitOut { get; set; }

        public Visibility SplitPreviewLineVisibility
        {
            get { return (Visibility)GetValue(SplitPreviewLineVisibilityProperty); }
            set { SetValue(SplitPreviewLineVisibilityProperty, value); }
        }

        public static readonly DependencyProperty SplitPreviewLineVisibilityProperty =
            DependencyProperty.Register("SplitPreviewLineVisibility", typeof(Visibility), typeof(TimelineClip), new PropertyMetadata(default(Visibility)));

        public double SplitPreviewLineOffsetX
        {
            get { return (double)GetValue(SplitPreviewLineOffsetXProperty); }
            set { SetValue(SplitPreviewLineOffsetXProperty, value); }
        }

        public static readonly DependencyProperty SplitPreviewLineOffsetXProperty =
            DependencyProperty.Register("SplitPreviewLineOffsetX", typeof(double), typeof(TimelineClip), new PropertyMetadata(default(double)));

        public TimeRulerElement Ruler
        {
            get { return (TimeRulerElement)GetValue(RulerProperty); }
            set { SetValue(RulerProperty, value); }
        }

        public static readonly DependencyProperty RulerProperty =
            DependencyProperty.Register("Ruler", typeof(TimeRulerElement), typeof(TimelineClip), new PropertyMetadata(default(TimeRulerElement)));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(TimelineClip), new PropertyMetadata(default(string)));

        public Visibility TitleEditorVisibility
        {
            get { return (Visibility)GetValue(TitleEditorVisibilityProperty); }
            set { SetValue(TitleEditorVisibilityProperty, value); }
        }

        public static readonly DependencyProperty TitleEditorVisibilityProperty =
            DependencyProperty.Register("TitleEditorVisibility", typeof(Visibility), typeof(TimelineClip), new PropertyMetadata(default(Visibility)));

        public double ZoomFactor
        {
            get { return (double)GetValue(ZoomFactorProperty); }
            set { SetValue(ZoomFactorProperty, value); }
        }

        public static readonly DependencyProperty ZoomFactorProperty =
            DependencyProperty.Register("ZoomFactor", typeof(double), typeof(TimelineClip), new PropertyMetadata(default(double)));

        public double MinDuration
        {
            get { return (double)GetValue(MinDurationProperty); }
            set { SetValue(MinDurationProperty, value); }
        }

        public static readonly DependencyProperty MinDurationProperty =
            DependencyProperty.Register("MinDuration", typeof(double), typeof(TimelineClip), new PropertyMetadata(default(double)));

        public Brush ClipColor
        {
            get { return (Brush)GetValue(ClipColorProperty); }
            set { SetValue(ClipColorProperty, value); }
        }

        public static readonly DependencyProperty ClipColorProperty =
            DependencyProperty.Register("ClipColor", typeof(Brush), typeof(TimelineClip), new PropertyMetadata(default(Brush)));

        public TimeSpan StartTime
        {
            get { return (TimeSpan)GetValue(StartTimeProperty); }
            set { SetValue(StartTimeProperty, value); }
        }

        public static readonly DependencyProperty StartTimeProperty =
            DependencyProperty.Register("StartTime", typeof(TimeSpan), typeof(TimelineClip), new PropertyMetadata(default(TimeSpan), Frame_PropertyChanged));

        public TimeSpan EndTime
        {
            get { return (TimeSpan)GetValue(EndTimeProperty); }
            set { SetValue(EndTimeProperty, value); }
        }

        public static readonly DependencyProperty EndTimeProperty =
            DependencyProperty.Register("EndTime", typeof(TimeSpan), typeof(TimelineClip), new PropertyMetadata(default(TimeSpan), Frame_PropertyChanged));

        public TimeSpan Duration => EndTime - StartTime;

        public double DurationSecond
        {
            get { return (double)GetValue(DurationSecondProperty); }
            set { SetValue(DurationSecondProperty, value); }
        }

        public static readonly DependencyProperty DurationSecondProperty =
            DependencyProperty.Register("DurationSecond", typeof(double), typeof(TimelineClip), new PropertyMetadata(default(double)));

        public string DurationText
        {
            get { return (string)GetValue(DurationTextProperty); }
            set { SetValue(DurationTextProperty, value); }
        }

        public static readonly DependencyProperty DurationTextProperty =
            DependencyProperty.Register("DurationText", typeof(string), typeof(TimelineClip), new PropertyMetadata(default(string)));

        public double InnerCanvasWidth
        {
            get { return (double)GetValue(InnerCanvasWidthProperty); }
            set { SetValue(InnerCanvasWidthProperty, value); }
        }

        public static readonly DependencyProperty InnerCanvasWidthProperty =
            DependencyProperty.Register("InnerCanvasWidth", typeof(double), typeof(TimelineClip), new PropertyMetadata(default(double)));

        public double RectangleModeWidth
        {
            get { return (double)GetValue(RectangleModeWidthProperty); }
            set { SetValue(RectangleModeWidthProperty, value); }
        }

        public static readonly DependencyProperty RectangleModeWidthProperty =
            DependencyProperty.Register("RectangleModeWidth", typeof(double), typeof(TimelineClip), new PropertyMetadata(16.0,RectangleModeWidthChanged));

        public double RectangleModeHeight
        {
            get { return (double)GetValue(RectangleModeHeightProperty); }
            set { SetValue(RectangleModeHeightProperty, value); }
        }

        public static readonly DependencyProperty RectangleModeHeightProperty =
            DependencyProperty.Register("RectangleModeHeight", typeof(double), typeof(TimelineClip), new PropertyMetadata(default(double)));

        public ObservableCollection<ContinuousKeyframeItem> InnerKeyFrameList
        {
            get { return (ObservableCollection<ContinuousKeyframeItem>)GetValue(InnerKeyFrameListProperty); }
            set { SetValue(InnerKeyFrameListProperty, value); }
        }

        public static readonly DependencyProperty InnerKeyFrameListProperty =
            DependencyProperty.Register("InnerKeyFrameList", typeof(ObservableCollection<ContinuousKeyframeItem>), typeof(TimelineClip), new PropertyMetadata(default(ObservableCollection<ContinuousKeyframeItem>)));
        #endregion

        #region Method
        public TimelineClip()
        {
            SplitPreviewLineVisibility = Visibility.Hidden;
            InnerKeyFrameList = [];
            TitleEditorVisibility = Visibility.Hidden;
            Loaded += TimelineClip_Loaded;
        }

        /// <summary>
        /// 更新帧成员数据
        /// </summary>
        public void UpdateInnerKeyFrameList()
        {
            double offset = 0.0;
            foreach (var keyFrameItem in InnerKeyFrameList)
            {
                offset = (Math.Sqrt(Math.Pow(keyFrameItem.Width, 2) + Math.Pow(keyFrameItem.Height, 2))) / -2;
                offset += BorderThickness.Left + Padding.Left;
                Canvas.SetLeft(keyFrameItem, keyFrameItem.X + offset);
                Canvas.SetTop(keyFrameItem, (innerTimeCanvas.ActualHeight / 2) - (keyFrameItem.Width / 2));
            }
        }

        public void AddKeyFrameItem(ContinuousKeyframeItem item,double PositionX)
        {
            if(innerTimeCanvas is not null)
            {
                innerTimeCanvas.Children.Add(item);
                Canvas.SetLeft(item, PositionX);
                Canvas.SetTop(item, (innerTimeCanvas.ActualHeight / 2) - (item.Width / 2));
                if(item.IsBorderKeyFrame)
                {
                    item.Cursor = Cursors.SizeWE;
                    item.IsChecked = true;
                    item.PreviewMouseLeftButtonDown += BorderKeyFrameItem_PreviewMouseLeftButtonDown;
                    item.MouseEnter += KeyFrameItem_MouseEnter;
                    item.MouseLeave += KeyFrameItem_MouseLeave;
                }
            }
        }

        public void RemoveKeyFrameItem(ContinuousKeyframeItem item)
        {
            innerTimeCanvas.Children.Remove(item);
            InnerKeyFrameList.Remove(item);
        }

        /// <summary>
        /// 作为被分割片段重新订阅尾部帧成员的事件
        /// </summary>
        public void ProcessDivided()
        {
            RightBorderFrameItem.Cursor = Cursors.SizeWE;
            RightBorderFrameItem.IsChecked = true;
            RightBorderFrameItem.PreviewMouseLeftButtonDown += BorderKeyFrameItem_PreviewMouseLeftButtonDown;
            RightBorderFrameItem.MouseEnter += KeyFrameItem_MouseEnter;
            RightBorderFrameItem.MouseLeave += KeyFrameItem_MouseLeave;
        }

        public TimelineClip Clone()
        {
            TimelineClip result = new()
            {
                ZoomFactor = ZoomFactor,
                RectangleModeHeight = RectangleModeHeight,
                OriginCanvasTop = OriginCanvasTop,
                OriginStartTime = StartTime,
                OriginEndTime = EndTime,
                Title = Title,
                Style = Application.Current.Resources["TimelineClipStyle"] as Style,
                Ruler = Ruler,
                ParentTimeline = ParentTimeline,
                ParentPanel = ParentPanel
            };
            foreach (var keyFrameItem in InnerKeyFrameList)
            {
                ContinuousKeyframeItem newKeyFrame = keyFrameItem.Clone() as ContinuousKeyframeItem;
                result.InnerKeyFrameList.Add(newKeyFrame);
            }
            return result;
        }
        #endregion

        #region Event
        private void TimelineClip_Loaded(object sender, RoutedEventArgs e)
        {
            if(isTimelineClipLoaded)
            {
                return;
            }

            StartTime = OriginStartTime;
            EndTime = OriginEndTime;

            BorderBrush = Brushes.White;
            Canvas.SetTop(this, OriginCanvasTop - ActualHeight / 2);

            // 向上查找父级 Canvas
            parentCanvas = VisualTreeHelper.GetParent(this) as Canvas;
            while (parentCanvas == null && VisualTreeHelper.GetParent(this) != null)
            {
                parentCanvas = VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(this)) as Canvas;
            }

            Geometry geometry = (Application.Current.Resources["ShaverGeometry"] as Geometry).Clone();
            Path path = new()
            {
                Data = geometry,
                Fill = Brushes.Black,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            Viewbox viewbox = new()
            {
                Child = path,
                Width = 35,
                Height = 35
            };
            geometry.Transform = new RotateTransform(45);
            splitCursor = CursorHelper.CreateCursor(viewbox, 0, 0);

            isTimelineClipLoaded = true;
        }

        public void InnerTimeCanvas_Loaded(object sender,RoutedEventArgs e)
        {
            if(sender is Canvas canvas && innerTimeCanvas is null)
            {
                innerTimeCanvas = canvas;
            }

            if (innerTimeCanvas is not null)
            {
                #region 字段
                IsChecked = false;
                double offset = 0;
                TimeSpan leftTime = TimeSpan.FromHours(24), rightTime = TimeSpan.Zero; 
                #endregion

                #region 搜索最小和最大成员
                if (LeftBorderFrameItem is null || RightBorderFrameItem is null)
                {
                    for (int i = 0; i < InnerKeyFrameList.Count; i++)
                    {
                        if (InnerKeyFrameList[i].CurrentTime <= leftTime)
                        {
                            leftTime = InnerKeyFrameList[i].CurrentTime;
                            LeftBorderFrameItem = InnerKeyFrameList[i];
                        }
                        if (InnerKeyFrameList[i].CurrentTime >= rightTime)
                        {
                            rightTime = InnerKeyFrameList[i].CurrentTime;
                            RightBorderFrameItem = InnerKeyFrameList[i];
                        }
                    }
                }
                #endregion

                #region 设置位置
                foreach (var keyframeItem in InnerKeyFrameList)
                {
                    keyframeItem.IsChecked = false;

                    if (keyframeItem.Parent is not null)
                    {
                        continue;
                    }

                    innerTimeCanvas.Children.Add(keyframeItem);
                    offset = (Math.Sqrt(Math.Pow(keyframeItem.Width, 2) + Math.Pow(keyframeItem.Height, 2))) / -2;
                    offset += BorderThickness.Left + Padding.Left;

                    Canvas.SetLeft(keyframeItem, keyframeItem.X + offset);
                    Canvas.SetTop(keyframeItem, (innerTimeCanvas.ActualHeight / 2) - (keyframeItem.Width / 2));
                }
                #endregion

                #region 订阅事件
                if (InnerKeyFrameList.Count > 0)
                {
                    if (LeftBorderFrameItem is not null && !LeftBorderFrameItem.IsLoaded)
                    {
                        LeftBorderFrameItem.Cursor = Cursors.SizeWE;
                        LeftBorderFrameItem.IsChecked = true;
                        LeftBorderFrameItem.PreviewMouseLeftButtonDown += BorderKeyFrameItem_PreviewMouseLeftButtonDown;
                        LeftBorderFrameItem.MouseEnter += KeyFrameItem_MouseEnter;
                        LeftBorderFrameItem.MouseLeave += KeyFrameItem_MouseLeave;
                    }
                    if (RightBorderFrameItem is not null && !RightBorderFrameItem.IsLoaded)
                    {
                        RightBorderFrameItem.Cursor = Cursors.SizeWE;
                        RightBorderFrameItem.IsChecked = true;
                        RightBorderFrameItem.PreviewMouseLeftButtonDown += BorderKeyFrameItem_PreviewMouseLeftButtonDown;
                        RightBorderFrameItem.MouseEnter += KeyFrameItem_MouseEnter;
                        RightBorderFrameItem.MouseLeave += KeyFrameItem_MouseLeave;
                    }

                    if (!LeftBorderFrameItem.IsLoaded && !RightBorderFrameItem.IsLoaded)
                    {
                        PreviewMouseMove += BorderKeyFrameItem_PreviewMouseMove;
                        PreviewMouseMove += MemberKeyFrameItem_PreviewMouseMove;
                    }

                    for (int i = 0; i < InnerKeyFrameList.Count; i++)
                    {
                        if (InnerKeyFrameList[i] == LeftBorderFrameItem || InnerKeyFrameList[i] == RightBorderFrameItem)
                        {
                            continue;
                        }

                        if (!InnerKeyFrameList[i].IsLoaded)
                        {
                            InnerKeyFrameList[i].PreviewMouseLeftButtonDown += MemberKeyFrameItem_PreviewMouseLeftButtonDown;
                            InnerKeyFrameList[i].MouseEnter += KeyFrameItem_MouseEnter;
                            InnerKeyFrameList[i].MouseEnter += MemberFrameItem_MouseEnter;
                            InnerKeyFrameList[i].MouseLeave += KeyFrameItem_MouseLeave;
                            InnerKeyFrameList[i].Cursor = Cursors.SizeWE;
                        }
                    }
                }
                #endregion
            }
        }

        private void MemberFrameItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!IsMoveMemberKeyFrame)
            {
                currentMemberFrameItem = sender as ContinuousKeyframeItem;
            }
        }

        private void KeyFrameItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if(!isDragging)
            {
                isInKeyFrameItem = true;
            }
        }

        private void KeyFrameItem_MouseLeave(object sender, MouseEventArgs e)
        {
            if(!isDragging)
            {
                isInKeyFrameItem = false;
            }
        }

        /// <summary>
        /// 双击进入移动成员帧模式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MemberKeyFrameItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ClickCount == 2)
            {
                IsMoveMemberKeyFrame = true;
                dragPoint = e.GetPosition(innerTimeCanvas);
            }
        }

        /// <summary>
        /// 移动成员帧
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MemberKeyFrameItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (innerTimeCanvas is not null && IsMoveMemberKeyFrame)
            {
                dragPoint = e.GetPosition(innerTimeCanvas);
                TimeSpan currentTime = animationTimelineTool.ConvertPixelToTime(dragPoint.X, Ruler);
                double currentTimeValue = animationTimelineTool.ConvertTimeToPixel(currentTime, Ruler);
                currentMemberFrameItem.CurrentTime = currentTime;
                Canvas.SetLeft(currentMemberFrameItem, currentTimeValue - (currentMemberFrameItem.Width / 2));
            }
        }

        #region 拉伸起始/末尾关键帧
        private void BorderKeyFrameItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 获取父级 Canvas (作为计算坐标的绝对参考系)
            if (parentCanvas is not null && !isDragging)
            {
                dragPoint = dragStartPoint = e.GetPosition(parentCanvas);
                IsAdjustingFirstSize = IsAdjustingLastSize = false;
                if (sender == LeftBorderFrameItem)
                {
                    IsAdjustingFirstSize = true;
                }
                else
                if (sender == RightBorderFrameItem)
                {
                    IsAdjustingLastSize = true;
                }
            }
        }

        private void BorderKeyFrameItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (parentCanvas is not null && (IsAdjustingFirstSize || IsAdjustingLastSize))
            {
                dragPoint = e.GetPosition(parentCanvas);

                #region 拉伸左右两侧
                if (IsAdjustingFirstSize)
                {
                    StartTime = animationTimelineTool.ConvertPixelToTime(dragPoint.X, Ruler);
                    TimeSpan timeSpan = EndTime - StartTime;
                    int seconds = timeSpan.Seconds;
                    int milliseconds = timeSpan.Milliseconds;
                    int frames = (int)(timeSpan.TotalSeconds * 20) % 20;
                    DurationText = string.Format("{0:D2}s{1:D2}ms{2:D2}f", seconds, milliseconds, frames);

                    double startPoint = animationTimelineTool.ConvertTimeToPixel(StartTime, Ruler);
                    double endPoint = animationTimelineTool.ConvertTimeToPixel(EndTime, Ruler);

                    double lastPoint = endPoint - startPoint;

                    Canvas.SetLeft(this, startPoint + BorderThickness.Left + Padding.Left);
                    RectangleModeWidth = endPoint - startPoint - BorderThickness.Left;

                    RightBorderFrameItem.X = lastPoint;
                    RightBorderFrameItem.CurrentTime = timeSpan;
                    double offset = (Math.Sqrt(Math.Pow(LeftBorderFrameItem.Width, 2) + Math.Pow(LeftBorderFrameItem.Height, 2))) / -2;
                    offset += BorderThickness.Left + Padding.Left;

                    Canvas.SetLeft(LeftBorderFrameItem, offset);
                    Canvas.SetLeft(RightBorderFrameItem, lastPoint + offset);
                }
                else
                if (IsAdjustingLastSize)
                {
                    EndTime = animationTimelineTool.ConvertPixelToTime(dragPoint.X, Ruler);
                    double startPoint = animationTimelineTool.ConvertTimeToPixel(StartTime, Ruler);
                    double endPoint = animationTimelineTool.ConvertTimeToPixel(EndTime, Ruler);
                    double lastPoint = endPoint - startPoint + BorderThickness.Left;
                    RectangleModeWidth = lastPoint;

                    RightBorderFrameItem.X = lastPoint;
                    RightBorderFrameItem.CurrentTime = EndTime - StartTime;

                    double offset = (Math.Sqrt(Math.Pow(RightBorderFrameItem.Width, 2) + Math.Pow(RightBorderFrameItem.Height, 2))) / -2;
                    offset += BorderThickness.Left + Padding.Left;

                    Canvas.SetLeft(RightBorderFrameItem, lastPoint + offset);
                }
                #endregion
            }
        }
        #endregion

        #region 调整动画片段整体位置
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (ParentTimeline.IsSplitModeOpened)
            {
                ParentTimeline.CurrentSplitTimelineClip = this;
            }
            ParentTimeline?.Track_MouseLeftButtonDown(ParentPanel, null);
            if (e.ClickCount == 1)
            {
                if (IsChecked is bool)
                {
                    IsChecked = !IsChecked;
                }

                if(IsChecked is bool result && result)
                {
                    BorderBrush = Brushes.Black;
                }
                else
                {
                    BorderBrush = Brushes.White;
                }
            }
            // 获取父级 Canvas (作为计算坐标的绝对参考系)
            if (parentCanvas is not null && !isInKeyFrameItem && e.ClickCount == 2)
            {
                dragPoint = dragStartPoint = e.GetPosition(parentCanvas);
                isDragging = true;
                // 【最关键的一句】强制控件捕获鼠标！
                // 这样即使拖拽速度过快，鼠标箭头移出了控件范围，它依然能持续触发 MouseMove。
                CaptureMouse();
            }

            // 调用基类方法，保证 ToggleButton 原本的选中/点击逻辑正常工作
            base.OnPreviewMouseLeftButtonDown(e);
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            #region 处理动画片段拖拽
            if (IsMouseCaptured && parentCanvas is not null && !isInKeyFrameItem && isDragging)
            {
                dragPoint = e.GetPosition(parentCanvas);
                TimeSpan currentDuration = EndTime - StartTime;
                TimeSpan newStartTime = animationTimelineTool.ConvertPixelToTime(dragPoint.X, Ruler);
                StartTime = newStartTime;
                if (CurrentClipMode is ClipMode.Point)
                {
                    EndTime = StartTime;
                }
                else
                {
                    EndTime = newStartTime + currentDuration;
                }
            }
            #endregion

            #region 处理预览分割线的移动
            if(ParentTimeline is not null && ParentTimeline.IsSplitModeOpened)
            {
                dragPoint = e.GetPosition(innerTimeCanvas);
                double currentX = dragPoint.X;
                TimeSpan timeSpan = animationTimelineTool.ConvertPixelToTime(currentX, Ruler);
                currentX = animationTimelineTool.ConvertTimeToPixel(timeSpan, Ruler);
                currentX = Math.Clamp(currentX,0,parentCanvas.ActualWidth);
                SplitPreviewLineOffsetX = currentX;
            }
            #endregion

            base.OnPreviewMouseMove(e);
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (!isDragging && IsChecked is not null && CurrentClipMode is ClipMode.Point)
            {
                IsChecked = !IsChecked;
            }
            isDragging = false;
            ReleaseMouseCapture();
            IsAdjustingFirstSize = IsAdjustingLastSize = IsMoveMemberKeyFrame = false;

            base.OnPreviewMouseLeftButtonUp(e);
        }
        #endregion

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            #region 处理分割片段时的鼠标图案
            if (ParentTimeline is not null && ParentTimeline.IsSplitModeOpened)
            {
                SplitPreviewLineVisibility = Visibility.Visible;
                Cursor = splitCursor;
            }
            else
            {
                Cursor = Cursors.Arrow;
            }
            #endregion
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            SplitPreviewLineVisibility = Visibility.Hidden;
        }

        public static void RectangleModeWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimelineClip clip)
            {
                clip.TimelineClipRectangleModeWidth_Changed(e.Property.Name);
            }
        }

        public void TimelineClipRectangleModeWidth_Changed(string PropertyName)
        {
            switch (PropertyName)
            {
                case "RectangleModeWidth":
                    {
                        if (CurrentClipMode is ClipMode.Rectangle)
                        {
                            double horizontalBorder = BorderThickness.Left + BorderThickness.Right;
                            InnerCanvasWidth = Math.Max(0, RectangleModeWidth - horizontalBorder);
                        }
                        break;
                    }
            }
        }

        private static void Frame_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is TimelineClip clip)
            {
                clip.TimelineClipFrame_Changed(e.Property.Name);
            }
        }

        public void TimelineClipFrame_Changed(string PropertyName)
        {
            switch (PropertyName)
            {
                case "EndTime":
                    {
                        TimeSpan timeSpan = EndTime - StartTime;
                        int seconds = timeSpan.Seconds;
                        int milliseconds = timeSpan.Milliseconds;
                        int frames = (int)(timeSpan.TotalSeconds * 20) % 20;
                        DurationSecond = Duration.TotalSeconds;
                        DurationText = string.Format("{0:D2}s{1:D2}ms{2:D2}f", seconds, milliseconds, frames);
                        UpdateClipByCurrentTime();
                        break;
                    }
            }
        }

        /// <summary>
        /// 根据时间更新动画片段的位置
        /// </summary>
        public void UpdateClipByCurrentTime()
        {
            if (Ruler is not null)
            {
                double startPoint = animationTimelineTool.ConvertTimeToPixel(StartTime, Ruler);
                double endPoint = animationTimelineTool.ConvertTimeToPixel(EndTime, Ruler) - BorderThickness.Left;

                startPoint += -BorderThickness.Left - Padding.Left;

                Canvas.SetLeft(this, startPoint);

                if (CurrentClipMode == ClipMode.Point)
                {
                    // 单点模式下，控件外层宽度应与内层视觉宽度保持一致，确保 Transform 生效基准正确
                    Width = PointModeSize = 16;
                }
                else
                {
                    // 连续模式（Rectangle）
                    if (endPoint >= startPoint)
                    {
                        RectangleModeWidth = endPoint - startPoint;
                    }
                }
            }
        }

        protected override void OnChecked(RoutedEventArgs e)
        {
            base.OnChecked(e);
            if (IsChecked is bool)
            {
                isReFreshingBrush = true;
                Background = new BrushConverter().ConvertFromString("#FFFFFF") as Brush;
                isReFreshingBrush = false;
            }
        }

        protected override void OnUnchecked(RoutedEventArgs e)
        {
            base.OnUnchecked(e);
            if (IsChecked is bool)
            {
                isReFreshingBrush = true;
                Background = new BrushConverter().ConvertFromString("#B8BEBA") as Brush;
                isReFreshingBrush = false;
            }
        }
        #endregion
    }
}