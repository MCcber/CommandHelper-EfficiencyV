﻿using CBHK.CustomControl.Interfaces;
using CBHK.CustomControl.JsonTreeViewComponents;
using CBHK.Service.Json;
using CBHK.Model.Common;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using static CBHK.Model.Common.Enums;
using ICSharpCode.AvalonEdit.Document;
using System.Linq;

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
        public void RecursiveRemoveFlattenDescendantNodeList(CompoundJsonTreeViewItem CurrentParent, CompoundJsonTreeViewItem Target)
        {
            foreach (var item in Target.Children)
            {
                CurrentParent.FlattenDescendantNodeList.Remove(item);
                if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                {
                    RecursiveRemoveFlattenDescendantNodeList(compoundJsonTreeViewItem.Parent, compoundJsonTreeViewItem);
                }
            }
        }

        public Tuple<JsonTreeViewItem, JsonTreeViewItem> LocateTheNodesOfTwoAdjacentExistingValues(JsonTreeViewItem previous, JsonTreeViewItem next)
        {
            while (previous is not null && previous.StartLine is null)
            {
                if (previous.Previous is null)
                {
                    break;
                }
                previous = previous.Previous;
            }

            while (next is not null && next.StartLine is null)
            {
                if (next.Next is null)
                {
                    break;
                }
                next = next.Next;
            }
            return new(previous, next);
        }

        public void RemoveCurrentItem(JsonTreeViewItem jsonTreeViewItem,bool isNeedRemoveItem = true)
        {
            #region Field
            ICustomWorldUnifiedPlan plan = jsonTreeViewItem.Plan;
            Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNextItem = LocateTheNodesOfTwoAdjacentExistingValues(jsonTreeViewItem.Previous, jsonTreeViewItem.Next);
            JsonTreeViewItem previous = previousAndNextItem.Item1;
            JsonTreeViewItem next = previousAndNextItem.Item2;
            int offset = 0, length = 0;
            #endregion

            #region 删除可选节点
            if (isNeedRemoveItem)
            {
                jsonTreeViewItem.Parent?.Children.Remove(jsonTreeViewItem);
            }
            if(jsonTreeViewItem.Parent is not null && jsonTreeViewItem.Parent.Children[0] is CompoundJsonTreeViewItem firstCompoundItem && firstCompoundItem.DataType is DataType.None)
            {
                jsonTreeViewItem.Parent.Children.Clear();
            }

            #region 处理排序按钮
            int customItemIndex = 0;
            int notElementItemCount = 0;
            while (jsonTreeViewItem.Parent is not null && customItemIndex < jsonTreeViewItem.Parent.Children.Count)
            {
                if (jsonTreeViewItem.Parent.Children[customItemIndex].DisplayText != "Entry")
                {
                    notElementItemCount++;
                }
                customItemIndex++;
            }
            if(jsonTreeViewItem.Parent is not null && jsonTreeViewItem.Parent.Children.Count - notElementItemCount == 1)
            {
                jsonTreeViewItem.Parent.Children[0].SortButtonVisibility = Visibility.Collapsed;
            }
            #endregion

            if (jsonTreeViewItem.StartLine is null)
            {
                return;
            }
            if (previous is not null && previous.StartLine is not null)
            {
                DocumentLine targetLine = null;
                if(previous is CompoundJsonTreeViewItem previousCompoundItem && previousCompoundItem.EndLine is not null)
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
                if (jsonTreeViewItem is CompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.EndLine is not null)
                {
                    length = compoundJsonTreeViewItem.EndLine.EndOffset - offset;
                }
                else
                {
                    length = jsonTreeViewItem.StartLine.EndOffset - offset;
                }
            }
            else
            if(next is not null && next.StartLine is not null)
            {
                offset = jsonTreeViewItem.Parent.StartLine.EndOffset;
                if(jsonTreeViewItem is CompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.EndLine is not null)
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
                if(jsonTreeViewItem is CompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.EndLine is not null)
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
            if(jsonTreeViewItem is CompoundJsonTreeViewItem compound)
            {
                compound.EndLine = null;
                compound.Children.Clear();
            }
            //收束父节点
            if(jsonTreeViewItem.Parent is not null && jsonTreeViewItem.Parent.EndLine is not null && jsonTreeViewItem.Parent.EndLine.IsDeleted)
            {
                jsonTreeViewItem.Parent.EndLine = null;
            }
            #endregion
        }

        public JsonTreeViewItem SearchForTheLastItemWithRowReference(CompoundJsonTreeViewItem compoundJsonTreeViewItem)
        {
            JsonTreeViewItem result = null;
            int maxValue = 0;
            for (int i = compoundJsonTreeViewItem.Children.Count - 1; i >= 0; i--)
            {
                if ((compoundJsonTreeViewItem.Children[i] is CompoundJsonTreeViewItem currentCompoundItem && currentCompoundItem.DataType is DataType.None) && compoundJsonTreeViewItem.Children[i].DataType is DataType.None && compoundJsonTreeViewItem.Children[i] is CompoundJsonTreeViewItem childCompoundItem && childCompoundItem.DataType is DataType.None)
                {
                    continue;
                }
                if (compoundJsonTreeViewItem.Children[i] is CompoundJsonTreeViewItem subCompoundItem && subCompoundItem.EndLine is not null && !subCompoundItem.EndLine.IsDeleted)
                {
                    if (maxValue < subCompoundItem.EndLine.LineNumber)
                    {
                        result = subCompoundItem;
                        maxValue = subCompoundItem.EndLine.LineNumber;
                    }
                }
                else
                if (compoundJsonTreeViewItem.Children[i].StartLine is not null)
                {
                    if (maxValue < compoundJsonTreeViewItem.Children[i].StartLine.LineNumber)
                    {
                        result = compoundJsonTreeViewItem.Children[i];
                        maxValue = compoundJsonTreeViewItem.Children[i].StartLine.LineNumber;
                    }
                }
            }
            return result;
        }

        public List<string> ExtractSubInformationFromPromptSourceCode(CompoundJsonTreeViewItem compoundJsonTreeViewItem)
        {
            Match contextMatch = GetContextKey().Match(compoundJsonTreeViewItem.ChildrenStringList.Count > 0 ? compoundJsonTreeViewItem.ChildrenStringList.FirstOrDefault() : "");
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

        public void SetLineNumbersForEachItem(ObservableCollection<JsonTreeViewItem> list, CompoundJsonTreeViewItem parent, bool withType = false)
        {
            int index = parent is not null && parent.StartLine is not null ? parent.StartLine.LineNumber + 1 : 2;

            if (withType)
            {
                index = parent is not null && parent.StartLine is not null ? parent.StartLine.LineNumber + 2 : 1;
                withType = false;
            }

            for (int i = 0; i < list.Count; i++)
            {
                list[i].Parent ??= parent;

                if (list[i].IsCanBeDefaulted || ((list[i] is not CompoundJsonTreeViewItem && list[i].DataType is DataType.None) || (list[i] is CompoundJsonTreeViewItem compoundElementItem1 && (compoundElementItem1.DataType is DataType.None || compoundElementItem1.DataType is DataType.OptionalCompound || compoundElementItem1.DataType is DataType.CustomCompound))))
                {
                    continue;
                }

                list[i].StartLine = list[0].Plan.GetLineByNumber(index);

                if (!list[i].IsCanBeDefaulted && list[i].DataType is not DataType.None || (list[i] is CompoundJsonTreeViewItem compoundElementItem2 && compoundElementItem2.DataType is not DataType.None && compoundElementItem2.DataType is not DataType.CustomCompound && compoundElementItem2.DataType is not DataType.OptionalCompound))
                {
                    index++;
                }
                if (list[i] is CompoundJsonTreeViewItem subCompoundItem2)
                {
                    if (subCompoundItem2.Children.Count > 0 && (subCompoundItem2.Children[0] is CompoundJsonTreeViewItem subsubCompoundItem && subsubCompoundItem.DataType is not DataType.CustomCompound || subCompoundItem2.Children[0] is not CompoundJsonTreeViewItem))
                    {
                        SetLineNumbersForEachItem(subCompoundItem2.Children, subCompoundItem2);
                        JsonTreeViewItem subItem = SearchForTheLastItemWithRowReference(subCompoundItem2);
                        if (subItem is CompoundJsonTreeViewItem subsubCompoundItem2)
                        {
                            subCompoundItem2.EndLine = subsubCompoundItem2.EndLine.NextLine;
                        }
                        else
                        if (subItem is not null && subItem.StartLine is not null)
                        {
                            subCompoundItem2.EndLine = subItem.StartLine.NextLine;
                        }
                        else
                        if(subItem is not null)
                        {
                            subCompoundItem2.EndLine = subCompoundItem2.Plan.GetLineByNumber(subCompoundItem2.StartLine.LineNumber + subCompoundItem2.Children.Count);
                        }
                    }
                    else
                    {
                        subCompoundItem2.EndLine = subCompoundItem2.StartLine;
                    }
                    if (subCompoundItem2.StartLine != subCompoundItem2.EndLine && subCompoundItem2.EndLine is not null)
                    {
                        index = subCompoundItem2.EndLine.LineNumber + 1;
                    }
                }
            }
        }

        public void SetLayerCountForEachItem(ObservableCollection<JsonTreeViewItem> list,int startCount)
        {
            foreach (var item in list)
            {
                if(item is CompoundJsonTreeViewItem compoundJsonTreeViewItem1 && compoundJsonTreeViewItem1.DataType is DataType.CustomCompound)
                {
                    item.LayerCount = item.Parent.LayerCount;
                }
                else
                {
                    item.LayerCount = startCount;
                }
                if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem2 && compoundJsonTreeViewItem2.Children.Count > 0)
                {
                    SetLayerCountForEachItem(compoundJsonTreeViewItem2.Children, compoundJsonTreeViewItem2.LayerCount + 1);
                }
            }
        }

        public void SetParentForEachItem(ObservableCollection<JsonTreeViewItem> list, CompoundJsonTreeViewItem startParent)
        {
            foreach (var item in list)
            {
                item.Parent = startParent;
                if(item is CompoundJsonTreeViewItem compoundJsonTreeViewItem && compoundJsonTreeViewItem.Children.Count > 0)
                {
                    SetParentForEachItem(compoundJsonTreeViewItem.Children, compoundJsonTreeViewItem);
                }
            }
        }

        public void AddSubStructure(CompoundJsonTreeViewItem compoundJsonTreeViewItem)
        {
            #region Field
            bool isNotNeedNewLine = false;
            DataType currentDataType = compoundJsonTreeViewItem.DataType;
            DataType parentDataType = compoundJsonTreeViewItem.Parent is not null ? compoundJsonTreeViewItem.Parent.DataType : DataType.None;
            //当可以是多种数据类型时，采用当前选择的类型
            if(currentDataType is DataType.MultiType)
            {
                currentDataType = (DataType)Enum.Parse(typeof(DataType), compoundJsonTreeViewItem.SelectedValueType.Text);
            }
            if(parentDataType is DataType.MultiType && compoundJsonTreeViewItem.Parent is not null)
            {
                parentDataType = (DataType)Enum.Parse(typeof(DataType), compoundJsonTreeViewItem.Parent.SelectedValueType.Text);
            }
            bool currentIsList = currentDataType is DataType.List ||currentDataType is DataType.Array;

            bool parentIsList = parentDataType is DataType.Array || parentDataType is DataType.List;

            bool currentIsCompound = currentDataType is DataType.Compound ||
                currentDataType is DataType.CustomCompound ||
                currentDataType is DataType.OptionalCompound;
            JsonTreeViewDataStructure result = new();
            List<string> CurrentChildrenStringList = [];
            List<string> CurrentDependencyItemList = [];
            int customItemCount = 0;
            ChangeType changeType = ChangeType.String;
            CompoundJsonTreeViewItem targetCompoundItem;
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
                changeType = ChangeType.AddCompoundObject;
                if(compoundJsonTreeViewItem.DataType is DataType.CustomCompound && compoundJsonTreeViewItem.SelectedEnumItem is null && (compoundJsonTreeViewItem.Value is null || (compoundJsonTreeViewItem.Value is not null && (compoundJsonTreeViewItem.Value + "").Length == 0)))
                {
                    return;
                }
            }
            else
            if (currentIsList)
            {
                changeType = ChangeType.AddListElement;
            }
            else
            if (parentIsList)
            {
                changeType = ChangeType.AddListElementToEnd;
            }

            HtmlHelper htmlHelper = new(_container)
            {
                plan = compoundJsonTreeViewItem.Plan,
                jsonTool = compoundJsonTreeViewItem.JsonItemTool
            };
            #endregion

            #region 定位相邻的已有值的两个节点
            JsonTreeViewItem previous = compoundJsonTreeViewItem.Previous;
            CompoundJsonTreeViewItem parent = compoundJsonTreeViewItem.Parent;
            JsonTreeViewItem next = compoundJsonTreeViewItem.Next;
            CompoundJsonTreeViewItem previousCompound = null;
            CompoundJsonTreeViewItem nextCompound = null;

            Tuple<JsonTreeViewItem, JsonTreeViewItem> previousAndNextItem = LocateTheNodesOfTwoAdjacentExistingValues(previous, next);
            previous = previousAndNextItem.Item1;
            next = previousAndNextItem.Item2;

            if (previous is CompoundJsonTreeViewItem)
            {
                previousCompound = previous as CompoundJsonTreeViewItem;
            }
            if (next is CompoundJsonTreeViewItem)
            {
                nextCompound = next as CompoundJsonTreeViewItem;
            }
            #endregion

            #region 计算有多少个自定义子节点
            if (parent is not null && parent.Children.Count > 1 && parent.Children[1] is CompoundJsonTreeViewItem subCompoundItem2 && subCompoundItem2.DataType is DataType.CustomCompound)
            {
                customItemCount = 2;
            }
            else
            if (parent is not null && parent.Children.Count > 0 && parent.Children[0] is CompoundJsonTreeViewItem subCompoundItem1 && subCompoundItem1.DataType is DataType.CustomCompound)
            {
                customItemCount = 1;
            }
            #endregion

            #region 判断是在添加枚举型结构还是展开复合节点
            bool addMoreCustomStructure = currentDataType is DataType.CustomCompound && parent is not null && customItemCount > 0;

            bool addMoreStructure = currentDataType is DataType.OptionalCompound || currentDataType is DataType.Compound;

            bool addListStructure = currentDataType is DataType.List;

            bool addParentListStructure = currentDataType is not DataType.List && parent is not null && (parent.DataType is DataType.List || (parent.DataType is DataType.MultiType && parent.SelectedValueType is not null && parent.SelectedValueType.Text == "List"));
            #endregion

            #region 定义前置与末尾衔接符
            string connectorSymbol = "\r\n";
            string endConnectorSymbol = "";
            #endregion

            #region 计算前置衔接符
            bool isExpendOptionalCompound = currentDataType is DataType.OptionalCompound && (previous is not null && previous.StartLine is not null && (next is null || (next is not null && next.StartLine is null)));

            bool isParentAddElement = addParentListStructure;

            bool isAddListItem = compoundJsonTreeViewItem.StartLine is null && compoundJsonTreeViewItem.EndLine is null && currentDataType is DataType.List && previous is not null && previous.StartLine is not null && (next is null || (next is not null && next.StartLine is null));

            if (isExpendOptionalCompound || isParentAddElement || isAddListItem)
            {
                connectorSymbol = ",\r\n";
            }
            #endregion

            #region 计算后置衔接符
            CompoundJsonTreeViewItem endConnectorSymbolItem = null;
            if (addMoreCustomStructure || addParentListStructure)
            {
                endConnectorSymbolItem = parent;
            }
            else
            if (addMoreStructure || addListStructure)
            {
                endConnectorSymbolItem = compoundJsonTreeViewItem;
            }

            if (endConnectorSymbolItem is not null && ((addMoreCustomStructure && endConnectorSymbolItem.Children.Count - customItemCount > 0) || (addMoreStructure && next is not null && next.StartLine is not null)))
            {
                endConnectorSymbol = ",";
            }

            if (addListStructure && (endConnectorSymbolItem.Children.Count > 1 || (next is not null && next.StartLine is not null && endConnectorSymbolItem.StartLine is null && endConnectorSymbolItem.EndLine is null)))
            {
                endConnectorSymbol = ",";
            }
            #endregion

            #region 若没有子信息，则分析当前的提示源文本中是否有上下文数据
            if(compoundJsonTreeViewItem.ChildrenStringList.Count > 0)
            {
                CurrentChildrenStringList = compoundJsonTreeViewItem.ChildrenStringList;
            }
            else
            {
                Match contextMatch = GetContextKey().Match(compoundJsonTreeViewItem.InfoTipText);
                if(contextMatch.Success)
                {
                    string currentKey = '#' + contextMatch.Groups[1].Value;
                    ICustomWorldUnifiedPlan plan = compoundJsonTreeViewItem.Plan;
                    if(plan.DependencyItemList.TryGetValue(currentKey,out List<string> targetList1))
                    {
                        CurrentChildrenStringList = targetList1;
                    }
                    else
                    if(plan.TranslateDictionary.TryGetValue(currentKey,out string targetKey))
                    {
                        if(plan.DependencyItemList.TryGetValue(targetKey,out List<string> targetList2))
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
                            if(isNeedTargetValue && plan.TranslateDefaultDictionary.TryGetValue(targetKey, out string targetEnumKey))
                            {
                                if(targetDictionary.TryGetValue(targetEnumKey, out targetList4))
                                {
                                    CurrentChildrenStringList = targetList4[1..];
                                }
                                else
                                if(plan.DependencyItemList.TryGetValue(targetEnumKey, out targetList4))
                                {
                                    CurrentChildrenStringList = targetList4;
                                }
                            }
                            if(isNeedValueType)
                            {
                                CurrentChildrenStringList = ["*{{nbt " + targetKey.Trim('#') + "}}[[" + targetKey + "]]"];
                            }
                        }
                    }
                }

                if(CurrentChildrenStringList.Count == 0)
                {
                    if(compoundJsonTreeViewItem.EnumKey.Length > 0 && compoundJsonTreeViewItem.DataType is DataType.CustomCompound)
                    {
                        if(compoundJsonTreeViewItem.Plan.EnumCompoundDataDictionary.TryGetValue(compoundJsonTreeViewItem.EnumKey,out Dictionary<string,List<string>> targetDictionary) && targetDictionary.TryGetValue(compoundJsonTreeViewItem.SelectedEnumItem.Text.Trim('!'),out List<string> targetList))
                        {
                            CurrentChildrenStringList = [.. targetList];
                        }
                    }
                }
            }
            #endregion

            #region 执行一系列添加操作
            if ((parentIsList || currentIsList || currentIsCompound) && CurrentChildrenStringList.Count > 0)
            {
                #region 提取需要分析的成员(仅提取子信息而不进行源码分析)
                if (parent is not null && parentDataType is DataType.Compound && parent.DisplayText == "Entry" && previous is not null && previous is CompoundJsonTreeViewItem previousCompoundItem && previousCompoundItem.EnumKey.Length > 0 && CurrentChildrenStringList.Count == 0)
                {
                    CurrentDependencyItemList = [..parent.Parent.ChildrenStringList];
                }

                if (CurrentDependencyItemList.Count == 0)
                {
                    CurrentDependencyItemList = [..CurrentChildrenStringList];
                }
                if (compoundJsonTreeViewItem.ChildrenStringList.Count == 1 && compoundJsonTreeViewItem.SelectedValueType is not null && compoundJsonTreeViewItem.SelectedValueType.Text != "List" && compoundJsonTreeViewItem.ValueTypeSource.Count > 0)
                {
                    List<string> listOrCompoundResult = ExtractSubInformationFromPromptSourceCode(compoundJsonTreeViewItem);
                    CurrentDependencyItemList = listOrCompoundResult;
                }
                #endregion

                #region 执行解析
                string currentReferenceString = compoundJsonTreeViewItem.DataType is DataType.CustomCompound ? compoundJsonTreeViewItem.Value + "" : "";
                if(currentReferenceString.Length == 0 && compoundJsonTreeViewItem.SelectedEnumItem is not null && compoundJsonTreeViewItem.SelectedEnumItem.Text != "- unset -")
                {
                    currentReferenceString = compoundJsonTreeViewItem.SelectedEnumItem.Text;
                }

                result = htmlHelper.GetTreeViewItemResult(new(), CurrentDependencyItemList, compoundJsonTreeViewItem.LayerCount + 1, currentReferenceString, currentDataType is not DataType.None ? compoundJsonTreeViewItem : parent, null, 1, true);

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
                if (currentDataType is DataType.CustomCompound && parent is not null)
                {
                    parent.Children.Insert(customItemCount, result.Result[0]);
                }
                else
                if (addListStructure || addParentListStructure)
                {
                    if (compoundJsonTreeViewItem.Children.Count == 0 && currentDataType is not DataType.None)
                    {
                        compoundJsonTreeViewItem.Children.Add(new CompoundJsonTreeViewItem(compoundJsonTreeViewItem.Plan, compoundJsonTreeViewItem.JsonItemTool, _container)
                        {
                            StartLine = compoundJsonTreeViewItem.StartLine,
                            LayerCount = compoundJsonTreeViewItem.LayerCount,
                            ChildrenStringList = CurrentChildrenStringList,
                            ElementButtonTip = "添加到末尾",
                            DataType = DataType.None,
                            Parent = compoundJsonTreeViewItem,
                            AddOrSwitchElementButtonVisibility = Visibility.Visible,
                            SwitchButtonColor = compoundJsonTreeViewItem.PlusColor,
                            SwitchButtonIcon = compoundJsonTreeViewItem.PlusIcon,
                            PressedSwitchButtonColor = compoundJsonTreeViewItem.PressedPlusColor
                        });
                    }

                    List<string> NBTFeatureList = htmlHelper.GetHeadTypeAndKeyList(CurrentChildrenStringList[0]);
                    NBTFeatureList = HtmlHelper.RemoveUIMarker(NBTFeatureList);

                    if ((currentIsList || parentIsList) && ((compoundJsonTreeViewItem.DataType is DataType.MultiType && compoundJsonTreeViewItem.SelectedValueType is not null && compoundJsonTreeViewItem.SelectedValueType.Text == "List") || (NBTFeatureList.Count == 1 && NBTFeatureList[0] == "compound")))
                    {
                        if (currentDataType is not DataType.None)
                        {
                            compoundJsonTreeViewItem.Children.Insert(0, result.Result[0]);
                            SetLayerCountForEachItem(result.Result, compoundJsonTreeViewItem.LayerCount + 1);
                            SetParentForEachItem(result.Result, compoundJsonTreeViewItem);
                        }
                        else
                        {
                            parent.Children.Insert(parent.Children.Count - 1, result.Result[0]);
                            SetLayerCountForEachItem(result.Result, parent.LayerCount + 1);
                            SetParentForEachItem(result.Result, parent);
                        }
                    }
                    else
                    {
                        if (currentDataType is not DataType.None && result.Result.Count > 0)
                        {
                            compoundJsonTreeViewItem.Children.Insert(0, result.Result[0]);
                        }
                        else
                        if(compoundJsonTreeViewItem.Parent is not null && result.Result.Count > 0)
                        {
                            compoundJsonTreeViewItem.Parent.Children.Insert(compoundJsonTreeViewItem.Parent.Children.Count - 1, result.Result[0]);
                        }
                    }
                }
                else
                {
                    compoundJsonTreeViewItem.Children.AddRange(result.Result);
                }
                #endregion

                #region 显示换位按钮
                if (currentIsList && compoundJsonTreeViewItem.Children.Count > 2)
                {
                     compoundJsonTreeViewItem.Children[0].SortButtonVisibility = Visibility.Visible;
                    if (compoundJsonTreeViewItem.Children.Count == 3)
                    {
                        compoundJsonTreeViewItem.Children[^2].SortButtonVisibility = Visibility.Visible;
                    }
                }
                if (parentIsList && !currentIsList && parent is not null && parent.Children.Count > 2)
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
                    if (parent.Children.Count == 3)
                    {
                        parent.Children[0].SortButtonVisibility = Visibility.Visible;
                    }
                }
                #endregion

                #region 更新代码编辑器
                if (result.ResultString.Length > 0)
                {
                    #region Field
                    int endOffset = 0;
                    char compoundStartConnectorChar = ' ', compoundEndConnectorChar = ' ';
                    #endregion

                    #region 处理当前节点是否缺省以及设置首行引用
                    if (currentDataType is not DataType.None && currentDataType is not DataType.CustomCompound && currentDataType is not DataType.List)
                    {
                        compoundStartConnectorChar = '{';
                        compoundEndConnectorChar = '}';
                    }
                    else
                    if (currentDataType is DataType.List)
                    {
                        compoundStartConnectorChar = '[';
                        compoundEndConnectorChar = ']';
                    }

                    if ((compoundJsonTreeViewItem.IsCanBeDefaulted && compoundJsonTreeViewItem.StartLine is null && compoundJsonTreeViewItem.EndLine is null) /* || (!compoundJsonTreeViewItem.IsCanBeDefaulted && (compoundJsonTreeViewItem.StartLine == compoundJsonTreeViewItem.EndLine || (compoundJsonTreeViewItem.EndLine is null || (compoundJsonTreeViewItem.EndLine is not null && compoundJsonTreeViewItem.EndLine.IsDeleted))))*/)
                    {
                        if (compoundJsonTreeViewItem.Plan.GetDocumentLineCount() > 1)
                        {
                            isNotNeedNewLine = true;
                        }
                        result.ResultString.Insert(0, new string(' ', compoundJsonTreeViewItem.LayerCount * 2) + "\"" + compoundJsonTreeViewItem.Key + "\": " + compoundStartConnectorChar + "\r\n").Append("\r\n" + new string(' ', compoundJsonTreeViewItem.LayerCount * 2) + compoundEndConnectorChar + (parent is not null && (parent.StartLine == parent.EndLine || parent.StartLine is null) ? "\r\n" + new string(' ', parent.LayerCount * 2) : ""));
                    }

                    if (compoundJsonTreeViewItem.StartLine is null && currentDataType is DataType.CustomCompound && parent is not null)
                    {
                        compoundJsonTreeViewItem.StartLine = parent.StartLine;
                    }
                    #endregion

                    #region 计算是否需要新行
                    string newLine = "";
                    if (!isNotNeedNewLine && compoundJsonTreeViewItem.DataType is not DataType.None &&  (compoundJsonTreeViewItem.StartLine is null || compoundJsonTreeViewItem.EndLine is null || compoundJsonTreeViewItem.StartLine == compoundJsonTreeViewItem.EndLine || (parent is not null && parent.StartLine == parent.EndLine) || (compoundJsonTreeViewItem.DisplayText == "Entry" && compoundJsonTreeViewItem.StartLine is not null && compoundJsonTreeViewItem.EndLine is not null && compoundJsonTreeViewItem.EndLine.IsDeleted)))
                    {
                        if (currentDataType is DataType.CustomCompound)
                        {
                            newLine = parent.Children.Count - customItemCount == 1 ? "\r\n" + new string(' ', parent.LayerCount * 2) : "";
                        }
                        else
                        if (parent is not null && ((currentDataType is DataType.CustomCompound && parent.Children.Count == 2) || (currentDataType is DataType.List && compoundJsonTreeViewItem.Children.Count < 3)))
                        {
                            newLine = "\r\n" + new string(' ', (parent.StartLine == parent.EndLine || parent.EndLine is null || (parent.EndLine is not null && parent.EndLine.IsDeleted)) ? parent.LayerCount * 2 : compoundJsonTreeViewItem.LayerCount * 2);
                        }
                        else
                        if (compoundJsonTreeViewItem.Children.Count == 0 && currentDataType is not DataType.None)
                        {
                            newLine = parent.Children.Count == 1 ? "\r\n" + new string(' ', compoundJsonTreeViewItem.LayerCount * 2) : "";
                        }
                        else
                        if (parent is null && compoundJsonTreeViewItem.DataType is DataType.OptionalCompound)
                        {
                            newLine = "\r\n";
                        }
                        else
                        if (compoundJsonTreeViewItem.Children.Count == 2)
                        {
                            newLine = "\r\n" + new string(' ', compoundJsonTreeViewItem.LayerCount * 2);
                        }
                    }
                    #endregion

                    #region 执行替换
                    string currentNewString = connectorSymbol + result.ResultString.ToString() + endConnectorSymbol + newLine;
                    if (currentDataType is not DataType.None)
                    {
                        if (previousCompound is not null && previousCompound.EndLine is not null && compoundJsonTreeViewItem.IsCanBeDefaulted && compoundJsonTreeViewItem.StartLine is null)
                        {
                            endOffset = previousCompound.EndLine is not null ? previousCompound.EndLine.EndOffset : previousCompound.StartLine.EndOffset;
                            compoundJsonTreeViewItem.Plan.UpdateNullValueBySpecifyingInterval(endOffset, currentNewString);
                        }
                        else
                        if (compoundJsonTreeViewItem.IsCanBeDefaulted && compoundJsonTreeViewItem.StartLine is null)
                        {
                            if (previous is not null && previous.StartLine is not null)
                            {
                                endOffset = previous.StartLine.EndOffset;
                            }
                            else
                            if (compoundJsonTreeViewItem.Parent is not null)
                            {
                                string parentStartLineText = compoundJsonTreeViewItem.Plan.GetRangeText(compoundJsonTreeViewItem.Parent.StartLine.Offset, compoundJsonTreeViewItem.Parent.StartLine.Length);
                                endOffset = compoundJsonTreeViewItem.Parent.StartLine.Offset + parentStartLineText.IndexOf(':') + 3;
                            }
                            else
                            {
                                endOffset = 1;
                            }
                            compoundJsonTreeViewItem.StartLine = null;
                            compoundJsonTreeViewItem.Plan.UpdateNullValueBySpecifyingInterval(endOffset, currentNewString);
                        }
                        else
                        if (currentDataType is DataType.CustomCompound)
                        {
                            string currentParentLineText = compoundJsonTreeViewItem.Plan.GetRangeText(parent.StartLine.Offset, parent.StartLine.Length);
                            if (parent.Key.Contains(':'))
                            {
                                endOffset = parent.StartLine.Offset + currentParentLineText.LastIndexOf(':') + 3;
                            }
                            else
                            {
                                endOffset = parent.StartLine.Offset + currentParentLineText.IndexOf(':') + 3;
                            }
                            compoundJsonTreeViewItem.Plan.UpdateNullValueBySpecifyingInterval(endOffset, currentNewString);
                        }
                        else
                        {
                            compoundJsonTreeViewItem.Plan.UpdateValueBySpecifyingInterval(compoundJsonTreeViewItem, changeType, currentNewString);
                        }
                    }
                    else
                    if (currentDataType is DataType.None && parentIsList)
                    {
                        JsonTreeViewItem subItem = SearchForTheLastItemWithRowReference(parent);
                        if (subItem is CompoundJsonTreeViewItem lastChildItem && lastChildItem.EndLine is not null)
                        {
                            endOffset = lastChildItem.EndLine.EndOffset;
                        }
                        else
                        if(subItem is not null)
                        {
                            endOffset = subItem.StartLine.EndOffset;
                        }
                        compoundJsonTreeViewItem.Plan.UpdateNullValueBySpecifyingInterval(endOffset, currentNewString);
                    }
                    else
                    {
                        compoundJsonTreeViewItem.Plan.UpdateValueBySpecifyingInterval(compoundJsonTreeViewItem, changeType, currentNewString);
                    }
                    #endregion

                    #region 确定起始行号
                    if (compoundJsonTreeViewItem.StartLine is null && currentDataType is not DataType.None)
                    {
                        if (previous is not null && previous.StartLine is not null)
                        {
                            if (previousCompound is not null && previousCompound.EndLine is not null)
                            {
                                compoundJsonTreeViewItem.StartLine = previousCompound.EndLine.NextLine;
                            }
                            else
                            {
                                compoundJsonTreeViewItem.StartLine = previous.StartLine.NextLine;
                            }
                        }
                        else
                        if (compoundJsonTreeViewItem.Parent is not null)
                        {
                            compoundJsonTreeViewItem.StartLine = compoundJsonTreeViewItem.Parent.StartLine.NextLine;
                        }
                        else
                        {
                            compoundJsonTreeViewItem.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(2);
                        }
                    }
                    #endregion
                }
                else
                if(compoundJsonTreeViewItem.DataType is not DataType.None && compoundJsonTreeViewItem.DataType is not DataType.CustomCompound)
                {
                    string bracketString = "{}";
                    if (currentDataType is DataType.List)
                    {
                        bracketString = "[]";
                    }
                    if (previousCompound is not null && previousCompound.EndLine is not null && compoundJsonTreeViewItem.StartLine is null)
                    {
                        compoundJsonTreeViewItem.Plan.UpdateNullValueBySpecifyingInterval(previousCompound.EndLine.EndOffset, connectorSymbol + new string(' ', compoundJsonTreeViewItem.LayerCount * 2) + (compoundJsonTreeViewItem.Key.Length > 0 ? "\"" + compoundJsonTreeViewItem.Key + "\": " : "") + bracketString + endConnectorSymbol);
                        compoundJsonTreeViewItem.StartLine = previousCompound.EndLine.NextLine;
                    }
                    else
                    if (previous is not null && previous.StartLine is not null && compoundJsonTreeViewItem.StartLine is null)
                    {
                        compoundJsonTreeViewItem.Plan.UpdateNullValueBySpecifyingInterval(previous.StartLine.EndOffset, connectorSymbol + new string(' ', compoundJsonTreeViewItem.LayerCount * 2) + (compoundJsonTreeViewItem.Key.Length > 0 ? "\"" + compoundJsonTreeViewItem.Key + "\": " : "") + bracketString + endConnectorSymbol);
                        compoundJsonTreeViewItem.StartLine = previous.StartLine.NextLine;
                    }
                    else
                    if (parent is not null && compoundJsonTreeViewItem.StartLine is null && currentDataType is not DataType.None)
                    {
                        if (parent.DataType is DataType.List)
                        {
                            bracketString = "[]";
                        }

                        #region 计算偏移位置
                        string newLine = "";
                        int offset = 0;
                        if (previousCompound is not null && previousCompound.EndLine is not null)
                        {
                            offset = previousCompound.EndLine.EndOffset;
                        }
                        else
                        if (previous is not null && previous.StartLine is not null)
                        {
                            offset = previous.StartLine.EndOffset;
                        }
                        if (offset == 0)
                        {
                            string parentStartLineText = compoundJsonTreeViewItem.Plan.GetRangeText(parent.StartLine.Offset, parent.StartLine.Length);
                            offset = parent.StartLine.Offset + parentStartLineText.IndexOf(':') + 3;
                        }
                        #endregion

                        if (parent is not null && (parent.DataType is DataType.Compound || parent.DataType is DataType.OptionalCompound || parent.DataType is DataType.MultiType && parent.SelectedValueType is not null && parent.SelectedValueType.Text == "Compound") && (parent.EndLine is null || (parent.StartLine == parent.EndLine)))
                        {
                            newLine = "\r\n" + new string(' ', parent.LayerCount * 2);
                        }

                        compoundJsonTreeViewItem.Plan.UpdateNullValueBySpecifyingInterval(offset, connectorSymbol + new string(' ', compoundJsonTreeViewItem.LayerCount * 2) + (compoundJsonTreeViewItem.Key.Length > 0 ? "\"" + compoundJsonTreeViewItem.Key + "\": " : "") + bracketString + endConnectorSymbol + newLine);
                        compoundJsonTreeViewItem.StartLine = parent.StartLine.NextLine;
                    }
                }
                #endregion

                #region 将子结构行号与代码编辑器对齐
                if (currentDataType is DataType.None && addParentListStructure)
                {
                    JsonTreeViewItem subItem = SearchForTheLastItemWithRowReference(parent);
                    JsonTreeViewItem targetItem = null;
                    if (result.Result[0].Parent is not null && result.Result[0].Parent.DisplayText == "Entry")
                    {
                        targetItem = result.Result[0].Parent;
                    }
                    else
                    {
                        targetItem = result.Result[0];
                    }
                    targetItem.Parent = compoundJsonTreeViewItem.Parent;
                    if (subItem is not null && !result.Result[0].IsCanBeDefaulted)
                    {
                        targetItem.StartLine = subItem is CompoundJsonTreeViewItem subCompoundItem && subCompoundItem.EndLine is not null ? subCompoundItem.EndLine.NextLine : subItem.StartLine.NextLine;
                    }
                    if (targetItem is CompoundJsonTreeViewItem firstCompoundItem)
                    {
                        SetLineNumbersForEachItem(firstCompoundItem.Children, firstCompoundItem);
                    }
                    else
                    {
                        result.Result[0].Parent = parent;
                    }
                }
                else
                {
                    if (addMoreCustomStructure || addParentListStructure)
                    {
                        SetLineNumbersForEachItem(parent.Children, parent);
                    }
                    else
                    {
                        SetLineNumbersForEachItem(compoundJsonTreeViewItem.Children, compoundJsonTreeViewItem);
                    }
                }
                #endregion

                #region 设置列表元素之间的前后关系
                if (addListStructure && compoundJsonTreeViewItem.Children.Count > 2)
                {
                    compoundJsonTreeViewItem.Children[1].Previous = compoundJsonTreeViewItem.Children[0];
                    compoundJsonTreeViewItem.Children[0].Next = compoundJsonTreeViewItem.Children[1];
                }
                else
                if (addParentListStructure && parent.Children.Count > 2)
                {
                    parent.Children[^2].Previous = parent.Children[^3];
                    parent.Children[^3].Next = parent.Children[^2];
                }
                else
                if(addMoreCustomStructure && parent.Children.Count > 2 && parent.Children[^2] is CompoundJsonTreeViewItem customCompoundSubItem && customCompoundSubItem.DataType is not DataType.CustomCompound)
                {
                    parent.Children[customItemCount + 1].Previous = parent.Children[customItemCount];
                    parent.Children[customItemCount].Next = parent.Children[customItemCount + 1];
                }
                #endregion

                #region 确认已有子级时的末尾行号
                if ((compoundJsonTreeViewItem.DataType is DataType.OptionalCompound || compoundJsonTreeViewItem.Children.Count > 1) && (compoundJsonTreeViewItem.EndLine is null || compoundJsonTreeViewItem.StartLine == compoundJsonTreeViewItem.EndLine))
                {
                    JsonTreeViewItem subItem = SearchForTheLastItemWithRowReference(compoundJsonTreeViewItem);
                    if (subItem is CompoundJsonTreeViewItem lastChildItem1 && lastChildItem1.EndLine is not null)
                    {
                        compoundJsonTreeViewItem.EndLine = lastChildItem1.EndLine.NextLine;
                    }
                    else
                    if (subItem is not null)
                    {
                        compoundJsonTreeViewItem.EndLine = subItem.StartLine.NextLine;
                    }

                    compoundJsonTreeViewItem.EndLine ??= compoundJsonTreeViewItem.StartLine;
                }
                else
                if (result.Result.Count > 0 && result.Result[0].Parent.DisplayText == "Entry" && result.Result[0].Parent.EndLine is null)
                {
                    JsonTreeViewItem subItem = SearchForTheLastItemWithRowReference(result.Result[0].Parent);
                    if (subItem is CompoundJsonTreeViewItem lastChildItem1 && lastChildItem1.EndLine is not null)
                    {
                        result.Result[0].Parent.EndLine = lastChildItem1.EndLine.NextLine;
                    }
                    else
                    if (subItem is not null)
                    {
                        result.Result[0].Parent.EndLine = subItem.StartLine.NextLine;
                    }
                }
                else
                if(result.Result.Count > 0 && result.Result[0] is CompoundJsonTreeViewItem firstCompoundItem && firstCompoundItem.DisplayText == "Entry" && compoundJsonTreeViewItem.DataType is DataType.None)
                {
                    JsonTreeViewItem subItem = SearchForTheLastItemWithRowReference(firstCompoundItem);
                    if (subItem is CompoundJsonTreeViewItem lastChildItem && lastChildItem.EndLine is not null)
                    {
                        firstCompoundItem.EndLine = lastChildItem.EndLine.NextLine;
                    }
                    else
                    if (subItem is not null)
                    {
                        firstCompoundItem.EndLine = subItem.StartLine.NextLine;
                    }
                }
                else
                if (compoundJsonTreeViewItem.DataType is DataType.List || (compoundJsonTreeViewItem.DataType is DataType.MultiType && compoundJsonTreeViewItem.SelectedValueType is not null && compoundJsonTreeViewItem.SelectedValueType.Text == "List"))
                {
                    JsonTreeViewItem subItem = SearchForTheLastItemWithRowReference(compoundJsonTreeViewItem);
                    if (subItem is CompoundJsonTreeViewItem lastChildItem && lastChildItem.EndLine is not null)
                    {

                    }
                }
                #endregion
            }
            #endregion

            #region 处理CustomCompound生效时父级的尾行引用
            if (currentDataType is DataType.CustomCompound && parent is not null && parent.Children.Count - customItemCount > 0 && (parent.EndLine is null || parent.StartLine == parent.EndLine))
            {
                JsonTreeViewItem subItem = SearchForTheLastItemWithRowReference(parent);
                if (subItem is CompoundJsonTreeViewItem firstChildItem && (firstChildItem.StartLine == firstChildItem.EndLine || firstChildItem.EndLine is not null))
                {
                    parent.EndLine = firstChildItem.EndLine.NextLine;
                }
                else
                {
                    parent.EndLine = parent.Children[customItemCount].StartLine.NextLine;
                }
            }
            else
            if(parent is not null && (parent.DataType is DataType.Compound || parent.DataType is DataType.OptionalCompound || parent.DataType is DataType.List || parent.IsCanBeDefaulted) && (parent.EndLine is null || parent.StartLine == parent.EndLine || (parent.EndLine is not null && parent.EndLine.IsDeleted)))
            {
                JsonTreeViewItem subItem = SearchForTheLastItemWithRowReference(parent);
                if (subItem is not null)
                {
                    if (compoundJsonTreeViewItem.EndLine is not null)
                    {
                        parent.EndLine = compoundJsonTreeViewItem.EndLine.NextLine;
                    }
                    else
                    {
                        parent.EndLine = compoundJsonTreeViewItem.StartLine.NextLine;
                    }
                }
            }
            #endregion
        }

        public void CollapseCurrentItem(CompoundJsonTreeViewItem compoundJsonTreeViewItem)
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
                if (item is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                {
                    RecursiveTraverseAndRunOperate(jsonItemList, action);
                }
            }
        }

        public void UpdateFlattenDescendantNodeList(CompoundJsonTreeViewItem target, ObservableCollection<JsonTreeViewItem> list)
        {
            foreach (JsonTreeViewItem item in list)
            {
                target.FlattenDescendantNodeList.Add(item);
                //if (previousItem is CompoundJsonTreeViewItem compoundJsonTreeViewItem)
                //{
                //    target.FlattenDescendantNodeList.AddRange(compoundJsonTreeViewItem.FlattenDescendantNodeList);
                //}
            }
            if (target.Parent is not null)
            {
                UpdateFlattenDescendantNodeList(target.Parent, list);
            }
        }
        #endregion
    }
}