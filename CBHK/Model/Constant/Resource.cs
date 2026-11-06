using CBHK.Domain.DataContext;
using CBHK.Utility.Data.Mcdoc;
using MinecraftLanguageModelLibrary.Data;
using MinecraftLanguageModelLibrary.Utility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CBHK.Model.Constant
{
    public class Resource
    {
        #region Field
        private string configurationPath = AppDomain.CurrentDomain.BaseDirectory + @"Configuration.json";
        /// <summary>Configuration.json 缺失时用的注册表目录（仓里 Resource 下就带这一份）。</summary>
        private static readonly string DefaultRegistriesDirectoryPath = "Resource" + Path.DirectorySeparatorChar + "Registries";
        #endregion

        #region Property
        public RegexService RegexService { get; set; } = new();
        public Dictionary<string, string> GenertorConfiguration { get; private set; }
        public JObject RunningDataObject { get;private set; } = [];
        public string ImageSetDirectoryPath { get; set; } = @"ImageSet";
        public string MCDocumentLeadingPath { get; set; } = "Resource" + Path.DirectorySeparatorChar + "vanilla-mcdoc" + Path.DirectorySeparatorChar + "java";
        public string MCDocumentBasePath { get; set; } = @"::java";
        public string MCDocumentEditorKey { get; set; } = Path.DirectorySeparatorChar + "data";
        /// <summary>
        /// 文档别名映射表
        /// </summary>
        //public ConcurrentDictionary<string, MetaTypeEditorFieldDTO> DocumentAliaItemMap { get; set; } = [];
        /// <summary>
        /// 记录所有文档的路径与对应的DTO对象的映射关系
        /// </summary>
        public ConcurrentDictionary<string, MetaTypeEditorFieldDTO> DocumentItemMap { get; set; } = [];
        /// <summary>
        /// 记录所有文档的路径与对应的use语句映射关系
        /// </summary>
        public ConcurrentDictionary<string, List<string>> DocumentPathItemMap { get; set; } = [];
        /// <summary>
        /// 新 mcdoc 求值管线的符号索引（由 MCDocumentFile.TypeDefList 建立）。
        /// 与上面两张表并行存在、互不依赖：旧管线读它们，新管线只读这一份。
        /// </summary>
        public McdocTypeIndex McdocTypes { get; } = new();

        /// <summary>
        /// 建索引时取不到的 mcdoc 文档（语言服务器没返回 / 返回空）。
        /// 一个文档缺失会让别处对它的引用全部退化，所以必须能在界面上看见，而不是静默残缺。
        /// </summary>
        public IReadOnlyCollection<string> McdocIndexFailures { get; private set; } = [];
        #endregion

        #region Method
        /// <summary>
        /// 载入一个目录下的全部注册表数据（Registries/*.json）：文件名当版本键，内容并进 RunningDataObject。
        /// Init 与探针共用这一份：探针不走 Init，但没有这批数据时 #[id] 字段会全部退化成文本框。
        /// </summary>
        /// <returns>读到的版本文件数；目录不存在返回 0。</returns>
        public int LoadRegistries(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                return 0;
            }

            int count = 0;
            foreach (string file in Directory.GetFiles(directoryPath, "*.json"))
            {
                string versionString = Path.GetFileNameWithoutExtension(file);
                //创建/覆盖版本节点
                RunningDataObject[versionString] = JObject.Parse(File.ReadAllText(file));
                count++;
            }
            return count;
        }

        /// <summary>
        /// 取指定版本的注册表数据：版本键不存在时回退到最高可用版本。
        /// 版本列表（VersionList.json）通常领先于 Registries 目录的覆盖范围，
        /// 不回退的话所有 #[id=...] 枚举都会空掉——界面上表现为一个只剩 "- unset -" 的下拉框加红色告警。
        /// </summary>
        public JToken GetRegistriesData(string version)
        {
            if (RunningDataObject is null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(version) && RunningDataObject.TryGetValue(version, out JToken exactVersionData))
            {
                return exactVersionData;
            }

            string latestVersionKey = RunningDataObject.Properties()
                .Select(property => property.Name)
                .OrderByDescending(ParseVersionKey)
                .FirstOrDefault();

            return string.IsNullOrEmpty(latestVersionKey) ? null : RunningDataObject[latestVersionKey];
        }

        private static Version ParseVersionKey(string key)
            => Version.TryParse(key, out Version parsedVersion) ? parsedVersion : new Version(0, 0);

        public async Task Init(IProgress<(int Done, int Total)> progress = null)
        {
            // Configuration.json 是可选的（仓里没有这一份）：以前 File.Exists 为假会让整条 Init 静默跳过 ——
            // 注册表与符号表双双为空，生成器页面的默认文档只能是 null，而状态栏一条提示都没有。
            JObject configuration = null;
            string configurationNote = null;
            if (File.Exists(configurationPath))
            {
                try
                {
                    configuration = JObject.Parse(File.ReadAllText(configurationPath));
                }
                catch (Exception error)
                {
                    configurationNote = "Configuration.json 读取失败，已按默认路径继续：" + error.Message;
                }
            }

            try
            {
                #region 收集注册表数据
                // 配置只负责"改路径"：缺文件不改变这条管线本身（注册表/符号表都还在仓里 Resource 下）
                string registriesDirectoryPath = configuration?["Registries"]?.Value<string>();
                if (string.IsNullOrEmpty(registriesDirectoryPath))
                {
                    registriesDirectoryPath = DefaultRegistriesDirectoryPath;
                }
                GenertorConfiguration = configuration?["Generator"]?.ToObject<Dictionary<string, string>>() ?? [];
                LoadRegistries(AppDomain.CurrentDomain.BaseDirectory + registriesDirectoryPath);
                #endregion

                #region 语言服务器就位：应用自己拉起，失败不挂起
                // 起进程 + 等就绪都是阻塞的（最长 15s），放后台线程：冷启动不能把 UI 冻住
                McdocLanguageServerStatus serverStatus = await Task.Run(() => McdocLanguageServerHost.Shared.EnsureRunning(
                    AppDomain.CurrentDomain.BaseDirectory, TimeSpan.FromSeconds(15)));
                if (!serverStatus.IsRunning)
                {
                    // 不抛不退出：页面照常打开，#[id] 退化但带诊断，状态栏能看到原因
                    McdocIndexFailures = configurationNote is null
                        ? new[] { "语言服务器不可用：" + serverStatus.Reason }
                        : new[] { configurationNote, "语言服务器不可用：" + serverStatus.Reason };
                    return;
                }
                #endregion

                #region 全量解析mcdoc目录下所有文档
                string fullDirectoryPath = AppDomain.CurrentDomain.BaseDirectory + MCDocumentLeadingPath;
                string[] fileEntryArray = Directory.GetFileSystemEntries(fullDirectoryPath, "*.mcdoc", SearchOption.AllDirectories);
                string directorySeparatorString = Path.DirectorySeparatorChar.ToString();

                var indexFailures = new ConcurrentBag<string>();
                int documentTotal = fileEntryArray.Length;
                int documentDone = 0;
                progress?.Report((0, documentTotal));
                // 整批给一个总预算：服务器半死不活时宁可残缺也要把流程放出去（骨架屏必须能收起）
                using CancellationTokenSource budget = new(TimeSpan.FromSeconds(90));
                // 并发别开太大：每个文档要开一条命名管道，几百条同时压过去容易把服务器压崩，
                // 一崩就是"某些类型查不到"的静默残缺。
                try
                {
                    await Parallel.ForEachAsync(fileEntryArray,
                        new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = budget.Token },
                        async (fileEntry, cancellationToken) =>
                        {
                            try
                            {
                                if (!File.Exists(fileEntry))
                                {
                                    return;
                                }

                                string fullDirectoryName = Path.GetDirectoryName(fileEntry);
                                string fileName = "::" + Path.GetFileNameWithoutExtension(fileEntry);
                                string mcdocFilePath = fullDirectoryName.Replace(fullDirectoryPath, "");
                                string mcdocFileReferencePath = mcdocFilePath.Replace(directorySeparatorString, "::");
                                string documentFilePath = MCDocumentBasePath + mcdocFileReferencePath;
                                if (fileName == "::mod")
                                {
                                    fileName = "";
                                }
                                MCDocumentFile file;
                                try
                                {
                                    file = await MinecraftLanguageCommunicater.AnalysisMCDocumentFileOrContent(fileEntry, documentFilePath + fileName, cancellationToken);
                                }
                                catch (Exception error)
                                {
                                    // 单个文档失败只记账，不拖垮整张符号表
                                    indexFailures.Add(documentFilePath + fileName + "：" + error.Message);
                                    return;
                                }
                                if (file is null)
                                {
                                    indexFailures.Add(documentFilePath + fileName + "：语言服务器没有返回内容");
                                    return;
                                }
                                // 新求值管线的类型索引；mod.mcdoc 的文件名是空串，这里与旧路径算法保持一致
                                string typeDefBasePath = documentFilePath.TrimEnd(':') + fileName;
                                // 模块 → use 列表：引用解析的第二优先级（绝对路径 → 当前模块 → use 别名 → …）
                                if (file.UsePathList is { Count: > 0 })
                                {
                                    McdocTypes.RegisterUsePaths(typeDefBasePath, file.UsePathList);
                                }
                                if (file.TypeDefList is { Count: > 0 })
                                {
                                    McdocTypes.RegisterAll(file.TypeDefList
                                        .Where(typeDef => typeDef is not null)
                                        .Select(typeDef => (typeDefBasePath + "::" + (typeDef.Name ?? typeDef.MetaTypeName ?? ""), typeDef)));
                                }
                                if (file.RootList is not null)
                                {
                                    //添加文件引用路径
                                    DocumentPathItemMap.TryAdd(documentFilePath + fileName, file.UsePathList);

                                    for (int j = 0; j < file.RootList.Count; j++)
                                    {
                                        //添加DTO模板
                                        string documentItemPath = documentFilePath + fileName + "::" + file.RootList[j].FieldName;
                                        documentFilePath = documentFilePath.TrimEnd(':');
                                        file.RootList[j].Path = new(documentItemPath);
                                        DocumentItemMap.TryAdd(documentItemPath, file.RootList[j]);
                                    }
                                }
                            }
                            finally
                            {
                                progress?.Report((Interlocked.Increment(ref documentDone), documentTotal));
                            }
                        });
                }
                catch (OperationCanceledException)
                {
                    indexFailures.Add("索引超过 90 秒预算，已停止；剩余文档未载入");
                }
                if (configurationNote is not null)
                {
                    indexFailures.Add(configurationNote);
                }
                McdocIndexFailures = indexFailures.ToArray();
                #endregion
            }
            catch (Exception e)
            {
                // 配置坏了也不能退出应用：把原因摆到状态栏，页面照常打开
                McdocIndexFailures = ["mcdoc 索引构建失败：" + e.Message];
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion
    }
}
