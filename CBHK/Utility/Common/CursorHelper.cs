using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CBHK.Utility.Common
{
    public class CursorHelper
    {
        private struct IconInfo
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr CreateIconIndirect(ref IconInfo icon);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetIconInfo(IntPtr hIcon, ref IconInfo pIconInfo);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        // 自定义 SafeHandle 用于光标句柄（已移除过时的 ReliabilityContract）
        private sealed class SafeCursorHandle : SafeHandle
        {
            public SafeCursorHandle() : base(IntPtr.Zero, true) { }

            public SafeCursorHandle(IntPtr handle) : base(IntPtr.Zero, true)
            {
                SetHandle(handle);
            }

            public override bool IsInvalid => handle == IntPtr.Zero;

            protected override bool ReleaseHandle()
            {
                return DestroyIcon(handle);
            }
        }

        private static Cursor InternalCreateCursor(System.Drawing.Bitmap bmp, int xHotSpot, int yHotSpot)
        {
            IconInfo tmp = new();
            IntPtr hIcon = bmp.GetHicon(); // 该句柄随 Bitmap 释放
            if (!GetIconInfo(hIcon, ref tmp))
                throw new InvalidOperationException("Failed to get icon info.");

            tmp.xHotspot = xHotSpot;
            tmp.yHotspot = yHotSpot;
            tmp.fIcon = false;

            IntPtr cursorPtr = CreateIconIndirect(ref tmp);

            // 必须释放 GetIconInfo 分配的 GDI 位图句柄
            if (tmp.hbmMask != IntPtr.Zero) DeleteObject(tmp.hbmMask);
            if (tmp.hbmColor != IntPtr.Zero) DeleteObject(tmp.hbmColor);

            SafeCursorHandle safeHandle = new(cursorPtr);
            return CursorInteropHelper.Create(safeHandle);
        }

        public static Cursor CreateCursor(UIElement element, int xHotSpot, int yHotSpot)
        {
            element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            element.Arrange(new Rect(0, 0, element.DesiredSize.Width, element.DesiredSize.Height));

            RenderTargetBitmap rtb = new(
                (int)element.DesiredSize.Width,
                (int)element.DesiredSize.Height,
                96, 96, PixelFormats.Pbgra32);
            rtb.Render(element);

            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using MemoryStream ms = new();
            encoder.Save(ms);
            ms.Seek(0, SeekOrigin.Begin);

            using System.Drawing.Bitmap bmp = new(ms);
            return InternalCreateCursor(bmp, xHotSpot, yHotSpot);
        }
    }
}