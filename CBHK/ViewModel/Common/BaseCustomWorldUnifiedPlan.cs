using CBHK.CustomControl;
using CBHK.CustomControl.Interfaces;
using CBHK.CustomControl.JsonTreeViewComponents;
using CBHK.Domain;
using CBHK.GeneralTool;
using CBHK.GeneralTool.TreeViewComponentsHelper;
using CBHK.Model.Common;
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
        protected CBHKDataContext Context { get; set; }
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

        public virtual JsonTreeViewItem VisualLastItem { get; set; }
        #endregion

        public BaseCustomWorldUnifiedPlan(IContainerProvider container, MainView mainView, CBHKDataContext context)
        {
            Context = context;
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

        /// <summary>
        /// 搜索视觉上的前一个与后一个节点
        /// </summary>
        public virtual Tuple<JsonTreeViewItem, JsonTreeViewItem> SearchVisualPreviousAndNextItem(JsonTreeViewItem jsonTreeViewItem, bool isNeedSearchPrevious = true)
        {
            #region 定义字段、确定节点集合
            JsonTreeViewItem previous = null, next = null;
            int startIndex = jsonTreeViewItem.Index;
            #endregion

            #region 搜索视觉上的前一个节点
            if (isNeedSearchPrevious)
            {
                startIndex--;
                while (startIndex >= 0 && TreeViewItemList[startIndex].StartLine is null)
                {
                    startIndex--;
                }
                if (startIndex < 0)
                {
                    startIndex = 0;
                }
                if (startIndex < TreeViewItemList.Count && TreeViewItemList[startIndex].StartLine is not null && TreeViewItemList[startIndex] != jsonTreeViewItem)
                {
                    previous = TreeViewItemList[startIndex];
                }
            }
            #endregion

            #region 搜索视觉上的后一个节点
            startIndex = jsonTreeViewItem.Index + 1;
            while (startIndex < TreeViewItemList.Count && TreeViewItemList[startIndex].StartLine is null)
            {
                startIndex++;
            }
            if (startIndex < TreeViewItemList.Count && TreeViewItemList[startIndex].StartLine is not null && TreeViewItemList[startIndex] != jsonTreeViewItem)
            {
                next = TreeViewItemList[startIndex];
            }

            return new(previous, next);
            #endregion
        }

        /// <summary>
        /// 为指定节点集合的所有成员设置各自视觉上的前一个与后一个节点引用(二分查找最近邻居算法)
        /// </summary>
        /// <param name="treeViewItemList"></param>
        public void SetVisualPreviousAndNextForEachItem()
        {
            if (TreeViewItemList is null || TreeViewItemList.Count == 0)
            {
                return;
            }

            //收集必选节点
            var requiredNodes = TreeViewItemList.Where(item => !item.IsCanBeDefaulted || item.StartLine is not null).ToList();
            int requiredCount = requiredNodes.Count;

            // 没有必选节点时的处理
            if (requiredCount == 0)
            {
                foreach (var node in TreeViewItemList)
                {
                    node.VisualPrevious = null;
                    node.VisualNext = null;
                }
                return;
            }

            //提取索引列表
            var requiredIndices = requiredNodes.Select(r => r.Index).ToList();

            //处理每个节点
            foreach (var node in TreeViewItemList)
            {
                // 在必选节点索引列表中执行二分查找
                int position = requiredIndices.BinarySearch(node.Index);

                if (position >= 0)
                {
                    // 当前节点是必选节点
                    // 前一个必选节点 (索引-1)
                    node.VisualPrevious = (position > 0)
                        ? requiredNodes[position - 1]
                        : null;

                    // 后一个必选节点 (索引+1)
                    node.VisualNext = (position < requiredCount - 1)
                        ? requiredNodes[position + 1]
                        : null;
                }
                else
                {
                    // 当前节点是非必选节点
                    // 计算插入位置 (~position)
                    int insertPosition = ~position;

                    // 左侧最近的必选节点 (插入位置-1)
                    node.VisualPrevious = (insertPosition > 0)
                        ? requiredNodes[insertPosition - 1]
                        : null;

                    // 右侧最近的必选节点 (插入位置)
                    node.VisualNext = (insertPosition < requiredCount)
                        ? requiredNodes[insertPosition]
                        : null;
                }
            }
        }

        public void InsertChild(int targetIndex, JsonTreeViewDataStructure childData)
        {
            #region 处理视觉树
            bool isNeedComma = childData.ResultString.Length > 0;
            JsonTreeViewItem targetItem = null;
            string appendString = "";
            int offset = 0;
            if (targetIndex == TreeViewItemList.Count)
            {
                targetItem = VisualLastItem;
            }
            else
            if (targetIndex < TreeViewItemList.Count)
            {
                targetItem = TreeViewItemList[targetIndex].VisualPrevious;
            }

            if (targetItem is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem1 && baseCompoundJsonTreeViewItem1.EndLine is not null && !baseCompoundJsonTreeViewItem1.EndLine.IsDeleted)
            {
                offset = baseCompoundJsonTreeViewItem1.EndLine.EndOffset;
            }
            else
            if (targetItem is not null)
            {
                offset = targetItem.StartLine.EndOffset;
            }
            else
            {
                return;
            }

            appendString += (isNeedComma ? ',' : "") + "\r\n" + childData.ResultString.ToString() + "\r\n  ";
            TreeViewItemList[0].Plan.SetRangeText(offset, 0, appendString);
            if (targetItem is not null)
            {
                targetItem.VisualNext = childData.Result[0];
            }
            childData.Result[0].VisualPrevious = targetItem;
            #endregion

            #region 处理逻辑树
            TreeViewItemList.Insert(targetIndex, childData.Result[0]);
            #endregion
        }

        public void InsertChildren(int targetIndex, JsonTreeViewDataStructure childDataList)
        {
            #region 处理视觉树
            JsonTreeViewItem targetItem = null;
            int offset = 0;
            if (targetIndex == TreeViewItemList.Count)
            {
                targetItem = VisualLastItem;
            }
            else
            {
                targetItem = TreeViewItemList[targetIndex].VisualPrevious;
            }

            if (targetItem is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem && baseCompoundJsonTreeViewItem.EndLine is not null && !baseCompoundJsonTreeViewItem.EndLine.IsDeleted)
            {
                offset = baseCompoundJsonTreeViewItem.EndLine.EndOffset;
            }
            else
            {
                offset = targetItem.StartLine.EndOffset;
            }
            bool isNeedComma = targetItem.VisualNext is null;
            string currentString = (isNeedComma ? ',' : "") + "\r\n  " + childDataList.ResultString;
            TreeViewItemList[0].Plan.SetRangeText(offset, 0, currentString);
            targetItem.VisualNext = childDataList.Result[0];
            childDataList.Result[0].VisualPrevious = targetItem;
            #endregion

            #region 处理逻辑树
            for (int i = 0; i < childDataList.Result.Count; i++)
            {
                TreeViewItemList.Insert(targetIndex, childDataList.Result[i]);
                targetIndex++;
            }
            #endregion
        }

        /// <summary>
        /// 用于删除单个节点或列表元素
        /// </summary>
        /// <param name="childItem"></param>
        public void RemoveChild(List<JsonTreeViewItem> childrenList)
        {
            #region Field
            if (childrenList.Count == 0)
            {
                return;
            }
            JsonTreeViewItem groupLastItem = BaseCompoundJsonTreeViewItem.SearchEnumGroupLastVisualItem(childrenList);
            int offset = 0, length = 0;
            #endregion

            #region 确定替换的起始偏移
            if (childrenList[0].VisualPrevious is BaseCompoundJsonTreeViewItem visualPreviousCompoundItem && visualPreviousCompoundItem.EndLine is not null && !visualPreviousCompoundItem.EndLine.IsDeleted)
            {
                offset = visualPreviousCompoundItem.EndLine.EndOffset - (childrenList[0].VisualNext is null ? 1 : 0);
            }
            else
            if (childrenList[0].VisualPrevious is not null)
            {
                offset = childrenList[0].VisualPrevious.StartLine.EndOffset - (childrenList[0].VisualNext is null ? 1 : 0);
            }
            else
            {
                offset = 1;
            }
            #endregion

            #region 确定替换的长度
            if (groupLastItem is not null)
            {
                int endOffset = 0;
                if (groupLastItem is BaseCompoundJsonTreeViewItem lastCompoundItem && lastCompoundItem.EndLine is not null && !lastCompoundItem.EndLine.IsDeleted)
                {
                    length = lastCompoundItem.EndLine.EndOffset - offset;
                    endOffset = lastCompoundItem.EndLine.EndOffset;
                }
                else
                if (groupLastItem.StartLine is not null)
                {
                    length = childrenList[^1].StartLine.EndOffset - offset;
                    endOffset = childrenList[^1].StartLine.EndOffset;
                }
                if(groupLastItem.VisualPrevious is null && groupLastItem.VisualNext is null && groupLastItem.Parent is null)
                {
                    length = endOffset + 1;
                }
            }
            #endregion

            #region 执行替换并删除节点
            if (groupLastItem is not null)
            {
                childrenList[0].Plan.SetRangeText(offset, length, "");
            }
            foreach (var item in childrenList)
            {
                if (item.IsCanBeDefaulted)
                {
                    item.StartLine = null;
                }
                if (item.DisplayText == "Entry")
                {
                    TreeViewItemList.Remove(item);
                }
                else
                if(item is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem)
                {
                    baseCompoundJsonTreeViewItem.EndLine = null;
                    baseCompoundJsonTreeViewItem.LogicChildren.Clear();
                    baseCompoundJsonTreeViewItem.VisualLastChild = null;
                }
            }

            if (TreeViewItemList.Count == 1 && TreeViewItemList[0] is BaseCompoundJsonTreeViewItem bottomItem && bottomItem.ItemType is ItemType.BottomButton)
            {
                TreeViewItemList.Clear();
            }

            if (TreeViewItemList.Count > 0)
            {
                SetVisualPreviousAndNextForEachItem();
                Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNext = SearchVisualPreviousAndNextItem(TreeViewItemList[^1]);
                VisualLastItem = null;
                if (previousAndNext is not null && previousAndNext.Item2 is not null)
                {
                    VisualLastItem = previousAndNext.Item2;
                }
            }
            #endregion
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
            BaseCompoundJsonTreeViewItem parent = item.Parent;
            BaseCompoundJsonTreeViewItem previousCompound = null;
            BaseCompoundJsonTreeViewItem nextCompound = null;

            if (item.VisualPrevious is BaseCompoundJsonTreeViewItem)
            {
                previousCompound = item.VisualPrevious as BaseCompoundJsonTreeViewItem;
            }
            if (item.VisualNext is BaseCompoundJsonTreeViewItem)
            {
                nextCompound = item.VisualNext as BaseCompoundJsonTreeViewItem;
            }
            #endregion

            #region 处理复合型跟值类型的起始索引与长度

            #region 判定起始行位置
            if (startDocumentLine is null && item.VisualPrevious is BaseCompoundJsonTreeViewItem previousItem1 && previousItem1.EndLine is not null)
            {
                IsCurrentNull = true;
                startDocumentLine = previousItem1.EndLine;
            }
            else
            if (startDocumentLine is null && item.VisualPrevious is not null && item.VisualPrevious.StartLine is not null)
            {
                IsCurrentNull = true;
                startDocumentLine = item.VisualPrevious.StartLine;
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
            #endregion

            startLineText = TextEditor.Document.GetText(startDocumentLine);

            if (!IsCurrentNull)
            {
                if (item is BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem)
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
                                    length = startDocumentLine.EndOffset - offset - (item.VisualNext is not null && item.VisualNext.StartLine is not null ? 1 : 0);
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
                                JsonTreeViewItem lastItem = (item as BaseCompoundJsonTreeViewItem).LogicChildren[^1];
                                if (lastItem is BaseCompoundJsonTreeViewItem lastCompoundItem && lastCompoundItem.EndLine is not null)
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
                                    bool isNeedComma = item.VisualNext is not null && item.VisualNext.StartLine is not null && (item.VisualNext is null || (item.VisualNext is not null && item.VisualNext.StartLine is null));
                                    if (previousCompound is not null && previousCompound.EndLine is not null)
                                    {
                                        offset = previousCompound.EndLine.EndOffset;
                                        length = compoundJsonTreeViewItem.EndLine.EndOffset - offset;
                                    }
                                    else
                                    if (item.VisualNext is not null && item.VisualNext.StartLine is not null)
                                    {
                                        offset = item.VisualNext.StartLine.EndOffset;
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
                                    compoundJsonTreeViewItem.LogicChildren.Clear();
                                    compoundJsonTreeViewItem.EndLine = null;
                                }
                                else
                                {
                                    offset = compoundJsonTreeViewItem.StartLine.EndOffset;
                                    length = compoundJsonTreeViewItem.EndLine.Offset + (compoundJsonTreeViewItem.LayerCount * 2) - offset;
                                    compoundJsonTreeViewItem.LogicChildren.Clear();
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
                        if ((item.VisualNext is not null && item.VisualNext.StartLine is null && item.VisualNext is not null && item.VisualNext.StartLine is null) || (item.VisualNext is null && item.VisualNext is null) && item.IsCanBeDefaulted)
                        {
                            if (item.Parent.ItemType is not ItemType.Array)
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
                    if (item.VisualPrevious is not null && item.VisualPrevious.StartLine is not null)
                    {
                        lastOffset = startLineText.TrimEnd().Length;
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
                if (item.StartLine is null && item.VisualPrevious is not null && item.VisualPrevious.StartLine is not null)
                {
                    item.StartLine = item.VisualPrevious.StartLine.NextLine;
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