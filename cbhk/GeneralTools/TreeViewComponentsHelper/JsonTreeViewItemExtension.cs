using cbhk.CustomControls.JsonTreeViewComponents;
using cbhk.Interface.Json;
using cbhk.Model.Common;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using static cbhk.Model.Common.Enums;

namespace cbhk.GeneralTools.TreeViewComponentsHelper
{
    public partial class JsonTreeViewItemExtension(IContainerProvider container) : IJsonItemTool
    {
        #region Field
        private IContainerProvider _container = container;

        [GeneratedRegex(@"\[\[\#(?<1>[\u4e00-\u9fff]+)\|(?<2>[\u4e00-\u9fff]+)\]\]")]
        private static partial Regex GetContextKey();

        [GeneratedRegex(@"\s?\s*\*+\s?\s*{{[nN]bt\sinherit(?<1>[a-z_/|=*\s]+)}}\s?\s*", RegexOptions.IgnoreCase)]
        private static partial Regex GetInheritString();

        [GeneratedRegex(@"\r\n")]
        private static partial Regex GetLineBreakCount();
        #endregion

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

        public JsonTreeViewItem SearchForTheLastItemWithRowReference(CompoundJsonTreeViewItem compoundJsonTreeViewItem)
        {
            JsonTreeViewItem result = null;
            int maxValue = 0;
            for (int i = compoundJsonTreeViewItem.Children.Count - 1; i >= 0; i--)
            {
                if ((compoundJsonTreeViewItem.Children[i] is CompoundJsonTreeViewItem currentCompoundItem && currentCompoundItem.DataType is DataType.None) && compoundJsonTreeViewItem.Children[i].DataType is DataType.None)
                {
                    continue;
                }
                if (compoundJsonTreeViewItem.Children[i] is CompoundJsonTreeViewItem subCompoundItem && subCompoundItem.EndLine is not null)
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

                if (list[i].IsCanBeDefaulted)
                {
                    continue;
                }

                if (parent is not null)
                {
                    list[i].StartLine = parent.Plan.GetLineByNumber(index);
                }
                else
                if (list[i] is CompoundJsonTreeViewItem subCompoundItem1 && ((subCompoundItem1.DataType is not DataType.OptionalCompound) || (subCompoundItem1.DataType is DataType.OptionalCompound && subCompoundItem1.Children.Count > 0)))
                {
                    list[i].StartLine = list[i].Plan.GetLineByNumber(index);
                }

                index++;
                if (list[i] is CompoundJsonTreeViewItem subCompoundItem2)
                {
                    if (subCompoundItem2.Children.Count > 0 && (subCompoundItem2.Children[^1] is CompoundJsonTreeViewItem subsubCompoundItem && subsubCompoundItem.DataType is not DataType.CustomCompound || subCompoundItem2.Children[^1] is not CompoundJsonTreeViewItem))
                    {
                        SetLineNumbersForEachItem(subCompoundItem2.Children, subCompoundItem2);
                        #region 搜索最后一个有值的子节点
                        JsonTreeViewItem subItem = null;
                        for (int j = subCompoundItem2.Children.Count - 1; j >= 0; j--)
                        {
                            if (subCompoundItem2.Children[j] is CompoundJsonTreeViewItem subCompoundItem && subCompoundItem.EndLine is not null)
                            {
                                subItem = subCompoundItem;
                            }
                            else
                            if (subCompoundItem2.Children[j].StartLine is not null)
                            {
                                subItem = subCompoundItem2.Children[j];
                            }
                        }
                        #endregion
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

        public void AddSubStructure(CompoundJsonTreeViewItem compoundJsonTreeViewItem)
        {
            #region Field
            bool currentIsList = compoundJsonTreeViewItem.DataType is DataType.List ||
                compoundJsonTreeViewItem.DataType is DataType.Array;
            bool parentIsList = compoundJsonTreeViewItem.Parent is not null && (
                compoundJsonTreeViewItem.Parent.DataType is DataType.Array ||
                compoundJsonTreeViewItem.Parent.DataType is DataType.List);

            bool currentIsCompound = compoundJsonTreeViewItem.DataType is DataType.Compound ||
                compoundJsonTreeViewItem.DataType is DataType.CustomCompound ||
                compoundJsonTreeViewItem.DataType is DataType.OptionalCompound;
            bool IsArrayOrList = currentIsList || parentIsList;
            JsonTreeViewDataStructure result = new();
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

            #region 定义前置与末尾衔接符
            string connectorSymbol = "\r\n";
            string endConnectorSymbol = "";
            if ((previous is not null && previous.StartLine is null && (next is null || (next is not null && next.StartLine is not null)) && compoundJsonTreeViewItem.IsCanBeDefaulted && compoundJsonTreeViewItem.DataType is not DataType.CustomCompound) || (compoundJsonTreeViewItem.DataType is DataType.None && compoundJsonTreeViewItem.Parent is not null && compoundJsonTreeViewItem.Parent.DataType is DataType.List))
            {
                connectorSymbol = ",\r\n";
            }
            #endregion

            #region 实例化复合节点子链表信息
            if ((parentIsList || currentIsList || currentIsCompound) && compoundJsonTreeViewItem.ChildrenStringList.Count > 0)
            {
                #region 提取需要分析的成员
                Match inheritMatch = GetInheritString().Match(compoundJsonTreeViewItem.ChildrenStringList[0]);
                if (compoundJsonTreeViewItem.ChildrenStringList.Count == 1 && !compoundJsonTreeViewItem.ChildrenStringList[0].Contains('*'))
                {
                    Match contextMatch = GetContextKey().Match(compoundJsonTreeViewItem.ChildrenStringList[0]);
                    if (contextMatch.Success)
                    {
                        string contextKey = ("#" + contextMatch.Groups[1].Value + "|" + contextMatch.Groups[2].Value);
                        if (compoundJsonTreeViewItem.Plan.DependencyItemList.TryGetValue(contextKey, out List<string> targetList1))
                        {
                            CurrentDependencyItemList = targetList1;
                        }
                        else
                        {
                            string targetKey = compoundJsonTreeViewItem.Plan.TranslateDictionary[contextKey];
                            if (compoundJsonTreeViewItem.Plan.DependencyItemList.TryGetValue(targetKey, out List<string> targetList2))
                            {
                                CurrentDependencyItemList = targetList2;
                            }
                        }
                        CurrentDependencyItemList.RemoveAt(0);
                    }
                }
                else
                if (compoundJsonTreeViewItem.ChildrenStringList.Count == 1 && inheritMatch.Success)
                {
                    string targetInheritString = inheritMatch.Groups[1].Value.TrimStart('/').Replace("/", "\\");
                    string targetReference = @"#Inherit\" + targetInheritString;
                    if (compoundJsonTreeViewItem.Plan.DependencyItemList.TryGetValue(targetReference, out List<string> list))
                    {
                        CurrentDependencyItemList = htmlHelper.TypeSetting([.. list]);
                    }
                }

                if (CurrentDependencyItemList.Count == 0)
                {
                    CurrentDependencyItemList = compoundJsonTreeViewItem.ChildrenStringList;
                }
                #endregion

                #region 执行解析
                result = htmlHelper.GetTreeViewItemResult(new(), CurrentDependencyItemList, compoundJsonTreeViewItem.LayerCount + 1, (string)compoundJsonTreeViewItem.Value, compoundJsonTreeViewItem.DataType is DataType.CustomCompound ? compoundJsonTreeViewItem : null, null, 1, true);
                #endregion

                #region 计算有多少个自定义子节点
                if (compoundJsonTreeViewItem.Parent is not null && compoundJsonTreeViewItem.Parent.Children.Count > 0 && compoundJsonTreeViewItem.Parent.Children[0] is CompoundJsonTreeViewItem subCompoundItem1 && subCompoundItem1.DataType is DataType.CustomCompound)
                {
                    customItemCount = 1;
                }
                else
                if (compoundJsonTreeViewItem.Parent is not null && compoundJsonTreeViewItem.Parent.Children.Count > 1 && compoundJsonTreeViewItem.Parent.Children[1] is CompoundJsonTreeViewItem subCompoundItem2 && subCompoundItem2.DataType is DataType.CustomCompound)
                {
                    customItemCount = 2;
                }
                #endregion

                #region 判断是在添加枚举型结构还是展开复合节点
                bool addMoreCustomStructure = compoundJsonTreeViewItem.DataType is DataType.CustomCompound && compoundJsonTreeViewItem.Parent is not null && compoundJsonTreeViewItem.Parent.Children.Count - customItemCount > 0;

                bool addMoreStructure = compoundJsonTreeViewItem.DataType is DataType.OptionalCompound || compoundJsonTreeViewItem.DataType is DataType.Compound;

                bool addListStructure = compoundJsonTreeViewItem.DataType is DataType.List || (compoundJsonTreeViewItem.Parent is CompoundJsonTreeViewItem parentItem && parentItem.DataType is DataType.List);

                bool addParentListStructure = compoundJsonTreeViewItem.DataType is not DataType.List && compoundJsonTreeViewItem.Parent is not null && compoundJsonTreeViewItem.Parent.DataType is DataType.List;
                #endregion

                #region 计算后置连接符
                CompoundJsonTreeViewItem endConnectorSymbolItem = null;
                if (addMoreCustomStructure || addParentListStructure)
                {
                    endConnectorSymbolItem = compoundJsonTreeViewItem.Parent;
                }
                else
                if (addMoreStructure || addListStructure)
                {
                    endConnectorSymbolItem = compoundJsonTreeViewItem;
                }

                if (endConnectorSymbolItem is not null && next is not null && next.StartLine is not null && ((addMoreCustomStructure && endConnectorSymbolItem.Children.Count - customItemCount > 0) || addMoreStructure))
                {
                    endConnectorSymbol = ",";
                }

                if (addListStructure && (compoundJsonTreeViewItem.IsCanBeDefaulted || compoundJsonTreeViewItem.Children.Count > 1))
                {
                    endConnectorSymbol = ",";
                }
                #endregion

                #region 添加当前节点的子节点集
                if (compoundJsonTreeViewItem.DataType is DataType.CustomCompound && compoundJsonTreeViewItem.Parent is not null)
                {
                    compoundJsonTreeViewItem.Parent.Children.Insert(1, result.Result[0]);
                }
                if (compoundJsonTreeViewItem.DataType is DataType.List)
                {
                    if (compoundJsonTreeViewItem.Children.Count == 0)
                    {
                        compoundJsonTreeViewItem.Children.Add(new CompoundJsonTreeViewItem(compoundJsonTreeViewItem.Plan, compoundJsonTreeViewItem.JsonItemTool, _container)
                        {
                            StartLine = compoundJsonTreeViewItem.StartLine,
                            LayerCount = compoundJsonTreeViewItem.LayerCount,
                            ChildrenStringList = compoundJsonTreeViewItem.ChildrenStringList,
                            ElementButtonTip = "添加到末尾",
                            DataType = DataType.None,
                            Parent = compoundJsonTreeViewItem,
                            AddOrSwitchElementButtonVisibility = Visibility.Visible,
                            SwitchButtonColor = compoundJsonTreeViewItem.PlusColor,
                            SwitchButtonIcon = compoundJsonTreeViewItem.PlusIcon,
                            PressedSwitchButtonColor = compoundJsonTreeViewItem.PressedPlusColor
                        });
                    }
                    compoundJsonTreeViewItem.Children.Insert(0, result.Result[0]);
                }
                else
                if (compoundJsonTreeViewItem.Parent is not null && compoundJsonTreeViewItem.Parent.DataType is DataType.List)
                {
                    compoundJsonTreeViewItem.Parent.Children.Insert(compoundJsonTreeViewItem.Parent.Children.Count - 1, result.Result[0]);
                }
                else
                {
                    compoundJsonTreeViewItem.Children.AddRange(result.Result);
                }
                #endregion

                #region 更新代码编辑器
                if (result.ResultString.Length > 0)
                {
                    #region Field
                    int endOffset;
                    char compoundStartConnectorChar = ' ', compoundEndConnectorChar = ' ';
                    #endregion

                    #region 处理当前节点是否缺省以及设置首行引用
                    if (compoundJsonTreeViewItem.DataType is not DataType.None && compoundJsonTreeViewItem.DataType is not DataType.CustomCompound && compoundJsonTreeViewItem.DataType is not DataType.List)
                    {
                        compoundStartConnectorChar = '{';
                        compoundEndConnectorChar = '}';
                    }
                    else
                    if (compoundJsonTreeViewItem.DataType is DataType.List)
                    {
                        compoundStartConnectorChar = '[';
                        compoundEndConnectorChar = ']';
                    }

                    if (compoundJsonTreeViewItem.IsCanBeDefaulted && compoundJsonTreeViewItem.StartLine is null && compoundJsonTreeViewItem.EndLine is null)
                    {
                        result.ResultString.Insert(0, new string(' ', compoundJsonTreeViewItem.LayerCount * 2) + "\"" + compoundJsonTreeViewItem.Key + "\": " + compoundStartConnectorChar + "\r\n").Append("\r\n" + new string(' ', compoundJsonTreeViewItem.LayerCount * 2) + compoundEndConnectorChar);
                    }

                    //替换前分离首末行的引用避免执行替换时首行产生变动
                    if (compoundJsonTreeViewItem.StartLine is null && previousCompound is not null && previousCompound.EndLine is not null)
                    {
                        compoundJsonTreeViewItem.StartLine = previousCompound.EndLine.NextLine;
                    }
                    else
                    if (compoundJsonTreeViewItem.StartLine is null && previous is not null && previous.StartLine is not null)
                    {
                        compoundJsonTreeViewItem.StartLine = previous.StartLine.NextLine;
                    }

                    if (compoundJsonTreeViewItem.DataType is not DataType.None && compoundJsonTreeViewItem.StartLine is null)
                    {
                        compoundJsonTreeViewItem.StartLine ??= compoundJsonTreeViewItem.Plan.GetLineByNumber(2);
                    }
                    if (compoundJsonTreeViewItem.StartLine == compoundJsonTreeViewItem.EndLine)
                    {
                        compoundJsonTreeViewItem.EndLine = null;
                    }
                    #endregion

                    #region 计算是否需要新行
                    string newLine = "";
                    if (compoundJsonTreeViewItem.DataType is DataType.CustomCompound && compoundJsonTreeViewItem.Parent is not null)
                    {
                        newLine = compoundJsonTreeViewItem.Parent.Children.Count - customItemCount == 1 ? "\r\n" + new string(' ', compoundJsonTreeViewItem.Parent.LayerCount * 2) : "";
                    }
                    else
                        if (compoundJsonTreeViewItem.Children.Count == 0)
                    {
                        newLine = compoundJsonTreeViewItem.Parent.Children.Count == 1 ? "\r\n" + new string(' ', compoundJsonTreeViewItem.LayerCount * 2) : "";
                    }
                    else
                    if ((compoundJsonTreeViewItem.DataType is DataType.CustomCompound && compoundJsonTreeViewItem.Parent.Children.Count == 2) || (compoundJsonTreeViewItem.Parent is not null && compoundJsonTreeViewItem.Parent.DataType is DataType.List && !compoundJsonTreeViewItem.IsCanBeDefaulted))
                    {
                        newLine = "\r\n" + new string(' ', compoundJsonTreeViewItem.LayerCount * 2);
                    }
                    #endregion

                    #region 执行替换
                    string currentNewString = connectorSymbol + result.ResultString.ToString() + endConnectorSymbol + newLine;
                    if (compoundJsonTreeViewItem.DataType is not DataType.None)
                    {
                        if (previousCompound is not null && compoundJsonTreeViewItem.IsCanBeDefaulted)
                        {
                            endOffset = previousCompound.EndLine is not null ? previousCompound.EndLine.EndOffset : previousCompound.StartLine.EndOffset;
                            compoundJsonTreeViewItem.StartLine = null;
                            compoundJsonTreeViewItem.Plan.UpdateNullValueBySpecifyingInterval(endOffset, currentNewString);
                        }
                        else
                        if (compoundJsonTreeViewItem.IsCanBeDefaulted)
                        {
                            endOffset = previous is not null ? previous.StartLine.EndOffset : 1;
                            compoundJsonTreeViewItem.StartLine = null;
                            compoundJsonTreeViewItem.Plan.UpdateNullValueBySpecifyingInterval(endOffset, currentNewString);
                        }
                        else
                        {
                            compoundJsonTreeViewItem.Plan.UpdateValueBySpecifyingInterval(compoundJsonTreeViewItem, changeType, currentNewString);
                        }
                    }
                    else
                    if (compoundJsonTreeViewItem.DataType is DataType.None && parentIsList)
                    {
                        JsonTreeViewItem subItem = SearchForTheLastItemWithRowReference(compoundJsonTreeViewItem.Parent);
                        if (subItem is CompoundJsonTreeViewItem lastChildItem && lastChildItem.EndLine is not null)
                        {
                            endOffset = lastChildItem.EndLine.EndOffset;
                        }
                        else
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
                    int newLineCount = 0;
                    if (compoundJsonTreeViewItem.Children.Count == 1)
                    {
                        newLineCount = GetLineBreakCount().Matches(currentNewString).Count + (!string.IsNullOrEmpty(connectorSymbol) ? -1 : 0) + (result.Result[^1] is CompoundJsonTreeViewItem lastCompoundChildItem && (lastCompoundChildItem.DataType is not DataType.OptionalCompound ||
                            lastCompoundChildItem.ValueTypeList.Select(item => item.Text.Contains("compound")).Count() > 0) && result.Result[^1].Key != compoundJsonTreeViewItem.Key ? 1 : 0);
                    }
                    if (compoundJsonTreeViewItem.StartLine is null && compoundJsonTreeViewItem.DataType is not DataType.None)
                    {
                        if (previous is not null)
                        {
                            if (previousCompound.EndLine is not null)
                            {
                                compoundJsonTreeViewItem.StartLine = previousCompound.EndLine.NextLine;
                            }
                            else
                            {
                                compoundJsonTreeViewItem.StartLine = previous.StartLine.NextLine;
                            }
                        }
                        else
                        {
                            compoundJsonTreeViewItem.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(2);
                        }
                    }
                    #endregion

                    #region 确定首次添加子级时的末尾行号
                    if (compoundJsonTreeViewItem.Children.Count == 1)
                    {
                        compoundJsonTreeViewItem.EndLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.StartLine.LineNumber + newLineCount);
                    }
                    #endregion
                }
                else
                {
                    #region 执行替换
                    if (previousCompound is not null && previousCompound.EndLine is not null)
                    {
                        compoundJsonTreeViewItem.Plan.UpdateNullValueBySpecifyingInterval(previousCompound.EndLine.EndOffset, connectorSymbol + new string(' ', compoundJsonTreeViewItem.LayerCount * 2) + "\"" + compoundJsonTreeViewItem.Key + "\": {}" + endConnectorSymbol);
                        compoundJsonTreeViewItem.StartLine = previousCompound.EndLine.NextLine;
                    }
                    else
                    if (previous is not null && previous.StartLine is not null)
                    {
                        compoundJsonTreeViewItem.Plan.UpdateNullValueBySpecifyingInterval(previous.StartLine.EndOffset, connectorSymbol + new string(' ', compoundJsonTreeViewItem.LayerCount * 2) + "\"" + compoundJsonTreeViewItem.Key + "\": {}" + endConnectorSymbol);
                        compoundJsonTreeViewItem.StartLine = previous.StartLine.NextLine;
                    }
                    #endregion
                }
                #endregion

                #region 将子结构行号与代码编辑器对齐
                ObservableCollection<JsonTreeViewItem> subDependencyItemList = result.Result;
                if (compoundJsonTreeViewItem.DataType is DataType.None && addParentListStructure)
                {
                    JsonTreeViewItem subItem = SearchForTheLastItemWithRowReference(compoundJsonTreeViewItem.Parent);
                    result.Result[0].StartLine = subItem is CompoundJsonTreeViewItem subCompoundItem && subCompoundItem.EndLine is not null ? subCompoundItem.EndLine.NextLine : subItem.StartLine.NextLine;
                    if (result.Result[0] is CompoundJsonTreeViewItem firstCompoundItem && firstCompoundItem.Children.Count > 0)
                    {
                        SetLineNumbersForEachItem(firstCompoundItem.Children, firstCompoundItem);
                    }
                    else
                    {
                        result.Result[0].Parent = compoundJsonTreeViewItem.Parent;
                    }
                }
                else
                {
                    SetLineNumbersForEachItem(subDependencyItemList, compoundJsonTreeViewItem);
                }
                #endregion

                #region 设置列表元素之间的前后关系
                if (addListStructure && compoundJsonTreeViewItem.Children.Count > 2)
                {
                    compoundJsonTreeViewItem.Children[1].Previous = compoundJsonTreeViewItem.Children[0];
                    compoundJsonTreeViewItem.Children[0].Next = compoundJsonTreeViewItem.Children[1];
                }
                else
                if (addParentListStructure && compoundJsonTreeViewItem.Parent.Children.Count > 2)
                {
                    compoundJsonTreeViewItem.Parent.Children[^2].Previous = compoundJsonTreeViewItem.Parent.Children[^3];
                    compoundJsonTreeViewItem.Parent.Children[^3].Next = compoundJsonTreeViewItem.Parent.Children[^2];
                }
                #endregion

                #region 确认已有子级时的末尾行号
                if (compoundJsonTreeViewItem.Children.Count > 1 && compoundJsonTreeViewItem.EndLine is null)
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
                #endregion
            }
            #endregion

            #region 处理CustomCompound生效时父级的尾行引用
            if (compoundJsonTreeViewItem.DataType is DataType.CustomCompound && parent is not null && parent.Children.Count == 2 && (parent.EndLine is null || parent.StartLine == parent.EndLine))
            {
                if (parent.Children[1] is CompoundJsonTreeViewItem firstChildItem && (firstChildItem.StartLine == firstChildItem.EndLine || firstChildItem.EndLine is not null))
                {
                    parent.EndLine = firstChildItem.EndLine.NextLine;
                }
                else
                {
                    parent.EndLine = parent.Children[1].StartLine.NextLine;
                }
            }
            #endregion
        }

        public void CollapseCurrentItem(CompoundJsonTreeViewItem compoundJsonTreeViewItem)
        {
            #region 确定删除的数据类型
            ChangeType changeType = ChangeType.String;
            if (compoundJsonTreeViewItem.DataType is DataType.ArrayElement)
            {
                changeType = ChangeType.RemoveListElement;
            }
            else
            if (compoundJsonTreeViewItem.DataType is DataType.Compound || compoundJsonTreeViewItem.DataType is DataType.OptionalCompound || compoundJsonTreeViewItem.DataType is DataType.CustomCompound)
            {
                changeType = ChangeType.RemoveCompound;
            }
            else
            if (compoundJsonTreeViewItem.DataType is DataType.String)
            {
                changeType = ChangeType.String;
            }
            else
            {
                changeType = ChangeType.NumberAndBool;
            }
            #endregion

            #region 更新TreeView与JsonView
            //Parallel.ForEach(compoundJsonTreeViewItem.Plan.KeyValueContextDictionary.Keys, item =>
            //{
            //    if (item.Contains(compoundJsonTreeViewItem.Path))
            //    {
            //        compoundJsonTreeViewItem.Plan.KeyValueContextDictionary.Remove(item);
            //    }
            //});
            compoundJsonTreeViewItem.Plan.UpdateValueBySpecifyingInterval(compoundJsonTreeViewItem, changeType);
            if (changeType is ChangeType.RemoveCompound)
            {
                compoundJsonTreeViewItem.Children.Clear();
            }
            if (changeType is ChangeType.RemoveListElement)
            {
                compoundJsonTreeViewItem.Parent.Children.Remove(compoundJsonTreeViewItem);
                compoundJsonTreeViewItem.Parent.FlattenDescendantNodeList.Remove(compoundJsonTreeViewItem);
                if (compoundJsonTreeViewItem.Parent.Children.Count == 1)
                {
                    compoundJsonTreeViewItem.Parent.Children.Clear();
                }
            }
            #endregion
        }

        public void RemoveCurrentItem(JsonTreeViewItem jsonTreeViewItem)
        {

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
    }
}