using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CBHK.Utility.Visual.Screen
{
    public static class ScreenShoot
    {
        public static BitmapSource CaptureUIElement(FrameworkElement element)
        {
            // 1. 获取控件的实际尺寸
            int width = (int)element.ActualWidth;
            int height = (int)element.ActualHeight;
            // 2. 创建 RenderTargetBitmap (参数：宽, 高, 水平DPI, 垂直DPI, 像素格式)
            // 使用 96 DPI 是 WPF 的标准
            RenderTargetBitmap renderTarget = new(
                width, height, 96, 96, PixelFormats.Pbgra32);
            // 3. 渲染控件
            renderTarget.Render(element);
            // 4. 冻结对象以提高跨线程访问性能并防止内存泄漏
            renderTarget.Freeze();

            return renderTarget;
        }
    }
}
