using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Collections.ObjectModel;
using cbhk_environment.GeneralTools.Displayer;
using System.Windows;
using System.Windows.Controls;

namespace cbhk_environment.GeneralTools
{
    public class GenerateBubbleChart
    {
        public static void Generator(ref Image currentImage,ObservableCollection<ItemStructure> CurrentMaterialCollection)
        {
            // 创建一个WriteableBitmap对象
            WriteableBitmap wb = new(300, 300, 300, 300, PixelFormats.Bgra32, null);
            RenderOptions.SetBitmapScalingMode(wb, BitmapScalingMode.HighQuality);

            // 定义每个图像的位置和大小
            int x = 0;
            int y = 0;
            int width = 150;
            int height = 150;
            int index = 0;

            //使用循环遍历图像列表，只处理前9个
            for (int i = 0; i < 4; i++)
            {
                if (i >= CurrentMaterialCollection.Count) break;
                // 获取当前图像
                BitmapImage bi = new(CurrentMaterialCollection[i].ImagePath);
                WriteableBitmap wbImage = new(bi);

                // 将当前图像绘制到指定位置
                wb.Blit(new Rect(x, y, width, height), wbImage, new Rect(0, 0, bi.PixelWidth, bi.PixelHeight));

                // 更新x坐标和y坐标
                x += width;
                index++;
                if (index >= 2)
                {
                    x = 0;
                    y += height;
                    index = 0;
                }
            }
            // 将WriteableBitmap设置为Image控件的Source属性
            currentImage.Source = wb;
        }
    }
}
