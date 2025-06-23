using CBHK.CustomControl.Interfaces;
using CBHK.CustomControl.JsonTreeViewComponents;
using CBHK.Model.Common;
using CBHK.Service.Json;
using ICSharpCode.AvalonEdit.Document;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using static CBHK.Model.Common.Enums;

namespace CBHK.GeneralTool.TreeViewComponentsHelper
{
    public partial class JsonTreeViewItemExtension(IContainerProvider container) : IJsonItemTool
    {
        #region Field
        private IContainerProvider _container = container;

        [GeneratedRegex(@"\[\[\#?((?<1>[\u4e00-\u9fff]+)\|(?<2>[\u4e00-\u9fff]+)|(?<1>[\u4e00-\u9fff]+))\]\]")]
        private static partial Regex GetContextKey();

        [GeneratedRegex(@"\s?\s*\*+\s?\s*{{[nN]bt\sinherit(?<1>[a-z_/|=*\s]+)}}\s?\s*", RegexOptions.IgnoreCase)]
        private static partial Regex GetInheritString();

        private List<string> singleLineElementType = ["string", "bool", "int", "short", "float", "double", "long"];
        #endregion

        #region Method
        public BaseCompoundJsonTreeViewItem GetAddToBottomButtonItem(BaseCompoundJsonTreeViewItem template)
        {
            return new BaseCompoundJsonTreeViewItem(template.Plan, template.JsonItemTool, _container)
            {
                StartLine = template.StartLine,
                LayerCount = template.LayerCount,
                ElementButtonTip = "添加到末尾",
                ItemType = ItemType.BottomButton,
                Parent = template,
                AddOrSwitchElementButtonVisibility = Visibility.Visible,
                SwitchButtonColor = template.PlusColor,
                SwitchButtonIcon = template.PlusIcon,
                PressedSwitchButtonColor = template.PressedPlusColor
            };
        }

