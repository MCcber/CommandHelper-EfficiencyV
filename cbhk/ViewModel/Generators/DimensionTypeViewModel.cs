using cbhk.CustomControls.JsonTreeViewComponents;
using cbhk.GeneralTools.TreeViewComponentsHelper;
using CommunityToolkit.Mvvm.ComponentModel;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Windows;
using System.Xml;
using System.Threading.Tasks;
using cbhk.CustomControls.Interfaces;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using ICSharpCode.AvalonEdit.Document;
using static cbhk.CustomControls.JsonTreeViewComponents.Enums;
using Prism.Ioc;
using cbhk.View;
using HtmlAgilityPack;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using cbhk.Model.Common;
using System.Linq;

namespace cbhk.Generators.DimensionTypeGenerator
{
    public partial class DimensionTypeViewModel : ObservableObject, ICustomWorldUnifiedPlan
    {
        #region Field
        private Window home = null;
        private string initRuleFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\DimensionType\Data\Rules\testForCompound.json";
        private string configDirectoryPath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\DimensionType\Data\Rules";
        private string valueProviderFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\ValueProviderStructure.json";
        public TextEditor textEditor = null;
        private FoldingManager foldingManager = null;
        private IContainerProvider _container;
        private string AdvancementWikiFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Advancement\Data\Rule\1.20.4.wiki";
        JsonTreeViewItemExtension jsonTool = new();
        private Dictionary<int,string> WikiDesignateLine = [];
        private string[] ProgressClassList = ["treeview"];
        private Dictionary<string, List<JsonTreeViewItem>> CriteriaDataList = [];
        private Dictionary<string, List<JsonTreeViewItem>> AdvancementReferenceItemList = [];

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
        #endregion

        #region Property
        [ObservableProperty]
        public string _dimensionTypeItemString = "";

        public Dictionary<string, JsonTreeViewItem> KeyValueContextDictionary { get; set; } = [];
        public Dictionary<string, CompoundJsonTreeViewItem> ValueProviderContextDictionary { get; set; } = [];
        [ObservableProperty]
        public ObservableCollection<JsonTreeViewItem> _advancementTreeViewItemList = [];
        #endregion

        public DimensionTypeViewModel(IContainerProvider container, MainView mainView)
        {
            _container = container;
            home = mainView;
        }

        public async void Home_Loaded(object sender, RoutedEventArgs e)
        {
            await AnalyzeHTMLData();
        }

        private async Task AnalyzeHTMLData()
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

                    JsonTreeViewDataStructure firstResultData = GetAdvancementItemList(new(), firstNodeContent, 2, 1, null);
                    result.Result.AddRange(firstResultData.Result);
                    result.ResultString.Append(firstResultData.ResultString);
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

                            JsonTreeViewDataStructure data = GetAdvancementItemList(new(), nodeList, 2, 1, null);

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

        private JsonTreeViewDataStructure GetAdvancementItemList(JsonTreeViewDataStructure result, List<string> nodeList, int lineNumber, int layerCount,CompoundJsonTreeViewItem Parent, JsonTreeViewItem Last = null, bool isProcessingTemplate = false, int LastStarCount = 1)
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
                Match starMatch = Regex.Match(node, @"^\s+?(\*+)");
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
                            item = new CompoundJsonTreeViewItem(this, jsonTool);
                            if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                            {
                                compoundJsonTreeViewItem.ValueTypeList.Add(new CustomControls.TextComboBoxItem()
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
                                    SetTypeCompoundItem.EnumItemsSource.Add(new CustomControls.TextComboBoxItem() { Text = enumMatch.Value });
                                }
                            }
                            else
                            if (BlockTagMark.Index > 0)
                            {
                                foreach (string blockTagMatch in JsonToJsonTreeViewItemConverter.BlockTagList)
                                {
                                    SetTypeCompoundItem.EnumItemsSource.Add(new CustomControls.TextComboBoxItem() { Text = blockTagMatch });
                                }
                            }
                            else
                            if (EntityTagMark.Index > 0)
                            {
                                foreach (string entityTagMatch in JsonToJsonTreeViewItemConverter.ItemTagList)
                                {
                                    SetTypeCompoundItem.EnumItemsSource.Add(new CustomControls.TextComboBoxItem() { Text = entityTagMatch });
                                }
                            }

