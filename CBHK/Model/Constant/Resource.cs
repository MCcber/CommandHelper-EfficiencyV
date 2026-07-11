using CBHK.Domain.DataContext;
using MinecraftLanguageModelLibrary.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace CBHK.Model.Constant
{
    public class Resource
    {
        #region Field
        private string configurationPath = AppDomain.CurrentDomain.BaseDirectory + @"Configuration.json";
        #endregion

        #region Property
        public Dictionary<string, string> GenertorConfiguration { get; private set; }
        public JObject RunningDataObject { get;private set; } = [];
        public string ImageSetDirectoryPath { get; set; } = @"ImageSet";
        public string MCDocumentLeadingPath { get; set; } = "Resource" + Path.DirectorySeparatorChar + "vanilla-mcdoc" + Path.DirectorySeparatorChar + "java";
        public string MCDocumentBasePath { get; set; } = @"::java";
        public string MCDocumentEditorKey { get; set; } = Path.DirectorySeparatorChar + "data";
        /// <summary>
        /// 记录所有文档的路径与对应的DTO对象的映射关系
        /// </summary>
        public ConcurrentDictionary<string, MetaTypeEditorFieldDTO> DocumentItemMap { get; set; } = [];
        /// <summary>
        /// 记录所有文档的路径与对应的use语句映射关系
        /// </summary>
        public ConcurrentDictionary<string, List<string>> DocumentPathItemMap { get; set; } = [];
        #endregion

        #region Method
        public async Task Init()
        {
            if (File.Exists(configurationPath))
            {
                try
                {
                    JObject dataObject = JObject.Parse(File.ReadAllText(configurationPath));
                    string registriesDirectoryPath = dataObject["Registries"].Value<string>();
                    GenertorConfiguration = dataObject["Generator"].ToObject<Dictionary<string, string>>();

                    #region 收集注册表数据
                    if (!string.IsNullOrEmpty(registriesDirectoryPath) && Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + registriesDirectoryPath))
                    {
                        string[] RegistriesDataArray = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + registriesDirectoryPath);
                        //读取现有的所有版本的注册数据
                        for (int i = 0; i < RegistriesDataArray.Length; i++)
                        {
                            string versionString = Path.GetFileNameWithoutExtension(RegistriesDataArray[i]);
                            JObject RegistriesDataObject = JObject.Parse(File.ReadAllText(RegistriesDataArray[i]));
                            //创建版本节点
                            RunningDataObject[versionString] = RegistriesDataObject;
                        }
                    }
                    #endregion

                    #region 全量解析mcdoc目录下所有文档
                    string fullDirectoryPath = AppDomain.CurrentDomain.BaseDirectory + MCDocumentLeadingPath;
                    string[] fileEntryArray = Directory.GetFileSystemEntries(fullDirectoryPath, "*.mcdoc", SearchOption.AllDirectories);
                    string directorySeparatorString = Path.DirectorySeparatorChar.ToString();
                    await Parallel.ForEachAsync(fileEntryArray, async (fileEntry, cancellationToken) =>
                    {
                        if (File.Exists(fileEntry))
                        {
                            string fullDirectoryName = Path.GetDirectoryName(fileEntry);
                            string fileName = "::" + Path.GetFileNameWithoutExtension(fileEntry);
                            string mcdocFilePath = fullDirectoryName.Replace(fullDirectoryPath, "");
                            string mcdocFileReferencePath = mcdocFilePath.Replace(directorySeparatorString, "::");
                            if(fileName == "::mod")
                            {
                                fileName = "";
                            }
                            MCDocumentFile file = await MinecraftLanguageCommunicater.AnalysisMCDocumentFileOrContent(fileEntry);
                            if (file.RootList is not null)
                            {
                                string documentFilePath = MCDocumentBasePath + mcdocFileReferencePath;
                                DocumentPathItemMap.TryAdd(documentFilePath + fileName, file.UsePathList);

                                for (int i = 0; i < file.RootList.Count; i++)
                                {
                                    string documentItemPath = documentFilePath + fileName + "::" + file.RootList[i].FieldName;
                                    file.RootList[i].DocumentItemPath = new(documentItemPath);
                                    DocumentItemMap.TryAdd(documentItemPath, file.RootList[i]);
                                }
                            }
                        }
                    });
                    #endregion
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(0);
                }
            }
        }
        #endregion
    }
}