        public void RemoveCurrentItem(JsonTreeViewItem jsonTreeViewItem,bool isNeedRemoveItem = true)
        {
            #region Field
            ICustomWorldUnifiedPlan plan = jsonTreeViewItem.Plan;
            JsonTreeViewItem previous = jsonTreeViewItem.VisualPrevious;
            JsonTreeViewItem next = jsonTreeViewItem.VisualNext;
            int offset = 0, length = 0;
            #endregion

            #region 删除可选节点
            if (isNeedRemoveItem)
            {
                jsonTreeViewItem.Parent?.LogicChildren.Remove(jsonTreeViewItem);
            }
            if(jsonTreeViewItem.Parent is not null && jsonTreeViewItem.Parent.LogicChildren[0] is BaseCompoundJsonTreeViewItem firstCompoundItem && firstCompoundItem.DataType is DataType.None)
            {
                jsonTreeViewItem.Parent.LogicChildren.Clear();
            }

            #region 处理排序按钮
            int customItemIndex = 0;
            int notElementItemCount = 0;
            while (jsonTreeViewItem.Parent is not null && customItemIndex < jsonTreeViewItem.Parent.LogicChildren.Count)
            {
                if (jsonTreeViewItem.Parent.LogicChildren[customItemIndex].DisplayText != "Entry")
                {
                    notElementItemCount++;
                }
                customItemIndex++;
            }
            if(jsonTreeViewItem.Parent is not null && jsonTreeViewItem.Parent.LogicChildren.Count - notElementItemCount == 1)
            {
                jsonTreeViewItem.Parent.LogicChildren[0].SortButtonVisibility = Visibility.Collapsed;
            }
            #endregion

            if (jsonTreeViewItem.StartLine is null)
            {
                return;
            }
            if (previous is not null)
            {
                DocumentLine targetLine = null;
                if(previous is BaseCompoundJsonTreeViewItem previousCompoundItem && previousCompoundItem.EndLine is not null)
                {
                    targetLine = previousCompoundItem.EndLine;
                }
                else
                {
                    targetLine = previous.StartLine;
                }
                offset = targetLine.EndOffset;
                if (next is null || (next is not null && next.StartLine is null))
                {
                    offset--;
                }
                if (jsonTreeViewItem is BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.EndLine is not null)
                {
                    length = compoundJsonTreeViewItem.EndLine.EndOffset - offset;
                }
                else
                {
                    length = jsonTreeViewItem.StartLine.EndOffset - offset;
                }
            }
            else
            if(next is not null)
            {
                offset = jsonTreeViewItem.Parent.StartLine.EndOffset;
                if(jsonTreeViewItem is BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.EndLine is not null)
                {
                    length = compoundJsonTreeViewItem.EndLine.EndOffset - offset;
                }
                else
                {
                    length = jsonTreeViewItem.StartLine.EndOffset - offset;
                }    
            }
            else
            if(jsonTreeViewItem.Parent is not null)
            {
                string parentStartLineText = plan.GetRangeText(jsonTreeViewItem.Parent.StartLine.Offset, jsonTreeViewItem.Parent.StartLine.Length);
                string parentEndLineText = plan.GetRangeText(jsonTreeViewItem.Parent.EndLine.Offset, jsonTreeViewItem.Parent.EndLine.Length);
                int startOffset = parentStartLineText.LastIndexOf(':') + 3;
                if(startOffset == 2)
                {
                    startOffset = parentStartLineText.IndexOf('{') + 1;
                }
                if(startOffset == 0)
                {
                    startOffset = parentStartLineText.IndexOf('[') + 1;
                }
                offset = jsonTreeViewItem.Parent.StartLine.Offset + startOffset;
                if(jsonTreeViewItem.Parent.EndLine is not null)
                {
                    int index = parentEndLineText.IndexOf('}');
                    if(index == -1)
                    {
                        index = parentEndLineText.IndexOf(']');
                    }
                    length = jsonTreeViewItem.Parent.EndLine.Offset + index - offset;
                }
            }
            else
            {
                offset = 2;
                if(jsonTreeViewItem is BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.EndLine is not null)
                {
                    length = compoundJsonTreeViewItem.EndLine.EndOffset - 2;
                }
                else
                {
                    length = jsonTreeViewItem.StartLine.EndOffset - 2;
                }
            }

            plan.SetRangeText(offset, length, "");
            int lineCount = plan.GetDocumentLineCount();
            DocumentLine documentEndLine = plan.GetLineByNumber(lineCount);
            string documentString = plan.GetRangeText(0, documentEndLine.EndOffset);
            documentString = documentString.Replace("\r", "").Replace("\n","").Replace(" ", "").Trim();
            if (documentString == "{}" || documentString == "[]")
            {
                plan.SetRangeText(0, documentEndLine.EndOffset, documentString);
            }
            jsonTreeViewItem.StartLine = null;
            if(jsonTreeViewItem is BaseCompoundJsonTreeViewItem compound)
            {
                compound.EndLine = null;
                compound.LogicChildren.Clear();
            }
            //收束父节点
            if(jsonTreeViewItem.Parent is not null && jsonTreeViewItem.Parent.EndLine is not null && jsonTreeViewItem.Parent.EndLine.IsDeleted)
            {
                jsonTreeViewItem.Parent.EndLine = null;
            }
            #endregion
        }

        public List<string> ExtractSubInformationFromPromptSourceCode(BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem)
        {
            Match contextMatch = GetContextKey().Match(compoundJsonTreeViewItem.CompoundChildrenStringList.Count > 0 ? compoundJsonTreeViewItem.CompoundChildrenStringList.FirstOrDefault() : "");
            if(!contextMatch.Success)
            {
                contextMatch = GetContextKey().Match(compoundJsonTreeViewItem.InfoTipText);
            }
            string currentValueTypeString = compoundJsonTreeViewItem.SelectedValueType is not null ? compoundJsonTreeViewItem.SelectedValueType.Text.ToLower() : "";
            string contextKey = '#' + contextMatch.Groups[1].Value;
            List<string> Result = [], targetList = [];
            Dictionary<string, List<string>> targetDictionary = [];
            ICustomWorldUnifiedPlan Plan = compoundJsonTreeViewItem.Plan;

            #region 所有存储结构中依次轮询，找到当前需要的数据
            if (Plan.DependencyItemList.TryGetValue(contextKey, out Result))
            {
            }
            else
            if (Plan.EnumIDDictionary.TryGetValue(contextKey, out targetList))
            {
                Result = [.. targetList];
            }
            else
            if (Plan.TranslateDictionary.TryGetValue(contextKey, out string targetKey))
            {
                if (Plan.DependencyItemList.TryGetValue(targetKey, out targetList))
                {
                }
                else
                if (Plan.TranslateDefaultDictionary.TryGetValue(targetKey, out string defaultKey) && Plan.DependencyItemList.TryGetValue(defaultKey, out targetList))
                {
                }
                else
                if (Plan.EnumIDDictionary.TryGetValue(targetKey, out targetList))
                {
                }
                else
                if (Plan.EnumCompoundDataDictionary.TryGetValue(targetKey, out targetDictionary))
                {
                    targetList = [.. targetDictionary.Keys.Cast<string>()];
                }
                Result = [.. targetList];
            }
            Result ??= [];
            #endregion

            return Result;
        }

