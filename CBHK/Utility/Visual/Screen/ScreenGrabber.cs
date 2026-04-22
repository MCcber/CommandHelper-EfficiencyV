using System.Drawing;
using System.Runtime.InteropServices;

namespace CBHK.Utility.Visual.Screen
{
    public static class ScreenGrabber
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public int X;
            public int Y;
        };

        // 获取鼠标当前位置的物理坐标（即便是多屏高DPI也能准确定位）
        public static Point GetMousePosition()
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }

        // 获取指定位置的颜色
        public static System.Windows.Media.Color GetColorAt(Point location)
        {
            // 创建一个 1x1 像素的 Bitmap
            using Bitmap screenPixel = new(1, 1, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(screenPixel))
            {
                // 从屏幕拷贝该点
                g.CopyFromScreen(location.X, location.Y, 0, 0, new Size(1, 1), CopyPixelOperation.SourceCopy);
            }

            // 读取像素颜色
            var pixel = screenPixel.GetPixel(0, 0);
            return System.Windows.Media.Color.FromArgb(pixel.A, pixel.R, pixel.G, pixel.B);
        }
    }
}
