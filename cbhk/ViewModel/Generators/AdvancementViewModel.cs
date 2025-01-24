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
using Newtonsoft.Json;
using ICSharpCode.AvalonEdit.Document;
using static cbhk.Model.Common.Enums;
using Prism.Ioc;
using cbhk.View;
using System.Collections.ObjectModel;
using cbhk.Model.Common;
using cbhk.GeneralTools;
using System.Net.NetworkInformation;

namespace cbhk.ViewModel.Generators
{
    public partial class AdvancementViewModel : ObservableObject, ICustomWorldUnifiedPlan
    {
        #region Field
        private HtmlHelper _htmlHelper = null;
        private Window home = null;
        private string configDirectoryPath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Advancement\Data\Rule\1.20.4";
        public TextEditor textEditor = null;
        private FoldingManager foldingManager = null;
        private IContainerProvider _container;
        JsonTreeViewItemExtension jsonTool = null;

        public Dictionary<string, string> PresetCustomCompoundKeyDictionary { get; set; } = new()
        {
            { "enum:trigger.compound:conditions","#准则触发器" }
        };

        public Dictionary<string, Dictionary<string, List<string>>> EnumCompoundDataDictionary { get; set; } = [];

        public Dictionary<string, List<string>> EnumIDDictionary { get; set; } = [];


        public ObservableCollection<JsonTreeViewItem> AdvancementTreeViewItemList = [];
        #endregion

        #region Property
        [ObservableProperty]
        public ObservableCollection<JsonTreeViewItem> _advancementItemList = [];

        public Dictionary<string, JsonTreeViewItem> KeyValueContextDictionary { get; set; } = [];
        public Dictionary<string, List<string>> CurrentDependencyItemList { get; set; } = [];
        #endregion

        public AdvancementViewModel(IContainerProvider container, MainView mainView)
        {
            _container = container;
            home = mainView;
            _htmlHelper = new(_container)
            {
                plan = this,
                jsonTool = jsonTool = new JsonTreeViewItemExtension(_container)
            };
        }

        /// <summary>
        /// 关闭窗体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CommonWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CurrentDependencyItemList.Clear();
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
                //生成值提供器字典
                //CurrentDependencyItemList = htmlHelper.AnalyzeHTMLData("");
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    JsonTreeViewDataStructure result = _htmlHelper.AnalyzeHTMLData(configDirectoryPath);
                    textEditor.Text = "{\r\n" + result.ResultString.ToString().TrimEnd([',', '\r', '\n']) + "\r\n}";
                    foreach (var item in result.Result)
                    {
                        item.JsonItemTool = jsonTool;
                        item.JsonItemTool.SetDocumentLineByLineNumber(item, textEditor);
                        KeyValueContextDictionary.TryAdd(item.Path, item);
                    }
                    AdvancementItemList = result.Result;

                    //为代码编辑器安装大纲管理器
                    foldingManager = FoldingManager.Install(textEditor.TextArea);
                    XshdSyntaxDefinition xshdSyntaxDefinition = new();
                    xshdSyntaxDefinition = HighlightingLoader.LoadXshd(new XmlTextReader(AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Common\Json.xshd"));
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

        public void UpdateValueBySpecifyingInterval(JsonTreeViewItem item, ChangeType changeType, string newValue = "")
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

                switch (changeType)
                {
                    case ChangeType.Input:
                        {
                            int index = startLineText.IndexOf(':') + 2;
                            offset = startDocumentLine.Offset + index;
                            if (startLineText.TrimEnd().EndsWith(','))
                            {
                                length = startLineText.LastIndexOf(',') - index;
                            }
                            else
                            {
                                length = startDocumentLine.EndOffset - startDocumentLine.Offset - index - 1;
                            }
                            if(item.DataType is DataTypes.String || (item is CompoundJsonTreeViewItem compoundItem && compoundItem.DataType is DataTypes.Enum))
                            {
                                newValue = "\"" + newValue + "\"";
                            }
                            break;
                        }
                    case ChangeType.AddCompoundObject:
                        {
                            int index = startLineText.IndexOf('{') + 1;
                            offset = startDocumentLine.Offset + index;
                            break;
                        }
                    case ChangeType.AddArrayElement:
                        {
                            int index = startLineText.IndexOf('[') + 1;
                            offset = startDocumentLine.Offset + index;
                            break;
                        }
                    case ChangeType.AddArrayElementToEnd:
                        {
                            JsonTreeViewItem lastItem = (item as CompoundJsonTreeViewItem).Children[^1];
                            if (lastItem is CompoundJsonTreeViewItem lastCompoundItem && lastCompoundItem.EndLine is not null)
                            {
                                offset = lastCompoundItem.EndLine.EndOffset;
                            }
                            else
                            {
                                offset = lastItem.StartLine.EndOffset;
                            }
                            break;
                        }
                    case ChangeType.RemoveCompound:
                        {
                            break;
                        }
                    case ChangeType.RemoveCompoundObject:
                        {
                            break;
                        }
                    case ChangeType.RemoveArray:
                        {
                            break;
                        }
                    case ChangeType.RemoveArrayElement:
                        {
                            break;
                        }
                }
            }
            #endregion

            //计算好偏移量和替换长度后执行替换
            if (offset != 0 || length != 0)
                textEditor.Document.Replace(offset, length, newValue);
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
            textEditor.Document.Remove(compoundJsonTreeViewItem.StartLine.Offset, compoundJsonTreeViewItem.EndLine.EndOffset + 1 - compoundJsonTreeViewItem.StartLine.Offset);
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