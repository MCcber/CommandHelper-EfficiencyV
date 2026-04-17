using System;
using System.Windows.Media;

namespace CBHK.Utility.Visual
{
    public struct HsvColor(double h, double s, double v)
    {
        public double H = h; // 0-360
        public double S = s; // 0-1
        public double V = v; // 0-1

        public readonly Color ToRgb()
        {
            if (double.IsNaN(H) ||
                double.IsInfinity(H) ||
                double.IsNaN(S) ||
                double.IsInfinity(S) ||
                double.IsNaN(V) ||
                double.IsInfinity(V))
            {
                return Colors.Black; // 或者你想要的默认安全色
            }

            int hi = Convert.ToInt32(Math.Floor(H / 60)) % 6;
            double f = H / 60 - Math.Floor(H / 60);
            double value = V * 255;
            byte v = Convert.ToByte(value);
            byte p = Convert.ToByte(value * (1 - S));
            byte q = Convert.ToByte(value * (1 - f * S));
            byte t = Convert.ToByte(value * (1 - (1 - f) * S));

            return hi switch
            {
                0 => Color.FromRgb(v, t, p),
                1 => Color.FromRgb(q, v, p),
                2 => Color.FromRgb(p, v, t),
                3 => Color.FromRgb(p, q, v),
                4 => Color.FromRgb(t, p, v),
                _ => Color.FromRgb(v, p, q),
            };
        }

        public static HsvColor FromRgb(Color color)
        {
            double r = color.R / 255d;
            double g = color.G / 255d;
            double b = color.B / 255d;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double h = 0;
            if (delta != 0)
            {
                if (max == r) h = 60 * (((g - b) / delta) % 6);
                else if (max == g) h = 60 * (((b - r) / delta) + 2);
                else h = 60 * (((r - g) / delta) + 4);
            }
            if (h < 0) h += 360;

            double s = max == 0 ? 0 : delta / max;
            return new HsvColor(h, s, max);
        }
    }
}
