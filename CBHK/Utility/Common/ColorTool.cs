using System;
using System.Windows.Media;

namespace CBHK.Utility.Common
{
    public class ColorTool
    {
        public static Color LightenByHSL(Color originalColor, float lightenFactor)
        {
            // 转换为System.Drawing.Color以便使用GetHue/GetBrightness
            System.Drawing.Color sysColor = System.Drawing.Color.FromArgb(
                originalColor.A, originalColor.R, originalColor.G, originalColor.B
            );

            // 获取HSL中的H和S
            float hue = sysColor.GetHue() / 360.0f; // 转换为0-1范围
            float saturation = sysColor.GetSaturation();
            float lightness = sysColor.GetBrightness(); // 这就是L

            // 提高明度
            lightness = Math.Min(1.0f, lightness * (1 + lightenFactor));

            // HSL转RGB (使用辅助方法)
            return HslToRgb(hue, saturation, lightness, originalColor.A);
        }

        public static Color DarkenByHSL(Color originalColor, float darkenFactor)
        {
            // 转换为System.Drawing.Color以便使用GetHue/GetBrightness
            System.Drawing.Color sysColor = System.Drawing.Color.FromArgb(
                originalColor.A, originalColor.R, originalColor.G, originalColor.B
            );

            // 获取HSL中的H、S、L
            float hue = sysColor.GetHue() / 360.0f; // 转换为0-1范围
            float saturation = sysColor.GetSaturation();
            float lightness = sysColor.GetBrightness(); // 这就是L

            // 降低明度（保持darkenFactor在0-1范围）
            lightness = Math.Max(0.0f, lightness * (1 - darkenFactor));

            // HSL转RGB
            return HslToRgb(hue, saturation, lightness, originalColor.A);
        }

        // HSL转RGB辅助方法（完整实现）
        private static Color HslToRgb(float h, float s, float l, byte alpha)
        {
            float r, g, b;
            if (s == 0)
            {
                r = g = b = l; // 灰度
            }
            else
            {
                float q = l < 0.5f ? l * (1 + s) : l + s - l * s;
                float p = 2 * l - q;
                r = HueToRgb(p, q, h + 1f / 3);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 1f / 3);
            }
            return Color.FromArgb(alpha, (byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        private static float HueToRgb(float p, float q, float t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1f / 6) return p + (q - p) * 6 * t;
            if (t < 1f / 2) return q;
            if (t < 2f / 3) return p + (q - p) * (2f / 3 - t) * 6;
            return p;
        }
    }
}
