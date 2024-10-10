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
    public partial class HtmlHelper
    {
        #region Field
        public Dictionary<string, CompoundJsonTreeViewItem> ValueProviderContextDictionary { get; set; } = [];

        DimensionTypeViewModel plan = new(null, null);

        JsonTreeViewItemExtension jsonTool = new();

        [GeneratedRegex(@"^\s+?\:?\s+?(\*+)")]
        private static partial Regex GetLineStarCount();

        [GeneratedRegex(@"{{Nbt inherit/[a-z_\s]+}}", RegexOptions.IgnoreCase)]
        private static partial Regex GetTemplateKey();

        [GeneratedRegex(@"可选", RegexOptions.IgnoreCase)]
        private static partial Regex GetOptionalKey();

        [GeneratedRegex(@"可以为空", RegexOptions.IgnoreCase)]
        private static partial Regex GetIsCanNullableKey();

        [GeneratedRegex(@"^\:?\s+?\*+\s+?\{\{nbt\|(?<1>[a-z_]+)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetExplanationForHierarchicalNodes();

        [GeneratedRegex(@"^\:?\s+?\*+\s+?\{\{nbt\|(?<1>[a-z_]+)\|(?<2>[a-z_]+)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetNodeTypeAndKey();

        [GeneratedRegex(@"^\:?\s+?\*+\s+?(?<branch>\{\{nbt\|[a-z]+\}\})+(\{\{nbt\|(?<1>[a-z]+)\|(?<2>[a-z_]+)\}\})", RegexOptions.IgnoreCase)]
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

        [GeneratedRegex(@"====\s(minecraft\:[a-z_]+)\s====", RegexOptions.IgnoreCase)]
        private static partial Regex GetBoldKeywords();

        private string AdvancementWikiFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\1.20.4.wiki";
        private Dictionary<int, string> WikiDesignateLine = [];
        private string[] ProgressClassList = ["treeview"];

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

        public JsonTreeViewDataStructure AnalyzeHTMLData(string fileName)
        {
            JsonTreeViewDataStructure Result = new();
            HtmlDocument doc = new()
            {
                OptionFixNestedTags = true,
                OptionAutoCloseOnEnd = true
            };

            string wikiData = File.ReadAllText(fileName);
            string[] wikiLines = File.ReadAllLines(fileName);
            doc.LoadHtml(wikiData);
            List<HtmlNode> treeviewDivs = [.. doc.DocumentNode.SelectNodes("//div[@class='treeview']")];

            MatchCollection keywordList = GetBoldKeywords().Matches(wikiData);

            for (int i = 0; i < wikiLines.Length; i++)
            {
                Match content = GetBoldKeywords().Match(wikiLines[i]);
                if (content is not null && content.Success)
                {
                    WikiDesignateLine.Add(i + 1, content.Groups[1].Value);
                }
            }

            if (treeviewDivs is not null)
            {
                #region 由于第一个HtmlNode实例的InnerHtml属性总会包含所有标签的内容，所以这里需要进行特殊处理

                #region 直接覆盖InnerHtml属性会导致内容错误，所以这里直接用链表来提取正确的结构数据
                List<string> firstNodeContent = [];
                for (int i = 0; i < treeviewDivs[1].Line - 2; i++)
                {
                    if (wikiLines[i].TrimStart().StartsWith('<') || wikiLines[i].TrimStart().StartsWith('='))
                    {
                        continue;
                    }
                    firstNodeContent.Add(wikiLines[i]);
                }
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

                #region 添加第一个div解析后的内容
                JsonTreeViewDataStructure firstResultData = GetTreeViewItemResult(new(), firstNodeContent, 2, 1);
                Result.Result.AddRange(firstResultData.Result);
                Result.ResultString.Append(firstResultData.ResultString);
                int test = 0;
                #endregion

                #endregion

                #region 从第二个节点开始循环处理
                for (int i = 1; i < treeviewDivs.Count; i++)
                {
                    IEnumerable<string> classList = treeviewDivs[i].GetClasses();
                    if (ProgressClassList.Intersect(classList).Any())
                    {
                        int line = treeviewDivs[i].Line;
                        // 可以在这里对每个 div 标签进行操作，比如获取属性、子节点等
                        List<string> nodeList = [.. treeviewDivs[i].InnerHtml.Split("\r\n")];

                        #region 把VSC排版后的格式重新整理(有些应该归为一行的被排版分割为了多行)
                        for (int j = 0; j < nodeList.Count; j++)
                        {
                            if (nodeList[j].Replace("*", "").Trim().Length == 0)
                            {
                                int nextStarLine = j + 1;
                                while (nextStarLine < nodeList.Count && !nodeList[nextStarLine].TrimStart().StartsWith('*'))
                                {
                                    nodeList[j] += nodeList[j + 1];
                                    nodeList.RemoveAt(j + 1);
                                }
                            }
                        }
                        #endregion

                        JsonTreeViewDataStructure data = GetTreeViewItemResult(new(), nodeList, 2, 1);

                        string currentKey = GetDesignateKeywordByLineNumber(line - 2);
                        if (currentKey.Length > 0)
                        {
                            //CriteriaDataList.Add(currentKey, [.. data.Result]);
                        }
                        Result.Result.AddRange(data.Result);
                        Result.ResultString.Append(data.ResultString);
                    }
                }
                #endregion
            }
            return Result;
        }

        private JsonTreeViewDataStructure GetTreeViewItemResult(JsonTreeViewDataStructure result, List<string> nodeList, int lineNumber, int layerCount, CompoundJsonTreeViewItem Parent = null, JsonTreeViewItem Last = null, bool isProcessingTemplate = false, int LastStarCount = 1)
        {
            #region 处理起始字符串
            if (result.ResultString.Length == 0)
                result.ResultString.Append("{\r\n");
            #endregion

            //遍历找到的 div 标签内容集合
            for (int i = 0; i < nodeList.Count; i++)
            {
                #region Field
                bool IsExplanationNode = GetExplanationForHierarchicalNodes().Match(nodeList[i]).Success;
                bool IsCurrentOptional = GetOptionalKey().Match(nodeList[i]).Success;
                bool IsHaveNextNode = i < nodeList.Count - 1;
                bool IsNextOptionalNode = false;
                if (IsHaveNextNode)
                    IsNextOptionalNode = GetOptionalKey().Match(nodeList[i + 1]).Success;
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

                #region 处理值类型
                MatchCollection nodeTypeAndKeyInfoGroupList = GetNodeTypeAndKey().Matches(nodeList[i]);
                MatchCollection multiNodeTypeAndKeyInfoGroupList = GetMultiTypeAndKeyOfNode().Matches(nodeList[i]);

                MatchCollection? targetInfoGroupList = null;
                if (nodeTypeAndKeyInfoGroupList.Count > 0)
                    targetInfoGroupList = nodeTypeAndKeyInfoGroupList;
                else
                if (multiNodeTypeAndKeyInfoGroupList.Count > 0)
                    targetInfoGroupList = multiNodeTypeAndKeyInfoGroupList;

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
                            if (!isProcessingTemplate)
                            {
                                item.Last = Last;
                                if (Last is not null)
                                    Last.Next = item.Next;
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
                                            item.BoolButtonVisibility = Visibility.Visible;

                                            Match boolMatch = GetDefaultBoolValue().Match(nodeList[i]);
                                            if (boolMatch is not null)
                                            {
                                                if (!IsCurrentOptional)
                                                {
                                                    result.ResultString.Append(new string(' ', layerCount * 2));
                                                    result.ResultString.Append("\"" + key.ToLower() + "\"");
                                                }

                                                if (bool.TryParse(boolMatch.Groups[1].Value, out bool defaultValue))
                                                {
                                                    item.Value = item.DefaultValue = defaultValue;
                                                    if (item.Value)
                                                    {
                                                        item.IsTrue = true;
                                                        item.IsFalse = false;
                                                    }
                                                }

                                                if (!IsCurrentOptional)
                                                {
                                                    result.ResultString.Append(": " + item.Value.ToString().ToLower());
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

                                                if (!IsCurrentOptional)
                                                {
                                                    result.ResultString.Append(": \"" + item.Value.ToString().ToLower() + "\"");
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

                                            if (!IsCurrentOptional)
                                            {
                                                result.ResultString.Append(": " + item.Value.ToString().ToLower());
                                            }
                                        }
                                        break;
                                    }
                            }

                            item.StartLineNumber = lineNumber;
                            if (Last is not null && !isProcessingTemplate && Last.LayerCount == layerCount)
                            {
                                Last.Next = item;
                                item.Last = Last;
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
                                SetTypeCompoundItem.DataType = DataTypes.Array;
                            }
                            #endregion

                            #region 更新非可选复合型节点的文本值与层数
                            result.ResultString.Append(new string(' ', layerCount * 2));
                            SetTypeCompoundItem.StartLineNumber = SetTypeCompoundItem.EndLineNumber = lineNumber;
                            SetTypeCompoundItem.LayerCount = layerCount;
                            SetTypeCompoundItem.CurrentValueType ??= SetTypeCompoundItem.ValueTypeList[0];
                            if (key is not null && key.Length > 0 && !IsCurrentOptional)
                            {
                                if (SetTypeCompoundItem.DataType is DataTypes.Array)
                                {
                                    result.ResultString.Append("\"" + key + "\": [\r\n");
                                }
                                else
                                if (SetTypeCompoundItem.DataType is not DataTypes.OptionalCompound)
                                {
                                    result.ResultString.Append("\"" + key + "\": {\r\n");
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
                            //    if (Last is CompoundJsonTreeViewItem lastCompletedCompoundItem && (lastCompletedCompoundItem.DataType is DataTypes.OptionalCompound || lastCompletedCompoundItem.DataType is DataTypes.NullableCompound || lastCompletedCompoundItem.DataType is DataTypes.Compound || lastCompletedCompoundItem.DataType is DataTypes.CustomCompound))
                            //    {
                            //        SetTypeCompoundItem.StartLineNumber = SetTypeCompoundItem.EndLineNumber = lastCompletedCompoundItem.EndLineNumber + 1;
                            //    }

                            //    result.ResultString.Append(new string(' ', layerCount * 2) + (dataMatch.Value == "array" ? "\"" + key + "\"" : ""));
                            //}
                            //result.ResultString.Append((dataMatch.Value == "array" ? ": " : "") + "[]");
                            #endregion

                            #region 处理各种数值提供器或结构模板
                            Match templateMatch = GetTemplateKey().Match(nodeList[i]);
                            if (key is not null && templateMatch.Success && templateMatch.Groups[1] is Group type && ValueProviderContextDictionary.TryGetValue(type.Value, out CompoundJsonTreeViewItem? currentCompoundItem) && currentCompoundItem is not null)
                            {
                                SetTypeCompoundItem.SwitchChildren = currentCompoundItem.SwitchChildren;
                                SetTypeCompoundItem.SwitchKey = currentCompoundItem.SwitchKey;
                                SetTypeCompoundItem.CompoundHead = currentCompoundItem.CompoundHead;
                                SetTypeCompoundItem.DataType = DataTypes.ValueProvider;
                                SetTypeCompoundItem.StartLineNumber = SetTypeCompoundItem.EndLineNumber = lineNumber;
                                SetTypeCompoundItem.EnumBoxVisibility = SetTypeCompoundItem.InputBoxVisibility = Visibility.Visible;
                                SetTypeCompoundItem.EnumItemsSource = currentCompoundItem.EnumItemsSource;
                                SetTypeCompoundItem.SelectedEnumItem = currentCompoundItem.EnumItemsSource.FirstOrDefault()!;
                                SetTypeCompoundItem.CurrentValueType = SetTypeCompoundItem.ValueTypeList[0];
                                result.ResultString.Append(new string(' ', SetTypeCompoundItem.LayerCount * 2) + "\"" + SetTypeCompoundItem.Key + "\": " + SetTypeCompoundItem.Value);
                            }
                            else//表示当前正在初始化值提供器
                            if (isProcessingTemplate && dataMatch.Groups[2] is Group initType)
                            {
                                SetTypeCompoundItem.EnumBoxVisibility = Visibility.Visible;
                                SetTypeCompoundItem.ValueProviderType = (ValueProviderTypes)Enum.Parse(typeof(ValueProviderTypes), initType.Value);
                                SetTypeCompoundItem.DataType = DataTypes.ValueProvider;
                            }
                            #endregion

                            #region 设置前后关系
                            if (Last is not null && !isProcessingTemplate && Last.LayerCount == layerCount)
                            {
                                Last.Next = SetTypeCompoundItem;
                                SetTypeCompoundItem.Last = Last;
                            }
                            #endregion

                            #region 处理递归
                            //获取下一行的*数量
                            int nextNodeIndex = nodeList.IndexOf(nodeList[i]) + 1;
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
                                        if (nodeList[nextNodeIndex].Contains('{'))
                                            subNodeList.Add(nodeList[nextNodeIndex]);
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
                                                        subNodeList.Add(nodeList[nextNodeIndex]);
                                                }
                                            }
                                            else
                                                break;
                                            nextLineStar = nextLineStarMatch.Groups[1].Value.Trim();
                                        }

                                        JsonTreeViewDataStructure subResult = new();
                                        if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.DataType is not DataTypes.OptionalCompound && subNodeList.Count > 0)
                                        {
                                            subResult = GetTreeViewItemResult(result, subNodeList, lineNumber + 1, layerCount + 1, item as CompoundJsonTreeViewItem, item, isProcessingTemplate, starCount);
                                        }

                                        i = nextNodeIndex - 1;

                                        if (subResult.Result.Count > 0 && subResult.Result[^1].Key.Length > 0)
                                        {
                                            result = subResult;
                                        }
                                        #endregion
                                    }
                                }

                            }
                            #endregion
                        }

                        #region 计算可缺省参数的默认值
                        if (((item.DefaultValue is null && item.SelectedEnumItem is null) || item is CompoundJsonTreeViewItem compoundJsonItem && compoundJsonItem.DataType is DataTypes.OptionalCompound) && ((Last is not null && Last.StartLineNumber == lineNumber) || (Last is CompoundJsonTreeViewItem lastCompoundItem && lastCompoundItem.EndLineNumber == lineNumber) || (Last is null && lineNumber == 2 && item is CompoundJsonTreeViewItem firstCompoundItem && firstCompoundItem.DataType is DataTypes.OptionalCompound)))
                        {
                            lineNumber--;
                            item.StartLineNumber = lineNumber;
                            if (item is CompoundJsonTreeViewItem optionalItem)
                            {
                                optionalItem.EndLineNumber = lineNumber;
                            }
                        }
                        #endregion

                        #region 赋予Plan属性以及判断是否需要为上一个复合节点收尾
                        item.Plan ??= plan;
                        if (Last is CompoundJsonTreeViewItem lastItem && (lastItem.DataType is DataTypes.Compound || lastItem.DataType is DataTypes.NullableCompound) && lastItem.LayerCount + 1 == starCount)
                        {
                            result.ResultString.Append('}');
                        }
                        #endregion

                        #endregion
                    }
                }
                #endregion

                #region 处理节点的追加与收尾工作
                if (IsNextOptionalNode && !IsExplanationNode && item.Key.Length > 0 && !IsCurrentOptional)
                    result.ResultString.Append(',');

                if (starCount > LastStarCount && Parent is not null && item.Key.Length > 0)
                {
                    Parent.Children.Add(item);
                }
                else
                if (item.Key.Length > 0)
                {
                    result.Result.Add(item);
                }

                if (item is CompoundJsonTreeViewItem compound && compound.DataType is not DataTypes.OptionalCompound && !IsCurrentOptional)
                {
                    if (compound.DataType is not DataTypes.Array && compound.DataType is not DataTypes.InnerArray)
                        result.ResultString.Append("\r\n" + new string(' ', (layerCount - 1) * 2) + '}');
                    else
                        result.ResultString.Append("\r\n" + new string(' ', (layerCount - 1) * 2) + ']');
                }
                #endregion

                #region 将当前节点保存为上一个节点
                //将当前节点保存为上一个节点
                Last = item;
                //保存当前的*号数量
                LastStarCount = starCount;
                #endregion
            }

            return result;
        }

        /// <summary>
        /// 获取指定行的关键字
        /// </summary>
        /// <param name="lineNumber"></param>
        /// <returns></returns>
        private string GetDesignateKeywordByLineNumber(int lineNumber)
        {
            if (WikiDesignateLine.TryGetValue(lineNumber, out string result))
                return result;
            return "";
        }
    }
}