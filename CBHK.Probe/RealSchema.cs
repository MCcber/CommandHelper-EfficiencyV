using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using CBHK.Utility.Data.Mcdoc;
using MinecraftLanguageModelLibrary.Data;
using MinecraftLanguageServer.Visitor;

namespace CBHK.Probe
{
    /// <summary>
    /// 真 schema 索引：把 Resource\vanilla-mcdoc 下的 .mcdoc 全部登记进 McdocTypeIndex。
    /// 与宿主走同一条路（MCDocumentMetaVisitor → RegisterAll），只是不经过语言服务器进程。
    /// </summary>
    internal static class RealSchema
    {
        /// <summary>从 exe 目录往上找 Resource\vanilla-mcdoc（构建输出与源码树两种位置都认）。</summary>
        public static string LocateMcdocRoot()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (var depth = 0; directory is not null && depth < 8; depth++, directory = directory.Parent)
            {
                var candidates = new[]
                {
                    Path.Combine(directory.FullName, "Resource", "vanilla-mcdoc"),
                    Path.Combine(directory.FullName, "CBHK", "Resource", "vanilla-mcdoc"),
                };
                foreach (var candidate in candidates)
                {
                    if (Directory.Exists(candidate)) return candidate;
                }
                // 源码树里只有构建输出的那份（CBHK\bin\Debug\<tfm>\Resource\vanilla-mcdoc）
                var buildRoot = Path.Combine(directory.FullName, "CBHK", "bin", "Debug");
                if (Directory.Exists(buildRoot))
                {
                    foreach (var tfm in Directory.GetDirectories(buildRoot))
                    {
                        var candidate = Path.Combine(tfm, "Resource", "vanilla-mcdoc");
                        if (Directory.Exists(candidate)) return candidate;
                    }
                }
            }
            return null;
        }

        public static int Load(McdocTypeIndex index, string root)
        {
            if (index is null || string.IsNullOrEmpty(root) || !Directory.Exists(root)) return 0;
            var loaded = 0;
            foreach (var file in Directory.GetFiles(root, "*.mcdoc", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                relative = relative.Substring(0, relative.Length - ".mcdoc".Length);
                if (relative.EndsWith("/mod", StringComparison.Ordinal))
                {
                    relative = relative.Substring(0, relative.Length - 4);
                }
                var module = "::" + relative.Replace("/", "::");
                var lexer = new mcdocLexer(new AntlrInputStream(File.ReadAllText(file)));
                var parser = new mcdocParser(new CommonTokenStream(lexer));
                var visitor = new MCDocumentMetaVisitor();
                var declared = visitor.Visit(parser.file()) as List<MetaType>;
                index.RegisterUsePaths(module, visitor.UsePathList);
                foreach (var type in declared ?? [])
                {
                    index.RegisterAll([(module + "::" + (type.Name ?? type.MetaTypeName ?? ""), type)]);
                    loaded++;
                }
                foreach (var pair in MinecraftLanguageServer.Mcdoc.McdocTypeExporter.CollectNamedStructs(declared))
                {
                    index.RegisterAll([(module + "::" + pair.Key, pair.Value)]);
                }
            }
            return loaded;
        }
    }
}
