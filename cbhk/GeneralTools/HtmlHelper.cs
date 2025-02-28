using cbhk.CustomControls;
using cbhk.CustomControls.Interfaces;
using cbhk.CustomControls.JsonTreeViewComponents;
using cbhk.Interface.Json;
using cbhk.Model.Common;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using static cbhk.Model.Common.Enums;

namespace cbhk.GeneralTools
{
    public partial class HtmlHelper(IContainerProvider container)
    {
        #region Field
        public string RootDirectory { get; set; } = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\";

        public ICustomWorldUnifiedPlan plan = null;

        public IJsonItemTool jsonTool = null;

        private List<string> EnumKeyList = ["命名空间ID"];

        [GeneratedRegex(@"\s?\s*\*+\s?\s*{{[nN]bt\sinherit(?<1>[a-z_/|=*\s]+)}}\s?\s*", RegexOptions.IgnoreCase)]
        private static partial Regex GetInheritString();

        [GeneratedRegex(@"(?<=与).+(?=不能同时存在)")]
        private static partial Regex GetMutexKey();

        [GeneratedRegex(@"(?<=\s*\s?\*+;?\s*\s?(如果|若)).+(?=为|是).+")]
        private static partial Regex GetEnumRawKey();

        [GeneratedRegex(@"\[\[\#(?<1>[\u4e00-\u9fff]+)\|(?<2>[\u4e00-\u9fff]+)\]\]")]
        private static partial Regex GetContextKey();

        [GeneratedRegex(@"\[\[(?<1>[a-zA-Z_\u4e00-\u9fff]+)\]\]")]
        private static partial Regex GetEnumKey();

        [GeneratedRegex(@"^\s*\s?\:?\s*\s?(\*+)")]
        private static partial Regex GetLineStarCount();

        [GeneratedRegex(@"{{:物品堆叠组件/[a-z_]+}}", RegexOptions.IgnoreCase)]
        private static partial Regex GetItemComponentKey();

        [GeneratedRegex(@"{{interval\|left=(?<1>\d+)\|right=(?<2>\d+)}}", RegexOptions.IgnoreCase)]
        private static partial Regex GetNumberRange();

        [GeneratedRegex(@"required=1", RegexOptions.IgnoreCase)]
        private static partial Regex GetRequiredKey();

        [GeneratedRegex(@"可以为空", RegexOptions.IgnoreCase)]
        private static partial Regex GetIsCanNullableKey();

