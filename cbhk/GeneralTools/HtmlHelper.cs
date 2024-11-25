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
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using static cbhk.CustomControls.JsonTreeViewComponents.Enums;

namespace cbhk.GeneralTools
{
    public partial class HtmlHelper
    {
        #region Field
        public DimensionTypeViewModel plan = null;

        public JsonTreeViewItemExtension jsonTool = null;

        [GeneratedRegex(@"^\s*\s?\:?\s*\s?(\*+)")]
        private static partial Regex GetLineStarCount();

        [GeneratedRegex(@"{{Nbt (inherit/[a-z_\s]+)}}", RegexOptions.IgnoreCase)]
        private static partial Regex GetTemplateKey();

        [GeneratedRegex(@"{{interval\|left=(?<1>\d+)\|right=(?<2>\d+)}}", RegexOptions.IgnoreCase)]
        private static partial Regex GetNumberRange();

        [GeneratedRegex(@"可选", RegexOptions.IgnoreCase)]
        private static partial Regex GetOptionalKey();

        [GeneratedRegex(@"可以为空", RegexOptions.IgnoreCase)]
        private static partial Regex GetIsCanNullableKey();

        [GeneratedRegex(@"^\:?\s*\s?\*+\s+?\{\{nbt\|(?<1>[a-z_]+)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetExplanationForHierarchicalNodes();

        [GeneratedRegex(@"^\:?\s*\s?\*+\s+?\{\{nbt\|(?<1>[a-z_]+)\|(?<2>[\u4e00-\u9fff''><a-z_]+)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetNodeTypeAndCustomKey();

        [GeneratedRegex(@"^\:?\s*\s?\*+\s+?\{\{nbt\|(?<1>[a-z_]+)\|(?<2>[a-z_]+)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetNodeTypeAndKey();

        [GeneratedRegex(@"^\:?\s*\s?\*+\s+?(?<1>\{\{nbt\|[a-z]+\}\})+(\{\{nbt\|(?<2>[a-z]+)\|(?<3>[a-z_]+)\}\})", RegexOptions.IgnoreCase)]
        private static partial Regex GetMultiTypeAndKeyOfNode();

        [GeneratedRegex(@"默认为\{\{cd\|[a-z_]+\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultStringValue();

        [GeneratedRegex(@"默认为\d+", RegexOptions.IgnoreCase)]
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
        private static partial Regex GetContextReference();

        [GeneratedRegex(@"====\s*\s?(minecraft\:[a-z_]+)\s*\s?====", RegexOptions.IgnoreCase)]
        private static partial Regex GetBoldKeywords();

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

        private IContainerProvider _container = null;
        #endregion

        public HtmlHelper(IContainerProvider container)
        {
            _container = container;
            blockService = _container.Resolve<BlockService>();
            entityService = _container.Resolve<EntityService>();
            itemService = _container.Resolve<ItemService>();
        }

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
                    List<string> firstNodeContent = [.. treeviewDivs[0].InnerHtml.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)];
                    #endregion

                    #region 把VSC排版后的格式重新整理(有些应该归为一行的被排版分割为了多行)
                    for (int j = 0; j < firstNodeContent.Count; j++)
                    {
                        if (firstNodeContent[j].Replace("*", "").Trim().Length == 0)
                        {
                            int nextStarLine = j + 1;
                            while (nextStarLine < firstNodeContent.Count && !firstNodeContent[nextStarLine].TrimStart().StartsWith('*'))
                            {
                                firstNodeContent[j] += firstNodeContent[j + 1];
                                firstNodeContent.RemoveAt(j + 1);
                            }
                        }
                    }
                    #endregion

                    #region 添加解析后的内容
                    JsonTreeViewDataStructure firstResultData = GetTreeViewItemResult(new(), firstNodeContent, 2, 1);
                    Result.Result.AddRange(firstResultData.Result);
                    Result.ResultString.Append(firstResultData.ResultString);
                    #endregion

                    #endregion
                }
            }
            return Result;
        }

        /// <summary>
        /// 获取树视图成员
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
        public JsonTreeViewDataStructure GetTreeViewItemResult(JsonTreeViewDataStructure result, List<string> nodeList, int lineNumber, int layerCount, CompoundJsonTreeViewItem Parent = null, JsonTreeViewItem Previous = null, bool isProcessingCommonTemplate = false, int PreviousStarCount = 1)
        {
            //遍历找到的 div 标签内容集合
            for (int i = 0; i < nodeList.Count; i++)
            {
                #region Field
                bool IsExplanationNode = GetExplanationForHierarchicalNodes().Match(nodeList[i]).Success;
                bool IsCurrentOptional = GetOptionalKey().Match(nodeList[i]).Success;
                bool IsHaveNextNode = i < nodeList.Count - 1;
                bool IsNextOptionalNode = false;
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
                    continue;
                }
                #endregion

                #region 声明当前节点
                JsonTreeViewItem item = new()
                {
                    StartLineNumber = lineNumber
                };
                #endregion

                #region 计算当前行星号数量
                Match starMatch = GetLineStarCount().Match(nodeList[i]);
                int starCount = starMatch.Value.Trim().Length;
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
                if(customNodeTypeAndKeyInfoGroupList.Count > 0)
                    targetInfoGroupList = customNodeTypeAndKeyInfoGroupList;

                if (targetInfoGroupList is not null && targetInfoGroupList.Count > 0)
                {
                    foreach (var dataMatch in targetInfoGroupList.Cast<Match>())
                    {
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
                                    IsSimpleItem = true;
                                    item.DataType = DataTypes.Input;
                                    if (dataMatch.Value == "string")
                                        item.DataType = DataTypes.String;
                                    break;
                                }
                        }

                        if (!IsSimpleItem)
                        {
                            if (!isProcessingCommonTemplate)
                            {
                                item.Last = Previous;
                                if (Previous is not null)
                                    Previous.Next = item.Next;
                            }
                            item = new CompoundJsonTreeViewItem(plan, jsonTool);
                            if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
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
                                                if (!IsCurrentOptional)
                                                {
                                                    result.ResultString.Append(new string(' ', layerCount * 2));
                                                    result.ResultString.Append("\"" + key.ToLower() + "\"");
                                                }

                                                if (bool.TryParse(boolMatch.Groups[1].Value, out bool defaultValue) && !IsCurrentOptional)
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

                                                if (!IsCurrentOptional && boolMatch.Groups.Count > 1)
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
                                                if (!IsCurrentOptional)
                                                {
                                                    result.ResultString.Append(new string(' ', layerCount * 2));
                                                    result.ResultString.Append("\"" + key.ToLower() + "\"");
                                                }

                                                item.Value = item.DefaultValue = stringMatch.Groups[1].Value;

                                                if (!IsCurrentOptional && stringMatch.Groups.Count > 1)
                                                {
                                                    result.ResultString.Append(": \"" + stringMatch.Groups[1].Value.ToLower() + "\"");
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

                                            if (!IsCurrentOptional)
                                            {
                                                result.ResultString.Append(new string(' ', layerCount * 2));
                                                result.ResultString.Append("\"" + key.ToLower() + "\"");
                                            }

                                            item.InputBoxVisibility = Visibility.Visible;
                                            if (decimal.TryParse(numberMatch.Groups[1].Value, out decimal defaultDecimalValue))
                                            {
                                                item.Value = item.DefaultValue = defaultDecimalValue;
                                            }

                                            if (!IsCurrentOptional && numberMatch.Groups.Count > 1)
                                            {
                                                result.ResultString.Append(": " + item.Value.ToString().ToLower());
                                            }
                                        }
                                        break;
                                    }
                            }

                            if (Previous is not null && !isProcessingCommonTemplate && Previous.LayerCount == layerCount)
                            {
                                Previous.Next = item;
                                item.Last = Previous;
                            }
                        }
                        #endregion

                        #region 处理复合类型
                        if (item is CompoundJsonTreeViewItem SetTypeCompoundItem)
                        {
                            //设置key
                            SetTypeCompoundItem.Key = key;

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
                                    SetTypeCompoundItem.EnumItemsSource.Add(new TextComboBoxItem() { Text = enumMatch.Value });
                                }
                            }
                            else
                            if (BlockTagMark.Index > 0)
                            {
                                foreach (string blockName in blockService.GetAllBlockName())
                                {
                                    SetTypeCompoundItem.EnumItemsSource.Add(new TextComboBoxItem() { Text = blockName });
                                }
                            }
                            else
                            if (EntityTagMark.Index > 0)
                            {
                                foreach (string entityName in entityService.GetAllEntityName())
                                {
                                    SetTypeCompoundItem.EnumItemsSource.Add(new TextComboBoxItem() { Text = entityName });
                                }
                            }
                            #endregion

                            #region 处理Key与外观
                            if (EnumCollection.Count > 0 || BlockTagMark.Index > 0 || EntityTagMark.Index > 0)
                            {
                                SetTypeCompoundItem.DisplayText = key[0].ToString().ToUpper() + key[1..];
                                SetTypeCompoundItem.DataType = DataTypes.Enum;
                                SetTypeCompoundItem.EnumBoxVisibility = Visibility.Visible;
                                SetTypeCompoundItem.SelectedEnumItem = SetTypeCompoundItem.EnumItemsSource[0];

                                result.ResultString.Append(new string(' ', layerCount * 2));
                                result.ResultString.Append("\"" + key.ToLower() + "\"");
                                result.ResultString.Append(": \"" + SetTypeCompoundItem.EnumItemsSource[0].Text + "\"");
                            }
                            #endregion

                            #endregion

                            #region 处理不同行为的复合型数据

                            #region 确定数据类型
                            if (dataMatch.Groups[1].Value == "compound")
                            {
                                SetTypeCompoundItem.AddElementButtonVisibility = Visibility.Visible;
                                SetTypeCompoundItem.ElementButtonTip = "展开";
                                if (GetIsCanNullableKey().Match(nodeList[i]).Success)
                                {
                                    SetTypeCompoundItem.DataType = DataTypes.NullableCompound;
                                }
                                else
                                if (IsCurrentOptional)
                                {
                                    SetTypeCompoundItem.DataType = DataTypes.OptionalCompound;
                                }
                                //customCompound类型需要结合整体上下文才能判断，故这里不设置
                            }
                            else
                            if (dataMatch.Groups[1].Value == "list")
                            {
                                SetTypeCompoundItem.DataType = DataTypes.List;
                            }
                            else
                                if (dataMatch.Groups[1].Value == "array")
                            {
                                SetTypeCompoundItem.DataType = DataTypes.Array;
                            }
                            #endregion

                            #region 更新非可选复合型节点的文本值与层数
                            SetTypeCompoundItem.DisplayText = key[0].ToString().ToUpper() + key[1..];
                            SetTypeCompoundItem.StartLineNumber = SetTypeCompoundItem.EndLineNumber = lineNumber;
                            SetTypeCompoundItem.LayerCount = layerCount;
                            SetTypeCompoundItem.CurrentValueType ??= SetTypeCompoundItem.ValueTypeList[0];
                            if (key is not null && key.Length > 0 && !IsCurrentOptional)
                            {
                                if (SetTypeCompoundItem.DataType is DataTypes.Array || SetTypeCompoundItem.DataType is DataTypes.List)
                                {
                                    SetTypeCompoundItem.AddElementButtonVisibility = Visibility.Visible;
                                    result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + key + "\": []");
                                }
                                else
                                if (SetTypeCompoundItem.DataType is not DataTypes.OptionalCompound)
                                {
                                    result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + key + "\": {");
                                }
                            }
                            #endregion

