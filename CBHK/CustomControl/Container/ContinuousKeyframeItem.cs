using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    public class ContinuousKeyframeItem:ToggleButton
    {
        #region Property
        public bool IsBorderKeyFrame { get; set; }

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

        public Brush OriginBackground
        {
            get { return (Brush)GetValue(OriginBackgroundProperty); }
            set { SetValue(OriginBackgroundProperty, value); }
        }

        public static readonly DependencyProperty OriginBackgroundProperty =
            DependencyProperty.Register("OriginBackground", typeof(Brush), typeof(ContinuousKeyframeItem), new PropertyMetadata(default(Brush)));

        public Brush SelectedBackground
        {
            get { return (Brush)GetValue(SelectedBackgroundProperty); }
            set { SetValue(SelectedBackgroundProperty, value); }
        }

        public static readonly DependencyProperty SelectedBackgroundProperty =
            DependencyProperty.Register("SelectedBackground", typeof(Brush), typeof(ContinuousKeyframeItem), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public ContinuousKeyframeItem()
        {
            var originBackgroundBrushSource = DependencyPropertyHelper.GetValueSource(this, BackgroundProperty);
            if (originBackgroundBrushSource.BaseValueSource is BaseValueSource.Default || originBackgroundBrushSource.BaseValueSource is BaseValueSource.Style || Background is null)
            {
                Background = OriginBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B8BEBA"));
            }

            var originSelectedBackgroundBrushSource = DependencyPropertyHelper.GetValueSource(this, SelectedBackgroundProperty);
            if (originSelectedBackgroundBrushSource.BaseValueSource is BaseValueSource.Default || originSelectedBackgroundBrushSource.BaseValueSource is BaseValueSource.Style || SelectedBackground is null)
            {
                SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DD7929"));
            }
        } 
        #endregion
    }
}
