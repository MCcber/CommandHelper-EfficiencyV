using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace cbhk_environment.GeneralTools
{
    public sealed class UriToBitmapConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Uri uri = (Uri)value;
            if (uri == null) return null;
            BitmapImage bmp = new()
            {
                DecodePixelHeight = 250 // 确定解码高度，宽度不同时设置
            };
            bmp.BeginInit();
            // 延迟，必要时创建
            bmp.CreateOptions = BitmapCreateOptions.DelayCreation;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = uri;
            bmp.EndInit(); //结束初始化
            return bmp;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
