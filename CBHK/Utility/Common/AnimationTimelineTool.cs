using CBHK.CustomControl.Container;
using System;

namespace CBHK.Utility.Common
{
    public class AnimationTimelineTool
    {
        /// <summary>
        /// 将时间点转为画布横坐标
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public double ConvertTimeToPixel(TimeSpan time,TimeRulerElement Ruler)
        {
            if (Ruler == null)
            {
                return 0;
            }
            // 公式：秒数 * 基础每秒像素 * 缩放倍率
            return time.TotalSeconds * Ruler.BasePixelsPerSecond * Ruler.ZoomFactor;
        }

        /// <summary>
        /// 将鼠标横坐标转换为时间点
        /// </summary>
        /// <param name="mousePoint"></param>
        /// <returns></returns>
        public TimeSpan ConvertPixelToTime(double x, TimeRulerElement Ruler)
        {
            if (Ruler == null)
            {
                return TimeSpan.Zero;
            }

            // 1. 计算原始秒数
            double totalSeconds = x / (Ruler.BasePixelsPerSecond * Ruler.ZoomFactor);

            // 2. 限制在有效时长内
            totalSeconds = Math.Clamp(totalSeconds, 0, Ruler.Maximum);

            // 3. 磁吸：四舍五入到最近的 0.05s (即 1/20 秒，1个 Minecraft Tick)
            double tickCount = Math.Round(totalSeconds * 20);
            return TimeSpan.FromSeconds(tickCount / 20.0);
        }
    }
}