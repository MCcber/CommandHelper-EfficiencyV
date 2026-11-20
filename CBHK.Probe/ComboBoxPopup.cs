using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CBHK.CustomControl.VectorComboBox;

namespace CBHK.Probe
{
    /// <summary>
    /// 下拉弹层的像素判据：成员区到弹层四条边的留白必须一致（外观类缺陷只能用截图 + 几何核对）。
    /// 用法：--render-combobox-popup &lt;目录&gt;
    /// </summary>
    internal static class ComboBoxPopup
    {
        public static int Render(string outputDirectory)
        {
            var dir = outputDirectory ?? Path.Combine(Path.GetTempPath(), "combobox-popup");
            Directory.CreateDirectory(dir);
            WindowPerf.EnsureApplication();

            var items = Enumerable.Range(1, 24)
                .Select(index => new VectorTextComboBoxItem
                {
                    ItemID = "minecraft:armor/" + index + "/emerald_iron_chestplate",
                    Text = "minecraft:armor/" + index + "/emerald_iron_chestplate",
                })
                .ToList();

            var combo = new VectorTextComboBox
            {
                Title = "搜索成员",
                Width = 480,
                ItemsSource = items,
                Style = Application.Current.Resources["VectorTextComboBoxStyle"] as Style,
            };
            var host = new Grid();
            host.Children.Add(combo);
            var window = new Window
            {
                Width = 700,
                Height = 700,
                Left = -4000,
                Top = -4000,
                ShowInTaskbar = false,
                Content = host,
            };
            window.Show();
            Pump();
            combo.IsDropDownOpen = true;
            Pump();
            Pump();

            var popup = combo.Template.FindName("PART_Popup", combo) as Popup;
            if (popup?.Child is not FrameworkElement root)
            {
                Console.WriteLine("[环境缺失] 弹层没有内容（退出码 2）");
                return 2;
            }
            root.UpdateLayout();

            Console.WriteLine("[弹层] 根=" + root.GetType().Name
                + " 尺寸=" + root.ActualWidth.ToString("F0") + "x" + root.ActualHeight.ToString("F0")
                + " MaxDropDownHeight=" + combo.MaxDropDownHeight.ToString("F0"));
            foreach (var element in Visuals(root).OfType<FrameworkElement>().Where(element => !string.IsNullOrEmpty(element.Name)))
            {
                Print(root, element);
            }
            var containers = Enumerable.Range(0, combo.Items.Count)
                .Select(index => combo.ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement)
                .Where(container => container is not null)
                .ToList();
            Console.WriteLine("[容器] 实到=" + containers.Count);
            if (containers.Count > 0)
            {
                Print(root, containers[0]);
                Print(root, containers[containers.Count - 1]);
                Console.WriteLine("[容器] 高=" + containers[0].ActualHeight.ToString("F2") + " 宽=" + containers[0].ActualWidth.ToString("F2"));
            }
            if (Visuals(root).OfType<ScrollViewer>().FirstOrDefault() is { } scroller)
            {
                Console.WriteLine("[滚动] 视口=" + scroller.ViewportWidth.ToString("F1") + "x" + scroller.ViewportHeight.ToString("F1")
                    + " 内容=" + scroller.ExtentWidth.ToString("F1") + "x" + scroller.ExtentHeight.ToString("F1")
                    + " 横向条=" + scroller.ComputedHorizontalScrollBarVisibility
                    + " 纵向条=" + scroller.ComputedVerticalScrollBarVisibility);
            }

            var width = Math.Max(1, (int)Math.Ceiling(root.ActualWidth));
            var height = Math.Max(1, (int)Math.Ceiling(root.ActualHeight));
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(root);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            var path = Path.Combine(dir, "combobox-popup.png");
            using (var stream = File.Create(path))
            {
                encoder.Save(stream);
            }
            Console.WriteLine("[信息] 已渲染 " + path + " 尺寸=" + width + "x" + height);
            var itemArea = FindByName(root, "DropDownScrollViewer") as FrameworkElement;
            var itemTop = 0;
            if (itemArea is not null)
            {
                itemTop = (int)Math.Round(itemArea.TransformToVisual(root).TransformBounds(new Rect(itemArea.RenderSize)).Top);
            }
            var marginsPassed = ReportMargins(path, itemTop);
            SaveCrop(bitmap, dir, "combobox-popup-topleft", 0, 52, 72, 72);
            SaveCrop(bitmap, dir, "combobox-popup-right", width - 72, 52, 72, height - 52);
            SaveCrop(bitmap, dir, "combobox-popup-bottom", 0, height - 56, width, 56);
            window.Close();
            Pump();
            return marginsPassed ? 0 : 1;
        }

