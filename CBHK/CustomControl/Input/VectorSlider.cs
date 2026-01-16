using System.Windows;
using System.Windows.Controls;

namespace CBHK.CustomControl.Input
{
    public class VectorSlider : Slider
    {
        public double SlidingAreaHeight
        {
            get { return (double)GetValue(SlidingAreaHeightProperty); }
            set { SetValue(SlidingAreaHeightProperty, value); }
        }

        public static readonly DependencyProperty SlidingAreaHeightProperty =
            DependencyProperty.Register("SlidingAreaHeight", typeof(double), typeof(VectorSlider), new PropertyMetadata(default(double)));
    }
}
