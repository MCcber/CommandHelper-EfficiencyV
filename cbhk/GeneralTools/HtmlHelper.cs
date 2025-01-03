using cbhk.CustomControls;
using cbhk.CustomControls.JsonTreeViewComponents;
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
using static cbhk.Model.Common.Enums;

namespace cbhk.GeneralTools
{
    public partial class HtmlHelper(IContainerProvider container)
    {
        #region Field
        public DimensionTypeViewModel plan = null;

        public JsonTreeViewItemExtension jsonTool = null;

        private List<string> EnumKeyList = ["命名空间ID"];

        private string InheritDirectory = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Inherit";

        [GeneratedRegex(@"\[\[\#(?<1>[\u4e00-\u9fff]+)\|(?<2>[\u4e00-\u9fff]+)\]\]")]
        private static partial Regex GetContextKey();

        [GeneratedRegex(@"^\s*\s?\:?\s*\s?(\*+)")]
        private static partial Regex GetLineStarCount();

        [GeneratedRegex(@"\s*\s?\*+\s*\s?{{[nN]bt\sinherit(?<1>[a-z_/|=*]+)}}\s*\s?", RegexOptions.IgnoreCase)]
        private static partial Regex GetInheritString();

        [GeneratedRegex(@"{{interval\|left=(?<1>\d+)\|right=(?<2>\d+)}}", RegexOptions.IgnoreCase)]
        private static partial Regex GetNumberRange();

        [GeneratedRegex(@"可选", RegexOptions.IgnoreCase)]
        private static partial Regex GetOptionalKey();

        [GeneratedRegex(@"可以为空", RegexOptions.IgnoreCase)]
        private static partial Regex GetIsCanNullableKey();

