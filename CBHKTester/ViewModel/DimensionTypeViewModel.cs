using cbhk.GeneralTools.TreeViewComponentsHelper;
using cbhk.View;
using CommunityToolkit.Mvvm.ComponentModel;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using static cbhk.CustomControls.JsonTreeViewComponents.Enums;
using System.Windows;
using CBHKTester.Model;
using CBHKTester.Interface;

namespace CBHKTester.ViewModel
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
        #endregion

        #region Property
        [ObservableProperty]
        public string _dimensionTypeItemString = "";

        public Dictionary<string, JsonTreeViewItem> KeyValueContextDictionary { get; set; } = [];
        public Dictionary<string, CompoundJsonTreeViewItem> ValueProviderContextDictionary { get; set; } = [];
        #endregion

        public DimensionTypeViewModel(IContainerProvider container, MainView mainView)
        {
            _container = container;
            home = mainView;
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
