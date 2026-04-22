using CBHK.Model.Constant;
using CBHK.Utility.Common;
using CBHK.Utility.Visual;
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
        public bool IsAllowDragToScroll
        {
            get { return (bool)GetValue(IsAllowDragToScrollProperty); }
            set { SetValue(IsAllowDragToScrollProperty, value); }
        }

        public static readonly DependencyProperty IsAllowDragToScrollProperty =
            DependencyProperty.Register("IsAllowDragToScroll", typeof(bool), typeof(VectorScrollViewer), new PropertyMetadata(default(bool)));

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

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(VectorScrollViewer), new PropertyMetadata(default(Brush)));

        public Brush ScrollbarBackground
        {
            get { return (Brush)GetValue(ScrollbarBackgroundProperty); }
            set { SetValue(ScrollbarBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ScrollbarBackgroundProperty =
            DependencyProperty.Register("ScrollbarBackground", typeof(Brush), typeof(VectorScrollViewer), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorScrollViewer()
        {
            SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
            SetResourceReference(ForegroundProperty, Theme.CommonForeground);
            Loaded += VectorScrollViewer_Loaded;
            MouseWheel += VectorScrollViewer_MouseWheel;
            PreviewMouseDown += ScrollViewer_PreviewMouseDown;
            PreviewMouseMove += ScrollViewer_PreviewMouseMove;
            PreviewMouseUp += ScrollViewer_PreviewMouseUp;
        }

        private void UpdateBorderColorByBackgroundColor()
        {
            if(ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(themeBrush.Color);
                ScrollbarBackground = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.6f));
                LeftTopBorderBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.2f));
                RightBottomBorderBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.1f));
                BorderCornerBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.4f));
                BottomBorderBrush = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.4f));
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

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if(e.Property == ThemeBackgroundProperty)
            {
                UpdateBorderColorByBackgroundColor();
            }
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

            if(!IsAllowDragToScroll)
            {
                return;
            }
            _scrollStartPoint = e.GetPosition(sv);
            _scrollStartOffset = new Point(sv.HorizontalOffset, sv.VerticalOffset);
            _isDragging = false;
        }

        private void ScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsAllowDragToScroll)
            {
                return;
            }
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
            if (!IsAllowDragToScroll)
            {
                return;
            }
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