using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Media3D;

namespace CBHK.GeneralTools
{
    public class PositionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (double.TryParse(values[0].ToString(), out double x) && double.TryParse(values[1].ToString(), out double y) && double.TryParse(values[2].ToString(), out double z))
                return new Vector3D(x, y, z);
            else
                return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            Vector3D vector = (Vector3D)value;
            return [float.Parse(vector.X.ToString()), float.Parse(vector.Y.ToString()), float.Parse(vector.Z.ToString())];
        }
    }
}
