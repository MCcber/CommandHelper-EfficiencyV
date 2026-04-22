using CBHK.Model.Common;
using System;
using System.Windows.Media;

namespace CBHK.Utility.Visual
{
    public static class ColorTool
    {
        public static Color ModifyColorBrightness(Color color, float factor, ColorModifyMode mode)
        {
            switch (mode)
            {
                default:
                case ColorModifyMode.Lighten:
                    {
                        return Lighten(color, factor);
                    }
                case ColorModifyMode.Darken:
                    {
                        return Darken(color, factor);
                    }
            }
        }

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
            l *= 1.0 - factor;

            // 3. 限制范围
            l = Math.Min(1.0, Math.Max(0.0, l));

            // 4. 转回 RGB
            return HslToRgb(h, s, l, color.A);
        }

        /// <summary>
        /// 淡化颜色（降低饱和度）
        /// </summary>
        /// <param name="color">原始颜色</param>
        /// <param name="factor">程度 (0.0 到 1.0)，1.0 会完全变成灰色</param>
        /// <returns>淡化后的颜色</returns>
        public static Color Desaturate(Color color, float factor)
        {
            // 转换为 HSL
            RgbToHsl(color, out double h, out double s, out double l);

            // 降低饱和度：factor 为 1 时，s 变为 0（完全灰色）
            s = s * (1.0 - factor);
            s = Math.Min(1.0, Math.Max(0.0, s));

            // 转回 RGB
            return HslToRgb(h, s, l, color.A);
        }

        // 基于同色系生成水印色（非纯灰）
        public static SolidColorBrush GetWatermarkBrush(SolidColorBrush backgroundBrush)
        {
            var color = backgroundBrush.Color;

            // 1. 转为 HSL 计算亮度（Lightness）
            RgbToHsl(color, out double h, out double s, out double l);

            // 2. 依然依靠亮度判断是变深还是变浅
            // 背景亮(L > 0.5) -> 水印变暗(L * 0.4)
            // 背景暗(L <= 0.5) -> 水印变亮(L + (1-L) * 0.4)
            double targetL = l > 0.5 ? l * 0.4 : l + (1 - l) * 0.4;

            // 3. 保持色相(H)和饱和度(S)，只改亮度(L)
            Color result = HslToRgb(h, s, targetL, color.A);

            return new SolidColorBrush(result);
        }

        public static bool IsDarkColor(string hexColor)
        {
            // 1. 将 Hex 转换为 WPF Color 对象
            var color = (Color)ColorConverter.ConvertFromString(hexColor);

            // 2. 计算亮度 (YIQ 算法)
            // 权重：Red 29.9%, Green 58.7%, Blue 11.4%
            double brightness = color.R * 0.299 + color.G * 0.587 + color.B * 0.114;

            // 3. 返回结果：亮度小于 128 则视为深色
            return brightness < 128;
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

        private static double GetLuminance(Color color)
        {
            double[] c = [color.R / 255.0, color.G / 255.0, color.B / 255.0];
            for (int i = 0; i < 3; i++)
            {
                c[i] = c[i] <= 0.03928 ? c[i] / 12.92 : Math.Pow((c[i] + 0.055) / 1.055, 2.4);
            }
            return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2];
        }

        private static double GetContrastRatio(Color color1, Color color2)
        {
            double lum1 = GetLuminance(color1);
            double lum2 = GetLuminance(color2);
            double lighter = Math.Max(lum1, lum2);
            double darker = Math.Min(lum1, lum2);
            return (lighter + 0.05) / (darker + 0.05);
        }

        /// <summary>
        /// 将前景色与背景色混合（假设背景为白色）
        /// </summary>
        private static Color BlendWithWhite(Color color)
        {
            if (color.A == 255) return color;

            double alpha = color.A / 255.0;
            // 混合公式：混合后 = 前景 * Alpha + 背景 * (1 - Alpha)
            byte r = (byte)(color.R * alpha + 255 * (1 - alpha));
            byte g = (byte)(color.G * alpha + 255 * (1 - alpha));
            byte b = (byte)(color.B * alpha + 255 * (1 - alpha));
            return Color.FromRgb(r, g, b);
        }

        public static Color GetOptimalForeground(Color backgroundColor)
        {
            // 关键修复：将半透明颜色混合到白色背景上
            Color solidColor = BlendWithWhite(backgroundColor);

            double contrastWithWhite = GetContrastRatio(solidColor, Colors.White);
            double contrastWithBlack = GetContrastRatio(solidColor, Colors.Black);

            return contrastWithWhite > contrastWithBlack ? Colors.White : Colors.Black;
        }
    }
}