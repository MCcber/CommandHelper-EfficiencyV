using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Animation;

namespace CBHK.Utility.Visual
{
    public static class SkeletonAnimationFactory
    {
        /// <summary>
        /// 为矩形创建呼吸动画（明暗交替）。
        /// </summary>
        /// <param name="rectangle">目标矩形</param>
        /// <param name="lightColor">浅色（起始/结束色）</param>
        /// <param name="darkColor">深色（中间过渡色）</param>
        /// <param name="duration">动画周期时长</param>
        public static void ApplyBreathAnimation(this Rectangle rectangle, Color lightColor, Color darkColor, Duration? duration = null)
        {
            if (rectangle.Fill is not SolidColorBrush brush || brush.IsFrozen)
            {
                brush = new SolidColorBrush(lightColor);
                rectangle.Fill = brush;
            }

            ColorAnimation animation = new()
            {
                From = lightColor,
                To = darkColor,
                Duration = duration ?? TimeSpan.FromSeconds(1.2),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            Storyboard storyBoard = new();
            Storyboard.SetTarget(animation, rectangle);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(Shape.Fill).(SolidColorBrush.Color)"));
            storyBoard.Children.Add(animation);

            rectangle.IsVisibleChanged += (s,e)=> 
            {
                if(e.NewValue is Visibility visibility && visibility is Visibility.Collapsed)
                {
                    storyBoard.Stop();
                }
            };

            storyBoard.Begin(rectangle, true);
        }

        /// <summary>
        /// 为矩形创建扫屏动画（光影移动）。
        /// </summary>
        /// <param name="rectangle">目标矩形</param>
        /// <param name="lightColor">背景浅色</param>
        /// <param name="darkColor">移动高光深色</param>
        /// <param name="duration">动画周期时长</param>
        public static void ApplySweepAnimation(this Rectangle rectangle, Color lightColor, Color darkColor, Duration? duration = null)
        {
            LinearGradientBrush linearGradientBrush = new()
            {
                StartPoint = new Point(0, 0.5),
                EndPoint = new Point(1, 0.5)
            };
            linearGradientBrush.GradientStops.Add(new GradientStop(lightColor, 0));
            linearGradientBrush.GradientStops.Add(new GradientStop(darkColor, 0));   // 初始位于 0，动画移动它
            linearGradientBrush.GradientStops.Add(new GradientStop(lightColor, 1));
            rectangle.Fill = linearGradientBrush;

            rectangle.ClipToBounds = true;
            rectangle.Margin = new Thickness(10, 0, 0, 0);
            rectangle.HorizontalAlignment = HorizontalAlignment.Stretch;
            rectangle.VerticalAlignment = VerticalAlignment.Stretch;

            // 3. 创建偏移量动画（目标为 GradientStops[1]）
            DoubleAnimation doubleAnimation = new()
            {
                To = 1,
                Duration = duration ?? TimeSpan.FromSeconds(1),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            Storyboard storyBoard = new();
            Storyboard.SetTarget(doubleAnimation, rectangle);
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("(Shape.Fill).(LinearGradientBrush.GradientStops)[1].(GradientStop.Offset)"));
            storyBoard.Children.Add(doubleAnimation);

            rectangle.IsVisibleChanged += (s, e) =>
            {
                if (e.NewValue is Visibility visibility && visibility is Visibility.Collapsed)
                {
                    storyBoard.Stop();
                }
            };

            storyBoard.Begin(rectangle, true);
        }
    }
}
