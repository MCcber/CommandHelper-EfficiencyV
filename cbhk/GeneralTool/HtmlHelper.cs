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
using System.Text;
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

        private List<string> EnumKeyList = ["[[命名空间ID]]", "[[内容类型ID]]"];

        private List<string> dataStringType = ["string", "bool", "int", "short", "float", "double", "long", "decimal", "compound", "list"];

        [GeneratedRegex(@"\s*\s?\*+\s*\s?{{")]
        private static partial Regex JudgeHead();
        [GeneratedRegex(@"\s?\s*\*+\s?\s*{{([nN]bt\s)?inherit(?<1>[a-z_/|=*\s]+)}}\s?\s*", RegexOptions.IgnoreCase)]
        private static partial Regex GetInheritString();

        [GeneratedRegex(@"(其余的附加条件，)?取决于{{nbt\|string\|(?<1>[a-z_:]+)}}的值")]
        private static partial Regex GetExtraKey();

        [GeneratedRegex(@"(?<=与).+(?=不能同时存在)")]
        private static partial Regex GetMutexKey();

        [GeneratedRegex(@"(?<=\s*\s?\*+;?\s*\s?(如果|若|当)).+(?=为|是).+")]
        private static partial Regex GetEnumRawKey();

        [GeneratedRegex(@"\[\[\#?((?<1>[\u4e00-\u9fff]+)\|(?<2>[\u4e00-\u9fff]+)|(?<1>[\u4e00-\u9fff]+))\]\]")]
        private static partial Regex GetContextKey();

        [GeneratedRegex(@"\[\[(?<1>[a-zA-Z_\u4e00-\u9fff|#]+)\]\]")]
        private static partial Regex GetEnumKey();

        [GeneratedRegex(@"^\s*\s?\:?\s*\s?(\*+)")]
        private static partial Regex GetLineStarCount();

        [GeneratedRegex(@"{{interval(\|left=(?<1>\d+))?(\|right=(?<2>\d+))?}}", RegexOptions.IgnoreCase)]
        private static partial Regex GetNumberRange();

        [GeneratedRegex(@"required=1", RegexOptions.IgnoreCase)]
        private static partial Regex GetRequiredKey();

        [GeneratedRegex(@"默认为\{\{cd\|(?<1>[a-z_]+)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultStringValue();

        [GeneratedRegex(@"默认为(?<1>\d)+", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultNumberValue();

        [GeneratedRegex(@"默认为\{\{cd\|(?<1>true|false)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultBoolValue();

        [GeneratedRegex(@"(?<=默认为<code>)(?<1>[a-z:_]+)(?=</code>)", RegexOptions.IgnoreCase)]
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
        /// 生成一个自定义Key值的复合节点
        /// </summary>
        /// <param name="template">模板节点</param>
        /// <returns></returns>
        private BaseCompoundJsonTreeViewItem GetCustomKeyBaseCompoundItem(BaseCompoundJsonTreeViewItem template)
        {
            BaseCompoundJsonTreeViewItem result = new(plan, jsonTool, _container)
            {
                DataType = template.DataType
            };

            result.Key = template.Key;
            result.EnumKey = template.EnumKey;
            result.Value = template.Value;
            result.IsCurrentExpanded = true;
            result.Parent = template.Parent;
            result.ItemType = template.ItemType;
            result.LayerCount = template.Parent.LayerCount;
            result.DisplayText = template.DisplayText;
            result.AddOrSwitchElementButtonVisibility = template.AddOrSwitchElementButtonVisibility;
            result.RemoveElementButtonVisibility = template.RemoveElementButtonVisibility;
            result.SelectedValueType = template.SelectedValueType;
            result.SelectedEnumItem = template.SelectedEnumItem;
            result.IsCanBeDefaulted = template.IsCanBeDefaulted;
            result.ArrayChildrenStringList = template.ArrayChildrenStringList;
            result.CompoundChildrenStringList = template.CompoundChildrenStringList;
            result.ListChildrenStringList = template.ListChildrenStringList;
            result.InputBoxVisibility = template.InputBoxVisibility;
            result.ValueTypeBoxVisibility = template.ValueTypeBoxVisibility;
            result.EnumBoxVisibility = template.EnumBoxVisibility;
            result.EnumItemsSource = template.EnumItemsSource;
            result.ValueTypeSource = template.ValueTypeSource;

            if (result.AddOrSwitchElementButtonVisibility is Visibility.Visible)
            {
                result.ElementButtonTip = "展开";
                result.SwitchButtonIcon = result.PlusIcon;
                result.SwitchButtonColor = result.PlusColor;
                result.PressedSwitchButtonColor = result.PressedPlusColor;
            }
            return result;
        }

        private JsonTreeViewDataStructure GetUUIDJsonTreeViewItem(bool isHighVersion, bool isCanBeDefault, int layerCount)
        {
            ObservableCollection<JsonTreeViewItem> result = [];
            StringBuilder resultString = new();
            for (int i = 0; i < 4; i++)
            {
                JsonTreeViewItem jsonTreeViewItem = new()
                {
                    Plan = plan,
                    JsonItemTool = jsonTool,
                    InputBoxVisibility = Visibility.Visible,
                    DisplayText = "Entry",
                    IsCanBeDefaulted = isCanBeDefault
                };
                if (!isHighVersion)
                {
                    jsonTreeViewItem.DataType = DataType.String;
                    if (!isCanBeDefault)
                    {
                        jsonTreeViewItem.DefaultValue = jsonTreeViewItem.Value = "\"\"";
                    }
                }
                else
                {
                    jsonTreeViewItem.DataType = DataType.Int;
                    if (!isCanBeDefault)
                    {
                        jsonTreeViewItem.DefaultValue = jsonTreeViewItem.Value = "0";
                    }
                }
                result.Add(jsonTreeViewItem);
                resultString.Append("\r\n" + new string(' ', layerCount * 2) + jsonTreeViewItem.Value + ',');
            }
            return new() { Result = result, ResultString = resultString };
        }

        private JsonTreeViewItem GetReverseJsonTreeViewItem(string key)
        {
            return new JsonTreeViewItem()
            {
                Plan = plan,
                JsonItemTool = jsonTool,
                IsCanBeDefaulted = false,
                DataType = DataType.Object,
                DisplayText = key,
                Key = key,
                RemoveElementButtonVisibility = Visibility.Visible
            };
        }

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
                        string title = target[i].Replace(RootDirectory, "").Replace(".wiki", "").Replace("\\", "/") + "";
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
        public void HandlingTheTypingAppearanceOfCompositeItemList(List<JsonTreeViewItem> list, BaseCompoundJsonTreeViewItem parent)
        {
            for (int i = 0; i < list.Count; i++)
            {
                #region 处理冲突节点

                #region 如果枚举节点成员结构与同层其它子节点Key相同
                if (i > 0 && list[i - 1] is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem && baseCompoundJsonTreeViewItem.ItemType is ItemType.Enum && baseCompoundJsonTreeViewItem.EnumKey.Length > 0 &&
                    plan.EnumCompoundDataDictionary.TryGetValue(baseCompoundJsonTreeViewItem.EnumKey, out Dictionary<string, List<string>> targetDictionary) &&
                    targetDictionary.Count > 0)
                {
                    List<string> firstOrDefault = targetDictionary.FirstOrDefault().Value;
                    if (firstOrDefault.Count > 0)
                    {
                        List<string> PreviousNBTFeatureList = GetHeadTypeAndKeyList(firstOrDefault[0]);
                        PreviousNBTFeatureList = RemoveUIMarker(PreviousNBTFeatureList);
                        PreviousNBTFeatureList = [.. PreviousNBTFeatureList.Except(dataStringType)];
                        if (PreviousNBTFeatureList.Count > 0 && PreviousNBTFeatureList[^1] == list[i].Key)
                        {
                            list.RemoveAt(i);
                            continue;
                        }
                    }
                }
                #endregion

                #endregion

                if (list[i] is BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem)
                {
                    #region 是否直接返回
                    if (compoundJsonTreeViewItem.ItemType is ItemType.CustomCompound || compoundJsonTreeViewItem.ItemType is ItemType.BottomButton)
                    {
                        continue;
                    }
                    #endregion

                    #region 处理键入外观
                    for (int j = 0; j < compoundJsonTreeViewItem.CompoundChildrenStringList.Count; j++)
                    {
                        string childString = compoundJsonTreeViewItem.CompoundChildrenStringList[j];
                        Match contentMatch = GetContextKey().Match(childString);
                        if (contentMatch.Success && (list[i].DataType is not DataType.None || (list[i] is BaseCompoundJsonTreeViewItem compoundElementItem && ((compoundElementItem.ItemType is not ItemType.List && compoundElementItem.ItemType is not ItemType.OptionalCompound) || (compoundElementItem.ItemType is ItemType.MultiType && compoundElementItem.SelectedValueType.Text != "List")))))
                        {
                            string key = contentMatch.Groups[1].Value;
                            string targetKey2 = "";
                            BaseCompoundJsonTreeViewItem subCompoundItem = new(plan, jsonTool, _container)
                            {
                                LayerCount = compoundJsonTreeViewItem.LayerCount,
                                ItemType = ItemType.CustomCompound,
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
                                            compoundJsonTreeViewItem.IsProcessedByAppearanceChecker = true;
                                            compoundJsonTreeViewItem.SwitchButtonIcon = compoundJsonTreeViewItem.MinusIcon;
                                            compoundJsonTreeViewItem.SwitchButtonColor = compoundJsonTreeViewItem.MinusColor;
                                            compoundJsonTreeViewItem.LogicChildren.Add(subCompoundItem);
                                            subCompoundItem.Parent = compoundJsonTreeViewItem;
                                        }
                                    }
                                }
                            }
                            else
                            if (plan.TranslateDictionary.TryGetValue(key, out targetKey2))
                            {
                            }
                            else
                            if (plan.TranslateDictionary.TryGetValue(compoundJsonTreeViewItem.Key, out targetKey2))
                            {
                            }

                            if (!isDependencyItemListKey && targetKey2 is not null)
                            {
                                if (plan.DependencyItemList.TryGetValue(targetKey2, out List<string> subChildrenList))
                                {
                                    if (subCompoundItem.CompoundChildrenStringList.Count == 0)
                                    {
                                        subCompoundItem.CompoundChildrenStringList = subChildrenList;
                                    }
                                    for (int k = 0; k < subChildrenList.Count; k++)
                                    {
                                        List<string> subNBTFeatureList = GetHeadTypeAndKeyList(subChildrenList[k]);
                                        if (subNBTFeatureList.Count > 0 && subNBTFeatureList[^1].Contains('\'') && subNBTFeatureList[^1].Contains('<'))
                                        {
                                            subCompoundItem.IsProcessedByAppearanceChecker = true;
                                            subCompoundItem.InputBoxVisibility = Visibility.Visible;
                                            subCompoundItem.SwitchButtonIcon = compoundJsonTreeViewItem.PlusIcon;
                                            subCompoundItem.SwitchButtonColor = compoundJsonTreeViewItem.PlusColor;
                                            compoundJsonTreeViewItem.LogicChildren.Insert(0, subCompoundItem);
                                            subCompoundItem.Parent = compoundJsonTreeViewItem;
                                            compoundJsonTreeViewItem.AddOrSwitchElementButtonVisibility = Visibility.Collapsed;
                                            compoundJsonTreeViewItem.CompoundChildrenStringList.RemoveAt(i);
                                        }
                                    }
                                }
                                else
                                if (plan.EnumCompoundDataDictionary.TryGetValue(targetKey2, out Dictionary<string, List<string>> dictionary) && (compoundJsonTreeViewItem.EnumItemsSource.Count == 0 || (compoundJsonTreeViewItem.EnumItemsSource.Count > 1 && !dictionary.ContainsKey(compoundJsonTreeViewItem.EnumItemsSource[1].Text))))
                                {
                                    BaseCompoundJsonTreeViewItem targetItem = null;
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
                                            targetItem.ItemType = ItemType.Enum;
                                            targetItem.DataType = DataType.String;
                                            targetItem.LayerCount = compoundJsonTreeViewItem.LayerCount + 1;
                                        }
                                        targetItem.EnumKey = targetKey2;
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
                                        compoundJsonTreeViewItem.IsProcessedByAppearanceChecker = true;
                                        compoundJsonTreeViewItem.SwitchButtonIcon = compoundJsonTreeViewItem.MinusIcon;
                                        compoundJsonTreeViewItem.SwitchButtonColor = compoundJsonTreeViewItem.MinusColor;
                                        compoundJsonTreeViewItem.LogicChildren.Add(targetItem);
                                        if (compoundJsonTreeViewItem.Key.Length == 0 && dictionary.TryGetValue(targetItem.EnumItemsSource[1].Text, out List<string> firstItemRawStringList))
                                        {
                                            string firstItemRawString = firstItemRawStringList[0];
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

                #region 执行递归
                if (list[i] is BaseCompoundJsonTreeViewItem baseItem && baseItem.LogicChildren.Count > 0)
                {
                    List<JsonTreeViewItem> jsonTreeViewItemList = [.. baseItem.LogicChildren];
                    HandlingTheTypingAppearanceOfCompositeItemList(jsonTreeViewItemList, baseItem);
                    baseItem.LogicChildren = [.. jsonTreeViewItemList];
                }
                #endregion
            }
        }

        private int CountInLineSourceCodeSameLayerItem(List<string> inlineSourceCode)
        {
            int result = 0;
            int firstLineCount = GetLineStarCount().Match(inlineSourceCode[0]).Value.Trim().Length;
            for (int i = 1; i < inlineSourceCode.Count; i++)
            {
                int currentCount = GetLineStarCount().Match(inlineSourceCode[i]).Value.Trim().Length;
                if (firstLineCount == currentCount)
                {
                    result++;
                }
            }
            return result;
        }

        /// <summary>
        /// 获取节点标头数据
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public List<string> GetHeadTypeAndKeyList(string target)
        {
            List<string> result = [];
            Match inheritMatch = GetInheritString().Match(target);
            if (!inheritMatch.Success)
            {
                string RemoveIrrelevantString = target.Replace("}}{{nbt", "").Replace("}}{{Nbt", "");
                int nbtFeatureStartIndex = RemoveIrrelevantString.IndexOf("{{");
                Match isStartMatch = JudgeHead().Match(RemoveIrrelevantString[..(nbtFeatureStartIndex + 2)]);
                int nbtFeatureEndIndex = RemoveIrrelevantString.IndexOf("}}");
                if (isStartMatch.Success && RemoveIrrelevantString.Contains('{') && nbtFeatureEndIndex > 0 && nbtFeatureEndIndex > 0)
                {
                    result.AddRange(RemoveIrrelevantString[(nbtFeatureStartIndex + 2)..nbtFeatureEndIndex].Split('|'));
                    result.RemoveAt(0);
                }
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
            if (target.Contains('：'))
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

        private static Tuple<List<string>, int> CollectionSubItem(List<string> nodeList, int currentIndex, int currentLinestarCount, int nextLineStarCount)
        {
            List<string> result = [];
            int nextNodeIndex = currentIndex;
            result.Add(nodeList[currentIndex]);
            Match nextLineStarMatch = GetLineStarCount().Match(nodeList[nextNodeIndex]);
            nextNodeIndex++;
            while (nextLineStarMatch.Success && nextLineStarCount > currentLinestarCount)
            {
                if (nextNodeIndex < nodeList.Count)
                {
                    nextLineStarMatch = GetLineStarCount().Match(nodeList[nextNodeIndex]);
                    if (nextLineStarMatch.Success && nextLineStarMatch.Groups[1].Value.Trim().Length > currentLinestarCount)
                    {
                        nextLineStarCount = nextLineStarMatch.Groups[1].Value.Trim().Length;
                        if (nodeList[nextNodeIndex].Contains('{') || nodeList[nextNodeIndex].Contains('['))
                        {
                            result.Add(nodeList[nextNodeIndex]);
                        }
                        nextNodeIndex++;
                    }
                }
                else
                {
                    break;
                }
                nextLineStarCount = nextLineStarMatch.Groups[1].Value.Trim().Length;
            }
            nextNodeIndex--;
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
                    string contextFileMarker = mainContextFileMarker.Value.Replace("=", "").Replace("#", "").Trim() + (subContextFileMarker.Success ? "|" + subContextFileMarker.Value.Replace("=", "").Trim() : "");

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
                            List<string> targetRawStringList = [.. wikiLines.Skip(treeviewDivs[i].Line).Take(treeviewDivs[i].EndNode.Line - 1 - treeviewDivs[i].Line)];
                            targetRawStringList = TypeSetting(targetRawStringList);
                            bool NoKey = plan.EnumCompoundDataDictionary.TryAdd(contextFileMarker, keyValuePairs);
                            if (NoKey)
                            {
                                keyValuePairs.Add(enumMatch.Value.Replace("=", "").Trim(), targetRawStringList);
                            }
                            else
                            if (contextFileMarker.Trim().Length > 0)
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
                    jsonTool.SetParentForEachItem(Result.Result, null);
                    HandlingTheTypingAppearanceOfCompositeItemList([.. Result.Result], null);
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
        public JsonTreeViewDataStructure GetTreeViewItemResult(JsonTreeViewDataStructure result, List<string> nodeList, int layerCount, string currentReferenceKey = "", BaseCompoundJsonTreeViewItem parent = null, JsonTreeViewItem previous = null, int previousStarCount = 1, bool isAddToParent = false)
        {
            #region Field
            bool isCompoundRoot = false, isListRoot = false;
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
                bool isAddedStringInMulipleCode = false;
                bool HaveBranches = false;
                string currentNodeItemType = "";
                string currentNodeKey = "";
                string currentDescription = "";
                bool IsSimpleItem = true;
                currentReferenceKey ??= "";
                bool IsCurrentOptionalNode = !GetRequiredKey().Match(nodeList[i]).Success && KeyList.Count == 0 && !(parent is not null && parent.ItemType is ItemType.CustomCompound);
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
                    #region 处理父级列表与当前列表节点都为有key的情况
                    if ((parent.ItemType is ItemType.List || (parent.ItemType is ItemType.MultiType && parent.SelectedValueType is not null && parent.SelectedValueType.Text == "List")) && i >= currentContextNextIndex && parent.Key.Length > 0 && NBTFeatureList.Except(dataStringType).Any() && NBTFeatureList.Count > 0 && NBTFeatureList[0] == "list")
                    {
                        int nextLineStarCount = 0;
                        if (i + 1 < nodeList.Count)
                        {
                            Match nextLineStarMatch = GetLineStarCount().Match(nodeList[i + 2]);
                            nextLineStarCount = nextLineStarMatch.Value.Trim().Length;
                            Tuple<List<string>, int> subNodeTuple = CollectionSubItem(nodeList, i + 1, starCount, nextLineStarCount);
                            nodeList.RemoveRange(i, nodeList.Count - i);
                            nodeList.InsertRange(i, subNodeTuple.Item1);
                            i = -1;
                            continue;
                        }
                    }
                    #endregion

                    #region 处理注入的结构
                    if (inheritMatch.Success)
                    {
                        string currentSourceCode = inheritMatch.Groups[1].Value;
                        List<string> sourceCodeContentList = RemoveUIMarker([.. currentSourceCode.Replace("|", "/").Split('/', StringSplitOptions.RemoveEmptyEntries)]);
                        currentSourceCode = "Inherit" + "/" + string.Join("/", sourceCodeContentList);
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
                            if (NBTFeatureList.Count < 2)
                            {
                                NBTFeatureList = GetHeadTypeAndKeyList(nodeList[i + 1]);
                                NBTFeatureList = RemoveUIMarker(NBTFeatureList);
                            }
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
                                EnumItemCount = CountInLineSourceCodeSameLayerItem(inlineSourceCode);
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
                        string contextKey = contextMatch.Groups[1].Value;
                        if (plan.DependencyItemList.TryGetValue(contextKey, out List<string> targetList1))
                        {
                            List<string> targetList = [.. targetList1];
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
                                List<string> targetList = [.. targetList2];
                                nodeList.RemoveAt(i);
                                nodeList.InsertRange(i, targetList);
                                i--;
                                currentContextNextIndex = (i < 0 ? 0 : i) + targetList.Count;
                                continue;
                            }
                            else
                            if (plan.EnumCompoundDataDictionary.TryGetValue(targetKey, out Dictionary<string, List<string>> targetDictionary))
                            {
                                IsPreIdentifiedAsEnumCompoundType = true;
                                HadPreIdentifiedAsEnumCompoundType = true;
                                CurrentEnumKey = targetKey;
                                isSimpleDataType = false;
                                KeyList.AddRange(targetDictionary.Keys);
                                List<string> inlineSourceCode = targetDictionary[KeyList.FirstOrDefault()];
                                bool haveExtraItem = GetExtraKey().Match(nodeList[^1]).Success;
                                if (!haveExtraItem && ((!NBTFeatureList.Contains("compound") &&
                                    !NBTFeatureList.Where(item => item.Contains("array")).Any() &&
                                    !NBTFeatureList.Contains("list")) || i >= currentContextNextIndex))
                                {
                                    nodeList.RemoveAt(i);
                                    nodeList.InsertRange(i, inlineSourceCode);

                                    int firstInlineLayer = GetLineStarCount().Match(inlineSourceCode[0]).Value.Trim().Length;
                                    EnumItemCount = CountInLineSourceCodeSameLayerItem(inlineSourceCode);
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
                bool isEnumKey = plan.TranslateDictionary.Count > 0 && (plan.TranslateDictionary.ContainsKey(EnumMatch.Groups[1].Value) || plan.EnumIDDictionary.ContainsKey(EnumMatch.Groups[1].Value));
                bool isBoolKey = (EnumCollectionMode1.Count > 0 && (EnumCollectionMode1[0].Groups[1].Value == "false" || EnumCollectionMode1[0].Groups[1].Value == "true")) || (EnumCollectionMode2.Count > 0 && (EnumCollectionMode2[0].Groups[1].Value == "false" || EnumCollectionMode2[0].Groups[1].Value == "true"));
                if (NBTFeatureList.Count > 0 && !IsPreIdentifiedAsEnumCompoundType)
                {
                    isSimpleDataType = !(parent is not null && parent.ItemType is ItemType.CustomCompound && currentReferenceKey.Length > 0) && ((!NBTFeatureList[0].Contains("array") && NBTFeatureList[0] != "list" && NBTFeatureList[0] != "compound" && NBTFeatureList.Count < 3) || isBoolKey) && ((isBoolKey || (EnumCollectionMode1.Count < 2 && EnumCollectionMode2.Count < 2)) && !isEnumKey) && !NBTFeatureList[^1].Contains('\'');
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
                if (NBTFeatureList.Count == 0 || ((NBTFeatureList.Count == 1 && NBTFeatureList[0] == "compound") && !inheritMatch.Success && !contextMatch.Success && !EnumMatch.Success))
                {
                    continue;
                }

                if (nodeList[i].Trim().Length == 0)
                {
                    continue;
                }

                int lastCurlyBracesIndex = nodeList[i].LastIndexOf('}');
                string rootString = nodeList[i][(lastCurlyBracesIndex + 1)..].Trim();
                if (lastCurlyBracesIndex > -1 && !string.IsNullOrEmpty(rootString) && (rootString == "根节点" || rootString == "根标签" && NBTFeatureList[0] != "list") && (NBTFeatureList.Count < 3 || isAddToParent))
                {
                    continue;
                }
                #endregion

                #region 获取当前节点的数据类型和键、获取描述数据
                currentNodeItemType = NBTFeatureList[0];
                List<string> keyDataList = [.. NBTFeatureList.Except(dataStringType)];
                if (keyDataList.Count > 0)
                {
                    currentNodeKey = keyDataList[^1];
                }
                currentDescription = GetDescription(nodeList[i]);
                if (currentNodeKey.Contains('\''))
                {
                    IsCurrentOptionalNode = false;
                }
                #endregion

                #region 声明当前节点
                if (currentDescription == "根标签" || currentDescription == "根节点")
                {
                    layerCount = 0;
                }

                if (NBTFeatureList[0] == "list" && (currentDescription == "根标签" || currentDescription == "根节点"))
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
                bool IsCanBeAnalyzed = currentNodeItemType.Length > 0;
                #endregion

                #region 处理两大值类型
                if (IsCanBeAnalyzed)
                {
                    #region 判断是否为复合节点
                    if (isEnumIDList || isHaveNameSpaceKey || !isSimpleDataType)
                    {
                        item = new BaseCompoundJsonTreeViewItem(plan, jsonTool, _container)
                        {
                            IsCanBeDefaulted = IsCurrentOptionalNode
                        };
                        IsSimpleItem = false;
                    }
                    #endregion

                    #region 判断是否捕获多组信息
                    if (NBTFeatureList is not null && NBTFeatureList.Count > 2 && currentNodeItemType.Length > 0 && currentNodeKey.Length > 0)
                    {
                        if (item is BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem)
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

                            if (compoundJsonTreeViewItem.ValueTypeSource.Count > 0)
                            {
                                compoundJsonTreeViewItem.SelectedValueType ??= compoundJsonTreeViewItem.ValueTypeSource[0];
                            }
                            compoundJsonTreeViewItem.ItemType = ItemType.MultiType;
                            if (!IsCurrentOptionalNode && !(currentDescription == "根标签" || currentDescription == "根节点"))
                            {
                                result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + (currentReferenceKey.Length > 0 ? currentReferenceKey : currentNodeKey) + "\": ");

                                if (currentReferenceKey.Length > 0)
                                {
                                    compoundJsonTreeViewItem.RemoveElementButtonVisibility = Visibility.Visible;
                                }
                            }

                            isCompoundRoot = NBTFeatureList[0] == "compound" && !isAddToParent;
                            isListRoot = NBTFeatureList[0] == "list" && !isAddToParent;
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
                        }
                    }
                    #endregion

                    #region 处理无Key的多重数据类型节点
                    List<string> keyFilter = [.. NBTFeatureList.Except(dataStringType)];
                    keyFilter.RemoveAll(item => item.Contains("array"));
                    if (keyFilter.Count == 0 && NBTFeatureList.Count > 2)
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
                            BaseCompoundJsonTreeViewItem multipleDataTypeElement = new(plan, jsonTool, _container)
                            {
                                IsCanBeDefaulted = false,
                                ItemType = ItemType.MultiType,
                                InfoTipText = description,
                                RemoveElementButtonVisibility = Visibility.Visible,
                                DisplayText = "Entry",
                                LayerCount = parent.LayerCount + 1,
                                Parent = parent
                            };
                            #region 处理值类型切换需要使用的字段
                            if (contextMatch.Success)
                            {
                                if (plan.TranslateDictionary.TryGetValue('#' + contextMatch.Groups[1].Value, out string targetKey))
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
                                                multipleDataTypeElement.CompoundChildrenStringList = currentSubChildren;
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
                        switch (currentNodeItemType)
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
                                    bool parseResult = Enum.TryParse(typeof(DataType), currentNodeItemType[0].ToString().ToUpper() + currentNodeItemType[1..], out dataTypes);
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
                                        if (!currentNodeKey.Contains(':'))
                                        {
                                            item.DisplayText = string.Join(' ', currentNodeKey.Split('_').Select(item => item[0].ToString().ToUpper() + item[1..]));
                                        }
                                        else
                                        {
                                            item.DisplayText = currentNodeKey;
                                        }
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
                                    if (parent is not null && (parent.ItemType is ItemType.List || parent.ItemType is ItemType.Array) && isAddToParent && currentNodeKey.Length == 0)
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
                                        if (!currentNodeKey.Contains(':'))
                                        {
                                            item.DisplayText = string.Join(' ', currentNodeKey.Split('_').Select(item => item[0].ToString().ToUpper() + item[1..]));
                                        }
                                        else
                                        {
                                            item.DisplayText = currentNodeKey;
                                        }
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
                                    if (currentNodeKey.Length == 0 && isAddToParent && parent is not null && (parent.ItemType is ItemType.List || parent.ItemType is ItemType.Array))
                                    {
                                        IsNoKeyOrMultiDataTypeItem = true;
                                        item.LayerCount = layerCount = parent.LayerCount + 1;
                                        item.RemoveElementButtonVisibility = Visibility.Visible;
                                        item.Value = "";
                                        item.IsCanBeDefaulted = false;
                                        result.ResultString.Append(new string(' ', item.LayerCount * 2) + "\"\"");
                                    }

                                    if (currentNodeKey.Length == 0 && (parent.ItemType is ItemType.List || (parent.ItemType is ItemType.MultiType && parent.SelectedValueType.Text == "List")))
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
                                        if (!currentNodeKey.Contains(':'))
                                        {
                                            item.DisplayText = string.Join(' ', currentNodeKey.Split('_').Select(item => item[0].ToString().ToUpper() + item[1..]));
                                        }
                                        else
                                        {
                                            item.DisplayText = currentNodeKey;
                                        }
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
                                    if (currentNodeKey.Length == 0 && isAddToParent && parent is not null && (parent.ItemType is ItemType.List || parent.ItemType is ItemType.Array || (parent.ItemType is ItemType.MultiType && parent.SelectedValueType is not null && parent.SelectedValueType.Text == "List" || parent.SelectedValueType.Text.Contains("array"))))
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
                            previous.LogicNext = item;
                            item.LogicPrevious = previous;
                        }
                    }
                    #endregion

                    #region 处理复合类型
                    if (item is BaseCompoundJsonTreeViewItem CurrentCompoundItem)
                    {
                        #region 判断是否需要重新分析上下文关键字
                        if (NBTFeatureList.Count < 3 && contextMatch.Success && parent is not null && parent.CompoundChildrenStringList.Count == 1 && isAddToParent)
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
                                CurrentCompoundItem.CompoundChildrenStringList.Add(nodeList[i]);
                                HandlingTheTypingAppearanceOfCompositeItemList([CurrentCompoundItem], null);
                                if (CurrentCompoundItem.LogicChildren.Count > 0)
                                {
                                    BaseCompoundJsonTreeViewItem subCompoundItem = CurrentCompoundItem.LogicChildren[0] as BaseCompoundJsonTreeViewItem;
                                    CurrentCompoundItem.LogicChildren.Clear();
                                    item = CurrentCompoundItem = subCompoundItem;
                                }
                                else
                                {
                                    CurrentCompoundItem.CompoundChildrenStringList.Clear();
                                }
                                if (CurrentCompoundItem.EnumKey.Length > 0)
                                {
                                    result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + CurrentCompoundItem.Key + "\": \"\"");
                                }
                            }
                        }
                        #endregion

                        #region 设置Key
                        if (CurrentCompoundItem.Key.Length == 0 && currentNodeKey.Length > 0 && !(currentDescription == "根标签" || currentDescription == "根节点"))
                        {
                            CurrentCompoundItem.Key = currentNodeKey;
                        }
                        #endregion

                        #region 处理不同行为的复合型数据

                        #region 确定数据类型
                        if (NBTFeatureList.Count > 0)
                        {
                            if (currentNodeItemType == "compound" && NBTFeatureList.Count > 1)
                            {
                                if (IsCurrentOptionalNode)
                                {
                                    CurrentCompoundItem.ItemType = ItemType.OptionalCompound;
                                }
                                else
                                {
                                    CurrentCompoundItem.ItemType = ItemType.Compound;
                                }
                            }
                            else
                            if (currentNodeItemType == "list")
                            {
                                CurrentCompoundItem.ElementButtonTip = "添加到顶部";
                                CurrentCompoundItem.ItemType = ItemType.List;
                                if (currentNodeKey.Length == 0)
                                {
                                    IsNoKeyOrMultiDataTypeItem = true;
                                    CurrentCompoundItem.RemoveElementButtonVisibility = Visibility.Visible;
                                    CurrentCompoundItem.DisplayText = "Entry";
                                    CurrentCompoundItem.IsCanBeDefaulted = false;
                                }
                            }
                            else//数组会有数据类型的区分，所以这里用包含而不是等于
                            if (currentNodeItemType.Contains("array"))
                            {
                                CurrentCompoundItem.ElementButtonTip = "添加到顶部";
                                CurrentCompoundItem.ItemType = ItemType.Array;
                            }

                            CurrentCompoundItem.AddOrSwitchElementButtonVisibility = CurrentCompoundItem.EnumBoxVisibility is Visibility.Collapsed && (CurrentCompoundItem.ItemType is ItemType.OptionalCompound || CurrentCompoundItem.ItemType is ItemType.List || (CurrentCompoundItem.ItemType is ItemType.Array && CurrentCompoundItem.IsCanBeDefaulted)) ? Visibility.Visible : Visibility.Collapsed;
                            if (CurrentCompoundItem.AddOrSwitchElementButtonVisibility is Visibility.Visible && CurrentCompoundItem.ItemType is ItemType.Compound || CurrentCompoundItem.ItemType is ItemType.OptionalCompound)
                            {
                                CurrentCompoundItem.ElementButtonTip = "展开";
                            }

                            if (CurrentCompoundItem.ItemType is ItemType.Array || CurrentCompoundItem.ItemType is ItemType.List || CurrentCompoundItem.ItemType is ItemType.Compound || CurrentCompoundItem.ItemType is ItemType.OptionalCompound)
                            {
                                CurrentCompoundItem.SwitchButtonIcon = CurrentCompoundItem.PlusIcon;
                                CurrentCompoundItem.SwitchButtonColor = CurrentCompoundItem.PlusColor;
                                CurrentCompoundItem.PressedSwitchButtonColor = CurrentCompoundItem.PressedPlusColor;
                            }
                        }
                        #endregion

                        #region 更新非可选复合型节点的文本值与层数
                        if (CurrentCompoundItem.Key.Replace("\"", "").Length > 0 && CurrentCompoundItem.DisplayText.Length == 0)
                        {
                            if (!currentNodeKey.Contains(':'))
                            {
                                CurrentCompoundItem.DisplayText = string.Join(' ', CurrentCompoundItem.Key.Split('_').Select(item => item[0].ToString().ToUpper() + item[1..]));
                            }
                            else
                            {
                                CurrentCompoundItem.DisplayText = currentNodeKey;
                            }
                        }
                        if (currentDescription == "根标签" || currentDescription == "根节点")
                        {
                            CurrentCompoundItem.DisplayText = "Root";
                        }
                        CurrentCompoundItem.LayerCount = layerCount;

                        bool isNeedCloseListOrArray = false;
                        bool isNeedCloseCompound = false;
                        if (CurrentCompoundItem.Key.Length > 0 && !IsCurrentOptionalNode && NBTFeatureList.Count < 3)
                        {
                            if (NBTFeatureList[0].Contains("list") || NBTFeatureList[0].Contains("array"))
                            {
                                isNeedCloseListOrArray = true;
                                result.ResultString.Append(new string(' ', CurrentCompoundItem.LayerCount * 2) + "\"" + currentNodeKey + "\": [");
                            }
                            else
                            if (NBTFeatureList[0].Contains("compound") && !CurrentCompoundItem.Key.Contains('\''))
                            {
                                isNeedCloseCompound = true;
                                result.ResultString.Append(new string(' ', CurrentCompoundItem.LayerCount * 2) + "\"" + CurrentCompoundItem.Key.Replace("\"", "") + "\": {");
                            }
                        }
                        else
                        if (CurrentCompoundItem.Key.Length == 0 && NBTFeatureList[0].Contains("list"))
                        {
                            CurrentCompoundItem.IsCanBeDefaulted = false;
                            if (parent is not null)
                            {
                                CurrentCompoundItem.LayerCount = layerCount = parent.LayerCount + 1;
                            }
                            else
                            {
                                CurrentCompoundItem.LayerCount = layerCount;
                            }
                            result.ResultString.Append(new string(' ', CurrentCompoundItem.LayerCount * 2) + "[]");
                        }
                        #endregion

                        #endregion

                        #region 处理各类数组
                        if (NBTFeatureList[0].Contains("array"))
                        {
                            string[] arrayInfoArray = NBTFeatureList[0].Split('-');
                            if (EnumMatch.Success && EnumMatch.Groups[1].Value == "UUID")
                            {
                                CurrentCompoundItem.AddOrSwitchElementButtonVisibility = Visibility.Collapsed;
                                JsonTreeViewDataStructure uuidItemData = GetUUIDJsonTreeViewItem(true, IsCurrentOptionalNode, layerCount + 1);
                                CurrentCompoundItem.LogicChildren.AddRange(uuidItemData.Result);
                                while (uuidItemData.ResultString[^1] == ',')
                                {
                                    uuidItemData.ResultString.Length--;
                                }
                                result.ResultString.Append(uuidItemData.ResultString.ToString() + "\r\n" + new string(' ', layerCount * 2) + ']');
                                isNeedCloseListOrArray = false;
                            }
                            else
                            {
                                CurrentCompoundItem.AddOrSwitchElementButtonVisibility = Visibility.Visible;
                            }
                        }
                        #endregion

                        #region 处理方块属性
                        if (parent is not null && parent.Parent is not null && NBTFeatureList.Count > 2 && NBTFeatureList[^1].Contains("方块属性"))
                        {
                            bool isHaveIDItem = false;
                            BaseCompoundJsonTreeViewItem grandParent = parent.Parent;
                            foreach (var parentItem in grandParent.LogicChildren)
                            {
                                Match parentItemMatch = GetEnumKey().Match(parentItem.InfoTipText);
                                if (parentItemMatch.Success && parentItemMatch.Groups[1].Value.Contains("ID"))
                                {
                                    isHaveIDItem = true;
                                }
                                if (parentItemMatch.Success && parentItemMatch.Groups[1].Value.Contains("ID") && parentItem is BaseCompoundJsonTreeViewItem parentCompoundItem && parentCompoundItem.SelectedEnumItem is not null && parentCompoundItem.SelectedEnumItem.Text == "string" && plan.EnumCompoundDataDictionary["BlockStateProperty"].TryGetValue(parentCompoundItem.SelectedEnumItem.Text.Replace("minecraft:", ""), out List<string> targetBlockPropertyList))
                                {
                                    BaseCompoundJsonTreeViewItem oldPropertyItem = null;
                                    foreach (var blockPropertyItem in targetBlockPropertyList)
                                    {
                                        JObject propertyObject = JObject.Parse(blockPropertyItem);
                                        if (propertyObject.First is JProperty jProperty && propertyObject[jProperty.Name] is JArray jarray)
                                        {
                                            BaseCompoundJsonTreeViewItem newPropertyItem = new(plan, jsonTool, _container)
                                            {
                                                Parent = parent,
                                                LayerCount = layerCount,
                                                IsCanBeDefaulted = true,
                                                Key = jProperty.Name,
                                                DisplayText = jProperty.Name[0].ToString().ToUpper() + jProperty.Name[1..],
                                                ItemType = ItemType.Enum,
                                                DataType = DataType.String,
                                                EnumBoxVisibility = Visibility.Visible
                                            };
                                            if (oldPropertyItem is not null)
                                            {
                                                oldPropertyItem.LogicNext = newPropertyItem;
                                                newPropertyItem.LogicPrevious = oldPropertyItem;
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
                            if (isHaveIDItem)
                            {
                                result.ResultString.Clear();
                                return result;
                            }
                        }
                        #endregion

                        #region 处理字符串枚举
                        string currentCustomKey = currentNodeKey.TrimStart('!');
                        string operatorString = "";
                        if (currentCustomKey.Length != currentNodeKey.Length)
                        {
                            operatorString = "!";
                        }
                        int EnumKeyCount = EnumKeyList.Where(item => nodeList[i].Contains(item)).Count();
                        if (DefaultEnumValueMatch.Success)
                        {
                            item.DefaultValue = item.Value = "\"" + DefaultEnumValueMatch.Value + "\"";
                        }

                        #region 处理Key与外观
                        if ((NBTFeatureList[0] == "string" || (currentCustomKey.StartsWith("<''") && currentCustomKey.EndsWith("''>"))) && (EnumKeyCount > 0 || EnumCollectionMode1.Count > 0 || EnumCollectionMode2.Count > 0 || EnumMatch.Success) && !IsPreIdentifiedAsEnumCompoundType)
                        {
                            string targetEnumKey = EnumMatch.Groups[1].Value.TrimStart('#');
                            if (targetEnumKey.Length == 0)
                            {
                                targetEnumKey = currentNodeKey.TrimStart('!');
                            }
                            List<string> enumSource = [];
                            List<string> targetEnumIDList = [];
                            Dictionary<string, List<string>> targetSourceCodeDictionary = [];
                            if (CurrentCompoundItem.ValueTypeSource.Count == 0)
                            {
                                CurrentCompoundItem.ItemType = ItemType.Enum;
                                CurrentCompoundItem.EnumBoxVisibility = Visibility.Visible;
                            }

                            if (plan.TranslateDictionary.TryGetValue(targetEnumKey, out string targetTranslateKey))
                            {
                                if (parent is not null && parent.ItemType is ItemType.List)
                                {
                                    CurrentCompoundItem.RemoveElementButtonVisibility = Visibility.Visible;
                                }


                                if (plan.EnumIDDictionary.TryGetValue(targetTranslateKey, out targetEnumIDList))
                                {
                                }
                                else
                                if (plan.EnumCompoundDataDictionary.TryGetValue(targetTranslateKey, out targetSourceCodeDictionary))
                                {
                                    CurrentCompoundItem.EnumKey = targetTranslateKey;
                                }
                            }
                            else
                            if (plan.EnumIDDictionary.TryGetValue(targetEnumKey, out targetEnumIDList))
                            {
                            }
                            else
                            if (plan.EnumCompoundDataDictionary.TryGetValue(targetEnumKey, out targetSourceCodeDictionary))
                            {
                                CurrentCompoundItem.EnumKey = targetEnumKey;
                            }

                            if (targetEnumIDList is not null && targetEnumIDList.Count > 0)
                            {
                                enumSource.AddRange(targetEnumIDList);
                            }
                            else
                            if (targetSourceCodeDictionary is not null && targetSourceCodeDictionary.Count > 0)
                            {
                                enumSource.AddRange(targetSourceCodeDictionary.Keys);
                                CurrentCompoundItem.EnumKey = targetTranslateKey.Length > 0 ? targetTranslateKey : targetEnumKey;
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

                            if (targetEnumIDList is null && targetSourceCodeDictionary is null)
                            {
                                enumSource.AddRange([.. EnumCollectionMode1.Select(enum1 => { return enum1.Groups[1].Value; })]);
                                enumSource.AddRange([.. EnumCollectionMode2.Select(enum1 => { return enum1.Groups[1].Value; })]);
                            }
                            enumSource = [.. enumSource.Distinct()];
                            enumSource.Sort();

                            if (CurrentCompoundItem.EnumItemsSource.Count == 0)
                            {
                                CurrentCompoundItem.EnumItemsSource.Add(new TextComboBoxItem() { Text = "- unset -" });
                            }
                            CurrentCompoundItem.EnumItemsSource.AddRange(enumSource.Select(enum1 =>
                            {
                                return new TextComboBoxItem() { Text = operatorString + enum1 };
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

                        #region 处理预分析枚举结构
                        if (IsPreIdentifiedAsEnumCompoundType)
                        {
                            CurrentCompoundItem.EnumItemsSource.Clear();
                            CurrentCompoundItem.ItemType = ItemType.Enum;
                            CurrentCompoundItem.RemoveElementButtonVisibility = Visibility.Collapsed;
                            CurrentCompoundItem.EnumItemCount = EnumItemCount;
                            EnumItemCount = 0;
                            CurrentCompoundItem.EnumBoxVisibility = Visibility.Visible;
                            CurrentCompoundItem.EnumItemsSource.Add(new TextComboBoxItem() { Text = "- unset -" });
                            CurrentCompoundItem.EnumItemsSource.AddRange(KeyList.Select(item => new TextComboBoxItem() { Text = item }));
                            KeyList.Clear();
                            CurrentCompoundItem.SelectedEnumItem = CurrentCompoundItem.EnumItemsSource.FirstOrDefault();
                            CurrentCompoundItem.EnumKey = CurrentEnumKey;
                            result.ResultString.Append(new string(' ', layerCount * 2) + '"' + currentNodeKey + '"' + ": \"\"");
                            Match extraMatch = GetExtraKey().Match(nodeList[^1]);
                            //这行范围删除源码的操作，可能会导致错误
                            if (!extraMatch.Success && i == nodeList.Count - 1)
                            {
                                nodeList.RemoveRange(i + 1, nodeList.Count - i - 1);
                            }
                        }
                        #endregion

                        #endregion

                        #region 处理复合枚举
                        if (currentCustomKey.StartsWith("<''") && currentCustomKey.EndsWith("''>"))
                        {
                            currentCustomKey = currentCustomKey.Replace("<''", "").Replace("''>", "");

                            CurrentCompoundItem.Key = CurrentCompoundItem.DisplayText = currentReferenceKey;
                            CurrentCompoundItem.AddOrSwitchElementButtonVisibility = Visibility.Visible;
                            if (parent.LogicChildren.Count > 0 || parent.ItemType is ItemType.CustomCompound)
                            {
                                CurrentCompoundItem.Parent = parent.Parent;
                                CurrentCompoundItem.ItemType = ItemType.Compound;
                                CurrentCompoundItem.RemoveElementButtonVisibility = Visibility.Visible;
                                if(!parent.IsProcessedByAppearanceChecker && parent.ItemType is ItemType.CustomCompound)
                                {
                                    i++;
                                }
                            }
                            else
                            {
                                CurrentCompoundItem.Parent = parent;
                                CurrentCompoundItem.ItemType = ItemType.CustomCompound;
                                CurrentCompoundItem.AddOrSwitchElementButtonVisibility = Visibility.Visible;
                                if (i + 1 < nodeList.Count)
                                {
                                    Match nextLineStarMatch = GetLineStarCount().Match(nodeList[i + 1]);
                                    string nextLineStar = nextLineStarMatch.Groups[1].Value.Trim();
                                    Tuple<List<string>, int> currentSubChildrenTuple = CollectionSubItem(nodeList, i, starCount, nextLineStar.Length);
                                    if (currentSubChildrenTuple.Item1.Count > 1 || (currentSubChildrenTuple.Item1.Count == 1 && currentSubChildrenTuple.Item1[0] != nodeList[i]))
                                    {
                                        CurrentCompoundItem.CompoundChildrenStringList = [.. currentSubChildrenTuple.Item1];
                                        i = currentSubChildrenTuple.Item2;
                                    }
                                }
                            }

                            if (parent is not null && parent.IsCanBeDefaulted && parent.StartLine is null)
                            {
                                result.ResultString.Clear();
                                result.ResultString.Append(' ');
                            }
                            else
                            if (parent is not null)
                            {
                                CurrentCompoundItem.EnumItemsSource.Clear();
                                CurrentCompoundItem.ValueTypeSource.Clear();
                            }

                            CurrentCompoundItem.EnumBoxVisibility = CurrentCompoundItem.ValueTypeBoxVisibility = Visibility.Collapsed;
                            if (CurrentCompoundItem.EnumItemsSource.Count > 0)
                            {
                                CurrentCompoundItem.EnumBoxVisibility = Visibility.Visible;
                            }
                            if (CurrentCompoundItem.ValueTypeSource.Count > 0)
                            {
                                CurrentCompoundItem.ValueTypeBoxVisibility = Visibility.Visible;
                            }

                            BaseCompoundJsonTreeViewItem customKeyCompoundItem = null;
                            if (parent is not null && parent.StartLine is null && parent.ItemType is not ItemType.CustomCompound)
                            {
                                customKeyCompoundItem = GetCustomKeyBaseCompoundItem(CurrentCompoundItem);
                            }
                            if (NBTFeatureList[0] == "compound" && parent is not null && (parent.StartLine is not null || !parent.IsCanBeDefaulted))
                            {
                                result.ResultString.Clear();
                                result.ResultString.Append(new string(' ', layerCount * 2) + '"' + currentReferenceKey + '"' + ": {\r\n");
                            }

                            if (plan.TranslateDictionary.TryGetValue(currentCustomKey, out string translatedKey) && parent is not null && (parent.StartLine is not null || !parent.IsCanBeDefaulted))
                            {
                                #region 处理拥有固定复合结构的枚举节点
                                if (plan.EnumCompoundDataDictionary.TryGetValue(translatedKey, out Dictionary<string, List<string>> targetDictionary) && !CurrentCompoundItem.IsCanBeDefaulted)
                                {

                                }

                                if (plan.EnumIDDictionary.TryGetValue(translatedKey, out List<string> targetIDList))
                                {

                                }
                                #endregion
                            }

                            if (customKeyCompoundItem is not null)
                            {
                                item = CurrentCompoundItem = customKeyCompoundItem;
                            }
                            else
                            {
                                item = CurrentCompoundItem;
                            }
                        }

                        if (parent is not null && parent.ItemType is ItemType.CustomCompound && currentReferenceKey.Length > 0 && currentReferenceKey.StartsWith('!'))
                        {
                            JsonTreeViewItem reverseItem = GetReverseJsonTreeViewItem('!' + currentNodeKey);
                            result.Result.Add(reverseItem);
                            result.ResultString.Clear();
                            result.ResultString.Append(new string(' ', layerCount * 2) + "\"!" + currentNodeKey + "\": {}");
                            return result;
                        }
                        #endregion

                        #region 设置前后关系
                        if (previous is not null && previous.LayerCount == layerCount)
                        {
                            previous.LogicNext = CurrentCompoundItem;
                            CurrentCompoundItem.LogicPrevious = previous;
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
                                CurrentCompoundItem.CompoundChildrenStringList.AddRange(nodeList.Skip(i + 1));
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
                                        bool isNeedPlusOne = (parent is not null && parent.IsProcessedByAppearanceChecker) || !isAddToParent || nodeList[i].Contains(CurrentCompoundItem.Key) || (parent.LogicChildren.Count > 0 && parent.LogicChildren[0] is BaseCompoundJsonTreeViewItem firstProcessedItem && firstProcessedItem.IsProcessedByAppearanceChecker);
                                        Tuple<List<string>, int> currentSubChildrenTuple = new([],0);
                                        if (CurrentCompoundItem.CompoundChildrenStringList.Count == 0 &&
                                            CurrentCompoundItem.ArrayChildrenStringList.Count == 0 &&
                                            CurrentCompoundItem.ListChildrenStringList.Count == 0)
                                        {
                                            currentSubChildrenTuple = CollectionSubItem(nodeList, i + (isNeedPlusOne ? 1 : 0), starCount, nextLineStar.Length);
                                        }
                                        #endregion

                                        #region 根据递归的情况来向后移动N个迭代单位
                                        if (currentSubChildrenTuple.Item1.Count > 0)
                                        {
                                            i = currentSubChildrenTuple.Item2;
                                        }
                                        #endregion

                                        #region 存储原始信息、处理递归
                                        if ((NBTFeatureList[0] == "compound" || NBTFeatureList[0].Contains("array")) && currentSubChildrenTuple.Item1.Count > 0 && !IsCurrentOptionalNode)
                                        {
                                            #region 如果子信息只有一条并引用指定文件，则将其文档内容取出并直接解析
                                            Match subInheritMatch = GetInheritString().Match(currentSubChildrenTuple.Item1.Count > 0 ? currentSubChildrenTuple.Item1[0] : "");
                                            int index = subInheritMatch.Groups[1].Value.IndexOf('|');
                                            string subInheritMatchString = subInheritMatch.Groups[1].Value;
                                            if (index != -1)
                                            {
                                                subInheritMatchString = subInheritMatch.Groups[1].Value[..index];
                                            }
                                            if (currentSubChildrenTuple.Item1.Count == 1 && subInheritMatch.Success && plan.DependencyItemList.TryGetValue("Inherit" + subInheritMatchString, out List<string> subTargetInheritList))
                                            {
                                                currentSubChildrenTuple = new Tuple<List<string>, int>(subTargetInheritList, subTargetInheritList.Count);
                                            }
                                            #endregion

                                            #region 执行递归
                                            JsonTreeViewDataStructure subResult = GetTreeViewItemResult(new(), [.. currentSubChildrenTuple.Item1], layerCount + 1, currentReferenceKey, CurrentCompoundItem, null, previousStarCount, isAddToParent);

                                            CurrentCompoundItem.LogicChildren.AddRange(subResult.Result);

                                            while (subResult.ResultString.Length > 0 &&
                                            (subResult.ResultString[^1] == ',' ||
                                             subResult.ResultString[^1] == '\r' ||
                                             subResult.ResultString[^1] == '\n' ||
                                             subResult.ResultString[^1] == ' '))
                                            {
                                                subResult.ResultString.Length--;
                                            }
                                            while (result.ResultString.Length > 0 &&
                                            (result.ResultString[^1] == ',' ||
                                             result.ResultString[^1] == '\r' ||
                                             result.ResultString[^1] == '\n' ||
                                             result.ResultString[^1] == ' '))
                                            {
                                                result.ResultString.Length--;
                                            }
                                            #endregion

                                            #region 处理外观并收尾
                                            if (subResult.Result.Count > 0)
                                            {
                                                CurrentCompoundItem.AddOrSwitchElementButtonVisibility = Visibility.Collapsed;
                                            }

                                            if (currentReferenceKey.Length > 0 && parent is not null && parent.ItemType is ItemType.CustomCompound)
                                            {
                                                CurrentCompoundItem.RemoveElementButtonVisibility = Visibility.Visible;
                                            }

                                            if (subResult.ResultString.Length > 0 && (!IsCurrentOptionalNode || isAddToParent))
                                            {
                                                result.ResultString.Append("\r\n" + subResult.ResultString + "\r\n");
                                                if (!isCompoundRoot && !isListRoot)
                                                {
                                                    result.ResultString.Append(new string(' ', layerCount * 2) + "}");
                                                    isNeedCloseCompound = false;
                                                }
                                            }
                                            else//复合节点收尾
                                            if (!IsCurrentOptionalNode)
                                            {
                                                if (NBTFeatureList[0] == "compound")
                                                {
                                                    result.ResultString.Append('}');
                                                    isNeedCloseCompound = false;
                                                }
                                                else
                                                if (NBTFeatureList[0].Contains("array"))
                                                {
                                                    result.ResultString.Append(']');
                                                    isNeedCloseListOrArray = false;
                                                }
                                            }
                                            #endregion
                                        }
                                        else
                                        if (!IsCurrentOptionalNode)
                                        {
                                            if (NBTFeatureList[0] == "compound" && result.ResultString.ToString().Trim().Length > 0)
                                            {
                                                result.ResultString.Append('}');
                                                isNeedCloseCompound = false;
                                            }
                                            else
                                            if (NBTFeatureList[0].Contains("array") && result.ResultString.ToString().Trim().Length > 0)
                                            {
                                                result.ResultString.Append(']');
                                                isNeedCloseListOrArray = false;
                                            }
                                        }

                                        #region 给予不同复合标签子信息
                                        if (CurrentCompoundItem.LogicChildren.Count == 0 || (CurrentCompoundItem.DisplayText == "Root" && CurrentCompoundItem.Parent is null))
                                        {
                                            List<string> tupleList = currentSubChildrenTuple.Item1;
                                            List<string> firstFeatureList = GetHeadTypeAndKeyList(tupleList[0]);
                                            firstFeatureList = RemoveUIMarker(firstFeatureList);
                                            if (currentSubChildrenTuple.Item1.Count > 0 && firstFeatureList.Count > 0 && firstFeatureList[^1].Contains(CurrentCompoundItem.Key) && CurrentCompoundItem.Key.Length > 0)
                                            {
                                                tupleList = [..tupleList.Skip(1)];
                                            }
                                            if (CurrentCompoundItem.ItemType is ItemType.List && CurrentCompoundItem.ListChildrenStringList.Count == 0)
                                            {
                                                CurrentCompoundItem.ListChildrenStringList = tupleList;
                                            }
                                            else
                                            if (CurrentCompoundItem.ItemType is ItemType.Array && CurrentCompoundItem.ArrayChildrenStringList.Count == 0)
                                            {
                                                CurrentCompoundItem.ArrayChildrenStringList = tupleList;
                                            }
                                            else
                                            if(CurrentCompoundItem.CompoundChildrenStringList.Count == 0)
                                            {
                                                CurrentCompoundItem.CompoundChildrenStringList = tupleList;
                                            }
                                        }
                                        #endregion

                                        #endregion

                                        #region 判断后一个节点是否可选并赋予Plan属性
                                        IsHaveNextNode = i < nodeList.Count - 1;
                                        item.Plan ??= plan;
                                        #endregion
                                    }
                                    else
                                    {
                                        #region 没有子信息时，提取当前行的上下文信息作为子信息，因为引用一定包含在当前行中
                                        if (contextMatch.Success && NBTFeatureList.Count < 3)
                                        {
                                            CurrentCompoundItem.CompoundChildrenStringList.Add("*{{nbt|compound}} [[#" + contextMatch.Groups[1].Value + "]]");
                                        }
                                        #endregion
                                    }
                                }
                            }
                        }
                        #endregion

                        #region 闭合列表、数组、对象
                        if (isNeedCloseListOrArray)
                        {
                            result.ResultString.Append(']');
                        }
                        if (isNeedCloseCompound)
                        {
                            result.ResultString.Append('}');
                        }
                        #endregion

                        #region 此处所有处理流程已结束，如果还是没有Key，那自动划分为列表或数组元素
                        if (CurrentCompoundItem.Key.Length == 0 && currentDescription != "根标签")
                        {
                            CurrentCompoundItem.DisplayText = "Entry";
                            CurrentCompoundItem.RemoveElementButtonVisibility = Visibility.Visible;
                        }
                        #endregion

                        #region 设置描述、预分析标记复位
                        CurrentCompoundItem.InfoTipText = currentDescription;
                        IsPreIdentifiedAsEnumCompoundType = false;
                        #endregion
                    }
                    #endregion
                }
                #endregion

                #region 符合条件时将当前节点与前一个合并
                if (previous is not null && item.Key == previous.Key && item.Key.Length > 0)
                {
                    List<string> previousItemTypeList = [];
                    if (item is BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.DataType is not DataType.None)
                    {
                        previousItemTypeList.Add(compoundJsonTreeViewItem.ItemType.ToString());
                    }

                    bool optionalNode = previous.IsCanBeDefaulted;
                    if (previous is not BaseCompoundJsonTreeViewItem)
                    {
                        previous = new BaseCompoundJsonTreeViewItem(plan, jsonTool, _container)
                        {
                            IsCanBeDefaulted = optionalNode
                        };
                    }
                    if (previous is BaseCompoundJsonTreeViewItem previousCompoundItem)
                    {
                        if (previousCompoundItem.DataType is not DataType.None)
                        {
                            previousItemTypeList.Add(previousCompoundItem.ItemType.ToString());
                        }
                        for (int j = 0; j < previousItemTypeList.Count; j++)
                        {
                            if (previousItemTypeList[j].Contains("Compound"))
                            {
                                previousItemTypeList[j] = "Compound";
                            }
                            if (previousItemTypeList[j].Contains("Array"))
                            {
                                previousItemTypeList[j] = "Array";
                            }
                        }
                        previousItemTypeList.Sort();
                        if (previousItemTypeList[0] == "Compound")
                        {
                            previousCompoundItem.ElementButtonTip = "展开";
                        }
                        else
                        {
                            previousCompoundItem.ElementButtonTip = "添加在顶部";
                        }
                        if (previousCompoundItem.ItemType is ItemType.List ||
                           previousCompoundItem.ItemType is ItemType.Array ||
                           previousCompoundItem.ItemType is ItemType.Compound ||
                           previousCompoundItem.ItemType is ItemType.OptionalCompound)
                        {
                            previousCompoundItem.AddOrSwitchElementButtonVisibility = Visibility.Visible;
                        }
                        else
                        {
                            previousCompoundItem.AddOrSwitchElementButtonVisibility = Visibility.Collapsed;
                        }
                        previousCompoundItem.ItemType = ItemType.MultiType;
                        previousCompoundItem.ValueTypeBoxVisibility = Visibility.Visible;
                        previousCompoundItem.ValueTypeSource.AddRange(previousItemTypeList.Select(previousValueTypeItem => new TextComboBoxItem() { Text = previousValueTypeItem }));
                        previousCompoundItem.SelectedValueType = previousCompoundItem.ValueTypeSource.FirstOrDefault();
                        if (item is BaseCompoundJsonTreeViewItem CurrentCompoundItem)
                        {
                            previousCompoundItem.CompoundChildrenStringList = [.. CurrentCompoundItem.CompoundChildrenStringList];
                            previousCompoundItem.ArrayChildrenStringList = [.. CurrentCompoundItem.ArrayChildrenStringList];
                            previousCompoundItem.ListChildrenStringList = [.. CurrentCompoundItem.ListChildrenStringList];
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
            if (parent is not null && (parent.ItemType is ItemType.List || (parent.ItemType is ItemType.MultiType && parent.SelectedValueType is not null && parent.SelectedValueType.Text == "List")) && (HadPreIdentifiedAsEnumCompoundType || nodeList.Count > 1) && !IsNoKeyOrMultiDataTypeItem)
            {
                BaseCompoundJsonTreeViewItem entry = new(plan, jsonTool, _container)
                {
                    ItemType = ItemType.Compound,
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
                entry.LogicChildren.AddRange([.. result.Result]);
                result.ResultString = result.ResultString.Replace("\r\n", "\r\n  ");
                if (result.ResultString.Length > 0)
                {
                    result.ResultString.Insert(0, "  ");
                }
                while (result.ResultString.Length > 0 && (result.ResultString[^1] == ',' || result.ResultString[^1] == '\r' || result.ResultString[^1] == '\n' || result.ResultString[^1] == ' '))
                {
                    result.ResultString.Length--;
                }

                int resultLength = result.ResultString.Length;
                result.ResultString.Insert(0, new string(' ', entry.LayerCount * 2) + "{" + (resultLength > 0 ? "\r\n" : ""));
                if (resultLength > 0)
                {
                    result.ResultString.Append("\r\n" + new string(' ', entry.LayerCount * 2));
                }
                result.ResultString.Append('}');
                result.Result.Clear();
                result.Result.Add(entry);
            }
            #endregion

            #region 处理多重类型根节点
            if (isCompoundRoot || isListRoot)
            {
                result.IsHaveRootItem = true;
                while (result.ResultString.Length > 0 && (result.ResultString[^1] == ' ' || result.ResultString[^1] == ',' ||
                    result.ResultString[^1] == '\r' || result.ResultString[^1] == '\n'))
                {
                    result.ResultString.Length--;
                }
                result.ResultString.Append("\r\n");
                if (isCompoundRoot)
                {
                    result.ResultString.Append('}');
                }
                else
                if (isListRoot)
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