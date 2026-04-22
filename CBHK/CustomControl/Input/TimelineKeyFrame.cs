using CBHK.CustomControl.Container;
using CBHK.Interface.Data;
using CBHK.Interface.Visual;
using CBHK.Utility.Common;
using CBHK.Utility.Visual;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CBHK.CustomControl.Input
{
    public class TimelineKeyFrame : BaseVectorControl, ITimelineElement
    {
        #region Field
        private AnimationTimelineTool animationTimelineTool = new();
        private Brush OriginInnerBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B8BEBA"));
        private bool isReFreshingBrush = false;
        private double defaultSize = 16.0;
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

        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked", typeof(bool), typeof(TimelineKeyFrame), new PropertyMetadata(default(bool),IsChecked_Changed));

        public TimeSpan StartTime
        {
            get { return (TimeSpan)GetValue(StartTimeProperty); }
            set { SetValue(StartTimeProperty, value); }
        }

        public static readonly DependencyProperty StartTimeProperty =
            DependencyProperty.Register("StartTime", typeof(TimeSpan), typeof(TimelineKeyFrame), new PropertyMetadata(default(TimeSpan), Frame_PropertyChanged));

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

        public bool IsPlayed { get; set; }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(TimelineKeyFrame), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public TimelineKeyFrame()
        {
            Width = Height = defaultSize;
            Loaded += TimelineKeyFrame_Loaded;
        }

        public override void UpdateBorderColorByBackgroundColor()
        {
            base.UpdateBorderColorByBackgroundColor();

            SolidColorBrush solidColorBrush = IsChecked is bool value && value ? ThemeBackground as SolidColorBrush : OriginInnerBrush as SolidColorBrush;

            if (solidColorBrush is not null)
            {
                BorderBrush = new SolidColorBrush(solidColorBrush.Color);
                BorderCornerBrush = new SolidColorBrush(ColorTool.ModifyColorBrightness(solidColorBrush.Color, 0.4f, Model.Common.ColorModifyMode.Lighten));
                LeftTopBorderBrush = new SolidColorBrush(ColorTool.ModifyColorBrightness(solidColorBrush.Color, 0.3f, Model.Common.ColorModifyMode.Lighten));
                RightBottomBorderBrush = new SolidColorBrush(ColorTool.ModifyColorBrightness(solidColorBrush.Color, 0.2f, Model.Common.ColorModifyMode.Lighten));
                InnerLeftTopBackground = new SolidColorBrush(ColorTool.ModifyColorBrightness(solidColorBrush.Color, 0.02f, Model.Common.ColorModifyMode.Darken));
                InnerRightTopBackground = new SolidColorBrush(ColorTool.ModifyColorBrightness(solidColorBrush.Color, 0.05f, Model.Common.ColorModifyMode.Darken));
                InnerLeftBottomBackground = new SolidColorBrush(ColorTool.ModifyColorBrightness(solidColorBrush.Color, 0.05f, Model.Common.ColorModifyMode.Darken));
                InnerRightBottomBackground = new SolidColorBrush(ColorTool.ModifyColorBrightness(solidColorBrush.Color, 0.08f, Model.Common.ColorModifyMode.Darken));
            }
        }

        private static void IsChecked_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimelineKeyFrame keyFrame)
            {
                keyFrame.OnIsChecked_Changed(e);
            }
        }

        private void OnIsChecked_Changed(DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool value && value)
            {
                OnChecked();
            }
            else
            {
                OnUnchecked();
            }
        }

        /// <summary>
        /// 根据时间更新关键帧的位置
        /// </summary>
        public void UpdateKeyFrameByTime()
        {
            if (Ruler is not null)
            {
                double startPoint = animationTimelineTool.ConvertTimeToPixel(StartTime, Ruler);
                Canvas.SetLeft(this, startPoint);
            }
        }

        public TimelineKeyFrame Clone()
        {
            TimelineKeyFrame result = new();
            return result;
        }
        #endregion

        #region Event
        private void OnChecked()
        {
            isReFreshingBrush = true;
            UpdateBorderColorByBackgroundColor();
            isReFreshingBrush = false;
        }

        private void OnUnchecked()
        {
            isReFreshingBrush = true;
            UpdateBorderColorByBackgroundColor();
            isReFreshingBrush = false;
        }

        private void TimelineKeyFrame_Loaded(object sender, RoutedEventArgs e)
        {
            Canvas.SetTop(this, OriginCanvasTop - ActualHeight / 2);
            // 向上查找父级 Canvas
            parentCanvas = VisualTreeHelper.GetParent(this) as Canvas;
            while (parentCanvas == null && VisualTreeHelper.GetParent(this) != null)
            {
                parentCanvas = VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(this)) as Canvas;
            }
            UpdateBorderColorByBackgroundColor();
        }

        private static void Frame_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimelineKeyFrame keyFrame)
            {
                keyFrame.TimelineClipFrame_Changed(e.Property.Name);
            }
        }

        public void TimelineClipFrame_Changed(string PropertyName)
        {
            switch (PropertyName)
            {
                case "StartTime":
                    {
                        UpdateKeyFrameByTime();
                        break;
                    }
            }
        }

        #region 调整关键帧位置
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if(ParentTimeline is not null)
            {
                ParentTimeline.CurrentSplitTimelineClip = null;
            }
            ParentTimeline?.Track_MouseLeftButtonDown(ParentPanel, null);
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

        /// <summary>
        /// 处理关键帧拖拽
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            if (IsMouseCaptured && parentCanvas is not null && isDragging)
            {
                dragPoint = e.GetPosition(parentCanvas);
                TimeSpan timeSpan = animationTimelineTool.ConvertPixelToTime(dragPoint.X, Ruler);
                if (ParentTimeline is not null && ParentTimeline.TimelineElementMarkerMap.TryGetValue(this, out (TimeSpan, List<TimeSpan>) dataItemList))
                {
                    List<TimeSpan> list = dataItemList.Item2;
                    ParentTimeline.TimelineElementMarkerMap[this] = new(timeSpan, list);
                }
                StartTime = timeSpan;
            }

            base.OnPreviewMouseMove(e);
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1 && !isDragging)
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