        public Tuple<JsonTreeViewItem, JsonTreeViewItem> SetLineNumbersForEachSubItem(ObservableCollection<JsonTreeViewItem> list, BaseCompoundJsonTreeViewItem parent)
        {
            if (parent is not null && parent.StartLine is null)
            {
                if (parent.VisualPrevious is BaseCompoundJsonTreeViewItem baseCompoundJsonTreeViewItem && baseCompoundJsonTreeViewItem.EndLine is not null && !baseCompoundJsonTreeViewItem.EndLine.IsDeleted)
                {
                    parent.StartLine = parent.Plan.GetLineByNumber(baseCompoundJsonTreeViewItem.EndLine.LineNumber + 1);
                }
                else
                if (parent.VisualPrevious is not null && parent.VisualPrevious.StartLine is not null)
                {
                    parent.StartLine = parent.Plan.GetLineByNumber(parent.VisualPrevious.StartLine.LineNumber + 1);
                }
                else
                if (parent.Parent is not null)
                {
                    parent.StartLine = parent.Plan.GetLineByNumber(parent.Parent.StartLine.LineNumber + 1);
                }
                else
                {
                    parent.StartLine = parent.Plan.GetLineByNumber(2);
                }
            }

            int lineNumber = parent is not null && parent.StartLine is not null ? parent.StartLine.LineNumber + 1 : 2;

            JsonTreeViewItem theLastItem = null;
            JsonTreeViewItem theFirstItem = null;
            for (int i = 0; i < list.Count; i++)
            {
                if ((list[i].IsCanBeDefaulted && (list[i].StartLine is null || (list[i].StartLine is not null && list[0].StartLine.IsDeleted))) || ((list[i] is not BaseCompoundJsonTreeViewItem && list[i].DataType is DataType.None) || (list[i] is BaseCompoundJsonTreeViewItem compoundElementItem1 && (compoundElementItem1.DataType is DataType.None || compoundElementItem1.ItemType is ItemType.OptionalCompound || compoundElementItem1.ItemType is ItemType.CustomCompound))))
                {
                    continue;
                }

                list[i].StartLine = list[0].Plan.GetLineByNumber(lineNumber);

                theFirstItem ??= list[i];
                theLastItem = list[i];

                if ((!list[i].IsCanBeDefaulted || (list[i].StartLine is not null && !list[0].StartLine.IsDeleted)) && list[i].DataType is not DataType.None || (list[i] is BaseCompoundJsonTreeViewItem compoundElementItem2 && compoundElementItem2.DataType is not DataType.None && compoundElementItem2.ItemType is not ItemType.CustomCompound && compoundElementItem2.ItemType is not ItemType.OptionalCompound))
                {
                    lineNumber++;
                }
                if (list[i] is BaseCompoundJsonTreeViewItem subCompoundItem2)
                {
                    if (subCompoundItem2.LogicChildren.Count > 0 && (subCompoundItem2.LogicChildren[0] is BaseCompoundJsonTreeViewItem subsubCompoundItem && subsubCompoundItem.ItemType is not ItemType.CustomCompound || subCompoundItem2.LogicChildren[0] is not BaseCompoundJsonTreeViewItem))
                    {
                        Tuple<JsonTreeViewItem, JsonTreeViewItem> result = SetLineNumbersForEachSubItem(subCompoundItem2.LogicChildren, subCompoundItem2);
                        if(result.Item2 is not null)
                        {
                            if (subCompoundItem2.VisualLastChild is not null)
                            {
                                subCompoundItem2.VisualLastChild.VisualNext = result.Item1;
                            }
                            subCompoundItem2.VisualLastChild = result.Item2;
                        }

                        if (result.Item2 is BaseCompoundJsonTreeViewItem subsubCompoundItem2)
                        {
                            subCompoundItem2.EndLine = subsubCompoundItem2.EndLine.NextLine;
                        }
                        else
                        if (result is not null && result.Item2.StartLine is not null)
                        {
                            subCompoundItem2.EndLine = result.Item2.StartLine.NextLine;
                        }
                        else
                        if(result is not null)
                        {
                            subCompoundItem2.EndLine = subCompoundItem2.Plan.GetLineByNumber(subCompoundItem2.StartLine.LineNumber + subCompoundItem2.LogicChildren.Count);
                        }
                    }
                    else
                    {
                        subCompoundItem2.EndLine = subCompoundItem2.StartLine;
                    }
                    if (subCompoundItem2.StartLine != subCompoundItem2.EndLine && subCompoundItem2.EndLine is not null)
                    {
                        lineNumber = subCompoundItem2.EndLine.LineNumber + 1;
                    }
                }
            }

            return new(theFirstItem, theLastItem);
        }

