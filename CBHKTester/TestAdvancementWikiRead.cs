using cbhk.CustomControls.JsonTreeViewComponents;
using cbhk.GeneralTools.TreeViewComponentsHelper;
using cbhk.Model.Common;
using cbhk.ViewModel.Generators;
using HtmlAgilityPack;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using static cbhk.CustomControls.JsonTreeViewComponents.Enums;
using cbhk.CustomControls;

namespace CBHKTester
{
    [TestClass]
    public partial class TestAdvancementWikiRead
    {
        #region Field
        public Dictionary<string, CompoundJsonTreeViewItem> ValueProviderContextDictionary { get; set; } = [];

        DimensionTypeViewModel plan = new(null,null);

        JsonTreeViewItemExtension jsonTool = new();

        [GeneratedRegex(@"^\:?\s+?\*+\s+?\{\{nbt\|(?<1>[a-z_]+)\|(?<2>[a-z_]+)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetNodeTypeAndKey();

        [GeneratedRegex(@"^\:?\s+?\*+\s+?(?<branch>\{\{nbt\|[a-z]+\}\})+(\{\{nbt\|(?<1>[a-z]+)\|(?<2>[a-z_]+)\}\})", RegexOptions.IgnoreCase)]
        private static partial Regex GetMultiTypeAndKeyOfNode();

        [GeneratedRegex(@"（可选，默认为\{\{cd\|[a-z_]+\}\}）", RegexOptions.IgnoreCase)]
        private static partial Regex GetOptionalDefaultStringValue();

        [GeneratedRegex(@"（可选，默认为\d+）", RegexOptions.IgnoreCase)]
        private static partial Regex GetOptionalDefaultNumberValue();

        [GeneratedRegex(@"（可选，默认为\{\{cd\|(true|false)\}\}）", RegexOptions.IgnoreCase)]
        private static partial Regex GetOptionalDefaultBoolValue();

        [GeneratedRegex(@"（可选）", RegexOptions.IgnoreCase)]
        private static partial Regex GetOptionalKey();

        [GeneratedRegex(@"\[\[方块标签\]\]", RegexOptions.IgnoreCase)]
        private static partial Regex GetBlcokTagValue();

        [GeneratedRegex(@"\[\[实体标签\]\]", RegexOptions.IgnoreCase)]
        private static partial Regex GetEntityTagValue();

        [GeneratedRegex(@"\{\{cd\|[a-z:_]+\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetEnumValue();

        [GeneratedRegex(@"====\s(minecraft\:[a-z_]+)\s====", RegexOptions.IgnoreCase)]
        private static partial Regex GetBoldKeywords();

        private string AdvancementWikiFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Advancement\Data\Rule\1.20.4.wiki";
        private Dictionary<int, string> WikiDesignateLine = [];
        private string[] ProgressClassList = ["treeview"];

        private Dictionary<string, List<JsonTreeViewItem>> CriteriaDataList = [];
        private Dictionary<string, List<JsonTreeViewItem>> AdvancementReferenceItemList = [];

        public ObservableCollection<JsonTreeViewItem> AdvancementTreeViewItemList = [];
        #endregion

        [TestMethod]
        public async Task AnalyzeHTMLData()
        {
            await Task.Run(async () =>
            {
                HtmlDocument doc = new()
                {
                    OptionFixNestedTags = true,
                    OptionAutoCloseOnEnd = true
                };
                string wikiData = File.ReadAllText(AdvancementWikiFilePath);
                string[] wikiLines = await File.ReadAllLinesAsync(AdvancementWikiFilePath);
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
                    JsonTreeViewDataStructure result = new();

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
                    JsonTreeViewDataStructure firstResultData = GetAdvancementItemList(new(), firstNodeContent, 2, 1);
                    result.Result.AddRange(firstResultData.Result);
                    result.ResultString.Append(firstResultData.ResultString);
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

                            JsonTreeViewDataStructure data = GetAdvancementItemList(new(), nodeList, 2, 1);

                            string currentKey = GetDesignateKeywordByLineNumber(line - 2);
                            if (currentKey.Length > 0)
                            {
                                CriteriaDataList.Add(currentKey, [.. data.Result]);
                            }
                            result.Result.AddRange(data.Result);
                            result.ResultString.Append(data.ResultString);
                        }
                    }
                    #endregion

                    AdvancementTreeViewItemList = result.Result;
                }
            });
        }

        private JsonTreeViewDataStructure GetAdvancementItemList(JsonTreeViewDataStructure result, List<string> nodeList, int lineNumber, int layerCount, CompoundJsonTreeViewItem? Parent = null, JsonTreeViewItem? Last = null, bool isProcessingTemplate = false, int LastStarCount = 1)
        {
            #region 处理起始字符串
            if (result.ResultString.Length == 0)
                result.ResultString.Append("{\r\n");
            #endregion

            // 遍历找到的 div 标签集合
            foreach (var node in nodeList)
            {
                #region 判断是否跳过本次处理
                if (node.Trim().Length == 0)
                {
                    continue;
                }

                int lastCurlyBracesIndex = node.LastIndexOf('}');

                if (lastCurlyBracesIndex > -1 && node[lastCurlyBracesIndex..].Trim() == "根标签")
                {
                    continue;
                }
                #endregion

                #region 声明当前节点、连接前后节点
                JsonTreeViewItem item = new()
                {
                    StartLineNumber = lineNumber
                };
                #endregion

                #region 计算当前行星号数量
                Match starMatch = Regex.Match(node, @"^\s+?\:?\s+?(\*+)");
                int starCount = starMatch.Value.Trim().Length;
                #endregion

                #region 处理值类型
                MatchCollection nodeTypeAndKeyInfoGroupList = GetNodeTypeAndKey().Matches(node);
                MatchCollection multiNodeTypeAndKeyInfoGroupList = GetMultiTypeAndKeyOfNode().Matches(node);

                if (multiNodeTypeAndKeyInfoGroupList.Count > 0 || nodeTypeAndKeyInfoGroupList.Count > 0)
                {
                    foreach (var dataType in nodeTypeAndKeyInfoGroupList.Cast<Match>())
                    {
                        #region 确定当前的节点数据类型
                        string key = dataType.Groups[2].Value;
                        bool IsSimpleItem = false;
                        switch (dataType.Value)
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
                                    if (dataType.Value == "string")
                                        item.DataType = DataTypes.String;
                                    break;
                                }
                        }

                        if (!IsSimpleItem)
                        {
                            if (!isProcessingTemplate)
                            {
                                Last = item.Last;
                                Last.Next = item.Next;
                            }
                            item = new CompoundJsonTreeViewItem(plan, jsonTool);
                            if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                            {
                                compoundJsonTreeViewItem.ValueTypeList.Add(new TextComboBoxItem()
                                {
                                    Text = dataType.Value
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
                                        if (dataType.Groups.Count > 1)
                                        {
                                            item.Key = key;
                                            item.BoolButtonVisibility = Visibility.Visible;

                                            Match boolMatch = GetOptionalDefaultBoolValue().Match(node);
                                            if (boolMatch is not null)
                                            {
                                                result.ResultString.Append(new string(' ', layerCount * 2));
                                                result.ResultString.Append("\"" + key.ToLower() + "\"");
                                                string defaultValue = boolMatch.Value;

                                                if (bool.TryParse(defaultValue, out bool boolValue))
                                                {
                                                    item.Value = item.DefaultValue = boolValue;
                                                    if (boolValue)
                                                        item.IsTrue = true;
                                                    else
                                                        item.IsFalse = true;
                                                    result.ResultString.Append(": " + boolValue.ToString().ToLower());
                                                }
                                            }
                                        }
                                        break;
                                    }
                                case DataTypes.Input:
                                case DataTypes.String:
                                    {
                                        if (dataType.Groups.Count > 1)
                                        {
                                            item.Key = key;
                                            item.InputBoxVisibility = Visibility.Visible;

                                            Match stringMatch = GetOptionalDefaultStringValue().Match(node);
                                            if (stringMatch is not null)
                                            {
                                                result.ResultString.Append(new string(' ', layerCount * 2));
                                                result.ResultString.Append("\"" + key.ToLower() + "\"");
                                                string defaultValue = stringMatch.Value;

                                                if (bool.TryParse(defaultValue, out bool boolValue))
                                                {
                                                    item.Value = item.DefaultValue = boolValue;
                                                    if (boolValue)
                                                        item.IsTrue = true;
                                                    else
                                                        item.IsFalse = true;
                                                    result.ResultString.Append(": " + boolValue.ToString().ToLower());
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
                                        Match numberMatch = GetOptionalDefaultNumberValue().Match(node);
                                        if (numberMatch is not null)
                                        {
                                            item.Key = key;
                                            item.InputBoxVisibility = Visibility.Visible;

                                            result.ResultString.Append(new string(' ', layerCount * 2));
                                            item.InputBoxVisibility = Visibility.Visible;
                                            result.ResultString.Append("\"" + key.ToLower() + "\"");
                                            string defaultValue = numberMatch.Value;
                                            item.Value = item.DefaultValue = defaultValue;
                                            if (item.DataType is not DataTypes.String)
                                                result.ResultString.Append(": " + defaultValue.ToString().ToLower());
                                            else
                                                result.ResultString.Append(": \"" + defaultValue.ToString().ToLower() + "\"");
                                        }
                                        break;
                                    }
                            }

                            item.StartLineNumber = lineNumber;
                            if (Last is not null && !isProcessingTemplate)
                            {
                                Last.Next = item;
                                item.Last = Last;
                            }
                        }
                        #endregion

                        #region 处理复合类型
                        if (item is CompoundJsonTreeViewItem SetTypeCompoundItem)
                        {
                            #region 处理枚举型数据
                            MatchCollection EnumCollection = GetEnumValue().Matches(node);
                            Match BlockTagMark = GetBlcokTagValue().Match(node);
                            Match EntityTagMark = GetEntityTagValue().Match(node);
                            Match DefaultValueMark = GetOptionalDefaultBoolValue().Match(node);

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
                                foreach (string blockTagMatch in JsonToJsonTreeViewItemConverter.BlockTagList)
                                {
                                    SetTypeCompoundItem.EnumItemsSource.Add(new TextComboBoxItem() { Text = blockTagMatch });
                                }
                            }
                            else
                            if (EntityTagMark.Index > 0)
                            {
                                foreach (string entityTagMatch in JsonToJsonTreeViewItemConverter.ItemTagList)
                                {
                                    SetTypeCompoundItem.EnumItemsSource.Add(new TextComboBoxItem() { Text = entityTagMatch });
                                }
                            }

                            #region 处理默认值
                            if (EnumCollection.Count > 0 || BlockTagMark.Index > 0 || EntityTagMark.Index > 0)
                            {
                                #region 控制显示隐藏、Key和前导空格
                                SetTypeCompoundItem.EnumBoxVisibility = Visibility.Visible;
                                SetTypeCompoundItem.Key = key;
                                result.ResultString.Append(new string(' ', layerCount * 2));
                                #endregion

                                #region 添加Key与默认值
                                result.ResultString.Append("\"" + key.ToLower() + "\"");
                                result.ResultString.Append(": \"" + SetTypeCompoundItem.EnumItemsSource[0].Text + "\"");
                                SetTypeCompoundItem.SelectedEnumItem = SetTypeCompoundItem.EnumItemsSource[0];
                                //if (DefaultValueMatch is not null)
                                //{
                                //    result.ResultString.Append("\"" + key.ToLower() + "\"");
                                //    string defaultValue = DefaultValueMatch.Value;

                                //    if (bool.TryParse(defaultValue, out bool boolValue))
                                //    {
                                //        SetTypeCompoundItem.DataType = DataTypes.Bool;
                                //        SetTypeCompoundItem.Value = boolValue;
                                //        if (boolValue)
                                //            SetTypeCompoundItem.IsTrue = true;
                                //        else
                                //            SetTypeCompoundItem.IsFalse = true;
                                //        result.ResultString.Append(": " + boolValue.ToString().ToLower());
                                //    }
                                //    else
                                //        if (int.TryParse(defaultValue, out int intValue))
                                //    {
                                //        SetTypeCompoundItem.DataType = DataTypes.Int;
                                //        SetTypeCompoundItem.Value = intValue;
                                //        result.ResultString.Append(": " + intValue);
                                //    }
                                //    else
                                //    {
                                //        SetTypeCompoundItem.DataType = DataTypes.String;
                                //        SetTypeCompoundItem.Value = "\"" + defaultValue + "\"";
                                //        result.ResultString.Append(": \"" + defaultValue + "\"");
                                //    }
                                //}
                                #endregion
                            }
                            #endregion

                            #endregion

                            #region 处理不同行为的复合型数据
                            //case "optionalCompound":
                            //case "nullableCompound":
                            //case "compound":
                            //case "customCompound":
                            if (dataType.Value == "compound")
                            {
                                if (dataType.Value == "nullableCompound" || dataType.Value == "optionalCompound")
                                {
                                    SetTypeCompoundItem.DataType = dataType.Value == "nullableCompound" ? DataTypes.NullableCompound : DataTypes.OptionalCompound;
                                }
                                else
                                if (dataType.Value == "customCompound")
                                {
                                    SetTypeCompoundItem.DataType = DataTypes.CustomCompound;
                                }
                                else
                                {
                                    SetTypeCompoundItem.DataType = DataTypes.Compound;
                                }
                            }

                            #region 计算当前数据类型、追加Key、前置追加空格
                            SetTypeCompoundItem.DefaultValue = "";
                            if (key is not null)
                                SetTypeCompoundItem.Key = key;
                            if (SetTypeCompoundItem.DataType is not DataTypes.OptionalCompound)
                            {
                                result.ResultString.Append(new string(' ', layerCount * 2));
                                SetTypeCompoundItem.StartLineNumber = SetTypeCompoundItem.EndLineNumber = lineNumber;
                                if (key.Length > 0)
                                {
                                    result.ResultString.Append("\"" + key + "\": {\r\n");
                                    SetTypeCompoundItem.LayerCount = layerCount;
                                }
                            }
                            SetTypeCompoundItem.CurrentValueType ??= SetTypeCompoundItem.ValueTypeList[0];
                            #endregion

                            #endregion

                            #region 处理数组与内联数据
                            if (key is not null)
                            {
                                SetTypeCompoundItem.AddElementButtonVisibility = Visibility.Visible;
                                SetTypeCompoundItem.DataType = DataTypes.Array;
                                if (dataType.Value == "innerArray")
                                    SetTypeCompoundItem.DataType = DataTypes.InnerArray;
                                SetTypeCompoundItem.Key = key;
                                SetTypeCompoundItem.StartLineNumber = SetTypeCompoundItem.EndLineNumber = lineNumber;
                                if (Last is CompoundJsonTreeViewItem lastCompletedCompoundItem && (lastCompletedCompoundItem.DataType is DataTypes.OptionalCompound || lastCompletedCompoundItem.DataType is DataTypes.NullableCompound || lastCompletedCompoundItem.DataType is DataTypes.Compound || lastCompletedCompoundItem.DataType is DataTypes.CustomCompound))
                                {
                                    SetTypeCompoundItem.StartLineNumber = SetTypeCompoundItem.EndLineNumber = lastCompletedCompoundItem.EndLineNumber + 1;
                                }

                                result.ResultString.Append(new string(' ', layerCount * 2) + (dataType.Value == "array" ? "\"" + key + "\"" : ""));
                            }
                            result.ResultString.Append((dataType.Value == "array" ? ": " : "") + "[]");
                            #endregion

                            #region 处理各种数值提供器或结构模板
                            //case "intProvider":
                            //case "floatProvider":
                            //case "heightProvider":
                            //case "verticalAnchor":
                            //case "blockPredicate":
                            //case "blockStateProvider":
                            if (key is not null && dataType.Groups[2] is Group type && ValueProviderContextDictionary.TryGetValue(type.Value, out CompoundJsonTreeViewItem currentCompoundItem))
                            {
                                SetTypeCompoundItem.SwitchChildren = currentCompoundItem.SwitchChildren;
                                SetTypeCompoundItem.SwitchKey = currentCompoundItem.SwitchKey;
                                SetTypeCompoundItem.CompoundHead = currentCompoundItem.CompoundHead;
                                SetTypeCompoundItem.DataType = DataTypes.ValueProvider;
                                SetTypeCompoundItem.Value = SetTypeCompoundItem.DefaultValue = "0";
                                SetTypeCompoundItem.StartLineNumber = SetTypeCompoundItem.EndLineNumber = lineNumber;
                                SetTypeCompoundItem.EnumBoxVisibility = SetTypeCompoundItem.InputBoxVisibility = Visibility.Visible;
                                SetTypeCompoundItem.EnumItemsSource = currentCompoundItem.EnumItemsSource;
                                SetTypeCompoundItem.SelectedEnumItem = currentCompoundItem.EnumItemsSource.FirstOrDefault();
                                SetTypeCompoundItem.Key = key;
                                SetTypeCompoundItem.CurrentValueType = SetTypeCompoundItem.ValueTypeList[0];
                                result.ResultString.Append(new string(' ', SetTypeCompoundItem.LayerCount * 2) + "\"" + SetTypeCompoundItem.Key + "\": " + SetTypeCompoundItem.Value);
                            }
                            else//表示当前正在初始化值提供器
                            if (isProcessingTemplate && dataType.Groups[2] is Group initType)
                            {
                                SetTypeCompoundItem.Key = key;
                                SetTypeCompoundItem.EnumBoxVisibility = Visibility.Visible;
                                SetTypeCompoundItem.ValueProviderType = (ValueProviderTypes)Enum.Parse(typeof(ValueProviderTypes), initType.Value);
                                SetTypeCompoundItem.DataType = DataTypes.ValueProvider;
                            }
                            #endregion

                            #region 设置前后关系和行号
                            if (Last is not null && !isProcessingTemplate)
                            {
                                Last.Next = SetTypeCompoundItem;
                                SetTypeCompoundItem.Last = Last;
                            }
                            #endregion
                        }
                        #endregion

                        #region 计算可缺省参数的默认值
                        if (((item.DefaultValue is null && item.SelectedEnumItem is null) || (item is CompoundJsonTreeViewItem compoundJsonItem && compoundJsonItem.DataType is DataTypes.OptionalCompound)) && ((Last is not null && Last.StartLineNumber == lineNumber) || (Last is CompoundJsonTreeViewItem lastCompoundItem && lastCompoundItem.EndLineNumber == lineNumber) || (Last is null && lineNumber == 2)))
                        {
                            lineNumber--;
                            item.StartLineNumber = lineNumber;
                            if (item is CompoundJsonTreeViewItem optionalItem)
                            {
                                optionalItem.EndLineNumber = lineNumber;
                            }
                        }
                        #endregion

                        item.Plan = plan;
                    }
                }
                #endregion

                if (starCount < LastStarCount)
                {

                }
                else
                    if (starCount == LastStarCount)
                {

                }
                else
                if (Parent is not null)
                {
                    Parent.Children.Add(item);
                }
                else
                    result.Result.Add(item);
                LastStarCount = starCount;
            }

            #region Json收尾后返回
            result.ResultString.Append("\r\n" + new string(' ', (layerCount - 1) * 2) + '}');
            return result;
            #endregion
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