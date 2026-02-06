using CBHK.Utility.Common;
using System.Windows;
using System.Windows.Controls;
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
        #endregion
    }
}
