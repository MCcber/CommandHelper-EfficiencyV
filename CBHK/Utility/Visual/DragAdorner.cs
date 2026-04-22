using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace CBHK.Utility.Visual
{
    public class DragAdorner : Adorner
    {
        #region Field
        private Point offset;
        private Size renderSize;
        private Window targetWindow;
        private VisualBrush visualBrush;
        #endregion

        #region Method
        public DragAdorner(UIElement dragVisual, double height) : base(dragVisual)
        {
            targetWindow = Window.GetWindow(dragVisual);
            visualBrush = new VisualBrush(dragVisual)
            {
                Opacity = 0.7,
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top,
                ViewboxUnits = BrushMappingMode.RelativeToBoundingBox,
                Viewbox = new Rect(0, 0, dragVisual.RenderSize.Width, height)
            };
            renderSize = new Size(dragVisual.RenderSize.Width, height);
            IsHitTestVisible = false;
        }

        public void UpdatePosition(Point windowPoint)
        {
            // 将传入的窗口坐标转为相对于 AdornedElement (即 TreeView) 的坐标
            // 这样 OnRender 画出来的矩形才会在正确的位置
            offset = targetWindow.TranslatePoint(windowPoint, AdornedElement);
            InvalidateVisual();
            UpdateLayout();
        } 
        #endregion

        #region Event
        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(visualBrush, null, new Rect(offset, renderSize));
        } 
        #endregion
    }
}