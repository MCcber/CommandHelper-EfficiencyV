using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MinecraftLanguageModelLibrary.Data;

namespace CBHK.Domain.DataContext
{
    /// <summary>
    /// 语言服务器解析结果的磁盘缓存：mcdoc 文档没变就直接反序列化服务器当初返回的那份 JSON，
    /// 不再为每个文件开一条命名管道往返（实测每个文件一次 Connect，237 个文件 ~1.0 s）。
    /// 键 = 文档绝对路径的 SHA256；值 = 头一行 (mtime.ticks|length) 戳 + 原始 JSON（零保真风险：
    /// 缓存的就是服务器原样返回的内容，反序列化走同一份 JsonSerializerOptions）。
    /// 关掉：环境变量 CBHK_MCDOC_INDEX_CACHE=0。
    /// </summary>
    public static class McdocIndexCache
    {
        private static readonly JsonSerializerOptions DeserializeOptions = new() { PropertyNameCaseInsensitive = true };

        public static bool Enabled { get; } =
            !string.Equals(Environment.GetEnvironmentVariable("CBHK_MCDOC_INDEX_CACHE"), "0", StringComparison.Ordinal);

        /// <summary>缓存目录：LocalApplicationData（程序装在 Program Files 也能写）。</summary>
        private static string Directory
        {
            get
            {
                // 目录里带程序版本：升级后旧缓存一律不复用（解析逻辑可能变了）
                var version = typeof(McdocIndexCache).Assembly.GetName().Version?.ToString() ?? "0";
                var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CBHK", "mcdoc-index-cache", version);
                try { System.IO.Directory.CreateDirectory(root); } catch (Exception) { return null; }
                return root;
            }
        }

        private static string PathFor(string sourcePath, out string stamp)
        {
            stamp = null;
            var directory = Directory;
            if (directory is null || string.IsNullOrEmpty(sourcePath)) return null;
            try
            {
                var info = new FileInfo(sourcePath);
                if (!info.Exists) return null;
                stamp = info.LastWriteTimeUtc.Ticks + "|" + info.Length;
            }
            catch (Exception)
            {
                return null;
            }
            var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sourcePath)));
            return Path.Combine(directory, key + ".json");
        }

        /// <summary>命中返回文档；任何异常/戳不符都当未命中（绝不让缓存拖坏索引）。</summary>
        public static MCDocumentFile TryRead(string sourcePath)
        {
            if (!Enabled) return null;
            try
            {
                var path = PathFor(sourcePath, out var stamp);
                if (path is null || !File.Exists(path)) return null;
                using var reader = new StreamReader(path, Encoding.UTF8);
                if (!string.Equals(reader.ReadLine(), stamp, StringComparison.Ordinal)) return null;
                var json = reader.ReadToEnd();
                return string.IsNullOrWhiteSpace(json)
                    ? null
                    : JsonSerializer.Deserialize<MCDocumentFile>(json, DeserializeOptions);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>写缓存：先写 .tmp 再换名，半截文件不会被下次当成命中。</summary>
        public static void Write(string sourcePath, string json)
        {
            if (!Enabled || string.IsNullOrWhiteSpace(json)) return;
            try
            {
                var path = PathFor(sourcePath, out var stamp);
                if (path is null) return;
                var temp = path + ".tmp";
                using (var writer = new StreamWriter(temp, false, Encoding.UTF8))
                {
                    writer.WriteLine(stamp);
                    writer.Write(json);
                }
                File.Move(temp, path, true);
            }
            catch (Exception)
            {
                // 缓存失败不影响正确性
            }
        }
    }
}
