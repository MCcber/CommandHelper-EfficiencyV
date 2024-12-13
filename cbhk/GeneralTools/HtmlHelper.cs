using cbhk.CustomControls;
using cbhk.CustomControls.JsonTreeViewComponents;
using cbhk.GeneralTools.DataService;
using cbhk.GeneralTools.TreeViewComponentsHelper;
using cbhk.Model.Common;
using cbhk.ViewModel.Generators;
using HtmlAgilityPack;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using static cbhk.CustomControls.JsonTreeViewComponents.Enums;

namespace cbhk.GeneralTools
{
    public partial class HtmlHelper(IContainerProvider container)
    {
        #region Field
        public DimensionTypeViewModel plan = null;

        public JsonTreeViewItemExtension jsonTool = null;

        private List<string> EnumKeyList = ["命名空间ID"];

        [GeneratedRegex(@"\[\[\#(?<1>[\u4e00-\u9fff]+)\|[\u4e00-\u9fff]+\]\]")]
        private static partial Regex GetContextKey();

        [GeneratedRegex(@"^\s*\s?\:?\s*\s?(\*+)")]
        private static partial Regex GetLineStarCount();

        [GeneratedRegex(@"{{(?<1>n|N)bt (?<2>inherit/[a-z_\s]+)}}", RegexOptions.IgnoreCase)]
        private static partial Regex GetTemplateKey();

        [GeneratedRegex(@"{{interval\|left=(?<1>\d+)\|right=(?<2>\d+)}}", RegexOptions.IgnoreCase)]
        private static partial Regex GetNumberRange();

        [GeneratedRegex(@"可选", RegexOptions.IgnoreCase)]
        private static partial Regex GetOptionalKey();

        [GeneratedRegex(@"可以为空", RegexOptions.IgnoreCase)]
        private static partial Regex GetIsCanNullableKey();