        [GeneratedRegex(@"默认为\{\{cd\|(?<1>[a-z_]+)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultStringValue();

        [GeneratedRegex(@"默认为(?<1>\d)+", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultNumberValue();

        [GeneratedRegex(@"默认为\{\{cd\|(?<1>true|false)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultBoolValue();

        [GeneratedRegex(@"(?<=默认为<code>)(?<1>[a-z_]+)(?=</code>)", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultEnumValue();

        [GeneratedRegex(@"(?<=<code>)(?<1>[a-z_]+)(?=</code>)")]
        private static partial Regex GetEnumValueMode1();

        [GeneratedRegex(@"\{\{cd\|(?<1>[a-z:_]+)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetEnumValueMode2();

        [GeneratedRegex(@"\s?\s*=+\s?\s*([\u4e00-\u9fffa-z_]+)\s?\s*=+\s?\s*", RegexOptions.IgnoreCase)]
        private static partial Regex GetContextFileMarker();

        [GeneratedRegex(@"={3,4}\s*\s?(minecraft\:[a-z_]+)\s*\s?={3,4}", RegexOptions.IgnoreCase)]
        private static partial Regex GetEnumTypeKeywords();

        public List<string> ProgressClassList = ["treeview"];

        private IContainerProvider _container = container;
        #endregion

        /// <summary>
        /// 把VSC排版后的格式重新整理(有些应该归为一行的被排版分割为了多行)
        /// </summary>
        /// <param name="target">需要被排版的链表</param>
        /// <returns></returns>
        public List<string> TypeSetting(List<string> target)
        {
            for (int j = 0; j < target.Count; j++)
            {
                if (target[j].Replace("*", "").Trim().Length == 0 || (j + 1 < target.Count && !target[j + 1].StartsWith('*')))
                {
                    int nextStarLine = j + 1;
                    while (nextStarLine < target.Count && !target[nextStarLine].TrimStart().StartsWith('*'))
                    {
                        target[j] += target[j + 1];
                        target.RemoveAt(j + 1);
                    }
                }
            }
            return target;
        }

        private void HandlingDependencyFile(string[] target)
        {
            HtmlDocument htmlDocument = new();
            for (int i = 0; i < target.Length; i++)
            {
                if (File.Exists(target[i]))
                {
                    string[] fileArrayContent = File.ReadAllLines(target[i]);
                    string fileContent = File.ReadAllText(target[i]);
                    htmlDocument.LoadHtml(fileContent);
                    List<HtmlNode> treeviewDivs = [.. htmlDocument.DocumentNode.SelectNodes("//div[@class='treeview']")];

                    #region 执行树视图节点的生成
                    if (treeviewDivs is not null)
                    {
                        #region 直接覆盖InnerHtml属性会导致内容错误，所以这里直接用链表来提取正确的结构数据
                        List<string> nodeContent = [.. treeviewDivs[0].InnerHtml.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)];
                        nodeContent = TypeSetting(nodeContent);
                        #endregion

                        #region 执行解析、分析是否需要被添加为依赖项
                        Match mainContextFileMarker = GetContextFileMarker().Match(fileArrayContent[0]);
                        Match subContextFileMarker = GetContextFileMarker().Match(fileArrayContent[1]);
                        string contextFileMarker = "#" + mainContextFileMarker.Value.Replace("=", "").Trim() + (subContextFileMarker.Success ? "|" + subContextFileMarker.Value.Replace("=", "").Trim() : "");
                        if(contextFileMarker == "#")
                        {
                            contextFileMarker = '#' + target[i].Replace(RootDirectory,"").Replace(".wiki","") + "";
                        }

                        if (treeviewDivs.Count == 1)
                        {
                            plan.DependencyItemList[contextFileMarker] = nodeContent;
                        }
                        else
                        {
                            for (int j = 0; j < treeviewDivs.Count; j++)
                            {
                                int enumKeyIndex = treeviewDivs[j].Line - 1;
                                Match enumMatch = GetEnumTypeKeywords().Match(fileArrayContent[enumKeyIndex]);
                                while (!enumMatch.Success)
                                {
                                    if (enumKeyIndex > 0)
                                    {
                                        enumKeyIndex--;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                    enumMatch = GetEnumTypeKeywords().Match(fileArrayContent[enumKeyIndex]);
                                }
                                Dictionary<string, List<string>> keyValuePairs = [];
                                List<string> targetRawStringList = fileArrayContent.Skip(treeviewDivs[j].Line).Take(treeviewDivs[j].EndNode.Line - 1 - treeviewDivs[j].Line).ToList();
                                targetRawStringList = TypeSetting(targetRawStringList);
                                bool NoKey = plan.EnumCompoundDataDictionary.TryAdd(contextFileMarker, keyValuePairs);
                                if (NoKey)
                                {
                                    keyValuePairs.Add(enumMatch.Value.Replace("=", "").Trim(), targetRawStringList);
                                }
                                else
                                {
                                    plan.EnumCompoundDataDictionary[contextFileMarker].Add(enumMatch.Value.Replace("=", "").Trim(), targetRawStringList);
                                }
                            }
                        }
                        #endregion
                    }
                    #endregion
                }
            }
        }

        /// <summary>
        /// 在Wiki文档被解析完毕后由上到下处理一次复合控件的键入外观与键入逻辑
        /// </summary>
        private void HandlingTheTypingAppearanceOfCompositeItemList(IEnumerable<JsonTreeViewItem> list)
        {
            foreach (var element in list)
            {
                if (element is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                {
                    foreach (var childString in compoundJsonTreeViewItem.ChildrenStringList)
                    {
                        #region 处理键入外观
                        Match contentMatch = GetContextKey().Match(childString);
                        if (contentMatch.Success)
                        {
                            string key = '#' + contentMatch.Groups[1].Value + '|' + contentMatch.Groups[2].Value;
                            CompoundJsonTreeViewItem subCompoundItem = new(plan, jsonTool, _container)
                            {
                                LayerCount = compoundJsonTreeViewItem.LayerCount,
                                DataType = DataType.CustomCompound,
                                AddOrSwitchElementButtonVisibility = Visibility.Visible,
                                IsExpanded = false
                            };
                            if (plan.DependencyItemList.TryGetValue(key, out List<string> dependencyList))
                            {
                                foreach (var dependencyItem in dependencyList)
                                {
                                    List<string> NBTFeatureList = GetHeadTypeAndKeyList(dependencyItem);
                                    foreach (var NBTFeatureItem in NBTFeatureList)
                                    {
                                        if (NBTFeatureItem.Contains('\'') && NBTFeatureItem.Contains('<'))
                                        {
                                            MatchCollection enumValueList1 = GetEnumValueMode1().Matches(NBTFeatureItem);
                                            MatchCollection enumValueList2 = GetEnumValueMode2().Matches(NBTFeatureItem);
                                            int enumMode1Count = GetEnumValueMode1().Matches(NBTFeatureItem).Count;
                                            int enumMode2Count = GetEnumValueMode2().Matches(NBTFeatureItem).Count;
                                            if (enumMode1Count + enumMode2Count > 0)
                                            {
                                                subCompoundItem.EnumBoxVisibility = Visibility.Visible;
                                                subCompoundItem.EnumItemsSource.AddRange(enumValueList1.Select(item => new TextComboBoxItem()
                                                {
                                                    Text = item.Value
                                                }));
                                                subCompoundItem.EnumItemsSource.AddRange(enumValueList2.Select(item => new TextComboBoxItem()
                                                {
                                                    Text = item.Value
                                                }));

                                            }
                                            else
                                            {
                                                compoundJsonTreeViewItem.InputBoxVisibility = Visibility.Visible;
                                            }
                                            compoundJsonTreeViewItem.SwitchButtonIcon = compoundJsonTreeViewItem.MinusIcon;
                                            compoundJsonTreeViewItem.SwitchButtonColor = compoundJsonTreeViewItem.MinusColor;
                                            compoundJsonTreeViewItem.Children.Add(subCompoundItem);
                                            subCompoundItem.Parent = compoundJsonTreeViewItem;
                                        }
                                    }
                                }
                            }
                            else
                            if (plan.TranslateDictionary.TryGetValue(key, out string targetKey1) && plan.DependencyItemList.TryGetValue(targetKey1, out List<string> subChildrenList))
                            {
                                subCompoundItem.ChildrenStringList = subChildrenList;
                                for (int i = 0; i < subChildrenList.Count; i++)
                                {
                                    List<string> subNBTFeatureList = GetHeadTypeAndKeyList(subChildrenList[i]);
                                    if (subNBTFeatureList[^1].Contains('\'') && subNBTFeatureList[^1].Contains('<'))
                                    {
                                        subCompoundItem.InputBoxVisibility = Visibility.Visible;
                                        subCompoundItem.SwitchButtonIcon = compoundJsonTreeViewItem.PlusIcon;
                                        subCompoundItem.SwitchButtonColor = compoundJsonTreeViewItem.PlusColor;
                                        compoundJsonTreeViewItem.Children.Add(subCompoundItem);
                                        subCompoundItem.Parent = compoundJsonTreeViewItem;
                                        compoundJsonTreeViewItem.AddOrSwitchElementButtonVisibility = Visibility.Collapsed;
                                    }
                                }
                            }
                            else
                            if (plan.TranslateDictionary.TryGetValue(key, out string targetKey2) && plan.EnumCompoundDataDictionary.TryGetValue(targetKey2, out Dictionary<string, List<string>> dictionary))
                            {
                                subCompoundItem.EnumBoxVisibility = Visibility.Visible;
                                subCompoundItem.EnumItemsSource.AddRange(dictionary.Keys.Select(item =>
                                {
                                    return new TextComboBoxItem()
                                    {
                                        Text = item
                                    };
                                }));
                                compoundJsonTreeViewItem.SwitchButtonIcon = compoundJsonTreeViewItem.MinusIcon;
                                compoundJsonTreeViewItem.SwitchButtonColor = compoundJsonTreeViewItem.MinusColor;
                                compoundJsonTreeViewItem.Children.Add(subCompoundItem);
                                subCompoundItem.Parent = compoundJsonTreeViewItem;  
                            }
                        }
                        #endregion
                        #region 处理键入逻辑

                        #endregion
                    }
                }
                #region 处理冲突节点

                #endregion
            }
        }

        public List<string> GetHeadTypeAndKeyList(string target)
        {
            List<string> result = [];
            string RemoveIrrelevantString = target.Replace("}}{{nbt", "").Replace("}}{{Nbt","");
            int nbtFeatureStartIndex = RemoveIrrelevantString.IndexOf("{{");
            int nbtFeatureEndIndex = RemoveIrrelevantString.IndexOf("}}");
            if (RemoveIrrelevantString.Contains('{'))
            {
                result.AddRange(RemoveIrrelevantString[(nbtFeatureStartIndex + 2)..nbtFeatureEndIndex].Split('|'));
                result.RemoveAt(0);
            }
            return result;
        }

        public JsonTreeViewDataStructure AnalyzeHTMLData(string directoryPath)
        {
            JsonTreeViewDataStructure Result = new();
            HtmlDocument htmlDocument = new();

            #region 处理主结构的引用文件
            foreach (var file in Directory.GetFiles(directoryPath, "*.wiki"))
            {
                string wikiData = File.ReadAllText(file);
                string[] wikiLines = File.ReadAllLines(file);

                //非入口文件
                bool IsNotMainFile = Path.GetFileNameWithoutExtension(file) != "main";

                #region 生成html文档节点集
                htmlDocument.LoadHtml(wikiData);
                List<HtmlNode> treeviewDivs = [.. htmlDocument.DocumentNode.SelectNodes("//div[@class='treeview']")];
                #endregion

                #region 执行树视图节点的生成
                if (treeviewDivs is not null && IsNotMainFile)
                {
                    #region 直接覆盖InnerHtml属性会导致内容错误，所以这里直接用链表来提取正确的结构数据
                    List<string> nodeContent = [.. treeviewDivs[0].InnerHtml.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)];
                    nodeContent = TypeSetting(nodeContent);
                    #endregion

                    #region 执行解析、分析是否需要被添加为依赖项
                    Match mainContextFileMarker = GetContextFileMarker().Match(wikiLines[0]);
                    Match subContextFileMarker = GetContextFileMarker().Match(wikiLines[1]);
                    string contextFileMarker = "#" + mainContextFileMarker.Value.Replace("=", "").Trim() + (subContextFileMarker.Success ? "|" + subContextFileMarker.Value.Replace("=", "").Trim() : "");

                    if (treeviewDivs.Count == 1)
                    {
                        plan.DependencyItemList[contextFileMarker] = nodeContent;
                    }
                    else
                    {
                        for (int i = 0; i < treeviewDivs.Count; i++)
                        {
                            int enumKeyIndex = treeviewDivs[i].Line - 1;
                            Match enumMatch = GetEnumTypeKeywords().Match(wikiLines[enumKeyIndex]);
                            while (!enumMatch.Success)
                            {
                                if (enumKeyIndex > 0)
                                {
                                    enumKeyIndex--;
                                }
                                else
                                {
                                    break;
                                }
                                enumMatch = GetEnumTypeKeywords().Match(wikiLines[enumKeyIndex]);
                            }
                            Dictionary<string, List<string>> keyValuePairs = [];
                            List<string> targetRawStringList = wikiLines.Skip(treeviewDivs[i].Line).Take(treeviewDivs[i].EndNode.Line - 1 - treeviewDivs[i].Line).ToList();
                            targetRawStringList = TypeSetting(targetRawStringList);
                            bool NoKey = plan.EnumCompoundDataDictionary.TryAdd(contextFileMarker, keyValuePairs);
                            if (NoKey)
                            {
                                keyValuePairs.Add(enumMatch.Value.Replace("=", "").Trim(), targetRawStringList);
                            }
                            else
                            {
                                plan.EnumCompoundDataDictionary[contextFileMarker].Add(enumMatch.Value.Replace("=", "").Trim(), targetRawStringList);
                            }
                        }
                    }
                    #endregion
                }
                #endregion
            }
            #endregion

            #region 处理依赖文件和目录
            string dependencyDirectoryListPath = Path.Combine(RootDirectory + plan.RootDirectory + plan.CurrentVersion.Text, "dependencyDirectoryList.json");
            string dependencyFileListPath = Path.Combine(RootDirectory + plan.RootDirectory + plan.CurrentVersion.Text, "dependencyFileList.json");

            JArray directoryArray = JArray.Parse(File.ReadAllText(dependencyDirectoryListPath));
            JArray fileArray = JArray.Parse(File.ReadAllText(dependencyFileListPath));
            foreach (var entry in directoryArray)
            {
                string[] fileList = Directory.GetFiles(RootDirectory + entry.Value<string>());
                HandlingDependencyFile(fileList);
            }
            string[] directFileList = fileArray.Select(item => RootDirectory + item.Value<string>().Replace("/", "\\")).ToArray();
            HandlingDependencyFile(directFileList);
            #endregion

            #region 处理主文件
            string mainFilePath = directoryPath + "\\main.wiki";
            if (File.Exists(mainFilePath))
            {
                string wikiData = File.ReadAllText(mainFilePath);
                string[] wikiLines = File.ReadAllLines(mainFilePath);

                #region 生成html文档节点集
                htmlDocument.LoadHtml(wikiData);
                List<HtmlNode> treeviewDivs = [.. htmlDocument.DocumentNode.SelectNodes("//div[@class='treeview']")];
                #endregion

                #region 执行树视图节点的生成
                if (treeviewDivs is not null)
                {
                    List<string> nodeContent = [.. treeviewDivs[0].InnerHtml.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)];
                    nodeContent = TypeSetting(nodeContent);

                    JsonTreeViewDataStructure resultData = GetTreeViewItemResult(new(), nodeContent, 1);
                    Result.Result.AddRange(resultData.Result);
                    Result.ResultString.Append(resultData.ResultString);
                    HandlingTheTypingAppearanceOfCompositeItemList(Result.Result);
                }
                #endregion
            }
            #endregion

            return Result;
        }

        /// <summary>
        /// 转换Wiki文档为树视图节点
        /// </summary>
        /// <param name="result">返回结果</param>
        /// <param name="nodeList">需处理的节点文本列表</param>
        /// <param name="layerCount">层级</param>
        /// <param name="parent">父节点</param>
        /// <param name="previous">前一个节点</param>
        /// <param name="previousStarCount">前一个节点星号数量</param>
        /// <returns></returns>
        public JsonTreeViewDataStructure GetTreeViewItemResult(JsonTreeViewDataStructure result, List<string> nodeList, int layerCount, string currentReferenceKey = "", CompoundJsonTreeViewItem parent = null, JsonTreeViewItem previous = null, int previousStarCount = 1, bool isAddToParent = false)
        {
            //遍历找到的div标签内容集合
            for (int i = 0; i < nodeList.Count; i++)
            {
                #region Field
                bool HaveBranches = false;
                string currentNodeDataType = "";
                string currentNodeKey = "";
                bool IsSimpleItem = true;

                //提取引用节点所代指的文档并将其内容插入到当前节点列表中
                Match inheritMatch = GetInheritString().Match(nodeList[i]);
                if (inheritMatch.Success && plan.DependencyItemList.TryGetValue("#Inherit" + inheritMatch.Groups[1].Value.Replace("/", "\\"), out List<string> targetInheritList))
                {
                    nodeList.InsertRange(i, targetInheritList);
                }

                Match itemComponentMatch = GetItemComponentKey().Match(nodeList[i]);
                List<string> NBTFeatureList = GetHeadTypeAndKeyList(nodeList[i]);

                #region 去除用于Wiki页面显示的一些UI标记
                if (NBTFeatureList is not null && NBTFeatureList.Count > 0)
                {
                    for (int j = NBTFeatureList.Count - 1; j > 0; j--)
                    {
                        if (NBTFeatureList[j].Contains('='))
                        {
                            NBTFeatureList.RemoveAt(j);
                        }
                    }
                }
                #endregion

                bool IsExplanationNode = NBTFeatureList.Count > 1;
                bool IsCurrentOptionalNode = !GetRequiredKey().Match(nodeList[i]).Success;
                bool IsHaveNextNode = i < nodeList.Count - 1;
                bool IsNextOptionalNode = false;

                bool isHaveNameSpaceKey = EnumKeyList.Where(enumItem => { return nodeList[i].Contains(enumItem); }).Any();
                MatchCollection EnumCollectionMode1 = GetEnumValueMode1().Matches(nodeList[i]);
                MatchCollection EnumCollectionMode2 = GetEnumValueMode2().Matches(nodeList[i]);
                bool isBoolKey = (EnumCollectionMode1.Count > 0 && (EnumCollectionMode1[0].Groups[1].Value == "false" || EnumCollectionMode1[0].Groups[1].Value == "true")) || (EnumCollectionMode2.Count > 0 && (EnumCollectionMode2[0].Groups[1].Value == "false" || EnumCollectionMode2[0].Groups[1].Value == "true"));
                bool isSimpleDataType = true;
                if (NBTFeatureList.Count > 0)
                {
                    isSimpleDataType = ((NBTFeatureList[0] != "array" && NBTFeatureList[0] != "list" && NBTFeatureList[0] != "compound" && NBTFeatureList.Count < 3) || isBoolKey) && EnumCollectionMode1.Count < 2 && EnumCollectionMode2.Count < 2;
                }
                Match contextMatch = GetContextKey().Match(nodeList[i]);
                Match EnumMatch = GetEnumKey().Match(nodeList[i]);
                bool isEnumIDList = false;
                if (EnumMatch.Success)
                {
                    isEnumIDList = plan.EnumIDDictionary.ContainsKey(EnumMatch.Groups[1].Value);
                }

                Match DefaultEnumValueMatch = GetDefaultEnumValue().Match(nodeList[i]);
                Match DefaultNumberValueMatch = GetDefaultNumberValue().Match(nodeList[i]);
                Match DefaultBoolValueMatch = GetDefaultBoolValue().Match(nodeList[i]);
                Match DefaultStringValueMatch = GetDefaultStringValue().Match(nodeList[i]);
                #endregion

                #region 判断是否跳过本次处理
                if (NBTFeatureList.Count == 0 && !itemComponentMatch.Success)
                {
                    continue;
                }

                if (nodeList[i].Trim().Length == 0)
                {
                    continue;
                }

                int lastCurlyBracesIndex = nodeList[i].LastIndexOf('}');

                if (lastCurlyBracesIndex > -1 && nodeList[i][(lastCurlyBracesIndex + 1)..].Trim() == "根标签")
                {
                    continue;
                }

                if (!nodeList[i].Contains('{'))
                {
                    if (result.ResultString.ToString().TrimEnd(['\r', '\n']).EndsWith(','))
                    {
                        int index = result.ResultString.ToString().LastIndexOf(',');
                        result.ResultString.Remove(result.ResultString.Length - 5, 1);
                    }
                    continue;
                }
                #endregion

                #region 获取当前行星号数量
                Match starMatch = GetLineStarCount().Match(nodeList[i]);
                int starCount = starMatch.Value.Trim().Length;
                #endregion

                #region 声明当前节点
                JsonTreeViewItem item = new()
                {
                    LayerCount = layerCount,
                    IsCanBeDefaulted = IsCurrentOptionalNode
                };
                #endregion

                #region 获取当前节点的数据类型和键
                currentNodeDataType = NBTFeatureList[0];
                if (NBTFeatureList.Count > 1)
                {
                    currentNodeKey = NBTFeatureList[^1];
                }
                #endregion

                #region 确认是否能够进入解析流程
                bool IsCanBeAnalyzed = currentNodeDataType.Length > 0;
                #endregion

                #region 处理两大值类型
                if (IsCanBeAnalyzed)
                {
                    #region 判断是否为复合节点
                    if (isEnumIDList || isHaveNameSpaceKey || !isSimpleDataType)
                    {
                        item = new CompoundJsonTreeViewItem(plan, jsonTool, _container)
                        {
                            IsCanBeDefaulted = IsCurrentOptionalNode
                        };
                        IsSimpleItem = false;
                    }
                    #endregion

                    #region 判断是否捕获多组信息
                    if (NBTFeatureList is not null && NBTFeatureList.Count > 2 && currentNodeDataType.Length > 0 && currentNodeKey.Length > 0)
                    {
                        if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                        {
                            for (int j = 0; j < NBTFeatureList.Count - 1; j++)
                            {
                                compoundJsonTreeViewItem.ValueTypeList.Add(new TextComboBoxItem()
                                {
                                    Text = NBTFeatureList[j][0].ToString().ToUpper() + NBTFeatureList[j][1..]
                                });
                            }
                            compoundJsonTreeViewItem.CurrentValueType ??= compoundJsonTreeViewItem.ValueTypeList[0];
                            item.DataType = compoundJsonTreeViewItem.DataType = DataType.MultiType;
                            if (!IsCurrentOptionalNode)
                            {
                                result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + currentNodeKey + "\": ");
                                switch (NBTFeatureList[0])
                                {
                                    case "bool":
                                        {
                                            bool defaultValue = false;
                                            if (DefaultBoolValueMatch.Success)
                                            {
                                                defaultValue = bool.Parse(DefaultBoolValueMatch.Groups[1].Value);
                                            }
                                            result.ResultString.Append(defaultValue.ToString().ToLower());
                                            compoundJsonTreeViewItem.Value = defaultValue;
                                            compoundJsonTreeViewItem.BoolButtonVisibility = Visibility.Visible;
                                            break;
                                        }
                                    case "byte":
                                    case "short":
                                    case "int":
                                    case "float":
                                    case "double":
                                    case "long":
                                    case "decimal":
                                        {
                                            int defaultValue = 0;
                                            if(DefaultNumberValueMatch.Success)
                                            {
                                                defaultValue = int.Parse(DefaultNumberValueMatch.Groups[1].Value);
                                            }
                                            result.ResultString.Append(defaultValue.ToString());
                                            compoundJsonTreeViewItem.Value = defaultValue;
                                            compoundJsonTreeViewItem.InputBoxVisibility = Visibility.Visible;
                                            break;
                                        }
                                    case "string":
                                        {
                                            string defaultValue = "";
                                            if(DefaultStringValueMatch.Success)
                                            {
                                                defaultValue = DefaultStringValueMatch.Groups[1].Value;
                                            }
                                            result.ResultString.Append("\"" + defaultValue + "\"");
                                            compoundJsonTreeViewItem.Value = defaultValue;
                                            compoundJsonTreeViewItem.InputBoxVisibility = Visibility.Visible;
                                            break;
                                        }
                                    case "compound":
                                        {
                                            result.ResultString.Append('{');
                                            break;
                                        }
                                    case "list":
                                        {
                                            result.ResultString.Append("[]");
                                            break;
                                        }
                                }
                                if (NBTFeatureList[0].Contains("array"))
                                {
                                    result.ResultString.Append('[');
                                }
                            }
                            compoundJsonTreeViewItem.SwitchBoxVisibility = Visibility.Visible;
                            compoundJsonTreeViewItem.AddOrSwitchElementButtonVisibility = Visibility.Visible;
                            compoundJsonTreeViewItem.SwitchButtonColor = compoundJsonTreeViewItem.PlusColor;
                            compoundJsonTreeViewItem.SwitchButtonIcon = compoundJsonTreeViewItem.PlusIcon;
                        }
                    }
                    #endregion

                    #region 处理值类型
                    if (IsSimpleItem)
                    {
                        switch (currentNodeDataType)
                        {
                            case "bool":
                            case "boolean":
                                {
                                    item.DataType = DataType.Bool;
                                    item.Plan = plan;
                                    break;
                                }
                            case "byte":
                            case "short":
                            case "int":
                            case "float":
                            case "double":
                            case "long":
                            case "decimal":
                            case "string":
                                {
                                    object dataTypes = DataType.None;
                                    bool parseResult = Enum.TryParse(typeof(DataType), currentNodeDataType[0].ToString().ToUpper() + currentNodeDataType[1..], out dataTypes);
                                    if (parseResult)
                                    {
                                        item.DataType = (DataType)dataTypes;
                                    }
                                    item.Plan = plan;
                                    break;
                                }
                        }

                        switch (item.DataType)
                        {
                            case DataType.Bool:
                                {
                                    item.Key = currentNodeKey;
                                    if (currentNodeKey.Length > 0)
                                    {
                                        item.DisplayText = string.Join(' ', currentNodeKey.Split('_').Select(item => item[0].ToString().ToUpper() + item[1..]));
                                    }
                                    else
                                    {
                                        item.DisplayText = "Entry";
                                    }
                                    item.BoolButtonVisibility = Visibility.Visible;

                                    if (!IsCurrentOptionalNode && currentNodeKey.Length > 0)
                                    {
                                        result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + currentNodeKey.ToLower() + "\": ");
                                    }
                                    else
                                    {
                                        item.IsFalse = item.IsTrue = false;
                                    }

                                    if (DefaultBoolValueMatch.Success && !IsCurrentOptionalNode && currentNodeKey.Length > 0)
                                    {

                                        if (bool.TryParse(DefaultBoolValueMatch.Groups[1].Value, out bool defaultValue) && !IsCurrentOptionalNode)
                                        {
                                            item.Value = item.DefaultValue = defaultValue;
                                            if (item.Value)
                                            {
                                                item.IsTrue = true;
                                                item.IsFalse = false;
                                            }
                                        }
                                        else
                                        {
                                            item.IsTrue = item.IsFalse = false;
                                        }

                                        if (!IsCurrentOptionalNode)
                                        {
                                            result.ResultString.Append(DefaultBoolValueMatch.Groups[1].Value.ToLower());
                                        }
                                    }
                                    else
                                    if(!IsCurrentOptionalNode || isAddToParent)
                                    {
                                        item.RemoveElementButtonVisibility = Visibility.Visible;
                                        item.Value = DefaultBoolValueMatch.Success && bool.Parse(DefaultBoolValueMatch.Groups[1].Value);
                                        item.IsCanBeDefaulted = false;
                                        result.ResultString.Append(new string(' ', item.LayerCount * 2) + (DefaultBoolValueMatch.Success ? DefaultBoolValueMatch.Groups[1].Value.ToString().ToLower() : "false"));
                                    }
                                    item.Plan = plan;
                                    item.JsonItemTool = jsonTool;
                                    break;
                                }
                            case DataType.String:
                                {
                                    item.Key = currentNodeKey;
                                    if (currentNodeKey.Length > 0)
                                    {
                                        item.DisplayText = string.Join(' ', currentNodeKey.Split('_').Select(item => item[0].ToString().ToUpper() + item[1..]));
                                    }
                                    else
                                    {
                                        item.DisplayText = "Entry";
                                    }
                                    item.InputBoxVisibility = Visibility.Visible;

                                    if(!IsCurrentOptionalNode && currentNodeKey.Length > 0)
                                    {
                                        result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + currentNodeKey.ToLower() + "\": ");
                                    }

                                    if (DefaultStringValueMatch.Success && !IsCurrentOptionalNode && currentNodeKey.Length > 0)
                                    {
                                        item.Value = item.DefaultValue = "\"" + DefaultStringValueMatch.Groups[1].Value + "\"";
                                            result.ResultString.Append("\"" + DefaultStringValueMatch.Groups[1].Value.ToLower() + "\"");
                                    }
                                    else
                                    if(currentNodeKey.Length == 0)
                                    {
                                        item.Plan = plan;
                                        item.RemoveElementButtonVisibility = Visibility.Visible;
                                        item.JsonItemTool = jsonTool;
                                        item.Value = "";
                                        item.IsCanBeDefaulted = false;
                                        result.ResultString.Append(new string(' ',item.LayerCount * 2) + "\"\"");
                                    }
                                    break;
                                }
                            case DataType.Byte:
                            case DataType.Short:
                            case DataType.Int:
                            case DataType.Float:
                            case DataType.Double:
                            case DataType.Decimal:
                            case DataType.Long:
                                {
                                    item.Key = currentNodeKey;
                                    if (currentNodeKey.Length > 0)
                                    {
                                        item.DisplayText = string.Join(' ', currentNodeKey.Split('_').Select(item => item[0].ToString().ToUpper() + item[1..]));
                                    }
                                    else
                                    {
                                        item.DisplayText = "Entry";
                                    }
                                    item.InputBoxVisibility = Visibility.Visible;

                                    if (!IsCurrentOptionalNode && currentNodeKey.Length > 0)
                                    {
                                        result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + currentNodeKey.ToLower() + "\": ");
                                    }

                                    if (DefaultNumberValueMatch.Success && !IsCurrentOptionalNode && currentNodeKey.Length > 0)
                                    {
                                        if (!IsCurrentOptionalNode && decimal.TryParse(DefaultNumberValueMatch.Groups[1].Value, out decimal defaultDecimalValue))
                                        {
                                            item.Value = item.DefaultValue = defaultDecimalValue;
                                        }

                                        if (!IsCurrentOptionalNode && DefaultNumberValueMatch.Success)
                                        {
                                            result.ResultString.Append(DefaultNumberValueMatch.Groups[1].Value.ToString().ToLower());
                                        }
                                    }
                                    else
                                    {
                                        item.Plan = plan;
                                        item.RemoveElementButtonVisibility = Visibility.Visible;
                                        item.JsonItemTool = jsonTool;
                                        item.Value = DefaultNumberValueMatch.Success ? decimal.Parse(DefaultNumberValueMatch.Groups[1].Value) : 0;
                                        item.IsCanBeDefaulted = false;
                                        result.ResultString.Append(new string(' ', item.LayerCount * 2) + (DefaultNumberValueMatch.Success ? decimal.Parse(DefaultNumberValueMatch.Groups[1].Value) : '0'));
                                    }
                                    break;
                                }
                        }

                        if (previous is not null && previous.LayerCount == layerCount)
                        {
                            previous.Next = item;
                            item.Previous = previous;
                        }
                    }
                    #endregion

                    #region 处理复合类型
                    if (item is CompoundJsonTreeViewItem CurrentCompoundItem)
                    {
                        #region 设置key
                        if (CurrentCompoundItem.Key.Length == 0 && currentNodeKey.Length > 0)
                        {
                            CurrentCompoundItem.Key = currentNodeKey;
                        }
                        #endregion

                        #region 处理不同行为的复合型数据

                        #region 确定数据类型
                        CurrentCompoundItem.AddOrSwitchElementButtonVisibility = NBTFeatureList[0] == "compound" || NBTFeatureList[0] == "list" || NBTFeatureList[0].Contains("array") ? Visibility.Visible : Visibility.Collapsed;
                        if (CurrentCompoundItem.AddOrSwitchElementButtonVisibility is Visibility.Visible)
                        {
                            if (NBTFeatureList[0] == "compound")
                            {
                                CurrentCompoundItem.ElementButtonTip = "折叠";
                            }
                            CurrentCompoundItem.SwitchButtonIcon = CurrentCompoundItem.PlusIcon;
                            CurrentCompoundItem.SwitchButtonColor = CurrentCompoundItem.PlusColor;
                            CurrentCompoundItem.PressedSwitchButtonColor = CurrentCompoundItem.PressedPlusColor;
                        }
                        if (currentNodeDataType == "compound")
                        {
                            if (IsCurrentOptionalNode)
                            {
                                CurrentCompoundItem.DataType = DataType.OptionalCompound;
                            }

                            if (currentNodeKey.Contains('\''))
                            {
                                IsCurrentOptionalNode = false;
                                CurrentCompoundItem.DataType = DataType.Compound;
                                CurrentCompoundItem.IsCanBeDefaulted = false;
                                CurrentCompoundItem.Key = CurrentCompoundItem.DisplayText = currentReferenceKey;
                                CurrentCompoundItem.RemoveElementButtonVisibility = Visibility.Visible;
                            }
                        }
                        else
                        if (currentNodeDataType == "list")
                        {
                            CurrentCompoundItem.DataType = DataType.List;
                            if(currentNodeKey.Length == 0)
                            {
                                CurrentCompoundItem.RemoveElementButtonVisibility = Visibility.Visible;
                                CurrentCompoundItem.DisplayText = "Entry";
                                CurrentCompoundItem.IsCanBeDefaulted = false;
                            }
                        }
                        else//数组会有数据类型的区分，所以这里用包含而不是等于
                        if (currentNodeDataType.Contains("array"))
                        {
                            CurrentCompoundItem.DataType = DataType.Array;
                        }
                        else
                        if (currentNodeDataType == "any")
                        {
                            parent.EnumBoxVisibility = Visibility.Visible;
                            CurrentCompoundItem.DataType = DataType.CustomCompound;
                        }
                        #endregion

                        #region 更新非可选复合型节点的文本值与层数
                        if (currentNodeKey.Length > 0 && CurrentCompoundItem.DisplayText.Length == 0)
                        {
                            CurrentCompoundItem.DisplayText = string.Join(' ', currentNodeKey.Split('_').Select(item => item[0].ToString().ToUpper() + item[1..]));
                        }
                        CurrentCompoundItem.LayerCount = layerCount;
                        if (CurrentCompoundItem.Key.Length > 0 && !IsCurrentOptionalNode && NBTFeatureList.Count < 3)
                        {
                            if (NBTFeatureList[0].Contains("list") || NBTFeatureList[0].Contains("array"))
                            {
                                result.ResultString.Append(new string(' ', CurrentCompoundItem.LayerCount * 2) + "\"" + currentNodeKey + "\": [" + (NBTFeatureList[0].Contains("list") ? ']' : ""));
                            }
                            else
                            if (NBTFeatureList[0].Contains("compound"))
                            {
                                result.ResultString.Append(new string(' ', CurrentCompoundItem.LayerCount * 2) + "\"" + CurrentCompoundItem.Key + "\": {");
                            }
                        }
                        else
                        if(CurrentCompoundItem.Key.Length == 0 && NBTFeatureList[0].Contains("list"))
                        {
                            CurrentCompoundItem.IsCanBeDefaulted = false;
                            result.ResultString.Append(new string(' ', CurrentCompoundItem.LayerCount * 2) + "[]");
                        }
                        #endregion

                        #endregion

                        #region 处理枚举
                        int EnumKeyCount = EnumKeyList.Where(item => nodeList[i].Contains(item)).Count();
                        if (DefaultEnumValueMatch.Success)
                        {
                            item.DefaultValue = item.Value = "\"" + DefaultEnumValueMatch.Value + "\"";
                        }
                        if (NBTFeatureList[0] == "string" && (EnumKeyCount > 0 || EnumCollectionMode1.Count > 0 || EnumCollectionMode2.Count > 0 || EnumMatch.Success))
                        {
                            CurrentCompoundItem.DataType = DataType.Enum;
                            CurrentCompoundItem.EnumBoxVisibility = Visibility.Visible;
                            #region 处理Key与外观
                            if (NBTFeatureList[0] == "string")
                            {
                                List<string> enumSource = [];
                                enumSource.Add("- unset -");
                                if(plan.EnumIDDictionary.TryGetValue(EnumMatch.Groups[1].Value,out List<string> targetEnumIDList) && targetEnumIDList is not null && targetEnumIDList.Count > 0)
                                {
                                    enumSource.AddRange(targetEnumIDList);
                                }
                                enumSource.AddRange([.. EnumCollectionMode1.Select(enum1 => { return enum1.Groups[1].Value; })]);
                                enumSource.AddRange([.. EnumCollectionMode2.Select(enum1 => { return enum1.Groups[1].Value; })]);
                                enumSource = [..enumSource.Distinct()];
                                enumSource.Sort();
                                CurrentCompoundItem.EnumItemsSource.AddRange(enumSource.Select(enum1 =>
                                {
                                    return new TextComboBoxItem() { Text = enum1 };
                                }));
                                if (CurrentCompoundItem.EnumItemsSource.Count > 0)
                                {
                                    CurrentCompoundItem.SelectedEnumItem = CurrentCompoundItem.EnumItemsSource[0];
                                }

                                string setString = CurrentCompoundItem.SelectedEnumItem.Text.Trim() != "- unset -" ? CurrentCompoundItem.SelectedEnumItem.Text :"";
                                if (!CurrentCompoundItem.IsCanBeDefaulted)
                                {
                                    result.ResultString.Append(new string(' ', layerCount * 2) +
                                        "\"" + currentNodeKey.ToLower() + "\": " + (IsCurrentOptionalNode ? "\"\"" : "\"" + setString + "\""));
                                }
                            }
                            #endregion
                        }
                        #endregion

                        #region 处理CustomCompound
                        if (previous is CompoundJsonTreeViewItem)
                        {
                            string presetKey = (previous as CompoundJsonTreeViewItem).DataType.ToString().ToLower() + ':' + previous.Key + '.' + CurrentCompoundItem.DataType.ToString().ToLower() + ':' + CurrentCompoundItem.Key;
                            if (plan.PresetCustomCompoundKeyDictionary.TryGetValue(presetKey, out string targetDependencyKey))
                            {
                                if (plan.EnumCompoundDataDictionary.TryGetValue(targetDependencyKey, out Dictionary<string, List<string>> targetEnumList))
                                {
                                    previous.IsCanBeDefaulted = false;
                                    result.ResultString.Append(new string(' ',previous.LayerCount * 2) + "\"" + previous.Key + "\": \"\",\r\n");
                                    (previous as CompoundJsonTreeViewItem).EnumBoxVisibility = Visibility.Visible;
                                    (previous as CompoundJsonTreeViewItem).EnumItemsSource.AddRange(targetEnumList.Keys.Select(item =>
                                    {
                                        return new TextComboBoxItem()
                                        {
                                            Text = item
                                        };
                                    }));
                                    (previous as CompoundJsonTreeViewItem).EnumKey = targetDependencyKey;
                                    return result;
                                }
                            }
                        }
                        #endregion

                        #region 设置前后关系
                        if (previous is not null && previous.LayerCount == layerCount)
                        {
                            previous.Next = CurrentCompoundItem;
                            CurrentCompoundItem.Previous = previous;
                        }
                        #endregion

                        #region 检查文档是否存在枚举分支
                        if (i + 1 < nodeList.Count)
                        {
                            Match nextEnumLineMatch = GetEnumRawKey().Match(nodeList[i + 1]);
                            if (nextEnumLineMatch.Success)
                            {
                                result.ResultString.Append(new string(' ',CurrentCompoundItem.LayerCount * 2) + "\"" + CurrentCompoundItem.Key + "\": \"\"");
                                CurrentCompoundItem.ChildrenStringList.AddRange(nodeList.Skip(i + 1));
                                CurrentCompoundItem.IsCanBeDefaulted = false;
                                HaveBranches = true;
                                i = nodeList.Count;
                            }
                        }
                        #endregion

                        #region 处理递归或给予子数据
                        //获取下一行的*数量
                        int nextNodeIndex = i + 1;
                        if (!HaveBranches)
                        {
                            if (nextNodeIndex < nodeList.Count)
                            {
                                Match nextLineStarMatch = GetLineStarCount().Match(nodeList[nextNodeIndex]);
                                if (nextLineStarMatch.Success)
                                {
                                    string nextLineStar = nextLineStarMatch.Groups[1].Value.Trim();
                                    if (nextLineStar.Length > starCount)
                                    {
                                        #region 一次收集所有子节点，执行递归
                                        List<string> currentSubChildren = [];

                                        currentSubChildren.Add(nodeList[nextNodeIndex]);
                                        while (nextLineStarMatch.Success && nextLineStar.Length > starCount)
                                        {
                                            nextNodeIndex++;

                                            if (nextNodeIndex < nodeList.Count)
                                            {
                                                nextLineStarMatch = GetLineStarCount().Match(nodeList[nextNodeIndex]);
                                                if (nextLineStarMatch.Success && nextLineStarMatch.Groups[1].Value.Trim().Length > starCount)
                                                {
                                                    nextLineStar = nextLineStarMatch.Groups[1].Value.Trim();
                                                    if (nodeList[nextNodeIndex].Contains('{'))
                                                    {
                                                        currentSubChildren.Add(nodeList[nextNodeIndex]);
                                                    }
                                                }
                                            }
                                            else
                                                break;
                                            nextLineStar = nextLineStarMatch.Groups[1].Value.Trim();
                                        }
                                        #endregion

                                        #region 根据递归的情况来向后移动N个迭代单位
                                        if (currentSubChildren.Count > 0)
                                        {
                                            i = nextNodeIndex - 1;
                                        }
                                        #endregion

                                        #region 是否存储原始信息、处理递归
                                        if ((NBTFeatureList[0] == "compound" || NBTFeatureList[0].Contains("array")) && currentSubChildren.Count > 0 && !IsCurrentOptionalNode)
                                        {
                                            #region 如果子信息只有一条并引用指定文件，则将其文档内容取出并直接解析
                                            Match subInheritMatch = GetInheritString().Match(currentSubChildren[0]);
                                            if (currentSubChildren.Count == 1 && subInheritMatch.Success && plan.DependencyItemList.TryGetValue("#Inherit" + subInheritMatch.Groups[1].Value.Replace("/","\\"),out List<string> subTargetInheritList))
                                            {
                                                currentSubChildren = subTargetInheritList;
                                            }
                                            #endregion

                                            JsonTreeViewDataStructure subResult = GetTreeViewItemResult(new(), currentSubChildren, layerCount + 1, currentReferenceKey, CurrentCompoundItem, null, previousStarCount);
                                            CurrentCompoundItem.Children.AddRange(subResult.Result);
                                            if (subResult.Result.Count > 0)
                                            {
                                                CurrentCompoundItem.AddOrSwitchElementButtonVisibility = Visibility.Collapsed;
                                            }

                                            if (subResult.ResultString.Length > 0 && (!IsCurrentOptionalNode || isAddToParent))
                                            {
                                                if(subResult.ResultString.ToString().TrimEnd().EndsWith(','))
                                                {
                                                    subResult.ResultString.Remove(subResult.ResultString.Length - 3, 3);
                                                }
                                                result.ResultString.Append("\r\n" + subResult.ResultString + "\r\n" + new string(' ', layerCount * 2) + "}");
                                            }
                                            else//复合节点收尾
                                            if (!IsCurrentOptionalNode)
                                            {
                                                if (NBTFeatureList[0] == "compound")
                                                {
                                                    result.ResultString.Append('}');
                                                }
                                                else
                                                if (NBTFeatureList[0].Contains("array"))
                                                {
                                                    result.ResultString.Append(']');
                                                }
                                            }
                                        }
                                        else
                                        if (!IsCurrentOptionalNode)
                                        {
                                            if (NBTFeatureList[0] == "compound")
                                            {
                                                result.ResultString.Append('}');
                                            }
                                            else
                                            if (NBTFeatureList[0].Contains("array"))
                                            {
                                                result.ResultString.Append(']');
                                            }
                                        }

                                        if (CurrentCompoundItem.Children.Count == 0)
                                        {
                                            CurrentCompoundItem.ChildrenStringList.AddRange(currentSubChildren);
                                        }
                                        #endregion

                                        #region 判断后一个节点是否可选
                                        IsHaveNextNode = i < nodeList.Count - 1;
                                        if (IsHaveNextNode)
                                            IsNextOptionalNode = !GetRequiredKey().Match(nodeList[i + 1]).Success;
                                        #endregion

                                        //赋予Plan属性
                                        item.Plan ??= plan;
                                    }
                                    else
                                    {
                                        //没有子信息时，提取当前行的上下文信息作为子信息，因为引用一定包含在当前行中
                                        if (contextMatch.Success)
                                        {
                                            CurrentCompoundItem.ChildrenStringList.Add("[[#" + contextMatch.Groups[1].Value + '|' + contextMatch.Groups[2] + "]]");
                                        }
                                        IsNextOptionalNode = !GetRequiredKey().Match(nodeList[i + 1]).Success;
                                    }
                                }
                            }
                        }
                        #endregion
                    }
                    #endregion
                }
                #endregion

                #region 处理节点的追加
                if (i < nodeList.Count - 1 && !result.ResultString.ToString().TrimEnd(['\r', '\n', ' ']).EndsWith(',') && !IsCurrentOptionalNode && IsHaveNextNode)
                {
                    result.ResultString.Append(",\r\n");
                }

                result.Result.Add(item);
                #endregion

                #region 保存信息
                //将当前节点保存为上一个节点
                previous = item;
                //保存当前的*号数量
                previousStarCount = starCount;
                #endregion
            }

            return result;
        }
    }
}