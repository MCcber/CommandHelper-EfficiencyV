using CBHK.CustomControl.Container;
using CBHK.Model.Common;
using CBHK.Utility.Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Windows.Management.Deployment;

namespace CBHK.CustomControl.Input
{
    public class TimelineClip : ToggleButton
    {
        #region Field
        private AnimationTimelineTool animationTimelineTool = new();
        private bool isDragging = false;
        private bool isInKeyFrameItem = false;
        private bool isReFreshingBrush = false;
        private Brush OriginInnerBorderBrush;
        private Brush OriginBackground;
        private Canvas parentCanvas;
        private Canvas innerTimeCanvas;
        private Point dragPoint;
        private Point dragStartPoint;
        private TimeRulerElement timeRulerElement;
        private bool isTimelineClipLoaded;
        private bool isInnerTimeCanvasLoaded;
        private ContinuousKeyframeItem currentMemberFrameItem;

        #endregion

        #region Property
        public ContinuousKeyframeItem LeftBorderFrameItem;
        public ContinuousKeyframeItem RightBorderFrameItem;
        public TimeSpan OriginStartTime { get; set; }
        public TimeSpan OriginEndTime { get; set; }
        public double OriginCanvasTop { get; set; }
        public bool IsAdjustingFirstSize { get; set; }
        public bool IsAdjustingLastSize { get; set; }

        public bool IsMoveMemberKeyFrame { get; set; }
        public Timeline ParentTimeline { get; set; }
        public object ParentPanel { get; set; }
        public bool IsDivided { get; set; }
        public bool IsSplitOut { get; set; }
        public Action UpdateStateAction { get; set; }

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

        public double PointModeSize
        {
            get { return (double)GetValue(PointModeSizeProperty); }
            set { SetValue(PointModeSizeProperty, value); }
        }

        public static readonly DependencyProperty PointModeSizeProperty =
            DependencyProperty.Register("PointModeSize", typeof(double), typeof(TimelineClip), new PropertyMetadata(16.0));

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

        public ClipMode CurrentClipMode
        {
            get { return (ClipMode)GetValue(CurrentClipModeProperty); }
            set { SetValue(CurrentClipModeProperty, value); }
        }

        public static readonly DependencyProperty CurrentClipModeProperty =
            DependencyProperty.Register("CurrentClipMode", typeof(ClipMode), typeof(TimelineClip), new PropertyMetadata(default(ClipMode)));

        public ObservableCollection<ContinuousKeyframeItem> InnerKeyFrameList
        {
            get { return (ObservableCollection<ContinuousKeyframeItem>)GetValue(InnerKeyFrameListProperty); }
            set { SetValue(InnerKeyFrameListProperty, value); }
        }

        public static readonly DependencyProperty InnerKeyFrameListProperty =
            DependencyProperty.Register("InnerKeyFrameList", typeof(ObservableCollection<ContinuousKeyframeItem>), typeof(TimelineClip), new PropertyMetadata(default(ObservableCollection<ContinuousKeyframeItem>)));

