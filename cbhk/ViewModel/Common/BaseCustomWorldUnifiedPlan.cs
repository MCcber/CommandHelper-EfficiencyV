using CBHK.CustomControl;
using CBHK.CustomControl.Interfaces;
using CBHK.CustomControl.JsonTreeViewComponents;
using CBHK.GeneralTool;
using CBHK.GeneralTool.TreeViewComponentsHelper;
using CBHK.View;
using CommunityToolkit.Mvvm.ComponentModel;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using Newtonsoft.Json.Linq;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static CBHK.Model.Common.Enums;

namespace CBHK.ViewModel.Common
{
    public abstract class BaseCustomWorldUnifiedPlan: ObservableObject,ICustomWorldUnifiedPlan
    {
        #region Property
        protected virtual HtmlHelper htmlHelper { get; set; }
        protected virtual Window Home { get; set; }
        public virtual string ConfigDirectoryPath { get; set; }
        public virtual string CommonCompoundDataDirectoryPath { get; set; }
        protected virtual TextEditor TextEditor { get; set; }
        protected virtual FoldingManager FoldingManager { get; set; }
        protected virtual IContainerProvider Container { get; set; }
        protected virtual JsonTreeViewItemExtension JsonTool { get; set; }

        public virtual TextComboBoxItem CurrentVersion { get; set; }

        public virtual ObservableCollection<TextComboBoxItem> VersionList { get; set; } = [];

        public virtual ObservableCollection<JsonTreeViewItem> TreeViewItemList { get; set; } = [];

        public virtual Dictionary<string, JsonTreeViewItem> KeyValueContextDictionary { get; set; } = [];
        public virtual Dictionary<string, List<string>> DependencyItemList { get; set; } = [];
        public virtual Dictionary<string, Dictionary<string, List<string>>> EnumCompoundDataDictionary { get; set; } = [];

        public virtual Dictionary<string, List<string>> EnumIDDictionary { get; set; } = [];
        public virtual Dictionary<string, string> TranslateDictionary { get; set; }
        public virtual Dictionary<string, string> TranslateDefaultDictionary { get; set; }
        public virtual List<string> DependencyFileList { get; set; }
        public virtual List<string> DependencyDirectoryList { get; set; }
        #endregion

        public BaseCustomWorldUnifiedPlan(IContainerProvider container, MainView mainView)
        {
            Container = container;
            Home = mainView;
            htmlHelper = new(Container)
            {
                plan = this,
                jsonTool = JsonTool = new JsonTreeViewItemExtension(Container)
            };

            CurrentVersion = VersionList[0];
            LoadDictionaryFromFile();
        }

        public int GetDocumentLineCount()
        {
            return TextEditor.Document.LineCount;
        }

        public virtual void LoadDictionaryFromFile()
        {
            string rawString = "";
            if(File.Exists(ConfigDirectoryPath + CurrentVersion.Text + "\\translateDictionary.json"))
            {
                rawString = File.ReadAllText(ConfigDirectoryPath + CurrentVersion.Text + "\\translateDictionary.json");
                JObject data = JObject.Parse(rawString);
                foreach (var item in data.Properties().Cast<JProperty>())
                {
                    TranslateDictionary.TryAdd(item.Name, data[item.Name].ToString());
                }
            }
            if (File.Exists(ConfigDirectoryPath + CurrentVersion.Text + "\\translateDefaultDictionary.json"))
            {
                rawString = File.ReadAllText(ConfigDirectoryPath + CurrentVersion.Text + "\\translateDefaultDictionary.json");
                JObject data = JObject.Parse(rawString);
                foreach (var item in data.Properties().Cast<JProperty>())
                {
                    TranslateDefaultDictionary.TryAdd(item.Name, data[item.Name].ToString());
                }
            }
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

        public void UpdateNullValueBySpecifyingInterval(int endOffset, string newValue = "\r\n")
        {
            TextEditor.Document.Insert(endOffset, newValue);
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
                startLineText = TextEditor.Document.GetText(startDocumentLine);
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
                    if (parent.Parent is null)
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
            if (next is CompoundJsonTreeViewItem)
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
            if (startDocumentLine is null && previous is not null && previous.StartLine is not null)
            {
                IsCurrentNull = true;
                startDocumentLine = previous.StartLine;
            }
            else
            if (startDocumentLine is null && parent is not null && parent.StartLine is not null)
            {
                IsCurrentNull = true;
                startDocumentLine = parent.StartLine;
            }
            else
            if (startDocumentLine is null)
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

            startLineText = TextEditor.Document.GetText(startDocumentLine);

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
                                if (index > 2)
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
                                if (compoundJsonTreeViewItem.IsCanBeDefaulted)
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
                                        if (lastCharIndex == -1)
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
                    if (string.IsNullOrEmpty(newValue.Replace("\"", "").Trim()))
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
                if (item.StartLine is null && previous is not null && previous.StartLine is not null)
                {
                    item.StartLine = previous.StartLine.NextLine;
                }
                else
                if (item.StartLine is null && parent is not null && parent.StartLine is not null)
                {
                    item.StartLine = parent.StartLine.NextLine;
                }
                item.StartLine ??= GetLineByNumber(2);
            }
            #endregion
        }

        public DocumentLine GetLineByNumber(int lineNumber)
        {
            if (lineNumber <= TextEditor.Document.LineCount)
            {
                return TextEditor.Document.GetLineByNumber(lineNumber);
            }
            return null;
        }

        public string GetRangeText(int startOffset, int length)
        {
            return TextEditor.Document.GetText(startOffset, length);
        }

        public void SetRangeText(int startOffset, int length, string value)
        {
            TextEditor.Document.Replace(startOffset, length, value);
        }
    }
}
