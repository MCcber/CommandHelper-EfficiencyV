using System;
using System.Globalization;
using System.Windows.Data;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 候选 id → 图标：给"带图标的下拉"当 ItemTemplate 的绑定源用。
    /// 只在行被真正物化时才查一次磁盘（<see cref="McdocItemTextures"/> 内部按路径缓存），
    /// 所以一千多个候选的 id 下拉不会为了图标把整张表都读进来；没有贴图就返回 null（那一项只显示文字）。
    /// </summary>
    public class MdocItemIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string id && !string.IsNullOrWhiteSpace(id))
            {
                return McdocItemTextures.Get(id);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
