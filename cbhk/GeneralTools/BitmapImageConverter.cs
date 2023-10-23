using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Media.Imaging;

namespace cbhk.GeneralTools
{
    public partial class BitmapImageConverter
    {
        /// <summary>
        /// Bitmap转BitmapImage
        /// </summary>
        /// <param name="ImageOriginal"></param>
        /// <returns></returns>
        public static BitmapImage ToBitmapImage(Bitmap ImageOriginal)
        {

            Bitmap ImageOriginalBase = new (ImageOriginal);
            BitmapImage bitmapImage = new();
            using (MemoryStream ms = new())
            {
                ImageOriginalBase.Save(ms, ImageFormat.Png);
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }
            return bitmapImage;
        }

        /// <summary>
        /// 字节数组转BitmapImage
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static BitmapImage ByteArrayToBitmapImage(byte[] content)
        {
            using var memoryStream = new MemoryStream(content);
            using var bitmap = new Bitmap(memoryStream);
            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                bitmap.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = new MemoryStream();
            bitmap.Save(bitmapImage.StreamSource, ImageFormat.Png);
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        /// <summary>
        /// BitmapImage转为Bitmap
        /// </summary>
        /// <param name="bitmapImage"></param>
        /// <returns></returns>
        private Bitmap BitmapImageToBitmap(BitmapImage bitmapImage)
        {
            using MemoryStream outStream = new();
            BitmapEncoder enc = new BmpBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bitmapImage));
            enc.Save(outStream);
            Bitmap bitmap = new Bitmap(outStream);

            return new Bitmap(bitmap);
        }
    }
}