        [GeneratedRegex(@"^\:?\s*\s?\*+\s+?\{\{nbt\|(?<1>[a-z_]+)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetExplanationForHierarchicalNodes();

        [GeneratedRegex(@"^\:?\s*\s?\*+\s+?\{\{nbt\|(?<1>[a-z_]+)\|(?<2>[\u4e00-\u9fff''><]+)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetNodeTypeAndCustomKey();

        [GeneratedRegex(@"^\:?\s*\s?\*+\s+?\{\{nbt\|(?<1>[a-z_]+)\|(?<2>[a-z_]+)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetNodeTypeAndKey();

        [GeneratedRegex(@"^\:?\s*\s?\*+\s+?(?<1>\{\{nbt\|[a-z]+\}\})+(\{\{nbt\|(?<2>[a-z]+)\|(?<3>[a-z_]+)\}\})", RegexOptions.IgnoreCase)]
        private static partial Regex GetMultiTypeAndKeyOfNode();

        [GeneratedRegex(@"默认为\{\{cd\|[a-z_]+\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultStringValue();

        [GeneratedRegex(@"默认为(\d)+", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultNumberValue();

        [GeneratedRegex(@"默认为\{\{cd\|(true|false)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultBoolValue();

        [GeneratedRegex(@"\[\[方块标签\]\]", RegexOptions.IgnoreCase)]
        private static partial Regex GetBlcokTagValue();

        [GeneratedRegex(@"\[\[实体标签\]\]", RegexOptions.IgnoreCase)]
        private static partial Regex GetEntityTagValue();

        [GeneratedRegex(@"\{\{cd\|[a-z:_]+\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetEnumValue();

        [GeneratedRegex(@"==\s*\s?([\u4e00-\u9fffa-z_]+)\s*\s?==", RegexOptions.IgnoreCase)]
        private static partial Regex GetContextFileMarker();

        [GeneratedRegex(@"====\s*\s?(minecraft\:[a-z_]+)\s*\s?====", RegexOptions.IgnoreCase)]
        private static partial Regex GetEnumTypeKeywords();

        private string AdvancementWikiFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\1.20.4.wiki";
        /// <summary>
        /// 包含关键字的行
        /// </summary>
        public Dictionary<string, HtmlNode> EnumStructureList = [];
        /// <summary>
        /// 当前上下文引用结构
        /// </summary>
        public Dictionary<string, HtmlNode> CurrentContextReference = [];
        public List<string> ProgressClassList = ["treeview"];

        private BlockService blockService = null;
        private EntityService entityService = null;
        private ItemService itemService = null;

        private IContainerProvider _container = container;

        #endregion

        public JsonTreeViewDataStructure AnalyzeHTMLData(string directoryPath)
        {
            JsonTreeViewDataStructure Result = new();
            HtmlDocument htmlDocument = new();

            foreach (var file in Directory.GetFiles(directoryPath))
            {
                string wikiData = File.ReadAllText(file);
                string[] wikiLines = File.ReadAllLines(file);
                htmlDocument.LoadHtml(wikiData);
                List<HtmlNode> treeviewDivs = [.. htmlDocument.DocumentNode.SelectNodes("//div[@class='treeview']")];

                if (treeviewDivs is not null)
                {
                    #region 由于第一个HtmlNode实例的InnerHtml属性总会包含所有标签的内容，所以这里需要进行特殊处理

                    #region 直接覆盖InnerHtml属性会导致内容错误，所以这里直接用链表来提取正确的结构数据
                    List<string> nodeContent = [.. treeviewDivs[0].InnerHtml.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)];
                    #endregion

                    #region 把VSC排版后的格式重新整理(有些应该归为一行的被排版分割为了多行)
                    for (int j = 0; j < nodeContent.Count; j++)
                    {
                        if (nodeContent[j].Replace("*", "").Trim().Length == 0)
                        {
                            int nextStarLine = j + 1;
                            while (nextStarLine < nodeContent.Count && !nodeContent[nextStarLine].TrimStart().StartsWith('*'))
                            {
                                nodeContent[j] += nodeContent[j + 1];
                                nodeContent.RemoveAt(j + 1);
                            }
                        }
                    }
                    #endregion

                    #region 执行解析、分析是否需要被添加为依赖项
                    JsonTreeViewDataStructure resultData = GetTreeViewItemResult(new(), nodeContent, 2, 1);
                    Match contextFileMarker = GetContextFileMarker().Match(wikiLines[0]);
                    if (contextFileMarker is not null && contextFileMarker.Groups.Count > 1 && contextFileMarker.Groups[1].Value != "主格式")
                    {
                        if (resultData.DependencyList.TryGetValue(contextFileMarker.Groups[1].Value, out List<JsonTreeViewItem> list))
                        {
                            list.AddRange(resultData.Result);
                        }
                        else
                        {
                            resultData.DependencyList.Add(contextFileMarker.Groups[1].Value, [.. resultData.Result]);
                        }
                    }
                    #endregion

                    Result.Result.AddRange(resultData.Result);
                    Result.ResultString.Append(resultData.ResultString);
                    #endregion
                }
            }
            return Result;
        }

        /// <summary>
        /// 转换Wiki文档为树视图节点
        /// </summary>
        /// <param name="result">返回结果</param>
        /// <param name="nodeList">需处理的节点文本列表</param>
        /// <param name="lineNumber">起始行号</param>
        /// <param name="layerCount">层级</param>
        /// <param name="Parent">父节点</param>
        /// <param name="Previous">前一个节点</param>
        /// <param name="isProcessingCommonTemplate">是否正在处理模板</param>
        /// <param name="PreviousStarCount">前一个节点星号数量</param>
        /// <returns></returns>
        public JsonTreeViewDataStructure GetTreeViewItemResult(JsonTreeViewDataStructure result, List<string> nodeList, int lineNumber, int layerCount, CompoundJsonTreeViewItem Parent = null, JsonTreeViewItem Previous = null, int PreviousStarCount = 1)
        {
            //遍历找到的 div 标签内容集合
            for (int i = 0; i < nodeList.Count; i++)
            {
                #region 字段
                //bool IsPreviousOptionalNode = false;
                bool IsExplanationNode = GetExplanationForHierarchicalNodes().Match(nodeList[i]).Success;
                bool IsCurrentOptionalNode = GetOptionalKey().Match(nodeList[i]).Success;
                bool IsHaveNextNode = i < nodeList.Count - 1 && GetNodeTypeAndKey().Match(nodeList[i + 1]).Success;
                bool IsNextOptionalNode = false;
                #endregion

                #region 计算当前行星号数量
                Match starMatch = GetLineStarCount().Match(nodeList[i]);
                int starCount = starMatch.Value.Trim().Length;
                #endregion

                #region 判断是否跳过本次处理
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
                    if (result.ResultString.ToString().TrimEnd(['\r','\n']).EndsWith(','))
                    {
                        int index = result.ResultString.ToString().LastIndexOf(',');
                        result.ResultString.Remove(result.ResultString.Length - 5,1);
                    }
                    continue;
                }
                #endregion

                #region 得到上一个同级节点的索引
                //if (i > 0 && !IsCurrentOptionalNode)
                //{
                //    int PreviousIndex = i;
                //    bool PreviousIsBrother = GetLineStarCount().Match(nodeList[PreviousIndex]).Value.Trim().Length > starCount;
                //    do
                //    {
                //        PreviousIndex--;
                //        PreviousIsBrother = GetLineStarCount().Match(nodeList[PreviousIndex]).Value.Trim().Length > starCount;
                //    }
                //    while (PreviousIsBrother);

                //    PreviousIsBrother = PreviousIndex > -1;

                //    if (PreviousIsBrother && PreviousIndex > -1)
                //    {
                //        IsPreviousOptionalNode = GetOptionalKey().Match(nodeList[PreviousIndex]).Success;
                //    }
                //}
                #endregion

                #region 声明当前节点
                JsonTreeViewItem item = new()
                {
                    StartLineNumber = lineNumber,
                    LayerCount = layerCount
                };
                #endregion

                #region 分析是否需要引用依赖项
                List<JsonTreeViewItem> dependencyList = [];
                Match contextFileMarker = GetContextFileMarker().Match(nodeList[0]);
                if (contextFileMarker is not null && contextFileMarker.Groups.Count > 1 && result.DependencyList.TryGetValue(contextFileMarker.Groups[1].Value, out List<JsonTreeViewItem> list))
                {
                    dependencyList = list;
                }
                #endregion

                #region 处理两大值类型

                //匹配节点类型和键
                MatchCollection nodeTypeAndKeyInfoGroupList = GetNodeTypeAndKey().Matches(nodeList[i]);
                //匹配多重节点类型和键
                MatchCollection multiNodeTypeAndKeyInfoGroupList = GetMultiTypeAndKeyOfNode().Matches(nodeList[i]);
                //匹配自定义节点类型和键
                MatchCollection customNodeTypeAndKeyInfoGroupList = GetNodeTypeAndCustomKey().Matches(nodeList[i]);

                MatchCollection targetInfoGroupList = null;
                if (nodeTypeAndKeyInfoGroupList.Count > 0)
                    targetInfoGroupList = nodeTypeAndKeyInfoGroupList;
                else
                if (multiNodeTypeAndKeyInfoGroupList.Count > 0)
                    targetInfoGroupList = multiNodeTypeAndKeyInfoGroupList;
                if (customNodeTypeAndKeyInfoGroupList.Count > 0)
                    targetInfoGroupList = customNodeTypeAndKeyInfoGroupList;

                if (targetInfoGroupList is not null && targetInfoGroupList.Count > 0)
                {
                    Match dataMatch = targetInfoGroupList[0];

                    #region 确定当前的节点数据类型
                    if (dataMatch.Groups.Count < 2)
                    {
                        continue;
                    }
                    string key = dataMatch.Groups[2].Value;
                    bool IsSimpleItem = false;
                    switch (dataMatch.Groups[1].Value)
                    {
                        case "bool":
                            {
                                IsSimpleItem = true;
                                item.DataType = DataTypes.Bool;
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
                                //确定字符串节点是否为枚举类型
                                int EnumKeyCount = EnumKeyList.Where(item => nodeList[i].Contains(item)).Count();
                                if (EnumKeyCount > 0)
                                {
                                    item.DataType = DataTypes.Enum;
                                }
                                else
                                {
                                    IsSimpleItem = true;
                                    object dataTypes = DataTypes.Input;
                                    bool parseResult = Enum.TryParse(typeof(DataTypes), dataMatch.Groups[1].Value[0].ToString().ToUpper() + dataMatch.Groups[1].Value[1..], out dataTypes);
                                    if (parseResult)
                                    {
                                        item.DataType = (DataTypes)dataTypes;
                                    }
                                }
                                break;
                            }
                    }

                    if (!IsSimpleItem)
                    {
                        item = new CompoundJsonTreeViewItem(plan, jsonTool);
                        if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem && multiNodeTypeAndKeyInfoGroupList.Count > 0)
                        {
                            compoundJsonTreeViewItem.ValueTypeList.Add(new TextComboBoxItem()
                            {
                                Text = dataMatch.Groups[1].Value
                            });
                        }
                    }
                    #endregion

                    #region 处理值类型
                    if (item is JsonTreeViewItem and not CompoundJsonTreeViewItem)
                    {
                        switch (item.DataType)
                        {
                            case DataTypes.Bool:
                                {
                                    if (dataMatch.Groups.Count > 1)
                                    {
                                        item.Key = key;
                                        item.DisplayText = key[0].ToString().ToUpper() + key[1..];
                                        item.BoolButtonVisibility = Visibility.Visible;

                                        Match boolMatch = GetDefaultBoolValue().Match(nodeList[i]);
                                        if (boolMatch is not null)
                                        {
                                            if (!IsCurrentOptionalNode)
                                            {
                                                result.ResultString.Append(new string(' ', layerCount * 2));
                                                result.ResultString.Append("\"" + key.ToLower() + "\"");
                                            }

                                            if (bool.TryParse(boolMatch.Groups[1].Value, out bool defaultValue) && !IsCurrentOptionalNode)
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
                                                item.IsCanBeDefaulted = true;
                                                item.IsTrue = item.IsFalse = false;
                                            }

                                            if (!IsCurrentOptionalNode && boolMatch.Groups.Count > 1)
                                            {
                                                result.ResultString.Append(": " + boolMatch.Groups[1].Value.ToLower());
                                            }
                                        }
                                    }
                                    break;
                                }
                            case DataTypes.Input:
                            case DataTypes.String:
                                {
                                    if (dataMatch.Groups.Count > 1)
                                    {
                                        item.Key = key;
                                        item.DisplayText = key[0].ToString().ToUpper() + key[1..];
                                        item.InputBoxVisibility = Visibility.Visible;

                                        Match stringMatch = GetDefaultStringValue().Match(nodeList[i]);
                                        if (stringMatch is not null)
                                        {
                                            if (!IsCurrentOptionalNode)
                                            {
                                                result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + key.ToLower() + "\"");
                                            }

                                            if (stringMatch.Groups.Count > 1)
                                            {
                                                item.Value = item.DefaultValue = "\"" + stringMatch.Groups[1].Value + "\"";
                                            }

                                            if (!IsCurrentOptionalNode && stringMatch.Groups.Count > 1)
                                            {
                                                result.ResultString.Append(": \"" + stringMatch.Groups[1].Value.ToLower() + "\"");
                                            }
                                            else
                                            {
                                                result.ResultString.Append(": \"\"");
                                            }
                                        }
                                    }
                                    break;
                                }
                            case DataTypes.Byte:
                            case DataTypes.Short:
                            case DataTypes.Int:
                            case DataTypes.Float:
                            case DataTypes.Double:
                            case DataTypes.Decimal:
                            case DataTypes.Long:
                                {
                                    Match numberMatch = GetDefaultNumberValue().Match(nodeList[i]);
                                    if (numberMatch is not null)
                                    {
                                        item.Key = key;
                                        item.DisplayText = key[0].ToString().ToUpper() + key[1..];
                                        item.InputBoxVisibility = Visibility.Visible;

                                        if (!IsCurrentOptionalNode)
                                        {
                                            result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + key.ToLower() + "\"");
                                        }

                                        item.InputBoxVisibility = Visibility.Visible;
                                        if (decimal.TryParse(numberMatch.Groups[1].Value, out decimal defaultDecimalValue))
                                        {
                                            item.Value = item.DefaultValue = defaultDecimalValue;
                                        }

                                        if (!IsCurrentOptionalNode && numberMatch.Groups.Count > 1)
                                        {
                                            result.ResultString.Append(": " + item.Value.ToString().ToLower());
                                        }
                                    }
                                    break;
                                }
                        }

                        if (Previous is not null && Previous.LayerCount == layerCount)
                        {
                            Previous.Next = item;
                            item.Last = Previous;
                        }
                    }
                    #endregion

                    #region 处理复合类型
                    if (item is CompoundJsonTreeViewItem CurrentCompoundItem)
                    {
                        //设置key
                        if (CurrentCompoundItem.Key.Length == 0)
                        {
                            CurrentCompoundItem.Key = key;
                        }

                        #region 处理枚举型数据

                        #region 抓取数据
                        MatchCollection EnumCollection = GetEnumValue().Matches(nodeList[i]);
                        Match BlockTagMark = GetBlcokTagValue().Match(nodeList[i]);
                        Match EntityTagMark = GetEntityTagValue().Match(nodeList[i]);
                        #endregion

                        #region 添加枚举成员
                        if (EnumCollection.Count > 0)
                        {
                            foreach (Match enumMatch in EnumCollection)
                            {
                                CurrentCompoundItem.EnumItemsSource.Add(new TextComboBoxItem() { Text = enumMatch.Value });
                            }
                        }
                        else
                        if (BlockTagMark.Index > 0)
                        {
                            foreach (string blockName in blockService.GetAllBlockName())
                            {
                                CurrentCompoundItem.EnumItemsSource.Add(new TextComboBoxItem() { Text = blockName });
                            }
                        }
                        else
                        if (EntityTagMark.Index > 0)
                        {
                            foreach (string entityName in entityService.GetAllEntityName())
                            {
                                CurrentCompoundItem.EnumItemsSource.Add(new TextComboBoxItem() { Text = entityName });
                            }
                        }
                        #endregion

                        #region 处理Key与外观
                        if (EnumCollection.Count > 0 || BlockTagMark.Index > 0 || EntityTagMark.Index > 0)
                        {
                            CurrentCompoundItem.DisplayText = key[0].ToString().ToUpper() + key[1..];
                            CurrentCompoundItem.DataType = DataTypes.Enum;
                            CurrentCompoundItem.EnumBoxVisibility = Visibility.Visible;
                            CurrentCompoundItem.SelectedEnumItem = CurrentCompoundItem.EnumItemsSource[0];

                            result.ResultString.Append(new string(' ', layerCount * 2) +
                                "\"" + key.ToLower() + "\"" +
                                ": \"" + CurrentCompoundItem.EnumItemsSource[0].Text + "\"");
                        }
                        #endregion

                        #endregion

                        #region 处理不同行为的复合型数据

                        #region 确定数据类型
                        if (dataMatch.Groups[1].Value == "compound")
                        {
                            CurrentCompoundItem.AddElementButtonVisibility = Visibility.Visible;
                            CurrentCompoundItem.ElementButtonTip = "展开";
                            if (GetIsCanNullableKey().Match(nodeList[i]).Success)
                            {
                                CurrentCompoundItem.DataType = DataTypes.NullableCompound;
                            }
                            else
                            if (IsCurrentOptionalNode)
                            {
                                CurrentCompoundItem.DataType = DataTypes.OptionalCompound;
                            }

                            if (customNodeTypeAndKeyInfoGroupList.Count > 0 && customNodeTypeAndKeyInfoGroupList[0].Groups.Count > 1)
                            {
                                CurrentCompoundItem.DataType = DataTypes.CustomCompound;
                            }
                        }
                        else
                        if (dataMatch.Groups[1].Value == "list")
                        {
                            CurrentCompoundItem.DataType = DataTypes.List;
                        }
                        else
                            if (dataMatch.Groups[1].Value == "array")
                        {
                            CurrentCompoundItem.DataType = DataTypes.Array;
                        }
                        #endregion

                        #region 更新非可选复合型节点的文本值与层数
                        CurrentCompoundItem.DisplayText = key[0].ToString().ToUpper() + key[1..];
                        CurrentCompoundItem.StartLineNumber = CurrentCompoundItem.EndLineNumber = lineNumber;
                        CurrentCompoundItem.LayerCount = layerCount;
                        if (CurrentCompoundItem.ValueTypeList.Count > 0)
                        {
                            CurrentCompoundItem.CurrentValueType ??= CurrentCompoundItem.ValueTypeList[0];
                        }
                        if (key is not null && key.Length > 0 && !IsCurrentOptionalNode)
                        {
                            if (CurrentCompoundItem.DataType is DataTypes.Array || CurrentCompoundItem.DataType is DataTypes.List)
                            {
                                CurrentCompoundItem.AddElementButtonVisibility = Visibility.Visible;
                                result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + key + "\": []");
                            }
                            else
                            if (CurrentCompoundItem.DataType is not DataTypes.OptionalCompound)
                            {
                                result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + key + "\": {");
                            }
                        }
                        #endregion

                        #endregion

                        #region 处理所需上下文数据
                        //匹配上下文关键字
                        Match contextKeyMatch = GetContextKey().Match(nodeList[i]);
                        if (contextKeyMatch is not null && contextKeyMatch.Groups.Count > 1 &&
                            plan is not null && plan.CurrentTreeViewMap.Count > 0)
                        {
                            IEnumerable<string> keyList = plan.CurrentTreeViewMap.Keys.Where(key => key.Contains(contextKeyMatch.Groups[1].Value));
                            List<JsonTreeViewItem> contextTreeViewItemList = plan.CurrentTreeViewMap[keyList.FirstOrDefault()];
                            CurrentCompoundItem.Children.AddRange(contextTreeViewItemList);

                            //判断搜索到的字典链表中的根节点的Key是否包含需要被替代的部分，有则开启当前节点的文本框
                            if (contextTreeViewItemList[0].Key.Contains('<') &&
                                contextTreeViewItemList[0].Key.Contains('\''))
                            {
                                CurrentCompoundItem.InputBoxVisibility = Visibility.Visible;
                            }
                        }
                        #endregion

                        #region 处理顶级数据结构
                        Match templateMatch = GetTemplateKey().Match(nodeList[i]);
                        if (key is not null && templateMatch.Success && templateMatch.Groups[2] is Group type && plan.CurrentTreeViewMap.TryGetValue(type.Value[0].ToString().ToLower() + string.Join(' ', type.Value[1..].Split('_').Select(item => item[0].ToString().ToUpper() + item[1..])), out List<JsonTreeViewItem> templateCompoundItem) && templateCompoundItem is not null)
                        {
                            CurrentCompoundItem.Children.AddRange(templateCompoundItem);
                            if (CurrentCompoundItem.DataType is not DataTypes.OptionalCompound)
                            {
                                result.ResultString.Append(new string(' ', CurrentCompoundItem.LayerCount * 2) + "\"" + key + "\": {");
                            }
                        }
                        #endregion

                        #region 设置前后关系
                        if (Previous is not null && Previous.LayerCount == layerCount)
                        {
                            Previous.Next = CurrentCompoundItem;
                            CurrentCompoundItem.Last = Previous;
                        }
                        #endregion

                        #region 处理递归
                        //获取下一行的*数量
                        int nextNodeIndex = i + 1;
                        if (nextNodeIndex < nodeList.Count)
                        {
                            Match nextLineStarMatch = GetLineStarCount().Match(nodeList[nextNodeIndex]);
                            if (nextLineStarMatch.Success && nextLineStarMatch.Groups.Count > 1)
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
                                    else
                                        if (currentSubChildren.Count > 0)
                                    {
                                        i++;
                                    }
                                    #endregion

                                    #region 是否存储原始信息、处理递归
                                    if (CurrentCompoundItem.DataType is DataTypes.Compound || CurrentCompoundItem.DataType is DataTypes.Array)
                                    {
                                        JsonTreeViewDataStructure subResult = GetTreeViewItemResult(new(), currentSubChildren, lineNumber, layerCount + 1, CurrentCompoundItem);

                                        if (subResult.ResultString.Length > 0)
                                        {
                                            result.ResultString.Append("\r\n" + subResult.ResultString + "\r\n" + new string(' ', layerCount * 2) + "}\r\n");
                                        }
                                        else
                                        {
                                            CurrentCompoundItem.ChildrenStringList = currentSubChildren;
                                            result.ResultString.Append('}');
                                        }
                                    }
                                    else
                                        CurrentCompoundItem.ChildrenStringList = currentSubChildren;
                                    #endregion

                                    #region 判断后一个节点是否可选
                                    IsHaveNextNode = i < nodeList.Count - 1;
                                    if (IsHaveNextNode)
                                        IsNextOptionalNode = GetOptionalKey().Match(nodeList[i + 1]).Success;
                                    #endregion

                                    //赋予Plan属性
                                    item.Plan ??= plan;
                                }
                                else
                                {
                                    IsNextOptionalNode = GetOptionalKey().Match(nodeList[i + 1]).Success;
                                }
                            }
                        }
                        #endregion
                    }

                    #region 计算可缺省参数的默认值
                    //真正要使用的时候再设置具体行号
                    if (item is CompoundJsonTreeViewItem compoundJsonItem && IsCurrentOptionalNode)
                    {
                        compoundJsonItem.StartLineNumber = compoundJsonItem.EndLineNumber = 0;
                    }
                    else
                        if (IsCurrentOptionalNode)
                    {
                        item.StartLineNumber = 0;
                    }
                    #endregion

                    #endregion
                }
                #endregion

                #region 处理节点的追加
                if (i < nodeList.Count - 1 && !result.ResultString.ToString().TrimEnd(['\r', '\n']).EndsWith(',') && !IsNextOptionalNode && IsHaveNextNode)
                {
                    result.ResultString.Append(",\r\n");
                }

                if (Parent is not null && item.Key.Length > 0)
                {
                    Parent.Children.Add(item);
                }
                else
                if (item.Key.Length > 0)
                {
                    result.Result.Add(item);
                }
                #endregion

                #region 将当前节点保存为上一个节点
                //将当前节点保存为上一个节点
                Previous = item;
                //保存当前的*号数量
                PreviousStarCount = starCount;
                //行号加1
                if (!IsCurrentOptionalNode)
                {
                    lineNumber++;
                }
                #endregion
            }

            #region 复合节点收尾
            if (result.Result.Count > 0 && result.Result[^1] is CompoundJsonTreeViewItem compound && compound.DataType is DataTypes.Compound)
            {
                result.ResultString.Append('}');
            }
            #endregion

            return result;
        }

        /// <summary>
        /// 向下结构搜索器
        /// </summary>
        /// <param name="treeNodeList">树节点列表</param>
        /// <param name="targetList">目标链表</param>
        /// <param name="currentKey">当前Key</param>
        /// <param name="currentNode">当前div节点</param>
        /// <returns></returns>
        private Dictionary<string, JsonTreeViewItem> DownwardStructureSearcher(List<HtmlNode> treeNodeList, string[] targetList, string currentKey, HtmlNode currentNode)
        {
            Dictionary<string, JsonTreeViewItem> result = [];

            for (int i = currentNode.Line + 1; i < targetList.Length; i++)
            {
                List<HtmlNode> nodeList = treeNodeList.Where(item => item.Line == i).ToList();
                if (nodeList.Count > 0)
                {

                }
            }

            return result;
        }
    }
}