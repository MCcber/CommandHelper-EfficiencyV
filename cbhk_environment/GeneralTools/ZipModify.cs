using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Media.Imaging;

namespace cbhk_environment.GeneralTools
{
    public static class ZipModify
    {
        /// <summary>
        /// 解压指定压缩包内的所有.png文件
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <returns></returns>
        public static List<BitmapImage> DeCompressionImageSet(string sourcePath)
        {
            List<BitmapImage> result = new();
            using FileStream zipToOpen = new(sourcePath, FileMode.Open);
            using ZipArchive archive = new(zipToOpen, ZipArchiveMode.Read);
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (entry.Length > 0 && entry.FullName.EndsWith(".png"))
                {
                    using Stream stream = entry.Open();
                    BitmapImage bitmapImage = new();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                    result.Add(bitmapImage);
                }
                else
                    result.Add(new BitmapImage());
            }
            return result;
        }

        /// <summary>
        /// 解压指定图像
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="targetName"></param>
        /// <returns></returns>
        public static BitmapImage DeCompressionTargetImage(string sourcePath,string targetName)
        {
            using FileStream zipToOpen = new(sourcePath, FileMode.Open);
            using ZipArchive archive = new(zipToOpen, ZipArchiveMode.Read);
            ZipArchiveEntry entry = archive.GetEntry(targetName);
            entry ??= archive.GetEntry(targetName[..targetName.LastIndexOf('.')] + "_spawn_egg.png");
            if (entry != null)
            {
                try
                {
                    using Stream stream = entry.Open();
                    using MemoryStream ms = new();
                    stream.CopyTo(ms);
                    BitmapImage result = new()
                    {
                        CacheOption = BitmapCacheOption.OnLoad
                    };
                    result.BeginInit();
                    result.StreamSource = ms;
                    ms.Position = 0;
                    result.EndInit();
                    result.Freeze();
                    return result;
                }
                catch(Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
            return null;
        }
    }
}
