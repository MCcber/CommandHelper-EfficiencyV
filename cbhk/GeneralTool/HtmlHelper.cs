using CBHK.CustomControl;
using CBHK.CustomControl.Interfaces;
using CBHK.CustomControl.JsonTreeViewComponents;
using CBHK.Model.Common;
using CBHK.Service.Json;
using CBHK.ViewModel.Common;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using static CBHK.Model.Common.Enums;

namespace CBHK.GeneralTool
{
    public partial class HtmlHelper(IContainerProvider container)
    {
        #region Field
        public string RootDirectory { get; set; } = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\";

        public ICustomWorldUnifiedPlan plan = null;

        public IJsonItemTool jsonTool = null;

        private List<string> EnumKeyList = ["[[命名空间ID]]","[[内容类型ID]]"];

        private List<string> dataStringType = ["string", "bool", "int", "short", "float", "double", "long", "decimal", "compound", "list"];

        [GeneratedRegex(@"\s?\s*\*+\s?\s*{{([nN]bt\s)?inherit(?<1>[a-z_/|=*\s]+)}}\s?\s*", RegexOptions.IgnoreCase)]
        private static partial Regex GetInheritString();

        [GeneratedRegex(@"(?<=与).+(?=不能同时存在)")]
        private static partial Regex GetMutexKey();

        [GeneratedRegex(@"(?<=\s*\s?\*+;?\s*\s?(如果|若|当)).+(?=为|是).+")]
        private static partial Regex GetEnumRawKey();

        [GeneratedRegex(@"\[\[\#?((?<1>[\u4e00-\u9fff]+)\|(?<2>[\u4e00-\u9fff]+)|(?<1>[\u4e00-\u9fff]+))\]\]")]
        private static partial Regex GetContextKey();

        [GeneratedRegex(@"(?<=\s*\s?\*+;?\s*\s?(取决于)).+(?=的额外内容).+")]
        private static partial Regex GetExtraKey();

        [GeneratedRegex(@"\[\[(?<1>[a-zA-Z_\u4e00-\u9fff|#]+)\]\]")]
        private static partial Regex GetEnumKey();

        [GeneratedRegex(@"^\s*\s?\:?\s*\s?(\*+)")]
        private static partial Regex GetLineStarCount();

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

        [GeneratedRegex(@"(?<=<code>)(?<1>[a-z_:]+)(?=</code>)")]
        private static partial Regex GetEnumValueMode1();

        [GeneratedRegex(@"\{\{cd\|(?<1>[a-z:_]+)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetEnumValueMode2();

        [GeneratedRegex(@"\s?\s*=+\s?\s*([#\u4e00-\u9fffa-z_/]+)\s?\s*=+\s?\s*", RegexOptions.IgnoreCase)]
        private static partial Regex GetContextFileMarker();

        [GeneratedRegex(@"=+\s*\s?((minecraft\:)?[a-z_/]+)\s*\s?=+", RegexOptions.IgnoreCase)]
        private static partial Regex GetEnumTypeKeywords();

        public List<string> ProgressClassList = ["treeview"];

        private IContainerProvider _container = container;
        private bool HadPreIdentifiedAsEnumCompoundType { get; set; }
        #endregion

        /// <summary>
        /// 把VSC排版后的格式重新整理(有些应该归为一行的被排版分割为了多行)
        /// </summary>
        /// <param name="target">需要被排版的链表</param>
        /// <returns></returns>
        public static List<string> TypeSetting(List<string> target)
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

        /// <summary>
        /// 去除UI标记
        /// </summary>
        /// <param name="NBTFeatureList"></param>
        /// <returns></returns>
        public static List<string> RemoveUIMarker(List<string> NBTFeatureList)
        {
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
            return NBTFeatureList;
        }

        /// <summary>
        /// 处理依赖文件
        /// </summary>
        /// <param name="target"></param>
        private void HandlingDependencyFile(string[] target)
        {
            HtmlDocument htmlDocument = new();
            for (int i = 0; i < target.Length; i++)
            {
                if (File.Exists(target[i]) && Path.GetExtension(target[i]) == ".wiki")
                {
                    string[] fileArrayContent = File.ReadAllLines(target[i]);
                    string fileContent = File.ReadAllText(target[i]);
                    htmlDocument.LoadHtml(fileContent);
                    List<HtmlNode> treeviewDivs = [.. htmlDocument.DocumentNode.SelectNodes("//div[@class='treeview']")];

                    if (treeviewDivs is not null)
                    {
                        #region 直接覆盖InnerHtml属性会导致内容错误，所以这里直接用链表来提取正确的结构数据
                        List<string> nodeContent = [.. treeviewDivs[0].InnerHtml.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)];
                        nodeContent = TypeSetting(nodeContent);
                        #endregion

                        #region 执行解析、分析是否需要被添加为依赖项
                        string title = '#' + target[i].Replace(RootDirectory, "").Replace(".wiki", "").Replace("\\", "/") + "";
                        if (treeviewDivs.Count == 1)
                        {
                            plan.DependencyItemList[title] = nodeContent;
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
                                if (!enumMatch.Success)
                                {
                                    enumMatch = GetEnumValueMode1().Match(fileArrayContent[treeviewDivs[j].Line]);
                                }
                                if (enumMatch.Success)
                                {
                                    Dictionary<string, List<string>> keyValuePairs = [];
                                    List<string> targetRawStringList = [.. fileArrayContent.Skip(treeviewDivs[j].Line).Take(treeviewDivs[j].EndNode.Line - 1 - treeviewDivs[j].Line)];
                                    targetRawStringList = TypeSetting(targetRawStringList);
                                    bool NoKey = plan.EnumCompoundDataDictionary.TryAdd(title, keyValuePairs);
                                    if (NoKey)
                                    {
                                        keyValuePairs.Add(enumMatch.Value.Replace("=", "").Trim(), targetRawStringList);
                                    }
                                    else
                                    {
                                        plan.EnumCompoundDataDictionary[title].TryAdd(enumMatch.Value.Replace("=", "").Trim(), targetRawStringList);
                                    }
                                }
                            }
                        }
                        #endregion
                    }
                }
            }
        }