        public Brush InnerBorderBrush
        {
            get { return (Brush)GetValue(InnerBorderBrushProperty); }
            set { SetValue(InnerBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty InnerBorderBrushProperty =
            DependencyProperty.Register("InnerBorderBrush", typeof(Brush), typeof(TimelineClip), new PropertyMetadata(default(Brush)));

        public Brush InnerLeftTopBackground
        {
            get { return (Brush)GetValue(InnerLeftTopBackgroundProperty); }
            set { SetValue(InnerLeftTopBackgroundProperty, value); }
        }

        public static readonly DependencyProperty InnerLeftTopBackgroundProperty =
            DependencyProperty.Register("InnerLeftTopBackground", typeof(Brush), typeof(TimelineClip), new PropertyMetadata(default(Brush)));

        public Brush InnerRightTopBackground
        {
            get { return (Brush)GetValue(InnerRightTopBackgroundProperty); }
            set { SetValue(InnerRightTopBackgroundProperty, value); }
        }

        public static readonly DependencyProperty InnerRightTopBackgroundProperty =
            DependencyProperty.Register("InnerRightTopBackground", typeof(Brush), typeof(TimelineClip), new PropertyMetadata(default(Brush)));

        public Brush InnerLeftBottomBackground
        {
            get { return (Brush)GetValue(InnerLeftBottomBackgroundProperty); }
            set { SetValue(InnerLeftBottomBackgroundProperty, value); }
        }

        public static readonly DependencyProperty InnerLeftBottomBackgroundProperty =
            DependencyProperty.Register("InnerLeftBottomBackground", typeof(Brush), typeof(TimelineClip), new PropertyMetadata(default(Brush)));

        public Brush InnerRightBottomBackground
        {
            get { return (Brush)GetValue(InnerRightBottomBackgroundProperty); }
            set { SetValue(InnerRightBottomBackgroundProperty, value); }
        }

        public static readonly DependencyProperty InnerRightBottomBackgroundProperty =
            DependencyProperty.Register("InnerRightBottomBackground", typeof(Brush), typeof(TimelineClip), new PropertyMetadata(default(Brush)));

        public Brush LeftTopBorderBrush
        {
            get { return (Brush)GetValue(LeftTopBorderBrushProperty); }
            set { SetValue(LeftTopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftTopBorderBrushProperty =
            DependencyProperty.Register("LeftTopBorderBrush", typeof(Brush), typeof(TimelineClip), new PropertyMetadata(default(Brush)));

        public Brush RightBottomBorderBrush
        {
            get { return (Brush)GetValue(RightBottomBorderBrushProperty); }
            set { SetValue(RightBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RightBottomBorderBrushProperty =
            DependencyProperty.Register("RightBottomBorderBrush", typeof(Brush), typeof(TimelineClip), new PropertyMetadata(default(Brush)));

        public Brush BorderCornerBrush
        {
            get { return (Brush)GetValue(BorderCornerBrushProperty); }
            set { SetValue(BorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(TimelineClip), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public TimelineClip()
        {
            SplitPreviewLineVisibility = Visibility.Hidden;
            InnerKeyFrameList = [];
            TitleEditorVisibility = Visibility.Hidden;
            Loaded += TimelineClip_Loaded;
        }

        private void UpdateBorderColorByBackgroundColor()
        {
            var foregroundSource = DependencyPropertyHelper.GetValueSource(this, ForegroundProperty);
            if (foregroundSource.BaseValueSource is BaseValueSource.DefaultStyle || foregroundSource.BaseValueSource is BaseValueSource.Style)
            {
                Foreground = Brushes.White;
            }

            var borderBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderBrushProperty);
            if (borderBrushSource.BaseValueSource is BaseValueSource.DefaultStyle || borderBrushSource.BaseValueSource is BaseValueSource.Style || BorderBrush is null)
            {
                BorderBrush = Brushes.Black;
            }

            var originborderCornerBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderCornerBrushProperty);
            if (originborderCornerBrushSource.BaseValueSource is BaseValueSource.Default || originborderCornerBrushSource.BaseValueSource is BaseValueSource.Style || BorderCornerBrush is null || isReFreshingBrush)
            {
                SolidColorBrush solidBorderBrush = OriginInnerBorderBrush as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.4f);
                BorderCornerBrush = new SolidColorBrush(color);
            }

            var originLeftTopBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, LeftTopBorderBrushProperty);
            if (originLeftTopBorderBrushSource.BaseValueSource is BaseValueSource.Default || originLeftTopBorderBrushSource.BaseValueSource is BaseValueSource.Style || LeftTopBorderBrush is null || isReFreshingBrush)
            {
                SolidColorBrush solidBorderBrush = OriginInnerBorderBrush as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.3f);
                LeftTopBorderBrush = new SolidColorBrush(color);
            }

            var originRightBottomBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, RightBottomBorderBrushProperty);
            if (originRightBottomBorderBrushSource.BaseValueSource is BaseValueSource.Default || originRightBottomBorderBrushSource.BaseValueSource is BaseValueSource.Style || RightBottomBorderBrush is null || isReFreshingBrush)
            {
                SolidColorBrush solidBorderBrush = OriginInnerBorderBrush as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.2f);
                RightBottomBorderBrush = new SolidColorBrush(color);
            }

            var originInnerLeftTopBackgroundSource = DependencyPropertyHelper.GetValueSource(this, InnerLeftTopBackgroundProperty);
            if (originInnerLeftTopBackgroundSource.BaseValueSource is BaseValueSource.Default || originInnerLeftTopBackgroundSource.BaseValueSource is BaseValueSource.Style || InnerLeftTopBackground is null || isReFreshingBrush)
            {
                SolidColorBrush solidBorderBrush = OriginBackground as SolidColorBrush;
                Color color = ColorTool.Darken(solidBorderBrush.Color, 0.02f);
                InnerLeftTopBackground = new SolidColorBrush(color);
            }

            var originInnerRightTopBackgroundSource = DependencyPropertyHelper.GetValueSource(this, InnerRightTopBackgroundProperty);
            if (originInnerRightTopBackgroundSource.BaseValueSource is BaseValueSource.Default || originInnerRightTopBackgroundSource.BaseValueSource is BaseValueSource.Style || InnerRightTopBackground is null || isReFreshingBrush)
            {
                SolidColorBrush solidBorderBrush = OriginBackground as SolidColorBrush;
                Color color = ColorTool.Darken(solidBorderBrush.Color, 0.05f);
                InnerRightTopBackground = new SolidColorBrush(color);
            }

            var originInnerLeftBottomBackgroundSource = DependencyPropertyHelper.GetValueSource(this, InnerLeftBottomBackgroundProperty);
            if (originInnerLeftBottomBackgroundSource.BaseValueSource is BaseValueSource.Default || originInnerLeftBottomBackgroundSource.BaseValueSource is BaseValueSource.Style || InnerLeftBottomBackground is null || isReFreshingBrush)
            {
                SolidColorBrush solidBorderBrush = OriginBackground as SolidColorBrush;
                Color color = ColorTool.Darken(solidBorderBrush.Color, 0.05f);
                InnerLeftBottomBackground = new SolidColorBrush(color);
            }

            var originInnerRightBottomBackgroundSource = DependencyPropertyHelper.GetValueSource(this, InnerRightBottomBackgroundProperty);
            if (originInnerRightBottomBackgroundSource.BaseValueSource is BaseValueSource.Default || originInnerRightBottomBackgroundSource.BaseValueSource is BaseValueSource.Style || InnerRightBottomBackground is null || isReFreshingBrush)
            {
                SolidColorBrush solidBorderBrush = OriginBackground as SolidColorBrush;
                Color color = ColorTool.Darken(solidBorderBrush.Color, 0.08f);
                InnerRightBottomBackground = new SolidColorBrush(color);
            }
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
            if(CurrentClipMode is ClipMode.Point)
            {
                OriginEndTime = OriginStartTime;
            }

            BorderBrush = Brushes.White;
            EndTime = OriginEndTime;
            Canvas.SetTop(this, OriginCanvasTop - ActualHeight / 2);
            if (IsChecked is bool value && value)
            {
                OriginBackground = new BrushConverter().ConvertFromString("#FFFFFF") as Brush;
                OriginInnerBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DD7929"));
            }
            else
            {
                OriginBackground = new BrushConverter().ConvertFromString("#B8BEBA") as Brush;
                OriginInnerBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B8BEBA"));
            }

            var originBackgroundBrushSource = DependencyPropertyHelper.GetValueSource(this, BackgroundProperty);
            if (originBackgroundBrushSource.BaseValueSource is BaseValueSource.Default || originBackgroundBrushSource.BaseValueSource is BaseValueSource.Style || Background is null)
            {
                Background = OriginBackground;
            }
            var originInnerBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, InnerBorderBrushProperty);
            if (originInnerBorderBrushSource.BaseValueSource is BaseValueSource.Default || originInnerBorderBrushSource.BaseValueSource is BaseValueSource.Style || InnerBorderBrush is null)
            {
                InnerBorderBrush = OriginInnerBorderBrush;
            }

            // 向上查找父级 Canvas
            parentCanvas = VisualTreeHelper.GetParent(this) as Canvas;
            while (parentCanvas == null && VisualTreeHelper.GetParent(this) != null)
            {
                parentCanvas = VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(this)) as Canvas;
            }

            UpdateBorderColorByBackgroundColor();

            isTimelineClipLoaded = true;
        }

        public void InnerTimeCanvas_Loaded(object sender,RoutedEventArgs e)
        {
            if(isInnerTimeCanvasLoaded)
            {
                return;
            }

            if(sender is Canvas canvas)
            {
                innerTimeCanvas = canvas;
            }

            if (innerTimeCanvas is not null && CurrentClipMode is ClipMode.Rectangle)
            {
                IsChecked = false;
                double offset = 0;
                TimeSpan leftTime = TimeSpan.FromHours(24), rightTime = TimeSpan.Zero;

                #region 搜索最小和最大成员
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
                #endregion

                foreach (var keyframeItem in InnerKeyFrameList)
                {
                    keyframeItem.IsChecked = false;
                    if(keyframeItem.Parent is not null)
                    {
                        continue;
                    }
                    innerTimeCanvas.Children.Add(keyframeItem);
                    offset = (Math.Sqrt(Math.Pow(keyframeItem.Width, 2) + Math.Pow(keyframeItem.Height, 2))) / -2;
                    offset += BorderThickness.Left + Padding.Left;

                    Canvas.SetLeft(keyframeItem, keyframeItem.X + offset);
                    Canvas.SetTop(keyframeItem, (innerTimeCanvas.ActualHeight / 2) - (keyframeItem.Width / 2));
                }

                if (InnerKeyFrameList.Count > 0)
                {
                    if (LeftBorderFrameItem is not null)
                    {
                        LeftBorderFrameItem.Cursor = Cursors.SizeWE;
                        LeftBorderFrameItem.IsChecked = true;
                        LeftBorderFrameItem.PreviewMouseLeftButtonDown += BorderKeyFrameItem_PreviewMouseLeftButtonDown;
                        LeftBorderFrameItem.MouseEnter += KeyFrameItem_MouseEnter;
                        LeftBorderFrameItem.MouseLeave += KeyFrameItem_MouseLeave;
                    }
                    if (RightBorderFrameItem is not null || IsDivided)
                    {
                        RightBorderFrameItem.Cursor = Cursors.SizeWE;
                        RightBorderFrameItem.IsChecked = true;
                        RightBorderFrameItem.PreviewMouseLeftButtonDown += BorderKeyFrameItem_PreviewMouseLeftButtonDown;
                        RightBorderFrameItem.MouseEnter += KeyFrameItem_MouseEnter;
                        RightBorderFrameItem.MouseLeave += KeyFrameItem_MouseLeave;
                        IsDivided = false;
                    }

                    PreviewMouseMove += BorderKeyFrameItem_PreviewMouseMove;
                    PreviewMouseMove += MemberKeyFrameItem_PreviewMouseMove;

                    for (int i = 0; i < InnerKeyFrameList.Count; i++)
                    {
                        if (InnerKeyFrameList[i] == LeftBorderFrameItem || InnerKeyFrameList[i] == RightBorderFrameItem)
                        {
                            continue;
                        }

                        InnerKeyFrameList[i].PreviewMouseLeftButtonDown += MemberKeyFrameItem_PreviewMouseLeftButtonDown;
                        InnerKeyFrameList[i].MouseEnter += KeyFrameItem_MouseEnter;
                        InnerKeyFrameList[i].MouseEnter += MemberFrameItem_MouseEnter;
                        InnerKeyFrameList[i].MouseLeave += KeyFrameItem_MouseLeave;
                        InnerKeyFrameList[i].Cursor = Cursors.SizeWE;
                    }
                }
            }

            isInnerTimeCanvasLoaded = true;
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
            ParentTimeline?.Track_MouseLeftButtonDown(ParentPanel, null);
            if (e.ClickCount == 1)
            {
                if (IsChecked is bool && CurrentClipMode is ClipMode.Rectangle)
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
                dragPoint = e.GetPosition(parentCanvas);
                double currentX = dragPoint.X;
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

            #region 作为被分割片段重新订阅尾部帧成员的事件
            if (IsDivided)
            {
                IsDivided = false;
                UpdateStateAction?.Invoke();
                RightBorderFrameItem.Cursor = Cursors.SizeWE;
                RightBorderFrameItem.IsChecked = true;
                RightBorderFrameItem.PreviewMouseLeftButtonDown += BorderKeyFrameItem_PreviewMouseLeftButtonDown;
                RightBorderFrameItem.MouseEnter += KeyFrameItem_MouseEnter;
                RightBorderFrameItem.MouseLeave += KeyFrameItem_MouseLeave;
            } 
            #endregion

            if(ParentTimeline is not null && ParentTimeline.IsSplitModeOpened)
            {
                SplitPreviewLineVisibility = Visibility.Visible;

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
                    Width = 50,
                    Height = 50
                };
                geometry.Transform = new RotateTransform(45);
                Cursor = CursorHelper.CreateCursor(viewbox, 0, 0);
            }
            else
            {
                Cursor = Cursors.Arrow;
            }
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

                if(CurrentClipMode is ClipMode.Rectangle)
                {
                    startPoint += -BorderThickness.Left - Padding.Left;
                }

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
                Background = OriginBackground = new BrushConverter().ConvertFromString("#FFFFFF") as Brush;
                InnerBorderBrush = OriginInnerBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DD7929"));
                UpdateBorderColorByBackgroundColor();
                isReFreshingBrush = false;
            }
        }

        protected override void OnUnchecked(RoutedEventArgs e)
        {
            base.OnUnchecked(e);
            if (IsChecked is bool)
            {
                isReFreshingBrush = true;
                Background = OriginBackground = new BrushConverter().ConvertFromString("#B8BEBA") as Brush;
                InnerBorderBrush = OriginInnerBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B8BEBA"));
                UpdateBorderColorByBackgroundColor();
                isReFreshingBrush = false;
            }
        }
        #endregion
    }
}