                            #endregion

                            #region 处理列表与内联列表
                            //if (key is not null)
                            //{
                            //    SetTypeCompoundItem.AddElementButtonVisibility = Visibility.Visible;
                            //    SetTypeCompoundItem.DataType = DataTypes.Array;
                            //    if (dataMatch.Value == "innerArray")
                            //        SetTypeCompoundItem.DataType = DataTypes.InnerArray;
                            //    SetTypeCompoundItem.Key = key;
                            //    SetTypeCompoundItem.StartLineNumber = SetTypeCompoundItem.EndLineNumber = lineNumber;
                            //    if (Previous is CompoundJsonTreeViewItem lastCompletedCompoundItem && (lastCompletedCompoundItem.DataType is DataTypes.OptionalCompound || lastCompletedCompoundItem.DataType is DataTypes.NullableCompound || lastCompletedCompoundItem.DataType is DataTypes.Compound || lastCompletedCompoundItem.DataType is DataTypes.CustomCompound))
                            //    {
                            //        SetTypeCompoundItem.StartLineNumber = SetTypeCompoundItem.EndLineNumber = lastCompletedCompoundItem.EndLineNumber + 1;
                            //    }

                            //    result.ResultString.Append(new string(' ', layerCount * 2) + (dataMatch.Value == "array" ? "\"" + key + "\"" : ""));
                            //}
                            //result.ResultString.Append((dataMatch.Value == "array" ? ": " : "") + "[]");
                            #endregion

