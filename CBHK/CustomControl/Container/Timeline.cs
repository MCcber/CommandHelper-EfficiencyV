using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
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
        #endregion

        #region Property
        public TimeRulerElement Ruler { get; set; }
        public Action TimeUpdateAction { get; set; }

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

        public TimeSpan CurrentTime
        {
            get { return (TimeSpan)GetValue(CurrentTimeProperty); }
            set { SetValue(CurrentTimeProperty, value); }
        }

        public static readonly DependencyProperty CurrentTimeProperty =
            DependencyProperty.Register("CurrentTime", typeof(TimeSpan), typeof(Timeline), new PropertyMetadata(default(TimeSpan), OnCurrentTimeChanged));

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
            TrackPanelMargin = new(5, 30, 10, 0);
            TrackList = [
                new TimelineTrack() { TrackName = "头部",Foreground = Brushes.Black,Margin = new(5,5,5,10) },
                new TimelineTrack() { TrackName = "身体",Foreground = Brushes.Black,Margin = new(5,5,5,10) },
                new TimelineTrack() { TrackName = "左臂",Foreground = Brushes.Black,Margin = new(5,5,5,10) },
                new TimelineTrack() { TrackName = "右臂",Foreground = Brushes.Black,Margin = new(5,5,5,10) },
                new TimelineTrack() { TrackName = "左腿",Foreground = Brushes.Black,Margin = new(5,5,5,10) },
                new TimelineTrack() { TrackName = "右腿",Foreground = Brushes.Black,Margin = new(5,5,5,10) }
                ];
        }

        /// <summary>
        /// 将鼠标横坐标转换为时间点
        /// </summary>
        /// <param name="mousePoint"></param>
        /// <returns></returns>
        private TimeSpan ConvertPixelToTime(double x)
        {
            if (Ruler == null)
            {
                return TimeSpan.Zero;
            }

            // 1. 计算原始秒数
            double totalSeconds = x / (Ruler.BasePixelsPerSecond * Ruler.ZoomFactor);

            // 2. 限制在有效时长内
            totalSeconds = Math.Clamp(totalSeconds, 0, Ruler.Maximum);

            // 3. 磁吸：四舍五入到最近的 0.05s (即 1/20 秒，1个 Minecraft Tick)
            double tickCount = Math.Round(totalSeconds * 20);
            return TimeSpan.FromSeconds(tickCount / 20.0);
        }

        /// <summary>
        /// 将时间点转为画布横坐标
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private double ConvertTimeToPixel(TimeSpan time)
        {
            if (Ruler == null)
            {
                return 0;
            }
            // 公式：秒数 * 基础每秒像素 * 缩放倍率
            return time.TotalSeconds * Ruler.BasePixelsPerSecond * Ruler.ZoomFactor;
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

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if(previewLine is null || !IsShowPreviewLine)
            {
                return;
            }
            double rawSeconds = ConvertPixelToTime(e.GetPosition(canvas).X).TotalSeconds;
            TimeSpan currentTime = SnapToTick(rawSeconds);
            double pixelX = ConvertTimeToPixel(currentTime);
            pixelX = Math.Clamp(pixelX, 0, canvas.ActualWidth);
            Canvas.SetLeft(previewLine, pixelX);
        }

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
            CurrentTime = ConvertPixelToTime(newX);
        }

        private void Canvas_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (canvas is not null && Ruler is not null)
            {
                double rawSeconds = ConvertPixelToTime(e.GetPosition(canvas).X).TotalSeconds;
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
            double newX = ConvertTimeToPixel(newValue);
            Canvas.SetLeft(playHeadGrid, newX);
            TimeUpdateAction?.Invoke();
        }

        public void UpdatePlayHeadPositionByCurrentTime()
        {
            if (playHeadGrid != null && Ruler != null)
            {
                // 每次更新位置前，先确保画布宽度是对的
                UpdateTotalWidth();

                double newX = ConvertTimeToPixel(CurrentTime);
                Canvas.SetLeft(playHeadGrid, newX);
            }
        }
        #endregion
    }
}