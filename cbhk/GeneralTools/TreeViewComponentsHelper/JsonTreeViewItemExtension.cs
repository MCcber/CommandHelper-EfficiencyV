using cbhk.CustomControls.JsonTreeViewComponents;
using cbhk.Interface.Json;
using cbhk.Model.Common;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        public void SetLineNumbersForEachItem(ObservableCollection<JsonTreeViewItem> list,CompoundJsonTreeViewItem parent,bool withType = false)
        {
            int index = 0;

            if(parent is null)
            {
                index = 1;
            }

            if(withType)
            {
                index = 1;
                withType = false;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].IsCanBeDefaulted)
                {
                    continue;
                }

                if (parent is not null)
                {
                    list[i].StartLine = parent.Plan.GetLineByNumber(index + 1 + parent.StartLine.LineNumber);
                    list[i].Parent = parent;
                }
                else
                {
                    list[i].StartLine = list[i].Plan.GetLineByNumber(index + 1);
                }

                if (list[i] is CompoundJsonTreeViewItem subCompoundItem)
                {
                    if (subCompoundItem.Children.Count > 0)
                    {
                        SetLineNumbersForEachItem(subCompoundItem.Children, subCompoundItem);
                        if (subCompoundItem.Children[^1] is CompoundJsonTreeViewItem subSubCompoundItem)
                        {
                            subCompoundItem.EndLine = parent.Plan.GetLineByNumber(subSubCompoundItem.EndLine.LineNumber + 1);
                        }
                        else
                        {
                            subCompoundItem.EndLine = parent.Plan.GetLineByNumber(subCompoundItem.Children[^1].StartLine.LineNumber + 1);
                        }
                    }
                    else
                    {
                        subCompoundItem.EndLine = subCompoundItem.StartLine;
                    }
                }
                index++;
            }
        }

        public void AddSubStructure(CompoundJsonTreeViewItem compoundJsonTreeViewItem)
        {
            #region 字段
            bool currentIsArray = compoundJsonTreeViewItem.DataType is DataTypes.List ||
                compoundJsonTreeViewItem.DataType is DataTypes.Array ||
                compoundJsonTreeViewItem.DataType is DataTypes.InnerArray;
            bool parentIsArray = compoundJsonTreeViewItem.Parent is not null && (
                compoundJsonTreeViewItem.Parent.DataType is DataTypes.List || 
                compoundJsonTreeViewItem.Parent.DataType is DataTypes.Array || 
                compoundJsonTreeViewItem.Parent.DataType is DataTypes.InnerArray ||
                compoundJsonTreeViewItem.Parent.DataType is DataTypes.List);

            bool currentIsCompound = compoundJsonTreeViewItem.DataType is DataTypes.Compound ||
                compoundJsonTreeViewItem.DataType is DataTypes.CustomCompound ||
                compoundJsonTreeViewItem.DataType is DataTypes.NullableCompound ||
                compoundJsonTreeViewItem.DataType is DataTypes.OptionalCompound;
            bool IsArrayOrList = currentIsArray || parentIsArray;
            JsonTreeViewDataStructure result = new();
            List<string> CurrentDependencyItemList = [];
            ChangeType changeType = ChangeType.Input;
            CompoundJsonTreeViewItem? targetCompoundItem;
            if (currentIsArray || currentIsCompound)
            {
                targetCompoundItem = compoundJsonTreeViewItem;
            }
            else
            if (parentIsArray)
            {
                targetCompoundItem = compoundJsonTreeViewItem.Parent;
            }
            if (currentIsCompound)
            {
                changeType = ChangeType.AddCompoundObject;
            }
            else
            {
                changeType = ChangeType.AddArrayElement;
            }

            HtmlHelper htmlHelper = new(_container)
            {
                plan = compoundJsonTreeViewItem.Plan,
                jsonTool = compoundJsonTreeViewItem.JsonItemTool
            };
            #endregion

            #region 定位上一个已有值的节点
            JsonTreeViewItem previous = compoundJsonTreeViewItem.Previous;
            JsonTreeViewItem parentPrevious = compoundJsonTreeViewItem.Parent?.Previous;
            while (previous is not null && previous.StartLine is null)
            {
                if (previous.Previous is null)
                {
                    break;
                }
                previous = previous.Previous;
            }
            while (parentPrevious is not null && parentPrevious.StartLine is null)
            {
                if(parentPrevious.Previous is null)
                {
                    break;
                }
                parentPrevious = parentPrevious.Previous;
            }
            #endregion

            #region 实例化复合节点子链表信息
            if (currentIsCompound && compoundJsonTreeViewItem.ChildrenStringList.Count > 0)
            {
                #region 定义前置衔接符
                string connectorSymbol = "";
                if(previous is not null)
                {
                    connectorSymbol = ",\r\n";
                }
                else
                if(compoundJsonTreeViewItem.DataType is DataTypes.Compound && compoundJsonTreeViewItem.StartLine == compoundJsonTreeViewItem.EndLine)
                {
                    connectorSymbol = "\r\n";
                }
                #endregion

                #region 提取需要分析的成员
                if (compoundJsonTreeViewItem.ChildrenStringList.Count == 1)
                {
                    Match contextMatch = GetContextKey().Match(compoundJsonTreeViewItem.ChildrenStringList[0]);
                    if (contextMatch.Success)
                    {
                        string contextKey = ("#" + contextMatch.Groups[1].Value + "|" + contextMatch.Groups[2].Value).Replace("|上文", "").Replace("|下文", "");
                        CurrentDependencyItemList = compoundJsonTreeViewItem.Plan.CurrentDependencyItemList[contextKey];
                    }
                    else
                    {
                        CurrentDependencyItemList = compoundJsonTreeViewItem.ChildrenStringList;
                    }
                }
                else
                if(compoundJsonTreeViewItem.ChildrenStringList.Count > 1)
                {
                    CurrentDependencyItemList = compoundJsonTreeViewItem.ChildrenStringList;
                }
                #endregion

                #region 执行分析
                result = htmlHelper.GetTreeViewItemResult(new(), CurrentDependencyItemList, compoundJsonTreeViewItem.LayerCount + (compoundJsonTreeViewItem.DataType is not DataTypes.OptionalCompound ? 1 : 0), (string)compoundJsonTreeViewItem.Value, compoundJsonTreeViewItem, null, 1, true);

                if (result.Result.Count == 1 && result.Result[0] is CompoundJsonTreeViewItem firstCompoundItem && firstCompoundItem.DataType is DataTypes.CustomCompound)
                {
                    firstCompoundItem.AddElementButtonVisibility = Visibility.Collapsed;
                    firstCompoundItem.RemoveElementButtonVisibility = Visibility.Visible;
                }
                #endregion

                #region 添加当前节点的子节点集
                if (result.Result.Count == 1 && result.Result[0] is CompoundJsonTreeViewItem CurrentSubDependencyItem && CurrentSubDependencyItem.Children.Count > 0)
                {
                    if (CurrentSubDependencyItem.DataType is not DataTypes.CustomCompound)
                    {
                        compoundJsonTreeViewItem.Children.AddRange(CurrentSubDependencyItem.Children);
                    }
                    else
                    {
                        compoundJsonTreeViewItem.Children.Add(CurrentSubDependencyItem);
                    }
                }
                else
                {
                    compoundJsonTreeViewItem.Children.AddRange(result.Result);
                }
                #endregion

                #region 更新代码编辑器
                if (result.ResultString.Length > 0)
                {
                    int endOffset;
                    //替换前分离首末行的引用避免执行替换时首行产生变动
                    int startLineNumber = compoundJsonTreeViewItem.StartLine.LineNumber;
                    int endlineNumber = compoundJsonTreeViewItem.EndLine.LineNumber;
                    compoundJsonTreeViewItem.EndLine = null;

                    #region 整理最终拼接字符串
                    string currentNewString = "";
                    if (startLineNumber != endlineNumber)
                    {
                        currentNewString = connectorSymbol + result.ResultString.ToString().TrimEnd(['\r', '\n', ',']);
                    }
                    else
                    {
                        currentNewString = connectorSymbol + result.ResultString.ToString() + (compoundJsonTreeViewItem.DataType is not DataTypes.OptionalCompound ? "\r\n" : "") + new string(' ', compoundJsonTreeViewItem.LayerCount * 2);
                    }
                    #endregion

                    #region 执行替换
                    if (previous is CompoundJsonTreeViewItem previousCompoundItem && compoundJsonTreeViewItem.DataType is DataTypes.OptionalCompound)
                    {
                        endOffset = previousCompoundItem.EndLine is not null ? previousCompoundItem.EndLine.EndOffset : previousCompoundItem.StartLine.EndOffset;
                        compoundJsonTreeViewItem.StartLine = null;
                        compoundJsonTreeViewItem.Plan.UpdateNullValueBySpecifyingInterval(endOffset, currentNewString);
                    }
                    else
                    {
                        compoundJsonTreeViewItem.Plan.UpdateValueBySpecifyingInterval(compoundJsonTreeViewItem, changeType, currentNewString);
                    }
                    #endregion

                    #region 确定起始行号
                    int newLineCount = GetLineBreakCount().Matches(currentNewString).Count + (!string.IsNullOrEmpty(connectorSymbol) ? -1 : 0) + (result.Result[^1] is CompoundJsonTreeViewItem lastCompoundChildItem && (lastCompoundChildItem.DataType is DataTypes.Compound || 
                        lastCompoundChildItem.DataType is DataTypes.CustomCompound || 
                        lastCompoundChildItem.DataType is DataTypes.EnumCompound || 
                        lastCompoundChildItem.DataType is DataTypes.NullableCompound) && result.Result[^1].Key != compoundJsonTreeViewItem.Key ? 1 : 0);
                    if (compoundJsonTreeViewItem.StartLine is null)
                    {
                        if (previous is not null)
                        {
                            if (previous is CompoundJsonTreeViewItem previousItem && previousItem.EndLine is not null)
                            {
                                compoundJsonTreeViewItem.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(previousItem.EndLine.LineNumber + 1);
                            }
                            else
                            {
                                compoundJsonTreeViewItem.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(previous.StartLine.LineNumber + 1);
                            }
                        }
                        else
                        {
                            compoundJsonTreeViewItem.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(2);
                        }
                    }
                    #endregion

                    #region 确定末尾行号
                    compoundJsonTreeViewItem.EndLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.StartLine.LineNumber + newLineCount);
                    #endregion
                }
                #endregion

                #region 将子结构行号与代码编辑器对齐
                ObservableCollection<JsonTreeViewItem> subDependencyItemList = [];
                if ((result.Result[0] as CompoundJsonTreeViewItem).DataType is not DataTypes.CustomCompound)
                {
                    subDependencyItemList = (result.Result[0] as CompoundJsonTreeViewItem).Children;
                }
                else
                {
                    subDependencyItemList = result.Result;
                }
                SetLineNumbersForEachItem(subDependencyItemList, compoundJsonTreeViewItem);
                #endregion

                #region 根据当前节点类型判断是否切换功能按钮
                compoundJsonTreeViewItem.AddElementButtonVisibility = Visibility.Collapsed;
                compoundJsonTreeViewItem.RemoveElementButtonVisibility = Visibility.Visible;
                #endregion
            }
            #endregion

            #region 实例化数组/列表节点子链表信息
            if ((currentIsArray && compoundJsonTreeViewItem.ChildrenStringList.Count > 0) || 
                (parentIsArray && compoundJsonTreeViewItem.Parent is not null && compoundJsonTreeViewItem.Parent.ChildrenStringList.Count > 0))
            {
                string connectorSymbol = (currentIsArray && previous is not null) || (parentIsArray && parentPrevious is not null) ? ",\r\n" : "";
                result = htmlHelper.GetTreeViewItemResult(new(), compoundJsonTreeViewItem.ChildrenStringList, compoundJsonTreeViewItem.LayerCount + 1, "", compoundJsonTreeViewItem, null, 1, true);
                string currentNewString = result.ResultString.ToString();
                CompoundJsonTreeViewItem entry = new(compoundJsonTreeViewItem.Plan, compoundJsonTreeViewItem.JsonItemTool,_container)
                {
                    Path = currentIsArray ? compoundJsonTreeViewItem.Path + "[" + 0 + "]" : compoundJsonTreeViewItem.Parent.Path + "[" + (compoundJsonTreeViewItem.Parent.Children.Count - 1) + "]",
                    Parent = currentIsCompound ? compoundJsonTreeViewItem : compoundJsonTreeViewItem.Parent,
                    DisplayText = "Entry",
                    DataType = DataTypes.ArrayElement,
                    LayerCount = compoundJsonTreeViewItem.LayerCount + 1,
                    RemoveElementButtonVisibility = Visibility.Visible
                };
                entry.Children.AddRange(result.Result);
                if(compoundJsonTreeViewItem.DataType is DataTypes.Array || compoundJsonTreeViewItem.DataType is DataTypes.InnerArray)
                {
                    compoundJsonTreeViewItem.Children.Insert(0, entry);
                }
                else
                {
                    compoundJsonTreeViewItem.Parent.Children.Add(entry);
                }

                #region 更新代码编辑器
                int endOffset = 0;
                if (currentIsArray)
                {
                    endOffset = compoundJsonTreeViewItem.StartLine.EndOffset;
                }
                else
                if(parentIsArray)
                {
                    endOffset = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.EndLine.LineNumber - 1).EndOffset;
                }
                compoundJsonTreeViewItem.Plan.SetRangeText(endOffset, 0, connectorSymbol + new string(' ', compoundJsonTreeViewItem.LayerCount + 1) + "{\r\n" + new string(' ', compoundJsonTreeViewItem.LayerCount + 2) + currentNewString + "\r\n" + new string(' ', compoundJsonTreeViewItem.LayerCount + 1) + "}");
                #endregion

                #region 将子结构行号与代码编辑器对齐
                if (currentIsArray)
                {
                    entry.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.StartLine.LineNumber + 1);
                    entry.EndLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.Children[1].StartLine.LineNumber - 1);
                }
                else
                if (parentIsArray)
                {
                    entry.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber((compoundJsonTreeViewItem.Children[^1] as CompoundJsonTreeViewItem).EndLine.LineNumber + 1);
                    entry.EndLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.Parent.EndLine.LineNumber - 1);
                }
                SetLineNumbersForEachItem(entry.Children, entry);
                #endregion
            }
            #endregion
        }

        /// <summary>
        /// 删除数组元素
        /// </summary>
        /// <param name="compoundJsonTreeViewItem">当前数组节点</param>
        public void RemoveSubStructure(CompoundJsonTreeViewItem compoundJsonTreeViewItem)
        {
            #region 确定删除的数据类型
            ChangeType changeType = ChangeType.Input;
            if (compoundJsonTreeViewItem.DataType is DataTypes.ArrayElement)
                changeType = ChangeType.RemoveArrayElement;
            else
                if (compoundJsonTreeViewItem.DataType is DataTypes.Compound || compoundJsonTreeViewItem.DataType is DataTypes.CustomCompound)
                changeType = ChangeType.RemoveCompound;
            #endregion

            #region 更新TreeView与JsonView
            Parallel.ForEach(compoundJsonTreeViewItem.Plan.KeyValueContextDictionary.Keys, item =>
            {
                if (item.Contains(compoundJsonTreeViewItem.Path))
                {
                    compoundJsonTreeViewItem.Plan.KeyValueContextDictionary.Remove(item);
                }
            });
            compoundJsonTreeViewItem.Plan.UpdateValueBySpecifyingInterval(compoundJsonTreeViewItem, changeType);
            if (changeType is ChangeType.RemoveCompound)
                compoundJsonTreeViewItem.Children.Clear();
            if (changeType is ChangeType.RemoveArrayElement)
            {
                compoundJsonTreeViewItem.Parent.Children.Remove(compoundJsonTreeViewItem);
                compoundJsonTreeViewItem.Parent.FlattenDescendantNodeList.Remove(compoundJsonTreeViewItem);
                if (compoundJsonTreeViewItem.Parent.Children.Count == 1)
                    compoundJsonTreeViewItem.Parent.Children.Clear();
            }
            #endregion

            #region 更新数组元素前后关系
            if (compoundJsonTreeViewItem.Previous is not null)
                compoundJsonTreeViewItem.Previous.Next = compoundJsonTreeViewItem.Next;
            if (compoundJsonTreeViewItem.Next is not null)
                compoundJsonTreeViewItem.Next.Previous = compoundJsonTreeViewItem.Previous;
            #endregion

            RecursiveRemoveFlattenDescendantNodeList(compoundJsonTreeViewItem.Parent, compoundJsonTreeViewItem);
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