                            #region 处理各种数值提供器或结构模板
                            Match templateMatch = GetTemplateKey().Match(nodeList[i]);
                            if (key is not null && templateMatch.Success && templateMatch.Groups[1] is Group type && plan.CurrentTreeViewMap.TryGetValue(type.Value, out CompoundJsonTreeViewItem currentCompoundItem) && currentCompoundItem is not null)
                            {
                                SetTypeCompoundItem.DisplayText = key[0].ToString().ToUpper() + key[1..];
                                SetTypeCompoundItem.SubChildrenString = currentCompoundItem.SubChildrenString;
                                SetTypeCompoundItem.SwitchKey = currentCompoundItem.SwitchKey;
                                SetTypeCompoundItem.CompoundHead = currentCompoundItem.CompoundHead;
                                SetTypeCompoundItem.DataType = DataTypes.ValueProvider;
                                SetTypeCompoundItem.StartLineNumber = SetTypeCompoundItem.EndLineNumber = lineNumber;
                                SetTypeCompoundItem.EnumBoxVisibility = SetTypeCompoundItem.InputBoxVisibility = Visibility.Visible;
                                SetTypeCompoundItem.EnumItemsSource = currentCompoundItem.EnumItemsSource;
                                SetTypeCompoundItem.SelectedEnumItem = currentCompoundItem.EnumItemsSource.FirstOrDefault()!;
                                SetTypeCompoundItem.CurrentValueType = SetTypeCompoundItem.ValueTypeList[0];

                                if (SetTypeCompoundItem.Value is not null)
                                    result.ResultString.Append(new string(' ', SetTypeCompoundItem.LayerCount * 2) + "\"" + SetTypeCompoundItem.Key + "\": " + SetTypeCompoundItem.Value);
                            }
                            else//表示当前正在初始化值提供器
                            if (isProcessingCommonTemplate && dataMatch.Groups[2] is Group initType)
                            {
                                SetTypeCompoundItem.EnumBoxVisibility = Visibility.Visible;
                                SetTypeCompoundItem.ValueProviderType = (ValueProviderTypes)Enum.Parse(typeof(ValueProviderTypes), initType.Value);
                                SetTypeCompoundItem.DataType = DataTypes.ValueProvider;
                            }
                            #endregion

