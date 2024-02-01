using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace cbhk.GeneralTools
{
    public static class ColorManagement
    {
        private static List<Color> PresetColorList = [Colors.Black, Colors.DarkBlue, Colors.DarkGreen, Colors.Cyan, Colors.DarkRed, Colors.Purple, Colors.Gold, Colors.LightGray, Colors.DarkGray, Colors.Blue, Colors.LightGreen, Colors.LightBlue, Colors.Red, Colors.Pink, Colors.Yellow, Colors.White];
        public static Color ToNearestPredefinedColor(this Color color)
        {
            double minDistance = double.MaxValue;
            Color nearestColor = default;

            foreach (var item in PresetColorList)
            {
                var distance = GetDistance(color, item);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestColor = item;
                }
            }

            return nearestColor;
        }

        private static double GetDistance(Color color1, Color color2)
        {
            int rDiff = color1.R - color2.R;
            int gDiff = color1.G - color2.G;
            int bDiff = color1.B - color2.B;
            int aDiff = color1.A - color2.A;

            return Math.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff + aDiff * aDiff);
        }
    }
}