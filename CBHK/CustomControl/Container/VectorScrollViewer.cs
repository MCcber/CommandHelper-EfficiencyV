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

        private Point scrollStartPoint;
        private Point scrollStartOffset;
        private bool isDragging = false;
        private bool pressOnInteractiveChild = false;
        private readonly double dragThreshold = 5.0; // 移动超过5像素才判定为拖拽
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

            pressOnInteractiveChild = false;
            if(!IsAllowDragToScroll)
            {
                return;
            }
            // 按在"自己有鼠标语义"的控件上（文本框选字 / 按钮按住 / 滑块 / 滚动条）时，
            // 这一次按住不许升级成拖拽滚动：CaptureMouse 会把鼠标从正在拖选文字的编辑器手里抢走，
            // 现场就是"行内文本框拖不出选区"（选区在越过拖拽阈值那一下就停了）。
            pressOnInteractiveChild = IsInteractiveSource(e.OriginalSource as DependencyObject, sv);
            scrollStartPoint = e.GetPosition(sv);
            scrollStartOffset = new Point(sv.HorizontalOffset, sv.VerticalOffset);
            isDragging = false;
        }

        private void ScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsAllowDragToScroll || pressOnInteractiveChild)
            {
                return;
            }
            var sv = sender as ScrollViewer;
            if (e.LeftButton == MouseButtonState.Pressed && PanningMode is not PanningMode.None)
            {
                Point currentPoint = e.GetPosition(sv);

                // 判断是否达到了拖拽的最小距离
                if (!isDragging &&
                    (Math.Abs(currentPoint.X - scrollStartPoint.X) > dragThreshold ||
                     Math.Abs(currentPoint.Y - scrollStartPoint.Y) > dragThreshold) && HorizontalScrollBar.Visibility is not Visibility.Visible && VerticalScrollBar.Visibility is not Visibility.Visible)
                {
                    isDragging = true;
                    sv.CaptureMouse(); // 确定是拖拽后，再强行夺取鼠标控制权
                }

                if (isDragging)
                {
                    double deltaX = currentPoint.X - scrollStartPoint.X;
                    double deltaY = currentPoint.Y - scrollStartPoint.Y;

                    sv.ScrollToHorizontalOffset(scrollStartOffset.X - deltaX);
                    sv.ScrollToVerticalOffset(scrollStartOffset.Y - deltaY);

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

                if (isDragging)
                {
                    e.Handled = true;
                }
            }

            isDragging = false;
            pressOnInteractiveChild = false;
        }

        /// <summary>按下点（含祖先，到本 ScrollViewer 为止）是不是"自己做主鼠标"的控件。</summary>
        private static bool IsInteractiveSource(DependencyObject source, DependencyObject stopAt)
        {
            for (var current = source; current is not null && !ReferenceEquals(current, stopAt); current = ParentOf(current))
            {
                if (current is TextBoxBase or PasswordBox or ComboBox or ButtonBase or Thumb or ScrollBar or Slider)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 上溯父级：富文本的 Run 是 FrameworkContentElement、不是 Visual，
        /// 直接 VisualTreeHelper.GetParent 会抛"不是 Visual"（与行视图同一套兜底）。
        /// </summary>
        private static DependencyObject ParentOf(DependencyObject current)
        {
            if (current is Visual or System.Windows.Media.Media3D.Visual3D)
            {
                return VisualTreeHelper.GetParent(current);
            }
            if (current is ContentElement content)
            {
                return ContentOperations.GetParent(content) ?? (content as FrameworkContentElement)?.Parent;
            }
            return null;
        }
        #endregion
    }
}