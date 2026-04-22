using System;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace CBHK.Utility.Common
{
    public static class NativePicker
    {
        #region Field
        // 1. 获取鼠标位置
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(ref Win32Point pt);

        // 2. 获取设备上下文 (DC) - 这里的 IntPtr.Zero 代表整个屏幕
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        // 3. 释放设备上下文 (一定要释放，否则会内存泄漏)
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        // 4. 获取指定坐标的颜色
        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [StructLayout(LayoutKind.Sequential)]
        private struct Win32Point
        {
            public int X;
            public int Y;
        };
        #endregion

        #region Method
        /// <summary>
        /// 获取当前鼠标位置下的屏幕颜色
        /// </summary>
        public static Color GetColorAtMousePosition()
        {
            // 1. 拿到鼠标坐标
            Win32Point p = new();
            GetCursorPos(ref p);

            // 2. 拿到整个屏幕的句柄
            IntPtr hdc = GetDC(IntPtr.Zero);

            // 3. 读取像素 (返回的是 COLORREF: 0x00BBGGRR)
            uint pixel = GetPixel(hdc, p.X, p.Y);

            // 4. 用完立刻释放!
            _ = ReleaseDC(IntPtr.Zero, hdc);

            // 5. 解析颜色 (Win32 返回的是 BGR 顺序，不是 RGB)
            byte r = (byte)(pixel & 0x000000FF);
            byte g = (byte)((pixel & 0x0000FF00) >> 8);
            byte b = (byte)((pixel & 0x00FF0000) >> 16);

            return Color.FromRgb(r, g, b);
        } 
        #endregion
    }
}