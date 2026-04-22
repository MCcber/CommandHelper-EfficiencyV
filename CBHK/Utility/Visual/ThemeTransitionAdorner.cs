using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CBHK.Utility.Visual
{
    public class ThemeTransitionAdorner : Adorner
    {
        #region Field
        private Point _visualOffset;
        private readonly Brush snapshotBrush;
        private Geometry clipGeometry;          // 动态计算的“矩形挖圆”几何体
        private readonly double maxRadius;
        private Point center;

        private readonly RectangleGeometry _rectGeometry;
        private readonly EllipseGeometry _circleGeometry;
        private readonly CombinedGeometry _combinedGeometry;

        Rect renderRect = new();
        #endregion

        #region Property
        public double CurrentRadius
        {
            get { return (double)GetValue(CurrentRadiusProperty); }
            set { SetValue(CurrentRadiusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CurrentRadius.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentRadiusProperty =
            DependencyProperty.Register("CurrentRadius", typeof(double), typeof(ThemeTransitionAdorner), new PropertyMetadata(default(double), OnCurrentRadiusChanged));
        #endregion

        #region Method
        public ThemeTransitionAdorner(UIElement adornedElement, Brush snapshotBrush) : base(adornedElement)
        {
            this.snapshotBrush = snapshotBrush;
            this.snapshotBrush.Freeze();

            // 基于窗口实际大小计算最大半径（稍后会在 OnRender 中重新获取）
            var window = adornedElement as Window ?? GetParentWindow(adornedElement);
            if (window is not null)
            {
                _visualOffset = adornedElement.TranslatePoint(new Point(0, 0), window);
                var bounds = new Rect(0, 0, window.ActualWidth, window.ActualHeight);
                maxRadius = Math.Sqrt(bounds.Width * bounds.Width + bounds.Height * bounds.Height) / 1.5;
                center = new Point(bounds.Width / 2 - _visualOffset.X, bounds.Height / 2 - _visualOffset.Y);

                _rectGeometry = new RectangleGeometry(bounds);
                _circleGeometry = new EllipseGeometry(new Point(bounds.Width / 2, bounds.Height / 2), 0, 0);
                _combinedGeometry = new CombinedGeometry(GeometryCombineMode.Exclude, _rectGeometry, _circleGeometry);

                renderRect = new(-_visualOffset.X, -_visualOffset.Y, AdornedElement.RenderSize.Width + _visualOffset.X, AdornedElement.RenderSize.Height + _visualOffset.Y);
            }
            else
            {
                maxRadius = 2000; // 保底值
                center = new Point(0, 0);
            }

            IsHitTestVisible = false; // 让点击穿透
        }

        private Window GetParentWindow(UIElement element)
        {
            return PresentationSource.FromVisual(element)?.RootVisual as Window;
        }

        /// <summary>
        /// 启动圆形扩散动画
        /// </summary>
        /// <param name="duration">动画持续时间</param>
        /// <param name="startRadius">起始半径（默认 10）</param>
        public async Task PlayRevealAnimationAsync(TimeSpan duration, double startRadius = 10)
        {
            var tcs = new TaskCompletionSource<bool>();

            var radiusAnim = new DoubleAnimation(startRadius, maxRadius, duration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            SetValue(CurrentRadiusProperty, startRadius);

            radiusAnim.Completed += (s, e) =>
            {
                var layer = AdornerLayerHelper.GetAdornerLayer(AdornedElement);
                layer?.Remove(this);
                tcs.SetResult(true);
            };
            BeginAnimation(CurrentRadiusProperty, radiusAnim);

            await tcs.Task;
        }

        private void UpdateClipGeometry(double currentRadius)
        {
            // 只修改现有的几何体属性，不要 new 任何东西！
            _circleGeometry.RadiusX = currentRadius;
            _circleGeometry.RadiusY = currentRadius;

            // 强制重绘
            InvalidateVisual();
        }

        private static void OnCurrentRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ThemeTransitionAdorner adorner)
            {
                // 半径变化时，重新生成裁剪几何体并刷新
                adorner.UpdateClipGeometry((double)e.NewValue);
            }
        }
        #endregion

        #region Event
        protected override void OnRender(DrawingContext drawingContext)
        {
            // 直接使用预设好的几何体
            drawingContext.PushClip(_combinedGeometry);
            drawingContext.DrawRectangle(snapshotBrush, null, /*new Rect(AdornedElement.RenderSize)*/renderRect);
            drawingContext.Pop();
        } 
        #endregion
    }
}