        [GeneratedRegex(@"默认为\{\{cd\|[a-z_]+\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultStringValue();

        [GeneratedRegex(@"默认为(\d)+", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultNumberValue();

        [GeneratedRegex(@"默认为\{\{cd\|(true|false)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultBoolValue();

        [GeneratedRegex(@"(?<=默认为<code>)[a-z_]+(?=</code>)", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultEnumValue();

        [GeneratedRegex(@"(?<=<code>)[a-z_]+(?=</code>)")]
        private static partial Regex GetEnumValueMode1();

        [GeneratedRegex(@"\{\{cd\|[a-z:_]+\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetEnumValueMode2();

        [GeneratedRegex(@"\[\[方块标签\]\]", RegexOptions.IgnoreCase)]
        private static partial Regex GetBlcokTagValue();

        [GeneratedRegex(@"\[\[实体标签\]\]", RegexOptions.IgnoreCase)]
        private static partial Regex GetEntityTagValue();

        [GeneratedRegex(@"\[\[物品标签\]\]", RegexOptions.IgnoreCase)]
        private static partial Regex GetItemTagValue();

        [GeneratedRegex(@"\s*\s+?=+\s*\s?([\u4e00-\u9fffa-z_]+)\s*\s?=+\s*\s+?", RegexOptions.IgnoreCase)]
        private static partial Regex GetContextFileMarker();

        [GeneratedRegex(@"====\s*\s?(minecraft\:[a-z_]+)\s*\s?====", RegexOptions.IgnoreCase)]
        private static partial Regex GetEnumTypeKeywords();

        private string AdvancementWikiFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\1.20.4.wiki";
        public List<string> ProgressClassList = ["treeview"];

        private IContainerProvider _container = container;
        #endregion

        /// <summary>
        /// 把VSC排版后的格式重新整理(有些应该归为一行的被排版分割为了多行)
        /// </summary>
        /// <param name="target">需要被排版的链表</param>
        /// <returns></returns>
        private List<string> TypeSetting(List<string> target)
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

        public JsonTreeViewDataStructure AnalyzeHTMLData(string directoryPath)
        {
            JsonTreeViewDataStructure Result = new();
            HtmlDocument htmlDocument = new();

            #region 优先解析继承文件列表
            string[] inheritItemArray = Directory.GetFileSystemEntries(InheritDirectory);
            for (int i = 0; i < inheritItemArray.Length; i++)
            {
                if (Path.GetExtension(inheritItemArray[i]) != ".wiki")
                {
                    continue;
                }
                if (File.Exists(inheritItemArray[i]))
                {
                    int position = inheritItemArray[i].LastIndexOf("Inherit");
                    htmlDocument.LoadHtml(File.ReadAllText(inheritItemArray[i]));
                    if (htmlDocument.DocumentNode.ChildNodes.Count == 0)
                    {
                        continue;
                    }
                    List<HtmlNode> treeviewDivs = [.. htmlDocument.DocumentNode.SelectNodes("//div[@class='treeview']")];
                    for (int j = 0; j < treeviewDivs.Count; j++)
                    {
                        List<string> nodeContent = [.. treeviewDivs[j].InnerHtml.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)];
                        string inheritKey = Path.GetDirectoryName(inheritItemArray[i][position..]).ToLower() + "\\" + Path.GetFileNameWithoutExtension(inheritItemArray[i][position..]);
                        nodeContent = TypeSetting(nodeContent);
                        JsonTreeViewDataStructure inheritResultData = GetTreeViewItemResult(new(), nodeContent, 2, 1, false);
                        plan.CurrentDependencyItemList.Add(inheritKey, [.. inheritResultData.Result]);
                    }
                }
            }
            #endregion

            #region 处理主文件与依赖文件
            foreach (var file in Directory.GetFiles(directoryPath))
            {
                string wikiData = File.ReadAllText(file);
                string[] wikiLines = File.ReadAllLines(file);

                #region 处理非入口文件
                bool IsNotMainFile = false;
                string currentReferenceKey = "";
                if (Path.GetFileNameWithoutExtension(file) != "main")
                {
                    IsNotMainFile = true;
                    string mainKey = wikiLines[0].Replace("=", "").Trim();
                    string subKey = "";
                    if (wikiLines[1].Contains('='))
                    {
                        subKey = wikiLines[1].Replace("=", "").Trim();
                    }
                    currentReferenceKey = "#" + mainKey + "|" + subKey;
                    plan.CurrentDependencyItemList.Add(currentReferenceKey, []);
                }
                #endregion

                htmlDocument.LoadHtml(wikiData);
                List<HtmlNode> treeviewDivs = [.. htmlDocument.DocumentNode.SelectNodes("//div[@class='treeview']")];

                if (treeviewDivs is not null)
                {
                    #region 直接覆盖InnerHtml属性会导致内容错误，所以这里直接用链表来提取正确的结构数据
                    List<string> nodeContent = [.. treeviewDivs[0].InnerHtml.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)];
                    nodeContent = TypeSetting(nodeContent);
                    #endregion

                    #region 执行解析、分析是否需要被添加为依赖项
                    JsonTreeViewDataStructure resultData = GetTreeViewItemResult(new(), nodeContent, 2, 1);
                    if (IsNotMainFile)
                    {
                        Match mainContextFileMarker = GetContextFileMarker().Match(wikiLines[0]);
                        Match subContextFileMarker = GetContextFileMarker().Match(wikiLines[1]);
                        if (currentReferenceKey == "#" + mainContextFileMarker.Value + "|" + subContextFileMarker.Value && plan.CurrentDependencyItemList.TryGetValue(currentReferenceKey, out List<JsonTreeViewItem> list))
                        {
                            list.AddRange(resultData.Result);
                        }
                    }
                    else
                    {
                        Result.Result.AddRange(resultData.Result);
                        Result.ResultString.Append(resultData.ResultString);
                    }
                    #endregion
                }
            }
            #endregion

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
        public JsonTreeViewDataStructure GetTreeViewItemResult(JsonTreeViewDataStructure result, List<string> nodeList, int lineNumber, int layerCount, bool NonReferenceSet = true, CompoundJsonTreeViewItem Parent = null, JsonTreeViewItem Previous = null, int PreviousStarCount = 1)
        {
            //遍历找到的 div 标签内容集合
            for (int i = 0; i < nodeList.Count; i++)
            {
                #region 字段
                string currentNodeDataType = "";
                string currentNodeKey = "";
                string RemoveIrrelevantString = nodeList[i].Replace("}}{{nbt", "");
                int nbtFeatureStartIndex = RemoveIrrelevantString.IndexOf("{{");
                int nbtFeatureEndIndex = RemoveIrrelevantString.IndexOf("}}");
                List<string> NBTFeatureList = [.. RemoveIrrelevantString[(nbtFeatureStartIndex + 2)..nbtFeatureEndIndex].Split('|')];
                NBTFeatureList.RemoveAt(0);
                bool IsSimpleItem = false;
                bool IsExplanationNode = NBTFeatureList.Count > 1;
                bool IsCurrentOptionalNode = GetOptionalKey().Match(RemoveIrrelevantString).Success;
                bool IsHaveNextNode = i < nodeList.Count - 1 && NBTFeatureList.Count > 2;
                bool IsNextOptionalNode = false;
                #endregion

                #region 去除用于Wiki页面显示的一些UI标记
                for (int j = NBTFeatureList.Count - 1; j >= 0; j--)
                {
                    if (NBTFeatureList[j].Contains('='))
                    {
                        NBTFeatureList.RemoveAt(j);
                        j++;
                    }
                }
                #endregion

                #region 判断是否跳过本次处理
                if (NBTFeatureList.Count < 2)
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
                    StartLineNumber = lineNumber,
                    LayerCount = layerCount
                };
                #endregion

                #region 处理两大值类型

                #region 获取当前节点的数据类型和键
                currentNodeDataType = NBTFeatureList[^2];
                if (!NBTFeatureList[^1].Contains('\'') && !NBTFeatureList[^1].Contains('<') && !NBTFeatureList[^1].Contains('>'))
                {
                    currentNodeKey = NBTFeatureList[^1];
                }
                #endregion

                #region 确认是否能够进入解析流程
                bool IsCanBeAnalyzed = currentNodeDataType.Length > 0 && currentNodeKey.Length > 0;
                #endregion

                if (IsCanBeAnalyzed)
                {
                    #region 判断是否为复合节点
                    if(currentNodeDataType == "compound" || currentNodeDataType == "list" || currentNodeDataType == "array")
                    {
                        NonReferenceSet = false;
                        item = new CompoundJsonTreeViewItem(plan, jsonTool);
                    }
                    #endregion

                    #region 判断是否捕获多组信息
                    if (NBTFeatureList is not null && NBTFeatureList.Count > 2 && currentNodeDataType.Length > 0 && currentNodeKey.Length > 0)
                    {
                        if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                        {
                            for (int j = 0; j < NBTFeatureList.Count - 2; j++)
                            {
                                compoundJsonTreeViewItem.ValueTypeList.Add(new TextComboBoxItem()
                                {
                                    Text = NBTFeatureList[j][0].ToString().ToUpper() + NBTFeatureList[j][1..]
                                });
                            }
                        }
                    }
                    else
                    {
                        IsSimpleItem = true;
                    }
                    #endregion

                    #region 处理值类型
                    if (IsSimpleItem)
                    {
                        switch (currentNodeDataType)
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
                                        bool parseResult = Enum.TryParse(typeof(DataTypes), currentNodeDataType[0].ToString().ToUpper() + currentNodeDataType[1..], out dataTypes);
                                        if (parseResult)
                                        {
                                            item.DataType = (DataTypes)dataTypes;
                                        }
                                    }
                                    break;
                                }
                        }

                        switch (item.DataType)
                        {
                            case DataTypes.Bool:
                                {
                                    item.Key = currentNodeKey;
                                    item.DisplayText = currentNodeKey[0].ToString().ToUpper() + currentNodeKey[1..];
                                    item.BoolButtonVisibility = Visibility.Visible;

                                    Match boolMatch = GetDefaultBoolValue().Match(nodeList[i]);
                                    if (boolMatch is not null)
                                    {
                                        if (!IsCurrentOptionalNode && NonReferenceSet)
                                        {
                                            result.ResultString.Append(new string(' ', layerCount * 2));
                                            result.ResultString.Append("\"" + currentNodeKey.ToLower() + "\"");
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
                                    break;
                                }
                            case DataTypes.Input:
                            case DataTypes.String:
                                {
                                    item.Key = currentNodeKey;
                                    item.DisplayText = currentNodeKey[0].ToString().ToUpper() + currentNodeKey[1..];
                                    item.InputBoxVisibility = Visibility.Visible;

                                    Match stringMatch = GetDefaultStringValue().Match(nodeList[i]);
                                    if (stringMatch is not null)
                                    {
                                        if (!IsCurrentOptionalNode && NonReferenceSet)
                                        {
                                            result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + currentNodeKey.ToLower() + "\"");
                                        }

                                        if (stringMatch.Groups.Count > 1)
                                        {
                                            item.Value = item.DefaultValue = "\"" + stringMatch.Groups[1].Value + "\"";
                                        }

                                        if (!IsCurrentOptionalNode && stringMatch.Groups.Count > 1 && NonReferenceSet)
                                        {
                                            result.ResultString.Append(": \"" + stringMatch.Groups[1].Value.ToLower() + "\"");
                                        }
                                        else
                                        if (NonReferenceSet)
                                        {
                                            result.ResultString.Append(": \"\"");
                                        }
                                    }
                                    break;
                                }
                            case DataTypes.Enum:
                                {
                                    item.Key = currentNodeKey;
                                    item.DisplayText = currentNodeKey[0].ToString().ToUpper() + currentNodeKey[1..];
                                    item.EnumBoxVisibility = Visibility.Visible;
                                    #region 抓取数据
                                    MatchCollection EnumCollectionMode1 = GetEnumValueMode1().Matches(nodeList[i]);
                                    MatchCollection EnumCollectionMode2 = GetEnumValueMode2().Matches(nodeList[i]);
                                    Match DefaultEnumValueMark = GetDefaultEnumValue().Match(nodeList[i]);
                                    Match BlockTagMark = GetBlcokTagValue().Match(nodeList[i]);
                                    Match EntityTagMark = GetEntityTagValue().Match(nodeList[i]);
                                    Match ItemTagMark = GetItemTagValue().Match(nodeList[i]);

                                    if(DefaultEnumValueMark.Success)
                                    {
                                        item.DefaultValue = item.Value = "\"" + DefaultEnumValueMark.Value + "\"";
                                    }
                                    #endregion
                                    #region 添加枚举成员
                                    if ((nodeList[i].Contains("可以是") || nodeList[i].Contains("可以为")) && EnumCollectionMode1.Count > 0)
                                    {
                                        foreach (Match enumMatch in EnumCollectionMode1)
                                        {
                                            item.EnumItemsSource.Add(new TextComboBoxItem() { Text = enumMatch.Value });
                                        }
                                        foreach (Match enumMatch in EnumCollectionMode2)
                                        {
                                            item.EnumItemsSource.Add(new TextComboBoxItem() { Text = enumMatch.Value });
                                        }
                                    }
                                    else
                                    if (BlockTagMark.Index > 0)
                                    {
                                        item.EnumItemsSource.Add(new TextComboBoxItem() { Text = "block" });
                                        //foreach (string blockName in blockService.GetAllBlockName())
                                        //{
                                        //    item.EnumItemsSource.Add(new TextComboBoxItem() { Text = blockName });
                                        //}
                                    }
                                    else
                                    if (EntityTagMark.Index > 0)
                                    {
                                        item.EnumItemsSource.Add(new TextComboBoxItem() { Text = "entity" });
                                        //foreach (string entityName in entityService.GetAllEntityName())
                                        //{
                                        //    item.EnumItemsSource.Add(new TextComboBoxItem() { Text = entityName });
                                        //}
                                    }
                                    if(ItemTagMark.Index > 0)
                                    {
                                        item.EnumItemsSource.Add(new TextComboBoxItem() { Text = "item" });
                                        //foreach (string itemName in entityService.GetAllEntityName())
                                        //{
                                        //    item.EnumItemsSource.Add(new TextComboBoxItem() { Text = itemName });
                                        //}
                                    }
                                    #endregion
                                    #region 处理Key与外观
                                    if (EnumCollectionMode1.Count > 0 || BlockTagMark.Index > 0 || EntityTagMark.Index > 0)
                                    {
                                        item.DisplayText = currentNodeKey[0].ToString().ToUpper() + currentNodeKey[1..];
                                        item.DataType = DataTypes.Enum;
                                        item.EnumBoxVisibility = Visibility.Visible;
                                        item.SelectedEnumItem = item.EnumItemsSource[0];

                                        if (NonReferenceSet)
                                        {
                                            result.ResultString.Append(new string(' ', layerCount * 2) +
                                                "\"" + currentNodeKey.ToLower() + "\"" +
                                                ": \"" + item.EnumItemsSource[0].Text + "\"");
                                        }
                                    }
                                    #endregion

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
                                    item.Key = currentNodeKey;
                                    item.DisplayText = currentNodeKey[0].ToString().ToUpper() + currentNodeKey[1..];
                                    item.InputBoxVisibility = Visibility.Visible;

                                    if (!IsCurrentOptionalNode && NonReferenceSet)
                                    {
                                        result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + currentNodeKey.ToLower() + "\"");
                                    }

                                    if (numberMatch is not null)
                                    {
                                        if (decimal.TryParse(numberMatch.Groups[1].Value, out decimal defaultDecimalValue))
                                        {
                                            item.Value = item.DefaultValue = defaultDecimalValue;
                                        }

                                        if (!IsCurrentOptionalNode && numberMatch.Groups.Count > 1 && NonReferenceSet)
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
                            CurrentCompoundItem.Key = currentNodeKey;
                        }

                        #region 处理不同行为的复合型数据

                        #region 确定数据类型
                        CurrentCompoundItem.AddElementButtonVisibility = Visibility.Visible;
                        CurrentCompoundItem.ElementButtonTip = "添加";
                        if (currentNodeDataType == "compound")
                        {
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

                            if (currentNodeKey.Contains('\''))
                            {
                                CurrentCompoundItem.DataType = DataTypes.CustomCompound;
                            }
                        }
                        else
                        if (currentNodeDataType == "list")
                        {
                            CurrentCompoundItem.DataType = DataTypes.List;
                        }
                        else//数组会有数据类型的区分，所以这里用包含而不是等于
                        if (currentNodeDataType.Contains("array"))
                        {
                            CurrentCompoundItem.DataType = DataTypes.Array;
                        }
                        else
                        if (currentNodeDataType == "any")
                        {
                            Parent.EnumBoxVisibility = Visibility.Visible;
                            CurrentCompoundItem.DataType = DataTypes.Any;
                        }
                        #endregion

                        #region 更新非可选复合型节点的文本值与层数
                        if (currentNodeKey.Length > 0)
                        {
                            CurrentCompoundItem.DisplayText = currentNodeKey[0].ToString().ToUpper() + currentNodeKey[1..];
                        }
                        CurrentCompoundItem.StartLineNumber = CurrentCompoundItem.EndLineNumber = lineNumber;
                        CurrentCompoundItem.LayerCount = layerCount;
                        if (CurrentCompoundItem.ValueTypeList.Count > 0)
                        {
                            CurrentCompoundItem.CurrentValueType ??= CurrentCompoundItem.ValueTypeList[0];
                        }
                        if (currentNodeKey is not null && currentNodeKey.Length > 0 && !IsCurrentOptionalNode)
                        {
                            if (CurrentCompoundItem.DataType is DataTypes.Array || CurrentCompoundItem.DataType is DataTypes.List && NonReferenceSet)
                            {
                                result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + currentNodeKey + "\": []");
                            }
                            else
                            if (CurrentCompoundItem.DataType is not DataTypes.OptionalCompound && NonReferenceSet)
                            {
                                result.ResultString.Append(new string(' ', layerCount * 2) + "\"" + currentNodeKey + "\": {");
                            }
                        }
                        #endregion

                        #endregion

                        #region 处理所需上下文数据
                        Match contextKeyMatch = GetContextKey().Match(nodeList[i]);
                        if (contextKeyMatch is not null && contextKeyMatch.Groups.Count > 2 &&
                            plan is not null && plan.CurrentDependencyItemList.Count > 0)
                        {
                            result.HaveReferenceContent = true;
                            string currentKey = "";
                            if (contextKeyMatch.Groups[2].Value == "下文")
                            {
                                currentKey = "#" + contextKeyMatch.Groups[1].Value;
                            }
                            else
                            {
                                currentKey = "#" + contextKeyMatch.Groups[1].Value + "|" + contextKeyMatch.Groups[2].Value;
                            }
                            if (plan.CurrentDependencyItemList.TryGetValue(currentKey, out List<JsonTreeViewItem> targetDependencyList))
                            {
                                //判断搜索到的字典链表中的根节点的Key是否包含需要被替代的部分，有则开启当前节点的文本框
                                if (targetDependencyList[0].Key.Contains('<') &&
                                    targetDependencyList[0].Key.Contains('\''))
                                {
                                    CurrentCompoundItem.InputBoxVisibility = Visibility.Visible;
                                }
                            }
                        }
                        #endregion

                        #region 处理继承信息
                        Match inheritMatch = GetInheritString().Match(nodeList[i]);
                        string targetInherit = "Inherit\\";
                        result.HaveReferenceContent |= inheritMatch.Groups.Count > 1;

                        if (inheritMatch.Groups[1].Value.Contains('*'))
                        {
                            targetInherit += inheritMatch.Groups[1].Value.Split('|')[0].Replace("/","\\");
                        }
                        else
                        {
                            targetInherit += inheritMatch.Groups[1].Value.Replace("|","\\").Replace("/", "\\");
                        }
                        if (currentNodeKey is not null && inheritMatch.Success && plan.CurrentDependencyItemList.TryGetValue(targetInherit, out List<JsonTreeViewItem> templateCompoundItem) && templateCompoundItem is not null)
                        {
                            if (templateCompoundItem[0].Key.Contains('\'') && templateCompoundItem[0].Key.Contains('^'))
                            {
                                CurrentCompoundItem.InputBoxVisibility = Visibility.Visible;
                            }
                            if (CurrentCompoundItem.DataType is not DataTypes.OptionalCompound && NonReferenceSet)
                            {
                                result.ResultString.Append(new string(' ', CurrentCompoundItem.LayerCount * 2) + "\"" + currentNodeKey + "\": {");
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
                                    if (CurrentCompoundItem.DataType is DataTypes.Compound || CurrentCompoundItem.DataType is DataTypes.Array || CurrentCompoundItem.DataType is DataTypes.List)
                                    {
                                        JsonTreeViewDataStructure subResult = GetTreeViewItemResult(new(), currentSubChildren, lineNumber, layerCount + 1, true, CurrentCompoundItem);

                                        if (subResult.ResultString.Length > 0 && NonReferenceSet && !subResult.HaveReferenceContent)
                                        {
                                            result.ResultString.Append("\r\n" + subResult.ResultString + "\r\n" + new string(' ', layerCount * 2) + "}\r\n");
                                        }
                                        else
                                        {
                                            if (CurrentCompoundItem.Children.Count == 0)
                                            {
                                                CurrentCompoundItem.ChildrenStringList = currentSubChildren;
                                            }
                                            if (NonReferenceSet)
                                            {
                                                result.ResultString.Append('}');
                                            }
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
                if (i < nodeList.Count - 1 && !result.ResultString.ToString().TrimEnd(['\r', '\n']).EndsWith(',') && !IsNextOptionalNode && IsHaveNextNode && NonReferenceSet)
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
            if (result.Result.Count > 0 && result.Result[^1] is CompoundJsonTreeViewItem compound && compound.DataType is DataTypes.Compound && NonReferenceSet)
            {
                result.ResultString.Append('}');
            }
            #endregion

            return result;
        }
    }
}