        public Tuple<JsonTreeViewItem, JsonTreeViewItem> SetLineNumbersForEachSubItem(ObservableCollection<JsonTreeViewItem> list, int lineNumber)
        {
            JsonTreeViewItem theFirstItem = null;
            JsonTreeViewItem theLastItem = null;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].IsCanBeDefaulted || ((list[i] is not BaseCompoundJsonTreeViewItem && list[i].DataType is  DataType.None) || (list[i] is BaseCompoundJsonTreeViewItem compoundElementItem1 && (compoundElementItem1.DataType is DataType.None || compoundElementItem1.ItemType is ItemType.OptionalCompound || compoundElementItem1.ItemType is ItemType.CustomCompound))))
                {
                    continue;
                }

                list[i].StartLine = list[0].Plan.GetLineByNumber(lineNumber);

                theFirstItem ??= list[i];
                theLastItem = list[i];

                if (!list[i].IsCanBeDefaulted && list[i].DataType is not DataType.None || (list[i] is BaseCompoundJsonTreeViewItem compoundElementItem2 && compoundElementItem2.DataType is not DataType.None && compoundElementItem2.ItemType is not ItemType.CustomCompound && compoundElementItem2.ItemType is not ItemType.OptionalCompound))
                {
                    lineNumber++;
                }
                if (list[i] is BaseCompoundJsonTreeViewItem subCompoundItem2)
                {
                    if (subCompoundItem2.LogicChildren.Count > 0 && (subCompoundItem2.LogicChildren[0] is BaseCompoundJsonTreeViewItem subsubCompoundItem && subsubCompoundItem.ItemType is not ItemType.CustomCompound || subCompoundItem2.LogicChildren[0] is not BaseCompoundJsonTreeViewItem))
                    {
                        Tuple<JsonTreeViewItem, JsonTreeViewItem> result = SetLineNumbersForEachSubItem(subCompoundItem2.LogicChildren, subCompoundItem2);

                        if (result is not null)
                        {
                            if(subCompoundItem2.VisualLastChild is not null)
                            {
                                subCompoundItem2.VisualLastChild.VisualNext = result.Item2;
                            }
                            subCompoundItem2.VisualLastChild = result.Item2;
                        }
                        if (result.Item2 is BaseCompoundJsonTreeViewItem subsubCompoundItem2)
                        {
                            subCompoundItem2.EndLine = subsubCompoundItem2.EndLine.NextLine;
                        }
                        else
                        if (result is not null && result.Item2.StartLine is not null)
                        {
                            subCompoundItem2.EndLine = result.Item2.StartLine.NextLine;
                        }
                        else
                        if (result is not null)
                        {
                            subCompoundItem2.EndLine = subCompoundItem2.Plan.GetLineByNumber(subCompoundItem2.StartLine.LineNumber + subCompoundItem2.LogicChildren.Count);
                        }
                    }
                    else
                    {
                        subCompoundItem2.EndLine = subCompoundItem2.StartLine;
                    }
                    if (subCompoundItem2.StartLine != subCompoundItem2.EndLine && subCompoundItem2.EndLine is not null)
                    {
                        lineNumber = subCompoundItem2.EndLine.LineNumber + 1;
                    }
                }
            }

            return new(theFirstItem, theLastItem);
        }

        public void SetLayerCountForEachItem(ObservableCollection<JsonTreeViewItem> list,int startCount)
        {
            foreach (var item in list)
            {
                if(item is BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem1 && compoundJsonTreeViewItem1.ItemType is ItemType.CustomCompound)
                {
                    item.LayerCount = item.Parent.LayerCount;
                }
                else
                {
                    item.LayerCount = startCount;
                }
                if (item is BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem2 && compoundJsonTreeViewItem2.LogicChildren.Count > 0)
                {
                    SetLayerCountForEachItem(compoundJsonTreeViewItem2.LogicChildren, compoundJsonTreeViewItem2.LayerCount + 1);
                }
            }
        }

        public void SetParentForEachItem(ObservableCollection<JsonTreeViewItem> list, BaseCompoundJsonTreeViewItem currentParent,int startIndex = 0)
        {
            for (int i = 0; i < list.Count; i++)
            {
                list[i].Parent = currentParent;
                if (list[i] is not BaseCompoundJsonTreeViewItem || (list[i] is BaseCompoundJsonTreeViewItem subBaseCompoundItem && subBaseCompoundItem.ItemType is not ItemType.BottomButton && subBaseCompoundItem.ItemType is not ItemType.CustomCompound))
                {
                    if (startIndex > 0)
                    {
                        list[i].Index = startIndex;
                        startIndex++;
                    }
                    else
                    {
                        list[i].Index = i;
                    }
                }
                if (list[i] is BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.LogicChildren.Count > 0)
                {
                    BaseCompoundJsonTreeViewItem.SetVisualPreviousAndNextForEachItem(compoundJsonTreeViewItem.LogicChildren);
                    if (currentParent is not null)
                    {
                        Tuple<JsonTreeViewItem, JsonTreeViewItem> result = compoundJsonTreeViewItem.SearchVisualPreviousAndNextItem(currentParent,false);
                        currentParent.VisualLastChild = result.Item2;
                    }
                    
                    SetParentForEachItem(compoundJsonTreeViewItem.LogicChildren, compoundJsonTreeViewItem);
                }
            }
        }

        public void AddSubStructure(BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem)
        {
            #region Field
            ItemType currentItemType = compoundJsonTreeViewItem.ItemType;
            ItemType parentItemType = compoundJsonTreeViewItem.Parent is not null ? compoundJsonTreeViewItem.Parent.ItemType : ItemType.Compound;
            //当可以是多种数据类型时，采用当前选择的类型
            if(currentItemType is ItemType.MultiType)
            {
                currentItemType = (ItemType)Enum.Parse(typeof(ItemType), compoundJsonTreeViewItem.SelectedValueType.Text);
            }
            if(parentItemType is ItemType.MultiType && compoundJsonTreeViewItem.Parent is not null)
            {
                parentItemType = (ItemType)Enum.Parse(typeof(ItemType), compoundJsonTreeViewItem.Parent.SelectedValueType.Text);
            }
            bool currentIsList = currentItemType is ItemType.List ||currentItemType is ItemType.Array;

            bool parentIsList = parentItemType is ItemType.Array || parentItemType is ItemType.List;

            bool currentIsCompound = currentItemType is ItemType.Compound ||
                currentItemType is ItemType.CustomCompound ||
                currentItemType is ItemType.OptionalCompound;
            JsonTreeViewDataStructure result = new();
            List<string> CurrentChildrenStringList = [];
            List<string> CurrentDependencyItemList = [];
            int customItemCount = 0;
            BaseCompoundJsonTreeViewItem targetCompoundItem;
            if (currentIsList || currentIsCompound)
            {
                targetCompoundItem = compoundJsonTreeViewItem;
            }
            else
            if (parentIsList)
            {
                targetCompoundItem = compoundJsonTreeViewItem.Parent;
            }
            if (currentIsCompound)
            {
                if(compoundJsonTreeViewItem.ItemType is ItemType.CustomCompound && compoundJsonTreeViewItem.SelectedEnumItem is null && (compoundJsonTreeViewItem.Value is null || (compoundJsonTreeViewItem.Value is not null && (compoundJsonTreeViewItem.Value + "").Length == 0)))
                {
                    return;
                }
            }

            HtmlHelper htmlHelper = new(_container)
            {
                plan = compoundJsonTreeViewItem.Plan,
                jsonTool = compoundJsonTreeViewItem.JsonItemTool
            };
            #endregion

            #region 定位相邻的已有值的两个节点
            JsonTreeViewItem previous = compoundJsonTreeViewItem.VisualPrevious;
            BaseCompoundJsonTreeViewItem parent = compoundJsonTreeViewItem.Parent;
            JsonTreeViewItem next = compoundJsonTreeViewItem.VisualNext;
            BaseCompoundJsonTreeViewItem previousCompound = null;
            BaseCompoundJsonTreeViewItem nextCompound = null;

            if (previous is BaseCompoundJsonTreeViewItem)
            {
                previousCompound = previous as BaseCompoundJsonTreeViewItem;
            }
            if (next is BaseCompoundJsonTreeViewItem)
            {
                nextCompound = next as BaseCompoundJsonTreeViewItem;
            }
            #endregion

            #region 计算有多少个自定义子节点
            if (parent is not null && parent.LogicChildren.Count > 1 && parent.LogicChildren[1] is BaseCompoundJsonTreeViewItem subCompoundItem2 && subCompoundItem2.ItemType is ItemType.CustomCompound)
            {
                customItemCount = 2;
            }
            else
            if (parent is not null && parent.LogicChildren.Count > 0 && parent.LogicChildren[0] is BaseCompoundJsonTreeViewItem subCompoundItem1 && subCompoundItem1.ItemType is ItemType.CustomCompound)
            {
                customItemCount = 1;
            }
            #endregion

            #region 判断是在添加枚举型结构还是展开复合节点
            bool addMoreCustomStructure = currentItemType is ItemType.CustomCompound && parent is not null && customItemCount > 0;

            bool addMoreStructure = currentItemType is ItemType.OptionalCompound || currentItemType is ItemType.Compound;

            bool addListStructure = currentItemType is ItemType.List;

            bool addParentListStructure = currentItemType is not ItemType.List && parent is not null && (parent.ItemType is ItemType.List || (parent.ItemType is  ItemType.MultiType && parent.SelectedValueType is not null && parent.SelectedValueType.Text == "List"));
            #endregion

            #region 若没有子信息，则分析当前的提示源文本中是否有上下文数据
            if(compoundJsonTreeViewItem.CompoundChildrenStringList.Count > 0)
            {
                CurrentChildrenStringList = compoundJsonTreeViewItem.CompoundChildrenStringList;
            }
            else
            if(compoundJsonTreeViewItem.ListChildrenStringList.Count > 0)
            {
                CurrentChildrenStringList = compoundJsonTreeViewItem.ListChildrenStringList;
            }
            else
            if(compoundJsonTreeViewItem.ArrayChildrenStringList.Count > 0)
            {
                CurrentChildrenStringList = compoundJsonTreeViewItem.ArrayChildrenStringList;
            }
            else
            {
                Match contextMatch = GetContextKey().Match(compoundJsonTreeViewItem.InfoTipText);
                if (contextMatch.Success)
                {
                    string currentKey = '#' + contextMatch.Groups[1].Value;
                    ICustomWorldUnifiedPlan plan = compoundJsonTreeViewItem.Plan;
                    if (plan.DependencyItemList.TryGetValue(currentKey, out List<string> targetList1))
                    {
                        CurrentChildrenStringList = targetList1;
                    }
                    else
                    if (plan.TranslateDictionary.TryGetValue(currentKey, out string targetKey))
                    {
                        if (plan.DependencyItemList.TryGetValue(targetKey, out List<string> targetList2))
                        {
                            CurrentChildrenStringList = targetList2;
                        }
                        else
                        if (plan.EnumIDDictionary.TryGetValue(targetKey, out List<string> targetList3))
                        {
                            CurrentChildrenStringList = targetList3;
                        }
                        else
                        if (plan.EnumCompoundDataDictionary.TryGetValue(targetKey, out Dictionary<string, List<string>> targetDictionary))
                        {
                            bool isNeedValueType = (compoundJsonTreeViewItem.SelectedValueType is not null && compoundJsonTreeViewItem.SelectedValueType.Text == "List") || (compoundJsonTreeViewItem.SelectedEnumItem is not null && compoundJsonTreeViewItem.SelectedEnumItem.Text == "List");
                            bool isNeedTargetValue = (compoundJsonTreeViewItem.SelectedValueType is not null && compoundJsonTreeViewItem.SelectedValueType.Text == "Compound") || (compoundJsonTreeViewItem.SelectedEnumItem is not null && compoundJsonTreeViewItem.SelectedEnumItem.Text == "Compound");
                            List<string> targetList4 = [];
                            if (isNeedTargetValue && plan.TranslateDefaultDictionary.TryGetValue(targetKey, out string targetEnumKey))
                            {
                                if (targetDictionary.TryGetValue(targetEnumKey, out targetList4))
                                {
                                    CurrentChildrenStringList = targetList4[1..];
                                }
                                else
                                if (plan.DependencyItemList.TryGetValue(targetEnumKey, out targetList4))
                                {
                                    CurrentChildrenStringList = targetList4;
                                }
                            }
                            if (isNeedValueType)
                            {
                                CurrentChildrenStringList = ["*{{nbt " + targetKey.Trim('#') + "}}[[" + targetKey + "]]"];
                            }
                        }
                    }
                }

                if (CurrentChildrenStringList.Count == 0)
                {
                    if (compoundJsonTreeViewItem.EnumKey.Length > 0 && compoundJsonTreeViewItem.ItemType is ItemType.CustomCompound)
                    {
                        if (compoundJsonTreeViewItem.Plan.EnumCompoundDataDictionary.TryGetValue(compoundJsonTreeViewItem.EnumKey, out Dictionary<string, List<string>> targetDictionary) && targetDictionary.TryGetValue(compoundJsonTreeViewItem.SelectedEnumItem.Text.Trim('!'), out List<string> targetList))
                        {
                            CurrentChildrenStringList = [.. targetList];
                        }
                    }
                    else
                    if (compoundJsonTreeViewItem.ItemType is ItemType.BottomButton && compoundJsonTreeViewItem.Parent is not null)
                    {
                        CurrentChildrenStringList = compoundJsonTreeViewItem.Parent.ListChildrenStringList;
                        if (CurrentChildrenStringList.Count == 0)
                        {
                            CurrentChildrenStringList = compoundJsonTreeViewItem.Parent.ArrayChildrenStringList;
                        }
                    }
                }
            }
            #endregion

            #region 执行一系列添加操作
            if ((parentIsList || currentIsList || currentIsCompound) && CurrentChildrenStringList.Count > 0)
            {
                #region 提取需要分析的成员(仅提取子信息而不进行源码分析)
                if (parent is not null && parentItemType is ItemType.Compound && parent.DisplayText == "Entry" && previous is not null && previous is BaseCompoundJsonTreeViewItem previousCompoundItem && previousCompoundItem.EnumKey.Length > 0 && CurrentChildrenStringList.Count == 0)
                {
                    CurrentDependencyItemList = [..parent.Parent.CompoundChildrenStringList];
                }

                if (CurrentDependencyItemList.Count == 0)
                {
                    CurrentDependencyItemList = [..CurrentChildrenStringList];
                }
                if (compoundJsonTreeViewItem.CompoundChildrenStringList.Count == 1 && compoundJsonTreeViewItem.SelectedValueType is not null && compoundJsonTreeViewItem.SelectedValueType.Text != "List" && compoundJsonTreeViewItem.ValueTypeSource.Count > 0)
                {
                    List<string> listOrCompoundResult = ExtractSubInformationFromPromptSourceCode(compoundJsonTreeViewItem);
                    CurrentDependencyItemList = listOrCompoundResult;
                }
                #endregion

                #region 执行解析
                string currentReferenceString = compoundJsonTreeViewItem.ItemType is ItemType.CustomCompound ? compoundJsonTreeViewItem.Value + "" : "";
                if(currentReferenceString.Length == 0 && compoundJsonTreeViewItem.SelectedEnumItem is not null && compoundJsonTreeViewItem.SelectedEnumItem.Text != "- unset -")
                {
                    currentReferenceString = compoundJsonTreeViewItem.SelectedEnumItem.Text;
                }

                result = htmlHelper.GetTreeViewItemResult(new(), CurrentDependencyItemList, compoundJsonTreeViewItem.LayerCount + 1, currentReferenceString, currentItemType is not ItemType.BottomButton ? compoundJsonTreeViewItem : parent, null, 1, true);

                htmlHelper.HandlingTheTypingAppearanceOfCompositeItemList(result.Result, compoundJsonTreeViewItem);

                while (result.ResultString.Length > 0 &&
                    (result.ResultString[^1] == ',' ||
                     result.ResultString[^1] == '\r' ||
                     result.ResultString[^1] == '\n' ||
                     result.ResultString[^1] == ' '))
                {
                    result.ResultString.Length--;
                }
                #endregion

                #region 添加当前节点的子节点集
                if (currentItemType is ItemType.CustomCompound && parent is not null)
                {
                    parent.InsertChild(customItemCount, result);
                }
                else
                if (addListStructure || addParentListStructure)
                {
                    if (compoundJsonTreeViewItem.LogicChildren.Count == 0 && currentItemType is not ItemType.BottomButton)
                    {
                        compoundJsonTreeViewItem.LogicChildren.Add(GetAddToBottomButtonItem(compoundJsonTreeViewItem));
                    }

                    List<string> NBTFeatureList = htmlHelper.GetHeadTypeAndKeyList(CurrentChildrenStringList[0]);
                    NBTFeatureList = HtmlHelper.RemoveUIMarker(NBTFeatureList);

                    if ((currentIsList || parentIsList)/* && ((compoundJsonTreeViewItem.ItemType is ItemType.MultiType && compoundJsonTreeViewItem.SelectedValueType is not null && compoundJsonTreeViewItem.SelectedValueType.Text == "List") || (NBTFeatureList.Count == 1 && NBTFeatureList[0] == "compound"))*/)
                    {
                        if (currentItemType is not ItemType.BottomButton)
                        {
                            compoundJsonTreeViewItem.InsertChild(0, result);
                        }
                        else
                        {
                            parent.InsertChild(parent.LogicChildren.Count - 1, result);
                        }
                    }
                    //else
                    //{
                    //    if (currentItemType is not ItemType.BottomButton)
                    //    {
                    //        compoundJsonTreeViewItem.InsertChild(0, result);
                    //    }
                    //    else
                    //    if(parent is not null)
                    //    {
                    //        compoundJsonTreeViewItem.Parent.InsertChild(compoundJsonTreeViewItem.Parent.LogicChildren.Count - 1, result);
                    //    }
                    //}
                }
                else
                {
                    compoundJsonTreeViewItem.AddChildrenList(result);
                }
                #endregion

                #region 显示换位按钮
                if (currentIsList && compoundJsonTreeViewItem.LogicChildren.Count > 2)
                {
                    compoundJsonTreeViewItem.LogicChildren[0].SortButtonVisibility = Visibility.Visible;
                    if (compoundJsonTreeViewItem.LogicChildren.Count == 3)
                    {
                        compoundJsonTreeViewItem.LogicChildren[^2].SortButtonVisibility = Visibility.Visible;
                    }
                }
                if (parentIsList && !currentIsList && parent is not null && parent.LogicChildren.Count > 2)
                {
                    if ((result.Result[0].Parent is not null && result.Result[0].Parent.DisplayText != "Entry") || result.Result[0].Parent is null)
                    {
                        result.Result[0].SortButtonVisibility = Visibility.Visible;
                    }
                    else
                    if (result.Result[0].Parent is not null)
                    {
                        result.Result[0].Parent.SortButtonVisibility = Visibility.Visible;
                    }
                    if (parent.LogicChildren.Count == 3)
                    {
                        parent.LogicChildren[0].SortButtonVisibility = Visibility.Visible;
                    }
                }
                #endregion
            }
            #endregion
        }

        public void CollapseCurrentItem(BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem)
        {
            if (compoundJsonTreeViewItem.IsCanBeDefaulted)
            {
                RemoveCurrentItem(compoundJsonTreeViewItem, compoundJsonTreeViewItem.RemoveElementButtonVisibility is Visibility.Visible);
            }
            else
            {
                int offset = compoundJsonTreeViewItem.StartLine.EndOffset;
                string compoundEndLineText = compoundJsonTreeViewItem.Plan.GetRangeText(compoundJsonTreeViewItem.EndLine.Offset, compoundJsonTreeViewItem.EndLine.Length);
                int lastCharIndex = compoundEndLineText.LastIndexOf(']');
                if (lastCharIndex == -1)
                {
                    lastCharIndex = compoundEndLineText.LastIndexOf('}');
                }
                int length = compoundJsonTreeViewItem.EndLine.Offset + lastCharIndex - offset;
                compoundJsonTreeViewItem.Plan.SetRangeText(offset, length, "");
                compoundJsonTreeViewItem.EndLine = compoundJsonTreeViewItem.StartLine;
            }
        }

        public void RecursiveTraverseAndRunOperate(ObservableCollection<JsonTreeViewItem> jsonItemList, Action<JsonTreeViewItem> action)
        {
            foreach (var item in jsonItemList)
            {
                action.Invoke(item);
                if (item is BaseCompoundJsonTreeViewItem compoundJsonTreeViewItem)
                {
                    RecursiveTraverseAndRunOperate(jsonItemList, action);
                }
            }
        }
        #endregion
    }
}