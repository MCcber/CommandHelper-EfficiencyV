using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace CBHK.CustomControl.Container
{
    public class ContinuousKeyframeItem:ToggleButton
    {
        public TimeSpan CurrentTime
        {
            get { return (TimeSpan)GetValue(CurrentTimeProperty); }
            set { SetValue(CurrentTimeProperty, value); }
        }

        public static readonly DependencyProperty CurrentTimeProperty =
            DependencyProperty.Register("CurrentTime", typeof(TimeSpan), typeof(ContinuousKeyframeItem), new PropertyMetadata(default(TimeSpan)));

        public double X
        {
            get { return (double)GetValue(XProperty); }
            set { SetValue(XProperty, value); }
        }

        public static readonly DependencyProperty XProperty =
            DependencyProperty.Register("X", typeof(double), typeof(ContinuousKeyframeItem), new PropertyMetadata(default(double)));
    }
}
