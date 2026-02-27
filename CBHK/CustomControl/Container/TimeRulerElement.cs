using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    public class TimeRulerElement : FrameworkElement
    {
        #region Field
        private readonly Pen _mainTickPen;
        private readonly Pen _subTickPen;
        private readonly Typeface _typeface;
        /// <summary>
        /// 定义缩放等级结构
        /// </summary>
        private struct RulerLevel
        {
            public double LargeStepSeconds; // 大刻度间隔 (秒)
            public double SmallStepSeconds; // 小刻度间隔 (秒)
            public string Format;           // 显示格式描述
        }
        #endregion

        #region Property

        // 基础每秒像素 (ZoomFactor = 1.0 时)
        public double BasePixelsPerSecond { get; set; } = 100;

        // 缩放倍率
        public double ZoomFactor
        {
            get { return (double)GetValue(ZoomFactorProperty); }
            set { SetValue(ZoomFactorProperty, value); }
        }
        public static readonly DependencyProperty ZoomFactorProperty =
            DependencyProperty.Register("ZoomFactor", typeof(double), typeof(TimeRulerElement),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

        // 总时长 (秒)
        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(TimeRulerElement),
                new FrameworkPropertyMetadata(60.0, FrameworkPropertyMetadataOptions.AffectsRender));

        // 帧率 (Minecraft JE 固定为 20)
        public int FPS { get; set; } = 20;

        #endregion

        #region Method
        public TimeRulerElement()
        {
            // 初始化画笔，冻结以提升性能
            _mainTickPen = new Pen(new SolidColorBrush(Color.FromRgb(120, 120, 120)), 1);
            _mainTickPen.Freeze();

            _subTickPen = new Pen(new SolidColorBrush(Color.FromRgb(80, 80, 80)), 1);
            _subTickPen.Freeze();

            _typeface = new Typeface("Segoe UI");
        }

        // 定义你要求的缩放层级表
        private static List<RulerLevel> GetRulerLevels()
        {
            return
            [
                // 1. [最小缩放] 10秒一大格，1秒一小格 (10等分，9个小刻度)
                new RulerLevel { LargeStepSeconds = 10.0, SmallStepSeconds = 1.0, Format = "10s" },

                // 2. 5秒一大格，0.5秒(10t)一小格 (10等分)
                new RulerLevel { LargeStepSeconds = 5.0, SmallStepSeconds = 0.5, Format = "5s" },

                // 3. 2秒一大格，0.2秒(4t)一小格 (10等分)
                new RulerLevel { LargeStepSeconds = 2.0, SmallStepSeconds = 0.2, Format = "2s" },

                // 4. 1秒一大格，0.1秒(2t)一小格 (10等分)
                new RulerLevel { LargeStepSeconds = 1.0, SmallStepSeconds = 0.1, Format = "1s" },

                // 5. 10帧(0.5s)一大格，1帧(0.05s)一小格 (10等分，精确到1t)
                new RulerLevel { LargeStepSeconds = 0.5, SmallStepSeconds = 0.05, Format = "10t" },

                // 6. 5帧(0.25s)一大格，1帧(0.05s)一小格 (5等分) -> *无法10等分，物理极限*
                new RulerLevel { LargeStepSeconds = 0.25, SmallStepSeconds = 0.05, Format = "5t" },

                // 7. 2帧(0.1s)一大格，1帧(0.05s)一小格 (2等分)
                new RulerLevel { LargeStepSeconds = 0.1, SmallStepSeconds = 0.05, Format = "2t" },

                // 8. [最大缩放] 1帧(0.05s)一大格，1帧(0.05s)一小格 (1等分)
                new RulerLevel { LargeStepSeconds = 0.05, SmallStepSeconds = 0.05, Format = "1t" }
            ];
        }

        /// <summary>
        /// 格式化文本：整数秒显示 "1s", "2s"；非整数显示 "5t", "10t" (tick)
        /// </summary>
        /// <param name="timeInSeconds"></param>
        /// <returns></returns>
        private string FormatTimeText(double timeInSeconds)
        {
            // 检查是否是整秒
            if (Math.Abs(timeInSeconds % 1.0) < 0.001)
            {
                return $"{(int)timeInSeconds}s";
            }
            else
            {
                // 转换为 Tick 显示
                int ticks = (int)Math.Round(timeInSeconds * FPS);
                // 这里可以选择显示总Ticks，或者显示 "0s + 10t" 这种格式
                // MC习惯：通常如果小于1秒，直接显示数字+t。如果大于1秒，显示 1s:10t 比较拥挤
                // 建议：直接显示当前秒内的 Tick 数，或者总 Tick 数

                // 方案A: 显示总Tick数 (适合短动画) -> "25t"
                // return $"{ticks}t"; 

                // 方案B: 显示相对Tick数 (0~19) -> "10t"
                int relativeTick = ticks % FPS;
                return $"{relativeTick}t";
            }
        }
        #endregion

        #region Event
        /// <summary>
        /// 绘制刻度尺
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // 1. 计算当前每秒多少像素
            double pixelsPerSecond = BasePixelsPerSecond * ZoomFactor;
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            // 2. 智能选择缩放等级
            // 策略：我们希望大刻度之间的间距至少有 60 像素宽，这样文字才写得下
            var levels = GetRulerLevels();
            RulerLevel currentLevel = levels[0]; // 默认最小缩放

            foreach (var level in levels)
            {
                double pixelWidthOfLargeStep = level.LargeStepSeconds * pixelsPerSecond;
                if (pixelWidthOfLargeStep >= 80) // 阈值：80px
                {
                    currentLevel = level;
                }
                else
                {
                    // 因为列表是从大到小排的，一旦不满足就不需要看后面更细的了
                    // 但这里我们希望一直往更精细的匹配，直到匹配到最精细的
                    // 所以这里的逻辑应该是：只要当前这个能显示下，就选这个，然后继续看下一个能不能显示得更好
                    // 修正逻辑：我们遍历列表，找到第一个“看起来太挤”的前一个，或者直接找“刚好合适”的
                }
            }

            // 重新写匹配逻辑：从最精细(Scale最大)往回找，找到第一个能展示下的
            for (int i = levels.Count - 1; i >= 0; i--)
            {
                double pixelWidthOfLargeStep = levels[i].LargeStepSeconds * pixelsPerSecond;
                // 如果大刻度间距 > 80px，说明这一层级适合展示
                if (pixelWidthOfLargeStep > 80)
                {
                    currentLevel = levels[i];
                    break;
                }
                // 如果循环到底都没匹配上，就用默认的 levels[0]
            }


            // 3. 计算绘制范围 (可视区域优化)
            // 获取 ScrollViewer 的可视范围（如果可能），这里简单全量绘制
            // 实际为了性能，应该只绘制 Viewport 范围
            double totalSeconds = Maximum;

            // 4. 开始绘制循环
            // 使用 SmallStepSeconds 作为增量
            // 为了避免浮点数累积误差，建议用整数索引
            int totalSmallSteps = (int)Math.Ceiling(totalSeconds / currentLevel.SmallStepSeconds);

            // 定义常量公差
            double epsilon = 0.0001;
            double currentTime = 0;
            double xPos = 0;
            double renderX = 0; // 像素对齐
            for (int i = 0; i <= totalSmallSteps; i++)
            {
                currentTime = i * currentLevel.SmallStepSeconds;
                xPos = currentTime * pixelsPerSecond;
                renderX = xPos + 0.5;
                // 判断是大刻度还是小刻度
                // 检查 currentTime 是否是大刻度的整数倍
                double remainder = currentTime % currentLevel.LargeStepSeconds;

                // 处理浮点数取模的精度问题：如果余数非常接近0 或者 非常接近LargeStep，都算整除
                bool isLargeTick = Math.Abs(remainder) < epsilon || Math.Abs(remainder - currentLevel.LargeStepSeconds) < epsilon;

                if (isLargeTick)
                {
                    // --- 绘制大刻度 ---
                    drawingContext.DrawLine(_mainTickPen, new Point(renderX, 0), new Point(renderX, 15));

                    // 准备文字
                    string text = FormatTimeText(currentTime);
                    FormattedText ft = new(
                        text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        _typeface, 10, Brushes.Gray, pixelsPerDip);

                    drawingContext.DrawText(ft, new Point(renderX + 2, 16));
                }
                else
                {
                    // --- 绘制小刻度 ---
                    // 只有当小刻度之间距离大于 4 像素时才画，否则糊成一团
                    if (currentLevel.SmallStepSeconds * pixelsPerSecond > 4)
                    {
                        drawingContext.DrawLine(_subTickPen, new Point(renderX, 0), new Point(renderX, 6));
                    }
                }
            }
        }

        /// <summary>
        /// 测量大小，确保 ScrollViewer 能滚动
        /// </summary>
        /// <param name="availableSize"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            double totalWidth = Maximum * BasePixelsPerSecond * ZoomFactor;
            // 加上 50px 的右侧余量，防止最后一秒的文字被切掉
            return new Size(totalWidth + 50, 35);
        } 
        #endregion
    }
}