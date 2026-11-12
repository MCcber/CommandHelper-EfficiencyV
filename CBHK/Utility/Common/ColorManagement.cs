using System;
using System.Windows.Media;

namespace CBHK.Utility.Common
{
    public static class ColorManagement
    {
        /// <summary>
        /// 16 个预设色，顺序即 §0–§f（0=黑 … 7=浅灰 … f=白），与旧版拾色器的色码表一致。
        /// Name 直接取 WPF 系统色标识符，避免手写字符串拼错。
        /// </summary>
        private static readonly (string Name, Color Value)[] PresetColors =
        [
            (nameof(Colors.Black), Colors.Black),
            (nameof(Colors.DarkBlue), Colors.DarkBlue),
            (nameof(Colors.DarkGreen), Colors.DarkGreen),
            (nameof(Colors.Cyan), Colors.Cyan),
            (nameof(Colors.DarkRed), Colors.DarkRed),
            (nameof(Colors.Purple), Colors.Purple),
            (nameof(Colors.Gold), Colors.Gold),
            (nameof(Colors.LightGray), Colors.LightGray),
            (nameof(Colors.DarkGray), Colors.DarkGray),
            (nameof(Colors.Blue), Colors.Blue),
            (nameof(Colors.LightGreen), Colors.LightGreen),
            (nameof(Colors.LightBlue), Colors.LightBlue),
            (nameof(Colors.Red), Colors.Red),
            (nameof(Colors.Pink), Colors.Pink),
            (nameof(Colors.Yellow), Colors.Yellow),
            (nameof(Colors.White), Colors.White),
        ];

        public static Color ToNearestPredefinedColor(this Color color) => GetNearestPreset(color).Value;

        /// <summary>
        /// 取该颜色最接近的预设色名称（系统色名，如 "DarkBlue"、"Cyan"）。
        /// 与 <see cref="ToNearestPredefinedColor"/> 同一套距离口径，预设色本身返回其自身名称。
        /// </summary>
        public static string GetName(this Color color) => GetNearestPreset(color).Name;

        private static (string Name, Color Value) GetNearestPreset(Color color)
        {
            double minDistance = double.MaxValue;
            (string Name, Color Value) nearest = PresetColors[0];

            foreach (var item in PresetColors)
            {
                var distance = GetDistance(color, item.Value);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = item;
                }
            }

            return nearest;
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
