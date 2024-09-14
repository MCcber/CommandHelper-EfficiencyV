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
using static cbhk.Model.MainWindowProperties;

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

        [GeneratedRegex(@"^\:?\*+\{\{nbt\|(?<1>[a-z]+)\|(?<2>[a-z_]+)\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetNodeTypeAndKey();

        [GeneratedRegex(@"^\:?\*+(?<branch>\{\{nbt\|[a-z]+\}\})+(\{\{nbt\|(?<1>[a-z]+)\|(?<2>[a-z_]+)\}\})", RegexOptions.IgnoreCase)]
        private static partial Regex GetMultiTypeAndKeyOfNode();

        [GeneratedRegex(@"（可选，默认为\{\{cd\|[a-z]+\}\}）", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultStringValue();

        [GeneratedRegex(@"（可选，默认为\d+）", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultNumberValue();

        [GeneratedRegex(@"（可选，默认为\{\{cd\|(true|false)\}\}）", RegexOptions.IgnoreCase)]
        private static partial Regex GetDefaultBoolValue();

        [GeneratedRegex(@"\[\[方块标签\]\]", RegexOptions.IgnoreCase)]
        private static partial Regex GetBlcokTagValue();

        [GeneratedRegex(@"\[\[实体标签\]\]", RegexOptions.IgnoreCase)]
        private static partial Regex GetEntityTagValue();

        [GeneratedRegex(@"\{\{cd\|[a-z:_]+\}\}", RegexOptions.IgnoreCase)]
        private static partial Regex GetEnumValue();
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

        private async Task AnalyzeHTMLData()
        {
            await Task.Run(() =>
            {
                HtmlDocument doc = new()
                {
                    OptionFixNestedTags = true,
                    OptionAutoCloseOnEnd = true
                };
                string wikiData = File.ReadAllText(AdvancementWikiFilePath);
                doc.LoadHtml(wikiData);
                HtmlNodeCollection divNodes = doc.DocumentNode.SelectNodes("//div");

                if (divNodes is not null)
                {
                    JsonTreeViewDataStructure result = new();
                    //遍历找到的div标签集合
                    foreach (HtmlNode divNode in divNodes)
                    {
                        // 可以在这里对每个 div 标签进行操作，比如获取属性、子节点等
                        string[] nodeList = divNode.InnerHtml.Split("\r\n");
                        JsonTreeViewDataStructure data = GetAdvancementItemList(new(), nodeList, 2, 1);
                        result.Result.AddRange(data.Result);
                        result.ResultString.Append(data.ResultString);
                        AdvancementTreeViewItemList = result.Result;
                    }
                }
            });
        }

        private JsonTreeViewDataStructure GetAdvancementItemList(JsonTreeViewDataStructure result, string[] array, int lineNumber, int layerCount, CompoundJsonTreeViewItem ParentItem = null, JsonTreeViewItem Last = null, bool isProcessingTemplate = false, int LastStarCount = 1)
        {
            #region 处理起始字符串
            if (result.ResultString.Length == 0)
                result.ResultString.Append("{\r\n");
            #endregion

            // 遍历找到的 div 标签集合
            foreach (var node in array)
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
                JsonTreeViewItem lastItem = null, nextItem = null;
                #endregion

                #region 计算当前行星号数量
                MatchCollection starCollection = Regex.Matches(node, @"^\*+");
                int count = starCollection.Count;
                #endregion

                #region 处理值类型
                MatchCollection nodeTypeAndKeyInfoGroupList = GetNodeTypeAndKey().Matches(node);
                MatchCollection multiNodeTypeAndKeyInfoGroupList = GetMultiTypeAndKeyOfNode().Matches(node);

                if (multiNodeTypeAndKeyInfoGroupList.Count > 0)
                {
                    foreach (var dataType in multiNodeTypeAndKeyInfoGroupList.Cast<Match>())
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
                                    if (dataType.Value == "TAG_String")
                                        item.DataType = DataTypes.String;
                                    break;
                                }
                        }
                        if (!IsSimpleItem)
                        {
                            if (!isProcessingTemplate)
                            {
                                lastItem = item.Last;
                                nextItem = item.Next;
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

                                            Match boolMatch = GetDefaultBoolValue().Match(node);
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

                                            Match stringMatch = GetDefaultStringValue().Match(node);
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
                                        Match numberMatch = GetDefaultNumberValue().Match(node);
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
                            Match BlockTagCollection = GetBlcokTagValue().Match(node);
                            Match EntityTagCollection = GetEntityTagValue().Match(node);
                            Match DefaultValueMatch = GetDefaultBoolValue().Match(node);

                            if (EnumCollection.Count > 0)
                            {
                                foreach (Match enumMatch in EnumCollection)
                                {
                                    SetTypeCompoundItem.EnumItemsSource.Add(new CustomControls.TextComboBoxItem() { Text = enumMatch.Value });
                                }
                            }
                            else
                            if (BlockTagCollection.Count > 0)
                            {
                                foreach (string blockTagMatch in BlockTagList)
                                {
                                    SetTypeCompoundItem.EnumItemsSource.Add(new CustomControls.TextComboBoxItem() { Text = blockTagMatch.Value });
                                }
                            }
                            else
                            if (EntityTagCollection.Count > 0)
                            {
                                foreach (string entityTagMatch in ItemTagList)
                                {
                                    SetTypeCompoundItem.EnumItemsSource.Add(new CustomControls.TextComboBoxItem() { Text = entityTagMatch.Value });
                                }
                            }

                            #region 处理默认值
                            if(EnumCollection.Count > 0 || BlockTagCollection.Count > 0 || EntityTagCollection.Count > 0)
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
                                if (DefaultValueMatch is not null)
                                {
                                    result.ResultString.Append("\"" + key.ToLower() + "\"");
                                    string defaultValue = DefaultValueMatch.Value;

                                    if (bool.TryParse(defaultValue, out bool boolValue))
                                    {
                                        SetTypeCompoundItem.DataType = DataTypes.Bool;
                                        SetTypeCompoundItem.Value = boolValue;
                                        if (boolValue)
                                            SetTypeCompoundItem.IsTrue = true;
                                        else
                                            SetTypeCompoundItem.IsFalse = true;
                                        result.ResultString.Append(": " + boolValue.ToString().ToLower());
                                    }
                                    else
                                        if (int.TryParse(defaultValue, out int intValue))
                                    {
                                        SetTypeCompoundItem.DataType = DataTypes.Int;
                                        SetTypeCompoundItem.Value = intValue;
                                        result.ResultString.Append(": " + intValue);
                                    }
                                    else
                                    {
                                        SetTypeCompoundItem.DataType = DataTypes.String;
                                        SetTypeCompoundItem.Value = "\"" + defaultValue + "\"";
                                        result.ResultString.Append(": \"" + defaultValue + "\"");
                                    }
                                }
                                #endregion
                            }
                            #endregion

                            #endregion

                            #region 处理不同行为的复合型数据
                            //case "optionalCompound":
                            //case "nullableCompound":
                            //case "compound":
                            //case "customCompound":
                            if (dataType == "TAG_NullableCompound" || dataType == "TAG_OptionalCompound")
                            {
                                SetTypeCompoundItem.DataType = dataType == "TAG_NullableCompound" ? DataTypes.NullableCompound : DataTypes.OptionalCompound;
                            }
                            else
                        if (dataType == "TAG_CustomCompound")
                            {
                                SetTypeCompoundItem.DataType = DataTypes.CustomCompound;
                            }
                            else
                            {
                                SetTypeCompoundItem.DataType = DataTypes.Compound;
                            }

                            #region 计算当前数据类型、追加Key、前置追加空格
                            string key = "";
                            SetTypeCompoundItem.DefaultValue = "";
                            if (token["key"] is JToken keyToken)
                                SetTypeCompoundItem.Key = key = keyToken.ToString();
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
                            if (token["key"] is JToken keyToken)
                            {
                                SetTypeCompoundItem.AddElementButtonVisibility = Visibility.Visible;
                                SetTypeCompoundItem.DataType = DataTypes.Array;
                                if (dataType == "TAG_InnerArray")
                                    SetTypeCompoundItem.DataType = DataTypes.InnerArray;
                                SetTypeCompoundItem.Key = keyToken.ToString();
                                SetTypeCompoundItem.StartLineNumber = SetTypeCompoundItem.EndLineNumber = lineNumber;
                                if (Last is CompoundJsonTreeViewItem lastCompletedCompoundItem && (lastCompletedCompoundItem.DataType is DataTypes.OptionalCompound || lastCompletedCompoundItem.DataType is DataTypes.NullableCompound || lastCompletedCompoundItem.DataType is DataTypes.Compound || lastCompletedCompoundItem.DataType is DataTypes.CustomCompound))
                                {
                                    SetTypeCompoundItem.StartLineNumber = SetTypeCompoundItem.EndLineNumber = lastCompletedCompoundItem.EndLineNumber + 1;
                                }

                                result.ResultString.Append(new string(' ', layerCount * 2) + (dataType == "TAG_Array" ? "\"" + keyToken.ToString() + "\"" : ""));
                            }
                            result.ResultString.Append((dataType == "TAG_Array" ? ": " : "") + "[]");
                            #endregion

                            #region 处理各种数值提供器或结构模板
                            //case "intProvider":
                            //case "floatProvider":
                            //case "heightProvider":
                            //case "verticalAnchor":
                            //case "blockPredicate":
                            //case "blockStateProvider":
                                if (token["key"] is JToken keyToken && token["dataTypeList"] is JArray dataTypeList)
                            {
                                string key = keyToken.ToString();
                                if (plan.ValueProviderContextDictionary.TryGetValue(dataTypeList[0].ToString().Replace("TAG_", ""), out CompoundJsonTreeViewItem currentCompoundItem))
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
                                if (isProcessingTemplate)
                                {
                                    SetTypeCompoundItem.Key = key;
                                    SetTypeCompoundItem.EnumBoxVisibility = Visibility.Visible;
                                    SetTypeCompoundItem.ValueProviderType = (ValueProviderTypes)Enum.Parse(typeof(ValueProviderTypes), dataTypeList[0].ToString().Replace("TAG_", ""));
                                    SetTypeCompoundItem.DataType = DataTypes.ValueProvider;
                                }
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
                        if (token["defaultValue"] is JToken DefaultValue)
                            item.DefaultValue = DefaultValue.ToString();
                        #endregion

                        item.Plan = this;
                    }
                }
                #endregion

                if (count < LastStarCount)
                {

                }
                else
                    if (count == LastStarCount)
                {

                }
                else
                if (ParentItem is not null)
                {
                    ParentItem.Children.Add(item);
                }
                else
                    result.Result.Add(item);
            }

            #region Json收尾后返回
            result.ResultString.Append("\r\n" + new string(' ', (layerCount - 1) * 2) + '}');
            return result;
            #endregion
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