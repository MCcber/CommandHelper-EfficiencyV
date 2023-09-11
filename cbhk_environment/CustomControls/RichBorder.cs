using System;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace cbhk_environment.CustomControls
{
    public class RichBorder:Border
    {
        public Brush LeftBorderBrush
        {
            get { return (Brush)GetValue(LeftBorderBrushProperty); }
            set { SetValue(LeftBorderBrushProperty, value); }
        }


        public static readonly DependencyProperty LeftBorderBrushProperty =
            DependencyProperty.Register("LeftBorderBrush", typeof(Brush), typeof(RichBorder), new PropertyMetadata(null));

        public Brush TopBorderBrush
        {
            get { return (Brush)GetValue(TopBorderBrushProperty); }
            set { SetValue(TopBorderBrushProperty, value); }
        }


        public static readonly DependencyProperty TopBorderBrushProperty =
            DependencyProperty.Register("TopBorderBrush", typeof(Brush), typeof(RichBorder), new PropertyMetadata(null));

        public Brush RightBorderBrush
        {
            get { return (Brush)GetValue(RightBorderBrushProperty); }
            set { SetValue(RightBorderBrushProperty, value); }
        }


        public static readonly DependencyProperty RightBorderBrushProperty =
            DependencyProperty.Register("RightBorderBrush", typeof(Brush), typeof(RichBorder), new PropertyMetadata(null));

        public Brush BottomBorderBrush
        {
            get { return (Brush)GetValue(BottomBorderBrushProperty); }
            set { SetValue(BottomBorderBrushProperty, value); }
        }


        public static readonly DependencyProperty BottomBorderBrushProperty =
            DependencyProperty.Register("BottomBorderBrush", typeof(Brush), typeof(RichBorder), new PropertyMetadata(null));

        public RichBorder()
        {
            RenderOptions.SetCachingHint(this,CachingHint.Cache);
            RenderOptions.SetClearTypeHint(this,ClearTypeHint.Auto);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            bool useLayoutRounding = UseLayoutRounding;
            Thickness borderThickness = BorderThickness;
            CornerRadius cornerRadius = CornerRadius;
            double topLeft = cornerRadius.TopLeft;
            bool flag = !DoubleUtil.IsZero(topLeft);
            Brush borderBrush = null;

            Pen pen =  new();
                borderBrush = LeftBorderBrush;
                pen.Brush = LeftBorderBrush;

            if (DoubleUtil.GreaterThan(borderThickness.Top, 0.0))
            {
                pen = new Pen
                {
                    Brush = TopBorderBrush
                };
                if (useLayoutRounding)
                {
                    pen.Thickness = UlementEx.RoundLayoutValue(borderThickness.Top, DoubleUtil.DpiScaleY);
                }
                else
                {
                    pen.Thickness = borderThickness.Top;
                }
                if (borderBrush != null)
                {
                    if (borderBrush.IsFrozen)
                    {
                        pen.Freeze();
                    }
                }
                double num = pen.Thickness * 0.5;
                dc.DrawLine(pen, new Point(0.0 + 3.5, num), new Point(RenderSize.Width - 3.5, num));
            }
            if (DoubleUtil.GreaterThan(borderThickness.Bottom, 0.0))
            {
                pen = new Pen
                {
                    Brush = BottomBorderBrush
                };
                if (useLayoutRounding)
                {
                    pen.Thickness = UlementEx.RoundLayoutValue(borderThickness.Bottom, DoubleUtil.DpiScaleY);
                }
                else
                {
                    pen.Thickness = borderThickness.Bottom;
                }
                if (borderBrush != null)
                {
                    if (borderBrush.IsFrozen)
                    {
                        pen.Freeze();
                    }
                }
                double num = pen.Thickness * 0.5;
                dc.DrawLine(pen, new Point(0.0, RenderSize.Height - num), new Point(RenderSize.Width, RenderSize.Height - num));
            }
            if (DoubleUtil.GreaterThan(borderThickness.Left, 0.0))
            {
                pen = new Pen
                {
                    Brush = LeftBorderBrush
                };
                if (useLayoutRounding)
                {
                    pen.Thickness = UlementEx.RoundLayoutValue(borderThickness.Left, DoubleUtil.DpiScaleX);
                }
                else
                {
                    pen.Thickness = borderThickness.Left;
                }
                if (borderBrush != null)
                {
                    if (borderBrush.IsFrozen)
                    {
                        pen.Freeze();
                    }
                }
                double num = pen.Thickness * 0.5;
                dc.DrawLine(pen, new Point(/*RenderSize.Width - */num, 0.0), new Point(/*RenderSize.Width - */num, RenderSize.Height));
            }
            if (DoubleUtil.GreaterThan(borderThickness.Right, 0.0))
            {
                pen = new Pen
                {
                    Brush = RightBorderBrush
                };
                if (useLayoutRounding)
                {
                    pen.Thickness = UlementEx.RoundLayoutValue(borderThickness.Right, DoubleUtil.DpiScaleX);
                }
                else
                {
                    pen.Thickness = borderThickness.Right;
                }
                if (borderBrush != null)
                {
                    if (borderBrush.IsFrozen)
                    {
                        pen.Freeze();
                    }
                }
                double num = pen.Thickness * 0.5;
                dc.DrawLine(pen, new Point(RenderSize.Width - num, 0.0), new Point(RenderSize.Width - num, RenderSize.Height));
            }
        }

        public class DoubleUtil
        {

            public static double DpiScaleX
            {
                get
                {
                    GetDPI(out int dx, out int dy);
                    if (dx != 96)
                    {
                        return dx / 96.0;
                    }
                    return 1.0;
                }
            }

            public static double DpiScaleY
            {
                get
                {
                    GetDPI(out int dx, out int dy);
                    if (dy != 96)
                    {
                        return dy / 96.0;
                    }
                    return 1.0;
                }
            }

            public static void GetDPI(out int dpix, out int dpiy)
            {
                dpix = 0;
                dpiy = 0;
                using ManagementClass mc = new("Win32_DesktopMonitor");
                using ManagementObjectCollection moc = mc.GetInstances();

                foreach (ManagementObject each in moc.Cast<ManagementObject>())
                {
                    dpix = int.Parse((each.Properties["PixelsPerXLogicalInch"].Value.ToString()));
                    dpiy = int.Parse((each.Properties["PixelsPerYLogicalInch"].Value.ToString()));
                }
            }
            public static bool GreaterThan(double value1, double value2)
            {
                return value1 > value2 && !DoubleUtil.AreClose(value1, value2);
            }

            public static bool AreClose(double value1, double value2)
            {
                if (value1 == value2)
                {
                    return true;
                }
                double num = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * 2.2204460492503131E-16;
                double num2 = value1 - value2;
                return -num < num2 && num > num2;
            }

            public static bool IsZero(double value)
            {
                return Math.Abs(value) < 2.2204460492503131E-15;
            }

            [StructLayout(LayoutKind.Explicit)]
            private struct NanUnion
            {
                [FieldOffset(0)]
                internal double DoubleValue;
                [FieldOffset(0)]
                internal ulong UintValue;
            }

            public static bool IsNaN(double value)
            {
                NanUnion nanUnion = default;
                nanUnion.DoubleValue = value;
                ulong num = nanUnion.UintValue & 18442240474082181120uL;
                ulong num2 = nanUnion.UintValue & 4503599627370495uL;
                return (num == 9218868437227405312uL || num == 18442240474082181120uL) && num2 != 0uL;
            }
        }

        public static class UlementEx
        {
            public static double RoundLayoutValue(double value, double dpiScale)
            {
                double num;
                if (!DoubleUtil.AreClose(dpiScale, 1.0))
                {
                    num = Math.Round(value * dpiScale) / dpiScale;
                    if (DoubleUtil.IsNaN(num) || double.IsInfinity(num) || DoubleUtil.AreClose(num, 1.7976931348623157E+308))
                    {
                        num = value;
                    }
                }
                else
                {
                    num = Math.Round(value);
                }
                return num;
            }
        }
    }
}