        /// <summary>放大裁图：2px 级别的边距在原始比例下看不出来，必须放大出证据。</summary>
        private static void SaveCrop(BitmapSource source, string directory, string name, int x, int y, int width, int height)
        {
            var clippedWidth = Math.Min(width, source.PixelWidth - x);
            var clippedHeight = Math.Min(height, source.PixelHeight - y);
            var crop = new CroppedBitmap(source, new Int32Rect(x, y, clippedWidth, clippedHeight));
            var scaled = new TransformedBitmap(crop, new ScaleTransform(6, 6));
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(scaled));
            var path = Path.Combine(directory, name + ".png");
            using (var stream = File.Create(path))
            {
                encoder.Save(stream);
            }
            Console.WriteLine("[信息] 裁图 " + path + " 区域=" + x + "," + y + " " + clippedWidth + "x" + clippedHeight + " 放大6倍");
        }

        /// <summary>成员区四条边的像素剖面必须同构：黑框 → 2px 灰环 → 成员底色。</summary>
        private static bool ReportMargins(string path, int itemTop)
        {
            var frame = BitmapFrame.Create(new Uri(path), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            var converted = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
            var width = converted.PixelWidth;
            var height = converted.PixelHeight;
            var pixels = new byte[width * height * 4];
            converted.CopyPixels(pixels, width * 4, 0);

            (byte, byte, byte) At(int x, int y)
            {
                var offset = (y * width * 4) + (x * 4);
                return (pixels[offset], pixels[offset + 1], pixels[offset + 2]);
            }

            void Profile(string edge, Func<int, (byte, byte, byte)> sample, int from, int count)
            {
                var parts = new List<string>();
                (byte, byte, byte)? previous = null;
                var start = from;
                for (var index = from; index < from + count; index++)
                {
                    var color = sample(index);
                    if (previous is null)
                    {
                        previous = color;
                        continue;
                    }
                    if (!color.Equals(previous.Value))
                    {
                        parts.Add(start + ".." + (index - 1) + "=" + Hex(previous.Value));
                        previous = color;
                        start = index;
                    }
                }
                parts.Add(start + ".." + (from + count - 1) + "=" + Hex(previous!.Value));
                Console.WriteLine("[剖面] " + edge + " " + string.Join(" | ", parts));
            }

            var line = height / 2;
            var column = width / 2;
            var ring = ((byte)144, (byte)141, (byte)140);
            Console.WriteLine("[剖面] 图=" + width + "x" + height + " 横线 y=" + line + " 竖线 x=" + column + " 成员区上沿=" + itemTop);
            var edges = new (string Edge, Func<int, (byte, byte, byte)> Sample, int From, int Count)[]
            {
                ("左", index => At(index, line), 0, 12),
                ("右", index => At(index, line), width - 12, 12),
                ("上", index => At(column, index), Math.Max(0, itemTop - 8), 16),
                ("下", index => At(column, index), height - 12, 12),
            };
            var passed = true;
            foreach (var (edge, sample, from, count) in edges)
            {
                Profile(edge, sample, from, count);
                // 灰环 #8C8D90 必须出现在离该边 12px 以内：四条边同构，谁缺了谁就是缺陷。
                var hasRing = Enumerable.Range(from, count).Any(index => sample(index) == ring);
                Console.WriteLine("[判据] " + edge + " 边 12px 内灰环=" + (hasRing ? "有" : "无"));
                passed &= hasRing;
            }
            Console.WriteLine(passed ? "[判据] 四边边距对称：通过" : "[判据] 四边边距不对称：失败");
            return passed;
        }

        private static string Hex((byte R, byte G, byte B) color) =>
            "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");

        private static void Print(FrameworkElement root, FrameworkElement element)
        {
            var rect = element.TransformToVisual(root).TransformBounds(new Rect(element.RenderSize));
            Console.WriteLine("[几何] " + element.GetType().Name + " name='" + element.Name + "'"
                + " 左=" + rect.Left.ToString("F1") + " 上=" + rect.Top.ToString("F1")
                + " 右余=" + (root.ActualWidth - rect.Right).ToString("F1")
                + " 下余=" + (root.ActualHeight - rect.Bottom).ToString("F1")
                + " 尺寸=" + rect.Width.ToString("F1") + "x" + rect.Height.ToString("F1"));
        }

        private static FrameworkElement FindByName(DependencyObject root, string name) =>
            Visuals(root).OfType<FrameworkElement>().FirstOrDefault(element => element.Name == name);

        private static IEnumerable<DependencyObject> Visuals(DependencyObject root)
        {
            yield return root;
            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var index = 0; index < count; index++)
            {
                foreach (var child in Visuals(VisualTreeHelper.GetChild(root, index)))
                {
                    yield return child;
                }
            }
        }

        private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }
}
