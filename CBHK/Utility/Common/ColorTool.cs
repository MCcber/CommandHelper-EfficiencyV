using System;
using System.Windows.Media;

namespace CBHK.Utility.Common
{
    public static class ColorTool
    {
        /// <summary>
        /// 调亮颜色 (支持纯黑色变亮)
        /// </summary>
        /// <param name="color">原始颜色</param>
        /// <param name="factor">程度 (0.0 到 1.0)</param>
        /// <returns>调亮后的颜色</returns>
        public static Color Lighten(Color color, float factor)
        {
            // 1. 将 RGB 转换为 HSL
            RgbToHsl(color, out double h, out double s, out double l);

            // 2. 提高亮度 (算法改进)
            // 旧算法: l = l * (1 + factor); // 纯黑无法变亮
            // 新算法: 向 1.0 (白色) 靠近，即使是 0 也能增加
            l += (1.0 - l) * factor;

            // 3. 限制范围
            l = Math.Min(1.0, Math.Max(0.0, l));

            // 4. 转回 RGB
            return HslToRgb(h, s, l, color.A);
        }

        /// <summary>
        /// 调暗颜色
        /// </summary>
        /// <param name="color">原始颜色</param>
        /// <param name="factor">程度 (0.0 到 1.0)</param>
        /// <returns>调暗后的颜色</returns>
        public static Color Darken(Color color, float factor)
        {
            // 1. 将 RGB 转换为 HSL
            RgbToHsl(color, out double h, out double s, out double l);

            // 2. 降低亮度
            // 对于变暗，乘法是没问题的 (向 0 靠近)
            l *= (1.0 - factor);

            // 3. 限制范围
            l = Math.Min(1.0, Math.Max(0.0, l));

            // 4. 转回 RGB
            return HslToRgb(h, s, l, color.A);
        }

        private static void RgbToHsl(Color color, out double h, out double s, out double l)
        {
            // 归一化 RGB 到 0-1
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            // 计算亮度 L
            l = (max + min) / 2.0;

            if (Math.Abs(delta) < 0.00001) // 灰色系，无饱和度
            {
                h = 0;
                s = 0;
            }
            else
            {
                // 计算饱和度 S
                s = l < 0.5 ? delta / (max + min) : delta / (2.0 - max - min);

                // 计算色相 H
                if (Math.Abs(r - max) < 0.00001)
                {
                    h = (g - b) / delta;
                }
                else if (Math.Abs(g - max) < 0.00001)
                {
                    h = 2.0 + (b - r) / delta;
                }
                else
                {
                    h = 4.0 + (r - g) / delta;
                }

                h *= 60.0;
                if (h < 0) h += 360.0;
            }

            // 为了保持和 HslToRgb 的兼容，通常把 H 转为 0-1
            h /= 360.0;
        }

        private static Color HslToRgb(double h, double s, double l, byte alpha)
        {
            double r, g, b;

            if (s == 0)
            {
                r = g = b = l; // 灰色
            }
            else
            {
                double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                double p = 2 * l - q;

                r = HueToRgb(p, q, h + 1.0 / 3.0);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 1.0 / 3.0);
            }

            return Color.FromArgb(
                alpha,
                (byte)Math.Round(r * 255),
                (byte)Math.Round(g * 255),
                (byte)Math.Round(b * 255));
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
            if (t < 1.0 / 2.0) return q;
            if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
            return p;
        }
    }
}