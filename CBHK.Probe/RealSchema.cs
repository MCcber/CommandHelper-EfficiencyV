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

        public static List<string> ScanParseErrors(string root)
        {
            var report = new List<string>();
            var total = 0;
            var bad = 0;
            foreach (var file in Directory.GetFiles(root, "*.mcdoc", SearchOption.AllDirectories))
            {
                total++;
                var parser = new mcdocParser(new CommonTokenStream(new mcdocLexer(new AntlrInputStream(File.ReadAllText(file)))));
                parser.RemoveErrorListeners();
                parser.file();
                if (parser.NumberOfSyntaxErrors <= 0) continue;
                bad++;
                if (bad <= 15) report.Add("[scan] 解析失败 " + Path.GetRelativePath(root, file) + " errors=" + parser.NumberOfSyntaxErrors);
            }
            report.Add("[scan] total=" + total + " bad=" + bad);
            return report;
        }

        /// <summary>
        /// 按语言服务器那条路解析单个文件（ANTLR 语法 + 元数据访问器），返回一句人话。
        /// 宿主那边整份文件失败会让它声明的类型全部缺席 —— 页面只看到"未解析 N"，看不到是哪份。
        /// </summary>
        public static string ParseOne(string file)
        {
            if (!File.Exists(file)) return "文件缺失";
            try
            {
                var parser = new mcdocParser(new CommonTokenStream(new mcdocLexer(new AntlrInputStream(File.ReadAllText(file)))));
                var visitor = new MCDocumentMetaVisitor();
                var declared = visitor.Visit(parser.file()) as List<MetaType>;
                return "errors=" + parser.NumberOfSyntaxErrors + " 类型=" + (declared?.Count ?? -1)
                    + " → " + string.Join(",", (declared ?? []).Select(item => item.Name ?? item.MetaTypeName).Take(12));
            }
            catch (Exception error)
            {
                return "异常 " + error.GetType().Name + "：" + error.Message;
            }
        }

        /// <summary>
        /// 复刻语言服务器侧的 Service()：解析 → 访问器 → 逐个类型建 DTO → 导出类型图。
        /// 服务器异常只回一条 Message（logs\mcdoc-server-*.log 里没有栈），所以在本地分开跑拿栈。
        /// </summary>
        public static List<string> ServerPipeline(string file)
        {
            var lines = new List<string>();
            try
            {
                var parser = new mcdocParser(new CommonTokenStream(new mcdocLexer(new AntlrInputStream(File.ReadAllText(file)))));
                var visitor = new MCDocumentMetaVisitor();
                var metaTypes = (List<MetaType>)visitor.Visit(parser.file());
                lines.Add("parse+visit 类型=" + metaTypes.Count);
                for (var i = 0; i < metaTypes.Count; i++)
                {
                    MetaType schema = metaTypes[i];
                    string name = !string.IsNullOrEmpty(schema.Name) ? schema.Name : schema.MetaTypeName ?? "";
                    try
                    {
                        MinecraftLanguageServer.Service.MetaTypeDataViewModelGenerator.StartBuildDTO(schema, name);
                    }
                    catch (Exception error)
                    {
                        lines.Add("DTO 失败 序号=" + i + " 名字=" + name + " 种类=" + schema.Kind + " → " + error.GetType().Name + "：" + error.Message);
                        foreach (var frame in (error.StackTrace ?? "").Split('\n').Take(6)) lines.Add("    " + frame.Trim());
                        return lines;
                    }
                }
                lines.Add("DTO 全部成功");
                var named = MinecraftLanguageServer.Mcdoc.McdocTypeExporter.CollectNamedStructs(metaTypes);
                var defs = MinecraftLanguageServer.Mcdoc.McdocTypeExporter.CollectTypeDefs(metaTypes, named);
                lines.Add("导出 命名结构=" + named.Count + " 类型图=" + defs.Count);
            }
            catch (Exception error)
            {
                lines.Add("外层异常 " + error.GetType().Name + "：" + error.Message);
                foreach (var frame in (error.StackTrace ?? "").Split('\n').Take(6)) lines.Add("    " + frame.Trim());
            }
            return lines;
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
