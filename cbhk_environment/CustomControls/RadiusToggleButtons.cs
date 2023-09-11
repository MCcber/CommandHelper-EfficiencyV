using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace cbhk_environment.CustomControls
{
    public class RadiusToggleButtons:ToggleButton
    {
        public string SelectedToggleText
        {
            get { return (string)GetValue(SelectedToggleTextProperty); }
            set { SetValue(SelectedToggleTextProperty, value); }
        }

        public static readonly DependencyProperty SelectedToggleTextProperty =
            DependencyProperty.Register("SelectedToggleText", typeof(string), typeof(RadiusToggleButtons), new PropertyMetadata(default(string)));

        public string UnSelectedToggleText
        {
            get { return (string)GetValue(UnSelectedToggleTextProperty); }
            set { SetValue(UnSelectedToggleTextProperty, value); }
        }

        public static readonly DependencyProperty UnSelectedToggleTextProperty =
            DependencyProperty.Register("UnSelectedToggleText", typeof(string), typeof(RadiusToggleButtons), new PropertyMetadata(default(string)));

        public ImageBrush ToggleBackground
        {
            get { return (ImageBrush)GetValue(ToggleBackgroundProperty); }
            set { SetValue(ToggleBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ToggleBackgroundProperty =
            DependencyProperty.Register("ToggleBackground", typeof(ImageBrush), typeof(RadiusToggleButtons), new PropertyMetadata(default(ImageBrush)));

        public double ToggleWidth
        {
            get { return (double)GetValue(ToggleWidthProperty); }
            set { SetValue(ToggleWidthProperty, value); }
        }

        public static readonly DependencyProperty ToggleWidthProperty =
            DependencyProperty.Register("ToggleWidth", typeof(double), typeof(RadiusToggleButtons), new PropertyMetadata(default(double)));

        public double ToggleHeight
        {
            get { return (double)GetValue(ToggleHeightProperty); }
            set { SetValue(ToggleHeightProperty, value); }
        }

        public static readonly DependencyProperty ToggleHeightProperty =
            DependencyProperty.Register("ToggleHeight", typeof(double), typeof(RadiusToggleButtons), new PropertyMetadata(default(double)));
    }
}
