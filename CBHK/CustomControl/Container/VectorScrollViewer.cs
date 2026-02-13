using CBHK.Utility.Common;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    public class VectorScrollViewer:ScrollViewer
    {
        #region Field
        private Brush OriginForeground;
        private Brush OriginBackground;
        private Brush OriginLeftTopBorderBrush;
        private Brush OriginRightTopBorderBrush;
        private Brush OriginBottomBorderBrush;

        private Point _scrollStartPoint;
        private Point _scrollStartOffset;
        private bool _isDragging = false;
        private readonly double _dragThreshold = 5.0; // 移动超过5像素才判定为拖拽
        private ScrollBar VerticalScrollBar;
        private ScrollBar HorizontalScrollBar;
        #endregion

        #region Property
        public Brush BottomBorderBrush
        {
            get { return (Brush)GetValue(BottomBorderBrushProperty); }
            set { SetValue(BottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty BottomBorderBrushProperty =
            DependencyProperty.Register("BottomBorderBrush", typeof(Brush), typeof(VectorScrollViewer), new PropertyMetadata(default(Brush)));

        public Brush LeftTopBorderBrush
        {
            get { return (Brush)GetValue(LeftTopBorderBrushProperty); }
            set { SetValue(LeftTopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftTopBorderBrushProperty =
            DependencyProperty.Register("LeftTopBorderBrush", typeof(Brush), typeof(VectorScrollViewer), new PropertyMetadata(default(Brush)));

        public Brush RightBottomBorderBrush
        {
            get { return (Brush)GetValue(RightBottomBorderBrushProperty); }
            set { SetValue(RightBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RightBottomBorderBrushProperty =
            DependencyProperty.Register("RightBottomBorderBrush", typeof(Brush), typeof(VectorScrollViewer), new PropertyMetadata(default(Brush)));

        public Brush BorderCornerBrush
        {
            get { return (Brush)GetValue(BorderCornerBrushProperty); }
            set { SetValue(BorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(VectorScrollViewer), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorScrollViewer()
        {
            Loaded += VectorScrollViewer_Loaded;
            MouseWheel += VectorScrollViewer_MouseWheel;
            PreviewMouseDown += ScrollViewer_PreviewMouseDown;
            PreviewMouseMove += ScrollViewer_PreviewMouseMove;
            PreviewMouseUp += ScrollViewer_PreviewMouseUp;
        }

        private void UpdateBorderColorByBackgroundColor()
        {
            var foregroundSource = DependencyPropertyHelper.GetValueSource(this, ForegroundProperty);
            if (foregroundSource.BaseValueSource is BaseValueSource.DefaultStyle || foregroundSource.BaseValueSource is BaseValueSource.Style || Foreground is null)
            {
                Foreground = OriginForeground = Brushes.White;
            }
            var backgroundSource = DependencyPropertyHelper.GetValueSource(this, BackgroundProperty);
            if (backgroundSource.BaseValueSource is BaseValueSource.DefaultStyle || backgroundSource.BaseValueSource is BaseValueSource.Style || backgroundSource.BaseValueSource is BaseValueSource.Default || Background is null)
            {
                Background = OriginBackground = new BrushConverter().ConvertFromString("#48494A") as Brush;
            }
            var borderBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderBrushProperty);
            if (borderBrushSource.BaseValueSource is BaseValueSource.DefaultStyle || borderBrushSource.BaseValueSource is BaseValueSource.Style || BorderBrush is null)
            {
                BorderBrush = Brushes.Black;
            }
            var originLeftTopBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, LeftTopBorderBrushProperty);
            if (originLeftTopBorderBrushSource.BaseValueSource is BaseValueSource.Default || originLeftTopBorderBrushSource.BaseValueSource is BaseValueSource.Style || LeftTopBorderBrush is null)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.2f);
                LeftTopBorderBrush = OriginLeftTopBorderBrush = new SolidColorBrush(color);
            }
            var originRightBottomBrushSource = DependencyPropertyHelper.GetValueSource(this, RightBottomBorderBrushProperty);
            if (originRightBottomBrushSource.BaseValueSource is BaseValueSource.Default || originRightBottomBrushSource.BaseValueSource is BaseValueSource.Style || RightBottomBorderBrush is null)
            {
                Color color = ColorTool.Lighten((Background as SolidColorBrush).Color, 0.1f);
                RightBottomBorderBrush = new SolidColorBrush(color);
            }
            var originCornerBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderCornerBrushProperty);
            if (originCornerBrushSource.BaseValueSource is BaseValueSource.Default || originCornerBrushSource.BaseValueSource is BaseValueSource.Style || BorderCornerBrush is null)
            {
                Color color = ColorTool.Lighten((Background as SolidColorBrush).Color, 0.4f);
                BorderCornerBrush = new SolidColorBrush(color);
            }
            var originBottomBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, BottomBorderBrushProperty);
            if (originBottomBorderBrushSource.BaseValueSource is BaseValueSource.Default || originBottomBorderBrushSource.BaseValueSource is BaseValueSource.Style || BottomBorderBrush is null)
            {
                Color color = ColorTool.Darken((Background as SolidColorBrush).Color, 0.4f);
                BottomBorderBrush = new SolidColorBrush(color);
            }
        }
        #endregion

        #region Event
        private void VectorScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            VerticalScrollBar = GetTemplateChild("PART_VerticalScrollBar") as ScrollBar;
            HorizontalScrollBar = GetTemplateChild("PART_HorizontalScrollBar") as ScrollBar;
            UpdateBorderColorByBackgroundColor();
        }

        public void VectorScrollViewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var templatedParent = (sender as FrameworkElement).TemplatedParent;
            if (templatedParent is VectorScrollViewer vectorScrollViewer)
            {
                if (vectorScrollViewer.VerticalOffset == vectorScrollViewer.ScrollableHeight || vectorScrollViewer.VerticalOffset == 0)
                {
                    VectorScrollViewer parent = vectorScrollViewer.FindParent<VectorScrollViewer>();
                    if (parent is null) return;
                    var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                    {
                        RoutedEvent = MouseWheelEvent,
                        Source = sender
                    };
                    parent.RaiseEvent(eventArg);
                }
            }
        }

        private void ScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var sv = sender as ScrollViewer;

            _scrollStartPoint = e.GetPosition(sv);
            _scrollStartOffset = new Point(sv.HorizontalOffset, sv.VerticalOffset);
            _isDragging = false;
        }

        private void ScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var sv = sender as ScrollViewer;
            if (e.LeftButton == MouseButtonState.Pressed && PanningMode is not PanningMode.None)
            {
                Point currentPoint = e.GetPosition(sv);

                // 判断是否达到了拖拽的最小距离
                if (!_isDragging &&
                    (Math.Abs(currentPoint.X - _scrollStartPoint.X) > _dragThreshold ||
                     Math.Abs(currentPoint.Y - _scrollStartPoint.Y) > _dragThreshold) && HorizontalScrollBar.Visibility is not Visibility.Visible && VerticalScrollBar.Visibility is not Visibility.Visible)
                {
                    _isDragging = true;
                    sv.CaptureMouse(); // 确定是拖拽后，再强行夺取鼠标控制权
                }

                if (_isDragging)
                {
                    double deltaX = currentPoint.X - _scrollStartPoint.X;
                    double deltaY = currentPoint.Y - _scrollStartPoint.Y;

                    sv.ScrollToHorizontalOffset(_scrollStartOffset.X - deltaX);
                    sv.ScrollToVerticalOffset(_scrollStartOffset.Y - deltaY);

                    // 改变光标增加反馈
                    sv.Cursor = Cursors.Hand;
                }
            }
        }

        private void ScrollViewer_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            var sv = sender as ScrollViewer;

            if (sv.IsMouseCaptured)
            {
                sv.ReleaseMouseCapture();
                sv.Cursor = Cursors.Arrow;

                if (_isDragging)
                {
                    e.Handled = true;
                }
            }

            _isDragging = false;
        }
        #endregion
    }
}
