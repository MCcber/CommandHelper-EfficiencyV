using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 物品/方块 id 的真实纹理（misode 的 ItemIdPreview 拉远端贴图；仓内没有 vanilla 贴图，走本地 Resource）。
    /// 根目录默认 &lt;bin&gt;/Resource/Textures/item，可被 &lt;bin&gt;/Configuration.json 的 ItemTextures 覆盖。
    /// 解析/解码失败一律返回 null（调用方回落到确定性瓷砖），绝不抛。
    /// </summary>
    public static class McdocItemTextures
    {
        private static readonly Dictionary<string, ImageSource> Cache = new(StringComparer.Ordinal);
        private static string root;
        private static bool resolved;

        /// <summary>纹理根目录（惰性解析；测试/宿主可直接赋值，改完记得 <see cref="Reload"/>）。</summary>
        public static string Root
        {
            get
            {
                lock (Cache)
                {
                    if (!resolved)
                    {
                        root = ResolveRoot();
                        resolved = true;
                    }
                    return root;
                }
            }
            set
            {
                lock (Cache)
                {
                    root = value;
                    resolved = true;
                }
            }
        }

        /// <summary>丢掉缓存与已解析的根（换配置/换目录后用）。</summary>
        public static void Reload()
        {
            lock (Cache)
            {
                root = null;
                resolved = false;
                Cache.Clear();
            }
        }

        private static string ResolveRoot()
        {
            var baseDirectory = AppContext.BaseDirectory ?? "";
            try
            {
                var configPath = Path.Combine(baseDirectory, "Configuration.json");
                if (File.Exists(configPath))
                {
                    var configured = JObject.Parse(File.ReadAllText(configPath))["ItemTextures"]?.Value<string>();
                    if (!string.IsNullOrWhiteSpace(configured))
                    {
                        return Path.IsPathRooted(configured) ? configured : Path.Combine(baseDirectory, configured);
                    }
                }
            }
            catch
            {
                // 配置坏了就当没配：回落默认目录
            }
            return Path.Combine(baseDirectory, "Resource", "Textures", "item");
        }

        /// <summary>id → png 绝对路径（"minecraft:stone" → &lt;root&gt;/minecraft/stone.png）；不合法返回 null。</summary>
        public static string ResolvePath(string id)
        {
            var directory = Root;
            if (string.IsNullOrEmpty(directory) || string.IsNullOrWhiteSpace(id)) return null;
            var name = id.Trim();
            var nameSpace = "minecraft";
            var path = name;
            var colon = name.IndexOf(':');
            if (colon > 0)
            {
                nameSpace = name.Substring(0, colon);
                path = name.Substring(colon + 1);
            }
            if (nameSpace.Length == 0 || path.Length == 0) return null;
            if (path.Contains("..") || nameSpace.Contains("..")) return null;
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                if (nameSpace.IndexOf(invalid) >= 0) return null;
            }
            return Path.Combine(directory, nameSpace, path.Replace('/', Path.DirectorySeparatorChar) + ".png");
        }

        /// <summary>取 id 的纹理；没有/解码失败返回 null（缓存 null，避免每帧重试）。</summary>
        public static ImageSource Get(string id)
        {
            string path;
            try
            {
                path = ResolvePath(id);
            }
            catch
            {
                return null;
            }
            if (path is null) return null;
            lock (Cache)
            {
                if (Cache.TryGetValue(path, out var cached)) return cached;
            }

            ImageSource image = null;
            try
            {
                if (File.Exists(path))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze();
                    image = bitmap;
                }
            }
            catch
            {
                image = null;   // 损坏的 png / 不支持的编码：回落到瓷砖
            }

            lock (Cache)
            {
                Cache[path] = image;
            }
            return image;
        }

        /// <summary>瓷砖首字：去掉命名空间后取前两个字符（"minecraft:stone" → "ST"）。</summary>
        public static string TileText(string id)
        {
            var name = (id ?? "").Trim();
            var colon = name.IndexOf(':');
            if (colon >= 0) name = name.Substring(colon + 1);
            if (name.Length == 0) return "?";
            return (name.Length <= 2 ? name : name.Substring(0, 2)).ToUpperInvariant();
        }

        /// <summary>确定性瓷砖底色：稳定 hash（不用 string.GetHashCode，它逐进程随机化）→ 色相。</summary>
        public static Color TileColor(string id)
        {
            var hash = StableHash(id ?? "");
            var hue = (hash % 360u) / 360.0;
            return HslToRgb(hue, 0.45, 0.52);
        }

        private static uint StableHash(string text)
        {
            var hash = 2166136261u;   // FNV-1a
            foreach (var character in text)
            {
                hash ^= character;
                hash *= 16777619u;
            }
            return hash;
        }

        private static Color HslToRgb(double hue, double saturation, double lightness)
        {
            double R, G, B;
            if (saturation == 0)
            {
                R = G = B = lightness;
            }
            else
            {
                var q = lightness < 0.5 ? lightness * (1 + saturation) : lightness + saturation - lightness * saturation;
                var p = 2 * lightness - q;
                R = HueToRgb(p, q, hue + 1.0 / 3.0);
                G = HueToRgb(p, q, hue);
                B = HueToRgb(p, q, hue - 1.0 / 3.0);
            }
            return Color.FromRgb((byte)(R * 255), (byte)(G * 255), (byte)(B * 255));
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
            if (t < 1.0 / 2.0) return q;
            if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
            return p;
        }
    }
}
