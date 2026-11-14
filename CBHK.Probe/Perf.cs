using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CBHK.Model.Constant;
using CBHK.Utility.Data;
using CBHK.Utility.Data.Mcdoc;
using MinecraftLanguageModelLibrary.Data;

namespace CBHK.Probe
{
    /// <summary>
    /// 物品生成器"打开一次"的耗时拆解：走应用真实启动路径（语言服务器 + 每份 mcdoc 一次管道往返），
    /// 再逐段量"取 id 列表 / 建树 / 投影 / 回写文本"。只用于分析，不做断言。
    /// </summary>
    internal static class Perf
    {
        public static int Item()
        {
            var rootDir = RealSchema.LocateMcdocRoot();
            if (rootDir is null)
            {
                Console.WriteLine("[环境缺失] 找不到 Resource\\vanilla-mcdoc（退出码 2）");
                return 2;
            }
            var baseDir = Path.GetDirectoryName(Path.GetDirectoryName(rootDir));   // <tfm> 目录
            Console.WriteLine("[环境] base=" + baseDir);

            // ① 应用启动：Resource.Init（注册表 + 语言服务器 + 每份 mcdoc 一次管道往返）
            var total = Stopwatch.StartNew();
            var resource = new Resource();
            var progressCount = 0;
            var initWatch = Stopwatch.StartNew();
            try
            {
                resource.Init(new Progress<(int Done, int Total)>(_ => progressCount++)).GetAwaiter().GetResult();
            }
            catch (Exception error)
            {
                Console.WriteLine("[信息] Init 抛了：" + error.GetType().Name + "：" + error.Message);
            }
            initWatch.Stop();
            Console.WriteLine("[实测] Resource.Init = " + initWatch.ElapsedMilliseconds + " ms"
                + "（进度回调 " + progressCount + " 次）"
                + " 符号=" + resource.McdocTypes.TypeDefPaths.Count
                + " 调度器=" + resource.McdocTypes.DispatcherRegistries.Count
                + " 失败=" + resource.McdocIndexFailures.Count);
            foreach (var failure in resource.McdocIndexFailures.Take(3)) Console.WriteLine("   失败例：" + failure);

            // ①b 对照：同样的 schema 用**进程内** ANTLR 解析一遍要多久（不走管道、不起子进程）
            var inProcWatch = Stopwatch.StartNew();
            var localIndex = new McdocTypeIndex();
            var localCount = RealSchema.Load(localIndex, rootDir);
            inProcWatch.Stop();
            Console.WriteLine("[实测] 进程内解析同一批 mcdoc = " + inProcWatch.ElapsedMilliseconds + " ms（类型 "
                + localCount + " 个，" + localIndex.TypeDefPaths.Count + " 条）");

            // ①c 缓存命中路径单独量：237 份文档"读盘 + 反序列化"到底多少
            var cachedWatch = Stopwatch.StartNew();
            var cachedHits = 0;
            foreach (var file in Directory.GetFiles(Path.Combine(rootDir, "java"), "*.mcdoc", SearchOption.AllDirectories))
            {
                if (CBHK.Domain.DataContext.McdocIndexCache.TryRead(file) is not null) cachedHits++;
            }
            cachedWatch.Stop();
            Console.WriteLine("[实测] 缓存命中路径（" + cachedHits + " 份：读盘+反序列化）= " + cachedWatch.ElapsedMilliseconds + " ms");

            // ② 物品 id 列表（每次进页面/切版本都会重建）
            var version = "1.21.11";
            var idsWatch = Stopwatch.StartNew();
            var ids = resource.GetIds("item", version);
            idsWatch.Stop();
            Console.WriteLine("[实测] GetIds(item) = " + idsWatch.ElapsedMilliseconds + " ms，条数=" + ids.Count);

            // ③ 图标那一格：每个 id 一次 File.Exists（有图就再 new BitmapImage，这里只量文件探测的下限）
            var imageSet = Path.Combine(baseDir, "ImageSet");
            var existsWatch = Stopwatch.StartNew();
            var hits = 0;
            foreach (var id in ids)
            {
                if (File.Exists(Path.Combine(imageSet, id + ".png"))) hits++;
            }
            existsWatch.Stop();
            Console.WriteLine("[实测] 图标探测（" + ids.Count + " 次 File.Exists，ImageSet 存在=" + Directory.Exists(imageSet)
                + "）= " + existsWatch.ElapsedMilliseconds + " ms，命中=" + hits);

            // ③b 真图标：按 LoadItemIdsForVersion 的写法（File.Exists + new BitmapImage）建 1505 项
            var appImageSet = LocateImageSet();
            if (Directory.Exists(appImageSet))
            {
                var sw = Stopwatch.StartNew();
                var bitmaps = new System.Collections.Generic.List<System.Windows.Media.Imaging.BitmapImage>();
                foreach (var id in ids)
                {
                    var file = Path.Combine(appImageSet, id + ".png");
                    if (!File.Exists(file)) continue;
                    bitmaps.Add(new System.Windows.Media.Imaging.BitmapImage(new Uri(file, UriKind.Absolute)));
                }
                sw.Stop();
                Console.WriteLine("[实测] 建 BitmapImage（" + bitmaps.Count + " 张，惰性）= " + sw.ElapsedMilliseconds + " ms");
                sw.Restart();
                long pixels = 0;
                foreach (var bitmap in bitmaps)
                {
                    if (bitmap is null) continue;
                    pixels += bitmap.PixelWidth;   // 逼出真正的解码（行模板里的 <Image Source> 就是这么触发的）
                }
                sw.Stop();
                Console.WriteLine("[实测] 强制解码 " + bitmaps.Count + " 张（PixelWidth）= " + sw.ElapsedMilliseconds + " ms，累计宽=" + pixels);

                // 新口径（第 1 步之后）：一次目录列举 + 只建文本项（首屏）；解码挪到后台且降采样到 32px
                sw.Restart();
                var names = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var file in Directory.GetFiles(appImageSet, "*.png")) names.Add(Path.GetFileNameWithoutExtension(file));
                var textOnly = new System.Collections.Generic.List<object>(ids.Count);
                foreach (var id in ids) textOnly.Add(id);
                sw.Stop();
                Console.WriteLine("[实测] 新口径首屏（目录列举 + 只建文本项 " + textOnly.Count + " 条）= " + sw.ElapsedMilliseconds + " ms，图标命中=" + names.Count);

                sw.Restart();
                var lazy = 0;
                foreach (var id in ids)
                {
                    if (!names.Contains(id)) continue;
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.DecodePixelWidth = 32;
                    bitmap.UriSource = new Uri(Path.Combine(appImageSet, id + ".png"), UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze();
                    lazy++;
                }
                sw.Stop();
                Console.WriteLine("[实测] 新口径后台补齐（" + lazy + " 张，32px+OnLoad+Freeze）= " + sw.ElapsedMilliseconds + " ms（不占首屏）");
            }
            else
            {
                Console.WriteLine("[信息] 找不到 app 的 ImageSet：" + appImageSet);
            }

            // ④ 建树 + 投影 + 回写文本（acacia_boat，一份空文档；连做 3 次看稳态）
            var rootType = new McdocType
            {
                Kind = McdocTypeKind.Dispatcher,
                Registry = "minecraft:item",
                Indices = [McdocIndex.Static("acacia_boat")],
            };
            for (var round = 1; round <= 3; round++)
            {
                var document = new McdocTreeDocument(resource.McdocTypes)
                {
                    GameVersion = version,
                    RegistryOptions = new MdocRegistryOptions(resource),
                    DocumentUri = "file:///generator/minecraft:item.json",
                };
                var projector = new McdocRowProjector();
                var loading = false;
                var pipeline = new McdocPipeline
                {
                    Document = document,
                    Projector = projector,
                    RootType = () => rootType,
                    GetLoading = () => loading,
                    SetLoading = value => loading = value,
                };
                pipeline.ScheduleRebuild = anchor => pipeline.RebuildTargetedOrFallback(anchor);
                pipeline.Fallback = () => pipeline.Reload();
                projector.Document = document;
                projector.RequestReload = () => pipeline.Reload();
                document.NodeValueChanged = node => pipeline.OnValueChanged(node);
                document.NodeStructureChanged = node => pipeline.OnStructureChanged(node);

                var buildWatch = Stopwatch.StartNew();
                document.Load(rootType, McdocJsonParser.Parse("{ }"));
                var loadMs = buildWatch.ElapsedMilliseconds;
                buildWatch.Restart();
                projector.Reset(document.Nodes.ToList());
                var projectMs = buildWatch.ElapsedMilliseconds;
                buildWatch.Restart();
                var text = document.RootJson is null ? "" : McdocJsonFormatter.Format(document.RootJson);
                var textMs = buildWatch.ElapsedMilliseconds;
                Console.WriteLine("[实测] 第" + round + "次建树 load=" + loadMs + "ms 投影=" + projectMs
                    + "ms 文本=" + textMs + "ms 顶层行=" + document.Nodes.Count + " 字符=" + text.Length);
            }
            total.Stop();
            Console.WriteLine("[实测] 合计（含启动）= " + total.ElapsedMilliseconds + " ms");
            return 0;
        }

        /// <summary>找应用 bin 下的 ImageSet（探针自己的 bin 通常没有：图标只在宿主安装/构建输出里）。</summary>
        private static string LocateImageSet()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (var depth = 0; directory is not null && depth < 6; depth++, directory = directory.Parent)
            {
                var buildRoot = Path.Combine(directory.FullName, "CBHK", "bin", "Debug");
                if (!Directory.Exists(buildRoot)) continue;
                foreach (var tfm in Directory.GetDirectories(buildRoot))
                {
                    var candidate = Path.Combine(tfm, "ImageSet");
                    if (Directory.Exists(candidate)) return candidate;
                }
            }
            return null;
        }
    }
}