                            #region 处理默认值
                            if(EnumCollection.Count > 0 || BlockTagMark.Index > 0 || EntityTagMark.Index > 0)
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
                            if(dataType.Value == "compound")
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

                        item.Plan = this;
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

        /// <summary>
        /// 关闭窗体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CommonWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ValueProviderContextDictionary.Clear();
        }

        /// <summary>
        /// 安装大纲、应用高亮规则
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void TextEditor_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(async () =>
            {
                textEditor = sender as TextEditor;
                JsonToJsonTreeViewItemConverter.textEditor = textEditor;
                JsonToJsonTreeViewItemConverter.plan = this;
                //生成值提供器字典
                //ValueProviderContextDictionary = TreeViewRuleReader.LoadValueProviderStructure(valueProviderFilePath);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var item in Directory.GetFiles(configDirectoryPath))
                    {
                        string fileName = Path.GetFileNameWithoutExtension(item);
                        if (File.Exists(item) && fileName == "BlockTag" || fileName == "ItemTag")
                        {
                            string data = File.ReadAllText(item);
                            JArray jArray = JArray.Parse(data);
                            if (fileName == "BlockTag")
                                foreach (var tag in jArray)
                                {
                                    JsonToJsonTreeViewItemConverter.BlockTagList.Add(tag.ToString());
                                }
                            else
                                foreach (var tag in jArray)
                                {
                                    JsonToJsonTreeViewItemConverter.ItemTagList.Add(tag.ToString());
                                }
                        }
                    }
                    DimensionTypeItemString = File.ReadAllText(initRuleFilePath);
                    textEditor.Text = JsonToJsonTreeViewItemConverter.CurrentData.ResultString.ToString();

                    foreach (var item in JsonToJsonTreeViewItemConverter.CurrentData.Result)
                    {
                        item.JsonItemTool = new JsonTreeViewItemExtension();
                        item.JsonItemTool.SetDocumentLineByLineNumber(item, textEditor);
                        //KeyValueContextDictionary.Add(item.Path, item);
                    }

                    //为代码编辑器安装大纲管理器
                    foldingManager = FoldingManager.Install(textEditor.TextArea);
                    XshdSyntaxDefinition xshdSyntaxDefinition = new();
                    xshdSyntaxDefinition = HighlightingLoader.LoadXshd(new XmlTextReader(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\json.xshd"));
                    IHighlightingDefinition jsonHighlighting = HighlightingLoader.Load(xshdSyntaxDefinition, HighlightingManager.Instance);
                    textEditor.SyntaxHighlighting = jsonHighlighting;
                });
            });
        }

        /// <summary>
        /// 失焦后执行数据同步
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TextEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            string currentText = textEditor.Text;
            Task.Run(() =>
            {
                try
                {
                    JsonTextReader jsonTextReader = new(new StringReader(currentText));
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
                }
                catch (JsonReaderException exception)
                {
                    int lineNumber = exception.LineNumber;
                    int linPosition = exception.LinePosition;
                }
            });
        }

        public async Task<JsonTreeViewItem> FindNodeBySpecifyingPath(string path)
        {
            JsonTreeViewItem result = null;
            CancellationToken cancellationToken = new();
            ParallelOptions options = new()
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };
            await Parallel.ForEachAsync(KeyValueContextDictionary.Values, options, (context, cancellationToken) =>
            {
                if (context.Path == path)
                {
                    result = context;
                }
                return new ValueTask();
            });
            return result;
        }

        public bool VerifyCorrectnessLayerByLayer(JsonTreeViewItem currentItem)
        {
            bool result = false;
            return result;
        }

        public void UpdateNullValueBySpecifyingInterval(int endOffset, string newValue = "\r\n")
        {
            textEditor.Document.Insert(endOffset, newValue);
        }

        public void UpdateValueBySpecifyingInterval(JsonTreeViewItem item, ReplaceType replaceType, string newValue = "", bool markValue = true)
        {
            #region 初始化
            DocumentLine startDocumentLine = null;
            DocumentLine endDocumentLine = null;
            int offset = 0, length = 0;

            if (item.StartLine is not null)
                startDocumentLine = item.StartLine;

            string startLineText = textEditor.Document.GetText(startDocumentLine);
            int originLength = startLineText.Length;
            int noSpaceLength = startLineText.TrimStart().Length;
            int spaceLength = originLength - noSpaceLength;
            int currentIndex = -1;
            if (item.Parent is not null)
            {
                currentIndex = item.Parent.Children.IndexOf(item);
            }
            #endregion

            #region 处理复合型跟值类型的起始索引与长度
            if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
            {
                if (compoundJsonTreeViewItem.EndLine is not null)
                    endDocumentLine = compoundJsonTreeViewItem.EndLine;

                switch (replaceType)
                {
                    case ReplaceType.Direct:
                        {
                            offset = item.StartLine.Offset;
                            if (item is CompoundJsonTreeViewItem compound)
                            {
                                length = compound.EndLine.EndOffset - offset;
                            }
                            else
                            {
                                length = item.StartLine.EndOffset - offset;
                            }
                            break;
                        }
                    case ReplaceType.AddElement:
                        {
                            break;
                        }
                    case ReplaceType.RemoveElement:
                        {
                            break;
                        }
                    case ReplaceType.AddArrayElement:
                        {
                            if (compoundJsonTreeViewItem.DataType is DataTypes.Array)
                            {
                                if (compoundJsonTreeViewItem.Children.Count == 0 || markValue)
                                {
                                    int openSquareIndex = startLineText.IndexOf('[') + 1;
                                    offset = startDocumentLine.Offset + openSquareIndex;
                                    length = 0;
                                }
                                else
                                {
                                    JsonTreeViewItem firstElement = compoundJsonTreeViewItem.Children[^2];
                                    if (firstElement is CompoundJsonTreeViewItem firstCompound)
                                    {
                                        offset = firstCompound.EndLine.EndOffset;
                                        length = 0;
                                    }
                                }
                            }
                            break;
                        }
                    case ReplaceType.RemoveArrayElement:
                        {
                            CompoundJsonTreeViewItem currentItem = item as CompoundJsonTreeViewItem;
                            if (currentIndex > 0)
                                offset = currentItem.StartLine.PreviousLine.EndOffset - (currentItem.Next is null ? 1 : 0);
                            if (currentIndex == 0)
                                offset = currentItem.Parent.StartLine.EndOffset;
                            length = currentItem.Next is null && item.Parent.Children.Count == 2 ? currentItem.EndLine.NextLine.EndOffset - 1 - offset : currentItem.EndLine.EndOffset - offset;
                            break;
                        }
                    case ReplaceType.Compound:
                        {
                            int colonIndex = startLineText.IndexOf(':') + 2;
                            int endOffset = 0;
                            string endLineText = textEditor.Document.GetText(endDocumentLine);
                            if (endLineText.TrimEnd().EndsWith(','))
                                endOffset = endLineText.LastIndexOf(',') + endDocumentLine.Offset;
                            else
                                endOffset = endDocumentLine.EndOffset;
                            offset = startDocumentLine.Offset + colonIndex;
                            length = endOffset - offset;
                            break;
                        }
                    case ReplaceType.String:
                    case ReplaceType.Input:
                        {
                            offset = item.StartLine.Offset;
                            newValue = new string(' ', spaceLength) + "\"" + item.Key + "\": " + (replaceType is ReplaceType.String ? "\"" : "") + newValue + (replaceType is ReplaceType.String ? "\"" : "") + (markValue ? "" : ',');
                            length = startDocumentLine.EndOffset - startDocumentLine.Offset;
                            break;
                        }
                }
            }
            else
            if ((replaceType is ReplaceType.Input || replaceType is ReplaceType.String) && startDocumentLine is not null)
            {
                offset = item.StartLine.Offset;
                newValue = new string(' ', spaceLength) + "\"" + item.Key + "\": " + (replaceType is ReplaceType.String ? "\"" : "") + newValue + (replaceType is ReplaceType.String ? "\"" : "") + (markValue ? "" : ',');
                length = startDocumentLine.EndOffset - startDocumentLine.Offset;
            }
            #endregion

            //计算好偏移量和替换长度后执行替换
            if (offset != 0 || length != 0)
                textEditor.Document.Replace(offset, length, newValue);
        }

        public void UpdateCompoundHeadAndSwitchKey(CompoundJsonTreeViewItem templateItem, CompoundJsonTreeViewItem targetItem)
        {
            string templateSwitchKey = templateItem.SwitchKey;
            if (templateSwitchKey.Length > 0)
                targetItem.SwitchKey = templateSwitchKey;
            string templateCompoundHead = templateItem.CompoundHead;
            if (templateCompoundHead.Length > 0)
                targetItem.CompoundHead = templateCompoundHead;
        }

        public JsonTreeViewItem ModifyJsonItemDictionary(JsonTreeViewItem targetItem, ModifyType modifyType)
        {
            JsonTreeViewItem result = null;
            switch (modifyType)
            {
                case ModifyType.Remove:
                    {
                        if (targetItem is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                        {
                            string parentPath = compoundJsonTreeViewItem.Path;

                            Parallel.ForEach(KeyValueContextDictionary.Keys, item =>
                            {
                                if (item.Contains(parentPath) && item != parentPath)
                                {
                                    KeyValueContextDictionary.Remove(item);
                                }
                            });
                        }
                        else
                        {
                            KeyValueContextDictionary.Remove(targetItem.Path);
                        }
                        break;
                    }
                case ModifyType.Get:
                    {
                        result = KeyValueContextDictionary[targetItem.Path];
                        break;
                    }
            }
            return result;
        }

        public DocumentLine GetLineByNumber(int lineNumber)
        {
            return textEditor.Document.GetLineByNumber(lineNumber);
        }

        public string GetRangeText(int startOffset, int length)
        {
            return textEditor.Document.GetText(startOffset, length);
        }

        public void SetRangeText(int startOffset, int length, string value)
        {
            textEditor.Document.Replace(startOffset, length, value);
        }

        public void DeleteAllLinesInTheSpecifiedRange(CompoundJsonTreeViewItem compoundJsonTreeViewItem)
        {
            textEditor.Document.Remove(compoundJsonTreeViewItem.StartLine.Offset, compoundJsonTreeViewItem.EndLine.EndOffset - compoundJsonTreeViewItem.StartLine.Offset);
            KeyValueContextDictionary.Remove(compoundJsonTreeViewItem.Path);

            #region 这里需要递归遍历每一层的节点来删除字典中所有相关的Pair
            compoundJsonTreeViewItem.JsonItemTool.RecursiveTraverseAndRunOperate(compoundJsonTreeViewItem.Children, item =>
            {
                if (KeyValueContextDictionary.ContainsKey(item.Path))
                    KeyValueContextDictionary.Remove(item.Path);
            });
            #endregion
        }

        public void DeleteAllLinesInTheSpecifiedRange(int startOffset, int endOffset)
        {
            textEditor.Document.Remove(startOffset, endOffset - startOffset);
        }
    }
}