        /// <summary>
        /// 在Wiki文档被解析完毕后由上到下处理一次复合控件的键入外观与键入逻辑
        /// </summary>
        public void HandlingTheTypingAppearanceOfCompositeItemList(IEnumerable<JsonTreeViewItem> list,CompoundJsonTreeViewItem parent)
        {
            foreach (var element in list)
            {
                if (element is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                {
                    #region 处理键入外观
                    foreach (var childString in compoundJsonTreeViewItem.ChildrenStringList)
                    {
                        Match contentMatch = GetContextKey().Match(childString);
                        if (contentMatch.Success && (element.DataType is not DataType.None || (element is CompoundJsonTreeViewItem compoundElementItem && ((compoundElementItem.DataType is not DataType.List && compoundElementItem.DataType is not DataType.OptionalCompound) || (compoundElementItem.DataType is DataType.MultiType && compoundElementItem.SelectedValueType.Text != "List")))))
                        {
                            string key = '#' + contentMatch.Groups[1].Value;
                            string targetKey = "";
                            CompoundJsonTreeViewItem subCompoundItem = new(plan, jsonTool, _container)
                            {
                                LayerCount = compoundJsonTreeViewItem.LayerCount,
                                DataType = DataType.CustomCompound,
                                AddOrSwitchElementButtonVisibility = Visibility.Visible,
                                IsCurrentExpanded = false
                            };
                            bool isDependencyItemListKey = false;

                            if (plan.DependencyItemList.TryGetValue(key, out List<string> dependencyList))
                            {
                                isDependencyItemListKey = true;
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
                            if (plan.TranslateDictionary.TryGetValue(key, out targetKey))
                            {
                            }
                            else
                            if (plan.TranslateDictionary.TryGetValue(compoundJsonTreeViewItem.Key, out targetKey))
                            {
                            }

                            if (!isDependencyItemListKey && targetKey is not null)
                            {
                                if (plan.DependencyItemList.TryGetValue(targetKey, out List<string> subChildrenList))
                                {
                                    if (subCompoundItem.ChildrenStringList.Count == 0)
                                    {
                                        subCompoundItem.ChildrenStringList = subChildrenList;
                                    }
                                    bool isHaveExtraField = subChildrenList[^1].Contains("根据内容而指定的额外字段") || GetExtraKey().Match(subChildrenList[^1]).Success;
                                    for (int i = 0; i < subChildrenList.Count; i++)
                                    {
                                        List<string> subNBTFeatureList = GetHeadTypeAndKeyList(subChildrenList[i]);
                                        if (subNBTFeatureList.Count > 0 && subNBTFeatureList[^1].Contains('\'') && subNBTFeatureList[^1].Contains('<'))
                                        {
                                            subCompoundItem.InputBoxVisibility = Visibility.Visible;
                                            subCompoundItem.SwitchButtonIcon = compoundJsonTreeViewItem.PlusIcon;
                                            subCompoundItem.SwitchButtonColor = compoundJsonTreeViewItem.PlusColor;
                                            compoundJsonTreeViewItem.Children.Insert(0, subCompoundItem);
                                            subCompoundItem.Parent = compoundJsonTreeViewItem;
                                            compoundJsonTreeViewItem.AddOrSwitchElementButtonVisibility = Visibility.Collapsed;
                                            if (isHaveExtraField)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }
                                else
                                if (plan.EnumCompoundDataDictionary.TryGetValue(targetKey, out Dictionary<string, List<string>> dictionary) && (compoundJsonTreeViewItem.EnumItemsSource.Count == 0 || (compoundJsonTreeViewItem.EnumItemsSource.Count > 1 && !dictionary.ContainsKey(compoundJsonTreeViewItem.EnumItemsSource[1].Text))))
                                {
                                    CompoundJsonTreeViewItem targetItem = null;
                                    if (compoundJsonTreeViewItem.Key.Length > 0)
                                    {
                                        targetItem = subCompoundItem;
                                    }
                                    else
                                    {
                                        targetItem = parent;
                                    }

                                    if (targetItem is not null)
                                    {
                                        if (targetItem.DataType is DataType.None)
                                        {
                                            targetItem.DataType = DataType.Enum;
                                            targetItem.LayerCount = compoundJsonTreeViewItem.LayerCount + 1;
                                        }
                                        targetItem.EnumKey = targetKey;
                                        targetItem.AddOrSwitchElementButtonVisibility = Visibility.Collapsed;
                                        targetItem.EnumBoxVisibility = Visibility.Visible;
                                        if (targetItem.IsCanBeDefaulted)
                                        {
                                            targetItem.EnumItemsSource.Add(new TextComboBoxItem() { Text = "- unset -" });
                                        }
                                        targetItem.EnumItemsSource.AddRange(dictionary.Keys.Select(item =>
                                        {
                                            return new TextComboBoxItem()
                                            {
                                                Text = item
                                            };
                                        }));
                                        targetItem.SelectedEnumItem = targetItem.EnumItemsSource[0];
                                        compoundJsonTreeViewItem.SwitchButtonIcon = compoundJsonTreeViewItem.MinusIcon;
                                        compoundJsonTreeViewItem.SwitchButtonColor = compoundJsonTreeViewItem.MinusColor;
                                        compoundJsonTreeViewItem.Children.Add(targetItem);
                                        if (compoundJsonTreeViewItem.Key.Length == 0)
                                        {
                                            string firstItemRawString = dictionary[targetItem.EnumItemsSource[1].Text][0];
                                            List<string> NBTFeatureList = GetHeadTypeAndKeyList(firstItemRawString);
                                            NBTFeatureList = RemoveUIMarker(NBTFeatureList);
                                            targetItem.Key = NBTFeatureList[^1];
                                            targetItem.DisplayText = targetItem.Key[0].ToString().ToUpper() + targetItem.Key[1..];
                                        }
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                }

                #region 处理键入逻辑

                #endregion

                #region 处理冲突节点

                #endregion
            }
        }

        /// <summary>
        /// 获取节点标头数据
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public List<string> GetHeadTypeAndKeyList(string target)
        {
            List<string> result = [];
            string RemoveIrrelevantString = target.Replace("}}{{nbt", "").Replace("}}{{Nbt","");
            int nbtFeatureStartIndex = RemoveIrrelevantString.IndexOf("{{");
            int nbtFeatureEndIndex = RemoveIrrelevantString.IndexOf("}}");
            if (RemoveIrrelevantString.Contains('{') && nbtFeatureEndIndex > 0 && nbtFeatureEndIndex > 0)
            {
                result.AddRange(RemoveIrrelevantString[(nbtFeatureStartIndex + 2)..nbtFeatureEndIndex].Split('|'));
                result.RemoveAt(0);
            }
            return result;
        }

        /// <summary>
        /// 获取节点的描述
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public string GetDescription(string target)
        {
            string result = "";
            if(target.Contains('：'))
            {
                result = target[(target.IndexOf('：') + 1)..];
            }
            else//获取标头信息，定位最后一个成员的末尾位置，再加上两个花括号的长度得出描述开头位置
            {
                List<string> headList = GetHeadTypeAndKeyList(target);
                int lastWord = target.IndexOf(headList[^1]);
                result = target[(lastWord + headList[^1].Length + 2)..].Trim();
            }
            return result;
        }

        private static Tuple<List<string>,int> CollectionSubItem(List<string> nodeList,int currentIndex,int starCount,int nextLineStarCount)
        {
            List<string> result = [];
            int nextNodeIndex = currentIndex + 1;
            Match nextLineStarMatch = GetLineStarCount().Match(nodeList[nextNodeIndex]);
            result.Add(nodeList[nextNodeIndex]);
            while (nextLineStarMatch.Success && nextLineStarCount > starCount)
            {
                nextNodeIndex++;

                if (nextNodeIndex < nodeList.Count)
                {
                    nextLineStarMatch = GetLineStarCount().Match(nodeList[nextNodeIndex]);
                    if (nextLineStarMatch.Success && nextLineStarMatch.Groups[1].Value.Trim().Length > starCount)
                    {
                        nextLineStarCount = nextLineStarMatch.Groups[1].Value.Trim().Length;
                        if (nodeList[nextNodeIndex].Contains('{') || nodeList[nextNodeIndex].Contains('['))
                        {
                            result.Add(nodeList[nextNodeIndex]);
                        }
                    }
                }
                else
                    break;
                nextLineStarCount = nextLineStarMatch.Groups[1].Value.Trim().Length;
            }
            return new Tuple<List<string>, int>(result, nextNodeIndex);
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
            if (plan is BaseCustomWorldUnifiedPlan baseCustomWorldUnifiedPlan)
            {
                string dependencyDirectoryListPath = Path.Combine(baseCustomWorldUnifiedPlan.ConfigDirectoryPath + plan.CurrentVersion.Text, "dependencyDirectoryList.json");
                string dependencyFileListPath = Path.Combine(baseCustomWorldUnifiedPlan.ConfigDirectoryPath + plan.CurrentVersion.Text, "dependencyFileList.json");
                if (File.Exists(dependencyDirectoryListPath))
                {
                    JArray directoryArray = JArray.Parse(File.ReadAllText(dependencyDirectoryListPath));
                    foreach (var entry in directoryArray)
                    {
                        string[] fileList = Directory.GetFiles(RootDirectory + entry.Value<string>());
                        HandlingDependencyFile(fileList);
                    }
                }
                if (File.Exists(dependencyFileListPath))
                {
                    JArray fileArray = JArray.Parse(File.ReadAllText(dependencyFileListPath));
                    string[] directFileList = [.. fileArray.Select(item => RootDirectory + item.Value<string>())];
                    HandlingDependencyFile(directFileList);
                }
            }
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

                    Result = GetTreeViewItemResult(new(), nodeContent, 1);
                    HandlingTheTypingAppearanceOfCompositeItemList(Result.Result,null);
                }
                #endregion
            }
            #endregion

            return Result;
        }

        /// <summary>
        /// 转换Wiki文档为树视图节点与Json代码
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
            #region Field
            bool isCompoundRoot = false, isListRoot = false;
            bool isHaveExtraField = nodeList.Count > 0 && (nodeList[^1].Contains("根据内容而指定的额外字段") || GetExtraKey().Match(nodeList[^1]).Success);
            bool IsPreIdentifiedAsEnumCompoundType = false;
            bool IsNoKeyOrMultiDataTypeItem = false;
            int currentContextNextIndex = 0;
            int EnumItemCount = 0;
            List<string> KeyList = [];
            string CurrentEnumKey = "";
            bool isSimpleDataType = true;
            #endregion

            //遍历找到的div标签内容集合
            for (int i = 0; i < nodeList.Count; i++)
            {
                #region Field
                bool isParentMultipleCustomCompound = false;
                bool isAddedStringInMulipleCode = false;
                bool HaveBranches = false;
                string currentNodeDataType = "";
                string currentNodeKey = "";
                string currentDescription = "";
                bool IsSimpleItem = true;
                currentReferenceKey ??= "";
                bool IsCurrentOptionalNode = !GetRequiredKey().Match(nodeList[i]).Success && KeyList.Count == 0 && !(parent is not null && parent.DataType is DataType.CustomCompound);
                Match inheritMatch = GetInheritString().Match(nodeList[i]);
                Match contextMatch = GetContextKey().Match(nodeList[i]);
                List<string> NBTFeatureList = GetHeadTypeAndKeyList(nodeList[i]);
                NBTFeatureList = RemoveUIMarker(NBTFeatureList);
                #endregion

                #region 获取当前行星号数量
                Match starMatch = GetLineStarCount().Match(nodeList[i]);
                int starCount = starMatch.Value.Trim().Length;
                #endregion

                #region 提取引用节点所代指的文档并将其内容插入到当前节点列表中
                if (parent is not null && isAddToParent && parent.DisplayText != "Root")
                {
                    #region 处理父级类型与当前节点类型都为有key列表
                    if((parent.DataType is DataType.List || (parent.DataType is DataType.MultiType && parent.SelectedValueType is not null && parent.SelectedValueType.Text == "List")) && i > currentContextNextIndex && parent.Key.Length > 0 && NBTFeatureList.Except(dataStringType).Any() && NBTFeatureList.Count > 0 && NBTFeatureList[0] == "list")
                    {
                        int nextLineStarCount = 0;
                        if(i + 1 < nodeList.Count)
                        {
                            Match nextLineStarMatch = GetLineStarCount().Match(nodeList[i + 1]);
                            nextLineStarCount = nextLineStarMatch.Value.Trim().Length;
                        }
                        Tuple<List<string>,int> subNodeTuple = CollectionSubItem(nodeList, i, starCount, nextLineStarCount);
                        nodeList.RemoveRange(i,nodeList.Count - i);
                        nodeList.InsertRange(i, subNodeTuple.Item1);
                        i = -1;
                        continue;
                    }
                    #endregion

                    #region 处理注入的结构
                    if (inheritMatch.Success)
                    {
                        string currentSourceCode = inheritMatch.Groups[1].Value;
                        List<string> sourceCodeContentList = RemoveUIMarker([.. currentSourceCode.Replace("|", "/").Split('/',StringSplitOptions.RemoveEmptyEntries)]);
                        currentSourceCode = "#Inherit" + "/" + string.Join("/", sourceCodeContentList);
                        if (plan.DependencyItemList.TryGetValue(currentSourceCode, out List<string> targetInheritList))
                        {
                            nodeList.RemoveAt(i);
                            nodeList.InsertRange(i, [.. targetInheritList]);
                            if (targetInheritList.FirstOrDefault().Contains(parent.Key))
                            {
                                nodeList.RemoveAt(0);
                            }
                            NBTFeatureList = GetHeadTypeAndKeyList(nodeList[i]);
                            NBTFeatureList = RemoveUIMarker(NBTFeatureList);
                            IsCurrentOptionalNode = !GetRequiredKey().Match(nodeList[i]).Success && KeyList.Count == 0;
                            currentContextNextIndex = i + targetInheritList.Count;
                        }
                        else
                            if (plan.EnumCompoundDataDictionary.TryGetValue(currentSourceCode, out Dictionary<string, List<string>> targetDictionary))
                        {
                            IsPreIdentifiedAsEnumCompoundType = true;
                            HadPreIdentifiedAsEnumCompoundType = true;
                            CurrentEnumKey = currentSourceCode;
                            isSimpleDataType = false;
                            KeyList.AddRange(targetDictionary.Keys);
                            List<string> inlineSourceCode = targetDictionary[KeyList.FirstOrDefault()];
                            if ((!NBTFeatureList.Contains("compound") &&
                                !NBTFeatureList.Where(item => item.Contains("array")).Any() &&
                                !NBTFeatureList.Contains("list")) || i >= currentContextNextIndex)
                            {
                                nodeList.RemoveAt(i);
                                nodeList.InsertRange(i, inlineSourceCode);
                                EnumItemCount = inlineSourceCode.Count - 1;
                                currentContextNextIndex = i + inlineSourceCode.Count;
                                i--;
                                continue;
                            }
                        }
                    }
                    #endregion

                    #region 处理上下文关键字
                    if (contextMatch.Success && parent is not null && i >= currentContextNextIndex && (NBTFeatureList.Contains("required=1") || !IsCurrentOptionalNode || NBTFeatureList.Count == 1))
                    {
                        string contextKey = ("#" + contextMatch.Groups[1].Value);
                        if (plan.DependencyItemList.TryGetValue(contextKey, out List<string> targetList1))
                        {
                            List<string> targetList = [..targetList1];
                            targetList.RemoveAt(0);
                            nodeList.RemoveAt(i);
                            nodeList.InsertRange(i, targetList);
                            currentContextNextIndex = i + targetList.Count;
                            i--;
                            continue;
                        }
                        else
                        if (plan.TranslateDictionary.TryGetValue(contextKey, out string targetKey))
                        {
                            if (plan.DependencyItemList.TryGetValue(targetKey, out List<string> targetList2))
                            {
                                List<string> targetList = [..targetList2];
                                nodeList.RemoveAt(i);
                                nodeList.InsertRange(i, targetList);
                                i--;
                                currentContextNextIndex = (i < 0 ? 1 : 0) + targetList.Count;
                                continue;
                            }
                            else
                            if(plan.EnumCompoundDataDictionary.TryGetValue(targetKey,out Dictionary<string,List<string>> targetDictionary))
                            {
                                IsPreIdentifiedAsEnumCompoundType = true;
                                HadPreIdentifiedAsEnumCompoundType = true;
                                CurrentEnumKey = targetKey;
                                isSimpleDataType = false;
                                KeyList.AddRange(targetDictionary.Keys);
                                List<string> inlineSourceCode = targetDictionary[KeyList.FirstOrDefault()];
                                if ((!NBTFeatureList.Contains("compound") &&
                                    !NBTFeatureList.Where(item=>item.Contains("array")).Any() &&
                                    !NBTFeatureList.Contains("list")) || i >= currentContextNextIndex)
                                {
                                    nodeList.RemoveAt(i);
                                    nodeList.InsertRange(i, inlineSourceCode);
                                    EnumItemCount = inlineSourceCode.Count - 1;
                                    currentContextNextIndex = i + inlineSourceCode.Count;
                                    i--;
                                    continue;
                                }
                            }
                        }
                    }
                    #endregion
                }
                #endregion

                #region 获取当前行星号数量
                starMatch = GetLineStarCount().Match(nodeList[i]);
                starCount = starMatch.Value.Trim().Length;
                #endregion

                #region Field
                MatchCollection EnumCollectionMode1 = GetEnumValueMode1().Matches(nodeList[i]);
                MatchCollection EnumCollectionMode2 = GetEnumValueMode2().Matches(nodeList[i]);
                Match EnumMatch = GetEnumKey().Match(nodeList[i]);
                bool isEnumKey = plan.TranslateDictionary.Count > 0 && (plan.TranslateDictionary.ContainsKey('#' + EnumMatch.Groups[1].Value) || plan.EnumIDDictionary.ContainsKey('#' + EnumMatch.Groups[1].Value));
                bool isBoolKey = (EnumCollectionMode1.Count > 0 && (EnumCollectionMode1[0].Groups[1].Value == "false" || EnumCollectionMode1[0].Groups[1].Value == "true")) || (EnumCollectionMode2.Count > 0 && (EnumCollectionMode2[0].Groups[1].Value == "false" || EnumCollectionMode2[0].Groups[1].Value == "true"));
                if (NBTFeatureList.Count > 0 && !IsPreIdentifiedAsEnumCompoundType)
                {
                    isSimpleDataType = !(parent is not null && parent.DataType is DataType.CustomCompound && currentReferenceKey.Length > 0) && ((!NBTFeatureList[0].Contains("array") && NBTFeatureList[0] != "list" && NBTFeatureList[0] != "compound" && NBTFeatureList.Count < 3) || isBoolKey) && ((isBoolKey || (EnumCollectionMode1.Count < 2 && EnumCollectionMode2.Count < 2)) && !isEnumKey) && !NBTFeatureList[^1].Contains('\'');
                }

                bool IsExplanationNode = NBTFeatureList.Count > 1;
                bool IsHaveNextNode = i < nodeList.Count - 1;
                bool isHaveNameSpaceKey = EnumKeyList.Where(enumItem => { return nodeList[i].Contains(enumItem); }).Any();
                bool isEnumIDList = false;
                if (EnumMatch.Success)
                {
                    isEnumIDList = plan.EnumIDDictionary.Count > 0 && plan.EnumIDDictionary.ContainsKey(EnumMatch.Groups[1].Value);
                }
                Match DefaultEnumValueMatch = GetDefaultEnumValue().Match(nodeList[i]);
                Match DefaultNumberValueMatch = GetDefaultNumberValue().Match(nodeList[i]);
                Match DefaultBoolValueMatch = GetDefaultBoolValue().Match(nodeList[i]);
                Match DefaultStringValueMatch = GetDefaultStringValue().Match(nodeList[i]);
                #endregion

                #region 判断是否跳过本次处理
                if(NBTFeatureList.Count == 0 || ((NBTFeatureList.Count == 1 && NBTFeatureList[0] == "compound") && !inheritMatch.Success && !contextMatch.Success && !EnumMatch.Success))
                {
                    continue;
                }

                if (nodeList[i].Trim().Length == 0)
                {
                    continue;
                }

                int lastCurlyBracesIndex = nodeList[i].LastIndexOf('}');
                string rootString = nodeList[i][(lastCurlyBracesIndex + 1)..].Trim();

                if (lastCurlyBracesIndex > -1 && !string.IsNullOrEmpty(rootString) && (rootString == "根节点" || rootString == "根标签" && NBTFeatureList[0] != "list") && NBTFeatureList.Count < 3)
                {
                    continue;
                }
                #endregion

                #region 获取当前节点的数据类型和键、获取描述数据
                currentNodeDataType = NBTFeatureList[0];
                if (NBTFeatureList.Count > 1)
                {
                    currentNodeKey = NBTFeatureList[^1];
                }
                currentDescription = GetDescription(nodeList[i]);
                if(currentNodeKey.Contains('\''))
                {
                    IsCurrentOptionalNode = false;
                }
                #endregion

                #region 声明当前节点
                if (currentDescription == "根标签" || currentDescription == "根节点")
                {
                    layerCount = 0;
                }

                if (NBTFeatureList[0] == "list" && currentDescription == "根标签" || currentDescription == "根节点")
                {
                    isListRoot = true;
                }
                JsonTreeViewItem item = new()
                {
                    LayerCount = layerCount,
                    IsCanBeDefaulted = IsCurrentOptionalNode
                };
                #endregion

                #region 确认是否能够进入解析流程
                bool IsCanBeAnalyzed = currentNodeDataType.Length > 0;
                #endregion

                #region 处理两大值类型
                if (IsCanBeAnalyzed)
                {
                    #region 判断是否为复合节点
                    if (isHaveExtraField || isEnumIDList || isHaveNameSpaceKey || !isSimpleDataType)
                    {
                        item = new CompoundJsonTreeViewItem(plan, jsonTool, _container)
                        {
                            IsCanBeDefaulted = IsCurrentOptionalNode
                        };
                        IsSimpleItem = false;
                    }
                    #endregion

                    #region 判断是否捕获多组信息
                    isParentMultipleCustomCompound = parent is not null && parent.DataType is DataType.CustomCompound && parent.ValueTypeSource.Count > 0;
                    if ((NBTFeatureList is not null && NBTFeatureList.Count > 2 && currentNodeDataType.Length > 0 && currentNodeKey.Length > 0) || isParentMultipleCustomCompound)
                    {
                        if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                        {
                            if(isParentMultipleCustomCompound)
                            {
                                compoundJsonTreeViewItem.ValueTypeSource.AddRange([..parent.ValueTypeSource]);
                            }
                            else
                            {
                                if (compoundJsonTreeViewItem.IsCanBeDefaulted)
                                {
                                    compoundJsonTreeViewItem.ValueTypeSource.Add(new TextComboBoxItem() { Text = "- unset -" });
                                }
                                for (int j = 0; j < NBTFeatureList.Count - 1; j++)
                                {
                                    compoundJsonTreeViewItem.ValueTypeSource.Add(new TextComboBoxItem()
                                    {
                                        Text = NBTFeatureList[j][0].ToString().ToUpper() + NBTFeatureList[j][1..]
                                    });
                                }
                            }
                            compoundJsonTreeViewItem.SelectedValueType ??= compoundJsonTreeViewItem.ValueTypeSource[0];
                            compoundJsonTreeViewItem.DataType = DataType.MultiType;
                            if (!IsCurrentOptionalNode && !(currentDescription == "根标签" || currentDescription == "根节点"))
                            {
                                result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + (currentReferenceKey.Length > 0 ? currentReferenceKey : currentNodeKey) + "\": ");
                            }

                            isCompoundRoot = NBTFeatureList[0] == "compound";
                            isListRoot = NBTFeatureList[0] == "list";
                            switch (NBTFeatureList[0])
                            {
                                case "bool":
                                    {
                                        if (!IsCurrentOptionalNode)
                                        {
                                            isAddedStringInMulipleCode = true;
                                            bool defaultValue = false;
                                            if (DefaultBoolValueMatch.Success)
                                            {
                                                defaultValue = bool.Parse(DefaultBoolValueMatch.Groups[1].Value);
                                            }
                                            result.ResultString.Append(defaultValue.ToString().ToLower());
                                            compoundJsonTreeViewItem.Value = defaultValue;
                                        }
                                        if (compoundJsonTreeViewItem.SelectedValueType is null || (compoundJsonTreeViewItem.SelectedValueType is not null && compoundJsonTreeViewItem.SelectedValueType.Text != "- unset -"))
                                        {
                                            compoundJsonTreeViewItem.BoolButtonVisibility = Visibility.Visible;
                                        }
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
                                        if (!IsCurrentOptionalNode)
                                        {
                                            isAddedStringInMulipleCode = true;
                                            int defaultValue = 0;
                                            if (DefaultNumberValueMatch.Success)
                                            {
                                                defaultValue = int.Parse(DefaultNumberValueMatch.Groups[1].Value);
                                            }
                                            result.ResultString.Append(defaultValue);
                                            compoundJsonTreeViewItem.Value = defaultValue;
                                        }
                                        else
                                        {
                                            compoundJsonTreeViewItem.Value = 0;
                                        }
                                        if (compoundJsonTreeViewItem.SelectedValueType is null || (compoundJsonTreeViewItem.SelectedValueType is not null && compoundJsonTreeViewItem.SelectedValueType.Text != "- unset -"))
                                        {
                                            compoundJsonTreeViewItem.InputBoxVisibility = Visibility.Visible;
                                        }
                                        break;
                                    }
                                case "string":
                                    {
                                        if (!IsCurrentOptionalNode)
                                        {
                                            isAddedStringInMulipleCode = true;
                                            string defaultValue = "";
                                            if (DefaultStringValueMatch.Success)
                                            {
                                                defaultValue = DefaultStringValueMatch.Groups[1].Value;
                                            }
                                            result.ResultString.Append("\"" + defaultValue + "\"");
                                            compoundJsonTreeViewItem.Value = defaultValue;
                                        }
                                        else
                                        {
                                            compoundJsonTreeViewItem.Value = "";
                                        }
                                        if (compoundJsonTreeViewItem.SelectedValueType is null || (compoundJsonTreeViewItem.SelectedValueType is not null && compoundJsonTreeViewItem.SelectedValueType.Text != "- unset -"))
                                        {
                                            compoundJsonTreeViewItem.InputBoxVisibility = Visibility.Visible;
                                        }
                                        break;
                                    }
                                case "compound":
                                    {
                                        if (!IsCurrentOptionalNode)
                                        {
                                            result.ResultString.Append('{');
                                        }
                                        break;
                                    }
                                case "list":
                                    {
                                        if (!IsCurrentOptionalNode)
                                        {
                                            result.ResultString.Append("[]");
                                        }
                                        break;
                                    }
                            }
                            if (NBTFeatureList[0].Contains("array") && !IsCurrentOptionalNode)
                            {
                                result.ResultString.Append('[');
                            }
                            compoundJsonTreeViewItem.ValueTypeBoxVisibility = Visibility.Visible;
                            compoundJsonTreeViewItem.SwitchButtonColor = compoundJsonTreeViewItem.PlusColor;
                            compoundJsonTreeViewItem.SwitchButtonIcon = compoundJsonTreeViewItem.PlusIcon;
                            if(isParentMultipleCustomCompound && NBTFeatureList[0] != "compound")
                            {
                                result.Result.Add(compoundJsonTreeViewItem);
                                return result;
                            }
                        }
                    }
                    #endregion

                    #region 处理无Key的多重数据类型节点
                    List<string> keyFilter = [.. NBTFeatureList.Except(dataStringType)];
                    keyFilter.RemoveAll(item=>item.Contains("array"));
                    if(keyFilter.Count == 0 && NBTFeatureList.Count > 2)
                    {
                        if (NBTFeatureList[0] == "compound")
                        {
                            int lastWordIndex = nodeList[i].IndexOf(NBTFeatureList[^1]);
                            int keyAndDataTypeEndPoint = lastWordIndex + NBTFeatureList[^1].Length;
                            //使其能够被引擎起始处的识别器选中
                            nodeList[i] = nodeList[i].Insert(keyAndDataTypeEndPoint, "|required=1");
                            i--;
                            continue;
                        }
                        else
                        if (!NBTFeatureList[0].Contains("array") && NBTFeatureList[0] != "list")
                        {
                            IsNoKeyOrMultiDataTypeItem = true;
                            string description = GetDescription(nodeList[i]);
                            CompoundJsonTreeViewItem multipleDataTypeElement = new(plan,jsonTool,_container)
                            {
                                IsCanBeDefaulted = false,
                                DataType = DataType.MultiType,
                                InfoTipText = description,
                                RemoveElementButtonVisibility = Visibility.Visible,
                                DisplayText = "Entry",
                                LayerCount = parent.LayerCount + 1,
                                Parent = parent
                            };
                            #region 处理值类型切换需要使用的字段
                            if (contextMatch.Success)
                            {
                                if(plan.TranslateDictionary.TryGetValue('#' + contextMatch.Groups[1].Value,out string targetKey))
                                {
                                    multipleDataTypeElement.EnumKey = targetKey;
                                }
                            }
                            else
                            {
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

                                                #region 添加子信息
                                                multipleDataTypeElement.ChildrenStringList = currentSubChildren;
                                                #endregion
                                            }
                                        }
                                    }
                                }
                            }
                            #endregion
                            multipleDataTypeElement.ValueTypeSource.AddRange(NBTFeatureList.Select(item => new TextComboBoxItem() { Text = item[0].ToString().ToUpper() + item[1..] }));
                            multipleDataTypeElement.SelectedValueType = multipleDataTypeElement.ValueTypeSource[0];
                            multipleDataTypeElement.ValueTypeBoxVisibility = Visibility.Visible;
                            result.Result.Add(multipleDataTypeElement);
                            result.ResultString.Append(new string(' ', multipleDataTypeElement.LayerCount * 2));
                            switch (NBTFeatureList[0])
                            {
                                case "bool":
                                    {
                                        multipleDataTypeElement.BoolButtonVisibility = Visibility.Visible;
                                        multipleDataTypeElement.Value = "false";
                                        result.ResultString.Append("false");
                                        continue;
                                    }
                                case "int":
                                case "short":
                                case "float":
                                case "double":
                                case "long":
                                case "decimal":
                                    {
                                        multipleDataTypeElement.InputBoxVisibility = Visibility.Visible;
                                        multipleDataTypeElement.Value = "0";
                                        result.ResultString.Append('0');
                                        continue;
                                    }
                                case "string":
                                    {
                                        multipleDataTypeElement.InputBoxVisibility = Visibility.Visible;
                                        multipleDataTypeElement.Value = "";
                                        result.ResultString.Append("\"\"");
                                        continue;
                                    }
                            }
                        }
                    }
                    #endregion

                    #region 处理值类型
                    if (IsSimpleItem)
                    {
                        item.InfoTipText = currentDescription;
                        switch (currentNodeDataType)
                        {
                            case "bool":
                            case "boolean":
                                {
                                    item.DataType = DataType.Bool;
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
                                        result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + currentNodeKey.ToLower() + "\": false");
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
                                    if(parent is not null && (parent.DataType is DataType.List || parent.DataType is DataType.Array) && isAddToParent && currentNodeKey.Length == 0)
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
                                    item.InputBoxVisibility = Visibility.Visible;
                                    if (currentNodeKey.Length > 0)
                                    {
                                        item.DisplayText = string.Join(' ', currentNodeKey.Split('_').Select(item => item[0].ToString().ToUpper() + item[1..]));
                                    }
                                    else
                                    {
                                        item.DisplayText = "Entry";
                                    }

                                    if (DefaultStringValueMatch.Success && !IsCurrentOptionalNode && currentNodeKey.Length > 0)
                                    {
                                        item.Value = item.DefaultValue = "\"" + DefaultStringValueMatch.Groups[1].Value + "\"";
                                        result.ResultString.Append("\"" + DefaultStringValueMatch.Groups[1].Value.ToLower() + "\"");
                                    }
                                    else
                                    if (!IsCurrentOptionalNode && currentNodeKey.Length > 0)
                                    {
                                        result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + currentNodeKey.ToLower() + "\": \"\"");
                                    }
                                    else
                                    if (currentNodeKey.Length == 0 && isAddToParent && parent is not null && (parent.DataType is DataType.List || parent.DataType is DataType.Array))
                                    {
                                        IsNoKeyOrMultiDataTypeItem = true;
                                        item.LayerCount = layerCount = parent.LayerCount + 1;
                                        item.RemoveElementButtonVisibility = Visibility.Visible;
                                        item.Value = "";
                                        item.IsCanBeDefaulted = false;
                                        result.ResultString.Append(new string(' ', item.LayerCount * 2) + "\"\"");
                                    }
                                    
                                    if(currentNodeKey.Length == 0 && (parent.DataType is DataType.List || (parent.DataType is DataType.MultiType && parent.SelectedValueType.Text == "List")))
                                    {
                                        item.RemoveElementButtonVisibility = Visibility.Visible;
                                    }
                                    item.Plan = plan;
                                    item.JsonItemTool = jsonTool;
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
                                    if (!IsCurrentOptionalNode && currentNodeKey.Length > 0)
                                    {
                                        item.Value = 0;
                                        result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + currentNodeKey.ToLower() + "\": 0");
                                    }
                                    else
                                    if (currentNodeKey.Length == 0 && isAddToParent && parent is not null && (parent.DataType is DataType.List || parent.DataType is DataType.Array || (parent.DataType is DataType.MultiType && parent.SelectedValueType is not null && parent.SelectedValueType.Text == "List" || parent.SelectedValueType.Text.Contains("array"))))
                                    {
                                        item.RemoveElementButtonVisibility = Visibility.Visible;
                                        item.Value = DefaultNumberValueMatch.Success ? decimal.Parse(DefaultNumberValueMatch.Groups[1].Value) : 0;
                                        item.IsCanBeDefaulted = IsCurrentOptionalNode = false;
                                        result.ResultString.Append(new string(' ', item.LayerCount * 2) + (DefaultNumberValueMatch.Success ? decimal.Parse(DefaultNumberValueMatch.Groups[1].Value) : "0"));
                                    }
                                    item.Plan = plan;
                                    item.JsonItemTool = jsonTool;
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
                        #region 判断是否需要重新分析上下文关键字
                        if (NBTFeatureList.Count < 3 && contextMatch.Success && parent is not null && parent.ChildrenStringList.Count == 1 && isAddToParent)
                        {
                            string contextString = contextMatch.Groups[1].Value;
                            //优先处理另类枚举
                            if (plan.EnumIDDictionary.TryGetValue(contextString, out List<string> targetList1))
                            {
                                CurrentCompoundItem.EnumItemsSource.AddRange(targetList1.Select(item =>
                                {
                                    return new TextComboBoxItem() { Text = item };
                                }));
                                CurrentCompoundItem.EnumBoxVisibility = Visibility.Visible;
                            }
                            else//转交给外观检查器，让其检索与当前节点源码匹配的数据上下文并添加子节点
                            {
                                CurrentCompoundItem.ChildrenStringList.Add(nodeList[i]);
                                HandlingTheTypingAppearanceOfCompositeItemList([CurrentCompoundItem],null);
                                if (CurrentCompoundItem.Children.Count > 0)
                                {
                                    CompoundJsonTreeViewItem subCompoundItem = CurrentCompoundItem.Children[0] as CompoundJsonTreeViewItem;
                                    CurrentCompoundItem.Children.Clear();
                                    item = CurrentCompoundItem = subCompoundItem;
                                }
                                else
                                {
                                    CurrentCompoundItem.ChildrenStringList.Clear();
                                }
                                if (CurrentCompoundItem.EnumKey.Length > 0)
                                {
                                    result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + CurrentCompoundItem.Key + "\": \"\"");
                                }
                            }
                        }
                        #endregion

                        #region 设置key
                        if (CurrentCompoundItem.Key.Length == 0 && currentNodeKey.Length > 0 && !(currentDescription == "根标签" || currentDescription == "根节点"))
                        {
                            CurrentCompoundItem.Key = currentNodeKey;
                        }
                        #endregion

                        #region 处理不同行为的复合型数据

                        #region 确定数据类型
                        if (NBTFeatureList.Count > 0)
                        {
                            CurrentCompoundItem.AddOrSwitchElementButtonVisibility = CurrentCompoundItem.EnumBoxVisibility is Visibility.Collapsed && (NBTFeatureList[0] == "compound" || NBTFeatureList[0] == "list" || NBTFeatureList[0].Contains("array")) && (CurrentCompoundItem.DataType is not DataType.MultiType || (CurrentCompoundItem.DataType is DataType.MultiType && CurrentCompoundItem.SelectedValueType.Text == "List")) ? Visibility.Visible : Visibility.Collapsed;
                            if (CurrentCompoundItem.AddOrSwitchElementButtonVisibility is Visibility.Visible)
                            {
                                if (NBTFeatureList[0] == "compound" && !CurrentCompoundItem.IsCanBeDefaulted)
                                {
                                    CurrentCompoundItem.ElementButtonTip = "折叠";
                                }
                                CurrentCompoundItem.SwitchButtonIcon = CurrentCompoundItem.PlusIcon;
                                CurrentCompoundItem.SwitchButtonColor = CurrentCompoundItem.PlusColor;
                                CurrentCompoundItem.PressedSwitchButtonColor = CurrentCompoundItem.PressedPlusColor;
                            }
                        }
                        if (currentNodeDataType == "compound" && NBTFeatureList.Count > 1 && IsCurrentOptionalNode)
                        {
                            CurrentCompoundItem.DataType = DataType.OptionalCompound;
                        }
                        else
                        if (currentNodeDataType == "list")
                        {
                            CurrentCompoundItem.ElementButtonTip = "添加到顶部";
                            CurrentCompoundItem.DataType = DataType.List;
                            if(currentNodeKey.Length == 0)
                            {
                                IsNoKeyOrMultiDataTypeItem = true;
                                CurrentCompoundItem.RemoveElementButtonVisibility = Visibility.Visible;
                                CurrentCompoundItem.DisplayText = "Entry";
                                CurrentCompoundItem.IsCanBeDefaulted = false;
                            }
                        }
                        else//数组会有数据类型的区分，所以这里用包含而不是等于
                        if (currentNodeDataType.Contains("array"))
                        {
                            CurrentCompoundItem.ElementButtonTip = "添加到顶部";
                            CurrentCompoundItem.DataType = DataType.Array;
                        }
                        #endregion

                        #region 更新非可选复合型节点的文本值与层数
                        if (CurrentCompoundItem.Key.Replace("\"", "").Length > 0 && CurrentCompoundItem.DisplayText.Length == 0)
                        {
                            CurrentCompoundItem.DisplayText = string.Join(' ', CurrentCompoundItem.Key.Split('_').Select(item => item[0].ToString().ToUpper() + item[1..]));
                        }
                        if(currentDescription == "根标签" || currentDescription == "根节点")
                        {
                            CurrentCompoundItem.DisplayText = "Root";
                        }
                        CurrentCompoundItem.LayerCount = layerCount;
                        if (CurrentCompoundItem.Key.Length > 0 && !IsCurrentOptionalNode && NBTFeatureList.Count < 3)
                        {
                            if (NBTFeatureList[0].Contains("list") || NBTFeatureList[0].Contains("array"))
                            {
                                result.ResultString.Append(new string(' ', CurrentCompoundItem.LayerCount * 2) + "\"" + currentNodeKey + "\": [");
                                result.ResultString.Append(']');
                            }
                            else
                            if (NBTFeatureList[0].Contains("compound") && !CurrentCompoundItem.Key.Contains('\''))
                            {
                                result.ResultString.Append(new string(' ', CurrentCompoundItem.LayerCount * 2) + "\"" + CurrentCompoundItem.Key.Replace("\"","") + "\": {");
                            }
                        }
                        else
                        if(CurrentCompoundItem.Key.Length == 0 && NBTFeatureList[0].Contains("list"))
                        {
                            CurrentCompoundItem.IsCanBeDefaulted = false;
                            CurrentCompoundItem.LayerCount = layerCount = parent.LayerCount + 1;
                            result.ResultString.Append(new string(' ', CurrentCompoundItem.LayerCount * 2) + "[]");
                        }
                        #endregion

                        #endregion

                        #region 处理方块属性
                        if(parent is not null && parent.Parent is not null && NBTFeatureList.Count > 2 && NBTFeatureList[^1].Contains("方块属性"))
                        {
                            bool isHaveIDItem = false;
                            CompoundJsonTreeViewItem grandParent = parent.Parent;
                            foreach (var parentItem in grandParent.Children)
                            {
                                Match parentItemMatch = GetEnumKey().Match(parentItem.InfoTipText);
                                if(parentItemMatch.Success && parentItemMatch.Groups[1].Value.Contains("ID"))
                                {
                                    isHaveIDItem = true;
                                }
                                if (parentItemMatch.Success && parentItemMatch.Groups[1].Value.Contains("ID") && parentItem is CompoundJsonTreeViewItem parentCompoundItem && parentCompoundItem.SelectedEnumItem is not null && parentCompoundItem.SelectedEnumItem.Text == "string" && plan.EnumCompoundDataDictionary["BlockStateProperty"].TryGetValue(parentCompoundItem.SelectedEnumItem.Text.Replace("minecraft:",""),out List<string> targetBlockPropertyList))
                                {
                                    CompoundJsonTreeViewItem oldPropertyItem = null;
                                    foreach (var blockPropertyItem in targetBlockPropertyList)
                                    {
                                        JObject propertyObject = JObject.Parse(blockPropertyItem);
                                        if(propertyObject.First is JProperty jProperty && propertyObject[jProperty.Name] is JArray jarray)
                                        {
                                            CompoundJsonTreeViewItem newPropertyItem = new(plan, jsonTool, _container)
                                            {
                                                Parent = parent,
                                                LayerCount = layerCount,
                                                IsCanBeDefaulted = true,
                                                Key = jProperty.Name,
                                                DisplayText = jProperty.Name[0].ToString().ToUpper() + jProperty.Name[1..],
                                                DataType = DataType.Enum,
                                                EnumBoxVisibility = Visibility.Visible
                                            };
                                            if(oldPropertyItem is not null)
                                            {
                                                oldPropertyItem.Next = newPropertyItem;
                                                newPropertyItem.Previous = oldPropertyItem;
                                            }
                                            if (newPropertyItem.IsCanBeDefaulted && newPropertyItem.EnumItemsSource.Count == 0)
                                            {
                                                newPropertyItem.EnumItemsSource.Add(new TextComboBoxItem() { Text = "- unset -" });
                                            }
                                            newPropertyItem.SelectedEnumItem = newPropertyItem.EnumItemsSource[0];
                                            foreach (var element in jarray)
                                            {
                                                newPropertyItem.EnumItemsSource.Add(new TextComboBoxItem() { Text = element.ToString() });
                                            }
                                            result.Result.Add(newPropertyItem);
                                            oldPropertyItem = newPropertyItem;
                                        }
                                    }
                                    result.ResultString.Clear();
                                    break;
                                }
                            }
                            if(isHaveIDItem)
                            {
                                result.ResultString.Clear();
                                return result;
                            }
                        }
                        #endregion

                        #region 处理非复合数据类型的CustomCompound
                        if (NBTFeatureList.Count == 2 && NBTFeatureList[1].Contains('<') && NBTFeatureList[0] != "any")
                        {
                            if (NBTFeatureList[1] != "<'>")
                            {
                                List<string> simpleDataTypeList = [.. dataStringType];
                                simpleDataTypeList.Remove("compound");
                                simpleDataTypeList.Remove("list");
                                bool IsSimpleCustomCompound = simpleDataTypeList.Intersect(NBTFeatureList).Any();
                                if (IsSimpleCustomCompound)
                                {
                                    CurrentCompoundItem.Parent = parent;
                                    CurrentCompoundItem.LayerCount = parent.LayerCount;
                                    CurrentCompoundItem.DataType = DataType.CustomCompound;
                                    CurrentCompoundItem.InputBoxVisibility = Visibility.Visible;
                                    CurrentCompoundItem.Key = CurrentCompoundItem.DisplayText = "";
                                    CurrentCompoundItem.AddOrSwitchElementButtonVisibility = Visibility.Visible;
                                    CurrentCompoundItem.SwitchButtonIcon = CurrentCompoundItem.PlusIcon;
                                    CurrentCompoundItem.SwitchButtonColor = CurrentCompoundItem.PlusColor;
                                    CurrentCompoundItem.PressedSwitchButtonColor = CurrentCompoundItem.PressedPlusColor;
                                    CurrentCompoundItem.ChildrenStringList.Add("{{nbt|" + NBTFeatureList[0] + "|<'>}}");
                                    result.Result.Add(CurrentCompoundItem);
                                    return result;
                                }
                            }
                            else
                            {
                                CurrentCompoundItem.IsCanBeDefaulted = false;
                                CurrentCompoundItem.Key = CurrentCompoundItem.DisplayText = currentReferenceKey;
                                CurrentCompoundItem.RemoveElementButtonVisibility = Visibility.Visible;
                                CurrentCompoundItem.DataType = (DataType)Enum.Parse(typeof(DataType), NBTFeatureList[0], true);
                                if(CurrentCompoundItem.DataType is not DataType.Bool)
                                {
                                    CurrentCompoundItem.InputBoxVisibility = Visibility.Visible;
                                }
                                else
                                {
                                    CurrentCompoundItem.BoolButtonVisibility = Visibility.Visible;
                                }
                                result.ResultString.Append(new string(' ',CurrentCompoundItem.LayerCount * 2) + "\"" + currentReferenceKey + "\": ");
                                switch (CurrentCompoundItem.DataType)
                                {
                                    case DataType.Bool:
                                        {
                                            result.ResultString.Append("false");
                                            break;
                                        }
                                    case DataType.String:
                                        {
                                            result.ResultString.Append("\"\"");
                                            break;
                                        }
                                    case DataType.Number:
                                    case DataType.Byte:
                                    case DataType.Decimal:
                                    case DataType.Short:
                                    case DataType.Int:
                                    case DataType.Float:
                                    case DataType.Double:
                                    case DataType.Long:
                                        {
                                            result.ResultString.Append('0');
                                            break;
                                        }
                                }
                                result.Result.Add(CurrentCompoundItem);
                                return result;
                            }
                        }
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
                            #region 处理Key与外观
                            if (NBTFeatureList[0] == "string")
                            {
                                CurrentCompoundItem.EnumBoxVisibility = Visibility.Visible;
                                if (parent is not null && parent.DataType is DataType.List)
                                {
                                    CurrentCompoundItem.RemoveElementButtonVisibility = Visibility.Visible;
                                }
                                List<string> enumSource = [];
                                string targetEnumKey = '#' + EnumMatch.Groups[1].Value;

                                if (plan.TranslateDictionary.TryGetValue(targetEnumKey, out targetEnumKey))
                                {

                                }

                                if (targetEnumKey is not null && plan.EnumIDDictionary.TryGetValue(targetEnumKey, out List<string> targetEnumIDList) && targetEnumIDList is not null && targetEnumIDList.Count > 0)
                                {
                                    enumSource.AddRange(targetEnumIDList);
                                }
                                else
                                if(targetEnumKey is not null && plan.EnumCompoundDataDictionary.TryGetValue(targetEnumKey,out Dictionary<string,List<string>> targetDictionary))
                                {
                                    enumSource.AddRange(targetDictionary.Keys);
                                    CurrentCompoundItem.EnumKey = targetEnumKey;
                                }
                                else
                                if (parent is not null)
                                {
                                    Match parentEnumMatch = GetEnumKey().Match(parent.InfoTipText);
                                    if (parentEnumMatch.Success && plan.EnumIDDictionary.TryGetValue(parentEnumMatch.Groups[1].Value, out List<string> parentEnumIDList))
                                    {
                                        enumSource.AddRange(parentEnumIDList);
                                    }
                                }

                                enumSource.AddRange([.. EnumCollectionMode1.Select(enum1 => { return enum1.Groups[1].Value; })]);
                                enumSource.AddRange([.. EnumCollectionMode2.Select(enum1 => { return enum1.Groups[1].Value; })]);
                                enumSource = [.. enumSource.Distinct()];
                                enumSource.Sort();

                                if (CurrentCompoundItem.EnumItemsSource.Count == 0)
                                {
                                    CurrentCompoundItem.EnumItemsSource.Add(new TextComboBoxItem() { Text = "- unset -" });
                                }
                                CurrentCompoundItem.EnumItemsSource.AddRange(enumSource.Select(enum1 =>
                                {
                                    return new TextComboBoxItem() { Text = enum1 };
                                }));
                                if (CurrentCompoundItem.EnumItemsSource.Count > 0)
                                {
                                    CurrentCompoundItem.SelectedEnumItem = CurrentCompoundItem.EnumItemsSource[0];
                                }

                                if (!CurrentCompoundItem.IsCanBeDefaulted && !isAddedStringInMulipleCode)
                                {
                                    result.ResultString.Append(new string(' ', layerCount * 2) +
                                        (currentNodeKey.Length > 0 ? "\"" + currentNodeKey.ToLower() + "\": " : "") + (!IsCurrentOptionalNode && CurrentCompoundItem.EnumItemsSource.Count > 0 ? "\"\"" : "\"\""));
                                }
                                if (currentNodeKey.Length == 0)
                                {
                                    IsNoKeyOrMultiDataTypeItem = true;
                                }
                            }
                            #endregion
                        }
                        if (IsPreIdentifiedAsEnumCompoundType)
                        {
                            CurrentCompoundItem.RemoveElementButtonVisibility = Visibility.Collapsed;
                            CurrentCompoundItem.EnumItemsSource.Clear();
                            CurrentCompoundItem.EnumItemCount = EnumItemCount;
                            EnumItemCount = 0;
                            CurrentCompoundItem.EnumBoxVisibility = Visibility.Visible;
                            CurrentCompoundItem.EnumItemsSource.Add(new TextComboBoxItem() { Text = "- unset -" });
                            CurrentCompoundItem.EnumItemsSource.AddRange(KeyList.Select(item => new TextComboBoxItem() { Text = item }));
                            CurrentCompoundItem.SelectedEnumItem = CurrentCompoundItem.EnumItemsSource.FirstOrDefault();
                            KeyList.Clear();
                            CurrentCompoundItem.EnumKey = CurrentEnumKey;
                            //这行范围删除源码的操作，可能会导致错误
                            nodeList.RemoveRange(i + 1, nodeList.Count - i - 1);
                        }
                        #endregion

                        #region 处理复合枚举
                        if (plan.TranslateDictionary.TryGetValue(currentNodeKey.Trim('!'), out string targetKey) && isHaveExtraField)
                        {
                            CurrentCompoundItem.DataType = DataType.Enum;
                            CurrentCompoundItem.AddOrSwitchElementButtonVisibility = Visibility.Collapsed;
                            CurrentCompoundItem.IsCanBeDefaulted = false;
                        }
                        bool isParentEnumCompound = false;
                        if(targetKey is null && parent is not null && parent.EnumKey.Length > 0 && plan.EnumCompoundDataDictionary.TryGetValue(parent.EnumKey,out Dictionary<string,List<string>> enumCompoundDataDictionary) && enumCompoundDataDictionary.TryGetValue(currentNodeKey.Replace("minecraft:","").Trim('!'),out List<string> enumCompoundList))
                        {
                            targetKey = parent.EnumKey;
                            isParentEnumCompound = true;
                        }
                        else
                        if(parent is null && contextMatch.Success && plan.TranslateDictionary.TryGetValue('#' + contextMatch.Groups[1].Value,out targetKey))
                        {
                        }

                        #region 处理自定义Key节点的逻辑运算符
                        bool isInvertMode = false;
                        if (NBTFeatureList[^1].StartsWith('!') || (parent is not null && parent.SelectedEnumItem is not null && parent.SelectedEnumItem.Text.Contains('!')))
                        {
                            isInvertMode = true;
                        }
                        #endregion

                        //检测到需要处理的枚举符合类型的固定内容
                        if (targetKey is not null && targetKey.Length > 0 && i >= currentContextNextIndex)
                        {
                            currentContextNextIndex = 0;

                            if (plan.DependencyItemList.TryGetValue(targetKey, out List<string> targetList))
                            {
                                List<string> list = [.. targetList];
                                nodeList.InsertRange(i, list);
                            }
                            else
                            if (plan.EnumCompoundDataDictionary.TryGetValue(targetKey, out Dictionary<string, List<string>> targetEnumDictionary))
                            {
                                if (isHaveExtraField || NBTFeatureList[^1].Contains('\'') || contextMatch.Success)
                                {
                                    Match nextLineStarMatch = (i + 1) < nodeList.Count ? GetLineStarCount().Match(nodeList[i + 1]) : GetLineStarCount().Match("");
                                    List<string> subFeatureList = (i + 1) < nodeList.Count ? GetHeadTypeAndKeyList(nodeList[i + 1]) : [];
                                    subFeatureList = RemoveUIMarker(subFeatureList);
                                    if (nextLineStarMatch.Value.Trim().Length > starCount)
                                    {
                                        CurrentCompoundItem.Key = (isInvertMode ? '!' : "") + subFeatureList[^1];
                                    }
                                    if (parent is not null)
                                    {
                                        CurrentCompoundItem.LayerCount = layerCount + 1;
                                        CurrentCompoundItem.ElementButtonTip = "添加到顶部";
                                        CurrentCompoundItem.SwitchButtonIcon = CurrentCompoundItem.PlusIcon;
                                        CurrentCompoundItem.SwitchButtonColor = CurrentCompoundItem.PlusColor;
                                        CurrentCompoundItem.PressedSwitchButtonColor = CurrentCompoundItem.PressedPlusColor;
                                    }
                                    CurrentCompoundItem.DisplayText = (isInvertMode ? '!' : "") + (CurrentCompoundItem.Key.Length > 0 ? string.Join(' ', CurrentCompoundItem.Key.Split('_').Select(item => item[0].ToString().ToUpper() + item[1..])) : "");
                                    CurrentCompoundItem.EnumKey = targetKey;
                                    CurrentCompoundItem.EnumBoxVisibility = Visibility.Visible;
                                    string logicOperator = isInvertMode ? "!" : "";
                                    if (CurrentCompoundItem.EnumItemsSource.Count == 0)
                                    {
                                        CurrentCompoundItem.EnumItemsSource.Add(new TextComboBoxItem() { Text = "- unset -" });
                                    }
                                    CurrentCompoundItem.EnumItemsSource.AddRange(targetEnumDictionary.Keys.Select(item =>
                                    {
                                        return new TextComboBoxItem()
                                        {
                                            Text = logicOperator + item
                                        };
                                    }));
                                    result.ResultString.Clear();
                                    //处理自定义key值的节点
                                    if (parent is not null)
                                    {
                                        result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + currentReferenceKey + "\": {" + (!isInvertMode ? "\r\n" + new string(' ', CurrentCompoundItem.LayerCount * 2) + "\"" + CurrentCompoundItem.Key + "\": \"\"\r\n" + new string(' ', layerCount * 2) : "") + "}");
                                    }
                                    else
                                    {
                                        result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + CurrentCompoundItem.Key + "\": \"\"");
                                    }

                                    if (isHaveExtraField)
                                    {
                                        CompoundJsonTreeViewItem referenceKeyItem = new(plan, jsonTool, _container)
                                        {
                                            RemoveElementButtonVisibility = Visibility.Visible,
                                            Key = currentReferenceKey,
                                            DisplayText = currentReferenceKey,
                                            LayerCount = layerCount,
                                            DataType = DataType.Compound,
                                            EnumItemsSource = []
                                        };
                                        CurrentCompoundItem.Parent = referenceKeyItem;
                                        if (!isInvertMode)
                                        {
                                            referenceKeyItem.Children.Add(CurrentCompoundItem);
                                        }
                                        result.Result.Add(referenceKeyItem);
                                    }
                                    else
                                    if(parent is not null)
                                    {
                                        CurrentCompoundItem.Key = CurrentCompoundItem.DisplayText = "";
                                        CurrentCompoundItem.DataType = DataType.CustomCompound;
                                        CurrentCompoundItem.LayerCount = layerCount - 1;
                                        CurrentCompoundItem.EnumBoxVisibility = CurrentCompoundItem.AddOrSwitchElementButtonVisibility = Visibility.Visible;
                                        CurrentCompoundItem.Parent = parent;
                                        result.ResultString.Clear();
                                        result.Result.Add(CurrentCompoundItem);
                                    }
                                    CurrentCompoundItem.SelectedEnumItem = CurrentCompoundItem.EnumItemsSource.FirstOrDefault();
                                    if (isHaveExtraField)
                                    {
                                        return result;
                                    }
                                    else
                                    if (NBTFeatureList[^1].Contains('\''))
                                    {
                                        continue;
                                    }
                                }
                                else
                                if (isParentEnumCompound)
                                {
                                    CurrentCompoundItem.RemoveElementButtonVisibility = Visibility.Visible;
                                    if (isInvertMode)
                                    {
                                        CurrentCompoundItem.Key = CurrentCompoundItem.DisplayText = '!' + CurrentCompoundItem.Key;
                                        CurrentCompoundItem.IsCanBeDefaulted = false;
                                        CurrentCompoundItem.ValueTypeBoxVisibility = Visibility.Collapsed;
                                        CurrentCompoundItem.AddOrSwitchElementButtonVisibility = Visibility.Collapsed;
                                        CurrentCompoundItem.DataType = DataType.Compound;
                                        result.Result.Add(CurrentCompoundItem);
                                        result.ResultString.Clear();
                                        result.ResultString.Append(new string(' ', layerCount * 2) + "\"!" + currentNodeKey + "\": {}");
                                        return result;
                                    }
                                }
                            }
                        }
                        else
                        if (currentReferenceKey.Length > 0 && currentNodeKey.Contains('\''))
                        {
                            CurrentCompoundItem.Key = CurrentCompoundItem.DisplayText = (isInvertMode ? '!' : "") + currentReferenceKey;
                            CurrentCompoundItem.RemoveElementButtonVisibility = Visibility.Visible;
                            IsCurrentOptionalNode = CurrentCompoundItem.IsCanBeDefaulted = false;
                            if (!isHaveExtraField)
                            {
                                //CurrentCompoundItem.LayerCount = layerCount = parent.LayerCount;
                                if (parent.DataType is DataType.CustomCompound && NBTFeatureList.Count < 3)
                                {
                                    result.ResultString.Append(new string(' ', parent.LayerCount * 2) + "\"" + currentReferenceKey + "\": {");
                                }
                            }
                        }
                        else//处理枚举型CustomCompound节点
                        if (currentNodeKey.Contains('\'') && parent is not null && targetKey is not null && targetKey.Length > 0 && plan.EnumIDDictionary.TryGetValue(targetKey, out List<string> targetIDList))
                        {
                            CurrentCompoundItem.Key = CurrentCompoundItem.DisplayText = "";
                            CurrentCompoundItem.ChildrenStringList = parent.ChildrenStringList;
                            CurrentCompoundItem.AddOrSwitchElementButtonVisibility = Visibility.Visible;
                            CurrentCompoundItem.DataType = DataType.CustomCompound;
                            CurrentCompoundItem.EnumBoxVisibility = Visibility.Visible;
                            if (CurrentCompoundItem.IsCanBeDefaulted)
                            {
                                CurrentCompoundItem.EnumItemsSource.Add(new TextComboBoxItem() { Text = "- unset -" });
                            }
                            CurrentCompoundItem.EnumItemsSource.AddRange(targetIDList.Select(item => new TextComboBoxItem() { Text = item }));
                            CurrentCompoundItem.SelectedEnumItem = CurrentCompoundItem.EnumItemsSource[0];
                            result.ResultString.Clear();
                            result.Result.Add(CurrentCompoundItem);
                            return result;
                        }
                        else
                        if(currentNodeKey.Contains('<') && currentNodeKey.Contains('>'))
                        {
                            CurrentCompoundItem.DataType = DataType.CustomCompound;
                            CurrentCompoundItem.Value = "";
                            CurrentCompoundItem.AddOrSwitchElementButtonVisibility = Visibility.Visible;
                            if (CurrentCompoundItem.ValueTypeSource.Count > 0)
                            {
                                CurrentCompoundItem.ValueTypeBoxVisibility = Visibility.Visible;
                            }
                            string firstType = CurrentCompoundItem.ValueTypeSource[0].Text.ToLower();
                            if (firstType != "bool")
                            {
                                CurrentCompoundItem.InputBoxVisibility = Visibility.Visible;
                            }
                            else
                            if(firstType != "compound" && firstType != "list" && !firstType.Contains("array"))
                            {
                                CurrentCompoundItem.BoolButtonVisibility = Visibility.Visible;
                            }
                            CurrentCompoundItem.Key = CurrentCompoundItem.DisplayText = "";
                            result.ResultString.Clear();
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
                        //分支文档有主动分支和被动分支两种情况
                        if (i + 1 < nodeList.Count)
                        {
                            Match nextEnumLineMatch = GetEnumRawKey().Match(nodeList[i + 1]);
                            if (nextEnumLineMatch.Success)
                            {
                                if (isAddedStringInMulipleCode || CurrentCompoundItem.IsCanBeDefaulted)
                                {
                                    result.ResultString.Append(new string(' ', CurrentCompoundItem.LayerCount * 2) + "\"" + CurrentCompoundItem.Key + "\": \"\"");
                                }
                                CurrentCompoundItem.IsEnumBranch = true;
                                CurrentCompoundItem.ChildrenStringList.AddRange(nodeList.Skip(i + 1));
                                CurrentCompoundItem.IsCanBeDefaulted = false;
                                HaveBranches = true;
                                i = nodeList.Count - 1;
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
                                        Tuple<List<string>,int> currentSubChildrenTuple = CollectionSubItem(nodeList, i, starCount,nextLineStar.Length);
                                        #endregion

                                        #region 根据递归的情况来向后移动N个迭代单位
                                        if (currentSubChildrenTuple.Item1.Count > 0)
                                        {
                                            i = currentSubChildrenTuple.Item2 - 1;
                                        }
                                        #endregion

                                        #region 是否存储原始信息、处理递归
                                        if ((NBTFeatureList[0] == "compound" || NBTFeatureList[0].Contains("array")) && currentSubChildrenTuple.Item1.Count > 0 && !IsCurrentOptionalNode)
                                        {
                                            #region 如果子信息只有一条并引用指定文件，则将其文档内容取出并直接解析
                                            Match subInheritMatch = GetInheritString().Match(currentSubChildrenTuple.Item1.Count > 0 ? currentSubChildrenTuple.Item1[0] : "");
                                            int index = subInheritMatch.Groups[1].Value.IndexOf('|');
                                            string subInheritMatchString = subInheritMatch.Groups[1].Value;
                                            if(index != -1)
                                            {
                                                subInheritMatchString = subInheritMatch.Groups[1].Value[..index];
                                            }
                                            if (currentSubChildrenTuple.Item1.Count == 1 && subInheritMatch.Success && plan.DependencyItemList.TryGetValue("#Inherit" + subInheritMatchString, out List<string> subTargetInheritList))
                                            {
                                                currentSubChildrenTuple.Item1.Clear();
                                                currentSubChildrenTuple.Item1.AddRange(subTargetInheritList);
                                            }
                                            #endregion

                                            #region 执行递归
                                            JsonTreeViewDataStructure subResult = GetTreeViewItemResult(new(), [..currentSubChildrenTuple.Item1], layerCount + 1, currentReferenceKey, CurrentCompoundItem, null, previousStarCount, isAddToParent);
                                            CurrentCompoundItem.Children.AddRange(subResult.Result);
                                            #endregion

                                            #region 处理外观并收尾
                                            if (subResult.Result.Count > 0)
                                            {
                                                CurrentCompoundItem.AddOrSwitchElementButtonVisibility = Visibility.Collapsed;
                                            }

                                            if (subResult.ResultString.Length > 0 && (!IsCurrentOptionalNode || isAddToParent))
                                            {
                                                result.ResultString.Append("\r\n" + subResult.ResultString + "\r\n");
                                                if(!isCompoundRoot && !isListRoot)
                                                {
                                                    result.ResultString.Append(new string(' ', layerCount * 2) + "}");
                                                }
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
                                            #endregion
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

                                        if (CurrentCompoundItem.Children.Count == 0 || (CurrentCompoundItem.DisplayText == "Root" && CurrentCompoundItem.Parent is null))
                                        {
                                            CurrentCompoundItem.ChildrenStringList.AddRange(currentSubChildrenTuple.Item1);
                                        }
                                        #endregion

                                        #region 判断后一个节点是否可选
                                        IsHaveNextNode = i < nodeList.Count - 1;
                                        #endregion

                                        //赋予Plan属性
                                        item.Plan ??= plan;
                                    }
                                    else
                                    {
                                        //没有子信息时，提取当前行的上下文信息作为子信息，因为引用一定包含在当前行中
                                        if (contextMatch.Success && NBTFeatureList.Count < 3)
                                        {
                                            CurrentCompoundItem.ChildrenStringList.Add("*{{nbt|compound}} [[#" + contextMatch.Groups[1].Value + "]]");
                                        }
                                    }
                                }
                            }
                        }
                        #endregion

                        //设置描述
                        CurrentCompoundItem.InfoTipText = currentDescription;
                        IsPreIdentifiedAsEnumCompoundType = false;
                    }
                    #endregion
                }
                #endregion

