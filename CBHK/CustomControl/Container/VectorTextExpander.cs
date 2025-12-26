using System.Windows;
using System.Windows.Controls;

namespace CBHK.CustomControl.Container
{
    public class VectorTextExpander:Expander
    {
        #region Property
        public double RotationAngle
        {
            get { return (double)GetValue(RotationAngleProperty); }
            set { SetValue(RotationAngleProperty, value); }
        }

        public static readonly DependencyProperty RotationAngleProperty =
            DependencyProperty.Register("RotationAngle", typeof(double), typeof(VectorTextExpander), new PropertyMetadata(default(double)));
        #endregion

        #region Method
        public VectorTextExpander()
        {
            Loaded += VectorTextExpander_Loaded;
            Expanded += VectorTextExpander_Expanded;
            Collapsed += VectorTextExpander_Collapsed;
        }

        #endregion

        #region Event
        private void VectorTextExpander_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
        }

        private void VectorTextExpander_Expanded(object sender, System.Windows.RoutedEventArgs e) => RotationAngle = 180;

        private void VectorTextExpander_Collapsed(object sender, RoutedEventArgs e) => RotationAngle = 0;
        #endregion

    }
}
