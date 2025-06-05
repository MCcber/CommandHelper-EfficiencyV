using CBHK.CustomControl.JsonTreeViewComponents;
using CBHK.GeneralTool.TreeViewComponentsHelper;
using CommunityToolkit.Mvvm.ComponentModel;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Windows;
using System.Xml;
using System.Threading.Tasks;
using CBHK.CustomControl.Interfaces;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using ICSharpCode.AvalonEdit.Document;
using static CBHK.Model.Common.Enums;
using Prism.Ioc;
using CBHK.View;
using System.Collections.ObjectModel;
using CBHK.Model.Common;
using CBHK.GeneralTool;
using CBHK.CustomControl;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace CBHK.ViewModel.Generator
{
    public partial class AdvancementViewModel : ObservableObject, ICustomWorldUnifiedPlan
    {
        #region Field
        private HtmlHelper _htmlHelper = null;
        private Window home = null;
        private string configDirectoryPath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Advancement\Data\Rule\";
        private string commonCompoundDataDirectoryPath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Common\";
        public TextEditor textEditor = null;
        private FoldingManager foldingManager = null;
        private IContainerProvider _container;
        JsonTreeViewItemExtension jsonTool = null;
        #endregion

        #region Property
        public string RootDirectory { get; set; } = @"Advancement\Data\Rule\";

        private TextComboBoxItem _currentVersion = new();
        public TextComboBoxItem CurrentVersion
        {
            get => _currentVersion;
            set => SetProperty(ref _currentVersion, value);
        }

        public ObservableCollection<TextComboBoxItem> VersionList { get; set; } =
            [
                new TextComboBoxItem()
                {
                    Text = "1.20.3-1.20.4"
                }
            ];

        [ObservableProperty]
        public ObservableCollection<JsonTreeViewItem> _treeViewItemList = [];

        public Dictionary<string, JsonTreeViewItem> KeyValueContextDictionary { get; set; } = [];
        public Dictionary<string, List<string>> DependencyItemList { get; set; } = [];

        public Dictionary<string, string> PresetCustomCompoundKeyDictionary { get; set; } = new()
        {
            { "enum:trigger.optionalcompound:conditions","#准则触发器" },
            { "string:type.any:contentType","#文本组件" }
        };

        public Dictionary<string, Dictionary<string, List<string>>> EnumCompoundDataDictionary { get; set; } = [];

        public Dictionary<string, List<string>> EnumIDDictionary { get; set; } = [];
        public Dictionary<string, string> TranslateDictionary { get; set; } = new()
        {
            { "#准则|上文", "#Inherit/predicate" },
            { "#准则|下文", "#准则" },
            { "#战利品表谓词","#Inherit/predicate" },
            { "#图例|上文","#Inherit/predicate" },
            { "#文本组件","#Inherit/text component/main" },
            { "#文本组件内容","#Inherit/text component/content" },
            { "#记分板|记分项","#Inherit/common/scoreboardObject" },
            { "#流体状态|流体状态","#Inherit/common/fluidStructure"},
            { "<''物品堆叠组件''>" , "#Inherit/common/itemStackComponent" },
            { "<''准则名称''>","#准则触发器" },
            { "<''状态效果ID''>","药水#物品数据值|酿造药水的ID" },
            { "<''流体属性''>","#Inherit/common/fluidStructure"}
        };
        public Dictionary<string, string> TranslateDefaultDictionary { get; set; } = new()
        {
            { "#Inherit/predicate","#Inherit/conditions/entity" }
        };
        public List<string> DependencyFileList { get; set; }
        public List<string> DependencyDirectoryList { get; set; }
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
            DependencyItemList.Clear();
        }

        /// <summary>
        /// 安装大纲、应用高亮规则
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void TextEditor_Loaded(object sender, RoutedEventArgs e)
        {
            #region 添加数据上下文所需的枚举集合与转换字典数据
            EnumIDDictionary.Add("流体ID", ["minecraft:water","minecraft:lava"]);
            EnumIDDictionary.Add("方块ID", ["minecraft:acacia_button", "minecraft:acacia_door"]);
            EnumIDDictionary.Add("物品ID", ["minecraft:acacia_button", "minecraft:acacia_door"]);
            EnumIDDictionary.Add("战利品表", ["minecraft:a", "minecraft:b", "minecraft:c"]);
            EnumIDDictionary.Add("药水#物品数据值|酿造药水的ID", ["minecraft:potion_a", "minecraft:potion_b"]);
            EnumIDDictionary.Add("染料颜色", ["red","green","blue"]);
            #endregion
            #region 添加复合类数据
            string[] commonDirectoryFileArray = Directory.GetFiles(commonCompoundDataDirectoryPath);
            foreach (var item in commonDirectoryFileArray)
            {
                string fileName = Path.GetFileNameWithoutExtension(item);
                string data = File.ReadAllText(item);
                switch (fileName)
                {
                    case "BlockStateProperty":
                        {
                            JObject blockStatePropertyObject = JObject.Parse(data);
                            List<JProperty> blockIDList = [..blockStatePropertyObject.Properties()];
                            Dictionary<string, List<string>> blockStateCompound = [];
                            foreach (var blockID in blockIDList)
                            {
                                blockStateCompound.TryAdd(blockID.Name, []);
                                if (blockStatePropertyObject[blockID.Name][0] is JObject propertObject)
                                {
                                    blockStateCompound[blockID.Name].AddRange(propertObject.Properties().Select(item => '{' + item.ToString() + '}'));
                                }
                            }
                            EnumCompoundDataDictionary.Add(fileName, blockStateCompound);
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            #endregion

            await Task.Run(async () =>
            {
                textEditor = sender as TextEditor;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    JsonTreeViewDataStructure result = _htmlHelper.AnalyzeHTMLData(configDirectoryPath + CurrentVersion.Text);
                    textEditor.Text = "{\r\n" + result.ResultString.ToString().TrimEnd([',', '\r', '\n']) + "\r\n}";
                    foreach (var item in result.Result)
                    {
                        if(item is CompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.Children.Count > 0)
                        {
                            jsonTool.SetParentForEachItem(compoundJsonTreeViewItem.Children, compoundJsonTreeViewItem);
                        }
                    }

                    jsonTool.SetLayerCountForEachItem(result.Result,1);
                    jsonTool.SetLineNumbersForEachItem(result.Result, null);
                    TreeViewItemList = result.Result;

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
            #region Field
            bool IsCurrentNull = false;
            DocumentLine startDocumentLine = null;
            int offset = 0, length = 0;

            if (item.StartLine is not null && !item.StartLine.IsDeleted)
            {
                startDocumentLine = item.StartLine;
            }

            string startLineText = "";
            if (startDocumentLine is not null && !startDocumentLine.IsDeleted)
            {
                startLineText = textEditor.Document.GetText(startDocumentLine);
            }
            #endregion

            #region 定位相邻的已有值的两个节点
            CompoundJsonTreeViewItem parent = item.Parent;
            CompoundJsonTreeViewItem previousCompound = null;
            CompoundJsonTreeViewItem nextCompound = null;
            Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNextItem = item.JsonItemTool.LocateTheNodesOfTwoAdjacentExistingValues(item.Previous, item.Next);
            JsonTreeViewItem previous = previousAndNextItem.Item1;
            JsonTreeViewItem next = previousAndNextItem.Item2;

            if (previous is not null && previous.StartLine is null)
            {
                while (parent is not null && parent.StartLine is null)
                {
                    if(parent.Parent is null)
                    {
                        break;
                    }
                    parent = parent.Parent;
                }
            }

            if (previous is CompoundJsonTreeViewItem)
            {
                previousCompound = previous as CompoundJsonTreeViewItem;
            }
            if(next is CompoundJsonTreeViewItem)
            {
                nextCompound = next as CompoundJsonTreeViewItem;
            }
            #endregion

            #region 处理复合型跟值类型的起始索引与长度

            #region 判定起始行位置
            if (startDocumentLine is null && previous is CompoundJsonTreeViewItem previousItem1 && previousItem1.EndLine is not null)
            {
                IsCurrentNull = true;
                startDocumentLine = previousItem1.EndLine;
            }
            else
            if(startDocumentLine is null && previous is not null && previous.StartLine is not null)
            {
                IsCurrentNull = true;
                startDocumentLine = previous.StartLine; 
            }
            else
            if(startDocumentLine is null && parent is not null && parent.StartLine is not null)
            {
                IsCurrentNull = true;
                startDocumentLine = parent.StartLine;
            }
            else
            if(startDocumentLine is null)
            {
                IsCurrentNull = true;
                startDocumentLine = item.Plan.GetLineByNumber(2);
            }
            //else
            //    if(item is CompoundJsonTreeViewItem compoundJsonTreeViewItem1 && compoundJsonTreeViewItem1.StartLine == compoundJsonTreeViewItem1.EndLine)
            //{
            //    IsCurrentNull = true;
            //    startDocumentLine = item.Plan.GetLineByNumber(item.StartLine.LineNumber);
            //}
            #endregion

            startLineText = textEditor.Document.GetText(startDocumentLine);

            if (!IsCurrentNull)
            {
                if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                {
                    switch (changeType)
                    {
                        case ChangeType.NumberAndBool:
                        case ChangeType.String:
                            {
                                int index = startLineText.IndexOf(':') + 2;
                                if(index > 2)
                                {
                                    offset = startDocumentLine.Offset + index;
                                    length = startDocumentLine.EndOffset - offset - (next is not null && next.StartLine is not null ? 1 : 0);
                                }
                                else
                                {
                                    index = startLineText.IndexOf('"');
                                    int lastCharIndex = startLineText.LastIndexOf('"') + 1;
                                    offset = startDocumentLine.Offset + index;
                                    length = startDocumentLine.Offset + lastCharIndex - offset;
                                }
                                break;
                            }
                        case ChangeType.AddCompoundObject:
                            {
                                int index = startLineText.IndexOf('{') + 1;
                                offset = startDocumentLine.Offset + index;
                                break;
                            }
                        case ChangeType.AddListElement:
                            {
                                int index = startLineText.IndexOf('[') + 1;
                                offset = startDocumentLine.Offset + index;
                                break;
                            }
                        case ChangeType.AddListElementToEnd:
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
                                if(compoundJsonTreeViewItem.IsCanBeDefaulted)
                                {
                                    bool isNeedComma = previous is not null && previous.StartLine is not null && (next is null || (next is not null && next.StartLine is null));
                                    if (previousCompound is not null && previousCompound.EndLine is not null)
                                    {
                                        offset = previousCompound.EndLine.EndOffset;
                                        length = compoundJsonTreeViewItem.EndLine.EndOffset - offset;
                                    }
                                    else
                                    if (previous is not null && previous.StartLine is not null)
                                    {
                                        offset = previous.StartLine.EndOffset;
                                        length = compoundJsonTreeViewItem.EndLine.EndOffset - offset;
                                    }
                                    else
                                    if (parent is not null)
                                    {
                                        offset = parent.StartLine.EndOffset;
                                        string parentStartlineText = GetRangeText(compoundJsonTreeViewItem.Parent.EndLine.Offset, compoundJsonTreeViewItem.Parent.EndLine.Length);
                                        int lastCharIndex = parentStartlineText.LastIndexOf('}');
                                        if(lastCharIndex == -1)
                                        {
                                            lastCharIndex = parentStartlineText.LastIndexOf(']');
                                        }
                                        length = parent.EndLine.Offset + lastCharIndex - offset;
                                    }
                                    else
                                    {
                                        offset = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.StartLine.LineNumber - 1).EndOffset;
                                    }
                                    offset -= isNeedComma ? 1 : 0;
                                    compoundJsonTreeViewItem.Children.Clear();
                                    compoundJsonTreeViewItem.EndLine = null;
                                }
                                else
                                {
                                    offset = compoundJsonTreeViewItem.StartLine.EndOffset;
                                    length = compoundJsonTreeViewItem.EndLine.Offset + (compoundJsonTreeViewItem.LayerCount * 2) - offset;
                                    compoundJsonTreeViewItem.Children.Clear();
                                }
                                break;
                            }
                        case ChangeType.RemoveList:
                            {
                                break;
                            }
                        case ChangeType.RemoveListElement:
                            {
                                break;
                            }
                    }
                }
                else
                if (changeType is ChangeType.NumberAndBool || changeType is ChangeType.String)
                {
                    //满足一系列条件后父节点大括号内部清空
                    if (string.IsNullOrEmpty(newValue.Replace("\"","").Trim()))
                    {
                        char locateStartChar = ' ';
                        char locateEndChar = ' ';
                        if ((previous is not null && previous.StartLine is null && next is not null && next.StartLine is null) || (previous is null && next is null) && item.IsCanBeDefaulted)
                        {
                            if (item.Parent.DataType is not DataType.Array)
                            {
                                locateStartChar = '{';
                                locateEndChar = '}';
                            }
                            else
                            {
                                locateStartChar = '[';
                                locateEndChar = ']';
                            }
                        }
                        else
                        {
                            locateStartChar = ',';
                        }
                        DocumentLine parentStartLine = startDocumentLine.PreviousLine;
                        DocumentLine parentEndLine = startDocumentLine.NextLine;
                        string parentEndLineText = "";

                        if (item.Parent is not null)
                        {
                            string parentStartLineText = GetRangeText(parentStartLine.Offset, parentStartLine.Length);
                            parentEndLineText = GetRangeText(parentEndLine.Offset, parentEndLine.Length);

                            offset = parentStartLine.Offset + parentStartLineText.IndexOf(locateStartChar) + 1;
                        }
                        int closeBracketOffset = 0;
                        if (locateEndChar == ' ')
                        {
                            closeBracketOffset = item.StartLine.EndOffset;
                        }
                        else
                        {
                            closeBracketOffset = parentEndLine.Offset + parentEndLineText.IndexOf(locateEndChar);
                        }

                        length = closeBracketOffset - offset;
                        newValue = "";
                    }
                    else
                    {
                        int targetOffset = 0;
                        if (startLineText.Contains(':'))
                        {
                            targetOffset = startLineText.IndexOf(':') + 2;
                        }
                        else
                        if (startLineText.Contains('"'))
                        {
                            targetOffset = startLineText.IndexOf('"');
                        }
                        else
                        {
                            targetOffset = startLineText.Length - startLineText.TrimStart().Length;
                        }
                        offset = startDocumentLine.Offset + targetOffset;
                        if (startLineText.TrimEnd().EndsWith(','))
                        {
                            length = startLineText.LastIndexOf(',') - targetOffset;
                        }
                        else
                        {
                            length = startDocumentLine.Length - targetOffset;
                        }
                    }
                }
            }
            else
            {
                int lastOffset = 0;

                if (item.StartLine is null)
                {
                    if (previous is not null && previous.StartLine is not null)
                    {
                        lastOffset = startLineText.LastIndexOf(',') + 1;
                    }
                    else
                    if (parent is not null && parent.StartLine is not null)
                    {
                        lastOffset = GetRangeText(parent.StartLine.Offset, parent.StartLine.Length).LastIndexOf(':') + 3;
                    }
                }
                else
                {
                    lastOffset = startLineText.IndexOf('"');
                }

                if (lastOffset == 0)
                {
                    offset = startDocumentLine.EndOffset;
                }
                else
                {
                    offset = startDocumentLine.Offset + lastOffset;
                }
            }
            #endregion

            #region 计算好偏移量和替换长度后执行替换
            if (offset >= 0 || length > -1)
            {
                SetRangeText(offset, length, newValue);
            }
            #endregion

            #region 设置可选节点的行引用
            if (string.IsNullOrEmpty(newValue))
            {
                item.StartLine = null;
            }
            else
            {
                if (item.StartLine is null && previousCompound is not null && previousCompound.EndLine is not null)
                {
                    item.StartLine = previousCompound.EndLine.NextLine;
                }
                else
                if(item.StartLine is null && previous is not null && previous.StartLine is not null)
                {
                    item.StartLine = previous.StartLine.NextLine;
                }
                else
                if(item.StartLine is null && parent is not null && parent.StartLine is not null)
                {
                    item.StartLine = parent.StartLine.NextLine;
                }
                item.StartLine ??= GetLineByNumber(2);
            }
            #endregion
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
            if (lineNumber <= textEditor.Document.LineCount)
            {
                return textEditor.Document.GetLineByNumber(lineNumber);
            }
            return null;
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