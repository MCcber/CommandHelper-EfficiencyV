using cbhk.CustomControls.JsonTreeViewComponents;
using cbhk.Interface.Json;
using cbhk.Model.Common;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Converters;
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
                list[i].Parent ??= parent;
                if (list[i].IsCanBeDefaulted)
                {
                    continue;
                }

                if (parent is not null)
                {
                    list[i].StartLine = parent.Plan.GetLineByNumber(index + 1 + parent.StartLine.LineNumber);
                }
                else
                if (list[i] is CompoundJsonTreeViewItem subCompoundItem1 && ((subCompoundItem1.DataType is not DataType.OptionalCompound) || (subCompoundItem1.DataType is DataType.OptionalCompound && subCompoundItem1.Children.Count > 0)))
                {
                    list[i].StartLine = list[i].Plan.GetLineByNumber(index + 1);
                }

                if (list[i] is CompoundJsonTreeViewItem subCompoundItem2)
                {
                    if (subCompoundItem2.Children.Count > 0 && subCompoundItem2.Children[^1] is CompoundJsonTreeViewItem subsubCompoundItem && subsubCompoundItem.DataType is not DataType.CustomCompound)
                    {
                        SetLineNumbersForEachItem(subCompoundItem2.Children, subCompoundItem2);
                        if (subCompoundItem2.Children[^1] is CompoundJsonTreeViewItem subsubCompoundItem2)
                        {
                            subCompoundItem2.EndLine = parent.Plan.GetLineByNumber(subsubCompoundItem2.EndLine.LineNumber + 1);
                        }
                        else
                        {
                            subCompoundItem2.EndLine = parent.Plan.GetLineByNumber(subCompoundItem2.Children[^1].StartLine.LineNumber + 1);
                        }
                    }
                    else
                    {
                        subCompoundItem2.EndLine = subCompoundItem2.StartLine;
                    }
                }
                index++;
            }
        }

        public void AddSubStructure(CompoundJsonTreeViewItem compoundJsonTreeViewItem)
        {
            #region 字段
            bool currentIsArray = compoundJsonTreeViewItem.DataType is DataType.List ||
                compoundJsonTreeViewItem.DataType is DataType.Array ||
                compoundJsonTreeViewItem.DataType is DataType.InnerArray;
            bool parentIsArray = compoundJsonTreeViewItem.Parent is not null && (
                compoundJsonTreeViewItem.Parent.DataType is DataType.List || 
                compoundJsonTreeViewItem.Parent.DataType is DataType.Array || 
                compoundJsonTreeViewItem.Parent.DataType is DataType.InnerArray ||
                compoundJsonTreeViewItem.Parent.DataType is DataType.List);

            bool currentIsCompound = compoundJsonTreeViewItem.DataType is DataType.Compound ||
                compoundJsonTreeViewItem.DataType is DataType.CustomCompound ||
                compoundJsonTreeViewItem.DataType is DataType.NullableCompound ||
                compoundJsonTreeViewItem.DataType is DataType.OptionalCompound;
            bool IsArrayOrList = currentIsArray || parentIsArray;
            JsonTreeViewDataStructure result = new();
            List<string> CurrentDependencyItemList = [];
            int customItemCount = 0;
            CompoundJsonTreeViewItem targetItem = compoundJsonTreeViewItem.DataType is DataType.CustomCompound ? compoundJsonTreeViewItem.Parent : compoundJsonTreeViewItem;
            ChangeType changeType = ChangeType.String;
            CompoundJsonTreeViewItem targetCompoundItem;
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

            #region 定位相邻的已有值的两个节点
            JsonTreeViewItem previous = compoundJsonTreeViewItem.Previous;
            CompoundJsonTreeViewItem parent = compoundJsonTreeViewItem.Parent;
            JsonTreeViewItem next = compoundJsonTreeViewItem.Next;
            CompoundJsonTreeViewItem previousCompound = null;
            CompoundJsonTreeViewItem nextCompound = null;
            while (previous is not null && previous.StartLine is null)
            {
                if (previous.Previous is null)
                {
                    break;
                }
                previous = previous.Previous;
            }

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

            while (next is not null && next.StartLine is null)
            {
                if (next.Next is null)
                {
                    break;
                }
                next = next.Next;
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

            #region 实例化复合节点子链表信息
            if (currentIsCompound && compoundJsonTreeViewItem.ChildrenStringList.Count > 0)
            {
                #region 定义前置与末尾衔接符
                string connectorSymbol = "\r\n";
                string endConnectorSymbol = "";
                if(previous is not null && previous.StartLine is not null && (next is null || (next is not null && next.StartLine is null)))
                {
                    connectorSymbol = ",\r\n";
                }
                #endregion

                #region 提取需要分析的成员
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
                if(compoundJsonTreeViewItem.ChildrenStringList.Count > 1)
                {
                    CurrentDependencyItemList = compoundJsonTreeViewItem.ChildrenStringList;
                }
                else
                if(compoundJsonTreeViewItem.EnumKey.Length > 0)
                {

                }
                #endregion

                #region 执行分析
                result = htmlHelper.GetTreeViewItemResult(new(), CurrentDependencyItemList, compoundJsonTreeViewItem.LayerCount + 1, (string)compoundJsonTreeViewItem.Value, compoundJsonTreeViewItem.DataType is DataType.CustomCompound ? compoundJsonTreeViewItem : null, null, 1, true);

                if (compoundJsonTreeViewItem.Parent is not null && compoundJsonTreeViewItem.Parent.Children.Count > 0 && compoundJsonTreeViewItem.Parent.Children[0] is CompoundJsonTreeViewItem subCompoundItem1 && subCompoundItem1.DataType is DataType.CustomCompound)
                {
                    customItemCount = 1;
                }
                else
                if(compoundJsonTreeViewItem.Parent is not null && compoundJsonTreeViewItem.Parent.Children.Count > 1 && compoundJsonTreeViewItem.Parent.Children[1] is CompoundJsonTreeViewItem subCompoundItem2 && subCompoundItem2.DataType is DataType.CustomCompound)
                {
                    customItemCount = 2;
                }

                bool addMoreCustomStructure = compoundJsonTreeViewItem.DataType is DataType.CustomCompound && compoundJsonTreeViewItem.Parent is not null && compoundJsonTreeViewItem.Parent.Children.Count - customItemCount > 0;

                bool addMoreStructure = (compoundJsonTreeViewItem.DataType is DataType.NullableCompound || compoundJsonTreeViewItem.DataType is DataType.OptionalCompound || compoundJsonTreeViewItem.DataType is DataType.Compound) && next is null || (next is not null && next.StartLine is not null);

                CompoundJsonTreeViewItem endConnectorSymbolItem = null;
                if (addMoreCustomStructure)
                {
                    endConnectorSymbolItem = compoundJsonTreeViewItem.Parent;
                }
                else
                if(addMoreStructure)
                {
                    endConnectorSymbolItem = compoundJsonTreeViewItem;
                }
                if(endConnectorSymbolItem is not null && (addMoreCustomStructure && endConnectorSymbolItem.Children.Count - customItemCount > 0) || (addMoreStructure && next is not null && next.StartLine is not null))
                {
                    endConnectorSymbol = ",";
                }
                #endregion

                #region 添加当前节点的子节点集
                if(compoundJsonTreeViewItem.DataType is DataType.CustomCompound && compoundJsonTreeViewItem.Parent is not null)
                {
                    compoundJsonTreeViewItem.Parent.Children.Insert(1,result.Result[0]);
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

                    if(compoundJsonTreeViewItem.DataType is not DataType.CustomCompound)
                    {
                        result.ResultString.Insert(0,new string(' ',compoundJsonTreeViewItem.LayerCount * 2) + "\"" + compoundJsonTreeViewItem.Key + "\": {\r\n").Append("\r\n" + new string(' ',compoundJsonTreeViewItem.LayerCount * 2) + '}');
                    }

                    //替换前分离首末行的引用避免执行替换时首行产生变动
                    if(compoundJsonTreeViewItem.StartLine is null && previousCompound is not null && previousCompound.EndLine is not null)
                    {
                        compoundJsonTreeViewItem.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(previousCompound.EndLine.LineNumber + 1);
                    }
                    else
                    if(compoundJsonTreeViewItem.StartLine is null && previous is not null && previous.StartLine is not null)
                    {
                        compoundJsonTreeViewItem.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(previous.StartLine.LineNumber + 1);
                    }

                    compoundJsonTreeViewItem.StartLine ??= compoundJsonTreeViewItem.Plan.GetLineByNumber(2);

                    int startLineNumber = compoundJsonTreeViewItem.StartLine is not null ? compoundJsonTreeViewItem.StartLine.LineNumber : 2;
                    int endlineNumber = compoundJsonTreeViewItem.EndLine is not null ? compoundJsonTreeViewItem.EndLine.LineNumber : compoundJsonTreeViewItem.StartLine.LineNumber;
                    compoundJsonTreeViewItem.EndLine = null;

                    //整理最终拼接字符串
                    string newLine = "";
                    if(compoundJsonTreeViewItem.DataType is DataType.CustomCompound && compoundJsonTreeViewItem.Parent is not null)
                    {
                        newLine = compoundJsonTreeViewItem.Parent.Children.Count - customItemCount == 1 ? "\r\n" + new string(' ', compoundJsonTreeViewItem.Parent.LayerCount * 2) : "";
                    }
                    else
                        if(compoundJsonTreeViewItem.Children.Count == 0)
                    {
                        newLine = compoundJsonTreeViewItem.Parent.Children.Count == 1 ? "\r\n" + new string(' ', compoundJsonTreeViewItem.LayerCount * 2) : "";
                    }

                    string currentNewString = connectorSymbol + result.ResultString.ToString() + endConnectorSymbol + newLine;

                    #region 执行替换
                    if (previous is CompoundJsonTreeViewItem previousCompoundItem && compoundJsonTreeViewItem.DataType is DataType.OptionalCompound)
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
                    int newLineCount = 0;
                    if (compoundJsonTreeViewItem.Children.Count == 1)
                    {
                        newLineCount = GetLineBreakCount().Matches(currentNewString).Count + (!string.IsNullOrEmpty(connectorSymbol) ? -1 : 0) + (result.Result[^1] is CompoundJsonTreeViewItem lastCompoundChildItem && (lastCompoundChildItem.DataType is not DataType.OptionalCompound ||
                            lastCompoundChildItem.ValueTypeList.Select(item => item.Text.Contains("compound")).Count() > 0) && result.Result[^1].Key != compoundJsonTreeViewItem.Key ? 1 : 0);
                    }
                    if (compoundJsonTreeViewItem.StartLine is null)
                    {
                        if (previous is not null)
                        {
                            if (previousCompound.EndLine is not null)
                            {
                                compoundJsonTreeViewItem.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(previousCompound.EndLine.LineNumber + 1);
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

                    #region 确定首次添加子级时的末尾行号
                    if (compoundJsonTreeViewItem.Children.Count == 1)
                    {
                        compoundJsonTreeViewItem.EndLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(compoundJsonTreeViewItem.StartLine.LineNumber + newLineCount);
                    }
                    #endregion
                }
                else
                {
                    //执行替换
                    if (previousCompound.EndLine is not null)
                    {
                        compoundJsonTreeViewItem.Plan.UpdateNullValueBySpecifyingInterval(previousCompound.EndLine.EndOffset, connectorSymbol + new string(' ',compoundJsonTreeViewItem.LayerCount * 2) + "\"" + compoundJsonTreeViewItem.Key + "\": {}" + endConnectorSymbol);
                        compoundJsonTreeViewItem.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(previousCompound.EndLine.LineNumber + 1);
                    }
                    else
                    {
                        compoundJsonTreeViewItem.Plan.UpdateNullValueBySpecifyingInterval(previousCompound.StartLine.EndOffset, connectorSymbol + new string(' ', compoundJsonTreeViewItem.LayerCount * 2) + "\"" + compoundJsonTreeViewItem.Key + "\": {}" + endConnectorSymbol);
                        compoundJsonTreeViewItem.StartLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(previousCompound.StartLine.LineNumber + 1);
                    }
                }
                #endregion

                #region 将子结构行号与代码编辑器对齐
                ObservableCollection<JsonTreeViewItem> subDependencyItemList = result.Result;
                SetLineNumbersForEachItem(subDependencyItemList, targetItem);
                #endregion

                #region 确认已有子级时的末尾行号
                if (compoundJsonTreeViewItem.Children.Count > 1)
                {
                    if (compoundJsonTreeViewItem.Children[^1] is CompoundJsonTreeViewItem lastChildItem1 && lastChildItem1.EndLine is not null)
                    {
                        compoundJsonTreeViewItem.EndLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(lastChildItem1.EndLine.LineNumber + 1);
                    }
                    else
                    if (compoundJsonTreeViewItem.Children[^1] is CompoundJsonTreeViewItem lastChildItem2 && lastChildItem2.EndLine is not null)
                    {
                        compoundJsonTreeViewItem.EndLine = compoundJsonTreeViewItem.Plan.GetLineByNumber(lastChildItem2.StartLine.LineNumber + 1);
                    }

                    compoundJsonTreeViewItem.EndLine ??= compoundJsonTreeViewItem.StartLine;
                }
                #endregion

                #region 根据当前节点类型判断是否切换功能按钮
                if (compoundJsonTreeViewItem.Children.Count > 0 && ((compoundJsonTreeViewItem.Children[0] is CompoundJsonTreeViewItem childItem && childItem.DataType is not DataType.CustomCompound) || compoundJsonTreeViewItem.Children[0] is not CompoundJsonTreeViewItem))
                {
                    compoundJsonTreeViewItem.AddElementButtonVisibility = Visibility.Collapsed;
                    compoundJsonTreeViewItem.RemoveElementButtonVisibility = Visibility.Visible;
                }
                #endregion

            }
            #endregion

            #region 实例化数组/列表节点子链表信息
            if ((currentIsArray && compoundJsonTreeViewItem.ChildrenStringList.Count > 0) || 
                (parentIsArray && compoundJsonTreeViewItem.Parent is not null && compoundJsonTreeViewItem.Parent.ChildrenStringList.Count > 0))
            {
                string connectorSymbol = (currentIsArray && previous is not null) || (parentIsArray && parent is not null) ? ",\r\n" : "";
                result = htmlHelper.GetTreeViewItemResult(new(), compoundJsonTreeViewItem.ChildrenStringList, compoundJsonTreeViewItem.LayerCount + 1, "", compoundJsonTreeViewItem, null, 1, true);
                string currentNewString = result.ResultString.ToString();
                CompoundJsonTreeViewItem entry = new(compoundJsonTreeViewItem.Plan, compoundJsonTreeViewItem.JsonItemTool,_container)
                {
                    Path = currentIsArray ? compoundJsonTreeViewItem.Path + "[" + 0 + "]" : compoundJsonTreeViewItem.Parent.Path + "[" + (compoundJsonTreeViewItem.Parent.Children.Count - 1) + "]",
                    Parent = currentIsCompound ? compoundJsonTreeViewItem : compoundJsonTreeViewItem.Parent,
                    DisplayText = "Entry",
                    DataType = DataType.ArrayElement,
                    LayerCount = compoundJsonTreeViewItem.LayerCount + 1,
                    RemoveElementButtonVisibility = Visibility.Visible
                };
                entry.Children.AddRange(result.Result);
                if(compoundJsonTreeViewItem.DataType is DataType.Array || compoundJsonTreeViewItem.DataType is DataType.InnerArray)
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

            #region 更新当前节点与父节点的末行引用

            #region 当前节点
            if (targetItem.Children.Count == 1 || targetItem.Children.Count - customItemCount == 1)
            {
                if (targetItem.Children[customItemCount] is CompoundJsonTreeViewItem childItem && childItem.EndLine is not null)
                {
                    targetItem.EndLine = targetItem.Plan.GetLineByNumber(childItem.EndLine.LineNumber + 1);
                }
                else
                {
                    targetItem.EndLine = targetItem.Plan.GetLineByNumber(targetItem.Children[customItemCount].StartLine.LineNumber + 1);
                }
            }
            #endregion

            #region 父节点
            if (targetItem is not null && targetItem.Parent is not null && targetItem.Parent.Children.Count == 1)
            {
                if(targetItem.EndLine is not null)
                {
                    targetItem.Parent.EndLine = targetItem.Plan.GetLineByNumber(targetItem.EndLine.LineNumber + 1);
                }
                else
                {
                    targetItem.Parent.EndLine = targetItem.Plan.GetLineByNumber(targetItem.StartLine.LineNumber + 1);
                }
            }
            #endregion

            #endregion
        }

        /// <summary>
        /// 删除数组元素
        /// </summary>
        /// <param name="compoundJsonTreeViewItem">当前数组节点</param>
        public void RemoveSubStructure(CompoundJsonTreeViewItem compoundJsonTreeViewItem)
        {
            #region 确定删除的数据类型
            ChangeType changeType = ChangeType.String;
            if (compoundJsonTreeViewItem.DataType is DataType.ArrayElement)
            {
                changeType = ChangeType.RemoveArrayElement;
            }
            else
            if (compoundJsonTreeViewItem.DataType is DataType.Compound || compoundJsonTreeViewItem.DataType is DataType.CustomCompound)
            {
                changeType = ChangeType.RemoveCompound;
            }
            else
            if(compoundJsonTreeViewItem.DataType is DataType.String)
            {
                changeType = ChangeType.String;
            }
            else
            {
                changeType = ChangeType.NumberAndBool;
            }
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