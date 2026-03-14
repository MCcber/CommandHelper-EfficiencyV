using CBHK.CustomControl.Container;
using CBHK.Interface;
using CBHK.Utility.Common;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace CBHK.CustomControl.Input
{
    public class TimelineKeyFrame : ToggleButton, ITimelineElement
    {
        #region Field
        private AnimationTimelineTool animationTimelineTool = new();
        private bool isReFreshingBrush = false;
        private double defaultSize = 16.0;
        private Brush OriginInnerBorderBrush;
        private Brush OriginBackground;
        private Canvas parentCanvas;
        private bool isDragging = false;
        private Point dragPoint;
        private Point dragStartPoint;
        #endregion

        #region Property
        public double OriginCanvasTop { get; set; }
        public Timeline ParentTimeline { get; set; }
        public object ParentPanel { get; set; }
        public ObservableCollection<IKeyFrameData> DataList { get; set; } = [];

        public TimeSpan StartTime
        {
            get { return (TimeSpan)GetValue(StartTimeProperty); }
            set { SetValue(StartTimeProperty, value); }
        }

        public static readonly DependencyProperty StartTimeProperty =
            DependencyProperty.Register("StartTime", typeof(TimeSpan), typeof(ContinuousKeyframeItem), new PropertyMetadata(default(TimeSpan)));

        public TimeRulerElement Ruler
        {
            get { return (TimeRulerElement)GetValue(RulerProperty); }
            set { SetValue(RulerProperty, value); }
        }

        public static readonly DependencyProperty RulerProperty =
            DependencyProperty.Register("Ruler", typeof(TimeRulerElement), typeof(TimelineKeyFrame), new PropertyMetadata(default(TimeRulerElement)));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(TimelineKeyFrame), new PropertyMetadata(default(string)));

        public Brush InnerBorderBrush
        {
            get { return (Brush)GetValue(InnerBorderBrushProperty); }
            set { SetValue(InnerBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty InnerBorderBrushProperty =
            DependencyProperty.Register("InnerBorderBrush", typeof(Brush), typeof(TimelineKeyFrame), new PropertyMetadata(default(Brush)));

        public Brush InnerLeftTopBackground
        {
            get { return (Brush)GetValue(InnerLeftTopBackgroundProperty); }
            set { SetValue(InnerLeftTopBackgroundProperty, value); }
        }

        public static readonly DependencyProperty InnerLeftTopBackgroundProperty =
            DependencyProperty.Register("InnerLeftTopBackground", typeof(Brush), typeof(TimelineKeyFrame), new PropertyMetadata(default(Brush)));

        public Brush InnerRightTopBackground
        {
            get { return (Brush)GetValue(InnerRightTopBackgroundProperty); }
            set { SetValue(InnerRightTopBackgroundProperty, value); }
        }

        public static readonly DependencyProperty InnerRightTopBackgroundProperty =
            DependencyProperty.Register("InnerRightTopBackground", typeof(Brush), typeof(TimelineKeyFrame), new PropertyMetadata(default(Brush)));

        public Brush InnerLeftBottomBackground
        {
            get { return (Brush)GetValue(InnerLeftBottomBackgroundProperty); }
            set { SetValue(InnerLeftBottomBackgroundProperty, value); }
        }

        public static readonly DependencyProperty InnerLeftBottomBackgroundProperty =
            DependencyProperty.Register("InnerLeftBottomBackground", typeof(Brush), typeof(TimelineKeyFrame), new PropertyMetadata(default(Brush)));

        public Brush InnerRightBottomBackground
        {
            get { return (Brush)GetValue(InnerRightBottomBackgroundProperty); }
            set { SetValue(InnerRightBottomBackgroundProperty, value); }
        }

        public static readonly DependencyProperty InnerRightBottomBackgroundProperty =
            DependencyProperty.Register("InnerRightBottomBackground", typeof(Brush), typeof(TimelineKeyFrame), new PropertyMetadata(default(Brush)));

        public Brush LeftTopBorderBrush
        {
            get { return (Brush)GetValue(LeftTopBorderBrushProperty); }
            set { SetValue(LeftTopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftTopBorderBrushProperty =
            DependencyProperty.Register("LeftTopBorderBrush", typeof(Brush), typeof(TimelineKeyFrame), new PropertyMetadata(default(Brush)));

        public Brush RightBottomBorderBrush
        {
            get { return (Brush)GetValue(RightBottomBorderBrushProperty); }
            set { SetValue(RightBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RightBottomBorderBrushProperty =
            DependencyProperty.Register("RightBottomBorderBrush", typeof(Brush), typeof(TimelineKeyFrame), new PropertyMetadata(default(Brush)));

        public Brush BorderCornerBrush
        {
            get { return (Brush)GetValue(BorderCornerBrushProperty); }
            set { SetValue(BorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(TimelineKeyFrame), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public TimelineKeyFrame()
        {
            Width = Height = defaultSize;
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

        public void UpdateKeyFrameByTime()
        {

        }

        public TimelineKeyFrame Clone()
        {
            TimelineKeyFrame result = new();
            return result;
        }
        #endregion

        #region Event
        protected override void OnChecked(RoutedEventArgs e)
        {
            base.OnChecked(e);
            InnerBorderBrush = OriginInnerBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DD7929"));
            UpdateBorderColorByBackgroundColor();
        }

        protected override void OnUnchecked(RoutedEventArgs e)
        {
            base.OnUnchecked(e);
            Background = OriginBackground = new BrushConverter().ConvertFromString("#B8BEBA") as Brush;
            InnerBorderBrush = OriginInnerBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B8BEBA"));
            UpdateBorderColorByBackgroundColor();
        }

        #region 调整关键帧位置
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if(ParentTimeline is not null)
            {
                ParentTimeline.CurrentSplitTimelineClip = null;
            }
            ParentTimeline?.Track_MouseLeftButtonDown(ParentPanel, null);
            if (e.ClickCount == 1)
            {
                if (IsChecked is bool)
                {
                    IsChecked = !IsChecked;
                }

                if (IsChecked is bool result && result)
                {
                    BorderBrush = Brushes.Black;
                }
                else
                {
                    BorderBrush = Brushes.White;
                }
            }
            // 获取父级 Canvas (作为计算坐标的绝对参考系)
            if (parentCanvas is not null && e.ClickCount == 2)
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
            if (IsMouseCaptured && parentCanvas is not null && isDragging)
            {
                dragPoint = e.GetPosition(parentCanvas);
                TimeSpan newStartTime = animationTimelineTool.ConvertPixelToTime(dragPoint.X, Ruler);
                StartTime = newStartTime;
            }
            #endregion

            base.OnPreviewMouseMove(e);
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (!isDragging && IsChecked is not null)
            {
                IsChecked = !IsChecked;
            }
            isDragging = false;
            ReleaseMouseCapture();

            base.OnPreviewMouseLeftButtonUp(e);
        }

        #endregion

        #endregion
    }
}
