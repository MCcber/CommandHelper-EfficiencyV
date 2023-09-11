using System.Windows.Controls;
using System.Windows.Input;

namespace cbhk_environment.CustomControls.TimeLines
{
    /// <summary>
    /// TimelineElement.xaml 的交互逻辑
    /// </summary>
    public partial class TimelineElement : UserControl
    {
        TimeLine parent;
        int seconds;
        string display = "";
        double canvasLeft;
        double mouseXInitial;
        bool primed = false;

        /// <summary>
        /// Creates the visual TimelineElement
        /// </summary>
        /// <param name="height">Height of the element (typically height of the Timeline's inner 'border' field)</param>
        /// <param name="seconds">Position on the timeline in seconds</param>
        public TimelineElement(TimeLine parent, int height, int seconds)
        {
            InitializeComponent();
            this.seconds = seconds;
            this.parent = parent;
            rectOuter.Height = height;
            rectInner.Height = height;

            // Create friendly time for tooltip text
            setupTooltip();

            // Setup for draggability
            MouseEnter += TimelineElement_MouseEnter;
            MouseLeave += TimelineElement_MouseLeave;
            MouseLeftButtonDown += TimelineElement_MouseLeftButtonDown;
        }

        // Creates tooltip from seconds value
        private void setupTooltip()
        {
            string m = (seconds / 60).ToString();
            if (m.Length < 2)
                m = "0" + m;
            string s = (seconds % 60).ToString();
            if (s.Length < 2)
                s = "0" + s;
            display = m + ":" + s;
            mainCanvas.ToolTip = display;
        }

        // Called by the parent to give it updated seconds based on its position
        public void SetSeconds(int seconds)
        {
            this.seconds = seconds;
            setupTooltip();
        }

        // Listeners
        private void TimelineElement_MouseEnter(object sender, MouseEventArgs e)
        {
            // Respond visually
            rectOuter.Opacity = 0.5;

            // Prime
            primed = true;
        }
        private void TimelineElement_MouseLeave(object sender, MouseEventArgs e)
        {
            // Respond visually
            rectOuter.Opacity = 0.3;

            // Deprime
            primed = false;
        }
        private void TimelineElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (primed)
            {
                // Respond visually
                rectOuter.Opacity = 0.6;

                // Enter dragging
                canvasLeft = Canvas.GetLeft(this);
                mouseXInitial = Mouse.GetPosition(parent).X;
                parent.MouseMove += Parent_MouseMove;
                parent.MouseLeftButtonUp += Parent_MouseLeftButtonUp;
                ToolTip = "";
            }
        }

        // Dragging handler
        private void Parent_MouseMove(object sender, MouseEventArgs e)
        {
            double diff = mouseXInitial - Mouse.GetPosition(parent).X;
            Canvas.SetLeft(this, canvasLeft - diff);
            if (Canvas.GetLeft(this) > parent.pixelDistance - 2)
                Canvas.SetLeft(this, parent.pixelDistance - 2);
            if (Canvas.GetLeft(this) < -2)
                Canvas.SetLeft(this, -2);
        }
        private void Parent_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Respond visually
            rectOuter.Opacity = 0.3;

            // Reset to default 'stance'
            primed = false;
            parent.MouseMove -= Parent_MouseMove;
            parent.MouseLeftButtonUp -= Parent_MouseLeftButtonUp;
            parent.RefreshElement(this); // Notify parent to give this element new data about its position in the timeline in seconds
        }
    }
}