                #region 符合条件时将当前节点与前一个合并
                if(previous is not null && item.Key == previous.Key && item.Key.Length > 0)
                {
                    List<string> previousDataTypeList = [];
                    if(item is CompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.DataType is not DataType.None)
                    {
                        previousDataTypeList.Add(compoundJsonTreeViewItem.DataType.ToString());
                    }

                    bool optionalNode = previous.IsCanBeDefaulted;
                    if (previous is not CompoundJsonTreeViewItem)
                    {
                        previous = new CompoundJsonTreeViewItem(plan, jsonTool, _container)
                        {
                            IsCanBeDefaulted = optionalNode
                        };
                    }
                    if(previous is CompoundJsonTreeViewItem previousCompoundItem)
                    {
                        if (previousCompoundItem.DataType is not DataType.None)
                        {
                            previousDataTypeList.Add(previousCompoundItem.DataType.ToString());
                        }
                        for (int j = 0; j < previousDataTypeList.Count; j++)
                        {
                            if (previousDataTypeList[j].Contains("Compound"))
                            {
                                previousDataTypeList[j] = "Compound";
                            }
                            if (previousDataTypeList[j].Contains("Array"))
                            {
                                previousDataTypeList[j] = "Array";
                            }
                        }
                        previousDataTypeList.Sort();
                        if (previousDataTypeList[0] == "Compound")
                        {
                            previousCompoundItem.ElementButtonTip = "展开";
                        }
                        else
                        {
                            previousCompoundItem.ElementButtonTip = "添加在顶部";
                        }
                        if (previousCompoundItem.DataType is DataType.List ||
                           previousCompoundItem.DataType is DataType.Array ||
                           previousCompoundItem.DataType is DataType.Compound ||
                           previousCompoundItem.DataType is DataType.OptionalCompound)
                        {
                            previousCompoundItem.AddOrSwitchElementButtonVisibility = Visibility.Visible;
                        }
                        else
                        {
                            previousCompoundItem.AddOrSwitchElementButtonVisibility = Visibility.Collapsed;
                        }
                        previousCompoundItem.DataType = DataType.MultiType;
                        previousCompoundItem.ValueTypeBoxVisibility = Visibility.Visible;
                        previousCompoundItem.ValueTypeSource.AddRange(previousDataTypeList.Select(previousValueTypeItem => new TextComboBoxItem() { Text = previousValueTypeItem }));
                        previousCompoundItem.SelectedValueType = previousCompoundItem.ValueTypeSource.FirstOrDefault();
                        if (item is CompoundJsonTreeViewItem CurrentCompoundItem)
                        {
                            previousCompoundItem.ChildrenStringList = CurrentCompoundItem.ChildrenStringList;
                        }
                    }
                }
                #endregion

