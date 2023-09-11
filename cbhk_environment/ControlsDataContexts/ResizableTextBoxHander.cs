using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace cbhk_environment.ControlsDataContexts
{
    public class ResizableTextBoxHander
    {
        private double prevWidth, prevHeight;
        private Point prevPoint;
        Grid this_grid;

        public void Preview_mouseleftbutton_down(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;
                Grid grid = (Grid)sender;
                grid.CaptureMouse();

                if (grid.TemplatedParent == null) return;
                prevWidth = (grid.TemplatedParent as TextBox).ActualWidth;
                prevHeight = (grid.TemplatedParent as TextBox).ActualHeight;

                prevPoint = e.GetPosition(null);
            }
            catch { }
        }

        public void Preview_mouseleftbutton_up(object sender, MouseButtonEventArgs e)
        {
            try
            {
                e.Handled = true;
                Grid grid = (Grid)sender;
                grid.ReleaseMouseCapture();

                prevWidth = (grid.TemplatedParent as TextBox).ActualWidth;
                prevHeight = (grid.TemplatedParent as TextBox).ActualHeight;
            }
            catch { }
        }
        public void Preview_mouseleftbutton_move(object sender, MouseEventArgs e)
        {
            try
            {
                this_grid = e.Source as Grid;
                if (e.LeftButton == MouseButtonState.Pressed && this_grid != null)
                {
                    Point point = e.GetPosition(null);
                    var xDiff = point.X - prevPoint.X;
                    var yDiff = point.Y - prevPoint.Y;

                    (this_grid.TemplatedParent as TextBox).Width = prevWidth + xDiff;
                    (this_grid.TemplatedParent as TextBox).Height = prevHeight + yDiff;


                    prevWidth = (this_grid.TemplatedParent as TextBox).Width;
                    prevHeight = (this_grid.TemplatedParent as TextBox).Height;
                    prevPoint = e.GetPosition(null);
                }
            }
            catch { }
        }
    }
}