                            #region 设置前后关系
                            if (Previous is not null && !isProcessingCommonTemplate && Previous.LayerCount == layerCount)
                            {
                                Previous.Next = SetTypeCompoundItem;
                                SetTypeCompoundItem.Last = Previous;
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
                                        List<string> subNodeList = [];
                                        StringBuilder currentSubChildrenString = new();
                                        if (nodeList[nextNodeIndex].Contains('{'))
                                        {
                                            subNodeList.Add(nodeList[nextNodeIndex]);
                                        }
                                        currentSubChildrenString.Append(nodeList[nextNodeIndex] + "\r\n");
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
                                                        subNodeList.Add(nodeList[nextNodeIndex]);
                                                        currentSubChildrenString.Append(nodeList[nextNodeIndex] + "\r\n");
                                                    }
                                                }
                                            }
                                            else
                                                break;
                                            nextLineStar = nextLineStarMatch.Groups[1].Value.Trim();
                                        }

                                        if (subNodeList.Count > 0)
                                        {
                                            i = nextNodeIndex - 1;
                                        }

                                        IsHaveNextNode = i < nodeList.Count - 1;
                                        if (IsHaveNextNode)
                                            IsNextOptionalNode = GetOptionalKey().Match(nodeList[i + 1]).Success;

                                        SetTypeCompoundItem.SubChildrenString = currentSubChildrenString.ToString();

                                        #region 赋予Plan属性以及判断是否需要为上一个复合节点收尾
                                        item.Plan ??= plan;
                                        if (!IsCurrentOptional)
                                        {
                                            if ((item as CompoundJsonTreeViewItem).Children.Count > 0)
                                                result.ResultString.Append("\r\n" + new string(' ', layerCount * 2));
                                            result.ResultString.Append('}');
                                        }
                                        #endregion

                                        #endregion
                                    }
                                }
                            }
                            #endregion
                        }

                        #region 计算可缺省参数的默认值
                        if (((item.DefaultValue is null && item.SelectedEnumItem is null) || item is CompoundJsonTreeViewItem compoundJsonItem && compoundJsonItem.DataType is DataTypes.OptionalCompound) && ((Previous is not null && Previous.StartLineNumber == lineNumber) || (Previous is CompoundJsonTreeViewItem lastCompoundItem && lastCompoundItem.EndLineNumber == lineNumber) || (Previous is null && lineNumber == 2 && item is CompoundJsonTreeViewItem firstCompoundItem && firstCompoundItem.DataType is DataTypes.OptionalCompound)))
                        {
                            if (lineNumber > 1)
                                lineNumber--;
                            item.StartLineNumber = lineNumber;
                            if (item is CompoundJsonTreeViewItem optionalItem)
                            {
                                optionalItem.EndLineNumber = lineNumber;
                            }
                        }
                        #endregion

                        #endregion
                    }
                }
                #endregion

                #region 处理节点的追加
                if (IsNextOptionalNode && !IsExplanationNode && item.Key.Length > 0 && !IsCurrentOptional)
                    result.ResultString.Append(',');

                if (starCount > PreviousStarCount && Parent is not null && item.Key.Length > 0)
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
                #endregion
            }

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
        private Dictionary<string,JsonTreeViewItem> DownwardStructureSearcher(List<HtmlNode> treeNodeList, string[] targetList, string currentKey, HtmlNode currentNode)
        {
            Dictionary<string, JsonTreeViewItem> result = [];

            for (int i = currentNode.Line + 1; i < targetList.Length; i++)
            {
                List<HtmlNode> nodeList = treeNodeList.Where(item=>item.Line == i).ToList();
                if(nodeList.Count > 0)
                {

                }
            }

            return result;
        }
    }
}