                #region 处理节点的追加
                if (i < nodeList.Count - 1 && !result.ResultString.ToString().TrimEnd(['\r', '\n', ' ']).EndsWith(',') && !IsCurrentOptionalNode && IsHaveNextNode && (previous is null || (previous is not null && (previous.Key != item.Key || item.Key.Length == 0))))
                {
                    result.ResultString.Append(",\r\n");
                }

                if (previous is null || (previous is not null && (previous.Key != item.Key || item.Key.Length == 0)))
                {
                    result.Result.Add(item);
                }
                #endregion

                #region 保存信息
                //将当前节点保存为上一个节点
                if (previous is null || (previous is not null && previous.Key != item.Key))
                {
                    previous = item;
                }
                //保存当前的*号数量
                previousStarCount = starCount;
                #endregion
            }

            #region 如果父节点类型为列表，则将当前计算结果放入对象节点中
            if (parent is not null && (parent.DataType is DataType.List || (parent.DataType is DataType.MultiType && parent.SelectedValueType is not null && parent.SelectedValueType.Text == "List")) && (HadPreIdentifiedAsEnumCompoundType || nodeList.Count > 1) && !IsNoKeyOrMultiDataTypeItem)
            {
                CompoundJsonTreeViewItem entry = new(plan, jsonTool, _container)
                {
                    DataType = DataType.Compound,
                    RemoveElementButtonVisibility = Visibility.Visible,
                    DisplayText = "Entry",
                    LayerCount = parent.LayerCount + 1,
                    Parent = parent
                };
                foreach (var item in result.Result)
                {
                    item.LayerCount = entry.LayerCount + 1;
                    item.RemoveElementButtonVisibility = Visibility.Collapsed;
                }
                entry.Children.AddRange([.. result.Result]);
                result.ResultString = result.ResultString.Replace("\r\n","\r\n  ");
                result.ResultString.Insert(0,"  ");
                while (result.ResultString.Length > 0 && (result.ResultString[^1] == ',' || result.ResultString[^1] == '\r' || result.ResultString[^1] == '\n' || result.ResultString[^1] == ' '))
                {
                    result.ResultString.Length--;
                }
                result.ResultString.Insert(0, new string(' ', entry.LayerCount * 2) + "{\r\n").Append("\r\n" + new string(' ', entry.LayerCount * 2) + '}');
                result.Result.Clear();
                result.Result.Add(entry);
            }
            #endregion

            #region 处理多重类型根节点
            if (isCompoundRoot || isListRoot)
            {
                result.IsHaveRootItem = true;
                while (result.ResultString[^1] == ' ' || result.ResultString[^1] == ',' ||
                    result.ResultString[^1] == '\r' || result.ResultString[^1] == '\n')
                {
                    result.ResultString.Length--;
                }
                result.ResultString.Append("\r\n");
                if (isCompoundRoot)
                {
                    result.ResultString.Append('}');
                }
                else
                if(isListRoot)
                {
                    result.ResultString.Append(']');
                }
            }
            #endregion

            return result;
        }

        /// <summary>
        /// 接收文档内容并生成树视图
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public JsonTreeViewDataStructure GenerateTreeViewItemListFromJson(string data)
        {
            JsonTreeViewDataStructure result = new();

            JsonTextReader jsonTextReader = new(new StringReader(data));
            while (jsonTextReader.Read())
            {
                string path = jsonTextReader.Path;
                switch (jsonTextReader.TokenType)
                {
                    case JsonToken.None:
                        break;
                    case JsonToken.StartObject:
                        break;
                    case JsonToken.StartArray:
                        break;
                    case JsonToken.StartConstructor:
                        break;
                    case JsonToken.PropertyName:
                        break;
                    case JsonToken.Raw:
                        break;
                    case JsonToken.Integer:
                        break;
                    case JsonToken.Float:
                        break;
                    case JsonToken.String:
                        break;
                    case JsonToken.Boolean:
                        break;
                    case JsonToken.Null:
                        break;
                    case JsonToken.Undefined:
                        break;
                    case JsonToken.EndObject:
                        break;
                    case JsonToken.EndArray:
                        break;
                    case JsonToken.EndConstructor:
                        break;
                    case JsonToken.Date:
                        break;
                    case JsonToken.Bytes:
                        break;
                    default:
                        break;
                }
            }

            return result;
        }
    }
}